// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.XR
{
    [StructLayout(LayoutKind.Sequential)]
    [NativeConditional("ENABLE_VR")]
    public struct HapticCapabilities : IEquatable<HapticCapabilities>
    {
        uint m_NumChannels;
        bool m_SupportsImpulse;
        bool m_SupportsBuffer;
        uint m_BufferFrequencyHz;
        uint m_BufferMaxSize;

        public uint numChannels             { get { return m_NumChannels; }         internal set { m_NumChannels = value; } }
        public bool supportsImpulse         { get { return m_SupportsImpulse; }     internal set { m_SupportsImpulse = value; } }
        public bool supportsBuffer          { get { return m_SupportsBuffer; }      internal set { m_SupportsBuffer = value; } }
        public uint bufferFrequencyHz       { get { return m_BufferFrequencyHz; }   internal set { m_BufferFrequencyHz = value; } }
        public uint bufferMaxSize           { get { return m_BufferMaxSize; }       internal set { m_BufferMaxSize = value; } }

        public override bool Equals(object obj)
        {
            if (!(obj is HapticCapabilities))
                return false;

            return Equals((HapticCapabilities)obj);
        }

        public bool Equals(HapticCapabilities other)
        {
            return numChannels == other.numChannels &&
                supportsImpulse == other.supportsImpulse &&
                supportsBuffer == other.supportsBuffer &&
                bufferFrequencyHz == other.bufferFrequencyHz &&
                bufferMaxSize == other.bufferMaxSize;
        }

        public override int GetHashCode()
        {
            return numChannels.GetHashCode() ^
                (supportsImpulse.GetHashCode() << 1) ^
                (supportsBuffer.GetHashCode() >> 1) ^
                (bufferFrequencyHz.GetHashCode() << 2) ^
                (bufferMaxSize.GetHashCode() >> 2);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeConditional("ENABLE_VR")]
    public struct HapticState : IEquatable<HapticState>
    {
        uint m_SamplesQueued;
        uint m_SamplesAvailable;

        public uint samplesQueued { get { return m_SamplesQueued; } internal set { m_SamplesQueued = value; } }
        public uint samplesAvailable { get { return m_SamplesAvailable; } internal set { m_SamplesAvailable = value; } }

        public override bool Equals(object obj)
        {
            if (!(obj is HapticState))
                return false;

            return Equals((HapticState)obj);
        }

        public bool Equals(HapticState other)
        {
            return samplesQueued == other.samplesQueued &&
                samplesAvailable == other.samplesAvailable;
        }

        public override int GetHashCode()
        {
            return samplesQueued.GetHashCode() ^ (samplesAvailable.GetHashCode() << 2);
        }
    }

    [NativeHeader("Modules/XR/Subsystems/Input/Public/XRInputTrackingFacade.h")]
    [NativeConditional("ENABLE_VR")]
    [StaticAccessor("XRInputTrackingFacade::Get()", StaticAccessorType.Dot)]
    public partial class InputHaptic
    {
        [NativeConditional("ENABLE_VR")]
        bool SendImpulse(XRNode node, uint channel, float amplitude) { return SendImpulse(node, channel, amplitude, 1.0f); }

        [NativeConditional("ENABLE_VR")]
        [NativeMethod("SendHapticImpulse")]
        extern public static bool SendImpulse(XRNode node, uint channel, float amplitude, [UnityEngine.Internal.DefaultValue("1.0f")] float frequency);

        [NativeConditional("ENABLE_VR")]
        [NativeMethod("SendHapticBuffer")]
        extern public static bool SendBuffer(XRNode node, uint channel, byte[] buffer);

        [NativeConditional("ENABLE_VR", "false")]
        [NativeMethod("TryGetHapticCapabilities")]
        extern public static bool TryGetCapabilities(XRNode node, out HapticCapabilities capabilities);

        [NativeConditional("ENABLE_VR", "false")]
        [NativeMethod("TryGetHapticState")]
        extern public static bool TryGetState(XRNode node, out HapticState state);

        [NativeConditional("ENABLE_VR")]
        [NativeMethod("StopHaptics")]
        extern public static void Stop(XRNode node);
    }

    [NativeHeader("Modules/XR/Subsystems/Input/Public/XRInputTrackingFacade.h")]
    [NativeConditional("ENABLE_VR")]
    [StaticAccessor("XRInputTrackingFacade::Get()", StaticAccessorType.Dot)]
    public partial class InputTracking
    {
        [NativeConditional("ENABLE_VR", "Vector3f::zero")]
        extern public static Vector3 GetLocalPosition(XRNode node);

        [NativeConditional("ENABLE_VR", "Quaternionf::identity()")]
        extern public static Quaternion GetLocalRotation(XRNode node);

        [NativeConditional("ENABLE_VR")]
        extern public static void Recenter();

        [NativeConditional("ENABLE_VR")]
        extern public static string GetNodeName(ulong uniqueId);

        public static void GetNodeStates(List<XRNodeState> nodeStates)
        {
            if (null == nodeStates)
            {
                throw new ArgumentNullException("nodeStates");
            }

            nodeStates.Clear();
            GetNodeStates_Internal(nodeStates);
        }

        [NativeConditional("ENABLE_VR && !ENABLE_DOTNET")]
        extern private static void GetNodeStates_Internal(List<XRNodeState> nodeStates);

        [NativeConditional("ENABLE_VR && ENABLE_DOTNET")]
        extern private static XRNodeState[] GetNodeStates_Internal_WinRT();

        [NativeConditional("ENABLE_VR")]
        extern public static bool disablePositionalTracking
        {
            [NativeName("GetPositionalTrackingDisabled")]
            get;
            [NativeName("SetPositionalTrackingDisabled")]
            set;
        }
    }
}
