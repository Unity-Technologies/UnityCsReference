// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Marks a static method as a contributor to the state machine view's right-click contextual menu.
    /// Every time the menu opens, the decorated method is invoked with a
    /// <see cref="StateMachineMenuContext"/> that exposes the element under the cursor and lets the
    /// method append entries via <see cref="MenuContext.AppendAction(string, Action)"/>.
    /// </summary>
    /// <remarks>
    /// The decorated method must be <c>static</c>, return <c>void</c>, and take a single
    /// <see cref="StateMachineMenuContext"/> parameter. The user is responsible for filtering on the
    /// clicked element and deciding what to append.
    /// <br/>
    /// <br/>
    /// The handler is invoked when the active state machine's type matches the listed type or derives
    /// from it. Apply the attribute multiple times on the same method to register it for several state
    /// machine types.
    /// <br/>
    /// <br/>
    /// <see cref="MenuContext.ClickedObject"/> is the element under the cursor: an
    /// <see cref="IState"/>, an <see cref="ITransition"/> or an <see cref="IPort"/>. A subgraph state
    /// is an <see cref="ISubgraphState"/>, which derives from <see cref="IState"/>. The value is
    /// <c>null</c> when the click landed on empty space or on an element that is not exposed through
    /// the public API.
    /// <br/>
    /// <br/>
    /// To contribute entries to the graph view, use <see cref="GraphMenuAttribute"/>. For the
    /// blackboard, use <see cref="BlackboardMenuAttribute"/>. For the condition list of a transition
    /// inspector, use <see cref="ConditionMenuAttribute"/>.
    /// </remarks>
    /// <example>
    /// <code lang="cs">
    /// <![CDATA[
    /// [StateMachineMenu(typeof(MyStateMachine))]
    /// static void AppendStateMachineItems(StateMachineMenuContext context)
    /// {
    ///     context.AppendAction("Custom/State Machine", () => Debug.Log(context.StateMachine));
    ///
    ///     if (context.ClickedObject is IState state)
    ///         context.AppendAction("Custom/State", () => Debug.Log(state));
    ///
    ///     if (context.ClickedObject is ITransition transition)
    ///         context.AppendAction("Custom/Transition", () => Debug.Log(transition));
    /// }
    /// ]]>
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class StateMachineMenuAttribute : Attribute
    {
        /// <summary>
        /// The <see cref="StateMachine"/> subclass the handler is restricted to.
        /// </summary>
        public Type StateMachineType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StateMachineMenuAttribute"/> class.
        /// </summary>
        /// <param name="stateMachineType">
        /// The <see cref="StateMachine"/> subclass the handler is restricted to.
        /// </param>
        public StateMachineMenuAttribute(Type stateMachineType)
        {
            StateMachineType = stateMachineType;
        }
    }
}
