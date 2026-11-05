// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    delegate bool SearchGlobalEventHandler<in T>(T evt) where T : EventBase;
    delegate SearchGlobalEventHandlerResult SearchGlobalEventHandlerWithResult<in T>(T evt) where T : EventBase;

    readonly record struct SearchGlobalEventHandlerResult(bool Handled, bool KeepFocusOnOriginalElement)
    {
        public bool Handled { get; } = Handled;
        public bool KeepFocusOnOriginalElement { get; } = KeepFocusOnOriginalElement;

        public static SearchGlobalEventHandlerResult Default { get; } = new SearchGlobalEventHandlerResult(false, false);

        // Default behavior from legacy global event handlers is to
        // mark the event as handled and not keep the focus on the original element.
        public static implicit operator SearchGlobalEventHandlerResult(bool handled)
        {
            return new SearchGlobalEventHandlerResult(handled, false);
        }
    }

    readonly struct SearchGlobalEventHandlerContainer
    {
        public readonly int priority;
        public readonly Type type;
        public readonly Delegate handler;

        SearchGlobalEventHandlerContainer(Delegate handler, Type type, int priority)
        {
            this.priority = priority;
            this.type = type;
            this.handler = handler;
        }

        public static SearchGlobalEventHandlerContainer Make<T>(SearchGlobalEventHandler<T> handler, int priority)
            where T : EventBase
        {
            return new SearchGlobalEventHandlerContainer(handler, typeof(T), priority);
        }

        public static SearchGlobalEventHandlerContainer Make<T>(SearchGlobalEventHandlerWithResult<T> handler, int priority)
            where T : EventBase
        {
            return new SearchGlobalEventHandlerContainer(handler, typeof(T), priority);
        }

        public bool IsType<T>()
        {
            return IsType(typeof(T));
        }

        public bool IsType(Type targetType)
        {
            return type == targetType;
        }

        public SearchGlobalEventHandlerResult Invoke<T>(T evt)
            where T : EventBase
        {
            if (handler is SearchGlobalEventHandler<T> legacyEventHandler)
                return legacyEventHandler(evt);
            if (handler is SearchGlobalEventHandlerWithResult<T> eventHandler)
                return eventHandler.Invoke(evt);
            return SearchGlobalEventHandlerResult.Default;
        }
    }

    class SearchGlobalEventHandlerManager
    {
        Dictionary<Type, List<SearchGlobalEventHandlerContainer>> m_GlobalEventHandlers = new Dictionary<Type, List<SearchGlobalEventHandlerContainer>>();

        public Action RegisterGlobalEventHandler<T>(SearchGlobalEventHandler<T> eventHandler, int priority)
            where T : EventBase
        {
            var eventType = typeof(T);
            if (!m_GlobalEventHandlers.ContainsKey(eventType))
                m_GlobalEventHandlers.Add(eventType, new List<SearchGlobalEventHandlerContainer>());

            var handlerList = m_GlobalEventHandlers[eventType];
            RemoveHandler(eventHandler, handlerList);

            handlerList.Add(SearchGlobalEventHandlerContainer.Make(eventHandler, priority));

            return () => UnregisterGlobalEventHandler(eventHandler);
        }

        public Action RegisterGlobalEventHandler<T>(SearchGlobalEventHandlerWithResult<T> eventHandler, int priority)
            where T : EventBase
        {
            var eventType = typeof(T);
            if (!m_GlobalEventHandlers.ContainsKey(eventType))
                m_GlobalEventHandlers.Add(eventType, new List<SearchGlobalEventHandlerContainer>());

            var handlerList = m_GlobalEventHandlers[eventType];
            RemoveHandler(eventHandler, handlerList);

            handlerList.Add(SearchGlobalEventHandlerContainer.Make(eventHandler, priority));

            return () => UnregisterGlobalEventHandler(eventHandler);
        }

        public void UnregisterGlobalEventHandler<T>(SearchGlobalEventHandler<T> eventHandler)
            where T : EventBase
        {
            var eventType = typeof(T);
            if (!m_GlobalEventHandlers.TryGetValue(eventType, out var handlerList))
                return;

            RemoveHandler(eventHandler, handlerList);
        }

        public void UnregisterGlobalEventHandler<T>(SearchGlobalEventHandlerWithResult<T> eventHandler)
            where T : EventBase
        {
            var eventType = typeof(T);
            if (!m_GlobalEventHandlers.TryGetValue(eventType, out var handlerList))
                return;

            RemoveHandler(eventHandler, handlerList);
        }

        public IEnumerable<SearchGlobalEventHandlerContainer> GetOrderedGlobalEventHandlers<T>()
            where T : EventBase
        {
            if (!m_GlobalEventHandlers.TryGetValue(typeof(T), out var handlerList))
                return Array.Empty<SearchGlobalEventHandlerContainer>();
            #pragma warning disable UAC2001 // Avoid Linq
            return handlerList
#pragma warning restore UAC2001
                .Where(container => container.IsType<T>())
                .OrderBy(container => container.priority);
        }

        static void RemoveHandler(Delegate handler, List<SearchGlobalEventHandlerContainer> handlerList)
        {
            // Delegate.GetHashCode ignores the method on CoreCLR, so compare the delegates instead
            var existingHandlerIndex = handlerList.FindIndex(container => handler.Equals(container.handler));
            if (existingHandlerIndex >= 0)
                handlerList.RemoveAt(existingHandlerIndex);
        }

        public static SearchGlobalEventHandlerResult HandleGlobalEventHandlers<T>(SearchGlobalEventHandlerManager eventManager, T evt) where T : EventBase
        {
            var globalEventHandlers = eventManager.GetOrderedGlobalEventHandlers<T>();
            foreach (var globalEventHandler in globalEventHandlers)
            {
                var result = globalEventHandler.Invoke(evt);
                if (result.Handled)
                    return result;
            }

            return SearchGlobalEventHandlerResult.Default;
        }
    }
}
