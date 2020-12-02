// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine;

namespace UnityEditor
{
    // Built-in DecoratorDrawers. See matching attributes in PropertyAttribute.cs

    [CustomPropertyDrawer(typeof(SpaceAttribute))]
    internal sealed class SpaceDrawer : DecoratorDrawer
    {
        public override float GetHeight()
        {
            return (attribute as SpaceAttribute).height;
        }
    }

    [CustomPropertyDrawer(typeof(HeaderAttribute))]
    internal sealed class HeaderDrawer : DecoratorDrawer
    {
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
    }
}
