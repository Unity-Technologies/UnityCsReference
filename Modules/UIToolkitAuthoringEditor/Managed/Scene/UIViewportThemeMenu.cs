// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[UxmlElement(visibility = LibraryVisibility.Hidden)]
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
sealed partial class UIViewportThemeMenu : VisualElement
{
    public const string UssClass = "unity-ui-viewport__toolbar-theme-selection";
    public const string ToolbarMenuUssClass = "unity-ui-viewport__toolbar-theme-menu";
    public const string ContextTypeMenuUssClass = "unity-ui-viewport__toolbar-context-type-menu";
    public const string ResetButtonUssClass = "unity-ui-viewport__toolbar-reset-theme-button";

    static readonly string k_RuntimeText = L10n.Tr("Runtime", null);
    static readonly string k_EditorText = L10n.Tr("Editor", null);
    static readonly string k_ContextTypeTooltip = L10n.Tr("Select whether the document is runtime UI or editor UI. Editor UI can use the editor controls and previews with the editor themes.", null);
    static readonly string k_ThemeTooltip = L10n.Tr("Select the theme used to preview the content.", null);
    static readonly string k_PanelSettingsText = L10n.Tr("Panel Settings", null);
    static readonly string k_PanelSettingsThemeFormat = L10n.Tr("{0} (Panel Settings)", null);
    static readonly string k_DefaultThemeFormat = L10n.Tr("{0} (Default)", null);
    static readonly string k_ResetTooltipFormat = L10n.Tr("Reset to {0}.", null);

    readonly ToolbarMenu m_ContextTypeMenu;
    readonly ToolbarMenu m_Menu;
    readonly Button m_ResetButton;

    PanelSettings m_PanelSettings;
    StyleSheet m_SelectedTheme;
    StyleSheet m_DefaultTheme;
    ContextType m_ContextType = ContextType.Player;
    bool m_ShowContextTypeMenu;

    public event Action<StyleSheet> ThemeSelected;

    public event Action<ContextType> ContextTypeSelected;

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

    /// <summary>
    /// Whether the content previews as runtime UI or as editor UI. The menu lists the themes of that context type.
    /// </summary>
    public ContextType ContextType
    {
        get => m_ContextType;
        set
        {
            if (m_ContextType == value)
                return;
            m_ContextType = value;
            m_ContextTypeMenu.text = GetContextTypeText(value);
            RebuildMenu();
            UpdateResetButtonState();
        }
    }

