// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;
using System.Runtime.CompilerServices;

namespace Unity.Animations
{
    [GenerateTestsForBurstCompatibility]
    [BurstCompile]
    internal partial struct WeightMaskInstance
    {
        static readonly Profiling.ProfilerMarker s_CreateMarker = new Profiling.ProfilerMarker(nameof(WeightMaskInstance) + "." + nameof(Create));

        const int k_BitsPerField = 32;
        const int k_BitShiftPerField = 5;

        struct BindingCacheEntry
        {
            public int BindingIndex;
            public bool BindingInherited;

            public BindingCacheEntry(int bindingIndex, bool bindingInherited)
            {
                BindingIndex = bindingIndex;
                BindingInherited = bindingInherited;
            }
        }

        [BurstCompile]
        static void FindInheritedWeightIndex(
            ref BlobAssetReference<RigDefinition> rigDefinition,
            ref BlobArray<StringHash> bindings,
            ref NativeHashMap<StringHash, BindingCacheEntry> bindingCache,
            StringHash searchedBinding,
            out int bindingIndex,
            out bool bindingInherited)
        {
            if (bindingCache.ContainsKey(searchedBinding))
            {
                var entry = bindingCache[searchedBinding];
                bindingIndex = entry.BindingIndex;
                bindingInherited = entry.BindingInherited;
                return;
            }

            bindingIndex = BindingUtility.FindBindingIndex(ref bindings, searchedBinding);
            if (bindingIndex != -1)
            {
                bindingInherited = false;
                bindingCache.Add(searchedBinding, new(bindingIndex, bindingInherited));
                return;
            }

            // Iterate through parent bindings in the skeleton.
            var skeletonIndex = BindingUtility.FindBindingIndex(ref rigDefinition.Value.Skeleton.Ids, searchedBinding);
            if (skeletonIndex == -1)
            {
                bindingIndex = -1;
                bindingInherited = false;
                bindingCache.Add(searchedBinding, new(bindingIndex, bindingInherited));
                return;
            }

            skeletonIndex = rigDefinition.Value.Skeleton.ParentIndexes[skeletonIndex];
            while (skeletonIndex != -1)
            {
                bindingIndex = BindingUtility.FindBindingIndex(ref bindings, rigDefinition.Value.Skeleton.Ids[skeletonIndex]);
                if (bindingIndex != -1)
                {
                    bindingInherited = true;
                    bindingCache.Add(searchedBinding, new(bindingIndex, bindingInherited));
                    return;
                }

                skeletonIndex = rigDefinition.Value.Skeleton.ParentIndexes[skeletonIndex];
            }

            bindingIndex = -1;
            bindingInherited = false;
            bindingCache.Add(searchedBinding, new(bindingIndex, bindingInherited));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float GetTransformWeightValue(int bindingIndex, bool bindingInherited, ref BlobAssetReference<WeightMask> weightMask)
        {
            if (bindingIndex == -1)
                return weightMask.Value.DefaultTransformChannelWeight;

            return math.select(
                weightMask.Value.DefaultTransformChannelWeight,
                weightMask.Value.TransformWeights[bindingIndex],
                !bindingInherited || weightMask.Value.TransformPropagateWeights[bindingIndex >> k_BitShiftPerField].IsSet(bindingIndex % k_BitsPerField));
        }

        [BurstCompile]
        static unsafe void CreateWeightMask(
            ref BlobAssetReference<RigDefinition> rigDefinition,
            ref BlobAssetReference<WeightMask> weightMask,
            ref BlobAssetReference<WeightMaskInstance> result,
            int allocator)
        {
            using var blobBuilder = new BlobBuilder(Allocator.Temp);

            ref var weightMaskInstance = ref blobBuilder.ConstructRoot<WeightMaskInstance>();
            weightMaskInstance.RigId = (ulong)rigDefinition.GetUnsafePtr();

            ref var rigBindings = ref rigDefinition.Value.Bindings;
            var weights = blobBuilder.Allocate(ref weightMaskInstance.Value, rigBindings.StreamMaskSize);
            var streamMask = AnimationStreamMask.Create(rigDefinition, (WeightData*)weights.GetUnsafePtr(), weights.Length);
            streamMask.Clear();

            var bindingCache = new NativeHashMap<StringHash, BindingCacheEntry>(rigDefinition.Value.Skeleton.BoneCount, Allocator.Temp);

            // Fill in TRS for transform weight masks.
            for (int i = 0; i < rigBindings.TranslationBindings.Length; ++i)
            {
                FindInheritedWeightIndex(
                    ref rigDefinition,
                    ref weightMask.Value.TransformBindings,
                    ref bindingCache,
                    rigBindings.TranslationBindings[i],
                    out var bindingIndex, out var bindingInherited);

                streamMask.SetTranslationWeight(i, GetTransformWeightValue(bindingIndex, bindingInherited, ref weightMask));
            }

            for (int i = 0; i < rigBindings.RotationBindings.Length; ++i)
            {
                FindInheritedWeightIndex(
                    ref rigDefinition,
                    ref weightMask.Value.TransformBindings,
                    ref bindingCache,
                    rigBindings.RotationBindings[i],
                    out var bindingIndex, out var bindingInherited);

                streamMask.SetRotationWeight(i, GetTransformWeightValue(bindingIndex, bindingInherited, ref weightMask));
            }

            for (int i = 0; i < rigBindings.ScaleBindings.Length; ++i)
            {
                FindInheritedWeightIndex(
                    ref rigDefinition,
                    ref weightMask.Value.TransformBindings,
                    ref bindingCache,
                    rigBindings.ScaleBindings[i],
                    out var bindingIndex, out var bindingInherited);

                streamMask.SetScaleWeight(i, GetTransformWeightValue(bindingIndex, bindingInherited, ref weightMask));
            }

            // Will override previous transform translation weight if an individual overlapping mask has been declared
            for (int i = 0, count = weightMask.Value.TranslationBindings.Length; i < count; ++i)
            {
                var rigBindingIndex = BindingUtility.FindBindingIndex(ref rigBindings.TranslationBindings, weightMask.Value.TranslationBindings[i]);
                if (rigBindingIndex != -1)
                    streamMask.SetTranslationWeight(rigBindingIndex, weightMask.Value.TranslationWeights[i]);
            }

            // Will override previous transform rotation weight if an individual overlapping mask has been declared
            for (int i = 0, count = weightMask.Value.RotationBindings.Length; i < count; ++i)
            {
                var rigBindingIndex = BindingUtility.FindBindingIndex(ref rigBindings.RotationBindings, weightMask.Value.RotationBindings[i]);
                if (rigBindingIndex != -1)
                    streamMask.SetRotationWeight(rigBindingIndex, weightMask.Value.RotationWeights[i]);
            }

            // Will override previous transform scale weight if an individual overlapping mask has been declared
            for (int i = 0, count = weightMask.Value.ScaleBindings.Length; i < count; ++i)
            {
                var rigBindingIndex = BindingUtility.FindBindingIndex(ref rigBindings.ScaleBindings, weightMask.Value.ScaleBindings[i]);
                if (rigBindingIndex != -1)
                    streamMask.SetScaleWeight(rigBindingIndex, weightMask.Value.ScaleWeights[i]);
            }

            // Fill in float values.
            for (int i = 0; i < rigBindings.FloatBindings.Length; ++i)
            {
                var bindingIndex = BindingUtility.FindBindingIndex(ref weightMask.Value.FloatBindings, rigBindings.FloatBindings[i]);
                streamMask.SetFloatWeight(i, bindingIndex != -1 ? weightMask.Value.FloatWeights[bindingIndex] : weightMask.Value.DefaultGenericChannelWeight);
            }

            // Fill in integer values.
            for (int i = 0; i < rigBindings.IntBindings.Length; ++i)
            {
                var bindingIndex = BindingUtility.FindBindingIndex(ref weightMask.Value.IntBindings, rigBindings.IntBindings[i]);
                streamMask.SetIntWeight(i, bindingIndex != -1 ? weightMask.Value.IntWeights[bindingIndex] : weightMask.Value.DefaultGenericChannelWeight);
            }

            // Fill in EntityId values
            for (int i = 0; i < rigBindings.EntityIdBindings.Length; ++i)
            {
                var bindingIndex = BindingUtility.FindBindingIndex(ref weightMask.Value.EntityIdBindings, rigBindings.EntityIdBindings[i]);
                streamMask.SetEntityIdWeight(i, bindingIndex != -1 ? weightMask.Value.EntityIdWeights[bindingIndex] : weightMask.Value.DefaultGenericChannelWeight);
            }

            result = blobBuilder.CreateBlobAssetReference<WeightMaskInstance>((Allocator)allocator);
            bindingCache.Dispose();
        }

        /// <summary>
        /// Creates a <see cref="WeightMaskInstance"/> blob asset reference from specified BlobAssetReference{RigDefinition}
        /// and BlobAssetReference{WeightMask}.
        /// </summary>
        /// <param name="rig">Rig definition</param>
        /// <param name="weightMask"><see cref="WeightMask"/> blob asset reference</param>
        /// <param name="allocator">The type of memory to allocate. Unless the asset has a very short life span, use
        /// <see cref="Allocator.Persistent"/>.</param>
        /// <returns>Returns a new BlobAssetReference{WeightMaskInstance}.</returns>
        [GenerateTestsForBurstCompatibility]
        internal static BlobAssetReference<WeightMaskInstance> Create(
            BlobAssetReference<RigDefinition> rig,
            BlobAssetReference<WeightMask> weightMask,
            Allocator allocator
        )
        {
            if (weightMask == default || rig == default)
                return default;

            using var marker = s_CreateMarker.Auto();
            BlobAssetReference<WeightMaskInstance> result = default;
            CreateWeightMask(ref rig, ref weightMask, ref result, (int)allocator);
            return result;
        }
    }
}
