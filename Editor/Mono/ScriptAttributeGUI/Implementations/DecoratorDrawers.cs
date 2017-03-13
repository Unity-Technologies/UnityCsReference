// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
            position.y += 8;
            position = EditorGUI.IndentedRect(position);
            GUI.Label(position, (attribute as HeaderAttribute).header, EditorStyles.boldLabel);
        }

        public override float GetHeight()
        {
            return 24;
        }
    }
}
