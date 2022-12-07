// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;

namespace Unity.CommandStateObserver
{
    /// <summary>
    /// Holds and binds together the tool's <see cref="State"/>, its <see cref="Dispatcher"/> and the <see cref="ObserverManager"/>.
    /// </summary>
    abstract class CsoTool : IDisposable, ICommandTarget
    {
        /// <summary>
        /// Creates and initializes a new <see cref="CsoTool"/>.
        /// </summary>
        /// <typeparam name="T">The type of tool to create.</typeparam>
        /// <returns>The newly created tool.</returns>
        public static T Create<T>() where T : CsoTool, new()
        {
            var tool = new T();
            tool.Initialize();
            return tool;
        }

        /// <summary>
        /// The command dispatcher.
        /// </summary>
        public Dispatcher Dispatcher { get; protected set; }

        /// <summary>
        /// The observer manager.
        /// </summary>
        public ObserverManager ObserverManager { get; protected set; }

        /// <summary>
        /// The state of the tool.
        /// </summary>
        public IState State { get; protected set; }

        /// <summary>
        /// Creates and initializes the tool's command dispatcher.
        /// </summary>
        protected virtual void InitDispatcher()
        {
            Dispatcher = new Dispatcher();
        }

        /// <summary>
        /// Creates and initializes the tool's observer manager.
        /// </summary>
        protected virtual void InitObserverManager()
        {
            ObserverManager = new ObserverManager();
        }

        /// <summary>
        /// Creates and initializes the tool's state.
        /// </summary>
        /// <remarks>
        /// Derived classes should override this method to create the tool
        /// state components and add them to the <see cref="State"/>
        /// </remarks>
        protected virtual void InitState()
        {
            State = new State();
        }

        ~CsoTool()
        {
            Dispose(false);
        }

        /// <summary>
        /// Initializes the tool.
        /// </summary>
        protected virtual void Initialize()
        {
            InitDispatcher();
            InitObserverManager();
            InitState();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose implementation.
        /// </summary>
        /// <param name="disposing">When true, this method is called from IDisposable.Dispose.
        /// Otherwise it is called from the finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (State is IDisposable disposableState)
                    disposableState.Dispose();

                Dispatcher = null;
                ObserverManager = null;
                // Make sure the state gets emptied before getting destroyed
                // so the components get a chance to cleanup.
                if (State != null)
                    foreach (var stateComponent in State.AllStateComponents.ToList())
                        State.RemoveStateComponent(stateComponent);
                State = null;
            }
        }

        /// <summary>
        /// Updates the state by running the observers.
        /// </summary>
        public virtual void Update()
        {
            ObserverManager.NotifyObservers(State);
        }

        /// <inheritdoc />
        /// <summary>
        /// Dispatches a command to the tool.
        /// </summary>
        /// <param name="command">The command to dispatch.</param>
        /// <param name="diagnosticsFlags">Diagnostic flags for the dispatch process.</param>
        public virtual void Dispatch(ICommand command, Diagnostics diagnosticsFlags = Diagnostics.None)
        {
            Dispatcher?.Dispatch(command, diagnosticsFlags);
        }

        /// <summary>
        /// Registers a handler for a command type. Replaces any previously registered handler for the command type.
        /// </summary>
        /// <param name="commandHandlerFunctor">The command handler.</param>
        /// <typeparam name="TCommand">The command type.</typeparam>
        public void RegisterCommandHandler<TCommand>(ICommandHandlerFunctor commandHandlerFunctor) where TCommand : ICommand
        {
            Dispatcher?.RegisterCommandHandler<TCommand>(commandHandlerFunctor);
        }

        /// <summary>
        /// Unregisters the command handler for a command type.
        /// </summary>
        /// <remarks>
        /// Since there is only one command handler registered for a command type, it is not necessary
        /// to specify the command handler to unregister.
        /// </remarks>
        /// <typeparam name="TCommand">The command type.</typeparam>
        public void UnregisterCommandHandler<TCommand>() where TCommand : ICommand
        {
            Dispatcher?.UnregisterCommandHandler<TCommand>();
        }
    }
}
