// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.CompilerServices;

namespace Unity.Animations
{
    [GenerateTestsForBurstCompatibility]
    public static partial class Core
    {
        const float k_Epsilon = 1e-5f;
        const float k_SqEpsilon = 1e-8f;

        /// <summary>
        /// 2-bone IK solver settings.
        /// </summary>
        [GenerateTestsForBurstCompatibility]
        public struct TwoBoneIKData
        {
            /// <summary>Source transform specified in LocalToCharacter space that defines the desired location of the tip bone.</summary>
            public RigidTransform Target;
            /// <summary>Specifies an offset on the target to maintain when solving the limbs.</summary>
            public RigidTransform TargetOffset;
            /// <summary>Optional source position specified in LocalToCharacter space to describe the direction the limb should be oriented when it bends.</summary>
            public float3 Hint;

            /// <summary>Root index of the limb hierarchy.</summary>
            public int RootIndex;
            /// <summary>Middle index of the limb hierarchy, it must be a child of the root.</summary>
            public int MidIndex;
            /// <summary>Tip index of the limb hierarchy, it must be a child of the mid bone.</summary>
            public int TipIndex;

            /// <summary>
            /// The weight to apply for calculating the desired position when reaching for the Target.
            /// If set to 1, the Root and Mid bones will extend as far as necessary to try to reach the specified Target position. As with the constraint's overall weight, intermediate values are interpolated linearly.
            /// </summary>
            public float TargetPositionWeight; // [0, 1]
            /// <summary>
            /// The weight of the rotation applied to the Tip.
            /// If set to 1, the Tip aligns it rotation to the specified Target. As with the constraint's overall weight, intermediate values are interpolated linearly.
            /// </summary>
            public float TargetRotationWeight; // [0, 1]
            /// <summary>
            /// The amount of influence the Hint has on the configuration of the hierarchy.
            /// When set to 1, the Hint fully influences the hierarchy configuration. Set to 0 if the hint should be ignored.
            /// </summary>
            public float HintWeight; // [0, 1]

            /// <summary>
            /// Returns a set of default values for the TwoBoneIK solver.
            /// </summary>
            /// <returns>Default 2-bone IK solver settings.</returns>
            public static TwoBoneIKData Default() => new TwoBoneIKData
            {
                Target               = RigidTransform.identity,
                TargetOffset         = RigidTransform.identity,
                Hint                 = float3.zero,
                RootIndex            = -1,
                MidIndex             = -1,
                TipIndex             = -1,
                TargetPositionWeight = 1f,
                TargetRotationWeight = 1f,
                HintWeight           = 0f
            };
        }


        /// <summary>
        /// Simple 2-bone IK solver based on cosine rule. It uses an additional hint position (specified in LocalToCharacter space) which
        /// allows you to specify the direction the limb should be oriented when it bends.
        /// </summary>
        /// <param name="stream">The animation stream to apply the solver to.</param>
        /// <param name="data">The solver settings. See <see cref="TwoBoneIKData"/>.</param>
        /// <param name="weight">The overall weight of the solver. If set to 0, the solver has no influence on the limb elements. When set to 1, it applies full influence with the current settings.</param>
        /// <remarks>This solver does not work on non-uniform scaled hierarchies.</remarks>
        /// <exception cref="System.InvalidOperationException">Thrown if safety checks are enabled and a bone index, the
        /// target rotation, or the target offset rotation is invalid.</exception>
        public static void SolveTwoBoneIK(ref AnimationStream stream, in TwoBoneIKData data, float weight)
        {
            weight = math.saturate(weight);
            if (!(weight > 0f))
                return;

            Core.ValidateGreater(data.RootIndex, -1);
            Core.ValidateGreater(data.MidIndex, -1);
            Core.ValidateGreater(data.TipIndex, -1);
            Core.Validate(data.Target.rot);
            Core.Validate(data.TargetOffset.rot);

            stream.GetLocalToCharacterTR(data.RootIndex, out float3 rootPos, out quaternion rootRot);
            stream.GetLocalToCharacterTR(data.MidIndex , out float3 midPos , out quaternion midRot);
            stream.GetLocalToCharacterTR(data.TipIndex , out float3 tipPos , out quaternion tipRot);

            float3 tPos = math.lerp(tipPos, data.Target.pos + data.TargetOffset.pos, math.saturate(data.TargetPositionWeight) * weight);
            quaternion tRot = math.nlerp(tipRot, math.mul(data.Target.rot, data.TargetOffset.rot), math.saturate(data.TargetRotationWeight) * weight);
            float hintWeight = math.saturate(data.HintWeight) * weight;
            bool hasHint = hintWeight > 0f;

            float3 ab = midPos - rootPos;
            float3 bc = tipPos - midPos;
            float3 ac = tipPos - rootPos;
            float3 at = tPos   - rootPos;

            float lenAB = math.length(ab);
            float lenBC = math.length(bc);
            float lenAC = math.length(ac);
            float lenAT = math.length(at);

            float prevAngle = TriangleAngle(lenAC, lenAB, lenBC);
            float newAngle  = TriangleAngle(lenAT, lenAB, lenBC);

            // Bend normal strategy is to take whatever has been provided in the animation
            // stream to minimize configuration changes, however if this is collinear
            // try computing a bend normal given a hint (when provided) or otherwise the desired target position.
            // If all fails, math.up() is used.
            float3 axis = math.cross(ab, bc);
            if (math.lengthsq(axis) < k_SqEpsilon)
            {
                axis = math.select(float3.zero, math.cross(data.Hint - rootPos, bc), hasHint);
                if (math.lengthsq(axis) < k_SqEpsilon)
                    axis = math.cross(at, bc);
                if (math.lengthsq(axis) < k_SqEpsilon)
                    axis = math.up();
            }
            axis = math.normalize(axis);

            math.sincos(0.5f * (prevAngle - newAngle), out float sin, out float cos);
            quaternion deltaRot = new float4(axis * sin, cos);
            stream.SetLocalToCharacterRotation(data.MidIndex, math.mul(deltaRot, midRot));

            tipPos = stream.GetLocalToCharacterTranslation(data.TipIndex);
            ac = tipPos - rootPos;

            stream.SetLocalToCharacterRotation(data.RootIndex, math.mul(MathUtility.fromToRotationSafe(ac, at), rootRot));

            if (hasHint)
            {
                float acLengthSq = math.lengthsq(ac);
                if (acLengthSq > 0f)
                {
                    midPos = stream.GetLocalToCharacterTranslation(data.MidIndex);
                    tipPos = stream.GetLocalToCharacterTranslation(data.TipIndex);
                    ab = midPos - rootPos;
                    ac = tipPos - rootPos;

                    float3 acNorm = ac * math.rsqrt(acLengthSq);
                    float3 ah = data.Hint - rootPos;
                    float3 abProj = ab - acNorm * math.dot(ab, acNorm);
                    float3 ahProj = ah - acNorm * math.dot(ah, acNorm);

                    float maxReach = lenAB + lenBC;
                    if (math.lengthsq(abProj) > (maxReach * maxReach * 0.001f) && math.lengthsq(ahProj) > 0f)
                    {
                        // abProj and ahProj are both perpendicular to acNorm, so their cross product normally
                        // gives acNorm back; when the hint is opposite the elbow it vanishes and acNorm must be supplied.
                        quaternion hintRot = MathUtility.fromToSafe(abProj, ahProj, acNorm);
                        hintRot = math.slerp(quaternion.identity, hintRot, hintWeight);
                        stream.SetLocalToCharacterRotation(data.RootIndex, math.mul(hintRot, stream.GetLocalToCharacterRotation(data.RootIndex)));
                    }
                }
            }

            stream.SetLocalToCharacterRotation(data.TipIndex, tRot);
        }

