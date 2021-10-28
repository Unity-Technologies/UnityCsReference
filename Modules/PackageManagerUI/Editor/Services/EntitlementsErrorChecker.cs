// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{

    internal static class EntitlementsErrorChecker
    {
        public static void ManagePackageManagerEntitlementError(UpmClient upmClient)
        {
            upmClient.onListOperation += OnListOperation;
            upmClient.List(true);
        }

        private static void OnListOperation(IOperation operation)
        {
            var listOperation = operation as UpmListOperation;
            if (listOperation.isOfflineMode)
            {
                listOperation.onProcessResult += (request) =>
                {
                    var upmClient = ServicesContainer.instance.Resolve<UpmClient>();
                    upmClient.onListOperation -= OnListOperation;

                    // Package Manager Window always seems to default to name ascending order
                    var packages = request.Result;

                    var entitlementErrorPackage = packages.Where(p => p.entitlements?.isAllowed == false
                        || p.errors.Any(error => error.message.Contains("You do not have a subscription for this package")))
                        .OrderBy(p => p.displayName ?? p.name)
                        .FirstOrDefault();

                    if (entitlementErrorPackage != null)
                    {
                        PackageManagerWindow.OpenPackageManager(entitlementErrorPackage.name);
                    }
                };
            }
        }
    }
}
