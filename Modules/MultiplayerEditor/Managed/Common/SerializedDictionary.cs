// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Multiplayer.Editor
{
    [Serializable]
    internal class SerializedDictionary<K, V> : SortedDictionary<K, V>, ISerializationCallbackReceiver
    {
        [SerializeField] List<K> m_Keys = new List<K>();
        [SerializeField] List<V> m_Values = new List<V>();

        public SerializedDictionary() {}
        public SerializedDictionary(Comparer<K> comparer) : base(comparer) {}

        public void OnBeforeSerialize()
        {
            m_Keys.Clear();
            m_Values.Clear();
            foreach (var kvp in this)
            {
                m_Keys.Add(kvp.Key);
                m_Values.Add(kvp.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            this.Clear();

            for (int i = 0; i < m_Keys.Count; i++)
                this[m_Keys[i]] = m_Values[i];

            m_Keys.Clear();
            m_Values.Clear();
        }
    }
}
