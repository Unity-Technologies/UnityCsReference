// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using uei = UnityEngine.Internal;

namespace UnityEngine.Apple.ReplayKit
{
    [NativeHeader("PlatformDependent/iPhonePlayer/IOSScriptBindings.h")]
    public static partial class ReplayKit
    {
        // please note that we call trampoline function directly
        // they return int (as we want to have c-compatible interface) while c# expects bool
        // and so the code generated looks like bool f() { return f_returning_int(); }
        // this is perfectly standard c++ (at least we believe so) and it saves us from writing stupid glue code

        // TODO: at the time of writing there was a bug with attributes on properties: they were not "propagated",
        // thats why we do NativeConditional on both get/set and not on property itself

        extern public static bool APIAvailable
        {
            [NativeConditional("PLATFORM_IPHONE || PLATFORM_TVOS")]
            [FreeFunction("UnityReplayKitAPIAvailable")]
            get;
        }

        extern public static bool broadcastingAPIAvailable
        {
            [NativeConditional("PLATFORM_IPHONE || PLATFORM_TVOS")]
            [FreeFunction("UnityReplayKitBroadcastingAPIAvailable")]
            get;
        }

        extern public static bool recordingAvailable
        {
            [NativeConditional("PLATFORM_IPHONE || PLATFORM_TVOS")]
            [FreeFunction("UnityReplayKitRecordingAvailable")]
            get;
        }

        extern public static bool isRecording
        {
            [NativeConditional("PLATFORM_IPHONE || PLATFORM_TVOS")]
            [FreeFunction("UnityReplayKitIsRecording")]
            get;
        }

        extern public static bool isBroadcasting
        {
            [NativeConditional("PLATFORM_IPHONE || PLATFORM_TVOS")]
            [FreeFunction("UnityReplayKitIsBroadcasting")]
            get;
        }

        extern public static bool cameraEnabled
        {
            [NativeConditional("PLATFORM_IPHONE")]
            [FreeFunction("UnityReplayKitIsCameraEnabled")]
            get;

            [NativeConditional("PLATFORM_IPHONE")]
            [FreeFunction("UnityReplayKitSetCameraEnabled")]
            set;
        }
        extern public static bool microphoneEnabled
        {
            [NativeConditional("PLATFORM_IPHONE")]
            [FreeFunction("UnityReplayKitIsMicrophoneEnabled")]
            get;

            [NativeConditional("PLATFORM_IPHONE")]
            [FreeFunction("UnityReplayKitSetMicrophoneEnabled")]
            set;
        }

        extern public static string broadcastURL
        {
            [NativeConditional("PLATFORM_IPHONE || PLATFORM_TVOS")]
            [FreeFunction("UnityReplayKitGetBroadcastURL")]
            get;
        }

        extern public static string lastError
        {
            [NativeConditional("PLATFORM_IPHONE || PLATFORM_TVOS")]
            [FreeFunction("UnityReplayKitLastError")]
            get;
        }

        [NativeConditional("PLATFORM_IPHONE || PLATFORM_TVOS")]
        [FreeFunction("ReplayKitScripting::StartRecording")]
        extern private static bool StartRecordingImpl(bool enableMicrophone, bool enableCamera);

        public delegate void BroadcastStatusCallback(bool hasStarted, string errorMessage);

        [NativeConditional("PLATFORM_IPHONE || PLATFORM_TVOS")]
        [FreeFunction("ReplayKitScripting::StartBroadcasting")]
        extern private static void StartBroadcastingImpl(BroadcastStatusCallback callback, bool enableMicrophone, bool enableCamera);

        // we cannot have default args in public api yet (because we still support js that doesnt have it)
        // public static bool StartRecording(bool enableMicrophone = false, bool enableCamera = false);
        // public static bool StartBroadcasting(BroadcastStatusCallback callback, bool enableMicrophone = false, bool enableCamera = false);
        public static bool StartRecording([uei.DefaultValue("false")] bool enableMicrophone, [uei.DefaultValue("false")] bool enableCamera)
        {
            return StartRecordingImpl(enableMicrophone, enableCamera);
        }

        public static bool StartRecording(bool enableMicrophone)
        {
            return StartRecording(enableMicrophone, false);
        }

        public static bool StartRecording()
        {
            return StartRecording(false, false);
        }

        public static void StartBroadcasting(BroadcastStatusCallback callback, [uei.DefaultValue("false")] bool enableMicrophone, [uei.DefaultValue("false")] bool enableCamera)
        {
            StartBroadcastingImpl(callback, enableMicrophone, enableCamera);
        }

        public static void StartBroadcasting(BroadcastStatusCallback callback, bool enableMicrophone)
        {
            StartBroadcasting(callback, enableMicrophone, false);
        }

        public static void StartBroadcasting(BroadcastStatusCallback callback)
        {
            StartBroadcasting(callback, false, false);
        }


        [NativeConditional("PLATFORM_IPHONE || PLATFORM_TVOS")]
        [FreeFunction("UnityReplayKitStopRecording")]
        extern public static bool StopRecording();

        [NativeConditional("PLATFORM_IPHONE || PLATFORM_TVOS")]
        [FreeFunction("UnityReplayKitStopBroadcasting")]
        extern public static void StopBroadcasting();

        [NativeConditional("PLATFORM_IPHONE || PLATFORM_TVOS")]
        [FreeFunction("UnityReplayKitPreview")]
        extern public static bool Preview();

        [NativeConditional("PLATFORM_IPHONE || PLATFORM_TVOS")]
        [FreeFunction("UnityReplayKitDiscard")]
        extern public static bool Discard();

        [NativeConditional("PLATFORM_IPHONE || PLATFORM_TVOS")]
        [FreeFunction("UnityReplayKitShowCameraPreviewAt")]
        extern public static bool ShowCameraPreviewAt(float posX, float posY);

        [NativeConditional("PLATFORM_IPHONE || PLATFORM_TVOS")]
        [FreeFunction("UnityReplayKitHideCameraPreview")]
        extern public static void HideCameraPreview();
    }
}
