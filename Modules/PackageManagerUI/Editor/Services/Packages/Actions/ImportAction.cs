// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal;

internal class ImportAction : PackageAction
{
    private readonly PackageOperationDispatcher m_OperationDispatcher;
    private readonly AssetStoreDownloadManager m_AssetStoreDownloadManager;
    private readonly ApplicationProxy m_Application;
    public ImportAction(PackageOperationDispatcher operationDispatcher, AssetStoreDownloadManager assetStoreDownloadManager, ApplicationProxy application)
    {
        m_OperationDispatcher = operationDispatcher;
        m_AssetStoreDownloadManager = assetStoreDownloadManager;
        m_Application = application;
    }

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
        return version?.HasTag(PackageTag.LegacyFormat) == true
               && version.isAvailableOnDisk
               && version.package.progress == PackageProgress.None
               && m_AssetStoreDownloadManager.GetDownloadOperation(version.package.product?.id)?.isProgressVisible != true
               && (version.importedAssets == null || !version.importedAssets.Any());
    }

    public override string GetTooltip(IPackageVersion version, bool isInProgress)
    {
        return string.Format(L10n.Tr("Click to import assets from the {0} into your project."), version.GetDescriptor());
    }

    public override string GetText(IPackageVersion version, bool isInProgress)
    {
        return !string.IsNullOrEmpty(version?.versionString) ? string.Format(L10n.Tr("Import {0} to project"), version.versionString) : L10n.Tr("Import");
    }

    public override bool IsInProgress(IPackageVersion version) => false;

    protected override IEnumerable<DisableCondition> GetAllTemporaryDisableConditions()
    {
        yield return new DisableIfCompiling(m_Application);
    }

    protected override IEnumerable<DisableCondition> GetAllDisableConditions(IPackageVersion version)
    {
        yield return new DisableIfPackageDisabled(version);
    }
}
