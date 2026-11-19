// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;


namespace Unity.Animations
{
    /// <summary>
    /// An intermediate data representing the rig.
    /// </summary>
    /// <remarks>
    /// If you don't use the Skeleton but your own custom rig representation,
    /// as long as you have a method to convert this representation to a RigBuilder,
    /// we can build a RigDefinition from it.
    ///
    /// The following example uses the `Unity.Animations`, `Unity.Collections`, `Unity.Entities`, `Unity.Mathematics`,
    /// and `UnityEngine` namespaces. It builds a rig definition with three bones and one float channel.
    /// </remarks>
    /// <example nocheck="true">
    /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/BuildRigExample.cs#BuildRigDefinition}]]></code>
    /// </example>
    /// <seealso cref="RigDefinition"/>
    /// <seealso cref="BindingSet"/>
    [GenerateTestsForBurstCompatibility, BurstCompile]
    public partial struct RigBuilder : IDisposable
    {

        internal NativeList<SkeletonNode> m_SkeletonNodes;
        internal NativeList<LocalTranslationChannel> m_TranslationChannels;
        internal NativeList<LocalRotationChannel> m_RotationChannels;
        internal NativeList<LocalScaleChannel> m_ScaleChannels;
        internal NativeList<FloatChannel> m_FloatChannels;
        internal NativeList<IntChannel> m_IntChannels;
        internal NativeList<EntityIdChannel> m_EntityIdChannels;
        internal NativeList<SocketNode> m_SocketNodes;

        [MarshalAs(UnmanagedType.U1)]
        bool m_IsCreated;

        /// <summary>
        /// RigBuilder constructor.
        /// </summary>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.
        /// </param>
        public RigBuilder(Allocator allocator)
        {
            m_SkeletonNodes = new NativeList<SkeletonNode>(allocator);
            m_TranslationChannels = new NativeList<LocalTranslationChannel>(allocator);
            m_RotationChannels = new NativeList<LocalRotationChannel>(allocator);
            m_ScaleChannels = new NativeList<LocalScaleChannel>(allocator);
            m_FloatChannels = new NativeList<FloatChannel>(allocator);
            m_IntChannels = new NativeList<IntChannel>(allocator);
            m_EntityIdChannels = new NativeList<EntityIdChannel>(allocator);
            m_SocketNodes = new NativeList<SocketNode>(allocator);

            m_IsCreated = true;
        }

        /// <summary>
        /// Returns true if the RigBuilder has been properly created.
        /// </summary>
        /// <remarks>The default constructor doesn't create the builder correctly. Use
        /// <see cref="RigBuilder(Allocator)"/> instead.</remarks>
        public bool IsCreated => m_IsCreated;

        /// <summary>
        /// Adds a SkeletonNode to the RigBuilder.
        /// </summary>
        /// <param name="node">The skeleton node to add.</param>
        public void Add(in SkeletonNode node)
        {
            ValidateIsCreated();
            m_SkeletonNodes.Add(node);
        }

        /// <summary>
        /// Adds a LocalTranslationChannel to the Rigbuilder.
        /// </summary>
        /// <param name="channel">The LocalTranslationChannel to add.</param>
        public void Add(in LocalTranslationChannel channel)
        {
            ValidateIsCreated();
            m_TranslationChannels.Add(channel);
        }

        /// <summary>
        /// Adds a LocalRotationChannel to the RigBuilder/
        /// </summary>
        /// <param name="channel">The LocalRotationChannel to add.</param>
        public void Add(in LocalRotationChannel channel)
        {
            ValidateIsCreated();
            m_RotationChannels.Add(channel);
        }

        /// <summary>
        /// Adds a LocalScaleChannel to the RigBuilder.
        /// </summary>
        /// <param name="channel">The LocalScaleChannel to add.</param>
        public void Add(in LocalScaleChannel channel)
        {
            ValidateIsCreated();
            m_ScaleChannels.Add(channel);
        }

        /// <summary>
        /// Adds a FloatChannel to the RigBuilder.
        /// </summary>
        /// <param name="channel">The FloatChannel to add.</param>
        public void Add(in FloatChannel channel)
        {
            ValidateIsCreated();
            m_FloatChannels.Add(channel);
        }

        /// <summary>
        /// Adds an IntChannel to the RigBuilder.
        /// </summary>
        /// <param name="channel">The IntChannel to add.</param>
        public void Add(in IntChannel channel)
        {
            ValidateIsCreated();
            m_IntChannels.Add(channel);
        }

