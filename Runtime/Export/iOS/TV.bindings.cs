// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.Apple.TV
{
    [NativeHeader("PlatformDependent/iPhonePlayer/IOSScriptBindings.h")]
    public sealed partial class Remote
    {
        extern public static bool allowExitToHome
        {
            [NativeConditional("PLATFORM_TVOS")]
            [FreeFunction("UnityGetAppleTVRemoteAllowExitToMenu")]
            get;

            [NativeConditional("PLATFORM_TVOS")]
            [FreeFunction("UnitySetAppleTVRemoteAllowExitToMenu")]
            set;
        }

        extern public static bool allowRemoteRotation
        {
            [NativeConditional("PLATFORM_TVOS")]
            [FreeFunction("UnityGetAppleTVRemoteAllowRotation")]
            get;

            [NativeConditional("PLATFORM_TVOS")]
            [FreeFunction("UnitySetAppleTVRemoteAllowRotation")]
            set;
        }

        extern public static bool reportAbsoluteDpadValues
        {
            [NativeConditional("PLATFORM_TVOS")]
            [FreeFunction("UnityGetAppleTVRemoteReportAbsoluteDpadValues")]
            get;

            [NativeConditional("PLATFORM_TVOS")]
            [FreeFunction("UnitySetAppleTVRemoteReportAbsoluteDpadValues")]
            set;
        }

        extern public static bool touchesEnabled
        {
            [FreeFunction("TVOSScripting::GetRemoteTouchesEnabled")]
            get;

            [NativeConditional("PLATFORM_TVOS")]
            [FreeFunction("UnitySetAppleTVRemoteTouchesEnabled")]
            set;
        }
    }
}
