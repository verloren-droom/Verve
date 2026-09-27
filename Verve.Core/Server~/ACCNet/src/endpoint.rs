//! 端点；负责 TLS 配置、连接接管和接收预算发布。

use crate::{AccPacketValidator, AccPeer, Limits, NetResult, PacketValidator, ALPN};
use quinn::rustls::{
    self,
    pki_types::{CertificateDer, PrivateKeyDer},
    RootCertStore,
};
use quinn::{Connection, Endpoint};
use std::{error::Error, net::SocketAddr, sync::Arc};
use tokio::sync::{watch, OwnedSemaphorePermit, Semaphore};

const PREFACE: &[u8; 8] = b"VACCQ002";

/// 可热更新的接收预算；整组发布，避免新旧字段混用。
#[derive(Clone, Copy)]
pub(crate) struct ReceiveRates {
    pub(crate) frames: u32,
    pub(crate) bytes: u64,
}

impl From<&Limits> for ReceiveRates {
    fn from(limits: &Limits) -> Self {
        Self {
            frames: limits.frames_per_second,
            bytes: limits.bytes_per_second,
        }
    }
}

/// QUIC 端点；释放时关闭所拥有的全部连接。
pub struct AccEndpoint {
    endpoint: Endpoint,
    limits: Limits,
    slots: Arc<Semaphore>,
    validator: Arc<dyn PacketValidator>,
    rates: watch::Sender<ReceiveRates>,
}

impl AccEndpoint {
    /// 绑定服务端；传入证书链及私钥，可用客户端 CA 开启强制双向认证。
    pub fn server(
        bind: SocketAddr,
        certificates: Vec<CertificateDer<'static>>,
        key: PrivateKeyDer<'static>,
        client_ca: Option<RootCertStore>,
        limits: Limits,
    ) -> NetResult<Self> {
        let validator = AccPacketValidator {
            max_state_bytes: limits.max_state_bytes,
        };
        Self::server_with_validator(bind, certificates, key, client_ca, limits, validator)
    }

    /// 使用自定义帧校验策略绑定服务端。
    pub fn server_with_validator<V>(
        bind: SocketAddr,
        certificates: Vec<CertificateDer<'static>>,
        key: PrivateKeyDer<'static>,
        client_ca: Option<RootCertStore>,
        limits: Limits,
        validator: V,
    ) -> NetResult<Self>
    where
        V: PacketValidator + 'static,
    {
        limits.validate()?;
        let builder = rustls::ServerConfig::builder_with_provider(Arc::new(
            rustls::crypto::ring::default_provider(),
        ))
        .with_protocol_versions(&[&rustls::version::TLS13])?;
        let mut tls = match client_ca {
            Some(roots) => builder
                .with_client_cert_verifier(
                    rustls::server::WebPkiClientVerifier::builder_with_provider(
                        Arc::new(roots),
                        Arc::new(rustls::crypto::ring::default_provider()),
                    )
                    .build()?,
                )
                .with_single_cert(certificates, key)?,
            None => builder
                .with_no_client_auth()
                .with_single_cert(certificates, key)?,
        };
        tls.alpn_protocols = vec![ALPN.to_vec()];
        tls.max_early_data_size = 0;
        let crypto = quinn::crypto::rustls::QuicServerConfig::try_from(tls)?;
        let mut config = quinn::ServerConfig::with_crypto(Arc::new(crypto));
        config.transport_config(limits.transport()?);
        Ok(Self {
            endpoint: Endpoint::server(config, bind)?,
            slots: Arc::new(Semaphore::new(limits.max_connections)),
            rates: watch::channel(ReceiveRates::from(&limits)).0,
            limits,
            validator: Arc::new(validator),
        })
    }

    /// 创建客户端；必须提供受信任 CA，证书名称在连接时校验。
    pub fn client(
        bind: SocketAddr,
        roots: RootCertStore,
        identity: Option<(Vec<CertificateDer<'static>>, PrivateKeyDer<'static>)>,
        limits: Limits,
    ) -> NetResult<Self> {
        let validator = AccPacketValidator {
            max_state_bytes: limits.max_state_bytes,
        };
        Self::client_with_validator(bind, roots, identity, limits, validator)
    }

