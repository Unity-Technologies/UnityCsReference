// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.QuickInstall
{

    /// <summary>
    /// Configuration for the QuickInstall module, which provides a declarative interface for defining packages that
    /// can be installed via the Editor with minimal setup. This includes specifying the package to install, the
    /// UI surfaces to expose installation options, and analytics tracking for installation events.
    /// </summary>
    internal class QuickInstallConfig
    {
        public string PackageName { get; }
        public string Assembly { get; }
        public MenuConfig MenuConfig { get; }
        public SettingsPageConfig SettingsPageConfig { get; }
        public AnalyticConfig AnalyticConfig { get; }

        /// <summary>
        /// Creates a <see cref="QuickInstallConfig"/>.
        /// </summary>
        /// <param name="packageName">
        /// The unique name of the package as listed by the package manager (e.g., "com.unity.purchasing").
        /// </param>
        /// <param name="assembly">
        /// The assembly name used to detect if the package is already installed. This is checked against loaded
        /// assemblies to determine installation status.
        /// </param>
        /// <param name="menuConfig">
        /// Optional configuration for adding a menu item to trigger package installation. If null, no menu item
        /// will be created.
        /// </param>
        /// <param name="settingsPageConfig">
        /// Optional configuration for the Project Settings page UI. If null, no settings page will be created.
        /// </param>
        /// <param name="analyticConfig">
        /// Optional analytics configuration for tracking installation events. If null, no analytics will be sent.
        /// </param>
        public QuickInstallConfig(
            string packageName,
            string assembly,
            MenuConfig menuConfig = null,
            SettingsPageConfig settingsPageConfig = null,
            AnalyticConfig analyticConfig = null)
        {
            PackageName = packageName;
            Assembly = assembly;
            MenuConfig = menuConfig;
            SettingsPageConfig = settingsPageConfig;
            AnalyticConfig = analyticConfig;
        }
    }

    /// <summary>
    /// Configuration for the Editor menu item that triggers package installation.
    /// </summary>
    internal class MenuConfig
    {
        public string MenuPath { get; }
        public int Priority { get; }

        /// <summary>
        /// Creates a <see cref="MenuConfig"/>.
        /// </summary>
        /// <param name="menuPath">
        /// The menu path where the install menu item should appear (e.g., "Services/In-App Purchasing/Install").
        /// </param>
        /// <param name="priority">
        /// The menu item priority, which determines ordering within the menu. Lower values appear first. Defaults to
        /// int.MaxValue - 100 so that it appears near the bottom of the menu, but provides room for other menu items
        /// to be added above or below as needed.
        /// </param>
        public MenuConfig(string menuPath, int priority = int.MaxValue - 100)
        {
            MenuPath = menuPath;
            Priority = priority;
        }
    }
    
    /// <summary>
    /// Configuration for the Project Settings installation page UI.
    /// </summary>
    internal class SettingsPageConfig
    {
        public string Body { get; }
        public string SettingsPath { get; }
        public string InstallButton { get; }
        public string Installing { get; }
        public bool ShowSeparator { get; }
        public string DocumentationUrl { get; }
        public string Subtitle { get; }

        /// <summary>
        /// Creates a <see cref="SettingsPageConfig"/>.
        /// </summary>
        /// <param name="body">
        /// The main body text displayed on the settings page. Typically used to describe the package and explain why
        /// installation is needed, but can contain any informational content.
        /// </param>
        /// <param name="settingsPath">
        /// The title/path for the settings page in the Project Settings window
        /// (e.g., "Project/Services/In-App Purchasing").
        /// </param>
        /// <param name="installButton">Text displayed on the install button.</param>
        /// <param name="installing">Progress text displayed while the package is being installed.</param>
        /// <param name="showSeparator">
        /// Whether to show a separator line in the UI between the subtitle and the body. If a subtitle isn't provided,
        /// this will have no effect. Defaults to false.
        /// </param>
        /// <param name="documentationUrl">
        /// Optional URL to package documentation. If provided, a documentation button will be shown beneath the body
        /// leading to the specified URL.
        /// </param>
        /// <param name="subtitle">Optional subtitle text to display below the main title.</param>
        public SettingsPageConfig(
            string body,
            string settingsPath,
            string installButton,
            string installing,
            bool showSeparator = false,
            string documentationUrl = null,
            string subtitle = null)
        {
            Body = body;
            SettingsPath = settingsPath;
            InstallButton = installButton;
            Installing = installing;
            ShowSeparator = showSeparator;
            DocumentationUrl = documentationUrl;
            Subtitle = subtitle;
        }
    }

    /// <summary>
    /// Configuration for analytics tracking of package installation events.
    /// </summary>
    internal class AnalyticConfig
    {
        public bool SendAssetInstallAnalytic { get; }

        /// <summary>
        /// Creates an <see cref="AnalyticConfig"/>.
        /// </summary>
        /// <param name="sendAssetInstallAnalytic">
        /// Whether to send analytics when the package is detected as installed via loaded assemblies rather than the
        /// Package Manager (e.g., imported as .unitypackage or dropped into Assets). Defaults to false.
        /// </param>
        public AnalyticConfig(bool sendAssetInstallAnalytic = false)
        {
            SendAssetInstallAnalytic = sendAssetInstallAnalytic;
        }
    }
}
