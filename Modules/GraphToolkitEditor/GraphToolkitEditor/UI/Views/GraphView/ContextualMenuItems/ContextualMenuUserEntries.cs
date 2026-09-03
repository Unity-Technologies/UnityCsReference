// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.Editor.Implementation;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor.ContextualMenuItems
{
    /// <summary>
    /// Appends the entries contributed by user code to a contextual menu that is being built,
    /// and keeps them visually separated from the built-in entries above them. Also translates the
    /// internal model under the cursor into the public object handed to user code through
    /// <see cref="MenuContext.ClickedObject"/>.
    /// </summary>
    static class ContextualMenuUserEntries
    {
        internal static void AppendGraphEntries(ContextualMenuPopulateEvent evt, IGraphInternal owner, object clickedObject)
        {
            Append(evt, CreateContext(owner, clickedObject, evt.mousePosition, evt.menu),
                MenuCommandRegistry.InvokeGraphHandlers);
        }

        internal static void AppendBlackboardEntries(ContextualMenuPopulateEvent evt, IGraphInternal owner, object clickedObject)
        {
            Append(evt, CreateContext(owner, clickedObject, evt.mousePosition, evt.menu),
                MenuCommandRegistry.InvokeBlackboardHandlers);
        }

        internal static void AppendConditionEntries(ContextualMenuPopulateEvent evt,
            TransitionSupportModel transitionSupport, TransitionModel rule, ConditionModel clickedCondition)
        {
            if (transitionSupport == null || rule == null ||
                !((transitionSupport.GraphModel as GraphModelImp)?.Graph is StateMachine stateMachine))
                return;

            Append(evt, new ConditionMenuContext(stateMachine, transitionSupport.AsPublicTransition(), rule,
                GetClickedCondition(clickedCondition), evt.mousePosition, evt.menu),
                MenuCommandRegistry.InvokeConditionHandlers);
        }

        /// <summary>
        /// The public object a graph element under the cursor is exposed as through
        /// <see cref="MenuContext.ClickedObject"/>, or <c>null</c> when it has no public equivalent.
        /// </summary>
        internal static object GetClickedObject(IGraphInternal owner, GraphElementModel model)
        {
            switch (model)
            {
                case IUserNodeModelImp userNode:
                    return userNode.Node;
                // A user state model is itself an IState, so it has to be matched before the IState
                // case, otherwise the model would be returned instead of the user's own State.
                case UserStateModelImp userState:
                    return userState.Node;
                case IState state:
                    return state;
                case INode node:
                    return node;
                case IPort port:
                    return port;
                // A transition support is a WireModel, so it has to be matched before the wire case.
                case TransitionSupportModel transition:
                    return transition.AsPublicTransition();
            }

            if (model is WireModel wireModel && wireModel.FromPort != null && wireModel.ToPort != null)
                return (owner as Graph)?.GetWire(wireModel.FromPort, wireModel.ToPort);

            return null;
        }

        /// <summary>
        /// The public object a condition under the cursor is exposed as through
        /// <see cref="MenuContext.ClickedObject"/>, or <c>null</c> when the cursor is over empty
        /// space in the condition list.
        /// </summary>
        internal static object GetClickedCondition(ConditionModel model)
        {
            // The root group spans the whole condition list, so clicking it is clicking empty space.
            if (model == null || model.Parent == null)
                return null;

            if (model is UserConditionModelImp userCondition)
                return userCondition.Condition;

            return model;
        }

        static void Append(ContextualMenuPopulateEvent evt, MenuContext context, Action<MenuContext> invokeHandlers)
        {
            if (context == null)
                return;

            var itemCountBefore = evt.menu.MenuItems().Count;
            invokeHandlers(context);

            // If a handler added entries, separate them from the built-in entries
            // above so the user's items don't blend visually with the previous
            // category. Skip when the user's first item is already a separator so
            // a handler that prepends its own doesn't end up with two.
            var items = evt.menu.MenuItems();
            if (items.Count > itemCountBefore && items[itemCountBefore] is not DropdownMenuSeparator)
                evt.menu.InsertSeparator(string.Empty, itemCountBefore);
        }

        static MenuContext CreateContext(IGraphInternal owner, object clickedObject, UnityEngine.Vector2 mousePosition, DropdownMenu menu)
        {
            return owner switch
            {
                Graph graph => new GraphMenuContext(graph, clickedObject, mousePosition, menu),
                StateMachine stateMachine => new StateMachineMenuContext(stateMachine, clickedObject, mousePosition, menu),
                _ => null
            };
        }
    }
}