        /// <summary>
        /// Given a triangle with sides of length len0, len1 and len2, this function computes the angle between len1 and len2, and opposite to len0.
        /// </summary>
        /// <param name="len0">Length of the side of the triangle opposite to the angle</param>
        /// <param name="len1">Length of the side of the triangle adjacent to the angle</param>
        /// <param name="len2">Length of the side of the triangle adjacent to the angle</param>
        /// <returns>The angle opposite to len0, in radians</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float TriangleAngle(float len0, float len1, float len2) =>
            math.acos(math.clamp(((math.lengthsq(math.float2(len1, len2)) - (len0 * len0)) / (len1 * len2)) * 0.5f, -1f, 1f));

        static unsafe float Sum(NativeArray<float> array)
        {
            if (array.Length == 0)
                return 0f;

            float4* arrayPtr = (float4*)array.GetUnsafeReadOnlyPtr();
            float4 sum = float4.zero;

            int l = array.Length >> 2;
            for (int i = 0; i < l; ++i)
                sum += arrayPtr[i];
            for (int i = l << 2; i < array.Length; ++i)
                sum.x += array[i];

            return math.csum(sum);
        }

        /// <summary>
        /// Position constraint solver settings.
        /// </summary>
        [GenerateTestsForBurstCompatibility]
        public struct PositionConstraintData
        {
            /// <summary>Specifies an additional local space translation offset to apply to the constrained bone, after it has been moved toward its target.</summary>
            public float3 LocalOffset;
            /// <summary>Specifies the axes to which the constraint can apply translation.</summary>
            public bool3 LocalAxesMask;
            /// <summary>The constrained bone index.</summary>
            public int Index;

            /// <summary>
            /// The list of source positions that influence the constrained bone's position.
            /// The constraint calculates translation toward each target to produce a weighted sum. The order of source positions does not affect the result.
            /// </summary>
            public NativeArray<float3> SourcePositions;
            /// <summary> The list of additional translation offsets to apply on the source positions.</summary>
            public NativeArray<float3> SourceOffsets;
            /// <summary>The list of weights each source contributes to the final constrained pose.</summary>
            public NativeArray<float> SourceWeights;

            /// <summary>
            /// Returns a set of default values for the position constraint solver.
            /// </summary>
            /// <returns>Default position constraint solver settings.</returns>
            public static PositionConstraintData Default() => new PositionConstraintData
            {
                Index = -1,
                LocalAxesMask = new bool3(true),
                LocalOffset = float3.zero
            };
        }

        /// <summary>
        /// Position constraint solver moves a constrained bone to follow its source objects.
        /// </summary>
        /// <param name="stream">The animation stream to apply the solver to.</param>
        /// <param name="data">The solver settings. See <see cref="PositionConstraintData"/>.</param>
        /// <param name="weight">The overall weight of the solver. If set to 0, the solver has no influence on the constrained bone. When set to 1, it applies full influence with the current settings. Intermediate values are interpolated linearly.</param>
        /// <exception cref="System.InvalidOperationException">Thrown if safety checks are enabled and the constrained
        /// bone index is invalid, or if the source buffers don't all have the same length.</exception>
        public static void SolvePositionConstraint(ref AnimationStream stream, in PositionConstraintData data, float weight)
        {
            weight = math.saturate(weight);
            if (!(weight > 0f))
                return;

            Core.ValidateGreater(data.Index, -1);
            Core.ValidateBufferLengthsAreEqual(data.SourcePositions.Length, data.SourceOffsets.Length);
            Core.ValidateBufferLengthsAreEqual(data.SourcePositions.Length, data.SourceWeights.Length);

            float sumWeights = Sum(data.SourceWeights);
            if (sumWeights < k_Epsilon)
                return;

            float weightScale = math.select(1f, math.rcp(sumWeights), sumWeights > 1f);
            float3 currentT = stream.GetLocalToCharacterTranslation(data.Index);
            float3 accumT = currentT;

            for (int i = 0; i < data.SourcePositions.Length; ++i)
            {
                float normalizedWeight = data.SourceWeights[i] * weightScale;
                if (normalizedWeight < k_Epsilon)
                    continue;

                accumT += (data.SourcePositions[i] + data.SourceOffsets[i] - currentT) * normalizedWeight;
            }

            ApplyPositionConstraint(ref stream, data, accumT, weight);
        }

