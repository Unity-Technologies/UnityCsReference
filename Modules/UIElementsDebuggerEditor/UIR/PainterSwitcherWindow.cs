// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    internal class PainterSwitcherWindow : EditorWindow
    {
        [MenuItem("Window/Analysis/UIR Painter Switcher", false, 200, true)]
        public static void Open()
        {
            GetWindow<PainterSwitcherWindow>().Show();
        }

        public void OnEnable()
        {
            titleContent = new GUIContent("Painter Switcher");
        }

        public void OnGUI()
        {
            DoViewsPanel();
        }

        private void DoViewsPanel()
        {
            GUILayout.Label("Views Panel", EditorStyles.boldLabel);
            List<GUIView> guiViews = new List<GUIView>();
            GUIViewDebuggerHelper.GetViews(guiViews);
            var it = UIElementsUtility.GetPanelsIterator();
            while (it.MoveNext())
            {
                HostView view = guiViews.FirstOrDefault(v => v.GetInstanceID() == it.Current.Key) as HostView;
                if (view == null)
                    continue;

                // Skip this window
                if (view.actualView == this)
                    continue;

                var panel = it.Current.Value;
                var panelMode = UIRDebugUtility.GetPanelRepaintMode(panel);
                string name = panel.name;
                var mode = (RepaintMode)EditorGUILayout.EnumPopup(name, panelMode);
                if (panelMode != mode)
                {
                    if (Panel.BeforeUpdaterChange != null)
                        Panel.BeforeUpdaterChange();
                    UIRDebugUtility.SwitchPanelRepaintMode(panel, mode);
                    if (mode == RepaintMode.UIR)
                    {
                        view.actualView.depthBufferBits = 24;
                    }
                    else
                    {
                        view.actualView.depthBufferBits = 0;
                    }
                    view.actualView.MakeParentsSettingsMatchMe();
                    if (Panel.AfterUpdaterChange != null)
                        Panel.AfterUpdaterChange();
                }
            }
        }
    }
}
