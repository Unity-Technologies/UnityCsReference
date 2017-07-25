// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.Experimental.UIElements.Debugger
{
    class PanelPickerWindow : EditorWindow
    {
        private Action<UIElementsDebugger.ViewPanel?> m_Callback;
        private PickingData m_Data;

        internal static PanelPickerWindow Show(PickingData data, Action<UIElementsDebugger.ViewPanel?> callback)
        {
            var overlayWindow = CreateInstance<PanelPickerWindow>();
            overlayWindow.m_Data = data;

            overlayWindow.m_Pos = data.screenRect;
            overlayWindow.m_Callback = callback;
            overlayWindow.ShowPopup();
            overlayWindow.Focus();
            return overlayWindow;
        }

        public void OnGUI()
        {
            UIElementsDebugger.ViewPanel? p = null;
            if (m_Data.Draw(ref p, m_Data.screenRect))
            {
                Close();
                m_Callback(p);
            }
            else if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                Close();
                m_Callback(null);
            }
        }
    }
}
