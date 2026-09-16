// =============================================================================
// Deskillz SDK for Unity -- native WebSocket transport  [P2-3.7.0]
// Android / iOS / Standalone / Editor. Events are raised on the receive task's
// thread; SocketIOClient marshals them to the main thread.
// =============================================================================
#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Deskillz.Realtime
{
    internal sealed class NativeSocketTransport : ISocketTransport
    {
        public event Action OnOpen;
        public event Action<string> OnMessage;
        public event Action<string> OnError;
        public event Action<int> OnClose;

        private ClientWebSocket _ws;
        private CancellationTokenSource _cts;
        private bool _closedRaised;

        public bool IsOpen => _ws != null && _ws.State == WebSocketState.Open;

        public void Connect(Uri uri)
        {
            _cts = new CancellationTokenSource();
            _ws = new ClientWebSocket();
            _ = RunAsync(uri, _cts.Token);
        }

        private async Task RunAsync(Uri uri, CancellationToken ct)
        {
            try
            {
                await _ws.ConnectAsync(uri, ct);
                OnOpen?.Invoke();
                var buffer = new byte[16 * 1024];
                var sb = new StringBuilder();
                while (_ws != null && _ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
                {
                    sb.Clear();
                    WebSocketReceiveResult r;
                    do
                    {
                        r = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                        if (r.MessageType == WebSocketMessageType.Close) { RaiseClose(1000); return; }
                        sb.Append(Encoding.UTF8.GetString(buffer, 0, r.Count));
                    } while (!r.EndOfMessage);
                    OnMessage?.Invoke(sb.ToString());
                }
                RaiseClose(1000);
            }
            catch (OperationCanceledException) { RaiseClose(1000); }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
                RaiseClose(1006);
            }
        }

        private readonly object _sendLock = new object();
        private Task _sendChain = Task.CompletedTask;

        /// <summary>ClientWebSocket allows one outstanding SendAsync; calls are chained in order.</summary>
        public void Send(string text)
        {
            var ws = _ws;
            if (ws == null || ws.State != WebSocketState.Open) return;
            var bytes = Encoding.UTF8.GetBytes(text);
            lock (_sendLock)
            {
                _sendChain = _sendChain.ContinueWith(async _ =>
                {
                    try { await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None); }
                    catch (Exception ex) { OnError?.Invoke("send failed: " + ex.Message); }
                }, TaskScheduler.Default).Unwrap();
            }
        }

        public void Close()
        {
            try { _cts?.Cancel(); } catch (Exception) { }
            var ws = _ws; _ws = null;
            if (ws != null)
            {
                try { ws.Abort(); } catch (Exception) { }
                try { ws.Dispose(); } catch (Exception) { }
            }
            RaiseClose(1000);
        }

        private void RaiseClose(int code)
        {
            if (_closedRaised) return;
            _closedRaised = true;
            OnClose?.Invoke(code);
        }

        public void Dispose() => Close();
    }
}
#endif
