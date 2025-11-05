// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal;

internal interface IInProjectPackagesMonitor : IService
{
    void OnEditorFinishLoadingProject();
    void OnRegisteredPackages(PackageRegistrationEventArgs args);
    void OnPackageManagerResolve();
}

[Serializable]
internal class InProjectPackagesMonitor : BaseService<IInProjectPackagesMonitor>, IInProjectPackagesMonitor
{
    [SerializeField]
    private bool m_CheckStartupPackageErrors;

    [SerializeField]
    private bool m_StartupPackagesChecked;

    private readonly IApplicationProxy m_Application;
    private readonly IProjectSettingsProxy m_SettingsProxy;
    private readonly IUpmCache m_UpmCache;
    private readonly IUpmRegistryClient m_UpmRegistryClient;
    private readonly IPackageDatabase m_PackageDatabase;
    private readonly IPageRefreshHandler m_PageRefreshHandler;
    private readonly ICustomDisplayDialog m_CustomDisplayDialog;
    public InProjectPackagesMonitor(IApplicationProxy application,
        IProjectSettingsProxy settingsProxy,
        IUpmCache upmCache,
        IUpmRegistryClient upmRegistryClient,
        IPackageDatabase packageDatabase,
        IPageRefreshHandler pageRefreshHandler,
        ICustomDisplayDialog customDisplayDialog)
    {
        m_Application = RegisterDependency(application);
        m_SettingsProxy = RegisterDependency(settingsProxy);
        m_UpmCache = RegisterDependency(upmCache);
        m_UpmRegistryClient = RegisterDependency(upmRegistryClient);
        m_PackageDatabase = RegisterDependency(packageDatabase);
        m_PageRefreshHandler = RegisterDependency(pageRefreshHandler);
        m_CustomDisplayDialog = RegisterDependency(customDisplayDialog);
    }

    public override void OnEnable()
    {
        m_PackageDatabase.onPackagesChanged += OnPackagesChanged;
    }

    public override void OnDisable()
    {
        m_PackageDatabase.onPackagesChanged -= OnPackagesChanged;
    }

    public void OnEditorFinishLoadingProject()
    {
        if (m_Application.isBatchMode || !m_Application.isUpmRunning)
            return;

        // We only check start up packages errors once per project load, if the Package Manager UI is reset, this value won't be set to true again
        // and hence we won't try to pop another dialog
        m_CheckStartupPackageErrors = !m_SettingsProxy.oneTimePackageErrorsPopUpShown;
        if (m_PageRefreshHandler.GetRefreshTimestamp(RefreshOptions.UpmListOffline) <= 0)
            m_PageRefreshHandler.Refresh(RefreshOptions.UpmListOffline);
    }

    public void OnRegisteredPackages(PackageRegistrationEventArgs _)
    {
        if (m_Application.isBatchMode || !m_Application.isUpmRunning)
            return;
        m_PageRefreshHandler.Refresh(RefreshOptions.UpmListOffline);
    }

    public void OnPackageManagerResolve()
    {
        if (m_Application.isBatchMode || !m_Application.isUpmRunning)
            return;
        m_PackageDatabase.ClearSamplesCache();
        m_UpmRegistryClient.CheckRegistriesChanged();
        m_PageRefreshHandler.Refresh(RefreshOptions.UpmListOffline);
    }

    private void OnPackagesChanged(PackagesChangeArgs args)
    {
        // The Active Trust window will have warned users about packages being added or removed
        // so we don't need to show the untrusted/invalid signature dialog in that case
        if (args.packagesChangedSource == PackagesChangedSource.AddAndRemove)
            return;

        if (!m_StartupPackagesChecked)
        {
            if (!m_UpmCache.installedPackageInfosReady)
                return;
            m_StartupPackagesChecked = true;
            CheckStartupPackageErrors();
        }
        else
        {
            CheckForUntrustedPackages(args);
        }
    }

