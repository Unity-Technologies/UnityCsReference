// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;

namespace Unity.Animations
{
    enum BindType
    {
        kUnbound                            = 0,

        // Builtin transform bindings
        kBindTransformPosition              = 1,// This enum may not be changed. It is used in GenericClipBinding.
        kBindTransformRotation              = 2,// This enum may not be changed.  It is used in GenericClipBinding.
        kBindTransformScale                 = 3,// It is used in GenericClipBinding.
        kBindTransformEuler                 = 4,

        // Builtin float bindings
        kMinSinglePropertyBinding           = 5,
        kBindFloat                          = 5,
        kBindFloatToBool                    = 6,
        kBindGameObjectActive               = 7,
        kBindMuscle                         = 8,
        kBindScriptObjectReference          = 9,
        kBindFloatToInt                     = 10,
        kBindDiscreteInt                    = 11,

        // Custom bindings
        kMinCustomBinding                   = 20,

        kBlendShapeWeightBinding            = 20,
        kRendererMaterialPPtrBinding        = 21,
        kRendererMaterialPropertyBinding    = 22,
        kSpriteRendererPPtrBinding          = 23,
        kMonoBehaviourPropertyBinding       = 24,
        kLightPropertyBinding               = 25,
        kRendererOtherPropertyBinding       = 26,
        kParticleSystemPropertyBindings     = 27,
        kRectTransformPropertyBindings      = 28,
        kLineRendererPropertyBindings       = 29,
        kTrailRendererPropertyBindings      = 30,
        kPositionConstraintPropertyBindings = 31,
        kRotationConstraintPropertyBindings = 32,
        kScaleConstraintPropertyBindings    = 33,
        kAimConstraintPropertyBindings      = 34,
        kParentConstraintPropertyBindings   = 35,
        kLookAtConstraintPropertyBindings   = 36,
        kCameraPropertyBindings             = 37,
        kVisualEffectPropertyBindings       = 38,
        kParticleForceFieldPropertyBinding  = 39,
        kUserDefinedBinding                 = 40,
        kMeshFilterBinding                  = 41,
        kUIToolkitBinding                   = 42,

        kAllBindingCount
    };

    [GenerateTestsForBurstCompatibility]
    [BurstCompile]
    internal partial struct ClipInstance
    {
        // Persistent type ID of Transform (GenericBinding::IsTransform)
        const int k_TransformTypeID = 4;

        // A property that two components on one GameObject share by name has a channel for each component, with an id
        // that includes the component. The other properties have the id of their path and name. A rig that was built
        // without the other component has only that id, which then is the one to match.
        static int FindPropertyBindingIndex(ref BlobArray<StringHash> rigBindings, GenericBinding clipBinding)
        {
            return BindingUtility.FindBindingIndex(ref rigBindings, clipBinding);
        }

        static NativeArray<GenericBinding> GetGenericBindings(AnimationClip clip, Allocator allocator)
        {
            var bindings = new NativeArray<GenericBinding>(clip.GetGenericBindingCount(), allocator);
            for (var i = 0; i < bindings.Length; i++)
            {
                bindings[i] = clip.GetGenericBinding(i);
            }
            return bindings;
        }

        static readonly Profiling.ProfilerMarker s_CreateMarker = new Profiling.ProfilerMarker(nameof(ClipInstance) + "." + nameof(Create));

