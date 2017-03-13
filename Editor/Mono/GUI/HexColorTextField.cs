// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine;
using System;

namespace UnityEditor
{
    public sealed partial class EditorGUILayout
    {
        internal static Color HexColorTextField(GUIContent label, Color color, bool showAlpha, params GUILayoutOption[] options)
        {
            return HexColorTextField(label, color, showAlpha, EditorStyles.textField, options);
        }

        internal static Color HexColorTextField(GUIContent label, Color color, bool showAlpha, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
            return EditorGUI.HexColorTextField(r, label, color, showAlpha, style);
        }
    }

    public sealed partial class EditorGUI
    {
        static readonly GUIContent s_HDRWarning = new GUIContent(string.Empty, EditorGUIUtility.warningIcon, LocalizationDatabase.GetLocalizedString("For HDR colors the normalized LDR hex color value is shown"));

        internal static Color HexColorTextField(Rect rect, GUIContent label, Color color, bool showAlpha)
        {
            return HexColorTextField(rect, label, color, showAlpha, EditorStyles.textField);
        }

        internal static Color HexColorTextField(Rect rect, GUIContent label, Color color, bool showAlpha, GUIStyle style)
        {
            int id = GUIUtility.GetControlID(s_FloatFieldHash, FocusType.Keyboard, rect);
            Rect controlRect = PrefixLabel(rect, id, label);

            return DoHexColorTextField(controlRect, color, showAlpha, style);
        }

        internal static Color DoHexColorTextField(Rect rect, Color color, bool showAlpha, GUIStyle style)
        {
            const string kValidHexChars = "0123456789ABCDEFabcdef";
            const float prefixCharWidth = 14;

            bool showWarning = false;
            if (color.maxColorComponent > 1f)
            {
                // If color is HDR then we normalize it to be able to show it as LDR
                color = color.RGBMultiplied(1f / color.maxColorComponent);
                showWarning = true;
            }

            // Prefix
            Rect prefixRect = new Rect(rect.x, rect.y, prefixCharWidth, rect.height);
            rect.xMin += prefixCharWidth;
            GUI.Label(prefixRect, GUIContent.Temp("#"));

            // Hex field
            string hex = showAlpha ? ColorUtility.ToHtmlStringRGBA(color) : ColorUtility.ToHtmlStringRGB(color);
            BeginChangeCheck();

            int id = GUIUtility.GetControlID(s_TextFieldHash, FocusType.Keyboard, rect);
            bool dummy;
            string newHex = DoTextField(s_RecycledEditor, id, rect, hex, style, kValidHexChars, out dummy, false, false, false);

            if (EndChangeCheck())
            {
                s_RecycledEditor.text = s_RecycledEditor.text.ToUpper();

                Color c;
                if (ColorUtility.TryParseHtmlString("#" + newHex, out c))
                    color = new Color(c.r, c.g, c.b, showAlpha ? c.a : color.a);
            }

            // Warning icon
            if (showWarning)
            {
                EditorGUIUtility.SetIconSize(new Vector2(16, 16));
                GUI.Label(new Rect(prefixRect.x - 20, rect.y, 20, 20), s_HDRWarning);
                EditorGUIUtility.SetIconSize(Vector2.zero);
            }

            return color;
        }
    }
} // namespace
