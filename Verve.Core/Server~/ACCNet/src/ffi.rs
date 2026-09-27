//! C ABI；不透明整数句柄避免重复释放悬空指针，端点统一拥有全部连接。

use crate::{AccEndpoint, AccPeer, Limits, NetResult};
use bytes::Bytes;
use quinn::rustls::{
    pki_types::{CertificateDer, PrivateKeyDer},
    RootCertStore,
};
use std::{
    cell::RefCell,
    collections::HashMap,
    panic::{catch_unwind, AssertUnwindSafe},
    sync::{
        atomic::{AtomicU64, Ordering},
        Arc, Mutex, OnceLock, RwLock, Weak,
    },
    time::Duration,
};
use tokio::runtime::Runtime;

const ERROR: i32 = -1;
const OK: i32 = 0;
const EMPTY: i32 = 1;
const FULL: i32 = 2;
const BUFFER_TOO_SMALL: i32 = 3;
const TIMEOUT: i32 = 4;
const ABI_VERSION: u32 = 2;
const MAX_TEXT_BYTES: usize = 4096;
const MAX_CERTIFICATE_BYTES: usize = 1024 * 1024;

/// 借用字节；仅在调用期间使用，返回前完成拷贝。
#[repr(C)]
#[derive(Clone, Copy)]
pub struct AccBytes {
    /// 调用方拥有的输入地址；库不会在调用返回后保存它。
    pub data: *const u8,
    /// 输入字节数。
    pub length: usize,
}
/// ABI v2 配置；字段与 include/verve_acc_net.h 保持一致。
#[repr(C)]
pub struct AccConfig {
    /// ABI 版本，当前必须为 2。
    pub abi_version: u32,
    /// 端点模式，1 为服务端，0 为客户端。
    pub server: u32,
    /// UTF-8 绑定地址，例如 `127.0.0.1:9000`。
    pub bind_address: AccBytes,
    /// DER 证书；服务端必填。
    pub certificate_der: AccBytes,
    /// 与证书匹配的 DER 私钥。
    pub private_key_der: AccBytes,
    /// DER 信任根；客户端必填，服务端填写时启用双向 TLS。
    pub ca_der: AccBytes,
    /// 单批复制状态上限。
    pub max_state_bytes: u32,
    /// 单帧最大字节数。
    pub max_frame: u32,
    /// 每个连接的有界队列容量。
    pub queue_frames: u32,
    /// 端点最大连接数。
    pub max_connections: u32,
    /// 每个连接每秒最大帧数。
    pub frames_per_second: u32,
    /// 每个连接每秒最大字节数。
    pub bytes_per_second: u64,
    /// 连接和帧操作超时（毫秒）。
    pub timeout_ms: u32,
}
struct NativePeer {
    peer: AccPeer,
    pending: Option<Bytes>,
}
struct NativeEndpoint {
    endpoint: AccEndpoint,
    peers: RwLock<Option<HashMap<u64, Arc<Mutex<NativePeer>>>>>,
    max_frame_size: usize,
    // 端点共享运行时；最后一个端点和在途调用结束后释放线程。
    runtime: Arc<Runtime>,
}
impl NativeEndpoint {
    fn register_peer(&self, peer: AccPeer) -> NetResult<u64> {
        let mut peers = self.peers.write().map_err(|_| "poisoned peer registry")?;
        let peers = peers.as_mut().ok_or("endpoint is closed")?;
        let handle = next_handle()?;
        peers.insert(
            handle,
            Arc::new(Mutex::new(NativePeer {
                peer,
                pending: None,
            })),
        );
        Ok(handle)
    }