        unsafe static void Build(ref BlobAssetReference<RigDefinition> rigDefinition, AnimationClip sourceClip, ref BlobAssetReference<ClipInstance> result, int allocator)
        {
            using var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var clipInstance = ref blobBuilder.ConstructRoot<ClipInstance>();

            ref var rigBindings = ref rigDefinition.Value.Bindings;
            ref var info = ref clipInstance.m_BindingInfo;

            var clipBindings = GetGenericBindings(sourceClip, Allocator.Temp);
            var pptrIds = sourceClip.GetPPtrCurveMappingEntityIds(Allocator.Temp);

            clipInstance.RigId = (ulong)rigDefinition.GetUnsafePtr();

            var translationBindings = new NativeList<ClipInstance.MappedBinding>(rigBindings.BindingCount, Allocator.Temp);
            var rotationBindings = new NativeList<ClipInstance.MappedBinding>(rigBindings.BindingCount, Allocator.Temp);
            var scaleBindings = new NativeList<ClipInstance.MappedBinding>(rigBindings.BindingCount, Allocator.Temp);
            var intBindings = new NativeList<ClipInstance.MappedBinding>(rigBindings.BindingCount, Allocator.Temp);
            var floatBindings = new NativeList<ClipInstance.MappedBinding>(rigBindings.BindingCount, Allocator.Temp);
            var objectBindings = new NativeList<ClipInstance.MappedBinding>(rigBindings.BindingCount, Allocator.Temp);
            var eulerBindings = new NativeList<ClipInstance.MappedBinding>(rigBindings.BindingCount, Allocator.Temp);
            var eulerRotationOrders = new NativeList<math.RotationOrder>(rigBindings.BindingCount, Allocator.Temp);

            var rootTranslationCurveIndex = -1;
            var rootRotationCurveIndex = -1;
            var rootRotationEulerOrder = -1;

            var curveIndex = 0;
            for (var i = 0; i < clipBindings.Length; ++i)
            {
                var clipBinding = clipBindings[i];

                // Custom bindings, such as RectTransform or material properties, use small attribute values that
                // collide with the transform bind types
                var bindType = clipBinding.propertyNameHash;
                if (bindType <= (uint)BindType.kBindTransformEuler && clipBinding.typeID != k_TransformTypeID)
                    bindType = (uint)BindType.kUnbound;

                switch (bindType)
                {
                    case (uint)BindType.kBindTransformPosition:
                    {
                        var rigBinding = BindingUtility.FindBindingIndex(ref rigBindings.TranslationBindings, clipBinding.transformPathHash);
                        if (rigBinding != -1)
                        {
                            translationBindings.Add(new ClipInstance.MappedBinding(curveIndex, rigBinding));
                            if (rigBinding == 0)
                                rootTranslationCurveIndex = curveIndex;
                        }
                        curveIndex += 3;
                    }
                        break;
                    case (uint)BindType.kBindTransformRotation:
                    {
                        var rigBinding = BindingUtility.FindBindingIndex(ref rigBindings.RotationBindings, clipBinding.transformPathHash);
                        if (rigBinding != -1)
                        {
                            rotationBindings.Add(new ClipInstance.MappedBinding(curveIndex, rigBinding));
                            if (rigBinding == 0)
                            {
                                // A quaternion curve wins over Euler curves on the same bone, as for the pose
                                rootRotationCurveIndex = curveIndex;
                                rootRotationEulerOrder = -1;
                            }
                        }
                        curveIndex += 4;
                    }
                        break;
                    case (uint)BindType.kBindTransformEuler:
                    {
                        var rigBinding = BindingUtility.FindBindingIndex(ref rigBindings.RotationBindings, clipBinding.transformPathHash);
                        if (rigBinding != -1)
                        {
                            eulerBindings.Add(new ClipInstance.MappedBinding(curveIndex, rigBinding));
                            var rotationOrder = ToRotationOrder(clipBinding.customTypeID);
                            eulerRotationOrders.Add(rotationOrder);
                            if (rigBinding == 0 && rootRotationCurveIndex < 0)
                            {
                                rootRotationCurveIndex = curveIndex;
                                rootRotationEulerOrder = (int)rotationOrder;
                            }
                        }
                        curveIndex += 3;
                    }
                        break;
                    case (uint)BindType.kBindTransformScale:
                    {
                        var rigBinding = BindingUtility.FindBindingIndex(ref rigBindings.ScaleBindings, clipBinding.transformPathHash);
                        if (rigBinding != -1)
                            scaleBindings.Add(new ClipInstance.MappedBinding(curveIndex, rigBinding));
                        curveIndex += 3;
                    }
                        break;
                    case (uint)BindType.kBindFloat:
                    {
                        var rigBinding = FindPropertyBindingIndex(ref rigBindings.FloatBindings, clipBinding);
                        if (rigBinding != -1)
                            floatBindings.Add(new ClipInstance.MappedBinding(curveIndex, rigBinding));
                        curveIndex += 1;
                    }
                        break;
                    case (uint)BindType.kBindDiscreteInt:
                    {
                        var rigBinding = FindPropertyBindingIndex(ref rigBindings.IntBindings, clipBinding);
                        if (rigBinding != -1)
                            intBindings.Add(new ClipInstance.MappedBinding(curveIndex, rigBinding));
                        curveIndex += 1;
                    }
                        break;
                    case (uint)BindType.kBindScriptObjectReference:
                    {
                        var rigBinding = FindPropertyBindingIndex(ref rigBindings.EntityIdBindings, clipBinding);
                        if (rigBinding != -1)
                            objectBindings.Add(new ClipInstance.MappedBinding(curveIndex, rigBinding));
                        curveIndex += 1;
                    }
                        break;
                    default:
                    {
                        if (clipBinding.isObjectReference)
                        {
                            var rigBinding = FindPropertyBindingIndex(ref rigBindings.EntityIdBindings, clipBinding);
                            if (rigBinding != -1)
                                objectBindings.Add(new ClipInstance.MappedBinding(curveIndex, rigBinding));
                            curveIndex += 1;
                        }
                        else if (clipBinding.isDiscrete)
                        {
                            var rigBinding = FindPropertyBindingIndex(ref rigBindings.IntBindings, clipBinding);
                            if (rigBinding != -1)
                                intBindings.Add(new ClipInstance.MappedBinding(curveIndex, rigBinding));
                            curveIndex += 1;
                        }
                        else
                        {
                            var rigBinding = FindPropertyBindingIndex(ref rigBindings.FloatBindings, clipBinding);
                            if (rigBinding != -1)
                                floatBindings.Add(new ClipInstance.MappedBinding(curveIndex, rigBinding));
                            curveIndex += 1;
                        }
                    }
                        break;

                }
            }

            // SortBindingsByChunk(ref translationBindings, 0, translationBindings.Length);
            // SortBindingsByChunk(ref rotationBindings, 0, rotationBindings.Length);

            info.ScaleOffset = translationBindings.Length;
            info.FloatOffset = info.ScaleOffset + scaleBindings.Length;
            info.IntOffset = info.FloatOffset + floatBindings.Length;
            info.EntityIdOffset = info.IntOffset + intBindings.Length;
            info.RotationOffset = info.EntityIdOffset + objectBindings.Length;
            info.EulerRotationOffset = info.RotationOffset + rotationBindings.Length;

            var bindingCounts = translationBindings.Length + rotationBindings.Length + scaleBindings.Length + floatBindings.Length + intBindings.Length + objectBindings.Length + eulerBindings.Length;

            clipBindings.Dispose();

            var bindingMap = blobBuilder.Allocate(ref clipInstance.m_BindingMap, bindingCounts);
            UnsafeUtility.MemCpy(bindingMap.GetUnsafePtr(), translationBindings.GetUnsafePtr(), UnsafeUtility.SizeOf<ClipInstance.MappedBinding>() * translationBindings.Length);
            UnsafeUtility.MemCpy((ClipInstance.MappedBinding*)bindingMap.GetUnsafePtr() + info.RotationOffset, rotationBindings.GetUnsafePtr(), UnsafeUtility.SizeOf<ClipInstance.MappedBinding>() * rotationBindings.Length);
            UnsafeUtility.MemCpy((ClipInstance.MappedBinding*)bindingMap.GetUnsafePtr() + info.ScaleOffset, scaleBindings.GetUnsafePtr(), UnsafeUtility.SizeOf<ClipInstance.MappedBinding>() * scaleBindings.Length);
            UnsafeUtility.MemCpy((ClipInstance.MappedBinding*)bindingMap.GetUnsafePtr() + info.IntOffset, intBindings.GetUnsafePtr(), UnsafeUtility.SizeOf<ClipInstance.MappedBinding>() * intBindings.Length);
            UnsafeUtility.MemCpy((ClipInstance.MappedBinding*)bindingMap.GetUnsafePtr() + info.FloatOffset, floatBindings.GetUnsafePtr(), UnsafeUtility.SizeOf<ClipInstance.MappedBinding>() * floatBindings.Length);
            UnsafeUtility.MemCpy((ClipInstance.MappedBinding*)bindingMap.GetUnsafePtr() + info.EntityIdOffset, objectBindings.GetUnsafePtr(), UnsafeUtility.SizeOf<ClipInstance.MappedBinding>() * objectBindings.Length);
            UnsafeUtility.MemCpy((ClipInstance.MappedBinding*)bindingMap.GetUnsafePtr() + info.EulerRotationOffset, eulerBindings.GetUnsafePtr(), UnsafeUtility.SizeOf<ClipInstance.MappedBinding>() * eulerBindings.Length);

            var rotationOrders = blobBuilder.Allocate(ref clipInstance.m_EulerRotationOrders, eulerRotationOrders.Length);
            UnsafeUtility.MemCpy(rotationOrders.GetUnsafePtr(), eulerRotationOrders.GetUnsafePtr(), UnsafeUtility.SizeOf<math.RotationOrder>() * eulerRotationOrders.Length);

            translationBindings.Dispose();
            rotationBindings.Dispose();
            scaleBindings.Dispose();
            intBindings.Dispose();
            floatBindings.Dispose();
            objectBindings.Dispose();
            eulerBindings.Dispose();
            eulerRotationOrders.Dispose();

            // Mecanim PPtr curves store 32-bit indices, not EntityIds. Grab the flat mapping
            // table from the native clip and stash it in the instance so sample-time code can
            // dereference the index into a real EntityId.
            var pptrBlob = blobBuilder.Allocate(ref clipInstance.m_PPtrEntityIds, pptrIds.Length);
            if (pptrIds.Length > 0)
            {
                UnsafeUtility.MemCpy(
                    pptrBlob.GetUnsafePtr(),
                    pptrIds.GetUnsafeReadOnlyPtr(),
                    UnsafeUtility.SizeOf<EntityId>() * pptrIds.Length);
            }

            pptrIds.Dispose();

            result = blobBuilder.CreateBlobAssetReference<ClipInstance>((Allocator)allocator);

            using (var muscleObjRef = sourceClip.ToClipReference())
            {
                result.Value.RootMotionSamplingData = ComputeRootMotionSamplingData(
                    muscleObjRef.AsBlobAssetReference(),
                    rootTranslationCurveIndex,
                    rootRotationCurveIndex,
                    rootRotationEulerOrder);
            }
        }