    // Only hosts that follow a switch to an editor-context sub-panel offer the context type menu.
    public bool ShowContextTypeMenu
    {
        get => m_ShowContextTypeMenu;
        set
        {
            m_ShowContextTypeMenu = value;
            m_ContextTypeMenu.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    // False for a document that is only previewed: its context type shows, but cannot be changed.
    public bool CanChangeContextType { get; set; } = true;

    /// <summary>The context type menu, after which a host can insert what goes with the context type.</summary>
    public VisualElement ContextTypeMenu => m_ContextTypeMenu;

    public StyleSheet SelectedTheme
    {
        get => m_SelectedTheme;
        set
        {
            m_SelectedTheme = value;
            UpdateMenuLabel();
            UpdateResetButtonState();
        }
    }

    // What "Panel Settings" previews with when the panel settings have no theme.
    public StyleSheet DefaultTheme
    {
        get => m_DefaultTheme;
        set
        {
            m_DefaultTheme = value;
            UpdateMenuLabel();
            UpdateResetButtonState();
        }
    }

    public UIViewportThemeMenu()
    {
        AddToClassList(UssClass);

        m_ContextTypeMenu = new ToolbarMenu { text = GetContextTypeText(m_ContextType), tooltip = k_ContextTypeTooltip };
        m_ContextTypeMenu.AddToClassList(ContextTypeMenuUssClass);
        m_ContextTypeMenu.style.display = DisplayStyle.None;
        AppendContextTypeAction(ContextType.Player);
        AppendContextTypeAction(ContextType.Editor);
        Add(m_ContextTypeMenu);

        m_Menu = new ToolbarMenu();
        m_Menu.AddToClassList(ToolbarMenuUssClass);
        m_Menu.tooltip = k_ThemeTooltip;
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
        m_DefaultTheme = null;
    }

    /// <summary>
    /// Chooses <paramref name="contextType"/> as if it was picked in the context type menu.
    /// </summary>
    public void SelectContextType(ContextType contextType)
    {
        if (contextType != m_ContextType && CanChangeContextType)
            ContextTypeSelected?.Invoke(contextType);
    }

    void AppendContextTypeAction(ContextType contextType)
    {
        m_ContextTypeMenu.menu.AppendAction(GetContextTypeText(contextType),
            _ => SelectContextType(contextType),
            _ => m_ContextType == contextType
                ? DropdownMenuAction.Status.Checked
                : CanChangeContextType ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
    }

    static string GetContextTypeText(ContextType contextType)
        => contextType == ContextType.Editor ? k_EditorText : k_RuntimeText;

    void RebuildMenu()
    {
        m_Menu.menu.ClearItems();

        m_Menu.menu.AppendAction(GetDefaultThemeLabel(),
            _ => OnThemeSelected(null),
            _ => m_SelectedTheme == null ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

        m_Menu.menu.AppendSeparator();

        if (m_ContextType == ContextType.Editor)
        {
            // The active editor theme is the entry above: it follows the editor skin.
            foreach (var (canvasTheme, displayName) in ThemeUtility.GetEditorThemesToDisplayName())
            {
                if (canvasTheme == CanvasTheme.Default)
                    continue;

                m_Menu.menu.AppendAction(displayName,
                    _ => OnThemeSelected(ThemeUtility.GetStyleSheetForTheme(canvasTheme, null)),
                    _ => IsSelectedEditorTheme(canvasTheme) ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            }
        }
        else
        {
            foreach (var (theme, displayName) in ThemeUtility.GetRuntimeThemesToDisplayName())
            {
                m_Menu.menu.AppendAction(displayName,
                    _ => OnThemeSelected(theme),
                    _ => m_SelectedTheme == theme ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            }
        }

        UpdateMenuLabel();
    }

    void OnThemeSelected(StyleSheet theme)
    {
        m_SelectedTheme = theme;
        UpdateMenuLabel();
        UpdateResetButtonState();
        ThemeSelected?.Invoke(theme);
    }

    void UpdateMenuLabel()
    {
        m_Menu.text = m_SelectedTheme != null
            ? GetThemeDisplayName(m_SelectedTheme)
            : GetDefaultThemeLabel();
    }

    bool IsSelectedEditorTheme(CanvasTheme canvasTheme)
    {
        return ThemeUtility.TryGetEditorCanvasTheme(m_SelectedTheme, out var selected) && selected == canvasTheme;
    }

    static string GetThemeDisplayName(StyleSheet theme)
    {
        if (ThemeUtility.TryGetEditorCanvasTheme(theme, out var canvasTheme))
            return ThemeUtility.GetEditorThemesToDisplayName()[canvasTheme];
        return theme is ThemeStyleSheet themeStyleSheet
            ? ThemeUtility.NicifyThemeName(themeStyleSheet)
            : ObjectNames.NicifyVariableName(theme.name);
    }

    void UpdateResetButtonState()
    {
        var hasOverride = m_SelectedTheme != null;
        m_ResetButton.style.display = hasOverride ? DisplayStyle.Flex : DisplayStyle.None;
        if (hasOverride)
            m_ResetButton.tooltip = string.Format(k_ResetTooltipFormat, GetDefaultThemeLabel());
    }

    // What the content previews with when no theme is chosen.
    string GetDefaultThemeLabel()
    {
        if (m_ContextType == ContextType.Editor)
            return ThemeUtility.GetEditorThemesToDisplayName()[CanvasTheme.Default];

        var panelTheme = m_PanelSettings ? m_PanelSettings.themeStyleSheet : null;
        if (panelTheme != null)
            return string.Format(k_PanelSettingsThemeFormat, ThemeUtility.NicifyThemeName(panelTheme));

        return m_DefaultTheme != null
            ? string.Format(k_DefaultThemeFormat, GetThemeDisplayName(m_DefaultTheme))
            : k_PanelSettingsText;
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
