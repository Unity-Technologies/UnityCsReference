// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal;

[UxmlElement]
internal partial class Sidebar : ScrollView
{
    private SidebarRow m_CurrentlySelectedRow;
    private Foldout m_CloudFoldout;
    private Foldout m_RegistriesFoldout;

    private bool m_FoldoutsCreated = false;

    private readonly string k_FoldoutClassName = "sidebarFoldout";

    private readonly IPageManager m_PageManager;
    private readonly IPackageManagerPrefs m_PackageManagerPrefs;

    public Sidebar() : this(
        ServicesContainer.instance.Resolve<PageManager>(),
        ServicesContainer.instance.Resolve<IPackageManagerPrefs>())
    {
    }

    public Sidebar(IPageManager pageManager, IPackageManagerPrefs packageManagerPrefs)
    {
        m_PageManager = pageManager;
        m_PackageManagerPrefs = packageManagerPrefs;

        RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
    }

    private void CreateRowsAndFoldouts()
    {
        if (m_FoldoutsCreated)
            return;

        var projectFoldout = CreateAndAddFoldout(L10n.Tr("Project"));
        projectFoldout.Add(CreateSidebarRow(m_PageManager.GetPage(InProjectPage.k_Id)));
        projectFoldout.Add(CreateSidebarRow(m_PageManager.GetPage(InProjectUpdatesPage.k_Id)));
        projectFoldout.Add(CreateSidebarRow(m_PageManager.GetPage(SamplesPage.k_Id)));
        projectFoldout.Add(CreateSidebarRow(m_PageManager.GetPage(InProjectNonCompliancePage.k_Id)));
        projectFoldout.Add(CreateSidebarRow(m_PageManager.GetPage(InProjectErrorsAndWarningsPage.k_Id)));

        var sourcesFoldout = CreateAndAddFoldout(L10n.Tr("Sources"));
        sourcesFoldout.Add(CreateSidebarRow(m_PageManager.GetPage(MyAssetsPage.k_Id)));
        sourcesFoldout.Add(CreateSidebarRow(m_PageManager.GetPage(UnityRegistryPage.k_Id)));
        sourcesFoldout.Add(CreateSidebarRow(m_PageManager.GetPage(BuiltInPage.k_Id)));

        m_CloudFoldout = CreateAndAddFoldout(L10n.Tr("Cloud"));
        m_RegistriesFoldout = CreateAndAddFoldout(L10n.Tr("My Registries"));

        m_FoldoutsCreated = true;
    }

    private void OnAttachToPanel(AttachToPanelEvent evt)
    {
        CreateRowsAndFoldouts();

        m_PageManager.onActivePageChanged += OnActivePageChanged;
        m_PageManager.onExtensionPagesChanged += UpdateExtensionPageRelatedRows;
        m_PageManager.onScopedRegistryPagesChanged += UpdateScopedRegistryRelatedRows;
        m_PageManager.onStateChanged += OnStateChanged;

        UpdateExtensionPageRelatedRows();
        UpdateScopedRegistryRelatedRows();

        var foldouts = Children().FilterByType<Foldout>().ToNewArray(childCount);
        if (m_PackageManagerPrefs.orderedSidebarFoldoutsExpandedStatus?.Length == foldouts.Length)
            for (var i = 0; i < foldouts.Length; i++)
                foldouts[i].value = m_PackageManagerPrefs.orderedSidebarFoldoutsExpandedStatus[i];

        OnActivePageChanged(m_PageManager.activePage);
    }

    private void OnDetachFromPanel(DetachFromPanelEvent evt)
    {
        m_PageManager.onActivePageChanged -= OnActivePageChanged;
        m_PageManager.onExtensionPagesChanged -= UpdateExtensionPageRelatedRows;
        m_PageManager.onScopedRegistryPagesChanged -= UpdateScopedRegistryRelatedRows;
        m_PageManager.onStateChanged -= OnStateChanged;

        m_PackageManagerPrefs.orderedSidebarFoldoutsExpandedStatus = Children().FilterByType<Foldout>().SelectAsEnumerable(i => i.value).ToNewArray(childCount);
    }

    private Foldout CreateAndAddFoldout(string foldoutName)
    {
        var foldout = new Foldout {text = foldoutName};
        foldout.AddToClassList(k_FoldoutClassName);
        foldout.tooltip = foldoutName;
        Add(foldout);
        return foldout;
    }

    private SidebarRow CreateSidebarRow(IPage page)
    {
        var pageId = page.id;
        var sidebarRow = new SidebarRow(page.id, page.displayName, page.icon);
        sidebarRow.OnLeftClick(() => OnRowClick(pageId));
        UIUtils.SetElementDisplay(sidebarRow, page.visible);
        return sidebarRow;
    }

    private void OnRowClick(string pageId)
    {
        if (pageId == m_PageManager.activePage.id)
            return;

        m_PageManager.activePage = m_PageManager.GetPage(pageId);
        PackageManagerWindowAnalytics.SendEvent("changeFilter");
    }

    private void OnActivePageChanged(IPage page)
    {
        m_CurrentlySelectedRow?.SetSelected(false);
        m_CurrentlySelectedRow = GetRow(page.id);
        m_CurrentlySelectedRow?.SetSelected(true);
    }

    private void OnStateChanged(PageStateChangeArgs args)
    {
        var row = GetRow(args.page.id);
        if (row == null)
            return;

        if (args.visible)
            row.UpdateIcon(args.icon);
        UIUtils.SetElementDisplay(row, args.visible);
    }

    private void SyncFoldoutWithPages(Foldout foldout, IEnumerable<IPage> pages)
    {
        var oldRows = foldout.Children().FilterByType<SidebarRow>().ToNewDictionary(r => r.pageId);

        foldout.Clear();
        foreach (var page in pages)
            foldout.Add(oldRows.GetValueOrDefault(page.id) ?? CreateSidebarRow(page));

        UIUtils.SetElementDisplay(foldout, foldout.childCount > 0);
    }

    private void UpdateExtensionPageRelatedRows()
    {
        SyncFoldoutWithPages(m_CloudFoldout, m_PageManager.orderedExtensionPages);
    }

    private void UpdateScopedRegistryRelatedRows()
    {
        SyncFoldoutWithPages(m_RegistriesFoldout, m_PageManager.orderedScopedRegistryPages);
    }

    public SidebarRow GetRow(string pageId)
    {
        foreach (var foldout in Children())
            foreach (var item in foldout.Children())
                if (item is SidebarRow row && row.pageId == pageId)
                    return row;
        return null;
    }
}
