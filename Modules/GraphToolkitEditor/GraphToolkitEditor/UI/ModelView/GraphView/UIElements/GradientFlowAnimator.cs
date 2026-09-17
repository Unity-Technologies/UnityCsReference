// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Drives the moving gradient band animation shared by <see cref="WireControl"/> and <see cref="TransitionControl"/>,
    /// and, on the same clock, a self transition's band traveling around its arrow's outline, on
    /// <see cref="TransitionArrow"/>.
    /// </summary>
    internal class GradientFlowAnimator
    {
        [NoAutoStaticsCleanup] // single reusable Gradient buffer for gradient flow interpolation; mutated each draw call, no user-type references
        static readonly Gradient k_Gradient = new();

        const float k_MinOpacity = 0.6f;
        const float k_Offset = 0.1f;
        const float k_MaxSegmentOffset = 1.3f;
        const float k_InitialSegmentOffset = -0.3f;

        readonly GradientColorKey[] m_ColorKeys = new GradientColorKey[2];
        readonly GradientAlphaKey[] m_AnimAlphaKeys = new GradientAlphaKey[6];
        readonly GradientAlphaKey[] m_FillAlphaKeys = new GradientAlphaKey[1];

        float m_AnimationSpeed;

        /// <summary>
        /// Whether the gradient flow animation is currently playing.
        /// </summary>
        public bool IsAnimating { get; private set; }

        /// <summary>
        /// The current position of the animated band, in the [<see cref="k_InitialSegmentOffset"/>, <see cref="k_MaxSegmentOffset"/>] range.
        /// </summary>
        public float SegmentOffset { get; private set; } = k_InitialSegmentOffset;

        /// <summary>
        /// Starts the animation, or updates its speed if it is already playing.
        /// </summary>
        /// <param name="animationSpeed">Speed at which the animated band advances.</param>
        public void BeginAnimating(float animationSpeed)
        {
            m_AnimationSpeed = animationSpeed;
            IsAnimating = true;
        }

        /// <summary>
        /// Stops the animation.
        /// </summary>
        public void StopAnimating()
        {
            IsAnimating = false;
        }

        /// <summary>
        /// Advances the animated band by the given elapsed time.
        /// </summary>
        /// <param name="deltaTime">Elapsed time in seconds since the last update.</param>
        public void AnimationUpdate(double deltaTime)
        {
            if (!IsAnimating)
                return;

            SegmentOffset += (float)deltaTime * m_AnimationSpeed;
            if (SegmentOffset > k_MaxSegmentOffset)
                SegmentOffset = k_InitialSegmentOffset;
        }

        /// <summary>
        /// Builds the shared stroke gradient: a moving alpha band around <see cref="SegmentOffset"/> while animating,
        /// or a single flat alpha otherwise.
        /// </summary>
        /// <param name="startColor">The color at the start of the gradient.</param>
        /// <param name="endColor">The color at the end of the gradient.</param>
        /// <param name="alpha">The alpha at the peak of the band, or applied uniformly when not animating.</param>
        /// <returns>The reusable <see cref="Gradient"/> instance, ready to assign to <c>Painter2D.strokeGradient</c>.</returns>
        public Gradient BuildStrokeGradient(Color startColor, Color endColor, float alpha)
        {
            m_ColorKeys[0] = new GradientColorKey(startColor, 0);
            m_ColorKeys[1] = new GradientColorKey(endColor, 1);

            if (IsAnimating)
            {
                var minTransparency = k_MinOpacity * alpha;
                m_AnimAlphaKeys[0] = new GradientAlphaKey(minTransparency, 0.0f);
                m_AnimAlphaKeys[1] = new GradientAlphaKey(minTransparency, SegmentOffset - k_Offset * 2);
                m_AnimAlphaKeys[2] = new GradientAlphaKey(alpha, SegmentOffset - k_Offset);
                m_AnimAlphaKeys[3] = new GradientAlphaKey(alpha, SegmentOffset + k_Offset);
                m_AnimAlphaKeys[4] = new GradientAlphaKey(minTransparency, SegmentOffset + k_Offset * 2);
                m_AnimAlphaKeys[5] = new GradientAlphaKey(minTransparency, 1.0f);

                k_Gradient.SetKeys(m_ColorKeys, m_AnimAlphaKeys);
            }
            else
            {
                m_FillAlphaKeys[0] = new GradientAlphaKey(alpha, 0);

                k_Gradient.SetKeys(m_ColorKeys, m_FillAlphaKeys);
            }

            return k_Gradient;
        }
    }
}
