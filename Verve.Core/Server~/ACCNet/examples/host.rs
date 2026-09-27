//! Minimal host integration; admission and business authorization belong here.
use quinn::rustls::pki_types::{CertificateDer, PrivateKeyDer};
use verve_acc_net::{AccEndpoint, Limits, NetResult};

#[tokio::main]
async fn main() -> NetResult<()> {
    let args: Vec<_> = std::env::args().collect();
    if args.len() != 4 {
        return Err("usage: host certificate.der private-key.der accnet.toml".into());
    }
    let endpoint = AccEndpoint::server(
        "127.0.0.1:9000".parse()?,
        vec![CertificateDer::from(std::fs::read(&args[1])?)],
        PrivateKeyDer::try_from(std::fs::read(&args[2])?)?,
        None,
        Limits::load(&args[3])?,
    )?;
    let mut sessions = tokio::task::JoinSet::new();
    loop {
        tokio::select! {
            result = endpoint.accept() => match result {
                Ok(Some(mut peer)) => { sessions.spawn(async move {
                    // Replace this loop with the project's own authorization and routing.
                    // A packet's entity IDs are untrusted data, not logged-in player IDs.
                    loop {
                        match peer.recv().await {
                            Ok(frame) => eprintln!("received {} bytes; awaiting host policy", frame.len()),
                            Err(error) => { eprintln!("session ended: {error}"); break; }
                        }
                    }
                }); },
                Ok(None) => break,
                Err(error) => eprintln!("connection rejected: {error}"),
            },
            result = sessions.join_next(), if !sessions.is_empty() => {
                if let Some(Err(error)) = result { eprintln!("session failed: {error}"); }
            }
        }
    }
    Ok(())
}
