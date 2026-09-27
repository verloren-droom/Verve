//! ACC 网络接入库；仅负责加密连接与可靠帧，业务授权和复制策略由宿主管理。

mod config;
mod endpoint;
mod ffi;
mod peer;
pub mod protocol;

pub use config::Limits;
pub use endpoint::AccEndpoint;
pub use peer::{AccPeer, SendError};
use std::error::Error;

/// ALPN；只接受当前版本的 ACC 传输。
pub const ALPN: &[u8] = b"verve-acc/2";
/// 接入错误；不会吞掉连接、协议或背压错误。
pub type NetResult<T> = Result<T, Box<dyn Error + Send + Sync>>;

/// 帧校验策略；只负责同步校验完整帧，不负责鉴权、路由或业务状态。
///
/// 端点和连接会持有策略的共享引用，策略本身应保持无状态或自行保证并发安全。
pub trait PacketValidator: Send + Sync {
    /// 校验一帧完整数据；返回错误会拒绝发送或关闭接收连接。
    fn validate(&self, packet: &[u8]) -> NetResult<()>;
}

impl<F> PacketValidator for F
where
    F: Fn(&[u8]) -> NetResult<()> + Send + Sync,
{
    fn validate(&self, packet: &[u8]) -> NetResult<()> {
        self(packet)
    }
}

/// ACC 默认帧校验；限制批次大小并验证分片范围，不维护 ECS 状态。
#[derive(Clone, Copy, Debug)]
pub struct AccPacketValidator {
    /// 一批完整复制状态的字节上限。
    pub max_state_bytes: usize,
}

impl Default for AccPacketValidator {
    fn default() -> Self {
        Self {
            max_state_bytes: Limits::default().max_state_bytes,
        }
    }
}

impl PacketValidator for AccPacketValidator {
    fn validate(&self, packet: &[u8]) -> NetResult<()> {
        protocol::PacketHeader::decode(packet, self.max_state_bytes)
            .map(|_| ())
            .map_err(|error| error.into())
    }
}
