// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
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

        // Pushed by native on OnConfigurationReady, and read from the SDK callback threads the
        // ManagedCapture hooks run on. Only ever replaced, never mutated, so readers need no lock.
        [NoAutoStaticsCleanup] static int[] s_BlockedEventTypes;

        [RequiredByNativeCode(Optional = true)]
        internal static void SetBlockedEventTypes(int[] blockedEventTypes)
        {
            // Empty normalizes to null so the hook's check is a single null test.
            Volatile.Write(ref s_BlockedEventTypes,
                blockedEventTypes != null && blockedEventTypes.Length > 0 ? blockedEventTypes : null);
        }

        // The setter is only ever called from native, so it needs a managed anchor the linker keeps.
        // [Preserve] would do this unconditionally, but it's banned in engine modules because it also
        // stops the whole module from being stripped when nothing uses Insights at all — DynamicDependency
        // only keeps the setter alive when this method itself is already reachable.
        [DynamicDependency("SetBlockedEventTypes", typeof(EngineDiagnostics))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsEventTypeBlocked(int eventType)
        {
            var blockedEventTypes = Volatile.Read(ref s_BlockedEventTypes);
            if (blockedEventTypes == null)
                return false;

            for (int i = 0; i < blockedEventTypes.Length; i++)
            {
                if (blockedEventTypes[i] == eventType)
                    return true;
            }
            return false;
        }

        [NativeMethod(IsThreadSafe = true)]
        public static extern void LogEvent(int eventType, ReadOnlySpan<char> eventData, bool immediate = false);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern void LogEventDeferred(int eventType, ReadOnlySpan<char> eventData, bool immediate = false);
    }
}
