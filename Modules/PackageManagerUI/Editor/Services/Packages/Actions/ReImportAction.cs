// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal class ReImportAction : ImportActionBase
{
    public ReImportAction(IPackageOperationDispatcher operationDispatcher, IAssetStoreDownloadManager assetStoreDownloadManager, IApplicationProxy application, IUnityConnectProxy unityConnect)
        : base(operationDispatcher, assetStoreDownloadManager, application, unityConnect)
    {
    }

    protected override string analyticEventName => "importAgain";

    // Re-import action covers all the `import` scenarios that's not covered by `ImportNew` and `ImportUpdate` actions
    public override bool IsVisible(IPackageVersion version)
    {
        var versions = version.package.versions;
        return base.IsVisible(version) && versions.imported != null &&
               (versions.importAvailable.uploadId != versions.recommended?.uploadId || versions.importAvailable.uploadId == versions.imported.uploadId);
    }

    public override string GetTooltip(IPackageVersion version, bool isInProgress)
    {
        return string.Format(L10n.Tr("Click to re-import assets from the {0} into your project."), version.GetDescriptor());
    }

    public override string GetText(IPackageVersion version, bool isInProgress)
    {
        var importAvailableVersionString = version.package.versions.importAvailable?.versionString;
        if (string.IsNullOrEmpty(importAvailableVersionString))
            return L10n.Tr("Re-import");
        return string.Format(importAvailableVersionString == version.versionString ? L10n.Tr("Re-import {0}") : L10n.Tr("Import {0}"), importAvailableVersionString);
    }
}
