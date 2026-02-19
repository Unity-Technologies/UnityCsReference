using System;
using System.Collections.Generic;
using System.Text;

namespace Unity.Scripting.LifecycleManagement
{
    /// <summary>
    /// Defines if automatic statics cleanup should be performed on entering or exiting the scope.
    /// </summary>
    internal enum ScopeTransitionType
    {
        Unset,
        Entering,
        Exiting,
        Both
    }

    /// <summary>
    /// Defines how field or property cleanup should be performed.
    /// Events ignore this enum, and all event handlers are removed on cleanup.
    /// </summary>
    internal enum CleanupStrategy
    {
        Unset,
        Auto, // based on the field or property type, will either call clear, capture initialization expression or reset to default value
        Clear, // Call a Clear method on the field or property
        CaptureInitializationExpression, // Capture the initialization expression and call it on cleanup
        ResetToDefaultValue // Reset the field or property to its type's default value
    }

    /// <summary>
    /// Applied to a type, it will define default cleanup strategy for all static fields, events and properties.
    /// On a field, property or event, it will override the default cleanup strategy.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event, AllowMultiple = true)]
    internal sealed class AutoStaticsCleanupAttribute : Attribute
    {
        /// <summary>
        /// Type of the scope that will trigger the cleanup.
        /// Null means inherit defaults or "PlayMode" scope (if no default value set).
        /// </summary>
        public Type? ScopeType { get; set; } = null;

        /// <summary>
        /// Defines if the cleanup should be performed on entering or exiting the scope.
        /// Null means inherit defaults or "Both" (if no default value set).
        /// </summary>
        public ScopeTransitionType TransitionType { get; set; }

        /// <summary>
        /// Defines how field or property cleanup should be performed.
        /// Null means inherit defaults or "Auto" (if no default value set).
        /// </summary>
        public CleanupStrategy CleanupStrategy { get; set; }
    }

    /// <summary>
    /// Applied to a type field, property or event, it will define default cleanup strategy for all static fields, events and properties for handling Code Reload
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event, AllowMultiple = true)]
    internal sealed class AutoStaticsCleanupOnCodeReloadAttribute : Attribute
    {
        /// <summary>
        /// Defines how field or property cleanup should be performed.
        /// Null means inherit defaults or "Auto" (if no default value set).
        /// </summary>
        public CleanupStrategy CleanupStrategy { get; set; }
    }

    /// <summary>
    /// Applied on a type, it will set the default rule as "no cleanup" for all static fields, events and properties. This can be overriden by applying AutoStaticsCleanupAttribute on a specific field, property or event.
    /// Applied on a field, property or event, it will override the default rule.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event, AllowMultiple = false)]
    internal sealed class NoAutoStaticsCleanupAttribute : Attribute
    {
    }

    /// <summary>
    /// Signals that this symbol should not be taken into account for UAL 0015 (because it is used as a cache that is cleared on code reload for example)
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event, AllowMultiple = true)]
    internal sealed class IgnoreForUAL0015Attribute : Attribute
    {
        public string Reason { get; }
        public IgnoreForUAL0015Attribute(string reason) => Reason = reason;
    }
}
