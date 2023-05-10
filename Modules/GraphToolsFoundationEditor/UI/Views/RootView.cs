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
    abstract class RootView : View, IHierarchicalCommandTarget, IUndoableCommandMerger, IDisposable
    {
        public static readonly string ussClassName = "ge-view";
        public static readonly string focusedViewModifierUssClassName = ussClassName.WithUssModifier("focused");

        bool m_RequiresCompleteUIBuild;

        /// <summary>
        /// The graph tool.
        /// </summary>
        public BaseGraphTool GraphTool { get; private set; }

        /// <summary>
        /// The <see cref="EditorWindow"/> containing this view.
        /// </summary>
        public EditorWindow Window { get; private set; }

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
        protected Dispatcher Dispatcher { get; private set; }

        TypeHandleInfos m_TypeHandleInfos;

        public TypeHandleInfos TypeHandleInfos => m_TypeHandleInfos;

        /// <summary>
        /// Initializes a new instance of the <see cref="RootView"/> class.
        /// </summary>
        /// <param name="window">The <see cref="EditorWindow"/> containing this view.</param>
        /// <param name="graphTool">The tool hosting this view.</param>
        /// <param name="typeHandleInfos">A <see cref="TypeHandleInfos"/> to use or this view. If null a new one will be created.</param>
        protected RootView(EditorWindow window, BaseGraphTool graphTool, TypeHandleInfos typeHandleInfos = null)
        {
            if (typeHandleInfos != null)
                m_TypeHandleInfos = typeHandleInfos;
            else
                m_TypeHandleInfos = new TypeHandleInfos();

            focusable = true;
            m_RequiresCompleteUIBuild = true;

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

        /// <summary>
        /// Adds the <see cref="Model"/> to the <see cref="GraphTool"/>'s state and registers all observers.
        /// </summary>
        protected virtual void Initialize()
        {
            Model?.AddToState(GraphTool?.State);
            RegisterModelObservers();
        }

        ~RootView()
        {
            Dispose(false);
        }

        /// <summary>
        /// Disposes all resources, unregisters all observers and removes the <see cref="Model"/> from the <see cref="GraphTool"/>'s state.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes all resources, unregisters all observers and removes the <see cref="Model"/> from the <see cref="GraphTool"/>'s state.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            UnregisterModelObservers();
            Model?.RemoveFromState(GraphTool?.State);

            GraphTool = null;
            Dispatcher = null;
            Window = null;

            UnregisterCallback<FocusInEvent>(OnFocus);
            UnregisterCallback<FocusOutEvent>(OnLostFocus);
            UnregisterCallback<AttachToPanelEvent>(OnEnterPanel);
            UnregisterCallback<DetachFromPanelEvent>(OnLeavePanel);
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
        public virtual void StartMergingUndoableCommands()
        {
            GraphTool.UndoStateComponent.StartMergingUndoableCommands();
        }

        /// <summary>
        /// Ends the merging of undoables commands into one undo.
        /// </summary>
        public virtual void StopMergingUndoableCommands()
        {
            GraphTool.UndoStateComponent.StopMergingUndoableCommands();
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
        /// Registers all observers that can affect the models.
        /// </summary>
        protected abstract void RegisterModelObservers();

        /// <summary>
        /// Registers all observers that affect only the view.
        /// </summary>
        protected abstract void RegisterViewObservers();

        /// <summary>
        /// Unregisters all observers that can affect the models.
        /// </summary>
        protected abstract void UnregisterModelObservers();

        /// <summary>
        /// Unregisters all observers that affect only the view.
        /// </summary>
        protected abstract void UnregisterViewObservers();

        /// <summary>
        /// Callback for the <see cref="FocusInEvent"/>.
        /// </summary>
        /// <param name="e">The event.</param>
        protected virtual void OnFocus(FocusInEvent e)
        {
            // View is focused if itself or any of its descendant has focus.
            AddToClassList(focusedViewModifierUssClassName);
        }

        /// <summary>
        /// Callback for the <see cref="FocusOutEvent"/>.
        /// </summary>
        /// <param name="e">The event.</param>
        protected virtual void OnLostFocus(FocusOutEvent e)
        {
            RemoveFromClassList(focusedViewModifierUssClassName);
        }

        /// <summary>
        /// Callback for the <see cref="AttachToPanelEvent"/>.
        /// </summary>
        /// <param name="e">The event.</param>
        protected virtual void OnEnterPanel(AttachToPanelEvent e)
        {
            if (m_RequiresCompleteUIBuild)
            {
                BuildUI();
                m_RequiresCompleteUIBuild = false;
            }
            RegisterViewObservers();
            UpdateFromModel();
        }

        /// <summary>
        /// Callback for the <see cref="DetachFromPanelEvent"/>.
        /// </summary>
        /// <param name="e">The event.</param>
        protected virtual void OnLeavePanel(DetachFromPanelEvent e)
        {
            UnregisterViewObservers();
        }
    }
}
