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

            var boneNodes = CreateRootMotionBoneNodes(rigDefinition, out var boneNodePositions, out var invBindRotation);

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
                            SetRootMotionBoneTranslationCurves(boneNodes, boneNodePositions, rigBinding, curveIndex);
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
                            SetRootMotionBoneRotationCurves(boneNodes, boneNodePositions, rigBinding, curveIndex, -1);
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
                            SetRootMotionBoneRotationCurves(boneNodes, boneNodePositions, rigBinding, curveIndex, (int)rotationOrder);
                        }
                        curveIndex += 3;
                    }
                        break;
                    case (uint)BindType.kBindTransformScale:
                    {
                        var rigBinding = BindingUtility.FindBindingIndex(ref rigBindings.ScaleBindings, clipBinding.transformPathHash);
                        if (rigBinding != -1)
                        {
                            scaleBindings.Add(new ClipInstance.MappedBinding(curveIndex, rigBinding));
                            SetRootMotionBoneScaleCurves(boneNodes, boneNodePositions, rigBinding, curveIndex);
                        }
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

            using var muscleClipReference = sourceClip.ToClipReference();
            var muscleClip = muscleClipReference.AsBlobAssetReference();

            var source = SelectRootMotionSource(muscleClip, boneNodes);
            var rootMotionNodes = source == RootMotionSource.Bone ? boneNodes : CreateRootMotionMuscleNodes(muscleClip, source);
            boneNodePositions.Dispose();

            var nodes = blobBuilder.Allocate(ref clipInstance.m_RootMotionNodes, rootMotionNodes.Length);
            using var curveIndices = new NativeList<int>(Allocator.Temp);
            for (var i = 0; i < rootMotionNodes.Length; i++)
            {
                var node = rootMotionNodes[i];
                nodes[i] = node;
                for (var j = 0; j < 4; j++)
                {
                    AddCurveIndex(curveIndices, j < 3 ? node.TranslationCurves[j] : -1);
                    AddCurveIndex(curveIndices, node.RotationCurves[j]);
                    AddCurveIndex(curveIndices, j < 3 ? node.ScaleCurves[j] : -1);
                }
            }
            if (source != RootMotionSource.Bone)
                rootMotionNodes.Dispose();

            curveIndices.Sort();
            var curveIndicesBlob = blobBuilder.Allocate(ref clipInstance.m_RootMotionCurveIndices, curveIndices.Length);
            for (var i = 0; i < curveIndices.Length; i++)
                curveIndicesBlob[i] = curveIndices[i];

            result = blobBuilder.CreateBlobAssetReference<ClipInstance>((Allocator)allocator);

            var samplingData = Core.CreateRootMotionSamplingData(source, invBindRotation);
            if (source != RootMotionSource.None)
            {
                ComputeRootMotionSamplingDataBursted(ref muscleClip.Value, ref result.Value, ref samplingData);

                samplingData.ExtractsBone = AnimatesTranslationOrRotation(boneNodes) ? (byte)1 : (byte)0;
                if (samplingData.ExtractsBone != 0 && muscleClip.Value.m_LoopBlend != 0)
                    ComputeLoopPoseBones(rigDefinition, ref muscleClip.Value, boneNodes, ref samplingData);
            }

            boneNodes.Dispose();

            result.Value.RootMotionSamplingData = samplingData;
        }

        // The root motion bone followed by its parents up to the root bone, with the default values of the rig
        static NativeList<RootMotionNode> CreateRootMotionBoneNodes(BlobAssetReference<RigDefinition> rigDefinition, out NativeArray<int> nodePositions, out quaternion invBindRotation)
        {
            ref var skeleton = ref rigDefinition.Value.Skeleton;
            var nodes = new NativeList<RootMotionNode>(Allocator.Temp);
            nodePositions = new NativeArray<int>(skeleton.BoneCount, Allocator.Temp);
            for (var i = 0; i < nodePositions.Length; i++)
                nodePositions[i] = -1;

            invBindRotation = quaternion.identity;
            var boneIndex = rigDefinition.Value.RootMotionBoneIndex;
            if (boneIndex < 0 || boneIndex >= skeleton.BoneCount)
                return nodes;

            var defaultStream = AnimationStream.FromDefaultValues(rigDefinition);
            for (var i = boneIndex; i != -1; i = skeleton.ParentIndexes[i])
            {
                // Like Mecanim with the curves of the Animator transform, a missing curve of the root bone is identity
                var defaultValue = boneIndex == 0 ? TRS.identity : defaultStream.GetLocalToParentTRS(i);
                nodePositions[i] = nodes.Length;
                nodes.Add(RootMotionNode.Create(defaultValue.t, defaultValue.r, defaultValue.s));
            }

            // Relative to the root bone, whose default rotation is its placement in the scene and not a bind rotation
            var bindRotation = quaternion.identity;
            if (nodes.Length > 1)
            {
                bindRotation = nodes[0].DefaultRotation;
                for (var i = 1; i < nodes.Length - 1; i++)
                    bindRotation = math.mul(nodes[i].DefaultRotation, MathUtility.scaleMulQuat(nodes[i].DefaultScale, bindRotation));
            }
            invBindRotation = math.conjugate(math.normalize(bindRotation));

            return nodes;
        }

        static void AddCurveIndex(NativeList<int> curveIndices, int curveIndex)
        {
            if (curveIndex >= 0 && !curveIndices.Contains(curveIndex))
                curveIndices.Add(curveIndex);
        }

        // A bone of the skeleton has the index of its translation, rotation and scale channels
        static void SetRootMotionBoneTranslationCurves(NativeList<RootMotionNode> nodes, NativeArray<int> nodePositions, int rigBinding, int curveIndex)
        {
            if (rigBinding < nodePositions.Length && nodePositions[rigBinding] >= 0)
                nodes.ElementAt(nodePositions[rigBinding]).TranslationCurves = new int3(curveIndex, curveIndex + 1, curveIndex + 2);
        }

        static void SetRootMotionBoneScaleCurves(NativeList<RootMotionNode> nodes, NativeArray<int> nodePositions, int rigBinding, int curveIndex)
        {
            if (rigBinding < nodePositions.Length && nodePositions[rigBinding] >= 0)
                nodes.ElementAt(nodePositions[rigBinding]).ScaleCurves = new int3(curveIndex, curveIndex + 1, curveIndex + 2);
        }

        // A quaternion curve wins over Euler curves on the same bone, as for the pose
        static void SetRootMotionBoneRotationCurves(NativeList<RootMotionNode> nodes, NativeArray<int> nodePositions, int rigBinding, int curveIndex, int eulerOrder)
        {
            if (rigBinding >= nodePositions.Length || nodePositions[rigBinding] < 0)
                return;

            ref var node = ref nodes.ElementAt(nodePositions[rigBinding]);
            var hasQuaternionCurves = node.RotationCurves.x >= 0 && node.RotationEulerOrder < 0;
            if (eulerOrder >= 0 && hasQuaternionCurves)
                return;

            node.RotationCurves = new int4(curveIndex, curveIndex + 1, curveIndex + 2, eulerOrder >= 0 ? -1 : curveIndex + 3);
            node.RotationEulerOrder = eulerOrder;
        }

        // Clip.m_IndexArray holds the curve index of each muscle curve, or -1. The Motion curves come first, then the
        // Root curves, each as translation x, y, z then quaternion x, y, z, w (clipmuscle.h)
        internal const int k_MotionCurvesBegin = 0;
        internal const int k_RootCurvesBegin = 7;
        const int k_MuscleTransformCurveCount = 7;

        static unsafe bool HasMuscleTransformCurves(ref Clip clip, int begin)
        {
            for (var i = 0; i < k_MuscleTransformCurveCount; i++)
            {
                if (clip.m_IndexArray[begin + i] != -1)
                    return true;
            }

            return false;
        }

        static bool AnimatesTranslationOrRotation(NativeList<RootMotionNode> boneNodes)
        {
            for (var i = 0; i < boneNodes.Length; i++)
            {
                if (boneNodes[i].HasTranslationOrRotationCurves)
                    return true;
            }

            return false;
        }

        // The root motion bone extracted from the start and stop values of the clip, as Mecanim's ComputeRootMotionValues
        // does before it applies Loop Pose
        static unsafe void ComputeLoopPoseBones(BlobAssetReference<RigDefinition> rigDefinition, ref Clip clip, NativeList<RootMotionNode> boneNodes, ref Core.ClipRootMotionSamplingData samplingData)
        {
            var deltas = (ValueDelta*)clip.m_ValueArrayDelta.Get();
            if (deltas == null)
                return;

            var stream = AnimationStream.Create(rigDefinition, Allocator.Temp);
            try
            {
                samplingData.LoopStartBone = ExtractBoneFromClipValues(ref stream, deltas, boneNodes, samplingData.StartX, true);
                stream.ResetToDefaultValues();
                samplingData.LoopStopBone = ExtractBoneFromClipValues(ref stream, deltas, boneNodes, samplingData.StopX, false);
            }
            finally
            {
                stream.Dispose();
            }
        }

        // boneNodes holds the root motion bone followed by its parents. Components without a curve keep the default value
        // of the rig, as in the sampled pose
        static unsafe TRS ExtractBoneFromClipValues(ref AnimationStream stream, ValueDelta* deltas, NativeList<RootMotionNode> boneNodes, RigidTransform rootX, bool start)
        {
            ref var skeleton = ref stream.Rig.Skeleton;
            var boneIndex = stream.Rig.RootMotionBoneIndex;
            var index = boneIndex;
            for (var i = 0; i < boneNodes.Length; i++, index = skeleton.ParentIndexes[index])
            {
                var node = boneNodes[i];

                var translation = stream.GetLocalToParentTranslation(index);
                for (var j = 0; j < 3; j++)
                {
                    if (node.TranslationCurves[j] >= 0)
                        translation[j] = LoadClipValue(deltas, node.TranslationCurves[j], start);
                }
                stream.SetLocalToParentTranslation(index, translation);

                if (node.RotationEulerOrder >= 0)
                {
                    var euler = float3.zero;
                    for (var j = 0; j < 3; j++)
                    {
                        if (node.RotationCurves[j] >= 0)
                            euler[j] = LoadClipValue(deltas, node.RotationCurves[j], start);
                    }
                    stream.SetLocalToParentRotation(index, quaternion.Euler(math.radians(euler), (math.RotationOrder)node.RotationEulerOrder));
                }
                else if (math.any(node.RotationCurves >= 0))
                {
                    var rotation = stream.GetLocalToParentRotation(index).value;
                    for (var j = 0; j < 4; j++)
                    {
                        if (node.RotationCurves[j] >= 0)
                            rotation[j] = LoadClipValue(deltas, node.RotationCurves[j], start);
                    }
                    stream.SetLocalToParentRotation(index, math.normalizesafe(new quaternion(rotation), quaternion.identity));
                }

                var scale = stream.GetLocalToParentScale(index);
                for (var j = 0; j < 3; j++)
                {
                    if (node.ScaleCurves[j] >= 0)
                        scale[j] = LoadClipValue(deltas, node.ScaleCurves[j], start);
                }
                stream.SetLocalToParentScale(index, scale);
            }

            stream.ExtractRootMotionBone(rootX.pos, rootX.rot);
            return stream.GetLocalToParentTRS(boneIndex);
        }

        static unsafe float LoadClipValue(ValueDelta* deltas, int curveIndex, bool start) =>
            start ? deltas[curveIndex].m_Start : deltas[curveIndex].m_Stop;

        static RootMotionSource SelectRootMotionSource(BlobAssetReference<Clip> muscleClip, NativeList<RootMotionNode> boneNodes)
        {
            if (!muscleClip.IsCreated || muscleClip.Value.m_Clip.IsNull)
                return RootMotionSource.None;

            if (HasMuscleTransformCurves(ref muscleClip.Value, k_MotionCurvesBegin))
                return RootMotionSource.Motion;

            if (HasMuscleTransformCurves(ref muscleClip.Value, k_RootCurvesBegin))
                return RootMotionSource.Root;

            if (AnimatesTranslationOrRotation(boneNodes))
                return RootMotionSource.Bone;

            return RootMotionSource.None;
        }

        // Missing Motion or Root curves default to a zero translation and an identity rotation, as in GetClipX
        static unsafe NativeList<RootMotionNode> CreateRootMotionMuscleNodes(BlobAssetReference<Clip> muscleClip, RootMotionSource source)
        {
            var nodes = new NativeList<RootMotionNode>(1, Allocator.Temp);
            if (source != RootMotionSource.Motion && source != RootMotionSource.Root)
                return nodes;

            var begin = source == RootMotionSource.Motion ? k_MotionCurvesBegin : k_RootCurvesBegin;
            var node = RootMotionNode.Create(float3.zero, quaternion.identity, new float3(1f));
            ref var clip = ref muscleClip.Value;
            node.TranslationCurves = new int3(clip.m_IndexArray[begin], clip.m_IndexArray[begin + 1], clip.m_IndexArray[begin + 2]);
            node.RotationCurves = new int4(clip.m_IndexArray[begin + 3], clip.m_IndexArray[begin + 4], clip.m_IndexArray[begin + 5], clip.m_IndexArray[begin + 6]);

            nodes.Add(node);
            return nodes;
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

        [BurstCompile]
        static void ComputeRootMotionSamplingDataBursted(ref Clip muscleClip, ref ClipInstance clipInstance, ref Core.ClipRootMotionSamplingData samplingData)
        {
            var cache = new ClipSamplingCache(0, Allocator.Temp);
            try
            {
                cache.InitializeRootMotion(ref clipInstance);

                var startX = Core.SampleRootMotionSourceTransform(ref muscleClip, ref clipInstance, ref cache.Memory, muscleClip.m_StartTime);
                var stopX = Core.SampleRootMotionSourceTransform(ref muscleClip, ref clipInstance, ref cache.Memory, muscleClip.m_StopTime);

                if (samplingData.Source == RootMotionSource.Root)
                    Core.InitializeRootSettings(ref samplingData, ref muscleClip, startX);

                startX = Core.ProcessRootMotionTransform(in samplingData, startX);
                stopX = Core.ProcessRootMotionTransform(in samplingData, stopX);

                samplingData.StartX = startX;
                samplingData.StopX = stopX;
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
