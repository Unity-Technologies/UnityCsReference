// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor
{
    // Player Settings is where you define various parameters for the final game that you will build in Unity. Some of these values are used in the Resolution Dialog that launches when you open a standalone game.
    public sealed partial class PlayerSettings : UnityEngine.Object
    {
        public enum WSAApplicationShowName
        {
            NotSet = 0,
            AllLogos = 1,
            NoLogos = 2,
            StandardLogoOnly = 3,
            WideLogoOnly = 4,
        }

        public enum WSADefaultTileSize
        {
            NotSet = 0,
            Medium = 1,
            Wide = 2,
        }

        public enum WSAApplicationForegroundText
        {
            //notSet = 0,
            Light = 1,
            Dark = 2,
        }

        public enum WSACompilationOverrides
        {
            None = 0,
            UseNetCore = 1,
            UseNetCorePartially = 2
        }

        // match these with the capabilities listed in MetroCapabilities.h
        public enum WSACapability
        {
            EnterpriseAuthentication = 0,
            InternetClient = 1,
            InternetClientServer = 2,
            MusicLibrary = 3,
            PicturesLibrary = 4,
            PrivateNetworkClientServer = 5,
            RemovableStorage = 6,
            SharedUserCertificates = 7,
            VideosLibrary = 8,
            WebCam = 9,
            Proximity = 10,
            Microphone = 11,
            Location = 12,
            HumanInterfaceDevice = 13,
            AllJoyn = 14,
            BlockedChatMessages = 15,
            Chat = 16,
            CodeGeneration = 17,
            Objects3D = 18,
            PhoneCall = 19,
            UserAccountInformation = 20,
            VoipCall = 21,
            Bluetooth = 22,
            SpatialPerception = 23,
            InputInjectionBrokered = 24,
            Appointments = 25,
            BackgroundMediaPlayback = 26,
            Contacts = 27,
            LowLevelDevices = 28,
            OfflineMapsManagement = 29,
            PhoneCallHistoryPublic = 30,
            PointOfService = 31,
            RecordedCallsFolder = 32,
            RemoteSystem = 33,
            SystemManagement = 34,
            UserDataTasks = 35,
            UserNotificationListener = 36,
        }

        // match these with the capabilities listed in MetroCapabilities.h
        public enum WSATargetFamily
        {
            Desktop = 0,
            Mobile = 1,
            Xbox = 2,
            Holographic = 3,
            Team = 4,
            IoT = 5,
            IoTHeadless = 6,
        }

        public enum WSAImageScale
        {
            _80 = 80,
            _100 = 100,
            _125 = 125,
            _140 = 140,
            _150 = 150,
            _180 = 180,
            _200 = 200,
            _240 = 240,
            _400 = 400,

            Target16 = 16,
            Target24 = 24,
            Target32 = 32,
            Target48 = 48,
            Target256 = 256,
        }

        public enum WSAImageType
        {
            // Generic
            PackageLogo = 1,
            SplashScreenImage = 2,

            // Values of 11-20 used to be Windows Store 8.1 images
            // Values of 21-30 used to be Windows Store 8.1 images

            // UWP
            UWPSquare44x44Logo = 31,
            UWPSquare71x71Logo = 32,
            UWPSquare150x150Logo = 33,
            UWPSquare310x310Logo = 34,
            UWPWide310x150Logo = 35,
        }

        // Keep in sync with WSAInputSource in PlayerSettings.h
        public enum WSAInputSource
        {
            CoreWindow = 0,
            IndependentInputSource = 1,
            SwapChainPanel = 2,
        }

        [RequiredByNativeCode]
        public struct WSASupportedFileType
        {
            public string contentType;
            public string fileType;
        }

        public struct WSAFileTypeAssociations
        {
            public string name;
            public WSASupportedFileType[] supportedFileTypes;
        }

        [NativeHeader("Editor/Mono/PlayerSettingsWSA.bindings.h")]
        [NativeHeader("Runtime/Misc/PlayerSettings.h")]
        [StaticAccessor("PlayerSettingsBindings::WSA", StaticAccessorType.DoubleColon)]
        public sealed partial class WSA
        {
            [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
            [NativeProperty("wsaTransparentSwapchain", TargetType.Field)]
            public static extern bool transparentSwapchain { get; set; }

            public static extern string packageName { get; set; }

            public static extern string packageLogo { get; set; }

            private static extern string GetWSAImage(WSAImageType type, WSAImageScale scale);

            private static extern void SetWSAImage(string image, WSAImageType type, WSAImageScale scale);

            private static extern string packageVersionRaw { get; set; }

            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [Obsolete("PlayerSettings.WSA.commandLineArgsFile is deprecated", error: true)]
            public static string commandLineArgsFile { get { return string.Empty; } set {} }

            [NativeThrows]
            public static extern bool SetCertificate(string path, string password);

            [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
            [NativeProperty("metroCertificatePath", TargetType.Field)]
            public static extern string certificatePath { get; }

            internal static extern string certificatePassword { get; }

            private static string NullIfEmpty(string value)
            {
                return String.IsNullOrEmpty(value) ? null : value;
            }

            public static string certificateSubject
            {
                get { return NullIfEmpty(internalCertificateSubject); }
            }

            [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
            [NativeProperty("metroCertificateSubject", TargetType.Field)]
            private static extern string internalCertificateSubject { get; }

            public static string certificateIssuer
            {
                get { return NullIfEmpty(internalCertificateIssuer); }
            }

            [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
            [NativeProperty("metroCertificateIssuer", TargetType.Field)]
            private static extern string internalCertificateIssuer { get; }

            [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
            [NativeProperty("metroCertificateNotAfter", TargetType.Field)]
            private static extern long certificateNotAfterRaw { get; }

            public static extern string applicationDescription { get; set; }

            public static extern string tileShortName { get; set; }

            [NativeProperty("metroTileShowName", TargetType.Field)]
            public static extern WSAApplicationShowName tileShowName
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;

                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            [NativeProperty("metroMediumTileShowName", TargetType.Field)]
            public static extern bool mediumTileShowName
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;

                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            [NativeProperty("metroLargeTileShowName", TargetType.Field)]
            public static extern bool largeTileShowName
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;

                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            [NativeProperty("metroWideTileShowName", TargetType.Field)]
            public static extern bool wideTileShowName
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;

                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            [NativeProperty("metroDefaultTileSize", TargetType.Field)]
            public static extern WSADefaultTileSize defaultTileSize
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;

                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            [NativeProperty("metroCompilationOverrides", TargetType.Field)]
            public static extern WSACompilationOverrides compilationOverrides
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;

                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            [NativeProperty("metroTileForegroundText", TargetType.Field)]
            public static extern WSAApplicationForegroundText tileForegroundText
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;

                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            [NativeProperty("metroTileBackgroundColor", TargetType.Field)]
            public static extern Color tileBackgroundColor
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;

                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            [Obsolete("PlayerSettings.WSA.enableIndependentInputSource is deprecated. Use PlayerSettings.WSA.inputSource.", false)]
            public static bool enableIndependentInputSource
            {
                get { return inputSource == WSAInputSource.IndependentInputSource; }
                set { inputSource = value ? WSAInputSource.IndependentInputSource : WSAInputSource.CoreWindow; }
            }

            [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
            [NativeProperty("metroInputSource", TargetType.Field)]
            public static extern WSAInputSource inputSource { get; set; }

            [NativeProperty("metroSupportStreamingInstall", TargetType.Field)]
            public static extern bool supportStreamingInstall
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;

                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            [NativeProperty("metroLastRequiredScene", TargetType.Field)]
            public static extern int lastRequiredScene
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;

                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            [NativeProperty("metroSplashScreenUseBackgroundColor", TargetType.Field)]
            private static extern bool splashScreenUseBackgroundColor
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;

                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            [NativeProperty("metroSplashScreenBackgroundColor", TargetType.Field)]
            private static extern Color splashScreenBackgroundColorRaw
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;

                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            private static extern void InternalSetCapability(string name, string value);

            private static extern string InternalGetCapability(string name);

            private static extern void InternalSetTargetDeviceFamily(string name, string value);

            private static extern string InternalGetTargetDeviceFamily(string name);

            // Workaround for Case 756100 - properties (and probably functions) in nested+nested class are not correctly bound, causing MissingMethodException when calling them from C#
            internal static extern string internalProtocolName { get; set; }

            internal static WSAFileTypeAssociations internalFileTypeAssociations
            {
                get
                {
                    return new WSAFileTypeAssociations { name = metroFTAName, supportedFileTypes = metroFTAFileTypes };
                }

                set
                {
                    metroFTAName = value.name;
                    metroFTAFileTypes = value.supportedFileTypes;
                }
            }

            private static extern string metroFTAName { get; set; }

            [NativeProperty("metroFTAFileTypes", TargetType.Field)]
            private static extern WSASupportedFileType[] metroFTAFileTypes
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;

                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            public static class Declarations
            {
                public static string protocolName
                {
                    get
                    {
                        return PlayerSettings.WSA.internalProtocolName;
                    }
                    set
                    {
                        PlayerSettings.WSA.internalProtocolName = value;
                    }
                }

                public static WSAFileTypeAssociations fileTypeAssociations
                {
                    get
                    {
                        return PlayerSettings.WSA.internalFileTypeAssociations;
                    }
                    set
                    {
                        PlayerSettings.WSA.internalFileTypeAssociations = value;
                    }
                }
            }
        }
    }
}
