// Package accnet embeds the Rust QUIC transport. Endpoint owns every Peer.
package accnet

/*
#cgo CFLAGS: -I${SRCDIR}/../../include
#cgo LDFLAGS: -L${SRCDIR}/../../target/release -lverve_acc_net
#include "verve_acc_net.h"
#include <stdlib.h>
*/
import "C"
import (
	"errors"
	"runtime"
	"sync/atomic"
	"unsafe"
)

type Endpoint struct{ handle atomic.Uint64 }
type Peer struct {
	owner  *Endpoint
	handle atomic.Uint64
}

func input(b []byte) (C.AccBytes, func()) {
	if len(b) == 0 {
		return C.AccBytes{}, func() {}
	}
	p := C.CBytes(b)
	return C.AccBytes{data: (*C.uint8_t)(p), length: C.size_t(len(b))}, func() { C.free(p) }
}
func status(s C.int32_t) error {
	if s == 0 {
		return nil
	}
	n := C.verve_acc_last_error(nil, 0)
	if n == 0 {
		return errors.New("ACC native call failed")
	}
	b := make([]byte, int(n))
	C.verve_acc_last_error((*C.uint8_t)(unsafe.Pointer(&b[0])), n)
	return errors.New(string(b))
}

// New creates a server or client with mandatory server identity/client trust.
// A server CA enables mTLS. All DER buffers are copied before returning.
func New(server bool, bind string, cert, key, ca []byte) (*Endpoint, error) {
	runtime.LockOSThread()
	defer runtime.UnlockOSThread()
	b, fb := input([]byte(bind))
	defer fb()
	c, fc := input(cert)
	defer fc()
	k, fk := input(key)
	defer fk()
	a, fa := input(ca)
	defer fa()
	config := C.AccConfig{abi_version: 2, bind_address: b, certificate_der: c, private_key_der: k, ca_der: a, max_state_bytes: 8388608, max_frame: 65536, queue_frames: 32, max_connections: 128, frames_per_second: 2048, bytes_per_second: 16777216, timeout_ms: 10000}
	if server {
		config.server = 1
	}
	var h C.uint64_t
	if e := status(C.verve_acc_endpoint_create(&config, &h)); e != nil {
		return nil, e
	}
	end := &Endpoint{}
	end.handle.Store(uint64(h))
	runtime.SetFinalizer(end, func(e *Endpoint) { _ = e.Close() })
	return end, nil
}
func (e *Endpoint) Port() (uint16, error) {
	runtime.LockOSThread()
	defer runtime.UnlockOSThread()
	var p C.uint16_t
	err := status(C.verve_acc_endpoint_port(C.uint64_t(e.handle.Load()), &p))
	runtime.KeepAlive(e)
	return uint16(p), err
}

// Accept returns nil, nil on timeout. Execute on a host worker goroutine.
func (e *Endpoint) Accept(timeoutMs uint32) (*Peer, error) {
	runtime.LockOSThread()
	defer runtime.UnlockOSThread()
	var p C.uint64_t
	s := C.verve_acc_endpoint_accept(C.uint64_t(e.handle.Load()), C.uint32_t(timeoutMs), &p)
	runtime.KeepAlive(e)
	if s == 4 {
		return nil, nil
	}
	if err := status(s); err != nil {
		return nil, err
	}
	return e.peer(uint64(p)), nil
}
func (e *Endpoint) Connect(address, name string) (*Peer, error) {
	runtime.LockOSThread()
	defer runtime.UnlockOSThread()
	a, fa := input([]byte(address))
	defer fa()
	n, fn := input([]byte(name))
	defer fn()
	var p C.uint64_t
	s := C.verve_acc_endpoint_connect(C.uint64_t(e.handle.Load()), a, n, &p)
	runtime.KeepAlive(e)
	if err := status(s); err != nil {
		return nil, err
	}
	return e.peer(uint64(p)), nil
}
func (e *Endpoint) peer(h uint64) *Peer { p := &Peer{owner: e}; p.handle.Store(h); return p }
func (e *Endpoint) Close() error {
	runtime.LockOSThread()
	defer runtime.UnlockOSThread()
	h := e.handle.Swap(0)
	if h == 0 {
		return nil
	}
	runtime.SetFinalizer(e, nil)
	return status(C.verve_acc_endpoint_destroy(C.uint64_t(h)))
}

// TrySend returns false on backpressure. The original slice remains host-owned.
func (p *Peer) TrySend(data []byte) (bool, error) {
	runtime.LockOSThread()
	defer runtime.UnlockOSThread()
	b, free := input(data)
	defer free()
	s := C.verve_acc_peer_send(C.uint64_t(p.owner.handle.Load()), C.uint64_t(p.handle.Load()), b)
	runtime.KeepAlive(p)
	if s == 2 {
		return false, nil
	}
	return s == 0, status(s)
}

// Receive returns zero for an empty queue. An undersized buffer retains the frame.
func (p *Peer) Receive(buffer []byte) (int, error) {
	runtime.LockOSThread()
	defer runtime.UnlockOSThread()
	var ptr *C.uint8_t
	if len(buffer) > 0 {
		ptr = (*C.uint8_t)(unsafe.Pointer(&buffer[0]))
	}
	var n C.size_t
	s := C.verve_acc_peer_receive(C.uint64_t(p.owner.handle.Load()), C.uint64_t(p.handle.Load()), ptr, C.size_t(len(buffer)), &n)
	runtime.KeepAlive(p)
	if s == 1 {
		return 0, nil
	}
	if s == 3 {
		return int(n), errors.New("receive buffer too small; frame retained")
	}
	return int(n), status(s)
}
func (p *Peer) Close() error {
	runtime.LockOSThread()
	defer runtime.UnlockOSThread()
	h := p.handle.Swap(0)
	if h == 0 || p.owner.handle.Load() == 0 {
		return nil
	}
	s := C.verve_acc_peer_destroy(C.uint64_t(p.owner.handle.Load()), C.uint64_t(h))
	runtime.KeepAlive(p)
	return status(s)
}

// Reload atomically updates receive rates from TOML; immutable changes fail.
func (e *Endpoint) Reload(toml string) error {
    runtime.LockOSThread()
    defer runtime.UnlockOSThread()
    b, free := input([]byte(toml))
    defer free()
    result := status(C.verve_acc_endpoint_reload(C.uint64_t(e.handle.Load()), b))
    runtime.KeepAlive(e)
    return result
}
