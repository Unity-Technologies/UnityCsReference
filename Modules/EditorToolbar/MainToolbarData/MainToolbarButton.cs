// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    public sealed class MainToolbarButton : MainToolbarElement
    {
        readonly Action m_Action;

        public MainToolbarButton(MainToolbarContent content, Action action)
        {
            this.content = content;
            m_Action = action;
        }

        internal override VisualElement CreateElement()
        {
            var button = new EditorToolbarButton(content.text, content.image, m_Action);
            button.AddToClassList(EditorToolbar.elementClassName);
            button.text = content.text;
            button.icon = content.image;
            button.tooltip = content.tooltip;
            return button;
        }
    }
}
