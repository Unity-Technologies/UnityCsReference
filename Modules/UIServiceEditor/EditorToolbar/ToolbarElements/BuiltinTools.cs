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

            EditorToolbarUtility.SetupChildrenAsButtonStrip(this);

            // Only show the contexts dropdown if there are non-builtin contexts available
            if (EditorToolUtility.toolContextsInProject > 1)
            {
                var contexts = new ToolContextButton();
                contexts.AddToClassList(EditorToolbarUtility.aloneStripElementClassName);
                Add(contexts);
            }

            // View and Builtin Transform Tools
            var builtin = new VisualElement() { name = "Builtin View and Transform Tools" };
            builtin.AddToClassList("toolbar-contents");
            Add(builtin);
            for (int i = 0; i < (int)Tool.Custom; i++)
            {
                var button = new ToolButton((Tool) i);
                button.displayChanged += () => EditorToolbarUtility.SetupChildrenAsButtonStrip(builtin);
                builtin.Add(button);
            }
            EditorToolbarUtility.SetupChildrenAsButtonStrip(builtin);

            // Custom global tools button (only shown if custom global tools exist in project)
            if (EditorToolUtility.GetNonBuiltinToolCount() > 0)
            {
                var customToolButton = new LastCustomToolButton();
                customToolButton.AddToClassList(EditorToolbarUtility.aloneStripElementClassName);
                Add(customToolButton);
            }

            // Component tools are last
            Add(new ComponentToolsStrip());
        }
    }
}
