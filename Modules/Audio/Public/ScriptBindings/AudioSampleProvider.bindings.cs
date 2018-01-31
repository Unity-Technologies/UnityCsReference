// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Scripting;

[assembly: InternalsVisibleTo("UnityEngine.VideoModule")]

namespace UnityEngine.Experimental.Audio
{
    [NativeType(Header = "Modules/Audio/Public/ScriptBindings/AudioSampleProvider.bindings.h")]
    [StaticAccessor("AudioSampleProviderBindings", StaticAccessorType.DoubleColon)]
    public class AudioSampleProvider : IDisposable
    {
        [VisibleToOtherModules]
        static internal AudioSampleProvider Lookup(
            uint providerId, Object ownerObj, ushort trackIndex)
        {
            AudioSampleProvider provider = InternalGetScriptingPtr(providerId);
            if (provider != null)
                return provider;

            return new AudioSampleProvider(providerId, ownerObj, trackIndex);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate uint ConsumeSampleFramesNativeFunction(
            uint providerId, IntPtr interleavedSampleFrames, uint sampleFrameCount);

        private ConsumeSampleFramesNativeFunction m_ConsumeSampleFramesNativeFunction;

        private AudioSampleProvider(uint providerId, Object ownerObj, ushort trackIdx)
        {
            owner = ownerObj;
            id = providerId;
            trackIndex = trackIdx;
            m_ConsumeSampleFramesNativeFunction = (ConsumeSampleFramesNativeFunction)
                Marshal.GetDelegateForFunctionPointer(
                    InternalGetConsumeSampleFramesNativeFunctionPtr(),
                    typeof(ConsumeSampleFramesNativeFunction));
            ushort chCount = 0;
            uint sRate = 0;
            InternalGetFormatInfo(providerId, out chCount, out sRate);
            channelCount = chCount;
            sampleRate = sRate;
            InternalSetScriptingPtr(providerId, this);
        }

        ~AudioSampleProvider()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (id != 0)
            {
                InternalSetScriptingPtr(id, null);
                id = 0;
            }
            GC.SuppressFinalize(this);
        }

        public uint id { get; private set; }

        public ushort trackIndex { get; private set; }

        public Object owner { get; private set; }

        public bool valid { get { return InternalIsValid(id); } }

        public ushort channelCount { get; private set; }

        public uint sampleRate { get; private set; }

        public uint maxSampleFrameCount
        { get { return InternalGetMaxSampleFrameCount(id); } }

        public uint availableSampleFrameCount
        { get { return InternalGetAvailableSampleFrameCount(id); } }

        public uint freeSampleFrameCount
        { get { return InternalGetFreeSampleFrameCount(id); } }

        public uint freeSampleFrameCountLowThreshold
        {
            get { return InternalGetFreeSampleFrameCountLowThreshold(id); }
            set { InternalSetFreeSampleFrameCountLowThreshold(id, value); }
        }

        public bool enableSampleFramesAvailableEvents
        {
            get { return InternalGetEnableSampleFramesAvailableEvents(id); }
            set { InternalSetEnableSampleFramesAvailableEvents(id, value); }
        }

        public bool enableSilencePadding
        {
            get { return InternalGetEnableSilencePadding(id); }
            set { InternalSetEnableSilencePadding(id, value); }
        }

        unsafe public uint ConsumeSampleFrames(NativeArray<float> sampleFrames)
        {
            if (channelCount == 0)
                return 0;

            return m_ConsumeSampleFramesNativeFunction(
                id, (IntPtr)sampleFrames.GetUnsafePtr(), (uint)sampleFrames.Length / channelCount);
        }

        public static ConsumeSampleFramesNativeFunction consumeSampleFramesNativeFunction
        {
            get
            {
                return (ConsumeSampleFramesNativeFunction)Marshal.GetDelegateForFunctionPointer(
                    InternalGetConsumeSampleFramesNativeFunctionPtr(),
                    typeof(ConsumeSampleFramesNativeFunction));
            }
        }

        public delegate void SampleFramesHandler(
            AudioSampleProvider provider, uint sampleFrameCount);

        public event SampleFramesHandler sampleFramesAvailable;

