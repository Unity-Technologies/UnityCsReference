// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.AI;

namespace UnityEditor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(NavMeshObstacle))]
    internal class NavMeshObstacleInspector : Editor
    {
        private SerializedProperty m_Shape;
        private SerializedProperty m_Center;
        private SerializedProperty m_Extents;
        private SerializedProperty m_Carve;
        private SerializedProperty m_MoveThreshold;
        private SerializedProperty m_TimeToStationary;
        private SerializedProperty m_CarveOnlyStationary;

        static class Styles
        {
            public static readonly GUIContent Shape = EditorGUIUtility.TrTextContent("Shape", "The shape of the obstacle, applied to both carving and avoidance.");
            public static readonly GUIContent Center = EditorGUIUtility.TrTextContent("Center", "The center of the obstacle, specified in the object's local space.");
            public static readonly GUIContent Size = EditorGUIUtility.TrTextContent("Size", "The size of the obstacle, measured in the object's local space.");
            public static readonly GUIContent Carve = EditorGUIUtility.TrTextContent("Carve", "This obstacle cuts a hole in the NavMesh around it.");
            public static readonly GUIContent Radius = EditorGUIUtility.TrTextContent("Radius", "Radius of the obstacle's capsule shape.");
            public static readonly GUIContent Height = EditorGUIUtility.TrTextContent("Height", "Height of the obstacle's capsule shape.");
            public static readonly float RadiusAndHeightLabelsWidth = 45f;
        }

        void OnEnable()
        {
            m_Shape = serializedObject.FindProperty("m_Shape");
            m_Center = serializedObject.FindProperty("m_Center");
            m_Extents = serializedObject.FindProperty("m_Extents");
            m_Carve = serializedObject.FindProperty("m_Carve");
            m_MoveThreshold = serializedObject.FindProperty("m_MoveThreshold");
            m_TimeToStationary = serializedObject.FindProperty("m_TimeToStationary");
            m_CarveOnlyStationary = serializedObject.FindProperty("m_CarveOnlyStationary");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_Shape, Styles.Shape);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                (target as NavMeshObstacle).FitExtents();
                serializedObject.Update();
            }

            EditorGUILayout.PropertyField(m_Center, Styles.Center);

            if (m_Shape.enumValueIndex == 0)
            {
                // NavMeshObstacleShape : kObstacleShapeCapsule
                var r = EditorGUILayout.GetControlRect(true, EditorGUI.kSingleLineHeight * 2 + EditorGUI.kControlVerticalSpacing);
                EditorGUI.BeginProperty(r, GUIContent.none, m_Extents);

                Rect valueRect = EditorGUI.PrefixLabel(r, Styles.Size);

                float oldLabelWidth = EditorGUIUtility.labelWidth;
                int oldIndentLevel = EditorGUI.indentLevel;
                EditorGUIUtility.labelWidth = Styles.RadiusAndHeightLabelsWidth;
                EditorGUI.indentLevel = 0;

                EditorGUI.BeginChangeCheck();
                valueRect.height = EditorGUI.kSingleLineHeight;
                float radius = EditorGUI.FloatField(valueRect, Styles.Radius, m_Extents.vector3Value.x);
                valueRect.y += EditorGUI.kSingleLineHeight + EditorGUI.kControlVerticalSpacing;
                float height = EditorGUI.FloatField(valueRect, Styles.Height, m_Extents.vector3Value.y * 2.0f);
                if (EditorGUI.EndChangeCheck())
                {
                    m_Extents.vector3Value = new Vector3(radius, height / 2.0f, radius);
                }

                EditorGUI.indentLevel = oldIndentLevel;
                EditorGUIUtility.labelWidth = oldLabelWidth;

                EditorGUI.EndProperty();
            }
            else if (m_Shape.enumValueIndex == 1)
            {
                // NavMeshObstacleShape : kObstacleShapeBox
                var r = EditorGUILayout.GetControlRect(true, (EditorGUIUtility.wideMode ? 1 : 2) * EditorGUI.kSingleLineHeight + EditorGUI.kControlVerticalSpacing);
                EditorGUI.BeginProperty(r, GUIContent.none, m_Extents);

                EditorGUI.BeginChangeCheck();
                Vector3 size = m_Extents.vector3Value * 2.0f;
                size = EditorGUI.Vector3Field(r, Styles.Size, size);
                if (EditorGUI.EndChangeCheck())
                {
                    m_Extents.vector3Value = size / 2.0f;
                }
                EditorGUI.EndProperty();
            }

            EditorGUILayout.PropertyField(m_Carve, Styles.Carve);

            if (m_Carve.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_MoveThreshold);
                EditorGUILayout.PropertyField(m_TimeToStationary);
                EditorGUILayout.PropertyField(m_CarveOnlyStationary);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
