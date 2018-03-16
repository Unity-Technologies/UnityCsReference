// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Accessibility;

namespace UnityEditor
{
    internal class ColorPicker : EditorWindow
    {
        private const string k_HeightPrefKey = "CPickerHeight";
        private const string k_ShowPresetsPrefKey = "CPPresetsShow";
        // HDR and LDR have different slider mode pref keys because they have different defaults for the sake of discoverability
        private const string k_SliderModePrefKey = "CPSliderMode";
        private const string k_SliderModeHDRPrefKey = "CPSliderModeHDR";

        // the default max amount of stops to use for the intensity slider; same as in Photoshop/Affinity
        private const float k_DefaultExposureSliderMax = 10f;

        public static string presetsEditorPrefID { get { return "Color"; } }

        [SerializeField]
        bool m_HDR;

        [SerializeField]
        private ColorMutator m_Color;

        #pragma warning disable 649
        [SerializeField]
        Texture2D m_ColorSlider;

        [SerializeField]
        Color[] m_Colors;
        const int kHueRes = 64;
        const int kColorBoxSize = 32;
        [SerializeField]
        Texture2D m_ColorBox;
        static int s_Slider2Dhash = "Slider2D".GetHashCode();
        [SerializeField]
        bool m_ShowPresets = true;

        [SerializeField]
        bool m_IsOSColorPicker = false;
        [SerializeField]
        bool m_ShowAlpha = true;

        [SerializeField]
        Texture2D m_RTexture; float m_RTextureG = -1, m_RTextureB = -1;
        [SerializeField]
        Texture2D m_GTexture; float m_GTextureR = -1, m_GTextureB = -1;
        [SerializeField]
        Texture2D m_BTexture; float m_BTextureR = -1, m_BTextureG = -1;

        [SerializeField]
        Texture2D m_HueTexture; float m_HueTextureS = -1, m_HueTextureV = -1;
        [SerializeField]
        Texture2D m_SatTexture; float m_SatTextureH = -1, m_SatTextureV = -1;
        [SerializeField]
        Texture2D m_ValTexture; float m_ValTextureH = -1, m_ValTextureS = -1;

        [NonSerialized]
        int m_TextureColorBoxMode = -1;
        [SerializeField]
        float m_LastConstant = -1;

        [NonSerialized]
        bool m_ColorSpaceBoxDirty;

        enum ColorBoxMode { HSV, EyeDropper }

        [SerializeField]
        ColorBoxMode m_ColorBoxMode = ColorBoxMode.HSV;

        enum SliderMode { RGB, RGBFloat, HSV }

        [SerializeField]
        SliderMode m_SliderMode = SliderMode.HSV;

        [SerializeField]
        Texture2D m_AlphaTexture; float m_OldAlpha = -1;

        [SerializeField]
        GUIView m_DelegateView;

        private Action<Color> m_ColorChangedCallback;

        [SerializeField]
        int m_ModalUndoGroup = -1;

        // hdr float slider ranges dynamically adjust on mouse up
        private float m_FloatSliderMaxOnMouseDown;
        private bool m_DraggingFloatSlider;

        // the exposure slider range can dynamically grow if needed per color picker "session"
        private float m_ExposureSliderMax = k_DefaultExposureSliderMax;

        PresetLibraryEditor<ColorPresetLibrary> m_ColorLibraryEditor;
        PresetLibraryEditorState m_ColorLibraryEditorState;

        public static Color color
        {
            get { return instance.m_Color.exposureAdjustedColor; }
            set
            {
                instance.SetColor(value);
                instance.Repaint();
            }
        }

        public static bool visible
        {
            get { return s_Instance != null; }
        }

        public static ColorPicker instance
        {
            get
            {
                if (!s_Instance)
                {
                    var hmm = Resources.FindObjectsOfTypeAll(typeof(ColorPicker));
                    if (hmm != null && hmm.Length > 0)
                        s_Instance = (ColorPicker)hmm[0];
                    if (!s_Instance)
                    {
                        s_Instance = CreateInstance<ColorPicker>();
                        s_Instance.wantsMouseMove = true;
                    }
                }
                return s_Instance;
            }
        }
        static ColorPicker s_Instance;

        public static int originalKeyboardControl { get; private set; }

        // ------- Soerens 2D slider --------

        static void swap(ref float f1, ref float f2) { float tmp = f1; f1 = f2; f2 = tmp; }

        Vector2 Slider2D(Rect rect, Vector2 value, Vector2 maxvalue, Vector2 minvalue, GUIStyle thumbStyle)
        {
            int id = GUIUtility.GetControlID(s_Slider2Dhash, FocusType.Passive);

            // test max and min
            if (maxvalue.x < minvalue.x) // swap
                swap(ref maxvalue.x, ref minvalue.x);
            if (maxvalue.y < minvalue.y)
                swap(ref maxvalue.y, ref minvalue.y);

            Event e = Event.current;

            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                {
                    if (rect.Contains(e.mousePosition)) // inside this control
                    {
                        GUIUtility.hotControl = id;
                        GUIUtility.keyboardControl = 0;
                        value.x = (e.mousePosition.x - rect.x) / rect.width * (maxvalue.x - minvalue.x);
                        value.y = (e.mousePosition.y - rect.y) / rect.height * (maxvalue.y - minvalue.y);
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
                    value.x = (e.mousePosition.x - rect.x) / rect.width * (maxvalue.x - minvalue.x);
                    value.y = (e.mousePosition.y - rect.y) / rect.height * (maxvalue.y - minvalue.y);

                    // clamp
                    value.x = Mathf.Clamp(value.x, minvalue.x, maxvalue.x);
                    value.y = Mathf.Clamp(value.y, minvalue.y, maxvalue.y);
                    GUI.changed = true;
                    Event.current.Use();
                }
                break;

                case EventType.Repaint:
                {
                    // thumb (set to black for light colors to prevent blending with background)
                    var oldColor = GUI.color;
                    GUI.color = VisionUtility.ComputePerceivedLuminance(color) > Styles.highLuminanceThreshold ?
                        Styles.highLuminanceContentColor : Styles.lowLuminanceContentColor;

                    var thumbRect = new Rect
                    {
                        size = thumbStyle.CalcSize(GUIContent.none),
                        center = new Vector2(
                                value.x / (maxvalue.x - minvalue.x) * rect.width + rect.x,
                                value.y / (maxvalue.y - minvalue.y) * rect.height + rect.y
                                )
                    };
                    thumbStyle.Draw(thumbRect, GUIContent.none, id);

                    GUI.color = oldColor;
                }
                break;
            }
            return value;
        }

