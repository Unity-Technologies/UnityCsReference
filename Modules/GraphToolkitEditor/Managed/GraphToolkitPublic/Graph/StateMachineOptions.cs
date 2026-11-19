// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Flags that define configuration options that affect the behavior and capabilities of a <see cref="StateMachine"/> class.
    /// </summary>
    /// <remarks>
    /// Use the <see cref="StateMachineOptions"/> enum in conjunction with the <see cref="StateMachineAttribute"/> to customize how a state machine behaves,
    /// including automatic state discovery. The default value is <see cref="StateMachineOptions.Default"/>, which enables
    /// standard behavior such as allowing states defined in the same assembly as the state machine to be automatically included in the graph item library.
    /// Combine flags to customize behavior. This enum is marked with
    /// <see cref="System.FlagsAttribute"/>, so you can combine values using bitwise operations to enable multiple options.
    /// <br/>
    /// This is the <see cref="StateMachine"/> counterpart of <see cref="GraphOptions"/>. Flag values are kept in sync with
    /// <see cref="GraphOptions"/> so that a <see cref="StateMachineOptions"/> value can be safely cast to the matching <see cref="GraphOptions"/> value.
    /// </remarks>
    /// <example>
    /// <code>
    /// [StateMachine("mystatemachine", StateMachineOptions.DisableAutoInclusionOfStatesFromStateMachineAssembly)]
    /// public class MyStateMachine : StateMachine { }
    /// </code>
    /// </example>
    [Flags]
    public enum StateMachineOptions
    {
        /// <summary>
        /// Indicates that this state machine supports subgraphs.
        /// </summary>
        /// <remarks>
        /// When enabled, the “Create Local Subgraph from Selection” item will be available in the right click menu of a selection of elements in the state machine and
        /// the “Create Empty Local Subgraph” item will be available in the right click menu of the state machine canvas.
        /// </remarks>
        SupportsSubgraphs = 1 << 0,

        /// <summary>
        /// Disables the automatic inclusion of states and conditions defined in the same assembly as the state machine.
        /// </summary>
        /// <remarks>
        /// By default, subclasses of <see cref="State"/> defined in the same assembly as the state machine are available in the graph item library, and subclasses
        /// of <see cref="Condition"/> defined in that assembly are available in the add-condition menu of the transition inspector. Set this flag to disable both;
        /// states and conditions must then be registered with <see cref="UseWithStateMachineAttribute"/> to be available.
        /// </remarks>
        DisableAutoInclusionOfStatesFromStateMachineAssembly = 1 << 1,

        // -------------
        // If you're adding a new flag, make sure the default is 'false'. This ensures that the user
        // doesn't override defaults by mistake when setting one or more other flags for their state machine options.
        //
        // Values are kept in sync with GraphOptions so a StateMachineOptions value can be safely converted to the
        // matching GraphOptions value.

        /// <summary>
        /// The default state machine configuration.
        /// </summary>
        /// <remarks>
        /// This default is helpful for onboarding: if users forget to mark states with <see cref="UseWithStateMachineAttribute"/>, they will still appear in the graph item library
        /// as long as they are defined in the same assembly as the state machine.
        /// </remarks>
        Default = 0,

        /// <summary>
        /// No state machine options enabled.
        /// </summary>
        /// <remarks>
        /// This disables all optional features, including automatic state inclusion.
        /// </remarks>
        None = 0
    }
}
