// =============================================================================
// Deskillz SDK for Unity -- Web (browser) WebSocket transport  [P2-3.7.0]
// Backed by Runtime/Plugins/WebGL/DeskillzWebSocket.jslib. The browser is single
// threaded, so callbacks already arrive on the main thread.
// =============================================================================
#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;

namespace Deskillz.Realtime
{
    internal sealed class WebGLSocketTransport : ISocketTransport
    {
        public event Action OnOpen;
        public event Action<string> OnMessage;
        public event Action<string> OnError;
        public event Action<int> OnClose;

        private delegate void OpenCallback(int id);
        private delegate void MessageCallback(int id, IntPtr msg);
        private delegate void ErrorCallback(int id);
        private delegate void CloseCallback(int id, int code);

        [DllImport("__Internal")] private static extern void DeskillzWS_Init(OpenCallback onOpen, MessageCallback onMessage, ErrorCallback onError, CloseCallback onClose);
        [DllImport("__Internal")] private static extern int DeskillzWS_Connect(string url);
        [DllImport("__Internal")] private static extern int DeskillzWS_Send(int id, string msg);
        [DllImport("__Internal")] private static extern int DeskillzWS_State(int id);
        [DllImport("__Internal")] private static extern void DeskillzWS_Close(int id);

        private static readonly Dictionary<int, WebGLSocketTransport> Instances = new Dictionary<int, WebGLSocketTransport>();
        private static bool _initialized;

        private int _id = -1;

        public bool IsOpen => _id > 0 && DeskillzWS_State(_id) == 1;

        public void Connect(Uri uri)
        {
            if (!_initialized)
            {
                DeskillzWS_Init(OnOpenStatic, OnMessageStatic, OnErrorStatic, OnCloseStatic);
                _initialized = true;
            }
            _id = DeskillzWS_Connect(uri.ToString());
            if (_id <= 0)
            {
                OnError?.Invoke("browser refused the WebSocket");
                OnClose?.Invoke(1006);
                return;
            }
            Instances[_id] = this;
        }

        public void Send(string text)
        {
            if (_id > 0) DeskillzWS_Send(_id, text);
        }

        public void Close()
        {
            if (_id <= 0) return;
            var id = _id; _id = -1;
            Instances.Remove(id);
            DeskillzWS_Close(id);
            OnClose?.Invoke(1000);
        }

        public void Dispose() => Close();

        [MonoPInvokeCallback(typeof(OpenCallback))]
        private static void OnOpenStatic(int id)
        {
            if (Instances.TryGetValue(id, out var t)) t.OnOpen?.Invoke();
        }

        [MonoPInvokeCallback(typeof(MessageCallback))]
        private static void OnMessageStatic(int id, IntPtr msg)
        {
            if (Instances.TryGetValue(id, out var t)) t.OnMessage?.Invoke(Marshal.PtrToStringUTF8(msg));
        }

        [MonoPInvokeCallback(typeof(ErrorCallback))]
        private static void OnErrorStatic(int id)
        {
            if (Instances.TryGetValue(id, out var t)) t.OnError?.Invoke("websocket error");
        }

        [MonoPInvokeCallback(typeof(CloseCallback))]
        private static void OnCloseStatic(int id, int code)
        {
            if (Instances.TryGetValue(id, out var t))
            {
                Instances.Remove(id);
                t._id = -1;
                t.OnClose?.Invoke(code);
            }
        }
    }
}
#endif
