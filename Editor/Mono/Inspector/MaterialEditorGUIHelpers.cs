// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Rendering;
using System;

namespace UnityEditor
{
    public partial class MaterialEditor
    {
        public const int kMiniTextureFieldLabelIndentLevel = 2;
        const float kSpaceBetweenFlexibleAreaAndField = 5f;
        const float kQueuePopupWidth = 100f;
        const float kCustomQueuePopupWidth = kQueuePopupWidth + 15f;

        // Do currently edited materials have different render queue values?
        private bool HasMultipleMixedQueueValues()
        {
            int queue = ShaderUtil.GetMaterialRawRenderQueue(targets[0] as Material);
            for (int i = 1; i < targets.Length; ++i)
            {
                if (queue != ShaderUtil.GetMaterialRawRenderQueue(targets[i] as Material))
                {
                    return true;
                }
            }
            return false;
        }

        // Field for editing render queue value, with an automatically calculated rect
        public void RenderQueueField()
        {
            Rect r = GetControlRectForSingleLine();
            RenderQueueField(r);
        }

        // Field for editing render queue value, with an explicit rect
        public void RenderQueueField(Rect r)
        {
            var mixedValue = HasMultipleMixedQueueValues();
            EditorGUI.showMixedValue = mixedValue;

            var mat = targets[0] as Material;
            int curRawQueue = ShaderUtil.GetMaterialRawRenderQueue(mat);
            int curDisplayQueue = mat.renderQueue; // this gets final queue value used for rendering, taking shader's queue into account

            // Figure out if we're using one of common queues, or a custom one
            GUIContent[] queueNames = null;
            int[] queueValues = null;
            float labelWidth;
            // If we use queue value that is not available, lets switch to the custom one
            bool useCustomQueue = Array.IndexOf(Styles.queueValues, curRawQueue) < 0;
            if (useCustomQueue)
            {
                // It is a big chance that we already have this custom queue value available
                bool updateNewCustomQueueValue = Array.IndexOf(Styles.customQueueNames, curRawQueue) < 0;
                if (updateNewCustomQueueValue)
                {
                    int targetQueueIndex = CalculateClosestQueueIndexToValue(curRawQueue);
                    string targetQueueName = Styles.queueNames[targetQueueIndex].text;
                    int targetQueueValueOverflow = curRawQueue - Styles.queueValues[targetQueueIndex];

                    string newQueueName = string.Format(
                            targetQueueValueOverflow > 0 ? "{0}+{1}" : "{0}{1}",
                            targetQueueName,
                            targetQueueValueOverflow);
                    Styles.customQueueNames[Styles.kCustomQueueIndex].text = newQueueName;
                    Styles.customQueueValues[Styles.kCustomQueueIndex] = curRawQueue;
                }

                queueNames = Styles.customQueueNames;
                queueValues = Styles.customQueueValues;
                labelWidth = kCustomQueuePopupWidth;
            }
            else
            {
                queueNames = Styles.queueNames;
                queueValues = Styles.queueValues;
                labelWidth = kQueuePopupWidth;
            }

            // We want the custom queue number field to line up with thumbnails & other value fields
            // (on the right side), and common queues popup to be on the left of that.
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            float oldFieldWidth = EditorGUIUtility.fieldWidth;
            SetDefaultGUIWidths();
            EditorGUIUtility.labelWidth -= labelWidth;
            Rect popupRect = r;
            popupRect.width -= EditorGUIUtility.fieldWidth + 2;
            Rect numberRect = r;
            numberRect.xMin = numberRect.xMax - EditorGUIUtility.fieldWidth;

            // Queues popup
            int curPopupValue = curRawQueue;
            int newPopupValue = EditorGUI.IntPopup(popupRect, Styles.queueLabel, curRawQueue, queueNames, queueValues);

            // Custom queue field
            int newDisplayQueue = EditorGUI.DelayedIntField(numberRect, curDisplayQueue);

            // If popup or custom field changed, set the new queue
            if (curPopupValue != newPopupValue || curDisplayQueue != newDisplayQueue)
            {
                RegisterPropertyChangeUndo("Render Queue");
                // Take the value from the number field,
                int newQueue = newDisplayQueue;
                // But if it's the popup that was changed
                if (newPopupValue != curPopupValue)
                    newQueue = newPopupValue;
                newQueue = Mathf.Clamp(newQueue, -1, 5000); // clamp to valid queue ranges
                // Change the material queues
                foreach (var m in targets)
                {
                    ((Material)m).renderQueue = newQueue;
                }
            }

            EditorGUIUtility.labelWidth = oldLabelWidth;
            EditorGUIUtility.fieldWidth = oldFieldWidth;
            EditorGUI.showMixedValue = false;
        }

