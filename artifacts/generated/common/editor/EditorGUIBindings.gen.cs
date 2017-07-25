// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngineInternal;

namespace UnityEditor
{


public sealed partial class EditorGUI
{
    [uei.ExcludeFromDocs]
public static void LabelField (Rect position, string label) {
    GUIStyle style = EditorStyles.label;
    LabelField ( position, label, style );
}

public static void LabelField(Rect position, string label, [uei.DefaultValue("EditorStyles.label")]  GUIStyle style )
        { LabelField(position, GUIContent.none, EditorGUIUtility.TempContent(label), style); }

    [uei.ExcludeFromDocs]
public static void LabelField (Rect position, GUIContent label) {
    GUIStyle style = EditorStyles.label;
    LabelField ( position, label, style );
}

public static void LabelField(Rect position, GUIContent label, [uei.DefaultValue("EditorStyles.label")]  GUIStyle style )
        { LabelField(position, GUIContent.none, label, style); }

    [uei.ExcludeFromDocs]
public static void LabelField (Rect position, string label, string label2) {
    GUIStyle style = EditorStyles.label;
    LabelField ( position, label, label2, style );
}

public static void LabelField(Rect position, string label, string label2, [uei.DefaultValue("EditorStyles.label")]  GUIStyle style )
        { LabelField(position, new GUIContent(label), EditorGUIUtility.TempContent(label2), style); }

    
    
