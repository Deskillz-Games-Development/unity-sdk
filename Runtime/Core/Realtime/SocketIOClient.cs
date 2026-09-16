// =============================================================================
// Deskillz SDK for Unity -- Socket.IO v4 client  [P1-3.7.0]
// Engine.IO v4 over System.Net.WebSockets.ClientWebSocket. One instance per
// namespace ("/" = match relay, "/lobby" = rooms / quick play). Auth is sent
// in the CONNECT packet as { token } and re-read from the token provider on
// every (re)connect (mirrors the web bridge's N250c callback auth).
// Callbacks are queued and delivered on the Unity main thread by PumpAll(),
// which DeskillzManager calls from Update().
// WebGL/Web builds: no native socket in Patch 1 (a jslib bridge lands in
// Patch 2); Connect() reports WebSocketError on that platform.
// =============================================================================
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#if !UNITY_WEBGL || UNITY_EDITOR
using System.Net.WebSockets;
#endif

namespace Deskillz.Realtime
{
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
        private const int RECONNECT_BASE_MS = 1000;
        private const int RECONNECT_MAX_MS = 15000;

        private readonly string _origin;
        private readonly Func<string> _tokenProvider;
        private readonly ConcurrentQueue<Action> _mainThread = new ConcurrentQueue<Action>();
        private readonly Dictionary<int, Action<string>> _acks = new Dictionary<int, Action<string>>();
        private static readonly List<SocketIOClient> Live = new List<SocketIOClient>();
        private static readonly object LiveLock = new object();

        private int _ackId;
        private int _reconnectAttempts;
        private bool _closedByUser;
        private CancellationTokenSource _cts;
#if !UNITY_WEBGL || UNITY_EDITOR
        private ClientWebSocket _ws;
        private Timer _pingTimeoutTimer;
        private int _pingTimeoutMs = 20000;
#endif

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
        }

        private void Post(Action a) => _mainThread.Enqueue(a);

        // ---------------------------------------------------------------------
        // Connect / disconnect
        // ---------------------------------------------------------------------
        public void Connect()
        {
            _closedByUser = false;
#if UNITY_WEBGL && !UNITY_EDITOR
            Post(() => OnError?.Invoke("Socket.IO is not available on Web builds in SDK 3.7.0 Patch 1"));
#else
            if (_ws != null && _ws.State == WebSocketState.Open) return;
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            _ = RunAsync(_cts.Token);
#endif
        }

        public void Disconnect()
        {
            _closedByUser = true;
#if !UNITY_WEBGL || UNITY_EDITOR
            try { if (_ws != null && _ws.State == WebSocketState.Open) _ = SendRawAsync("41" + NsPrefix()); }
            catch (Exception) { }
            _cts?.Cancel();
            CloseSocket();
#endif
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

        // ---------------------------------------------------------------------
        // Emit
        // ---------------------------------------------------------------------
        /// <summary>Emit an event. `data` may be any object (serialized camelCase) or a pre-built JSON string.</summary>
        public void Emit(string eventName, object data = null, Action<string> ack = null)
        {
            if (!IsConnected)
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
                var n = Interlocked.Increment(ref _ackId);
                lock (_acks) _acks[n] = ack;
                id = n.ToString(CultureInfo.InvariantCulture);
            }
#if !UNITY_WEBGL || UNITY_EDITOR
            _ = SendRawAsync("42" + NsPrefix() + id + args);
#endif
        }

        private string NsPrefix() => Namespace == "/" ? "" : Namespace + ",";

#if !UNITY_WEBGL || UNITY_EDITOR
        // ---------------------------------------------------------------------
        // Engine.IO transport
        // ---------------------------------------------------------------------
        private async Task RunAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && !_closedByUser)
            {
                try
                {
                    _ws = new ClientWebSocket();
                    var wsOrigin = _origin.Replace("https://", "wss://").Replace("http://", "ws://");
                    var uri = new Uri(wsOrigin + "/socket.io/?EIO=4&transport=websocket");
                    await _ws.ConnectAsync(uri, ct);
                    await ReceiveLoopAsync(ct);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    var msg = ex.Message;
                    Post(() => OnError?.Invoke(msg));
                }
                var wasConnected = IsConnected;
                IsConnected = false;
                CloseSocket();
                if (wasConnected) Post(() => OnDisconnected?.Invoke("transport close"));
                if (_closedByUser || ct.IsCancellationRequested) break;
                if (++_reconnectAttempts > MAX_RECONNECT_ATTEMPTS)
                {
                    Post(() => OnError?.Invoke("reconnect attempts exhausted"));
                    break;
                }
                var delay = Math.Min(RECONNECT_MAX_MS, RECONNECT_BASE_MS * (1 << Math.Min(_reconnectAttempts, 4)));
                try { await Task.Delay(delay, ct); } catch (OperationCanceledException) { break; }
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken ct)
        {
            var buffer = new byte[16 * 1024];
            var sb = new StringBuilder();
            while (_ws != null && _ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                sb.Clear();
                WebSocketReceiveResult r;
                do
                {
                    r = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                    if (r.MessageType == WebSocketMessageType.Close) return;
                    sb.Append(Encoding.UTF8.GetString(buffer, 0, r.Count));
                } while (!r.EndOfMessage);
                await HandlePacketAsync(sb.ToString());
            }
        }

        private async Task HandlePacketAsync(string packet)
        {
            if (packet.Length == 0) return;
            switch (packet[0])
            {
                case '0': // OPEN {"sid","pingInterval","pingTimeout"}
                    try
                    {
                        var open = JObject.Parse(packet.Substring(1));
                        _pingTimeoutMs = (open.Value<int?>("pingInterval") ?? 25000) + (open.Value<int?>("pingTimeout") ?? 20000);
                    }
                    catch (JsonException) { }
                    ArmPingTimeout();
                    var token = _tokenProvider?.Invoke();
                    var auth = string.IsNullOrEmpty(token) ? "{}" : "{\"token\":" + JsonConvert.SerializeObject(token) + "}";
                    await SendRawAsync("40" + NsPrefix() + auth);
                    break;
                case '2': // PING -> PONG
                    ArmPingTimeout();
                    await SendRawAsync("3");
                    break;
                case '4': // MESSAGE
                    HandleSocketPacket(packet.Substring(1));
                    break;
                case '1': // CLOSE
                    CloseSocket();
                    break;
            }
        }

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

        private async Task SendRawAsync(string text)
        {
            var ws = _ws;
            if (ws == null || ws.State != WebSocketState.Open) return;
            var bytes = Encoding.UTF8.GetBytes(text);
            try { await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None); }
            catch (Exception ex) { Post(() => OnError?.Invoke("send failed: " + ex.Message)); }
        }

        private void ArmPingTimeout()
        {
            _pingTimeoutTimer?.Dispose();
            _pingTimeoutTimer = new Timer(_ =>
            {
                DeskillzLogger.Warning($"[SocketIO {Namespace}] ping timeout, dropping connection");
                CloseSocket();
            }, null, _pingTimeoutMs, Timeout.Infinite);
        }

        private void CloseSocket()
        {
            _pingTimeoutTimer?.Dispose();
            _pingTimeoutTimer = null;
            var ws = _ws;
            _ws = null;
            if (ws == null) return;
            try { ws.Abort(); } catch (Exception) { }
            try { ws.Dispose(); } catch (Exception) { }
        }
#endif
    }
}
