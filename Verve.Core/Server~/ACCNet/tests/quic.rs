use bytes::Bytes;
use quinn::rustls::{
    self,
    pki_types::{CertificateDer, PrivateKeyDer},
    RootCertStore,
};
use std::{net::SocketAddr, sync::Arc, time::Duration};
use verve_acc_net::{
    protocol::HEADER_SIZE, AccEndpoint, AccPeer, Limits, NetResult, SendError, ALPN,
};

fn local() -> SocketAddr {
    "127.0.0.1:0".parse().unwrap()
}
fn identity() -> (CertificateDer<'static>, PrivateKeyDer<'static>) {
    let cert = rcgen::generate_simple_self_signed(vec!["localhost".into()]).unwrap();
    (
        cert.cert.der().clone(),
        PrivateKeyDer::try_from(cert.key_pair.serialize_der()).unwrap(),
    )
}
fn trust(cert: &CertificateDer<'static>) -> RootCertStore {
    let mut roots = RootCertStore::empty();
    roots.add(cert.clone()).unwrap();
    roots
}
fn packet(sequence: u64, length: usize) -> Bytes {
    let sequence = sequence.max(1);
    let payload = length - HEADER_SIZE;
    let mut bytes = vec![0; length];
    bytes[..4].copy_from_slice(&0x4343_4156u32.to_le_bytes());
    bytes[4] = 2;
    bytes[5] = if sequence == 1 { 1 } else { 2 };
    bytes[8..16].copy_from_slice(&7u64.to_le_bytes());
    bytes[16..24].copy_from_slice(&sequence.to_le_bytes());
    bytes[24..32].copy_from_slice(&(sequence - 1).to_le_bytes());
    bytes[32..40].copy_from_slice(&sequence.to_le_bytes());
    bytes[40..44].copy_from_slice(&(payload as u32).to_le_bytes());
    Bytes::from(bytes)
}

fn marker_validator(packet: &[u8]) -> NetResult<()> {
    if packet.first().copied() != Some(0x7F) {
        return Err("custom frame marker is missing".into());
    }
    Ok(())
}
async fn pair(limits: Limits) -> (AccEndpoint, AccEndpoint, AccPeer, AccPeer) {
    let (cert, key) = identity();
    let client = AccEndpoint::client(local(), trust(&cert), None, limits.clone()).unwrap();
    let server = AccEndpoint::server(local(), vec![cert], key, None, limits).unwrap();
    let (a, b) = tokio::join!(
        server.accept(),
        client.connect(server.local_addr().unwrap(), "localhost")
    );
    (server, client, a.unwrap().unwrap(), b.unwrap())
}
#[tokio::test]
async fn encrypted_bidirectional_frames_preserve_order_under_load() {
    let limits = Limits {
        frames_per_second: 100_000,
        bytes_per_second: 1024 * 1024 * 1024,
        ..Limits::default()
    };
    let (_server, _client, mut a, b) = pair(limits).await;
    let sender = tokio::spawn(async move {
        for sequence in 1..=10_000 {
            let mut data = packet(sequence, 128);
            loop {
                match b.try_send(data) {
                    Ok(()) => break,
                    Err(SendError::Full(returned)) => {
                        data = returned;
                        tokio::task::yield_now().await;
                    }
                    Err(e) => panic!("{e}"),
                }
            }
        }
        b
    });
    let started = std::time::Instant::now();
    for seq in 1..=10_000 {
        let frame = tokio::time::timeout(Duration::from_secs(10), a.recv())
            .await
            .unwrap()
            .unwrap();
        assert_eq!(frame, packet(seq, 128));
    }
    let mut b = sender.await.unwrap();
    let largest = packet(10001, 64 * 1024);
    a.try_send(largest.clone()).unwrap();
    assert_eq!(
        tokio::time::timeout(Duration::from_secs(10), b.recv())
            .await
            .unwrap()
            .unwrap(),
        largest
    );
    eprintln!("QUIC loopback: 10000 x 128 B + reverse 64 KiB in {:?} (includes channel/network processing)",started.elapsed());
}
#[tokio::test]
async fn dropping_owner_closes_remote_and_releases_capacity() {
    let (server, _client, a, mut b) = pair(Limits {
        max_connections: 1,
        ..Limits::default()
    })
    .await;
    drop(a);
    assert!(tokio::time::timeout(Duration::from_secs(2), b.recv())
        .await
        .unwrap()
        .is_err());
    drop(server);
}
#[tokio::test]
async fn untrusted_certificate_and_wrong_name_are_rejected() {
    for wrong_name in [false, true] {
        let (cert, key) = identity();
        let (untrusted, _) = identity();
        let roots = if wrong_name {
            trust(&cert)
        } else {
            trust(&untrusted)
        };
        let server =
            AccEndpoint::server(local(), vec![cert], key, None, Limits::default()).unwrap();
        let client = AccEndpoint::client(local(), roots, None, Limits::default()).unwrap();
        let (a, b) = tokio::join!(
            server.accept(),
            client.connect(
                server.local_addr().unwrap(),
                if wrong_name {
                    "wrong.local"
                } else {
                    "localhost"
                }
            )
        );
        assert!(a.is_err());
        assert!(b.is_err());
    }
}
#[tokio::test]
async fn mutual_tls_requires_and_exposes_client_identity() {
    let (server_cert, key) = identity();
    let (client_cert, client_key) = identity();
    let server = AccEndpoint::server(
        local(),
        vec![server_cert.clone()],
        key,
        Some(trust(&client_cert)),
        Limits::default(),
    )
    .unwrap();
    let client =
        AccEndpoint::client(local(), trust(&server_cert), None, Limits::default()).unwrap();
    let (a, b) = tokio::join!(
        server.accept(),
        client.connect(server.local_addr().unwrap(), "localhost")
    );
    assert!(a.is_err());
    assert!(b.is_err());
    let client = AccEndpoint::client(
        local(),
        trust(&server_cert),
        Some((vec![client_cert.clone()], client_key)),
        Limits::default(),
    )
    .unwrap();
    let (a, b) = tokio::join!(
        server.accept(),
        client.connect(server.local_addr().unwrap(), "localhost")
    );
    let a = a.unwrap().unwrap();
    let _b = b.unwrap();
    assert_eq!(a.peer_certificates().unwrap(), vec![client_cert]);
}
#[tokio::test]
async fn invalid_limits_fail_before_binding() {
    let (cert, key) = identity();
    assert!(AccEndpoint::server(
        local(),
        vec![cert],
        key,
        None,
        Limits {
            queue_frames: 0,
            ..Limits::default()
        }
    )
    .is_err());
}
#[tokio::test]
async fn live_reload_changes_only_receive_rates_and_is_transactional() {
    let (server, _client, mut receiving, sending) = pair(Limits::default()).await;
    let invalid_changes: [fn(&mut Limits); 7] = [
        |limits| limits.max_frame /= 2,
        |limits| limits.max_state_bytes /= 2,
        |limits| limits.queue_frames += 1,
        |limits| limits.max_connections += 1,
        |limits| limits.timeout += Duration::from_millis(1),
        |limits| limits.frames_per_second = 0,
        |limits| limits.bytes_per_second = 1,
    ];
    for change in invalid_changes {
        let mut limits = Limits {
            frames_per_second: 1,
            ..Limits::default()
        };
        change(&mut limits);
        assert!(server.reload_limits(limits).is_err());
    }
    assert!(server
        .reload("missing-acc-config-directory/accnet.toml")
        .is_err());

    // 任一次失败都不能先发布 frames=1；原连接仍能连续接收多帧。
    for sequence in 1..=8 {
        let frame = packet(sequence, 128);
        sending.try_send(frame.clone()).unwrap();
        assert_eq!(
            tokio::time::timeout(Duration::from_secs(2), receiving.recv())
                .await
                .unwrap()
                .unwrap(),
            frame
        );
    }
}

