// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal class ImportUpdateAction : ImportActionBase
{
    public ImportUpdateAction(PackageOperationDispatcher operationDispatcher, AssetStoreDownloadManager assetStoreDownloadManager, ApplicationProxy application, UnityConnectProxy unityConnect)
        : base(operationDispatcher, assetStoreDownloadManager, application, unityConnect)
    {
    }

    protected override string analyticEventName => "importUpdate";

    public override bool isRecommended => true;

    public override Icon icon => Icon.Import;

    public override bool IsVisible(IPackageVersion version)
    {
        var versions = version.package.versions;
        return base.IsVisible(version) &&
               versions.imported != null &&
               versions.importAvailable.uploadId == versions.recommended?.uploadId &&
               versions.importAvailable.uploadId != versions.imported.uploadId;
    }

    public override string GetTooltip(IPackageVersion version, bool isInProgress)
    {
        var result = string.Format(L10n.Tr("Click to import updates from the {0} into your project."), version.GetDescriptor());
        if (IsAdaptedPackageUpdate(version.package.versions.importAvailable, version.package.versions.imported))
            result += L10n.Tr("\n*This package update has been adapted for this current version of Unity.");
        return result;
    }

    public override string GetText(IPackageVersion version, bool isInProgress)
    {
        var importAvailable = version.package.versions.importAvailable;
        if (string.IsNullOrEmpty(importAvailable?.versionString))
            return L10n.Tr("Import update");
        return string.Format(IsAdaptedPackageUpdate(importAvailable, version.package.versions.imported) ? L10n.Tr("Import update {0}* to project") : L10n.Tr("Import update {0} to project"), importAvailable.versionString);
    }

    // Adapted package update refers to the edge case where a publisher can publish different packages for different unity versions, resulting us
    // sometimes recommending user to update to a package with the same version string (or even lower version string)
    private static bool IsAdaptedPackageUpdate(IPackageVersion importAvailable, IPackageVersion imported)
    {
        return importAvailable?.versionString == imported?.versionString || importAvailable?.uploadId < imported?.uploadId;
    }
}
