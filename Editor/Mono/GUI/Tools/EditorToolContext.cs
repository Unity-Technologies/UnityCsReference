// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.EditorTools
{
    public abstract class EditorToolContext : ScriptableObject, IEditor
    {
        [HideInInspector]
        [SerializeField]
        internal UnityObject[] m_Targets;

        [HideInInspector]
        [SerializeField]
        internal UnityObject m_Target;

        public IEnumerable<UnityObject> targets => m_Targets != null && m_Targets.Length > 0
            ? m_Targets
            : Selection.objects;

        public UnityObject target => m_Target == null ? Selection.activeObject : m_Target;

        public virtual void OnActivated() {}

        public virtual void OnWillBeDeactivated() {}

        void IEditor.SetTarget(UnityObject value) => m_Target = value;

        void IEditor.SetTargets(UnityObject[] value) => m_Targets = value;

        public virtual void OnToolGUI(EditorWindow window) {}

        public Type ResolveTool(Tool tool)
        {
            switch (tool)
            {
                case Tool.None:
                    return typeof(NoneTool);

                case Tool.View:
                    return typeof(ViewModeTool);

                case Tool.Custom:
                    return null;

                default:
                    var resolved = GetEditorToolType(tool);

                    // Returning null is valid here, but types that do not inherit EditorTool or are abstract are not.
                    if (resolved != null && (!typeof(EditorTool).IsAssignableFrom(resolved) || resolved.IsAbstract))
                        Debug.LogError($"Tool context \"{GetType()}\" resolved {tool} to an invalid EditorTool type. " +
                            $"Resolved types must inherit EditorTool and not be abstract.");
                    else
                        return resolved;
                    return null;
            }
        }

        protected virtual Type GetEditorToolType(Tool tool)
        {
            switch (tool)
            {
                case Tool.Move:
                    return typeof(MoveTool);
                case Tool.Rotate:
                    return typeof(RotateTool);
                case Tool.Scale:
                    return typeof(ScaleTool);
                case Tool.Rect:
                    return typeof(RectTool);
                case Tool.Transform:
                    return typeof(TransformTool);
                default:
                    throw new ArgumentException("EditorToolContext should only be used to resolve transform tools. " +
                        "View, Custom, and None are not applicable.");
            }
        }
    }
}
