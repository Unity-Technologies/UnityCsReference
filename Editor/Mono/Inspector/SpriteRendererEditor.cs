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

        private static class Contents
        {
            public static readonly GUIContent flipLabel = EditorGUIUtility.TextContent("Flip|Sprite flipping");
            public static readonly GUIContent flipXLabel = EditorGUIUtility.TextContent("X|Sprite horizontal flipping");
            public static readonly GUIContent flipYLabel = EditorGUIUtility.TextContent("Y|Sprite vertical flipping");
            public static readonly int flipToggleHash = "FlipToggleHash".GetHashCode();

            public static readonly GUIContent fullTileLabel = EditorGUIUtility.TextContent("Tile Mode|Specify the 9 slice tiling behaviour");
            public static readonly GUIContent fullTileThresholdLabel = EditorGUIUtility.TextContent("Stretch Value|This value defines how much the center portion will stretch before it tiles.");
            public static readonly GUIContent drawModeLabel = EditorGUIUtility.TextContent("Draw Mode|Specify the draw mode for the sprite");
            public static readonly GUIContent widthLabel = EditorGUIUtility.TextContent("Width|The width dimension value for the sprite");
            public static readonly GUIContent heightLabel = EditorGUIUtility.TextContent("Height|The height dimension value for the sprite");
            public static readonly GUIContent sizeLabel = EditorGUIUtility.TextContent("Size|The rendering dimension for the sprite");
            public static readonly GUIContent notFullRectWarningLabel = EditorGUIUtility.TextContent("Sprite Tiling might not appear correctly because the Sprite used is not generated with Full Rect or Sprite Mode is set to Polygon mode. To fix this, change the Mesh Type in the Sprite's import setting to Full Rect and Sprite Mode is either Single or Multiple");
            public static readonly GUIContent notFullRectMultiEditWarningLabel = EditorGUIUtility.TextContent("Sprite Tiling might not appear correctly because some of the Sprites used are not generated with Full Rect. To fix this, change the Mesh Type in the Sprite's import setting to Full Rect");
            public static readonly int sizeFieldHash = "SpriteRendererSizeField".GetHashCode();
            public static readonly GUIContent materialLabel = EditorGUIUtility.TextContent("Material|Material to be used by SpriteRenderer");
            public static readonly GUIContent spriteLabel = EditorGUIUtility.TextContent("Sprite|The Sprite to render");
            public static readonly GUIContent colorLabel = EditorGUIUtility.TextContent("Color|Rendering color for the Sprite graphic");
            public static readonly Texture2D warningIcon = EditorGUIUtility.LoadIcon("console.warnicon");
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
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Sprite, Contents.spriteLabel);

            EditorGUILayout.PropertyField(m_Color, Contents.colorLabel, true);

            FlipToggles();
            if (m_Material.arraySize == 0)
                m_Material.InsertArrayElementAtIndex(0);

            Rect r = GUILayoutUtility.GetRect(
                    EditorGUILayout.kLabelFloatMinW, EditorGUILayout.kLabelFloatMaxW,
                    EditorGUI.kSingleLineHeight, EditorGUI.kSingleLineHeight);

            EditorGUI.showMixedValue = m_Material.hasMultipleDifferentValues;
            Object currentMaterialRef = m_Material.GetArrayElementAtIndex(0).objectReferenceValue;
            Object returnedMaterialRef = EditorGUI.ObjectField(r, Contents.materialLabel, currentMaterialRef, typeof(Material), false);
            if (returnedMaterialRef != currentMaterialRef)
            {
                m_Material.GetArrayElementAtIndex(0).objectReferenceValue = returnedMaterialRef;
            }
            EditorGUI.showMixedValue = false;

            EditorGUILayout.PropertyField(m_DrawMode, Contents.drawModeLabel);

            m_ShowDrawMode.target = ShouldShowDrawMode();
            if (EditorGUILayout.BeginFadeGroup(m_ShowDrawMode.faded))
            {
                string notFullRectWarning = GetSpriteNotFullRectWarning();
                if (notFullRectWarning != null)
                    EditorGUILayout.HelpBox(notFullRectWarning, MessageType.Warning);

                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(Contents.sizeLabel);
                EditorGUI.showMixedValue = m_Size.hasMultipleDifferentValues;
                FloatFieldLabelAbove(Contents.widthLabel, m_Size.FindPropertyRelative("x"));
                FloatFieldLabelAbove(Contents.heightLabel, m_Size.FindPropertyRelative("y"));
                EditorGUI.showMixedValue = false;
                EditorGUILayout.EndHorizontal();

                m_ShowTileMode.target = ShouldShowTileMode();
                if (EditorGUILayout.BeginFadeGroup(m_ShowTileMode.faded))
                {
                    EditorGUILayout.PropertyField(m_SpriteTileMode, Contents.fullTileLabel);

                    m_ShowAdaptiveThreshold.target = ShouldShowAdaptiveThreshold();
                    if (EditorGUILayout.BeginFadeGroup(m_ShowAdaptiveThreshold.faded))
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.Slider(m_AdaptiveModeThreshold, 0.0f, 1.0f, Contents.fullTileThresholdLabel);
                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndFadeGroup();
                }
                EditorGUILayout.EndFadeGroup();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();

            RenderSortingLayerFields();

            EditorGUILayout.PropertyField(m_MaskInteraction);

            CheckForErrors();

            serializedObject.ApplyModifiedProperties();
        }

        void FloatFieldLabelAbove(GUIContent contentLabel, SerializedProperty sp)
        {
            EditorGUILayout.BeginVertical();
            Rect rtLabel = GUILayoutUtility.GetRect(contentLabel, EditorStyles.label);
            GUIContent label = EditorGUI.BeginProperty(rtLabel, contentLabel, sp);
            int id = GUIUtility.GetControlID(Contents.sizeFieldHash, FocusType.Keyboard, rtLabel);
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
                    return targets.Length == 1 ? Contents.notFullRectWarningLabel.text : Contents.notFullRectMultiEditWarningLabel.text;
            }
            return null;
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
            int id = GUIUtility.GetControlID(Contents.flipToggleHash, FocusType.Keyboard, r);
            r = EditorGUI.PrefixLabel(r, id, Contents.flipLabel);
            r.width = toggleOffset;
            FlipToggle(r, Contents.flipXLabel, m_FlipX);
            r.x += toggleOffset;
            FlipToggle(r, Contents.flipYLabel, m_FlipY);
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
                Undo.RecordObjects(targets, "Edit Constraints");
                property.boolValue = toggle;
            }

            EditorGUI.EndProperty();
        }


        private void CheckForErrors()
        {
            if (IsMaterialTextureAtlasConflict())
                ShowError("Material has CanUseSpriteAtlas=False tag. Sprite texture has atlasHint set. Rendering artifacts possible.");

            bool isTextureTiled;
            if (!DoesMaterialHaveSpriteTexture(out isTextureTiled))
                ShowError("Material does not have a _MainTex texture property. It is required for SpriteRenderer.");
            else
            {
                if (isTextureTiled)
                    ShowError("Material texture property _MainTex has offset/scale set. It is incompatible with SpriteRenderer.");
            }
        }

        private bool IsMaterialTextureAtlasConflict()
        {
            Material material = (target as SpriteRenderer).sharedMaterial;
            if (material == null)
                return false;
            string tag = material.GetTag("CanUseSpriteAtlas", false);
            if (tag.ToLower() == "false")
            {
                Sprite frame = m_Sprite.objectReferenceValue as Sprite;
                TextureImporter ti = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(frame)) as TextureImporter;
                if (ti != null && ti.spritePackingTag != null && ti.spritePackingTag.Length > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private bool DoesMaterialHaveSpriteTexture(out bool tiled)
        {
            tiled = false;

            Material material = (target as SpriteRenderer).sharedMaterial;
            if (material == null)
                return true;


            bool has = material.HasProperty("_MainTex");
            if (has)
            {
                Vector2 offset = material.GetTextureOffset("_MainTex");
                Vector2 scale = material.GetTextureScale("_MainTex");
                if (offset.x != 0 || offset.y != 0 || scale.x != 1 || scale.y != 1)
                    tiled = true;
            }

            return material.HasProperty("_MainTex");
        }

        private static void ShowError(string error)
        {
            var c = new GUIContent(error) {image = Contents.warningIcon};

            GUILayout.Space(5);
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(c, EditorStyles.wordWrappedMiniLabel);
            GUILayout.EndVertical();
        }
    }
}
