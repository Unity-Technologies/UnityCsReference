// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.UIElements.StyleSheets
{
    #pragma warning disable 649
    [Serializable]
    internal struct StylePropertyInfo
    {
        public string name;
        public string syntax;
    }
    #pragma warning restore 649

    internal class StylePropertyInfoCache
    {
        #pragma warning disable 649
        [Serializable]
        private struct JsonWrapper
        {
            public StylePropertyInfo[] properties;
        }
        #pragma warning restore 649

        private Dictionary<string, StylePropertyInfo> m_Properties = new Dictionary<string, StylePropertyInfo>();

        public int count
        {
            get { return m_Properties.Count; }
        }

        public Dictionary<string, StylePropertyInfo>.Enumerator GetEnumerator()
        {
            return m_Properties.GetEnumerator();
        }

        public bool TryGet(string name, out StylePropertyInfo sp)
        {
            return m_Properties.TryGetValue(name, out sp);
        }

        public void LoadJson(string json)
        {
            var wrapper = JsonUtility.FromJson<JsonWrapper>(json);
            foreach (var sp in wrapper.properties)
            {
                m_Properties[sp.name] = sp;
            }
        }

        public string FindClosestPropertyName(string name)
        {
            float cost = float.MaxValue;
            string closestName = null;

            foreach (var propName in m_Properties.Keys)
            {
                float factor = 1;
                // Add some weight to the check if the name is part of the property name
                if (propName.Contains(name))
                    factor = 0.1f;

                float d = StringUtils.LevenshteinDistance(name, propName) * factor;
                if (d < cost)
                {
                    cost = d;
                    closestName = propName;
                }
            }

            return closestName;
        }
    }
}