        /// <summary>
        /// Adds an EntityIdChannel to the RigBuilder.
        /// </summary>
        /// <remarks>
        /// The channel becomes an entity ID binding of the <see cref="RigDefinition"/> that
        /// <see cref="ToRigDefinition(Unity.Collections.Allocator)"/> creates. A clip drives the channel when it has a
        /// curve that animates an object reference and whose combined path and property hash equals
        /// <see cref="EntityIdChannel.Id"/>. Call this method on a builder that you created with
        /// <see cref="RigBuilder(Allocator)"/>. Otherwise, if safety checks are enabled, the method throws a
        /// `NullReferenceException`.
        /// </remarks>
        /// <param name="channel">The channel to add to the rig that the builder creates.</param>
        public void Add(in EntityIdChannel channel)
        {
            ValidateIsCreated();
            m_EntityIdChannels.Add(channel);
        }

        /// <summary>
        /// Adds a SocketNode to the RigBuilder.
        /// </summary>
        /// <param name="socketNode">The SocketNode to add.</param>
        public void Add(in SocketNode socketNode)
        {
            ValidateIsCreated();
            m_SocketNodes.Add(socketNode);
        }

        /// <summary>
        /// Clear all RigBuilder channels.
        /// </summary>
        public void Clear()
        {
            if (!IsCreated)
                return;

            m_SkeletonNodes.Clear();
            m_TranslationChannels.Clear();
            m_RotationChannels.Clear();
            m_ScaleChannels.Clear();
            m_FloatChannels.Clear();
            m_EntityIdChannels.Clear();
            m_IntChannels.Clear();
            m_SocketNodes.Clear();
        }

        /// <summary>
        /// Disposes the RigBuilder.
        /// </summary>
        public void Dispose()
        {
            if (!IsCreated)
                return;

            m_SkeletonNodes.Dispose();
            m_TranslationChannels.Dispose();
            m_RotationChannels.Dispose();
            m_ScaleChannels.Dispose();
            m_FloatChannels.Dispose();
            m_IntChannels.Dispose();
            m_EntityIdChannels.Dispose();
            m_SocketNodes.Dispose();

            m_IsCreated = false;
        }

        /// <summary>
        /// Creates a RigDefinition from the current RigBuilder data.
        /// </summary>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration
        /// </param>
        /// <returns>The BlobAssetReference of a RigDefinition.</returns>
        public BlobAssetReference<RigDefinition> ToRigDefinition(Allocator allocator)
        {
            ValidateIsCreated();
            ValidateSkeletonData();

            BlobAssetReference<RigDefinition> rigDefinitionAsset = default;
            CreateRigDefinition(ref this, ref rigDefinitionAsset, (int)allocator);
            return rigDefinitionAsset;
        }

        [BurstCompile]
        static void CreateRigDefinition(ref RigBuilder rigBuilderData, ref BlobAssetReference<RigDefinition> result, int allocator)
        {
            var blobBuilder = new BlobBuilder(Allocator.Temp);
            try
            {
                using var translations = new NativeList<LocalTranslationChannel>(Allocator.Temp);
                using var rotations = new NativeList<LocalRotationChannel>(Allocator.Temp);
                using var scales = new NativeList<LocalScaleChannel>(Allocator.Temp);

                GetAnimationChannels(rigBuilderData.m_SkeletonNodes, translations, rotations, scales);
                GetAnimationChannels(rigBuilderData.m_TranslationChannels, translations);
                GetAnimationChannels(rigBuilderData.m_RotationChannels, rotations);
                GetAnimationChannels(rigBuilderData.m_ScaleChannels, scales);

                ref var rig = ref blobBuilder.ConstructRoot<RigDefinition>();

                InitializeSkeletonNodes(ref blobBuilder, rigBuilderData.m_SkeletonNodes, ref rig.Skeleton.ParentIndexes, ref rig.Skeleton.Ids);

                InitializeBindings(ref blobBuilder, translations, ref rig.Bindings.TranslationBindings);
                InitializeBindings(ref blobBuilder, scales, ref rig.Bindings.ScaleBindings);
                InitializeBindings(ref blobBuilder, rigBuilderData.m_FloatChannels, ref rig.Bindings.FloatBindings);
                InitializeBindings(ref blobBuilder, rigBuilderData.m_IntChannels, ref rig.Bindings.IntBindings);
                InitializeBindings(ref blobBuilder, rotations, ref rig.Bindings.RotationBindings);
                InitializeBindings(ref blobBuilder, rigBuilderData.m_EntityIdChannels, ref rig.Bindings.EntityIdBindings);

                rig.SetupBindingSet(
                    translations.Length,
                    rotations.Length,
                    scales.Length,
                    rigBuilderData.m_FloatChannels.Length,
                    rigBuilderData.m_IntChannels.Length,
                    rigBuilderData.m_EntityIdChannels.Length);

                var arrayBuilder = blobBuilder.Allocate(ref rig.DefaultValues, rig.Bindings.StreamSize);

                InitializeDefaultValues<LocalTranslationChannel, float3>(rig.Bindings.TranslationSamplesOffset, ref arrayBuilder, translations);
                InitializeDefaultValues<LocalScaleChannel, float3>(rig.Bindings.ScaleSamplesOffset, ref arrayBuilder, scales);
                InitializeDefaultValues<FloatChannel, float>(rig.Bindings.FloatSamplesOffset, ref arrayBuilder, rigBuilderData.m_FloatChannels);
                InitializeDefaultValues<IntChannel, int>(rig.Bindings.IntSamplesOffset, ref arrayBuilder, rigBuilderData.m_IntChannels);
                InitializeDefaultValues<EntityIdChannel, EntityId>(rig.Bindings.EntityIdSamplesOffset, ref arrayBuilder, rigBuilderData.m_EntityIdChannels);
                InitializeDefaultRotationValues(rig.Bindings.RotationSamplesOffset, rig.Bindings.RotationDataChunkCount, ref arrayBuilder, rotations);

                InitializeSocketValues(ref blobBuilder, rigBuilderData.m_SocketNodes, ref rig.SocketValues);

                result = blobBuilder.CreateBlobAssetReference<RigDefinition>((Allocator)allocator);
            }
            finally
            {
                blobBuilder.Dispose();
            }
        }

