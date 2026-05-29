// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal;

internal class CustomizeAction : PackageAction
{
    private readonly IPackageOperationDispatcher m_OperationDispatcher;
    private readonly IApplicationProxy m_Application;

    public CustomizeAction(IPackageOperationDispatcher packageOperationDispatcher, IApplicationProxy application)
    {
        m_OperationDispatcher = packageOperationDispatcher;
        m_Application = application;
    }

    protected override bool TriggerActionImplementation(IPackageVersion version)
    {
        if (!m_OperationDispatcher.Embed(version.package))
            return false;

        PackageManagerWindowAnalytics.SendEvent("customize", version);
        return true;
    }

    public override bool IsVisible(IPackageVersion version)
    {
        return version.isInstalled
               && version.isDirectDependency // Currently UpmClient only supports embedding packages that are direct dependencies, should be fixed in [PAK-8047]
               && !version.HasTag(PackageTag.Custom | PackageTag.LegacyFormat | PackageTag.BuiltIn | PackageTag.Feature);
    }

    protected override IEnumerable<DisableCondition> GetAllTemporaryDisableConditions()
    {
        yield return new DisableIfInstallOrEmbedOrUninstallInProgress(m_OperationDispatcher);
        yield return new DisableIfCompiling(m_Application);
    }

    public override string GetTooltip(IPackageVersion version, bool isInProgress)
    {
        if (isInProgress)
            return k_InProgressGenericTooltip;
        return L10n.Tr("Embed the package in your project so you can modify it.");
    }

    public override string GetText(IPackageVersion version, bool isInProgress)
    {
        return L10n.Tr("Customize");
    }

    public override bool IsInProgress(IPackageVersion version) => m_OperationDispatcher.isEmbedInProgress;

    protected override IEnumerable<DisableCondition> GetAllDisableConditions(IPackageVersion version)
    {
        yield return new DisableIfPackageIsInInvalidLocation(version);
        yield return new DisableIfEntitlementsError(version);
        yield return new DisableIfPackageIsNotLoaded(version);
    }
}
