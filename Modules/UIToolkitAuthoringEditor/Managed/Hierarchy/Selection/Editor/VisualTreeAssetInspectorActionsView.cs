// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[UxmlElement]
partial class VisualTreeAssetInspectorActionsView : VisualElement
{
    public const string UssClass = "unity-visual-tree-asset-inspector-actions-view";
    public const string MenuButtonUssClass = UssClass + "__menu-button";
    public const string HiddenUssClass = UssClass + "--hidden";

    internal static readonly string SaveTextFormat = L10n.Tr("Save {0}", null);
    internal static readonly string SelectInProjectText = L10n.Tr("Select in Project", null);
    internal static readonly string OpenInContextText = L10n.Tr("Open in Context", null);
    internal static readonly string MenuButtonTooltip = L10n.Tr("Asset actions", null);

    private const string k_VisualTreeAsset = "UIToolkitAuthoring/Inspector/VisualTreeAssetInspectorActionsView.uxml";

    private readonly Button m_MenuButton;

    private VisualTreeAsset m_VisualTreeAsset;
    private TemplateAsset[] m_SubDocumentPath;
    private bool m_CanOpenInContext = true;
    private string m_OpenInContextDisabledTooltip;

    public bool OpenInContextEnabled => m_CanOpenInContext;

    internal string SaveItemText => GetSaveItemText(m_VisualTreeAsset);

    internal bool CanSave => CanSaveAsset(m_VisualTreeAsset);

    private static string GetSaveItemText(VisualTreeAsset asset) =>
        string.Format(SaveTextFormat, GetAssetFileName(asset));

    private static bool CanSaveAsset(VisualTreeAsset asset) =>
        asset && UIAssetRegistry.LiveInstance?.CanSettleSingleAsset(asset) == true;

    private static string GetAssetFileName(VisualTreeAsset asset)
    {
        if (!asset)
            return string.Empty;

        return Path.GetFileName(AssetDatabase.GetAssetPath(asset.GetEntityId()));
    }

    public PanelSettings PanelSettings { get; set; }

    public TemplateAsset[] SubDocumentPath
    {
        get => m_SubDocumentPath;
        set
        {
            if (m_SubDocumentPath == value)
                return;

            m_SubDocumentPath = value;
            UpdateControlStates();
        }
    }

    public VisualTreeAsset VisualTreeAsset
    {
        get => m_VisualTreeAsset;
        set
        {
            if (m_VisualTreeAsset == value)
                return;

            m_VisualTreeAsset = value;
            UpdateControlStates();
        }
    }

    public VisualTreeAssetInspectorActionsView()
    {
        AddToClassList(UssClass);

        var vta = EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;
        vta.CloneTree(this);

        m_MenuButton = this.Q<Button>(className: MenuButtonUssClass);
        m_MenuButton.tooltip = MenuButtonTooltip;
        m_MenuButton.clicked += ShowMenu;

        UpdateControlStates();
    }

    public void SetOpenInContextState(bool enabled, string disabledTooltip = null)
    {
        m_CanOpenInContext = enabled;
        m_OpenInContextDisabledTooltip = disabledTooltip;
    }

    public void UpdateControlStates()
    {
        var isAssetPathValid = false;

        if (m_VisualTreeAsset)
            isAssetPathValid = !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(m_VisualTreeAsset.GetEntityId()));

        m_MenuButton.EnableInClassList(HiddenUssClass, !isAssetPathValid);
    }

    private void ShowMenu()
    {
        var menu = new GenericDropdownMenu();

        var assetAtMenuTime = m_VisualTreeAsset;
        var saveText = GetSaveItemText(assetAtMenuTime);
        if (CanSaveAsset(assetAtMenuTime))
            menu.AddItem(saveText, false, () => SaveAsset(assetAtMenuTime));
        else
            menu.AddDisabledItem(saveText, false);

        menu.AddSeparator(string.Empty);

        menu.AddItem(SelectInProjectText, false, SelectAssetInProject);

        if (m_CanOpenInContext)
        {
            menu.AddItem(OpenInContextText, false, OpenInContext);
        }
        else
        {
            menu.AddDisabledItem(OpenInContextText, false);
            if (!string.IsNullOrEmpty(m_OpenInContextDisabledTooltip))
                SetItemTooltip(menu, OpenInContextText, m_OpenInContextDisabledTooltip);
        }

        menu.AddItem(StageContextMenuUtility.OpenInUIBuilder, false, OpenInUIBuilder);

        menu.DropDown(m_MenuButton.worldBound, m_MenuButton, DropdownMenuSizeMode.Auto);
    }

    private static void SetItemTooltip(GenericDropdownMenu menu, string itemName, string tooltip)
    {
        foreach (var row in menu.contentContainer.Children())
        {
            var label = row.Q<Label>(className: GenericDropdownMenu.labelUssClassName);
            if (label == null || label.text != itemName)
                continue;

            row.tooltip = tooltip;
            return;
        }
    }

    private static void SaveAsset(VisualTreeAsset asset)
    {
        if (!CanSaveAsset(asset))
            return;

        UIAssetRegistry.instance.SaveSingleAsset(asset, CommandSources.Inspector);
    }

    private void SelectAssetInProject()
    {
        EditorGUIUtility.PingObject(VisualTreeAsset);
    }

    private void OpenInContext()
    {
        var options = m_SubDocumentPath is { Length: > 0 } ? SubDocumentOptions.InContext : SubDocumentOptions.None;
        var rootVisualTreeAsset = m_SubDocumentPath is { Length: > 0 } ? m_SubDocumentPath[0].visualTreeAsset : m_VisualTreeAsset;

        var context = new VisualTreeAssetEditingContext(
            rootVisualTreeAsset,
            m_SubDocumentPath,
            options,
            PanelSettings
        );

        UIStageNavigation.Navigate(context, BreadcrumbBar.SeparatorStyle.Line);
    }

    private void OpenInUIBuilder()
    {
        if (!m_VisualTreeAsset)
            return;

        AssetDatabase.OpenAsset(m_VisualTreeAsset.GetEntityId());
    }
}
