// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.UNode.Audio")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.UNode.Audio.Tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.Audio")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.Audio.Tests")]

namespace Unity.Experimental.Audio
{
    [StructLayout(LayoutKind.Sequential)]
    unsafe struct PortBuffer
    {
        public uint m_Channels;
        public SoundFormat m_Format;
        public float* m_Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct PortArray
    {
        public int PortCount { get { return (int)m_PortBufferCount; } }

        public NativeArray<float> GetPort(int index)
        {
            if (index < 0 || index >= m_PortBufferCount)
                throw new ArgumentException("Index out of range (GetPort)");

            var buffer = m_Buffers[index];
            var length = (int)(m_SampleCount * buffer.m_Channels);

            var portArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<float>(buffer.m_Buffer, length, Allocator.Invalid);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle<float>(ref portArray, m_Safety);

            return portArray;
        }

        public NativeArray<float> GetPortGeneric(int index)
        {
            return GetPort(index);
        }

        public float* GetPortUnsafe(int index)
        {
            return m_Buffers[index].m_Buffer;
        }

        public uint SampleCount { get { return m_SampleCount; } }

        public uint GetPortChannelCount(int index)
        {
            if (index < 0 || index >= m_PortBufferCount)
                throw new ArgumentException("Index out of range (GetPortLength)");

            return m_Buffers[index].m_Channels;
        }

        public SoundFormat GetPortFormat(int index)
        {
            if (index < 0 || index >= m_PortBufferCount)
                throw new ArgumentException("Index out of range (GetPortLength)");

            return m_Buffers[index].m_Format;
        }

        internal uint m_PortBufferCount;
        internal PortBuffer* m_Buffers;
        internal uint m_SampleCount;

        internal AtomicSafetyHandle m_Safety;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct DSPParameter
    {
        internal float m_Value;
        internal float* m_ValueBuffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct AudioData
    {
        public ulong m_DSPClock;
        public uint m_DSPBufferSize;
        public uint m_SampleRate;

        public uint m_SampleReadCount;

        public uint m_InputBuffersCount;
        public PortBuffer* m_InputBuffers;

        public uint m_OutputBuffersCount;
        public PortBuffer* m_OutputBuffers;

        public uint m_ParametersCount;
        public DSPParameter* m_Parameters;

        public AtomicSafetyHandle safety;
    }

    internal struct DSPInfo
    {
        public ulong DSPClock { get { return m_DSPClock; } }
        public uint DSPBufferSize { get { return m_DSPBufferSize; } }
        public uint SampleRate { get { return m_SampleRate; } }

        internal ulong m_DSPClock;
        internal uint m_DSPBufferSize;
        internal uint m_SampleRate;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct DSPParameterDescription
    {
        internal float m_Min;
        internal float m_Max;
        internal float m_DefaultValue;
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal class ParameterRangeAttribute : Attribute
    {
        internal float m_Min;
        internal float m_Max;

        public ParameterRangeAttribute(float min, float max)
        {
            this.m_Min = min;
            this.m_Max = max;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal class ParameterDefaultAttribute : Attribute
    {
        internal float defaultValue;

        public ParameterDefaultAttribute(float defaultVal)
        {
            this.defaultValue = defaultVal;
        }
    }

    internal unsafe struct ParameterData<P> where P : struct, IConvertible
    {
        public float GetFloat(P parameter, uint sampleOffset)
        {
            return GetFloat(Convert.ToInt32(parameter), sampleOffset);
        }

        public float GetFloat(int index, uint sampleOffset)
        {
            if (index >= m_ParametersCount)
                throw new ArgumentException("Undefined parameter in ParameterData.GetValue");

            if (m_Parameters[index].m_ValueBuffer != null)
            {
                if (sampleOffset >= m_ReadLength)
                    throw new ArgumentException("sampleOffset greater that the read length of the frame in ParameterData.GetValue");

                return m_Parameters[index].m_ValueBuffer[sampleOffset];
            }

            return m_Parameters[index].m_Value;
        }

        internal DSPParameter* m_Parameters;
        internal uint m_ParametersCount;
        internal uint m_ReadLength;
    }

    unsafe struct ParameterDescriptionData
    {
        public void* m_Descriptions;
        public int m_ParameterCount;
    }

    internal interface IAudioJob<P> where P : struct, IConvertible
    {
        void Init(ParameterData<P> parameters);
        void Execute(PortArray input, PortArray output, ParameterData<P> parameters, DSPInfo info);
    }

    static class AudioJobExtensions
    {
        unsafe struct AudioJobStructProduce<P, T> where P : struct, IConvertible where T : struct, IAudioJob<P>
        {
            // These structures are allocated but never freed at program termination
            static void* m_JobReflectionData;
            static ParameterDescriptionData m_ParameterDescriptionData;

            static ParameterDescriptionData CreateParameterDescription()
            {
                var pType = typeof(P);
                if (!pType.IsEnum)
                    throw new ArgumentException("CreateDSPNode<P, T> must have P as an enum.");

                var pValues = Enum.GetValues(pType);
                for (var i = 0; i < pValues.Length; i++)
                {
                    if ((int)pValues.GetValue(i) != i)
                        throw new ArgumentException("CreateDSPNode<P, T> enum values must start at 0 and be consecutive");
                }

                var descMemSize = UnsafeUtility.SizeOf<DSPParameterDescription>() * pValues.Length;
                var paramsDescriptionsPtr = UnsafeUtility.Malloc(descMemSize, UnsafeUtility.AlignOf<DSPParameterDescription>(), Allocator.Persistent);
                var paramsDescriptions = (DSPParameterDescription*)paramsDescriptionsPtr;
                for (var i = 0; i < pValues.Length; i++)
                {
                    paramsDescriptions[i].m_Min = float.MinValue;
                    paramsDescriptions[i].m_Max = float.MaxValue;
                    paramsDescriptions[i].m_DefaultValue = 0.0f;
                    var field = pType.GetField(Enum.GetName(pType, pValues.GetValue(i)));

                    var rangeAttr = (ParameterRangeAttribute)Attribute.GetCustomAttribute(field, typeof(ParameterRangeAttribute));
                    if (rangeAttr != null)
                    {
                        paramsDescriptions[i].m_Min = rangeAttr.m_Min;
                        paramsDescriptions[i].m_Max = rangeAttr.m_Max;
                    }

                    var defValAttr = (ParameterDefaultAttribute)Attribute.GetCustomAttribute(field, typeof(ParameterDefaultAttribute));
                    if (defValAttr != null)
                    {
                        paramsDescriptions[i].m_DefaultValue = defValAttr.defaultValue;
                    }
                }

                var data = new ParameterDescriptionData
                {
                    m_Descriptions = paramsDescriptionsPtr,
                    m_ParameterCount = pValues.Length
                };

                return data;
            }

            public static void Initialize(out void* jobRefData, out ParameterDescriptionData parameterDescData)
            {
                if (m_JobReflectionData == null)
                    m_JobReflectionData = (void*)JobsUtility.CreateJobReflectionData(typeof(T), JobType.Single, (ExecuteJobFunction)Execute);

                if (m_ParameterDescriptionData.m_Descriptions == null)
                    m_ParameterDescriptionData = CreateParameterDescription();

                jobRefData = m_JobReflectionData;
                parameterDescData = m_ParameterDescriptionData;
            }

            delegate void ExecuteJobFunction(ref T jobData, IntPtr audioInfoPtr, IntPtr portArrayInfoPtr, ref JobRanges ranges, int ignored2);

            static unsafe void Execute(ref T jobData, IntPtr audioDataPtr, IntPtr unused, ref JobRanges ranges, int ignored2)
            {
                AudioData* audioData = (AudioData*)audioDataPtr.ToPointer();

                var inputs = new PortArray
                {
                    m_PortBufferCount = audioData->m_InputBuffersCount,
                    m_Buffers = audioData->m_InputBuffers,
                    m_SampleCount = audioData->m_SampleReadCount,
                    m_Safety = audioData->safety
                };

                var outputs = new PortArray
                {
                    m_PortBufferCount = audioData->m_OutputBuffersCount,
                    m_Buffers = audioData->m_OutputBuffers,
                    m_SampleCount = audioData->m_SampleReadCount,
                    m_Safety = audioData->safety
                };

                var info = new DSPInfo
                {
                    m_DSPClock = audioData->m_DSPClock,
                    m_DSPBufferSize = audioData->m_DSPBufferSize,
                    m_SampleRate = audioData->m_SampleRate
                };

                var paramData = new ParameterData<P>
                {
                    m_Parameters = audioData->m_Parameters,
                    m_ParametersCount = audioData->m_ParametersCount,
                    m_ReadLength = audioData->m_SampleReadCount
                };

                jobData.Execute(inputs, outputs, paramData, info);
            }
        }

        public static unsafe void GetReflectionData<P, T>(out void* jobRefData, out ParameterDescriptionData parameterDescData)
            where P : struct, IConvertible where T : struct, IAudioJob<P>
        {
            AudioJobStructProduce<P, T>.Initialize(out jobRefData, out parameterDescData);
        }
    }

    internal struct AtomicAudioNode
    {
        internal IntPtr   m_Ptr;
        internal Int32    m_Version;
    }

    internal struct DSPNode
    {
        internal AtomicAudioNode m_Handle;
        internal AtomicAudioNode m_Graph;
    }

    internal enum SoundFormat
    {
        Raw,
        Mono,
        Stereo,
        Quad,
        Surround,
        FiveDot1,
        SevenDot1
    }

    [NativeType(Header = "Modules/Audio/Public/csas/DSPGraph.bindings.h")]
    internal struct DSPCommandBlock
    {
        internal AtomicAudioNode m_Handle;
        internal AtomicAudioNode m_Graph;

        public unsafe DSPNode CreateDSPNode<P, T>() where P : struct, IConvertible where T : struct, IAudioJob<P>
        {
            void* jobReflectionData;
            ParameterDescriptionData parameterDescriptionData;

            AudioJobExtensions.GetReflectionData<P, T>(out jobReflectionData, out parameterDescriptionData);

            var jobData = new T();
            var node = new DSPNode();

            var structMem = UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), Allocator.Persistent);
            UnsafeUtility.CopyStructureToPtr(ref jobData, structMem);

            Internal_CreateDSPNode(ref this, ref node, jobReflectionData, structMem,
                parameterDescriptionData.m_Descriptions, parameterDescriptionData.m_ParameterCount);

            return node;
        }

        public unsafe void SetFloat<P, T>(DSPNode node, P parameter, float value, uint interpolationLength = 0) where P : struct, IConvertible where T : struct, IAudioJob<P>
        {
            void* jobReflectionData;
            ParameterDescriptionData parameterDescriptionData;

            AudioJobExtensions.GetReflectionData<P, T>(out jobReflectionData, out parameterDescriptionData);

            var pIndex = Convert.ToInt32(parameter);
            if (pIndex < 0 || pIndex >= parameterDescriptionData.m_ParameterCount)
                throw new ArgumentException("Parameter Unknown");

            Internal_SetFloat(ref this, ref node, jobReflectionData, (uint)pIndex, value, interpolationLength);
        }

        public unsafe void AddFloatKey<P, T>(DSPNode node, P parameter, ulong dspClock, float value) where P : struct, IConvertible where T : struct, IAudioJob<P>
        {
            void* jobReflectionData;
            ParameterDescriptionData parameterDescriptionData;

            AudioJobExtensions.GetReflectionData<P, T>(out jobReflectionData, out parameterDescriptionData);

            var pIndex = Convert.ToInt32(parameter);
            if (pIndex < 0 || pIndex >= parameterDescriptionData.m_ParameterCount)
                throw new ArgumentException("Parameter Unknown");

            Internal_AddFloatKey(ref this, ref node, jobReflectionData, (uint)pIndex, dspClock, value);
        }

        public unsafe void SustainFloat<P, T>(DSPNode node, P parameter, ulong dspClock) where P : struct, IConvertible where T : struct, IAudioJob<P>
        {
            void* jobReflectionData;
            ParameterDescriptionData parameterDescriptionData;

            AudioJobExtensions.GetReflectionData<P, T>(out jobReflectionData, out parameterDescriptionData);

            var pIndex = Convert.ToInt32(parameter);
            if (pIndex < 0 || pIndex >= parameterDescriptionData.m_ParameterCount)
                throw new ArgumentException("Parameter Unknown");

            Internal_SustainFloat(ref this, ref node, jobReflectionData, (uint)pIndex, dspClock);
        }

        public void ReleaseDSPNode(DSPNode node)
        {
            Internal_ReleaseDSPNode(ref this, ref node);
        }

        public void Connect(DSPNode output, int outputPort, DSPNode input, int inputPort)
        {
            Internal_Connect(ref this, ref output, outputPort, ref input, inputPort);
        }

        public void Disconnect(DSPNode output, int outputPort, DSPNode input, int inputPort)
        {
            Internal_Disconnect(ref this, ref output, outputPort, ref input, inputPort);
        }

        public void AddInletPort(DSPNode node, int channelCount, SoundFormat format)
        {
            Internal_AddInletPort(ref this, ref node, channelCount, format);
        }

        public void AddOutletPort(DSPNode node, int channelCount, SoundFormat format)
        {
            Internal_AddOutletPort(ref this, ref node, channelCount, format);
        }

        public void Complete()
        {
            Internal_Complete(ref this);
        }

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe void Internal_CreateDSPNode(ref DSPCommandBlock block, ref DSPNode node, void* jobReflectionData, void* jobMemory, void* parameterDescriptionArray, int parameterCount);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe void Internal_SetFloat(ref DSPCommandBlock block, ref DSPNode node, void* jobReflectionData, uint pIndex, float value, uint interpolationLength);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe void Internal_AddFloatKey(ref DSPCommandBlock block, ref DSPNode node, void* jobReflectionData, uint pIndex, ulong dspClock, float value);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe void Internal_SustainFloat(ref DSPCommandBlock block, ref DSPNode node, void* jobReflectionData, uint pIndex, ulong dspClock);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern void Internal_ReleaseDSPNode(ref DSPCommandBlock block, ref DSPNode node);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern void Internal_Connect(ref DSPCommandBlock block, ref DSPNode output, int outputPort, ref DSPNode input, int inputPort);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern void Internal_Disconnect(ref DSPCommandBlock block, ref DSPNode output, int outputPort, ref DSPNode input, int inputPort);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern void Internal_AddInletPort(ref DSPCommandBlock block, ref DSPNode node, int channelCount, SoundFormat format);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern void Internal_AddOutletPort(ref DSPCommandBlock block, ref DSPNode node, int channelCount, SoundFormat format);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern void Internal_Complete(ref DSPCommandBlock block);
    }


    [NativeType(Header = "Modules/Audio/Public/csas/DSPGraph.bindings.h")]
    internal struct DSPGraph : IDisposable
    {
        internal AtomicAudioNode m_Handle;

        public static DSPGraph Create(SoundFormat outputFormat, uint outputChannels, uint dspBufferSize, uint sampleRate)
        {
            var graph = new DSPGraph();
            Internal_CreateDSPGraph(out graph, outputFormat, outputChannels, dspBufferSize, sampleRate);

            return graph;
        }

        public static DSPGraph GetDefaultGraph()
        {
            var graph = new DSPGraph();
            Internal_GetDefaultGraph(out graph);

            return graph;
        }

        public void Dispose()
        {
            Internal_DisposeDSPGraph(ref this);
        }

        public DSPCommandBlock CreateCommandBlock()
        {
            var block = new DSPCommandBlock();
            Internal_CreateDSPCommandBlock(ref this, ref block);

            return block;
        }

        public DSPNode GetRootDSP()
        {
            var root = new DSPNode();
            Internal_GetRootDSP(ref this, ref root);

            return root;
        }

        public ulong GetDSPClock()
        {
            return Internal_GetDSPClock(ref this);
        }

        public void BeginMix()
        {
            Internal_BeginMix(ref this);
        }

        public unsafe void ReadMix(NativeArray<float> buffer)
        {
            Internal_ReadMix(ref this, buffer.GetUnsafePtr<float>(), buffer.Length);
        }

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern void Internal_CreateDSPGraph(out DSPGraph graph, SoundFormat outputFormat, uint outputChannels, uint dspBufferSize, uint sampleRate);

        [NativeMethod(IsFreeFunction = true)]
        static extern void Internal_GetDefaultGraph(out DSPGraph graph);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern void Internal_DisposeDSPGraph(ref DSPGraph graph);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern void Internal_CreateDSPCommandBlock(ref DSPGraph graph, ref DSPCommandBlock block);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern void Internal_GetRootDSP(ref DSPGraph graph, ref DSPNode root);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern ulong Internal_GetDSPClock(ref DSPGraph graph);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern void Internal_BeginMix(ref DSPGraph graph);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe void Internal_ReadMix(ref DSPGraph graph, void* buffer, int length);
    }
}

