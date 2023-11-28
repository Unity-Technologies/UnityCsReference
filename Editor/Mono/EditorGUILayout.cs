// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Internal;
using Object = UnityEngine.Object;

namespace UnityEditor;

// Auto-layouted version of [[EditorGUI]]
sealed partial class EditorGUILayout
{
    // @TODO: Make private (and rename to not claim it's a constant). Shouldn't really be used outside of EditorGUI.
    // Places that use this directly should likely use GetControlRect instead.
    internal static float kLabelFloatMinW => EditorGUIUtility.labelWidth + EditorGUIUtility.fieldWidth + EditorGUI.kSpacing;

    internal static float kLabelFloatMaxW => EditorGUIUtility.labelWidth + EditorGUIUtility.fieldWidth + EditorGUI.kSpacing;

    internal static Rect s_LastRect;

    internal const float kPlatformTabWidth = 30;

    internal static SavedBool s_SelectedDefault = new SavedBool("Platform.ShownDefaultTab", true);

    static GUIStyle s_TabOnlyOne;
    static GUIStyle s_TabFirst;
    static GUIStyle s_TabMiddle;
    static GUIStyle s_TabLast;

    [ExcludeFromDocs]
    public static bool Foldout(bool foldout, string content)
    {
        return Foldout(foldout, content, EditorStyles.foldout);
    }

    public static bool Foldout(bool foldout, string content, [DefaultValue("EditorStyles.foldout")] GUIStyle style)
    {
        return Foldout(foldout, EditorGUIUtility.TempContent(content), false, style);
    }

    [ExcludeFromDocs]
    public static bool Foldout(bool foldout, GUIContent content)
    {
        return Foldout(foldout, content, EditorStyles.foldout);
    }

    public static bool Foldout(bool foldout, GUIContent content, [DefaultValue("EditorStyles.foldout")] GUIStyle style)
    {
        return Foldout(foldout, content, false, style);
    }

    [ExcludeFromDocs]
    public static bool Foldout(bool foldout, string content, bool toggleOnLabelClick)
    {
        return Foldout(foldout, content, toggleOnLabelClick, EditorStyles.foldout);
    }

    public static bool Foldout(bool foldout, string content, bool toggleOnLabelClick, [DefaultValue("EditorStyles.foldout")] GUIStyle style)
    {
        return Foldout(foldout, EditorGUIUtility.TempContent(content), toggleOnLabelClick, style);
    }

    [ExcludeFromDocs]
    public static bool Foldout(bool foldout, GUIContent content, bool toggleOnLabelClick)
    {
        return Foldout(foldout, content, toggleOnLabelClick, EditorStyles.foldout);
    }

    public static bool Foldout(bool foldout, GUIContent content, bool toggleOnLabelClick, [DefaultValue("EditorStyles.foldout")] GUIStyle style)
    {
        return FoldoutInternal(foldout, content, toggleOnLabelClick, style);
    }

    [ExcludeFromDocs]
    public static void PrefixLabel(string label)
    {
        GUIStyle followingStyle = "Button";
        PrefixLabel(label, followingStyle);
    }

    public static void PrefixLabel(string label, [DefaultValue("\"Button\"")] GUIStyle followingStyle)
    {
        PrefixLabel(EditorGUIUtility.TempContent(label), followingStyle, EditorStyles.label);
    }

    public static void PrefixLabel(string label, GUIStyle followingStyle, GUIStyle labelStyle)
    {
        PrefixLabel(EditorGUIUtility.TempContent(label), followingStyle, labelStyle);
    }

    [ExcludeFromDocs]
    public static void PrefixLabel(GUIContent label)
    {
        GUIStyle followingStyle = "Button";
        PrefixLabel(label, followingStyle);
    }

    public static void PrefixLabel(GUIContent label, [DefaultValue("\"Button\"")] GUIStyle followingStyle)
    {
        PrefixLabel(label, followingStyle, EditorStyles.label);
    }

    // Make a label in front of some control.
    public static void PrefixLabel(GUIContent label, GUIStyle followingStyle, GUIStyle labelStyle)
    {
        PrefixLabelInternal(label, followingStyle, labelStyle);
    }

    public static void LabelField(string label, params GUILayoutOption[] options)
    {
        LabelField(GUIContent.none, EditorGUIUtility.TempContent(label), EditorStyles.label, options);
    }

    public static void LabelField(string label, GUIStyle style, params GUILayoutOption[] options)
    {
        LabelField(GUIContent.none, EditorGUIUtility.TempContent(label), style, options);
    }

    public static void LabelField(GUIContent label, params GUILayoutOption[] options)
    {
        LabelField(GUIContent.none, label, EditorStyles.label, options);
    }

    public static void LabelField(GUIContent label, GUIStyle style, params GUILayoutOption[] options)
    {
        LabelField(GUIContent.none, label, style, options);
    }

    public static void LabelField(string label, string label2, params GUILayoutOption[] options)
    {
        LabelField(new GUIContent(label), EditorGUIUtility.TempContent(label2), EditorStyles.label, options);
    }

    public static void LabelField(string label, string label2, GUIStyle style, params GUILayoutOption[] options)
    {
        LabelField(new GUIContent(label), EditorGUIUtility.TempContent(label2), style, options);
    }

    public static void LabelField(GUIContent label, GUIContent label2, params GUILayoutOption[] options)
    {
        LabelField(label, label2, EditorStyles.label, options);
    }

