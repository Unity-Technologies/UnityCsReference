// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UnityConnectHub not yet converted

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Scripting.LifecycleManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityEditor.Connect
{
    [InitializeOnLoad]
    internal partial class ServicesRepository
    {
        [AutoStaticsCleanupOnCodeReload]
        static ServicesRepository k_Instance;

        public static ServicesRepository instance
        {
            get
            {
                EnsureInstanceInitialized();
                return k_Instance;
            }
        }

        [AutoStaticsCleanupOnCodeReload]
        static readonly Dictionary<string, SingleService> k_Services = new Dictionary<string, SingleService>();

        [AutoStaticsCleanupOnCodeReload]
        static readonly List<string> s_InitializedServices = new List<string>();

        const string k_serviceFlagsKey = "service_flags";

        const string k_NotInitializedMsg = "Service {0} was not initialized. See the console for more details.";

        // [OnCodeLoaded] recreates the repository after a reload so services re-register;
        // the null check in `instance` covers consumers that reach us before it has run
        // (ordering across classes is not guaranteed).
        [OnCodeLoaded]
        static void EnsureInstanceInitialized()
        {
            if (k_Instance == null)
            {
                k_Instance = new ServicesRepository();
            }
        }

        ServicesRepository()
        {
            InitializeServicesHandlers();
        }

        internal static void InitializeServicesHandlers()
        {
            if (s_InitializedServices.Count < k_Services.Count)
            {
                foreach (var serviceKey in k_Services.Keys)
                {
                    if (!s_InitializedServices.Contains(serviceKey))
                    {
                        var service = k_Services[serviceKey];
                        try
                        {
                            service.InitializeServiceEventHandlers();
                            s_InitializedServices.Add(serviceKey);
                        }
                        catch (Exception ex)
                        {
                            NotificationManager.instance.Publish(service.notificationTopic, Notification.Severity.Error, string.Format(L10n.Tr(k_NotInitializedMsg, null), L10n.Tr(service.title, null)));
                            Debug.LogException(ex);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds a new service in the services window list.
        /// Sorting and duplicates are already handled.
        /// Each service has the responsibility to add itself to this list.
        /// This should not be called by a service that isn't registering itself.
        /// </summary>
        /// <param name="singleService">Configuration for the service to add</param>
        public static void AddService(SingleService singleService)
        {
            if (!CanAddService(singleService))
                return;

            k_Services.Add(singleService.name, singleService);
        }


        /// <remarks>General rule: do not double add; if Core package exists, do not add services
        /// Exception: Installed services which do not have a hard dependency to Core</remarks>
        static bool CanAddService(SingleService singleService)
        {
            var isServiceAlreadyAdded = k_Services.ContainsKey(singleService.name);

            var isCorePackageRegistered = PackageHelper.IsCorePackageRegistered();
            var servicePackageRegistered = PackageManager.PackageInfo.IsPackageRegistered(singleService.editorGamePackageName);
            var servicePackageHasCoreDependency = PackageHelper.HasCoreDependency(singleService.editorGamePackageName);

            return !isServiceAlreadyAdded &&
                (!isCorePackageRegistered || (servicePackageRegistered && !servicePackageHasCoreDependency));
        }

        public static List<SingleService> GetServices()
        {
            #pragma warning disable UAC2001 // Avoid Linq
            return k_Services.Values.ToList();
#pragma warning restore UAC2001
        }

        public static void DisableAllServices(bool shouldUpdateApiFlag)
        {
            GetServices().ForEach(service => service.EnableService(false, shouldUpdateApiFlag));
        }

        public static void EnableServicesOnProjectCreation()
        {
            GetServices().ForEach(service =>
            {
                if (service.shouldEnableOnProjectCreation)
                {
                    service.EnableService(true);
                }
            });
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
