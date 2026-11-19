// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    internal class NodeColorLinePainterAnimator
    {
        const float k_Offset = 0.06f;

        readonly GradientColorKey[] m_BaseColorKeys = new GradientColorKey[2];
        readonly GradientAlphaKey[] m_BaseAlphaKeys = new GradientAlphaKey[2];
        readonly GradientColorKey[] m_AnimColorKeys = new GradientColorKey[6];
        readonly GradientAlphaKey[] m_AnimAlphaKeys = new GradientAlphaKey[6];
        readonly GradientColorKey[] m_FillColorKeys = new GradientColorKey[4];
        readonly GradientAlphaKey[] m_FillAlphaKeys = new GradientAlphaKey[2];

        readonly Gradient m_Gradient = new();
        bool m_IsAnimating = false;
        float m_SegmentOffset = -0.1f;
        float m_AnimationSpeed;
        float m_FillAmount = 0f;

        public bool isAnimating => m_IsAnimating;
        public float fillAmount => m_FillAmount;

        public void SetFillAmount(float percentage,  Action markDirty)
        {
            m_FillAmount = Mathf.Clamp(percentage, -100f, 100f);
            markDirty?.Invoke();
        }

        public void Play(float animationSpeed)
        {
            m_AnimationSpeed = animationSpeed;
            m_SegmentOffset = -0.1f;
            m_IsAnimating = true;
        }

        public void Update(double deltaTime, Action markDirty)
        {
            if (!m_IsAnimating)
                return;

            m_SegmentOffset += (float)deltaTime * m_AnimationSpeed;
            if (m_SegmentOffset > 1.1f)
                m_SegmentOffset = -0.1f;

            markDirty?.Invoke();
        }

        public void Stop(Action markDirty)
        {
            m_IsAnimating = false;
            m_SegmentOffset = -0.1f;
            markDirty?.Invoke();
        }

        public Gradient BuildGradient(Color baseColor)
        {
            Color progressColor = NodeColorLinePart.GetFillAmountColor(baseColor);

            m_Gradient.mode = GradientMode.Fixed;

            if (m_IsAnimating)
            {
                m_AnimColorKeys[0] = new GradientColorKey(baseColor, 0f);
                m_AnimColorKeys[1] = new GradientColorKey(baseColor, m_SegmentOffset - k_Offset - 0.0001f);
                m_AnimColorKeys[2] = new GradientColorKey(progressColor, m_SegmentOffset - k_Offset);
                m_AnimColorKeys[3] = new GradientColorKey(progressColor, m_SegmentOffset + k_Offset);
                m_AnimColorKeys[4] = new GradientColorKey(baseColor, m_SegmentOffset + k_Offset + 0.0001f);
                m_AnimColorKeys[5] = new GradientColorKey(baseColor, 1f);

                m_AnimAlphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
                m_AnimAlphaKeys[1] = new GradientAlphaKey(1.0f, m_SegmentOffset - k_Offset - 0.0001f);
                m_AnimAlphaKeys[2] = new GradientAlphaKey(progressColor.a, m_SegmentOffset - k_Offset);
                m_AnimAlphaKeys[3] = new GradientAlphaKey(progressColor.a, m_SegmentOffset + k_Offset);
                m_AnimAlphaKeys[4] = new GradientAlphaKey(1.0f, m_SegmentOffset + k_Offset + 0.0001f);
                m_AnimAlphaKeys[5] = new GradientAlphaKey(1.0f, 1.0f);

                m_Gradient.SetKeys(m_AnimColorKeys, m_AnimAlphaKeys);
            }
            else if (m_FillAmount != 0f)
            {
                float absNormalized = Mathf.Abs(m_FillAmount) / 100f;

                if (m_FillAmount > 0f)
                {
                    m_FillColorKeys[0] = new GradientColorKey(progressColor, 0.0f);
                    m_FillColorKeys[1] = new GradientColorKey(progressColor, absNormalized);
                    m_FillColorKeys[2] = new GradientColorKey(baseColor, absNormalized + 0.0001f);
                    m_FillColorKeys[3] = new GradientColorKey(baseColor, 1f);
                }
                else
                {
                    float fillStart = 1f - absNormalized;
                    m_FillColorKeys[0] = new GradientColorKey(baseColor, 0.0f);
                    m_FillColorKeys[1] = new GradientColorKey(baseColor, fillStart - 0.0001f);
                    m_FillColorKeys[2] = new GradientColorKey(progressColor, fillStart);
                    m_FillColorKeys[3] = new GradientColorKey(progressColor, 1f);
                }

                m_FillAlphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
                m_FillAlphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);

                m_Gradient.SetKeys(m_FillColorKeys, m_FillAlphaKeys);
            }
            else
            {
                m_BaseColorKeys[0] = new GradientColorKey(baseColor, 0.0f);
                m_BaseColorKeys[1] = new GradientColorKey(baseColor, 1f);

                m_BaseAlphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
                m_BaseAlphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);

                m_Gradient.SetKeys(m_BaseColorKeys, m_BaseAlphaKeys);
            }

            return m_Gradient;
        }
    }
}
