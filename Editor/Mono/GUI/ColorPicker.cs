// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditorInternal;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal class ColorPicker : EditorWindow
    {
        static ColorPicker s_SharedColorPicker;
        public static string presetsEditorPrefID { get {return "Color"; }}

        private const float kMaxfp16 = 65536f; // Clamp to a value that fits into fp16.
        private readonly static ColorPickerHDRConfig m_DefaultHDRConfig = new ColorPickerHDRConfig(0f, kMaxfp16, 1 / kMaxfp16, 3f);
        public static ColorPickerHDRConfig defaultHDRConfig { get { return m_DefaultHDRConfig; } }

        [SerializeField]
        bool m_HDR;

        [SerializeField]
        ColorPickerHDRConfig m_HDRConfig;

        internal enum TonemappingType
        {
            Linear = 0,
            Photographic = 1
        }

        [Serializable]
        class HDRValues
        {
            [NonSerialized]
            public TonemappingType m_TonemappingType = TonemappingType.Photographic;

            [SerializeField]
            public float m_HDRScaleFactor;

            [SerializeField]
            public float m_ExposureAdjustment = 1.5f;
        }

        [SerializeField]
        HDRValues m_HDRValues = new HDRValues();

        [SerializeField]
        Color m_Color = Color.black;

        [SerializeField]
        Color m_OriginalColor;

        [SerializeField]
        float m_R, m_G, m_B;
        [SerializeField]
        float m_H, m_S, m_V;
        [SerializeField]
        float m_A = 1;

        //1D slider stuff
        [SerializeField]
        float m_ColorSliderSize = 4; //default
        [SerializeField]
        Texture2D m_ColorSlider;
        [SerializeField]
#pragma warning disable 169
        float m_SliderValue;

        [SerializeField]
        Color[] m_Colors;
        const int kHueRes = 64;
        const int kColorBoxSize = 32;
        [SerializeField]
        Texture2D m_ColorBox;
        static int s_Slider2Dhash = "Slider2D".GetHashCode();
        [SerializeField]
        bool m_ShowPresets = true, m_UseTonemappingPreview = false;

        [SerializeField]
        bool m_IsOSColorPicker = false;
        [SerializeField]
        bool m_ShowAlpha = true;

        //draws RGBLayout
        Texture2D m_RTexture; float m_RTextureG = -1, m_RTextureB = -1;
        Texture2D m_GTexture; float m_GTextureR = -1, m_GTextureB = -1;
        Texture2D m_BTexture; float m_BTextureR = -1, m_BTextureG = -1;

        //draws HSVLayout
        [SerializeField]
        Texture2D m_HueTexture; float m_HueTextureS = -1, m_HueTextureV = -1;
        [SerializeField]
        Texture2D m_SatTexture; float m_SatTextureH = -1, m_SatTextureV = -1;
        [SerializeField]
        Texture2D m_ValTexture; float m_ValTextureH = -1, m_ValTextureS = -1;

        [SerializeField]
        int m_TextureColorSliderMode = -1;
        [SerializeField]
        Vector2 m_LastConstantValues = new Vector2(-1, -1);

        [System.NonSerialized]
        int m_TextureColorBoxMode = -1;
        [SerializeField]
        float m_LastConstant = -1;

        [NonSerialized]
        bool m_ColorSpaceBoxDirty;

        [NonSerialized]
        bool m_ColorSliderDirty;

        [NonSerialized]
        bool m_RGBHSVSlidersDirty;

        [SerializeField]
#pragma warning disable 169
        ContainerWindow m_TrackingWindow;

        enum ColorBoxMode { SV_H, HV_S, HS_V, BG_R, BR_G, RG_B, EyeDropper }
        string[] m_ColorBoxXAxisLabels = { "Saturation", "Hue", "Hue", "Blue", "Blue", "Red", "" };
        string[] m_ColorBoxYAxisLabels = {"Brightness", "Brightness", "Saturation", "Green", "Red", "Green", ""};
        string[] m_ColorBoxZAxisLabels = { "Hue", "Saturation", "Brightness", "Red", "Green", "Blue", "" };

        [SerializeField]
        ColorBoxMode m_ColorBoxMode = ColorBoxMode.BG_R, m_OldColorBoxMode;

        enum SliderMode { RGB, HSV }
        [SerializeField]
        SliderMode m_SliderMode = SliderMode.HSV;

        [SerializeField]
        Texture2D m_AlphaTexture; float m_OldAlpha = -1;

        [SerializeField]
        GUIView m_DelegateView;

        [SerializeField]
        int m_ModalUndoGroup = -1;


        PresetLibraryEditor<ColorPresetLibrary> m_ColorLibraryEditor;
        PresetLibraryEditorState m_ColorLibraryEditorState;
        bool colorChanged {get; set; }


        // Layout
        const int kEyeDropperHeight = 95;
        const int kSlidersHeight = 82;
        const int kColorBoxHeight = 162;
        const int kPresetsHeight = 300;
        const float kFixedWindowWidth = 233;
        const float kHDRFieldWidth = 45f;
        const float kLDRFieldWidth = 30f;

        private float fieldWidth
        {
            get { return m_HDR ? kHDRFieldWidth : kLDRFieldWidth; }
        }

        public static bool visible
        {
            get { return s_SharedColorPicker != null; }
        }

        public static Color color
        {
            get
            {
                if (get.m_HDRValues.m_HDRScaleFactor > 1.0f)
                    return get.m_Color.RGBMultiplied(get.m_HDRValues.m_HDRScaleFactor);

                return get.m_Color;
            }
            set
            {
                get.SetColor(value);
            }
        }

        public static ColorPicker get
        {
            get
            {
                if (!s_SharedColorPicker)
                {
                    Object[] hmm = Resources.FindObjectsOfTypeAll(typeof(ColorPicker));
                    if (hmm != null && hmm.Length > 0)
                        s_SharedColorPicker = (ColorPicker)hmm[0];
                    if (!s_SharedColorPicker)
                    {
                        s_SharedColorPicker = ScriptableObject.CreateInstance<ColorPicker>();
                        s_SharedColorPicker.wantsMouseMove = true;
                    }
                }
                return s_SharedColorPicker;
            }
        }

        void RGBToHSV()
        {
            Color.RGBToHSV(new Color(m_R, m_G, m_B, 1), out m_H, out m_S, out m_V);
        }

        void HSVToRGB()
        {
            Color col = Color.HSVToRGB(m_H, m_S, m_V);
            m_R = col.r;
            m_G = col.g;
            m_B = col.b;
        }

        // ------- Soerens 2D slider --------

        static void swap(ref float f1, ref float f2) { float tmp = f1; f1 = f2; f2 = tmp; }

        Vector2 Slider2D(Rect rect, Vector2 value, Vector2 maxvalue, Vector2 minvalue, GUIStyle backStyle, GUIStyle thumbStyle)
        {
            if (backStyle == null)
                return value;
            if (thumbStyle == null)
                return value;

            int id = GUIUtility.GetControlID(s_Slider2Dhash, FocusType.Passive);

            // test max and min
            if (maxvalue.x < minvalue.x) // swap
                swap(ref maxvalue.x, ref minvalue.x);
            if (maxvalue.y < minvalue.y)
                swap(ref maxvalue.y, ref minvalue.y);

            // thumb
            float thumbHeight = thumbStyle.fixedHeight == 0 ? thumbStyle.padding.vertical : thumbStyle.fixedHeight;
            float thumbWidth = thumbStyle.fixedWidth == 0 ? thumbStyle.padding.horizontal : thumbStyle.fixedWidth;

            // value to px ratio vector
            Vector2 val2pxRatio =  new Vector2(
                    (rect.width - (backStyle.padding.right + backStyle.padding.left) - (thumbWidth * 2)) / (maxvalue.x - minvalue.x),
                    (rect.height - (backStyle.padding.top + backStyle.padding.bottom) - (thumbHeight * 2)) / (maxvalue.y - minvalue.y));

            Rect thumbRect = new Rect(
                    rect.x + (value.x * val2pxRatio.x)  + (thumbWidth / 2) + backStyle.padding.left - (minvalue.x * val2pxRatio.x),
                    rect.y + (value.y * val2pxRatio.y) + (thumbHeight / 2) + backStyle.padding.top - (minvalue.y * val2pxRatio.y),
                    thumbWidth, thumbHeight);

            Event e = Event.current;

            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                {
                    if (rect.Contains(e.mousePosition)) // inside this control
                    {
                        GUIUtility.hotControl = id;
                        GUIUtility.keyboardControl = 0;
                        value.x = (((e.mousePosition.x - rect.x - thumbWidth) - backStyle.padding.left) / val2pxRatio.x) + (minvalue.x);
                        value.y = (((e.mousePosition.y - rect.y - thumbHeight) - backStyle.padding.top) / val2pxRatio.y) + (minvalue.y);
                        GUI.changed = true;

                        Event.current.Use();
                    }
                    break;
                }
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        e.Use();
                    }
                    break;
                case EventType.MouseDrag:
                {
                    if (GUIUtility.hotControl != id)
                        break;

                    // move thumb to mouse position
                    value.x = (((e.mousePosition.x - rect.x - thumbWidth) - backStyle.padding.left) / val2pxRatio.x) + (minvalue.x);
                    value.y = (((e.mousePosition.y - rect.y - thumbHeight) - backStyle.padding.top) / val2pxRatio.y) + (minvalue.y);

                    // clamp
                    value.x = Mathf.Clamp(value.x, minvalue.x, maxvalue.x);
                    value.y = Mathf.Clamp(value.y, minvalue.y, maxvalue.y);
                    GUI.changed = true;
                    Event.current.Use();
                }
                break;

                case EventType.Repaint:
                {
                    // background
                    backStyle.Draw(rect, GUIContent.none, id);

                    // thumb (set to black for light colors to prevent blending with background)
                    Color oldColor = GUI.color;
                    bool setBlack = color.grayscale > 0.5f;
                    if (setBlack)
                        GUI.color = Color.black;
                    thumbStyle.Draw(thumbRect, GUIContent.none, id);
                    if (setBlack)
                        GUI.color = oldColor;
                }
                break;
            }
            return value;
        }

        void OnFloatFieldChanged(float value)
        {
            if (m_HDR && value > m_HDRValues.m_HDRScaleFactor)
                SetHDRScaleFactor(value);
        }

        //draws RGBLayout
        void RGBSliders()
        {
            EditorGUI.BeginChangeCheck();
            // Update the RGB slider textures
            float exposureValue = GetTonemappingExposureAdjusment();
            float colorScale = GetColorScale();
            m_RTexture = Update1DSlider(m_RTexture, kColorBoxSize, m_G, m_B, ref m_RTextureG, ref m_RTextureB, 0, false, colorScale, exposureValue, m_RGBHSVSlidersDirty, m_HDRValues.m_TonemappingType);
            m_GTexture = Update1DSlider(m_GTexture, kColorBoxSize, m_R, m_B, ref m_GTextureR, ref m_GTextureB, 1, false, colorScale, exposureValue, m_RGBHSVSlidersDirty, m_HDRValues.m_TonemappingType);
            m_BTexture = Update1DSlider(m_BTexture, kColorBoxSize, m_R, m_G, ref m_BTextureR, ref m_BTextureG, 2, false, colorScale, exposureValue, m_RGBHSVSlidersDirty, m_HDRValues.m_TonemappingType);

            m_RGBHSVSlidersDirty = false;

            float displayScale = m_HDR ? colorScale : 255f;
            string formatString = m_HDR ? EditorGUI.kFloatFieldFormatString : EditorGUI.kIntFieldFormatString;
            m_R = TexturedSlider(m_RTexture, "R", m_R, 0f, 1f, displayScale, formatString, OnFloatFieldChanged);
            m_G = TexturedSlider(m_GTexture, "G", m_G, 0f, 1f, displayScale, formatString, OnFloatFieldChanged);
            m_B = TexturedSlider(m_BTexture, "B", m_B, 0f, 1f, displayScale, formatString, OnFloatFieldChanged);

            if (EditorGUI.EndChangeCheck())
            {
                RGBToHSV();
            }
        }

        static Texture2D Update1DSlider(Texture2D tex, int xSize, float const1, float const2, ref float oldConst1,
            ref float oldConst2, int idx, bool hsvSpace, float scale, float exposureValue, bool forceUpdate, TonemappingType tonemappingType)
        {
            if (!tex || const1 != oldConst1 || const2 != oldConst2 || forceUpdate)
            {
                if (!tex)
                    tex = MakeTexture(xSize, 2);

                Color[] colors = new Color[xSize * 2];
                Color start = Color.black, step = Color.black;
                switch (idx)
                {
                    case 0:
                        start = new Color(0, const1, const2, 1);
                        step = new Color(1, 0, 0, 0);
                        break;
                    case 1:
                        start = new Color(const1, 0, const2, 1);
                        step = new Color(0, 1, 0, 0);
                        break;
                    case 2:
                        start = new Color(const1, const2, 0, 1);
                        step = new Color(0, 0, 1, 0);
                        break;
                    case 3:
                        start = new Color(0, 0, 0, 1);
                        step = new Color(1, 1, 1, 0);
                        break;
                }
                FillArea(xSize, 2, colors, start, step, new Color(0, 0, 0, 0));
                if (hsvSpace)
                    HSVToRGBArray(colors, scale);
                else
                    ScaleColors(colors, scale);

                DoTonemapping(colors, exposureValue, tonemappingType);

                oldConst1 = const1;
                oldConst2 = const2;
                tex.SetPixels(colors);
                tex.Apply();
            }
            return tex;
        }

        float TexturedSlider(Texture2D background, string text, float val, float min, float max, float displayScale, string formatString, System.Action<float> onFloatFieldChanged)
        {
            Rect r = GUILayoutUtility.GetRect(16, 16, GUI.skin.label);

            GUI.Label(new Rect(r.x, r.y, 20, 16), text);
            const float kTextWidth = 14f;
            const float kSpacing = 6f;

            r.x += kTextWidth;
            r.width -= kTextWidth + kSpacing + fieldWidth;
            if (Event.current.type == EventType.Repaint)
            {
                Rect r2 = new Rect(r.x + 1, r.y + 2, r.width - 2, r.height - 4);
                Graphics.DrawTexture(r2, background, new Rect(.5f / background.width, .5f / background.height, 1 - 1f / background.width, 1 - 1f / background.height), 0, 0, 0, 0, Color.grey);
            }
            int id = EditorGUIUtility.GetControlID(869045, FocusType.Keyboard, position);

            EditorGUI.BeginChangeCheck();
            val = GUI.HorizontalSlider(new Rect(r.x, r.y + 1, r.width, r.height - 2), val, min, max, styles.pickerBox, styles.thumbHoriz);
            if (EditorGUI.EndChangeCheck())
            {
                if (EditorGUI.s_RecycledEditor.IsEditingControl(id))
                    EditorGUI.s_RecycledEditor.EndEditing();

                val = (float)Math.Round(val, 3); // We do not need more than 3 decimals for color channel values: 1/255 = 0.004

                GUIUtility.keyboardControl = 0;
            }

            Rect r3 = new Rect(r.xMax + kSpacing, r.y, fieldWidth, 16);
            EditorGUI.BeginChangeCheck();
            val = EditorGUI.DoFloatField(EditorGUI.s_RecycledEditor, r3, new Rect(0, 0, 0, 0), id, val * displayScale, formatString, EditorStyles.numberField, false);
            if (EditorGUI.EndChangeCheck() && onFloatFieldChanged != null)
                onFloatFieldChanged(val);

            val = Mathf.Clamp(val / displayScale, min, max);

            GUILayout.Space(3f);

            return val;
        }

        //draws HSVLayout
        void HSVSliders()
        {
            EditorGUI.BeginChangeCheck();

            // Update the HSV slider textures
            float exposureValue = GetTonemappingExposureAdjusment();
            float colorScale = GetColorScale();
            m_HueTexture = Update1DSlider(m_HueTexture, kHueRes, 1, 1, ref m_HueTextureS, ref m_HueTextureV, 0, true, 1, -1, m_RGBHSVSlidersDirty, m_HDRValues.m_TonemappingType);
            m_SatTexture = Update1DSlider(m_SatTexture, kColorBoxSize, m_H, Mathf.Max(m_V, .2f), ref m_SatTextureH, ref m_SatTextureV, 1, true, colorScale, exposureValue, m_RGBHSVSlidersDirty, m_HDRValues.m_TonemappingType);
            m_ValTexture = Update1DSlider(m_ValTexture, kColorBoxSize, m_H, m_S, ref m_ValTextureH, ref m_ValTextureS, 2, true, colorScale, exposureValue, m_RGBHSVSlidersDirty, m_HDRValues.m_TonemappingType);
            m_RGBHSVSlidersDirty = false;

            float displayScale = m_HDR ? colorScale : 255f;
            string formatString = m_HDR ? EditorGUI.kFloatFieldFormatString : EditorGUI.kIntFieldFormatString;

            m_H = TexturedSlider(m_HueTexture, "H", m_H, 0f, 1f, 359f, EditorGUI.kIntFieldFormatString, null);
            m_S = TexturedSlider(m_SatTexture, "S", m_S, 0f, 1f, m_HDR ? 1f : 255f, formatString, null);
            m_V = TexturedSlider(m_ValTexture, "V", m_V, 0f, 1f, displayScale, formatString, null);

            if (EditorGUI.EndChangeCheck())
            {
                HSVToRGB();
            }
        }

        static void FillArea(int xSize, int ySize, Color[] retval, Color topLeftColor, Color rightGradient, Color downGradient)
        {
            // Calc the deltas for stepping.
            Color rightDelta = new Color(0, 0, 0, 0), downDelta  = new Color(0, 0, 0, 0);
            if (xSize > 1)
                rightDelta = rightGradient / (xSize - 1);
            if (ySize > 1)
                downDelta = downGradient / (ySize - 1);

            // Assign all colors into the array
            Color p = topLeftColor;
            int current = 0;
            for (int y = 0; y < ySize; y++)
            {
                Color p2 = p;
                for (int x = 0; x < xSize; x++)
                {
                    retval[current++] = p2;
                    p2 += rightDelta;
                }
                p += downDelta;
            }
        }

        static void ScaleColors(Color[] colors, float scale)
        {
            int s = colors.Length;
            for (int i = 0; i < s; i++)
                colors[i] = colors[i].RGBMultiplied(scale);
        }

        static void HSVToRGBArray(Color[] colors, float scale)
        {
            int s = colors.Length;
            for (int i = 0; i < s; i++)
            {
                Color c = colors[i];
                Color c2 = Color.HSVToRGB(c.r, c.g, c.b);
                c2 = c2.RGBMultiplied(scale);
                c2.a = c.a;
                colors[i] = c2;
            }
        }

        static void LinearToGammaArray(Color[] colors)
        {
            int s = colors.Length;
            for (int i = 0; i < s; i++)
            {
                Color c = colors[i];
                Color c2 = c.gamma;
                c2.a = c.a;
                colors[i] = c2;
            }
        }

        // returns -1 if tonemapping is disabled
        float GetTonemappingExposureAdjusment()
        {
            return m_HDR && m_UseTonemappingPreview ? m_HDRValues.m_ExposureAdjustment : -1f;
        }

        float GetColorScale()
        {
            if (m_HDR)
            {
                return Mathf.Max(1f, m_HDRValues.m_HDRScaleFactor);
            }
            return 1f;
        }

        void DrawColorSlider(Rect colorSliderRect, Vector2 constantValues)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            // If we've switched mode, regenerate box
            if ((int)m_ColorBoxMode != m_TextureColorSliderMode)
            {
                int newXSize = 0, newYSize = 0;
                newXSize = (int)m_ColorSliderSize;

                // it might want a new size
                if (m_ColorBoxMode  == ColorBoxMode.SV_H)
                    newYSize = (int)kHueRes;
                else
                    newYSize = (int)m_ColorSliderSize;

                if (m_ColorSlider == null)
                    m_ColorSlider = MakeTexture(newXSize, newYSize);

                if (m_ColorSlider.width != newXSize || m_ColorSlider.height != newYSize)
                    m_ColorSlider.Resize(newXSize, newYSize);
            }

            if ((int)m_ColorBoxMode != m_TextureColorSliderMode || constantValues != m_LastConstantValues || m_ColorSliderDirty)
            {
                float exposureValue = GetTonemappingExposureAdjusment();
                float colorScale = GetColorScale();

                Color[] sliderColors = m_ColorSlider.GetPixels(0);

                int xSize = m_ColorSlider.width, ySize = m_ColorSlider.height;
                switch (m_ColorBoxMode)
                {
                    case ColorBoxMode.SV_H:
                        FillArea(xSize, ySize, sliderColors, new Color(0, 1, 1, 1), new Color(0, 0, 0, 0), new Color(1, 0, 0, 0));
                        HSVToRGBArray(sliderColors, 1);
                        break;
                    case ColorBoxMode.HV_S:
                        FillArea(xSize, ySize, sliderColors, new Color(m_H, 0, Mathf.Max(m_V, .30f), 1), new Color(0, 0, 0, 0), new Color(0, 1, 0, 0));
                        HSVToRGBArray(sliderColors, colorScale);
                        break;
                    case ColorBoxMode.HS_V:
                        FillArea(xSize, ySize, sliderColors, new Color(m_H, m_S, 0, 1), new Color(0, 0, 0, 0), new Color(0, 0, 1, 0));
                        HSVToRGBArray(sliderColors, colorScale);
                        break;
                    case ColorBoxMode.BG_R:
                        FillArea(xSize, ySize, sliderColors, new Color(0, m_G * colorScale, m_B * colorScale, 1), new Color(0, 0, 0, 0), new Color(colorScale, 0, 0, 0));
                        break;
                    case ColorBoxMode.BR_G:
                        FillArea(xSize, ySize, sliderColors, new Color(m_R * colorScale, 0, m_B * colorScale, 1), new Color(0, 0, 0, 0), new Color(0, colorScale, 0, 0));
                        break;
                    case ColorBoxMode.RG_B:
                        FillArea(xSize, ySize, sliderColors, new Color(m_R * colorScale, m_G * colorScale, 0, 1), new Color(0, 0, 0, 0), new Color(0, 0, colorScale, 0));
                        break;
                }

                if (QualitySettings.activeColorSpace == ColorSpace.Linear)
                    LinearToGammaArray(sliderColors);

                // Tonemapping (we do not tonemap the Hue vertical slider)
                if (m_ColorBoxMode != ColorBoxMode.SV_H)
                    DoTonemapping(sliderColors, exposureValue, m_HDRValues.m_TonemappingType);

                m_ColorSlider.SetPixels(sliderColors, 0);
                m_ColorSlider.Apply(true);
            }
            Graphics.DrawTexture(colorSliderRect, m_ColorSlider, new Rect(.5f / m_ColorSlider.width, .5f / m_ColorSlider.height, 1 - 1f / m_ColorSlider.width, 1 - 1f / m_ColorSlider.height), 0, 0, 0, 0, Color.grey);
        }

        public static Texture2D MakeTexture(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.hideFlags = HideFlags.HideAndDontSave;
            return tex;
        }

        void DrawColorSpaceBox(Rect colorBoxRect, float constantValue)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            // If we've switched mode, regenerate box
            if ((int)m_ColorBoxMode != m_TextureColorBoxMode)
            {
                int newXSize = 0, newYSize = 0;
                newYSize = (int)kColorBoxSize;

                // it might want a new size
                if (m_ColorBoxMode  == ColorBoxMode.HV_S || m_ColorBoxMode  == ColorBoxMode.HS_V)
                    newXSize = (int)kHueRes;
                else
                    newXSize = (int)kColorBoxSize;

                if (m_ColorBox == null)
                    m_ColorBox = MakeTexture(newXSize, newYSize);

                if (m_ColorBox.width != newXSize || m_ColorBox.height != newYSize)
                    m_ColorBox.Resize(newXSize, newYSize);
            }

            if ((int)m_ColorBoxMode != m_TextureColorBoxMode || m_LastConstant != constantValue || m_ColorSpaceBoxDirty)
            {
                float exposureValue = GetTonemappingExposureAdjusment();
                float colorScale = GetColorScale();
                System.Diagnostics.Debug.Assert(colorScale >= 1f);

                m_Colors = m_ColorBox.GetPixels(0);
                int xSize = m_ColorBox.width;
                int ySize = m_ColorBox.height;
                switch (m_ColorBoxMode)
                {
                    case ColorBoxMode.BG_R:
                        FillArea(xSize, ySize, m_Colors, new Color(m_R * colorScale, 0, 0, 1), new Color(0, 0, colorScale, 0), new Color(0, colorScale, 0, 0));
                        break;
                    case ColorBoxMode.BR_G:
                        FillArea(xSize, ySize, m_Colors, new Color(0, m_G * colorScale, 0, 1), new Color(0, 0, colorScale, 0), new Color(colorScale, 0, 0, 0));
                        break;
                    case ColorBoxMode.RG_B:
                        FillArea(xSize, ySize, m_Colors, new Color(0, 0, m_B * colorScale, 1), new Color(colorScale, 0, 0, 0), new Color(0, colorScale, 0, 0));
                        break;
                    case ColorBoxMode.SV_H:
                        FillArea(xSize, ySize, m_Colors, new Color(m_H, 0, 0, 1), new Color(0, 1, 0, 0), new Color(0, 0, 1, 0));
                        HSVToRGBArray(m_Colors, colorScale);
                        break;
                    case ColorBoxMode.HV_S:
                        FillArea(xSize, ySize, m_Colors, new Color(0, m_S, 0, 1), new Color(1, 0, 0, 0), new Color(0, 0, 1, 0));
                        HSVToRGBArray(m_Colors, colorScale);
                        break;
                    case ColorBoxMode.HS_V:
                        FillArea(xSize, ySize, m_Colors, new Color(0, 0, m_V * colorScale, 1), new Color(1, 0, 0, 0), new Color(0, 1, 0, 0));
                        HSVToRGBArray(m_Colors, 1.0f);
                        break;
                }
                if (QualitySettings.activeColorSpace == ColorSpace.Linear)
                    LinearToGammaArray(m_Colors);

                DoTonemapping(m_Colors, exposureValue, m_HDRValues.m_TonemappingType);

                m_ColorBox.SetPixels(m_Colors, 0);
                m_ColorBox.Apply(true);
                m_LastConstant = constantValue;
                m_TextureColorBoxMode = (int)m_ColorBoxMode;
            }
            Graphics.DrawTexture(colorBoxRect, m_ColorBox, new Rect(.5f / m_ColorBox.width, .5f / m_ColorBox.height, 1 - 1f / m_ColorBox.width, 1 - 1f / m_ColorBox.height), 0, 0, 0, 0, Color.grey);

            DrawLabelOutsideRect(colorBoxRect, GetXAxisLabel(m_ColorBoxMode), LabelLocation.Bottom);
            DrawLabelOutsideRect(colorBoxRect, GetYAxisLabel(m_ColorBoxMode), LabelLocation.Left);
        }

        string GetXAxisLabel(ColorBoxMode colorBoxMode)
        {
            return m_ColorBoxXAxisLabels[(int)colorBoxMode];
        }

        string GetYAxisLabel(ColorBoxMode colorBoxMode)
        {
            return m_ColorBoxYAxisLabels[(int)colorBoxMode];
        }

        string GetZAxisLabel(ColorBoxMode colorBoxMode)
        {
            return m_ColorBoxZAxisLabels[(int)colorBoxMode];
        }

        enum LabelLocation
        {
            Top,
            Bottom,
            Left,
            Right
        }
        static void DrawLabelOutsideRect(Rect position, string label, LabelLocation labelLocation)
        {
            Matrix4x4 normalMatrix = GUI.matrix;
            Rect labelRect = new Rect(position.x, position.y - 18, position.width, 16);
            switch (labelLocation)
            {
                case LabelLocation.Left:
                    GUIUtility.RotateAroundPivot(-90, position.center);
                    break;
                case LabelLocation.Right:
                    GUIUtility.RotateAroundPivot(90, position.center);
                    break;
                case LabelLocation.Top:
                    // nop
                    break;
                case LabelLocation.Bottom:
                    labelRect = new Rect(position.x, position.yMax, position.width, 16);
                    break;
            }
            using (new EditorGUI.DisabledScope(true)) // for half alpha
            {
                GUI.Label(labelRect, label, styles.label);
            }

            GUI.matrix = normalMatrix;
        }

        class Styles
        {
            public GUIStyle pickerBox = "ColorPickerBox";
            public GUIStyle thumb2D = "ColorPicker2DThumb";
            public GUIStyle thumbHoriz = "ColorPickerHorizThumb";
            public GUIStyle thumbVert = "ColorPickerVertThumb";
            public GUIStyle headerLine = "IN Title";
            public GUIStyle colorPickerBox = "ColorPickerBox";
            public GUIStyle background = new GUIStyle("ColorPickerBackground");
            public GUIStyle label = new GUIStyle(EditorStyles.miniLabel);
            public GUIStyle axisLabelNumberField = new GUIStyle(EditorStyles.miniTextField);
            public GUIStyle foldout = new GUIStyle(EditorStyles.foldout);
            public GUIStyle toggle = new GUIStyle(EditorStyles.toggle);
            public GUIContent eyeDropper = EditorGUIUtility.IconContent("EyeDropper.Large", "Pick a color from the screen.");
            public GUIContent colorCycle = EditorGUIUtility.IconContent("ColorPicker.CycleColor");
            public GUIContent colorToggle = EditorGUIUtility.TextContent("Colors");
            public GUIContent tonemappingToggle = new GUIContent("Tonemapped Preview", "When enabled preview colors are tonemapped using Photographic Tonemapping");
            public GUIContent sliderToggle = EditorGUIUtility.TextContent("Sliders|The RGB or HSV color sliders.");
            public GUIContent presetsToggle = new GUIContent("Presets");
            public GUIContent sliderCycle = EditorGUIUtility.IconContent("ColorPicker.CycleSlider");


            public Styles()
            {
                axisLabelNumberField.alignment = TextAnchor.UpperRight;
                axisLabelNumberField.normal.background = null;
                label.alignment = TextAnchor.LowerCenter;
            }
        }
        static Styles styles;

        public string currentPresetLibrary
        {
            get
            {
                InitIfNeeded();
                return m_ColorLibraryEditor.currentLibraryWithoutExtension;
            }
            set
            {
                InitIfNeeded();
                m_ColorLibraryEditor.currentLibraryWithoutExtension = value;
            }
        }

        void InitIfNeeded()
        {
            if (styles == null)
                styles = new Styles();

            if (m_ColorLibraryEditorState == null)
            {
                m_ColorLibraryEditorState = new PresetLibraryEditorState(presetsEditorPrefID);
                m_ColorLibraryEditorState.TransferEditorPrefsState(true);
            }

            if (m_ColorLibraryEditor == null)
            {
                var saveLoadHelper = new ScriptableObjectSaveLoadHelper<ColorPresetLibrary>("colors", SaveType.Text);
                m_ColorLibraryEditor = new PresetLibraryEditor<ColorPresetLibrary>(saveLoadHelper, m_ColorLibraryEditorState, PresetClickedCallback);
                m_ColorLibraryEditor.previewAspect = 1f;
                m_ColorLibraryEditor.minMaxPreviewHeight = new Vector2(ColorPresetLibrary.kSwatchSize, ColorPresetLibrary.kSwatchSize);
                m_ColorLibraryEditor.settingsMenuRightMargin = 2f;
                m_ColorLibraryEditor.useOnePixelOverlappedGrid = true;
                m_ColorLibraryEditor.alwaysShowScrollAreaHorizontalLines = false;
                m_ColorLibraryEditor.marginsForGrid = new RectOffset(0, 0, 0, 0);
                m_ColorLibraryEditor.marginsForList = new RectOffset(0, 5, 2, 2);
                m_ColorLibraryEditor.InitializeGrid(kFixedWindowWidth - (styles.background.padding.left + styles.background.padding.right));
            }
        }

        void PresetClickedCallback(int clickCount, object presetObject)
        {
            Color color = (Color)presetObject;
            if (!m_HDR && color.maxColorComponent > 1f)
                color = color.RGBMultiplied(1f / color.maxColorComponent); // Normalize color if we are LDR Color Picker

            SetColor(color);
            colorChanged = true;
        }

        void DoColorSwatchAndEyedropper()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(styles.eyeDropper, GUIStyle.none, GUILayout.Width(40), GUILayout.ExpandWidth(false)))
            {
                EyeDropper.Start(m_Parent);
                m_ColorBoxMode = ColorBoxMode.EyeDropper;
                GUIUtility.ExitGUI();
            }
            Color c = new Color(m_R, m_G, m_B, m_A);
            if (m_HDR)
                c = color;
            Rect r = GUILayoutUtility.GetRect(20, 20, 20, 20, styles.colorPickerBox, GUILayout.ExpandWidth(true));
            EditorGUIUtility.DrawColorSwatch(r, c, m_ShowAlpha, m_HDR);

            // Draw eyedropper icon
            if (Event.current.type == EventType.Repaint)
                styles.pickerBox.Draw(r, GUIContent.none, false, false, false, false);
            GUILayout.EndHorizontal();
        }

        void DoColorSpaceGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(styles.colorCycle, GUIStyle.none, GUILayout.ExpandWidth(false)))
                m_OldColorBoxMode = m_ColorBoxMode = (ColorBoxMode)(((int)m_ColorBoxMode + 1) % 6);
            GUILayout.EndHorizontal();

            {
                // We add some horizontal padding to make room for brightness values and axis labels
                const float kHorizontalPadding = 20f;
                GUILayout.BeginHorizontal();
                GUILayout.Space(kHorizontalPadding);
                GUILayout.BeginVertical();

                GUILayout.Space(7f);

                bool temp = GUI.changed;
                Rect colorBoxRect, colorSliderBoxRect;
                GUILayout.BeginHorizontal(GUILayout.ExpandHeight(false));
                colorBoxRect = GUILayoutUtility.GetAspectRect(1, styles.pickerBox, GUILayout.MinWidth(64), GUILayout.MinHeight(64), GUILayout.MaxWidth(256), GUILayout.MaxHeight(256));
                EditorGUILayout.Space();
                colorSliderBoxRect = GUILayoutUtility.GetRect(8, 32, 64, 128, styles.pickerBox);
                colorSliderBoxRect.height = colorBoxRect.height;
                GUILayout.EndHorizontal();

                GUI.changed = false;
                switch (m_ColorBoxMode)
                {
                    case ColorBoxMode.SV_H:
                        Slider3D(colorBoxRect, colorSliderBoxRect, ref m_S, ref m_V, ref m_H, styles.pickerBox, styles.thumb2D, styles.thumbVert);
                        if (GUI.changed)
                            HSVToRGB();
                        break;
                    case ColorBoxMode.HV_S:
                        Slider3D(colorBoxRect, colorSliderBoxRect, ref m_H, ref m_V, ref m_S, styles.pickerBox, styles.thumb2D, styles.thumbVert);
                        if (GUI.changed)
                            HSVToRGB();
                        break;
                    case ColorBoxMode.HS_V:
                        Slider3D(colorBoxRect, colorSliderBoxRect, ref m_H, ref m_S, ref m_V, styles.pickerBox, styles.thumb2D, styles.thumbVert);
                        if (GUI.changed)
                            HSVToRGB();
                        break;
                    case ColorBoxMode.BG_R:
                        Slider3D(colorBoxRect, colorSliderBoxRect, ref m_B, ref m_G, ref m_R, styles.pickerBox, styles.thumb2D, styles.thumbVert);
                        if (GUI.changed)
                            RGBToHSV();
                        break;
                    case ColorBoxMode.BR_G:
                        Slider3D(colorBoxRect, colorSliderBoxRect, ref m_B, ref m_R, ref m_G, styles.pickerBox, styles.thumb2D, styles.thumbVert);
                        if (GUI.changed)
                            RGBToHSV();
                        break;
                    case ColorBoxMode.RG_B:
                        Slider3D(colorBoxRect, colorSliderBoxRect, ref m_R, ref m_G, ref m_B, styles.pickerBox, styles.thumb2D, styles.thumbVert);
                        if (GUI.changed)
                            RGBToHSV();
                        break;
                    case ColorBoxMode.EyeDropper:
                        EyeDropper.DrawPreview(Rect.MinMaxRect(colorBoxRect.x, colorBoxRect.y, colorSliderBoxRect.xMax, colorBoxRect.yMax));
                        break;
                }

                GUI.changed |= temp;

                GUILayout.Space(5f);

                GUILayout.EndVertical();
                GUILayout.Space(kHorizontalPadding);
                GUILayout.EndHorizontal();
            }
        }

        void SetHDRScaleFactor(float value)
        {
            if (!m_HDR)
                Debug.LogError("HDR scale is being set in LDR mode!");

            if (value < 1f)
                Debug.LogError("SetHDRScaleFactor is below 1, should be >= 1!");
            m_HDRValues.m_HDRScaleFactor = Mathf.Clamp(value, 0f, m_HDRConfig.maxBrightness);
            m_ColorSliderDirty = true;
            m_ColorSpaceBoxDirty = true;
            m_RGBHSVSlidersDirty = true;
        }

        void BrightnessField(float availableWidth)
        {
            if (m_HDR)
            {
                float oldLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUI.BeginChangeCheck();
                EditorGUI.indentLevel++;

                EditorGUIUtility.labelWidth = availableWidth - kHDRFieldWidth - EditorGUI.indent;
                Color newColor = EditorGUILayout.ColorBrightnessField(GUIContent.Temp("Current Brightness"), color, m_HDRConfig.minBrightness, m_HDRConfig.maxBrightness);

                EditorGUI.indentLevel--;
                if (EditorGUI.EndChangeCheck())
                {
                    // New setup
                    float newMaxColorComponent = newColor.maxColorComponent;
                    if (newMaxColorComponent > m_HDRValues.m_HDRScaleFactor)
                    {
                        SetHDRScaleFactor(newMaxColorComponent);
                    }
                    SetNormalizedColor(newColor.RGBMultiplied(1f / m_HDRValues.m_HDRScaleFactor));
                }

                EditorGUIUtility.labelWidth = oldLabelWidth;
            }
        }

        void SetMaxDisplayBrightness(float brightness)
        {
            brightness = Mathf.Clamp(brightness, 1f, m_HDRConfig.maxBrightness);
            if (brightness != m_HDRValues.m_HDRScaleFactor)
            {
                // To ensure resulting color stays the same we counter change normalized color value when max brightness value changes
                Color normalizedColor = color.RGBMultiplied(1f / brightness);

                // Only allow change to hdr scale factor if our normalized color is valid
                float maxColorComponent = normalizedColor.maxColorComponent;
                if (maxColorComponent <= 1f)
                {
                    SetNormalizedColor(normalizedColor);
                    SetHDRScaleFactor(brightness);
                    Repaint();
                }
            }
        }

        void DoColorSliders()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(styles.sliderCycle, GUIStyle.none, GUILayout.ExpandWidth(false)))
                    m_SliderMode = (SliderMode)(((int)m_SliderMode + 1) % 2);
            }
            GUILayout.EndHorizontal();

            {
                GUILayout.Space(7f);

                switch (m_SliderMode)
                {
                    case SliderMode.HSV:
                        HSVSliders();
                        break;
                    case SliderMode.RGB:
                        RGBSliders();
                        break;
                }

                // Update the HSV slider textures
                if (m_ShowAlpha)
                {
                    m_AlphaTexture = Update1DSlider(m_AlphaTexture, kColorBoxSize, 0, 0, ref m_OldAlpha, ref m_OldAlpha, 3, false, 1f, -1f, false, m_HDRValues.m_TonemappingType);

                    float displayScale = m_HDR ? 1f : 255f;
                    string formatString = m_HDR ? EditorGUI.kFloatFieldFormatString : EditorGUI.kIntFieldFormatString;
                    m_A = TexturedSlider(m_AlphaTexture, "A", m_A, 0f, 1f, displayScale, formatString, null);
                }
            }
        }

        void DoHexField(float availableWidth)
        {
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            float oldFieldWidth = EditorGUIUtility.fieldWidth;
            const float kHexFieldWidth = 85;
            EditorGUIUtility.labelWidth = availableWidth - kHexFieldWidth;
            EditorGUIUtility.fieldWidth = kHexFieldWidth;

            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            Color newColor = EditorGUILayout.HexColorTextField(GUIContent.Temp("Hex Color"), color, m_ShowAlpha);
            if (EditorGUI.EndChangeCheck())
            {
                SetNormalizedColor(newColor);
                if (m_HDR)
                    SetHDRScaleFactor(1f);
            }
            EditorGUI.indentLevel--;

            EditorGUIUtility.labelWidth = oldLabelWidth;
            EditorGUIUtility.fieldWidth = oldFieldWidth;
        }

        void DoPresetsGUI()
        {
            GUILayout.BeginHorizontal();
            m_ShowPresets = GUILayout.Toggle(m_ShowPresets, styles.presetsToggle, styles.foldout);

            GUILayout.Space(17f); // Make room for presets settings menu button
            GUILayout.EndHorizontal();

            if (m_ShowPresets)
            {
                GUILayout.Space(-18f); // pull up to reuse space
                Rect presetsRect = GUILayoutUtility.GetRect(0, Mathf.Clamp(m_ColorLibraryEditor.contentHeight, 20f, 250f));
                m_ColorLibraryEditor.OnGUI(presetsRect, color);
            }
        }

        void OnGUI()
        {
            InitIfNeeded();

            EventType type = Event.current.type;

            if (type == EventType.ExecuteCommand)
            {
                switch (Event.current.commandName)
                {
                    case "EyeDropperUpdate":
                        Repaint();
                        break;
                    case "EyeDropperClicked":
                        Color col = EyeDropper.GetLastPickedColor();
                        m_R = col.r;
                        m_G = col.g;
                        m_B = col.b;
                        RGBToHSV();
                        m_ColorBoxMode = m_OldColorBoxMode;
                        m_Color = new Color(m_R, m_G, m_B, m_A);
                        SendEvent(true);
                        break;
                    case "EyeDropperCancelled":
                        Repaint();
                        m_ColorBoxMode = m_OldColorBoxMode;
                        break;
                }
            }

            Rect contentRect = EditorGUILayout.BeginVertical(styles.background);

            // Setup layout values
            float innerContentWidth = EditorGUILayout.GetControlRect(false, 1, EditorStyles.numberField).width;
            EditorGUIUtility.labelWidth = innerContentWidth - fieldWidth;
            EditorGUIUtility.fieldWidth = fieldWidth;

            EditorGUI.BeginChangeCheck();
            GUILayout.Space(10);
            DoColorSwatchAndEyedropper();

            GUILayout.Space(10);
            if (m_HDR)
            {
                TonemappingControls();
                GUILayout.Space(10);
            }

            DoColorSpaceGUI();
            GUILayout.Space(10);
            if (m_HDR)
            {
                GUILayout.Space(5);
                BrightnessField(innerContentWidth);
                GUILayout.Space(10);
            }
            DoColorSliders();
            GUILayout.Space(5);

            DoHexField(innerContentWidth);

            GUILayout.Space(10);
            if (EditorGUI.EndChangeCheck())
                colorChanged = true;

            // We leave presets GUI out of the change check because it has a scrollview that will
            // set changed=true when used and we do not want to send color changed events when scrolling
            DoPresetsGUI();

            // Call last to ensure we only use the copy paste events if no
            // other controls wants to use these events
            HandleCopyPasteEvents();

            if (colorChanged)
            {
                colorChanged = false;
                m_Color = new Color(m_R, m_G, m_B, m_A);
                //only register undo once per mouse click.
                SendEvent(true);
            }

            EditorGUILayout.EndVertical();

            if (contentRect.height > 0 && Event.current.type == EventType.Repaint)
            {
                SetHeight(contentRect.height);
            }

            if (Event.current.type == EventType.KeyDown)
            {
                switch (Event.current.keyCode)
                {
                    case KeyCode.Escape:
                        Undo.RevertAllDownToGroup(m_ModalUndoGroup);
                        m_Color = m_OriginalColor;
                        SendEvent(false);
                        Close();
                        GUIUtility.ExitGUI();
                        break;
                    case KeyCode.Return:
                    case KeyCode.KeypadEnter:
                        Close();
                        break;
                }
            }

            // Remove keyfocus when clicked outside any control
            if ((Event.current.type == EventType.MouseDown && Event.current.button != 1) || Event.current.type == EventType.ContextClick)
            {
                GUIUtility.keyboardControl = 0;
                Repaint();
            }
        }

        void SetHeight(float newHeight)
        {
            if (newHeight == position.height)
                return;
            minSize = new Vector2(kFixedWindowWidth, newHeight);
            maxSize = new Vector2(kFixedWindowWidth, newHeight);
        }

        void HandleCopyPasteEvents()
        {
            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.ValidateCommand:
                    switch (evt.commandName)
                    {
                        case "Copy":
                        case "Paste":
                            evt.Use();
                            break;
                    }
                    break;

                case EventType.ExecuteCommand:
                    switch (evt.commandName)
                    {
                        case "Copy":
                            ColorClipboard.SetColor(color);
                            evt.Use();
                            break;

                        case "Paste":
                            Color colorFromClipboard;
                            if (ColorClipboard.TryGetColor(m_HDR, out colorFromClipboard))
                            {
                                // Do not change alpha if color field is not showing alpha
                                if (!m_ShowAlpha)
                                    colorFromClipboard.a = m_A;

                                SetColor(colorFromClipboard);
                                colorChanged = true;

                                GUI.changed = true;
                                evt.Use();
                            }
                            break;
                    }
                    break;
            }
        }

        float GetScrollWheelDeltaInRect(Rect rect)
        {
            Event evt = Event.current;
            if (evt.type == EventType.ScrollWheel)
            {
                if (rect.Contains(evt.mousePosition))
                    return evt.delta.y;
            }
            return 0f;
        }

        void Slider3D(Rect boxPos, Rect sliderPos, ref float x, ref float y, ref float z, GUIStyle box, GUIStyle thumb2D, GUIStyle thumbHoriz)
        {
            // Color box
            Rect r = boxPos;
            r.x += 1;
            r.y += 1;
            r.width -= 2;
            r.height -= 2;
            DrawColorSpaceBox(r, z);

            Vector2 xy = new Vector2(x, 1 - y);
            xy = Slider2D(boxPos, xy, new Vector2(0, 0), new Vector2(1, 1), box, thumb2D);
            x = xy.x;
            y = 1 - xy.y;

            if (m_HDR)
                SpecialHDRBrightnessHandling(boxPos, sliderPos);

            Rect r2 = new Rect(sliderPos.x + 1, sliderPos.y + 1, sliderPos.width - 2, sliderPos.height - 2);
            DrawColorSlider(r2, new Vector2(x, y));

            // Vertical slider
            if (Event.current.type == EventType.MouseDown && sliderPos.Contains(Event.current.mousePosition))
                RemoveFocusFromActiveTextField();
            z = GUI.VerticalSlider(sliderPos, z, 1, 0, box, thumbHoriz);

            // Labels
            DrawLabelOutsideRect(new Rect(sliderPos.xMax - sliderPos.height, sliderPos.y, sliderPos.height + 1, sliderPos.height + 1), GetZAxisLabel(m_ColorBoxMode), LabelLocation.Right);
        }

        void RemoveFocusFromActiveTextField()
        {
            EditorGUI.EndEditingActiveTextField();
            GUIUtility.keyboardControl = 0;
        }

        static Texture2D s_LeftGradientTexture;
        static Texture2D s_RightGradientTexture;

        public static Texture2D GetGradientTextureWithAlpha1To0()
        {
            return s_LeftGradientTexture ?? (s_LeftGradientTexture = CreateGradientTexture("ColorPicker_1To0_Gradient", 4, 4, new Color(1, 1, 1, 1), new Color(1, 1, 1, 0)));
        }

        public static Texture2D GetGradientTextureWithAlpha0To1()
        {
            return s_RightGradientTexture ?? (s_RightGradientTexture = CreateGradientTexture("ColorPicker_0To1_Gradient", 4, 4, new Color(1, 1, 1, 0), new Color(1, 1, 1, 1)));
        }

        static Texture2D CreateGradientTexture(string name, int width, int height, Color leftColor, Color rightColor)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = name;
            texture.hideFlags = HideFlags.HideAndDontSave;
            var pixels = new Color[width * height];

            for (int i = 0; i < width; i++)
            {
                Color columnColor = Color.Lerp(leftColor, rightColor, i / (float)(width - 1));
                for (int j = 0; j < height; j++)
                    pixels[j * width + i] = columnColor;
            }

            texture.SetPixels(pixels);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply();
            return texture;
        }

        void TonemappingControls()
        {
            bool updateTextures = false;

            EditorGUI.BeginChangeCheck();
            m_UseTonemappingPreview = GUILayout.Toggle(m_UseTonemappingPreview, styles.tonemappingToggle, styles.toggle);
            if (EditorGUI.EndChangeCheck())
                updateTextures = true;

            if (m_UseTonemappingPreview)
            {
                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();
                float power = QualitySettings.activeColorSpace == ColorSpace.Linear ? 1f : 2f; // for gamma space we want a power slider
                m_HDRValues.m_ExposureAdjustment = EditorGUILayout.PowerSlider("", m_HDRValues.m_ExposureAdjustment, m_HDRConfig.minExposureValue, m_HDRConfig.maxExposureValue, power);
                if (Event.current.type == EventType.Repaint)
                    GUI.Label(EditorGUILayout.s_LastRect, GUIContent.Temp("", "Exposure value"));
                if (EditorGUI.EndChangeCheck())
                    updateTextures = true;

                Rect r = EditorGUILayout.GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.numberField);
                EditorGUI.LabelField(r, GUIContent.Temp("Tonemapped Color"));
                Rect colorBox = new Rect(r.xMax - fieldWidth, r.y, fieldWidth, r.height);
                EditorGUIUtility.DrawColorSwatch(colorBox, DoTonemapping(color, m_HDRValues.m_ExposureAdjustment), false, false);
                GUI.Label(colorBox, GUIContent.none, styles.colorPickerBox);

                EditorGUI.indentLevel--;
            }

            if (updateTextures)
            {
                m_RGBHSVSlidersDirty = true;
                m_ColorSpaceBoxDirty = true;
                m_ColorSliderDirty = true;
            }
        }

        static float PhotographicTonemapping(float value, float exposureAdjustment)
        {
            return 1 - Mathf.Pow(2, -exposureAdjustment * value);
        }

        static Color DoTonemapping(Color col, float exposureAdjustment)
        {
            col.r = PhotographicTonemapping(col.r, exposureAdjustment);
            col.g = PhotographicTonemapping(col.g, exposureAdjustment);
            col.b = PhotographicTonemapping(col.b, exposureAdjustment);
            return col;
        }

        static void DoTonemapping(Color[] colors, float exposureAdjustment, TonemappingType tonemappingType)
        {
            if (exposureAdjustment < 0)
                return;

            if (tonemappingType == TonemappingType.Linear)
            {
                for (int i = 0; i < colors.Length; ++i)
                    colors[i] = colors[i].RGBMultiplied(exposureAdjustment);
            }
            else // Tonemapping
            {
                for (int i = 0; i < colors.Length; ++i)
                    colors[i] = DoTonemapping(colors[i], exposureAdjustment);
            }
        }

        void SpecialHDRBrightnessHandling(Rect boxPos, Rect sliderPos)
        {
            const float editFieldHeight = 14f;
            const float margin = 2f;

            if (m_ColorBoxMode == ColorBoxMode.SV_H || m_ColorBoxMode == ColorBoxMode.HV_S)
            {
                float scrollDelta = GetScrollWheelDeltaInRect(boxPos);
                if (scrollDelta != 0f)
                {
                    SetMaxDisplayBrightness(m_HDRValues.m_HDRScaleFactor - scrollDelta * 0.05f);
                }

                Rect editAxisMaxValueRect = new Rect(0, boxPos.y - editFieldHeight * 0.5f, boxPos.x - margin, editFieldHeight);
                Rect dragAxisMaxValueRect = editAxisMaxValueRect;
                dragAxisMaxValueRect.y += editAxisMaxValueRect.height;
                EditorGUI.BeginChangeCheck();
                float newValue = EditableAxisLabel(editAxisMaxValueRect, dragAxisMaxValueRect, m_HDRValues.m_HDRScaleFactor, 1f, m_HDRConfig.maxBrightness, styles.axisLabelNumberField);
                if (EditorGUI.EndChangeCheck())
                    SetMaxDisplayBrightness(newValue);
            }

            if (m_ColorBoxMode == ColorBoxMode.HS_V)
            {
                Rect editAxisMaxValueRect = new Rect(sliderPos.xMax + margin, sliderPos.y - editFieldHeight * 0.5f, position.width - sliderPos.xMax - margin, editFieldHeight);
                Rect dragAxisMaxValueRect = editAxisMaxValueRect;
                dragAxisMaxValueRect.y += editAxisMaxValueRect.height;
                EditorGUI.BeginChangeCheck();
                float newValue = EditableAxisLabel(editAxisMaxValueRect, dragAxisMaxValueRect, m_HDRValues.m_HDRScaleFactor, 1f, m_HDRConfig.maxBrightness, styles.axisLabelNumberField);
                if (EditorGUI.EndChangeCheck())
                    SetMaxDisplayBrightness(newValue);
            }
        }

        private static float EditableAxisLabel(Rect rect, Rect dragRect, float value, float minValue, float maxValue, GUIStyle style)
        {
            int id = GUIUtility.GetControlID(162594855, FocusType.Keyboard, rect);
            string orgFormat = EditorGUI.kFloatFieldFormatString;
            EditorGUI.kFloatFieldFormatString = value < 10f ? "n1" : "n0";
            float newValue =  EditorGUI.DoFloatField(EditorGUI.s_RecycledEditor, rect, dragRect, id, value, EditorGUI.kFloatFieldFormatString, style, true);
            EditorGUI.kFloatFieldFormatString = orgFormat;
            return Mathf.Clamp(newValue, minValue, maxValue);
        }

        void SendEvent(bool exitGUI)
        {
            if (m_DelegateView)
            {
                Event e = EditorGUIUtility.CommandEvent("ColorPickerChanged");
                if (!m_IsOSColorPicker)
                    Repaint();
                m_DelegateView.SendEvent(e);
                if (!m_IsOSColorPicker && exitGUI)
                    GUIUtility.ExitGUI();
            }
            if (m_OnColorChanged != null)
            {
                m_OnColorChanged(color);
                Repaint();
            }
        }

        private void SetNormalizedColor(Color c)
        {
            if (c.maxColorComponent > 1f)
                Debug.LogError("Setting normalized color with a non-normalized color: " + c);
            m_Color = c;
            m_R = c.r;
            m_G = c.g;
            m_B = c.b;
            RGBToHSV();
            m_A = c.a;
        }

        private void SetColor(Color c)
        {
            if (m_IsOSColorPicker)
                OSColorPicker.color = c;
            else
            {
                float oldNormalizeValue = m_HDRValues.m_HDRScaleFactor;
                if (m_HDR)
                {
                    // Ensure internal state is normalized if hdr color has values above 1.0f
                    float maxColorComponent = c.maxColorComponent;
                    if (maxColorComponent > 1.0f)
                        c = c.RGBMultiplied(1f / maxColorComponent);
                    SetHDRScaleFactor(Mathf.Max(1, maxColorComponent));
                }

                if (m_Color.r == c.r && m_Color.g == c.g && m_Color.b == c.b && m_Color.a == c.a && oldNormalizeValue == m_HDRValues.m_HDRScaleFactor)
                    return;

                if (c.r > 1.0f || c.g > 1.0f || c.b > 1.0f)
                    Debug.LogError(string.Format("Invalid normalized color: {0}, normalize value: {1}", c, m_HDRValues.m_HDRScaleFactor));

                SetNormalizedColor(c);
                Repaint();
            }
        }

        public static void Show(GUIView viewToUpdate, Color col)
        {
            Show(viewToUpdate, col, true, false, null);
        }

        protected static ColorPicker PrepareShow(Color col, bool showAlpha, bool hdr, ColorPickerHDRConfig hdrConfig)
        {
            ColorPicker cp = ColorPicker.get;
            cp.m_HDR = hdr;
            cp.m_HDRConfig = new ColorPickerHDRConfig(hdrConfig ?? defaultHDRConfig);
            cp.SetColor(col);
            cp.m_OriginalColor = get.m_Color;
            cp.m_ShowAlpha = showAlpha;
            cp.m_ModalUndoGroup = Undo.GetCurrentGroup();


            // For now we enforce our Color Picker for hdr colors
            if (cp.m_HDR)
                cp.m_IsOSColorPicker = false;

            if (cp.m_IsOSColorPicker)
                OSColorPicker.Show(showAlpha);
            else
            {
                cp.titleContent = hdr ? EditorGUIUtility.TextContent("HDR Color") : EditorGUIUtility.TextContent("Color");
                float height = EditorPrefs.GetInt("CPickerHeight", (int)cp.position.height);
                cp.minSize = new Vector2(kFixedWindowWidth, height);
                cp.maxSize = new Vector2(kFixedWindowWidth, height);
                cp.InitIfNeeded(); // Ensure the heavy lifting of loading presets are done before window is visible
                cp.ShowAuxWindow();
            }

            return cp;
        }

        System.Action<Color> m_OnColorChanged;

        public static void Show(System.Action<Color> onColorChanged, Color col, bool showAlpha, bool hdr, ColorPickerHDRConfig hdrConfig)
        {
            ColorPicker cp = PrepareShow(col, showAlpha, hdr, hdrConfig);
            cp.m_DelegateView = null;
            cp.m_OnColorChanged = onColorChanged;
        }

        public static void Show(GUIView viewToUpdate, Color col, bool showAlpha, bool hdr, ColorPickerHDRConfig hdrConfig)
        {
            ColorPicker cp = PrepareShow(col, showAlpha, hdr, hdrConfig);
            cp.m_DelegateView = viewToUpdate;
            cp.m_OnColorChanged = null;
        }

        void PollOSColorPicker()
        {
            if (m_IsOSColorPicker)
            {
                if (!OSColorPicker.visible || Application.platform != RuntimePlatform.OSXEditor)
                {
                    DestroyImmediate(this);
                }
                else
                {
                    Color c = OSColorPicker.color;
                    if (m_Color != c)
                    {
                        m_Color = c;
                        SendEvent(true);
                    }
                }
            }
        }

        void OnEnable()
        {
            hideFlags = HideFlags.DontSave;
            m_IsOSColorPicker = EditorPrefs.GetBool("UseOSColorPicker");
            hideFlags = HideFlags.DontSave;
            EditorApplication.update += PollOSColorPicker;
            EditorGUIUtility.editingTextField = true; // To fix that color values is not directly editable when tabbing (case 557510)

            m_HDRValues.m_ExposureAdjustment = EditorPrefs.GetFloat("CPickerExposure", 1.0f);
            m_UseTonemappingPreview = EditorPrefs.GetInt("CPTonePreview", 0) != 0;
            m_SliderMode = (SliderMode)EditorPrefs.GetInt("CPSliderMode", (int)SliderMode.RGB);
            m_ColorBoxMode = (ColorBoxMode)EditorPrefs.GetInt("CPColorMode", (int)ColorBoxMode.SV_H);
            m_ShowPresets = EditorPrefs.GetInt("CPPresetsShow", 1) != 0;
        }

        void OnDisable()
        {
            EditorPrefs.SetFloat("CPickerExposure", m_HDRValues.m_ExposureAdjustment);
            EditorPrefs.SetInt("CPTonePreview", m_UseTonemappingPreview ? 1 : 0);
            EditorPrefs.SetInt("CPSliderMode", (int)m_SliderMode);
            EditorPrefs.SetInt("CPColorMode", (int)m_ColorBoxMode);
            EditorPrefs.SetInt("CPPresetsShow", m_ShowPresets ? 1 : 0);
            EditorPrefs.SetInt("CPickerHeight", (int)position.height);
        }

        public void OnDestroy()
        {
            Undo.CollapseUndoOperations(m_ModalUndoGroup);

            if (m_ColorSlider)
                Object.DestroyImmediate(m_ColorSlider);
            if (m_ColorBox)
                Object.DestroyImmediate(m_ColorBox);
            if (m_RTexture)
                Object.DestroyImmediate(m_RTexture);
            if (m_GTexture)
                Object.DestroyImmediate(m_GTexture);
            if (m_BTexture)
                Object.DestroyImmediate(m_BTexture);
            if (m_HueTexture)
                Object.DestroyImmediate(m_HueTexture);
            if (m_SatTexture)
                Object.DestroyImmediate(m_SatTexture);
            if (m_ValTexture)
                Object.DestroyImmediate(m_ValTexture);
            if (m_AlphaTexture)
                Object.DestroyImmediate(m_AlphaTexture);
            s_SharedColorPicker = null;
            if (m_IsOSColorPicker)
                OSColorPicker.Close();

            EditorApplication.update -= PollOSColorPicker;

            if (m_ColorLibraryEditorState != null)
                m_ColorLibraryEditorState.TransferEditorPrefsState(false);

            if (m_ColorLibraryEditor != null)
                m_ColorLibraryEditor.UnloadUsedLibraries();

            if (m_ColorBoxMode == ColorBoxMode.EyeDropper)
                EditorPrefs.SetInt("CPColorMode", (int)m_OldColorBoxMode);
        }
    }

    internal class EyeDropper : GUIView
    {
        const int kPixelSize = 10;
        const int kDummyWindowSize = 8192;
        static internal Color s_LastPickedColor;
        GUIView m_DelegateView;
        Texture2D m_Preview;
        static EyeDropper s_Instance;
        private static Vector2 s_PickCoordinates = Vector2.zero;
        private bool m_Focused = false;


        public System.Action<Color> m_OnColorPicked;

        public static void Start(GUIView viewToUpdate)
        {
            get.Show(viewToUpdate, null);
        }

        public static void Start(System.Action<Color> onColorPicked)
        {
            get.Show(null, onColorPicked);
        }

        static EyeDropper get
        {
            get
            {
                if (!s_Instance)
                    ScriptableObject.CreateInstance<EyeDropper>();
                return s_Instance;
            }
        }

        EyeDropper()
        {
            s_Instance = this;
        }

        void Show(GUIView sourceView, System.Action<Color> onColorPicked)
        {
            m_DelegateView = sourceView;
            m_OnColorPicked = onColorPicked;
            ContainerWindow win = ScriptableObject.CreateInstance<ContainerWindow>();
            win.m_DontSaveToLayout = true;
            win.title = "EyeDropper";
            win.hideFlags = HideFlags.DontSave;
            win.rootView = this;
            win.Show(ShowMode.PopupMenu, true, false);
            AddToAuxWindowList();
            win.SetInvisible();
            SetMinMaxSizes(new Vector2(0, 0), new Vector2(kDummyWindowSize, kDummyWindowSize));
            win.position = new Rect(-kDummyWindowSize / 2, -kDummyWindowSize / 2, kDummyWindowSize, kDummyWindowSize);
            wantsMouseMove = true;
            StealMouseCapture();
        }

        public static Color GetPickedColor()
        {
            return InternalEditorUtility.ReadScreenPixel(s_PickCoordinates, 1, 1)[0];
        }

        public static Color GetLastPickedColor()
        {
            return s_LastPickedColor;
        }

        class Styles
        {
            public GUIStyle eyeDropperHorizontalLine = "EyeDropperHorizontalLine";
            public GUIStyle eyeDropperVerticalLine = "EyeDropperVerticalLine";
            public GUIStyle eyeDropperPickedPixel = "EyeDropperPickedPixel";
        }
        static Styles styles;

        public static void DrawPreview(Rect position)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            if (styles == null)
                styles = new Styles();

            GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;

            Texture2D preview = get.m_Preview;
            int width = (int)Mathf.Ceil(position.width / kPixelSize);
            int height = (int)Mathf.Ceil(position.height / kPixelSize);
            if (preview == null)
            {
                get.m_Preview = preview = ColorPicker.MakeTexture(width, height);
                preview.filterMode = FilterMode.Point;
            }
            if (preview.width != width || preview.height != height)
            {
                preview.Resize((int)width, (int)height);
            }

            Vector2 p = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
            Vector2 mPos = p - new Vector2((int)(width / 2), (int)(height / 2));
            preview.SetPixels(InternalEditorUtility.ReadScreenPixel(mPos, width, height), 0);
            preview.Apply(true);

            Graphics.DrawTexture(position, preview);

            // Draw grid on top
            float xStep = position.width / width;
            GUIStyle sep = styles.eyeDropperVerticalLine;
            for (float x = position.x; x < position.xMax; x += xStep)
            {
                Rect r = new Rect(Mathf.Round(x), position.y, xStep, position.height);
                sep.Draw(r, false, false, false, false);
            }

            float yStep = position.height / height;
            sep = styles.eyeDropperHorizontalLine;
            for (float y = position.y; y < position.yMax; y += yStep)
            {
                Rect r = new Rect(position.x, Mathf.Floor(y), position.width, yStep);
                sep.Draw(r, false, false, false, false);
            }

            // Draw selected pixelSize
            Rect newR = new Rect((p.x - mPos.x) * xStep + position.x, (p.y - mPos.y) * yStep + position.y, xStep, yStep);
            styles.eyeDropperPickedPixel.Draw(newR, false, false, false, false);

            GL.sRGBWrite = false;
        }

        protected override void OldOnGUI()
        {
            // On mouse move/click we remember screen coordinates where we are. Then we'll use that
            // in GetPickedColor to read. The reason is that because GetPickedColor might be called from
            // an event which is different, so the coordinates would be already wrong.
            switch (Event.current.type)
            {
                case EventType.MouseMove:
                    s_PickCoordinates = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
                    StealMouseCapture();
                    SendEvent("EyeDropperUpdate", true);
                    break;
                case EventType.MouseDown:
                    if (Event.current.button == 0)
                    {
                        s_PickCoordinates = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
                        // We have to close helper window before we read color from screen. On Win
                        // the window covers whole desktop (see Show()) and is black with 0x01 alpha value.
                        // That might cause invalid picked color.
                        window.Close();
                        s_LastPickedColor = EyeDropper.GetPickedColor();
                        SendEvent("EyeDropperClicked", true);
                    }
                    break;
                case EventType.KeyDown:
                    if (Event.current.keyCode == KeyCode.Escape)
                    {
                        window.Close();
                        SendEvent("EyeDropperCancelled", true);
                    }
                    break;
            }
        }

        void SendEvent(string eventName, bool exitGUI)
        {
            if (m_DelegateView)
            {
                Event e = EditorGUIUtility.CommandEvent(eventName);
                m_DelegateView.SendEvent(e);
                if (exitGUI)
                    GUIUtility.ExitGUI();
            }

            if (m_OnColorPicked != null && eventName == "EyeDropperClicked")
            {
                m_OnColorPicked(s_LastPickedColor);
            }
        }

        public new void OnDestroy()
        {
            if (m_Preview)
                Object.DestroyImmediate(m_Preview);

            if (!m_Focused)
            {
                // Window closed before it was focused
                SendEvent("EyeDropperCancelled", false);
            }

            base.OnDestroy();
        }

        override protected bool OnFocus()
        {
            m_Focused = true;
            return base.OnFocus();
        }
    }


    [Serializable]
    public class ColorPickerHDRConfig
    {
        [SerializeField]
        public float minBrightness;
        [SerializeField]
        public float maxBrightness;
        [SerializeField]
        public float minExposureValue;
        [SerializeField]
        public float maxExposureValue;

        public ColorPickerHDRConfig(float minBrightness, float maxBrightness, float minExposureValue, float maxExposureValue)
        {
            this.minBrightness = minBrightness;
            this.maxBrightness = maxBrightness;
            this.minExposureValue = minExposureValue;
            this.maxExposureValue = maxExposureValue;
        }

        internal ColorPickerHDRConfig(ColorPickerHDRConfig other)
        {
            minBrightness = other.minBrightness;
            maxBrightness = other.maxBrightness;
            minExposureValue = other.minExposureValue;
            maxExposureValue = other.maxExposureValue;
        }

        private static readonly ColorPickerHDRConfig s_Temp = new ColorPickerHDRConfig(0, 0, 0, 0);

        internal static ColorPickerHDRConfig Temp(float minBrightness, float maxBrightness, float minExposure, float maxExposure)
        {
            s_Temp.minBrightness = minBrightness;
            s_Temp.maxBrightness = maxBrightness;
            s_Temp.minExposureValue = minExposure;
            s_Temp.maxExposureValue = maxExposure;
            return s_Temp;
        }
    }
} // namespace
