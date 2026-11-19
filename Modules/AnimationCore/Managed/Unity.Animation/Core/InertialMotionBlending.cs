// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace Unity.Animations
{
    [GenerateTestsForBurstCompatibility]
    internal static partial class MathUtility
    {
        /// <summary>
        /// Unwind angle (in radians) to be between [-PI, PI] range. Only works if the angle is already in the [-3PI, 3PI] range.
        /// </summary>
        /// <param name="a">angle in radians, must be in the [-3PI, 3PI] range</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float unwind_once(float a)
        {
            if (a < -math.PI)
            {
                a += math.PI2;
            }

            if (a > math.PI)
            {
                a -= math.PI2;
            }

            return a;
        }
    }

    [GenerateTestsForBurstCompatibility]
    static public partial class Core
    {
        internal static void InertializeVector(
            float3 secondLastVector,
            float3 lastVector,
            float3 currentVector,
            float deltaTime,
            float duration,
            out float3 direction,
            out InertialBlendingCoefficientsData coefficients
        )
        {
            var translation = lastVector - currentVector;
            var x0 = math.length(translation);
            direction = math.normalizesafe(translation, float3.zero);

            var previousTranslation = secondLastVector - currentVector;
            var previousMag = math.dot(previousTranslation, direction);
            var v0 = (x0 - previousMag) / deltaTime;
            coefficients = new InertialBlendingCoefficientsData(x0, v0, duration);
        }

        internal static void InertializeQuaternion(
            quaternion secondLastQuaternion,
            quaternion lastQuaternion,
            quaternion currentQuaternion,
            float deltaTime,
            float duration,
            out float3 axis,
            out InertialBlendingCoefficientsData coefficients
        )
        {
            var currentQuaternionInverse = math.inverse(currentQuaternion);
            quaternion rotation = math.normalize(math.mul(lastQuaternion, currentQuaternionInverse));
            axis = MathUtility.axis(rotation);
            float angle = MathUtility.angle(rotation);

            // Ensure that rotations are the shortest possible
            if (angle > math.PI)
            {
                angle = 2f * math.PI - angle;
                axis = -axis;
            }

            quaternion previousRotation = math.mul(secondLastQuaternion, currentQuaternionInverse);
            float previousAngle = MathUtility.unwind_once(2f * math.atan2(math.dot(previousRotation.value.xyz, axis), previousRotation.value.w));
            var v0 = MathUtility.unwind_once(angle - previousAngle) / deltaTime;
            coefficients = new InertialBlendingCoefficientsData(angle, v0, duration);
        }

        internal static void InertializeFloat(
            float secondLastFloat,
            float lastFloat,
            float currentFloat,
            float deltaTime,
            float duration,
            out InertialBlendingCoefficientsData coefficients
        )
        {
            var x0 = lastFloat - currentFloat;
            var v0 = (lastFloat - secondLastFloat) / deltaTime;
            coefficients = new InertialBlendingCoefficientsData(x0, v0, duration);
        }

        /// <summary>
        /// Computes the inertial blending coefficients of every channel of a rig at the start of a transition.
        /// </summary>
        /// <remarks>
        /// Call this method once when a transition to a new pose starts, then call <see cref="InertialBlend"/> on each
        /// update until the blend ends. An inertial blend is an alternative to a cross-fade. To compute the
        /// coefficients,
        /// this method needs the first pose of the destination animation and the last two output poses. After the
        /// transition starts, `InertialBlend` needs only the current pose of the destination animation. The blend
        /// never samples
        /// the source animation during the transition. Compared with a cross-fade, this approach saves a clip sample on
        /// each update. The blend also works with poses from any source, such as poses that procedural constraints
        /// modify.
        ///
        /// The method computes the velocity of each channel from the last two output poses. The blend starts with that
        /// velocity, so the motion stays continuous. The blend doesn't take the motion limits of the bones of your
        /// character into account. With a fast motion and a long blend, the result can exceed those limits. To stay
        /// within
        /// the limits, use a shorter duration or correct the bones after the blend. Because the blend depends on the
        /// previous poses, it's also sensitive to sudden changes of the time step.
        ///
        /// The method writes the coefficients of the translation, rotation, scale, and float channels, in this order,
        /// to <paramref name="outCoefficients"/>. It writes the direction of the offset of the translation, rotation,
        /// and
        /// scale channels, in this order, to <paramref name="outDirections"/>. A rotation direction is a rotation axis.
        ///
        /// The following example requires a rig definition. It uses the `Unity.Animations`, `Unity.Collections`,
        /// `Unity.Entities`, and `Unity.Mathematics` namespaces. It computes the coefficients for a switch to a new
        /// pose,
        /// then applies the blend with `InertialBlend` at a given elapsed time. It also returns the
        /// translation of the blended pose and the offset of the first channel.
        /// </remarks>
        /// <example nocheck="true">
        /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/InertialBlendExample.cs#InertialBlendExample}]]></code>
        /// </example>
        /// <param name="currentInput">The pose of the destination animation at the start of the transition.</param>
        /// <param name="lastOutput">The output pose of the previous update. It must use the same rig as
        /// <paramref name="currentInput"/>.</param>
        /// <param name="secondLastOutput">The output pose of the update before the previous one. It must use the same
        /// rig
        /// as <paramref name="currentInput"/>.</param>
        /// <param name="deltaTime">The time between <paramref name="secondLastOutput"/> and
        /// <paramref name="lastOutput"/>,
        /// in seconds. It must be greater than 0.</param>
        /// <param name="duration">The requested duration of the blend, in seconds. It must be 0 or greater. A channel
        /// finishes sooner than this duration if the offset of the channel moves toward zero quickly at the
        /// start.</param>
        /// <param name="outCoefficients">The buffer that receives the coefficients. Its length must equal the value
        /// that
        /// <see cref="InertialBlendingCoefficientsData.RequiredCoefficientBufferSize"/> returns for the rig of
        /// <paramref name="currentInput"/>.</param>
        /// <param name="outDirections">The buffer that receives the offset directions. Its length must equal the value
        /// that <see cref="InertialBlendingCoefficientsData.RequiredDirectionBufferSize"/> returns for the rig of
        /// <paramref name="currentInput"/>.</param>
        public static void ComputeInertialBlendingCoefficients(
            ref AnimationStream currentInput,
            ref AnimationStream lastOutput,
            ref AnimationStream secondLastOutput,
            float deltaTime,
            float duration,
            NativeArray<InertialBlendingCoefficientsData> outCoefficients,
            NativeArray<float3> outDirections
        )
        {
            currentInput.ValidateIsNotNull();
            currentInput.ValidateRigEquality(lastOutput);
            currentInput.ValidateRigEquality(secondLastOutput);

            Core.ValidateBufferLengthsAreEqual(currentInput.TranslationCount + currentInput.RotationCount + currentInput.ScaleCount + currentInput.FloatCount, outCoefficients.Length);
            Core.ValidateBufferLengthsAreEqual(currentInput.TranslationCount + currentInput.RotationCount + currentInput.ScaleCount, outDirections.Length);
            Core.ValidateGreater(deltaTime, 0.0f);
            Core.ValidateGreaterOrEqual(duration, 0f);

            int translationOffset = InertialBlendingCoefficientsData.GetTranslationsOffset(currentInput.m_Rig);
            for (int i = 0; i < currentInput.TranslationCount; i++)
            {
                InertializeVector(
                    secondLastOutput.GetLocalToParentTranslation(i),
                    lastOutput.GetLocalToParentTranslation(i),
                    currentInput.GetLocalToParentTranslation(i),
                    deltaTime,
                    duration,
                    out var direction,
                    out var coefficients);

                var idx = i + translationOffset;
                outCoefficients[idx] = coefficients;
                outDirections[idx] = direction;
            }

            int rotationOffset = InertialBlendingCoefficientsData.GetRotationsOffset(currentInput.m_Rig);
            for (int i = 0; i < currentInput.RotationCount; i++)
            {
                InertializeQuaternion(
                    secondLastOutput.GetRotation(i),
                    lastOutput.GetRotation(i),
                    currentInput.GetRotation(i),
                    deltaTime,
                    duration,
                    out var axis,
                    out var coefficients);

                var idx = i + rotationOffset;
                outCoefficients[idx] = coefficients;
                outDirections[idx] = axis;
            }

            int scaleOffset = InertialBlendingCoefficientsData.GetScalesOffset(currentInput.m_Rig);
            for (int i = 0; i < currentInput.ScaleCount; i++)
            {
                InertializeVector(
                    secondLastOutput.GetLocalToParentScale(i),
                    lastOutput.GetLocalToParentScale(i),
                    currentInput.GetLocalToParentScale(i),
                    deltaTime,
                    duration,
                    out var direction,
                    out var coefficients);

                var idx = i + scaleOffset;
                outCoefficients[idx] = coefficients;
                outDirections[idx] = direction;
            }

            int floatOffset = InertialBlendingCoefficientsData.GetFloatsOffset(currentInput.m_Rig);
            for (int i = 0; i < currentInput.FloatCount; i++)
            {
                InertializeFloat(secondLastOutput.GetFloat(i), lastOutput.GetFloat(i), currentInput.GetFloat(i), deltaTime, duration, out var coefficients);
                outCoefficients[i + floatOffset] = coefficients;
            }
        }

        /// <summary>
        /// Applies an inertial blend to an input pose and writes the blended pose to an output stream.
        /// </summary>
        /// <remarks>
        /// Call this method on each update of a transition, after you compute the coefficients once with
        /// <see cref="ComputeInertialBlendingCoefficients"/>. For each translation, rotation, scale, and float channel,
        /// the method applies the offset at the current blend time to the value in <paramref name="currentInput"/>. It
        /// writes the result to <paramref name="outputPose"/>. The method doesn't blend
        /// integer channels, because discrete values can't be blended. On every call, the method copies the integer
        /// values of <paramref name="currentInput"/>, so they switch to the destination values when the blend starts.
        ///
        /// The following example requires a rig definition. It uses the `Unity.Animations`, `Unity.Collections`,
        /// `Unity.Entities`, and `Unity.Mathematics` namespaces. It computes the coefficients with
        /// `ComputeInertialBlendingCoefficients` for a switch to a new pose, then applies the blend at a
        /// given elapsed time. It also returns the translation of the blended pose and the offset of the first
        /// channel.
        /// </remarks>
        /// <example nocheck="true">
        /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/InertialBlendExample.cs#InertialBlendExample}]]></code>
        /// </example>
        /// <param name="currentInput">The pose of the destination animation for the current update.</param>
        /// <param name="outputPose">The stream that receives the blended pose. It must use the same rig as
        /// <paramref name="currentInput"/>.</param>
        /// <param name="interpolationFactors">The coefficients from `ComputeInertialBlendingCoefficients`,
        /// computed once at the start of the transition.</param>
        /// <param name="interpolationDirections">The offset directions from
        /// `ComputeInertialBlendingCoefficients`, computed once at the start of the transition.</param>
        /// <param name="duration">The duration of the blend, in seconds, as passed to
        /// `ComputeInertialBlendingCoefficients`.</param>
        /// <param name="remainingTime">The time left in the blend, in seconds, from 0 to <paramref name="duration"/>.
        /// At 0, the output pose matches the input pose.</param>
        public static void InertialBlend(
            ref AnimationStream currentInput,
            ref AnimationStream outputPose,
            NativeArray<InertialBlendingCoefficientsData>.ReadOnly interpolationFactors,
            NativeArray<float3>.ReadOnly interpolationDirections,
            float duration,
            float remainingTime
        )
        {
            // TODO: Im 90% certain we can implement this as an operation on a single stream, and avoid allocating a whole other stream for the output
            currentInput.ValidateIsNotNull();
            outputPose.ValidateRigEquality(currentInput);

            Core.ValidateBufferLengthsAreEqual(currentInput.TranslationCount + currentInput.RotationCount + currentInput.ScaleCount + currentInput.FloatCount, interpolationFactors.Length);
            Core.ValidateBufferLengthsAreEqual(currentInput.TranslationCount + currentInput.RotationCount + currentInput.ScaleCount, interpolationDirections.Length);
            Core.ValidateGreaterOrEqual(duration, remainingTime);
            Core.ValidateGreaterOrEqual(remainingTime, 0f);

            var t = duration - remainingTime;
            var timePowers = InertialBlendingCoefficientsData.ComputeTimePowers(t);

            int translationOffset = InertialBlendingCoefficientsData.GetTranslationsOffset(currentInput.m_Rig);
            for (int i = 0; i < currentInput.TranslationCount; i++)
            {
                var idx = i + translationOffset;
                var factors = interpolationFactors[idx];
                var xInterpolated = factors.Interpolate(timePowers, t);
                var correction = interpolationDirections[idx] * xInterpolated;
                outputPose.SetLocalToParentTranslation(i, correction + currentInput.GetLocalToParentTranslation(i));
            }

            int rotationOffset = InertialBlendingCoefficientsData.GetRotationsOffset(currentInput.m_Rig);
            for (int i = 0; i < currentInput.RotationCount; i++)
            {
                var idx = i + rotationOffset;
                var factors = interpolationFactors[idx];
                var xInterpolated = factors.Interpolate(timePowers, t);
                var correction = quaternion.AxisAngle(interpolationDirections[idx], xInterpolated);
                outputPose.SetRotation(i, math.mul(correction, currentInput.GetRotation(i)));
            }

            int scaleOffset = InertialBlendingCoefficientsData.GetScalesOffset(currentInput.m_Rig);
            for (int i = 0; i < currentInput.ScaleCount; i++)
            {
                var idx = i + scaleOffset;
                var factors = interpolationFactors[idx];
                var xInterpolated = factors.Interpolate(timePowers, t);
                var correction = interpolationDirections[idx] * xInterpolated;
                outputPose.SetLocalToParentScale(i, correction + currentInput.GetLocalToParentScale(i));
            }

            int floatOffset = InertialBlendingCoefficientsData.GetFloatsOffset(currentInput.m_Rig);
            for (int i = 0; i < currentInput.FloatCount; i++)
            {
                var idx = i + floatOffset;
                var factors = interpolationFactors[idx];
                var correction = factors.Interpolate(timePowers, t);
                outputPose.SetFloat(i, correction + currentInput.GetFloat(i));
            }
            // Why do we not blend ints at all?
            // Ints make no sense to blend since they are discrete values. So all that we can choose is when to
            // switch from the source to the dest clip. There are 3 possibilities : at the beginning, somewhere in the middle and at the end.
            // The two last ones would require sampling the source clip during the transition, which breaks the performance guaranties
            // of inertial motion blending. All we are left with is to change int channels at the start of the blend.
            for (int i = 0; i < currentInput.IntCount; i++)
            {
                // TODO: Optimize with a memcopy
                outputPose.SetInt(i, currentInput.GetInt(i));
            }
        }
    }
}
