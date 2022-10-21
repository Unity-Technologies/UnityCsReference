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

    readonly struct SearchGlobalEventHandlerContainer
    {
        public readonly int priority;
        public readonly Type type;
        public readonly object handler;
        public readonly int handlerHashCode;

        SearchGlobalEventHandlerContainer(object handler, Type type, int priority, int handlerHashCode)
        {
            this.priority = priority;
            this.type = type;
            this.handler = handler;
            this.handlerHashCode = handlerHashCode;
        }

        public static SearchGlobalEventHandlerContainer Make<T>(SearchGlobalEventHandler<T> handler, int priority)
            where T : EventBase
        {
            return new SearchGlobalEventHandlerContainer(handler, typeof(T), priority, handler.GetHashCode());
        }

        public bool IsType<T>()
        {
            return IsType(typeof(T));
        }

        public bool IsType(Type targetType)
        {
            return type == targetType;
        }

        public bool Invoke<T>(T evt)
            where T : EventBase
        {
            if (handler is not SearchGlobalEventHandler<T> eventHandler)
                return false;
            return eventHandler.Invoke(evt);
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
            var eventHandlerHashCode = eventHandler.GetHashCode();
            RemoveHandlerWithHashCode(eventHandlerHashCode, handlerList);

            handlerList.Add(SearchGlobalEventHandlerContainer.Make(eventHandler, priority));

            return () => UnregisterGlobalEventHandler(eventHandler);
        }

        public void UnregisterGlobalEventHandler<T>(SearchGlobalEventHandler<T> eventHandler)
            where T : EventBase
        {
            var eventType = typeof(T);
            if (!m_GlobalEventHandlers.TryGetValue(eventType, out var handlerList))
                return;

            var eventHandlerHashCode = eventHandler.GetHashCode();
            RemoveHandlerWithHashCode(eventHandlerHashCode, handlerList);
        }

        public IEnumerable<SearchGlobalEventHandler<T>> GetOrderedGlobalEventHandlers<T>()
            where T : EventBase
        {
            if (!m_GlobalEventHandlers.TryGetValue(typeof(T), out var handlerList))
                return Enumerable.Empty<SearchGlobalEventHandler<T>>();
            return handlerList
                .Where(container => container.IsType<T>())
                .OrderBy(container => container.priority)
                .Select(container => container.handler as SearchGlobalEventHandler<T>);
        }

        static void RemoveHandlerWithHashCode(int handlerHashCode, List<SearchGlobalEventHandlerContainer> handlerList)
        {
            var existingHandlerIndex = handlerList.FindIndex(container => container.handlerHashCode == handlerHashCode);
            if (existingHandlerIndex >= 0)
                handlerList.RemoveAt(existingHandlerIndex);
        }
    }
}
