// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Marks a static method as a contributor to the blackboard's right-click contextual menu. Every
    /// time the menu opens, the decorated method is invoked with a context that exposes the element
    /// under the cursor and lets the method append entries via
    /// <see cref="MenuContext.AppendAction(string, Action)"/>.
    /// </summary>
    /// <remarks>
    /// The decorated method must be <c>static</c>, return <c>void</c>, and take a single context
    /// parameter. The type passed to the constructor decides which context type: a
    /// <see cref="Graph"/> subclass requires a <see cref="GraphMenuContext"/> parameter, a
    /// <see cref="StateMachine"/> subclass requires a <see cref="StateMachineMenuContext"/> parameter.
    /// The user is responsible for filtering on the clicked element and deciding what to append.
    /// <br/>
    /// <br/>
    /// The handler is invoked when the active graph or state machine's type matches the listed type or
    /// derives from it. Apply the attribute multiple times on the same method to register it for
    /// several types, as long as they all require the same context type.
    /// <br/>
    /// <br/>
    /// <see cref="MenuContext.ClickedObject"/> is the clicked <see cref="IVariable"/>, or <c>null</c>
    /// when the click landed on empty space in the blackboard.
    /// </remarks>
    /// <example>
    /// <code lang="cs">
    /// <![CDATA[
    /// [BlackboardMenu(typeof(MyGraph))]
    /// static void AppendNewItems(GraphMenuContext context)
    /// {
    ///     if (context.ClickedObject is IVariable variable)
    ///     {
    ///         context.AppendAction("Int/Reset", () => Debug.Log(variable));
    ///     }
    /// }
    /// ]]>
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class BlackboardMenuAttribute : Attribute
    {
        /// <summary>
        /// The <see cref="Graph"/> or <see cref="StateMachine"/> subclass the handler is restricted to.
        /// </summary>
        public Type GraphType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardMenuAttribute"/> class.
        /// </summary>
        /// <param name="graphType">
        /// The <see cref="Graph"/> or <see cref="StateMachine"/> subclass the handler is restricted to.
        /// </param>
        public BlackboardMenuAttribute(Type graphType)
        {
            GraphType = graphType;
        }
    }
}
