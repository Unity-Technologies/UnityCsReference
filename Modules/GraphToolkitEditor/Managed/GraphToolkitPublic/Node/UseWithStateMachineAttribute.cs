// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Attribute used to specify which <see cref="StateMachine"/> types are compatible with the decorated state machine
    /// element, such as a <see cref="State"/>, <see cref="SelfTransition"/>, or <see cref="Condition"/> class.
    /// </summary>
    /// <remarks>
    /// This attribute links a specific state machine element to one or more <see cref="StateMachine"/> types, enabling
    /// fine-grained control over which state machine types support the element. This allows framework authors to explicitly
    /// declare element compatibility across different kinds of state machines and ensures that only valid elements are
    /// available for use in each state machine context.
    /// <br/>
    /// <br/>
    /// By default, elements defined in the same assembly as the state machine are considered compatible and available.
    /// In this default setup, the <see cref="UseWithStateMachineAttribute"/> is not required.
    /// However, when a state machine uses <see cref="StateMachineOptions.DisableAutoInclusionOfStatesFromStateMachineAssembly"/>, this attribute must be used to declare which <see cref="StateMachine"/> types support the element.
    /// <br/>
    /// <br/>
    /// This attribute affects editor behaviors such as graph item library population and helps prevent the accidental use of unsupported elements.
    /// <br/>
    /// This is the <see cref="StateMachine"/> counterpart of <see cref="UseWithGraphAttribute"/>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UseWithStateMachineAttribute : Attribute
    {
        Type[] stateMachineTypes { get; }

        /// <summary>
        /// Determines whether the specified state machine type supports the element decorated with this attribute.
        /// </summary>
        /// <param name="stateMachineType">The type of the state machine to validate.</param>
        /// <returns><c>true</c> if the state machine type supports the element; otherwise, <c>false</c>.</returns>
        public bool IsStateMachineTypeSupported(Type stateMachineType)
        {
            foreach (var type in stateMachineTypes)
            {
                if (type.IsAssignableFrom(stateMachineType))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UseWithStateMachineAttribute"/> class with the specified supported state machine types.
        /// </summary>
        /// <param name="stateMachineTypes">An array of state machine types that support the decorated element type.</param>
        public UseWithStateMachineAttribute(params Type[] stateMachineTypes)
        {
            this.stateMachineTypes = stateMachineTypes;
        }
    }
}
