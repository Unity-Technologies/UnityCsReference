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
    protected new class UxmlFactory : UxmlFactory<Sidebar, UxmlTraits> {}

    private UpmRegistryClient m_UpmRegistryClient;
    private PackageManagerProjectSettingsProxy m_SettingsProxy;
    private PageManager m_PageManager;

    private SidebarRow m_CurrentlySelectedRow;

    private void ResolveDependencies()
    {
        var container = ServicesContainer.instance;
        m_UpmRegistryClient = container.Resolve<UpmRegistryClient>();
        m_SettingsProxy = container.Resolve<PackageManagerProjectSettingsProxy>();
        m_PageManager = container.Resolve<PageManager>();
    }

    public Sidebar()
    {
        ResolveDependencies();
    }

    public void OnEnable()
    {
        m_UpmRegistryClient.onRegistriesModified += OnRegistriesModified;
        m_PageManager.onActivePageChanged += OnActivePageChanged;
    }

    public void OnCreateGUI()
    {
        CreateRows();
        OnActivePageChanged(m_PageManager.activePage);
    }

    public void OnDisable()
    {
        m_UpmRegistryClient.onRegistriesModified -= OnRegistriesModified;
        m_PageManager.onActivePageChanged -= OnActivePageChanged;
    }

    private void CreateRows()
    {
        CreateAndAddSeparator();
        CreateAndAddSidebarRow(m_PageManager.GetPage(InProjectPage.k_Id));
        CreateAndAddSeparator();
        CreateAndAddSidebarRow(m_PageManager.GetPage(UnityRegistryPage.k_Id));
        CreateAndAddSidebarRow(m_PageManager.GetPage(MyAssetsPage.k_Id));
        CreateAndAddSidebarRow(m_PageManager.GetPage(BuiltInPage.k_Id));
        CreateAndAddSeparator();

        foreach (var page in m_PageManager.orderedExtensionPages)
            CreateAndAddSidebarRow(page);

        CreateAndAddSeparator();
        CreateAndAddSidebarRow(m_PageManager.GetPage(MyRegistriesPage.k_Id));

        UpdateMyRegistriesRowVisibility();
    }

    private void CreateAndAddSidebarRow(IPage page)
    {
        var pageId = page.id;
        var sidebarRow = new SidebarRow(page.id, page.displayName);
        sidebarRow.OnLeftClick(() => OnRowClick(pageId));
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

    private void OnRegistriesModified()
    {
        var rowVisibility = UpdateMyRegistriesRowVisibility();
        if (!rowVisibility && m_PageManager.activePage.id == MyRegistriesPage.k_Id)
            m_PageManager.activePage = m_PageManager.GetPage(PageManager.k_DefaultPageId);
    }

    // Returns true if the row is visible
    private bool UpdateMyRegistriesRowVisibility()
    {
        var visibility = m_SettingsProxy.registries?.Count > 1;
        UIUtils.SetElementDisplay(GetRow(MyRegistriesPage.k_Id), visibility);
        return visibility;
    }

    public SidebarRow GetRow(string pageId)
    {
        return Children().OfType<SidebarRow>().FirstOrDefault(i => i.pageId == pageId);
    }
}