        static void ApplyPositionConstraint(ref AnimationStream stream, in PositionConstraintData data, float3 accumT, float weight)
        {
            // Convert accumT to local space
            int parentIdx = stream.Rig.Skeleton.ParentIndexes[data.Index];
            if (parentIdx != -1)
            {
                accumT = math.transform(stream.GetLocalToCharacterInverseMatrix(parentIdx), accumT);
            }

            float3 currentLocalT = stream.GetLocalToParentTranslation(data.Index);
            if (!math.all(data.LocalAxesMask))
                accumT = math.select(currentLocalT, accumT, data.LocalAxesMask);

            stream.SetLocalToParentTranslation(data.Index, math.lerp(currentLocalT, accumT + data.LocalOffset, weight));
        }

        /// <summary>
        /// Applies a position constraint to a bone from a single source.
        /// </summary>
        /// <remarks>
        /// Use this overload when the constraint has one source. It gives the same result as
        /// <see cref="SolvePositionConstraint(ref AnimationStream, in PositionConstraintData, float)"/> with one source
        /// of weight 1, and you don't need to allocate a <see cref="NativeArray{T}"/> for the sources. The method
        /// ignores the source arrays in <paramref name="data"/>.
        ///
        /// The following example uses the `Unity.Animations` and `Unity.Mathematics` namespaces. It converts world-space
        /// targets to character space and passes them to the single-source solvers.
        /// </remarks>
        /// <example nocheck="true">
        /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/ConstrainToWorldTargetExample.cs#ConstrainToWorldTarget}]]></code>
        /// </example>
        /// <param name="stream">The animation stream to apply the solver to.</param>
        /// <param name="data">The solver settings. The method uses only <see cref="PositionConstraintData.Index"/>,
        /// <see cref="PositionConstraintData.LocalAxesMask"/>, and <see cref="PositionConstraintData.LocalOffset"/>,
        /// and ignores the source arrays.</param>
        /// <param name="sourcePosition">The source position, in character space. To follow a world-space target, convert it first with <c>math.transform(math.inverse(characterToWorld), worldPosition)</c>.</param>
        /// <param name="sourceOffset">The additional translation offset to apply on the source position.</param>
        /// <param name="weight">The overall weight of the solver, clamped to the range 0 to 1. A weight of 0 leaves the
        /// constrained bone unchanged, and a weight of 1 applies the full constraint. Intermediate values blend
        /// linearly.</param>
        /// <exception cref="System.InvalidOperationException">Thrown if safety checks are enabled and the constrained
        /// bone index is invalid.</exception>
        public static void SolvePositionConstraint(ref AnimationStream stream, in PositionConstraintData data, float3 sourcePosition, float3 sourceOffset, float weight)
        {
            weight = math.saturate(weight);
            if (!(weight > 0f))
                return;

            Core.ValidateGreater(data.Index, -1);

            float3 currentT = stream.GetLocalToCharacterTranslation(data.Index);
            ApplyPositionConstraint(ref stream, data, currentT + (sourcePosition + sourceOffset - currentT), weight);
        }

        /// <summary>
        /// Rotation constraint solver settings.
        /// </summary>
        [GenerateTestsForBurstCompatibility]
        public struct RotationConstraintData
        {
            /// <summary>Specifies an additional local space rotation offset to apply to the constrained bone, after it has been rotated toward its target.</summary>
            public quaternion LocalOffset;
            /// <summary>Specifies the axes to which the constraint can apply rotation.</summary>
            public bool3 LocalAxesMask;
            /// <summary>The constrained bone index.</summary>
            public int Index;

            /// <summary>
            /// The list of source rotations that influence the constrained bone's orientation.
            /// The constraint calculates rotation toward each target to produce a weighted sum. The order of source rotations does not affect the result.
            /// </summary>
            public NativeArray<quaternion> SourceRotations;
            /// <summary>The list of additional rotation offsets to apply on the sources.</summary>
            public NativeArray<quaternion> SourceOffsets;
            /// <summary>The list of weights each source contributes to the final constrained pose.</summary>
            public NativeArray<float> SourceWeights;

            /// <summary>
            /// Returns a set of default values for the rotation constraint solver.
            /// </summary>
            /// <returns>Default rotation constraint solver settings.</returns>
            public static RotationConstraintData Default() => new RotationConstraintData
            {
                Index = -1,
                LocalOffset = quaternion.identity,
                LocalAxesMask = new bool3(true)
            };
        }

