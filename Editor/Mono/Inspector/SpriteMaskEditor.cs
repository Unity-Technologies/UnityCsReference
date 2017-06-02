// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEngine;
using UnityEditor.AnimatedValues;

namespace UnityEditor
{
    [CustomEditor(typeof(SpriteMask))]
    [CanEditMultipleObjects]
    internal class SpriteMaskEditor : RendererEditorBase
    {
        private SerializedProperty m_Sprite;
        private SerializedProperty m_AlphaCutoff;
        private SerializedProperty m_IsCustomRangeActive;
        private SerializedProperty m_FrontSortingOrder;
        private SerializedProperty m_FrontSortingLayerID;
        private SerializedProperty m_BackSortingOrder;
        private SerializedProperty m_BackSortingLayerID;
        private AnimBool m_ShowCustomRangeValues;

        private static class Contents
        {
            public static readonly GUIContent spriteLabel = EditorGUIUtility.TextContent("Sprite|The Sprite defining the mask");
            public static readonly GUIContent alphaCutoffLabel = EditorGUIUtility.TextContent("Alpha Cutoff|The minimum alpha value used by the mask to select the area of influence defined over the mask's sprite.");
            public static readonly GUIContent isCustomRangeActive = EditorGUIUtility.TextContent("Custom Range|Mask sprites from front to back sorting values only.");
            public static readonly GUIContent createSpriteMaskUndoString = EditorGUIUtility.TextContent("Create Sprite Mask");
            public static readonly GUIContent newSpriteMaskName = EditorGUIUtility.TextContent("New Sprite Mask");
            public static readonly GUIContent frontLabel = EditorGUIUtility.TextContent("Front");
            public static readonly GUIContent backLabel = EditorGUIUtility.TextContent("Back");
        }

        [MenuItem("GameObject/2D Object/Sprite Mask")]
        static void CreateSpriteMaskGameObject()
        {
            var go = new GameObject("", typeof(SpriteMask));
            if (Selection.activeObject is Sprite)
                go.GetComponent<SpriteMask>().sprite = (Sprite)Selection.activeObject;
            else if (Selection.activeObject is Texture2D)
            {
                var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                    if (sprite != null)
                        go.GetComponent<SpriteMask>().sprite = sprite;
                }
            }
            else if (Selection.activeObject is GameObject)
            {
                var activeGO = (GameObject)Selection.activeObject;
                var prefabType = PrefabUtility.GetPrefabType(activeGO);
                if (prefabType != PrefabType.Prefab && prefabType != PrefabType.ModelPrefab)
                    GameObjectUtility.SetParentAndAlign(go, activeGO);
            }
            go.name = GameObjectUtility.GetUniqueNameForSibling(go.transform.parent, Contents.newSpriteMaskName.text);
            Undo.RegisterCreatedObjectUndo(go, Contents.createSpriteMaskUndoString.text);
            Selection.activeGameObject = go;
        }

        public override void OnEnable()
        {
            base.OnEnable();

            m_Sprite = serializedObject.FindProperty("m_Sprite");
            m_AlphaCutoff = serializedObject.FindProperty("m_MaskAlphaCutoff");
            m_IsCustomRangeActive = serializedObject.FindProperty("m_IsCustomRangeActive");
            m_FrontSortingOrder = serializedObject.FindProperty("m_FrontSortingOrder");
            m_FrontSortingLayerID = serializedObject.FindProperty("m_FrontSortingLayerID");
            m_BackSortingOrder = serializedObject.FindProperty("m_BackSortingOrder");
            m_BackSortingLayerID = serializedObject.FindProperty("m_BackSortingLayerID");
            m_ShowCustomRangeValues = new AnimBool(ShouldShowCustomRangeValues());
            m_ShowCustomRangeValues.valueChanged.AddListener(Repaint);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Sprite, Contents.spriteLabel);

            EditorGUILayout.Slider(m_AlphaCutoff, 0f, 1f, Contents.alphaCutoffLabel);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_IsCustomRangeActive, Contents.isCustomRangeActive);

            m_ShowCustomRangeValues.target = ShouldShowCustomRangeValues();
            if (EditorGUILayout.BeginFadeGroup(m_ShowCustomRangeValues.faded))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(Contents.frontLabel);
                SortingLayerEditorUtility.RenderSortingLayerFields(m_FrontSortingOrder, m_FrontSortingLayerID);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(Contents.backLabel);
                SortingLayerEditorUtility.RenderSortingLayerFields(m_BackSortingOrder, m_BackSortingLayerID);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();

            serializedObject.ApplyModifiedProperties();
        }

        bool ShouldShowCustomRangeValues()
        {
            return m_IsCustomRangeActive.boolValue && !m_IsCustomRangeActive.hasMultipleDifferentValues;
        }
    }
}

