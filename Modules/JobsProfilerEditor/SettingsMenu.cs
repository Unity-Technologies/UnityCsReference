// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.JobsProfiling;

/// <summary>
/// Settings for the 'New Timeline' view, contributed to the CPU module's options menu while that view is selected.
/// </summary>
internal class SettingsMenu
{
    // Master arrow toggle (controlled by the Jobs Info toolbar button)
    bool m_arrowsEnabled = true;

    // Display settings
    bool m_zoomOnEventFocus = true;
    bool m_showDependsOn = true;
    bool m_showDependantOn = true;
    bool m_showCompletedByWait = true;
    bool m_showCompletedByNoWait = true;
    bool m_showFullDependencyChain;

    // Experimental settings
    bool m_zoomOnEventHover;
    bool m_showFoldedGroupPreview = true;

    internal SettingsMenu()
    {
    }

    internal void AddMenuItems(GenericMenu menu)
    {
        menu.AddSeparator("");

        menu.AddItem(new GUIContent("Zoom when changing event"), m_zoomOnEventFocus,
            () => { m_zoomOnEventFocus = !m_zoomOnEventFocus; });

        AddArrowItem(menu, "Show depends on", m_showDependsOn, () => { m_showDependsOn = !m_showDependsOn; });
        AddArrowItem(menu, "Show dependent on", m_showDependantOn, () => { m_showDependantOn = !m_showDependantOn; });
        AddArrowItem(menu, "Show completed by (wait)", m_showCompletedByWait, () => { m_showCompletedByWait = !m_showCompletedByWait; });
        AddArrowItem(menu, "Show completed by (no wait)", m_showCompletedByNoWait, () => { m_showCompletedByNoWait = !m_showCompletedByNoWait; });

        menu.AddItem(new GUIContent("Show full dependency chain"), m_showFullDependencyChain,
            () => { m_showFullDependencyChain = !m_showFullDependencyChain; });

        menu.AddSeparator("");

        menu.AddItem(new GUIContent("Zoom on event hover"), m_zoomOnEventHover,
            () => { m_zoomOnEventHover = !m_zoomOnEventHover; });

        menu.AddItem(new GUIContent("Show folded group preview"), m_showFoldedGroupPreview,
            () => { m_showFoldedGroupPreview = !m_showFoldedGroupPreview; });
    }

    void AddArrowItem(GenericMenu menu, string text, bool on, GenericMenu.MenuFunction toggle)
    {
        var content = new GUIContent(text);
        if (m_arrowsEnabled)
            menu.AddItem(content, on, toggle);
        else
            menu.AddDisabledItem(content, on);
    }

    internal void SetArrowsEnabled(bool enabled)
    {
        m_arrowsEnabled = enabled;
    }

    // Properties to access settings (used by Timeline)
    internal bool ZoomOnEventFocus => m_zoomOnEventFocus;
    internal bool ShowDependsOn => m_showDependsOn;
    internal bool ShowDependantOn => m_showDependantOn;
    internal bool ShowCompletedByWait => m_showCompletedByWait;
    internal bool ShowCompletedByNoWait => m_showCompletedByNoWait;
    internal bool ShowFullDependencyChain => m_showFullDependencyChain;
    internal bool ZoomOnEventHover => m_zoomOnEventHover;
    internal bool ShowFoldedGroupPreview => m_showFoldedGroupPreview;
}