    fn get_peer(&self, handle: u64) -> NetResult<Arc<Mutex<NativePeer>>> {
        self.peers
            .read()
            .map_err(|_| "poisoned peer registry")?
            .as_ref()
            .ok_or("endpoint is closed")?
            .get(&handle)
            .cloned()
            .ok_or_else(|| "invalid peer handle".into())
    }
}
static ENDPOINTS: OnceLock<RwLock<HashMap<u64, Arc<NativeEndpoint>>>> = OnceLock::new();
static SHARED_RUNTIME: Mutex<Weak<Runtime>> = Mutex::new(Weak::new());
static NEXT_HANDLE: AtomicU64 = AtomicU64::new(1);
thread_local! { static LAST_ERROR: RefCell<String> = const { RefCell::new(String::new()) }; }
fn next_handle() -> NetResult<u64> {
    Ok(NEXT_HANDLE
        .fetch_update(Ordering::Relaxed, Ordering::Relaxed, |n| n.checked_add(1))
        .map_err(|_| "handle space exhausted")?)
}
fn endpoints() -> &'static RwLock<HashMap<u64, Arc<NativeEndpoint>>> {
    ENDPOINTS.get_or_init(|| RwLock::new(HashMap::new()))
}
fn shared_runtime() -> NetResult<Arc<Runtime>> {
    let mut shared = SHARED_RUNTIME
        .lock()
        .map_err(|_| "poisoned runtime registry")?;
    if let Some(runtime) = shared.upgrade() {
        return Ok(runtime);
    }
    let runtime = Arc::new(
        tokio::runtime::Builder::new_multi_thread()
            .worker_threads(2)
            .enable_all()
            .build()?,
    );
    *shared = Arc::downgrade(&runtime);
    Ok(runtime)
}
fn get_endpoint(handle: u64) -> NetResult<Arc<NativeEndpoint>> {
    endpoints()
        .read()
        .map_err(|_| "poisoned endpoint registry")?
        .get(&handle)
        .cloned()
        .ok_or_else(|| "invalid endpoint handle".into())
}
fn invoke(action: impl FnOnce() -> NetResult<i32>) -> i32 {
    let result = catch_unwind(AssertUnwindSafe(action));
    match result {
        Ok(Ok(status)) => {
            LAST_ERROR.with(|e| e.borrow_mut().clear());
            status
        }
        Ok(Err(error)) => {
            LAST_ERROR.with(|e| *e.borrow_mut() = error.to_string());
            ERROR
        }
        Err(_) => {
            LAST_ERROR.with(|e| *e.borrow_mut() = "Rust panic at ACC ABI boundary".into());
            ERROR
        }
    }
}
unsafe fn borrow_bytes<'a>(bytes: AccBytes, maximum: usize) -> NetResult<&'a [u8]> {
    if bytes.length > maximum {
        return Err("input exceeds size limit".into());
    }
    if bytes.length > isize::MAX as usize {
        return Err("input is too large for a platform slice".into());
    }
    if bytes.length == 0 {
        return Ok(&[]);
    }
    if bytes.data.is_null() {
        return Err("null input pointer".into());
    }
    Ok(unsafe { std::slice::from_raw_parts(bytes.data, bytes.length) })
}
unsafe fn borrow_utf8<'a>(bytes: AccBytes) -> NetResult<&'a str> {
    Ok(std::str::from_utf8(unsafe {
        borrow_bytes(bytes, MAX_TEXT_BYTES)?
    })?)
}
fn root_store(der: &[u8]) -> NetResult<RootCertStore> {
    let mut store = RootCertStore::empty();
    store.add(CertificateDer::from(der.to_vec()))?;
    Ok(store)
}