    /// 使用自定义帧校验策略创建客户端。
    pub fn client_with_validator<V>(
        bind: SocketAddr,
        roots: RootCertStore,
        identity: Option<(Vec<CertificateDer<'static>>, PrivateKeyDer<'static>)>,
        limits: Limits,
        validator: V,
    ) -> NetResult<Self>
    where
        V: PacketValidator + 'static,
    {
        limits.validate()?;
        let builder = rustls::ClientConfig::builder_with_provider(Arc::new(
            rustls::crypto::ring::default_provider(),
        ))
        .with_protocol_versions(&[&rustls::version::TLS13])?
        .with_root_certificates(roots);
        let mut tls = match identity {
            Some((certs, key)) => builder.with_client_auth_cert(certs, key)?,
            None => builder.with_no_client_auth(),
        };
        tls.alpn_protocols = vec![ALPN.to_vec()];
        tls.enable_early_data = false;
        let mut config = quinn::ClientConfig::new(Arc::new(
            quinn::crypto::rustls::QuicClientConfig::try_from(tls)?,
        ));
        config.transport_config(limits.transport()?);
        let mut endpoint = Endpoint::client(bind)?;
        endpoint.set_default_client_config(config);
        Ok(Self {
            endpoint,
            slots: Arc::new(Semaphore::new(limits.max_connections)),
            rates: watch::channel(ReceiveRates::from(&limits)).0,
            limits,
            validator: Arc::new(validator),
        })
    }

    /// 接收连接；调用方决定该连接可以访问哪些玩家和世界。
    pub async fn accept(&self) -> NetResult<Option<AccPeer>> {
        let Some(incoming) = self.endpoint.accept().await else {
            return Ok(None);
        };
        let permit = match self.slots.clone().try_acquire_owned() {
            Ok(permit) => permit,
            Err(_) => {
                incoming.refuse();
                return Err("connection limit reached".into());
            }
        };
        let connection = tokio::time::timeout(self.limits.timeout, incoming).await??;
        self.prepare(connection, permit, false).await.map(Some)
    }

    /// 连接并校验远端证书；不提供跳过 TLS 校验的入口。
    pub async fn connect(&self, address: SocketAddr, server_name: &str) -> NetResult<AccPeer> {
        let permit = self.slots.clone().try_acquire_owned()?;
        let connection = tokio::time::timeout(
            self.limits.timeout,
            self.endpoint.connect(address, server_name)?,
        )
        .await??;
        self.prepare(connection, permit, true).await
    }

    async fn prepare(
        &self,
        connection: Connection,
        permit: OwnedSemaphorePermit,
        client: bool,
    ) -> NetResult<AccPeer> {
        let result = tokio::time::timeout(self.limits.timeout, async {
            let (mut send, mut recv) = if client {
                connection.open_bi().await?
            } else {
                connection.accept_bi().await?
            };
            if client {
                send.write_all(PREFACE).await?;
            }
            let mut preface = [0; 8];
            recv.read_exact(&mut preface).await?;
            if &preface != PREFACE {
                return Err("invalid ACC transport preface".into());
            }
            if !client {
                send.write_all(PREFACE).await?;
            }
            Ok::<_, Box<dyn Error + Send + Sync>>((send, recv))
        })
        .await;
        match result {
            Ok(Ok((send, recv))) => Ok(AccPeer::start(
                connection,
                send,
                recv,
                self.limits.clone(),
                self.validator.clone(),
                self.rates.subscribe(),
                permit,
            )),
            failure => {
                connection.close(1_u32.into(), b"ACC stream handshake failed");
                match failure {
                    Ok(Err(error)) => Err(error),
                    Err(error) => Err(error.into()),
                    _ => unreachable!(),
                }
            }
        }
    }

    /// 事务式更新接收速率；下次帧长校验生效，保留当前计数窗口。
    /// 固定参数变化会整体拒绝；提高或降低预算都不重置已接收的流量。
    pub fn reload_limits(&self, limits: Limits) -> NetResult<()> {
        limits.validate()?;
        if limits.max_frame != self.limits.max_frame
            || limits.max_state_bytes != self.limits.max_state_bytes
            || limits.queue_frames != self.limits.queue_frames
            || limits.max_connections != self.limits.max_connections
            || limits.timeout != self.limits.timeout
        {
            return Err("live reload only supports frames_per_second and bytes_per_second; other changes require a new endpoint".into());
        }
        self.rates.send_replace(ReceiveRates::from(&limits));
        Ok(())
    }

    /// 显式重载完整 TOML 配置；省略项使用默认值，失败时保持当前预算。
    pub fn reload(&self, path: impl AsRef<std::path::Path>) -> NetResult<()> {
        self.reload_limits(Limits::load(path)?)
    }

    /// 当前监听地址；支持操作系统分配的端口。
    pub fn local_addr(&self) -> NetResult<SocketAddr> {
        Ok(self.endpoint.local_addr()?)
    }
    /// 停止端点及其连接。
    pub fn close(&self) {
        self.endpoint.close(0_u32.into(), b"host shutdown");
    }
}

impl Drop for AccEndpoint {
    fn drop(&mut self) {
        self.close();
    }
}