        // Mecanim stores the Euler order as math::RotationOrder, which swaps YZX and YXZ compared to Unity.Mathematics
        static math.RotationOrder ToRotationOrder(byte mecanimRotationOrder)
        {
            switch (mecanimRotationOrder)
            {
                case 0: return math.RotationOrder.XYZ;
                case 1: return math.RotationOrder.XZY;
                case 2: return math.RotationOrder.YZX;
                case 3: return math.RotationOrder.YXZ;
                case 4: return math.RotationOrder.ZXY;
                case 5: return math.RotationOrder.ZYX;
                default: return math.RotationOrder.Default;
            }
        }

        static Core.ClipRootMotionSamplingData ComputeRootMotionSamplingData(BlobAssetReference<Clip> muscleClip, int rootTranslationCurveIndex, int rootRotationCurveIndex, int rootRotationEulerOrder)
        {
            var samplingData = new Core.ClipRootMotionSamplingData()
            {
                RootTranslationIndex = rootTranslationCurveIndex,
                RootRotationIndex = rootRotationCurveIndex,
                RootRotationEulerOrder = rootRotationEulerOrder,
                InvStartX = RigidTransform.identity,
                TotalX = RigidTransform.identity
            };

            if (rootTranslationCurveIndex < 0 && rootRotationCurveIndex < 0)
                return samplingData;

            if (!muscleClip.IsCreated)
                return samplingData;

            ComputeRootMotionSamplingDataBursted(ref muscleClip.Value, rootTranslationCurveIndex, rootRotationCurveIndex, rootRotationEulerOrder, ref samplingData);
            return samplingData;
        }

