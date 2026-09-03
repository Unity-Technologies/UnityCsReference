// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal interface IDisableCondition<in TItem>
{
    bool IsActive(TItem item, out string tooltip);
}

internal class DisableIfCompiling : IDisableCondition<IPackageVersion>
{
    private static readonly string k_Tooltip = L10n.Tr("You need to wait until the compilation is finished to perform this action.", null);
    private readonly IApplicationProxy m_Application;
    public DisableIfCompiling(IApplicationProxy application)
    {
        m_Application = application;
    }

    public bool IsActive(IPackageVersion version, out string tooltip)
    {
        tooltip = k_Tooltip;
        return m_Application.isCompiling;
    }
}

internal class DisableIfNoNetwork : IDisableCondition<IPackageVersion>
{
    private static readonly string k_Tooltip = L10n.Tr("You need to restore your network connection to perform this action.", null);
    private readonly IApplicationProxy m_Application;
    public DisableIfNoNetwork(IApplicationProxy application)
    {
        m_Application = application;
    }

    public bool IsActive(IPackageVersion version, out string tooltip)
    {
        tooltip = k_Tooltip;
        return !m_Application.isInternetReachable;
    }
}

internal class DisableIfInstallOrEmbedOrUninstallInProgress : IDisableCondition<IPackageVersion>
{
    private static readonly string k_Tooltip = L10n.Tr("You need to wait until other install, embed or uninstall operations are finished to perform this action.", null);
    private readonly IPackageOperationDispatcher m_OperationDispatcher;
    public DisableIfInstallOrEmbedOrUninstallInProgress(IPackageOperationDispatcher operationDispatcher)
    {
        m_OperationDispatcher = operationDispatcher;
    }

    public bool IsActive(IPackageVersion version, out string tooltip)
    {
        tooltip = k_Tooltip;
        return m_OperationDispatcher.isInstallOrUninstallInProgress || m_OperationDispatcher.isEmbedInProgress;
    }
}

internal class DisableIfExportingInProgress : IDisableCondition<IPackageVersion>
{
    private static readonly string k_Tooltip = L10n.Tr("You need to wait until the export operation is finished to perform this action.", null);

    public bool IsActive(IPackageVersion version, out string tooltip)
    {
        tooltip = k_Tooltip;
        return version is { package.progress: PackageProgress.Exporting };
    }
}

internal class DisableIfVersionDeprecated : IDisableCondition<IPackageVersion>
{
    private static readonly string k_Tooltip = L10n.Tr("This version is deprecated.", null);

    public bool IsActive(IPackageVersion item, out string tooltip)
    {
        tooltip = k_Tooltip;
        var version = GetVersionToCheck(item);
        return version is { availableRegistry: not RegistryType.MyRegistries } && version.HasTag(PackageTag.Deprecated);
    }

    // Allows derived conditions (e.g. update actions) to check a version other than the item itself.
    protected virtual IPackageVersion GetVersionToCheck(IPackageVersion item) => item;
}

internal class DisableIfEnterpriseEntitlementsError : IDisableCondition<IPackageVersion>
{
    private static readonly string k_Tooltip = L10n.Tr("You need to sign in with a licensed account to perform this action.", null);

    public bool IsActive(IPackageVersion version, out string tooltip)
    {
        tooltip = k_Tooltip;
        return version is { package.hasEntitlementsError: true, package.isEnterprise: true };
    }
}

internal class DisableIfEntitlementsError : IDisableCondition<IPackageVersion>, IDisableCondition<Sample>
{
    private static readonly string k_Tooltip = L10n.Tr("You need to sign in with a licensed account to perform this action.", null);

    public bool IsActive(IPackageVersion version, out string tooltip)
    {
        tooltip = k_Tooltip;
        return version is { package.hasEntitlementsError: true };
    }

    public bool IsActive(Sample sample, out string tooltip)
    {
        tooltip = k_Tooltip;
        return sample is { isDefault: false, package.versions.primary.hasEntitlementsError: true };
    }
}

internal class DisableIfPackageIsNotLoaded : IDisableCondition<IPackageVersion>, IDisableCondition<Sample>
{
    private static readonly string k_Tooltip = L10n.Tr("This package isn't loaded in your project.", null);
    private static readonly string k_SampleTooltip = L10n.Tr("The package this sample belongs to isn't loaded in your project.", null);

    public bool IsActive(IPackageVersion version, out string tooltip)
    {
        tooltip = k_Tooltip;
        return PackageIsNotLoaded(version);
    }

    public bool IsActive(Sample sample, out string tooltip)
    {
        tooltip = k_SampleTooltip;
        return sample is { isDefault: false, package: not null }
               && PackageIsNotLoaded(sample.package.versions.primary);
    }

    private static bool PackageIsNotLoaded(IPackageVersion version)
    {
        return version?.errors?.AnyMatches(i => i.errorCode == UIErrorCode.UpmError_PackageNotLoaded) == true;
    }
}

internal class DisableIfPackageIsInInvalidLocation : IDisableCondition<IPackageVersion>, IDisableCondition<Sample>
{
    private static readonly string k_Tooltip = L10n.Tr("This package is stored in an invalid location.", null);
    private static readonly string k_SampleTooltip = L10n.Tr("The package this sample belongs to is stored in an invalid location.", null);

    public bool IsActive(IPackageVersion version, out string tooltip)
    {
        tooltip = k_Tooltip;
        return PackageIsInInvalidLocation(version);
    }

    public bool IsActive(Sample sample, out string tooltip)
    {
        tooltip = k_SampleTooltip;
        return PackageIsInInvalidLocation(sample.package?.versions?.primary);
    }

    private static bool PackageIsInInvalidLocation(IPackageVersion version)
    {
        var error = version?.errors?.FirstMatch(e => !e.HasAttribute(UIError.Attribute.Clearable | UIError.Attribute.HiddenFromUI));
        return error is { errorCode: UIErrorCode.UpmError_InvalidSourcePath };
    }
}

internal class DisableIfSampleHasNoPath : IDisableCondition<Sample>
{
    private static readonly string k_Tooltip = L10n.Tr("The path property for this sample is missing.", null);

    public bool IsActive(Sample sample, out string tooltip)
    {
        tooltip = k_Tooltip;
        return sample is { isDefault: false, package: not null }
               && string.IsNullOrEmpty(sample.resolvedPath);
    }
}

internal class DisableIfSamplePathDoesNotExist : IDisableCondition<Sample>
{
    private static readonly string k_Tooltip = L10n.Tr("The path specified for this sample doesn't exist.", null);
    private readonly IIOProxy m_IOProxy;
    public DisableIfSamplePathDoesNotExist(IIOProxy ioProxy)
    {
        m_IOProxy = ioProxy;
    }

    public bool IsActive(Sample sample, out string tooltip)
    {
        tooltip = k_Tooltip;
        return sample is { isDefault: false, package: not null }
               && !string.IsNullOrEmpty(sample.resolvedPath)
               && !m_IOProxy.DirectoryExists(sample.resolvedPath);
    }
}

internal class DisableIfPackageDisabled : IDisableCondition<IPackageVersion>
{
    private static readonly string k_Tooltip = L10n.Tr("This package is no longer available.", null);

    public bool IsActive(IPackageVersion version, out string tooltip)
    {
        tooltip = k_Tooltip;
        return version != null && version.HasTag(PackageTag.Disabled);
    }
}
