// =============================================================================
// Deskillz SDK for Unity -- JSON helper  [P1-3.7.0]
// Newtonsoft-based (com.unity.nuget.newtonsoft-json). PascalCase C# members
// serialize as camelCase; deserialization is case-insensitive; the backend's
// optional { data: ... } envelope is unwrapped exactly like the web bridge.
// =============================================================================
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Deskillz
{
    public static class DeskillzJson
    {
        private static readonly JsonSerializerSettings OutSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy(false, false) },
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        private static readonly JsonSerializerSettings InSettings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        /// <summary>Serialize any object (anonymous types included) as camelCase JSON.</summary>
        public static string Serialize(object value)
        {
            if (value == null) return null;
            if (value is string s) return s;
            return JsonConvert.SerializeObject(value, OutSettings);
        }

        /// <summary>Deserialize, unwrapping a { data: ... } envelope when present.</summary>
        public static T Deserialize<T>(string json)
        {
            if (string.IsNullOrEmpty(json)) return default;
            return JsonConvert.DeserializeObject<T>(Unwrap(json), InSettings);
        }

        /// <summary>Return the inner "data" JSON when the body is { data: ... }, else the body.</summary>
        public static string Unwrap(string json)
        {
            if (string.IsNullOrEmpty(json)) return json;
            var t = json.TrimStart();
            if (t.Length == 0 || t[0] != '{') return json;
            try
            {
                var obj = JObject.Parse(json);
                if (obj.TryGetValue("data", out var data) && data != null && data.Type != JTokenType.Null)
                {
                    return data.ToString(Formatting.None);
                }
            }
            catch (JsonException) { }
            return json;
        }

        /// <summary>Read a top-level string field without a model.</summary>
        public static string GetString(string json, string key)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                var obj = JObject.Parse(json);
                var tok = obj[key];
                if (tok == null || tok.Type == JTokenType.Null) return null;
                if (tok.Type == JTokenType.Array) return string.Join("; ", tok.ToObject<string[]>());
                return tok.ToString();
            }
            catch (JsonException) { return null; }
        }

        /// <summary>Prefer the server's { message } over the transport error text.</summary>
        public static DeskillzError WithServerMessage(DeskillzError error, string body)
        {
            var msg = GetString(body, "message") ?? GetString(body, "error");
            if (string.IsNullOrEmpty(msg) || error == null) return error;
            return new DeskillzError(error.ErrorCode, msg, error.IsRecoverable) { Details = error.Details, Code = error.Code };
        }
    }
}
