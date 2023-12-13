// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor;

sealed partial class EditorGUILayout
{
     // Rendering Layers
    public static uint RenderingLayerMaskField(string label, uint layers, params GUILayoutOption[] options)
    {
        return RenderingLayerMaskField(label, layers, EditorStyles.layerMaskField);
    }

    public static uint RenderingLayerMaskField(string label, uint layers, GUIStyle style, params GUILayoutOption[] options)
    {
        return RenderingLayerMaskField(new GUIContent(label), layers, style);
    }

    public static uint RenderingLayerMaskField(GUIContent label, uint layers, params GUILayoutOption[] options)
    {
        return RenderingLayerMaskField(label, layers, EditorStyles.layerMaskField);
    }

    public static RenderingLayerMask RenderingLayerMaskField(string label, RenderingLayerMask layers, params GUILayoutOption[] options)
    {
        return RenderingLayerMaskField(label, layers,  EditorStyles.layerMaskField);
    }
    public static RenderingLayerMask RenderingLayerMaskField(string label, RenderingLayerMask layers, GUIStyle style, params GUILayoutOption[] options)
    {
        return RenderingLayerMaskField(new GUIContent(label), layers,  EditorStyles.layerMaskField);
    }

    public static RenderingLayerMask RenderingLayerMaskField(GUIContent label, RenderingLayerMask layers, params GUILayoutOption[] options)
    {
        return RenderingLayerMaskField(label, layers,  EditorStyles.layerMaskField);
    }

    public static uint RenderingLayerMaskField(GUIContent label, uint layers, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, options);
        return EditorGUI.RenderingLayerMaskField(r, label, layers, style);
    }

    public static RenderingLayerMask RenderingLayerMaskField(GUIContent label, RenderingLayerMask layers, GUIStyle style, params GUILayoutOption[] options)
    {
        var rect = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, options);
        return EditorGUI.RenderingLayerMaskField(rect, label, layers);
    }

    public static void RenderingLayerMaskField(GUIContent label, SerializedProperty property, params GUILayoutOption[] options)
    {
        Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, options);
        EditorGUI.RenderingLayerMaskField(r, label, property);
    }
}
