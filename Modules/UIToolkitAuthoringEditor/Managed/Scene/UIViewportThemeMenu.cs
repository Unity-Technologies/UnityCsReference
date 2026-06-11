// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[UxmlElement(visibility = LibraryVisibility.Hidden)]
sealed partial class UIViewportThemeMenu : VisualElement
{
    public const string UssClass = "unity-ui-viewport__toolbar-theme-selection";
    public const string ToolbarMenuUssClass = "unity-ui-viewport__toolbar-theme-menu";
    public const string ResetButtonUssClass = "unity-ui-viewport__toolbar-reset-theme-button";

    readonly ToolbarMenu m_Menu;
    readonly Button m_ResetButton;

    PanelSettings m_PanelSettings;
    ThemeStyleSheet m_SelectedTheme;

    public event Action<ThemeStyleSheet> ThemeSelected;

    public PanelSettings PanelSettings
    {
        get => m_PanelSettings;
        set
        {
            m_PanelSettings = value;
            RebuildMenu();
            UpdateResetButtonState();
        }
    }

    public ThemeStyleSheet SelectedTheme
    {
        get => m_SelectedTheme;
        set
        {
            m_SelectedTheme = value;
            UpdateMenuLabel();
            UpdateResetButtonState();
        }
    }

    public UIViewportThemeMenu()
    {
        AddToClassList(UssClass);

        m_Menu = new ToolbarMenu();
        m_Menu.AddToClassList(ToolbarMenuUssClass);
        m_Menu.tooltip = "Select the theme used to preview the content.";
        Add(m_Menu);

        m_ResetButton = new Button();
        m_ResetButton.AddToClassList(ResetButtonUssClass);
        m_ResetButton.clicked += () => OnThemeSelected(null);
        Add(m_ResetButton);
    }

    protected override void HandleEventBubbleUp(EventBase evt)
    {
        switch (evt)
        {
            case AttachToPanelEvent:
                ThemeUtility.themeFilesChanged += OnThemeFilesChanged;
                ObjectChangeEvents.changesPublished += OnObjectChangesPublished;
                break;
            case DetachFromPanelEvent:
                ThemeUtility.themeFilesChanged -= OnThemeFilesChanged;
                ObjectChangeEvents.changesPublished -= OnObjectChangesPublished;
                break;
        }
        base.HandleEventBubbleUp(evt);
    }

    public void ClearItems()
    {
        m_Menu.menu.ClearItems();
        m_Menu.text = string.Empty;
        m_ResetButton.style.display = DisplayStyle.None;
        m_PanelSettings = null;
        m_SelectedTheme = null;
    }

    void RebuildMenu()
    {
        m_Menu.menu.ClearItems();

        m_Menu.menu.AppendAction(GetPanelSettingsLabel(),
            _ => OnThemeSelected(null),
            _ => m_SelectedTheme == null ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

        m_Menu.menu.AppendSeparator();

        foreach (var (theme, displayName) in ThemeUtility.GetRuntimeThemesToDisplayName())
        {
            m_Menu.menu.AppendAction(displayName,
                _ => OnThemeSelected(theme),
                _ => m_SelectedTheme == theme ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
        }

        UpdateMenuLabel();
    }

    void OnThemeSelected(ThemeStyleSheet theme)
    {
        m_SelectedTheme = theme;
        UpdateMenuLabel();
        UpdateResetButtonState();
        ThemeSelected?.Invoke(theme);
    }

    void UpdateMenuLabel()
    {
        m_Menu.text = m_SelectedTheme != null
            ? ThemeUtility.NicifyThemeName(m_SelectedTheme)
            : GetPanelSettingsLabel();
    }

    void UpdateResetButtonState()
    {
        var hasOverride = m_SelectedTheme != null;
        m_ResetButton.style.display = hasOverride ? DisplayStyle.Flex : DisplayStyle.None;
        if (hasOverride)
            m_ResetButton.tooltip = $"Reset to {GetPanelSettingsLabel()}.";
    }

    string GetPanelSettingsLabel()
    {
        var panelTheme = m_PanelSettings ? m_PanelSettings.themeStyleSheet : null;
        return panelTheme != null
            ? $"{ThemeUtility.NicifyThemeName(panelTheme)} (Panel Settings)"
            : "Panel Settings";
    }

    void OnThemeFilesChanged()
    {
        if (m_PanelSettings == null)
            return;

        // If the selected theme asset was deleted it becomes a fake-null (a non-null C# reference
        // to a destroyed Unity object). Normalize to actual null so the callback and all subsequent
        // callers receive an unambiguous null instead of a stale destroyed reference.
        if (m_SelectedTheme == null)
            m_SelectedTheme = null;

        RebuildMenu();
        UpdateResetButtonState();

        if (m_SelectedTheme == null)
            ThemeSelected?.Invoke(null);
    }

    void OnObjectChangesPublished(ref ObjectChangeEventStream stream)
    {
        if (m_PanelSettings == null)
            return;

        var entityId = m_PanelSettings.GetEntityId();
        for (int i = 0; i < stream.length; i++)
        {
            if (stream.GetEventType(i) != ObjectChangeKind.ChangeAssetObjectProperties)
                continue;

            stream.GetChangeAssetObjectPropertiesEvent(i, out var args);
            if (args.entityId != entityId)
                continue;

            RebuildMenu();
            if (m_SelectedTheme == null)
                ThemeSelected?.Invoke(null);
            break;
        }
    }

}
