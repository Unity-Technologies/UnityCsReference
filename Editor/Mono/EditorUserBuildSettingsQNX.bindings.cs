// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/EditorUserBuildSettings.h")]
    public enum QNXOsVersion
    {
        [UnityEngine.InspectorName("Neutrino RTOS 7.1")]
        Neutrino71 = 1,
        [UnityEngine.InspectorName("Neutrino RTOS 8.0")]
        Neutrino80 = 2,
    }

    /// <summary>
    /// The network stack to build a QNX 7.1 player against.
    /// </summary>
    /// <remarks>
    /// QNX 7.1 provides two network stacks, and Unity builds a separate player variation for each one.
    /// QNX 8.0 provides only io-sock, so it ignores this setting.
    ///
    /// <see cref="QNX71NetworkStack.IoSock"/> requires a QNX 7.1 SDK that provides the io-sock headers.
    /// A build against an SDK without them fails and names the SDK path in the error.
    /// </remarks>
    public enum QNX71NetworkStack
    {
        /// <summary>
        /// Uses the io-pkt network stack, which is the default on QNX 7.1.
        /// </summary>
        [UnityEngine.InspectorName("io-pkt (QNX 7.1 default)")]
        IoPkt = 0,

        /// <summary>
        /// Uses the io-sock network stack, which requires an io-sock enabled QNX 7.1 SDK.
        /// </summary>
        [UnityEngine.InspectorName("io-sock")]
        IoSock = 1,
    }
}
