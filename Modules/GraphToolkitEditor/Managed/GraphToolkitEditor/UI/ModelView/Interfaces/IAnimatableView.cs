// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Playback control and stepping for animatable <see cref="GraphElement"/> views.
    /// </summary>
    interface IAnimatableView
    {
        /// <summary>
        /// Starts animation playback for this element, or updates playback speed if it is already playing
        /// (for example when <see cref="GraphAnimator.Play"/> is invoked again before
        /// <see cref="StopAnimating"/>).
        /// </summary>
        /// <param name="animationSpeed">Speed at which the element will play its animation.</param>
        void BeginAnimating(float animationSpeed);

        /// <summary>
        /// Stops animation playback for this element.
        /// </summary>
        void StopAnimating();

        /// <summary>
        /// Advances animation state by the given elapsed time.
        /// </summary>
        /// <param name="deltaTime">Elapsed time in seconds since the last update.</param>
        void AnimationUpdate(double deltaTime);
    }
}
