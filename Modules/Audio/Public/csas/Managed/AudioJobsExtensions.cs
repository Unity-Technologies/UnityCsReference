// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;

namespace Unity.Experimental.Audio
{
    internal static class AudioJobExtensions
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct DSPParameterDescription
        {
            internal float m_Min;
            internal float m_Max;
            internal float m_DefaultValue;
        }

        public unsafe struct ParameterDescriptionData
        {
            public DSPParameterDescription* m_Descriptions; //This can be not void
            public int m_ParameterCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DSPSampleProviderDescription
        {
            internal bool m_IsArray;
            internal int  m_Size;
        }

        public unsafe struct SampleProviderDescriptionData
        {
            public AudioJobExtensions.DSPSampleProviderDescription* m_Descriptions;
            public int m_SampleProviderCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        unsafe struct NativeAudioData
        {
            public ulong m_DSPClock;
            public uint m_DSPBufferSize;
            public uint m_SampleRate;

            public uint m_SampleReadCount;

            public uint m_InputBuffersCount;
            public NativeSampleBuffer* m_InputBuffers;

            public uint m_OutputBuffersCount;
            public NativeSampleBuffer* m_OutputBuffers;

            public uint m_ParametersCount;
            public NativeDSPParameter* m_Parameters;

            public uint m_SampleProviderIndicesCount;
            public int* m_SampleProviderIndices;

            public uint m_SampleProvidersCount;
            public DSPSampleProvider* m_SampleProviders;

            public AtomicSafetyHandle safety;
        }

        public unsafe struct AudioJobStructProduce<TAudioJob, TParams, TProvs>
            where TAudioJob : struct, IAudioJob<TParams, TProvs>
            where TParams   : struct, IConvertible
            where TProvs    : struct, IConvertible
        {
            // These structures are allocated but never freed at program termination
            static void* m_JobReflectionData;
            static ParameterDescriptionData m_ParameterDescriptionData;
            static SampleProviderDescriptionData m_SampleProviderDescriptionData;

            static ParameterDescriptionData CreateParameterDescription()
            {
                var pType = typeof(TParams);
                if (!pType.IsEnum)
                    throw new ArgumentException("CreateDSPNode<Params, Provs, T> must have P as an enum.");

                var pValues = Enum.GetValues(pType);
                for (var i = 0; i < pValues.Length; i++)
                {
                    if ((int)pValues.GetValue(i) != i)
                        throw new ArgumentException("CreateDSPNode<Params, Provs, T> enum values must start at 0 and be consecutive");
                }

                var descMemSize = UnsafeUtility.SizeOf<DSPParameterDescription>() * pValues.Length;
                var paramsDescriptions = (DSPParameterDescription*)UnsafeUtility.Malloc(descMemSize, UnsafeUtility.AlignOf<DSPParameterDescription>(), Allocator.Persistent);
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
                    m_Descriptions = paramsDescriptions,
                    m_ParameterCount = pValues.Length
                };

                return data;
            }

            static SampleProviderDescriptionData CreateSampleProviderDescription()
            {
                var pType = typeof(TProvs);
                if (!pType.IsEnum)
                    throw new ArgumentException("CreateDSPNode<Params, Provs, T> must have Provs as an enum.");

                var pValues = Enum.GetValues(pType);
                for (var i = 0; i < pValues.Length; i++)
                {
                    if ((int)pValues.GetValue(i) != i)
                        throw new ArgumentException("CreateDSPNode<Params, Provs, T> enum values must start at 0 and be consecutive");
                }

                var descMemSize = UnsafeUtility.SizeOf<DSPSampleProviderDescription>() * pValues.Length;
                var provsDescriptionsPtr = UnsafeUtility.Malloc(descMemSize, UnsafeUtility.AlignOf<DSPSampleProviderDescription>(), Allocator.Persistent);
                var provsDescriptions = (DSPSampleProviderDescription*)provsDescriptionsPtr;
                for (var i = 0; i < pValues.Length; i++)
                {
                    provsDescriptions[i].m_IsArray = false;
                    provsDescriptions[i].m_Size = -1;

                    var field = pType.GetField(Enum.GetName(pType, pValues.GetValue(i)));

                    var arrayAttr = (SampleProviderArrayAttribute)Attribute.GetCustomAttribute(field, typeof(SampleProviderArrayAttribute));
                    if (arrayAttr != null)
                    {
                        provsDescriptions[i].m_IsArray = true;
                        provsDescriptions[i].m_Size = arrayAttr.size;
                    }
                }

                return new SampleProviderDescriptionData
                {
                    m_Descriptions = provsDescriptions,
                    m_SampleProviderCount = pValues.Length
                };
            }

