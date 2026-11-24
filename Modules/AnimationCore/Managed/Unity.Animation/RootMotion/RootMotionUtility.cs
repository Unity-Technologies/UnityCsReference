// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace Unity.Animations
{
    /// <summary>
    /// Utility functions for the root motion of a rig, such as projecting a root translation onto the ground.
    /// </summary>
    /// <remarks>
    /// Use <see cref="ProjectRootTranslation"/> to find the ground position of a bone, such as the hips, when you match
    /// clips during a transition.
    /// </remarks>
    [GenerateTestsForBurstCompatibility]
    public static class RootMotionUtility
    {
        /// <summary>
        /// Projects a root translation onto the ground, which is the horizontal plane at y = 0.
        /// </summary>
        /// <remarks>
        /// Use this method to find the ground position of a bone when you match clips during a transition. The bone
        /// doesn't have to be the top of the hierarchy. For example, project the hips to align a walk clip with a jog
        /// clip.
        ///
        /// If <paramref name="bankPivot"/> is `false`, the method projects the translation straight down and ignores
        /// the rotation of the bone. If it is `true`, the method takes the rotation into account and projects the
        /// translation along the up axis of <paramref name="rotation"/>, unless that axis is horizontal. A horizontal
        /// axis makes the projection impossible, so the method projects downward instead.
        ///
        /// The following example uses the `Unity.Animations` and `Unity.Mathematics` namespaces. It projects the hips of
        /// a walk clip and a jog clip onto the ground, then computes the offset that aligns the jog with the walk.
        /// </remarks>
        /// <example nocheck="true">
        /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/ProjectRootTranslationExample.cs#AlignOnHips}]]></code>
        /// </example>
        /// <param name="translation">The root translation to project onto the ground.</param>
        /// <param name="rotation">The root rotation. When <paramref name="bankPivot"/> is `true`, its up axis is the
        /// projection direction.</param>
        /// <param name="bankPivot">Whether to project along the up axis of <paramref name="rotation"/> instead of
        /// downward.</param>
        /// <returns>The projected translation, with a y value of 0.</returns>
        public static float3 ProjectRootTranslation(float3 translation, quaternion rotation, bool bankPivot)
        {
            if (bankPivot)
            {
                var up = math.mul(rotation, new float3(0, 1, 0));
                if (math.abs(up.y) > math.EPSILON)
                {
                    // Solving t for : (translation + t * up).y = 0 (project to the ground along up axis)
                    // translation.y + t * up.y = 0
                    // t = -translation.y / up.y
                    // so projected translation is :
                    // translation + t * up = translation - up * (translation.y / up.y)

                    return translation - up * (translation.y / up.y);
                }
            }

            return new float3(translation.x, 0.0f, translation.z);
        }

        /// <summary>
        /// Blends two root motion deltas, the way <see cref="AnimationStreamBlendingExtensions.Blend(ref AnimationStream, in AnimationStream, in AnimationStream, float)"/>
        /// blends the poses of two clips.
        /// </summary>
        /// <remarks>
        /// The method interpolates the translations linearly and the rotations with a normalized linear interpolation.
        /// The result is the delta that blending the root poses of both clips gives, when both clips start the frame
        /// from the same pose, such as the clips of a blend space that move the same character.
        /// </remarks>
        /// <param name="delta1">The first delta, for example <see cref="MotionOutput.RootMotion"/> from <see cref="ClipSampler.SampleRootMotion"/>.</param>
        /// <param name="delta2">The second delta.</param>
        /// <param name="weight">The weight of <paramref name="delta2"/> in the blend.</param>
        /// <returns>The blended delta.</returns>
        public static RigidTransform Blend(RigidTransform delta1, RigidTransform delta2, float weight) =>
            new RigidTransform(math.nlerp(delta1.rot, delta2.rot, weight), math.lerp(delta1.pos, delta2.pos, weight));

        /// <summary>
        /// Blends an additive root motion delta into a delta, the way
        /// <see cref="AnimationStreamBlendingExtensions.BlendAdditive(ref AnimationStream, in AnimationStream, in AnimationStream, float)"/>
        /// blends an additive pose into a pose.
        /// </summary>
        /// <remarks>
        /// The result matches the stream operation only when the root of both clips starts the frame at the identity
        /// transform.
        /// </remarks>
        /// <param name="delta">The delta to modify.</param>
        /// <param name="additive">The additive delta.</param>
        /// <param name="weight">The weight of <paramref name="additive"/> in the blend.</param>
        /// <returns>The blended delta.</returns>
        public static RigidTransform BlendAdditive(RigidTransform delta, RigidTransform additive, float weight) =>
            new RigidTransform(math.mul(delta.rot, MathUtility.quatWeight(additive.rot, weight)), math.mad(additive.pos, weight, delta.pos));

        /// <summary>
        /// Adds two root motion deltas, the way
        /// <see cref="AnimationStreamBlendingExtensions.Add(ref AnimationStream, in AnimationStream, in AnimationStream)"/>
        /// adds two poses.
        /// </summary>
        /// <remarks>
        /// The method adds the translations and multiplies the rotations. The result matches the stream operation only
        /// when the root of both clips starts the frame at the identity transform.
        /// </remarks>
        /// <param name="delta1">The first delta.</param>
        /// <param name="delta2">The second delta.</param>
        /// <returns>The sum of the deltas.</returns>
        public static RigidTransform Add(RigidTransform delta1, RigidTransform delta2) =>
            new RigidTransform(math.mul(delta1.rot, delta2.rot), delta1.pos + delta2.pos);

        /// <summary>
        /// Inverses a root motion delta, the way
        /// <see cref="AnimationStreamBlendingExtensions.Inverse(ref AnimationStream, in AnimationStream)"/> inverses a
        /// pose.
        /// </summary>
        /// <remarks>
        /// The method negates the translation and conjugates the rotation, so the result isn't the inverse transform
        /// that `math.inverse` returns. The result matches the stream operation only when the root of the clip starts
        /// the frame at the identity transform.
        /// </remarks>
        /// <param name="delta">The delta to inverse.</param>
        /// <returns>The inversed delta.</returns>
        public static RigidTransform Inverse(RigidTransform delta) =>
            new RigidTransform(math.conjugate(delta.rot), -delta.pos);

        /// <summary>
        /// Weights a root motion delta, the way
        /// <see cref="AnimationStreamBlendingExtensions.Weight(ref AnimationStream, in AnimationStream, float, in AnimationStreamMask)"/>
        /// weights a pose.
        /// </summary>
        /// <remarks>
        /// The result matches the stream operation only when the root of the clip starts the frame at the identity
        /// transform.
        /// </remarks>
        /// <param name="delta">The delta to weight.</param>
        /// <param name="weight">The weight to apply to the delta.</param>
        /// <returns>The weighted delta.</returns>
        public static RigidTransform Weight(RigidTransform delta, float weight) =>
            new RigidTransform(MathUtility.quatWeight(delta.rot, weight), delta.pos * weight);

        /// <summary>
        /// Computes the difference between two root motion deltas, the way
        /// <see cref="AnimationStreamBlendingExtensions.Delta(ref AnimationStream, in AnimationStream, in AnimationStream)"/>
        /// computes the difference between two poses.
        /// </summary>
        /// <remarks>
        /// The result is `Add(Inverse(subtract), input)`. It matches the stream operation only when the root of
        /// both clips starts the frame at the identity transform.
        /// </remarks>
        /// <param name="input">The input delta.</param>
        /// <param name="subtract">The delta to subtract.</param>
        /// <returns>The difference between the deltas.</returns>
        public static RigidTransform Delta(RigidTransform input, RigidTransform subtract) =>
            Add(Inverse(subtract), input);

        /// <summary>
        /// Mixes several weighted root motion deltas, the way <see cref="Core.PoseMixer"/> mixes the poses of several
        /// clips.
        /// </summary>
        /// <remarks>
        /// Use it with the same weights as the poses, for example the weights that a blend space computes. Call
        /// <see cref="Add"/> once for each delta and its weight, then <see cref="End"/> to get the result. The default
        /// value is ready to use. If the weights sum to less than 1, the remaining weight keeps the character still.
        ///
        /// The following example uses the `Unity.Animations`, `Unity.Collections`, `Unity.Entities`, `Unity.IntegerTime`,
        /// `Unity.Mathematics`, and `UnityEngine` namespaces. It mixes the poses and the root motion of the clips of a 1D
        /// blend space with the same weights, and moves a GameObject with the result.
        /// </remarks>
        /// <example nocheck="true">
        /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/RootMotionBlendSpaceExample.cs#BlendLocomotionWithRootMotion}]]></code>
        /// </example>
        public struct DeltaMixer
        {
            float3 m_Translation;
            quaternion m_Rotation;
            float m_WeightSum;

            /// <summary>
            /// Clears the deltas that the mixer has collected, so that you can start a new mix.
            /// </summary>
            public void Begin() => this = default;

            /// <summary>
            /// Adds a weighted root motion delta to the mix.
            /// </summary>
            /// <param name="delta">The delta to add, for example <see cref="MotionOutput.RootMotion"/> from <see cref="ClipSampler.SampleRootMotion"/>.</param>
            /// <param name="weight">The weight of the delta in the mix. The mixer doesn't normalize weights.</param>
            public void Add(RigidTransform delta, float weight)
            {
                m_Translation += delta.pos * weight;
                m_Rotation = MathUtility.add(m_Rotation, delta.rot.value * weight);
                m_WeightSum += weight;
            }

            /// <summary>
            /// Finishes the mix and returns the mixed delta.
            /// </summary>
            /// <returns>The mixed delta. The identity transform fills the weight that the added deltas miss to sum
            /// to 1.</returns>
            public readonly RigidTransform End()
            {
                var rotation = m_WeightSum < 1f ? MathUtility.add(m_Rotation, quaternion.identity.value * (1f - m_WeightSum)) : m_Rotation;
                return new RigidTransform(math.normalizesafe(rotation), m_Translation);
            }
        }

        /// <summary>
        /// Updates the RM displacements given a local TRS.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void UpdateRMDisplacements(in TRS localTRS, ref RigidTransform rm)
        {
            rm.pos = MathUtility.rotate(localTRS, rm.pos);
            rm.rot = MathUtility.scaleMulQuat(localTRS.s, rm.rot);
            rm.rot = math.mul(math.mul(localTRS.r, rm.rot), math.conjugate(localTRS.r));
        }
    }
}
