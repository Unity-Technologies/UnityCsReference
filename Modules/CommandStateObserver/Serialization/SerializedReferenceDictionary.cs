// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.CommandStateObserver
{
    /// <summary>
    /// Dictionary that safely survives serialization
    /// Done so by waiting for access before deserializing Values
    /// </summary>
    /// <typeparam name="TKey">Type of key.</typeparam>
    /// <typeparam name="TValue">Type of value.</typeparam>
    [Serializable]
    [MovedFrom(false, "Unity.CommandStateObserver", "Unity.GraphTools.Foundation.CommandStateObserver")]
    class SerializedReferenceDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField]
        List<TKey> m_KeyList;

        [SerializeReference]
        List<TValue> m_ValueList;

        Dictionary<TKey, TValue> m_Dictionary;

        bool IsValid => m_KeyList != null && m_ValueList != null;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializedReferenceDictionary{TKey, TValue}" /> class.
        /// </summary>
        /// <param name="capacity">Initial capacity.</param>
        public SerializedReferenceDictionary(int capacity = 0)
        {
            m_Dictionary = null;
            m_KeyList = new List<TKey>(capacity);
            m_ValueList = new List<TValue>(capacity);
        }

        Dictionary<TKey, TValue> GetSafeDictionary()
        {
            if (m_Dictionary == null)
            {
                if (!IsValid)
                {
                    m_KeyList = new List<TKey>();
                    m_ValueList = new List<TValue>();
                }
                m_Dictionary = new Dictionary<TKey, TValue>(m_KeyList.Count);
                DeserializeDictionaryFromLists(m_Dictionary, m_KeyList, m_ValueList);
            }

            return m_Dictionary;
        }

        /// <inheritdoc />
        public void OnBeforeSerialize()
        {
            if (m_Dictionary != null)
                SerializeDictionaryToLists(m_Dictionary, out m_KeyList, out m_ValueList);
        }

        /// <inheritdoc />
        public void OnAfterDeserialize()
        {
            m_Dictionary = null; // force rebuild dictionary, as references in Values will be lost
        }

        static void SerializeDictionaryToLists(IReadOnlyDictionary<TKey, TValue> dic, out List<TKey> keys, out List<TValue> values)
        {
            if (dic == null)
            {
                keys = new List<TKey>();
                values = new List<TValue>();
                return;
            }

            keys = new List<TKey>(dic.Keys);
            values = new List<TValue>(dic.Values);
        }

        static void DeserializeDictionaryFromLists(Dictionary<TKey, TValue> dic, IReadOnlyList<TKey> keys, IReadOnlyList<TValue> values)
        {
            int numKeys = keys?.Count ?? 0;

            dic.Clear();

            if (numKeys != 0)
            {
                Assert.IsNotNull(keys);
                Assert.IsNotNull(values);
                Assert.AreEqual(keys!.Count, values.Count);
                for (int i = 0; i < numKeys; i++)
                {
                    if (values[i] != null)
                        dic.Add(keys[i], values[i]);
                }
            }
        }

        /// <summary>
        /// Returns the value associated with a key.
        /// </summary>
        /// <param name="key">The key to retrieve.</param>
        public TValue this[TKey key]
        {
            get => GetSafeDictionary()[key];
            set => GetSafeDictionary()[key] = value;
        }

        /// <summary>
        /// Returns the collection of keys.
        /// </summary>
        public ICollection<TKey> Keys => GetSafeDictionary().Keys;

        /// <summary>
        /// Returns the collection of values.
        /// </summary>
        public ICollection<TValue> Values => GetSafeDictionary().Values;

        /// <summary>
        /// Checks if the dictionary contains a key.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the dictionary contains the key, false otherwise.</returns>
        public bool ContainsKey(TKey key) => GetSafeDictionary().ContainsKey(key);

        /// <summary>
        /// Adds an entry to the dictionary.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        /// <param name="value">The value of the entry.</param>
        public void Add(TKey key, TValue value) => GetSafeDictionary().Add(key, value);

        /// <summary>
        /// Removes an entry from the dictionary.
        /// </summary>
        /// <param name="key">The key of the entry to remove.</param>
        /// <returns>True if the entry was removed, false otherwise.</returns>
        public bool Remove(TKey key) => GetSafeDictionary().Remove(key);

        /// <summary>
        /// Tries to get the value for a key.
        /// </summary>
        /// <param name="key">The key of the value to retrieve.</param>
        /// <param name="value">On output, contains the value found. If the key was not found,
        /// contains the default value for type <typeparamref name="TKey"/>.</param>
        /// <returns>True if the value was found, false otherwise.</returns>
        public bool TryGetValue(TKey key, out TValue value) => GetSafeDictionary().TryGetValue(key, out value);

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetSafeDictionary().GetEnumerator();

        /// <summary>
        /// Gets an enumerator on the dictionary entries.
        /// </summary>
        /// <returns>An enumerator on the dictionary entries.</returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => GetSafeDictionary().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetSafeDictionary().GetEnumerator();

        /// <summary>
        /// Adds an item to the dictionary.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Add(KeyValuePair<TKey, TValue> item) => GetSafeDictionary().Add(item.Key, item.Value);

        /// <summary>
        /// Removes all entries from the dictionary.
        /// </summary>
        public void Clear() => GetSafeDictionary().Clear();

        /// <summary>
        /// Checks if the dictionary contains the item.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns>True if the dictionary has an entry item.Key with value item.Value. False otherwise.</returns>
        public bool Contains(KeyValuePair<TKey, TValue> item) => (GetSafeDictionary() as ICollection<KeyValuePair<TKey, TValue>>).Contains(item);

        /// <summary>
        /// Copies all the entries of the dictionary to an array, starting at a given index.
        /// </summary>
        /// <param name="array">The array where to copy the entries.</param>
        /// <param name="arrayIndex">The index in the array where the copy should start.</param>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            var i = 0;
            foreach (var kv in GetSafeDictionary())
            {
                array[arrayIndex + i] = kv;
                i++;
            }
        }

        /// <summary>
        /// Removes an entry from the dictionary.
        /// </summary>
        /// <param name="item">Specifies key of the item to remove in item.Key.</param>
        /// <returns>True if the entry was removed. False otherwise.</returns>
        public bool Remove(KeyValuePair<TKey, TValue> item) => GetSafeDictionary().Remove(item.Key);

        /// <summary>
        /// The number of entries in the dictionary.
        /// </summary>
        public int Count => GetSafeDictionary().Count;

        /// <summary>
        /// Whether the dictionary is read only. Always false.
        /// </summary>
        public bool IsReadOnly => false;

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;
    }
}
