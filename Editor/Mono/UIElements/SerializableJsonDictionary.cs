// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    internal class SerializableJsonDictionary : ScriptableObject, ISerializationCallbackReceiver, ISerializableJsonDictionary
    {
        [SerializeField]
        private List<string> m_Keys = new List<string>();

        [SerializeField]
        private List<string> m_Values = new List<string>();

        [NonSerialized]
        private Dictionary<string, object> m_Dict = new Dictionary<string, object>();

        public void Set<T>(string key, T value) where T : class
        {
            m_Dict[key] = value;
        }

        public T Get<T>(string key) where T : class
        {
            if (!ContainsKey(key))
                return null;

            if (m_Dict[key] is string)
            {
                T obj = Activator.CreateInstance<T>();
                EditorJsonUtility.FromJsonOverwrite((string)m_Dict[key], obj);
                m_Dict[key] = obj;
            }

            return m_Dict[key] as T;
        }

        public T GetScriptable<T>(string key) where T : ScriptableObject
        {
            if (!ContainsKey(key))
                return null;

            if (m_Dict[key] is string)
            {
                var newObject = ScriptableObject.CreateInstance<T>();
                EditorJsonUtility.FromJsonOverwrite((string)m_Dict[key], newObject);
                m_Dict[key] = newObject;
            }

            return m_Dict[key] as T;
        }

        public void Overwrite(object obj, string key)
        {
            if (!ContainsKey(key))
                return;

            if (m_Dict[key] is string)
            {
                EditorJsonUtility.FromJsonOverwrite((string)m_Dict[key], obj);
                m_Dict[key] = obj;
            }
            else if (m_Dict[key] != obj)
            {
                // If the dict. value has already been expanded but it's not
                // the same instance as the obj being passed in, we need to
                // copy the serialized data from the object in the dict to the
                // obj passed in and then fix the dict reference to point
                // to the obj.
                string json = EditorJsonUtility.ToJson(m_Dict[key]);
                EditorJsonUtility.FromJsonOverwrite(json, obj);
                m_Dict[key] = obj;
            }
        }

        public bool ContainsKey(string key)
        {
            return m_Dict.ContainsKey(key);
        }

        public void OnBeforeSerialize()
        {
            m_Keys.Clear();
            m_Values.Clear();

            foreach (var data in m_Dict)
            {
                if (data.Key != null && data.Value != null)
                {
                    m_Keys.Add(data.Key);
                    m_Values.Add(EditorJsonUtility.ToJson(data.Value));
                }
            }
        }

        public void OnAfterDeserialize()
        {
            if (m_Keys.Count == m_Values.Count)
            {
                m_Dict = Enumerable.Range(0, m_Keys.Count).ToDictionary(i => m_Keys[i], i => m_Values[i] as object);
            }

            m_Keys.Clear();
            m_Values.Clear();
        }
    }
}
