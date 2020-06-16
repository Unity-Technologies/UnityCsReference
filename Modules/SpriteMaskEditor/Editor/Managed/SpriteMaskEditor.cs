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
        private SerializedProperty m_SpriteSortPoint;
        private AnimBool m_ShowCustomRangeValues;

        class Styles
        {
            public static readonly GUIContent spriteLabel = EditorGUIUtility.TrTextContent("Sprite", "The Sprite defining the mask");
            public static readonly GUIContent alphaCutoffLabel = EditorGUIUtility.TrTextContent("Alpha Cutoff", "The minimum alpha value used by the mask to select the area of influence defined over the mask's sprite.");
            public static readonly GUIContent isCustomRangeActive = EditorGUIUtility.TrTextContent("Custom Range", "Mask sprites from front to back sorting values only.");
            public static readonly GUIContent createSpriteMaskUndoString = EditorGUIUtility.TrTextContent("Create Sprite Mask");
            public static readonly GUIContent newSpriteMaskName = EditorGUIUtility.TrTextContent("New Sprite Mask");
            public static readonly GUIContent frontLabel = EditorGUIUtility.TrTextContent("Front");
            public static readonly GUIContent backLabel = EditorGUIUtility.TrTextContent("Back");
            public static readonly GUIContent spriteSortPointLabel = EditorGUIUtility.TrTextContent("Sprite Sort Point", "Determines which position of the Sprite which is used for sorting");
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
            m_SpriteSortPoint = serializedObject.FindProperty("m_SpriteSortPoint");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Sprite, Styles.spriteLabel);

            EditorGUILayout.Slider(m_AlphaCutoff, 0f, 1f, Styles.alphaCutoffLabel);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_IsCustomRangeActive, Styles.isCustomRangeActive);

            m_ShowCustomRangeValues.target = ShouldShowCustomRangeValues();
            if (EditorGUILayout.BeginFadeGroup(m_ShowCustomRangeValues.faded))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(Styles.frontLabel);
                SortingLayerEditorUtility.RenderSortingLayerFields(m_FrontSortingOrder, m_FrontSortingLayerID);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(Styles.backLabel);
                SortingLayerEditorUtility.RenderSortingLayerFields(m_BackSortingOrder, m_BackSortingLayerID);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();

            EditorGUILayout.PropertyField(m_SpriteSortPoint, Styles.spriteSortPointLabel);

            DrawRenderingLayer();

            serializedObject.ApplyModifiedProperties();
        }

        bool ShouldShowCustomRangeValues()
        {
            return m_IsCustomRangeActive.boolValue && !m_IsCustomRangeActive.hasMultipleDifferentValues;
        }
    }
}
