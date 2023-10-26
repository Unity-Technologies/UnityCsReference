// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
    // Built-in DecoratorDrawers. See matching attributes in PropertyAttribute.cs

    [CustomPropertyDrawer(typeof(SpaceAttribute))]
    internal sealed class SpaceDrawer : DecoratorDrawer
    {
        public const string spaceDrawerClassName = "unity-space-drawer";

        public override float GetHeight()
        {
            return (attribute as SpaceAttribute).height;
        }

        public override VisualElement CreatePropertyGUI()
        {
            var spaceElement = new VisualElement();
            spaceElement.AddToClassList(spaceDrawerClassName);

            spaceElement.style.height = GetHeight();

            return spaceElement;
        }
    }

    [CustomPropertyDrawer(typeof(HeaderAttribute))]
    internal sealed class HeaderDrawer : DecoratorDrawer
    {
        public const string headerLabelClassName = "unity-header-drawer__label";

        public override void OnGUI(Rect position)
        {
            position.yMin += EditorGUIUtility.singleLineHeight * 0.5f;
            position = EditorGUI.IndentedRect(position);
            GUI.Label(position, (attribute as HeaderAttribute).header, EditorStyles.boldLabel);
        }

        public override float GetHeight()
        {
            float fullTextHeight = EditorStyles.boldLabel.CalcHeight(GUIContent.Temp((attribute as HeaderAttribute).header), 1.0f);
            int lines = 1;
            if ((attribute as HeaderAttribute).header != null)
                lines = (attribute as HeaderAttribute).header.Count(a => a == '\n') + 1;
            float eachLineHeight = fullTextHeight / lines;
            return EditorGUIUtility.singleLineHeight * 1.5f + (eachLineHeight * (lines - 1));
        }

        public override VisualElement CreatePropertyGUI()
        {
            var text = (attribute as HeaderAttribute).header;
            var label = new Label(text);

            label.AddToClassList(headerLabelClassName);

            return label;
        }
    }
}
