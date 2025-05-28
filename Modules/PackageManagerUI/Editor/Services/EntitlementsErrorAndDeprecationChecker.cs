// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal static class EntitlementsErrorAndDeprecationChecker
    {
        // These strings shouldn't be localized because they're used for string matching with UPM server error messages.
        internal const string k_NoSubscriptionUpmErrorMessage = "You do not have a subscription for this package";
        internal const string k_NotAcquiredUpmErrorMessage = "Your account does not grant permission to use the package";
        internal const string k_NotSignedInUpmErrorMessage = "You are not signed in";

        public static void ManagePackageManagerEntitlementErrorAndDeprecation(IUpmClient upmClient)
        {
            upmClient.onListOperation += OnListOperation;
            upmClient.List(true);
        }

        private static void OnListOperation(IOperation operation)
        {
            var listOperation = operation as UpmListOperation;
            if (listOperation?.isOfflineMode == true)
            {
                listOperation.onProcessResult += (request) =>
                {
                    var servicesContainer = ServicesContainer.instance;
                    var upmClient = servicesContainer.Resolve<IUpmClient>();
                    upmClient.onListOperation -= OnListOperation;

                    // Package Manager Window always seems to default to name ascending order
                    var packages = request.Result;

                    if (FindEntitlementErrorAndOpenPackageManager(packages))
                        return;
                    FindDeprecationPackagesAndOpenPackageManager(packages);
                };
            }
        }

        // Returns true if packages with entitlement errors are found and package manager window is opened
        private static bool FindEntitlementErrorAndOpenPackageManager(PackageCollection packages)
        {
            var entitlementErrorPackage = packages.Where(p => p.entitlements?.isAllowed == false
                       || p.errors.Any(error =>
                           error.message.Contains(k_NoSubscriptionUpmErrorMessage) ||
                           // The following two string matching is used to check for Asset Store
                           // entitlement errors because upm-core only exposes them via strings.
                           error.message.Contains(k_NotAcquiredUpmErrorMessage) ||
                           error.message.Contains(k_NotSignedInUpmErrorMessage)))
                       .OrderBy(p => p.displayName ?? p.name)
                       .FirstOrDefault();

            if (entitlementErrorPackage != null)
            {
                PackageManagerWindow.OpenAndSelectPackage(entitlementErrorPackage.name);
                return true;
            }

            return false;
        }

        // Returns true if deprecated packages are found and package manager window is opened
        private static bool FindDeprecationPackagesAndOpenPackageManager(PackageCollection packages)
        {
            var servicesContainer = ServicesContainer.instance;
            var settingsProxy = servicesContainer.Resolve<IProjectSettingsProxy>();
            if (settingsProxy.oneTimeDeprecatedPopUpShown)
                return false;

            var deprecatedPackage = packages.Where(p => (p.versions.deprecated.Contains(p.version))
                        || p.unityLifecycle.isDeprecated)
                        .OrderBy(p => p.displayName ?? p.name)
                        .FirstOrDefault();
            if (deprecatedPackage == null)
                return false;

            var applicationProxy = servicesContainer.Resolve<IApplicationProxy>();
            var dialogResult = applicationProxy.DisplayDialogComplex("openPackageManagerWithDeprecatedPackages",
                L10n.Tr("Deprecated packages"),
                L10n.Tr("This project contains one or more deprecated packages. Do you want to open Package Manager?"),
                L10n.Tr("Open Package Manager"),
                L10n.Tr("Dismiss"),
                L10n.Tr("Dismiss Forever"));
            switch (dialogResult)
            {
                case 0:
                    PackageManagerWindow.OpenAndSelectPackage(deprecatedPackage.name);
                    return true;
                // Dismiss Forever
                case 2:
                    settingsProxy.oneTimeDeprecatedPopUpShown = true;
                    settingsProxy.Save();
                    break;
            }
            return false;
        }
    }
}
