// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor.Implementation
{
    class GraphToolkitShortcuts<T> : IShortcutContextHandler, IShortcutContext where T : Graph
    {
        public bool active => EditorWindow.focusedWindow is GraphViewEditorWindowImp graphWindow && graphWindow.GraphTool.ToolState.GraphModel is GraphModelImp graphModel && graphModel.Graph is T;

        void IShortcutContextHandler.HandleShortcut(EventBase e)
        {
            var window = EditorWindow.focusedWindow as GraphViewEditorWindowImp;
            if (window == null)
            {
                return;
            }

            if (window.GraphTool.ToolState.GraphModel is GraphModelImp graphModel && graphModel.Graph is T)
            {
                e.target = window.rootVisualElement.panel.focusController.focusedElement ?? window.rootVisualElement;
                window.rootVisualElement.SendEvent(e);
            }
        }
    }
}
