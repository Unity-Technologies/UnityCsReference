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
    public DisableIfCompiling(IApplicationProxy application)
    {
        active = application.isCompiling;
        tooltip = k_Tooltip;
    }
}

internal class DisableIfNoNetwork : DisableCondition
{
    private static readonly string k_Tooltip = L10n.Tr("You need to restore your network connection to perform this action.");
    public DisableIfNoNetwork(IApplicationProxy application)
    {
        active = !application.isInternetReachable;
        tooltip = k_Tooltip;
    }
}

internal class DisableIfInstallOrEmbedOrUninstallInProgress : DisableCondition
{
    private static readonly string k_Tooltip = L10n.Tr("You need to wait until other install, embed or uninstall operations are finished to perform this action.");
    public DisableIfInstallOrEmbedOrUninstallInProgress(IPackageOperationDispatcher operationDispatcher)
    {
        active = operationDispatcher.isInstallOrUninstallInProgress || operationDispatcher.isEmbedInProgress;
        tooltip = k_Tooltip;
    }
}

internal class DisableIfExportingInProgress : DisableCondition
{
    private static readonly string k_Tooltip = L10n.Tr("You need to wait until the export operation is finished to perform this action.");
    public DisableIfExportingInProgress(IPackage package)
    {
        active = package.progress == PackageProgress.Exporting;
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

internal class DisableIfEnterpriseEntitlementsError : DisableCondition
{
    private static readonly string k_Tooltip = L10n.Tr("You need to sign in with a licensed account to perform this action.");
    public DisableIfEnterpriseEntitlementsError(IPackageVersion version)
    {
        active = version != null && version.package.hasEntitlementsError && version.package.isEnterprise;
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

    public DisableIfEntitlementsError(Sample sample)
    {
        active = !sample.isDefault && sample.package?.versions.primary.hasEntitlementsError == true;
        tooltip = k_Tooltip;
    }
}

internal class DisableIfPackageIsNotLoaded : DisableCondition
{
    public DisableIfPackageIsNotLoaded(IPackageVersion version)
    {
        active = PackageIsNotLoaded(version);
        tooltip = L10n.Tr("This package isn't loaded in your project.");
    }

    public DisableIfPackageIsNotLoaded(Sample sample)
    {
        active = sample is { isDefault: false, package: not null }
                 && PackageIsNotLoaded(sample.package.versions.primary);
        tooltip = L10n.Tr("The package this sample belongs to isn't loaded in your project.");
    }

    private bool PackageIsNotLoaded(IPackageVersion version)
    {
        return version?.errors?.AnyMatches(i => i.errorCode == UIErrorCode.UpmError_PackageNotLoaded) == true;
    }
}

internal class DisableIfPackageIsInInvalidLocation : DisableCondition
{
    public DisableIfPackageIsInInvalidLocation(IPackageVersion version)
    {
        active = PackageIsInInvalidLocation(version);
        tooltip = L10n.Tr("This package is stored in an invalid location.");
    }

    public DisableIfPackageIsInInvalidLocation(Sample sample)
    {
        active = PackageIsInInvalidLocation(sample.package?.versions?.primary);
        tooltip = L10n.Tr("The package this sample belongs to is stored in an invalid location.");
    }

    private bool PackageIsInInvalidLocation(IPackageVersion version)
    {
        var error = version?.errors?.FirstMatch(e => !e.HasAttribute(UIError.Attribute.Clearable | UIError.Attribute.HiddenFromUI));
        return error is { errorCode: UIErrorCode.UpmError_InvalidSourcePath };
    }
}

internal class DisableIfSampleHasNoPath : DisableCondition
{
    private static readonly string k_Tooltip = L10n.Tr("The path property for this sample is missing.");
    public DisableIfSampleHasNoPath(Sample sample)
    {
        active = sample is { isDefault: false, package: not null }
                 && string.IsNullOrEmpty(sample.resolvedPath);
        tooltip = k_Tooltip;
    }
}

internal class DisableIfSamplePathDoesNotExist : DisableCondition
{
    private static readonly string k_Tooltip = L10n.Tr("The path specified for this sample doesn't exist.");
    public DisableIfSamplePathDoesNotExist(Sample sample, IIOProxy ioProxy)
    {
        active = sample is { isDefault: false, package: not null }
                 && !string.IsNullOrEmpty(sample.resolvedPath)
                 && !ioProxy.DirectoryExists(sample.resolvedPath);
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
