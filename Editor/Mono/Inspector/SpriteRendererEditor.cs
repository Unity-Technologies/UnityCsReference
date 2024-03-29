// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.AnimatedValues;

namespace UnityEditor
{
    [CustomEditor(typeof(SpriteRenderer))]
    [CanEditMultipleObjects]
    internal class SpriteRendererEditor : RendererEditorBase
    {
        private SerializedProperty m_Sprite;
        private SerializedProperty m_Color;
        private SerializedProperty m_Material;

        class Styles
        {
            public static readonly GUIContent flipLabel = EditorGUIUtility.TrTextContent("Flip", "Sprite flipping");
            public static readonly GUIContent flipXLabel = EditorGUIUtility.TrTextContent("X", "Sprite horizontal flipping");
            public static readonly GUIContent flipYLabel = EditorGUIUtility.TrTextContent("Y", "Sprite vertical flipping");
            public static readonly int flipToggleHash = "FlipToggleHash".GetHashCode();

            public static readonly GUIContent fullTileLabel = EditorGUIUtility.TrTextContent("Tile Mode", "Specify the 9-slice tiling behaviour");
            public static readonly GUIContent fullTileThresholdLabel = EditorGUIUtility.TrTextContent("Stretch Value", "This value defines how much the center portion will stretch before it tiles.");
            public static readonly GUIContent drawModeLabel = EditorGUIUtility.TrTextContent("Draw Mode", "Specify the draw mode for the sprite");
            public static readonly GUIContent widthLabel = EditorGUIUtility.TrTextContent("Width", "The width dimension value for the sprite");
            public static readonly GUIContent heightLabel = EditorGUIUtility.TrTextContent("Height", "The height dimension value for the sprite");
            public static readonly GUIContent sizeLabel = EditorGUIUtility.TrTextContent("Size", "The rendering dimension for the sprite");
            public static readonly GUIContent notFullRectWarningLabel = EditorGUIUtility.TrTextContent("Sprite Tiling might not appear correctly because the Sprite used is not generated with Full Rect or Sprite Mode is set to Polygon mode. To fix this, change the Mesh Type in the Sprite's import setting to Full Rect and Sprite Mode is either Single or Multiple");
            public static readonly GUIContent notFullRectMultiEditWarningLabel = EditorGUIUtility.TrTextContent("Sprite Tiling might not appear correctly because some of the Sprites used are not generated with Full Rect. To fix this, change the Mesh Type in the Sprite's import setting to Full Rect");
            public static readonly int sizeFieldHash = "SpriteRendererSizeField".GetHashCode();
            public static readonly GUIContent materialLabel = EditorGUIUtility.TrTextContent("Material", "Material to be used by SpriteRenderer");
            public static readonly GUIContent spriteLabel = EditorGUIUtility.TrTextContent("Sprite", "The Sprite to render");
            public static readonly GUIContent colorLabel = EditorGUIUtility.TrTextContent("Color", "Rendering color for the Sprite graphic");
            public static readonly GUIContent maskInteractionLabel = EditorGUIUtility.TrTextContent("Mask Interaction", "SpriteRenderer's interaction with a Sprite Mask");
            public static readonly GUIContent spriteSortPointLabel = EditorGUIUtility.TrTextContent("Sprite Sort Point", "Determines which position of the Sprite which is used for sorting");
            public static readonly Texture2D warningIcon = EditorGUIUtility.LoadIcon("console.warnicon");
            public static readonly GUIContent drawModeChange = EditorGUIUtility.TrTextContent("Draw mode Change");
        }

        private SerializedProperty m_FlipX;
        private SerializedProperty m_FlipY;

        private SerializedProperty m_DrawMode;
        private SerializedProperty m_SpriteTileMode;
        private SerializedProperty m_AdaptiveModeThreshold;
        private SerializedProperty m_Size;
        private AnimBool m_ShowDrawMode;
        private AnimBool m_ShowTileMode;
        private AnimBool m_ShowAdaptiveThreshold;
        private SerializedProperty m_MaskInteraction;
        private SerializedProperty m_SpriteSortPoint;

