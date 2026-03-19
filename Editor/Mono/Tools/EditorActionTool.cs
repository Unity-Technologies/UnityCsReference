// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.EditorTools;
using UnityEngine;

namespace UnityEditor.Actions
{
    sealed class EditorActionTool : IDisposable
    {
        public EditorAction action { get; internal set; }
        bool m_IsCancelled;
        Type m_ToolOwner;

        EditorActionTool() { }

        public EditorActionTool(EditorAction action, Type toolOwner)
        {
            this.action = action;
            this.action.actionFinished += OnActionFinished;
            m_ToolOwner = toolOwner;

            Selection.selectionChanged += Dispose;
        }

        public void OnGUI(EditorWindow window)
        {
            var evt = Event.current;

            if(evt.type == EventType.KeyDown)
            {
                if (evt.keyCode == KeyCode.Escape)
                {
                    Cancel();
                    evt.Use();
                    return;
                }

                if (evt.keyCode == KeyCode.Return)
                {
                    Dispose();
                    evt.Use();
                    return;
                }
            }

            if (window is SceneView sceneView)
                action?.OnSceneGUI(sceneView);
            if (window is EditorToolWindowBase toolOwnerWindow)
                action?.OnToolOwnerGUI(toolOwnerWindow);
        }

        void OnActionFinished(EditorActionResult result) => Dispose();

        public void Cancel()
        {
            m_IsCancelled = true;
            Dispose();
        }

        public void Dispose()
        {
            if (action == null)
                return;
            action.actionFinished -= OnActionFinished;
            Selection.selectionChanged -= Dispose;

            action?.Finish(m_IsCancelled ? EditorActionResult.Canceled : EditorActionResult.Success);
            action = null;
            EditorToolManager.SetActiveOverride(null, m_ToolOwner);
        }
    }
}