#[tokio::test]
async fn live_reload_updates_existing_peers_without_resetting_received_usage() {
    for by_bytes in [false, true] {
        let limits = Limits {
            frames_per_second: 1,
            ..Limits::default()
        };
        let (server, _client, mut receiving, sending) = pair(limits).await;
        let raised = Limits {
            frames_per_second: 8,
            ..Limits::default()
        };
        server.reload_limits(raised.clone()).unwrap();
        let size = if by_bytes { raised.max_frame } else { 128 };
        for sequence in 1..=2 {
            sending.try_send(packet(sequence, size)).unwrap();
            tokio::time::timeout(Duration::from_secs(2), receiving.recv())
                .await
                .unwrap()
                .unwrap();
        }

        let mut lowered = raised;
        if by_bytes {
            lowered.bytes_per_second = size as u64;
        } else {
            lowered.frames_per_second = 1;
        }
        server.reload_limits(lowered).unwrap();
        sending.try_send(packet(3, size)).unwrap();
        let error = tokio::time::timeout(Duration::from_secs(2), receiving.recv())
            .await
            .unwrap()
            .unwrap_err();
        assert!(!error.to_string().is_empty());
    }
}

#[tokio::test]
async fn file_reload_before_accept_applies_to_future_peers() {
    let (cert, key) = identity();
    let client = AccEndpoint::client(local(), trust(&cert), None, Limits::default()).unwrap();
    let server = AccEndpoint::server(
        local(),
        vec![cert],
        key,
        None,
        Limits {
            frames_per_second: 1,
            ..Limits::default()
        },
    )
    .unwrap();
    // 示例配置将帧预算提升到 2048；此时 watch 尚无连接订阅者。
    server
        .reload(concat!(env!("CARGO_MANIFEST_DIR"), "/accnet.toml"))
        .unwrap();
    let (receiver, sender) = tokio::join!(
        server.accept(),
        client.connect(server.local_addr().unwrap(), "localhost")
    );
    let mut receiver = receiver.unwrap().unwrap();
    let sender = sender.unwrap();
    for sequence in 1..=4 {
        sender.try_send(packet(sequence, 128)).unwrap();
        tokio::time::timeout(Duration::from_secs(2), receiver.recv())
            .await
            .unwrap()
            .unwrap();
    }
}
#[tokio::test]
async fn queue_is_bounded_and_packet_rejection_preserves_connection() {
    // Current-thread executor: background writer cannot drain before assertions.
    let (_server, _client, _a, b) = pair(Limits {
        queue_frames: 1,
        ..Limits::default()
    })
    .await;
    b.try_send(packet(1, 60)).unwrap();
    assert!(matches!(b.try_send(packet(2, 60)), Err(SendError::Full(_))));
    assert!(matches!(
        b.try_send(Bytes::from_static(b"invalid")),
        Err(SendError::Invalid(_))
    ));
}

