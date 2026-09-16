// Deskillz SDK for Unity -- browser WebSocket bridge for Web builds  [P2-3.7.0]
// Engine.IO / Socket.IO framing stays in C# (SocketIOClient); this only moves bytes.
var DeskillzWebSocketLib = {
  $DSKWS: {
    sockets: {},
    nextId: 1,
    onOpen: null,
    onMessage: null,
    onError: null,
    onClose: null
  },

  DeskillzWS_Init: function (onOpen, onMessage, onError, onClose) {
    DSKWS.onOpen = onOpen;
    DSKWS.onMessage = onMessage;
    DSKWS.onError = onError;
    DSKWS.onClose = onClose;
  },

  DeskillzWS_Connect: function (urlPtr) {
    var url = UTF8ToString(urlPtr);
    var id = DSKWS.nextId++;
    var ws;
    try {
      ws = new WebSocket(url);
    } catch (e) {
      return -1;
    }
    DSKWS.sockets[id] = ws;
    ws.onopen = function () {
      {{{ makeDynCall('vi', 'DSKWS.onOpen') }}}(id);
    };
    ws.onmessage = function (ev) {
      if (typeof ev.data !== 'string') return;
      var len = lengthBytesUTF8(ev.data) + 1;
      var buf = _malloc(len);
      stringToUTF8(ev.data, buf, len);
      {{{ makeDynCall('vii', 'DSKWS.onMessage') }}}(id, buf);
      _free(buf);
    };
    ws.onerror = function () {
      {{{ makeDynCall('vi', 'DSKWS.onError') }}}(id);
    };
    ws.onclose = function (ev) {
      {{{ makeDynCall('vii', 'DSKWS.onClose') }}}(id, ev.code | 0);
      delete DSKWS.sockets[id];
    };
    return id;
  },

  DeskillzWS_Send: function (id, msgPtr) {
    var ws = DSKWS.sockets[id];
    if (!ws || ws.readyState !== 1) return 0;
    ws.send(UTF8ToString(msgPtr));
    return 1;
  },

  DeskillzWS_State: function (id) {
    var ws = DSKWS.sockets[id];
    return ws ? ws.readyState : 3;
  },

  DeskillzWS_Close: function (id) {
    var ws = DSKWS.sockets[id];
    if (!ws) return;
    try { ws.close(1000, 'client'); } catch (e) {}
    delete DSKWS.sockets[id];
  }
};

autoAddDeps(DeskillzWebSocketLib, '$DSKWS');
mergeInto(LibraryManager.library, DeskillzWebSocketLib);
