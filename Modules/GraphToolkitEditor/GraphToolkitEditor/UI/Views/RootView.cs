// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.CSO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Base class for root views.
    /// </summary>
    /// <remarks>Root views are model views that can receive commands. They are also usually being updated by an observer.</remarks>
    [UnityRestricted]
    internal abstract class RootView : View, IHierarchicalCommandTarget, IUndoableCommandMerger, IDisposable
    {
        /// <summary>
        /// The USS class name added to the <see cref="RootView"/>.
        /// </summary>
        public static readonly string ussClassName = "ge-view";

        /// <summary>
        /// The USS class name added to the view when it has the focus.
        /// </summary>
        public static readonly string focusedViewUssClassName = ussClassName.WithUssModifier("focused");

        protected static ViewUpdateVisitor[] s_DefaultViewUpdateVisitorList = { UpdateFromModelVisitor.genericUpdateFromModelVisitor, UpdateSelectionVisitor.Visitor };

        bool m_RequiresCompleteUIBuild;
        protected bool m_ObserversPaused;
        protected bool m_OnFocusCalled;
        bool m_Initialized;

        /// <summary>
        /// The graph tool.
        /// </summary>
        public GraphTool GraphTool { get; private set; }

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

        /// <summary>
        /// The <see cref="Unity.GraphToolkit.Editor.TypeHandleInfos"/> of the view.
        /// </summary>
        public TypeHandleInfos TypeHandleInfos { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RootView"/> class.
        /// </summary>
        /// <param name="window">The <see cref="EditorWindow"/> containing this view.</param>
        /// <param name="graphTool">The tool hosting this view.</param>
        /// <param name="typeHandleInfos">A <see cref="TypeHandleInfos"/> to use for this view. If null a new one will be created.</param>
        protected RootView(EditorWindow window, GraphTool graphTool, TypeHandleInfos typeHandleInfos = null)
        {
            TypeHandleInfos = typeHandleInfos ?? new TypeHandleInfos();

            focusable = true;
            m_RequiresCompleteUIBuild = true;

            GraphTool = graphTool;
            Window = window;

            AddToClassList(ussClassName);

            RegisterCallback<FocusInEvent>(OnFocus);
            RegisterCallback<FocusOutEvent>(OnLostFocus);
            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
        }

        /// <summary>
        /// Adds the <see cref="Model"/> to the <see cref="GraphTool"/>'s state and registers all observers.
        /// </summary>
        /// <remarks>GraphToolkit will call this method for you for standard views.</remarks>
        public virtual void Initialize()
        {
            //This code is for current users that might forget to remove the call to Initialize in their custom views.
            if (m_Initialized)
            {
                Debug.LogErrorFormat("{0} is already initialized", GetType().Name);
                return;
            }

            m_Initialized = true;

            InitDispatcher();
            Model?.AddToState(GraphTool?.State);

            var registrar = new CommandHandlerRegistrar(this);
            RegisterCommandHandlers(registrar);

            RegisterModelObservers();
        }

        /// <summary>
        /// Creates the dispatcher object.
        /// </summary>
        protected virtual void InitDispatcher()
        {
            Dispatcher = new CommandDispatcher();
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

            CustomPropertyDrawerAdapter.RemoveCommandTarget(this);
        }

        /// <summary>
        /// Gets the list of <see cref="ViewUpdateVisitor"/>s that must be used to update the given <see cref="ChildView"/>.
        /// </summary>
        /// <param name="childView">The <see cref="ChildView"/> to update.</param>
        /// <returns>The list of <see cref="ViewUpdateVisitor"/>s.</returns>
        public virtual IReadOnlyList<ViewUpdateVisitor> GetChildViewUpdaters(ChildView childView)
        {
            return s_DefaultViewUpdateVisitorList;
        }

        /// <inheritdoc />
        public virtual void Dispatch(ICommand command, Diagnostics diagnosticsFlags = Diagnostics.None)
        {
            if (command is UndoableCommand undoableCommand)
            {
                var undoString = undoableCommand.UndoString ?? "";
                GraphTool.UndoState.BeginOperation(undoString);
            }

            try
            {
                this.DispatchToHierarchy(command, diagnosticsFlags);
            }
            finally
            {
                if (command is UndoableCommand)
                {
                    GraphTool.UndoState.EndOperation();
                }
            }
        }

        /// <summary>
        /// Returns whether undoable commands are currently being merged into one undo.
        /// </summary>
        /// <returns>True if undoable commands are currently being merged into one undo</returns>
        public bool IsMergingUndoableCommands => GraphTool.UndoState.IsMerging;

        /// <summary>
        /// Indicate that you want to merge the next undoable commands into one undo.
        /// </summary>
        public virtual void StartMergingUndoableCommands()
        {
            GraphTool.UndoState.StartMergingUndoableCommands();
        }

        /// <summary>
        /// Ends the merging of undoables commands into one undo.
        /// </summary>
        public virtual void StopMergingUndoableCommands()
        {
            GraphTool.UndoState.StopMergingUndoableCommands();
        }

        /// <summary>
        /// Dispatches a command to itself, without dispatching it to the parent dispatcher.
        /// </summary>
        /// <param name="command">The command to dispatch.</param>
        /// <param name="diagnosticsFlags">Diagnostics flags to control logging and error checking.</param>
        public void DispatchToSelf(ICommand command, Diagnostics diagnosticsFlags = Diagnostics.None)
        {
            Dispatcher.Dispatch(command, diagnosticsFlags);
        }

        /// <summary>
        /// Registers the command handlers for the view
        /// </summary>
        /// <param name="registrar">Registrar to help in registering the command handlers.</param>
        protected abstract void RegisterCommandHandlers(CommandHandlerRegistrar registrar);

        /// <summary>
        /// Registers a command handler.
        /// </summary>
        /// <param name="commandHandlerFunctor">The command handler functor to register.</param>
        public void RegisterCommandHandler(ICommandHandlerFunctor commandHandlerFunctor)
        {
            Dispatcher.RegisterCommandHandler(commandHandlerFunctor);
        }

        /// <summary>
        /// Unregisters the command handler for a command type.
        /// </summary>
        /// <typeparam name="TCommand">The command type.</typeparam>
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
            m_OnFocusCalled = true;
            // View is focused if itself or any of its descendant has focus.
            AddToClassList(focusedViewUssClassName);
        }

        /// <summary>
        /// Callback for the <see cref="FocusOutEvent"/>.
        /// </summary>
        /// <param name="e">The event.</param>
        protected virtual void OnLostFocus(FocusOutEvent e)
        {
            m_OnFocusCalled = false;
            schedule.Execute(() =>
            {
                if (!m_OnFocusCalled)
                    RemoveFromClassList(focusedViewUssClassName);
            }).ExecuteLater(0);
        }

        /// <summary>
        /// Callback for the <see cref="AttachToPanelEvent"/>.
        /// </summary>
        /// <param name="e">The event.</param>
        protected virtual void OnEnterPanel(AttachToPanelEvent e)
        {
            if (m_RequiresCompleteUIBuild)
            {
                BuildUITree();
                m_RequiresCompleteUIBuild = false;
            }

            Update();
        }

        /// <summary>
        /// Attempts to pause the observers that affect the view.
        /// </summary>
        /// <returns><c>true</c> if the observers were successfully paused.</returns>
        /// <remarks>
        /// This method ensures that observers are temporarily halted. If overridden, the base method must be called to properly flag that
        /// the observers are paused. Subclasses can extend this method to pause any additional observers specific to their implementation.
        /// </remarks>
        public virtual bool TryPauseViewObservers()
        {
            if (m_ObserversPaused)
            {
                return false;
            }

            m_ObserversPaused = true;
            return true;
        }

        /// <summary>
        /// Attempts to resume the observers that affect the view.
        /// </summary>
        /// <returns><c>true</c> if the observers were successfully resumed.</returns>
        /// <remarks>
        /// This method ensures that paused observers are resumed. If overridden, the base method must be called to correctly flag that the
        /// observers are resumed. Subclasses can extend this method to resume any additional observers specific to their implementation.
        /// </remarks>
        public virtual bool TryResumeViewObservers()
        {
            if (!m_ObserversPaused)
            {
                return false;
            }

            m_ObserversPaused = false;
            return true;
        }

        public void OnCreate()
        {
            RegisterViewObservers();
        }

        public virtual void OnDestroy()
        {
            UnregisterViewObservers();
        }

        /// <summary>
        /// Handles a <see cref="ValidateCommandEvent"/> that happened while no view was focused.
        /// </summary>
        /// <param name="evt">The event.</param>
        /// <remarks>Must be overridden if this view can serve as the <see cref="GraphViewEditorWindow.DefaultCommandView"/>, handling commands when no other view is focused.</remarks>
        public virtual void HandleGlobalValidateCommand(ValidateCommandEvent evt) { }

        /// <summary>
        /// Handles an <see cref="ExecuteCommandEvent"/> that happened while no view was focused.
        /// </summary>
        /// <param name="evt">The event.</param>
        /// <remarks>Must be overridden when this view serves as the effective <see cref="GraphViewEditorWindow.DefaultCommandView"/>.</remarks>
        public virtual void HandleGlobalExecuteCommand(ExecuteCommandEvent evt) { }

        /// <summary>
        /// Updates the view.
        /// </summary>
        public abstract void Update();
    }
}
