// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    public sealed partial class EditorGUILayout
    {
        internal static Color ColorBrightnessField(GUIContent label, Color value, float minBrightness, float maxBrightness, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
            return EditorGUI.ColorBrightnessField(r, label, value, minBrightness, maxBrightness);
        }
    }

    public sealed partial class EditorGUI
    {
        internal static Color ColorBrightnessField(Rect r, GUIContent label, Color value, float minBrightness, float maxBrightness)
        {
            return ColorBrightnessFieldInternal(r, label, value, minBrightness, maxBrightness, EditorStyles.numberField);
        }

        internal static Color ColorBrightnessFieldInternal(Rect position, GUIContent label, Color value, float minBrightness, float maxBrightness, GUIStyle style)
        {
            int id = GUIUtility.GetControlID(s_FloatFieldHash, FocusType.Keyboard, position);
            Rect position2 = PrefixLabel(position, id, label);
            position.xMax = position2.x;
            return DoColorBrightnessField(position2, position, value, minBrightness, maxBrightness, style);
        }

        // This method handles changing the brightness of a color without destroying the color state while dragging:
        // You can drag to black and drag up again while preserving hue and saturation.
        internal static Color DoColorBrightnessField(Rect rect, Rect dragRect, Color col, float minBrightness, float maxBrightness, GUIStyle style)
        {
            float alpha = col.a;
            int dragID = GUIUtility.GetControlID(18975602, FocusType.Keyboard);


            Event evt = Event.current;
            switch (evt.GetTypeForControl(dragID))
            {
                case EventType.Repaint:
                    if (GUIUtility.hotControl == 0)
                        EditorGUIUtility.AddCursorRect(dragRect, MouseCursor.SlideArrow);
                    break;

                case EventType.MouseDown:
                    if (evt.button == 0)
                    {
                        if (dragRect.Contains(Event.current.mousePosition))
                        {
                            if (GUIUtility.hotControl == 0)
                            {
                                // Init state object
                                var stateObject = GUIUtility.GetStateObject(typeof(ColorBrightnessFieldStateObject), dragID) as ColorBrightnessFieldStateObject;
                                if (stateObject != null)
                                {
                                    Color.RGBToHSV(col, out stateObject.m_Hue, out stateObject.m_Saturation, out stateObject.m_Brightness);
                                }

                                GUIUtility.keyboardControl = 0;
                                GUIUtility.hotControl = dragID;
                                GUI.changed = true;
                                evt.Use();
                                EditorGUIUtility.SetWantsMouseJumping(1);
                            }
                        }
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == dragID)
                    {
                        var stateObject = GUIUtility.QueryStateObject(typeof(ColorBrightnessFieldStateObject), dragID) as ColorBrightnessFieldStateObject;
                        float maxColorComponent = col.maxColorComponent;
                        const float minStep = 0.004f; // 1/255f rounded (so we can adjust brightness by 1/255 steps)
                        float dragSensitity = Mathf.Clamp01(Mathf.Max(1f, Mathf.Pow(Mathf.Abs(maxColorComponent), 0.5f)) * minStep);
                        float deltaValue = HandleUtility.niceMouseDelta * dragSensitity;
                        float brightness = Mathf.Clamp(stateObject.m_Brightness + deltaValue, minBrightness, maxBrightness);
                        stateObject.m_Brightness = (float)System.Math.Round(brightness, 3); // We do not need more than 3 decimals for color channel values: 1/255 = 0.004
                        Color c = Color.HSVToRGB(stateObject.m_Hue, stateObject.m_Saturation, stateObject.m_Brightness, maxBrightness > 1f);
                        col = new Color(c.r, c.g, c.b, col.a);

                        GUIUtility.keyboardControl = 0; // remove keyboard focus when dragging
                        GUI.changed = true;
                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == dragID)
                    {
                        // Reset dragging
                        GUIUtility.hotControl = 0;
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }
                    break;
            }

            BeginChangeCheck();
            // Show input text field: Use delayed float field to prevent destroying color when entering '0' (this would set color to black while entering values)
            float newValue = DelayedFloatField(rect, col.maxColorComponent, style);
            if (EndChangeCheck())
            {
                float hue, saturation, dummy;
                Color.RGBToHSV(col, out hue, out saturation, out dummy);
                float brightness = Mathf.Clamp(newValue, minBrightness, maxBrightness);
                Color c = Color.HSVToRGB(hue, saturation, brightness, maxBrightness > 1f);
                col = new Color(c.r, c.g, c.b, col.a);
            }

            return new Color(col.r, col.g, col.b, alpha);
        }

        class ColorBrightnessFieldStateObject
        {
            public float m_Hue;         // Hue when started dragging
            public float m_Saturation;  // Saturation when started dragging
            public float m_Brightness;  // Brightness adjusted while dragging
        }
    }
} // namespace
