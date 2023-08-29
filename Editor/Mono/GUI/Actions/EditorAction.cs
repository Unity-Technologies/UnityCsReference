// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor.EditorTools;

namespace UnityEditor.Actions
{
    public enum EditorActionResult
    {
        Canceled,
        Success
    }

    public abstract class EditorAction
    {
        bool m_IsFinished;

        internal event Action<EditorActionResult> actionFinished;

        public static T Start<T>() where T : EditorAction, new() => Start(new T());

        public static T Start<T>(T action) where T : EditorAction
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (action.m_IsFinished)
                return action;

            EditorToolManager.activeOverride = new EditorActionTool(action);

            return action;
        }

        public virtual void OnSceneGUI(SceneView sceneView) {}

        public void Finish(EditorActionResult result)
        {
            if (m_IsFinished)
                return;

            m_IsFinished = true;
            OnFinish(result);
            actionFinished?.Invoke(result);
        }

        protected virtual void OnFinish(EditorActionResult result) {}
    }
}