    [uei.ExcludeFromDocs]
public static void LabelField (Rect position, GUIContent label, GUIContent label2) {
    GUIStyle style = EditorStyles.label;
    LabelField ( position, label, label2, style );
}

public static void LabelField(Rect position, GUIContent label, GUIContent label2, [uei.DefaultValue("EditorStyles.label")]  GUIStyle style )
        {
            LabelFieldInternal(position, label, label2, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static bool ToggleLeft (Rect position, string label, bool value) {
    GUIStyle labelStyle = EditorStyles.label;
    return ToggleLeft ( position, label, value, labelStyle );
}

public static bool ToggleLeft(Rect position, string label, bool value, [uei.DefaultValue("EditorStyles.label")]  GUIStyle labelStyle )
        { return ToggleLeft(position, EditorGUIUtility.TempContent(label), value, labelStyle); }

    
    [uei.ExcludeFromDocs]
public static bool ToggleLeft (Rect position, GUIContent label, bool value) {
    GUIStyle labelStyle = EditorStyles.label;
    return ToggleLeft ( position, label, value, labelStyle );
}

public static bool ToggleLeft(Rect position, GUIContent label, bool value, [uei.DefaultValue("EditorStyles.label")]  GUIStyle labelStyle )
        {
            return ToggleLeftInternal(position, label, value, labelStyle);
        }

    
    
    [uei.ExcludeFromDocs]
public static string TextField (Rect position, string text) {
    GUIStyle style = EditorStyles.textField;
    return TextField ( position, text, style );
}

public static string TextField(Rect position, string text, [uei.DefaultValue("EditorStyles.textField")]  GUIStyle style )
        {
            return TextFieldInternal(position, text, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static string TextField (Rect position, string label, string text) {
    GUIStyle style = EditorStyles.textField;
    return TextField ( position, label, text, style );
}

public static string TextField(Rect position, string label, string text, [uei.DefaultValue("EditorStyles.textField")]  GUIStyle style )
        {
            return TextField(position, EditorGUIUtility.TempContent(label), text, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static string TextField (Rect position, GUIContent label, string text) {
    GUIStyle style = EditorStyles.textField;
    return TextField ( position, label, text, style );
}

public static string TextField(Rect position, GUIContent label, string text, [uei.DefaultValue("EditorStyles.textField")]  GUIStyle style )
        {
            return TextFieldInternal(position, label, text, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static string DelayedTextField (Rect position, string text) {
    GUIStyle style = EditorStyles.textField;
    return DelayedTextField ( position, text, style );
}

public static string DelayedTextField(Rect position, string text, [uei.DefaultValue("EditorStyles.textField")]  GUIStyle style )
        {
            return DelayedTextField(position, GUIContent.none, text, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static string DelayedTextField (Rect position, string label, string text) {
    GUIStyle style = EditorStyles.textField;
    return DelayedTextField ( position, label, text, style );
}

public static string DelayedTextField(Rect position, string label, string text, [uei.DefaultValue("EditorStyles.textField")]  GUIStyle style )
        {
            return DelayedTextField(position, EditorGUIUtility.TempContent(label), text, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static string DelayedTextField (Rect position, GUIContent label, string text) {
    GUIStyle style = EditorStyles.textField;
    return DelayedTextField ( position, label, text, style );
}

public static string DelayedTextField(Rect position, GUIContent label, string text, [uei.DefaultValue("EditorStyles.textField")]  GUIStyle style )
        {
            int id = GUIUtility.GetControlID(s_TextFieldHash, FocusType.Keyboard, position);
            return DelayedTextFieldInternal(position, id, label, text, null, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static void DelayedTextField (Rect position, SerializedProperty property) {
    GUIContent label = null;
    DelayedTextField ( position, property, label );
}

public static void DelayedTextField(Rect position, SerializedProperty property, [uei.DefaultValue("null")]  GUIContent label )
        {
            int id = GUIUtility.GetControlID(s_TextFieldHash, FocusType.Keyboard, position);
            DelayedTextFieldInternal(position, id, property, null, label);
        }

    
    
    [uei.ExcludeFromDocs]
public static string DelayedTextField (Rect position, GUIContent label, int controlId, string text) {
    GUIStyle style = EditorStyles.textField;
    return DelayedTextField ( position, label, controlId, text, style );
}

public static string DelayedTextField(Rect position, GUIContent label, int controlId, string text, [uei.DefaultValue("EditorStyles.textField")]  GUIStyle style )
        {
            return DelayedTextFieldInternal(position, controlId, label, text, null, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static string TextArea (Rect position, string text) {
    GUIStyle style = EditorStyles.textField;
    return TextArea ( position, text, style );
}

public static string TextArea(Rect position, string text, [uei.DefaultValue("EditorStyles.textField")]  GUIStyle style )
        {
            return TextAreaInternal(position, text, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static void SelectableLabel (Rect position, string text) {
    GUIStyle style = EditorStyles.label;
    SelectableLabel ( position, text, style );
}

public static void SelectableLabel(Rect position, string text, [uei.DefaultValue("EditorStyles.label")]  GUIStyle style )
        {
            SelectableLabelInternal(position, text, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static string PasswordField (Rect position, string password) {
    GUIStyle style = EditorStyles.textField;
    return PasswordField ( position, password, style );
}

public static string PasswordField(Rect position, string password, [uei.DefaultValue("EditorStyles.textField")]  GUIStyle style )
        {
            return PasswordFieldInternal(position, password, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static string PasswordField (Rect position, string label, string password) {
    GUIStyle style = EditorStyles.textField;
    return PasswordField ( position, label, password, style );
}

public static string PasswordField(Rect position, string label, string password, [uei.DefaultValue("EditorStyles.textField")]  GUIStyle style )
        { return PasswordField(position, EditorGUIUtility.TempContent(label), password, style); }

    
    
    [uei.ExcludeFromDocs]
public static string PasswordField (Rect position, GUIContent label, string password) {
    GUIStyle style = EditorStyles.textField;
    return PasswordField ( position, label, password, style );
}

public static string PasswordField(Rect position, GUIContent label, string password, [uei.DefaultValue("EditorStyles.textField")]  GUIStyle style )
        {
            return PasswordFieldInternal(position, label, password, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static float FloatField (Rect position, float value) {
    GUIStyle style = EditorStyles.numberField;
    return FloatField ( position, value, style );
}

public static float FloatField(Rect position, float value, [uei.DefaultValue("EditorStyles.numberField")]  GUIStyle style )
        {
            return FloatFieldInternal(position, value, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static float FloatField (Rect position, string label, float value) {
    GUIStyle style = EditorStyles.numberField;
    return FloatField ( position, label, value, style );
}

public static float FloatField(Rect position, string label, float value, [uei.DefaultValue("EditorStyles.numberField")]  GUIStyle style )
        {
            return FloatField(position, EditorGUIUtility.TempContent(label), value, style);
        }

    
    [uei.ExcludeFromDocs]
public static float FloatField (Rect position, GUIContent label, float value) {
    GUIStyle style = EditorStyles.numberField;
    return FloatField ( position, label, value, style );
}

public static float FloatField(Rect position, GUIContent label, float value, [uei.DefaultValue("EditorStyles.numberField")]  GUIStyle style )
        {
            return FloatFieldInternal(position, label, value, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static float DelayedFloatField (Rect position, float value) {
    GUIStyle style = EditorStyles.numberField;
    return DelayedFloatField ( position, value, style );
}

public static float DelayedFloatField(Rect position, float value, [uei.DefaultValue("EditorStyles.numberField")]  GUIStyle style )
        {
            return DelayedFloatField(position, GUIContent.none, value, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static float DelayedFloatField (Rect position, string label, float value) {
    GUIStyle style = EditorStyles.numberField;
    return DelayedFloatField ( position, label, value, style );
}

public static float DelayedFloatField(Rect position, string label, float value, [uei.DefaultValue("EditorStyles.numberField")]  GUIStyle style )
        {
            return DelayedFloatField(position, EditorGUIUtility.TempContent(label), value, style);
        }

    
    [uei.ExcludeFromDocs]
public static float DelayedFloatField (Rect position, GUIContent label, float value) {
    GUIStyle style = EditorStyles.numberField;
    return DelayedFloatField ( position, label, value, style );
}

public static float DelayedFloatField(Rect position, GUIContent label, float value, [uei.DefaultValue("EditorStyles.numberField")]  GUIStyle style )
        {
            return DelayedFloatFieldInternal(position, label, value, style);
        }

    
    [uei.ExcludeFromDocs]
public static void DelayedFloatField (Rect position, SerializedProperty property) {
    GUIContent label = null;
    DelayedFloatField ( position, property, label );
}

public static void DelayedFloatField(Rect position, SerializedProperty property, [uei.DefaultValue("null")]  GUIContent label )
        {
            DelayedFloatFieldInternal(position, property, label);
        }

    
    
    [uei.ExcludeFromDocs]
public static double DoubleField (Rect position, double value) {
    GUIStyle style = EditorStyles.numberField;
    return DoubleField ( position, value, style );
}

public static double DoubleField(Rect position, double value, [uei.DefaultValue("EditorStyles.numberField")]  GUIStyle style )
        {
            return DoubleFieldInternal(position, value, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static double DoubleField (Rect position, string label, double value) {
    GUIStyle style = EditorStyles.numberField;
    return DoubleField ( position, label, value, style );
}

public static double DoubleField(Rect position, string label, double value, [uei.DefaultValue("EditorStyles.numberField")]  GUIStyle style )
        {
            return DoubleField(position, EditorGUIUtility.TempContent(label), value, style);
        }

    
    [uei.ExcludeFromDocs]
public static double DoubleField (Rect position, GUIContent label, double value) {
    GUIStyle style = EditorStyles.numberField;
    return DoubleField ( position, label, value, style );
}

public static double DoubleField(Rect position, GUIContent label, double value, [uei.DefaultValue("EditorStyles.numberField")]  GUIStyle style )
        {
            return DoubleFieldInternal(position, label, value, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static double DelayedDoubleField (Rect position, double value) {
    GUIStyle style = EditorStyles.numberField;
    return DelayedDoubleField ( position, value, style );
}

public static double DelayedDoubleField(Rect position, double value, [uei.DefaultValue("EditorStyles.numberField")]  GUIStyle style )
        {
            return DelayedDoubleFieldInternal(position, null, value, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static double DelayedDoubleField (Rect position, string label, double value) {
    GUIStyle style = EditorStyles.numberField;
    return DelayedDoubleField ( position, label, value, style );
}

public static double DelayedDoubleField(Rect position, string label, double value, [uei.DefaultValue("EditorStyles.numberField")]  GUIStyle style )
        {
            return DelayedDoubleField(position, EditorGUIUtility.TempContent(label), value, style);
        }

    
    [uei.ExcludeFromDocs]
public static double DelayedDoubleField (Rect position, GUIContent label, double value) {
    GUIStyle style = EditorStyles.numberField;
    return DelayedDoubleField ( position, label, value, style );
}

public static double DelayedDoubleField(Rect position, GUIContent label, double value, [uei.DefaultValue("EditorStyles.numberField")]  GUIStyle style )
        {
            return DelayedDoubleFieldInternal(position, label, value, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static int IntField (Rect position, int value) {
    GUIStyle style = EditorStyles.numberField;
    return IntField ( position, value, style );
}

public static int IntField(Rect position, int value, [uei.DefaultValue("EditorStyles.numberField")]  GUIStyle style )
        {
            return IntFieldInternal(position, value, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static int IntField (Rect position, string label, int value) {
    GUIStyle style = EditorStyles.numberField;
    return IntField ( position, label, value, style );
}

public static int IntField(Rect position, string label, int value, [uei.DefaultValue("EditorStyles.numberField")]  GUIStyle style )
        { return IntField(position, EditorGUIUtility.TempContent(label), value, style); }

    
    [uei.ExcludeFromDocs]
public static int IntField (Rect position, GUIContent label, int value) {
    GUIStyle style = EditorStyles.numberField;
    return IntField ( position, label, value, style );
}

public static int IntField(Rect position, GUIContent label, int value, [uei.DefaultValue("EditorStyles.numberField")]  GUIStyle style )
        {
            return IntFieldInternal(position, label, value, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static int DelayedIntField (Rect position, int value) {
    GUIStyle style = EditorStyles.numberField;
    return DelayedIntField ( position, value, style );
}

public static int DelayedIntField(Rect position, int value, [uei.DefaultValue("EditorStyles.numberField")]  GUIStyle style )
        {
            return DelayedIntField(position, GUIContent.none, value, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static int DelayedIntField (Rect position, string label, int value) {
    GUIStyle style = EditorStyles.numberField;
    return DelayedIntField ( position, label, value, style );
}

public static int DelayedIntField(Rect position, string label, int value, [uei.DefaultValue("EditorStyles.numberField")]  GUIStyle style )
        {
            return DelayedIntField(position, EditorGUIUtility.TempContent(label), value, style);
        }

    
    [uei.ExcludeFromDocs]
public static int DelayedIntField (Rect position, GUIContent label, int value) {
    GUIStyle style = EditorStyles.numberField;
    return DelayedIntField ( position, label, value, style );
}

public static int DelayedIntField(Rect position, GUIContent label, int value, [uei.DefaultValue("EditorStyles.numberField")]  GUIStyle style )
        {
            return DelayedIntFieldInternal(position, label, value, style);
        }

    
    [uei.ExcludeFromDocs]
public static void DelayedIntField (Rect position, SerializedProperty property) {
    GUIContent label = null;
    DelayedIntField ( position, property, label );
}

public static void DelayedIntField(Rect position, SerializedProperty property, [uei.DefaultValue("null")]  GUIContent label )
        {
            DelayedIntFieldInternal(position, property, label);
        }

    
    
    [uei.ExcludeFromDocs]
public static long LongField (Rect position, long value) {
    GUIStyle style = EditorStyles.numberField;
    return LongField ( position, value, style );
}

public static long LongField(Rect position, long value, [uei.DefaultValue("EditorStyles.numberField")]  GUIStyle style )
        {
            return LongFieldInternal(position, value, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static long LongField (Rect position, string label, long value) {
    GUIStyle style = EditorStyles.numberField;
    return LongField ( position, label, value, style );
}

public static long LongField(Rect position, string label, long value, [uei.DefaultValue("EditorStyles.numberField")]  GUIStyle style )
        { return LongField(position, EditorGUIUtility.TempContent(label), value, style); }

    
    [uei.ExcludeFromDocs]
public static long LongField (Rect position, GUIContent label, long value) {
    GUIStyle style = EditorStyles.numberField;
    return LongField ( position, label, value, style );
}

public static long LongField(Rect position, GUIContent label, long value, [uei.DefaultValue("EditorStyles.numberField")]  GUIStyle style )
        {
            return LongFieldInternal(position, label, value, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static int Popup (Rect position, int selectedIndex, string[] displayedOptions) {
    GUIStyle style = EditorStyles.popup;
    return Popup ( position, selectedIndex, displayedOptions, style );
}

public static int Popup(Rect position, int selectedIndex, string[] displayedOptions, [uei.DefaultValue("EditorStyles.popup")]  GUIStyle style )
        { return DoPopup(IndentedRect(position), GUIUtility.GetControlID(s_PopupHash, FocusType.Keyboard, position), selectedIndex, EditorGUIUtility.TempContent(displayedOptions), style);  }

    [uei.ExcludeFromDocs]
public static int Popup (Rect position, int selectedIndex, GUIContent[] displayedOptions) {
    GUIStyle style = EditorStyles.popup;
    return Popup ( position, selectedIndex, displayedOptions, style );
}

public static int Popup(Rect position, int selectedIndex, GUIContent[] displayedOptions, [uei.DefaultValue("EditorStyles.popup")]  GUIStyle style )
        { return DoPopup(IndentedRect(position), GUIUtility.GetControlID(s_PopupHash, FocusType.Keyboard, position), selectedIndex, displayedOptions, style); }

    [uei.ExcludeFromDocs]
public static int Popup (Rect position, string label, int selectedIndex, string[] displayedOptions) {
    GUIStyle style = EditorStyles.popup;
    return Popup ( position, label, selectedIndex, displayedOptions, style );
}

public static int Popup(Rect position, string label, int selectedIndex, string[] displayedOptions, [uei.DefaultValue("EditorStyles.popup")]  GUIStyle style )
        { return PopupInternal(position, EditorGUIUtility.TempContent(label), selectedIndex, EditorGUIUtility.TempContent(displayedOptions), style); }

    [uei.ExcludeFromDocs]
public static int Popup (Rect position, GUIContent label, int selectedIndex, GUIContent[] displayedOptions) {
    GUIStyle style = EditorStyles.popup;
    return Popup ( position, label, selectedIndex, displayedOptions, style );
}

public static int Popup(Rect position, GUIContent label, int selectedIndex, GUIContent[] displayedOptions, [uei.DefaultValue("EditorStyles.popup")]  GUIStyle style )
        {
            return PopupInternal(position, label, selectedIndex, displayedOptions, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static System.Enum EnumPopup (Rect position, System.Enum selected) {
    GUIStyle style = EditorStyles.popup;
    return EnumPopup ( position, selected, style );
}

public static System.Enum EnumPopup(Rect position, System.Enum selected, [uei.DefaultValue("EditorStyles.popup")]  GUIStyle style )
        {
            return EnumPopup(position, GUIContent.none, selected, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static System.Enum EnumPopup (Rect position, string label, System.Enum selected) {
    GUIStyle style = EditorStyles.popup;
    return EnumPopup ( position, label, selected, style );
}

public static System.Enum EnumPopup(Rect position, string label, System.Enum selected, [uei.DefaultValue("EditorStyles.popup")]  GUIStyle style )
        {
            return EnumPopup(position, EditorGUIUtility.TempContent(label), selected, style);
        }

    
    [uei.ExcludeFromDocs]
public static System.Enum EnumPopup (Rect position, GUIContent label, System.Enum selected) {
    GUIStyle style = EditorStyles.popup;
    return EnumPopup ( position, label, selected, style );
}

public static System.Enum EnumPopup(Rect position, GUIContent label, System.Enum selected, [uei.DefaultValue("EditorStyles.popup")]  GUIStyle style )
        {
            return EnumPopupInternal(position, label, selected, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static int IntPopup (Rect position, int selectedValue, string[] displayedOptions, int[] optionValues) {
    GUIStyle style = EditorStyles.popup;
    return IntPopup ( position, selectedValue, displayedOptions, optionValues, style );
}

public static int IntPopup(Rect position, int selectedValue, string[] displayedOptions, int[] optionValues, [uei.DefaultValue("EditorStyles.popup")]  GUIStyle style )
        { return IntPopup(position, GUIContent.none, selectedValue, EditorGUIUtility.TempContent(displayedOptions), optionValues, style); }

    
    [uei.ExcludeFromDocs]
public static int IntPopup (Rect position, int selectedValue, GUIContent[] displayedOptions, int[] optionValues) {
    GUIStyle style = EditorStyles.popup;
    return IntPopup ( position, selectedValue, displayedOptions, optionValues, style );
}

public static int IntPopup(Rect position, int selectedValue, GUIContent[] displayedOptions, int[] optionValues, [uei.DefaultValue("EditorStyles.popup")]  GUIStyle style )
        { return IntPopup(position, GUIContent.none, selectedValue, displayedOptions, optionValues, style); }

    
    
    [uei.ExcludeFromDocs]
public static int IntPopup (Rect position, GUIContent label, int selectedValue, GUIContent[] displayedOptions, int[] optionValues) {
    GUIStyle style = EditorStyles.popup;
    return IntPopup ( position, label, selectedValue, displayedOptions, optionValues, style );
}

public static int IntPopup(Rect position, GUIContent label, int selectedValue, GUIContent[] displayedOptions, int[] optionValues, [uei.DefaultValue("EditorStyles.popup")]  GUIStyle style )
        {
            return IntPopupInternal(position, label, selectedValue, displayedOptions, optionValues, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static void IntPopup (Rect position, SerializedProperty property, GUIContent[] displayedOptions, int[] optionValues) {
    GUIContent label = null;
    IntPopup ( position, property, displayedOptions, optionValues, label );
}

public static void IntPopup(Rect position, SerializedProperty property, GUIContent[] displayedOptions, int[] optionValues, [uei.DefaultValue("null")]  GUIContent label )
        {
            IntPopupInternal(position, property, displayedOptions, optionValues, label);
        }

    
    
    [uei.ExcludeFromDocs]
public static int IntPopup (Rect position, string label, int selectedValue, string[] displayedOptions, int[] optionValues) {
    GUIStyle style = EditorStyles.popup;
    return IntPopup ( position, label, selectedValue, displayedOptions, optionValues, style );
}

public static int IntPopup(Rect position, string label, int selectedValue, string[] displayedOptions, int[] optionValues, [uei.DefaultValue("EditorStyles.popup")]  GUIStyle style )
        { return IntPopupInternal(position, EditorGUIUtility.TempContent(label), selectedValue, EditorGUIUtility.TempContent(displayedOptions), optionValues, style); }

    
    
    
    [uei.ExcludeFromDocs]
public static string TagField (Rect position, string tag) {
    GUIStyle style = EditorStyles.popup;
    return TagField ( position, tag, style );
}

public static string TagField(Rect position, string tag, [uei.DefaultValue("EditorStyles.popup")]  GUIStyle style )
        {
            return TagFieldInternal(position, EditorGUIUtility.TempContent(string.Empty), tag, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static string TagField (Rect position, string label, string tag) {
    GUIStyle style = EditorStyles.popup;
    return TagField ( position, label, tag, style );
}

public static string TagField(Rect position, string label, string tag, [uei.DefaultValue("EditorStyles.popup")]  GUIStyle style )
        { return TagFieldInternal(position, EditorGUIUtility.TempContent(label), tag, style); }

    [uei.ExcludeFromDocs]
public static string TagField (Rect position, GUIContent label, string tag) {
    GUIStyle style = EditorStyles.popup;
    return TagField ( position, label, tag, style );
}

public static string TagField(Rect position, GUIContent label, string tag, [uei.DefaultValue("EditorStyles.popup")]  GUIStyle style )
        {
            return TagFieldInternal(position, label, tag, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static int LayerField (Rect position, int layer) {
    GUIStyle style = EditorStyles.popup;
    return LayerField ( position, layer, style );
}

public static int LayerField(Rect position, int layer, [uei.DefaultValue("EditorStyles.popup")]  GUIStyle style )
        { return LayerFieldInternal(position, GUIContent.none, layer, style); }

    
    
    [uei.ExcludeFromDocs]
public static int LayerField (Rect position, string label, int layer) {
    GUIStyle style = EditorStyles.popup;
    return LayerField ( position, label, layer, style );
}

public static int LayerField(Rect position, string label, int layer, [uei.DefaultValue("EditorStyles.popup")]  GUIStyle style )
        { return LayerFieldInternal(position, EditorGUIUtility.TempContent(label), layer, style); }

    [uei.ExcludeFromDocs]
public static int LayerField (Rect position, GUIContent label, int layer) {
    GUIStyle style = EditorStyles.popup;
    return LayerField ( position, label, layer, style );
}

public static int LayerField(Rect position, GUIContent label, int layer, [uei.DefaultValue("EditorStyles.popup")]  GUIStyle style )
        {
            return LayerFieldInternal(position, label, layer, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static int MaskField (Rect position, GUIContent label, int mask, string[] displayedOptions) {
    GUIStyle style = EditorStyles.popup;
    return MaskField ( position, label, mask, displayedOptions, style );
}

public static int MaskField(Rect position, GUIContent label, int mask, string[] displayedOptions, [uei.DefaultValue("EditorStyles.popup")]  GUIStyle style )
        {
            return MaskFieldInternal(position, label, mask, displayedOptions, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static int MaskField (Rect position, string label, int mask, string[] displayedOptions) {
    GUIStyle style = EditorStyles.popup;
    return MaskField ( position, label, mask, displayedOptions, style );
}

public static int MaskField(Rect position, string label, int mask, string[] displayedOptions, [uei.DefaultValue("EditorStyles.popup")]  GUIStyle style )
        {
            return MaskFieldInternal(position, GUIContent.Temp(label), mask, displayedOptions, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static int MaskField (Rect position, int mask, string[] displayedOptions) {
    GUIStyle style = EditorStyles.popup;
    return MaskField ( position, mask, displayedOptions, style );
}

public static int MaskField(Rect position, int mask, string[] displayedOptions, [uei.DefaultValue("EditorStyles.popup")]  GUIStyle style )
        {
            return MaskFieldInternal(position, mask, displayedOptions, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static bool Foldout (Rect position, bool foldout, string content) {
    GUIStyle style = EditorStyles.foldout;
    return Foldout ( position, foldout, content, style );
}

public static bool Foldout(Rect position, bool foldout, string content, [uei.DefaultValue("EditorStyles.foldout")]  GUIStyle style )
        { return FoldoutInternal(position, foldout, EditorGUIUtility.TempContent(content), false, style); }

    [uei.ExcludeFromDocs]
public static bool Foldout (Rect position, bool foldout, string content, bool toggleOnLabelClick) {
    GUIStyle style = EditorStyles.foldout;
    return Foldout ( position, foldout, content, toggleOnLabelClick, style );
}

public static bool Foldout(Rect position, bool foldout, string content, bool toggleOnLabelClick, [uei.DefaultValue("EditorStyles.foldout")]  GUIStyle style )
        { return FoldoutInternal(position, foldout, EditorGUIUtility.TempContent(content), toggleOnLabelClick, style); }

    [uei.ExcludeFromDocs]
public static bool Foldout (Rect position, bool foldout, GUIContent content) {
    GUIStyle style = EditorStyles.foldout;
    return Foldout ( position, foldout, content, style );
}

public static bool Foldout(Rect position, bool foldout, GUIContent content, [uei.DefaultValue("EditorStyles.foldout")]  GUIStyle style )
        { return FoldoutInternal(position, foldout, content, false, style); }

    [uei.ExcludeFromDocs]
public static bool Foldout (Rect position, bool foldout, GUIContent content, bool toggleOnLabelClick) {
    GUIStyle style = EditorStyles.foldout;
    return Foldout ( position, foldout, content, toggleOnLabelClick, style );
}

public static bool Foldout(Rect position, bool foldout, GUIContent content, bool toggleOnLabelClick, [uei.DefaultValue("EditorStyles.foldout")]  GUIStyle style )
        {
            return FoldoutInternal(position, foldout, content, toggleOnLabelClick, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static void HandlePrefixLabel (Rect totalPosition, Rect labelPosition, GUIContent label, int id ) {
    GUIStyle style = EditorStyles.label;
    HandlePrefixLabel ( totalPosition, labelPosition, label, id, style );
}

[uei.ExcludeFromDocs]
public static void HandlePrefixLabel (Rect totalPosition, Rect labelPosition, GUIContent label) {
    GUIStyle style = EditorStyles.label;
    int id = 0;
    HandlePrefixLabel ( totalPosition, labelPosition, label, id, style );
}

public static void HandlePrefixLabel(Rect totalPosition, Rect labelPosition, GUIContent label, [uei.DefaultValue("0")]  int id , [uei.DefaultValue("EditorStyles.label")]  GUIStyle style )
        {
            HandlePrefixLabelInternal(totalPosition, labelPosition, label, id, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static void DrawTextureAlpha (Rect position, Texture image, ScaleMode scaleMode ) {
    float imageAspect = 0;
    DrawTextureAlpha ( position, image, scaleMode, imageAspect );
}

[uei.ExcludeFromDocs]
public static void DrawTextureAlpha (Rect position, Texture image) {
    float imageAspect = 0;
    ScaleMode scaleMode = ScaleMode.StretchToFill;
    DrawTextureAlpha ( position, image, scaleMode, imageAspect );
}

public static void DrawTextureAlpha(Rect position, Texture image, [uei.DefaultValue("ScaleMode.StretchToFill")]  ScaleMode scaleMode , [uei.DefaultValue("0")]  float imageAspect )
        {
            DrawTextureAlphaInternal(position, image, scaleMode, imageAspect);
        }

    
    
    [uei.ExcludeFromDocs]
public static void DrawTextureTransparent (Rect position, Texture image, ScaleMode scaleMode ) {
    float imageAspect = 0;
    DrawTextureTransparent ( position, image, scaleMode, imageAspect );
}

[uei.ExcludeFromDocs]
public static void DrawTextureTransparent (Rect position, Texture image) {
    float imageAspect = 0;
    ScaleMode scaleMode = ScaleMode.StretchToFill;
    DrawTextureTransparent ( position, image, scaleMode, imageAspect );
}

public static void DrawTextureTransparent(Rect position, Texture image, [uei.DefaultValue("ScaleMode.StretchToFill")]  ScaleMode scaleMode , [uei.DefaultValue("0")]  float imageAspect )
        {
            DrawTextureTransparentInternal(position, image, scaleMode, imageAspect);
        }

    
    
    [uei.ExcludeFromDocs]
public static void DrawPreviewTexture (Rect position, Texture image, Material mat , ScaleMode scaleMode ) {
    float imageAspect = 0;
    DrawPreviewTexture ( position, image, mat, scaleMode, imageAspect );
}

[uei.ExcludeFromDocs]
public static void DrawPreviewTexture (Rect position, Texture image, Material mat ) {
    float imageAspect = 0;
    ScaleMode scaleMode = ScaleMode.StretchToFill;
    DrawPreviewTexture ( position, image, mat, scaleMode, imageAspect );
}

[uei.ExcludeFromDocs]
public static void DrawPreviewTexture (Rect position, Texture image) {
    float imageAspect = 0;
    ScaleMode scaleMode = ScaleMode.StretchToFill;
    Material mat = null;
    DrawPreviewTexture ( position, image, mat, scaleMode, imageAspect );
}

public static void DrawPreviewTexture(Rect position, Texture image, [uei.DefaultValue("null")]  Material mat , [uei.DefaultValue("ScaleMode.StretchToFill")]  ScaleMode scaleMode , [uei.DefaultValue("0")]  float imageAspect )
        {
            DrawPreviewTextureInternal(position, image, mat, scaleMode, imageAspect);
        }

    
    
    public static float GetPropertyHeight(SerializedProperty property, bool includeChildren)
        {
            return GetPropertyHeightInternal(property, null, includeChildren);
        }
    
    
    [uei.ExcludeFromDocs]
public static float GetPropertyHeight (SerializedProperty property, GUIContent label ) {
    bool includeChildren = true;
    return GetPropertyHeight ( property, label, includeChildren );
}

[uei.ExcludeFromDocs]
public static float GetPropertyHeight (SerializedProperty property) {
    bool includeChildren = true;
    GUIContent label = null;
    return GetPropertyHeight ( property, label, includeChildren );
}

public static float GetPropertyHeight(SerializedProperty property, [uei.DefaultValue("null")]  GUIContent label , [uei.DefaultValue("true")]  bool includeChildren )
        {
            return GetPropertyHeightInternal(property, label, includeChildren);
        }

    
    
    [uei.ExcludeFromDocs]
public static bool PropertyField (Rect position, SerializedProperty property) {
    bool includeChildren = false;
    return PropertyField ( position, property, includeChildren );
}

public static bool PropertyField(Rect position, SerializedProperty property, [uei.DefaultValue("false")]  bool includeChildren )
        {
            return PropertyFieldInternal(position, property, null, includeChildren);
        }

    
    
    [uei.ExcludeFromDocs]
public static bool PropertyField (Rect position, SerializedProperty property, GUIContent label) {
    bool includeChildren = false;
    return PropertyField ( position, property, label, includeChildren );
}

public static bool PropertyField(Rect position, SerializedProperty property, GUIContent label, [uei.DefaultValue("false")]  bool includeChildren )
        {
            return PropertyFieldInternal(position, property, label, includeChildren);
        }

    
    
}

public sealed partial class EditorGUILayout
{
    [uei.ExcludeFromDocs]
public static bool Foldout (bool foldout, string content) {
    GUIStyle style = EditorStyles.foldout;
    return Foldout ( foldout, content, style );
}

public static bool Foldout(bool foldout, string content, [uei.DefaultValue("EditorStyles.foldout")]  GUIStyle style )
        { return Foldout(foldout, EditorGUIUtility.TempContent(content), false, style); }

    [uei.ExcludeFromDocs]
public static bool Foldout (bool foldout, GUIContent content) {
    GUIStyle style = EditorStyles.foldout;
    return Foldout ( foldout, content, style );
}

public static bool Foldout(bool foldout, GUIContent content, [uei.DefaultValue("EditorStyles.foldout")]  GUIStyle style )
        {
            return Foldout(foldout, content, false, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static bool Foldout (bool foldout, string content, bool toggleOnLabelClick) {
    GUIStyle style = EditorStyles.foldout;
    return Foldout ( foldout, content, toggleOnLabelClick, style );
}

public static bool Foldout(bool foldout, string content, bool toggleOnLabelClick, [uei.DefaultValue("EditorStyles.foldout")]  GUIStyle style )
        { return Foldout(foldout, EditorGUIUtility.TempContent(content), toggleOnLabelClick, style); }

    [uei.ExcludeFromDocs]
public static bool Foldout (bool foldout, GUIContent content, bool toggleOnLabelClick) {
    GUIStyle style = EditorStyles.foldout;
    return Foldout ( foldout, content, toggleOnLabelClick, style );
}

public static bool Foldout(bool foldout, GUIContent content, bool toggleOnLabelClick, [uei.DefaultValue("EditorStyles.foldout")]  GUIStyle style )
        {
            return FoldoutInternal(foldout, content, toggleOnLabelClick, style);
        }

    
    
    [uei.ExcludeFromDocs]
public static void PrefixLabel (string label) {
    GUIStyle followingStyle = "Button";
    PrefixLabel ( label, followingStyle );
}

public static void PrefixLabel(string label, [uei.DefaultValue("\"Button\"")]  GUIStyle followingStyle )
        { PrefixLabel(EditorGUIUtility.TempContent(label), followingStyle, EditorStyles.label); }

    static public void PrefixLabel(string label, GUIStyle followingStyle, GUIStyle labelStyle)
        { PrefixLabel(EditorGUIUtility.TempContent(label), followingStyle, labelStyle); }
    [uei.ExcludeFromDocs]
public static void PrefixLabel (GUIContent label) {
    GUIStyle followingStyle = "Button";
    PrefixLabel ( label, followingStyle );
}

public static void PrefixLabel(GUIContent label, [uei.DefaultValue("\"Button\"")]  GUIStyle followingStyle )
        { PrefixLabel(label, followingStyle, EditorStyles.label); }

    static public void PrefixLabel(GUIContent label, GUIStyle followingStyle, GUIStyle labelStyle)
        {
            PrefixLabelInternal(label, followingStyle, labelStyle);
        }
    
    
}

}
