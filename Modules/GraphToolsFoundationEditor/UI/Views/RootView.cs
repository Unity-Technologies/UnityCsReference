// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.CommandStateObserver;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Base class for root views.
    /// </summary>
    /// <remarks>Root views are model views that can receive commands. They are also usually being updated by an observer.</remarks>
    abstract class RootView : BaseModelView, IHierarchicalCommandTarget, IUndoableCommandMerger
    {
        public static readonly string ussClassName = "ge-view";
        public static readonly string focusedViewModifierUssClassName = ussClassName.WithUssModifier("focused");

        int m_StartMergeGroup;

        /// <summary>
        /// The graph tool.
        /// </summary>
        public BaseGraphTool GraphTool { get; }

        /// <summary>
        /// The <see cref="EditorWindow"/> containing this view.
        /// </summary>
        public EditorWindow Window { get; }

        /// <summary>
        /// The model backing this view.
        /// </summary>
        public RootViewModel Model { get; protected set; }

        /// <summary>
        /// The parent command target.
        /// </summary>
        public virtual IHierarchicalCommandTarget ParentTarget => GraphTool;

        /// <summary>
        /// The dispatcher.
        /// </summary>
        /// <remarks>To dispatch a command, use <see cref="Dispatch"/>. This will ensure the command is also dispatched to parent dispatchers.</remarks>
        protected Dispatcher Dispatcher { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RootView"/> class.
        /// </summary>
        /// <param name="window">The <see cref="EditorWindow"/> containing this view.</param>
        /// <param name="graphTool">The tool hosting this view.</param>
        protected RootView(EditorWindow window, BaseGraphTool graphTool)
        {
            focusable = true;

            GraphTool = graphTool;
            Dispatcher = new CommandDispatcher();
            Window = window;

            AddToClassList(ussClassName);
            this.AddStylesheetWithSkinVariants_Internal("View.uss");

            RegisterCallback<FocusInEvent>(OnFocus);
            RegisterCallback<FocusOutEvent>(OnLostFocus);
            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);
        }

        /// <inheritdoc />
        public virtual void Dispatch(ICommand command, Diagnostics diagnosticsFlags = Diagnostics.None)
        {
            if (command is UndoableCommand undoableCommand)
            {
                var undoString = undoableCommand.UndoString ?? "";
                GraphTool.UndoStateComponent.BeginOperation(undoString);
            }

            this.DispatchToHierarchy(command, diagnosticsFlags);

            if (command is UndoableCommand)
            {
                GraphTool.UndoStateComponent.EndOperation();
            }
        }

        /// <summary>
        /// Indicate that you want to merge the next undoable commands into one undo.
        /// </summary>
        public virtual void StartMerging()
        {
            m_StartMergeGroup = Undo.GetCurrentGroup();
        }

        /// <summary>
        /// Ends the merging of undoables commands into one undo.
        /// </summary>
        public virtual void StopMerging()
        {
            Undo.CollapseUndoOperations(m_StartMergeGroup);
        }

        public void DispatchToSelf(ICommand command, Diagnostics diagnosticsFlags = Diagnostics.None)
        {
            Dispatcher.Dispatch(command, diagnosticsFlags);
        }

        public void RegisterCommandHandler<TCommand>(ICommandHandlerFunctor commandHandlerFunctor) where TCommand : ICommand
        {
            Dispatcher.RegisterCommandHandler<TCommand>(commandHandlerFunctor);
        }

        public void UnregisterCommandHandler<TCommand>() where TCommand : ICommand
        {
            Dispatcher.UnregisterCommandHandler<TCommand>();
        }

        /// <summary>
        /// Registers all observers.
        /// </summary>
        protected abstract void RegisterObservers();

        /// <summary>
        /// Unregisters all observers.
        /// </summary>
        protected abstract void UnregisterObservers();

        void OnFocus(FocusInEvent e)
        {
            // View is focused if itself or any of its descendant has focus.
            AddToClassList(focusedViewModifierUssClassName);
        }

        void OnLostFocus(FocusOutEvent e)
        {
            RemoveFromClassList(focusedViewModifierUssClassName);
        }

        /// <summary>
        /// Callback for the <see cref="AttachToPanelEvent"/>.
        /// </summary>
        /// <param name="e">The event.</param>
        protected virtual void OnEnterPanel(AttachToPanelEvent e)
        {
            BuildUI();
            Model?.AddToState(GraphTool?.State);
            RegisterObservers();
            UpdateFromModel();
        }

        /// <summary>
        /// Callback for the <see cref="DetachFromPanelEvent"/>.
        /// </summary>
        /// <param name="e">The event.</param>
        protected virtual void OnLeavePanel(DetachFromPanelEvent e)
        {
            UnregisterObservers();
            Model?.RemoveFromState(GraphTool?.State);
        }
    }
}
