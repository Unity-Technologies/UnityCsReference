// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal sealed class EntitlementsErrorChecker : ScriptableSingleton<EntitlementsErrorChecker>
    {
        // These strings shouldn't be localized because they're used for string matching with UPM server error messages.
        internal const string k_NoSubscriptionUpmErrorMessage = "You do not have a subscription for this package";
        internal const string k_NotAcquiredUpmErrorMessage = "Your account does not grant permission to use the package";
        internal const string k_NotSignedInUpmErrorMessage = "You are not signed in";

        public EntitlementsErrorChecker()
        {
            m_IsEditorStartUp = true;
        }

        [SerializeField]
        private bool m_IsEditorStartUp;

        [InitializeOnLoadMethod]
        private static void OpenFirstEntitlementError()
        {
            if (!instance.m_IsEditorStartUp)
                return;

            var servicesContainer = ServicesContainer.instance;
            var applicationProxy = servicesContainer.Resolve<ApplicationProxy>();

            if (!applicationProxy.isBatchMode && applicationProxy.isUpmRunning)
            {
                var upmClient = servicesContainer.Resolve<UpmClient>();

                upmClient.onListOperation += OnListOperation;
                upmClient.List(true);
            }
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

                    instance.m_IsEditorStartUp = false;
                };
            }
        }
    }
}
