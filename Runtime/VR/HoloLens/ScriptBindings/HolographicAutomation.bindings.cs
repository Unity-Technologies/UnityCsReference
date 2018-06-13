// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine;
using UnityEngine.XR.WSA;

namespace UnityEngine.XR.WSA
{
    [NativeHeader("Runtime/VR/HoloLens/HolographicEmulation/HolographicEmulationManager.h")]
    internal enum GestureHand
    {
        Left,
        Right
    };

    [NativeHeader("Runtime/VR/HoloLens/HolographicEmulation/HolographicEmulationManager.h")]
    internal enum SimulatedGesture
    {
        FingerPressed,
        FingerReleased
    }

    [NativeHeader("Runtime/VR/HoloLens/HolographicEmulation/HolographicEmulationManager.h")]
    [StaticAccessor("HolographicEmulation::HolographicEmulationManager::Get()", StaticAccessorType.Dot)]
    [NativeConditional("ENABLE_HOLOLENS_MODULE")]
    internal partial class HolographicAutomation
    {
        internal static extern void Initialize();

        internal static extern void Shutdown();

        internal static extern void LoadRoom(string id);

        internal static extern void SetEmulationMode(EmulationMode mode);

        internal static extern void SetGestureHand(GestureHand hand);

        [NativeName("ResetEmulationState")]
        internal static extern void Reset();

        internal static extern void PerformGesture(GestureHand hand, SimulatedGesture gesture);

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
        internal static extern Vector3 GetHandPosition(GestureHand hand);

        internal static extern void SetHandPosition(GestureHand hand, Vector3 position);

        internal static extern bool GetHandActivated(GestureHand hand);

        internal static extern void SetHandActivated(GestureHand hand, bool activated);

        internal static extern bool GetHandVisible(GestureHand hand);

        internal static extern void EnsureHandVisible(GestureHand hand);
    }
}
