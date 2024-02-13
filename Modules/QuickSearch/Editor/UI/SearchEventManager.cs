// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.Search
{
    readonly struct HeadlessSearchViewState : ISearchElement
    {
        private readonly SearchViewState m_ViewState;

        public SearchViewState viewState => m_ViewState;
        public SearchContext context => m_ViewState?.context;

        public HeadlessSearchViewState(SearchViewState viewState)
        {
            m_ViewState = viewState;
        }

        public HeadlessSearchViewState(SearchContext context)
        {
            m_ViewState = new SearchViewState(context);
        }
    }

    readonly struct SearchEventPayload
    {
        public readonly ISearchElement sourceElement;
        public SearchViewState sourceViewState => sourceElement?.viewState;
        public readonly object[] arguments;

        public int argumentCount => arguments.Length;

        public SearchEventPayload(ISearchElement sourceElement, params object[] arguments)
        {
            this.sourceElement = sourceElement;
            this.arguments = arguments ?? new object[] {};
        }

        public SearchEventPayload(SearchContext sourceContext, params object[] arguments)
            : this(new HeadlessSearchViewState(sourceContext), arguments)
        {
        }

        public SearchEventPayload(SearchViewState sourceViewState, params object[] arguments)
            : this(new HeadlessSearchViewState(sourceViewState), arguments)
        {
        }

        public T GetArgument<T>(int index)
        {
            if (index < 0 || index >= argumentCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            return (T)arguments[index];
        }

        public object GetArgument(int index)
        {
            if (index < 0 || index >= argumentCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            return arguments[index];
        }

        public bool HasArgument(int index)
        {
            return index >= 0 && index < argumentCount;
        }
    }

    interface ISearchEvent
    {
        string eventName { get; }
        ISearchElement sourceElement { get; }
        SearchViewState sourceViewState { get; }
        int argumentCount { get; }
        void Use(object returnValue);
        void Use();
        T GetArgument<T>(int index);
        T GetArgument<T>(int index, T defaultValue);
        object GetArgument(int index);
        bool HasArgument(int index);
    }

    enum SearchEventResultStatus
    {
        NoHandlers,
        Complete
    }

    interface ISearchEventResult
    {
        bool hasResults { get; }
        object result { get; }
        IEnumerable<object> results { get; }
        SearchEventResultStatus status { get; }
        int handlersWithResultsCount { get; }
    }

    delegate void SearchEventHandler(ISearchEvent evt);
    delegate void SearchEventPrepareHandler(ISearchEvent prepare);
    delegate void SearchEventResultHandler(ISearchEventResult result);

    class SearchEventManager
    {
        class SearchEvent : ISearchEvent
        {
            SearchEventPayload m_Payload;

            public bool wasUsed { get; private set; }
            public object result;

            public string eventName { get; }
            public ISearchElement sourceElement => m_Payload.sourceElement;
            public SearchViewState sourceViewState => m_Payload.sourceViewState;
            public int argumentCount => m_Payload.argumentCount;

            public SearchEvent(string eventName, SearchEventPayload payload)
            {
                this.eventName = eventName;
                this.m_Payload = payload;
                wasUsed = false;
                result = null;
            }

            public void Use(object returnValue)
            {
                result = returnValue;
                wasUsed = true;
            }

            public void Use()
            {
                Use(null);
            }

            public T GetArgument<T>(int index)
            {
                return m_Payload.GetArgument<T>(index);
            }

            public T GetArgument<T>(int index, T defaultValue)
            {
                if (index < 0 || index >= m_Payload.argumentCount)
                    return defaultValue;

                return GetArgument<T>(index) ?? defaultValue;
            }

            public object GetArgument(int index)
            {
                return m_Payload.GetArgument(index);
            }

            public bool HasArgument(int index)
            {
                return m_Payload.HasArgument(index);
            }

            public void Reset()
            {
                wasUsed = false;
                result = null;
            }
        }

        readonly struct SearchEventResult : ISearchEventResult
        {
            public bool hasResults { get; }
            public object result => hasResults ? results.First() : null;
            public IEnumerable<object> results { get; }
            public SearchEventResultStatus status { get; }
            public int handlersWithResultsCount { get; }

            public SearchEventResult(IEnumerable<object> results, int handlersWithResultsCount, SearchEventResultStatus status)
            {
                this.results = results;
                hasResults = results != null && results.Any();
                this.handlersWithResultsCount = handlersWithResultsCount;
                this.status = status;
            }
        }

        ConcurrentDictionary<string, ConcurrentDictionary<int, SearchEventHandler>> m_EventHandlers = new();

        public Action On(string eventName, SearchEventHandler handler)
        {
            return On(eventName, handler, GetSearchEventHandlerHashCode(handler));
        }

        public Action On(string eventName, SearchEventHandler handler, int handlerHashCode)
        {
            var eventHandlers = m_EventHandlers.GetOrAdd(eventName, key => new ConcurrentDictionary<int, SearchEventHandler>());
            eventHandlers.AddOrUpdate(handlerHashCode, key => handler, (key, oldCallback) => handler);

            return () => Off(eventName, handlerHashCode);
        }

        public Action OnOnce(string eventName, SearchEventHandler handler)
        {
            var handlerHashCode = GetSearchEventHandlerHashCode(handler);
            return OnOnce(eventName, handler, handlerHashCode);
        }

        public Action OnOnce(string eventName, SearchEventHandler handler, int handlerHashCode)
        {
            SearchEventHandler newHandler = evt =>
            {
                handler?.Invoke(evt);
                Off(eventName, handlerHashCode);
            };
            return On(eventName, newHandler, handlerHashCode);
        }

        public void Off(string eventName, SearchEventHandler handler)
        {
            Off(eventName, GetSearchEventHandlerHashCode(handler));
        }

        public void Off(string eventName, int handlerHashCode)
        {
            if (!m_EventHandlers.TryGetValue(eventName, out var eventHandlers))
                return;

            eventHandlers.TryRemove(handlerHashCode, out _);
        }

        public void Emit(string eventName, SearchEventPayload payload)
        {
            Emit(eventName, payload, null, null);
        }

        public void Emit(string eventName, SearchEventPayload payload, SearchEventPrepareHandler onPrepare, SearchEventResultHandler onResolved)
        {
            if (!m_EventHandlers.TryGetValue(eventName, out var eventHandlers))
            {
                onPrepare?.Invoke(new SearchEvent(eventName, payload));
                onResolved?.Invoke(new SearchEventResult(null, 0, SearchEventResultStatus.NoHandlers));
                return;
            }

            Dispatcher.Enqueue(() =>
            {
                List<object> results = null;
                if (onResolved != null)
                    results = new List<object>();

                var se = new SearchEvent(eventName, payload);
                var handlersWithResultsCount = 0;
                onPrepare?.Invoke(se);
                foreach (var kvp in eventHandlers)
                {
                    var eventHandler = kvp.Value;
                    eventHandler?.Invoke(se);
                    if (se.wasUsed && results != null)
                    {
                        results.Add(se.result);
                        ++handlersWithResultsCount;
                    }
                    se.Reset();
                }

                if (onResolved != null)
                {
                    var ser = new SearchEventResult(results, handlersWithResultsCount, SearchEventResultStatus.Complete);
                    onResolved.Invoke(ser);
                }
            });
        }

        public void Clear()
        {
            foreach (var kvp in m_EventHandlers)
            {
                var eventHandlers = kvp.Value;
                eventHandlers.Clear();
            }
            m_EventHandlers.Clear();
        }

        public bool HasHandler(string eventName, SearchEventHandler handler)
        {
            return HasHandler(eventName, GetSearchEventHandlerHashCode(handler));
        }

        public bool HasHandler(string eventName, int handlerHashCode)
        {
            if (!m_EventHandlers.TryGetValue(eventName, out var handlers))
                return false;
            return handlers.ContainsKey(handlerHashCode);
        }

        public int HandlerCount(string eventName)
        {
            if (!m_EventHandlers.TryGetValue(eventName, out var handlers))
                return 0;
            return handlers.Count;
        }

        public static int GetSearchEventHandlerHashCode(SearchEventHandler callback)
        {
            if (callback == null)
                return 0;
            return callback.GetHashCode();
        }
    }
}
