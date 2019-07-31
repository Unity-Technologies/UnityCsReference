// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.U2D;

namespace UnityEditor.U2D
{
    [CustomEditor(typeof(SpriteShapeRenderer))]
    [CanEditMultipleObjects]
    internal class SpriteShapeRendererInspector : RendererEditorBase
    {
        private SerializedProperty m_Color;
        private SerializedProperty m_Material;
        private SerializedProperty m_MaskInteraction;

        private static class Styles
        {
            public static readonly GUIContent fillMaterialLabel = EditorGUIUtility.TrTextContent("Fill Material", "Fill Material to be used by SpriteShapeRenderer");
            public static readonly GUIContent edgeMaterialLabel = EditorGUIUtility.TrTextContent("Edge Material", "Edge Material to be used by SpriteShapeRenderer");
            public static readonly GUIContent colorLabel = EditorGUIUtility.TrTextContent("Color", "Rendering color for the Sprite graphic");
            public static readonly Texture2D warningIcon = EditorGUIUtility.LoadIcon("console.warnicon");

            public static readonly string mainTexErrorText = L10n.Tr("Material does not have a _MainTex texture property. It is required for SpriteShapeRenderer.");
            public static readonly string offsetScaleErrorText = L10n.Tr("Material texture property _MainTex has offset/scale set. It is incompatible with SpriteShapeRenderer.");
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

            EditorGUILayout.PropertyField(m_Color, Styles.colorLabel, true);
            EditorGUILayout.PropertyField(m_MaskInteraction);

            EditorGUILayout.PropertyField(m_Material.GetArrayElementAtIndex(0), Styles.fillMaterialLabel, true);
            EditorGUILayout.PropertyField(m_Material.GetArrayElementAtIndex(1), Styles.edgeMaterialLabel, true);

            bool isTextureTiled;
            if (!DoesFillMaterialHaveSpriteTexture(out isTextureTiled))
                ShowError(Styles.mainTexErrorText);
            else
            {
                if (isTextureTiled)
                    ShowError(Styles.offsetScaleErrorText);
            }

            Other2DSettingsGUI();

            serializedObject.ApplyModifiedProperties();
        }

        private bool DoesFillMaterialHaveSpriteTexture(out bool tiled)
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
            var c = new GUIContent(error) {image = Styles.warningIcon};

            GUILayout.Space(5);
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(c, EditorStyles.wordWrappedMiniLabel);
            GUILayout.EndVertical();
        }
    }
}
