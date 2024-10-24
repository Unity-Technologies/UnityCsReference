// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Connect;
using UnityEditor.PackageManager.UI;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    [EditorToolbarElement("Services/Cloud", typeof(DefaultMainToolbar))]
    sealed class CloudButton : EditorToolbarButton
    {
        public CloudButton() : base(OpenCloudWindow)
        {
            name = "Cloud";
            tooltip = L10n.Tr("Manage services");

            this.Q<Image>(className: EditorToolbar.elementIconClassName).style.display = DisplayStyle.Flex;

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            EditorApplication.update += CheckAvailability;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            EditorApplication.update -= CheckAvailability;
        }

        void CheckAvailability()
        {
            style.display = MPE.ProcessService.level == MPE.ProcessLevel.Main ? DisplayStyle.Flex : DisplayStyle.None;
        }

        [MenuItem("Window/General/Services %0", false, 302)]
        static void OpenServicesDiscoveryWindowFromMenu()
        {
            OpenServicesDiscoveryWindow(Connect.EditorGameServicesAnalytics.SendTopMenuServicesEvent);
        }

        static void OpenCloudWindow()
        {
            OpenServicesDiscoveryWindow(Connect.EditorGameServicesAnalytics.SendToolbarCloudEvent);
        }

        static void OpenServicesDiscoveryWindow(Action analyticTrackingAction = null)
        {
            analyticTrackingAction?.Invoke();
            PackageManagerWindow.OpenAndSelectPage(ServicesConstants.ExploreServicesPackageManagerPageId);
        }
    }
}