        /// <summary>
        /// Rotation constraint solver rotates a constrained bone to match the rotation of its source objects.
        /// The constraint applies rotation about the shortest path to each source.
        /// The order the sources are specified does not affect the result.
        /// </summary>
        /// <param name="stream">The animation stream to apply the solver to.</param>
        /// <param name="data">The solver settings. See <see cref="RotationConstraintData"/>.</param>
        /// <param name="weight">The overall weight of the solver. If set to 0, the solver has no influence on the constrained bone. When set to 1, it applies full influence with the current settings. Intermediate values are interpolated linearly.</param>
        /// <exception cref="System.InvalidOperationException">Thrown if safety checks are enabled and the constrained
        /// bone index or a rotation is invalid, or if the source buffers don't all have the same length.</exception>
        public static void SolveRotationConstraint(ref AnimationStream stream, in RotationConstraintData data, float weight)
        {
            weight = math.saturate(weight);
            if (!(weight > 0f))
                return;

            Core.ValidateGreater(data.Index, -1);
            Core.ValidateBufferLengthsAreEqual(data.SourceRotations.Length, data.SourceOffsets.Length);
            Core.ValidateBufferLengthsAreEqual(data.SourceRotations.Length, data.SourceWeights.Length);
            Core.Validate(data.LocalOffset);

            float sumWeights = Sum(data.SourceWeights);
            if (sumWeights < k_Epsilon)
                return;

            float weightScale = math.select(1f, math.rcp(sumWeights), sumWeights > 1f);
            float accumWeights = 0f;
            quaternion accumR = float4.zero;

            for (int i = 0; i < data.SourceRotations.Length; ++i)
            {
                float normalizedWeight = data.SourceWeights[i] * weightScale;
                if (normalizedWeight < k_Epsilon)
                    continue;

                Core.Validate(data.SourceRotations[i]);
                Core.Validate(data.SourceOffsets[i]);

                quaternion addValue = math.mul(data.SourceRotations[i], data.SourceOffsets[i]);
                accumR = MathUtility.add(accumR, addValue.value * normalizedWeight);
                accumWeights += normalizedWeight;
            }

            ApplyRotationConstraint(ref stream, data, accumR, accumWeights, weight);
        }

        /// <summary>
        /// Applies a rotation constraint to a bone from a single source.
        /// </summary>
        /// <remarks>
        /// Use this overload when the constraint has one source. It gives the same result as
        /// <see cref="SolveRotationConstraint(ref AnimationStream, in RotationConstraintData, float)"/> with one source
        /// of weight 1, and you don't need to allocate a <see cref="NativeArray{T}"/> for the sources. The method
        /// ignores the source arrays in <paramref name="data"/>.
        ///
        /// The following example uses the `Unity.Animations` and `Unity.Mathematics` namespaces. It converts world-space
        /// targets to character space and passes them to the single-source solvers.
        /// </remarks>
        /// <example nocheck="true">
        /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/ConstrainToWorldTargetExample.cs#ConstrainToWorldTarget}]]></code>
        /// </example>
        /// <param name="stream">The animation stream to apply the solver to.</param>
        /// <param name="data">The solver settings. The method uses only <see cref="RotationConstraintData.Index"/>,
        /// <see cref="RotationConstraintData.LocalAxesMask"/>, and <see cref="RotationConstraintData.LocalOffset"/>,
        /// and ignores the source arrays.</param>
        /// <param name="sourceRotation">The source rotation, in character space. To follow a world-space target, convert it first with <c>math.mul(math.inverse(characterToWorld.rot), worldRotation)</c>.</param>
        /// <param name="sourceOffset">The rotation offset in the local space of the source. The solver multiplies it
        /// after <paramref name="sourceRotation"/>.</param>
        /// <param name="weight">The overall weight of the solver, clamped to the range 0 to 1. A weight of 0 leaves the
        /// constrained bone unchanged, and a weight of 1 applies the full constraint. Intermediate values blend the
        /// rotation with a normalized linear interpolation.</param>
        /// <exception cref="System.InvalidOperationException">Thrown if safety checks are enabled and the constrained
        /// bone index or a rotation is invalid.</exception>
        public static void SolveRotationConstraint(ref AnimationStream stream, in RotationConstraintData data, quaternion sourceRotation, quaternion sourceOffset, float weight)
        {
            weight = math.saturate(weight);
            if (!(weight > 0f))
                return;

            Core.ValidateGreater(data.Index, -1);
            Core.Validate(data.LocalOffset);
            Core.Validate(sourceRotation);
            Core.Validate(sourceOffset);

            quaternion accumR = MathUtility.add(float4.zero, math.mul(sourceRotation, sourceOffset).value);
            ApplyRotationConstraint(ref stream, data, accumR, 1f, weight);
        }

        static void ApplyRotationConstraint(ref AnimationStream stream, in RotationConstraintData data, quaternion accumR, float accumWeights, float weight)
        {
            accumR = math.normalizesafe(accumR);
            if (accumWeights < 1f)
                accumR = math.nlerp(stream.GetLocalToCharacterRotation(data.Index), accumR, accumWeights);

            // Convert accumR to local space
            int parentIdx = stream.Rig.Skeleton.ParentIndexes[data.Index];
            if (parentIdx != -1)
                accumR = math.mul(math.inverse(stream.GetLocalToCharacterRotation(parentIdx)), accumR);

            quaternion currentLocalR = stream.GetLocalToParentRotation(data.Index);
            if (!math.all(data.LocalAxesMask))
                accumR = quaternion.Euler(math.select(math.Euler(currentLocalR), math.Euler(accumR), data.LocalAxesMask));

            stream.SetLocalToParentRotation(data.Index, math.nlerp(currentLocalR, math.mul(accumR, data.LocalOffset), weight));
        }

        /// <summary>
        /// Parent constraint solver settings.
        /// </summary>
        [GenerateTestsForBurstCompatibility]
        public struct ParentConstraintData
        {
            /// <summary>Specifies the axes to which the constraint can apply translation.</summary>
            public bool3 LocalTranslationAxesMask;
            /// <summary>Specifies the axes to which the constraint can apply rotation.</summary>
            public bool3 LocalRotationAxesMask;
            /// <summary>The constrained bone index.</summary>
            public int Index;

