// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.JobsProfiling;

/// <summary>
/// Settings for the 'New Timeline' view, contributed to the CPU module's options menu while that view is selected.
/// </summary>
internal class SettingsMenu
{
    [Flags]
    internal enum Options
    {
        None = 0,
        ZoomOnEventFocus = 1 << 0,
        ShowDependsOn = 1 << 1,
        ShowDependantOn = 1 << 2,
        ShowCompletedByWait = 1 << 3,
        ShowCompletedByNoWait = 1 << 4,
        ShowFullDependencyChain = 1 << 5,
        ZoomOnEventHover = 1 << 6,
        ShowFoldedGroupPreview = 1 << 7,
    }

    internal const Options k_DefaultOptions = Options.ZoomOnEventFocus | Options.ShowDependsOn | Options.ShowDependantOn |
        Options.ShowCompletedByWait | Options.ShowCompletedByNoWait | Options.ShowFoldedGroupPreview;

    internal const string k_OptionsKey = "Profiler.JobsProfiler.SettingsMenu.Options";

    // Master arrow toggle (controlled by the Jobs Info toolbar button)
    bool m_arrowsEnabled = true;

    internal SettingsMenu()
    {
    }

    Options options
    {
        get { return (Options)SessionState.GetInt(k_OptionsKey, (int)k_DefaultOptions); }
        set { SessionState.SetInt(k_OptionsKey, (int)value); }
    }

    internal void AddMenuItems(GenericMenu menu)
    {
        menu.AddSeparator("");

        AddItem(menu, "Zoom when changing event", Options.ZoomOnEventFocus);

        AddArrowItem(menu, "Show depends on", Options.ShowDependsOn);
        AddArrowItem(menu, "Show dependent on", Options.ShowDependantOn);
        AddArrowItem(menu, "Show completed by (wait)", Options.ShowCompletedByWait);
        AddArrowItem(menu, "Show completed by (no wait)", Options.ShowCompletedByNoWait);

        AddItem(menu, "Show full dependency chain", Options.ShowFullDependencyChain);

        menu.AddSeparator("");

        AddItem(menu, "Zoom on event hover", Options.ZoomOnEventHover);

        AddItem(menu, "Show folded group preview", Options.ShowFoldedGroupPreview);
    }

    void AddItem(GenericMenu menu, string text, Options option)
    {
        menu.AddItem(new GUIContent(text), IsSet(option), () => { Toggle(option); });
    }

    void AddArrowItem(GenericMenu menu, string text, Options option)
    {
        var content = new GUIContent(text);
        if (m_arrowsEnabled)
            menu.AddItem(content, IsSet(option), () => { Toggle(option); });
        else
            menu.AddDisabledItem(content, IsSet(option));
    }

    internal bool IsSet(Options option)
    {
        return (options & option) != 0;
    }

    internal void Toggle(Options option)
    {
        options ^= option;
    }

    internal void SetArrowsEnabled(bool enabled)
    {
        m_arrowsEnabled = enabled;
    }

    // Properties to access settings (used by Timeline)
    internal bool ZoomOnEventFocus => IsSet(Options.ZoomOnEventFocus);
    internal bool ShowDependsOn => IsSet(Options.ShowDependsOn);
    internal bool ShowDependantOn => IsSet(Options.ShowDependantOn);
    internal bool ShowCompletedByWait => IsSet(Options.ShowCompletedByWait);
    internal bool ShowCompletedByNoWait => IsSet(Options.ShowCompletedByNoWait);
    internal bool ShowFullDependencyChain => IsSet(Options.ShowFullDependencyChain);
    internal bool ZoomOnEventHover => IsSet(Options.ZoomOnEventHover);
    internal bool ShowFoldedGroupPreview => IsSet(Options.ShowFoldedGroupPreview);
}
