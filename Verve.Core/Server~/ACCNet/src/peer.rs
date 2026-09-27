//! 连接；拥有有界收发队列和后台任务，释放时取消任务并关闭 QUIC 连接。

use crate::{endpoint::ReceiveRates, Limits, NetResult, PacketValidator};
use bytes::Bytes;
use quinn::{rustls::pki_types::CertificateDer, Connection, RecvStream, SendStream};
use std::{
    error::Error,
    fmt,
    sync::Arc,
    time::{Duration, Instant},
};
use tokio::{
    sync::{mpsc, watch, OwnedSemaphorePermit},
    task::JoinHandle,
};

/// 发送失败；背压时调用方继续拥有未排队的数据。
#[derive(Debug)]
pub enum SendError {
    Invalid(Box<dyn Error + Send + Sync>),
    Full(Bytes),
    Closed(Bytes),
}
impl fmt::Display for SendError {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            Self::Invalid(error) => write!(f, "invalid ACC packet: {error}"),
            Self::Full(_) => f.write_str("ACC peer send queue is full"),
            Self::Closed(_) => f.write_str("ACC peer is closed"),
        }
    }
}
impl Error for SendError {}

/// 连接句柄；释放时关闭连接并取消读写任务，无需额外维护后台任务。
pub struct AccPeer {
    connection: Connection,
    incoming: mpsc::Receiver<Bytes>,
    outgoing: mpsc::Sender<Bytes>,
    task: JoinHandle<()>,
    max_frame: usize,
    validator: Arc<dyn PacketValidator>,
}

impl AccPeer {
    pub(crate) fn start(
        connection: Connection,
        send: SendStream,
        recv: RecvStream,
        limits: Limits,
        validator: Arc<dyn PacketValidator>,
        rates: watch::Receiver<ReceiveRates>,
        permit: OwnedSemaphorePermit,
    ) -> Self {
        let (incoming_tx, incoming) = mpsc::channel(limits.queue_frames);
        let (outgoing, outgoing_rx) = mpsc::channel(limits.queue_frames);
        let worker = connection.clone();
        let max_frame = limits.max_frame;
        let read_validator = validator.clone();
        let task = tokio::spawn(async move {
            let _permit = permit;
            let result = tokio::select! {
                result = read_frames(recv, incoming_tx, &limits, read_validator, rates) => result,
                result = write_frames(send, outgoing_rx, limits.timeout) => result,
                error = worker.closed() => Err(Box::new(error) as Box<dyn Error + Send + Sync>),
            };
            let reason = result
                .err()
                .map(|error| error.to_string())
                .unwrap_or_else(|| "peer closed".into());
            worker.close(1_u32.into(), reason.as_bytes());
        });
        Self {
            connection,
            incoming,
            outgoing,
            task,
            max_frame,
            validator,
        }
    }
    /// 等待下一帧；连接失败会明确返回错误。
    pub async fn recv(&mut self) -> NetResult<Bytes> {
        self.incoming
            .recv()
            .await
            .ok_or_else(|| self.closed_error())
    }
    /// 非阻塞接收；无数据与连接失败分开报告。
    pub fn try_recv(&mut self) -> NetResult<Option<Bytes>> {
        match self.incoming.try_recv() {
            Ok(frame) => Ok(Some(frame)),
            Err(mpsc::error::TryRecvError::Empty) => Ok(None),
            Err(mpsc::error::TryRecvError::Disconnected) => Err(self.closed_error()),
        }
    }
    fn closed_error(&self) -> Box<dyn Error + Send + Sync> {
        self.connection
            .close_reason()
            .map(|e| Box::new(e) as Box<dyn Error + Send + Sync>)
            .unwrap_or_else(|| "ACC peer closed".into())
    }
    /// 校验并排队；成功仅表示传输层接管，连接中断仍须由业务重新同步。
    pub fn try_send(&self, packet: Bytes) -> Result<(), SendError> {
        self.validate_packet(&packet).map_err(SendError::Invalid)?;
        if self.connection.close_reason().is_some() {
            return Err(SendError::Closed(packet));
        }
        self.outgoing.try_send(packet).map_err(|error| match error {
            mpsc::error::TrySendError::Full(packet) => SendError::Full(packet),
            mpsc::error::TrySendError::Closed(packet) => SendError::Closed(packet),
        })
    }
    /// 借用 ABI 输入；预留队列容量后才复制，背压返回 false 且不分配。
    pub(crate) fn try_send_slice(&self, packet: &[u8]) -> NetResult<bool> {
        self.validate_packet(packet)?;
        self.check_open()?;
        match self.outgoing.try_reserve() {
            Ok(slot) => {
                slot.send(Bytes::copy_from_slice(packet));
                Ok(true)
            }
            Err(mpsc::error::TrySendError::Full(())) => Ok(false),
            Err(mpsc::error::TrySendError::Closed(())) => Err(self.closed_error()),
        }
    }
    fn validate_packet(&self, packet: &[u8]) -> NetResult<()> {
        if !(1..=self.max_frame).contains(&packet.len()) {
            return Err("invalid ACC frame size".into());
        }
        self.validator.validate(packet)
    }
    /// 检查连接状态；ABI 和发送入口共用，保留 QUIC 关闭原因。
    pub(crate) fn check_open(&self) -> NetResult<()> {
        match self.connection.close_reason() {
            Some(reason) => Err(reason.into()),
            None => Ok(()),
        }
    }
    /// 远端 TLS 身份；使用双向认证时可供宿主映射业务权限。
    pub fn peer_certificates(&self) -> Option<Vec<CertificateDer<'static>>> {
        self.connection
            .peer_identity()?
            .downcast::<Vec<CertificateDer<'static>>>()
            .ok()
            .map(|v| *v)
    }
    /// 关闭连接；宿主仍可读取已入队的数据。
    pub fn close(&self) {
        self.connection.close(0_u32.into(), b"host closed peer");
    }
}
impl Drop for AccPeer {
    fn drop(&mut self) {
        self.close();
        self.task.abort();
    }
}

