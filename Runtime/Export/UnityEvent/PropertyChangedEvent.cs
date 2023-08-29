// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace UnityEngine.Events
{
    internal class PropertyHelper<TBase>
    {
        public bool SetProperty<TData>(TBase instance, ref TData currentPropertyValue, TData newValue, [CallerMemberName] string propertyName = "")
        {
            if (Equals(currentPropertyValue, newValue))
                return false;

            currentPropertyValue = newValue;

            NotifyValueChange(instance, propertyName);

            return true;
        }

        public void NotifyValueChange(TBase instance, [CallerMemberName] string propertyName = "")
        {
            propertyChangedEvent.Notify(instance, propertyName);
        }

        interface IEventHolder
        {
            void Invoke(TBase setting, string property);
            bool IsEmpty();
        }

        class EventHolder<T> : IEventHolder
            where T : class, TBase
        {
            public event Action<T, string> propertyEvent;

            void IEventHolder.Invoke(TBase setting, string property)
            {
                propertyEvent?.Invoke(setting as T, property);
            }

            bool IEventHolder.IsEmpty() => propertyEvent == null;
        }

        public PropertyChangedEvent propertyChangedEvent = new();

        public struct PropertyChangedEvent
        {
            Dictionary<Type, IEventHolder> m_Subscriptions;

            public PropertyChangedEvent()
            {
                m_Subscriptions = new();
            }

            public void Subscribe<TChild>(Action<TChild, string> callback)
                where TChild : class, TBase
            {
                EventHolder<TChild> handler = null;
                if (m_Subscriptions.TryGetValue(typeof(TChild), out var tmp))
                {
                    handler = tmp as EventHolder<TChild>;
                }
                else
                {
                    handler = new();
                    m_Subscriptions.Add(typeof(TChild), handler);
                }

                System.Diagnostics.Debug.Assert(handler != null, nameof(handler) + " != null");
                handler.propertyEvent += callback;
            }

            public void Unsubscribe<TChild>(Action<TChild, string> callback)
                where TChild : class, TBase
            {
                if (!m_Subscriptions.TryGetValue(typeof(TChild), out var tmp))
                    return;

                var handler = tmp as EventHolder<TChild>;
                System.Diagnostics.Debug.Assert(handler != null, nameof(handler) + " != null");
                handler.propertyEvent -= callback;
                if (tmp.IsEmpty())
                    m_Subscriptions.Remove(typeof(TChild));
            }

            public void Notify(TBase instance, [CallerMemberName] string propertyName = "")
            {
                if (m_Subscriptions.TryGetValue(instance.GetType(), out var handler))
                {
                    handler.Invoke(instance, propertyName);
                }
            }
        }
    }
}
