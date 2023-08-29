// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal;

internal class GitUpdateAction : PackageAction
{
    private readonly IPackageOperationDispatcher m_OperationDispatcher;
    private readonly IUpmCache m_UpmCache;
    private readonly IApplicationProxy m_Application;
    public GitUpdateAction(IPackageOperationDispatcher operationDispatcher, IUpmCache upmCache, IApplicationProxy application)
    {
        m_OperationDispatcher = operationDispatcher;
        m_UpmCache = upmCache;
        m_Application = application;
    }

    protected override bool TriggerActionImplementation(IPackageVersion version)
    {
        var installedVersion = version.package.versions.installed;
        var packageInfo = m_UpmCache.GetBestMatchPackageInfo(installedVersion.name, true);
        m_OperationDispatcher.Install(packageInfo.packageId);
        PackageManagerWindowAnalytics.SendEvent("updateGit", installedVersion.uniqueId);
        return true;
    }

    public override bool IsVisible(IPackageVersion version) => version?.package.versions.installed?.HasTag(PackageTag.Git) == true;

    public override string GetTooltip(IPackageVersion version, bool isInProgress)
    {
        if (isInProgress)
            return k_InProgressGenericTooltip;
        return L10n.Tr("Click to check for updates and update to latest version");
    }

    public override string GetText(IPackageVersion version, bool isInProgress)
    {
        return L10n.Tr("Update");
    }

    public override bool IsInProgress(IPackageVersion version) => m_OperationDispatcher.IsInstallInProgress(version);

    protected override IEnumerable<DisableCondition> GetAllTemporaryDisableConditions()
    {
        yield return new DisableIfInstallOrUninstallInProgress(m_OperationDispatcher);
        yield return new DisableIfCompiling(m_Application);
    }
}
