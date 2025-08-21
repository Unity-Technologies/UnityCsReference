// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine.LowLevelPhysics2D;
using UnityEngine.UIElements;

namespace UnityEditor.LowLevelPhysics2D
{
    [CustomPropertyDrawer(typeof(PolygonGeometry))]
    sealed class PolygonGeometryPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            var foldout = new Foldout { text = property.displayName, viewDataKey = typeof(PolygonGeometryPropertyDrawer).ToString() };
            root.Add(foldout);
            
            foldout.Add(new PropertyField(property.FindPropertyRelative(nameof(PolygonGeometry.m_Count))));
            foldout.Add(new PropertyField(property.FindPropertyRelative(nameof(PolygonGeometry.vertices))));
            foldout.Add(new PropertyField(property.FindPropertyRelative(nameof(PolygonGeometry.m_Radius))));

            var readonlyElement = new VisualElement { enabledSelf = false, viewDataKey = typeof(PolygonGeometryPropertyDrawer).ToString() + "_hidden1" };
            foldout.Add(readonlyElement);
            readonlyElement.Add(new PropertyField(property.FindPropertyRelative(nameof(PolygonGeometry.normals))));
            readonlyElement.Add(new PropertyField(property.FindPropertyRelative(nameof(PolygonGeometry.m_Centroid))));
            
            return root;
        }        
    }
}
