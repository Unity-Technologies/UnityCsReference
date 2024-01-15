// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEditor.Rendering;
using UnityEngine;

namespace UnityEditor
{
    [SuppressMessage("ReSharper", "NotAccessedField.Local")]
    public partial class EditorGUI
    {
        private static readonly int s_RenderingLayerMaskField = "s_RenderingLayerMaskField".GetHashCode();

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
            var id = GUIUtility.GetControlID(s_RenderingLayerMaskField, FocusType.Keyboard, position);
            if (label != null)
                position = PrefixLabel(position, id, label);

            var (names, values) = RenderPipelineEditorUtility.GetRenderingLayerNamesAndValuesForMask(layers);

            using var scope = new MixedValueScope();

            BeginChangeCheck();
            var newValue = MaskFieldGUI.DoMaskField(position, id, unchecked((int)layers), names, values, style);
            if (EndChangeCheck() && property != null)
            {
                var bits = property.FindPropertyRelative("m_Bits");
                Debug.Assert(bits != null, $"Property for RenderingLayerMask doesn't contain m_Bits. You should use new {nameof(RenderingLayerMask)} type with this drawer.");
                bits.uintValue = (uint)newValue;
            }

            var currentLimit = RenderPipelineEditorUtility.GetActiveMaxRenderingLayers();
            var newValueUint = unchecked((uint)newValue);
            if (currentLimit != 32 && newValueUint != uint.MaxValue && newValueUint >= 1u << currentLimit)
                EditorGUILayout.HelpBox($"Current mask contains layers outside of a supported range by active Render Pipeline. The active Render Pipeline only supports up to {currentLimit} layers. Rendering Layers above {currentLimit} are ignored.", MessageType.Warning);

            return unchecked((uint)newValue);
        }
    }
}
