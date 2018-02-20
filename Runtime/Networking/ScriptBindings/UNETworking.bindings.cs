// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngineInternal;
using UnityEngine.Networking.Types;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEngine.Networking
{

    [NativeHeader("Runtime/Networking/UNETManager.h")]
    [NativeHeader("Runtime/Networking/UNetTypes.h")]
    [NativeHeader("Runtime/Networking/UNETConfiguration.h")]
    [NativeConditional("ENABLE_NETWORK && ENABLE_UNET", true)]
    public sealed partial class NetworkTransport
    {
        static public void Init(GlobalConfig config)
        {
            if (config.NetworkEventAvailable != null)
                SetNetworkEventAvailableCallback(config.NetworkEventAvailable);
            if (config.ConnectionReadyForSend != null)
                SetConnectionReadyForSendCallback(config.ConnectionReadyForSend);
            InitWithParameters(new GlobalConfigInternal(config));
        }

        [NativeThrows]
        [FreeFunction("UNETManager::SetNetworkEventAvailableCallback")]
        extern static void SetNetworkEventAvailableCallback(Action<int> callback);

        [NativeThrows]
        [FreeFunction("UNETManager::SetConnectionReadyForSendCallback")]
        extern static void SetConnectionReadyForSendCallback(Action<int, int> callback);

        [FreeFunction("UNETManager::Get()->NotifyWhenConnectionReadyForSend")]
        extern public static bool NotifyWhenConnectionReadyForSend(int hostId, int connectionId, int notificationLevel, out byte error);

        [FreeFunction("UNETManager::Get()->GetHostPort")]
        extern public static int GetHostPort(int hostId);
    }

    internal sealed partial class ConnectionConfigInternal : IDisposable
    {
        [NativeMethod("MakeChannelsSharedOrder")]
        private extern bool MakeChannelsSharedOrder(byte[] values);
    }

}
