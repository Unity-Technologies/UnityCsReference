// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using static UnityEditor.EditorGUI;

namespace UnityEditor
{
    [CustomEditor(typeof(PhysicsMaterial))]
    [CanEditMultipleObjects]
    internal class PhysicsMaterialEditor : Editor
    {
        private class Styles
        {
            public static GUIContent dynamicFriction = EditorGUIUtility.TrTextContent("Dynamic Friction", "How Unity should combine the Friction values of both Colliders in a collision pair to calculate the total friction between them enum { Average = 0, Minimum = 1, Multiply = 2, Maximum = 3 }");
            public static GUIContent staticFriction = EditorGUIUtility.TrTextContent("Static Friction", "Use the calculated tensor or set it directly.");
            public static GUIContent bounciness = EditorGUIUtility.TrTextContent("Bounciness", "How bouncy the Collider’s surface is, defined by how much speed the other Collider retains after collision. range { 0, 1 }");
            public static GUIContent frictionCombine = EditorGUIUtility.TrTextContent("Friction Combine", "How Unity should combine the Friction values of both Colliders in a collision pair to calculate the total friction between them enum { Average = 0, Minimum = 1, Multiply = 2, Maximum = 3 }");
            public static GUIContent bounceCombine = EditorGUIUtility.TrTextContent("Bounce Combine", "How Unity should combine the Bounce values of both Colliders in a collision pair to calculate the total bounciness between them enum { Average = 0, Minimum = 1, Multiply = 2, Maximum = 3 }");
        }

        SerializedProperty m_DynamicFriction;
        SerializedProperty m_StaticFriction;
        SerializedProperty m_Bounciness;
        SerializedProperty m_FrictionCombine;
        SerializedProperty m_BounceCombine;

        public void OnEnable()
        {
            m_DynamicFriction = serializedObject.FindProperty("m_DynamicFriction");
            m_StaticFriction = serializedObject.FindProperty("m_StaticFriction");
            m_Bounciness = serializedObject.FindProperty("m_Bounciness");
            m_FrictionCombine = serializedObject.FindProperty("m_FrictionCombine");
            m_BounceCombine = serializedObject.FindProperty("m_BounceCombine");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using(var changed = new ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(m_DynamicFriction, Styles.dynamicFriction);
                EditorGUILayout.PropertyField(m_StaticFriction, Styles.staticFriction);
                EditorGUILayout.PropertyField(m_Bounciness, Styles.bounciness);
                EditorGUILayout.PropertyField(m_FrictionCombine, Styles.frictionCombine);
                EditorGUILayout.PropertyField(m_BounceCombine, Styles.bounceCombine);

                if (changed.changed)
                    serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
