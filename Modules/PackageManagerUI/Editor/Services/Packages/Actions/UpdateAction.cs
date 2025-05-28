// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal;

internal class UpdateAction : UpdateActionBase
{
    public UpdateAction(IPackageOperationDispatcher operationDispatcher,
        IApplicationProxy application,
        IPackageDatabase packageDatabase,
        IPageManager pageManager)
        : base(operationDispatcher, application, packageDatabase, pageManager)
    {
        m_ShowVersion = true;
    }

    public override IPackageVersion GetUpdateTarget(IPackageVersion version) => version?.package?.versions.suggestedUpdate ?? version;

    protected override bool TriggerActionImplementation(IList<IPackage> packages)
    {
        var primaryVersions = new List<IPackageVersion>();
        var updateTargets = new List<IPackageVersion>();

        foreach (var package in packages)
            primaryVersions.Add(package.versions.primary);
        foreach (var version in primaryVersions)
            updateTargets.Add(GetUpdateTarget(version));

        if (!m_OperationDispatcher.Install(updateTargets))
            return false;

        // The current multi-select UI does not allow users to install non-recommended versions
        // Should this change in the future, we'll need to update the analytics event accordingly.
        PackageManagerWindowAnalytics.SendEvent("installUpdateRecommended", primaryVersions);
        return true;
    }

    public override bool IsVisible(IPackageVersion version) =>
        version is { isInstalled: true, isDirectDependency: true }
        && version.package.compliance.status == PackageComplianceStatus.Compliant
        && !version.HasTag(PackageTag.InstalledFromPath)
        && version != GetUpdateTarget(version)
        && m_PageManager.activePage.visualStates.Get(version.package?.uniqueId)?.isLocked != true;
}
