// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.XR.WSA
{
    public partial class HolographicRemoting
    {
        public static HolographicStreamerConnectionState ConnectionState
        {
            get
            {
                return HolographicStreamerConnectionState.Disconnected;
            }
        }

        public static void Connect(string clientName, int maxBitRate = 9999)
        {
        }

        public static void Disconnect()
        {
        }
    }
}