        public bool EnableInstancingField()
        {
            if (!ShaderUtil.HasInstancing(m_Shader))
                return false;
            Rect r = GetControlRectForSingleLine();
            EnableInstancingField(r);
            return true;
        }

        public void EnableInstancingField(Rect r)
        {
            EditorGUI.PropertyField(r, m_EnableInstancing, Styles.enableInstancingLabel);
            serializedObject.ApplyModifiedProperties();
        }

        public bool DoubleSidedGIField()
        {
            Rect r = GetControlRectForSingleLine();
            if (LightmapEditorSettings.lightmapper == LightmapEditorSettings.Lightmapper.ProgressiveCPU)
            {
                EditorGUI.PropertyField(r, m_DoubleSidedGI, Styles.doubleSidedGILabel);
                serializedObject.ApplyModifiedProperties();
                return true;
            }
            else
            {
                using (new EditorGUI.DisabledScope(LightmapEditorSettings.lightmapper != LightmapEditorSettings.Lightmapper.ProgressiveCPU))
                    EditorGUI.Toggle(r, Styles.doubleSidedGILabel, false);
            }
            return false;
        }

        private int CalculateClosestQueueIndexToValue(int requestedValue)
        {
            int bestCloseByDiff = int.MaxValue;
            int result = 1;
            for (int i = 1; i < Styles.queueValues.Length; i++)
            {
                int queueValue = Styles.queueValues[i];
                int closeByDiff = Mathf.Abs(queueValue - requestedValue);
                if (closeByDiff < bestCloseByDiff)
                {
                    result = i;
                    bestCloseByDiff = closeByDiff;
                }
            }
            return result;
        }

        public Rect TexturePropertySingleLine(GUIContent label, MaterialProperty textureProp)
        {
            return TexturePropertySingleLine(label, textureProp, null, null);
        }

        public Rect TexturePropertySingleLine(GUIContent label, MaterialProperty textureProp, MaterialProperty extraProperty1)
        {
            return TexturePropertySingleLine(label, textureProp, extraProperty1, null);
        }

        // Mini texture slot, with two extra controls on the same line (allocates rect in GUILayout)
        // Have up to 3 controls on one line
        public Rect TexturePropertySingleLine(GUIContent label, MaterialProperty textureProp, MaterialProperty extraProperty1, MaterialProperty extraProperty2)
        {
            Rect r = GetControlRectForSingleLine();
            TexturePropertyMiniThumbnail(r, textureProp, label.text, label.tooltip);

            // No extra properties: early out
            if (extraProperty1 == null && extraProperty2 == null)
                return r;

            // One extra property
            if (extraProperty1 == null || extraProperty2 == null)
            {
                var prop = extraProperty1 ?? extraProperty2;
                if (prop.type == MaterialProperty.PropType.Color)
                {
                    ExtraPropertyAfterTexture(GetLeftAlignedFieldRect(r), prop);
                }
                else
                {
                    ExtraPropertyAfterTexture(GetRectAfterLabelWidth(r), prop);
                }
            }
            else // Two extra properties
            {
                if (extraProperty1.type == MaterialProperty.PropType.Color)
                {
                    ExtraPropertyAfterTexture(GetFlexibleRectBetweenFieldAndRightEdge(r), extraProperty2);
                    ExtraPropertyAfterTexture(GetLeftAlignedFieldRect(r), extraProperty1);
                }
                else
                {
                    ExtraPropertyAfterTexture(GetRightAlignedFieldRect(r), extraProperty2);
                    ExtraPropertyAfterTexture(GetFlexibleRectBetweenLabelAndField(r), extraProperty1);
                }
            }
            return r;
        }

        public Rect TexturePropertyWithHDRColor(GUIContent label, MaterialProperty textureProp, MaterialProperty colorProperty, ColorPickerHDRConfig hdrConfig, bool showAlpha)
        {
            Rect r = GetControlRectForSingleLine();
            TexturePropertyMiniThumbnail(r, textureProp, label.text, label.tooltip);

            if (colorProperty.type != MaterialProperty.PropType.Color)
            {
                Debug.LogError("Assuming MaterialProperty.PropType.Color (was " + colorProperty.type + ")");
                return r;
            }

            BeginAnimatedCheck(r, colorProperty);

            ColorPickerHDRConfig hdrConfiguration = hdrConfig ?? ColorPicker.defaultHDRConfig;

            Rect leftRect = GetLeftAlignedFieldRect(r);
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = colorProperty.hasMixedValue;
            Color newValue = EditorGUI.ColorField(leftRect, GUIContent.none, colorProperty.colorValue, true, showAlpha, true, hdrConfiguration);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                colorProperty.colorValue = newValue;

            // Extra brightness control for color (for usability)
            {
                Rect brightnessRect = GetFlexibleRectBetweenFieldAndRightEdge(r);
                float oldLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = brightnessRect.width - EditorGUIUtility.fieldWidth;

                EditorGUI.BeginChangeCheck();
                newValue = EditorGUI.ColorBrightnessField(brightnessRect, GUIContent.Temp(" "), colorProperty.colorValue, hdrConfiguration.minBrightness, hdrConfiguration.maxBrightness);
                if (EditorGUI.EndChangeCheck())
                    colorProperty.colorValue = newValue;

                EditorGUIUtility.labelWidth = oldLabelWidth;
            }

            EndAnimatedCheck();

            return r;
        }

