// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("PlatformDependent/iPhonePlayer/IOSScriptBindings.h")]
    internal sealed partial class UnhandledExceptionHandler
    {
        [RequiredByNativeCode]
        static void RegisterUECatcher()
        {
            AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) =>
            {
                Debug.LogException(e.ExceptionObject as Exception);
            };
        }

    }
}
