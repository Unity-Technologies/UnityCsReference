// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Mathematics;

namespace Unity.Animations
{
    /// <summary>
    /// The root motion of a clip from the previous time of a timer to its current time, and the root transform of the
    /// clip at the current time.
    /// </summary>
    /// <remarks>
    /// <see cref="ClipSampler.SampleRootMotion(in TimerState)"/> returns this struct. Apply
    /// <see cref="RootMotion"/> to the transform of the character to move it. Sample the pose at the same time with
    /// <see cref="ClipSampler.SampleExtractingRootMotion(ref AnimationStream, in TimerState)"/> to remove the root
    /// motion from the pose, so that the character doesn't move twice.
    /// </remarks>
    /// <seealso cref="ClipSampler"/>
    public struct MotionOutput
    {
        internal static MotionOutput Identity => new MotionOutput
        {
            RootMotion = RigidTransform.identity,
            RootTransform = RigidTransform.identity,
        };

        /// <summary>
        /// The motion of the root from the previous time of the timer to its current time.
        /// </summary>
        /// <remarks>
        /// The value is the root transform at the current time, relative to the root transform at the previous time.
        /// For a looping clip, it includes the whole cycles that the timer crossed. Accumulate it after the transform
        /// of the character, for example `character = math.mul(character, output.RootMotion)`.
        /// </remarks>
        public RigidTransform RootMotion;

        /// <summary>
        /// The transform of the root in character space at the current time of the timer.
        /// </summary>
        /// <remarks>
        /// The value is the transform of the root on the curves of the clip, after the root motion settings of the clip.
        /// It doesn't include the cycles that a looping clip completed.
        /// <see cref="ClipSampler.SampleExtractingRootMotion(ref AnimationStream, in TimerState)"/> expresses the root
        /// motion bone of the pose relative to this transform. For a pose that you build yourself, pass it to
        /// <see cref="AnimationStream.ExtractRootMotionBone(float3, quaternion)"/>.
        /// </remarks>
        public RigidTransform RootTransform;
    }
}
