// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;

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
    };

    [Flags]
    internal enum RoleCapability
    {
        UMP_CAP_NIL = 0,
        UMP_CAP_MASTER = 1,
        UMP_CAP_SLAVE = 2
    };

    [NativeHeader("Modules/UMPE/EventService.h"),
     StaticAccessor("Unity::MPE::EventService", StaticAccessorType.DoubleColon)]
    internal partial class EventService
    {
        public static extern int ConnectionId { get; }
        public static extern int NewRequestId();
        public static extern void Send(string requestStr);
    }

    [NativeHeader("Modules/UMPE/ProcessService.h"),
     StaticAccessor("Unity::MPE::ProcessService", StaticAccessorType.DoubleColon)]
    internal class ProcessService
    {
        public static extern ProcessLevel level { get; }
        public static extern string roleName { get; }

        public static extern string ReadParameter(string paramName);
        public static extern void LaunchSlave(string roleName, params string[] keyValuePairs);

        public static extern void ApplyPropertyModifications(PropertyModification[] modifications);
        public static extern byte[] SerializeObject(int instanceId);
        public static extern UnityEngine.Object DeserializeObject(byte[] bytes);
    }

    [NativeHeader("Modules/UMPE/EventService.h"),
     StaticAccessor("Unity::MPE::TestClient", StaticAccessorType.DoubleColon)]
    internal partial class TestClient
    {
        public static extern void Start();
        public static extern void Stop();
        public static extern void Request(string eventType, string payload);
        public static extern int ConnectionId { get; }
        public static bool IsConnected => ConnectionId != -1;
    }
}