        [BurstCompile]
        static void ComputeRootMotionSamplingDataBursted(ref Clip muscleClip, int rootTranslationCurveIndex, int rootRotationCurveIndex, int rootRotationEulerOrder, ref Core.ClipRootMotionSamplingData samplingData)
        {
            var cache = new ClipSamplingCache(0, Allocator.Temp);
            try
            {
                cache.InitializeRootMotion(rootTranslationCurveIndex, rootRotationCurveIndex);

                var startX = RigidTransform.identity;
                Core.SampleAnimationClipRootTransformBursted(ref muscleClip,
                    rootTranslationCurveIndex, rootRotationCurveIndex, rootRotationEulerOrder, ref cache.Memory, muscleClip.m_StartTime, ref startX);
                var stopX = RigidTransform.identity;
                Core.SampleAnimationClipRootTransformBursted(ref muscleClip,
                    rootTranslationCurveIndex, rootRotationCurveIndex, rootRotationEulerOrder, ref cache.Memory, muscleClip.m_StopTime, ref stopX);

                samplingData.InvStartX = math.inverse(startX);
                samplingData.TotalX = math.mul(samplingData.InvStartX, stopX);
            }
            finally
            {
                cache.Dispose();
            }
        }

        static unsafe int AppendMatchingBindings(
            ref NativeList<ClipInstance.MappedBinding> instanceBindings,
            ref BlobArray<StringHash> rigBindings,
            ref BlobArray<StringHash> clipBindings,
            out ushort chunkCount
        )
        {
            var start = instanceBindings.Length;

            // Add instances bindings sorted by clipBindings index.
            for (var i = 0; i < clipBindings.Length; ++i)
            {
                var rigBinding = BindingUtility.FindBindingIndex(ref rigBindings, clipBindings[i]);
                if (rigBinding != -1)
                    instanceBindings.Add(new ClipInstance.MappedBinding(i, rigBinding));
            }
            var end = instanceBindings.Length;

            // Sort newly added bindings
            chunkCount = SortBindingsByChunk(ref instanceBindings, start, end);

            return end;
        }

