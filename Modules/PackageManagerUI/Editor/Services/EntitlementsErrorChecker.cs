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

                    instance.m_IsEditorStartUp = false;
                };
            }
        }
    }
}
