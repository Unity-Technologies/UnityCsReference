// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal abstract class DisableCondition
{
    public string tooltip { get; protected set; }

    public bool active { get; protected set; }
}

internal class DisableIfCompiling : DisableCondition
{
    private static readonly string k_Tooltip = L10n.Tr("You need to wait until the compilation is finished to perform this action.");
    public DisableIfCompiling(ApplicationProxy application)
    {
        active = application.isCompiling;
        tooltip = k_Tooltip;
    }
}

internal class DisableIfNoNetwork : DisableCondition
{
    private static readonly string k_Tooltip = L10n.Tr("You need to restore your network connection to perform this action.");
    public DisableIfNoNetwork(ApplicationProxy application)
    {
        active = !application.isInternetReachable;
        tooltip = k_Tooltip;
    }
}

internal class DisableIfInstallOrUninstallInProgress : DisableCondition
{
    private static readonly string k_Tooltip = L10n.Tr("You need to wait until other install or uninstall operations are finished to perform this action.");
    public DisableIfInstallOrUninstallInProgress(PackageOperationDispatcher operationDispatcher)
    {
        active = operationDispatcher.isInstallOrUninstallInProgress;
        tooltip = k_Tooltip;
    }
}

internal class DisableIfVersionDeprecated : DisableCondition
{
    private static readonly string k_Tooltip = L10n.Tr("This version is deprecated.");
    public DisableIfVersionDeprecated(IPackageVersion version)
    {
        active = version != null && version.HasTag(PackageTag.Deprecated) && version.availableRegistry != RegistryType.MyRegistries;
        tooltip = k_Tooltip;
    }
}

internal class DisableIfEntitlementsError : DisableCondition
{
    private static readonly string k_Tooltip = L10n.Tr("You need to sign in with a licensed account to perform this action.");
    public DisableIfEntitlementsError(IPackageVersion version)
    {
        active = version != null && version.package.hasEntitlementsError;
        tooltip = k_Tooltip;
    }
}

internal class DisableIfPackageDisabled : DisableCondition
{
    private static readonly string k_Tooltip = L10n.Tr("This package is no longer available.");
    public DisableIfPackageDisabled(IPackageVersion version)
    {
        active = version != null && version.HasTag(PackageTag.Disabled);
        tooltip = k_Tooltip;
    }
}