        public Rect TexturePropertyTwoLines(GUIContent label, MaterialProperty textureProp, MaterialProperty extraProperty1, GUIContent label2, MaterialProperty extraProperty2)
        {
            // If not using the second extra property then use the single line version as
            // the first extra property is always inlined with the the texture slot
            if (extraProperty2 == null)
            {
                return TexturePropertySingleLine(label, textureProp, extraProperty1);
            }

            Rect r = GetControlRectForSingleLine();
            TexturePropertyMiniThumbnail(r, textureProp, label.text, label.tooltip);

            // First extra control on the same line as the texture
            Rect r1 = GetRectAfterLabelWidth(r);
            if (extraProperty1.type == MaterialProperty.PropType.Color)
                r1 = GetLeftAlignedFieldRect(r);
            ExtraPropertyAfterTexture(r1, extraProperty1);

            // New line for extraProperty2
            Rect r2 = GetControlRectForSingleLine();
            ShaderProperty(r2, extraProperty2, label2.text, MaterialEditor.kMiniTextureFieldLabelIndentLevel + 1);

            // Return total rect
            r.height += r2.height;
            return r;
        }

        Rect GetControlRectForSingleLine()
        {
            const float extraSpacing = 2f; // The shader properties needs a little more vertical spacing due to the mini texture field (looks cramped without)
            return EditorGUILayout.GetControlRect(true, EditorGUI.kSingleLineHeight + extraSpacing, EditorStyles.layerMaskField);
        }

        void ExtraPropertyAfterTexture(Rect r, MaterialProperty property)
        {
            if ((property.type == MaterialProperty.PropType.Float || property.type == MaterialProperty.PropType.Color) && r.width > EditorGUIUtility.fieldWidth)
            {
                // We want color fields and float fields to have same width as EditorGUIUtility.fieldWidth
                // so controls aligns vertically.
                // This also makes us able to have a draggable area in front of the float fields. We therefore ensures
                // the property has a label (here we use a whitespace) and adjust label width.
                float oldLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = r.width - EditorGUIUtility.fieldWidth;
                ShaderProperty(r, property, " ");
                EditorGUIUtility.labelWidth = oldLabelWidth;
                return;
            }

            ShaderProperty(r, property, string.Empty);
        }

        static public Rect GetRightAlignedFieldRect(Rect r)
        {
            return new Rect(r.xMax - EditorGUIUtility.fieldWidth, r.y, EditorGUIUtility.fieldWidth, EditorGUIUtility.singleLineHeight);
        }

        static public Rect GetLeftAlignedFieldRect(Rect r)
        {
            return new Rect(r.x + EditorGUIUtility.labelWidth, r.y, EditorGUIUtility.fieldWidth, EditorGUIUtility.singleLineHeight);
        }

        static public Rect GetFlexibleRectBetweenLabelAndField(Rect r)
        {
            return new Rect(r.x + EditorGUIUtility.labelWidth, r.y, r.width - EditorGUIUtility.labelWidth - EditorGUIUtility.fieldWidth - kSpaceBetweenFlexibleAreaAndField, EditorGUIUtility.singleLineHeight);
        }

        static public Rect GetFlexibleRectBetweenFieldAndRightEdge(Rect r)
        {
            Rect r2 = GetRectAfterLabelWidth(r);
            r2.xMin += EditorGUIUtility.fieldWidth + kSpaceBetweenFlexibleAreaAndField;
            return r2;
        }

        static public Rect GetRectAfterLabelWidth(Rect r)
        {
            return new Rect(r.x + EditorGUIUtility.labelWidth, r.y, r.width - EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
        }

        static internal System.Type GetTextureTypeFromDimension(TextureDimension dim)
        {
            switch (dim)
            {
                case TextureDimension.Tex2D: return typeof(Texture); // common use case is RenderTextures too, so return base class
                case TextureDimension.Cube: return typeof(Cubemap);
                case TextureDimension.Tex3D: return typeof(Texture3D);
                case TextureDimension.Tex2DArray: return typeof(Texture2DArray);
                case TextureDimension.CubeArray: return typeof(CubemapArray);
                case TextureDimension.Any: return typeof(Texture);
                default: return null; // Unknown, None etc.
            }
        }
    }
} // namespace UnityEditor
