// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using UnityEditorInternal;

namespace UnityEditor
{
    internal class GradientEditor
    {
        class Styles
        {
            public GUIStyle upSwatch = "Grad Up Swatch";
            public GUIStyle upSwatchOverlay = "Grad Up Swatch Overlay";
            public GUIStyle downSwatch = "Grad Down Swatch";
            public GUIStyle downSwatchOverlay = "Grad Down Swatch Overlay";

            public GUIContent modeText = new GUIContent("Mode");
            public GUIContent alphaText = new GUIContent("Alpha");
            public GUIContent colorText = new GUIContent("Color");
            public GUIContent locationText = new GUIContent("Location");
            public GUIContent percentText = new GUIContent("%");

            static GUIStyle GetStyle(string name)
            {
                GUISkin s = (GUISkin)EditorGUIUtility.LoadRequired("GradientEditor.GUISkin");
                return s.GetStyle(name);
            }
        }
        static Styles s_Styles;
        static Texture2D s_BackgroundTexture;

        public class Swatch
        {
            public float m_Time;
            public Color m_Value;
            public bool m_IsAlpha;

            public Swatch(float time, Color value, bool isAlpha)
            {
                m_Time = time;
                m_Value = value;
                m_IsAlpha = isAlpha;
            }
        }

        const int k_MaxNumKeys = 8;
        List<Swatch> m_RGBSwatches;
        List<Swatch> m_AlphaSwatches;
        GradientMode m_GradientMode;
        [System.NonSerialized]
        Swatch m_SelectedSwatch;
        Gradient m_Gradient;
        int m_NumSteps;
        bool m_HDR;

        // Fixed steps are only used if numSteps > 1
        public void Init(Gradient gradient, int numSteps, bool hdr)
        {
            m_Gradient = gradient;
            m_NumSteps = numSteps;
            m_HDR = hdr;

            BuildArrays();

            if (m_RGBSwatches.Count > 0)
                m_SelectedSwatch = m_RGBSwatches[0];
        }

        public Gradient target
        {
            get { return m_Gradient; }
        }

        float GetTime(float actualTime)
        {
            actualTime = Mathf.Clamp01(actualTime);

            if (m_NumSteps > 1)
            {
                float stepSize = 1.0f / (m_NumSteps - 1);
                int step = Mathf.RoundToInt(actualTime / stepSize);
                return step / (float)(m_NumSteps - 1);
            }

            return actualTime;
        }

        void BuildArrays()
        {
            if (m_Gradient == null)
                return;
            GradientColorKey[] colorKeys = m_Gradient.colorKeys;
            m_RGBSwatches = new List<Swatch>(colorKeys.Length);
            for (int i = 0; i < colorKeys.Length; i++)
            {
                Color color = colorKeys[i].color;
                color.a = 1f;
                m_RGBSwatches.Add(new Swatch(colorKeys[i].time, color, false));
            }

            GradientAlphaKey[] alphaKeys = m_Gradient.alphaKeys;
            m_AlphaSwatches = new List<Swatch>(alphaKeys.Length);
            for (int i = 0; i < alphaKeys.Length; i++)
            {
                float a = alphaKeys[i].alpha;
                m_AlphaSwatches.Add(new Swatch(alphaKeys[i].time, new Color(a, a, a, 1), true));
            }
            m_GradientMode = m_Gradient.mode;
        }

        public static void DrawGradientWithBackground(Rect position, Gradient gradient)
        {
            Texture2D gradientTexture = UnityEditorInternal.GradientPreviewCache.GetGradientPreview(gradient);
            Rect r2 = new Rect(position.x + 1, position.y + 1, position.width - 2, position.height - 2);

            // Background checkers
            Texture2D backgroundTexture = GetBackgroundTexture();
            Rect texCoordsRect = new Rect(0, 0, r2.width / backgroundTexture.width, r2.height / backgroundTexture.height);
            GUI.DrawTextureWithTexCoords(r2, backgroundTexture, texCoordsRect, false);

            // Gradient texture
            if (gradientTexture != null)
                GUI.DrawTexture(r2, gradientTexture, ScaleMode.StretchToFill, true);

            // Frame over texture
            GUI.Label(position, GUIContent.none, EditorStyles.colorPickerBox);

            // HDR label
            float maxColorComponent = GetMaxColorComponent(gradient);
            if (maxColorComponent > 1.0f)
            {
                GUI.Label(new Rect(position.x, position.y, position.width - 3, position.height), "HDR", EditorStyles.centeredGreyMiniLabel);
            }
        }

