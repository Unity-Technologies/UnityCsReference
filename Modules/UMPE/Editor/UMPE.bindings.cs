// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace Unity.MPE
{
    internal enum ProcessEvent // Keep in sync with ProcessService.h
    {
        UMP_EVENT_UNDEFINED = -1,
        UMP_EVENT_CREATE = 1,
        UMP_EVENT_INITIALIZE = 2,

        UMP_EVENT_AFTER_DOMAIN_RELOAD,

        UMP_EVENT_SHUTDOWN,
    }

    internal enum ProcessLevel // Keep in sync with ProcessService.h
    {
        UMP_UNDEFINED,
        UMP_MASTER,
        UMP_SLAVE
    }

    internal enum ProcessState // Keep in sync with ProcessService.h
    {
        UMP_UNKNOWN_PROCESS,
        UMP_FINISHED_SUCCESSFULLY,
        UMP_FINISHED_WITH_ERROR,
        UMP_RUNNING
    }

    [Flags]
    internal enum RoleCapability
    {
        UMP_CAP_NIL = 0,
        UMP_CAP_MASTER = 1,
        UMP_CAP_SLAVE = 2
    }

    [NativeType("Modules/UMPE/ChannelService.h")]
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    internal struct ChannelInfo
    {
        public static ChannelInfo InvalidChannel = new ChannelInfo()
        {
            m_ChannelId = -1
        };

        [NativeName("channelName")]
        string m_ChannelName;

        [NativeName("channelId")]
        int m_ChannelId;

        public string channelName => m_ChannelName;

        public int channelId => m_ChannelId;

        public override bool Equals(System.Object obj)
        {
            return obj is ChannelInfo && this == (ChannelInfo)obj;
        }

        public override int GetHashCode()
        {
            return channelName.GetHashCode() ^ channelId.GetHashCode();
        }

        public static bool operator==(ChannelInfo x, ChannelInfo y)
        {
            return x.channelId == y.channelId && x.channelName == y.channelName;
        }

        public static bool operator!=(ChannelInfo x, ChannelInfo y)
        {
            return !(x == y);
        }
    }

    [NativeType("Modules/UMPE/ChannelService.h")]
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    internal struct ChannelClientInfo
    {
        public static ChannelClientInfo InvalidClient = new ChannelClientInfo()
        {
            m_ChannelClientId = -1,
            m_ConnectionId = -1
        };

        [NativeName("channelName")]
        string m_ChannelName;

        [NativeName("channelClientId")]
        int m_ChannelClientId;

        [NativeName("connectionId")]
        int m_ConnectionId;

        public string channelName => m_ChannelName;

        public int channelClientId => m_ChannelClientId;

        public int connectionId => m_ConnectionId;

        public override bool Equals(System.Object obj)
        {
            return obj is ChannelClientInfo && this == (ChannelClientInfo)obj;
        }

        public override int GetHashCode()
        {
            return channelName.GetHashCode() ^ channelClientId.GetHashCode() ^ connectionId.GetHashCode();
        }

        public static bool operator==(ChannelClientInfo x, ChannelClientInfo y)
        {
            return x.channelName == y.channelName && x.channelClientId == y.channelClientId && x.connectionId == y.connectionId;
        }

        public static bool operator!=(ChannelClientInfo x, ChannelClientInfo y)
        {
            return !(x == y);
        }
    }

    [NativeHeader("Modules/UMPE/ChannelService.h"),
     StaticAccessor("Unity::MPE::ChannelService", StaticAccessorType.DoubleColon)]
    internal static partial class ChannelService
    {
        public static extern string GetAddress();
        public static extern int GetPort();
        public static extern void Start();
        public static extern void Stop();
        public static extern bool IsRunning();
        public static extern ChannelInfo[] GetChannelList();
        public static extern ChannelClientInfo[] GetChannelClientList();
        public static extern void Broadcast(int channelId, string data);
        public static extern void BroadcastBinary(int channelId, byte[] data);
        public static extern void Send(int connectionId, string data);
        internal static extern void SendBinary(int connectionId, byte[] data);
        public static extern int ChannelNameToId(string channelName);
        internal static extern int Internal_GetOrCreateChannel(string channelName);
        internal static extern void Internal_CloseChannel(string channelName);
    }

    [NativeHeader("Modules/UMPE/ChannelService.h"),
     StaticAccessor("Unity::MPE::ChannelClient", StaticAccessorType.DoubleColon)]
    internal partial class ChannelClient
    {
        internal static extern void Internal_Shutdown();
        internal static extern void Start(int clientId, bool autoTick);
        internal static extern void Stop(int clientId);
        internal static extern void Tick(int clientId);
        internal static extern bool IsConnected(int clientId);
        internal static extern void Send(int clientId, string data);
        internal static extern void SendBinary(int clientId, byte[] data);
        internal static extern int Internal_GetOrCreateClient(string channelName);
        internal static extern void Internal_CloseClient(string channelName);
        public static extern int NewRequestId(int clientId);
        public static extern ChannelClientInfo GetChannelClientInfo(int clientId);
        public static extern ChannelClientInfo[] GetChannelClientList();
    }

    [NativeHeader("Modules/UMPE/ProcessService.h"),
     StaticAccessor("Unity::MPE::ProcessService", StaticAccessorType.DoubleColon)]
    internal class ProcessService
    {
        public static extern ProcessLevel level { get; }
        public static extern string roleName { get; }
        public static extern bool IsChannelServiceStarted();
        public static extern string ReadParameter(string paramName);
        public static extern int LaunchSlave(string roleName, params string[] keyValuePairs);
        public static extern void TerminateSlave(int pid);
        public static extern ProcessState GetSlaveProcessState(int pid);
        public static extern bool HasCapability(string capName);
        public static extern void ApplyPropertyModifications(PropertyModification[] modifications);
        public static extern byte[] SerializeObject(int instanceId);
        public static extern UnityEngine.Object DeserializeObject(byte[] bytes);
        public static extern int EnableProfileConnection(string dataPath);
        public static extern void DisableProfileConnection();

        public delegate void SlaveProcessExitedHandler(int pid, ProcessState newState);
        public static event SlaveProcessExitedHandler SlaveProcessExitedEvent;

        [RequiredByNativeCode]
        private static void OnSlaveProcessExited(int pid, ProcessState newState)
        {
            SlaveProcessExitedEvent?.Invoke(pid, newState);
        }
    }

    [NativeHeader("Modules/UMPE/TestClient.h"),
     StaticAccessor("Unity::MPE::TestClient", StaticAccessorType.DoubleColon)]
    internal partial class TestClient
    {
        public static extern void Start();
        public static extern void Stop();
        public static extern void Emit(string eventType, string payload);
        public static extern void Request(string eventType, string payload);
        public static extern int ConnectionId { get; }
        public static bool IsConnected => ConnectionId != -1;
    }
}
