// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class InMemoryRepository<TKey, TValue> where TValue : class, new()
    {
        readonly IDictionary<TKey, TValue> m_States = new Dictionary<TKey, TValue>();

        public bool ContainsKey(TKey identifier) => m_States.ContainsKey(identifier);

        public bool TryGetValue(TKey identifier, out TValue value) => m_States.TryGetValue(identifier, out value);

        public bool Create(TKey identifier, TValue state)
        {
            return m_States.TryAdd(identifier, state);
        }

        public bool Update(TKey identifier, Action<TValue> update, out TValue state)
        {
            if (m_States.TryGetValue(identifier, out state))
            {
                update.Invoke(state);
                return true;
            }

            return false;
        }

        public bool Delete(TKey identifier)
        {
            return m_States.ContainsKey(identifier) && m_States.Remove(identifier);
        }
    }
}
