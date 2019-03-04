// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.UIR;

namespace UnityEditor.UIElements
{
    internal enum RepaintMode
    {
        Standard,
        UIR
    }

    internal static class UIRDebugUtility
    {
        public static void SwitchPanelRepaintMode(IPanel panel, RepaintMode newMode)
        {
            if (GetPanelRepaintMode(panel) != newMode)
            {
                var p = panel as Panel;
                if (newMode == RepaintMode.UIR)
                {
                    p.SetUpdater(new UIRRepaintUpdater(), VisualTreeUpdatePhase.Repaint);
                    p.SetUpdater(new UIRLayoutUpdater(), VisualTreeUpdatePhase.Layout);
                }
                else
                {
                    // Put back the standard updater
                    p.SetUpdater(new VisualTreeRepaintUpdater(), VisualTreeUpdatePhase.Repaint);
                    p.SetUpdater(new VisualTreeLayoutUpdater(), VisualTreeUpdatePhase.Layout);
                }

                panel.visualTree.MarkDirtyRepaint();
            }
        }

        public static UIRenderDevice GetUIRenderDevice(IPanel panel)
        {
            UIRRepaintUpdater updater = GetUIRRepaintUpdater(panel);
            return updater?.DebugGetRenderChain()?.device as UIRenderDevice;
        }

        public static RepaintMode GetPanelRepaintMode(IPanel panel)
        {
            UIRRepaintUpdater updater = GetUIRRepaintUpdater(panel);
            return updater != null ? RepaintMode.UIR : RepaintMode.Standard;
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
            string t = ve.GetType() == typeof(VisualElement) ? String.Empty : (ve.GetType().Name + " ");
            string n = String.IsNullOrEmpty(ve.name) ? String.Empty : ("#" + ve.name + " ");
            string res = t + n + (ve.GetClasses().Any() ? ("." + string.Join(",.", ve.GetClasses().ToArray())) : String.Empty);
            if (res == String.Empty)
                return ve.GetType().Name;
            if (ve.renderHint != RenderHint.None)
                res += $" [{ve.renderHint}]";
            return res + " (" + ve.controlid + ")";
        }
    }
}
