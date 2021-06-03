// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Net.Mime;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(MeshCollider))]
    [CanEditMultipleObjects]
    internal class MeshColliderEditor : Collider3DEditorBase
    {
        private SerializedProperty m_Mesh;
        private SerializedProperty m_Convex;
        private SerializedProperty m_CookingOptions;

        private static class Styles
        {
            public static readonly GUIContent isTriggerText = EditorGUIUtility.TrTextContent("Is Trigger", "Is this collider a trigger? Triggers are only supported on convex colliders.");
            public static readonly GUIContent convexText = EditorGUIUtility.TrTextContent("Convex", "Is this collider convex?");
            public static readonly GUIContent cookingOptionsText = EditorGUIUtility.TrTextContent("Cooking Options", "Options affecting the result of the mesh processing by the physics engine.");
            public static readonly GUIContent meshText = EditorGUIUtility.TrTextContent("Mesh", "Reference to the Mesh to use for collisions.");
        }

        public override void OnEnable()
        {
            base.OnEnable();

            m_Mesh = serializedObject.FindProperty("m_Mesh");
            m_Convex = serializedObject.FindProperty("m_Convex");
            m_CookingOptions = serializedObject.FindProperty("m_CookingOptions");
        }

        private MeshColliderCookingOptions GetCookingOptions()
        {
            return (MeshColliderCookingOptions)m_CookingOptions.intValue;
        }

        private void SetCookingOptions(MeshColliderCookingOptions cookingOptions)
        {
            m_CookingOptions.intValue = (int)cookingOptions;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_Convex, Styles.convexText);

            if (EditorGUI.EndChangeCheck() && m_Convex.boolValue == false)
            {
                m_IsTrigger.boolValue = false;
            }

            EditorGUI.indentLevel++;
            using (new EditorGUI.DisabledScope(!m_Convex.boolValue))
            {
                EditorGUILayout.PropertyField(m_IsTrigger, Styles.isTriggerText);
            }
            EditorGUI.indentLevel--;

            using (var horizontal = new EditorGUILayout.HorizontalScope())
            {
                using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, GUIContent.none, m_CookingOptions))
                {
                    EditorGUI.BeginChangeCheck();
                    var newOptions = (MeshColliderCookingOptions)EditorGUILayout.EnumFlagsField(Styles.cookingOptionsText, GetCookingOptions());
                    if (EditorGUI.EndChangeCheck())
                        SetCookingOptions(newOptions);
                }
            }

            EditorGUILayout.PropertyField(m_Material, BaseStyles.materialContent);

            EditorGUILayout.PropertyField(m_Mesh, Styles.meshText);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
