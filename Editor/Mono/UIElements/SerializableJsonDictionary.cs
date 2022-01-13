// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    internal class SerializableJsonDictionary : ScriptableObject, ISerializationCallbackReceiver, ISerializableJsonDictionary
    {
        [SerializeField]
        private List<string> m_Keys = new List<string>();

        [SerializeField]
        private List<string> m_Values = new List<string>();

        [NonSerialized]
        private Dictionary<string, WeakReference> m_ObjectCache = new Dictionary<string, WeakReference>();

        [NonSerialized]
        private Dictionary<string, string> m_JsonCache = new Dictionary<string, string>();

        public void Set<T>(string key, T value) where T : class
        {
            m_ObjectCache[key] = new WeakReference(value);
            m_JsonCache[key] = EditorJsonUtility.ToJson(value);
        }

        public T Get<T>(string key) where T : class
        {
            if (!ContainsKey(key))
                return null;

            string json = null;
            if (!m_ObjectCache.ContainsKey(key) && m_JsonCache.ContainsKey(key))
            {
                json = m_JsonCache[key];
            }

            if (!string.IsNullOrEmpty(json))
            {
                T obj = Activator.CreateInstance<T>();
                EditorJsonUtility.FromJsonOverwrite(json, obj);
                m_ObjectCache[key] = new WeakReference(obj);
            }

            return m_ObjectCache[key].Target as T;
        }

        public T GetScriptable<T>(string key) where T : ScriptableObject
        {
            if (!ContainsKey(key))
                return null;

            string json = null;
            if (!m_ObjectCache.ContainsKey(key) && m_JsonCache.ContainsKey(key))
            {
                json = m_JsonCache[key];
            }

            if (!string.IsNullOrEmpty(json))
            {
                var newObject = ScriptableObject.CreateInstance<T>();
                EditorJsonUtility.FromJsonOverwrite(json, newObject);
                m_ObjectCache[key] = new WeakReference(newObject);
            }

            return m_ObjectCache[key].Target as T;
        }

        public void Overwrite(object obj, string key)
        {
            if (!ContainsKey(key))
                return;

            string json = null;
            if (!m_ObjectCache.ContainsKey(key) && m_JsonCache.ContainsKey(key))
            {
                json = m_JsonCache[key];
            }

            if (!string.IsNullOrEmpty(json))
            {
                EditorJsonUtility.FromJsonOverwrite(json, obj);
                m_ObjectCache[key] = new WeakReference(obj);
            }
        }

        public bool ContainsKey(string key)
        {
            return m_ObjectCache.ContainsKey(key) || m_JsonCache.ContainsKey(key);
        }

        public void OnBeforeSerialize()
        {
            m_Keys.Clear();
            m_Values.Clear();

            foreach (var data in m_ObjectCache)
            {
                if (data.Key == null)
                    continue;

                if (data.Value.Target != null)
                {
                    m_Keys.Add(data.Key);
                    m_Values.Add(EditorJsonUtility.ToJson(data.Value.Target));
                }
                else if (m_JsonCache.ContainsKey(data.Key))
                {
                    m_Keys.Add(data.Key);
                    m_Values.Add(m_JsonCache[data.Key]);
                }
            }
        }

        public void OnAfterDeserialize()
        {
            if (m_Keys.Count == m_Values.Count)
            {
                m_JsonCache = Enumerable.Range(0, m_Keys.Count).ToDictionary(i => m_Keys[i], i => m_Values[i]);
            }

            m_Keys.Clear();
            m_Values.Clear();
        }
    }
}