async fn read_frames(
    mut recv: RecvStream,
    sender: mpsc::Sender<Bytes>,
    config: &Limits,
    validator: Arc<dyn PacketValidator>,
    rates: watch::Receiver<ReceiveRates>,
) -> NetResult<()> {
    let mut started = Instant::now();
    let mut count = 0_u32;
    let mut bytes = 0_u64;
    loop {
        // 整帧读取共用一个超时，避免只发送部分长度字段的慢速连接长期占用资源。
        let packet = tokio::time::timeout(config.timeout, async {
            let mut prefix = [0; 4];
            recv.read_exact(&mut prefix).await?;
            let length = u32::from_le_bytes(prefix) as usize;
            if !(1..=config.max_frame).contains(&length) {
                return Err("invalid ACC frame size".into());
            }
            if started.elapsed() >= Duration::from_secs(1) {
                started = Instant::now();
                count = 0;
                bytes = 0;
            }
            count = count.saturating_add(1);
            bytes = bytes.saturating_add(length as u64);
            let budget = *rates.borrow();
            if count > budget.frames || bytes > budget.bytes {
                return Err("ACC receive rate exceeded".into());
            }
            let mut packet = vec![0; length];
            recv.read_exact(&mut packet).await?;
            validator.validate(&packet)?;
            Ok::<_, Box<dyn Error + Send + Sync>>(Bytes::from(packet))
        })
        .await??;
        // 慢宿主使用流控背压；超时则关闭连接，不静默丢掉组件变更。
        tokio::time::timeout(config.timeout, sender.send(packet)).await??;
    }
}
async fn write_frames(
    mut send: SendStream,
    mut receiver: mpsc::Receiver<Bytes>,
    timeout: Duration,
) -> NetResult<()> {
    while let Some(packet) = receiver.recv().await {
        tokio::time::timeout(timeout, async {
            send.write_all(&(packet.len() as u32).to_le_bytes()).await?;
            send.write_all(&packet).await
        })
        .await??;
    }
    send.finish()?;
    Ok(())
}
