// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.XR
{
    [UnityEngine.Internal.ExcludeFromDocs]
    class XRManagementInstaller : SettingsProvider
    {
        const string s_ManagementPackageId = "com.unity.xr.foundation";
        static readonly string s_ManagementPackagePath = $"Packages/{s_ManagementPackageId}/package.json";

        private PackageManager.Requests.AddRequest m_AddManagementRequest = null;

        static class Content
        {
            internal const string s_InstallationHelpText =
                "Install the XR Foundation package to enable Unity to build your app for XR devices.\n"
                + "Refer to <a href=\"https://docs.unity.com/en-us/engine/latest/manual/xr/support/support-packages\">XR packages</a> (Unity Engine documentation) for a full list of supported XR plug-ins.";

            internal const string s_SettingsRootTitle = "Project/XR Plug-in Management";
            internal static readonly string s_AddXrManagement = L10n.Tr("Install XR Foundation", null);
            internal static readonly string s_DownloadingText = L10n.Tr("Downloading XR Foundation...", null);
            internal static readonly string s_InstallingText = L10n.Tr("Installing XR Foundation...", null);
        }

        [UnityEngine.Internal.ExcludeFromDocs]
        XRManagementInstaller(string path, SettingsScope scopes = SettingsScope.Project) : base(path, scopes)
        {
        }

        [UnityEngine.Internal.ExcludeFromDocs]
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            var container = new VisualElement();
            container.style.paddingLeft = 10;
            container.style.paddingRight = 10;
            container.style.paddingTop = 10;

            var helpBox = new HelpBox(Content.s_InstallationHelpText, HelpBoxMessageType.Info);

            var installButton = new Button { text = Content.s_AddXrManagement };
            var statusLabel = new Label();

            void UpdateState()
            {
                var requested = m_AddManagementRequest != null;
                helpBox.style.display = requested ? DisplayStyle.None : DisplayStyle.Flex;
                installButton.style.display = requested ? DisplayStyle.None : DisplayStyle.Flex;
                statusLabel.style.display = requested ? DisplayStyle.Flex : DisplayStyle.None;
                if (requested)
                    statusLabel.text = m_AddManagementRequest.IsCompleted ? Content.s_InstallingText : Content.s_DownloadingText;
            }

            installButton.clicked += () =>
            {
                m_AddManagementRequest = PackageManager.Client.Add(s_ManagementPackageId);
                UpdateState();
            };

            // Unlike OnGUI, the panel does not redraw on its own, so poll the request for status changes.
            container.schedule.Execute(UpdateState).Every(100);
            UpdateState();

            container.Add(helpBox);
            container.Add(installButton);
            container.Add(statusLabel);
            rootElement.Add(container);
        }

        [SettingsProvider]
        static SettingsProvider CreateXRManagementInstallerProvider()
        {
            try
            {
                var packagePath = Path.GetFullPath(s_ManagementPackagePath);
                if (String.IsNullOrEmpty(packagePath) || !File.Exists(packagePath) || String.Compare(s_ManagementPackagePath, packagePath, true) == 0)
                    return new XRManagementInstaller(Content.s_SettingsRootTitle);
            }
            catch (Exception)
            {
                // DO NOTHING...
            }

            return null;
        }
    }
}
