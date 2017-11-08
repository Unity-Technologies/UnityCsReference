// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;

using UnityEngine;

namespace UnityEditor
{


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
        
        Light = 1,
        Dark = 2,
    }

    public enum WSACompilationOverrides    
    {
        None = 0,
        UseNetCore = 1,
        UseNetCorePartially = 2
    }

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
        InputInjectionBrokered = 24
    }

    public enum WSAImageScale    
    {
        _80  = 80,
        _100 = 100,
        _125 = 125,
        _140 = 140,
        _150 = 150,
        _180 = 180,
        _200 = 200,
        _240 = 240,
        _400 = 400,
        Target16  = 16,
        Target24  = 24,
        Target32  = 32,
        Target48  = 48,
        Target256 = 256,
    }

    public enum WSAImageType    
    {
        
        PackageLogo = 1,
        SplashScreenImage = 2,
        
        
        
        UWPSquare44x44Logo = 31,
        UWPSquare71x71Logo = 32,
        UWPSquare150x150Logo = 33,
        UWPSquare310x310Logo = 34,
        UWPWide310x150Logo = 35,
    }

    public enum WSAInputSource    
    {
        CoreWindow = 0,
        IndependentInputSource = 1,
        SwapChainPanel = 2,
    }

    [RequiredByNativeCode]
    [System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct WSASupportedFileType
    {
        public string contentType;
        public string fileType;
    }

    [RequiredByNativeCode]
    [System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct WSAFileTypeAssociations
    {
        public string name;
        public WSASupportedFileType[] supportedFileTypes;
    }

    public sealed partial class WSA    
    {
        public extern static bool transparentSwapchain
        {
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            get;
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            set;
        }

        public extern static string packageName
        {
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            get;
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            set;
        }

        public extern static string packageLogo
        {
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            get;
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            set;
        }

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  string GetWSAImage (WSAImageType type, WSAImageScale scale) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetWSAImage (string image, WSAImageType type, WSAImageScale scale) ;

        private extern static string packageVersionRaw
        {
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            get;
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            set;
        }

        public extern static string commandLineArgsFile
        {
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            get;
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            set;
        }

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern public static  bool SetCertificate (string path, string password) ;

        public extern static string certificatePath
        {
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            get;
        }

        internal extern static string certificatePassword
        {
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            get;
        }

        public extern static string certificateSubject
        {
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            get;
        }

        public extern static string certificateIssuer
        {
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            get;
        }

        private extern static long certificateNotAfterRaw
        {
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            get;
        }

        public extern static string applicationDescription
        {
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            get;
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            set;
        }

        public extern static string tileShortName
        {
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            get;
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            set;
        }

        public extern static WSAApplicationShowName tileShowName
        {
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            get;
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            set;
        }

        public extern static bool mediumTileShowName
        {
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            get;
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            set;
        }

        public extern static bool largeTileShowName
        {
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            get;
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            set;
        }

        public extern static bool wideTileShowName
        {
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            get;
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            set;
        }

        public extern static WSADefaultTileSize defaultTileSize
        {
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            get;
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            set;
        }

        public extern static WSACompilationOverrides compilationOverrides
        {
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            get;
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            set;
        }

        public extern static WSAApplicationForegroundText tileForegroundText
        {
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            get;
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            set;
        }

        public static Color tileBackgroundColor
        {
            get { Color tmp; INTERNAL_get_tileBackgroundColor(out tmp); return tmp;  }
            set { INTERNAL_set_tileBackgroundColor(ref value); }
        }

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static void INTERNAL_get_tileBackgroundColor (out Color value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static void INTERNAL_set_tileBackgroundColor (ref Color value) ;

        [Obsolete("PlayerSettings.WSA.enableIndependentInputSource is deprecated. Use PlayerSettings.WSA.inputSource.", false)]
        public static bool enableIndependentInputSource
            {
                get { return inputSource == WSAInputSource.IndependentInputSource; }
                set { inputSource = value ? WSAInputSource.IndependentInputSource : WSAInputSource.CoreWindow; }
            }
        
        
        public extern static WSAInputSource inputSource
        {
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            get;
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            set;
        }

        private extern static bool splashScreenUseBackgroundColor
        {
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            get;
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            set;
        }

        private static Color splashScreenBackgroundColorRaw
        {
            get { Color tmp; INTERNAL_get_splashScreenBackgroundColorRaw(out tmp); return tmp;  }
            set { INTERNAL_set_splashScreenBackgroundColorRaw(ref value); }
        }

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static void INTERNAL_get_splashScreenBackgroundColorRaw (out Color value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static void INTERNAL_set_splashScreenBackgroundColorRaw (ref Color value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void InternalSetCapability (string name, string value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  string InternalGetCapability (string name) ;

        internal extern static string internalProtocolName
        {
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            get;
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            set;
        }

        internal static WSAFileTypeAssociations internalFileTypeAssociations
        {
            get { WSAFileTypeAssociations tmp; INTERNAL_get_internalFileTypeAssociations(out tmp); return tmp;  }
            set { INTERNAL_set_internalFileTypeAssociations(ref value); }
        }

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static void INTERNAL_get_internalFileTypeAssociations (out WSAFileTypeAssociations value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static void INTERNAL_set_internalFileTypeAssociations (ref WSAFileTypeAssociations value) ;

public static partial class Declarations        
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
