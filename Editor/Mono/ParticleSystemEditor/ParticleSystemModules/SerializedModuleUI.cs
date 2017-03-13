// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.Collections;

namespace UnityEditor
{
    internal class SerializedModule
    {
        protected string m_ModuleName;
        SerializedObject m_Object;

        public SerializedModule(SerializedObject o, string name)
        {
            m_Object = o;
            m_ModuleName = name;
        }

        public SerializedProperty GetProperty0(string name)
        {
            SerializedProperty prop = m_Object.FindProperty(name);
            if (prop == null)
                Debug.LogError("GetProperty0: not found: " + name);
            return prop;
        }

        public SerializedProperty GetProperty(string name)
        {
            SerializedProperty prop = m_Object.FindProperty(Concat(m_ModuleName, name));
            if (prop == null)
                Debug.LogError("GetProperty: not found: " + Concat(m_ModuleName, name));
            return prop;
        }

        public SerializedProperty GetProperty0(string structName, string propName)
        {
            SerializedProperty prop = m_Object.FindProperty(Concat(structName, propName));
            if (prop == null)
                Debug.LogError("GetProperty: not found: " + Concat(structName, propName));
            return prop;
        }

        public SerializedProperty GetProperty(string structName, string propName)
        {
            SerializedProperty prop = m_Object.FindProperty(Concat(Concat(m_ModuleName, structName), propName));
            if (prop == null)
                Debug.LogError("GetProperty: not found: " + Concat(Concat(m_ModuleName, structName), propName));
            return prop;
        }

        public static string Concat(string a, string b)
        {
            return a + "." + b;
        }

        public string GetUniqueModuleName(Object o)
        {
            return Concat("" + o.GetInstanceID(), m_ModuleName);
        }

        internal SerializedObject serializedObject
        {
            get { return m_Object; }
        }
    }
} // namespace UnityEditor
