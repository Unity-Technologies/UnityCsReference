// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageAddButton : PackageToolBarRegularButton
    {
        public static readonly string k_InstallButtonText = L10n.Tr("Install");
        public static readonly string k_InstallingButtonText = L10n.Tr("Installing");

        public static readonly string k_EnableButtonText = L10n.Tr("Enable");
        public static readonly string k_EnablingButtonText = L10n.Tr("Enabling");

        private ApplicationProxy m_Application;
        private PackageDatabase m_PackageDatabase;
        private PackageOperationDispatcher m_OperationDispatcher;
        public PackageAddButton(ApplicationProxy applicationProxy,
                                PackageDatabase packageDatabase,
                                PackageOperationDispatcher operationDispatcher)
        {
            m_Application = applicationProxy;
            m_PackageDatabase = packageDatabase;
            m_OperationDispatcher = operationDispatcher;
        }

        protected override bool TriggerAction(IList<IPackageVersion> versions)
        {
            m_OperationDispatcher.Install(versions);
            // The current multi-select UI does not allow users to install non-recommended versions
            // Should this change in the future, we'll need to update the analytics event accordingly.
            PackageManagerWindowAnalytics.SendEvent("installNewRecommended", packageIds: versions.Select(v => v.uniqueId));
            return true;
        }

        protected override bool TriggerAction(IPackageVersion version)
        {
            IPackage[] packageToUninstall = null;
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
                        packageToUninstall = customizedDependencies;
                }
            }

            if (packageToUninstall?.Any() == true)
            {
                m_OperationDispatcher.InstallAndResetDependencies(version, packageToUninstall);
                PackageManagerWindowAnalytics.SendEvent("installAndReset", version.uniqueId);
            }
            else
            {
                var installRecommended = version.package.versions.recommended == version ? "Recommended" : "NonRecommended";
                var eventName = $"installNew{installRecommended}";

                if (version.package.isDeprecated && !m_Application.DisplayDialog("installDeprecatedPackage", L10n.Tr("Deprecated package installation"), L10n.Tr("Are you sure you want to install this deprecated package?"), L10n.Tr("Install"), L10n.Tr("Cancel")))
                    return false;

                m_OperationDispatcher.Install(version);

                PackageManagerWindowAnalytics.SendEvent(eventName, version.uniqueId);
            }
            return true;
        }

        protected override bool IsVisible(IPackageVersion version)
        {
            return version != null
                && version.package.versions.installed == null
                && !version.HasTag(PackageTag.Placeholder)
                && version.HasTag(PackageTag.UpmFormat);
        }

        protected override string GetTooltip(IPackageVersion version, bool isInProgress)
        {
            if (isInProgress)
                return k_InProgressGenericTooltip;

            if (version?.HasTag(PackageTag.BuiltIn) == true)
                return string.Format(L10n.Tr("Click to enable this {0} in your project."), version.GetDescriptor());

            return string.Format(L10n.Tr("Click to install this {0} into your project."), version.GetDescriptor());
        }

        protected override string GetText(IPackageVersion version, bool isInProgress)
        {
            if (version?.HasTag(PackageTag.BuiltIn) == true)
                return isInProgress ? k_EnablingButtonText : k_EnableButtonText;

            return isInProgress ? k_InstallingButtonText : k_InstallButtonText;
        }

        protected override bool IsInProgress(IPackageVersion version) => m_OperationDispatcher.IsInstallInProgress(version);

        protected override IEnumerable<ButtonDisableCondition> GetDisableConditions(IPackageVersion version)
        {
            yield return new ButtonDisableCondition(() => version != null && version.HasTag(PackageTag.Deprecated) && version.availableRegistry != RegistryType.MyRegistries,
                L10n.Tr("This version is deprecated."));

            yield return new ButtonDisableCondition(() => version?.package.hasEntitlementsError ?? false,
                L10n.Tr("You need to sign in with a licensed account to perform this action."));
        }
    }
}