/// 创建端点。安全要求：所有指针须在调用期间有效，输出指针可写。
#[no_mangle]
pub unsafe extern "C" fn verve_acc_endpoint_create(
    config: *const AccConfig,
    output: *mut u64,
) -> i32 {
    invoke(|| {
        if config.is_null() || output.is_null() {
            return Err("null configuration or output".into());
        }
        let config = unsafe { &*config };
        unsafe {
            *output = 0;
        }
        if config.abi_version != ABI_VERSION || config.server > 1 {
            return Err("unsupported ABI version or endpoint mode".into());
        }
        let bind = unsafe { borrow_utf8(config.bind_address)? }.parse()?;
        let cert = unsafe { borrow_bytes(config.certificate_der, MAX_CERTIFICATE_BYTES)? };
        let key = unsafe { borrow_bytes(config.private_key_der, MAX_CERTIFICATE_BYTES)? };
        let ca = unsafe { borrow_bytes(config.ca_der, MAX_CERTIFICATE_BYTES)? };
        if cert.is_empty() != key.is_empty() {
            return Err("certificate and key must be supplied together".into());
        }
        let limits = Limits {
            max_frame: config.max_frame as usize,
            max_state_bytes: config.max_state_bytes as usize,
            queue_frames: config.queue_frames as usize,
            max_connections: config.max_connections as usize,
            frames_per_second: config.frames_per_second,
            bytes_per_second: config.bytes_per_second,
            timeout: Duration::from_millis(config.timeout_ms as u64),
        };
        if limits.max_frame < crate::protocol::HEADER_SIZE + 12 {
            return Err("C ABI ACC v2 frames require a 60-byte minimum frame".into());
        }
        limits.validate()?;
        let runtime = shared_runtime()?;
        let endpoint = {
            let _scope = runtime.enter();
            if config.server == 1 {
                if cert.is_empty() {
                    return Err("server certificate and key are required".into());
                }
                AccEndpoint::server(
                    bind,
                    vec![cert.to_vec().into()],
                    PrivateKeyDer::try_from(key.to_vec())?,
                    if ca.is_empty() {
                        None
                    } else {
                        Some(root_store(ca)?)
                    },
                    limits.clone(),
                )?
            } else {
                if ca.is_empty() {
                    return Err("client trust anchor is required".into());
                }
                AccEndpoint::client(
                    bind,
                    root_store(ca)?,
                    if cert.is_empty() {
                        None
                    } else {
                        Some((
                            vec![cert.to_vec().into()],
                            PrivateKeyDer::try_from(key.to_vec())?,
                        ))
                    },
                    limits.clone(),
                )?
            }
        };
        let handle = next_handle()?;
        endpoints()
            .write()
            .map_err(|_| "poisoned endpoint registry")?
            .insert(
                handle,
                Arc::new(NativeEndpoint {
                    endpoint,
                    peers: RwLock::new(Some(HashMap::new())),
                    max_frame_size: limits.max_frame,
                    runtime,
                }),
            );
        unsafe {
            *output = handle;
        }
        Ok(OK)
    })
}

/// 重载 TOML 接收预算；返回错误时不改变现有连接，输入仅借用至调用结束。
#[no_mangle]
pub unsafe extern "C" fn verve_acc_endpoint_reload(endpoint: u64, toml: AccBytes) -> i32 {
    invoke(|| {
        let limits = Limits::from_toml(unsafe { borrow_utf8(toml)? })?;
        get_endpoint(endpoint)?.endpoint.reload_limits(limits)?;
        Ok(OK)
    })
}

/// 接受连接；超时返回 4，不会保留未完成连接。输出指针须可写。
#[no_mangle]
pub unsafe extern "C" fn verve_acc_endpoint_accept(
    endpoint: u64,
    timeout_ms: u32,
    output: *mut u64,
) -> i32 {
    invoke(|| {
        if output.is_null() || timeout_ms == 0 {
            return Err("invalid accept arguments".into());
        }
        unsafe {
            *output = 0;
        }
        let endpoint_ref = get_endpoint(endpoint)?;
        let result = endpoint_ref.runtime.block_on(async {
            tokio::time::timeout(
                Duration::from_millis(timeout_ms as u64),
                endpoint_ref.endpoint.accept(),
            )
            .await
        });
        match result {
            Err(_) => Ok(TIMEOUT),
            Ok(result) => {
                let peer = result?.ok_or("endpoint closed")?;
                unsafe {
                    *output = endpoint_ref.register_peer(peer)?;
                }
                Ok(OK)
            }
        }
    })
}

