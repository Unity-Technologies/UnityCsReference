// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: SceneTooling not yet converted
using System;
using UnityEditor.Connect;
using UnityEditor.PackageManager.UI;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.Toolbars
{
    static class CloudButton
    {
        [NoAutoStaticsCleanup] // Recomputed from MPE.ProcessService.level on every EditorApplication.update tick; safe to persist.
        static bool s_Availability = true;
        const string k_Path = "Services/Cloud";

        static CloudButton()
        {
            s_Availability = MPE.ProcessService.level == MPE.ProcessLevel.Main;
            EditorApplication.update += CheckAvailability;
        }

        static void CheckAvailability()
        {
            var available = MPE.ProcessService.level == MPE.ProcessLevel.Main;
            if (s_Availability != available)
                MainToolbar.Refresh(k_Path);
            s_Availability = available;
        }

        [UnityOnlyMainToolbarPreset]
        [MainToolbarElement(k_Path, defaultDockIndex = 6, defaultDockPosition = MainToolbarDockPosition.Right)]
        static MainToolbarElement CreateButton()
        {
            return new MainToolbarButton(new MainToolbarContent(EditorGUIUtility.LoadIcon("Icons/CloudConnect.png"), L10n.Tr("Manage services", null)), () => OpenServicesDiscoveryWindow(EditorGameServicesAnalytics.SendToolbarCloudEvent))
            {
                displayed = s_Availability
            };
        }

        static void OpenServicesDiscoveryWindow(Action analyticTrackingAction = null)
        {
            analyticTrackingAction?.Invoke();
            PackageManagerWindow.OpenAndSelectPage(ServicesConstants.ExploreServicesPackageManagerPageId);
        }

        [MenuItem("Window/Package Management/Services %0", false, 1502)]
        static void OpenServicesDiscoveryWindowFromMenu()
        {
            OpenServicesDiscoveryWindow(Connect.EditorGameServicesAnalytics.SendTopMenuServicesEvent);
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