        public event SampleFramesHandler sampleFramesOverflow;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void SampleFramesEventNativeFunction(
            IntPtr userData, uint providerId, uint sampleFrameCount);

        public void SetSampleFramesAvailableNativeHandler(
            SampleFramesEventNativeFunction handler, IntPtr userData)
        {
            InternalSetSampleFramesAvailableNativeHandler(
                id, Marshal.GetFunctionPointerForDelegate(handler), userData);
        }

        public void ClearSampleFramesAvailableNativeHandler()
        { InternalClearSampleFramesAvailableNativeHandler(id); }

        public void SetSampleFramesOverflowNativeHandler(
            SampleFramesEventNativeFunction handler, IntPtr userData)
        {
            InternalSetSampleFramesOverflowNativeHandler(
                id, Marshal.GetFunctionPointerForDelegate(handler), userData);
        }

        public void ClearSampleFramesOverflowNativeHandler()
        { InternalClearSampleFramesOverflowNativeHandler(id); }

        [RequiredByNativeCode]
        private void InvokeSampleFramesAvailable(int sampleFrameCount)
        {
            if (sampleFramesAvailable != null)
                // ScriptingInvication doesn't support uint, so we get int and cast.
                sampleFramesAvailable(this, (uint)sampleFrameCount);
        }

        [RequiredByNativeCode]
        private void InvokeSampleFramesOverflow(int droppedSampleFrameCount)
        {
            if (sampleFramesOverflow != null)
                sampleFramesOverflow(this, (uint)droppedSampleFrameCount);
        }

        private static extern void InternalGetFormatInfo(
            uint providerId, out ushort chCount, out uint sRate);

        private static extern AudioSampleProvider InternalGetScriptingPtr(uint providerId);

        // Can be done outside of the main thread so that garbage-collected provider can be
        // destoryed in any thread. The wanted constraint (must lookup from main thread) is enforced
        // by the fact that InternalGetScriptingPtr _must_ be called from the main thread.
        [NativeMethod(IsThreadSafe = true)]
        private static extern void InternalSetScriptingPtr(
            uint providerId, AudioSampleProvider provider);

        [NativeMethod(IsThreadSafe = true)]
        private static extern bool InternalIsValid(uint providerId);

        [NativeMethod(IsThreadSafe = true)]
        private static extern uint InternalGetMaxSampleFrameCount(uint providerId);

        [NativeMethod(IsThreadSafe = true)]
        private static extern uint InternalGetAvailableSampleFrameCount(uint providerId);

        [NativeMethod(IsThreadSafe = true)]
        private static extern uint InternalGetFreeSampleFrameCount(uint providerId);

        [NativeMethod(IsThreadSafe = true)]
        private static extern uint InternalGetFreeSampleFrameCountLowThreshold(uint providerId);

        [NativeMethod(IsThreadSafe = true)]
        private static extern void InternalSetFreeSampleFrameCountLowThreshold(
            uint providerId, uint sampleFrameCount);

        [NativeMethod(IsThreadSafe = true)]
        private static extern bool InternalGetEnableSampleFramesAvailableEvents(uint providerId);

        [NativeMethod(IsThreadSafe = true)]
        private static extern void InternalSetEnableSampleFramesAvailableEvents(
            uint providerId, bool enable);

        private static extern void InternalSetSampleFramesAvailableNativeHandler(
            uint providerId, IntPtr handler, IntPtr userData);

        private static extern void InternalClearSampleFramesAvailableNativeHandler(uint providerId);

        private static extern void InternalSetSampleFramesOverflowNativeHandler(
            uint providerId, IntPtr handler, IntPtr userData);

        private static extern void InternalClearSampleFramesOverflowNativeHandler(uint providerId);

        [NativeMethod(IsThreadSafe = true)]
        private static extern bool InternalGetEnableSilencePadding(uint id);

        [NativeMethod(IsThreadSafe = true)]
        private static extern void InternalSetEnableSilencePadding(uint id, bool enabled);

        [NativeMethod(IsThreadSafe = true)]
        private static extern IntPtr InternalGetConsumeSampleFramesNativeFunctionPtr();
    }
}
