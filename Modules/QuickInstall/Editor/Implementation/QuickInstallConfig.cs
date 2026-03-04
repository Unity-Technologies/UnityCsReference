// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.QuickInstall
{
    internal class QuickInstallConfig
    {
        public string packageId { get; set; }
        public string alternateInstallAssembly { get; set; }
        public SettingsProviderConfig settingsProviderConfig { get; set; }
        public string menuPath { get; set; }
        public QuickInstallAnalytic analytic { get; set; }
    }
    
    internal class SettingsProviderConfig
    {
        public string installationHelpText { get; set; }
        public string settingsRootTitle { get; set; }
        public Uri documentationUri { get; set; }
        public string installButtonText { get; set; }
        public string downloadingText { get; set; }
        public string installingText { get; set; }
        public string subtitle { get; set; }
        public bool showSubtitle { get; set; }
        public bool showDocumentationButton { get; set; }
        public bool showSeparator { get; set; }
    }
}
