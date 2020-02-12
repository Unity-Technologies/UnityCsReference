// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace UnityEditor.Connect
{
    internal static class ServicesUtils
    {
        internal static void TranslateStringsInTree(VisualElement rootElement)
        {
            rootElement.Query<TextElement>().ForEach((label) => label.text = L10n.Tr(label.text));
        }

        internal static void CapitalizeStringsInTree(VisualElement rootElement)
        {
            rootElement.Query<TextElement>().ForEach((label) => label.text = label.text.ToUpper());
        }

        static readonly string k_PlatformSupportUXMLPath = "UXML/ServicesWindow/PlatformSupportVisual.uxml";
        static readonly string k_PlatformSupportCommonUssPath = "StyleSheets/ServicesWindow/PlatformSupportVisualCommon.uss";
        static readonly string k_PlatformSupportDarkUssPath = "StyleSheets/ServicesWindow/PlatformSupportVisualDark.uss";
        static readonly string k_PlatformSupportLightUssPath = "StyleSheets/ServicesWindow/PlatformSupportVisualLight.uss";


        // Available platforms
        static readonly string k_PlatformAndroid = "Android";
        static readonly string k_PlatformIOS = "iOS";
        static readonly string k_PlatformLinux = "Linux";
        static readonly string k_PlatformMac = "Mac";
        static readonly string k_PlatformPC = "PC";
        static readonly string k_PlatformPS4 = "PS4";
        static readonly string k_PlatformTizenStore = "Tizen Store";
        static readonly string k_PlatformWebGL = "WebGL";
        static readonly string k_PlatformWebPlayer = "WebPlayer";
        static readonly string k_PlatformWindows8Universal = "Windows 8 Universal";
        static readonly string k_PlatformWindows8_1Universal = "Windows 8.1 Universal";
        static readonly string k_PlatformWindows10Universal = "Windows 10 Universal";
        static readonly string k_PlatformXboxOne = "Xbox One";

        static readonly string[] k_MultiplayerSupportedPlatforms =
        {
            k_PlatformAndroid,
            k_PlatformIOS,
            k_PlatformLinux,
            k_PlatformMac,
            k_PlatformPC,
            k_PlatformPS4,
            k_PlatformWebPlayer,
            k_PlatformXboxOne,
        };
        static readonly string[] k_CloudDiagCrashSupportedPlatforms =
        {
            k_PlatformAndroid,
            k_PlatformIOS,
            k_PlatformLinux,
            k_PlatformMac,
            k_PlatformPC,
            k_PlatformWebGL,
            k_PlatformWindows8Universal,
            k_PlatformWindows10Universal,
        };
        static readonly string[] k_AnalyticsSupportedPlatforms =
        {
            k_PlatformAndroid,
            k_PlatformIOS,
            k_PlatformLinux,
            k_PlatformMac,
            k_PlatformPC,
            k_PlatformTizenStore,
            k_PlatformWebGL,
            k_PlatformWindows8_1Universal,
            k_PlatformWindows10Universal,
        };
        static readonly string[] k_AdsSupportedPlatforms =
        {
            k_PlatformAndroid,
            k_PlatformIOS,
        };

        internal static class ServiceWindowErrorStrings
        {
            internal static readonly string initializationError = "Unity Connect is unable to initialize.";
        }

        internal static class StylesheetPath
        {
            internal static readonly string servicesCommon = "StyleSheets/ServicesWindow/ServicesProjectSettingsCommon.uss";
            internal static readonly string servicesDark = "StyleSheets/ServicesWindow/ServicesProjectSettingsDark.uss";
            internal static readonly string servicesLight = "StyleSheets/ServicesWindow/ServicesProjectSettingsLight.uss";
            internal static readonly string servicesWindowCommon = "StyleSheets/ServicesWindow/ServicesWindowCommon.uss";
            internal static readonly string servicesWindowDark = "StyleSheets/ServicesWindow/ServicesWindowDark.uss";
            internal static readonly string servicesWindowLight = "StyleSheets/ServicesWindow/ServicesWindowLight.uss";
        }

        internal static class UssStrings
        {
            internal static readonly string scrollContainer = "scroll-container";
            internal static readonly string extLinkIcon = "external-link-icon";
            internal static readonly string editButton = "edit-button";
            internal static readonly string cancelButton = "cancel-button";
            internal static readonly string fieldValue = "field-value";
            internal static readonly string readMode = "read-mode";
            internal static readonly string editMode = "edit-mode";

            internal static readonly string tagContainer = "tag-container";
            internal static readonly string platformTag = "platform-tag";
        }

        internal static class UxmlStrings
        {
            internal static readonly string createProjectIdBlock = "CreateProjectIdBlock";
            internal static readonly string supportedPlatformsBlock = "SupportedPlatformsBlock";
            internal static readonly string configOverview = "ConfigurationOverview";
            internal static readonly string platformsSupported = "PlatformsSupported";
        }

        public static List<string> GetCloudDiagCrashSupportedPlatforms()
        {
            return k_CloudDiagCrashSupportedPlatforms.ToList();
        }

        public static List<string> GetCloudDiagUserReportSupportedPlatforms()
        {
            return k_CloudDiagCrashSupportedPlatforms.ToList();
        }

        public static List<string> GetAnalyticsSupportedPlatforms()
        {
            return k_AnalyticsSupportedPlatforms.ToList();
        }

        public static List<string> GetMultiplayerSupportedPlatforms()
        {
            return k_MultiplayerSupportedPlatforms.ToList();
        }

        public static List<string> GetAdsSupportedPlatforms()
        {
            return k_AdsSupportedPlatforms.ToList();
        }

        public static VisualElement SetupSupportedPlatformsBlock(List<string> platforms)
        {
            var supportedPlatformsBlock = EditorGUIUtility.Load(k_PlatformSupportUXMLPath) as VisualTreeAsset;

            if (supportedPlatformsBlock == null)
            {
                return null;
            }
            var newVisual = supportedPlatformsBlock.CloneTree().contentContainer;
            TranslateStringsInTree(newVisual);
            newVisual.AddStyleSheetPath(k_PlatformSupportCommonUssPath);
            newVisual.AddStyleSheetPath(EditorGUIUtility.isProSkin ? k_PlatformSupportDarkUssPath : k_PlatformSupportLightUssPath);

            var tagContainer = newVisual.Q(className: UssStrings.tagContainer);
            tagContainer.Clear();

            foreach (var platform in platforms)
            {
                var tag = new Label(platform);
                tag.AddToClassList(UssStrings.platformTag);
                tagContainer.Add(tag);
            }

            return newVisual;
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static bool IsUnityWebRequestReadyForJsonExtract(UnityWebRequest unityWebRequest)
        {
            return (unityWebRequest != null &&
                unityWebRequest.result != UnityWebRequest.Result.ConnectionError &&
                unityWebRequest.result != UnityWebRequest.Result.ProtocolError &&
                !string.IsNullOrEmpty(unityWebRequest.downloadHandler.text));
        }

        public static void OpenServicesProjectSettings(SingleService singleService)
        {
            OpenServicesProjectSettings(singleService.projectSettingsPath, singleService.settingsProviderClassName);
        }

        public static void OpenServicesProjectSettings(string servicesProjectSettingsPath, string projectSettingsClassName)
        {
            var currentProvider = ((ProjectSettingsWindow)SettingsService.OpenProjectSettings()).GetCurrentProvider();
            if (currentProvider == null || !currentProvider.GetType().Name.Equals(projectSettingsClassName))
            {
                SettingsService.OpenProjectSettings(servicesProjectSettingsPath);
            }
        }
    }
}
