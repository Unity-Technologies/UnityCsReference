// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using UnityEditor.Rendering;
using UnityEngine;

namespace UnityEditor
{
    public partial class EditorGUI
    {
        static readonly int s_RenderingLayerMaskField = nameof(s_RenderingLayerMaskField).GetHashCode();

        // Make a field for rendering layer masks.
        public static void RenderingLayerMaskField(Rect position, string label, SerializedProperty property)
        {
            RenderingLayerMaskFieldInternal(position, new GUIContent(label), property.uintValue, property, EditorStyles.layerMaskField);
        }

        public static void RenderingLayerMaskField(Rect position, GUIContent label, SerializedProperty property)
        {
            RenderingLayerMaskFieldInternal(position, label, property.uintValue, property, EditorStyles.layerMaskField);
        }

        public static RenderingLayerMask RenderingLayerMaskField(Rect position, string label, RenderingLayerMask layers)
        {
            return RenderingLayerMaskField(position, label, layers, EditorStyles.layerMaskField);
        }

        public static RenderingLayerMask RenderingLayerMaskField(Rect position, string label, RenderingLayerMask layers, GUIStyle style)
        {
            return RenderingLayerMaskField(position, new GUIContent(label), layers, style);
        }

        public static RenderingLayerMask RenderingLayerMaskField(Rect position, GUIContent label, RenderingLayerMask layers)
        {
            return RenderingLayerMaskField(position, label, layers, EditorStyles.layerMaskField);
        }

        public static RenderingLayerMask RenderingLayerMaskField(Rect position, GUIContent label, RenderingLayerMask layers, GUIStyle style)
        {
            return RenderingLayerMaskFieldInternal(position, label, layers, null, style);
        }

        static uint RenderingLayerMaskFieldInternal(Rect position, GUIContent label, uint layers, SerializedProperty property, GUIStyle style)
        {
            var (names, values) = RenderPipelineEditorUtility.GetRenderingLayerNamesAndValuesForMask(layers);
            var bitCount = RenderPipelineEditorUtility.GetActiveMaxRenderingLayers();

            BeginChangeCheck();
            var mixedValue = property is { hasMultipleDifferentValues: true };
            var newValue = DrawMaskField(position, label, layers, names, values, style, mixedValue, !RenderPipelineEditorUtility.DoesMaskContainRenderingLayersOutsideOfMaxBitCount(layers, bitCount));
            var uintValue = unchecked((uint)newValue);

            if (EndChangeCheck())
            {
                if (uintValue != uint.MaxValue && BitOperationUtils.AreAllBitsSetForValues(uintValue, values, bitCount))
                        uintValue = uint.MaxValue;

                if(property != null)
                    ApplyModifiedProperties(property, uintValue);
            }

            if (RenderPipelineEditorUtility.DoesMaskContainRenderingLayersOutsideOfMaxBitCount(uintValue, bitCount))
                EditorGUILayout.HelpBox(RenderPipelineEditorUtility.GetOutsideOfMaxBitCountWarningMessage(bitCount), MessageType.Warning);

            return uintValue;
        }

        static int DrawMaskField(Rect position, GUIContent label, uint layers, string[] names, int[] values, GUIStyle style, bool mixedValue, bool autoSelectEverything)
        {
            var id = GUIUtility.GetControlID(s_RenderingLayerMaskField, FocusType.Keyboard, position);
            if (label != null)
                position = PrefixLabel(position, id, label);

            using var scope = new MixedValueScope(mixedValue);

            return MaskFieldGUI.DoMaskField(position, id, unchecked((int)layers), names, values, style, autoSelectEverything: autoSelectEverything);
        }

        static void ApplyModifiedProperties(SerializedProperty property, uint uintValue)
        {
            var bits = property.FindPropertyRelative("m_Bits");
            Debug.Assert(bits != null, $"Property for RenderingLayerMask doesn't contain m_Bits. You should use new {nameof(RenderingLayerMask)} type with this drawer.");
            bits.uintValue = uintValue;
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.SetIsDifferentCacheDirty();
        }
    }
}
