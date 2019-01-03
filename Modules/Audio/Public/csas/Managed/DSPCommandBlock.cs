// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Experimental.Audio;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.UNode.Audio")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.UNode.Audio.Tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.Audio")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.Audio.Tests")]

namespace Unity.Experimental.Audio
{
    internal partial struct DSPCommandBlock
    {
        internal AtomicAudioNode m_Handle;
        internal AtomicAudioNode m_Graph;

        public unsafe DSPNode CreateDSPNode<TParams, TProvs, TAudioJob>()
            where TParams   : struct, IConvertible
            where TProvs    : struct, IConvertible
            where TAudioJob : struct, IAudioJob<TParams, TProvs>
        {
            void* jobReflectionData;
            AudioJobExtensions.ParameterDescriptionData parameterDescriptionData;
            AudioJobExtensions.SampleProviderDescriptionData sampleProviderDescriptionData;
            AudioJobExtensions.GetReflectionData<TAudioJob, TParams, TProvs>(out jobReflectionData, out parameterDescriptionData, out sampleProviderDescriptionData);

            var jobData = new TAudioJob();
            var node = new DSPNode();

            var structMem = UnsafeUtility.Malloc(UnsafeUtility.SizeOf<TAudioJob>(), UnsafeUtility.AlignOf<TAudioJob>(), Allocator.Persistent);
            UnsafeUtility.CopyStructureToPtr(ref jobData, structMem);

            Internal_CreateDSPNode(ref this, ref node, jobReflectionData, structMem,
                parameterDescriptionData.m_Descriptions, parameterDescriptionData.m_ParameterCount,
                sampleProviderDescriptionData.m_Descriptions, sampleProviderDescriptionData.m_SampleProviderCount);

            return node;
        }

        public unsafe void SetFloat<TParams, TProvs, TAudioJob>(DSPNode node, TParams parameter, float value, uint interpolationLength = 0)
            where TParams   : struct, IConvertible
            where TProvs    : struct, IConvertible
            where TAudioJob : struct, IAudioJob<TParams, TProvs>
        {
            void* jobReflectionData;
            AudioJobExtensions.ParameterDescriptionData parameterDescriptionData;

            AudioJobExtensions.GetReflectionData<TAudioJob, TParams, TProvs>(out jobReflectionData, out parameterDescriptionData);

            var pIndex = UnsafeUtility.EnumToInt(parameter);
            if (pIndex < 0 || pIndex >= parameterDescriptionData.m_ParameterCount)
                throw new ArgumentException("Parameter Unknown");

            Internal_SetFloat(ref this, ref node, jobReflectionData, (uint)pIndex, value, interpolationLength);
        }

        public unsafe void AddFloatKey<TParams, TProvs, TAudioJob>(DSPNode node, TParams parameter, ulong dspClock, float value)
            where TParams   : struct, IConvertible
            where TProvs    : struct, IConvertible
            where TAudioJob : struct, IAudioJob<TParams, TProvs>
        {
            void* jobReflectionData;
            AudioJobExtensions.ParameterDescriptionData parameterDescriptionData;

            AudioJobExtensions.GetReflectionData<TAudioJob, TParams, TProvs>(out jobReflectionData, out parameterDescriptionData);

            var pIndex = UnsafeUtility.EnumToInt(parameter);
            if (pIndex < 0 || pIndex >= parameterDescriptionData.m_ParameterCount)
                throw new ArgumentException("Parameter Unknown");

            Internal_AddFloatKey(ref this, ref node, jobReflectionData, (uint)pIndex, dspClock, value);
        }

        public unsafe void SustainFloat<TParams, TProvs, TAudioJob>(DSPNode node, TParams parameter, ulong dspClock)
            where TParams   : struct, IConvertible
            where TProvs    : struct, IConvertible
            where TAudioJob : struct, IAudioJob<TParams, TProvs>
        {
            void* jobReflectionData;
            AudioJobExtensions.ParameterDescriptionData parameterDescriptionData;

            AudioJobExtensions.GetReflectionData<TAudioJob, TParams, TProvs>(out jobReflectionData, out parameterDescriptionData);

            var pIndex = UnsafeUtility.EnumToInt(parameter);
            if (pIndex < 0 || pIndex >= parameterDescriptionData.m_ParameterCount)
                throw new ArgumentException("Parameter Unknown");

            Internal_SustainFloat(ref this, ref node, jobReflectionData, (uint)pIndex, dspClock);
        }

