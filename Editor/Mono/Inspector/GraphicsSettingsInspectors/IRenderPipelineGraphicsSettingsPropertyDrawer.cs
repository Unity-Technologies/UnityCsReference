// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.Inspector.GraphicsSettingsInspectors
{
    [CustomPropertyDrawer(typeof(IRenderPipelineGraphicsSettings), useForChildren: true)]
    class IRenderPipelineGraphicsSettingsPropertyDrawer : PropertyDrawer
    {
        public static bool IsEmpty(SerializedProperty property, out string warnings)
        {
            if (!property.hasVisibleChildren)
            {
                warnings = $"This {nameof(IRenderPipelineGraphicsSettings)} has no visible children. Consider using {nameof(HideInInspector)} if you want to completely hide the setting.";
                return true;
            }

            warnings = string.Empty;
            return false;
        }

        // Used in Unit tests
        public static IEnumerable<SerializedProperty> VisibleChildrenEnumerator(SerializedProperty property)
        {
            if (property.hasVisibleChildren)
            {
                var iterator = property.Copy();
                var end = iterator.GetEndProperty();

                iterator.NextVisible(true); // Move to the first child property
                do
                {
                    yield return iterator;
                    iterator.NextVisible(false);
                } while (!SerializedProperty.EqualContents(iterator, end)); // Move to the next sibling property
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (IsEmpty(property, out var warnings))
            {
                EditorGUI.HelpBox(position, warnings, MessageType.Warning);
                return;
            }

            foreach (var child in VisibleChildrenEnumerator(property))
            {
                EditorGUI.PropertyField(position, child);
                position.y += EditorGUI.GetPropertyHeight(property) + EditorGUIUtility.standardVerticalSpacing;
            }
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new();

            if (IsEmpty(property, out var warnings))
            {
                root.Add(new HelpBox(warnings, HelpBoxMessageType.Warning));
                return root;
            }

            foreach (var child in VisibleChildrenEnumerator(property))
            {
                root.Add(new PropertyField(child));
            }

            return root;
        }
    }
}