            /// <summary>
            /// The list of source transforms that influence the constrained bone's position and orientation.
            /// The constraint applies linearly interpolated, weighted translation and rotation toward each target. The order of source transforms does not affect the result.
            /// </summary>
            public NativeArray<RigidTransform> SourceTransforms;
            /// <summary>The list of additional offsets to apply on the sources.</summary>
            public NativeArray<RigidTransform> SourceOffsets;
            /// <summary>The list of weights each source contributes to the final constrained pose.</summary>
            public NativeArray<float> SourceWeights;

            /// <summary>
            /// Returns a set of default values for the parent constraint solver.
            /// </summary>
            /// <returns>Default parent constraint solver settings.</returns>
            public static ParentConstraintData Default() => new ParentConstraintData
            {
                Index = -1,
                LocalTranslationAxesMask = new bool3(true),
                LocalRotationAxesMask = new bool3(true)
            };
        }

        /// <summary>
        /// Parent constraint solver moves and rotates a constrained bone as if it is the child of another source object.
        /// The order source objects are specified does not affect the result.
        /// </summary>
        /// <param name="stream">The animation stream to apply the solver to.</param>
        /// <param name="data">The solver settings. See <see cref="ParentConstraintData"/>.</param>
        /// <param name="weight">The overall weight of the solver. If set to 0, the solver has no influence on the constrained bone. When set to 1, it applies full influence with the current settings. Intermediate values are interpolated linearly.</param>
        /// <exception cref="System.InvalidOperationException">Thrown if safety checks are enabled and the constrained
        /// bone index or a rotation is invalid, or if the source buffers don't all have the same length.</exception>
        public static void SolveParentConstraint(ref AnimationStream stream, in ParentConstraintData data, float weight)
        {
            weight = math.saturate(weight);
            if (!(weight > 0f))
                return;

            Core.ValidateGreater(data.Index, -1);
            Core.ValidateBufferLengthsAreEqual(data.SourceTransforms.Length, data.SourceOffsets.Length);
            Core.ValidateBufferLengthsAreEqual(data.SourceTransforms.Length, data.SourceWeights.Length);

            float sumWeights = Sum(data.SourceWeights);
            if (sumWeights < k_Epsilon)
                return;

            float weightScale = math.select(1f, math.rcp(sumWeights), sumWeights > 1f);
            float accumWeights = 0f;
            RigidTransform accumTx = new RigidTransform(float4.zero, float3.zero);

            for (int i = 0; i < data.SourceTransforms.Length; ++i)
            {
                float normalizedWeight = data.SourceWeights[i] * weightScale;
                if (normalizedWeight < k_Epsilon)
                    continue;

                Core.Validate(data.SourceTransforms[i]);
                Core.Validate(data.SourceOffsets[i]);

                RigidTransform sourceTransform = math.mul(data.SourceTransforms[i], data.SourceOffsets[i]);
                accumTx.pos += sourceTransform.pos * normalizedWeight;
                accumTx.rot = MathUtility.add(accumTx.rot, sourceTransform.rot.value * normalizedWeight);
                accumWeights += normalizedWeight;
            }

            ApplyParentConstraint(ref stream, data, accumTx, accumWeights, weight);
        }

        /// <summary>
        /// Applies a parent constraint to a bone from a single source.
        /// </summary>
        /// <remarks>
        /// Use this overload when the constraint has one source. It gives the same result as
        /// <see cref="SolveParentConstraint(ref AnimationStream, in ParentConstraintData, float)"/> with one source of
        /// weight 1, and you don't need to allocate a <see cref="NativeArray{T}"/> for the sources. The method ignores
        /// the source arrays in <paramref name="data"/>.
        ///
        /// The following example uses the `Unity.Animations` and `Unity.Mathematics` namespaces. It converts world-space
        /// targets to character space and passes them to the single-source solvers.
        /// </remarks>
        /// <example nocheck="true">
        /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/ConstrainToWorldTargetExample.cs#ConstrainToWorldTarget}]]></code>
        /// </example>
        /// <param name="stream">The animation stream to apply the solver to.</param>
        /// <param name="data">The solver settings. The method uses only <see cref="ParentConstraintData.Index"/>,
        /// <see cref="ParentConstraintData.LocalTranslationAxesMask"/>, and
        /// <see cref="ParentConstraintData.LocalRotationAxesMask"/>, and ignores the source arrays.</param>
        /// <param name="sourceTransform">The source transform, in character space. To follow a world-space target, convert it first with <c>math.mul(math.inverse(characterToWorld), worldTx)</c>.</param>
        /// <param name="sourceOffset">The offset in the local space of the source transform. The solver multiplies it
        /// after <paramref name="sourceTransform"/>.</param>
        /// <param name="weight">The overall weight of the solver, clamped to the range 0 to 1. A weight of 0 leaves the
        /// constrained bone unchanged, and a weight of 1 applies the full constraint. Intermediate values blend the
        /// rotation with a normalized linear interpolation.</param>
        /// <exception cref="System.InvalidOperationException">Thrown if safety checks are enabled and the constrained
        /// bone index or a rotation is invalid.</exception>
        public static void SolveParentConstraint(ref AnimationStream stream, in ParentConstraintData data, RigidTransform sourceTransform, RigidTransform sourceOffset, float weight)
        {
            weight = math.saturate(weight);
            if (!(weight > 0f))
                return;

            Core.ValidateGreater(data.Index, -1);
            Core.Validate(sourceTransform);
            Core.Validate(sourceOffset);

            RigidTransform accumTx = new RigidTransform(float4.zero, float3.zero);
            RigidTransform offsetTx = math.mul(sourceTransform, sourceOffset);
            accumTx.pos += offsetTx.pos;
            accumTx.rot = MathUtility.add(accumTx.rot, offsetTx.rot.value);
            ApplyParentConstraint(ref stream, data, accumTx, 1f, weight);
        }