        public unsafe void UpdateAudioJob<TAudioJobUpdate, TParams, TProvs, TAudioJob>(TAudioJobUpdate updateJob, DSPNode node)
            where TAudioJobUpdate : struct, IAudioJobUpdate<TParams, TProvs, TAudioJob>
            where TParams         : struct, IConvertible
            where TProvs          : struct, IConvertible
            where TAudioJob       : struct, IAudioJob<TParams, TProvs>
        {
            void* nodeReflectionData;
            AudioJobExtensions.ParameterDescriptionData dummy;
            AudioJobExtensions.GetReflectionData<TAudioJob, TParams, TProvs>(out nodeReflectionData, out dummy);

            void* updateJobReflectionData;
            AudioJobUpdateExtensions.GetReflectionData<TAudioJobUpdate, TParams, TProvs, TAudioJob>(out updateJobReflectionData);

            var structMem = UnsafeUtility.Malloc(UnsafeUtility.SizeOf<TAudioJobUpdate>(), UnsafeUtility.AlignOf<TAudioJobUpdate>(), Allocator.TempJob);
            UnsafeUtility.CopyStructureToPtr(ref updateJob, structMem);

            Internal_UpdateAudioJob(ref this, ref node, structMem, updateJobReflectionData, nodeReflectionData);
        }

        public unsafe DSPNodeUpdateRequest<TAudioJobUpdate, TParams, TProvs, TAudioJob> CreateUpdateRequest<TAudioJobUpdate, TParams, TProvs, TAudioJob>(
            TAudioJobUpdate updateJob, DSPNode node, Action<DSPNodeUpdateRequest<TAudioJobUpdate, TParams, TProvs, TAudioJob>> callback)
            where TAudioJobUpdate : struct, IAudioJobUpdate<TParams, TProvs, TAudioJob>
            where TParams         : struct, IConvertible
            where TProvs          : struct, IConvertible
            where TAudioJob       : struct, IAudioJob<TParams, TProvs>
        {
            void* nodeReflectionData;
            AudioJobExtensions.ParameterDescriptionData dummy;
            AudioJobExtensions.GetReflectionData<TAudioJob, TParams, TProvs>(out nodeReflectionData, out dummy);

            void* updateJobReflectionData;
            AudioJobUpdateExtensions.GetReflectionData<TAudioJobUpdate, TParams, TProvs, TAudioJob>(out updateJobReflectionData);

            var structMem = UnsafeUtility.Malloc(UnsafeUtility.SizeOf<TAudioJobUpdate>(), UnsafeUtility.AlignOf<TAudioJobUpdate>(), Allocator.Persistent);
            UnsafeUtility.CopyStructureToPtr(ref updateJob, structMem);

            var handle = new DSPNodeUpdateRequestHandle();
            Internal_CreateUpdateRequest(ref this, ref node, ref handle, callback, structMem, updateJobReflectionData, nodeReflectionData);

            var request = new DSPNodeUpdateRequest<TAudioJobUpdate, TParams, TProvs, TAudioJob> { m_Handle = handle };
            return request;
        }

        public void ReleaseDSPNode(DSPNode node)
        {
            Internal_ReleaseDSPNode(ref this, ref node);
        }

        public DSPConnection Connect(DSPNode output, int outputPort, DSPNode input, int inputPort)
        {
            var connection = new DSPConnection();
            Internal_Connect(ref this, ref output, outputPort, ref input, inputPort, ref connection);
            return connection;
        }

        public void Disconnect(DSPConnection connection)
        {
            Internal_DisconnectByHandle(ref this, ref connection);
        }

        public void Disconnect(DSPNode output, int outputPort, DSPNode input, int inputPort)
        {
            Internal_Disconnect(ref this, ref output, outputPort, ref input, inputPort);
        }

        public void SetAttenuation(DSPConnection connection, float value, uint interpolationLength = 0)
        {
            Internal_SetAttenuation(ref this, ref connection, value, interpolationLength);
        }

        public void AddAttenuationKey(DSPConnection connection, ulong dspClock, float value)
        {
            Internal_AddAttenuationKey(ref this, ref connection, dspClock, value);
        }