            public static void Initialize(
                out void* jobRefData, out ParameterDescriptionData parameterDescData,
                out SampleProviderDescriptionData sampleProviderDescData)
            {
                if (m_JobReflectionData == null)
                    m_JobReflectionData = (void*)JobsUtility.CreateJobReflectionData(typeof(TAudioJob), JobType.Single, (ExecuteJobFunction)Execute);

                if (m_ParameterDescriptionData.m_Descriptions == null)
                    m_ParameterDescriptionData = CreateParameterDescription();

                if (m_SampleProviderDescriptionData.m_Descriptions == null)
                    m_SampleProviderDescriptionData = CreateSampleProviderDescription();

                jobRefData = m_JobReflectionData;
                parameterDescData = m_ParameterDescriptionData;
                sampleProviderDescData = m_SampleProviderDescriptionData;
            }

            delegate void ExecuteJobFunction(ref TAudioJob jobData, IntPtr audioInfoPtr, IntPtr dspNodePtr, ref JobRanges ranges, int ignored2);

            // Needs to be public so that Burst can pick this up during its compilation. Otherwise
            // should be private.
            public static unsafe void Execute(ref TAudioJob jobData, IntPtr audioDataPtr, IntPtr dspNodePtr, ref JobRanges ranges, int ignored2)
            {
                var audioData = (NativeAudioData*)audioDataPtr.ToPointer();
                var ctx = new ExecuteContext<TParams, TProvs>();

                ctx.m_DSPClock = audioData->m_DSPClock;
                ctx.m_DSPBufferSize = audioData->m_DSPBufferSize;
                ctx.m_SampleRate = audioData->m_SampleRate;
                ctx.m_DSPNodePtr = (void*)dspNodePtr.ToPointer();

                ctx.Inputs = new SampleBufferArray
                {
                    m_SampleBufferCount = audioData->m_InputBuffersCount,
                    m_Buffers = audioData->m_InputBuffers,
                    m_SampleCount = audioData->m_SampleReadCount,
                    m_Safety = audioData->safety
                };

                ctx.Outputs = new SampleBufferArray
                {
                    m_SampleBufferCount = audioData->m_OutputBuffersCount,
                    m_Buffers = audioData->m_OutputBuffers,
                    m_SampleCount = audioData->m_SampleReadCount,
                    m_Safety = audioData->safety
                };

                ctx.Parameters = new ParameterData<TParams>
                {
                    m_Parameters = audioData->m_Parameters,
                    m_ParametersCount = audioData->m_ParametersCount,
                    m_ReadLength = audioData->m_SampleReadCount
                };

                ctx.Providers = new SampleProviderContainer<TProvs>
                {
                    m_SampleProviderIndicesCount = audioData->m_SampleProviderIndicesCount,
                    m_SampleProviderIndices = audioData->m_SampleProviderIndices,
                    m_SampleProvidersCount = audioData->m_SampleProvidersCount,
                    m_SampleProviders = audioData->m_SampleProviders,
                    m_Safety = audioData->safety
                };

                jobData.Execute(ref ctx);
            }
        }

        public static unsafe void GetReflectionData<TAudioJob, TParams, TProvs>(
            out void* jobRefData, out ParameterDescriptionData parameterDescData,
            out SampleProviderDescriptionData sampleProviderDescData)
            where TAudioJob : struct, IAudioJob<TParams, TProvs>
            where TParams   : struct, IConvertible
            where TProvs    : struct, IConvertible
        {
            AudioJobStructProduce<TAudioJob, TParams, TProvs>.Initialize(
                out jobRefData, out parameterDescData, out sampleProviderDescData);
        }

        public static unsafe void GetReflectionData<TAudioJob, TParams, TProvs>(
            out void* jobRefData, out ParameterDescriptionData parameterDescData)
            where TAudioJob : struct, IAudioJob<TParams, TProvs>
            where TParams   : struct, IConvertible
            where TProvs    : struct, IConvertible
        {
            SampleProviderDescriptionData ignored;
            AudioJobStructProduce<TAudioJob, TParams, TProvs>.Initialize(
                out jobRefData, out parameterDescData, out ignored);
        }

        public static unsafe void GetReflectionData<TAudioJob, TParams, TProvs>(
            out SampleProviderDescriptionData sampleProviderDescData)
            where TAudioJob : struct, IAudioJob<TParams, TProvs>
            where TParams   : struct, IConvertible
            where TProvs    : struct, IConvertible
        {
            void* ignoredJobRefData;
            ParameterDescriptionData ignoredParameterDescriptionData;
            AudioJobStructProduce<TAudioJob, TParams, TProvs>.Initialize(
                out ignoredJobRefData, out ignoredParameterDescriptionData,
                out sampleProviderDescData);
        }
    }
}