/// 客户端连接；地址和证书名称采用 UTF-8，输出指针须可写。
#[no_mangle]
pub unsafe extern "C" fn verve_acc_endpoint_connect(
    endpoint: u64,
    address: AccBytes,
    server_name: AccBytes,
    output: *mut u64,
) -> i32 {
    invoke(|| {
        if output.is_null() {
            return Err("null output pointer".into());
        }
        unsafe {
            *output = 0;
        }
        let endpoint_ref = get_endpoint(endpoint)?;
        let address = unsafe { borrow_utf8(address)? }.parse()?;
        let name = unsafe { borrow_utf8(server_name)? };
        let peer = endpoint_ref
            .runtime
            .block_on(endpoint_ref.endpoint.connect(address, name))?;
        unsafe {
            *output = endpoint_ref.register_peer(peer)?;
        }
        Ok(OK)
    })
}

/// 非阻塞发送；返回 2 表示背压，数据未被接管。输入在返回后可释放。
#[no_mangle]
pub unsafe extern "C" fn verve_acc_peer_send(endpoint: u64, peer: u64, data: AccBytes) -> i32 {
    invoke(|| {
        let endpoint_ref = get_endpoint(endpoint)?;
        let data = unsafe { borrow_bytes(data, endpoint_ref.max_frame_size)? };
        let native_peer = endpoint_ref.get_peer(peer)?;
        let native_peer = native_peer.lock().map_err(|_| "poisoned peer")?;
        Ok(if native_peer.peer.try_send_slice(data)? {
            OK
        } else {
            FULL
        })
    })
}

/// 非阻塞接收；返回 3 时 length 为所需容量且帧仍保留。缓冲区须可写。
#[no_mangle]
pub unsafe extern "C" fn verve_acc_peer_receive(
    endpoint: u64,
    peer: u64,
    buffer: *mut u8,
    capacity: usize,
    length: *mut usize,
) -> i32 {
    invoke(|| {
        if length.is_null() || (buffer.is_null() && capacity != 0) {
            return Err("null receive buffer".into());
        }
        unsafe {
            *length = 0;
        }
        let endpoint_ref = get_endpoint(endpoint)?;
        let native_peer = endpoint_ref.get_peer(peer)?;
        let mut native_peer = native_peer.lock().map_err(|_| "poisoned peer")?;
        if native_peer.pending.is_none() {
            native_peer.pending = native_peer.peer.try_recv()?;
        }
        let Some(frame) = &native_peer.pending else {
            return Ok(EMPTY);
        };
        unsafe {
            *length = frame.len();
        }
        if capacity < frame.len() {
            return Ok(BUFFER_TOO_SMALL);
        }
        unsafe {
            std::ptr::copy_nonoverlapping(frame.as_ptr(), buffer, frame.len());
        }
        native_peer.pending = None;
        Ok(OK)
    })
}

