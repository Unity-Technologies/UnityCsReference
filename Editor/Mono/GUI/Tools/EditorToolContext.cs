// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.EditorTools
{
    // todo Make public when UI is finalized for displaying tool contexts
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    class EditorToolContextAttribute : Attribute
    {
        string m_Title;
        string m_Tooltip;

        public string title => m_Title;
        public string tooltip => m_Tooltip;

        EditorToolContextAttribute() {}

        public EditorToolContextAttribute(string title, string tooltip = "")
        {
            m_Title = title;
            m_Tooltip = tooltip;
        }
    }

    public abstract class EditorToolContext : ScriptableObject
    {
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
                        Debug.LogError($"Tool context \"{GetType()}\" resolved {tool} to an invalid EditorTool type. Resolved types must inherit EditorTool and not be abstract.");
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
                    throw new ArgumentException("EditorToolContext should only be used to resolve transform tools. View, Custom, and None are not applicable.");
            }
        }
    }
}
