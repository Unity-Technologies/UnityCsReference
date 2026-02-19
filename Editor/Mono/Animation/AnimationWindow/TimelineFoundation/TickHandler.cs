// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Animations.AnimationWindow.TimelineFoundation
{
    sealed class TickHandler
    {
        public const float defaultTickRulerFatThreshold = 0.5f; // size of ruler tick marks at which they begin getting fatter

        const int k_TickRulerDistMin = 3; // min distance between ruler tick marks before they disappear completely
        const int k_TickRulerDistFull = 80; // distance between ruler tick marks where they gain full strength
        const int k_TickRulerDistRange = k_TickRulerDistFull - k_TickRulerDistMin;

        // defaultModulos taken from Editor/Mono/Animation/TimeArea.cs
        static readonly float[] k_DefaultModulos =
        {
            0.0000001f, 0.0000005f, 0.000001f, 0.000005f, 0.00001f, 0.00005f, 0.0001f, 0.0005f, 0.001f, 0.005f, 0.01f, 0.05f, 0.1f, 0.5f,
            1, 5, 10, 50, 100, 500, 1000, 5000, 10000, 50000, 100000, 500000, 1000000, 5000000, 10000000
        };

        // Variables related to drawing tick markers
        float[] m_TickModulos;  // array with possible modulo numbers to choose from
        float[] m_TickStrengths;  // array with current strength of each modulo number
        int m_SmallestTickIndex; // index of the currently smallest modulo number used to draw ticks
        int m_BiggestTickIndex = -1; // index of the currently biggest modulo number used to draw ticks
        float m_MinValue; // shownArea min (in curve space)
        float m_MaxValue = 1; // shownArea max (in curve space)
        float valueRange => m_MaxValue - m_MinValue;
        float m_PixelRange = 1; // total width/height of the time area

        public int tickLevels => m_BiggestTickIndex - m_SmallestTickIndex + 1;

        public TickHandler()
        {
            SetTickModulos(k_DefaultModulos);
        }

        public void SetTickModulosForFrameRate(float frameRate)
        {
            SetTickModulos(TickHandlerUtils.GetTickModulosForFrameRate(frameRate));
        }

        public void SetRanges(float min, float max, float width)
        {
            m_MinValue = min;
            m_MaxValue = max;
            m_PixelRange = width;
            ComputeTickStrengths();
        }

        public void GetTicksAtLevel(int level, bool excludeTicksFromHigherLevels, List<float> list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            int clampedLevel = Mathf.Clamp(m_SmallestTickIndex + level, 0, m_TickModulos.Length - 1);
            int startTick = Math.Max(0, Mathf.FloorToInt(m_MinValue / m_TickModulos[clampedLevel]));
            int endTick = Math.Max(0, Mathf.CeilToInt(m_MaxValue / m_TickModulos[clampedLevel]));

            for (int i = startTick; i <= endTick; i++)
            {
                // Skip if tick mark is at same time as larger tick mark
                if (excludeTicksFromHigherLevels
                    && clampedLevel < m_BiggestTickIndex
                    && i % Mathf.RoundToInt(m_TickModulos[clampedLevel + 1] / m_TickModulos[clampedLevel]) == 0)
                {
                    continue;
                }

                list.Add(i * m_TickModulos[clampedLevel]);
            }
        }

        public float GetStrengthOfLevel(int level)
        {
            return m_TickStrengths[m_SmallestTickIndex + level];
        }

        public int GetLevelWithMinSeparation(float pixelSeparation)
        {
            for (int i = 0; i < m_TickModulos.Length; i++)
            {
                // How far apart (in pixels) these modulo ticks are spaced:
                float tickSpacing = TickSpacingForModulo(i);
                if (tickSpacing >= pixelSeparation)
                    return i - m_SmallestTickIndex;
            }

            return -1;
        }

        void SetTickModulos(float[] tickModulos)
        {
            m_TickModulos = tickModulos;
            m_TickStrengths = new float[m_TickModulos.Length];
            ComputeTickStrengths();
        }

        void ComputeTickStrengths()
        {
            m_SmallestTickIndex = 0;
            m_BiggestTickIndex = m_TickModulos.Length - 1;

            // Find the strength for each modulo number tick marker
            for (int i = m_TickModulos.Length - 1; i >= 0; i--)
            {
                // How far apart (in pixels) these modulo ticks are spaced:
                float tickSpacing = TickSpacingForModulo(i);

                // Calculate the strength of the tick markers based on the spacing:
                m_TickStrengths[i] = (tickSpacing - k_TickRulerDistMin) / k_TickRulerDistRange;

                if (m_TickStrengths[i] >= m_TickStrengths[m_BiggestTickIndex])
                    m_BiggestTickIndex = i;

                // Do not show tick markers less than 3 pixels apart:
                if (tickSpacing <= k_TickRulerDistMin)
                {
                    m_SmallestTickIndex = i + 1;
                    break;
                }
            }

            // Use sqrt on actively used modulo number tick markers
            for (int i = m_SmallestTickIndex; i <= m_BiggestTickIndex; i++)
            {
                m_TickStrengths[i] = Mathf.Sqrt(Mathf.Clamp01(m_TickStrengths[i]));
            }
        }

        float TickSpacingForModulo(int i)
        {
            return m_TickModulos[i] * m_PixelRange / valueRange;
        }
    }
}
