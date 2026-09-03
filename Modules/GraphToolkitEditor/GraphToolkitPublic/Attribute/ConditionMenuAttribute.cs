// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Marks a static method as a contributor to the right-click contextual menu of a transition's
    /// condition list. Every time the menu opens, the decorated method is invoked with a
    /// <see cref="ConditionMenuContext"/> that exposes the condition under the cursor and lets the
    /// method append entries via <see cref="MenuContext.AppendAction(string, Action)"/>.
    /// </summary>
    /// <remarks>
    /// The decorated method must be <c>static</c>, return <c>void</c>, and take a single
    /// <see cref="ConditionMenuContext"/> parameter, which also exposes the transition and the rule
    /// the condition list belongs to. The user is responsible for filtering on the clicked condition
    /// and deciding what to append.
    /// <br/>
    /// <br/>
    /// The handler is invoked when the active state machine's type matches the listed type or derives
    /// from it. Apply the attribute multiple times on the same method to register it for several state
    /// machine types.
    /// <br/>
    /// <br/>
    /// <see cref="MenuContext.ClickedObject"/> is the clicked <see cref="ICondition"/>, or <c>null</c>
    /// when the click landed on empty space in the condition list. A condition defined by deriving
    /// from <see cref="Condition"/> or <see cref="Condition{T}"/> is passed as that derived type.
    /// </remarks>
    /// <example>
    /// <code lang="cs">
    /// <![CDATA[
    /// [ConditionMenu(typeof(MyStateMachine))]
    /// static void AppendNewItems(ConditionMenuContext context)
    /// {
    ///     if (context.ClickedObject is ICondition condition)
    ///     {
    ///         context.AppendAction("Custom/Inspect", () => Debug.Log(condition));
    ///     }
    /// }
    /// ]]>
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class ConditionMenuAttribute : Attribute
    {
        /// <summary>
        /// The <see cref="StateMachine"/> subclass the handler is restricted to.
        /// </summary>
        public Type StateMachineType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConditionMenuAttribute"/> class.
        /// </summary>
        /// <param name="stateMachineType">
        /// The <see cref="StateMachine"/> subclass the handler is restricted to.
        /// </param>
        public ConditionMenuAttribute(Type stateMachineType)
        {
            StateMachineType = stateMachineType;
        }
    }
}
