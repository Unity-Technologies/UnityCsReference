// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;

namespace UnityEditor.EditorTools
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("EditorTools has been deprecated. Use ToolManager instead (UnityUpgradable) -> ToolManager")]
    public static class EditorTools
    {
        public static Type activeToolType => ToolManager.activeToolType;

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
            ToolManager.SetActiveTool(type);
        }

        public static void SetActiveTool(EditorTool tool)
        {
            ToolManager.SetActiveTool(tool);
        }

        public static void RestorePreviousTool()
        {
            ToolManager.RestorePreviousTool();
        }

        public static void RestorePreviousPersistentTool()
        {
            ToolManager.RestorePreviousTool();
        }

        public static bool IsActiveTool(EditorTool tool)
        {
            return ToolManager.IsActiveTool(tool);
        }
    }
}
