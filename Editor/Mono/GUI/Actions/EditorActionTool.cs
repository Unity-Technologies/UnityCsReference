// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.EditorTools;
using UnityEngine;

namespace UnityEditor.Actions
{
    sealed class EditorActionTool : EditorTool
    {
        public EditorAction action { get; internal set; }

        public override GUIContent toolbarIcon => new GUIContent(action?.icon);

        public override void OnActivated()
        {
            base.OnActivated();

            if (action != null)
                action.actionFinished += OnActionFinished;
        }

        public override void OnToolGUI(EditorWindow window)
        {
            if (action != null)
                action?.OnSceneGUI(window as SceneView);
        }

        void OnActionFinished(EditorActionResult result)
        {
            action.actionFinished -= OnActionFinished;
            action = null;
            DestroyImmediate(this);
        }

        public override void OnWillBeDeactivated()
        {
            // If the tool is deactivate without being completed first
            action?.Finish(EditorActionResult.Canceled);
        }
    }
}
