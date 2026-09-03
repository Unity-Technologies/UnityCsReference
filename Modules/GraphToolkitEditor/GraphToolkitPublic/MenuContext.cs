// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Base argument passed to a method that contributes entries to a right-click contextual menu.
    /// Exposes the element directly under the cursor, the mouse position, and helpers for appending
    /// entries to the menu.
    /// </summary>
    /// <remarks>
    /// A handler declares the derived type that matches the view it is registered for:
    /// <see cref="GraphMenuContext"/> for a <see cref="Graph"/>,
    /// <see cref="StateMachineMenuContext"/> for a <see cref="StateMachine"/>, or
    /// <see cref="ConditionMenuContext"/> for the condition list of a transition inspector. The
    /// attribute decides which one the method must take.
    /// <br/>
    /// <br/>
    /// Only the right-clicked element is exposed, via <see cref="ClickedObject"/>; reading the full
    /// multi-element selection is not part of the public API at this time.
    /// </remarks>
    public abstract class MenuContext
    {
        readonly DropdownMenu m_Menu;

        /// <summary>
        /// The element directly under the cursor at the time of the right-click, or <c>null</c> when
        /// the click landed on empty space or on an element that is not exposed through the public API.
        /// </summary>
        /// <remarks>
        /// Test the runtime type to filter on a specific element kind, for example
        /// <c>context.ClickedObject is INode node</c>. Depending on the surface the handler is
        /// registered for, the value can be an <see cref="INode"/>, an <see cref="IPort"/>, a
        /// <see cref="Wire"/>, an <see cref="IVariable"/>, an <see cref="IState"/>, an
        /// <see cref="ITransition"/> or an <see cref="ICondition"/>. A subgraph state is an
        /// <see cref="ISubgraphState"/>, which derives from <see cref="IState"/>.
        /// <br/>
        /// <br/>
        /// Elements with no public representation, such as placemats and sticky notes, also report
        /// <c>null</c>, so a <c>null</c> value does not guarantee the click landed on nothing.
        /// </remarks>
        public object ClickedObject { get; }

        /// <summary>
        /// World-space mouse position of the right-click.
        /// </summary>
        public Vector2 MousePosition { get; }

        internal MenuContext(object clickedObject, Vector2 mousePosition, DropdownMenu menu)
        {
            ClickedObject = clickedObject;
            MousePosition = mousePosition;
            m_Menu = menu;
        }

        /// <summary>
        /// Appends an entry to the contextual menu.
        /// </summary>
        /// <param name="actionName">The label of the entry. Use forward slashes to nest the entry under submenus.</param>
        /// <param name="action">The callback invoked when the user selects the entry.</param>
        public void AppendAction(string actionName, Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            m_Menu.AppendAction(actionName, _ => action());
        }

        /// <summary>
        /// Appends an entry to the contextual menu.
        /// </summary>
        /// <param name="actionName">The label of the entry. Use forward slashes to nest the entry under submenus.</param>
        /// <param name="action">The callback invoked when the user selects the entry. The argument
        /// gives access to the user data set on the entry.</param>
        public void AppendAction(string actionName, Action<DropdownMenuAction> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            m_Menu.AppendAction(actionName, action);
        }

        /// <summary>
        /// Appends an entry to the contextual menu, with a status callback that decides whether the
        /// entry is enabled, disabled, or checked when the menu opens.
        /// </summary>
        /// <param name="actionName">The label of the entry. Use forward slashes to nest the entry under submenus.</param>
        /// <param name="action">The callback invoked when the user selects the entry.</param>
        /// <param name="actionStatusCallback">Callback that returns the entry's status at menu-open time.</param>
        /// <param name="userData">Arbitrary data forwarded to <paramref name="action"/> and <paramref name="actionStatusCallback"/> via <see cref="DropdownMenuAction.userData"/>.</param>
        public void AppendAction(string actionName, Action<DropdownMenuAction> action,
            Func<DropdownMenuAction, DropdownMenuAction.Status> actionStatusCallback, object userData = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (actionStatusCallback == null)
                throw new ArgumentNullException(nameof(actionStatusCallback));
            m_Menu.AppendAction(actionName, action, actionStatusCallback, userData);
        }

        /// <summary>
        /// Appends a visual separator to the contextual menu.
        /// </summary>
        /// <param name="subMenuPath">Optional submenu path the separator belongs to. Pass an empty string for the root menu.</param>
        public void AppendSeparator(string subMenuPath = "")
        {
            m_Menu.AppendSeparator(subMenuPath);
        }
    }
}
