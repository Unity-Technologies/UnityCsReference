// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using Unity.IntegerTime;
using UnityEngine;

namespace Unity.Animations
{
    // The curves that give the root motion of a clip, in the order of priority of Mecanim (clipmuscle.cpp)
    internal enum RootMotionSource : byte
    {
        None,
        Motion,
        Root,
        Bone,
    }

    [Flags]
    internal enum RootMotionLoopBlend : byte
    {
        None = 0,
        Orientation = 1 << 0,
        PositionY = 1 << 1,
        PositionXZ = 1 << 2,
    }

    // The curves of the translation, rotation and scale of one transform. A curve index is -1 if the clip has no curve
    // for that component, which then takes its default value
    internal struct RootMotionNode
    {
        public int3 TranslationCurves;

        // Quaternion x, y, z, w, or Euler x, y, z in degrees when RotationEulerOrder isn't -1
        public int4 RotationCurves;
        public int3 ScaleCurves;

        // The math.RotationOrder of Euler rotation curves, or -1 for quaternion curves
        public int RotationEulerOrder;

        public float3 DefaultTranslation;
        public quaternion DefaultRotation;
        public float3 DefaultScale;

        internal static RootMotionNode Create(float3 defaultTranslation, quaternion defaultRotation, float3 defaultScale)
        {
            return new RootMotionNode
            {
                TranslationCurves = new int3(-1),
                RotationCurves = new int4(-1),
                ScaleCurves = new int3(-1),
                RotationEulerOrder = -1,
                DefaultTranslation = defaultTranslation,
                DefaultRotation = defaultRotation,
                DefaultScale = defaultScale,
            };
        }

        internal readonly bool HasTranslationOrRotationCurves =>
            math.any(TranslationCurves >= 0) || math.any(RotationCurves >= 0);
    }

    [GenerateTestsForBurstCompatibility]
    static public partial class Core
    {
        /// <summary>
        /// Clip necessary pre baked data to sample root motion during runtime.
        /// </summary>
        internal struct ClipRootMotionSamplingData
        {
            public RootMotionSource Source;
            public RootMotionLoopBlend LoopBlend;

            // Applied on the right of the rotation of the Bone source. Mecanim takes MotionQ as is, and removes the bind
            // rotation from RootQ at import
            public quaternion InvBindRotation;

            // Settings of the Root source: Root Transform Position (Y) Offset, Root Transform Rotation Offset and
            // Bake Into Pose (KeepOriginal*), and the start transform that the LoopBlend settings keep
            public float3 OffsetTranslation;
            public quaternion OffsetRotation;
            public RigidTransform LoopStartX;

            public RigidTransform InvStartX;
            public RigidTransform TotalX;

            // The root transforms at the start and stop of the clip, after the settings of the source (Mecanim's
            // motionXRef.startX and stopX)
            public RigidTransform StartX;
            public RigidTransform StopX;

            // Whether the clip animates the translation or rotation of the root motion bone or of one of its parents, so
            // that the pose moves the bone with the root transform. Mecanim only adjusts the pose for an Avatar with a
            // root node. A byte, because a bool changes the layout of the blob between Mono and Burst
            public byte ExtractsBone;

            // The root motion bone with the root motion removed, from the start and stop values of the clip. Loop Pose
            // uses them for that bone, because Mecanim applies Loop Pose after AdjustPoseForMotion
            public TRS LoopStartBone;
            public TRS LoopStopBone;

            public readonly bool HasRootMotion => Source != RootMotionSource.None;
        }

        internal static ClipRootMotionSamplingData CreateRootMotionSamplingData(RootMotionSource source, quaternion invBindRotation)
        {
            return new ClipRootMotionSamplingData
            {
                Source = source,
                InvBindRotation = invBindRotation,
                OffsetTranslation = float3.zero,
                OffsetRotation = quaternion.identity,
                LoopStartX = RigidTransform.identity,
                InvStartX = RigidTransform.identity,
                TotalX = RigidTransform.identity,
                StartX = RigidTransform.identity,
                StopX = RigidTransform.identity,
                LoopStartBone = TRS.identity,
                LoopStopBone = TRS.identity,
            };
        }