        static void ApplyParentConstraint(ref AnimationStream stream, in ParentConstraintData data, RigidTransform accumTx, float accumWeights, float weight)
        {
            accumTx.rot = math.normalizesafe(accumTx.rot);
            if (accumWeights < 1f)
            {
                stream.GetLocalToCharacterTR(data.Index, out float3 currentT, out quaternion currentR);
                accumTx.pos += currentT * (1f - accumWeights);
                accumTx.rot = math.nlerp(currentR, accumTx.rot, accumWeights);
            }

            // Convert accumTx to local space
            int parentIdx = stream.Rig.Skeleton.ParentIndexes[data.Index];
            if (parentIdx != -1)
            {
                TRS parentTx;
                stream.GetLocalToCharacterTRS(parentIdx, out parentTx.t, out parentTx.r, out parentTx.s);
                accumTx.pos = MathUtility.inverseTransform(parentTx, accumTx.pos);
                accumTx.rot = math.mul(math.conjugate(parentTx.r), accumTx.rot);
            }

            stream.GetLocalToParentTR(data.Index, out float3 currentLocalT, out quaternion currentLocalR);
            if (!math.all(data.LocalTranslationAxesMask))
                accumTx.pos = math.select(currentLocalT, accumTx.pos, data.LocalTranslationAxesMask);
            if (!math.all(data.LocalRotationAxesMask))
                accumTx.rot = quaternion.Euler(math.select(math.Euler(currentLocalR), math.Euler(accumTx.rot), data.LocalRotationAxesMask));

            stream.SetLocalToParentTR(
                data.Index,
                math.lerp(currentLocalT, accumTx.pos, weight),
                math.nlerp(currentLocalR, accumTx.rot, weight)
            );
        }

        /// <summary>
        /// AimConstraint solver settings.
        /// </summary>
        [GenerateTestsForBurstCompatibility]
        public struct AimConstraintData
        {
            /// <summary>Specifies an additional local space rotation offset to apply to the constrained bone, after it has been rotated toward its target.</summary>
            public quaternion LocalOffset;
            /// <summary>Specifies the aim axis of the constrained bone to use in order to orient its forward direction to the source positions.</summary>
            public float3 AimAxis;
            /// <summary>Specifies the axes to which the constraint can apply rotation.</summary>
            public bool3 LocalAxesMask;
            /// <summary>The constrained bone index.</summary>
            public int Index;

            /// <summary>Up axis of the constrained bone, in local space.</summary>
            public float3 UpAxis;
            /// <summary>
            /// Up axis in character space. The constraint will try to keep the RootUpAxis and the UpAxis aligned. Note
            /// that this axis can be zero, in this case no effort is made to keep the up axis aligned.
            /// </summary>
            public float3 RootUpAxis;

            /// <summary>
            /// The list of source positions that influence the constrained bone's orientation.
            /// The constraint calculates rotation toward each target to produce a weighted sum. The order of source positions does not affect the result.
            /// </summary>
            public NativeArray<float3> SourcePositions;
            /// <summary>The rotation offset for each source, in the parent space of the constrained bone. The solver
            /// applies each offset after the rotation that aims the bone at that source.</summary>
            public NativeArray<quaternion> SourceOffsets;
            /// <summary>The list of weights each source contributes to the final constrained pose.</summary>
            public NativeArray<float> SourceWeights;

            /// <summary>
            /// Returns a set of default values for the aim constraint solver.
            /// </summary>
            /// <returns>Default aim constraint solver settings.</returns>
            public static AimConstraintData Default() => new AimConstraintData
            {
                Index = -1,
                LocalOffset = quaternion.identity,
                AimAxis = new float3(0f, 1f, 0f),
                LocalAxesMask = new bool3(true),
                UpAxis = math.up(),
                RootUpAxis = float3.zero,
            };
        }

        /// <summary>
        /// Aim constraint solver rotates a constrained bone to face a target position specified by one or more source objects.
        /// </summary>
        /// <param name="stream">The animation stream to apply the solver to.</param>
        /// <param name="data">The solver settings. See <see cref="AimConstraintData"/>.</param>
        /// <param name="weight">The overall weight of the solver. If set to 0, the solver has no influence on the constrained bone. When set to 1, it applies full influence with the current settings. Intermediate values are interpolated linearly.</param>
        /// <exception cref="System.InvalidOperationException">Thrown if safety checks are enabled and the constrained
        /// bone index or a rotation offset is invalid, or if the source buffers don't all have the same
        /// length.</exception>
        public static void SolveAimConstraint(ref AnimationStream stream, in AimConstraintData data, float weight)
        {
            weight = math.saturate(weight);
            if (!(weight > 0f))
                return;

            Core.ValidateGreater(data.Index, -1);
            Core.ValidateBufferLengthsAreEqual(data.SourcePositions.Length, data.SourceOffsets.Length);
            Core.ValidateBufferLengthsAreEqual(data.SourcePositions.Length, data.SourceWeights.Length);
            Core.Validate(data.LocalOffset);

            float sumWeights = Sum(data.SourceWeights);
            if (sumWeights < k_Epsilon)
                return;

            float weightScale = math.select(1f, math.rcp(sumWeights), sumWeights > 1f);
            var context = new AimContext(ref stream, data);

            float accumWeights = 0f;
            quaternion accumDeltaR = float4.zero;

            for (int i = 0; i < data.SourcePositions.Length; ++i)
            {
                float normalizedWeight = data.SourceWeights[i] * weightScale;
                if (normalizedWeight < k_Epsilon)
                    continue;

                Core.Validate(data.SourceOffsets[i]);

                if (!TryComputeAimDelta(data, context, data.SourcePositions[i], data.SourceOffsets[i], out quaternion rotToSource))
                    continue;

                accumDeltaR = MathUtility.add(accumDeltaR, rotToSource.value * normalizedWeight);
                accumWeights += normalizedWeight;
            }

            ApplyAimConstraint(ref stream, data, context, accumDeltaR, accumWeights, weight);
        }

