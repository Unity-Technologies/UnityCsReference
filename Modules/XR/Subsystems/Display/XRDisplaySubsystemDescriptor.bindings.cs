// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UsedByNativeCodeAttribute = UnityEngine.Scripting.UsedByNativeCodeAttribute;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine.XR
{
    public struct XRMirrorViewBlitMode
    {
        // *MUST* be in sync with the kUnityXRMirrorBlitDefault
        public const int Default = 0;
        // *MUST* be in sync with the kUnityXRMirrorBlitLeftEye
        public const int LeftEye = -1;
        // *MUST* be in sync with the kUnityXRMirrorBlitRightEye
        public const int RightEye = -2;
        // *MUST* be in sync with the kUnityXRMirrorBlitSideBySide
        public const int SideBySide = -3;
        // *MUST* be in sync with the kUnityXRMirrorBlitSideBySideOcclusionMesh
        public const int SideBySideOcclusionMesh = -4;
        // *MUST* be in sync with the kUnityXRMirrorBlitDistort
        public const int Distort = -5;
        // *MUST* be in sync with the kUnityXRMirrorBlitNone
        public const int None = -6;
    }

    [NativeHeader("Modules/XR/XRPrefix.h")]
    [NativeType(Header = "Modules/XR/Subsystems/Display/XRDisplaySubsystemDescriptor.h")]
    [StructLayout(LayoutKind.Sequential)]
    public struct XRMirrorViewBlitModeDesc
    {
        public int blitMode;
        public String blitModeDesc;
    }

    [NativeType(Header = "Modules/XR/Subsystems/Display/XRDisplaySubsystemDescriptor.h")]
    [UsedByNativeCode]
    public class XRDisplaySubsystemDescriptor : IntegratedSubsystemDescriptor<XRDisplaySubsystem>
    {
        [NativeConditional("ENABLE_XR")]
        public extern bool disablesLegacyVr { get; }

        [NativeConditional("ENABLE_XR")]
        public extern bool enableBackBufferMSAA { get; }

        [NativeConditional("ENABLE_XR")]
        [NativeMethod("TryGetAvailableMirrorModeCount")]
        extern public int GetAvailableMirrorBlitModeCount();

        [NativeConditional("ENABLE_XR")]
        [NativeMethod("TryGetMirrorModeByIndex")]
        extern public void GetMirrorBlitModeByIndex(int index, out XRMirrorViewBlitModeDesc mode);
    }
}
