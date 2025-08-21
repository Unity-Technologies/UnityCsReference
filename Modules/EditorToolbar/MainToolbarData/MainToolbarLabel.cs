// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    public sealed class MainToolbarLabel : MainToolbarElement
    {
        const string k_ToolbarLabelClassName = "unity-editor-toolbar-label";

        public MainToolbarLabel(MainToolbarContent content)
        {
            this.content = content;
        }

        internal override VisualElement CreateElement()
        {
            var label = new VisualElement();
            new EditorToolbarContent(label, content.text, new EditorToolbarIcon(content.image));
            label.AddToClassList(EditorToolbar.elementClassName);
            label.AddToClassList(k_ToolbarLabelClassName);
            label.tooltip = content.tooltip;
            return label;
        }
    }
}