        static void GetAnimationChannels(NativeList<SkeletonNode> skeletonNodes, NativeList<LocalTranslationChannel> translationChannels, NativeList<LocalRotationChannel> rotationChannels, NativeList<LocalScaleChannel> scaleChannels)
        {
            for (int i = 0; i < skeletonNodes.Length; i++)
            {
                var localTranslationChannel = new LocalTranslationChannel { Id = skeletonNodes[i].Id, DefaultValue = skeletonNodes[i].LocalTranslationDefaultValue };
                if (!translationChannels.Contains(localTranslationChannel))
                    translationChannels.Add(localTranslationChannel);

                var localRotationChannel = new LocalRotationChannel { Id = skeletonNodes[i].Id, DefaultValue = skeletonNodes[i].LocalRotationDefaultValue };
                if (!rotationChannels.Contains(localRotationChannel))
                    rotationChannels.Add(localRotationChannel);

                var localScaleChannel = new LocalScaleChannel { Id = skeletonNodes[i].Id, DefaultValue = skeletonNodes[i].LocalScaleDefaultValue };
                if (!scaleChannels.Contains(localScaleChannel))
                    scaleChannels.Add(localScaleChannel);
            }
        }

        static void GetAnimationChannels<T>(NativeList<T> animationChannels, NativeList<T> filteredAnimationChannels) where T : unmanaged, IEquatable<T>
        {
            for (var i = 0; i != animationChannels.Length; i++)
            {
                var channel = animationChannels[i];
                if (!filteredAnimationChannels.Contains(channel))
                    filteredAnimationChannels.Add(channel);
            }
        }

        static void InitializeSkeletonNodes(ref BlobBuilder blobBuilder, NativeList<SkeletonNode> skeletonNodes, ref BlobArray<int> parentIndices, ref BlobArray<StringHash> skeletonIds)
        {
            if (!skeletonNodes.IsCreated || skeletonNodes.Length == 0)
                return;

            var parentIndicesBuilder = blobBuilder.Allocate(ref parentIndices, skeletonNodes.Length);
            var skeletonIdsBuilder = blobBuilder.Allocate(ref skeletonIds, skeletonNodes.Length);
            for (int i = 0; i < skeletonNodes.Length; i++)
            {
                parentIndicesBuilder[i] = skeletonNodes[i].ParentIndex;
                skeletonIdsBuilder[i] = skeletonNodes[i].Id;
            }
        }

        static void InitializeBindings<T>(ref BlobBuilder blobBuilder, NativeList<T> animationChannels, ref BlobArray<StringHash> bindings)
            where T : unmanaged, IAnimationChannel
        {
            if (!animationChannels.IsCreated || animationChannels.Length == 0)
                return;

            var arrayBuilder = blobBuilder.Allocate(ref bindings, animationChannels.Length);
            for (int i = 0; i < animationChannels.Length; i++)
            {
                arrayBuilder[i] = animationChannels[i].Id;
            }
        }

        static unsafe void InitializeDefaultValues<TChannel, TData>(int index, ref BlobBuilderArray<AnimatedData> arrayBuilder, NativeList<TChannel> animationChannels)
            where TChannel : unmanaged, IAnimationChannel<TData>
            where TData : unmanaged
        {
            if (!animationChannels.IsCreated || animationChannels.Length == 0)
                return;

            TData* data = (TData*)((float*)arrayBuilder.GetUnsafePtr() + index);
            for (int i = 0; i < animationChannels.Length; i++)
            {
                data[i] = animationChannels[i].DefaultValue;
            }
        }

