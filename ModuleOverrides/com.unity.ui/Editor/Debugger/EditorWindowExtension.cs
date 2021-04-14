// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.UIElements.Debugger
{
    internal static class EditorWindowExtensions
    {
        internal static bool CanDebugView(this EditorWindow editorWindow, HostView hostView)
        {
            if (hostView == null)
                return true;
            if (hostView.actualView == editorWindow)
                return false;
            if (editorWindow is UIElementsEventsDebugger)
                return !(hostView.actualView is UIElementsDebugger);
            return true;
        }
    }
}
