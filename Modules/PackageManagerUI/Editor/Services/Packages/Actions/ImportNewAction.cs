// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal class ImportNewAction : ImportActionBase
{
    public ImportNewAction(IPackageOperationDispatcher operationDispatcher, IAssetStoreDownloadManager assetStoreDownloadManager, IApplicationProxy application, IUnityConnectProxy unityConnect)
        : base(operationDispatcher, assetStoreDownloadManager, application, unityConnect)
    {
    }

    protected override string analyticEventName => "importNew";

    public override bool isRecommended => true;

    public override Icon icon => Icon.Import;

    protected override bool TriggerActionImplementation(IPackageVersion version)
    {
        m_OperationDispatcher.Import(version.package);
        PackageManagerWindowAnalytics.SendEvent("importNew", version);
        return true;
    }

    public override bool IsVisible(IPackageVersion version)
    {
        return base.IsVisible(version) && version.package.versions.imported == null;
    }

    public override string GetTooltip(IPackageVersion version, bool isInProgress)
    {
        return string.Format(L10n.Tr("Click to import assets from the {0} into your project."), version.GetDescriptor());
    }

    public override string GetText(IPackageVersion version, bool isInProgress)
    {
        return !string.IsNullOrEmpty(version?.versionString) ? string.Format(L10n.Tr("Import {0} to project"), version.versionString) : L10n.Tr("Import");
    }
}
