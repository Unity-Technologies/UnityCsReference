// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Acessibility.VisionUtility.Tests")]
namespace UnityEngine.Accessibility
{
    [UsedByNativeCode]
    public static class VisionUtility
    {
        // http://www.somersault1824.com/wp-content/uploads/2015/02/color-blindness-palette.png
        // colors are ordered across rows to maximize contrast with small palettes
        static readonly Color[] s_ColorBlindSafePalette = new Color[]
        {
            new Color32(0, 0, 0, 255), // Not used by profiler because too dark
            new Color32(73, 0, 146, 255), // Not used by profiler because too dark
//          new Color32(146, 0, 0, 255),                    // doesn't look good in editor

            new Color32(7, 71, 81, 255), // Not used by profiler because too dark
//          new Color32(0, 109, 219, 255),                  // tritanopia ambiguous 1
//          new Color32(146, 73, 0, 255),                   // doesn't look good in editor

            new Color32(0, 146, 146, 255),   // "Rendering"   // tritanopia ambiguous 1
            new Color32(182, 109, 255, 255), // "Scripts", "Managed Jobs", "Burst Jobs"
//          new Color32(219, 209, 0, 255),                  // tritanopia ambiguous 2; also doesn't look good in editor

            new Color32(255, 109, 182, 255), // "Physics"     // tritanopia ambiguous 2
            new Color32(109, 182, 255, 255), // "Animnation"
            new Color32(36, 255, 36, 255),   // "GC"

            new Color32(255, 182, 219, 255), // "VSync"
            new Color32(182, 219, 255, 255), // "GI"
            new Color32(255, 255, 109, 255), // UI: "UI Layout", "UI Render"
            // Bugfix additions to address https://fogbugz.unity3d.com/f/cases/1101387/
            new Color32(30, 92, 92, 255),   // Profiler - CPU Usage Chart: "Other" category
            new Color32(74, 154, 87, 255),   // Profiler - GI Chart: "Blocked Command Write" - CPU Timeline: "Audio"
            new Color32(113, 66, 183, 255),   // CPU Timeline: "Audio Job"
            new Color32(162, 66, 183, 255),   // CPU Timeline: "Audio Update Job"
            new Color32(178, 92, 25, 255),   // CPU Timeline: "Memory Allocation"
            new Color32(100, 100, 100, 255),   // CPU Timeline: "Internal"
            new Color32(80, 203, 181, 255),   // CPU Timeline: "Build Interface"
            new Color32(82, 205, 242, 255),   // CPU Timeline: "Input"
        };

        static readonly float[] s_ColorBlindSafePaletteLuminanceValues =
            s_ColorBlindSafePalette.Select(c => ComputePerceivedLuminance(c)).ToArray();

        // https://en.wikipedia.org/wiki/Relative_luminance
        internal static float ComputePerceivedLuminance(Color color)
        {
            color = color.linear;
            return Mathf.LinearToGammaSpace(0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b);
        }

        internal static void GetLuminanceValuesForPalette(Color[] palette, ref float[] outLuminanceValues)
        {
            Debug.Assert(palette != null && outLuminanceValues != null, "Passed in arrays can't be null.");
            Debug.Assert(palette.Length == outLuminanceValues.Length, "Passed in arrays need to be of the same length.");
            for (int i = 0; i < palette.Length; i++)
            {
                outLuminanceValues[i] = ComputePerceivedLuminance(palette[i]);
            }
        }

        public static int GetColorBlindSafePalette(Color[] palette, float minimumLuminance, float maximumLuminance)
        {
            if (palette == null)
                throw new ArgumentNullException("palette");
            unsafe
            {
                fixed(Color * p = palette)
                {
                    return GetColorBlindSafePaletteInternal(p, palette.Length, minimumLuminance, maximumLuminance, useColor32: false);
                }
            }
        }

        internal static int GetColorBlindSafePalette(Color32[] palette, float minimumLuminance, float maximumLuminance)
        {
            if (palette == null)
                throw new ArgumentNullException("palette");
            unsafe
            {
                fixed(Color32 * p = palette)
                {
                    return GetColorBlindSafePaletteInternal(p, palette.Length, minimumLuminance, maximumLuminance, useColor32: true);
                }
            }
        }

        // MethodImplOptions.AggressiveInlining = 256
        [MethodImpl(256)]
        unsafe static int GetColorBlindSafePaletteInternal(void* palette, int paletteLength, float minimumLuminance, float maximumLuminance, bool useColor32)
        {
            if (palette == null)
                throw new ArgumentNullException("palette");

            var acceptableColors = Enumerable.Range(0, s_ColorBlindSafePalette.Length).Where(
                i => s_ColorBlindSafePaletteLuminanceValues[i] >= minimumLuminance && s_ColorBlindSafePaletteLuminanceValues[i] <= maximumLuminance
                ).Select(i => s_ColorBlindSafePalette[i]
                ).ToArray();

            int numUniqueColors = Mathf.Min(paletteLength, acceptableColors.Length);

            if (numUniqueColors > 0)
            {
                for (int i = 0, count = paletteLength; i < count; ++i)
                {
                    if (useColor32)
                        ((Color32*)palette)[i] = (Color32)acceptableColors[i % numUniqueColors];
                    else
                        ((Color*)palette)[i] = (Color)acceptableColors[i % numUniqueColors];
                }
            }
            else
            {
                for (int i = 0, count = paletteLength; i < count; ++i)
                {
                    if (useColor32)
                        ((Color32*)palette)[i] = default(Color32);
                    else
                        ((Color*)palette)[i] = default(Color);
                }
            }

            return numUniqueColors;
        }
    }
}
