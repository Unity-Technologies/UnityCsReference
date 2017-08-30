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
        private SerializedProperty m_SkinWidth;

        static class Texts
        {
            public static GUIContent isTriggerText = new GUIContent("Is Trigger", "Is this collider a trigger? Triggers are only supported on convex colliders.");
            public static GUIContent convextText = new GUIContent("Convex", "Is this collider convex?");
            public static GUIContent skinWidthText = new GUIContent("Skin Width", "How far out to inflate the mesh when building collision mesh.");
            public static GUIContent cookingOptionsText = new GUIContent("Cooking Options", "Options affecting the result of the mesh processing by the physics engine.");
        }

        public override void OnEnable()
        {
            base.OnEnable();

            m_Mesh = serializedObject.FindProperty("m_Mesh");
            m_Convex = serializedObject.FindProperty("m_Convex");
            m_CookingOptions = serializedObject.FindProperty("m_CookingOptions");
            m_SkinWidth = serializedObject.FindProperty("m_SkinWidth");
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
            EditorGUILayout.PropertyField(m_Convex, Texts.convextText);

            if (EditorGUI.EndChangeCheck() && m_Convex.boolValue == false)
            {
                m_IsTrigger.boolValue = false;
                SetCookingOptions(GetCookingOptions() & (~MeshColliderCookingOptions.InflateConvexMesh));
            }

            EditorGUI.indentLevel++;
            using (new EditorGUI.DisabledScope(!m_Convex.boolValue))
            {
                EditorGUILayout.PropertyField(m_IsTrigger, Texts.isTriggerText);
            }
            EditorGUI.indentLevel--;

            if (m_Convex.boolValue && (GetCookingOptions() & MeshColliderCookingOptions.InflateConvexMesh) != 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_SkinWidth, Texts.skinWidthText);
                EditorGUI.indentLevel--;
            }

            SetCookingOptions((MeshColliderCookingOptions)EditorGUILayout.EnumFlagsField(Texts.cookingOptionsText, GetCookingOptions()));

            EditorGUILayout.PropertyField(m_Material);

            EditorGUILayout.PropertyField(m_Mesh);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
