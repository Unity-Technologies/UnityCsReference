// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEngine.UIElements;

using UObject = UnityEngine.Object;

namespace UnityEditor.Toolbars
{
    [EditorToolbarElement("Tools/Component Tools")]
    sealed class ComponentToolsStrip : VisualElement
    {
        static List<EditorTool> s_EditorToolModes = new List<EditorTool>();
        static Dictionary<UObject, List<EditorTool>> s_SortedTools = new Dictionary<UObject, List<EditorTool>>();

        public ComponentToolsStrip()
        {
            name = "ComponentTools";
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            Refresh();
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            EditorToolManager.availableComponentToolsChanged += Refresh;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            EditorToolManager.availableComponentToolsChanged -= Refresh;
            s_EditorToolModes.Clear();
        }

        void Refresh()
        {
            for (int i = childCount - 1; i > -1; i--)
                RemoveAt(i);

            AddToClassList("toolbar-contents");

            var contexts = new VisualElement() { name = "Contexts" };
            contexts.AddToClassList("toolbar-contents");

            if ((EditorToolManager.GetCustomEditorToolsCount(false) + EditorToolManager.availableComponentContextCount) < 1)
            {
                var disabled = new Label("...");
                disabled.AddToClassList("overlay-centered-disabled-text");
                Add(disabled);
                return;
            }

            foreach (var ctx in EditorToolManager.componentContexts)
                contexts.Add(new EditorToolContextButton<EditorToolContext>(ctx.GetEditor<EditorToolContext>()));
            EditorToolbarUtility.SetupChildrenAsButtonStrip(contexts);

            Add(contexts);

            EditorToolManager.GetComponentToolsForSharedTracker(s_EditorToolModes);
            s_SortedTools.Clear();

            foreach (var tool in s_EditorToolModes)
            {
                var component = tool.target;
                if (!s_SortedTools.TryGetValue(component, out List<EditorTool> editorTools))
                {
                    editorTools = new List<EditorTool>();
                    s_SortedTools.Add(component, editorTools);
                }
                editorTools.Add(tool);
            }

            foreach (var kvp in s_SortedTools)
            {
                var tools = new VisualElement() { name = "Component Tools" };
                tools.AddToClassList("toolbar-contents");

                foreach (var tool in kvp.Value)
                    tools.Add(new ComponentToolButton<EditorTool>(tool));
                EditorToolbarUtility.SetupChildrenAsButtonStrip(tools);

                Add(tools);
            }
        }
    }
}