        public override void OnEnable()
        {
            base.OnEnable();

            m_Sprite = serializedObject.FindProperty("m_Sprite");
            m_Color = serializedObject.FindProperty("m_Color");
            m_FlipX = serializedObject.FindProperty("m_FlipX");
            m_FlipY = serializedObject.FindProperty("m_FlipY");
            m_Material = serializedObject.FindProperty("m_Materials.Array"); // Only allow to edit one material
            m_DrawMode = serializedObject.FindProperty("m_DrawMode");
            m_Size = serializedObject.FindProperty("m_Size");
            m_SpriteTileMode = serializedObject.FindProperty("m_SpriteTileMode");
            m_AdaptiveModeThreshold = serializedObject.FindProperty("m_AdaptiveModeThreshold");
            m_ShowDrawMode = new AnimBool(ShouldShowDrawMode());
            m_ShowTileMode = new AnimBool(ShouldShowTileMode());
            m_ShowAdaptiveThreshold = new AnimBool(ShouldShowAdaptiveThreshold());

            m_ShowDrawMode.valueChanged.AddListener(Repaint);
            m_ShowTileMode.valueChanged.AddListener(Repaint);
            m_ShowAdaptiveThreshold.valueChanged.AddListener(Repaint);
            m_MaskInteraction = serializedObject.FindProperty("m_MaskInteraction");
            m_SpriteSortPoint = serializedObject.FindProperty("m_SpriteSortPoint");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Sprite, Styles.spriteLabel);
            using (new EditorGUI.DisabledScope(m_Sprite.objectReferenceValue == null))
            {
                if(SpriteUtilityWindow.DoOpenSpriteEditorWindowUI())
                    SpriteUtilityWindow.ShowSpriteEditorWindow(m_Sprite.objectReferenceValue);
            }
            GUILayout.Space(5);
            EditorGUILayout.PropertyField(m_Color, Styles.colorLabel, true);

            FlipToggles();

            using (new EditorGUI.DisabledGroupScope(IsTextureless()))
            {
                var showMixedValue = EditorGUI.showMixedValue;
                if (m_DrawMode.hasMultipleDifferentValues)
                    EditorGUI.showMixedValue = true;
                SpriteDrawMode drawMode = (SpriteDrawMode)m_DrawMode.intValue;
                drawMode = (SpriteDrawMode)EditorGUILayout.EnumPopup(Styles.drawModeLabel, drawMode);
                SetDrawMode(drawMode);
                EditorGUI.showMixedValue = showMixedValue;

                m_ShowDrawMode.target = ShouldShowDrawMode();
                if (EditorGUILayout.BeginFadeGroup(m_ShowDrawMode.faded))
                {
                    string notFullRectWarning = GetSpriteNotFullRectWarning();
                    if (notFullRectWarning != null)
                        EditorGUILayout.HelpBox(notFullRectWarning, MessageType.Warning);

                    EditorGUI.indentLevel++;
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel(Styles.sizeLabel);
                    EditorGUI.showMixedValue = m_Size.hasMultipleDifferentValues;
                    FloatFieldLabelAbove(Styles.widthLabel, m_Size.FindPropertyRelative("x"));
                    FloatFieldLabelAbove(Styles.heightLabel, m_Size.FindPropertyRelative("y"));
                    EditorGUI.showMixedValue = false;
                    EditorGUILayout.EndHorizontal();


                    m_ShowTileMode.target = ShouldShowTileMode();
                    if (EditorGUILayout.BeginFadeGroup(m_ShowTileMode.faded))
                    {
                        EditorGUILayout.PropertyField(m_SpriteTileMode, Styles.fullTileLabel);

                        m_ShowAdaptiveThreshold.target = ShouldShowAdaptiveThreshold();
                        if (EditorGUILayout.BeginFadeGroup(m_ShowAdaptiveThreshold.faded))
                        {
                            EditorGUI.indentLevel++;
                            EditorGUILayout.Slider(m_AdaptiveModeThreshold, 0.0f, 1.0f, Styles.fullTileThresholdLabel);
                            EditorGUI.indentLevel--;
                        }
                        EditorGUILayout.EndFadeGroup();
                    }
                    EditorGUILayout.EndFadeGroup();

                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFadeGroup();
            }

            EditorGUILayout.PropertyField(m_MaskInteraction, Styles.maskInteractionLabel);
            EditorGUILayout.PropertyField(m_SpriteSortPoint, Styles.spriteSortPointLabel);
            EditorGUILayout.PropertyField(m_Material.GetArrayElementAtIndex(0), Styles.materialLabel, true);

            ShowMaterialError();

            Other2DSettingsGUI();

            serializedObject.ApplyModifiedProperties();
        }

        internal void SetDrawMode(SpriteDrawMode drawMode)
        {
            if (drawMode != (SpriteDrawMode)m_DrawMode.intValue)
            {
                foreach (var target in serializedObject.targetObjects)
                {
                    var sr = (SpriteRenderer)target;
                    var t = sr.transform;
                    Undo.RecordObjects(new UnityEngine.Object[] {sr, t}, Styles.drawModeChange.text);
                    sr.drawMode = drawMode;
                    foreach (var editor in ActiveEditorTracker.sharedTracker.activeEditors)
                    {
                        if(editor.target == t)
                            editor.serializedObject.SetIsDifferentCacheDirty();
                    }
                }
                serializedObject.SetIsDifferentCacheDirty();
            }
        }

