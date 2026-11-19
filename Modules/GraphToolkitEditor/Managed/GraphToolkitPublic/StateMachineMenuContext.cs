// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Argument passed to a contextual menu handler registered for a <see cref="StateMachine"/> type.
    /// </summary>
    /// <remarks>
    /// A method decorated with <see cref="StateMachineMenuAttribute"/>, or with
    /// <see cref="BlackboardMenuAttribute"/> for a <see cref="StateMachine"/> subclass, must take
    /// this type. For a <see cref="Graph"/> subclass, take <see cref="GraphMenuContext"/> instead,
    /// and for <see cref="ConditionMenuAttribute"/> take <see cref="ConditionMenuContext"/>.
    /// </remarks>
    public sealed class StateMachineMenuContext : MenuContext
    {
        /// <summary>
        /// The state machine that owns the view the user right-clicked on.
        /// </summary>
        public StateMachine StateMachine { get; }

        internal StateMachineMenuContext(StateMachine stateMachine, object clickedObject, Vector2 mousePosition, DropdownMenu menu)
            : base(clickedObject, mousePosition, menu)
        {
            StateMachine = stateMachine;
        }
    }
}
