// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.XR.WSA
{
    [NativeHeader("Modules/VR/HoloLens/PerceptionRemoting.h")]
    public enum HolographicStreamerConnectionState
    {
        Disconnected,
        Connecting,
        Connected
    }

    [NativeHeader("Modules/VR/HoloLens/PerceptionRemoting.h")]
    public enum HolographicStreamerConnectionFailureReason
    {
        None,
        Unknown,
        NoServerCertificate,
        HandshakePortBusy,
        HandshakeUnreachable,
        HandshakeConnectionFailed,
        AuthenticationFailed,
        RemotingVersionMismatch,
        IncompatibleTransportProtocols,
        HandshakeFailed,
        TransportPortBusy,
        TransportUnreachable,
        TransportConnectionFailed,
        ProtocolVersionMismatch,
        ProtocolError,
        VideoCodecNotAvailable,
        Canceled,
        ConnectionLost,
        DeviceLost,
        DisconnectRequest
    }

    [NativeHeader("Modules/VR/HoloLens/PerceptionRemoting.h")]
    internal enum EmulationMode
    {
        None,
        RemoteDevice,
        Simulated
    };

    [NativeHeader("Modules/VR/HoloLens/PerceptionRemoting.h")]
    public enum RemoteDeviceVersion
    {
        V1,
        V2
    }

    [NativeHeader("Modules/VR/HoloLens/HolographicEmulation/HolographicEmulationManager.h")]
    [StaticAccessor("HolographicEmulation::HolographicEmulationManager::Get()", StaticAccessorType.Dot)]
    internal partial class HolographicEmulationHelper
    {
        [NativeName("GetEmulationMode")]
        [NativeConditional("ENABLE_HOLOLENS_MODULE", StubReturnStatement = "HolographicEmulation::EmulationMode_None")]
        internal static extern EmulationMode GetEmulationMode();
    }

    [NativeHeader("Modules/VR/HoloLens/PerceptionRemoting.h")]
    [NativeConditional("ENABLE_HOLOLENS_MODULE")]
    internal partial class PerceptionRemoting
    {
        internal static extern void SetRemoteDeviceVersion(RemoteDeviceVersion remoteDeviceVersion);

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

namespace UnityEngineInternal.XR.WSA
{
    [NativeHeader("Modules/VR/HoloLens/PerceptionRemoting.h")]
    [NativeConditional("ENABLE_HOLOLENS_MODULE")]
    public partial class RemoteSpeechAccess
    {
        [System.Obsolete("Support for built-in VR will be removed in Unity 2020.1. Please update to the new Unity XR Plugin System. More information about the new XR Plugin System can be found at https://docs.unity3d.com/2019.3/Documentation/Manual/XR.html.", false)]
        public static extern void EnableRemoteSpeech(UnityEngine.XR.WSA.RemoteDeviceVersion remoteDeviceVersion);
        [System.Obsolete("Support for built-in VR will be removed in Unity 2020.1. Please update to the new Unity XR Plugin System. More information about the new XR Plugin System can be found at https://docs.unity3d.com/2019.3/Documentation/Manual/XR.html.", false)]
        public static extern void DisableRemoteSpeech();
    }
}
