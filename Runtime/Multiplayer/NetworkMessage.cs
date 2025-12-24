// =============================================================================
// Deskillz SDK for Unity - Phase 5: Sync Multiplayer
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Deskillz.Multiplayer
{
    // =============================================================================
    // NETWORK MESSAGE
    // =============================================================================

    /// <summary>
    /// Network message for multiplayer communication.
    /// </summary>
    [Serializable]
    public class NetworkMessage
    {
        /// <summary>Message type.</summary>
        public MessageType Type { get; set; }

        /// <summary>Sender player ID.</summary>
        public string SenderId { get; set; }

        /// <summary>Target player ID (null for broadcast).</summary>
        public string TargetId { get; set; }

        /// <summary>Sequence number for ordering.</summary>
        public int Sequence { get; set; }

        /// <summary>Server timestamp.</summary>
        public double Timestamp { get; set; }

        /// <summary>Delivery mode.</summary>
        public DeliveryMode DeliveryMode { get; set; } = DeliveryMode.Reliable;

        /// <summary>JSON payload.</summary>
        public string Payload { get; set; }

        /// <summary>Message channel for prioritization.</summary>
        public int Channel { get; set; }

        /// <summary>
        /// Deserialize payload to type.
        /// </summary>
        public T GetPayload<T>() where T : class
        {
            if (string.IsNullOrEmpty(Payload)) return null;
            return JsonUtility.FromJson<T>(Payload);
        }

        /// <summary>
        /// Set payload from object.
        /// </summary>
        public void SetPayload<T>(T data) where T : class
        {
            Payload = JsonUtility.ToJson(data);
        }
    }

    // =============================================================================
    // MESSAGE TYPES
    // =============================================================================

    /// <summary>
    /// Types of network messages.
    /// </summary>
    public enum MessageType
    {
        /// <summary>Unknown message type.</summary>
        Unknown = 0,

        // Connection
        /// <summary>Player joined room.</summary>
        PlayerJoined = 1,
        /// <summary>Player left room.</summary>
        PlayerLeft = 2,
        /// <summary>Connection heartbeat.</summary>
        Heartbeat = 3,

        // State
        /// <summary>Player state update.</summary>
        StateUpdate = 10,
        /// <summary>Full state sync.</summary>
        FullSync = 11,
        /// <summary>Delta state update.</summary>
        DeltaSync = 12,

        // Room
        /// <summary>Room state broadcast.</summary>
        RoomState = 20,
        /// <summary>Room settings changed.</summary>
        RoomSettings = 21,
        /// <summary>Room closed.</summary>
        RoomClosed = 22,

        // Time
        /// <summary>Time synchronization.</summary>
        TimeSync = 30,

        // Game
        /// <summary>Match started.</summary>
        MatchStart = 40,
        /// <summary>Match ended.</summary>
        MatchEnd = 41,
        /// <summary>Score update.</summary>
        ScoreUpdate = 42,
        /// <summary>Game event.</summary>
        GameEvent = 43,

        // Custom
        /// <summary>Custom game message.</summary>
        Custom = 100,

        // RPC
        /// <summary>Remote procedure call.</summary>
        RPC = 200,
        /// <summary>RPC response.</summary>
        RPCResponse = 201
    }

    // =============================================================================
    // DELIVERY MODE
    // =============================================================================

    /// <summary>
    /// Message delivery reliability.
    /// </summary>
    public enum DeliveryMode
    {
        /// <summary>Best effort, may be dropped.</summary>
        Unreliable,

        /// <summary>Guaranteed delivery, may be out of order.</summary>
        Reliable,

        /// <summary>Guaranteed delivery and order.</summary>
        ReliableOrdered,

        /// <summary>Best effort, but sequenced (drop old).</summary>
        UnreliableSequenced
    }

    // =============================================================================
    // MESSAGE SERIALIZER
    // =============================================================================

    /// <summary>
    /// Handles message serialization and deserialization.
    /// </summary>
    public class MessageSerializer
    {
        /// <summary>
        /// Serialize a network message to JSON.
        /// </summary>
        public string SerializeMessage(NetworkMessage message)
        {
            return JsonUtility.ToJson(new SerializableMessage
            {
                t = (int)message.Type,
                s = message.SenderId,
                r = message.TargetId,
                q = message.Sequence,
                ts = message.Timestamp,
                d = (int)message.DeliveryMode,
                p = message.Payload,
                c = message.Channel
            });
        }

        /// <summary>
        /// Deserialize a network message from JSON.
        /// </summary>
        public NetworkMessage DeserializeMessage(string json)
        {
            try
            {
                var data = JsonUtility.FromJson<SerializableMessage>(json);
                return new NetworkMessage
                {
                    Type = (MessageType)data.t,
                    SenderId = data.s,
                    TargetId = data.r,
                    Sequence = data.q,
                    Timestamp = data.ts,
                    DeliveryMode = (DeliveryMode)data.d,
                    Payload = data.p,
                    Channel = data.c
                };
            }
            catch (Exception ex)
            {
                DeskillzLogger.Error($"Failed to deserialize message: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Serialize an object to JSON.
        /// </summary>
        public string Serialize<T>(T obj) where T : class
        {
            if (obj == null) return null;
            return JsonUtility.ToJson(obj);
        }

        /// <summary>
        /// Deserialize JSON to object.
        /// </summary>
        public T Deserialize<T>(string json) where T : class
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Internal serializable message format.
        /// </summary>
        [Serializable]
        private class SerializableMessage
        {
            public int t;      // type
            public string s;   // sender
            public string r;   // receiver/target
            public int q;      // sequence
            public double ts;  // timestamp
            public int d;      // delivery mode
            public string p;   // payload
            public int c;      // channel
        }
    }

    // =============================================================================
    // WEBSOCKET CLIENT
    // =============================================================================

    /// <summary>
    /// WebSocket client wrapper for Unity.
    /// </summary>
    public class WebSocketClient
    {
        // Events
        public event Action OnOpen;
        public event Action<string> OnMessage;
        public event Action<string> OnClose;
        public event Action<string> OnError;

        // State
        private readonly string _url;
        private bool _isConnected;
        private readonly Queue<string> _sendQueue = new Queue<string>();

        // For actual implementation, use a WebSocket library like:
        // - NativeWebSocket
        // - WebSocketSharp
        // - Best HTTP/2

        public bool IsConnected => _isConnected;

        public WebSocketClient(string url)
        {
            _url = url;
        }

        /// <summary>
        /// Connect to WebSocket server.
        /// </summary>
        public System.Threading.Tasks.Task ConnectAsync()
        {
            DeskillzLogger.Debug($"Connecting to WebSocket: {_url}");

            // In test mode, simulate connection
            if (Deskillz.TestMode)
            {
                _isConnected = true;
                OnOpen?.Invoke();
                return System.Threading.Tasks.Task.CompletedTask;
            }

            // TODO: Implement actual WebSocket connection
            // This would use a WebSocket library like NativeWebSocket
            // Example with NativeWebSocket:
            // _webSocket = new NativeWebSocket.WebSocket(_url);
            // _webSocket.OnOpen += () => { _isConnected = true; OnOpen?.Invoke(); };
            // _webSocket.OnMessage += (bytes) => OnMessage?.Invoke(Encoding.UTF8.GetString(bytes));
            // _webSocket.OnClose += (code) => { _isConnected = false; OnClose?.Invoke(code.ToString()); };
            // _webSocket.OnError += (error) => OnError?.Invoke(error);
            // return _webSocket.Connect();

            // Placeholder - simulate connection for now
            _isConnected = true;
            OnOpen?.Invoke();
            return System.Threading.Tasks.Task.CompletedTask;
        }

        /// <summary>
        /// Send message through WebSocket.
        /// </summary>
        public void Send(string message)
        {
            if (!_isConnected)
            {
                _sendQueue.Enqueue(message);
                return;
            }

            // In test mode, just log
            if (Deskillz.TestMode)
            {
                DeskillzLogger.Verbose($"WS Send: {message}");
                return;
            }

            // TODO: Implement actual send
            // _webSocket?.SendText(message);
            DeskillzLogger.Verbose($"WS Send: {message.Substring(0, Math.Min(100, message.Length))}...");
        }

        /// <summary>
        /// Send binary data.
        /// </summary>
        public void SendBinary(byte[] data)
        {
            if (!_isConnected) return;

            // TODO: Implement binary send
            // _webSocket?.Send(data);
        }

        /// <summary>
        /// Close the connection.
        /// </summary>
        public void Close()
        {
            if (!_isConnected) return;

            _isConnected = false;

            // TODO: Implement actual close
            // _webSocket?.Close();

            OnClose?.Invoke("Client closed");
        }

        /// <summary>
        /// Dispatch messages (call from Update).
        /// </summary>
        public void DispatchMessages()
        {
            // TODO: For NativeWebSocket, call:
            // _webSocket?.DispatchMessageQueue();
        }
    }

    // =============================================================================
    // RPC SYSTEM
    // =============================================================================

    /// <summary>
    /// Remote Procedure Call message.
    /// </summary>
    [Serializable]
    public class RPCMessage
    {
        /// <summary>Method name to call.</summary>
        public string Method { get; set; }

        /// <summary>Arguments as JSON.</summary>
        public string[] Arguments { get; set; }

        /// <summary>RPC ID for response matching.</summary>
        public int RpcId { get; set; }

        /// <summary>Target player (null for all).</summary>
        public string Target { get; set; }
    }

    /// <summary>
    /// RPC response message.
    /// </summary>
    [Serializable]
    public class RPCResponse
    {
        /// <summary>Matching RPC ID.</summary>
        public int RpcId { get; set; }

        /// <summary>Whether call succeeded.</summary>
        public bool Success { get; set; }

        /// <summary>Result as JSON.</summary>
        public string Result { get; set; }

        /// <summary>Error message if failed.</summary>
        public string Error { get; set; }
    }

    /// <summary>
    /// Attribute to mark methods as network-callable.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class NetworkRPCAttribute : Attribute
    {
        /// <summary>Whether only the host can call this.</summary>
        public bool HostOnly { get; set; }

        /// <summary>Whether only the server can call this.</summary>
        public bool ServerOnly { get; set; }
    }

    // =============================================================================
    // GAME EVENT MESSAGE
    // =============================================================================

    /// <summary>
    /// Generic game event message.
    /// </summary>
    [Serializable]
    public class GameEventMessage
    {
        /// <summary>Event type identifier.</summary>
        public string EventType { get; set; }

        /// <summary>Event data as JSON.</summary>
        public string Data { get; set; }

        /// <summary>World position (if applicable).</summary>
        public Vector3 Position { get; set; }

        /// <summary>Affected player IDs.</summary>
        public string[] AffectedPlayers { get; set; }
    }

    // =============================================================================
    // COMMON GAME EVENTS
    // =============================================================================

    /// <summary>
    /// Pre-defined game event types.
    /// </summary>
    public static class GameEvents
    {
        public const string PlayerSpawned = "player_spawned";
        public const string PlayerDied = "player_died";
        public const string PlayerRespawned = "player_respawned";
        public const string ItemPickup = "item_pickup";
        public const string ItemDrop = "item_drop";
        public const string DamageDealt = "damage_dealt";
        public const string EffectTriggered = "effect_triggered";
        public const string ObjectiveComplete = "objective_complete";
        public const string RoundStart = "round_start";
        public const string RoundEnd = "round_end";
        public const string PowerUpActivated = "powerup_activated";
        public const string ChatMessage = "chat_message";
        public const string Emote = "emote";
    }
}
