const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const { test } = require('node:test');

const encoder = new TextEncoder();
const decoder = new TextDecoder('utf-8', { ignoreBOM: true });
let nextPointer = 1;
global.HEAPU8 = new Uint8Array(1024 * 1024);
global._malloc = size => { const pointer = nextPointer; nextPointer += size; return pointer; };
global.lengthBytesUTF8 = text => encoder.encode(text).length;
global.stringToUTF8Array = (text, array, offset, size) => {
  const bytes = encoder.encode(text);
  array.set(bytes.subarray(0, size - 1), offset);
  array[offset + Math.min(bytes.length, size - 1)] = 0;
};
global.stringToUTF8 = (text, pointer, size) => stringToUTF8Array(text, HEAPU8, pointer, size);
global.UTF8ToString = pointer => {
  let end = pointer;
  while (HEAPU8[end]) end++;
  return decoder.decode(HEAPU8.subarray(pointer, end));
};
const pointerTo = text => {
  const size = lengthBytesUTF8(text) + 1;
  const pointer = _malloc(size);
  stringToUTF8(text, pointer, size);
  return pointer;
};
class BrowserSocket {
  constructor(url) {
    if (url === 'invalid') throw new Error('invalid URL');
    this.readyState = 0;
    this.sent = [];
    BrowserSocket.latest = this;
  }
  send(text) {
    if (this.readyState !== 1) throw new Error('not open');
    this.sent.push(text);
  }
  close() { this.readyState = 3; }
}
global.WebSocket = BrowserSocket;
global.LibraryManager = { library: {} };
global.mergeInto = Object.assign;
vm.runInThisContext(fs.readFileSync(path.join(__dirname, '../../../Runtime/Network/VerveWebSocket.jslib'), 'utf8'));
const bridge = LibraryManager.library;
global.VerveSockets = bridge.$VerveSockets;

function connect(limit = 1024) {
  const id = bridge.VerveWebSocketCreate(pointerTo('wss://example.invalid/'), limit);
  const socket = BrowserSocket.latest;
  assert.equal(bridge.VerveWebSocketState(id), 1);
  socket.readyState = 1;
  assert.equal(bridge.VerveWebSocketState(id), 2);
  return { id, socket };
}

test('UTF-8, embedded NUL, BOM, fragments and empty messages are preserved', () => {
  const { id, socket } = connect();
  const message = '\uFEFF中文\0🎮';
  const bytes = encoder.encode(message);
  const source = _malloc(bytes.length + 4);
  HEAPU8.set(bytes, source + 2);
  bridge.VerveWebSocketSend(id, source, 2, bytes.length);
  assert.deepEqual(socket.sent, [message]);
  socket.onmessage({ data: message });
  const output = _malloc(bytes.length + 4);
  bridge.VerveWebSocketReceive(id, output, 2, 3);
  assert.equal(bridge.VerveWebSocketPeek(id), bytes.length - 3);
  bridge.VerveWebSocketReceive(id, output, 5, bytes.length - 3);
  assert.equal(decoder.decode(HEAPU8.subarray(output + 2, output + 2 + bytes.length)), message);
  assert.equal(bridge.VerveWebSocketPeek(id), -1);
  socket.onmessage({ data: '' });
  assert.equal(bridge.VerveWebSocketPeek(id), 0);
  bridge.VerveWebSocketReceive(id, output, 0, 0);
  assert.equal(bridge.VerveWebSocketPeek(id), -1);
  bridge.VerveWebSocketRelease(id);
});

test('binary and oversized messages fail explicitly and close the connection', () => {
  for (const data of [new Uint8Array(1), 'oversized']) {
    const { id, socket } = connect(4);
    socket.onmessage({ data });
    assert.notEqual(bridge.VerveWebSocketError(id), 0);
    assert.equal(socket.readyState, 3);
    assert.equal(bridge.VerveWebSocketPeek(id), -1);
    bridge.VerveWebSocketRelease(id);
  }
});

test('empty-message floods are bounded and release removes all browser ownership', () => {
  const { id, socket } = connect();
  for (let i = 0; i < 1025; i++) socket.onmessage({ data: '' });
  assert.notEqual(bridge.VerveWebSocketError(id), 0);
  bridge.VerveWebSocketRelease(id);
  assert.equal(socket.onmessage, null);
  assert.equal(socket.onerror, null);
  assert.equal(VerveSockets.connections[id], undefined);
});

test('remote close, explicit close and construction errors remain distinguishable', () => {
  const { id, socket } = connect();
  socket.readyState = 3;
  assert.equal(bridge.VerveWebSocketState(id), 4);
  bridge.VerveWebSocketClose(id, 1000, pointerTo('Closing'));
  assert.equal(bridge.VerveWebSocketState(id), 5);
  bridge.VerveWebSocketRelease(id);
  const failed = bridge.VerveWebSocketCreate(pointerTo('invalid'), 1024);
  assert.match(UTF8ToString(bridge.VerveWebSocketError(failed)), /invalid URL/);
  bridge.VerveWebSocketRelease(failed);
});

test('updating the receive limit enforces the new budget on existing connections', () => {
  const { id, socket } = connect();
  socket.onmessage({ data: 'buffered' });
  bridge.VerveWebSocketSetLimit(id, 3);
  assert.notEqual(bridge.VerveWebSocketError(id), 0);
  assert.equal(socket.readyState, 3);
  bridge.VerveWebSocketRelease(id);
});
