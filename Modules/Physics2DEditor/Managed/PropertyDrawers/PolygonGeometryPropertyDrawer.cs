// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.LowLevelPhysics2D;
using UnityEngine.UIElements;

namespace UnityEditor.LowLevelPhysics2D
{
    [CustomPropertyDrawer(typeof(PolygonGeometry))]
    sealed class PolygonGeometryPropertyDrawer : PropertyDrawer
    {
        #region UITK

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            var foldout = new Foldout { text = property.displayName, value = false, viewDataKey = typeof(PolygonGeometryPropertyDrawer).ToString() };
            root.Add(foldout);

            foldout.Add(new PropertyField(property.FindPropertyRelative(nameof(PolygonGeometry.vertices))));
            foldout.Add(new PropertyField(property.FindPropertyRelative(nameof(PolygonGeometry.m_Count))));
            foldout.Add(new PropertyField(property.FindPropertyRelative(nameof(PolygonGeometry.m_Radius))));

            var readonlyElement = new VisualElement { enabledSelf = false, viewDataKey = typeof(PolygonGeometryPropertyDrawer).ToString() + "_hidden1" };
            foldout.Add(readonlyElement);
            readonlyElement.Add(new PropertyField(property.FindPropertyRelative(nameof(PolygonGeometry.normals))));
            readonlyElement.Add(new PropertyField(property.FindPropertyRelative(nameof(PolygonGeometry.m_Centroid))));

            return root;
        }

        #endregion

        #region IMGUI

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            var verticesProperty = property.FindPropertyRelative(nameof(PolygonGeometry.vertices));
            var normalsProperty = property.FindPropertyRelative(nameof(PolygonGeometry.normals));

            return EditorGUIUtility.singleLineHeight
                + EditorGUIUtility.standardVerticalSpacing + EditorGUI.GetPropertyHeight(verticesProperty, true)
                + (EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight) * 2
                + EditorGUIUtility.standardVerticalSpacing + EditorGUI.GetPropertyHeight(normalsProperty, true)
                + EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                var verticesProperty = property.FindPropertyRelative(nameof(PolygonGeometry.vertices));
                var countProperty = property.FindPropertyRelative(nameof(PolygonGeometry.m_Count));
                var radiusProperty = property.FindPropertyRelative(nameof(PolygonGeometry.m_Radius));
                var normalsProperty = property.FindPropertyRelative(nameof(PolygonGeometry.normals));
                var centroidProperty = property.FindPropertyRelative(nameof(PolygonGeometry.m_Centroid));

                float y = foldoutRect.yMax + EditorGUIUtility.standardVerticalSpacing;
                var lineHeight = EditorGUIUtility.singleLineHeight;
                var spacing = EditorGUIUtility.standardVerticalSpacing;

                var verticesHeight = EditorGUI.GetPropertyHeight(verticesProperty, true);
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, verticesHeight), verticesProperty, true);
                y += verticesHeight + spacing;

                EditorGUI.PropertyField(new Rect(position.x, y, position.width, lineHeight), countProperty, false);
                y += lineHeight + spacing;

                EditorGUI.PropertyField(new Rect(position.x, y, position.width, lineHeight), radiusProperty, false);
                y += lineHeight + spacing;

                EditorGUI.BeginDisabledGroup(true);

                var normalsHeight = EditorGUI.GetPropertyHeight(normalsProperty, true);
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, normalsHeight), normalsProperty, true);
                y += normalsHeight + spacing;

                EditorGUI.PropertyField(new Rect(position.x, y, position.width, lineHeight), centroidProperty, false);

                EditorGUI.EndDisabledGroup();

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        #endregion
    }
}
