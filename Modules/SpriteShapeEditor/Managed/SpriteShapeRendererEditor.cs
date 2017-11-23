// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditorInternal;
using System.Collections.Generic;
using UnityEngine.Experimental.U2D;

namespace UnityEditor.Experimental.U2D
{
    [CustomEditor(typeof(SpriteShapeRenderer))]
    [CanEditMultipleObjects]
    internal class SpriteShapeRendererInspector : RendererEditorBase
    {
        private SerializedProperty m_Color;
        private SerializedProperty m_Material;
        private SerializedProperty m_MaskInteraction;

        private static Texture2D s_WarningIcon;
        private static class Contents
        {
            public static readonly GUIContent materialLabel = EditorGUIUtility.TextContent("Material|Material to be used by SpriteRenderer");
            public static readonly GUIContent colorLabel = EditorGUIUtility.TextContent("Color|Rendering color for the Sprite graphic");
            public static readonly Texture2D warningIcon = EditorGUIUtility.LoadIcon("console.warnicon");
        }

        public override void OnEnable()
        {
            base.OnEnable();
            m_Color = serializedObject.FindProperty("m_Color");
            m_Material = serializedObject.FindProperty("m_Materials.Array"); // Only allow to edit one material
            m_MaskInteraction = serializedObject.FindProperty("m_MaskInteraction");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            GUI.enabled = true;

            EditorGUILayout.PropertyField(m_Color, Contents.colorLabel, true);
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

            bool isTextureTiled;
            if (!DoesMaterialHaveSpriteTexture(out isTextureTiled))
                ShowError("Material does not have a _MainTex texture property. It is required for SpriteRenderer.");
            else
            {
                if (isTextureTiled)
                    ShowError("Material texture property _MainTex has offset/scale set. It is incompatible with SpriteRenderer.");
            }

            EditorGUILayout.PropertyField(m_MaskInteraction);
            RenderSortingLayerFields();

            serializedObject.ApplyModifiedProperties();
        }

        private bool DoesMaterialHaveSpriteTexture(out bool tiled)
        {
            tiled = false;

            Material material = (target as SpriteShapeRenderer).sharedMaterial;
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
            if (s_WarningIcon == null)
                s_WarningIcon = EditorGUIUtility.LoadIcon("console.warnicon");

            var c = new GUIContent(error) {image = s_WarningIcon};

            GUILayout.Space(5);
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(c, EditorStyles.wordWrappedMiniLabel);
            GUILayout.EndVertical();
        }
    }
}
