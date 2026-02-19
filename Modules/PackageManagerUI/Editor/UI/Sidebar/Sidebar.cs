// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal;

internal class Sidebar : ScrollView
{
    [Serializable]
    public new class UxmlSerializedData : ScrollView.UxmlSerializedData
    {
        public override object CreateInstance()
        {
            var container = ServicesContainer.instance;
            return new Sidebar(
                container.Resolve<IUpmRegistryClient>(),
                container.Resolve<IProjectSettingsProxy>(),
                container.Resolve<IPageManager>(),
                container.Resolve<IPackageDatabase>(),
                container.Resolve<IPackageManagerPrefs>());
        }
    }

    private Dictionary<string, SidebarRow> m_ScopedRegistryRows = new();
    private SidebarRow m_CurrentlySelectedRow;
    private Foldout m_RegistriesFoldout;

    private readonly string k_FoldoutClassName = "sidebarFoldout";

    private readonly IUpmRegistryClient m_UpmRegistryClient;
    private readonly IProjectSettingsProxy m_SettingsProxy;
    private readonly IPageManager m_PageManager;
    private readonly IPackageDatabase m_PackageDatabase;
    private readonly IPackageManagerPrefs m_PackageManagerPrefs;

    public Sidebar(
        IUpmRegistryClient upmRegistryClient,
        IProjectSettingsProxy settingsProxy,
        IPageManager pageManager,
        IPackageDatabase packageDatabase,
        IPackageManagerPrefs packageManagerPrefs)
    {
        m_UpmRegistryClient = upmRegistryClient;
        m_SettingsProxy = settingsProxy;
        m_PageManager = pageManager;
        m_PackageDatabase = packageDatabase;
        m_PackageManagerPrefs = packageManagerPrefs;
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

        #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        m_PackageManagerPrefs.orderedSidebarFoldoutsExpandedStatus = Children().FilterByType<Foldout>().Select(i => i.value).ToArray();
#pragma warning restore UA2001
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

        #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        var foldouts = Children().FilterByType<Foldout>().ToArray();
#pragma warning restore UA2001
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
        var showNonCompliantPage = m_PackageDatabase.allPackages.Exists(nonCompliancePage.ShouldInclude);

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
        #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        var changedPackages = args.added.Join(args.removed).Join(args.updated).Join(args.preUpdate).Join(args.progressUpdated);
#pragma warning restore UA2001

        var errorsAndWarningsPage = m_PageManager.GetPage(InProjectErrorsAndWarningsPage.k_Id);
        if (changedPackages.AnyMatches(p => errorsAndWarningsPage.ShouldInclude(p)))
            UpdateErrorsAndWarningsRelatedRow();

        var nonCompliancePage = m_PageManager.GetPage(InProjectNonCompliancePage.k_Id);
        if (changedPackages.AnyMatches(p => nonCompliancePage.ShouldInclude(p)))
            UpdateComplianceRelatedRow();
    }

    private void UpdateScopedRegistryRelatedRows()
    {
        #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        var scopedRegistries = m_SettingsProxy.scopedRegistries.ToArray();
#pragma warning restore UA2001
        #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        var newPages = scopedRegistries.Length > 0 ? new []{ m_PageManager.GetPage(MyRegistriesPage.k_Id) }.Join(scopedRegistries.Select(r => m_PageManager.GetPage(r))).ToArray() : Array.Empty<IPage>();
#pragma warning restore UA2001
        #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        var oldRows = m_RegistriesFoldout.Children().FilterByType<SidebarRow>().ToDictionary(r => r.pageId);
#pragma warning restore UA2001

        m_RegistriesFoldout.Clear();
        foreach (var page in newPages)
            m_RegistriesFoldout.Add(oldRows.GetValueOrDefault(page.id) ?? CreateSidebarRow(page));

        UIUtils.SetElementDisplay(m_RegistriesFoldout, newPages.Length > 0);

        var activePageId = m_PageManager.activePage.id;
        #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        if (oldRows.ContainsKey(activePageId) && newPages.All(p => p.id != activePageId))
#pragma warning restore UA2001
            m_PageManager.activePage = m_PageManager.GetPage(PageManager.k_DefaultPageId);
    }

    public SidebarRow GetRow(string pageId)
    {
        #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        return Children().FilterByType<Foldout>().SelectMany(f => f.Children().FilterByType<SidebarRow>()).FirstOrDefault(i => i.pageId == pageId);
#pragma warning restore UA2001
    }
}
