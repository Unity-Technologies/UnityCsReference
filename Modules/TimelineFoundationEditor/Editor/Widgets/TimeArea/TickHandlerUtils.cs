// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Timeline.Foundation.Widgets.Internals
{
    static class TickHandlerUtils
    {
        static readonly float[] k_DefaultTicksInSeconds =
        {
            1, 5, 10, 30, 60,
            60 * 5, 60 * 10, 60 * 30,
            3600, 3600 * 6, 3600 * 24, 3600 * 24 * 7, 3600 * 24 * 14
        };

        // Code taken from Editor/Mono/Animation/TickHandler.cs
        public static float[] GetTickModulosForFrameRate(float frameRate)
        {
            // Make frames multiples of 5 and 10, if frameRate is too high (avoid overflow) or not an even number
            if (frameRate > int.MaxValue / 2.0f || frameRate != Mathf.Round(frameRate))
            {
                return new []
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

            int defaultTicksLength = k_DefaultTicksInSeconds.Length;
            var modulos = new float[defaultTicksLength + dividers.Count];

            for (int i = 0; i < dividers.Count; i++)
                modulos[i] = 1f / dividers[dividers.Count - i - 1];

            Array.Copy(k_DefaultTicksInSeconds, 0, modulos, dividers.Count, defaultTicksLength);

            return modulos;
        }
    }
}
