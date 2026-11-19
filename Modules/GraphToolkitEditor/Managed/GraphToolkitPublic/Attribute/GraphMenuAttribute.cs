// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Marks a static method as a contributor to the graph view's right-click contextual menu. Every
    /// time the menu opens, the decorated method is invoked with a context that exposes the element
    /// under the cursor and lets the method append entries via
    /// <see cref="MenuContext.AppendAction(string, Action)"/>.
    /// </summary>
    /// <remarks>
    /// The decorated method must be <c>static</c>, return <c>void</c>, and take a single
    /// <see cref="GraphMenuContext"/> parameter. The user is responsible for filtering on the clicked
    /// element and deciding what to append.
    /// <br/>
    /// <br/>
    /// The handler is invoked when the active graph's type matches the listed type or derives from it.
    /// Apply the attribute multiple times on the same method to register it for several graph types.
    /// <br/>
    /// <br/>
    /// <see cref="MenuContext.ClickedObject"/> is the element under the cursor: an
    /// <see cref="INode"/>, an <see cref="IPort"/> or a <see cref="Wire"/>. The value is <c>null</c>
    /// when the click landed on empty space or on an element that is not exposed through the public
    /// API.
    /// <br/>
    /// <br/>
    /// To contribute entries to the state machine view, use <see cref="StateMachineMenuAttribute"/>.
    /// For the blackboard, use <see cref="BlackboardMenuAttribute"/>. For the condition list of a
    /// transition inspector, use <see cref="ConditionMenuAttribute"/>.
    /// </remarks>
    /// <example>
    /// <code lang="cs">
    /// <![CDATA[
    /// [GraphMenu(typeof(MyGraph))]
    /// static void AppendGraphItems(GraphMenuContext context)
    /// {
    ///     if (context.ClickedObject is INode node)
    ///         context.AppendAction("Custom/Inspect", () => Debug.Log(node));
    /// }
    /// ]]>
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class GraphMenuAttribute : Attribute
    {
        /// <summary>
        /// The <see cref="Graph"/> subclass the handler is restricted to.
        /// </summary>
        public Type GraphType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphMenuAttribute"/> class.
        /// </summary>
        /// <param name="graphType">
        /// The <see cref="Graph"/> subclass the handler is restricted to.
        /// </param>
        public GraphMenuAttribute(Type graphType)
        {
            GraphType = graphType;
        }
    }
}
