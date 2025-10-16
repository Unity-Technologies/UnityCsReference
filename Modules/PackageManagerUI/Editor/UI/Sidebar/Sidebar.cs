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
    private IPackageDatabase m_PackageDatabase;
    private IPackageManagerPrefs m_PackageManagerPrefs;

    private Dictionary<string, SidebarRow> m_ScopedRegistryRows = new();
    private SidebarRow m_CurrentlySelectedRow;
    private Foldout m_RegistriesFoldout;

    private readonly string k_FoldoutClassName = "sidebarFoldout";

    private void ResolveDependencies()
    {
        var container = ServicesContainer.instance;
        m_UpmRegistryClient = container.Resolve<IUpmRegistryClient>();
        m_SettingsProxy = container.Resolve<IProjectSettingsProxy>();
        m_PageManager = container.Resolve<IPageManager>();
        m_PackageDatabase = container.Resolve<IPackageDatabase>();
        m_PackageManagerPrefs = container.Resolve<IPackageManagerPrefs>();
    }

    public Sidebar()
    {
        ResolveDependencies();
    }

    public void OnEnable()
    {
        m_UpmRegistryClient.onRegistriesModified += UpdateScopedRegistryRelatedRows;
        m_UpmRegistryClient.onRegistriesModified += UpdateComplianceRelatedRow;
        m_PageManager.onActivePageChanged += OnActivePageChanged;
        m_PackageDatabase.onPackagesChanged += OnPackageChanged;
    }

    public void OnCreateGUI()
    {
        CreateRowsAndFoldouts();
        OnActivePageChanged(m_PageManager.activePage);
    }

    public void OnDisable()
    {
        m_UpmRegistryClient.onRegistriesModified -= UpdateScopedRegistryRelatedRows;
        m_UpmRegistryClient.onRegistriesModified -= UpdateComplianceRelatedRow;
        m_PageManager.onActivePageChanged -= OnActivePageChanged;
        m_PackageDatabase.onPackagesChanged -= OnPackageChanged;

        m_PackageManagerPrefs.orderedSidebarFoldoutsExpandedStatus = Children().OfType<Foldout>().Select(i => i.value).ToArray();
    }

    private void CreateRowsAndFoldouts()
    {
        var projectFoldout = CreateAndAddFoldout(L10n.Tr("Project"));
        projectFoldout.Add(CreateSidebarRow(m_PageManager.GetPage(InProjectPage.k_Id)));
        projectFoldout.Add(CreateSidebarRow(m_PageManager.GetPage(InProjectUpdatesPage.k_Id)));
        projectFoldout.Add(CreateSidebarRow(m_PageManager.GetPage(InProjectNonCompliancePage.k_Id)));
        projectFoldout.Add(CreateSidebarRow(m_PageManager.GetPage(InProjectErrorsAndWarningsPage.k_Id)));

        var sourcesFoldout = CreateAndAddFoldout(L10n.Tr("Sources"));
        sourcesFoldout.Add(CreateSidebarRow(m_PageManager.GetPage(MyAssetsPage.k_Id)));
        sourcesFoldout.Add(CreateSidebarRow(m_PageManager.GetPage(UnityRegistryPage.k_Id)));
        sourcesFoldout.Add(CreateSidebarRow(m_PageManager.GetPage(BuiltInPage.k_Id)));

        var cloudFoldout = CreateAndAddFoldout(L10n.Tr("Cloud"));
        foreach (var page in m_PageManager.orderedExtensionPages)
            cloudFoldout.Add(CreateSidebarRow(page));

        m_RegistriesFoldout = CreateAndAddFoldout(L10n.Tr("My Registries"));

        var foldouts = Children().OfType<Foldout>().ToArray();
        if (m_PackageManagerPrefs.orderedSidebarFoldoutsExpandedStatus?.Length == foldouts.Length)
            for (var i = 0; i < foldouts.Length; i++)
                foldouts[i].value = m_PackageManagerPrefs.orderedSidebarFoldoutsExpandedStatus[i];

        UpdateComplianceRelatedRow();
        UpdateErrorsAndWarningsRelatedRow();
        UpdateScopedRegistryRelatedRows();
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
        return sidebarRow;
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

    private void UpdateComplianceRelatedRow()
    {
        var nonCompliancePage = m_PageManager.GetPage(InProjectNonCompliancePage.k_Id);
        var showNonCompliantPage = m_PackageDatabase.allPackages.Any(nonCompliancePage.ShouldInclude);

        UIUtils.SetElementDisplay(GetRow(InProjectNonCompliancePage.k_Id), showNonCompliantPage);

        if (!showNonCompliantPage && m_PageManager.activePage == nonCompliancePage)
            m_PageManager.activePage = m_PageManager.GetPage(PageManager.k_DefaultPageId);
    }

    private void UpdateErrorsAndWarningsRelatedRow()
    {
        var errorsAndWarningsPage = m_PageManager.GetPage(InProjectErrorsAndWarningsPage.k_Id);
        var showErrorsAndWarningsPage = m_PackageDatabase.allPackages.AnyMatches(errorsAndWarningsPage.ShouldInclude);
        var errorsAndWarningsRow = GetRow(InProjectErrorsAndWarningsPage.k_Id);

        UIUtils.SetElementDisplay(errorsAndWarningsRow, showErrorsAndWarningsPage);

        if (showErrorsAndWarningsPage)
            errorsAndWarningsRow?.UpdateIcon(errorsAndWarningsPage.icon);
        else if (m_PageManager.activePage == errorsAndWarningsPage)
            m_PageManager.activePage = m_PageManager.GetPage(PageManager.k_DefaultPageId);
    }

    private void OnPackageChanged(PackagesChangeArgs args)
    {
        var changedPackages = args.added.Concat(args.removed).Concat(args.updated).Concat(args.preUpdate).Concat(args.progressUpdated);

        var errorsAndWarningsPage = m_PageManager.GetPage(InProjectErrorsAndWarningsPage.k_Id);
        if (changedPackages.AnyMatches(p => errorsAndWarningsPage.ShouldInclude(p)))
            UpdateErrorsAndWarningsRelatedRow();

        var nonCompliancePage = m_PageManager.GetPage(InProjectNonCompliancePage.k_Id);
        if (changedPackages.AnyMatches(p => nonCompliancePage.ShouldInclude(p)))
            UpdateComplianceRelatedRow();
    }

    private void UpdateScopedRegistryRelatedRows()
    {
        var scopedRegistries = m_SettingsProxy.scopedRegistries.ToArray();
        var newPages = scopedRegistries.Length > 0 ? new []{ m_PageManager.GetPage(MyRegistriesPage.k_Id) }.Concat(scopedRegistries.Select(r => m_PageManager.GetPage(r))).ToArray() : Array.Empty<IPage>();
        var oldRows = m_RegistriesFoldout.Children().OfType<SidebarRow>().ToDictionary(r => r.pageId);

        m_RegistriesFoldout.Clear();
        foreach (var page in newPages)
            m_RegistriesFoldout.Add(oldRows.GetValueOrDefault(page.id) ?? CreateSidebarRow(page));

        UIUtils.SetElementDisplay(m_RegistriesFoldout, newPages.Length > 0);

        var activePageId = m_PageManager.activePage.id;
        if (oldRows.ContainsKey(activePageId) && newPages.All(p => p.id != activePageId))
            m_PageManager.activePage = m_PageManager.GetPage(PageManager.k_DefaultPageId);
    }

    public SidebarRow GetRow(string pageId)
    {
        return Children().OfType<Foldout>().SelectMany(f => f.Children().OfType<SidebarRow>()).FirstOrDefault(i => i.pageId == pageId);
    }
}
