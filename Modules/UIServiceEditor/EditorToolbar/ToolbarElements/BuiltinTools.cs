// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.EditorTools;
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

            EditorToolbarUtility.SetupChildrenAsButtonStrip(this);

            if (EditorToolUtility.GetNonBuiltinToolCount() > 0)
            {
                var customToolButton = new LastCustomToolButton();
                customToolButton.AddToClassList(EditorToolbarUtility.aloneStripElementClassName);
                Add(customToolButton);
            }

            // Only show the contexts dropdown if there are non-builtin contexts available
            if (EditorToolUtility.toolContextsInProject > 1)
            {
                var contexts = new ToolContextButton();
                contexts.AddToClassList(EditorToolbarUtility.aloneStripElementClassName);
                Insert(0, contexts);
            }

            Add(new ComponentToolsStrip());
        }
    }
}