        void FloatFieldLabelAbove(GUIContent contentLabel, SerializedProperty sp)
        {
            EditorGUILayout.BeginVertical();
            Rect rtLabel = GUILayoutUtility.GetRect(contentLabel, EditorStyles.label);
            GUIContent label = EditorGUI.BeginProperty(rtLabel, contentLabel, sp);
            int id = GUIUtility.GetControlID(Styles.sizeFieldHash, FocusType.Keyboard, rtLabel);
            EditorGUI.HandlePrefixLabel(rtLabel, rtLabel, label, id);
            Rect rt = GUILayoutUtility.GetRect(contentLabel, EditorStyles.textField);
            EditorGUI.BeginChangeCheck();
            float value = EditorGUI.DoFloatField(EditorGUI.s_RecycledEditor, rt, rtLabel, id, sp.floatValue, EditorGUI.kFloatFieldFormatString, EditorStyles.textField, true);
            if (EditorGUI.EndChangeCheck())
                sp.floatValue = value;
            EditorGUI.EndProperty();
            EditorGUILayout.EndVertical();
        }

        string GetSpriteNotFullRectWarning()
        {
            foreach (var t in targets)
            {
                if (!(t as SpriteRenderer).shouldSupportTiling)
                    return targets.Length == 1 ? Styles.notFullRectWarningLabel.text : Styles.notFullRectMultiEditWarningLabel.text;
            }
            return null;
        }

        bool IsTextureless()
        {
            foreach (var t in targets)
            {
                var sr = (t as SpriteRenderer);
                if (sr.sprite != null && sr.sprite.texture == null)
                    return true;
            }
            return false;
        }

        bool ShouldShowDrawMode()
        {
            return m_DrawMode.intValue != (int)SpriteDrawMode.Simple && !m_DrawMode.hasMultipleDifferentValues;
        }

        bool ShouldShowAdaptiveThreshold()
        {
            return m_SpriteTileMode.intValue == (int)SpriteTileMode.Adaptive && !m_SpriteTileMode.hasMultipleDifferentValues;
        }

        bool ShouldShowTileMode()
        {
            return m_DrawMode.intValue == (int)SpriteDrawMode.Tiled && !m_DrawMode.hasMultipleDifferentValues;
        }

        void FlipToggles()
        {
            const int toggleOffset = 30;
            GUILayout.BeginHorizontal();
            Rect r = GUILayoutUtility.GetRect(EditorGUIUtility.fieldWidth, EditorGUILayout.kLabelFloatMaxW, EditorGUI.kSingleLineHeight, EditorGUI.kSingleLineHeight, EditorStyles.numberField);
            int id = GUIUtility.GetControlID(Styles.flipToggleHash, FocusType.Keyboard, r);
            r = EditorGUI.PrefixLabel(r, id, Styles.flipLabel);
            r.width = toggleOffset;
            FlipToggle(r, Styles.flipXLabel, m_FlipX);
            r.x += toggleOffset;
            FlipToggle(r, Styles.flipYLabel, m_FlipY);
            GUILayout.EndHorizontal();
        }

        void FlipToggle(Rect r, GUIContent label, SerializedProperty property)
        {
            EditorGUI.BeginProperty(r, label, property);

            bool toggle = property.boolValue;
            EditorGUI.BeginChangeCheck();
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            toggle = EditorGUI.ToggleLeft(r, label, toggle);
            EditorGUI.indentLevel = oldIndent;
            if (EditorGUI.EndChangeCheck())
            {
                property.boolValue = toggle;
            }

            EditorGUI.EndProperty();
        }

        private void ShowMaterialError()
        {
            bool materialHasMainTex = DoesMaterialHaveSpriteTexture("_MainTex");
            bool materialHasBaseMap = DoesMaterialHaveSpriteTexture("_BaseMap");

            if (!materialHasMainTex && !materialHasBaseMap)
            {
                ShowWarning("Material does not have a _MainTex or _BaseMap texture property. Having one of them is required for SpriteRenderer.");
            }
            else
            {
                if(materialHasMainTex)
                    CheckPropertyForScaleAndOffset("_MainTex");
                else if(materialHasBaseMap)
                    CheckPropertyForScaleAndOffset("_BaseMap");
            }
        }

        private void CheckPropertyForScaleAndOffset(string propertyName)
        {
            Material material = (target as SpriteRenderer).sharedMaterial;
            if (material != null)
            {
                Vector2 offset = material.GetTextureOffset(propertyName);
                Vector2 scale = material.GetTextureScale(propertyName);
                if (offset.x != 0 || offset.y != 0 || scale.x != 1 || scale.y != 1)
                {
                    ShowWarning("Material texture property " + propertyName + " has offset/scale set. It is incompatible with SpriteRenderer.");
                }
            }
        }

        private bool DoesMaterialHaveSpriteTexture(string propertyName)
        {
            Material material = (target as SpriteRenderer).sharedMaterial;
            if (material == null)
                return true;

            return material.HasProperty(propertyName);
        }

        private static void ShowWarning(string message)
        {
            var c = new GUIContent(message) {image = Styles.warningIcon};

            GUILayout.Space(5);
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(c, EditorStyles.wordWrappedMiniLabel);
            GUILayout.EndVertical();
        }
    }
}
