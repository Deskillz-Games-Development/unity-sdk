// =============================================================================
// Deskillz SDK for Unity -- Socket.IO v4 client  [P1-3.7.0] [P2-3.7.0]
// Engine.IO v4 framing over a pluggable text WebSocket transport:
//   native (Android / iOS / Standalone / Editor) : System.Net.WebSockets.ClientWebSocket
//   Web (UNITY_WEBGL)                            : browser WebSocket via DeskillzWebSocket.jslib
// One instance per namespace ("/" = match relay, "/lobby" = rooms / quick play). Auth is
// sent in the CONNECT packet as { token } from the token provider on every (re)connect.
// The client is frame-driven: DeskillzManager.Update() calls PumpAll(), which delivers
// queued callbacks on the main thread, arms reconnects and enforces the ping timeout.
// =============================================================================
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Deskillz.Realtime
{
    /// <summary>Minimal text WebSocket surface the Socket.IO layer needs.</summary>
    internal interface ISocketTransport : IDisposable
    {
        bool IsOpen { get; }
        event Action OnOpen;
        event Action<string> OnMessage;
        event Action<string> OnError;
        event Action<int> OnClose;
        void Connect(Uri uri);
        void Send(string text);
        void Close();
    }

    public sealed class SocketIOClient : IDisposable
    {
        public event Action OnConnected;
        public event Action<string> OnDisconnected;
        public event Action<string> OnError;
        /// <summary>Every server event: (eventName, dataJson). dataJson is the first argument serialized.</summary>
        public event Action<string, string> OnEvent;

        public string Namespace { get; }
        public bool IsConnected { get; private set; }

        private const int MAX_RECONNECT_ATTEMPTS = 10;
        private const float RECONNECT_BASE_S = 1f;
        private const float RECONNECT_MAX_S = 15f;

        private readonly string _origin;
        private readonly Func<string> _tokenProvider;
        private readonly ConcurrentQueue<Action> _mainThread = new ConcurrentQueue<Action>();
        private readonly Dictionary<int, Action<string>> _acks = new Dictionary<int, Action<string>>();
        private static readonly List<SocketIOClient> Live = new List<SocketIOClient>();
        private static readonly object LiveLock = new object();

        private ISocketTransport _transport;
        private int _ackId;
        private int _reconnectAttempts;
        private bool _closedByUser;
        private bool _wantConnection;
        private float _reconnectAt = -1f;      // Time.realtimeSinceStartup deadline, -1 = none
        private float _pingDeadline = -1f;     // ping timeout deadline, -1 = none
        private float _pingWindowS = 45f;      // pingInterval + pingTimeout from the OPEN packet

        /// <param name="origin">https://host (the API origin, not wss://).</param>
        /// <param name="ns">"/" or "/lobby".</param>
        /// <param name="tokenProvider">Returns the current access token (called on every connect).</param>
        public SocketIOClient(string origin, string ns, Func<string> tokenProvider)
        {
            _origin = origin.TrimEnd('/');
            Namespace = string.IsNullOrEmpty(ns) ? "/" : ns;
            _tokenProvider = tokenProvider;
            lock (LiveLock) Live.Add(this);
        }

        // ---------------------------------------------------------------------
        // Main-thread pump (called by DeskillzManager.Update)
        // ---------------------------------------------------------------------
        public static void PumpAll()
        {
            SocketIOClient[] snapshot;
            lock (LiveLock) snapshot = Live.ToArray();
            foreach (var c in snapshot) c.Pump();
        }

        private void Pump()
        {
            while (_mainThread.TryDequeue(out var a))
            {
                try { a(); }
                catch (Exception ex) { DeskillzLogger.Error("[SocketIO] callback error: " + ex.Message); }
            }
            var now = Time.realtimeSinceStartup;
            if (_reconnectAt >= 0f && now >= _reconnectAt)
            {
                _reconnectAt = -1f;
                if (_wantConnection && !_closedByUser) Open();
            }
            if (_pingDeadline >= 0f && now >= _pingDeadline)
            {
                _pingDeadline = -1f;
                DeskillzLogger.Warning($"[SocketIO {Namespace}] ping timeout, dropping connection");
                _transport?.Close();
            }
        }

        private void Post(Action a) => _mainThread.Enqueue(a);

        // ---------------------------------------------------------------------
        // Connect / disconnect
        // ---------------------------------------------------------------------
        public void Connect()
        {
            _closedByUser = false;
            _wantConnection = true;
            if (_transport != null && _transport.IsOpen) return;
            Open();
        }

        private void Open()
        {
            DisposeTransport();
            _transport = CreateTransport();
            _transport.OnOpen += HandleTransportOpen;
            _transport.OnMessage += HandleTransportMessage;
            _transport.OnError += HandleTransportError;
            _transport.OnClose += HandleTransportClose;
            var wsOrigin = _origin.Replace("https://", "wss://").Replace("http://", "ws://");
            try
            {
                _transport.Connect(new Uri(wsOrigin + "/socket.io/?EIO=4&transport=websocket"));
            }
            catch (Exception ex)
            {
                HandleTransportError(ex.Message);
                HandleTransportClose(1006);
            }
        }

        private static ISocketTransport CreateTransport()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return new WebGLSocketTransport();
#else
            return new NativeSocketTransport();
#endif
        }

        public void Disconnect()
        {
            _closedByUser = true;
            _wantConnection = false;
            _reconnectAt = -1f;
            _pingDeadline = -1f;
            if (_transport != null && _transport.IsOpen)
            {
                try { _transport.Send("41" + NsPrefix()); } catch (Exception) { }
            }
            DisposeTransport();
            if (IsConnected)
            {
                IsConnected = false;
                Post(() => OnDisconnected?.Invoke("client"));
            }
        }

        public void Dispose()
        {
            Disconnect();
            lock (LiveLock) Live.Remove(this);
        }

        private void DisposeTransport()
        {
            var tr = _transport;
            _transport = null;
            if (tr == null) return;
            tr.OnOpen -= HandleTransportOpen;
            tr.OnMessage -= HandleTransportMessage;
            tr.OnError -= HandleTransportError;
            tr.OnClose -= HandleTransportClose;
            try { tr.Dispose(); } catch (Exception) { }
        }

        // ---------------------------------------------------------------------
        // Emit
        // ---------------------------------------------------------------------
        /// <summary>Emit an event. `data` may be any object (serialized camelCase) or a pre-built JSON string.</summary>
        public void Emit(string eventName, object data = null, Action<string> ack = null)
        {
            if (!IsConnected || _transport == null || !_transport.IsOpen)
            {
                DeskillzLogger.Warning($"[SocketIO {Namespace}] emit '{eventName}' dropped: not connected");
                return;
            }
            var args = new StringBuilder();
            args.Append('[').Append(JsonConvert.SerializeObject(eventName));
            if (data != null)
            {
                args.Append(',');
                args.Append(data is string s ? s : DeskillzJson.Serialize(data));
            }
            args.Append(']');
            var id = "";
            if (ack != null)
            {
                var n = ++_ackId;
                lock (_acks) _acks[n] = ack;
                id = n.ToString(CultureInfo.InvariantCulture);
            }
            SendRaw("42" + NsPrefix() + id + args);
        }

        private string NsPrefix() => Namespace == "/" ? "" : Namespace + ",";

        private void SendRaw(string text)
        {
            var tr = _transport;
            if (tr == null || !tr.IsOpen) return;
            try { tr.Send(text); }
            catch (Exception ex) { Post(() => OnError?.Invoke("send failed: " + ex.Message)); }
        }

        // ---------------------------------------------------------------------
        // Transport events (may arrive off the main thread on native)
        // ---------------------------------------------------------------------
        private void HandleTransportOpen()
        {
            // Engine.IO sends the OPEN packet first; nothing to do until then.
        }

        private void HandleTransportMessage(string packet)
        {
            if (packet.Length == 0) return;
            switch (packet[0])
            {
                case '0': // OPEN {"sid","pingInterval","pingTimeout"}
                    try
                    {
                        var open = JObject.Parse(packet.Substring(1));
                        _pingWindowS = ((open.Value<int?>("pingInterval") ?? 25000) + (open.Value<int?>("pingTimeout") ?? 20000)) / 1000f;
                    }
                    catch (JsonException) { }
                    ArmPingTimeout();
                    var token = _tokenProvider?.Invoke();
                    var auth = string.IsNullOrEmpty(token) ? "{}" : "{\"token\":" + JsonConvert.SerializeObject(token) + "}";
                    SendRaw("40" + NsPrefix() + auth);
                    break;
                case '2': // PING -> PONG
                    ArmPingTimeout();
                    SendRaw("3");
                    break;
                case '4': // MESSAGE
                    HandleSocketPacket(packet.Substring(1));
                    break;
                case '1': // CLOSE
                    _transport?.Close();
                    break;
            }
        }

        private void HandleTransportError(string error)
        {
            var msg = error;
            Post(() => OnError?.Invoke(msg));
        }

        private void HandleTransportClose(int code)
        {
            var wasConnected = IsConnected;
            IsConnected = false;
            _pingDeadline = -1f;
            if (wasConnected) Post(() => OnDisconnected?.Invoke("transport close " + code));
            if (_closedByUser || !_wantConnection) return;
            if (++_reconnectAttempts > MAX_RECONNECT_ATTEMPTS)
            {
                _wantConnection = false;
                Post(() => OnError?.Invoke("reconnect attempts exhausted"));
                return;
            }
            var delay = Math.Min(RECONNECT_MAX_S, RECONNECT_BASE_S * (1 << Math.Min(_reconnectAttempts, 4)));
            Post(() => _reconnectAt = Time.realtimeSinceStartup + delay);
        }

        private void ArmPingTimeout()
        {
            Post(() => _pingDeadline = Time.realtimeSinceStartup + _pingWindowS);
        }

        // ---------------------------------------------------------------------
        // Socket.IO packet layer
        // ---------------------------------------------------------------------
        private void HandleSocketPacket(string p)
        {
            if (p.Length == 0) return;
            var type = p[0];
            var i = 1;
            var ns = "/";
            if (i < p.Length && p[i] == '/')
            {
                var comma = p.IndexOf(',', i);
                if (comma < 0) return;
                ns = p.Substring(i, comma - i);
                i = comma + 1;
            }
            if (ns != Namespace) return;
            var idStart = i;
            while (i < p.Length && char.IsDigit(p[i])) i++;
            var idStr = p.Substring(idStart, i - idStart);
            var payload = i < p.Length ? p.Substring(i) : "";
            switch (type)
            {
                case '0': // CONNECT ack
                    IsConnected = true;
                    _reconnectAttempts = 0;
                    Post(() => OnConnected?.Invoke());
                    break;
                case '1': // DISCONNECT from server
                    IsConnected = false;
                    Post(() => OnDisconnected?.Invoke("server"));
                    break;
                case '2': // EVENT ["name", data...]
                    DispatchEvent(payload);
                    break;
                case '3': // ACK
                    if (int.TryParse(idStr, out var ackId))
                    {
                        Action<string> cb = null;
                        lock (_acks) { if (_acks.TryGetValue(ackId, out cb)) _acks.Remove(ackId); }
                        if (cb != null)
                        {
                            var first = FirstArg(payload);
                            Post(() => cb(first));
                        }
                    }
                    break;
                case '4': // CONNECT_ERROR {"message"}
                    var err = DeskillzJson.GetString(payload, "message") ?? payload;
                    IsConnected = false;
                    Post(() => OnError?.Invoke("connect_error: " + err));
                    break;
            }
        }

        private void DispatchEvent(string payload)
        {
            try
            {
                var arr = JArray.Parse(payload);
                if (arr.Count == 0) return;
                var name = arr[0].ToString();
                var data = arr.Count > 1 ? arr[1].ToString(Formatting.None) : "{}";
                Post(() => OnEvent?.Invoke(name, data));
            }
            catch (JsonException ex)
            {
                DeskillzLogger.Warning("[SocketIO] bad event payload: " + ex.Message);
            }
        }

        private static string FirstArg(string payload)
        {
            try
            {
                var arr = JArray.Parse(payload);
                return arr.Count > 0 ? arr[0].ToString(Formatting.None) : "{}";
            }
            catch (JsonException) { return payload; }
        }
    }
}