        public void OnGUI(Rect position)
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            float modeHeight = 24f;
            float swatchHeight = 16f;
            float editSectionHeight = 26f;
            float gradientTextureHeight = position.height - 2 * swatchHeight - editSectionHeight - modeHeight;

            position.height = modeHeight;
            m_GradientMode = (GradientMode)EditorGUI.EnumPopup(position, s_Styles.modeText, m_GradientMode);
            if (m_GradientMode != m_Gradient.mode)
                AssignBack();

            position.y += modeHeight;
            position.height = swatchHeight;

            // Alpha swatches (no idea why they're top, but that's what Adobe & Apple seem to agree on)
            ShowSwatchArray(position, m_AlphaSwatches, true);

            // Gradient texture
            position.y += swatchHeight;
            if (Event.current.type == EventType.Repaint)
            {
                position.height = gradientTextureHeight;
                DrawGradientWithBackground(position, m_Gradient);
            }
            position.y += gradientTextureHeight;
            position.height = swatchHeight;

            // Color swatches (bottom)
            ShowSwatchArray(position, m_RGBSwatches, false);

            if (m_SelectedSwatch != null)
            {
                position.y += swatchHeight;
                position.height = editSectionHeight;
                position.y += 10;

                float locationWidth = 45;
                float locationTextWidth = 60;
                float space = 20;
                float alphaOrColorTextWidth = 50;
                float totalLocationWidth = locationTextWidth + space + locationTextWidth + locationWidth;

                // Alpha or Color field
                Rect rect = position;
                rect.height = 18;
                rect.x += 17;
                rect.width -= totalLocationWidth;
                EditorGUIUtility.labelWidth = alphaOrColorTextWidth;
                if (m_SelectedSwatch.m_IsAlpha)
                {
                    EditorGUIUtility.fieldWidth = 30;
                    EditorGUI.BeginChangeCheck();
                    float sliderValue = EditorGUI.IntSlider(rect, s_Styles.alphaText, (int)(m_SelectedSwatch.m_Value.r * 255), 0, 255) / 255f;
                    if (EditorGUI.EndChangeCheck())
                    {
                        sliderValue = Mathf.Clamp01(sliderValue);
                        m_SelectedSwatch.m_Value.r = m_SelectedSwatch.m_Value.g = m_SelectedSwatch.m_Value.b = sliderValue;
                        AssignBack();
                        HandleUtility.Repaint();
                    }
                }
                else
                {
                    EditorGUI.BeginChangeCheck();
                    m_SelectedSwatch.m_Value = EditorGUI.ColorField(rect, s_Styles.colorText, m_SelectedSwatch.m_Value, true, false, m_HDR, ColorPicker.defaultHDRConfig);
                    if (EditorGUI.EndChangeCheck())
                    {
                        AssignBack();
                        HandleUtility.Repaint();
                    }
                }

                // Location of key
                rect.x += rect.width + space;
                rect.width = locationWidth + locationTextWidth;

                EditorGUIUtility.labelWidth = locationTextWidth;
                string orgFormatString = EditorGUI.kFloatFieldFormatString;
                EditorGUI.kFloatFieldFormatString = "f1";

                EditorGUI.BeginChangeCheck();
                float newLocation = EditorGUI.FloatField(rect, s_Styles.locationText, m_SelectedSwatch.m_Time * 100.0f) / 100.0f;
                if (EditorGUI.EndChangeCheck())
                {
                    m_SelectedSwatch.m_Time = Mathf.Clamp(newLocation, 0f, 1f);
                    AssignBack();
                }

                EditorGUI.kFloatFieldFormatString = orgFormatString;

                rect.x += rect.width;
                rect.width = 20;
                GUI.Label(rect, s_Styles.percentText);
            }
        }