        // Port of the start transform handling of the root curves in mecanim::animation::EvaluateRootMotion
        internal static void InitializeRootSettings(ref ClipRootMotionSamplingData samplingData, ref Clip clip, RigidTransform startX)
        {
            startX.rot = ProjectOnYPlane(startX.rot);

            var offsetT = new float3(0f, clip.m_Level, 0f);
            var offsetQ = quaternion.RotateY(math.radians(clip.m_OrientationOffsetY));

            if (clip.m_KeepOriginalPositionY != 0)
                offsetT.y -= startX.pos.y;

            if (clip.m_KeepOriginalPositionXZ != 0)
            {
                offsetT.x -= startX.pos.x;
                offsetT.z -= startX.pos.z;
            }

            if (clip.m_KeepOriginalOrientation != 0)
                offsetQ = math.normalize(math.mul(offsetQ, math.conjugate(startX.rot)));

            var loopBlend = RootMotionLoopBlend.None;
            if (clip.m_LoopBlendOrientation != 0)
                loopBlend |= RootMotionLoopBlend.Orientation;
            if (clip.m_LoopBlendPositionY != 0)
                loopBlend |= RootMotionLoopBlend.PositionY;
            if (clip.m_LoopBlendPositionXZ != 0)
                loopBlend |= RootMotionLoopBlend.PositionXZ;

            samplingData.LoopBlend = loopBlend;
            samplingData.OffsetTranslation = offsetT;
            samplingData.OffsetRotation = offsetQ;
            samplingData.LoopStartX = new RigidTransform(math.normalize(math.mul(offsetQ, startX.rot)), startX.pos + offsetT);
        }

        // Turns the transform sampled from the curves of the source into the transform whose motion is the root motion
        internal static RigidTransform ProcessRootMotionTransform(in ClipRootMotionSamplingData samplingData, RigidTransform x)
        {
            if (samplingData.Source == RootMotionSource.Motion)
                return x;

            if (samplingData.Source == RootMotionSource.Bone)
            {
                x.rot = math.normalize(math.mul(x.rot, samplingData.InvBindRotation));
                return x;
            }

            x.rot = ProjectOnYPlane(x.rot);
            x.pos += samplingData.OffsetTranslation;
            x.rot = math.normalize(math.mul(samplingData.OffsetRotation, x.rot));

            var start = samplingData.LoopStartX;
            if ((samplingData.LoopBlend & RootMotionLoopBlend.Orientation) != 0)
                x.rot = start.rot;
            if ((samplingData.LoopBlend & RootMotionLoopBlend.PositionY) != 0)
                x.pos.y = start.pos.y;
            if ((samplingData.LoopBlend & RootMotionLoopBlend.PositionXZ) != 0)
            {
                x.pos.x = start.pos.x;
                x.pos.z = start.pos.z;
            }

            return x;
        }

        // Port of math::quatProjOnYPlane: keeps the rotation around the Y axis
        internal static quaternion ProjectOnYPlane(quaternion q)
        {
            var up = new float3(0f, 1f, 0f);
            var alignUp = ArcRotate(math.mul(q, up), up);
            return math.normalizesafe(math.mul(alignUp, q).value * new float4(0f, 1f, 0f, 1f), quaternion.identity.value);
        }

        static quaternion ArcRotate(float3 a, float3 b)
        {
            var axis = math.cross(a, b);
            var w = math.dot(a, b) + math.sqrt(math.dot(a, a) * math.dot(b, b));
            return math.normalizesafe(new quaternion(axis.x, axis.y, axis.z, w), quaternion.identity);
        }

        internal static RigidTransform ComputeRootMotionDelta(in ClipRootMotionSamplingData samplingData, RigidTransform prevX, RigidTransform curX, WrapMode wrapMode, int loopCount)
        {
            prevX = math.mul(samplingData.InvStartX, prevX);
            curX = math.mul(samplingData.InvStartX, curX);

            if (wrapMode == WrapMode.Loop || (wrapMode == WrapMode.Once && loopCount != 0))
            {
                var totalX = MathUtility.select(samplingData.TotalX, math.inverse(samplingData.TotalX), loopCount < 0);
                var cycleX = MathUtility.rigidPow(totalX, math.asuint(math.abs(loopCount)));

                curX = math.mul(cycleX, curX);
            }

            var deltaX = math.mul(math.inverse(prevX), curX);

            if (samplingData.Source == RootMotionSource.Root)
                deltaX = FilterRootDelta(in samplingData, deltaX);

            return deltaX;
        }

        // The root curves move the character on the ground: the delta keeps the rotation around the Y axis only, and
        // the LoopBlend settings remove their components
        static RigidTransform FilterRootDelta(in ClipRootMotionSamplingData samplingData, RigidTransform deltaX)
        {
            const float k_Epsilon = 1e-6f;

            var q = deltaX.rot.value;
            var halfTangent = q.xyz / (math.abs(q.w) < k_Epsilon ? k_Epsilon : q.w);
            halfTangent.x = 0f;
            halfTangent.z = 0f;

            if ((samplingData.LoopBlend & RootMotionLoopBlend.Orientation) != 0)
                halfTangent.y = 0f;
            if ((samplingData.LoopBlend & RootMotionLoopBlend.PositionY) != 0)
                deltaX.pos.y = 0f;
            if ((samplingData.LoopBlend & RootMotionLoopBlend.PositionXZ) != 0)
            {
                deltaX.pos.x = 0f;
                deltaX.pos.z = 0f;
            }

            deltaX.rot = math.normalize(new quaternion(halfTangent.x, halfTangent.y, halfTangent.z, 1f));
            return deltaX;
        }
    }
}
