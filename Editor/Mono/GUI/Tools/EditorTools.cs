// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UObject = UnityEngine.Object;

namespace UnityEditor.EditorTools
{
    public static class EditorTools
    {
        public static Type activeToolType
        {
            get
            {
                var tool = EditorToolContext.activeTool;
                return tool != null ? tool.GetType() : null;
            }
        }

        public static event Action activeToolChanging;
        public static event Action activeToolChanged;

        internal static void ActiveToolWillChange()
        {
            if (activeToolChanging != null)
                activeToolChanging();
        }

        internal static void ActiveToolDidChange()
        {
            if (activeToolChanged != null)
                activeToolChanged();
        }

        public static void SetActiveTool<T>() where T : EditorTool
        {
            SetActiveTool(typeof(T));
        }

        public static void SetActiveTool(Type type)
        {
            if (!typeof(EditorTool).IsAssignableFrom(type) || type.IsAbstract)
                throw new ArgumentException("Type must be assignable to EditorTool, and not abstract.");

            var attrib = EditorToolUtility.GetEditorToolAttribute(type);

            if (attrib?.targetType != null)
            {
                var tool = EditorToolContext.GetCustomEditorToolOfType(type);

                if (tool == null)
                    throw new InvalidOperationException("The current selection does not contain any objects editable by the CustomEditor tool \"" + type + ".\"");

                SetActiveTool(tool);

                return;
            }

            SetActiveTool(EditorToolContext.GetSingleton(type));
        }

        public static void SetActiveTool(EditorTool tool)
        {
            EditorToolContext.activeTool = tool;
        }

        public static void RestorePreviousTool()
        {
            EditorToolContext.RestorePreviousTool();
        }

        public static bool IsActiveTool(EditorTool tool)
        {
            return EditorToolContext.activeTool == tool;
        }
    }
}