        void ShowSwatchArray(Rect position, List<Swatch> swatches, bool isAlpha)
        {
            int id = GUIUtility.GetControlID(652347689, FocusType.Passive);
            Event evt = Event.current;

            float mouseSwatchTime = GetTime((Event.current.mousePosition.x - position.x) / position.width);
            Vector2 fixedStepMousePosition = new Vector3(position.x + mouseSwatchTime * position.width, Event.current.mousePosition.y);

            switch (evt.GetTypeForControl(id))
            {
                case EventType.Repaint:
                {
                    bool hasSelection = false;
                    foreach (Swatch s in swatches)
                    {
                        if (m_SelectedSwatch == s)
                        {
                            hasSelection = true;
                            continue;
                        }
                        DrawSwatch(position, s, !isAlpha);
                    }
                    // selected swatch drawn last
                    if (hasSelection && m_SelectedSwatch != null)
                        DrawSwatch(position, m_SelectedSwatch, !isAlpha);
                    break;
                }
                case EventType.MouseDown:
                {
                    Rect clickRect = position;

                    // Swatches have some thickness thus we enlarge the clickable area
                    clickRect.xMin -= 10;
                    clickRect.xMax += 10;
                    if (clickRect.Contains(evt.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        evt.Use();

                        // Make sure selected is topmost for the click
                        if (swatches.Contains(m_SelectedSwatch) && !m_SelectedSwatch.m_IsAlpha && CalcSwatchRect(position, m_SelectedSwatch).Contains(evt.mousePosition))
                        {
                            if (evt.clickCount == 2)
                            {
                                GUIUtility.keyboardControl = id;
                                ColorPicker.Show(GUIView.current, m_SelectedSwatch.m_Value, false, m_HDR, ColorPicker.defaultHDRConfig);
                                GUIUtility.ExitGUI();
                            }
                            break;
                        }

                        bool found = false;
                        foreach (Swatch s in swatches)
                        {
                            if (CalcSwatchRect(position, s).Contains(fixedStepMousePosition))
                            {
                                found = true;
                                m_SelectedSwatch = s;
                                break;
                            }
                        }

                        if (!found)
                        {
                            if (swatches.Count < k_MaxNumKeys)
                            {
                                Color currentColor = m_Gradient.Evaluate(mouseSwatchTime);
                                if (isAlpha)
                                    currentColor = new Color(currentColor.a, currentColor.a, currentColor.a, 1f);
                                else
                                    currentColor.a = 1f;
                                m_SelectedSwatch = new Swatch(mouseSwatchTime, currentColor, isAlpha);
                                swatches.Add(m_SelectedSwatch);
                                AssignBack();
                            }
                            else
                            {
                                Debug.LogWarning("Max " + k_MaxNumKeys + " color keys and " + k_MaxNumKeys + " alpha keys are allowed in a gradient.");
                            }
                        }
                    }
                    break;
                }
                case EventType.MouseDrag:

                    if (GUIUtility.hotControl == id && m_SelectedSwatch != null)
                    {
                        evt.Use();

                        // If user drags swatch outside in vertical direction, we'll remove the swatch
                        if ((evt.mousePosition.y + 5 < position.y || evt.mousePosition.y - 5 > position.yMax))
                        {
                            if (swatches.Count > 1)
                            {
                                swatches.Remove(m_SelectedSwatch);
                                AssignBack();
                                break;
                            }
                        }
                        else if (!swatches.Contains(m_SelectedSwatch))
                            swatches.Add(m_SelectedSwatch);

                        m_SelectedSwatch.m_Time = mouseSwatchTime;
                        AssignBack();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();

                        // If the dragged swatch is NOT in the timeline, it means it was dragged outside.
                        // We just forget about it and let GC get it later.
                        if (!swatches.Contains(m_SelectedSwatch))
                            m_SelectedSwatch = null;

                        // Remove duplicate keys on mouse up so that we do not kill any keys during the drag
                        RemoveDuplicateOverlappingSwatches();
                    }
                    break;

                case EventType.KeyDown:
                    if (evt.keyCode == KeyCode.Delete)
                    {
                        if (m_SelectedSwatch != null)
                        {
                            List<Swatch> listToDeleteFrom;
                            if (m_SelectedSwatch.m_IsAlpha)
                                listToDeleteFrom = m_AlphaSwatches;
                            else
                                listToDeleteFrom = m_RGBSwatches;

                            if (listToDeleteFrom.Count > 1)
                            {
                                listToDeleteFrom.Remove(m_SelectedSwatch);
                                AssignBack();
                                HandleUtility.Repaint();
                            }
                        }
                        evt.Use();
                    }
                    break;

                case EventType.ValidateCommand:
                    if (evt.commandName == "Delete")
                        Event.current.Use();
                    break;

                case EventType.ExecuteCommand:
                    if (evt.commandName == "ColorPickerChanged")
                    {
                        GUI.changed = true;
                        m_SelectedSwatch.m_Value =  ColorPicker.color;
                        AssignBack();
                        HandleUtility.Repaint();
                    }
                    else if (evt.commandName == "Delete")
                    {
                        if (swatches.Count > 1)
                        {
                            swatches.Remove(m_SelectedSwatch);
                            AssignBack();
                            HandleUtility.Repaint();
                        }
                    }
                    break;
            }
        }

        void DrawSwatch(Rect totalPos, Swatch s, bool upwards)
        {
            Color temp = GUI.backgroundColor;
            Rect r = CalcSwatchRect(totalPos, s);
            GUI.backgroundColor = s.m_Value;
            GUIStyle back = upwards ? s_Styles.upSwatch : s_Styles.downSwatch;
            GUIStyle overlay = upwards ? s_Styles.upSwatchOverlay : s_Styles.downSwatchOverlay;
            back.Draw(r, false, false, m_SelectedSwatch == s, false);
            GUI.backgroundColor = temp;
            overlay.Draw(r, false, false, m_SelectedSwatch == s, false);
        }

        Rect CalcSwatchRect(Rect totalRect, Swatch s)
        {
            float time = s.m_Time;
            return new Rect(totalRect.x + Mathf.Round(totalRect.width * time) - 5, totalRect.y, 10, totalRect.height);
        }

        int SwatchSort(Swatch lhs, Swatch rhs)
        {
            if (lhs.m_Time == rhs.m_Time && lhs == m_SelectedSwatch)
                return -1;
            if (lhs.m_Time == rhs.m_Time && rhs == m_SelectedSwatch)
                return 1;

            return lhs.m_Time.CompareTo(rhs.m_Time);
        }

        // Assign back all swatches, to target gradient.
        void AssignBack()
        {
            m_RGBSwatches.Sort((a, b) => SwatchSort(a, b));
            GradientColorKey[] colorKeys = new GradientColorKey[m_RGBSwatches.Count];
            for (int i = 0; i < m_RGBSwatches.Count; i++)
            {
                colorKeys[i].color = m_RGBSwatches[i].m_Value;
                colorKeys[i].time = m_RGBSwatches[i].m_Time;
            }

            m_AlphaSwatches.Sort((a, b) => SwatchSort(a, b));
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[m_AlphaSwatches.Count];
            for (int i = 0; i < m_AlphaSwatches.Count; i++)
            {
                alphaKeys[i].alpha = m_AlphaSwatches[i].m_Value.r; // we use the red channel (see BuildArrays)
                alphaKeys[i].time = m_AlphaSwatches[i].m_Time;
            }

            m_Gradient.colorKeys = colorKeys;
            m_Gradient.alphaKeys = alphaKeys;
            m_Gradient.mode = m_GradientMode;

            GUI.changed = true;
        }

        // Kill any swatches that are at the same time (For example as the result of dragging a swatch on top of another)
        void RemoveDuplicateOverlappingSwatches()
        {
            bool didRemoveAny = false;
            for (int i = 1; i < m_RGBSwatches.Count; i++)
            {
                if (Mathf.Approximately(m_RGBSwatches[i - 1].m_Time, m_RGBSwatches[i].m_Time))
                {
                    m_RGBSwatches.RemoveAt(i);
                    i--;
                    didRemoveAny = true;
                }
            }

            for (int i = 1; i < m_AlphaSwatches.Count; i++)
            {
                if (Mathf.Approximately(m_AlphaSwatches[i - 1].m_Time, m_AlphaSwatches[i].m_Time))
                {
                    m_AlphaSwatches.RemoveAt(i);
                    i--;
                    didRemoveAny = true;
                }
            }

            if (didRemoveAny)
                AssignBack();
        }

        public static Texture2D GetBackgroundTexture()
        {
            if (s_BackgroundTexture == null)
                s_BackgroundTexture = GradientEditor.CreateCheckerTexture(32, 4, 4, Color.white, new Color(0.7f, 0.7f, 0.7f));
            return s_BackgroundTexture;
        }

        public static Texture2D CreateCheckerTexture(int numCols, int numRows, int cellPixelWidth, Color col1, Color col2)
        {
            int height = numRows * cellPixelWidth;
            int width = numCols * cellPixelWidth;

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            Color[] pixels = new Color[width * height];

            for (int i = 0; i < numRows; i++)
                for (int j = 0; j < numCols; j++)
                    for (int ci = 0; ci < cellPixelWidth; ci++)
                        for (int cj = 0; cj < cellPixelWidth; cj++)
                            pixels[(i * cellPixelWidth + ci) * width + j * cellPixelWidth + cj] = ((i + j) % 2 == 0) ? col1 : col2;

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        // GUI Helpers
        public static void DrawGradientSwatch(Rect position, Gradient gradient, Color bgColor)
        {
            DrawGradientSwatchInternal(position, gradient, null, bgColor);
        }

        public static void DrawGradientSwatch(Rect position, SerializedProperty property, Color bgColor)
        {
            DrawGradientSwatchInternal(position, null, property, bgColor);
        }

        private static void DrawGradientSwatchInternal(Rect position, Gradient gradient, SerializedProperty property, Color bgColor)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            if (EditorGUI.showMixedValue)
            {
                Color oldColor = GUI.color;
                float a = GUI.enabled ? 1 : 2;

                GUI.color = new Color(0.82f, 0.82f, 0.82f, a) * bgColor;
                GUIStyle mgs = EditorGUIUtility.whiteTextureStyle;
                mgs.Draw(position, false, false, false, false);

                EditorGUI.BeginHandleMixedValueContentColor();
                mgs.Draw(position, EditorGUI.mixedValueContent, false, false, false, false);
                EditorGUI.EndHandleMixedValueContentColor();

                GUI.color = oldColor;
                return;
            }

            // Draw Background
            Texture2D backgroundTexture = GradientEditor.GetBackgroundTexture();
            if (backgroundTexture != null)
            {
                Color oldColor = GUI.color;
                GUI.color = bgColor;

                GUIStyle backgroundStyle = EditorGUIUtility.GetBasicTextureStyle(backgroundTexture);
                backgroundStyle.Draw(position, false, false, false, false);

                GUI.color = oldColor;
            }

            // DrawTexture
            Texture2D preview = null;
            float maxColorComponent;
            if (property != null)
            {
                preview = GradientPreviewCache.GetPropertyPreview(property);
                maxColorComponent = GetMaxColorComponent(property.gradientValue);
            }
            else
            {
                preview = GradientPreviewCache.GetGradientPreview(gradient);
                maxColorComponent = GetMaxColorComponent(gradient);
            }

            if (preview == null)
            {
                Debug.Log("Warning: Could not create preview for gradient");
                return;
            }

            GUIStyle gs = EditorGUIUtility.GetBasicTextureStyle(preview);
            gs.Draw(position, false, false, false, false);

            // HDR label
            if (maxColorComponent > 1.0f)
            {
                GUI.Label(new Rect(position.x, position.y - 1, position.width - 3, position.height + 2), "HDR", EditorStyles.centeredGreyMiniLabel);
            }
        }

        private static float GetMaxColorComponent(Gradient gradient)
        {
            float maxColorComponent = 0.0f;
            GradientColorKey[] colorKeys = gradient.colorKeys;
            for (int i = 0; i < colorKeys.Length; i++)
            {
                maxColorComponent = Mathf.Max(maxColorComponent, colorKeys[i].color.maxColorComponent);
            }
            return maxColorComponent;
        }
    }
} // namespace
