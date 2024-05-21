// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.iOS
{
    [NativeHeader("PlatformDependent/iPhonePlayer/IOSScriptBindings.h")]
    public sealed partial class Device
    {
        extern public static bool hideHomeButton
        {
            [NativeConditional("PLATFORM_IOS")]
            [FreeFunction("IOSScripting::GetHideHomeButton")] get;
            [NativeConditional("PLATFORM_IOS")]
            [FreeFunction("IOSScripting::SetHideHomeButton")] set;
        }

        extern public static bool wantsSoftwareDimming
        {
            [NativeConditional("PLATFORM_IOS")]
            [FreeFunction("IOSScripting::GetWantsSoftwareDimming")] get;
            [NativeConditional("PLATFORM_IOS")]
            [FreeFunction("IOSScripting::SetWantsSoftwareDimming")] set;
        }

        extern private static int deferSystemGesturesModeInternal
        {
            [NativeConditional("PLATFORM_IOS")]
            [FreeFunction("IOSScripting::GetDeferSystemGesturesMode")] get;
            [NativeConditional("PLATFORM_IOS")]
            [FreeFunction("IOSScripting::SetDeferSystemGesturesMode")] set;
        }

        public static SystemGestureDeferMode deferSystemGesturesMode
        {
            get { return (SystemGestureDeferMode)deferSystemGesturesModeInternal; }
            set { deferSystemGesturesModeInternal = (int)value; }
        }

        [NativeConditional("PLATFORM_IOS")]
        [NativeMethod(Name = "IOSScripting::RequestStoreReview", IsFreeFunction = true, IsThreadSafe = true)]
        extern public static bool RequestStoreReview();
    }
}
