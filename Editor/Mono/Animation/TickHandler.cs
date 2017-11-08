// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor
{
    [System.Serializable]
    internal class TickHandler
    {
        // Variables related to drawing tick markers
        [SerializeField] private float[] m_TickModulos = new float[] {}; // array with possible modulo numbers to choose from
        [SerializeField] private float[] m_TickStrengths = new float[] {}; // array with current strength of each modulo number
        [SerializeField] private int m_SmallestTick = 0; // index of the currently smallest modulo number used to draw ticks
        [SerializeField] private int m_BiggestTick = -1; // index of the currently biggest modulo number used to draw ticks
        [SerializeField] private float m_MinValue = 0; // shownArea min (in curve space)
        [SerializeField] private float m_MaxValue = 1; // shownArea max (in curve space)
        [SerializeField] private float m_PixelRange = 1; // total width/height of curveeditor

        public int tickLevels { get { return m_BiggestTick - m_SmallestTick + 1; } }

        public void SetTickModulos(float[] tickModulos)
        {
            m_TickModulos = tickModulos;
        }

        public List<float> GetTickModulosForFrameRate(float frameRate)
        {
            List<float> modulos;

            // Make frames multiples of 5 and 10, if frameRate is too high (avoid overflow) or not an even number
            if (frameRate > int.MaxValue / 2.0f || frameRate != Mathf.Round(frameRate))
            {
                modulos = new List<float>
                {
                    1f / frameRate,
                    5f / frameRate,
                    10f / frameRate,
                    50f / frameRate,
                    100f / frameRate,
                    500f / frameRate,
                    1000f / frameRate,
                    5000f / frameRate,
                    10000f / frameRate,
                    50000f / frameRate,
                    100000f / frameRate,
                    500000f / frameRate
                };

                return modulos;
            }

            List<int> dividers = new List<int>();
            int divisor = 1;
            while (divisor < frameRate)
            {
                if (Math.Abs(divisor - frameRate) < 1e-5)
                    break;
                int multiple = Mathf.RoundToInt(frameRate / divisor);
                if (multiple % 60 == 0)
                {
                    divisor *= 2;
                    dividers.Add(divisor);
                }
                else if (multiple % 30 == 0)
                {
                    divisor *= 3;
                    dividers.Add(divisor);
                }
                else if (multiple % 20 == 0)
                {
                    divisor *= 2;
                    dividers.Add(divisor);
                }
                else if (multiple % 10 == 0)
                {
                    divisor *= 2;
                    dividers.Add(divisor);
                }
                else if (multiple % 5 == 0)
                {
                    divisor *= 5;
                    dividers.Add(divisor);
                }
                else if (multiple % 2 == 0)
                {
                    divisor *= 2;
                    dividers.Add(divisor);
                }
                else if (multiple % 3 == 0)
                {
                    divisor *= 3;
                    dividers.Add(divisor);
                }
                else
                    divisor = Mathf.RoundToInt(frameRate);
            }
            modulos = new List<float>(13 + dividers.Count);

            for (int i = 0; i < dividers.Count; i++)
                modulos.Add(1f / dividers[dividers.Count - i - 1]);

            // Ticks based on seconds
            modulos.Add(1);
            modulos.Add(5);
            modulos.Add(10);
            modulos.Add(30);
            modulos.Add(60);
            modulos.Add(60 * 5);
            modulos.Add(60 * 10);
            modulos.Add(60 * 30);
            modulos.Add(3600);
            modulos.Add(3600 * 6);
            modulos.Add(3600 * 24);
            modulos.Add(3600 * 24 * 7);
            modulos.Add(3600 * 24 * 14);
            return modulos;
        }

        public void SetTickModulosForFrameRate(float frameRate)
        {
            var modulos = GetTickModulosForFrameRate(frameRate);
            SetTickModulos(modulos.ToArray());
        }

        public void SetRanges(float minValue, float maxValue, float minPixel, float maxPixel)
        {
            m_MinValue = minValue;
            m_MaxValue = maxValue;
            m_PixelRange = maxPixel - minPixel;
        }

        public float[] GetTicksAtLevel(int level, bool excludeTicksFromHigherlevels)
        {
            if (level < 0)
                return new float[0] {};

            int l = Mathf.Clamp(m_SmallestTick + level, 0, m_TickModulos.Length - 1);
            List<float> ticks = new List<float>();
            int startTick = Mathf.FloorToInt(m_MinValue / m_TickModulos[l]);
            int endTick = Mathf.CeilToInt(m_MaxValue / m_TickModulos[l]);
            for (int i = startTick; i <= endTick; i++)
            {
                // Return if tick mark is at same time as larger tick mark
                if (excludeTicksFromHigherlevels
                    && l < m_BiggestTick
                    && (i % Mathf.RoundToInt(m_TickModulos[l + 1] / m_TickModulos[l]) == 0))
                    continue;
                ticks.Add(i * m_TickModulos[l]);
            }
            return ticks.ToArray();
        }

        public float GetStrengthOfLevel(int level)
        {
            return m_TickStrengths[m_SmallestTick + level];
        }

        public float GetPeriodOfLevel(int level)
        {
            return m_TickModulos[Mathf.Clamp(m_SmallestTick + level, 0, m_TickModulos.Length - 1)];
        }

        public int GetLevelWithMinSeparation(float pixelSeparation)
        {
            for (int i = 0; i < m_TickModulos.Length; i++)
            {
                // How far apart (in pixels) these modulo ticks are spaced:
                float tickSpacing = m_TickModulos[i] * m_PixelRange / (m_MaxValue - m_MinValue);
                if (tickSpacing >= pixelSeparation)
                    return i - m_SmallestTick;
            }
            return -1;
        }

        public void SetTickStrengths(float tickMinSpacing, float tickMaxSpacing, bool sqrt)
        {
            m_TickStrengths = new float[m_TickModulos.Length];
            m_SmallestTick = 0;
            m_BiggestTick = m_TickModulos.Length - 1;

            // Find the strength for each modulo number tick marker
            for (int i = m_TickModulos.Length - 1; i >= 0; i--)
            {
                // How far apart (in pixels) these modulo ticks are spaced:
                float tickSpacing = m_TickModulos[i] * m_PixelRange / (m_MaxValue - m_MinValue);

                // Calculate the strength of the tick markers based on the spacing:
                m_TickStrengths[i] =
                    (tickSpacing - tickMinSpacing) / (tickMaxSpacing - tickMinSpacing);

                // Beyond kTickHeightFatThreshold the ticks don't get any bigger or fatter,
                // so ignore them, since they are already covered by smalle modulo ticks anyway:
                if (m_TickStrengths[i] >= 1) m_BiggestTick = i;

                // Do not show tick markers less than 3 pixels apart:
                if (tickSpacing <= tickMinSpacing) { m_SmallestTick = i; break; }
            }

            // Use sqrt on actively used modulo number tick markers
            for (int i = m_SmallestTick; i <= m_BiggestTick; i++)
            {
                m_TickStrengths[i] = Mathf.Clamp01(m_TickStrengths[i]);
                if (sqrt)
                    m_TickStrengths[i] = Mathf.Sqrt(m_TickStrengths[i]);
            }
        }
    }
} // namespace