        /// <summary>
        /// Applies an aim constraint to a bone from a single source.
        /// </summary>
        /// <remarks>
        /// Use this overload when the constraint has one source. It gives the same result as
        /// <see cref="SolveAimConstraint(ref AnimationStream, in AimConstraintData, float)"/> with one source of weight
        /// 1, and you don't need to allocate a <see cref="NativeArray{T}"/> for the sources. The method ignores the
        /// source arrays in <paramref name="data"/>.
        ///
        /// The following example uses the `Unity.Animations` and `Unity.Mathematics` namespaces. It converts world-space
        /// targets to character space and passes them to the single-source solvers.
        /// </remarks>
        /// <example nocheck="true">
        /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/ConstrainToWorldTargetExample.cs#ConstrainToWorldTarget}]]></code>
        /// </example>
        /// <param name="stream">The animation stream to apply the solver to.</param>
        /// <param name="data">The solver settings. The source arrays are ignored.</param>
        /// <param name="sourcePosition">The source position, in character space. To aim at a world-space target, convert it first with <c>math.transform(math.inverse(characterToWorld), worldPosition)</c>.</param>
        /// <param name="sourceOffset">The rotation offset for the source, in the parent space of the constrained bone.
        /// The solver applies it after the rotation that aims the bone at the source.</param>
        /// <param name="weight">The overall weight of the solver, clamped to the range 0 to 1. A weight of 0 leaves the
        /// constrained bone unchanged, and a weight of 1 applies the full constraint. Intermediate values blend the
        /// rotation with a normalized linear interpolation.</param>
        /// <exception cref="System.InvalidOperationException">Thrown if safety checks are enabled and the constrained
        /// bone index or a rotation offset is invalid.</exception>
        public static void SolveAimConstraint(ref AnimationStream stream, in AimConstraintData data, float3 sourcePosition, quaternion sourceOffset, float weight)
        {
            weight = math.saturate(weight);
            if (!(weight > 0f))
                return;

            Core.ValidateGreater(data.Index, -1);
            Core.Validate(data.LocalOffset);
            Core.Validate(sourceOffset);

            var context = new AimContext(ref stream, data);

            float accumWeights = 0f;
            quaternion accumDeltaR = float4.zero;
            if (TryComputeAimDelta(data, context, sourcePosition, sourceOffset, out quaternion rotToSource))
            {
                accumDeltaR = MathUtility.add(accumDeltaR, rotToSource.value);
                accumWeights = 1f;
            }

            ApplyAimConstraint(ref stream, data, context, accumDeltaR, accumWeights, weight);
        }

        struct AimContext
        {
            public quaternion ParentInvR;
            public float3 CurrentT;
            public quaternion CurrentLocalR;
            public float3 CurrentLocalDir;
            public float3 LocalUpAxis;
            public bool HasMasks;
            public bool HasUpAxisCorrection;

            public AimContext(ref AnimationStream stream, in AimConstraintData data)
            {
                int parentIdx = stream.Rig.Skeleton.ParentIndexes[data.Index];
                ParentInvR = parentIdx != -1 ? math.inverse(stream.GetLocalToCharacterRotation(parentIdx)) : quaternion.identity;
                HasMasks = !math.all(data.LocalAxesMask);
                CurrentT = stream.GetLocalToCharacterTranslation(data.Index);
                CurrentLocalR = stream.GetLocalToParentRotation(data.Index);
                CurrentLocalDir = math.mul(CurrentLocalR, data.AimAxis);
                LocalUpAxis = math.select(float3.zero, data.UpAxis, data.LocalAxesMask);
                HasUpAxisCorrection = math.lengthsq(data.RootUpAxis) > k_Epsilon && math.lengthsq(LocalUpAxis) > k_Epsilon;
            }
        }

        static bool TryComputeAimDelta(in AimConstraintData data, in AimContext context, float3 sourcePosition, quaternion sourceOffset, out quaternion rotToSource)
        {
            rotToSource = quaternion.identity;

            float3 toLocalDir = math.mul(context.ParentInvR, sourcePosition - context.CurrentT);
            if (math.lengthsq(toLocalDir) < k_Epsilon)
                return false;

            float3 currentLocalDir = context.CurrentLocalDir;
            float3 fromLocalDir = currentLocalDir;
            float3 crossDir = math.cross(currentLocalDir, toLocalDir);
            if (context.HasMasks)
            {
                crossDir = math.normalizesafe(math.select(float3.zero, crossDir, data.LocalAxesMask), float3.zero);
                if (math.lengthsq(crossDir) > k_Epsilon)
                {
                    fromLocalDir = MathUtility.projectonplane(currentLocalDir, crossDir);
                    toLocalDir = MathUtility.projectonplane(toLocalDir, crossDir);
                }
                else
                {
                    toLocalDir = fromLocalDir;
                }
            }
            else
            {
                // TODO: if crossDir is 0 and the angle between fromLocalDir and toLocalDir is 180, the rotToSource will not be (0, 0, 0, 0). This will probably cause problems.
                crossDir = math.normalizesafe(crossDir, float3.zero);
            }

            rotToSource = quaternion.AxisAngle(
                crossDir,
                MathUtility.angle(fromLocalDir, toLocalDir)
            );

            if (context.HasUpAxisCorrection)
            {
                var worldUpAxisParentSpace = math.mul(context.ParentInvR, data.RootUpAxis);
                var localUpAxisParentSpace = math.mul(rotToSource, math.mul(context.CurrentLocalR, context.LocalUpAxis));
                var wupProject = math.normalize(math.cross(math.cross(worldUpAxisParentSpace, toLocalDir), toLocalDir));
                var rupProject = math.normalize(math.cross(math.cross(localUpAxisParentSpace, toLocalDir), toLocalDir));
                // When wup and rup are opposites we lose axis information, so fall back to toLocalDir.
                rotToSource = math.mul(MathUtility.fromToSafe(rupProject, wupProject, toLocalDir), rotToSource);
            }

            rotToSource = math.mul(sourceOffset, rotToSource);
            return true;
        }

