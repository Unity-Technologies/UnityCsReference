// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.GraphToolkit.InternalBridge
{
    static class CommandMenuItemNames
    {
        public static string Cut => $"Cut {UnityEditor.Menu.GetHotkey("Edit/Cut")}";

        public static string Copy => $"Copy {UnityEditor.Menu.GetHotkey("Edit/Copy")}";
        public static string Paste => $"Paste {UnityEditor.Menu.GetHotkey("Edit/Paste")}";
        public static string Duplicate => $"Duplicate {UnityEditor.Menu.GetHotkey("Edit/Duplicate")}";
        public static string Delete => $"Delete {UnityEditor.Menu.GetHotkey("Edit/Delete")}";
        public static string FrameSelected => $"Frame Selection {UnityEditor.Menu.GetHotkey("Edit/Frame Selected in Window under Cursor")}";
        public static string Rename => $"Rename {UnityEditor.Menu.GetHotkey("Edit/Rename")}";
        public static string SelectAll => $"Select All {UnityEditor.Menu.GetHotkey("Edit/Select All")}";
    }
}
