// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal;

internal class OpenManifestAction : PackageAction
{
    private readonly IPackageOperationDispatcher m_OperationDispatcher;

    public OpenManifestAction(IPackageOperationDispatcher operationDispatcher)
    {
        m_OperationDispatcher = operationDispatcher;
    }

    protected override bool TriggerActionImplementation(IPackageVersion version)
    {
        var result = m_OperationDispatcher.OpenManifest(version);
        if (result)
            PackageManagerWindowAnalytics.SendEvent("openManifest", version);
        return result;
    }

    public override bool IsInProgress(IPackageVersion version) => false;

    public override bool IsVisible(IPackageVersion version) => !version.HasTag(PackageTag.LegacyFormat | PackageTag.BuiltIn | PackageTag.Feature) && version.isInstalled;

    public override string GetTooltip(IPackageVersion version, bool isInProgress)
    {
        return L10n.Tr("Select manifest in project browser and open inspector.");
    }

    public override string GetText(IPackageVersion version, bool isInProgress)
    {
        return version.HasTag(PackageTag.Custom | PackageTag.Local) ? L10n.Tr("Edit Manifest") : L10n.Tr("Open Manifest");
    }

    protected override IEnumerable<DisableCondition> GetAllDisableConditions(IPackageVersion version)
    {
        yield return new DisableIfPackageIsInInvalidLocation(version);
        yield return new DisableIfEntitlementsError(version);
        yield return new DisableIfPackageIsNotLoaded(version);
    }
}