        void RGBSliders()
        {
            var r = m_Color.GetColorChannelNormalized(RgbaChannel.R);
            var g = m_Color.GetColorChannelNormalized(RgbaChannel.G);
            var b = m_Color.GetColorChannelNormalized(RgbaChannel.B);

            m_RTexture = Update1DSlider(m_RTexture, kColorBoxSize, g, b, ref m_RTextureG, ref m_RTextureB, 0, false);
            m_GTexture = Update1DSlider(m_GTexture, kColorBoxSize, r, b, ref m_GTextureR, ref m_GTextureB, 1, false);
            m_BTexture = Update1DSlider(m_BTexture, kColorBoxSize, r, g, ref m_BTextureR, ref m_BTextureG, 2, false);

            RGBSlider("R", RgbaChannel.R, m_RTexture);
            GUILayout.Space(Styles.extraVerticalSpacing);
            RGBSlider("G", RgbaChannel.G, m_GTexture);
            GUILayout.Space(Styles.extraVerticalSpacing);
            RGBSlider("B", RgbaChannel.B, m_BTexture);
            GUILayout.Space(Styles.extraVerticalSpacing);
        }

        void RGBSlider(string label, RgbaChannel channel, Texture2D sliderBackground)
        {
            float value;
            switch (m_SliderMode)
            {
                case SliderMode.RGB:
                    value = m_Color.GetColorChannel(channel);
                    EditorGUI.BeginChangeCheck();
                    value = EditorGUILayout.SliderWithTexture(
                            GUIContent.Temp(label), value, 0f, 255f, EditorGUI.kIntFieldFormatString, 0f, 255f, sliderBackground
                            );
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_Color.SetColorChannel(channel, value / 255f);
                        OnColorChanged();
                    }
                    m_DraggingFloatSlider = false;
                    break;
                case SliderMode.RGBFloat:
                    value = m_Color.GetColorChannelHdr(channel);
                    var maxRgbNormalized = ((Color)m_Color.color).maxColorComponent;
                    var evtType = Event.current.type;
                    var sliderMax = m_HDR && m_Color.exposureAdjustedColor.maxColorComponent > 1f ? m_Color.exposureAdjustedColor.maxColorComponent / maxRgbNormalized : 1f;
                    var textFieldMax = m_HDR ? float.MaxValue : 1f;
                    EditorGUI.BeginChangeCheck();
                    value = EditorGUILayout.SliderWithTexture(
                            GUIContent.Temp(label), value,
                            0f, m_DraggingFloatSlider ? m_FloatSliderMaxOnMouseDown : sliderMax,
                            EditorGUI.kFloatFieldFormatString,
                            0f, textFieldMax,
                            sliderBackground
                            );
                    switch (evtType)
                    {
                        case EventType.MouseDown:
                            m_FloatSliderMaxOnMouseDown = sliderMax;
                            m_DraggingFloatSlider = true;
                            break;
                        case EventType.MouseUp:
                            m_DraggingFloatSlider = false;
                            break;
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_Color.SetColorChannelHdr(channel, value);
                        OnColorChanged();
                    }
                    break;
            }
        }

        Texture2D Update1DSlider(
            Texture2D tex, int xSize, float const1, float const2, ref float oldConst1, ref float oldConst2, int idx, bool hsvSpace
            )
        {
            if (!tex || const1 != oldConst1 || const2 != oldConst2)
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
                        start = m_Color.color;
                        start.a = 0f;
                        step = new Color(0, 0, 0, 1);
                        break;
                }
                FillArea(xSize, 2, colors, start, step, new Color(0, 0, 0, 0), !hsvSpace && m_HDR);
                if (hsvSpace)
                    HSVToRGBArray(colors, m_HDR);

