// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal;

internal class AddAction : PackageAction
{
    private static readonly string k_InstallButtonText = L10n.Tr("Install");
    private static readonly string k_InstallingButtonText = L10n.Tr("Installing");

    private static readonly string k_EnableButtonText = L10n.Tr("Enable");
    private static readonly string k_EnablingButtonText = L10n.Tr("Enabling");

    private readonly IPackageOperationDispatcher m_OperationDispatcher;
    private readonly IApplicationProxy m_Application;
    private readonly IPackageDatabase m_PackageDatabase;
    public AddAction(IPackageOperationDispatcher operationDispatcher, IApplicationProxy applicationProxy, IPackageDatabase packageDatabase)
    {
        m_OperationDispatcher = operationDispatcher;
        m_Application = applicationProxy;
        m_PackageDatabase = packageDatabase;
    }

    protected override bool TriggerActionImplementation(IList<IPackage> packages)
    {
        var primaryVersions = packages.Select(p => p.versions.primary).ToArray();
        m_OperationDispatcher.Install(primaryVersions);
        // The current multi-select UI does not allow users to install non-recommended versions
        // Should this change in the future, we'll need to update the analytics event accordingly.
        PackageManagerWindowAnalytics.SendEvent("installNewRecommended", primaryVersions);
        return true;
    }

    protected override bool TriggerActionImplementation(IPackageVersion version)
    {
        IPackage[] packagesToUninstall = null;
        if (version.HasTag(PackageTag.Feature))
        {
            var customizedDependencies = m_PackageDatabase.GetCustomizedDependencies(version, true);
            if (customizedDependencies.Any())
            {
                var packageNameAndVersions = string.Join("\n\u2022 ",
                    customizedDependencies.Select(package => $"{package.displayName} - {package.versions.lifecycleVersion.version}").ToArray());

                var title = string.Format(L10n.Tr("Installing {0}"), version.GetDescriptor());
                var message = customizedDependencies.Length == 1 ?
                    string.Format(
                        L10n.Tr("This {0} includes a package version that is different from what's already installed. Would you like to reset the following package to the required version?\n\u2022 {1}"),
                        version.GetDescriptor(), packageNameAndVersions) :
                    string.Format(
                        L10n.Tr("This {0} includes package versions that are different from what are already installed. Would you like to reset the following packages to the required versions?\n\u2022 {1}"),
                        version.GetDescriptor(), packageNameAndVersions);

                var result = m_Application.DisplayDialogComplex("installPackageWithCustomizedDependencies", title, message, L10n.Tr("Install and Reset"), L10n.Tr("Cancel"), L10n.Tr("Install Only"));
                if (result == 1) // Cancel
                    return false;
                if (result == 0) // Install and reset
                    packagesToUninstall = customizedDependencies;
            }
        }

        if (packagesToUninstall?.Any() == true)
        {
            m_OperationDispatcher.InstallAndResetDependencies(version, packagesToUninstall);
            PackageManagerWindowAnalytics.SendEvent("installAndReset", version);
        }
        else
        {
            var installRecommended = version.package.versions.recommended == version ? "Recommended" : "NonRecommended";
            var eventName = $"installNew{installRecommended}";

            if (version.package.isDeprecated && !m_Application.DisplayDialog("installDeprecatedPackage", L10n.Tr("Deprecated package installation"), L10n.Tr("Are you sure you want to install this deprecated package?"), L10n.Tr("Install"), L10n.Tr("Cancel")))
                return false;

            m_OperationDispatcher.Install(version);

            PackageManagerWindowAnalytics.SendEvent(eventName, version);
        }
        return true;
    }

    public override bool IsVisible(IPackageVersion version)
    {
        return version != null
               && version.package.versions.installed == null
               && !version.HasTag(PackageTag.Placeholder)
               && version.HasTag(PackageTag.UpmFormat);
    }

    public override string GetTooltip(IPackageVersion version, bool isInProgress)
    {
        if (isInProgress)
            return k_InProgressGenericTooltip;

        if (version?.HasTag(PackageTag.BuiltIn) == true)
            return string.Format(L10n.Tr("Click to enable this {0} in your project."), version.GetDescriptor());

        return string.Format(L10n.Tr("Click to install this {0} into your project."), version.GetDescriptor());
    }

    public override string GetText(IPackageVersion version, bool isInProgress)
    {
        if (version?.HasTag(PackageTag.BuiltIn) == true)
            return isInProgress ? k_EnablingButtonText : k_EnableButtonText;

        return isInProgress ? k_InstallingButtonText : k_InstallButtonText;
    }

    public override bool IsInProgress(IPackageVersion version) => m_OperationDispatcher.IsInstallInProgress(version);

    protected override IEnumerable<DisableCondition> GetAllTemporaryDisableConditions()
    {
        yield return new DisableIfInstallOrUninstallInProgress(m_OperationDispatcher);
        yield return new DisableIfCompiling(m_Application);
    }

    protected override IEnumerable<DisableCondition> GetAllDisableConditions(IPackageVersion version)
    {
        yield return new DisableIfVersionDeprecated(version);
        yield return new DisableIfEntitlementsError(version);
    }
}