/// 关闭并回收连接句柄；重复调用明确返回错误。
#[no_mangle]
pub extern "C" fn verve_acc_peer_destroy(endpoint: u64, peer: u64) -> i32 {
    invoke(|| {
        let endpoint_ref = get_endpoint(endpoint)?;
        let value = endpoint_ref
            .peers
            .write()
            .map_err(|_| "poisoned peer registry")?
            .as_mut()
            .ok_or("endpoint is closed")?
            .remove(&peer);
        let value = value.ok_or("invalid peer handle")?;
        value.lock().map_err(|_| "poisoned peer")?.peer.close();
        Ok(OK)
    })
}
/// 查询连接；断开时通过最近错误返回原因。
#[no_mangle]
pub extern "C" fn verve_acc_peer_status(endpoint: u64, peer: u64) -> i32 {
    invoke(|| {
        let endpoint_ref = get_endpoint(endpoint)?;
        let native_peer = endpoint_ref.get_peer(peer)?;
        let native_peer = native_peer.lock().map_err(|_| "poisoned peer")?;
        native_peer.peer.check_open()?;
        Ok(OK)
    })
}
/// 返回监听端口；output 须为可写整数指针。
#[no_mangle]
pub unsafe extern "C" fn verve_acc_endpoint_port(endpoint: u64, output: *mut u16) -> i32 {
    invoke(|| {
        if output.is_null() {
            return Err("null output pointer".into());
        }
        unsafe {
            *output = get_endpoint(endpoint)?.endpoint.local_addr()?.port();
        }
        Ok(OK)
    })
}
/// 关闭端点及全部子连接；并发调用中的端点保留到最后一个调用完成。
#[no_mangle]
pub extern "C" fn verve_acc_endpoint_destroy(endpoint: u64) -> i32 {
    invoke(|| {
        let endpoint_ref = endpoints()
            .write()
            .map_err(|_| "poisoned endpoint registry")?
            .remove(&endpoint)
            .ok_or("invalid endpoint handle")?;
        endpoint_ref.endpoint.close();
        let peers = {
            let mut peers = endpoint_ref
                .peers
                .write()
                .map_err(|_| "poisoned peer registry")?;
            peers.take()
        };
        drop(peers);
        Ok(OK)
    })
}
/// 复制当前线程最近错误的 UTF-8 文本，返回所需字节数（不含结尾零）。
/// 安全要求：buffer 为至少 capacity 字节的可写内存，或 capacity 为零。
#[no_mangle]
pub unsafe extern "C" fn verve_acc_last_error(buffer: *mut u8, capacity: usize) -> usize {
    LAST_ERROR.with(|error| {
        let text = error.borrow();
        if !buffer.is_null() && capacity >= text.len() {
            unsafe {
                std::ptr::copy_nonoverlapping(text.as_ptr(), buffer, text.len());
            }
        }
        text.len()
    })
}

/// 无状态包头校验；输入指针须在调用期间有效。
#[no_mangle]
pub unsafe extern "C" fn verve_acc_validate_packet(
    data: *const u8,
    length: usize,
    max_state_bytes: usize,
) -> u8 {
    if data.is_null() || length < crate::protocol::HEADER_SIZE || length > isize::MAX as usize {
        return 0;
    }
    let packet = unsafe { std::slice::from_raw_parts(data, length) };
    u8::from(crate::protocol::PacketHeader::decode(packet, max_state_bytes).is_ok())
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn packet_validation_keeps_header_outside_state_budget() {
        use crate::protocol::{FULL_KIND, HEADER_SIZE, MAGIC, PROTOCOL_VERSION};
        let mut packet = [0; HEADER_SIZE + 12];
        packet[..4].copy_from_slice(&MAGIC.to_le_bytes());
        packet[4] = PROTOCOL_VERSION;
        packet[5] = FULL_KIND;
        for offset in [8, 16, 32] {
            packet[offset..offset + 8].copy_from_slice(&1u64.to_le_bytes());
        }
        packet[40..44].copy_from_slice(&12u32.to_le_bytes());
        for length in 0..=HEADER_SIZE {
            assert_eq!(
                unsafe { verve_acc_validate_packet(packet.as_ptr(), length, 12) },
                0
            );
        }
        assert_eq!(
            unsafe { verve_acc_validate_packet(packet.as_ptr(), packet.len(), 12) },
            1
        );
        assert_eq!(
            unsafe { verve_acc_validate_packet(packet.as_ptr(), packet.len(), 11) },
            0
        );
        assert_eq!(
            unsafe { verve_acc_validate_packet(std::ptr::null(), 60, 12) },
            0
        );
        packet[44..48].copy_from_slice(&1u32.to_le_bytes());
        assert_eq!(
            unsafe { verve_acc_validate_packet(packet.as_ptr(), packet.len(), 12) },
            0
        );
        assert_eq!(
            unsafe { verve_acc_validate_packet(packet.as_ptr(), packet.len() - 1, 12) },
            1
        );
    }

    #[test]
    fn null_config_and_double_destroy_are_errors() {
        assert_eq!(
            unsafe { verve_acc_endpoint_create(std::ptr::null(), std::ptr::null_mut()) },
            ERROR
        );
        assert_eq!(verve_acc_endpoint_destroy(u64::MAX), ERROR);
        assert_eq!(verve_acc_peer_destroy(u64::MAX, 1), ERROR);
    }
}