    private void CheckForUntrustedPackages(PackagesChangeArgs args)
    {
        IEnumerable<IPackage> PackagesToCheck()
        {
            foreach (var p in args.added)
                if (p.versions.installed != null)
                    yield return p;

            for (var i = 0; i < args.preUpdate.Count; i++)
            {
                var preUpdate = args.preUpdate[i];
                var postUpdate = args.updated[i];
                if (postUpdate.versions.installed != null && preUpdate.versions.installed?.packageId != postUpdate.versions.installed.packageId)
                    yield return postUpdate;
            }
        }

        IPackage unsignedPackage = null;
        IPackage invalidSignaturePackage = null;
        foreach (var package in PackagesToCheck())
        {
            var trustAndSignature = package.versions.installed?.trustAndSignature ?? TrustAndSignature.NotApplicable;
            if (trustAndSignature == TrustAndSignature.UntrustedInvalidSignature)
            {
                invalidSignaturePackage = package;
                break;
            }
            if (unsignedPackage == null && trustAndSignature == TrustAndSignature.UntrustedNoSignature)
                unsignedPackage = package;
        }

        if (invalidSignaturePackage != null)
        {
            var dialogArgs = new CustomDecisionDialogArgs(L10n.Tr("Invalid Signature"),"invalidSignatureInProject", L10n.Tr("Open Package Manager"), L10n.Tr("Close"))
            {
                headerIcon = Icon.PackageErrorLarge,
                headerMainText = L10n.Tr("This project contains at least one package that has an invalid signature."),
                bodyText = L10n.Tr("This might indicate they are unsafe or malicious. Would you like to open the Package Manager to review these packages?"),
                readMoreUrl = $"https://docs.unity3d.com/{m_Application.shortUnityVersion}/Documentation/Manual/upm-errors.html#pkg-invalid-sig",
                readMoreClickedAnalyticsId = "invalid-signature-in-project-read-more",
                headerColor = HeaderColor.Red
            };
            if (m_CustomDisplayDialog.Show(dialogArgs) == DialogResult.DefaultAction)
                PackageManagerWindow.OpenAndSelectPackage(invalidSignaturePackage.uniqueId, InProjectErrorsAndWarningsPage.k_Id);
        }
        else if (unsignedPackage != null)
        {
            var dialogArgs = new CustomDecisionDialogArgs(L10n.Tr("Missing Signature"), "unsignedPackageInProject", L10n.Tr("Open Package Manager"), L10n.Tr("Close"))
            {
                headerIcon = Icon.PackageWarningLarge,
                headerMainText = L10n.Tr("This project contains at least one package that doesn't have a signature."),
                bodyText = L10n.Tr("This could indicate they are potentially unsafe. Would you like to open the Package Manager to review these packages?"),
                readMoreUrl = $"https://docs.unity3d.com/{m_Application.shortUnityVersion}/Documentation/Manual/upm-signature.html",
                readMoreClickedAnalyticsId = "unsigned-package-in-project-read-more"
            };
            if (m_CustomDisplayDialog.Show(dialogArgs) == DialogResult.DefaultAction)
                PackageManagerWindow.OpenAndSelectPackage(unsignedPackage.uniqueId, InProjectErrorsAndWarningsPage.k_Id);
        }
    }

    private void CheckStartupPackageErrors()
    {
        if (!m_CheckStartupPackageErrors)
            return;

        if (!m_PackageDatabase.allPackages.AnyMatches(p => p.state == PackageState.Error && (p.versions.installed != null || p.versions.imported != null)))
            return;

        var dialogResult = m_Application.DisplayDialogComplex("openPackageManagerWithErrorPackages",
            L10n.Tr("Packages with Errors"),
            L10n.Tr("This project contains one or more packages with errors. Do you want to open Package Manager?"),
            L10n.Tr("Open Package Manager"),
            L10n.Tr("Dismiss"),
            L10n.Tr("Dismiss Forever"));
        switch (dialogResult)
        {
            case 0:
                PackageManagerWindow.OpenAndSelectPage(InProjectErrorsAndWarningsPage.k_Id);
                break;
            case 2:
                m_SettingsProxy.oneTimePackageErrorsPopUpShown = true;
                m_SettingsProxy.Save();
                break;
        }
    }
}
