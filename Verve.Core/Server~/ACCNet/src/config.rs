//! 端点配置；按需读取 TOML，热重载由宿主显式触发。

use crate::NetResult;
use quinn::VarInt;
use serde::{Deserialize, Deserializer};
use std::{path::Path, sync::Arc, time::Duration};

/// 资源上限；队列内存最多约为连接数 × 队列长度 × 最大帧长 × 2。
#[derive(Clone, Debug, serde::Deserialize)]
#[serde(default, deny_unknown_fields)]
pub struct Limits {
    /// 单帧最大字节数，包含 ACC v2 包头。
    pub max_frame: usize,
    /// 单批复制状态上限；默认帧校验器使用。
    pub max_state_bytes: usize,
    /// 每个连接的发送和接收队列容量。
    pub queue_frames: usize,
    /// 端点允许同时存在的连接数。
    pub max_connections: usize,
    /// 每个连接每秒允许接收的帧数。
    pub frames_per_second: u32,
    /// 每个连接每秒允许接收的字节数。
    pub bytes_per_second: u64,
    /// 握手、帧读写和连接空闲超时。
    #[serde(rename = "timeout_ms", deserialize_with = "milliseconds")]
    pub timeout: Duration,
}

impl Default for Limits {
    fn default() -> Self {
        Self {
            max_frame: 64 * 1024,
            max_state_bytes: 8 * 1024 * 1024,
            queue_frames: 32,
            max_connections: 128,
            frames_per_second: 2048,
            bytes_per_second: 16 * 1024 * 1024,
            timeout: Duration::from_secs(10),
        }
    }
}

impl Limits {
    pub(crate) fn validate(&self) -> NetResult<()> {
        if !(1..=256 * 1024).contains(&self.max_frame)
            || !(12..=64 * 1024 * 1024).contains(&self.max_state_bytes)
            || !(1..=4096).contains(&self.queue_frames)
            || !(1..=65536).contains(&self.max_connections)
            || self.frames_per_second == 0
            || self.bytes_per_second < self.max_frame as u64
            || self.timeout.is_zero()
            || self.timeout > Duration::from_secs(3600)
        {
            return Err("invalid ACC transport limits".into());
        }
        Ok(())
    }

    pub(crate) fn transport(&self) -> NetResult<Arc<quinn::TransportConfig>> {
        let mut transport = quinn::TransportConfig::default();
        transport.max_concurrent_bidi_streams(1_u32.into());
        transport.max_concurrent_uni_streams(0_u32.into());
        transport.datagram_receive_buffer_size(None);
        transport.send_window(self.max_frame as u64 * 2);
        transport.receive_window(VarInt::from_u32(self.max_frame as u32 * 2));
        transport.stream_receive_window(VarInt::from_u32(self.max_frame as u32 * 2));
        let idle_timeout = self
            .timeout
            .try_into()
            .map_err(|_| "invalid ACC idle timeout")?;
        transport.max_idle_timeout(Some(idle_timeout));
        transport.keep_alive_interval(Some(self.timeout / 3));
        Ok(Arc::new(transport))
    }
}

impl Limits {
    /// 加载 TOML 文件；未写的字段采用默认值，未知字段和无效参数直接报错。
    pub fn load(path: impl AsRef<Path>) -> NetResult<Self> {
        Self::from_toml(&std::fs::read_to_string(path)?)
    }

    /// 解析宿主提供的 TOML；允许宿主自行获取远程或内嵌配置。
    pub fn from_toml(text: &str) -> NetResult<Self> {
        let limits: Self = toml::from_str(text)?;
        limits.validate()?;
        Ok(limits)
    }
}

/// TOML 超时字段使用整毫秒，与 C ABI 保持一致。
fn milliseconds<'de, D: Deserializer<'de>>(deserializer: D) -> Result<Duration, D::Error> {
    u64::deserialize(deserializer).map(Duration::from_millis)
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn example_and_partial_configuration_use_validated_defaults() {
        let config = Limits::from_toml(include_str!("../accnet.toml")).unwrap();
        assert_eq!(config.max_frame, Limits::default().max_frame);
        let config = Limits::from_toml("queue_frames = 8\ntimeout_ms = 2500").unwrap();
        assert_eq!(config.queue_frames, 8);
        assert_eq!(config.timeout, Duration::from_millis(2500));
        assert_eq!(config.max_connections, Limits::default().max_connections);
    }

    #[test]
    fn rejects_unknown_duplicate_and_invalid_values() {
        for text in [
            "queue_frame = 8",
            "queue_frames = 1\nqueue_frames = 2",
            "queue_frames = 0",
            "timeout_ms = -1",
            "timeout_ms = 0",
            "timeout_ms = 3600001",
            "max_state_bytes = 11",
            "max_frame = 262145",
            "frames_per_second = 0",
            "bytes_per_second = 1",
            "max_connections = 0",
        ] {
            assert!(Limits::from_toml(text).is_err(), "accepted {text}");
        }
    }

    #[test]
    fn missing_file_is_an_error() {
        assert!(Limits::load("missing-acc-config-directory/accnet.toml").is_err());
    }
}
