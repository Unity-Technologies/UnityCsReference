// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    class TypeSelectionList
    {
        private List<TypeSelection> m_TypeSelections;
        public List<TypeSelection> typeSelections { get { return m_TypeSelections; } }

        public TypeSelectionList(Object[] objects)
        {
            // Create dictionary of lists of objects indexed by their type.
            Dictionary<string, List<Object>> types = new Dictionary<string, List<Object>>();
            foreach (Object o in objects)
            {
                string typeName = ObjectNames.GetTypeName(o);
                if (!types.ContainsKey(typeName))
                    types[typeName] = new List<Object>();
                types[typeName].Add(o);
            }

            // Create and store a TypeSelection per type.
            m_TypeSelections = new List<TypeSelection>();
            foreach (KeyValuePair<string, List<Object>> kvp in types)
                m_TypeSelections.Add(new TypeSelection(kvp.Key, kvp.Value.ToArray()));

            // Sort the TypeSelections
            m_TypeSelections.Sort();
        }
    }

    class TypeSelection : IComparable
    {
        public GUIContent label;
        public Object[] objects;

        public TypeSelection(string typeName, Object[] objects)
        {
            System.Diagnostics.Debug.Assert(objects != null || objects.Length >= 1);
            this.objects = objects;
            label = new GUIContent(objects.Length + " " + ObjectNames.NicifyVariableName(typeName) + (objects.Length > 1 ? "s" : ""));
            label.image = AssetPreview.GetMiniTypeThumbnail(objects[0]);
        }

        public int CompareTo(object o)
        {
            TypeSelection other = (TypeSelection)o;

            // Sort by amount of objects
            if (other.objects.Length != objects.Length)
                return other.objects.Length.CompareTo(objects.Length);

            // Sort alphabetically
            return label.text.CompareTo(other.label.text);
        }
    }
}
