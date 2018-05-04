// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.XR.WSA
{
    [NativeHeader("Runtime/VR/HoloLens/PerceptionRemoting.h")]
    public enum HolographicStreamerConnectionState
    {
        Disconnected,
        Connecting,
        Connected
    }

    [NativeHeader("Runtime/VR/HoloLens/PerceptionRemoting.h")]
    public enum HolographicStreamerConnectionFailureReason
    {
        None,
        Unknown,
        Unreachable,
        HandshakeFailed,
        ProtocolVersionMismatch,
        ConnectionLost
    }

    [NativeHeader("Runtime/VR/HoloLens/PerceptionRemoting.h")]
    internal enum EmulationMode
    {
        None,
        RemoteDevice,
        Simulated
    };

    [NativeHeader("Runtime/VR/HoloLens/HolographicEmulation/HolographicEmulationManager.h")]
    [StaticAccessor("HolographicEmulation::HolographicEmulationManager::Get()", StaticAccessorType.Dot)]
    internal partial class HolographicEmulationHelper
    {
        [NativeName("GetEmulationMode")]
        [NativeConditional("ENABLE_HOLOLENS_MODULE", StubReturnStatement = "HolographicEmulation::EmulationMode_None")]
        internal static extern EmulationMode GetEmulationMode();
    }

    [NativeHeader("Runtime/VR/HoloLens/PerceptionRemoting.h")]
    [NativeConditional("ENABLE_HOLOLENS_MODULE")]
    internal partial class PerceptionRemoting
    {
        internal static extern void Connect(string clientName);

        internal static extern void Disconnect();

        [NativeConditional("ENABLE_HOLOLENS_MODULE", StubReturnStatement = "HolographicEmulation::None")]
        internal static extern HolographicStreamerConnectionFailureReason CheckForDisconnect();

        [NativeConditional("ENABLE_HOLOLENS_MODULE", StubReturnStatement = "HolographicEmulation::Disconnected")]
        internal static extern HolographicStreamerConnectionState GetConnectionState();

        internal static extern void SetEnableAudio(bool enable);

        internal static extern void SetEnableVideo(bool enable);

        internal static extern void SetVideoEncodingParameters(int maxBitRate);
    }
}
