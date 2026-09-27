#ifndef VERVE_ACC_NET_H
#define VERVE_ACC_NET_H
#include <stdint.h>
#include <stddef.h>
#ifdef __cplusplus
extern "C" {
#endif
/* ABI 2. All strings are UTF-8 byte slices without a terminator. Borrowed memory
   must stay valid until the call returns. Output buffers must be writable.
   Do not unload the library while handles or calls are active. */
/* Borrowed byte range; the caller owns the memory and may release it after
   the synchronous ABI call returns. The library never retains this pointer. */
typedef struct { const uint8_t *data; size_t length; } AccBytes;
typedef struct {
    uint32_t abi_version;              /* Must be 2. */
    uint32_t server;                   /* 1 server, 0 client. */
    AccBytes bind_address;             /* IP:port, e.g. 127.0.0.1:9000. */
    AccBytes certificate_der;          /* Leaf certificate; required on server. */
    AccBytes private_key_der;          /* PKCS#8 or PKCS#1 DER; matches certificate. */
    AccBytes ca_der;                   /* Client: required trust anchor. Server: optional client CA for mTLS. */
    uint32_t max_state_bytes;          /* 8388608, must be 12..67108864. */
    uint32_t max_frame;                /* 65536, must be 60..262144. */
    uint32_t queue_frames;             /* 32, must be 1..4096. */
    uint32_t max_connections;          /* 128, must be 1..65536. */
    uint32_t frames_per_second;        /* 2048, positive. */
    uint64_t bytes_per_second;         /* 16777216, >= max_frame. */
    uint32_t timeout_ms;               /* 10000, 1..3600000. */
} AccConfig;
enum AccStatus { ACC_ERROR=-1, ACC_OK=0, ACC_EMPTY=1, ACC_FULL=2, ACC_BUFFER_TOO_SMALL=3, ACC_TIMEOUT=4 };
/* Endpoints own all peers. Destroying an endpoint closes every child peer.
   Integer handles never recycle, so stale/double destroys return ACC_ERROR.
   Calls are thread-safe; receive is serialized per peer. Blocking accept
   and connect should run on host worker threads. */
int32_t verve_acc_endpoint_create(const AccConfig *, uint64_t *endpoint);
/* Transactional TOML reload (up to 4096 UTF-8 bytes): only receive rates may change.
   Omitted fields use creation defaults, not current values. Preserve nondefault
   fixed fields. Existing counters are retained; failures leave the budget intact. */
int32_t verve_acc_endpoint_reload(uint64_t endpoint, AccBytes toml);
int32_t verve_acc_endpoint_accept(uint64_t endpoint, uint32_t timeout_ms, uint64_t *peer);
int32_t verve_acc_endpoint_connect(uint64_t endpoint, AccBytes address, AccBytes server_name, uint64_t *peer);
int32_t verve_acc_endpoint_port(uint64_t endpoint, uint16_t *port);
int32_t verve_acc_peer_send(uint64_t endpoint, uint64_t peer, AccBytes packet);
int32_t verve_acc_peer_receive(uint64_t endpoint, uint64_t peer, uint8_t *buffer, size_t capacity, size_t *length);
int32_t verve_acc_peer_status(uint64_t endpoint, uint64_t peer);
int32_t verve_acc_peer_destroy(uint64_t endpoint, uint64_t peer);
int32_t verve_acc_endpoint_destroy(uint64_t endpoint);
/* Returns UTF-8 byte count, no NUL. If capacity is insufficient, copies nothing.
   Last error belongs to the current OS thread; retrieve it before the next ABI call. */
size_t verve_acc_last_error(uint8_t *buffer, size_t capacity);
uint8_t verve_acc_validate_packet(const uint8_t *data, size_t length, size_t max_state_bytes);
#ifdef __cplusplus
}
#endif
#endif
