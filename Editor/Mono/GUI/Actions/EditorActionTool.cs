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

        EditorActionTool() { }

        public EditorActionTool(EditorAction action)
        {
            this.action = action;
            this.action.actionFinished += OnActionFinished;
        }

        public void OnGUI(EditorWindow window)
        {
            if (!(window is SceneView sceneView))
                return;

            var evt = Event.current;

            if(evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape)
            {
                Cancel();
                evt.Use();
                return;
            }

            action?.OnSceneGUI(sceneView);
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
            action?.Finish(m_IsCancelled ? EditorActionResult.Canceled : EditorActionResult.Success);
            action = null;
            EditorToolManager.activeOverride = null;
        }
    }
}
