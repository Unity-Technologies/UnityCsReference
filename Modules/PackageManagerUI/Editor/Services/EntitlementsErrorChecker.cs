// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal static class EntitlementsErrorChecker
    {
        // These strings shouldn't be localized because they're used for string matching with UPM server error messages.
        internal const string k_NoSubscriptionUpmErrorMessage = "You do not have a subscription for this package";
        internal const string k_NotAcquiredUpmErrorMessage = "Your account does not grant permission to use the package";
        internal const string k_NotSignedInUpmErrorMessage = "You are not signed in";

        public static void ManagePackageManagerEntitlementError(UpmClient upmClient)
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
                    var upmClient = ServicesContainer.instance.Resolve<UpmClient>();
                    upmClient.onListOperation -= OnListOperation;

                    // Package Manager Window always seems to default to name ascending order
                    var packages = request.Result;

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
                        PackageManagerWindow.OpenPackageManager(entitlementErrorPackage.name);
                };
            }
        }
    }
}
