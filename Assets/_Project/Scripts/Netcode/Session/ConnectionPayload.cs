using System;
using UnityEngine;

namespace Game.Netcode
{
    /// <summary>Sent by the client as NetworkConfig.ConnectionData; read by the host's approval callback.</summary>
    [Serializable]
    public class ConnectionPayload
    {
        public string BuildVersion;
        public string PlayerId;
        public string PlayerName;

        public byte[] ToBytes() => System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(this));

        public static ConnectionPayload FromBytes(byte[] bytes)
        {
            try
            {
                var json = System.Text.Encoding.UTF8.GetString(bytes);
                return JsonUtility.FromJson<ConnectionPayload>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}
