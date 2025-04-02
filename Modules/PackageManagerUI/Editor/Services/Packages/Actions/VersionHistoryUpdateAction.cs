// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal class VersionHistoryUpdateAction : UpdateActionBase
{
    public VersionHistoryUpdateAction(IPackageOperationDispatcher operationDispatcher,
        IApplicationProxy application,
        IPackageDatabase packageDatabase,
        IPageManager pageManager)
        : base(operationDispatcher, application, packageDatabase, pageManager)
    {
        m_ShowVersion = false;
    }

    public override IPackageVersion GetUpdateTarget(IPackageVersion version) => version;

    public override bool IsVisible(IPackageVersion version)
    {
        if (version == null || version.IsRequestedButOverriddenVersion)
            return false;

        if (!version.isInstalled)
            return true;

        var installed = version.package.versions.installed;
        if (installed == null)
            return false;

        if (installed.HasTag(PackageTag.InstalledFromPath))
        {
            // This allows Users to switch from their shadowed version to the official registry version
            // to gain updates or support provided by the official package.
            return version.version == installed.version && installed.HasTag(PackageTag.Local | PackageTag.Git);
        }

        return m_PageManager.activePage.visualStates.Get(version.package?.uniqueId)?.isLocked != true;
    }
}
