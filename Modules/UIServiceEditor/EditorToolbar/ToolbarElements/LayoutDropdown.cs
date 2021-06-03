// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    [EditorToolbarElement("Editor Utility/Layout", typeof(DefaultMainToolbar))]
    sealed class LayoutDropdown : EditorToolbarDropdown
    {
        public LayoutDropdown()
        {
            name = "LayoutDropdown";

            text = Toolbar.lastLoadedLayoutName; //Only assigned once, UI is recreated when changing layout
            clicked += () => OpenLayoutWindow(worldBound);
            tooltip = L10n.Tr("Select editor layout");

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            EditorApplication.delayCall += CheckAvailability; //Immediately after a domain reload, calling check availability sometimes returns the wrong value
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            ModeService.modeChanged += OnModeChanged;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            ModeService.modeChanged -= OnModeChanged;
        }

        void OpenLayoutWindow(Rect buttonRect)
        {
            Vector2 temp = GUIUtility.GUIToScreenPoint(new Vector2(buttonRect.x, buttonRect.y));
            buttonRect.x = temp.x;
            buttonRect.y = temp.y;
            EditorUtility.Internal_DisplayPopupMenu(buttonRect, "Window/Layouts", Toolbar.get, 0, true);
        }

        void OnModeChanged(ModeService.ModeChangedArgs args)
        {
            CheckAvailability();
        }

        void CheckAvailability()
        {
            style.display = ModeService.HasCapability(ModeCapability.LayoutWindowMenu, true) ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