                oldConst1 = const1;
                oldConst2 = const2;
                tex.SetPixels(colors);
                tex.Apply();
            }
            return tex;
        }

        void HSVSliders()
        {
            var h = m_Color.GetColorChannel(HsvChannel.H);
            var s = m_Color.GetColorChannel(HsvChannel.S);
            var v = m_Color.GetColorChannel(HsvChannel.V);

            m_HueTexture = Update1DSlider(m_HueTexture, kHueRes, 1, 1, ref m_HueTextureS, ref m_HueTextureV, 0, true);
            m_SatTexture = Update1DSlider(m_SatTexture, kColorBoxSize, h, Mathf.Max(v, .2f), ref m_SatTextureH, ref m_SatTextureV, 1, true);
            m_ValTexture = Update1DSlider(m_ValTexture, kColorBoxSize, h, s, ref m_ValTextureH, ref m_ValTextureS, 2, true);

            EditorGUI.BeginChangeCheck();
            h = EditorGUILayout.SliderWithTexture(
                    GUIContent.Temp("H"), h * 360f, 0f, 360f, EditorGUI.kIntFieldFormatString, m_HueTexture
                    );
            if (EditorGUI.EndChangeCheck())
            {
                m_Color.SetColorChannel(HsvChannel.H, h / 360f);
                OnColorChanged();
            }
            GUILayout.Space(Styles.extraVerticalSpacing);

            EditorGUI.BeginChangeCheck();
            s = EditorGUILayout.SliderWithTexture(
                    GUIContent.Temp("S"), s * 100f, 0f, 100f, EditorGUI.kIntFieldFormatString, m_SatTexture
                    );
            if (EditorGUI.EndChangeCheck())
            {
                m_Color.SetColorChannel(HsvChannel.S, s / 100f);
                OnColorChanged();
            }
            GUILayout.Space(Styles.extraVerticalSpacing);

            EditorGUI.BeginChangeCheck();
            v = EditorGUILayout.SliderWithTexture(
                    GUIContent.Temp("V"), v * 100f, 0f, 100f, EditorGUI.kIntFieldFormatString, m_ValTexture
                    );
            if (EditorGUI.EndChangeCheck())
            {
                m_Color.SetColorChannel(HsvChannel.V, v / 100f);
                OnColorChanged();
            }
            GUILayout.Space(Styles.extraVerticalSpacing);
        }

        static void FillArea(int xSize, int ySize, Color[] retval, Color topLeftColor, Color rightGradient, Color downGradient, bool convertToGamma)
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
                    retval[current++] = convertToGamma ? p2.gamma : p2;
                    p2 += rightDelta;
                }
                p += downDelta;
            }
        }

        static void HSVToRGBArray(Color[] colors, bool convertToGamma)
        {
            int s = colors.Length;
            for (int i = 0; i < s; i++)
            {
                Color c = colors[i];
                Color c2 = Color.HSVToRGB(c.r, c.g, c.b);
                c2.a = c.a;
                colors[i] = convertToGamma ? c2.gamma : c2;
            }
        }

        public static Texture2D MakeTexture(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }

        void DrawColorSpaceBox(Rect colorBoxRect, float constantValue)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            // If we've switched mode, regenerate box
            if ((int)m_ColorBoxMode != m_TextureColorBoxMode)
            {
                int xSize = kColorBoxSize, ySize = kColorBoxSize;

                if (m_ColorBox == null)
                    m_ColorBox = MakeTexture(xSize, ySize);

                if (m_ColorBox.width != xSize || m_ColorBox.height != ySize)
                    m_ColorBox.Resize(xSize, ySize);
            }

            if ((int)m_ColorBoxMode != m_TextureColorBoxMode || m_LastConstant != constantValue || m_ColorSpaceBoxDirty)
            {
                m_Colors = m_ColorBox.GetPixels(0);
                int xSize = m_ColorBox.width;
                int ySize = m_ColorBox.height;

                FillArea(xSize, ySize, m_Colors, new Color(m_Color.GetColorChannel(HsvChannel.H), 0, 0, 1), new Color(0, 1, 0, 0), new Color(0, 0, 1, 0), false);
                HSVToRGBArray(m_Colors, m_HDR);

                m_ColorBox.SetPixels(m_Colors, 0);
                m_ColorBox.Apply(true);
                m_LastConstant = constantValue;
                m_TextureColorBoxMode = (int)m_ColorBoxMode;
            }
            Graphics.DrawTexture(colorBoxRect, m_ColorBox, new Rect(.5f / m_ColorBox.width, .5f / m_ColorBox.height, 1 - 1f / m_ColorBox.width, 1 - 1f / m_ColorBox.height), 0, 0, 0, 0, Color.grey);
        }

        static class Styles
        {
            public const float fixedWindowWidth = 233;
            public const float hexFieldWidth = 72f;
            public const float sliderModeFieldWidth = hexFieldWidth;
            public const float channelSliderLabelWidth = 14f;
            public const float sliderTextFieldWidth = 45f;
            public const float extraVerticalSpacing = 8f - EditorGUI.kControlVerticalSpacing;
            public const float highLuminanceThreshold = 0.5f;

            public static readonly float hueDialThumbSize;

            public static readonly RectOffset colorBoxPadding = new RectOffset(6, 6, 6, 6);

            public static readonly Color lowLuminanceContentColor = Color.white;
            public static readonly Color highLuminanceContentColor = Color.black;

            public static readonly GUIStyle originalColorSwatch = "ColorPickerOriginalColor";
            public static readonly GUIStyle currentColorSwatch = "ColorPickerCurrentColor";
            public static readonly GUIStyle colorBoxThumb = "ColorPicker2DThumb";
            public static readonly GUIStyle hueDialBackground = "ColorPickerHueRing";
            public static readonly GUIStyle hueDialBackgroundHDR = "ColorPickerHueRing-HDR";
            public static readonly GUIStyle hueDialThumb = "ColorPickerHueRingThumb";
            public static readonly GUIStyle sliderBackground = "ColorPickerSliderBackground";
            public static readonly GUIStyle sliderThumb = "ColorPickerHorizThumb";
            public static readonly GUIStyle background = new GUIStyle("ColorPickerBackground");
            public static readonly GUIStyle exposureSwatch = "ColorPickerExposureSwatch";
            public static readonly GUIStyle selectedExposureSwatchStroke = "ColorPickerCurrentExposureSwatchBorder";

            public static readonly GUIContent eyeDropper = EditorGUIUtility.TrIconContent("EyeDropper.Large", "Pick a color from the screen.");
            public static readonly GUIContent exposureValue = EditorGUIUtility.TrTextContent("Intensity", "Number of stops to over- or under-expose the color.");
            public static readonly GUIContent hexLabel = EditorGUIUtility.TrTextContent("Hexadecimal");
            public static readonly GUIContent presetsToggle = EditorGUIUtility.TrTextContent("Swatches");

            public static readonly ScalableGUIContent originalColorSwatchFill =
                new ScalableGUIContent(string.Empty, "The original color. Click this swatch to reset the color picker to this value.", "ColorPicker-OriginalColor");
            public static readonly ScalableGUIContent currentColorSwatchFill =
                new ScalableGUIContent(string.Empty, "The new color.", "ColorPicker-CurrentColor");
            public static readonly ScalableGUIContent hueDialThumbFill = new ScalableGUIContent("ColorPicker-HueRing-Thumb-Fill");

            // force load the checker background from the light skin
            public static readonly Texture2D alphaSliderCheckerBackground =
                EditorGUIUtility.LoadRequired("Previews/Textures/textureChecker.png") as Texture2D;

            public static readonly GUIContent[] sliderModeLabels = new[]
            {
                EditorGUIUtility.TrTextContent("RGB 0-255"),
                EditorGUIUtility.TrTextContent("RGB 0-1.0"),
                EditorGUIUtility.TrTextContent("HSV")
            };

            public static readonly int[] sliderModeValues = new[] { 0, 1, 2 };

            static Styles()
            {
                var thumbSize = hueDialThumb.CalcSize(hueDialThumbFill);
                hueDialThumbSize = Mathf.Max(thumbSize.x, thumbSize.y);
            }
        }

        public string currentPresetLibrary
        {
            get
            {
                InitializePresetsLibraryIfNeeded();
                return m_ColorLibraryEditor.currentLibraryWithoutExtension;
            }
            set
            {
                InitializePresetsLibraryIfNeeded();
                m_ColorLibraryEditor.currentLibraryWithoutExtension = value;
            }
        }

        void InitializePresetsLibraryIfNeeded()
        {
            if (m_ColorLibraryEditorState == null)
            {
                m_ColorLibraryEditorState = new PresetLibraryEditorState(presetsEditorPrefID);
                m_ColorLibraryEditorState.TransferEditorPrefsState(true);
            }

            if (m_ColorLibraryEditor == null)
            {
                var saveLoadHelper = new ScriptableObjectSaveLoadHelper<ColorPresetLibrary>("colors", SaveType.Text);
                m_ColorLibraryEditor = new PresetLibraryEditor<ColorPresetLibrary>(saveLoadHelper, m_ColorLibraryEditorState, OnClickedPresetSwatch);
                m_ColorLibraryEditor.previewAspect = 1f;
                m_ColorLibraryEditor.minMaxPreviewHeight = new Vector2(ColorPresetLibrary.kSwatchSize, ColorPresetLibrary.kSwatchSize);
                m_ColorLibraryEditor.settingsMenuRightMargin = 2f;
                m_ColorLibraryEditor.useOnePixelOverlappedGrid = true;
                m_ColorLibraryEditor.alwaysShowScrollAreaHorizontalLines = false;
                m_ColorLibraryEditor.marginsForGrid = new RectOffset(0, 0, 0, 0);
                m_ColorLibraryEditor.marginsForList = new RectOffset(0, 5, 2, 2);
                m_ColorLibraryEditor.InitializeGrid(Styles.fixedWindowWidth - (Styles.background.padding.left + Styles.background.padding.right));
            }
        }

        void OnClickedPresetSwatch(int clickCount, object presetObject)
        {
            Color color = (Color)presetObject;
            // extract RGB if not in HDR mode
            if (!m_HDR && color.maxColorComponent > 1f)
                color = new ColorMutator(color).color;

            SetColor(color);
        }

        private Color GetGUIColor(Color color)
        {
            return m_HDR ? color.gamma : color;
        }

        void DoColorSwatchAndEyedropper()
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button(Styles.eyeDropper, GUIStyle.none, GUILayout.Width(40), GUILayout.ExpandWidth(false)))
            {
                GUIUtility.keyboardControl = 0;
                EyeDropper.Start(m_Parent, false);
                m_ColorBoxMode = ColorBoxMode.EyeDropper;
                GUIUtility.ExitGUI();
            }

            var swatchColor = m_Color.exposureAdjustedColor;
            // current swatch and original swatch have the same size, so they can lay out in the same row
            var rect = GUILayoutUtility.GetRect(Styles.currentColorSwatchFill, Styles.currentColorSwatch, GUILayout.ExpandWidth(true));

            var swatchSize = Styles.currentColorSwatch.CalcSize(Styles.currentColorSwatchFill);
            var swatchRect = new Rect
            {
                size = swatchSize,
                y = rect.y,
                x = rect.xMax - swatchSize.x
            };

            var backgroundColor = GUI.backgroundColor;
            var contentColor = GUI.contentColor;

            var id = GUIUtility.GetControlID(FocusType.Passive);
            if (Event.current.type == EventType.Repaint)
            {
                GUI.backgroundColor = m_Color.exposureAdjustedColor.a == 1f ? Color.clear : Color.white;
                GUI.contentColor = GetGUIColor(m_Color.exposureAdjustedColor);
                Styles.currentColorSwatch.Draw(swatchRect, Styles.currentColorSwatchFill, id);
            }

            swatchRect.x -= swatchRect.width;
            GUI.backgroundColor = m_Color.originalColor.a == 1f ? Color.clear : Color.white;
            GUI.contentColor = GetGUIColor(m_Color.originalColor);
            if (GUI.Button(swatchRect, Styles.originalColorSwatchFill, Styles.originalColorSwatch))
            {
                m_Color.Reset();
                Event.current.Use();
                OnColorChanged();
            }

            GUI.backgroundColor = backgroundColor;
            GUI.contentColor = contentColor;

            GUILayout.EndHorizontal();
        }

        void DoColorSpaceGUI()
        {
            var backgroundStyle = m_HDR ? Styles.hueDialBackgroundHDR : Styles.hueDialBackground;
            var dialSize = backgroundStyle.CalcSize(GUIContent.none);
            var dialRect = GUILayoutUtility.GetRect(dialSize.x, dialSize.y);
            switch (m_ColorBoxMode)
            {
                case ColorBoxMode.HSV:

                    dialRect.x += (dialRect.width - dialRect.height) * 0.5f;
                    dialRect.width = dialRect.height;
                    var hue = m_Color.GetColorChannel(HsvChannel.H);
                    var oldColor = GUI.contentColor;
                    GUI.contentColor = GetGUIColor(Color.HSVToRGB(hue, 1f, 1f));

                    EditorGUI.BeginChangeCheck();

                    hue = EditorGUI.AngularDial(
                            dialRect,
                            GUIContent.none,
                            hue * 360f,
                            ((GUIContent)Styles.hueDialThumbFill).image,
                            backgroundStyle,
                            Styles.hueDialThumb
                            );

                    if (EditorGUI.EndChangeCheck())
                    {
                        hue = Mathf.Repeat(hue, 360f) / 360f;
                        m_Color.SetColorChannel(HsvChannel.H, hue);
                        OnColorChanged();
                    }
                    GUI.contentColor = oldColor;

                    var innerRadius = dialRect.width * 0.5f - Styles.hueDialThumbSize;
                    var size = Mathf.FloorToInt(Mathf.Sqrt(2f) * innerRadius);
                    if ((size & 1) == 1)
                        size += 1;
                    var svRect = new Rect { size = Vector2.one * size, center = dialRect.center };
                    svRect = Styles.colorBoxPadding.Remove(svRect);

                    DrawColorSpaceBox(svRect, m_Color.GetColorChannel(HsvChannel.H));

                    EditorGUI.BeginChangeCheck();

                    var sv = new Vector2(m_Color.GetColorChannel(HsvChannel.S), 1f - m_Color.GetColorChannel(HsvChannel.V));
                    sv = Slider2D(svRect, sv, Vector2.zero, Vector2.one, Styles.colorBoxThumb);

                    if (EditorGUI.EndChangeCheck())
                    {
                        m_Color.SetColorChannel(HsvChannel.S, sv.x);
                        m_Color.SetColorChannel(HsvChannel.V, 1f - sv.y);
                        OnColorChanged();
                    }
                    break;
                case ColorBoxMode.EyeDropper:
                    EyeDropper.DrawPreview(dialRect);
                    break;
            }
        }

        void DoColorSliders(float availableWidth)
        {
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            float oldFieldWidth = EditorGUIUtility.fieldWidth;
            EditorGUIUtility.labelWidth = availableWidth - Styles.sliderModeFieldWidth;
            EditorGUIUtility.fieldWidth = Styles.sliderModeFieldWidth;

            m_SliderMode = (SliderMode)EditorGUILayout.IntPopup(
                    GUIContent.Temp(" "), (int)m_SliderMode, Styles.sliderModeLabels, Styles.sliderModeValues
                    );
            GUILayout.Space(Styles.extraVerticalSpacing);

            EditorGUIUtility.labelWidth = oldLabelWidth;
            EditorGUIUtility.fieldWidth = oldFieldWidth;

            EditorGUIUtility.labelWidth = Styles.channelSliderLabelWidth;

            switch (m_SliderMode)
            {
                case SliderMode.HSV:
                    HSVSliders();
                    break;
                default:
                    RGBSliders();
                    break;
            }

            if (m_ShowAlpha)
            {
                m_AlphaTexture = Update1DSlider(m_AlphaTexture, kColorBoxSize, 0, 0, ref m_OldAlpha, ref m_OldAlpha, 3, false);

                float displayScale = 1f;
                string formatString = EditorGUI.kFloatFieldFormatString;
                switch (m_SliderMode)
                {
                    case SliderMode.HSV:
                        displayScale = 100f;
                        formatString = EditorGUI.kIntFieldFormatString;
                        break;
                    case SliderMode.RGB:
                        displayScale = 255f;
                        formatString = EditorGUI.kIntFieldFormatString;
                        break;
                }

                var rect = EditorGUILayout.GetControlRect(true);

                if (Event.current.type == EventType.Repaint)
                {
                    var backgroundRect = rect;
                    backgroundRect.xMin += EditorGUIUtility.labelWidth;
                    backgroundRect.xMax -= EditorGUIUtility.fieldWidth + EditorGUI.kSpacing;
                    backgroundRect = Styles.sliderBackground.padding.Remove(backgroundRect);
                    var uvLayout = new Rect
                    {
                        x = 0f,
                        y = 0f,
                        width = backgroundRect.width / backgroundRect.height, // texture aspect is 1:1
                        height = 1f
                    };
                    Graphics.DrawTexture(backgroundRect, Styles.alphaSliderCheckerBackground, uvLayout, 0, 0, 0, 0);
                }

                EditorGUI.BeginChangeCheck();
                var alpha = m_Color.GetColorChannelNormalized(RgbaChannel.A) * displayScale;
                alpha = EditorGUI.SliderWithTexture(
                        rect, GUIContent.Temp("A"), alpha, 0f, displayScale, formatString, m_AlphaTexture
                        );
                if (EditorGUI.EndChangeCheck())
                {
                    m_Color.SetColorChannel(RgbaChannel.A, alpha / displayScale);
                    OnColorChanged();
                }
                GUILayout.Space(Styles.extraVerticalSpacing);
            }

            EditorGUIUtility.labelWidth = oldLabelWidth;
        }

        void DoHexField(float availableWidth)
        {
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            float oldFieldWidth = EditorGUIUtility.fieldWidth;
            EditorGUIUtility.labelWidth = availableWidth - Styles.hexFieldWidth;
            EditorGUIUtility.fieldWidth = Styles.hexFieldWidth;

            EditorGUI.BeginChangeCheck();
            var newColor = EditorGUILayout.HexColorTextField(Styles.hexLabel, m_Color.color, false);
            if (EditorGUI.EndChangeCheck())
            {
                m_Color.SetColorChannel(RgbaChannel.R, newColor.r);
                m_Color.SetColorChannel(RgbaChannel.G, newColor.g);
                m_Color.SetColorChannel(RgbaChannel.B, newColor.b);
                OnColorChanged();
            }

            EditorGUIUtility.labelWidth = oldLabelWidth;
            EditorGUIUtility.fieldWidth = oldFieldWidth;
        }

        void DoExposureSlider()
        {
            var oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth =
                EditorStyles.label.CalcSize(Styles.exposureValue).x
                + EditorStyles.label.margin.right;

            var sliderPosition = GUILayoutUtility.GetRect(0f, EditorGUIUtility.singleLineHeight);

            EditorGUI.BeginChangeCheck();
            var exposureValue = EditorGUI.Slider(
                    sliderPosition, Styles.exposureValue, m_Color.exposureValue, -m_ExposureSliderMax, m_ExposureSliderMax, float.MinValue, float.MaxValue
                    );
            if (EditorGUI.EndChangeCheck())
            {
                m_Color.exposureValue = exposureValue;
                OnColorChanged();
            }

            EditorGUIUtility.labelWidth = oldLabelWidth;
        }

        void DoExposureSwatches()
        {
            var swatchesRect =
                GUILayoutUtility.GetRect(GUIContent.none, Styles.exposureSwatch, GUILayout.ExpandWidth(true));

            var numSwatches = 5;
            var swatchRect = new Rect
            {
                x = swatchesRect.x + (swatchesRect.width - numSwatches * Styles.exposureSwatch.fixedWidth) * 0.5f,
                y = swatchesRect.y,
                width = Styles.exposureSwatch.fixedWidth,
                height = Styles.exposureSwatch.fixedHeight
            };
            var backgroundColor = GUI.backgroundColor;
            var contentColor = GUI.contentColor;
            for (int i = 0; i < numSwatches; ++i)
            {
                var stop = i - numSwatches / 2;
                var col = (m_Color.exposureAdjustedColor * Mathf.Pow(2f, stop)).gamma;
                col.a = 1f;
                GUI.backgroundColor = col;
                GUI.contentColor = VisionUtility.ComputePerceivedLuminance(col) < Styles.highLuminanceThreshold ?
                    Styles.lowLuminanceContentColor : Styles.highLuminanceContentColor;

                if (
                    GUI.Button(
                        swatchRect,
                        GUIContent.Temp(stop == 0 ? null : (stop < 0 ? stop.ToString() : string.Format("+{0}", stop))),
                        Styles.exposureSwatch
                        )
                    )
                {
                    m_Color.exposureValue =
                        Mathf.Clamp(m_Color.exposureValue + stop, -m_ExposureSliderMax, m_ExposureSliderMax);
                    OnColorChanged();
                }

                if (stop == 0 && Event.current.type == EventType.Repaint)
                {
                    GUI.backgroundColor = GUI.contentColor;
                    Styles.selectedExposureSwatchStroke.Draw(swatchRect, false, false, false, false);
                }

                swatchRect.x += swatchRect.width;
            }
            GUI.backgroundColor = backgroundColor;
            GUI.contentColor = contentColor;
        }

        void DoPresetsGUI()
        {
            var foldoutRect = GUILayoutUtility.GetRect(Styles.presetsToggle, EditorStyles.foldout);
            foldoutRect.xMax -= 17f; // make room for presets settings menu button
            m_ShowPresets = EditorGUI.Foldout(foldoutRect, m_ShowPresets, Styles.presetsToggle, true);

            if (m_ShowPresets)
            {
                GUILayout.Space(-(EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing)); // pull up to reuse space
                var presetsRect = GUILayoutUtility.GetRect(0, Mathf.Clamp(m_ColorLibraryEditor.contentHeight, 20f, 250f));
                m_ColorLibraryEditor.OnGUI(presetsRect, color);
            }
        }

        void OnGUI()
        {
            InitializePresetsLibraryIfNeeded();

            EventType type = Event.current.type;

            if (type == EventType.ExecuteCommand)
            {
                switch (Event.current.commandName)
                {
                    case EventCommandNames.EyeDropperUpdate:
                        Repaint();
                        break;
                    case EventCommandNames.EyeDropperClicked:
                        m_ColorBoxMode = ColorBoxMode.HSV;
                        Color col = EyeDropper.GetLastPickedColor();
                        if (m_HDR)
                            col = col.linear;
                        m_Color.SetColorChannelHdr(RgbaChannel.R, col.r);
                        m_Color.SetColorChannelHdr(RgbaChannel.G, col.g);
                        m_Color.SetColorChannelHdr(RgbaChannel.B, col.b);
                        m_Color.SetColorChannelHdr(RgbaChannel.A, col.a);
                        OnColorChanged();
                        break;
                    case EventCommandNames.EyeDropperCancelled:
                        OnEyedropperCancelled();
                        break;
                }
            }

            Rect contentRect = EditorGUILayout.BeginVertical(Styles.background);

            // Setup layout values
            float innerContentWidth = EditorGUILayout.GetControlRect(false, 1, EditorStyles.numberField).width;
            EditorGUIUtility.labelWidth = innerContentWidth - Styles.sliderTextFieldWidth;
            EditorGUIUtility.fieldWidth = Styles.sliderTextFieldWidth;

            GUILayout.Space(10);
            DoColorSwatchAndEyedropper();

            GUILayout.Space(10);

            DoColorSpaceGUI();

            GUILayout.Space(10);

            DoColorSliders(innerContentWidth);
            DoHexField(innerContentWidth);
            GUILayout.Space(Styles.extraVerticalSpacing);

            if (m_HDR)
            {
                DoExposureSlider();
                GUILayout.Space(Styles.extraVerticalSpacing);
                DoExposureSwatches();
                GUILayout.Space(Styles.extraVerticalSpacing);
            }

            DoPresetsGUI();

            // Call last to ensure we only use the copy paste events if no
            // other controls wants to use these events
            HandleCopyPasteEvents();

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
                        // eyedropper GUIView never gets keyboard focus from ColorPicker, so esc to exit it must be handled here
                        if (m_ColorBoxMode == ColorBoxMode.EyeDropper)
                        {
                            EyeDropper.End();
                            OnEyedropperCancelled();
                        }
                        else
                        {
                            Undo.RevertAllDownToGroup(m_ModalUndoGroup);
                            m_Color.Reset();
                            OnColorChanged(false);
                            Close();
                            GUIUtility.ExitGUI();
                        }
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

        void OnEyedropperCancelled()
        {
            Repaint();
            m_ColorBoxMode = ColorBoxMode.HSV;
        }

        void SetHeight(float newHeight)
        {
            if (newHeight == position.height)
                return;
            minSize = new Vector2(Styles.fixedWindowWidth, newHeight);
            maxSize = new Vector2(Styles.fixedWindowWidth, newHeight);
        }

        void HandleCopyPasteEvents()
        {
            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.ValidateCommand:
                    switch (evt.commandName)
                    {
                        case EventCommandNames.Copy:
                        case EventCommandNames.Paste:
                            evt.Use();
                            break;
                    }
                    break;

                case EventType.ExecuteCommand:
                    switch (evt.commandName)
                    {
                        case EventCommandNames.Copy:
                            ColorClipboard.SetColor(color);
                            evt.Use();
                            break;

                        case EventCommandNames.Paste:
                            Color colorFromClipboard;
                            if (ColorClipboard.TryGetColor(m_HDR, out colorFromClipboard))
                            {
                                // Do not change alpha if color field is not showing alpha
                                if (!m_ShowAlpha)
                                    colorFromClipboard.a = m_Color.GetColorChannelNormalized(RgbaChannel.A);

                                SetColor(colorFromClipboard);

                                GUI.changed = true;
                                evt.Use();
                            }
                            break;
                    }
                    break;
            }
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
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
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

        void OnColorChanged(bool exitGUI = true)
        {
            m_OldAlpha = -1f;
            m_ColorSpaceBoxDirty = true;
            m_ExposureSliderMax = Mathf.Max(m_ExposureSliderMax, m_Color.exposureValue);
            if (m_DelegateView != null)
            {
                var e = EditorGUIUtility.CommandEvent(EventCommandNames.ColorPickerChanged);
                if (!m_IsOSColorPicker)
                    Repaint();
                m_DelegateView.SendEvent(e);
                if (!m_IsOSColorPicker && exitGUI)
                    GUIUtility.ExitGUI();
            }
            if (m_ColorChangedCallback != null)
            {
                m_ColorChangedCallback(color);
            }
        }

        private void SetColor(Color c)
        {
            if (m_IsOSColorPicker)
                OSColorPicker.color = c;
            else
            {
                m_Color.SetColorChannelHdr(RgbaChannel.R, c.r);
                m_Color.SetColorChannelHdr(RgbaChannel.G, c.g);
                m_Color.SetColorChannelHdr(RgbaChannel.B, c.b);
                m_Color.SetColorChannelHdr(RgbaChannel.A, c.a);
                OnColorChanged();
                Repaint();
            }
        }

        public static void Show(GUIView viewToUpdate, Color col, bool showAlpha = true, bool hdr = false)
        {
            Show(viewToUpdate, null, col, showAlpha, hdr);
        }

        public static void Show(Action<Color> colorChangedCallback, Color col, bool showAlpha = true, bool hdr = false)
        {
            Show(null, colorChangedCallback, col, showAlpha, hdr);
        }

        static void Show(GUIView viewToUpdate, Action<Color> colorChangedCallback, Color col, bool showAlpha, bool hdr)
        {
            var cp = instance;
            cp.m_HDR = hdr;
            cp.m_Color = new ColorMutator(col);
            cp.m_ShowAlpha = showAlpha;
            cp.m_DelegateView = viewToUpdate;
            cp.m_ColorChangedCallback = colorChangedCallback;
            cp.m_ModalUndoGroup = Undo.GetCurrentGroup();
            cp.m_ExposureSliderMax = Mathf.Max(cp.m_ExposureSliderMax, cp.m_Color.exposureValue);
            originalKeyboardControl = GUIUtility.keyboardControl;

            if (cp.m_HDR)
            {
                cp.m_IsOSColorPicker = false;
                cp.m_SliderMode = (SliderMode)EditorPrefs.GetInt(k_SliderModeHDRPrefKey, (int)SliderMode.RGB);
            }

            if (cp.m_IsOSColorPicker)
                OSColorPicker.Show(showAlpha);
            else
            {
                cp.titleContent = hdr ? EditorGUIUtility.TrTextContent("HDR Color") : EditorGUIUtility.TrTextContent("Color");
                float height = EditorPrefs.GetInt(k_HeightPrefKey, (int)cp.position.height);
                cp.minSize = new Vector2(Styles.fixedWindowWidth, height);
                cp.maxSize = new Vector2(Styles.fixedWindowWidth, height);
                cp.InitializePresetsLibraryIfNeeded(); // Ensure the heavy lifting of loading presets is done before window is visible
                cp.ShowAuxWindow();
            }
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
                    if (m_Color.color != c)
                    {
                        m_Color.SetColorChannel(RgbaChannel.R, c.r);
                        m_Color.SetColorChannel(RgbaChannel.G, c.g);
                        m_Color.SetColorChannel(RgbaChannel.B, c.b);
                        m_Color.SetColorChannel(RgbaChannel.A, c.a);
                        OnColorChanged();
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

            m_SliderMode = (SliderMode)EditorPrefs.GetInt(k_SliderModePrefKey, (int)SliderMode.RGB);
            m_ShowPresets = EditorPrefs.GetInt(k_ShowPresetsPrefKey, 1) != 0;
        }

        void OnDisable()
        {
            EditorPrefs.SetInt(m_HDR ? k_SliderModeHDRPrefKey : k_SliderModePrefKey, (int)m_SliderMode);
            EditorPrefs.SetInt(k_ShowPresetsPrefKey, m_ShowPresets ? 1 : 0);
            EditorPrefs.SetInt(k_HeightPrefKey, (int)position.height);
        }

        public void OnDestroy()
        {
            Undo.CollapseUndoOperations(m_ModalUndoGroup);

            if (m_ColorSlider)
                DestroyImmediate(m_ColorSlider);
            if (m_ColorBox)
                DestroyImmediate(m_ColorBox);
            if (m_RTexture)
                DestroyImmediate(m_RTexture);
            if (m_GTexture)
                DestroyImmediate(m_GTexture);
            if (m_BTexture)
                DestroyImmediate(m_BTexture);
            if (m_HueTexture)
                DestroyImmediate(m_HueTexture);
            if (m_SatTexture)
                DestroyImmediate(m_SatTexture);
            if (m_ValTexture)
                DestroyImmediate(m_ValTexture);
            if (m_AlphaTexture)
                DestroyImmediate(m_AlphaTexture);
            s_Instance = null;
            if (m_IsOSColorPicker)
                OSColorPicker.Close();

            EditorApplication.update -= PollOSColorPicker;

            if (m_ColorLibraryEditorState != null)
                m_ColorLibraryEditorState.TransferEditorPrefsState(false);

            if (m_ColorLibraryEditor != null)
                m_ColorLibraryEditor.UnloadUsedLibraries();

            GUIUtility.keyboardControl = originalKeyboardControl;
            originalKeyboardControl = 0;
        }
    }

    internal class EyeDropper : GUIView
    {
        const int kPixelSize = 10;
        const int kDummyWindowSize = 8192;
        internal static Color s_LastPickedColor;
        GUIView m_DelegateView;
        Texture2D m_Preview;
        static EyeDropper s_Instance;
        private static Vector2 s_PickCoordinates = Vector2.zero;
        private bool m_Focused = false;
        private Action<Color> m_ColorPickedCallback;

        public static void Start(GUIView viewToUpdate, bool stealFocus = true)
        {
            Start(viewToUpdate, null, stealFocus);
        }

        public static void Start(Action<Color> colorPickedCallback, bool stealFocus = true)
        {
            Start(null, colorPickedCallback, stealFocus);
        }

        static void Start(GUIView viewToUpdate, Action<Color> colorPickedCallback, bool stealFocus)
        {
            instance.m_DelegateView = viewToUpdate;
            instance.m_ColorPickedCallback = colorPickedCallback;
            ContainerWindow win = CreateInstance<ContainerWindow>();
            win.m_DontSaveToLayout = true;
            win.title = "EyeDropper";
            win.hideFlags = HideFlags.DontSave;
            win.rootView = instance;
            win.Show(ShowMode.PopupMenu, true, false);
            instance.AddToAuxWindowList();
            win.SetInvisible();
            instance.SetMinMaxSizes(new Vector2(0, 0), new Vector2(kDummyWindowSize, kDummyWindowSize));
            win.position = new Rect(-kDummyWindowSize / 2, -kDummyWindowSize / 2, kDummyWindowSize, kDummyWindowSize);
            instance.wantsMouseMove = true;
            instance.StealMouseCapture();
            if (stealFocus)
                instance.Focus();
        }

        public static void End()
        {
            if (s_Instance != null)
                s_Instance.window.Close();
        }

        static EyeDropper instance
        {
            get
            {
                if (!s_Instance)
                    CreateInstance<EyeDropper>();
                return s_Instance;
            }
        }

        EyeDropper()
        {
            s_Instance = this;
        }

        public static Color GetPickedColor()
        {
            return InternalEditorUtility.ReadScreenPixel(s_PickCoordinates, 1, 1)[0];
        }

        public static Color GetLastPickedColor()
        {
            return s_LastPickedColor;
        }

        static class Styles
        {
            public static readonly GUIStyle eyeDropperHorizontalLine = "EyeDropperHorizontalLine";
            public static readonly GUIStyle eyeDropperVerticalLine = "EyeDropperVerticalLine";
            public static readonly GUIStyle eyeDropperPickedPixel = "EyeDropperPickedPixel";
        }

        public static void DrawPreview(Rect position)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            Texture2D preview = instance.m_Preview;
            int width = (int)Mathf.Ceil(position.width / kPixelSize);
            int height = (int)Mathf.Ceil(position.height / kPixelSize);
            if (preview == null)
            {
                instance.m_Preview = preview = ColorPicker.MakeTexture(width, height);
                preview.filterMode = FilterMode.Point;
            }
            if (preview.width != width || preview.height != height)
            {
                preview.Resize(width, height);
            }

            Vector2 p = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
            Vector2 mPos = p - new Vector2((width / 2), (height / 2));
            preview.SetPixels(InternalEditorUtility.ReadScreenPixel(mPos, width, height), 0);
            preview.Apply(true);

            Graphics.DrawTexture(position, preview);

            // Draw grid on top
            float xStep = position.width / width;
            GUIStyle sep = Styles.eyeDropperVerticalLine;
            for (float x = position.x; x < position.xMax; x += xStep)
            {
                Rect r = new Rect(Mathf.Round(x), position.y, xStep, position.height);
                sep.Draw(r, false, false, false, false);
            }

            float yStep = position.height / height;
            sep = Styles.eyeDropperHorizontalLine;
            for (float y = position.y; y < position.yMax; y += yStep)
            {
                Rect r = new Rect(position.x, Mathf.Floor(y), position.width, yStep);
                sep.Draw(r, false, false, false, false);
            }

            // Draw selected pixelSize
            Rect newR = new Rect((p.x - mPos.x) * xStep + position.x, (p.y - mPos.y) * yStep + position.y, xStep, yStep);
            Styles.eyeDropperPickedPixel.Draw(newR, false, false, false, false);
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
                    SendEvent(EventCommandNames.EyeDropperUpdate, true, false);
                    break;
                case EventType.MouseDown:
                    if (Event.current.button == 0)
                    {
                        s_PickCoordinates = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
                        // We have to close helper window before we read color from screen. On Win
                        // the window covers whole desktop (see Show()) and is black with 0x01 alpha value.
                        // That might cause invalid picked color.
                        window.Close();
                        s_LastPickedColor = GetPickedColor();
                        Event.current.Use();
                        SendEvent(EventCommandNames.EyeDropperClicked, true);
                        if (m_ColorPickedCallback != null)
                        {
                            m_ColorPickedCallback(s_LastPickedColor);
                        }
                    }
                    break;
                case EventType.KeyDown:
                    if (Event.current.keyCode == KeyCode.Escape)
                    {
                        window.Close();
                        Event.current.Use();
                        SendEvent(EventCommandNames.EyeDropperCancelled, true);
                    }
                    break;
            }
        }

        void SendEvent(string eventName, bool exitGUI, bool focusOther = true)
        {
            if (m_DelegateView != null)
            {
                var e = EditorGUIUtility.CommandEvent(eventName);
                if (focusOther)
                    m_DelegateView.Focus();
                m_DelegateView.SendEvent(e);
                if (exitGUI)
                    GUIUtility.ExitGUI();
            }
        }

        public new void OnDestroy()
        {
            if (m_Preview)
                DestroyImmediate(m_Preview);

            if (!m_Focused)
            {
                // Window closed before it was focused
                SendEvent(EventCommandNames.EyeDropperCancelled, false);
            }

            base.OnDestroy();
        }

        protected override bool OnFocus()
        {
            m_Focused = true;
            return base.OnFocus();
        }
    }
}
