// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling;
using Unity.Timeline.Foundation.ViewModel.Internals;
using UnityEngine;
using UnityEngine.Bindings;
using Unity.Timeline.Foundation.CSO;

namespace Unity.Timeline.Foundation.ViewModel
{
    /// <summary>
    /// Base class for all viewModels.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal abstract class ViewModelBase
    {
        /// <summary>
        /// Hook to this Action to be notified on any changes to the state.
        /// </summary>
        public event Action StateChanged;

        /// <summary>
        /// Hook to this event to be notified when a component is changed.
        /// </summary>
        public event Action<IStateComponent> ComponentChanged;

        //CSO-related objects
        Dispatcher m_Dispatcher = new Dispatcher();
        State m_State = new State();
        ObserverManager m_ObserverManager = new ObserverManager();
        List<IComponentUpdater> m_ComponentUpdaters = new();
        bool m_StateHasChanged;

        static ProfilerMarker s_UpdateMarker = new ProfilerMarker($"ViewModelUpdateMarker");

        /// <summary>
        /// Dispatches an Action to the components.
        /// </summary>
        /// <param name="command">The Action to dispatch.</param>
        /// <typeparam name="TCommand">The type of the Action to be dispatched.</typeparam>
        public void Dispatch<TCommand>(TCommand command) where TCommand : ICommand
        {
            m_Dispatcher.Dispatch(command);
        }

        public void RegisterCommandHandler<TCommand>(ICommandHandlerFunctor commandHandlerFunctor)
            where TCommand : ICommand
        {
            m_Dispatcher.RegisterCommandHandler<TCommand>(commandHandlerFunctor);
        }

        /// <summary>
        /// Requests read-only access to a specific type of data from the components.
        /// </summary>
        /// <typeparam name="TData">The type of the data requested.</typeparam>
        /// <returns>A copy of the data if it exists, </returns>
        public TData GetData<TData>() where TData : struct, IReadOnlyData
        {
            return m_State.GetData<TData>();
        }

        /// <summary>
        /// Registers a observerCallback triggered whenever data of a specific type changes.
        /// </summary>
        /// <typeparam name="TData">The type of the data to listen for changes on.</typeparam>
        /// <param name="callback">The callback triggered.</param>
        public void ListenTo<TData>(Action<TData> callback) where TData : struct, IReadOnlyData
        {
            ComponentUpdater<TData> observer = GetComponentUpdater<TData>(m_ComponentUpdaters);
            if (observer != null)
                observer.OnComponentChanged += callback;
        }

        /// <summary>
        /// Registers a callback triggered whenever a command is dispatched.
        /// </summary>
        /// <param name="observerCallback">The observer to trigger.</param>
        public void RegisterCommandObserver(Action<ICommand> observerCallback)
        {
            m_Dispatcher.RegisterCommandPreDispatchCallback(observerCallback);
        }

        /// <summary>
        /// Removes a callback registered with <see cref="RegisterObserver"/>.
        /// </summary>
        /// <param name="observerCallback">The observer to remove.</param>
        public void RemoveCommandObserver(Action<ICommand> observerCallback)
        {
            m_Dispatcher.UnregisterCommandPreDispatchCallback(observerCallback);
        }

        public void RegisterStateObserver(IStateObserver observer)
        {
            m_ObserverManager.RegisterObserver(observer);
        }

        public void RemoveStateObserver(IStateObserver observer)
        {
            m_ObserverManager.UnregisterObserver(observer);
        }

        /// <summary>
        /// Unregisters a callback triggered whenever data of a specific type changes.
        /// </summary>
        /// <typeparam name="TData">The type of the data to unregister the callback from.</typeparam>
        public void Detach<TData>(Action<TData> callback) where TData : struct, IReadOnlyData
        {
            ComponentUpdater<TData> observer = GetComponentUpdater<TData>(m_ComponentUpdaters);
            if (observer != null)
                observer.OnComponentChanged -= callback;
        }

        /// <summary>
        /// Unregisters all callbacks.
        /// </summary>
        public void DetachAll()
        {
            m_ObserverManager = new ObserverManager();
            m_ComponentUpdaters.Clear();
        }

        /// <summary>
        /// Creates then registers an instance of a component type in the ViewModel.
        /// </summary>
        /// <typeparam name="TComponent">The type of the component to register.</typeparam>
        /// <typeparam name="TData">The type of the data that the component handles.</typeparam>
        public TComponent RegisterComponent<TComponent, TData>()
            where TComponent : Component, IComponent<TData>, new()
            where TData : struct, IReadOnlyData
        {
            var component = new TComponent();
            RegisterComponent<TComponent, TData>(component);
            return component;
        }

        /// <summary>
        /// Registers an instance of a Component in the ViewModel.
        /// </summary>
        /// <typeparam name="TComponent">The type of the component to register.</typeparam>
        /// <typeparam name="TData">The type of the data that the component handles.</typeparam>
        public void RegisterComponent<TComponent, TData>(TComponent component)
            where TComponent : Component, IComponent<TData>
            where TData : struct, IReadOnlyData
        {
            if (m_State.GetComponent<TComponent>() != null)
                throw new InvalidOperationException($"Component of type {typeof(TComponent)} is already registered");
            if (m_State.GetComponentForData<TData>() != null)
                throw new InvalidOperationException($"Component is already registered for data: {typeof(TData)}");

            CreateComponentUpdater(component);
            component.Update();
            component.MarkAsDirty();
            m_State.AddStateComponent(component);
        }

        /// <summary>
        /// Removes a Component from the ViewModel.
        /// </summary>
        /// <typeparam name="TComponent">The type of the component to unregister</typeparam>
        public void RemoveComponent<TComponent>() where TComponent : Component
        {
            var component = GetComponent<TComponent>();
            if (component != null)
            {
                m_State.RemoveStateComponent(component);
                RemoveComponentUpdater(m_ComponentUpdaters, component);
            }
        }

        /// <summary>
        /// Retrieves the component of the specified type
        /// </summary>
        /// <typeparam name="TComponent">The type of the component to look for </typeparam>
        /// <returns>A reference to the component if found, null otherwise.</returns>
        public TComponent GetComponent<TComponent>() where TComponent : Component
        {
            return m_State.GetComponent<TComponent>();
        }

        /// <summary>
        /// Retrieves the component of the specified type
        /// </summary>
        /// <returns>A reference to the component if found, null otherwise.</returns>
        public IEnumerable<Component> GetAllComponents()
        {
            return m_State.GetAllComponents();
        }

        /// <summary>
        /// Releases allocated resources.
        /// </summary>
        public virtual void Dispose()
        {
            m_State.Dispose();
        }

        /// <summary>
        /// Override this method to react to changes in state in child classes.
        /// </summary>
        protected virtual void OnStateChanged() { }

        /// <summary>
        /// Updates all the components stored in the ViewModel.
        /// Triggers <see cref="StateChanged"/> action if any of the components changes.
        /// Triggers callbacks registered in <see cref="ListenTo{TData}"/> if the associated data has changed.
        /// </summary>
        public virtual void Update()
        {
            using (s_UpdateMarker.Auto())
            {
                m_StateHasChanged = false;

                m_ObserverManager.NotifyObservers(m_State);

                foreach (IComponentUpdater updater in m_ComponentUpdaters)
                    updater.Process();

                foreach (IComponentUpdater updater in m_ComponentUpdaters)
                    updater.Notify(this);

                if (m_StateHasChanged)
                {
                    OnStateChanged();
                    StateChanged?.Invoke();
                }
            }
        }

        void CreateComponentUpdater<TData>(IComponent<TData> component) where TData : struct, IReadOnlyData
        {
            var stateObserver = new ComponentUpdater<TData>( component);
            m_ComponentUpdaters.Add(stateObserver);
        }

        internal void OnComponentChanged_Internal(IStateComponent component)
        {
            m_StateHasChanged = true;
            ComponentChanged?.Invoke(component);
        }

        static ComponentUpdater<TData> GetComponentUpdater<TData>(List<IComponentUpdater> updaters)
            where TData : struct, IReadOnlyData
        {
            foreach (IComponentUpdater componentUpdater in updaters)
            {
                if (componentUpdater.DataType == typeof(TData))
                    return (ComponentUpdater<TData>)componentUpdater;
            }
            return null;
        }

        static void RemoveComponentUpdater(List<IComponentUpdater> componentUpdaters, Component component)
        {
            for (var i = 0; i < componentUpdaters.Count; i++)
            {
                IComponentUpdater componentUpdater = componentUpdaters[i];
                if (componentUpdater.Component == component)
                {
                    componentUpdaters.RemoveAt(i);
                    return;
                }
            }
        }
    }
}
