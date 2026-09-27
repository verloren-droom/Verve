mergeInto(LibraryManager.library, {
  $VerveSockets: { nextId: 1, connections: {} },

  VerveWebSocketCreate__deps: ['$VerveSockets'],
  VerveWebSocketCreate: function (uri, maxBufferedBytes) {
    var id = VerveSockets.nextId++;
    var connection = { socket: null, queue: [], offset: 0, bytes: 0, error: null, closing: false, limit: maxBufferedBytes };
    VerveSockets.connections[id] = connection;
    try {
      var socket = new WebSocket(UTF8ToString(uri));
      connection.socket = socket;
      socket.onmessage = function (event) {
        if (typeof event.data !== 'string') {
          connection.error = 'Received a non-text WebSocket message.';
          socket.close();
          return;
        }
        var length = lengthBytesUTF8(event.data);
        // 同时限制字节数和条目数，空消息也不能无限堆积。
        if (connection.bytes + length > connection.limit || connection.queue.length >= 1024) {
          connection.error = 'WebSocket receive queue exceeds its limit.';
          socket.close();
          return;
        }
        var bytes = new Uint8Array(length + 1);
        stringToUTF8Array(event.data, bytes, 0, bytes.length);
        connection.queue.push(bytes.subarray(0, length));
        connection.bytes += length;
      };
      socket.onerror = function () { connection.error = 'Browser WebSocket connection failed.'; };
    } catch (error) { connection.error = String(error); }
    return id;
  },

  VerveWebSocketState__deps: ['$VerveSockets'],
  VerveWebSocketState: function (id) {
    var connection = VerveSockets.connections[id];
    if (!connection.socket) return 5;
    switch (connection.socket.readyState) {
      case 0: return 1;
      case 1: return 2;
      case 2: return 3;
      default: return connection.closing ? 5 : 4;
    }
  },

  VerveWebSocketError__deps: ['$VerveSockets'],
  VerveWebSocketError: function (id) {
    var error = VerveSockets.connections[id].error;
    if (!error) return 0;
    var size = lengthBytesUTF8(error) + 1;
    var pointer = _malloc(size);
    stringToUTF8(error, pointer, size);
    return pointer; // IL2CPP 负责释放返回字符串。
  },

  VerveWebSocketSend__deps: ['$VerveSockets'],
  VerveWebSocketSend: function (id, buffer, offset, count) {
    var connection = VerveSockets.connections[id];
    try {
      // 显式长度保留文本内的 NUL 字符。
      var text = new TextDecoder('utf-8', { fatal: true, ignoreBOM: true }).decode(HEAPU8.subarray(buffer + offset, buffer + offset + count));
      connection.socket.send(text);
    } catch (error) { connection.error = String(error); }
  },

  VerveWebSocketPeek__deps: ['$VerveSockets'],
  VerveWebSocketPeek: function (id) {
    var connection = VerveSockets.connections[id];
    return connection.queue.length ? connection.queue[0].length - connection.offset : -1;
  },

  VerveWebSocketReceive__deps: ['$VerveSockets'],
  VerveWebSocketReceive: function (id, buffer, offset, count) {
    var connection = VerveSockets.connections[id];
    var message = connection.queue[0];
    HEAPU8.set(message.subarray(connection.offset, connection.offset + count), buffer + offset);
    connection.offset += count;
    connection.bytes -= count;
    if (connection.offset === message.length) {
      connection.queue.shift();
      connection.offset = 0;
    }
  },

  VerveWebSocketClose__deps: ['$VerveSockets'],
  VerveWebSocketClose: function (id, status, description) {
    var connection = VerveSockets.connections[id];
    connection.closing = true;
    try { connection.socket.close(status, UTF8ToString(description)); }
    catch (error) { connection.error = String(error); }
  },

  VerveWebSocketSetLimit__deps: ['$VerveSockets'],
  VerveWebSocketSetLimit: function (id, limit) {
    var connection = VerveSockets.connections[id];
    connection.limit = limit;
    if (connection.bytes > limit) {
      connection.error = 'WebSocket receive queue exceeds its limit.';
      connection.socket.close();
    }
  },

  VerveWebSocketRelease__deps: ['$VerveSockets'],
  VerveWebSocketRelease: function (id) {
    var connection = VerveSockets.connections[id];
    delete VerveSockets.connections[id];
    var socket = connection.socket;
    if (socket) {
      socket.onmessage = socket.onerror = null;
      if (socket.readyState < 2) socket.close();
    }
    connection.queue.length = 0;
  }
});
