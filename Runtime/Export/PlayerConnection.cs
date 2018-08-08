// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Diagnostics
{
    public static class PlayerConnection
    {
        [Obsolete("Use UnityEngine.Networking.PlayerConnection.PlayerConnection.instance.isConnected instead.")]
        public static bool connected { get { return UnityEngine.Networking.PlayerConnection.PlayerConnection.instance.isConnected; } }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("PlayerConnection.SendFile is no longer supported.", true)]
        public static void SendFile(string remoteFilePath, byte[] data)
        {
        }
    }
}
