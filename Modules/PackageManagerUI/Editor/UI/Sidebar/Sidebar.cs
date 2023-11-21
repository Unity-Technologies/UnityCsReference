// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal;

internal class Sidebar : ScrollView
{
    [Serializable]
    public new class UxmlSerializedData : ScrollView.UxmlSerializedData
    {
        public override object CreateInstance() => new Sidebar();
    }

    private IUpmRegistryClient m_UpmRegistryClient;
    private IProjectSettingsProxy m_SettingsProxy;
    private IPageManager m_PageManager;

    private Dictionary<string, SidebarRow> m_ScopedRegistryRows = new();
    private SidebarRow m_CurrentlySelectedRow;

    private void ResolveDependencies()
    {
        var container = ServicesContainer.instance;
        m_UpmRegistryClient = container.Resolve<IUpmRegistryClient>();
        m_SettingsProxy = container.Resolve<IProjectSettingsProxy>();
        m_PageManager = container.Resolve<IPageManager>();
    }

    public Sidebar()
    {
        ResolveDependencies();
    }

    public void OnEnable()
    {
        m_UpmRegistryClient.onRegistriesModified += UpdateScopedRegistryRelatedRows;
        m_PageManager.onActivePageChanged += OnActivePageChanged;
    }

    public void OnCreateGUI()
    {
        CreateRows();
        OnActivePageChanged(m_PageManager.activePage);
    }

    public void OnDisable()
    {
        m_UpmRegistryClient.onRegistriesModified -= UpdateScopedRegistryRelatedRows;
        m_PageManager.onActivePageChanged -= OnActivePageChanged;
    }

    private void CreateRows()
    {
        CreateAndAddSeparator();
        CreateAndAddSidebarRow(m_PageManager.GetPage(InProjectPage.k_Id));
        CreateAndAddSidebarRow(m_PageManager.GetPage(InProjectUpdatesPage.k_Id), isIndented: true);
        CreateAndAddSeparator();
        CreateAndAddSidebarRow(m_PageManager.GetPage(UnityRegistryPage.k_Id));
        CreateAndAddSidebarRow(m_PageManager.GetPage(MyAssetsPage.k_Id));
        CreateAndAddSidebarRow(m_PageManager.GetPage(BuiltInPage.k_Id));
        CreateAndAddSeparator();

        foreach (var page in m_PageManager.orderedExtensionPages)
            CreateAndAddSidebarRow(page);

        CreateAndAddSeparator();
        CreateAndAddSidebarRow(m_PageManager.GetPage(MyRegistriesPage.k_Id));

        UpdateScopedRegistryRelatedRows();
    }

    private void CreateAndAddSidebarRow(IPage page, bool isScopedRegistryPage = false, bool isIndented = false)
    {
        var pageId = page.id;
        var sidebarRow = new SidebarRow(page.id, page.displayName, page.icon, isIndented);
        sidebarRow.OnLeftClick(() => OnRowClick(pageId));
        if (isScopedRegistryPage)
            m_ScopedRegistryRows[page.id] = sidebarRow;
        Add(sidebarRow);
    }

    private void CreateAndAddSeparator()
    {
        Add(new VisualElement { classList = { "sidebarSeparator" } });
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

    private void UpdateScopedRegistryRelatedRows()
    {
        var scopedRegistryPages = m_SettingsProxy.scopedRegistries.Select(r => m_PageManager.GetPage(r)).ToArray();

        // We remove the rows from the hierarchy so we can add it back later with the right order
        foreach (var row in m_ScopedRegistryRows.Values)
            Remove(row);

        var deletedOrHiddenPageIds = m_ScopedRegistryRows.Keys.ToHashSet();
        foreach (var page in scopedRegistryPages)
        {
            if (m_ScopedRegistryRows.TryGetValue(page.id, out var row))
            {
                deletedOrHiddenPageIds.Remove(page.id);
                Add(row);
            }
            else
                CreateAndAddSidebarRow(page, true, true);
        }

        foreach (var pageId in deletedOrHiddenPageIds)
            m_ScopedRegistryRows.Remove(pageId);

        var myRegistriesRowVisible = scopedRegistryPages.Any();
        UIUtils.SetElementDisplay(GetRow(MyRegistriesPage.k_Id), myRegistriesRowVisible);
        if (!myRegistriesRowVisible)
            deletedOrHiddenPageIds.Add(MyRegistriesPage.k_Id);
        if (deletedOrHiddenPageIds.Contains(m_PageManager.activePage.id))
            m_PageManager.activePage = m_PageManager.GetPage(PageManager.k_DefaultPageId);
    }

    public SidebarRow GetRow(string pageId)
    {
        return Children().OfType<SidebarRow>().FirstOrDefault(i => i.pageId == pageId);
    }
}