        static unsafe void InitializeDefaultRotationValues(int index, int rotationDataChunkCount, ref BlobBuilderArray<AnimatedData> arrayBuilder, NativeList<LocalRotationChannel> rotationChannels)
        {
            if (rotationChannels.IsEmpty)
                return;

            var combinedRotationChannelCount = 0;
            if (rotationChannels.IsCreated)
                combinedRotationChannelCount += rotationChannels.Length;

            using var combinedRotationChannels = new NativeList<quaternion>(combinedRotationChannelCount, Allocator.Temp);
            if (rotationChannels.IsCreated)
            {
                for (var i = 0; i < rotationChannels.Length; i++)
                {
                    combinedRotationChannels.Add(rotationChannels[i].DefaultValue);
                }
            }

            // Fill as SOA 4-wide quaternions
            quaternion4* data = (quaternion4*)((float*)arrayBuilder.GetUnsafePtr() + index);
            int length = combinedRotationChannels.Length >> 2;
            for (int i = 0; i < length; ++i)
            {
                int idx = i << 2;
                data[i] = new quaternion4(
                    math.normalizesafe(combinedRotationChannels[idx + 0]),
                    math.normalizesafe(combinedRotationChannels[idx + 1]),
                    math.normalizesafe(combinedRotationChannels[idx + 2]),
                    math.normalizesafe(combinedRotationChannels[idx + 3])
                );
            }

            // Fill remaining rotations
            for (int i = length << 2; i < combinedRotationChannels.Length; ++i)
            {
                int chunkIdx = i >> 2;
                int subIdx = i & 0x3; // equivalent to % 4;

                quaternion q = math.normalizesafe(combinedRotationChannels[i]);
                ref quaternion4 q4 = ref UnsafeUtility.AsRef<quaternion4>(data + chunkIdx);

                q4.x[subIdx] = q.value.x;
                q4.y[subIdx] = q.value.y;
                q4.z[subIdx] = q.value.z;
                q4.w[subIdx] = q.value.w;
            }

            // Fill the empty quaternions in the last chunk with identity value, to not break blend code.
            var rotationChannelsInMemoryCount = rotationDataChunkCount * 4;
            for(var i = combinedRotationChannels.Length; i < rotationChannelsInMemoryCount ; ++i)
            {
                var chunkIdx = i >> 2;
                var subIdx = i & 0x3;

                ref quaternion4 q4 = ref UnsafeUtility.AsRef<quaternion4>(data + chunkIdx);

                q4.x[subIdx] = 0.0f;
                q4.y[subIdx] = 0.0f;
                q4.z[subIdx] = 0.0f;
                q4.w[subIdx] = 1.0f;
            }
        }

        static unsafe void InitializeSocketValues(ref BlobBuilder blobBuilder, NativeList<SocketNode> socketNodes, ref BlobArray<SocketNode> socketValues)
        {
            if (!socketNodes.IsCreated || socketNodes.Length == 0)
                return;

            var arrayBuilder = blobBuilder.Allocate(ref socketValues, socketNodes.Length);
            UnsafeUtility.MemCpy(arrayBuilder.GetUnsafePtr(), socketNodes.GetUnsafePtr(), UnsafeUtility.SizeOf<SocketNode>() * socketNodes.Length);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void ValidateSkeletonData()
        {
            if (m_SkeletonNodes.IsEmpty)
                return;

            var skeletonNodeCount = m_SkeletonNodes.Length;
            if (skeletonNodeCount > 0 && m_SkeletonNodes[0].ParentIndex != -1)
                throw new ArgumentException("First skeleton node is expected to be the root node.");

            for (int i = 1; i < skeletonNodeCount; ++i)
            {
                ref readonly var skeletonNode = ref m_SkeletonNodes.ElementAt(i);

                if (skeletonNode.ParentIndex == -1)
                {
                    throw new ArgumentException(
                        $"SkeletonNode[{i}] has an invalid ParentIndex: You can have only one root node in your skeleton."
                    );
                }

                if (skeletonNode.ParentIndex == i)
                {
                    throw new ArgumentException(
                        $"SkeletonNode[{i}] has an invalid ParentIndex '{skeletonNode.ParentIndex}': The SkeletonNode parent cannot be itself."
                    );
                }

                if (skeletonNode.ParentIndex < 0 || skeletonNode.ParentIndex >= skeletonNodeCount)
                {
                    throw new ArgumentOutOfRangeException(
                        $"SkeletonNode[{i}] has an invalid ParentIndex '{skeletonNode.ParentIndex}': value must be between 0 and {skeletonNodeCount - 1}."
                    );
                }
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void ValidateIsCreated()
        {
            if (!m_IsCreated)
                throw new NullReferenceException("RigBuilder is not properly created.");
        }
    }
}
