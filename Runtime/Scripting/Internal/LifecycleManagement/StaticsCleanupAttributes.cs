// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace Unity.Scripting.LifecycleManagement
{
    [VisibleToOtherModules]
    internal enum ScopeTransitionType
    {
        Unset,
        Entering,
        Exiting,
        Both
    }

    [VisibleToOtherModules]
    internal enum CleanupStrategy
    {
        Unset,
        Auto,
        Clear,
        CaptureInitializationExpression,
        ResetToDefaultValue
    }

    [VisibleToOtherModules]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event, AllowMultiple = true)]
    internal sealed class AutoStaticsCleanupAttribute : Attribute
    {
        public Type ScopeType { get; set; }
        public ScopeTransitionType TransitionType { get; set; }
        public CleanupStrategy CleanupStrategy { get; set; }
    }

    [VisibleToOtherModules]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event, AllowMultiple = true)]
    internal sealed class AutoStaticsCleanupOnCodeReloadAttribute : Attribute
    {
        public CleanupStrategy CleanupStrategy { get; set; }
    }

    [VisibleToOtherModules]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event, AllowMultiple = false)]
    internal sealed class NoAutoStaticsCleanupAttribute : Attribute
    {
    }

    [VisibleToOtherModules]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event, AllowMultiple = true)]
    internal sealed class IgnoreForUAL0015Attribute : Attribute
    {
        public string Reason { get; }
        public IgnoreForUAL0015Attribute(string reason) => Reason = reason;
    }
}
