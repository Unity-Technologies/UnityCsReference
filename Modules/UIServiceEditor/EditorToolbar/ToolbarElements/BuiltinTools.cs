// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    [EditorToolbarElement("Tools/Builtin Tools")]
    sealed class BuiltinToolsStrip : VisualElement
    {
        public BuiltinToolsStrip()
        {
            name = "BuiltinTools";

            Add(new ToolButton(Tool.View));
            Add(new ToolButton(Tool.Move));
            Add(new ToolButton(Tool.Rotate));
            Add(new ToolButton(Tool.Scale));
            Add(new ToolButton(Tool.Rect));
            Add(new ToolButton(Tool.Transform));
            Add(new LastCustomToolButton());

            EditorToolbarUtility.SetupChildrenAsButtonStrip(this);
        }
    }
}
