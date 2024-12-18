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
    /// <summary>A class containing methods to assist with accessibility for users with different vision capabilities.</summary>
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

        /// <summary>Gets a palette of colors that should be distinguishable for normal vision, deuteranopia, protanopia, and tritanopia.</summary>
        /// <param name="palette">An array of colors to populate with a palette.</param>
        /// <param name="minimumLuminance">Minimum allowable perceived luminance from 0 to 1. A value of 0.2 or greater is recommended for dark backgrounds.</param>
        /// <param name="maximumLuminance">Maximum allowable perceived luminance from 0 to 1. A value of 0.8 or less is recommended for light backgrounds.</param>
        /// <returns>The number of unambiguous colors in the palette.</returns>
        /// <remarks>{img ColorBlindSafePaletteTable.png}
        ///''The set of colors from which to draw, along with their perceived luminance values.''
        ///
        ///Allocate the size of your palette before passing it to this method to specify how many colors you need. The return value indicates how many unambiguous colors exist in the palette. If this value is less than the size of the palette, then the palette repeats colors in order.
        ///
        ///{img ColorBlindSafePalette.png}
        ///''A window to preview swatches that should be distinguishable for most vision conditions.''
        ///
        ///Add the following script to Assets/Editor as ColorSwatchExample.cs and navigate to the menu option Window -&gt; Color Swatch Example.</remarks>
        /// <example>
        /// <code><![CDATA[
        ///using UnityEditor;
        ///using UnityEngine;
        ///using UnityEngine.Accessibility;
        ///
        ///public class ColorSwatchExample : EditorWindow
        ///{
        ///    // size of swatch background textures to generate
        ///    private const int k_SwatchTextureSize = 16;
        ///    // the maximum number of swatches for this example
        ///    private const int k_MaxPaletteSize = 10;
        ///
        ///    [MenuItem("Window/Color Swatch Example")]
        ///    private static void CreateWindow()
        ///    {
        ///        var window = GetWindow<ColorSwatchExample>();
        ///        window.position = new Rect(0f, 0f, 400f, 80f);
        ///    }
        ///
        ///    // the background textures to use for the swatches
        ///    private Texture2D[] m_SwatchBackgrounds = new Texture2D[k_MaxPaletteSize];
        ///
        ///    // the desired number of swatches
        ///    [SerializeField]
        ///    private int m_PaletteSize = 8;
        ///    // the range of desired luminance values
        ///    [SerializeField]
        ///    private Vector2 m_DesiredLuminance = new Vector2(0.2f, 0.9f);
        ///    // the colors obtained
        ///    [SerializeField]
        ///    private Color[] m_Palette;
        ///    // the number of unique colors in the palette before they repeat
        ///    [SerializeField]
        ///    private int m_NumUniqueColors;
        ///
        ///    // create swatch background textures when window first opens
        ///    protected virtual void OnEnable()
        ///    {
        ///        titleContent = new GUIContent("Color Swatches");
        ///
        ///        // create background swatches with different patterns for repeated colors
        ///        m_SwatchBackgrounds[0] = CreateSwatchBackground(k_SwatchTextureSize, 0, 0);
        ///        m_SwatchBackgrounds[1] = CreateSwatchBackground(k_SwatchTextureSize, 1, 4);
        ///        m_SwatchBackgrounds[2] = CreateSwatchBackground(k_SwatchTextureSize, 1, 3);
        ///        m_SwatchBackgrounds[3] = CreateSwatchBackground(k_SwatchTextureSize, 6, 1);
        ///        m_SwatchBackgrounds[4] = CreateSwatchBackground(k_SwatchTextureSize, 4, 3);
        ///        m_SwatchBackgrounds[5] = CreateSwatchBackground(k_SwatchTextureSize, 6, 6);
        ///        m_SwatchBackgrounds[6] = CreateSwatchBackground(k_SwatchTextureSize, 4, 2);
        ///        m_SwatchBackgrounds[7] = CreateSwatchBackground(k_SwatchTextureSize, 6, 4);
        ///        m_SwatchBackgrounds[8] = CreateSwatchBackground(k_SwatchTextureSize, 2, 5);
        ///        m_SwatchBackgrounds[9] = CreateSwatchBackground(k_SwatchTextureSize, 1, 2);
        ///
        ///        UpdatePalette();
        ///    }
        ///
        ///    // clean up textures when window is closed
        ///    protected virtual void OnDisable()
        ///    {
        ///        for (int i = 0, count = m_SwatchBackgrounds.Length; i < count; ++i)
        ///            DestroyImmediate(m_SwatchBackgrounds[i]);
        ///    }
        ///
        ///    protected virtual void OnGUI()
        ///    {
        ///        // input desired number of colors and luminance values
        ///        EditorGUI.BeginChangeCheck();
        ///
        ///        m_PaletteSize = EditorGUILayout.IntSlider("Palette Size", m_PaletteSize, 0, k_MaxPaletteSize);
        ///
        ///        float min = m_DesiredLuminance.x;
        ///        float max = m_DesiredLuminance.y;
        ///        EditorGUILayout.MinMaxSlider("Luminance Range", ref min, ref max, 0f, 1f);
        ///        m_DesiredLuminance = new Vector2(min, max);
        ///
        ///        if (EditorGUI.EndChangeCheck())
        ///        {
        ///            UpdatePalette();
        ///        }
        ///
        ///        // display warning message if parameters are out of range
        ///        if (m_NumUniqueColors == 0)
        ///        {
        ///            string warningMessage = "Unable to generate any unique colors with the specified luminance requirements.";
        ///            EditorGUILayout.HelpBox(warningMessage, MessageType.Warning);
        ///        }
        ///        // otherwise display swatches in a row
        ///        else
        ///        {
        ///            using (new GUILayout.HorizontalScope())
        ///            {
        ///                GUILayout.FlexibleSpace();
        ///                Color oldColor = GUI.color;
        ///
        ///                int swatchBackgroundIndex = 0;
        ///                for (int i = 0; i < m_PaletteSize; ++i)
        ///                {
        ///                    // change swatch background pattern when reaching a repeated color
        ///                    if (i > 0 && i % m_NumUniqueColors == 0)
        ///                        ++swatchBackgroundIndex;
        ///
        ///                    Rect rect = GUILayoutUtility.GetRect(k_SwatchTextureSize * 2, k_SwatchTextureSize * 2);
        ///                    rect.width = k_SwatchTextureSize * 2;
        ///
        ///                    GUI.color = m_Palette[i];
        ///                    GUI.DrawTexture(rect, m_SwatchBackgrounds[swatchBackgroundIndex], ScaleMode.ScaleToFit, true);
        ///                }
        ///
        ///                GUI.color = oldColor;
        ///                GUILayout.FlexibleSpace();
        ///            }
        ///        }
        ///    }
        ///
        ///    // create a white texture with some pixels discarded to make a pattern
        ///    private Texture2D CreateSwatchBackground(int size, int discardPixelCount, int discardPixelStep)
        ///    {
        ///        var swatchBackground = new Texture2D(size, size);
        ///        swatchBackground.hideFlags = HideFlags.HideAndDontSave;
        ///        swatchBackground.filterMode = FilterMode.Point;
        ///
        ///        var pixels = swatchBackground.GetPixels32();
        ///        int counter = 0;
        ///        bool discard = false;
        ///        for (int i = 0, count = pixels.Length; i < count; ++i)
        ///        {
        ///            pixels[i] = new Color32(255, 255, 255, (byte)(discard ? 0 : 255));
        ///            ++counter;
        ///            if (discard && counter == discardPixelCount)
        ///            {
        ///                discard = false;
        ///                counter = 0;
        ///            }
        ///            else if (!discard && counter == discardPixelStep)
        ///            {
        ///                discard = true;
        ///                counter = 0;
        ///            }
        ///        }
        ///        swatchBackground.SetPixels32(pixels);
        ///
        ///        swatchBackground.Apply();
        ///        return swatchBackground;
        ///    }
        ///
        ///    // request new palette
        ///    private void UpdatePalette()
        ///    {
        ///        m_Palette = new Color[m_PaletteSize];
        ///        m_NumUniqueColors =
        ///            VisionUtility.GetColorBlindSafePalette(m_Palette, m_DesiredLuminance.x, m_DesiredLuminance.y);
        ///    }
        ///}
        ///]]></code>
        /// </example>
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
