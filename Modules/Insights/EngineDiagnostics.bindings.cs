// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics.CodeAnalysis;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.Scripting;

namespace Unity.EngineDiagnostics
{
    [NativeHeader("Modules/Insights/EngineDiagnostics.h")]
    [ExcludeFromDocs]
    public static class EngineDiagnostics
    {
        public static readonly bool IsEnabled = IsInsightsEnabled();
        static extern bool IsInsightsEnabled();

        [NoAutoStaticsCleanup] // native sets this once at startup and native state survives code reload, so the managed value must persist to stay in sync
        public static bool IsInitialized
        {
            // So this warrants some explanations...
            // The native side will call the setter once we're initialized, but if we just mark it
            // as RequiredByNativeCode, we force it to be included always which prevents stripping
            // of the Insights module. Thus we also need to mark it as optional. But if we do that
            // without having any managed code calling it, then it will be stripped. That's where
            // DynamicDependency comes in. We're basically telling the linker that if the getter is
            // used in any in the player build, to keep the setter around even though it's not used
            // anywhere on the managed side.
            [DynamicDependency("set_IsInitialized", typeof(EngineDiagnostics))]
            get;
            [RequiredByNativeCode(Optional = true)]
            private set;
        }

        public static extern bool IsEventAllowed(int eventType);

        [NativeMethod(IsThreadSafe = true)]
        public static extern void LogEvent(int eventType, ReadOnlySpan<char> eventData, bool immediate = false);
    }
}
