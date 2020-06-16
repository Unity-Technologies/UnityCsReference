// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UObject = UnityEngine.Object;

namespace UnityEditor.EditorTools
{
    public static class ToolManager
    {
        public static Type activeContextType => EditorToolManager.activeToolContextType;

        public static void SetActiveContext(Type context)
        {
            if (context != null && (!typeof(EditorToolContext).IsAssignableFrom(context) || context.IsAbstract))
                throw new ArgumentException("Type must be assignable to EditorToolContext, and not abstract.");
            EditorToolManager.activeToolContextType = context;
        }

        public static void SetActiveContext<T>() where T : EditorToolContext
        {
            SetActiveContext(typeof(T));
        }

        public static Type activeToolType
        {
            get
            {
                var tool = EditorToolManager.activeTool;
                return tool != null ? tool.GetType() : null;
            }
        }

        public static event Action activeToolChanging;

        public static event Action activeToolChanged;

        public static event Action activeContextChanging;

        public static event Action activeContextChanged;

        internal static void ActiveToolWillChange()
        {
            if (activeToolChanging != null)
                activeToolChanging();
#pragma warning disable 618
            EditorTools.ActiveToolWillChange();
#pragma warning restore 618
        }

        internal static void ActiveToolDidChange()
        {
            if (activeToolChanged != null)
                activeToolChanged();
#pragma warning disable 618
            EditorTools.ActiveToolDidChange();
#pragma warning restore 618
        }

        internal static void ActiveContextWillChange()
        {
            if (activeContextChanging != null)
                activeContextChanging();
        }

        internal static void ActiveContextDidChange()
        {
            if (activeContextChanged != null)
                activeContextChanged();
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
                var tool = EditorToolManager.GetCustomEditorToolOfType(type);

                if (tool == null)
                    throw new InvalidOperationException("The current selection does not contain any objects editable by the CustomEditor tool \"" + type + ".\"");

                SetActiveTool(tool);

                return;
            }

            SetActiveTool((EditorTool)EditorToolManager.GetSingleton(type));
        }

        public static void SetActiveTool(EditorTool tool)
        {
            EditorToolManager.activeTool = tool;
        }

        public static void RestorePreviousTool()
        {
            EditorToolManager.RestorePreviousTool();
        }

        public static void RestorePreviousPersistentTool()
        {
            var last = EditorToolManager.GetLastTool(x => x && !EditorToolUtility.IsCustomEditorTool(x.GetType()));

            if (last != null)
                SetActiveTool(last);
            else
                SetActiveTool<MoveTool>();
        }

        public static bool IsActiveTool(EditorTool tool)
        {
            return EditorToolManager.activeTool == tool;
        }
    }
}