    // Make a label field. (Useful for showing read-only info.)
    public static void LabelField(GUIContent label, GUIContent label2, GUIStyle style, params GUILayoutOption[] options)
    {
        if (!style.wordWrap)
        {
            // If we don't need word wrapping, just allocate the standard space to avoid corner case layout issues
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, options);
            EditorGUI.LabelField(r, label, label2, style);
        }
        else
        {
            BeginHorizontal();
            PrefixLabel(label, style);
            Rect r = GUILayoutUtility.GetRect(label2, style, options);
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.LabelField(r, label2, style);
            EditorGUI.indentLevel = oldIndent;
            EndHorizontal();
        }
    }

    public static bool LinkButton(string label, params GUILayoutOption[] options)
    {
        return LinkButton(EditorGUIUtility.TempContent(label), options);
    }

    public static bool LinkButton(GUIContent label, params GUILayoutOption[] options)
    {
        var position = s_LastRect = GUILayoutUtility.GetRect(label, EditorStyles.linkLabel, options);

        Handles.color = EditorStyles.linkLabel.normal.textColor;
        Handles.DrawLine(new Vector3(position.xMin + EditorStyles.linkLabel.padding.left, position.yMax), new Vector3(position.xMax - EditorStyles.linkLabel.padding.right, position.yMax));
        Handles.color = Color.white;

        EditorGUIUtility.AddCursorRect(position, MouseCursor.Link);

        return GUI.Button(position, label, EditorStyles.linkLabel);
    }

    public static bool Toggle(bool value, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetToggleRect(false, options);
        return EditorGUI.Toggle(r, value);
    }

    public static bool Toggle(string label, bool value, params GUILayoutOption[] options)
    {
        return Toggle(EditorGUIUtility.TempContent(label), value, options);
    }

    public static bool Toggle(GUIContent label, bool value, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetToggleRect(true, options);
        return EditorGUI.Toggle(r, label, value);
    }

    public static bool Toggle(bool value, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetToggleRect(false, options);
        return EditorGUI.Toggle(r, value, style);
    }

    public static bool Toggle(string label, bool value, GUIStyle style, params GUILayoutOption[] options)
    {
        return Toggle(EditorGUIUtility.TempContent(label), value, style, options);
    }

    // Make a toggle.
    public static bool Toggle(GUIContent label, bool value, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetToggleRect(true, options);
        return EditorGUI.Toggle(r, label, value, style);
    }

    public static bool ToggleLeft(string label, bool value, params GUILayoutOption[] options)
    {
        return ToggleLeft(EditorGUIUtility.TempContent(label), value, options);
    }

    public static bool ToggleLeft(GUIContent label, bool value, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, options);
        return EditorGUI.ToggleLeft(r, label, value);
    }

    public static bool ToggleLeft(string label, bool value, GUIStyle labelStyle, params GUILayoutOption[] options)
    {
        return ToggleLeft(EditorGUIUtility.TempContent(label), value, labelStyle, options);
    }

    // Make a toggle with the label on the right.
    public static bool ToggleLeft(GUIContent label, bool value, GUIStyle labelStyle, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, options);
        return EditorGUI.ToggleLeft(r, label, value, labelStyle);
    }

    public static string TextField(string text, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, EditorStyles.textField, options);
        return EditorGUI.TextField(r, text);
    }

    public static string TextField(string text, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.TextField(r, text, style);
    }

    public static string TextField(string label, string text, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.textField, options);
        return EditorGUI.TextField(r, label, text);
    }

    public static string TextField(string label, string text, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.TextField(r, label, text, style);
    }

    public static string TextField(GUIContent label, string text, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.textField, options);
        return EditorGUI.TextField(r, label, text);
    }

    // Make a text field.
    public static string TextField(GUIContent label, string text, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.TextField(r, label, text, style);
    }

    public static string DelayedTextField(string text, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, EditorStyles.textField, options);
        return EditorGUI.DelayedTextField(r, text);
    }

    public static string DelayedTextField(string text, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.DelayedTextField(r, text, style);
    }

    public static string DelayedTextField(string label, string text, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.textField, options);
        return EditorGUI.DelayedTextField(r, label, text);
    }

    public static string DelayedTextField(string label, string text, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.DelayedTextField(r, label, text, style);
    }

    public static string DelayedTextField(GUIContent label, string text, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.textField, options);
        return EditorGUI.DelayedTextField(r, label, text);
    }

    // Make a delayed text field.
    public static string DelayedTextField(GUIContent label, string text, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.DelayedTextField(r, label, text, style);
    }

    public static void DelayedTextField(SerializedProperty property, params GUILayoutOption[] options)
    {
        DelayedTextField(property, null, options);
    }

    // Make a delayed text field.
    internal static void DelayedTextField(SerializedProperty property, GUIContent label, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(EditorGUI.LabelHasContent(label), EditorGUI.kSingleLineHeight, EditorStyles.textField, options);
        EditorGUI.DelayedTextFieldHelper(r, property, label, style);
    }

    public static void DelayedTextField(SerializedProperty property, GUIContent label, params GUILayoutOption[] options)
    {
        DelayedTextField(property, label, EditorStyles.textField, options);
    }

    internal static string ToolbarSearchField(string text, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GUILayoutUtility.GetRect(0, kLabelFloatMaxW * 1.5f, EditorGUI.kSingleLineHeight, EditorGUI.kSingleLineHeight, EditorStyles.toolbarSearchField, options);
        int i = 0;
        return EditorGUI.ToolbarSearchField(r, null, ref i, text);
    }

    internal static string ToolbarSearchField(string text, string[] searchModes, ref int searchMode, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GUILayoutUtility.GetRect(0, kLabelFloatMaxW * 1.5f, EditorGUI.kSingleLineHeight, EditorGUI.kSingleLineHeight, EditorStyles.toolbarSearchField, options);
        return EditorGUI.ToolbarSearchField(r, searchModes, ref searchMode, text);
    }

    public static string TextArea(string text, params GUILayoutOption[] options)
    { return TextArea(text, EditorStyles.textField, options); }
    // Make a text area.
    public static string TextArea(string text, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GUILayoutUtility.GetRect(EditorGUIUtility.TempContent(text), style, options);
        return EditorGUI.TextArea(r, text, style);
    }

    public static void SelectableLabel(string text, params GUILayoutOption[] options)
    {
        SelectableLabel(text, EditorStyles.label, options);
    }

    // Make a selectable label field. (Useful for showing read-only info that can be copy-pasted.)
    public static void SelectableLabel(string text, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight * 2, style, options);
        EditorGUI.SelectableLabel(r, text, style);
    }

    internal static Event KeyEventField(Event e, params GUILayoutOption[] options)
    {
        Rect r = GUILayoutUtility.GetRect(EditorGUI.s_PleasePressAKey, GUI.skin.textField, options);
        return EditorGUI.KeyEventField(r, e);
    }

    public static string PasswordField(string password, params GUILayoutOption[] options)
    {
        return PasswordField(password, EditorStyles.textField, options);
    }

    public static string PasswordField(string password, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.PasswordField(r, password, style);
    }

    public static string PasswordField(string label, string password, params GUILayoutOption[] options)
    {
        return PasswordField(EditorGUIUtility.TempContent(label), password, EditorStyles.textField, options);
    }

    public static string PasswordField(string label, string password, GUIStyle style, params GUILayoutOption[] options)
    {
        return PasswordField(EditorGUIUtility.TempContent(label), password, style, options);
    }

    public static string PasswordField(GUIContent label, string password, params GUILayoutOption[] options)
    {
        return PasswordField(label, password, EditorStyles.textField, options);
    }

    // Make a text field where the user can enter a password.
    public static string PasswordField(GUIContent label, string password, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.PasswordField(r, label, password, style);
    }

    // Peak smoothing should be handled by client. Input: value and peak is normalized values (0 - 1).
    internal static void VUMeterHorizontal(float value, float peak, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
        EditorGUI.VUMeter.HorizontalMeter(r, value, peak, EditorGUI.VUMeter.horizontalVUTexture, Color.grey);
    }

    // Auto-smoothing of peak
    internal static void VUMeterHorizontal(float value, ref EditorGUI.VUMeter.SmoothingData data, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
        EditorGUI.VUMeter.HorizontalMeter(r, value, ref data, EditorGUI.VUMeter.horizontalVUTexture, Color.grey);
    }

    public static float FloatField(float value, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
        return EditorGUI.FloatField(r, value);
    }

    public static float FloatField(float value, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.FloatField(r, value, style);
    }

    public static float FloatField(string label, float value, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
        return EditorGUI.FloatField(r, label, value);
    }

    public static float FloatField(string label, float value, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.FloatField(r, label, value, style);
    }

    public static float FloatField(GUIContent label, float value, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
        return EditorGUI.FloatField(r, label, value);
    }

    // Make a text field for entering float values.
    public static float FloatField(GUIContent label, float value, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.FloatField(r, label, value, style);
    }

    public static float DelayedFloatField(float value, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
        return EditorGUI.DelayedFloatField(r, value);
    }

    public static float DelayedFloatField(float value, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.DelayedFloatField(r, value, style);
    }

    public static float DelayedFloatField(string label, float value, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
        return EditorGUI.DelayedFloatField(r, label, value);
    }

    public static float DelayedFloatField(string label, float value, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.DelayedFloatField(r, label, value, style);
    }

    public static float DelayedFloatField(GUIContent label, float value, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
        return EditorGUI.DelayedFloatField(r, label, value);
    }

    // Make a delayed text field for entering float values.
    public static float DelayedFloatField(GUIContent label, float value, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.DelayedFloatField(r, label, value, style);
    }

    public static void DelayedFloatField(SerializedProperty property, params GUILayoutOption[] options)
    {
        DelayedFloatField(property, null, options);
    }

    // Make a delayed text field for entering float values.
    public static void DelayedFloatField(SerializedProperty property, GUIContent label, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(EditorGUI.LabelHasContent(label), EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
        EditorGUI.DelayedFloatField(r, property, label);
    }

    public static double DoubleField(double value, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
        return EditorGUI.DoubleField(r, value);
    }

    public static double DoubleField(double value, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.DoubleField(r, value, style);
    }

    public static double DoubleField(string label, double value, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
        return EditorGUI.DoubleField(r, label, value);
    }

    public static double DoubleField(string label, double value, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.DoubleField(r, label, value, style);
    }

    public static double DoubleField(GUIContent label, double value, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
        return EditorGUI.DoubleField(r, label, value);
    }

    // Make a text field for entering double values.
    public static double DoubleField(GUIContent label, double value, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.DoubleField(r, label, value, style);
    }

    public static double DelayedDoubleField(double value, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
        return EditorGUI.DelayedDoubleField(r, value);
    }

    public static double DelayedDoubleField(double value, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.DelayedDoubleField(r, value, style);
    }

    public static double DelayedDoubleField(string label, double value, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
        return EditorGUI.DelayedDoubleField(r, label, value);
    }

    public static double DelayedDoubleField(string label, double value, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.DelayedDoubleField(r, label, value, style);
    }

    public static double DelayedDoubleField(GUIContent label, double value, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
        return EditorGUI.DelayedDoubleField(r, label, value);
    }

    // Make a delayed text field for entering double values.
    public static double DelayedDoubleField(GUIContent label, double value, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.DelayedDoubleField(r, label, value, style);
    }

    public static int IntField(int value, params GUILayoutOption[] options)
    {
        return IntField(value, EditorStyles.numberField, options);
    }

    public static int IntField(int value, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.IntField(r, value, style);
    }

    public static int IntField(string label, int value, params GUILayoutOption[] options)
    {
        return IntField(label, value, EditorStyles.numberField, options);
    }

    public static int IntField(string label, int value, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.IntField(r, label, value, style);
    }

    public static int IntField(GUIContent label, int value, params GUILayoutOption[] options)
    {
        return IntField(label, value, EditorStyles.numberField, options);
    }

    // Make a text field for entering integers.
    public static int IntField(GUIContent label, int value, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.IntField(r, label, value, style);
    }

    public static int DelayedIntField(int value, params GUILayoutOption[] options)
    {
        return DelayedIntField(value, EditorStyles.numberField, options);
    }

    public static int DelayedIntField(int value, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.DelayedIntField(r, value, style);
    }

    public static int DelayedIntField(string label, int value, params GUILayoutOption[] options)
    {
        return DelayedIntField(label, value, EditorStyles.numberField, options);
    }

    public static int DelayedIntField(string label, int value, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.DelayedIntField(r, label, value, style);
    }

    public static int DelayedIntField(GUIContent label, int value, params GUILayoutOption[] options)
    {
        return DelayedIntField(label, value, EditorStyles.numberField, options);
    }

    // Make a text field for entering integers.
    public static int DelayedIntField(GUIContent label, int value, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.DelayedIntField(r, label, value, style);
    }

    public static void DelayedIntField(SerializedProperty property, params GUILayoutOption[] options)
    {
        DelayedIntField(property, null, options);
    }

    // Make a text field for entering integers.
    public static void DelayedIntField(SerializedProperty property, GUIContent label, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(EditorGUI.LabelHasContent(label), EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
        EditorGUI.DelayedIntField(r, property, label);
    }

    public static long LongField(long value, params GUILayoutOption[] options)
    {
        return LongField(value, EditorStyles.numberField, options);
    }

    public static long LongField(long value, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.LongField(r, value, style);
    }

    public static long LongField(string label, long value, params GUILayoutOption[] options)
    {
        return LongField(label, value, EditorStyles.numberField, options);
    }

    public static long LongField(string label, long value, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.LongField(r, label, value, style);
    }

    public static long LongField(GUIContent label, long value, params GUILayoutOption[] options)
    {
        return LongField(label, value, EditorStyles.numberField, options);
    }

    // Make a text field for entering integers.
    public static long LongField(GUIContent label, long value, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.LongField(r, label, value, style);
    }

    public static float Slider(float value, float leftValue, float rightValue, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetSliderRect(false, options);
        return EditorGUI.Slider(r, value, leftValue, rightValue);
    }

    public static float Slider(string label, float value, float leftValue, float rightValue, params GUILayoutOption[] options)
    {
        return Slider(EditorGUIUtility.TempContent(label), value, leftValue, rightValue, options);
    }

    // Make a slider the user can drag to change a value between a min and a max.
    public static float Slider(GUIContent label, float value, float leftValue, float rightValue, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetSliderRect(true, options);
        return EditorGUI.Slider(r, label, value, leftValue, rightValue);
    }

    internal static float Slider(GUIContent label, float value, float sliderLeftValue, float sliderRightValue, float textLeftValue, float textRightValue, params GUILayoutOption[] options)
    {
        return Slider(label, value, sliderLeftValue, sliderRightValue, textLeftValue, textRightValue, EditorStyles.numberField,
            GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb, null, GUI.skin.horizontalSliderThumbExtent, options);
    }

    static void GetSliderParts(GUIStyle baseStyle, ref GUIStyle textFieldStyle, ref GUIStyle thumbStyle, ref GUIStyle thumbExtentStyle)
    {
        string baseName = baseStyle.name;
        thumbStyle = GUI.skin.FindStyle(baseName + "Thumb") ?? thumbStyle;
        thumbExtentStyle = GUI.skin.FindStyle(baseName + "ThumbExtent") ?? thumbExtentStyle;
        textFieldStyle = GUI.skin.FindStyle(baseName + "TextField") ?? textFieldStyle;
    }

    static void GetHorizontalSliderParts(GUIStyle baseStyle, out GUIStyle textFieldStyle, out GUIStyle thumbStyle, out GUIStyle thumbExtentStyle)
    {
        thumbStyle = GUI.skin.horizontalSliderThumb;
        thumbExtentStyle = GUI.skin.horizontalSliderThumbExtent;
        textFieldStyle = EditorStyles.numberField;

        GetSliderParts(baseStyle, ref textFieldStyle, ref thumbStyle, ref thumbExtentStyle);
    }

    static void GetVerticalSliderParts(GUIStyle baseStyle, out GUIStyle textFieldStyle, out GUIStyle thumbStyle, out GUIStyle thumbExtentStyle)
    {
        thumbStyle = GUI.skin.verticalSliderThumb;
        thumbExtentStyle = GUI.skin.verticalSliderThumbExtent;
        textFieldStyle = EditorStyles.numberField;

        GetSliderParts(baseStyle, ref textFieldStyle, ref thumbStyle, ref thumbExtentStyle);
    }

    internal static float Slider(GUIContent label, float value, float sliderLeftValue, float sliderRightValue, float textLeftValue, float textRightValue, GUIStyle sliderStyle, params GUILayoutOption[] options)
    {
        GUIStyle sliderThumbStyle, sliderThumbStyleExtent, sliderTextFieldStyle;

        GetHorizontalSliderParts(sliderStyle, out sliderTextFieldStyle, out sliderThumbStyle, out sliderThumbStyleExtent);

        return Slider(label, value, sliderLeftValue, sliderRightValue, textLeftValue, textRightValue, sliderTextFieldStyle, sliderStyle
            , sliderThumbStyle, null, sliderThumbStyleExtent);
    }

    internal static float Slider(GUIContent label, float value, float sliderLeftValue, float sliderRightValue, float textLeftValue, float textRightValue
        , GUIStyle sliderTextField, GUIStyle sliderStyle, GUIStyle sliderThumbStyle, Texture2D sliderBackground, GUIStyle sliderThumbStyleExtent, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetSliderRect(true, sliderStyle, options);
        return EditorGUI.Slider(r, label, value, sliderLeftValue, sliderRightValue, textLeftValue, textRightValue, sliderTextField, sliderStyle, sliderThumbStyle, sliderBackground, sliderThumbStyleExtent);
    }

    public static void Slider(SerializedProperty property, float leftValue, float rightValue, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetSliderRect(false, options);
        EditorGUI.Slider(r, property, leftValue, rightValue);
    }

    public static void Slider(SerializedProperty property, float leftValue, float rightValue, string label, params GUILayoutOption[] options)
    {
        Slider(property, leftValue, rightValue, EditorGUIUtility.TempContent(label), options);
    }

    // Make a slider the user can drag to change a value between a min and a max.
    public static void Slider(SerializedProperty property, float leftValue, float rightValue, GUIContent label, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetSliderRect(true, options);
        EditorGUI.Slider(r, property, leftValue, rightValue, label);
    }

    internal static void Slider(SerializedProperty property, float sliderLeftValue, float sliderRightValue, float textLeftValue, float textRightValue, GUIContent label, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetSliderRect(true, options);
        EditorGUI.Slider(r, property, sliderLeftValue, sliderRightValue, textLeftValue, textRightValue, label);
    }

    internal static float PowerSlider(string label, float value, float leftValue, float rightValue, float power, params GUILayoutOption[] options)
    {
        return PowerSlider(EditorGUIUtility.TempContent(label), value, leftValue, rightValue, power, options);
    }

    // Make a power slider the user can drag to change a value between a min and a max.
    internal static float PowerSlider(GUIContent label, float value, float leftValue, float rightValue, float power, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetSliderRect(true, options);
        return EditorGUI.PowerSlider(r, label, value, leftValue, rightValue, power);
    }

    internal static int LogarithmicIntSlider(string label, int value, int leftValue, int rightValue, int logbase, int textFieldMin, int textFieldMax, params GUILayoutOption[] options)
    {
        return LogarithmicIntSlider(EditorGUIUtility.TempContent(label), value, leftValue, rightValue, logbase, textFieldMin, textFieldMax, options);
    }

    internal static int LogarithmicIntSlider(GUIContent label, int value, int leftValue, int rightValue, int logbase, int textFieldMin, int textFieldMax, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetSliderRect(true, options);
        return EditorGUI.LogarithmicIntSlider(r, label, value, leftValue, rightValue, logbase, textFieldMin, textFieldMax);
    }

    public static int IntSlider(int value, int leftValue, int rightValue, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetSliderRect(false, options);
        return EditorGUI.IntSlider(r, value, leftValue, rightValue);
    }

    internal static int IntSlider(int value, int leftValue, int rightValue, float power, GUIStyle sliderStyle, params GUILayoutOption[] options)
    {
        GUIStyle sliderThumbStyle, sliderThumbStyleExtent, sliderTextFieldStyle;

        GetHorizontalSliderParts(sliderStyle, out sliderTextFieldStyle, out sliderThumbStyle, out sliderThumbStyleExtent);

        return IntSlider(value, leftValue, rightValue, power, sliderTextFieldStyle, sliderStyle, sliderThumbStyle, null, sliderThumbStyleExtent, options);
    }

    internal static int IntSlider(int value, int leftValue, int rightValue, float power,
        GUIStyle textfieldStyle, GUIStyle sliderStyle, GUIStyle thumbStyle, Texture2D sliderBackground, GUIStyle thumbStyleExtent, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetSliderRect(false, sliderStyle, options);
        return EditorGUI.IntSlider(r, value, leftValue, rightValue, power, logbase: 1, textfieldStyle, sliderStyle, thumbStyle, sliderBackground, thumbStyleExtent);
    }

    public static int IntSlider(string label, int value, int leftValue, int rightValue, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetSliderRect(true, options);
        return EditorGUI.IntSlider(r, label, value, leftValue, rightValue);
    }

    // Make a slider the user can drag to change an integer value between a min and a max.
    public static int IntSlider(GUIContent label, int value, int leftValue, int rightValue, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetSliderRect(true, options);
        return EditorGUI.IntSlider(r, label, value, leftValue, rightValue);
    }

    public static void IntSlider(SerializedProperty property, int leftValue, int rightValue, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetSliderRect(false, options);
        EditorGUI.IntSlider(r, property, leftValue, rightValue, property.displayName);
    }

    public static void IntSlider(SerializedProperty property, int leftValue, int rightValue, string label, params GUILayoutOption[] options)
    {
        IntSlider(property, leftValue, rightValue, EditorGUIUtility.TempContent(label), options);
    }

    // Make a slider the user can drag to change an integer value between a min and a max.
    public static void IntSlider(SerializedProperty property, int leftValue, int rightValue, GUIContent label, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetSliderRect(true, options);
        EditorGUI.IntSlider(r, property, leftValue, rightValue, label);
    }

    public static void MinMaxSlider(ref float minValue, ref float maxValue, float minLimit, float maxLimit, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetSliderRect(false, options);
        EditorGUI.MinMaxSlider(r, ref minValue, ref maxValue, minLimit, maxLimit);
    }

    public static void MinMaxSlider(string label, ref float minValue, ref float maxValue, float minLimit, float maxLimit, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetSliderRect(true, options);
        EditorGUI.MinMaxSlider(r, label, ref minValue, ref maxValue, minLimit, maxLimit);
    }

    // Make a special slider the user can use to specify a range between a min and a max.
    public static void MinMaxSlider(GUIContent label, ref float minValue, ref float maxValue, float minLimit, float maxLimit, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetSliderRect(true, options);
        EditorGUI.MinMaxSlider(r, label, ref minValue, ref maxValue, minLimit, maxLimit);
    }

    public static int Popup(int selectedIndex, string[] displayedOptions, params GUILayoutOption[] options)
    {
        return Popup(selectedIndex, displayedOptions, EditorStyles.popup, options);
    }

    public static int Popup(int selectedIndex, string[] displayedOptions, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.Popup(r, selectedIndex, displayedOptions, style);
    }

    public static int Popup(int selectedIndex, GUIContent[] displayedOptions, params GUILayoutOption[] options)
    {
        return Popup(selectedIndex, displayedOptions, EditorStyles.popup, options);
    }

    public static int Popup(int selectedIndex, GUIContent[] displayedOptions, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.Popup(r, selectedIndex, displayedOptions, style);
    }

    public static int Popup(string label, int selectedIndex, string[] displayedOptions, params GUILayoutOption[] options)
    {
        return Popup(label, selectedIndex, displayedOptions, EditorStyles.popup, options);
    }

    public static int Popup(GUIContent label, int selectedIndex, string[] displayedOptions, params GUILayoutOption[] options)
    {
        return Popup(label, selectedIndex, displayedOptions, EditorStyles.popup, options);
    }

    public static int Popup(string label, int selectedIndex, string[] displayedOptions, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.Popup(r, label, selectedIndex, displayedOptions, style);
    }

    public static int Popup(GUIContent label, int selectedIndex, GUIContent[] displayedOptions, params GUILayoutOption[] options)
    {
        return Popup(label, selectedIndex, displayedOptions, EditorStyles.popup, options);
    }

    // Make a generic popup selection field.
    public static int Popup(GUIContent label, int selectedIndex, GUIContent[] displayedOptions, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.Popup(r, label, selectedIndex, displayedOptions, style);
    }

    internal static int Popup(GUIContent label, int selectedIndex, string[] displayedOptions, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.Popup(r, label, selectedIndex, displayedOptions, style);
    }

    internal static void Popup(SerializedProperty property, GUIContent[] displayedOptions, params GUILayoutOption[] options)
    {
        Popup(property, displayedOptions, null, options);
    }

    internal static void Popup(SerializedProperty property, GUIContent[] displayedOptions, GUIContent label, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.popup, options);
        EditorGUI.Popup(r, property, displayedOptions, label);
    }

    public static Enum EnumPopup(Enum selected, params GUILayoutOption[] options)
    {
        return EnumPopup(selected, EditorStyles.popup, options);
    }

    public static Enum EnumPopup(Enum selected, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.EnumPopup(r, selected, style);
    }

    public static Enum EnumPopup(string label, Enum selected, params GUILayoutOption[] options)
    {
        return EnumPopup(label, selected, EditorStyles.popup, options);
    }

    public static Enum EnumPopup(string label, Enum selected, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.EnumPopup(r, GUIContent.Temp(label), selected, null, false, style);
    }

    public static Enum EnumPopup(GUIContent label, Enum selected, params GUILayoutOption[] options)
    {
        return EnumPopup(label, selected, EditorStyles.popup, options);
    }

    // Make an enum popup selection field.
    public static Enum EnumPopup(GUIContent label, Enum selected, GUIStyle style, params GUILayoutOption[] options)
    {
        return EnumPopup(label, selected, null, false, style, options);
    }

    public static Enum EnumPopup(GUIContent label, Enum selected, Func<Enum, bool> checkEnabled, bool includeObsolete, params GUILayoutOption[] options)
    {
        return EnumPopup(label, selected, checkEnabled, includeObsolete, EditorStyles.popup, options);
    }

    public static Enum EnumPopup(GUIContent label, Enum selected, Func<Enum, bool> checkEnabled, bool includeObsolete, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.EnumPopup(r, label, selected, checkEnabled, includeObsolete, style);
    }

    public static int IntPopup(int selectedValue, string[] displayedOptions, int[] optionValues, params GUILayoutOption[] options)
    {
        return IntPopup(selectedValue, displayedOptions, optionValues, EditorStyles.popup, options);
    }

    public static int IntPopup(int selectedValue, string[] displayedOptions, int[] optionValues, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.IntPopup(r,  selectedValue, displayedOptions, optionValues, style);
    }

    public static int IntPopup(int selectedValue, GUIContent[] displayedOptions, int[] optionValues, params GUILayoutOption[] options)
    {
        return IntPopup(selectedValue, displayedOptions, optionValues, EditorStyles.popup, options);
    }

    public static int IntPopup(int selectedValue, GUIContent[] displayedOptions, int[] optionValues, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.IntPopup(r,  GUIContent.none, selectedValue, displayedOptions, optionValues, style);
    }

    public static int IntPopup(string label, int selectedValue, string[] displayedOptions, int[] optionValues, params GUILayoutOption[] options)
    {
        return IntPopup(label, selectedValue, displayedOptions, optionValues, EditorStyles.popup, options);
    }

    public static int IntPopup(string label, int selectedValue, string[] displayedOptions, int[] optionValues, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.IntPopup(r, label, selectedValue, displayedOptions, optionValues, style);
    }

    public static int IntPopup(GUIContent label, int selectedValue, GUIContent[] displayedOptions, int[] optionValues, params GUILayoutOption[] options)
    {
        return IntPopup(label, selectedValue, displayedOptions, optionValues, EditorStyles.popup, options);
    }

    // Make an integer popup selection field.
    public static int IntPopup(GUIContent label, int selectedValue, GUIContent[] displayedOptions, int[] optionValues, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.IntPopup(r, label, selectedValue, displayedOptions, optionValues, style);
    }

    public static void IntPopup(SerializedProperty property, GUIContent[] displayedOptions, int[] optionValues, params GUILayoutOption[] options)
    {
        IntPopup(property, displayedOptions, optionValues, null, options);
    }

    // Make an integer popup selection field.
    public static void IntPopup(SerializedProperty property, GUIContent[] displayedOptions, int[] optionValues, GUIContent label, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.popup, options);
        EditorGUI.IntPopup(r, property, displayedOptions, optionValues, label);
    }

    [Obsolete("This function is obsolete and the style is not used.")]
    public static void IntPopup(SerializedProperty property, GUIContent[] displayedOptions, int[] optionValues, GUIContent label, GUIStyle style, params GUILayoutOption[] options)
    {
        IntPopup(property, displayedOptions, optionValues, label, options);
    }

    public static string TagField(string tag, params GUILayoutOption[] options)
    {
        return TagField(tag, EditorStyles.popup, options);
    }

    public static string TagField(string tag, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.TagField(r, tag, style);
    }

    public static string TagField(string label, string tag, params GUILayoutOption[] options)
    {
        return TagField(EditorGUIUtility.TempContent(label), tag, EditorStyles.popup, options);
    }

    public static string TagField(string label, string tag, GUIStyle style, params GUILayoutOption[] options)
    {
        return TagField(EditorGUIUtility.TempContent(label), tag, style, options);
    }

    public static string TagField(GUIContent label, string tag, params GUILayoutOption[] options)
    {
        return TagField(label, tag, EditorStyles.popup, options);
    }

    // Make a tag selection field.
    public static string TagField(GUIContent label, string tag, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.TagField(r, label, tag, style);
    }

    public static int LayerField(int layer, params GUILayoutOption[] options)
    {
        return LayerField(layer, EditorStyles.popup, options);
    }

    public static int LayerField(int layer, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.LayerField(r, layer, style);
    }

    public static int LayerField(string label, int layer, params GUILayoutOption[] options)
    {
        return LayerField(EditorGUIUtility.TempContent(label), layer, EditorStyles.popup, options);
    }

    public static int LayerField(string label, int layer, GUIStyle style, params GUILayoutOption[] options)
    {
        return LayerField(EditorGUIUtility.TempContent(label), layer, style, options);
    }

    public static int LayerField(GUIContent label, int layer, params GUILayoutOption[] options)
    {
        return LayerField(label, layer, EditorStyles.popup, options);
    }

    // Make a layer selection field.
    public static int LayerField(GUIContent label, int layer, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.LayerField(r, label, layer, style);
    }

    public static int MaskField(GUIContent label, int mask, string[] displayedOptions, GUIStyle style, params GUILayoutOption[] options)
    {
        var r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.MaskField(r, label, mask, displayedOptions, style);
    }

    public static int MaskField(string label, int mask, string[] displayedOptions, GUIStyle style, params GUILayoutOption[] options)
    {
        var r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.MaskField(r, label, mask, displayedOptions, style);
    }

    public static int MaskField(GUIContent label, int mask, string[] displayedOptions, params GUILayoutOption[] options)
    {
        var r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.popup, options);
        return EditorGUI.MaskField(r, label, mask, displayedOptions, EditorStyles.popup);
    }

    public static int MaskField(string label, int mask, string[] displayedOptions, params GUILayoutOption[] options)
    {
        var r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.popup, options);
        return EditorGUI.MaskField(r, label, mask, displayedOptions, EditorStyles.popup);
    }

    public static int MaskField(int mask, string[] displayedOptions, GUIStyle style, params GUILayoutOption[] options)
    {
        var r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.MaskField(r, mask, displayedOptions, style);
    }

    // Make a field for masks.
    public static int MaskField(int mask, string[] displayedOptions, params GUILayoutOption[] options)
    {
        var r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, EditorStyles.popup, options);
        return EditorGUI.MaskField(r, mask, displayedOptions, EditorStyles.popup);
    }

    public static Enum EnumFlagsField(Enum enumValue, params GUILayoutOption[] options)
    {
        return EnumFlagsField(enumValue, EditorStyles.popup, options);
    }

    public static Enum EnumFlagsField(Enum enumValue, GUIStyle style, params GUILayoutOption[] options)
    {
        var position = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.EnumFlagsField(position, enumValue, style);
    }

    public static Enum EnumFlagsField(string label, Enum enumValue, params GUILayoutOption[] options)
    {
        return EnumFlagsField(label, enumValue, EditorStyles.popup, options);
    }

    public static Enum EnumFlagsField(string label, Enum enumValue, GUIStyle style, params GUILayoutOption[] options)
    {
        return EnumFlagsField(EditorGUIUtility.TempContent(label), enumValue, style, options);
    }

    public static Enum EnumFlagsField(GUIContent label, Enum enumValue, params GUILayoutOption[] options)
    {
        return EnumFlagsField(label, enumValue, EditorStyles.popup, options);
    }

    public static Enum EnumFlagsField(GUIContent label, Enum enumValue, GUIStyle style, params GUILayoutOption[] options)
    {
        var position = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.EnumFlagsField(position, label, enumValue, style);
    }

    public static Enum EnumFlagsField(GUIContent label, Enum enumValue, bool includeObsolete, params GUILayoutOption[] options)
    {
        return EnumFlagsField(label, enumValue, includeObsolete, EditorStyles.popup, options);
    }

    public static Enum EnumFlagsField(GUIContent label, Enum enumValue, bool includeObsolete, GUIStyle style, params GUILayoutOption[] options)
    {
        var position = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.EnumFlagsField(position, label, enumValue, includeObsolete, style);
    }

    [Obsolete("Check the docs for the usage of the new parameter 'allowSceneObjects'.")]
    public static Object ObjectField(Object obj, Type objType, params GUILayoutOption[] options)
    {
        return ObjectField(obj, objType, true, options);
    }

    public static Object ObjectField(Object obj, Type objType, Object targetBeingEdited, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, options);
        return EditorGUI.ObjectField(r, obj, objType, targetBeingEdited);
    }

    public static Object ObjectField(Object obj, Type objType, bool allowSceneObjects, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, options);
        return EditorGUI.ObjectField(r, obj, objType, allowSceneObjects);
    }

    [Obsolete("Check the docs for the usage of the new parameter 'allowSceneObjects'.")]
    public static Object ObjectField(string label, Object obj, Type objType, params GUILayoutOption[] options)
    {
        return ObjectField(label, obj, objType, true, options);
    }

    public static Object ObjectField(string label, Object obj, Type objType, Object targetBeingEdited, params GUILayoutOption[] options)
    {
        return ObjectField(EditorGUIUtility.TempContent(label), obj, objType, targetBeingEdited, options);
    }

    public static Object ObjectField(string label, Object obj, Type objType, bool allowSceneObjects, params GUILayoutOption[] options)
    {
        return ObjectField(EditorGUIUtility.TempContent(label), obj, objType, allowSceneObjects, options);
    }

    [Obsolete("Check the docs for the usage of the new parameter 'allowSceneObjects'.")]
    public static Object ObjectField(GUIContent label, Object obj, Type objType, params GUILayoutOption[] options)
    {
        return ObjectField(label, obj, objType, true, options);
    }

    // Make an object field. You can assign objects either by drag'n drop objects or by selecting an object using the Object Picker.
    public static Object ObjectField(GUIContent label, Object obj, Type objType, Object targetBeingEdited, params GUILayoutOption[] options)
    {
        var height = EditorGUIUtility.HasObjectThumbnail(objType) ? EditorGUI.kObjectFieldThumbnailHeight : EditorGUI.kSingleLineHeight;
        Rect r = s_LastRect = GetControlRect(true, height, options);
        return EditorGUI.ObjectField(r, label, obj, objType, targetBeingEdited);
    }

    // Make an object field. You can assign objects either by drag'n drop objects or by selecting an object using the Object Picker.
    public static Object ObjectField(GUIContent label, Object obj, Type objType, bool allowSceneObjects, params GUILayoutOption[] options)
    {
        var height = EditorGUIUtility.HasObjectThumbnail(objType) ? EditorGUI.kObjectFieldThumbnailHeight : EditorGUI.kSingleLineHeight;
        Rect r = s_LastRect = GetControlRect(true, height, options);
        return EditorGUI.ObjectField(r, label, obj, objType, allowSceneObjects);
    }

    public static void ObjectField(SerializedProperty property, params GUILayoutOption[] options)
    {
        ObjectField(property, (GUIContent)null, options);
    }

    public static void ObjectField(SerializedProperty property, GUIContent label, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.objectField, options);
        EditorGUI.ObjectField(r, property, label);
    }

    public static void ObjectField(SerializedProperty property, Type objType, params GUILayoutOption[] options)
    {
        ObjectField(property, objType, null, options);
    }

    // Make an object field. You can assign objects either by drag'n drop objects or by selecting an object using the Object Picker.
    public static void ObjectField(SerializedProperty property, Type objType, GUIContent label, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.objectField, options);
        EditorGUI.ObjectField(r, property, objType, label);
    }

    internal static void ObjectField(SerializedProperty property, Type objType, GUIContent label, EditorGUI.ObjectFieldValidator validator, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.objectField, options);
        EditorGUI.ObjectField(r, property, objType, label, EditorStyles.objectField, validator);
    }

    internal static Object MiniThumbnailObjectField(GUIContent label, Object obj, Type objType, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, options);
        return EditorGUI.MiniThumbnailObjectField(r, label, obj, objType);
    }

    public static Vector2 Vector2Field(string label, Vector2 value, params GUILayoutOption[] options)
    {
        return Vector2Field(EditorGUIUtility.TempContent(label), value, options);
    }

    // Make an X & Y field for entering a [[Vector2]].
    public static Vector2 Vector2Field(GUIContent label, Vector2 value, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector2, label), EditorStyles.numberField, options);
        return EditorGUI.Vector2Field(r, label, value);
    }

    public static Vector3 Vector3Field(string label, Vector3 value, params GUILayoutOption[] options)
    {
        return Vector3Field(EditorGUIUtility.TempContent(label), value, options);
    }

    // Make an X, Y & Z field for entering a [[Vector3]].
    public static Vector3 Vector3Field(GUIContent label, Vector3 value, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector3, label), EditorStyles.numberField, options);
        return EditorGUI.Vector3Field(r, label, value);
    }

    // Make an X, Y & Z field for entering a [[Vector3]], with a "lock"
    internal static Vector3 LinkedVector3Field(GUIContent label, Vector3 value, ref bool proportionalScale, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector3, label), EditorStyles.numberField, options);
        return EditorGUI.LinkedVector3Field(r, label, value, ref proportionalScale);
    }

    // Make an X, Y & Z field for entering a [[Vector3]], with a "lock"
    internal static Vector3 LinkedVector3Field(GUIContent label, Vector3 value, Vector3 initialValue, ref bool proportionalScale, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector3, label), EditorStyles.numberField, options);
        int axisModified = 0;// Use X as default modified axis
        return EditorGUI.LinkedVector3Field(r, label, GUIContent.none, value, ref proportionalScale, initialValue, 0, ref axisModified, null);
    }

    // Make an X, Y, Z & W field for entering a [[Vector4]].
    public static Vector4 Vector4Field(string label, Vector4 value, params GUILayoutOption[] options)
    {
        return Vector4Field(EditorGUIUtility.TempContent(label), value, options);
    }

    // Make an X, Y, Z & W field for entering a [[Vector4]].
    public static Vector4 Vector4Field(GUIContent label, Vector4 value, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector4, label), EditorStyles.numberField, options);
        return EditorGUI.Vector4Field(r, label, value);
    }

    public static Vector2Int Vector2IntField(string label, Vector2Int value, params GUILayoutOption[] options)
    {
        return Vector2IntField(EditorGUIUtility.TempContent(label), value, options);
    }

    // Make an X & Y field for entering a [[Vector2Int]].
    public static Vector2Int Vector2IntField(GUIContent label, Vector2Int value, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector2Int, label), EditorStyles.numberField, options);
        return EditorGUI.Vector2IntField(r, label, value);
    }

    public static Vector3Int Vector3IntField(string label, Vector3Int value, params GUILayoutOption[] options)
    {
        return Vector3IntField(EditorGUIUtility.TempContent(label), value, options);
    }

    // Make an X, Y & Z field for entering a [[Vector3Int]].
    public static Vector3Int Vector3IntField(GUIContent label, Vector3Int value, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector3Int, label), EditorStyles.numberField, options);
        return EditorGUI.Vector3IntField(r, label, value);
    }

    public static Rect RectField(Rect value, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.GetPropertyHeight(SerializedPropertyType.Rect, GUIContent.none), EditorStyles.numberField, options);
        return EditorGUI.RectField(r, value);
    }

    public static Rect RectField(string label, Rect value, params GUILayoutOption[] options)
    {
        return RectField(EditorGUIUtility.TempContent(label), value, options);
    }

    // Make an X, Y, W & H field for entering a [[Rect]].
    public static Rect RectField(GUIContent label, Rect value, params GUILayoutOption[] options)
    {
        bool hasLabel = EditorGUI.LabelHasContent(label);
        float height = EditorGUI.GetPropertyHeight(SerializedPropertyType.Rect, label);
        Rect r = s_LastRect = GetControlRect(hasLabel, height, EditorStyles.numberField, options);
        return EditorGUI.RectField(r, label, value);
    }

    public static RectInt RectIntField(RectInt value, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.GetPropertyHeight(SerializedPropertyType.RectInt, GUIContent.none), EditorStyles.numberField, options);
        return EditorGUI.RectIntField(r, value);
    }

    public static RectInt RectIntField(string label, RectInt value, params GUILayoutOption[] options)
    {
        return RectIntField(EditorGUIUtility.TempContent(label), value, options);
    }

    // Make an X, Y, W & H field for entering a [[RectInt]].
    public static RectInt RectIntField(GUIContent label, RectInt value, params GUILayoutOption[] options)
    {
        bool hasLabel = EditorGUI.LabelHasContent(label);
        float height = EditorGUI.GetPropertyHeight(SerializedPropertyType.RectInt, label);
        Rect r = s_LastRect = GetControlRect(hasLabel, height, EditorStyles.numberField, options);
        return EditorGUI.RectIntField(r, label, value);
    }

    public static Bounds BoundsField(Bounds value, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.GetPropertyHeight(SerializedPropertyType.Bounds, GUIContent.none), EditorStyles.numberField, options);
        return EditorGUI.BoundsField(r, value);
    }

    public static Bounds BoundsField(string label, Bounds value, params GUILayoutOption[] options)
    {
        return BoundsField(EditorGUIUtility.TempContent(label), value, options);
    }

    // Make Center & Extents field for entering a [[Bounds]].
    public static Bounds BoundsField(GUIContent label, Bounds value, params GUILayoutOption[] options)
    {
        bool hasLabel = EditorGUI.LabelHasContent(label);
        float height = EditorGUI.GetPropertyHeight(SerializedPropertyType.Bounds, label);
        Rect r = s_LastRect = GetControlRect(hasLabel, height, EditorStyles.numberField, options);
        return EditorGUI.BoundsField(r, label, value);
    }

    public static BoundsInt BoundsIntField(BoundsInt value, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.GetPropertyHeight(SerializedPropertyType.BoundsInt, GUIContent.none), EditorStyles.numberField, options);
        return EditorGUI.BoundsIntField(r, value);
    }

    public static BoundsInt BoundsIntField(string label, BoundsInt value, params GUILayoutOption[] options)
    {
        return BoundsIntField(EditorGUIUtility.TempContent(label), value, options);
    }

    // Make Center & Extents field for entering a [[BoundsInt]].
    public static BoundsInt BoundsIntField(GUIContent label, BoundsInt value, params GUILayoutOption[] options)
    {
        bool hasLabel = EditorGUI.LabelHasContent(label);
        float height = EditorGUI.GetPropertyHeight(SerializedPropertyType.BoundsInt, label);
        Rect r = s_LastRect = GetControlRect(hasLabel, height, EditorStyles.numberField, options);
        return EditorGUI.BoundsIntField(r, label, value);
    }

    // Make a property field that look like a multi property field (but is made up of individual properties)
    internal static void PropertiesField(GUIContent label, SerializedProperty[] properties, GUIContent[] propertyLabels, float propertyLabelsWidth, params GUILayoutOption[] options)
    {
        bool hasLabel = EditorGUI.LabelHasContent(label);
        float height = EditorGUI.kSingleLineHeight * properties.Length + EditorGUI.kVerticalSpacingMultiField * (properties.Length - 1);
        Rect r = s_LastRect = GetControlRect(hasLabel, height, EditorStyles.numberField, options);
        EditorGUI.PropertiesField(r, label, properties, propertyLabels, propertyLabelsWidth);
    }

    internal static int CycleButton(int selected, GUIContent[] contents, GUIStyle style, params GUILayoutOption[] options)
    {
        if (GUILayout.Button(contents[selected], style, options))
        {
            selected++;
            if (selected >= contents.Length)
                selected = 0;
        }
        return selected;
    }

    public static Color ColorField(Color value, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, EditorStyles.colorField, options);
        return EditorGUI.ColorField(r, value);
    }

    public static Color ColorField(string label, Color value, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.colorField, options);
        return EditorGUI.ColorField(r, label, value);
    }

    // Make a field for selecting a [[Color]].
    public static Color ColorField(GUIContent label, Color value, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.colorField, options);
        return EditorGUI.ColorField(r, label, value);
    }

