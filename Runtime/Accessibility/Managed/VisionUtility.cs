// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEngine.Accessibility
{
    [UsedByNativeCode]
    public static class VisionUtility
    {
        // http://www.somersault1824.com/wp-content/uploads/2015/02/color-blindness-palette.png
        // colors are ordered across rows to maximize contrast with small palettes
        private static readonly Color[] s_ColorBlindSafePalette = new[]
        {
            (Color) new Color32(0, 0, 0, 255),
            (Color) new Color32(73, 0, 146, 255),
//            (Color) new Color32(146, 0, 0, 255), // doesn't look good in editor

            (Color) new Color32(7, 71, 81, 255),
//            (Color) new Color32(0, 109, 219, 255), // tritanopia ambiguous 1
//            (Color) new Color32(146, 73, 0, 255), // doesn't look good in editor

            (Color) new Color32(0, 146, 146, 255),   // tritanopia ambiguous 1
            (Color) new Color32(182, 109, 255, 255),
//            (Color) new Color32(219, 109, 0, 255), // tritanopia ambiguous 2; also doesn't look good in editor

            (Color) new Color32(255, 109, 182, 255), // tritanopia ambiguous 2
            (Color) new Color32(109, 182, 255, 255),
            (Color) new Color32(36, 255, 36, 255),

            (Color) new Color32(255, 182, 219, 255),
            (Color) new Color32(182, 219, 255, 255),
            (Color) new Color32(255, 255, 109, 255),
        };

        private static readonly float[] s_ColorBlindSafePaletteLuminanceValues =
            s_ColorBlindSafePalette.Select(c => ComputePerceivedLuminance(c)).ToArray();

        // https://en.wikipedia.org/wiki/Relative_luminance
        private static float ComputePerceivedLuminance(Color color)
        {
            color = color.linear;
            return Mathf.LinearToGammaSpace(0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b);
        }

        public static int GetColorBlindSafePalette(Color[] palette, float minimumLuminance, float maximumLuminance)
        {
            if (palette == null)
                throw new ArgumentNullException("palette");

            Color[] acceptableColors = Enumerable.Range(0, s_ColorBlindSafePalette.Length).Where(
                    i => s_ColorBlindSafePaletteLuminanceValues[i] >= minimumLuminance && s_ColorBlindSafePaletteLuminanceValues[i] <= maximumLuminance
                    ).Select(i => s_ColorBlindSafePalette[i]
                    ).ToArray();

            int numUniqueColors = Mathf.Min(palette.Length, acceptableColors.Length);

            if (numUniqueColors > 0)
            {
                for (int i = 0, count = palette.Length; i < count; ++i)
                    palette[i] = acceptableColors[i % numUniqueColors];
            }
            else
            {
                for (int i = 0, count = palette.Length; i < count; ++i)
                    palette[i] = default(Color);
            }

            return numUniqueColors;
        }
    }
}