#[tokio::test]
async fn custom_packet_validator_reuses_transport_ownership() {
    let (cert, key) = identity();
    let limits = Limits::default();
    let client = AccEndpoint::client_with_validator(
        local(),
        trust(&cert),
        None,
        limits.clone(),
        marker_validator,
    )
    .unwrap();
    let server = AccEndpoint::server_with_validator(
        local(),
        vec![cert],
        key,
        None,
        limits,
        marker_validator,
    )
    .unwrap();
    let (server_peer, client_peer) = tokio::join!(
        server.accept(),
        client.connect(server.local_addr().unwrap(), "localhost")
    );
    let mut server_peer = server_peer.unwrap().unwrap();
    let client_peer = client_peer.unwrap();

    assert!(matches!(
        client_peer.try_send(Bytes::from_static(&[0x01])),
        Err(SendError::Invalid(_))
    ));
    let valid = Bytes::from_static(&[0x7F, 0x01, 0x02]);
    client_peer.try_send(valid.clone()).unwrap();
    assert_eq!(server_peer.recv().await.unwrap(), valid);
}
async fn raw_client(
    server: &AccEndpoint,
    cert: CertificateDer<'static>,
) -> (
    quinn::Endpoint,
    quinn::Connection,
    quinn::SendStream,
    quinn::RecvStream,
) {
    let mut tls = rustls::ClientConfig::builder_with_provider(Arc::new(
        rustls::crypto::ring::default_provider(),
    ))
    .with_protocol_versions(&[&rustls::version::TLS13])
    .unwrap()
    .with_root_certificates(trust(&cert))
    .with_no_client_auth();
    tls.alpn_protocols = vec![ALPN.to_vec()];
    let config = quinn::ClientConfig::new(Arc::new(
        quinn::crypto::rustls::QuicClientConfig::try_from(tls).unwrap(),
    ));
    let mut endpoint = quinn::Endpoint::client(local()).unwrap();
    endpoint.set_default_client_config(config);
    let connection = endpoint
        .connect(server.local_addr().unwrap(), "localhost")
        .unwrap()
        .await
        .unwrap();
    let (mut send, mut recv) = connection.open_bi().await.unwrap();
    send.write_all(b"VACCQ002").await.unwrap();
    let mut preface = [0; 8];
    recv.read_exact(&mut preface).await.unwrap();
    (endpoint, connection, send, recv)
}
#[tokio::test]
async fn oversized_truncated_and_malformed_frames_close_connection() {
    for mode in 0..3 {
        let (cert, key) = identity();
        let limits = Limits {
            timeout: Duration::from_secs(2),
            ..Limits::default()
        };
        let server = AccEndpoint::server(local(), vec![cert.clone()], key, None, limits).unwrap();
        let (peer, raw) = tokio::join!(server.accept(), raw_client(&server, cert));
        let mut peer = peer.unwrap().unwrap();
        let (_endpoint, _connection, mut send, _recv) = raw;
        match mode {
            0 => send.write_all(&u32::MAX.to_le_bytes()).await.unwrap(),
            1 => {
                send.write_all(&100u32.to_le_bytes()).await.unwrap();
                send.write_all(&[1; 3]).await.unwrap();
                send.finish().unwrap();
            }
            _ => {
                send.write_all(&60u32.to_le_bytes()).await.unwrap();
                send.write_all(&[0; 60]).await.unwrap();
            }
        }
        assert!(tokio::time::timeout(Duration::from_secs(3), peer.recv())
            .await
            .unwrap()
            .is_err());
    }
}
#[tokio::test]
async fn slow_partial_frame_and_rate_abuse_are_bounded() {
    for mode in 0..2 {
        let (cert, key) = identity();
        let limits = Limits {
            timeout: Duration::from_millis(500),
            frames_per_second: 1,
            ..Limits::default()
        };
        let server = AccEndpoint::server(local(), vec![cert.clone()], key, None, limits).unwrap();
        let (peer, raw) = tokio::join!(server.accept(), raw_client(&server, cert));
        let mut peer = peer.unwrap().unwrap();
        let (_endpoint, _connection, mut send, _recv) = raw;
        if mode == 0 {
            send.write_all(&[60]).await.unwrap();
        } else {
            for seq in 0..2 {
                send.write_all(&60u32.to_le_bytes()).await.unwrap();
                send.write_all(&packet(seq + 1, 60)).await.unwrap();
            }
            let _ = peer.recv().await.unwrap();
        }
        assert!(tokio::time::timeout(Duration::from_secs(2), peer.recv())
            .await
            .unwrap()
            .is_err());
    }
}
