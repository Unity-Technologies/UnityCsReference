// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine;
using UnityEngine.XR.WSA;

namespace UnityEngine.XR.WSA
{
    [NativeHeader("Modules/VR/HoloLens/HolographicEmulation/HolographicEmulationManager.h")]
    internal enum PlaymodeInputType
    {
        LeftHand,
        RightHand,
        LeftController,
        RightController,
    }

    [NativeHeader("Modules/VR/HoloLens/HolographicEmulation/HolographicEmulationManager.h")]
    internal enum Handedness
    {
        Unknown,
        Left,
        Right,
    }

    [NativeHeader("Modules/VR/HoloLens/HolographicEmulation/HolographicEmulationManager.h")]
    internal enum SimulatedGesture
    {
        FingerPressed,
        FingerReleased
    }

    [NativeHeader("Modules/VR/HoloLens/HolographicEmulation/HolographicEmulationManager.h")]
    internal enum SimulatedControllerPress
    {
        PressButton,
        ReleaseButton,
        Grip,
        TouchPadPress,
        Select,
        TouchPadTouch,
        ThumbStick,
    }

    [NativeHeader("Modules/VR/HoloLens/HolographicEmulation/HolographicEmulationManager.h")]
    [StaticAccessor("HolographicEmulation::HolographicEmulationManager::Get()", StaticAccessorType.Dot)]
    [NativeConditional("ENABLE_HOLOLENS_MODULE")]
    internal partial class HolographicAutomation
    {
        [NativeThrows]
        internal static extern void Initialize();

        internal static extern void Shutdown();

        [NativeThrows]
        internal static extern void LoadRoom(string id);

        internal static extern void SetEmulationMode(EmulationMode mode);

        internal static extern void SetPlaymodeInputType(PlaymodeInputType inputType);

        [NativeName("ResetEmulationState")]
        internal static extern void Reset();

        internal static extern void PerformGesture(Handedness hand, SimulatedGesture gesture);

        internal static extern void PerformButtonPress(Handedness hand, SimulatedControllerPress buttonPress);

        [NativeConditional("ENABLE_HOLOLENS_MODULE", StubReturnStatement = "Vector3f::zero")]
        internal static extern Vector3 GetBodyPosition();

        internal static extern void SetBodyPosition(Vector3 position);

        internal static extern float GetBodyRotation();

        internal static extern void SetBodyRotation(float degrees);

        internal static extern float GetBodyHeight();

        internal static extern void SetBodyHeight(float degrees);

        [NativeConditional("ENABLE_HOLOLENS_MODULE", StubReturnStatement = "Vector3f::zero")]
        internal static extern Vector3 GetHeadRotation();

        internal static extern void SetHeadRotation(Vector3 degrees);

        internal static extern float GetHeadDiameter();

        internal static extern void SetHeadDiameter(float degrees);

        [NativeConditional("ENABLE_HOLOLENS_MODULE", StubReturnStatement = "Vector3f::zero")]
        internal static extern Vector3 GetHandPosition(Handedness hand);

        internal static extern void SetHandPosition(Handedness hand, Vector3 position);

        [NativeConditional("ENABLE_HOLOLENS_MODULE", StubReturnStatement = "Quaternionf::identity()")]
        internal static extern Quaternion GetHandOrientation(Handedness hand);

        internal static extern bool TrySetHandOrientation(Handedness hand, Quaternion orientation);

        internal static extern bool GetHandActivated(Handedness hand);

        internal static extern void SetHandActivated(Handedness hand, bool activated);

        internal static extern bool GetHandVisible(Handedness hand);

        internal static extern void EnsureHandVisible(Handedness hand);

        [NativeConditional("ENABLE_HOLOLENS_MODULE", StubReturnStatement = "Vector3f::zero")]
        internal static extern Vector3 GetControllerPosition(Handedness hand);

        internal static extern bool TrySetControllerPosition(Handedness hand, Vector3 position);

        internal static extern bool GetControllerActivated(Handedness hand);

        internal static extern bool TrySetControllerActivated(Handedness hand, bool activated);

        internal static extern bool GetControllerVisible(Handedness hand);

        internal static extern bool TryEnsureControllerVisible(Handedness hand);
    }
}
