// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Connect.Fallback
{
    static class VisualElementConstants
    {
        public static class UxmlPaths
        {
            public const string GeneralServicesTemplate = "UXML/ServicesWindow/GeneralProjectSettings.uxml";
            public const string InstallPackageTemplate = "UXML/ServicesWindow/PackageInstallation.uxml";
        }

        public static class StyleSheetPaths
        {
            public const string PackageInstallation = "StyleSheets/ServicesWindow/PackageInstallation.uss";
        }

        public static class ClassNames
        {
            public const string ServiceTitle = "service-title";
            public const string ScrollContainer = "scroll-container";
            public const string InstallButton = "install-button";
            public const string InstallMessage = "install-message";
        }
    }
}