#pragma warning disable 612
    [Obsolete("Use EditorGUILayout.ColorField(GUIContent label, Color value, bool showEyedropper, bool showAlpha, bool hdr, params GUILayoutOption[] options)")]
    public static Color ColorField(
        GUIContent label, Color value, bool showEyedropper, bool showAlpha, bool hdr, ColorPickerHDRConfig hdrConfig, params GUILayoutOption[] options
    )
    {
        return ColorField(label, value, showEyedropper, showAlpha, hdr);
    }

#pragma warning restore 612

    public static Color ColorField(GUIContent label, Color value, bool showEyedropper, bool showAlpha, bool hdr, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.colorField, options);
        return EditorGUI.ColorField(r, label, value, showEyedropper, showAlpha, hdr);
    }

    public static AnimationCurve CurveField(AnimationCurve value, params GUILayoutOption[] options)
    {
        // TODO Change style
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, EditorStyles.colorField, options);
        return EditorGUI.CurveField(r, value);
    }

    public static AnimationCurve CurveField(string label, AnimationCurve value, params GUILayoutOption[] options)
    {
        // TODO Change style
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.colorField, options);
        return EditorGUI.CurveField(r, label, value);
    }

    public static AnimationCurve CurveField(GUIContent label, AnimationCurve value, params GUILayoutOption[] options)
    {
        // TODO Change style
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.colorField, options);
        return EditorGUI.CurveField(r, label, value);
    }

    // Variants with settings
    public static AnimationCurve CurveField(AnimationCurve value, Color color, Rect ranges, params GUILayoutOption[] options)
    {
        // TODO Change style
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, EditorStyles.colorField, options);
        return EditorGUI.CurveField(r, value, color, ranges);
    }

    public static AnimationCurve CurveField(string label, AnimationCurve value, Color color, Rect ranges, params GUILayoutOption[] options)
    {
        // TODO Change style
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.colorField, options);
        return EditorGUI.CurveField(r, label, value, color, ranges);
    }

    // Make a field for editing an [[AnimationCurve]].
    public static AnimationCurve CurveField(GUIContent label, AnimationCurve value, Color color, Rect ranges, params GUILayoutOption[] options)
    {
        // TODO Change style
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.colorField, options);
        return EditorGUI.CurveField(r, label, value, color, ranges);
    }

    public static void CurveField(SerializedProperty property, Color color, Rect ranges, params GUILayoutOption[] options)
    {
        CurveField(property, color, ranges, null, options);
    }

    // Make a field for editing an [[AnimationCurve]].
    public static void CurveField(SerializedProperty property, Color color, Rect ranges, GUIContent label, params GUILayoutOption[] options)
    {
        // TODO Change style
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, EditorStyles.colorField, options);
        EditorGUI.CurveField(r, property, color, ranges, label);
    }

    public static bool InspectorTitlebar(bool foldout, Object targetObj)
    {
        return InspectorTitlebar(foldout, targetObj, true);
    }

    public static bool InspectorTitlebar(bool foldout, Object targetObj, bool expandable)
    {
        return EditorGUI.InspectorTitlebar(GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.inspectorTitlebar), foldout,
            targetObj, expandable);
    }

    // Make an inspector-window-like titlebar.
    public static bool InspectorTitlebar(bool foldout, Object[] targetObjs)
    {
        return InspectorTitlebar(foldout, targetObjs, true);
    }

    public static bool InspectorTitlebar(bool foldout, Object[] targetObjs, bool expandable)
    {
        return EditorGUI.InspectorTitlebar(GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.inspectorTitlebar), foldout,
            targetObjs, expandable);
    }

    public static bool InspectorTitlebar(bool foldout, Editor editor)
    {
        return EditorGUI.InspectorTitlebar(GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.inspectorTitlebar), foldout,
            editor);
    }

    public static void InspectorTitlebar(Object[] targetObjs)
    {
        EditorGUI.InspectorTitlebar(GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.inspectorTitlebar), targetObjs);
    }

    // Make a foldout with a toggle and title
    internal static bool ToggleTitlebar(bool foldout, GUIContent label, ref bool toggleValue)
    {
        return EditorGUI.ToggleTitlebar(GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.inspectorTitlebar), label, foldout, ref toggleValue);
    }

    internal static bool ToggleTitlebar(bool foldout, GUIContent label, SerializedProperty property)
    {
        bool toggleValue = property.boolValue;
        EditorGUI.BeginChangeCheck();
        foldout = EditorGUI.ToggleTitlebar(GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.inspectorTitlebar), label, foldout, ref toggleValue);
        if (EditorGUI.EndChangeCheck())
            property.boolValue = toggleValue;

        return foldout;
    }

    internal static bool FoldoutTitlebar(bool foldout, GUIContent label, bool skipIconSpacing)
    {
        return FoldoutTitlebar(foldout, label, skipIconSpacing, EditorStyles.inspectorTitlebar, EditorStyles.inspectorTitlebarText);
    }

    internal static bool FoldoutTitlebar(bool foldout, GUIContent label, bool skipIconSpacing, GUIStyle baseStyle, GUIStyle textStyle)
    {
        return EditorGUI.FoldoutTitlebar(GUILayoutUtility.GetRect(GUIContent.none, baseStyle, GUILayout.ExpandWidth(true)), label, foldout, skipIconSpacing, baseStyle, textStyle);
    }

    // Make a label with a foldout arrow to the left of it.
    internal static bool FoldoutInternal(bool foldout, GUIContent content, bool toggleOnLabelClick, GUIStyle style)
    {
        Rect r = s_LastRect = GUILayoutUtility.GetRect(EditorGUIUtility.fieldWidth, EditorGUIUtility.fieldWidth, EditorGUI.kSingleLineHeight, EditorGUI.kSingleLineHeight, style);
        return EditorGUI.Foldout(r, foldout, content, toggleOnLabelClick, style);
    }

    internal static uint LayerMaskField(UInt32 layers, GUIContent label, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, options);
        return EditorGUI.LayerMaskField(r, layers, label);
    }

    internal static LayerMask LayerMaskField(LayerMask layers, GUIContent label, params GUILayoutOption[] options)
    {
        var rect = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, options);
        return EditorGUI.LayerMaskField(rect, layers, label);
    }

    internal static void LayerMaskField(SerializedProperty property, GUIContent label, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, options);
        EditorGUI.LayerMaskField(r, property, label);
    }

    internal static uint RenderingLayerMaskField(uint layers, GUIContent label, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, options);
        return EditorGUI.RenderingLayerMaskField(r, layers, label);
    }

    internal static RenderingLayerMask RenderingLayerMaskField(RenderingLayerMask layers, GUIContent label, params GUILayoutOption[] options)
    {
        var rect = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, options);
        return EditorGUI.RenderingLayerMaskField(rect, layers, label);
    }

    internal static void RenderingLayerMaskField(SerializedProperty property, GUIContent label, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, options);
        EditorGUI.RenderingLayerMaskField(r, property, label);
    }

    public static void HelpBox(string message, MessageType type)
    {
        HelpBox(EditorGUIUtility.TempContent(message, EditorGUIUtility.GetHelpIcon(type)), true);
    }

    // Make a help box with a message to the user.
    public static void HelpBox(string message, MessageType type, bool wide)
    {
        HelpBox(EditorGUIUtility.TempContent(message, EditorGUIUtility.GetHelpIcon(type)), wide);
    }

    // Make a help box with a message to the user.
    public static void HelpBox(GUIContent content, bool wide = true)
    {
        BeginHorizontal();
        PrefixLabel(wide ? GUIContent.none : EditorGUIUtility.blankContent, EditorStyles.helpBox);
        Rect r = GUILayoutUtility.GetRect(content, EditorStyles.helpBox);
        int oldIndent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        EditorGUI.HelpBox(r, content);
        EditorGUI.indentLevel = oldIndent;
        EndHorizontal();
    }

    // Make a label in front of some control.
    internal static void PrefixLabelInternal(GUIContent label, GUIStyle followingStyle, GUIStyle labelStyle)
    {
        float p = followingStyle.margin.left;
        if (!EditorGUI.LabelHasContent(label))
        {
            GUILayoutUtility.GetRect(EditorGUI.indent - p, EditorGUI.kSingleLineHeight, followingStyle, GUILayout.ExpandWidth(false));
            return;
        }

        Rect r = GUILayoutUtility.GetRect(EditorGUIUtility.labelWidth - p, EditorGUI.kSingleLineHeight, followingStyle, GUILayout.ExpandWidth(false));
        r.xMin += EditorGUI.indent;
        EditorGUI.HandlePrefixLabel(r, r, label, 0, labelStyle);
    }

    // Make a small space between the previous control and the following.
    public static void Space()
    {
        Space(EditorGUI.kDefaultSpacing, true);
    }

    public static void Space(float width)
    {
        Space(width, true);
    }

    public static void Space(float width, bool expand)
    {
        GUILayoutUtility.GetRect(width, width, GUILayout.ExpandWidth(expand));
    }

    //[System.Obsolete ("Use Space() instead")]
    // Make this function Obsolete when someone has time to _rename_ all
    // the Standard Packages to Space(), as currently it shows tons of
    // warnings.
    // Same for the graphic tests.
    // *undoc*
    public static void Separator()
    {
        Space();
    }

    public class ToggleGroupScope : GUI.Scope
    {
        public bool enabled { get; protected set; }

        public ToggleGroupScope(string label, bool toggle)
        {
            enabled = BeginToggleGroup(label, toggle);
        }

        public ToggleGroupScope(GUIContent label, bool toggle)
        {
            enabled = BeginToggleGroup(label, toggle);
        }

        protected override void CloseScope()
        {
            EndToggleGroup();
        }
    }

    public static bool BeginToggleGroup(string label, bool toggle)
    {
        return BeginToggleGroup(EditorGUIUtility.TempContent(label), toggle);
    }

    // Begin a vertical group with a toggle to enable or disable all the controls within at once.
    public static bool BeginToggleGroup(GUIContent label, bool toggle)
    {
        toggle = ToggleLeft(label, toggle, EditorStyles.boldLabel);
        EditorGUI.BeginDisabled(!toggle);
        GUILayout.BeginVertical();

        return toggle;
    }

    // Close a group started with ::ref::BeginToggleGroup
    public static void EndToggleGroup()
    {
        GUILayout.EndVertical();
        EditorGUI.EndDisabled();
    }

    public class HorizontalScope : GUI.Scope
    {
        public Rect rect { get; protected set; }

        public HorizontalScope(params GUILayoutOption[] options)
        {
            rect = BeginHorizontal(options);
        }

        public HorizontalScope(GUIStyle style, params GUILayoutOption[] options)
        {
            rect = BeginHorizontal(style, options);
        }

        internal HorizontalScope(GUIContent content, GUIStyle style, params GUILayoutOption[] options)
        {
            rect = BeginHorizontal(content, style, options);
        }

        protected override void CloseScope()
        {
            EndHorizontal();
        }
    }

    public static Rect BeginHorizontal(params GUILayoutOption[] options)
    {
        return BeginHorizontal(GUIContent.none, GUIStyle.none, options);
    }

    // Begin a horizontal group and get its rect back.
    public static Rect BeginHorizontal(GUIStyle style, params GUILayoutOption[] options)
    {
        return BeginHorizontal(GUIContent.none, style, options);
    }

    // public static Rect BeginHorizontal (string text, params GUILayoutOption[] options)                       { return BeginHorizontal (EditorGUIUtility.TempContent (text), GUIStyle.none, options); }
    // public static Rect BeginHorizontal (Texture image, params GUILayoutOption[] options)                 { return BeginHorizontal (EditorGUIUtility.TempContent (image), GUIStyle.none, options); }
    // public static Rect BeginHorizontal (GUIContent content, params GUILayoutOption[] options)                { return BeginHorizontal (content, GUIStyle.none, options); }
    // public static Rect BeginHorizontal (string text, GUIStyle style, params GUILayoutOption[] options)           { return BeginHorizontal (EditorGUIUtility.TempContent (text), style, options); }
    // public static Rect BeginHorizontal (Texture image, GUIStyle style, params GUILayoutOption[] options)     { return BeginHorizontal (EditorGUIUtility.TempContent (image), style, options); }
    internal static Rect BeginHorizontal(GUIContent content, GUIStyle style, params GUILayoutOption[] options)
    {
        GUILayoutGroup g = GUILayoutUtility.BeginLayoutGroup(style, options, typeof(GUILayoutGroup));
        g.isVertical = false;
        if (style != GUIStyle.none || content != GUIContent.none)
        {
            GUI.Box(g.rect, GUIContent.none, style);
        }
        return g.rect;
    }

    // Close a group started with BeginHorizontal
    public static void EndHorizontal()
    {
        GUILayout.EndHorizontal();
    }

    public class VerticalScope : GUI.Scope
    {
        public Rect rect { get; protected set; }

        public VerticalScope(params GUILayoutOption[] options)
        {
            rect = BeginVertical(options);
        }

        public VerticalScope(GUIStyle style, params GUILayoutOption[] options)
        {
            rect = BeginVertical(style, options);
        }

        internal VerticalScope(GUIContent content, GUIStyle style, params GUILayoutOption[] options)
        {
            rect = BeginVertical(content, style, options);
        }

        protected override void CloseScope()
        {
            EndVertical();
        }
    }

    public static Rect BeginVertical(params GUILayoutOption[] options)
    {
        return BeginVertical(GUIContent.none, GUIStyle.none, options);
    }

    // Begin a vertical group and get its rect back.
    public static Rect BeginVertical(GUIStyle style, params GUILayoutOption[] options)
    {
        return BeginVertical(GUIContent.none, style, options);
    }

    // public static Rect BeginVertical (string text, params GUILayoutOption[] options)                     { return BeginVertical (EditorGUIUtility.TempContent (text), GUIStyle.none, options); }
    // public static Rect BeginVertical (Texture image, params GUILayoutOption[] options)                   { return BeginVertical (EditorGUIUtility.TempContent (image), GUIStyle.none, options); }
    // public static Rect BeginVertical (GUIContent content, params GUILayoutOption[] options)              { return BeginVertical (content, GUIStyle.none, options); }
    // public static Rect BeginVertical (string text, GUIStyle style, params GUILayoutOption[] options)         { return BeginVertical (EditorGUIUtility.TempContent (text), style, options); }
    // public static Rect BeginVertical (Texture image, GUIStyle style, params GUILayoutOption[] options)       { return BeginVertical (EditorGUIUtility.TempContent (image), style, options); }
    internal static Rect BeginVertical(GUIContent content, GUIStyle style, params GUILayoutOption[] options)
    {
        GUILayoutGroup g = GUILayoutUtility.BeginLayoutGroup(style, options, typeof(GUILayoutGroup));
        g.isVertical = true;
        if (style != GUIStyle.none || content != GUIContent.none)
        {
            GUI.Box(g.rect, GUIContent.none, style);
        }
        return g.rect;
    }

    // Close a group started with BeginVertical
    public static void EndVertical()
    {
        GUILayout.EndVertical();
    }

    public class ScrollViewScope : GUI.Scope
    {
        public Vector2 scrollPosition { get; protected set; }
        public bool handleScrollWheel { get; set; }

        public ScrollViewScope(Vector2 scrollPosition, params GUILayoutOption[] options)
        {
            handleScrollWheel = true;
            this.scrollPosition = BeginScrollView(scrollPosition, options);
        }

        public ScrollViewScope(Vector2 scrollPosition, bool alwaysShowHorizontal, bool alwaysShowVertical, params GUILayoutOption[] options)
        {
            handleScrollWheel = true;
            this.scrollPosition = BeginScrollView(scrollPosition, alwaysShowHorizontal, alwaysShowVertical, options);
        }

        public ScrollViewScope(Vector2 scrollPosition, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, params GUILayoutOption[] options)
        {
            handleScrollWheel = true;
            this.scrollPosition = BeginScrollView(scrollPosition, horizontalScrollbar, verticalScrollbar, options);
        }

        public ScrollViewScope(Vector2 scrollPosition, GUIStyle style, params GUILayoutOption[] options)
        {
            handleScrollWheel = true;
            this.scrollPosition = BeginScrollView(scrollPosition, style, options);
        }

        public ScrollViewScope(Vector2 scrollPosition, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, GUIStyle background, params GUILayoutOption[] options)
        {
            handleScrollWheel = true;
            this.scrollPosition = BeginScrollView(scrollPosition, alwaysShowHorizontal, alwaysShowVertical, horizontalScrollbar, verticalScrollbar, background, options);
        }

        internal ScrollViewScope(Vector2 scrollPosition, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, params GUILayoutOption[] options)
        {
            handleScrollWheel = true;
            this.scrollPosition = BeginScrollView(scrollPosition, alwaysShowHorizontal, alwaysShowVertical, horizontalScrollbar, verticalScrollbar, options);
        }

        protected override void CloseScope()
        {
            EndScrollView(handleScrollWheel);
        }
    }

    public static Vector2 BeginScrollView(Vector2 scrollPosition, params GUILayoutOption[] options)
    {
        return BeginScrollView(scrollPosition, false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.scrollView, options);
    }

    public static Vector2 BeginScrollView(Vector2 scrollPosition, bool alwaysShowHorizontal, bool alwaysShowVertical, params GUILayoutOption[] options)
    {
        return BeginScrollView(scrollPosition, alwaysShowHorizontal, alwaysShowVertical, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.scrollView, options);
    }

    public static Vector2 BeginScrollView(Vector2 scrollPosition, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, params GUILayoutOption[] options)
    {
        return BeginScrollView(scrollPosition, false, false, horizontalScrollbar, verticalScrollbar, GUI.skin.scrollView, options);
    }

    public static Vector2 BeginScrollView(Vector2 scrollPosition, GUIStyle style, params GUILayoutOption[] options)
    {
        string name = style.name;

        GUIStyle vertical = GUI.skin.FindStyle(name + "VerticalScrollbar") ?? GUI.skin.verticalScrollbar;
        GUIStyle horizontal = GUI.skin.FindStyle(name + "HorizontalScrollbar") ?? GUI.skin.horizontalScrollbar;
        return BeginScrollView(scrollPosition, false, false, horizontal, vertical, style, options);
    }

    internal static Vector2 BeginScrollView(Vector2 scrollPosition, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, params GUILayoutOption[] options)
    {
        return BeginScrollView(scrollPosition, alwaysShowHorizontal, alwaysShowVertical, horizontalScrollbar, verticalScrollbar, GUI.skin.scrollView, options);
    }

    // Begin an automatically layouted scrollview.
    public static Vector2 BeginScrollView(Vector2 scrollPosition, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, GUIStyle background, params GUILayoutOption[] options)
    {
        GUIScrollGroup g = (GUIScrollGroup)GUILayoutUtility.BeginLayoutGroup(background, null, typeof(GUIScrollGroup));
        if (Event.current.type == EventType.Layout)
        {
            g.resetCoords = true;
            g.isVertical = true;
            g.stretchWidth = 1;
            g.stretchHeight = 1;
            g.verticalScrollbar = verticalScrollbar;
            g.horizontalScrollbar = horizontalScrollbar;
            g.ApplyOptions(options);
        }
        return EditorGUIInternal.DoBeginScrollViewForward(g.rect, scrollPosition, new Rect(0, 0, g.clientWidth, g.clientHeight), alwaysShowHorizontal, alwaysShowVertical, horizontalScrollbar, verticalScrollbar, background);
    }

    internal class VerticalScrollViewScope : GUI.Scope
    {
        public Vector2 scrollPosition { get; protected set; }
        public bool handleScrollWheel { get; set; }

        public VerticalScrollViewScope(Vector2 scrollPosition, params GUILayoutOption[] options)
        {
            handleScrollWheel = true;
            this.scrollPosition = BeginVerticalScrollView(scrollPosition, options);
        }

        public VerticalScrollViewScope(Vector2 scrollPosition, bool alwaysShowVertical, GUIStyle verticalScrollbar, GUIStyle background, params GUILayoutOption[] options)
        {
            handleScrollWheel = true;
            this.scrollPosition = BeginVerticalScrollView(scrollPosition, alwaysShowVertical, verticalScrollbar, background, options);
        }

        protected override void CloseScope()
        {
            EndScrollView(handleScrollWheel);
        }
    }

    internal static Vector2 BeginVerticalScrollView(Vector2 scrollPosition, params GUILayoutOption[] options)
    {
        return BeginVerticalScrollView(scrollPosition, false, GUI.skin.verticalScrollbar, GUI.skin.scrollView, options);
    }

    // Begin an automatically layouted scrollview.
    internal static Vector2 BeginVerticalScrollView(Vector2 scrollPosition, bool alwaysShowVertical, GUIStyle verticalScrollbar, GUIStyle background, params GUILayoutOption[] options)
    {
        GUIScrollGroup g = (GUIScrollGroup)GUILayoutUtility.BeginLayoutGroup(background, null, typeof(GUIScrollGroup));
        if (Event.current.type == EventType.Layout)
        {
            g.resetCoords = true;
            g.isVertical = true;
            g.stretchWidth = 1;
            g.stretchHeight = 1;
            g.verticalScrollbar = verticalScrollbar;
            g.horizontalScrollbar = GUIStyle.none;
            g.allowHorizontalScroll = false;
            g.ApplyOptions(options);
        }
        return EditorGUIInternal.DoBeginScrollViewForward(g.rect, scrollPosition, new Rect(0, 0, g.clientWidth, g.clientHeight), false, alwaysShowVertical, GUI.skin.horizontalScrollbar, verticalScrollbar, background);
    }

    internal class HorizontalScrollViewScope : GUI.Scope
    {
        public Vector2 scrollPosition { get; protected set; }
        public bool handleScrollWheel { get; set; }

        public HorizontalScrollViewScope(Vector2 scrollPosition, params GUILayoutOption[] options)
        {
            handleScrollWheel = true;
            this.scrollPosition = BeginHorizontalScrollView(scrollPosition, options);
        }

        public HorizontalScrollViewScope(Vector2 scrollPosition, bool alwaysShowHorizontal, GUIStyle horizontalScrollbar, GUIStyle background, params GUILayoutOption[] options)
        {
            handleScrollWheel = true;
            this.scrollPosition = BeginHorizontalScrollView(scrollPosition, alwaysShowHorizontal, horizontalScrollbar, background, options);
        }

        protected override void CloseScope()
        {
            EndScrollView(handleScrollWheel);
        }
    }

    internal static Vector2 BeginHorizontalScrollView(Vector2 scrollPosition, params GUILayoutOption[] options)
    {
        return BeginHorizontalScrollView(scrollPosition, false, GUI.skin.horizontalScrollbar, GUI.skin.scrollView, options);
    }

    // Begin an automatically layouted scrollview.

    internal static Vector2 BeginHorizontalScrollView(Vector2 scrollPosition, bool alwaysShowHorizontal, GUIStyle horizontalScrollbar, GUIStyle background, params GUILayoutOption[] options)
    {
        GUIScrollGroup g = (GUIScrollGroup)GUILayoutUtility.BeginLayoutGroup(background, null, typeof(GUIScrollGroup));
        if (Event.current.type == EventType.Layout)
        {
            g.resetCoords = true;
            g.isVertical = true;
            g.stretchWidth = 1;
            g.stretchHeight = 1;
            g.verticalScrollbar = GUIStyle.none;
            g.horizontalScrollbar = horizontalScrollbar;
            g.allowHorizontalScroll = true;
            g.allowVerticalScroll = false;
            g.ApplyOptions(options);
        }
        return EditorGUIInternal.DoBeginScrollViewForward(g.rect, scrollPosition, new Rect(0, 0, g.clientWidth, g.clientHeight), alwaysShowHorizontal, false, horizontalScrollbar, GUI.skin.verticalScrollbar, background);
    }

    // Ends a scrollview started with a call to BeginScrollView.
    public static void EndScrollView()
    {
        GUILayout.EndScrollView(true);
    }

    internal static void EndScrollView(bool handleScrollWheel)
    {
        GUILayout.EndScrollView(handleScrollWheel);
    }

    public static bool PropertyField(SerializedProperty property, params GUILayoutOption[] options)
    {
        return PropertyField(property, null, IsChildrenIncluded(property), options);
    }

    public static bool PropertyField(SerializedProperty property, GUIContent label, params GUILayoutOption[] options)
    {
        return PropertyField(property, label, IsChildrenIncluded(property), options);
    }

    public static bool PropertyField(SerializedProperty property, bool includeChildren, params GUILayoutOption[] options)
    {
        return PropertyField(property, null, includeChildren, options);
    }

    // Make a field for [[SerializedProperty]].
    public static bool PropertyField(SerializedProperty property, GUIContent label, bool includeChildren, params GUILayoutOption[] options)
    {
        return ScriptAttributeUtility.GetHandler(property).OnGUILayout(property, label, includeChildren, options);
    }

    private static bool IsChildrenIncluded(SerializedProperty prop)
    {
        switch (prop.propertyType)
        {
            case SerializedPropertyType.Generic:
            case SerializedPropertyType.Vector4:
                return true;
            default:
                return false;
        }
    }

    public static Rect GetControlRect(params GUILayoutOption[] options)
    {
        return GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.layerMaskField, options);
    }

    public static Rect GetControlRect(bool hasLabel, params GUILayoutOption[] options)
    {
        return GetControlRect(hasLabel, EditorGUI.kSingleLineHeight, EditorStyles.layerMaskField, options);
    }

    public static Rect GetControlRect(bool hasLabel, float height, params GUILayoutOption[] options)
    {
        return GetControlRect(hasLabel, height, EditorStyles.layerMaskField, options);
    }

    public static Rect GetControlRect(bool hasLabel, float height, GUIStyle style, params GUILayoutOption[] options)
    {
        return GUILayoutUtility.GetRect(
            hasLabel ? kLabelFloatMinW : EditorGUIUtility.fieldWidth,
            kLabelFloatMaxW,
            height, height, style, options);
    }

    internal static Rect GetSliderRect(bool hasLabel, params GUILayoutOption[] options)
    {
        return GetSliderRect(hasLabel, GUI.skin.horizontalSlider, options);
    }

    internal static Rect GetSliderRect(bool hasLabel, GUIStyle sliderStyle, params GUILayoutOption[] options)
    {
        return GUILayoutUtility.GetRect(
            hasLabel ? kLabelFloatMinW : EditorGUIUtility.fieldWidth,
            kLabelFloatMaxW + EditorGUI.kSpacing + EditorGUI.kSliderMaxW,
            EditorGUI.kSingleLineHeight, EditorGUI.kSingleLineHeight, sliderStyle, options);
    }

    internal static Rect GetToggleRect(bool hasLabel, params GUILayoutOption[] options)
    {
        // Toggle is 14 pixels wide while float fields are EditorGUIUtility.fieldWidth pixels wide.
        // Store difference in variable and add to min and max width values used for float fields.
        float toggleAdjust = (14 - EditorGUIUtility.fieldWidth);
        return GUILayoutUtility.GetRect(
            hasLabel ? kLabelFloatMinW + toggleAdjust : EditorGUIUtility.fieldWidth + toggleAdjust,
            kLabelFloatMaxW + toggleAdjust,
            EditorGUI.kSingleLineHeight, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
    }

    public class FadeGroupScope : GUI.Scope
    {
        // when using the FadeGroupScope, make sure to only show the content when 'visible' is set to true,
        // otherwise only the hide animation will run, and then the content will be visible again.
        public bool visible { get; protected set; }

        public FadeGroupScope(float value)
        {
            visible = BeginFadeGroup(value);
        }

        protected override void CloseScope()
        {
            EndFadeGroup();
        }
    }

    public static bool BeginFadeGroup(float value)
    {
        GUILayoutFadeGroup g = (GUILayoutFadeGroup)GUILayoutUtility.BeginLayoutGroup(GUIStyle.none, null, typeof(GUILayoutFadeGroup));
        g.isVertical = true;
        g.resetCoords = false;
        g.fadeValue = value;
        g.wasGUIEnabled = GUI.enabled;
        g.guiColor = GUI.color;
        g.consideredForMargin = value > 0;

        // We don't want the fade group gui clip to be used for calculating the label width of controls in this fade group, so we lock the context width.
        EditorGUIUtility.LockContextWidth();

        if (value != 0.0f && value != 1.0f)
        {
            g.resetCoords = true;
            GUI.BeginGroup(g.rect);

            if (Event.current.type == EventType.MouseDown)
            {
                Event.current.Use();
            }
        }

        return value != 0;
    }

    public static void EndFadeGroup()
    {
        // If we're inside a fade group, end it here.
        GUILayoutFadeGroup g = EditorGUILayoutUtilityInternal.topLevel as GUILayoutFadeGroup;

        // If there are no more FadeGroups to end, display a warning.
        if (g == null)
        {
            Debug.LogWarning("Unexpected call to EndFadeGroup! Make sure to call EndFadeGroup the same number of times as BeginFadeGroup.");
            return;
        }

        if (g.fadeValue != 0.0f && g.fadeValue != 1.0f)
        {
            GUI.EndGroup();
        }

        EditorGUIUtility.UnlockContextWidth();
        GUI.enabled = g.wasGUIEnabled;
        GUI.color = g.guiColor;
        GUILayoutUtility.EndLayoutGroup();
    }

    public static BuildTargetGroup BeginBuildTargetSelectionGrouping()
    {
        BuildPlatform[] validPlatforms = BuildPlatforms.instance.GetValidPlatforms().ToArray();
        int selected = BeginPlatformGrouping(validPlatforms, null);
        return validPlatforms[selected].namedBuildTarget.ToBuildTargetGroup();
    }

    public static void EndBuildTargetSelectionGrouping()
    {
        EndPlatformGrouping();
    }

    internal static int BeginPlatformGrouping(BuildPlatform[] platforms, GUIContent defaultTab)
    {
        return BeginPlatformGrouping(platforms, defaultTab, EditorStyles.frameBox);
    }

    static Rect GetTabRect(Rect rect, int tabIndex, int tabCount, out GUIStyle tabStyle)
    {
        if (s_TabOnlyOne == null)
        {
            // Keep in sync with Tests/EditModeAndPlayModeTests/PlayerSettings/Assets/Editor/PlayerSettingsApplicationIdentifierTests.cs.
            s_TabOnlyOne = "Tab onlyOne";
            s_TabFirst = "Tab first";
            s_TabMiddle = "Tab middle";
            s_TabLast = "Tab last";
        }

        tabStyle = s_TabMiddle;

        if (tabCount == 1)
        {
            tabStyle = s_TabOnlyOne;
        }
        else if (tabIndex == 0)
        {
            tabStyle = s_TabFirst;
        }
        else if (tabIndex == (tabCount - 1))
        {
            tabStyle = s_TabLast;
        }

        float tabWidth = rect.width / tabCount;
        int left = Mathf.RoundToInt(tabIndex * tabWidth);
        int right = Mathf.RoundToInt((tabIndex + 1) * tabWidth);
        return new Rect(rect.x + left, rect.y, right - left, EditorGUI.kTabButtonHeight);
    }

    internal static int BeginPlatformGrouping(BuildPlatform[] platforms, GUIContent defaultTab, GUIStyle style)
    {
        return BeginPlatformGrouping(platforms, defaultTab, style, null);
    }

    internal static int BeginPlatformGrouping(BuildPlatform[] platforms, GUIContent defaultTab, GUIStyle style, Func<int, bool> showOverrideForPlatform)
    {
        int selectedPlatform = -1;
        for (int i = 0; i < platforms.Length; i++)
        {
            if (platforms[i].IsSelected())
            {
                selectedPlatform = i;
                break;
            }
        }
        if (selectedPlatform == -1)
        {
            s_SelectedDefault.value = true;
            selectedPlatform = 0;
        }

        int selected = defaultTab == null ? selectedPlatform : (s_SelectedDefault.value ? -1 : selectedPlatform);

        bool tempEnabled = GUI.enabled;
        GUI.enabled = true;
        EditorGUI.BeginChangeCheck();
        Rect r = BeginVertical(style);
        int platformCount = platforms.Length;
        int buttonCount = platformCount;
        int startIndex = 0;

        if (defaultTab != null)
        {
            buttonCount++;
            startIndex = -1;
        }

        int buttonIndex = 0;
        for (int i = startIndex; i < platformCount; i++, buttonIndex++)
        {
            GUIContent content = GUIContent.none;

            if (i == -1)
            {
                content = defaultTab;
            }
            else
            {
                content = new GUIContent(platforms[i].smallIcon, platforms[i].tooltip);
            }

            GUIStyle buttonStyle = null;
            Rect buttonRect = GetTabRect(r, buttonIndex, buttonCount, out buttonStyle);

            if (GUI.Toggle(buttonRect, selected == i, content, buttonStyle))
                selected = i;
            if (showOverrideForPlatform != null)
            {
                if (showOverrideForPlatform(i))
                {
                    var prevMargin = EditorGUIUtility.leftMarginCoord;
                    var overrideRect = buttonRect;
                    const int margin = 3;
                    overrideRect.y += margin;
                    overrideRect.height -= margin * 2;
                    EditorGUIUtility.leftMarginCoord = overrideRect.x + margin;
                    EditorGUI.DrawOverrideBackgroundApplicable(overrideRect);
                    EditorGUIUtility.leftMarginCoord = prevMargin;
                }
            }
        }

        // GUILayout.Space doesn't expand to available width, so use GetRect instead
        GUILayoutUtility.GetRect(10, EditorGUI.kTabButtonHeight);

        GUI.enabled = tempEnabled;

        // Important that we only actually set the selectedBuildTargetGroup if the user clicked the button.
        // If the current selectedBuildTargetGroup is one that is not among the tabs (because the build target
        // is not supported), then this should not be changed unless the user explicitly does so.
        // Otherwise, if the build window is open at the same time, the unsupported build target groups will
        // not be selectable in the build window.
        if (EditorGUI.EndChangeCheck())
        {
            if (defaultTab == null)
            {
                platforms[selected].Select();
            }
            else
            {
                if (selected < 0)
                {
                    s_SelectedDefault.value = true;
                }
                else
                {
                    platforms[selected].Select();
                    s_SelectedDefault.value = false;
                }
            }

            // Repaint build window, if open.
            Object[] buildWindows = Resources.FindObjectsOfTypeAll(typeof(BuildPlayerWindow));
            foreach (Object t in buildWindows)
            {
                BuildPlayerWindow buildWindow = t as BuildPlayerWindow;
                if (buildWindow != null)
                    buildWindow.Repaint();
            }
        }

        return selected;
    }

    internal static void EndPlatformGrouping()
    {
        EndVertical();
    }

    internal static void MultiSelectionObjectTitleBar(Object[] objects)
    {
        string text = objects[0].name + " (" + ObjectNames.NicifyVariableName(ObjectNames.GetTypeName(objects[0])) + ")";
        if (objects.Length > 1)
        {
            text += " and " + (objects.Length - 1) + " other" + (objects.Length > 2 ? "s" : "");
        }
        GUILayoutOption[] options = {  GUILayout.Height(16f) };
        GUILayout.Label(EditorGUIUtility.TempContent(text, AssetPreview.GetMiniThumbnail(objects[0])), EditorStyles.boldLabel, options);
    }

    // Returns true if specified bit is true for all targets
    internal static bool BitToggleField(string label, SerializedProperty bitFieldProperty, int flag)
    {
        bool toggle = (bitFieldProperty.intValue & flag) != 0;
        bool different = (bitFieldProperty.hasMultipleDifferentValuesBitwise & flag) != 0;
        EditorGUI.showMixedValue = different;
        EditorGUI.BeginChangeCheck();
        toggle = Toggle(label, toggle);
        if (EditorGUI.EndChangeCheck())
        {
            // If toggle has mixed values, always set all to true when clicking it
            if (different)
            {
                toggle = true;
            }
            different = false;
            int bitIndex = -1;
            for (int i = 0; i < 32; i++)
            {
                if (((1 << i) & flag) != 0)
                {
                    bitIndex = i;
                    break;
                }
            }
            bitFieldProperty.SetBitAtIndexForAllTargetsImmediate(bitIndex, toggle);
        }
        EditorGUI.showMixedValue = false;
        return toggle && !different;
    }

    internal static void SortingLayerField(GUIContent label, SerializedProperty layerID, GUIStyle style)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style);
        EditorGUI.SortingLayerField(r, label, layerID, style, EditorStyles.label);
    }

    internal static string TextFieldDropDown(string text, string[] dropDownElement)
    {
        return TextFieldDropDown(GUIContent.none, text, dropDownElement);
    }

    internal static string TextFieldDropDown(GUIContent label, string text, string[] dropDownElement)
    {
        Rect rect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.textField);
        return EditorGUI.TextFieldDropDown(rect, label, text, dropDownElement);
    }

    internal static string DelayedTextFieldDropDown(string text, string[] dropDownElement)
    {
        return DelayedTextFieldDropDown(GUIContent.none, text, dropDownElement);
    }

    internal static string DelayedTextFieldDropDown(GUIContent label, string text, string[] dropDownElement)
    {
        Rect rect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.textFieldDropDownText);
        return EditorGUI.DelayedTextFieldDropDown(rect, label, text, dropDownElement);
    }

    // A button that returns true on mouse down - like a popup button
    public static bool DropdownButton(GUIContent content, FocusType focusType, params GUILayoutOption[] options)
    {
        return DropdownButton(content, focusType, "MiniPullDown", options);
    }

    // A button that returns true on mouse down - like a popup button
    public static bool DropdownButton(GUIContent content, FocusType focusType, GUIStyle style, params GUILayoutOption[] options)
    {
        s_LastRect = GUILayoutUtility.GetRect(content, style, options);
        return EditorGUI.DropdownButton(s_LastRect, content, focusType, style);
    }

    // A toggle that returns true on mouse down - like a popup button and returns true if checked
    internal static bool DropDownToggle(ref bool toggled, GUIContent content, GUIStyle toggleStyle)
    {
        GUIStyle buttonStyle = GUIStyle.none;

        // This is to be compatible with existing code
        if (toggleStyle == EditorStyles.toolbarDropDownToggle || toggleStyle == EditorStyles.toolbarDropDownToggleRight)
            buttonStyle = EditorStyles.toolbarDropDownToggleButton;

        return DropDownToggle(ref toggled, content, toggleStyle, buttonStyle);
    }

    internal static bool DropDownToggle(ref bool toggled, GUIContent content, GUIStyle toggleStyle, GUIStyle toggleDropdownButtonStyle)
    {
        Rect toggleRect = GUILayoutUtility.GetRect(content, toggleStyle);
        Rect arrowRightRect = Rect.zero;

        if (toggleDropdownButtonStyle != null)
        {
            arrowRightRect = new Rect(toggleRect.xMax - toggleDropdownButtonStyle.fixedWidth - toggleDropdownButtonStyle.margin.right, toggleRect.y, toggleDropdownButtonStyle.fixedWidth, toggleRect.height);
        }
        else
        {
            arrowRightRect = new Rect(toggleRect.xMax - toggleStyle.padding.right, toggleRect.y, toggleStyle.padding.right, toggleRect.height);
        }


        int dropdownButtonId = GUIUtility.GetControlID(EditorGUI.s_DropdownButtonHash, FocusType.Passive, arrowRightRect);
        bool clicked = EditorGUI.DropdownButton(dropdownButtonId, arrowRightRect, GUIContent.none, GUIStyle.none);

        if (!clicked)
        {
            toggled = GUI.Toggle(toggleRect, toggled, content, toggleStyle);
        }

        // Ensure that the dropdown button is rendered on top of the toggle
        if (Event.current.type == EventType.Repaint && toggleDropdownButtonStyle != null && toggleDropdownButtonStyle != GUIStyle.none)
        {
            EditorGUI.DropdownButton(dropdownButtonId, arrowRightRect, GUIContent.none, toggleDropdownButtonStyle);
        }

        return clicked;
    }

    internal static int AdvancedPopup(int selectedIndex, string[] displayedOptions, params GUILayoutOption[] options)
    {
        return AdvancedPopup(selectedIndex, displayedOptions, "MiniPullDown", options);
    }

    internal static int AdvancedPopup(int selectedIndex, string[] displayedOptions, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.AdvancedPopup(r, selectedIndex, displayedOptions, style);
    }

    internal static int AdvancedLazyPopup(string displayedOption, int selectedIndex, Func<Tuple<int, string[]>> displayedOptionsFunc, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
        return EditorGUI.AdvancedLazyPopup(r, displayedOption, selectedIndex, displayedOptionsFunc, style);
    }

    [Obsolete("(UnityUpgradable) -> UnityEditor.HyperLinkClickedEventArgs", true)]
    internal class HyperLinkClickedEventArgs
    {
        [Obsolete("(UnityUpgradable) -> UnityEditor.HyperLinkClickedEventArgs.hyperLinkData", true)]
        public Dictionary<string, string> hyperlinkInfos { get; private set; }
        internal HyperLinkClickedEventArgs(Dictionary<string, string> hyperLinkData) {}
    }
}
