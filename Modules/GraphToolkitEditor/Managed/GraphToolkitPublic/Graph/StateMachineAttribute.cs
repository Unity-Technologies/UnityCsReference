// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Attribute used to declare a state machine type by associating it with a file extension and optional configuration options.
    /// </summary>
    /// <remarks>
    /// Use this attribute to associate a custom <see cref="StateMachine"/> class with a unique file extension and <see cref="StateMachineOptions"/>.
    /// The <c>extension</c> parameter defines the file extension for the state machine assets. This extension must be unique across the project
    /// because Unity uses it to select the correct importer. You can also configure additional options using <see cref="StateMachineOptions"/>.
    /// This attribute is required for any class that inherits from <see cref="StateMachine"/> and serves as the entry point for enabling
    /// editor support for the state machine tool.
    /// <br/>
    /// This is the <see cref="StateMachine"/> counterpart of <see cref="GraphAttribute"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// [StateMachine("mystatemachine", StateMachineOptions.DisableAutoInclusionOfStatesFromStateMachineAssembly)]
    /// public class MyStateMachine : StateMachine { }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class StateMachineAttribute : GraphAttribute
    {
        /// <summary>
        /// Gets the state machine configuration options.
        /// </summary>
        /// <remarks>
        /// These options define specific behaviors of the state machine, such as <see cref="StateMachineOptions.DisableAutoInclusionOfStatesFromStateMachineAssembly"/>.
        /// </remarks>
        public new StateMachineOptions Options { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StateMachineAttribute"/> class with a file extension and optional state machine options.
        /// </summary>
        /// <param name="extension">
        /// The file extension to associate with assets of the state machine type. This value must be unique because Unity uses it to select the correct importer.
        /// </param>
        /// <param name="options">
        /// The configuration options for the state machine. Defaults to <see cref="StateMachineOptions.Default"/> if not specified.
        /// </param>
        /// <remarks>
        /// Use this constructor to define the asset extension and configure the state machine. This allows for proper asset recognition and import handling by Unity.
        /// The values in <see cref="StateMachineOptions"/> support bitwise combination. Combine multiple flags to configure the state machine with custom behavior.
        /// </remarks>
        /// <example>
        /// <code>
        /// [StateMachine("mystatemachine", StateMachineOptions.DisableAutoInclusionOfStatesFromStateMachineAssembly)]
        /// public class MyStateMachine : StateMachine { }
        /// </code>
        /// </example>
        public StateMachineAttribute(string extension, StateMachineOptions options = StateMachineOptions.Default)
            : base(extension, (GraphOptions)options)
        {
            this.Options = options;
        }
    }
}
