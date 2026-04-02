// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;
using UnityEngine.UIElements.UIR;

namespace UnityEditor.UIElements.Debugger
{
    internal static class UIRDebugUtility
    {
        public static UIRenderDevice GetUIRenderDevice(IPanel panel)
        {
            UIRRepaintUpdater updater = GetUIRRepaintUpdater(panel);
            return updater?.renderTreeManager?.device as UIRenderDevice;
        }

        private static UIRRepaintUpdater GetUIRRepaintUpdater(IPanel panel)
        {
            var p = panel as Panel;
            return p.GetUpdater(VisualTreeUpdatePhase.Repaint) as UIRRepaintUpdater;
        }
    }

    internal static class VisualElementUIRExtension
    {
        internal static string DebugName(this VisualElement ve)
        {
            string t = ve.GetType() == typeof(VisualElement) ? string.Empty : (ve.GetType().Name + " ");
            string n = string.IsNullOrEmpty(ve.name) ? string.Empty : ("#" + ve.name + " ");
            string res = t + n + (ve.classListCount > 0 ? ("." + string.Join(",.", ve.GetClasses())) : string.Empty);
            if (res == string.Empty)
                return ve.GetType().Name;
            if (ve.renderHints != RenderHints.None)
                res += $" [{ve.renderHints}]";
            return res + " (" + ve.controlid + ")";
        }
    }
}