        static void ApplyAimConstraint(
            ref AnimationStream stream,
            in AimConstraintData data,
            in AimContext context,
            quaternion accumDeltaR,
            float accumWeights,
            float weight)
        {
            accumDeltaR = math.normalizesafe(accumDeltaR);
            if (accumWeights < 1f)
                accumDeltaR = math.nlerp(quaternion.identity, accumDeltaR, accumWeights);

            quaternion currentLocalR = context.CurrentLocalR;
            quaternion newLocalR = math.mul(accumDeltaR, currentLocalR);
            if (context.HasMasks)
                newLocalR = quaternion.Euler(math.select(math.Euler(currentLocalR), math.Euler(newLocalR), data.LocalAxesMask));

            stream.SetLocalToParentRotation(
                data.Index,
                math.nlerp(currentLocalR, math.mul(newLocalR, data.LocalOffset), weight)
            );
        }

        /// <summary>
        /// TwistCorrection solver settings.
        /// </summary>
        [GenerateTestsForBurstCompatibility]
        public struct TwistCorrectionData
        {
            /// <summary>The current source rotation that influences the twist nodes to rotate around a specific twist axis.</summary>
            public quaternion SourceRotation;
            /// <summary>The default source rotation at bind time to compute the delta twist rotation to apply on the twist nodes.</summary>
            public quaternion SourceInverseDefaultRotation;

            /// <summary>Specifies the local axis on the source object from which the rotation is extracted and then redistributed to the twist nodes.</summary>
            public float3 LocalTwistAxis;
            /// <summary>
            /// The list of twist node indices that will be influenced by the source rotation. These nodes are expected to be leaf boens in the hierarchy
            /// (i.e. they have a common parent and no children) and should have their twist axes oriented the same as the source in their initial pose.
            /// </summary>
            public NativeArray<int> TwistIndices;
            /// <summary>The list of twist node weights [-1, 1] representating the cumulative percentage of the source twist rotation that should be inherited.</summary>
            public NativeArray<float> TwistWeights; // [-1f, 1f]

            /// <summary>
            /// Returns a set of default values for the twist correction solver.
            /// </summary>
            /// <returns>Default twist correction solver settings.</returns>
            public static TwistCorrectionData Default() => new TwistCorrectionData
            {
                SourceRotation = quaternion.identity,
                SourceInverseDefaultRotation = quaternion.identity,
                LocalTwistAxis = new float3(0f, 1f, 0f)
            };
        }

        /// <summary>
        /// Twist correction solver redistributes a percentage of a source's rotation to one or more twist nodes in order to correct
        /// the twist deformation of a body part that rotates on multiple axes.
        /// </summary>
        /// <param name="stream">The animation stream to apply the solver to.</param>
        /// <param name="data">The solver settings. See <see cref="TwistCorrectionData"/>.</param>
        /// <param name="weight">The overall weight of the solver. If set to 0, the solver has no influence on the twist nodes. When set to 1, it applies full influence with the current settings. Intermediate values are interpolated linearly.</param>
        /// <exception cref="System.InvalidOperationException">Thrown if safety checks are enabled and a twist node
        /// index or a rotation is invalid, or if the twist node buffers don't have the same length.</exception>
        public static void SolveTwistCorrection(ref AnimationStream stream, in TwistCorrectionData data, float weight)
        {
            if (data.TwistIndices.Length == 0)
                return;

            weight = math.saturate(weight);
            if (!(weight > 0f))
                return;

            Core.ValidateBufferLengthsAreEqual(data.TwistIndices.Length, data.TwistWeights.Length);
            Core.Validate(data.SourceRotation);
            Core.Validate(data.SourceInverseDefaultRotation);

            quaternion deltaRot = math.mul(data.SourceInverseDefaultRotation, data.SourceRotation);
            quaternion twistRot = deltaRot.value * math.float4(data.LocalTwistAxis, 1f);
            quaternion invTwistRot = math.conjugate(twistRot);
            var defaultStream = stream.GetDefaultValues();

            for (int i = 0; i < data.TwistIndices.Length; ++i)
            {
                Core.ValidateGreater(data.TwistIndices[i], -1);

                float twistWeight = math.clamp(data.TwistWeights[i], -1f, 1f);
                quaternion rot = math.nlerp(quaternion.identity, MathUtility.select(twistRot, invTwistRot, math.sign(twistWeight) < 0f), math.abs(twistWeight));

                stream.SetLocalToParentRotation(data.TwistIndices[i], math.nlerp(defaultStream.GetLocalToParentRotation(data.TwistIndices[i]), rot, weight));
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void Validate(in quaternion q)
        {
            if (math.lengthsq(q) < math.FLT_MIN_NORMAL)
                throw new System.InvalidOperationException($"Quaternion is invalid {q}");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void Validate(RigidTransform transform) =>
            Validate(transform.rot);
    }
}
