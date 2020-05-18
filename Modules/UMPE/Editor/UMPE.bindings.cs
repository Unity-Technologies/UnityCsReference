// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.MPE
{
    [MovedFrom("Unity.MPE")]
    public enum ProcessEvent // Keep in sync with ProcessService.h
    {
        [Obsolete("... (UnityUpgradable) -> Undefined")]
        UMP_EVENT_UNDEFINED = 0,
        Undefined = 0,

        [Obsolete("... (UnityUpgradable) -> Create")]
        UMP_EVENT_CREATE = 1,
        Create = 1,

        [Obsolete("... (UnityUpgradable) -> Initialize")]
        UMP_EVENT_INITIALIZE = 2,
        Initialize = 2,

        [Obsolete("... (UnityUpgradable) -> AfterDomainReload")]
        UMP_EVENT_AFTER_DOMAIN_RELOAD = 3,
        AfterDomainReload = 3,

        [Obsolete("... (UnityUpgradable) -> Shutdown")]
        UMP_EVENT_SHUTDOWN = 4,
        Shutdown = 4
    }

    [MovedFrom("Unity.MPE")]
    public enum ProcessLevel // Keep in sync with ProcessService.h
    {
        [Obsolete("... (UnityUpgradable) -> Undefined")]
        UMP_UNDEFINED = 0,
        Undefined = 0,

        [Obsolete("... (UnityUpgradable) -> Master")]
        UMP_MASTER = 1,
        Master = 1,

        [Obsolete("... (UnityUpgradable) -> Slave")]
        UMP_SLAVE = 2,
        Slave = 2
    }

    [MovedFrom("Unity.MPE")]
    public enum ProcessState // Keep in sync with ProcessService.h
    {
        [Obsolete("... (UnityUpgradable) -> UnknownProcess")]
        UMP_UNKNOWN_PROCESS = 0,
        UnknownProcess = 0,
        [Obsolete("... (UnityUpgradable) -> FinishedSuccessfully")]
        UMP_FINISHED_SUCCESSFULLY = 1,
        FinishedSuccessfully = 1,
        [Obsolete("... (UnityUpgradable) -> FinishedWithError")]
        UMP_FINISHED_WITH_ERROR = 2,
        FinishedWithError = 2,
        [Obsolete("... (UnityUpgradable) -> Running")]
        UMP_RUNNING = 3,
        Running = 3
    }

    [MovedFrom("Unity.MPE")]
    [Flags]
    internal enum RoleCapability
    {
        None = 0,
        Master = 1,
        Slave = 2
    }

    [MovedFrom("Unity.MPE")]
    [NativeType("Modules/UMPE/ChannelService.h")]
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct ChannelInfo : IEquatable<ChannelInfo>
    {
        public static ChannelInfo invalidChannel = new ChannelInfo()
        {
            m_ChannelId = -1
        };

        [NativeName("channelName")]
        string m_ChannelName;

        [NativeName("channelId")]
        int m_ChannelId;

        public string name => m_ChannelName;

        public int id => m_ChannelId;

        public bool Equals(ChannelInfo obj)
        {
            return this == obj;
        }

        public override bool Equals(System.Object obj)
        {
            return obj is ChannelInfo info && this == info;
        }

        public override int GetHashCode()
        {
            return name.GetHashCode() ^ id.GetHashCode();
        }

        public static bool operator==(ChannelInfo x, ChannelInfo y)
        {
            return x.id == y.id && x.name == y.name;
        }

        public static bool operator!=(ChannelInfo x, ChannelInfo y)
        {
            return !(x == y);
        }
    }

    [MovedFrom("Unity.MPE")]
    [NativeType("Modules/UMPE/ChannelService.h")]
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct ChannelClientInfo : IEquatable<ChannelClientInfo>
    {
        public static ChannelClientInfo invalidClient = new ChannelClientInfo()
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

        public string name => m_ChannelName;

        public int clientId => m_ChannelClientId;

        public int connectionId => m_ConnectionId;

        public bool Equals(ChannelClientInfo obj)
        {
            return this == obj;
        }

        public override bool Equals(System.Object obj)
        {
            return obj is ChannelClientInfo info && Equals(info);
        }

        public override int GetHashCode()
        {
            return name.GetHashCode() ^ clientId.GetHashCode() ^ connectionId.GetHashCode();
        }

        public static bool operator==(ChannelClientInfo x, ChannelClientInfo y)
        {
            return x.name == y.name && x.clientId == y.clientId && x.connectionId == y.connectionId;
        }

        public static bool operator!=(ChannelClientInfo x, ChannelClientInfo y)
        {
            return !(x == y);
        }
    }

    [NativeHeader("Modules/UMPE/ChannelService.h"),
     StaticAccessor("Unity::MPE::ChannelService", StaticAccessorType.DoubleColon)]
    public static partial class ChannelService
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
    public partial class ChannelClient
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

    [MovedFrom("Unity.MPE")]
    [NativeHeader("Modules/UMPE/ProcessService.h"),
     StaticAccessor("Unity::MPE::ProcessService", StaticAccessorType.DoubleColon)]
    public class ProcessService
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

        public static event Action<int, ProcessState> SlaveProcessExitedEvent;

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