        public void SustainAttenuation(DSPConnection connection, ulong dspClock)
        {
            Internal_SustainAttenuation(ref this, ref connection, dspClock);
        }

        public void AddInletPort(DSPNode node, int channelCount, SoundFormat format)
        {
            Internal_AddInletPort(ref this, ref node, channelCount, format);
        }

        public void AddOutletPort(DSPNode node, int channelCount, SoundFormat format)
        {
            Internal_AddOutletPort(ref this, ref node, channelCount, format);
        }

        // provider can be null to clear an existing entry. index ignored for non-array items.
        public unsafe void SetSampleProvider<TParams, TProvs, TAudioJob>(
            AudioSampleProvider provider, DSPNode node, TProvs item, int index = 0)
            where TParams   : struct, IConvertible
            where TProvs    : struct, IConvertible
            where TAudioJob : struct, IAudioJob<TParams, TProvs>
        {
            AudioJobExtensions.SampleProviderDescriptionData sampleProviderDescriptionData;
            AudioJobExtensions.GetReflectionData<TAudioJob, TParams, TProvs>(out sampleProviderDescriptionData);

            var pItem = Convert.ToInt32(item);
            if (pItem < 0 || pItem >= sampleProviderDescriptionData.m_SampleProviderCount)
                throw new ArgumentException("SampleProvider Unknown");

            // Index validation for fixed-size array items can be performed here. For variable-array,
            // it can only be performed in the job threads, where the array size is known and stable.
            if (sampleProviderDescriptionData.m_Descriptions[pItem].m_IsArray &&
                sampleProviderDescriptionData.m_Descriptions[pItem].m_Size >= 0 &&
                (sampleProviderDescriptionData.m_Descriptions[pItem].m_Size < index || index < 0))
                throw new IndexOutOfRangeException("index");

            Internal_SetSampleProvider(ref this, ref node, pItem, index, provider != null ? provider.id : (uint)0);
        }

        // -1 = append. Just for variable arrays.
        public unsafe void InsertSampleProvider<TParams, TProvs, TAudioJob>(
            AudioSampleProvider provider, DSPNode node, TProvs item, int index = -1)
            where TParams   : struct, IConvertible
            where TProvs    : struct, IConvertible
            where TAudioJob : struct, IAudioJob<TParams, TProvs>
        {
            AudioJobExtensions.SampleProviderDescriptionData sampleProviderDescriptionData;
            AudioJobExtensions.GetReflectionData<TAudioJob, TParams, TProvs>(out sampleProviderDescriptionData);

            var pItem = Convert.ToInt32(item);
            if (pItem < 0 || pItem >= sampleProviderDescriptionData.m_SampleProviderCount)
                throw new ArgumentException("SampleProvider Unknown");

            // Can only insert into variable-size arrays.
            if (!sampleProviderDescriptionData.m_Descriptions[pItem].m_IsArray ||
                sampleProviderDescriptionData.m_Descriptions[pItem].m_Size >= 0)
                throw new InvalidOperationException("Can only insert in variable-size array.");

            Internal_InsertSampleProvider(ref this, ref node, pItem, index, provider.id);
        }

        // -1 = last, otherwise removes in middle.
        public unsafe void RemoveSampleProvider<TParams, TProvs, TAudioJob>(DSPNode node, TProvs item, int index = -1)
            where TParams   : struct, IConvertible
            where TProvs    : struct, IConvertible
            where TAudioJob : struct, IAudioJob<TParams, TProvs>
        {
            AudioJobExtensions.SampleProviderDescriptionData sampleProviderDescriptionData;
            AudioJobExtensions.GetReflectionData<TAudioJob, TParams, TProvs>(out sampleProviderDescriptionData);

            var pItem = Convert.ToInt32(item);
            if (pItem < 0 || pItem >= sampleProviderDescriptionData.m_SampleProviderCount)
                throw new ArgumentException("SampleProvider Unknown");

            // Can only remove from variable-size arrays.
            if (!sampleProviderDescriptionData.m_Descriptions[pItem].m_IsArray ||
                sampleProviderDescriptionData.m_Descriptions[pItem].m_Size >= 0)
                throw new InvalidOperationException("Can only remove from variable-size array.");

            Internal_RemoveSampleProvider(ref this, ref node, pItem, index);
        }

        public void Complete()
        {
            Internal_Complete(ref this);
        }
    }
}

