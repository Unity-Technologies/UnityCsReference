// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    public sealed partial class EditorGUILayout
    {
        internal static Color32 HexColorTextField(GUIContent label, Color32 color, bool showAlpha, params GUILayoutOption[] options)
        {
            return HexColorTextField(label, color, showAlpha, EditorStyles.textField, options);
        }

        internal static Color32 HexColorTextField(GUIContent label, Color32 color, bool showAlpha, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
            return EditorGUI.HexColorTextField(r, label, color, showAlpha, style);
        }
    }

    public sealed partial class EditorGUI
    {
        internal static Color32 HexColorTextField(Rect rect, GUIContent label, Color32 color, bool showAlpha)
        {
            return HexColorTextField(rect, label, color, showAlpha, EditorStyles.textField);
        }

        internal static Color32 HexColorTextField(Rect rect, GUIContent label, Color32 color, bool showAlpha, GUIStyle style)
        {
            var id = GUIUtility.GetControlID(s_TextFieldHash, FocusType.Keyboard, rect);
            return DoHexColorTextField(id, PrefixLabel(rect, id, label), color, showAlpha, style);
        }

        internal static Color32 DoHexColorTextField(int id, Rect rect, Color32 color, bool showAlpha, GUIStyle style)
        {
            const string kValidHexChars = "0123456789ABCDEFabcdef";

            // Hex field
            string hex = showAlpha ? ColorUtility.ToHtmlStringRGBA(color) : ColorUtility.ToHtmlStringRGB(color);
            BeginChangeCheck();

            bool dummy;
            string newHex = DoTextField(s_RecycledEditor, id, rect, hex, style, kValidHexChars, out dummy, false, false, false);

            if (EndChangeCheck())
            {
                s_RecycledEditor.text = s_RecycledEditor.text.ToUpper();

                Color newColor;
                if (ColorUtility.TryParseHtmlString("#" + newHex, out newColor))
                    color = new Color(newColor.r, newColor.g, newColor.b, showAlpha ? newColor.a : color.a);
            }

            return color;
        }
    }
} // namespace
