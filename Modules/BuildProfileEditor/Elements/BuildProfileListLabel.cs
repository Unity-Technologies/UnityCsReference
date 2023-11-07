// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile.Elements
{
    /// <summary>
    /// List item showing a build profile name and icon in the <see cref="BuildProfileWindow"/>
    /// classic platform or build profile columns.
    /// </summary>
    internal class BuildProfileListLabel : VisualElement
    {
        readonly Image m_Icon;
        readonly Label m_Text;

        internal BuildProfileListLabel()
        {
            style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            m_Icon = new Image();
            m_Text = new Label()
            {
                style =
                {
                    flexGrow = 1
                }
            };

            m_Text.AddToClassList("px-small");
            m_Text.AddToClassList("lhs-sidebar-item-label");
            m_Icon.AddToClassList("lhs-sidebar-item-icon");
            AddToClassList("lhs-sidebar-item");
            AddToClassList("mb-small");

            Add(m_Icon);
            Add(m_Text);
        }

        internal void Set(string displayName, Texture2D icon)
        {
            m_Icon.image = icon;
            m_Text.text = displayName;
        }
    }
}