        static unsafe ushort SortBindingsByChunk(ref NativeList<ClipInstance.MappedBinding> instanceBindings, int start, int end)
        {
            ushort chunkCount = 0;
            if (start + ClipInstance.k_BindingsInChunk > end)
                return chunkCount;

            // Move consecutive binding chunks at the start of the array while
            // remaining isolated bindings will be moved to the end
            var instances = instanceBindings.AsArray().GetSubArray(start, end - start);
            using var tmpChunks = new NativeArray<ClipInstance.MappedBinding>(instances.Length, Allocator.Temp);
            using var tmpSingle = new NativeList<ClipInstance.MappedBinding>(32, Allocator.Temp);

            int i = 0;
            while (i < instances.Length)
            {
                if (AreNextBindingsConsecutive(ref instances, i))
                {
                    UnsafeUtility.MemCpy(
                        (ClipInstance.MappedBinding*)tmpChunks.GetUnsafePtr() + chunkCount * ClipInstance.k_BindingsInChunk,
                        (ClipInstance.MappedBinding*)instances.GetUnsafeReadOnlyPtr() + i,
                        UnsafeUtility.SizeOf<ClipInstance.MappedBinding>() * ClipInstance.k_BindingsInChunk);

                    i += ClipInstance.k_BindingsInChunk;
                    chunkCount++;
                }
                else
                {
                    tmpSingle.Add(instances[i]);
                    i++;
                }
            }

            UnsafeUtility.MemCpy(
                instances.GetUnsafePtr(),
                tmpChunks.GetUnsafeReadOnlyPtr(),
                UnsafeUtility.SizeOf<ClipInstance.MappedBinding>() * chunkCount * ClipInstance.k_BindingsInChunk);

            UnsafeUtility.MemCpy(
                (ClipInstance.MappedBinding*)instances.GetUnsafePtr() + chunkCount * ClipInstance.k_BindingsInChunk,
                tmpSingle.GetUnsafeReadOnlyPtr(),
                UnsafeUtility.SizeOf<ClipInstance.MappedBinding>() * tmpSingle.Length);

            return chunkCount;
        }

        static bool AreNextBindingsConsecutive(ref NativeArray<ClipInstance.MappedBinding> bindings, int start)
        {
            var maxIndex = start + ClipInstance.k_BindingsInChunk;
            if (maxIndex > bindings.Length)
                return false;

            var test = bindings[start].ClipIndex;
            for (int i = start + 1; i < maxIndex; ++i)
                if (bindings[i].ClipIndex != ++test)
                    return false;

            return true;
        }

        /// <summary>
        /// Creates a <see cref="ClipInstance"/> blob asset reference from specified BlobAssetReference{RigDefinition} and Animation Clip.
        /// </summary>
        /// <param name="rig">Rig definition</param>
        /// <param name="clip">Animation Clip</param>
        /// <param name="allocator">The type of memory to allocate. Unless the asset has a very short life span, use
        /// <see cref="Allocator.Persistent"/>.</param>
        /// <returns>Returns a new BlobAssetReference{ClipInstance}.</returns>
        [GenerateTestsForBurstCompatibility]
        public static BlobAssetReference<ClipInstance> Create(
            BlobAssetReference<RigDefinition> rig,
            AnimationClip clip,
            Allocator allocator
        )
        {
            if (clip == default || rig == default)
                return default;

            using var marker = s_CreateMarker.Auto();
            BlobAssetReference<ClipInstance> result = default;
            Build(ref rig, clip, ref result, (int)allocator);

            return result;
        }
    }
}
