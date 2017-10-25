// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEngine.StyleSheets
{
    [Serializable]
    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    internal class StyleSheet : ScriptableObject
    {
        [SerializeField]
        StyleRule[] m_Rules;

        public StyleRule[] rules
        {
            get { return m_Rules; }
            internal set
            {
                m_Rules = value;
                SetupReferences();
            }
        }

        [SerializeField]
        StyleComplexSelector[] m_ComplexSelectors;

        public StyleComplexSelector[] complexSelectors
        {
            get { return m_ComplexSelectors; }
            internal set
            {
                m_ComplexSelectors = value;
                SetupReferences();
            }
        }

        // Only the importer should write to these fields
        // Normal usage should only go through ReadXXX methods
        [SerializeField]
        internal float[] floats;

        [SerializeField]
        internal Color[] colors;

        [SerializeField]
        internal string[] strings;

        static bool TryCheckAccess<T>(T[] list, StyleValueType type, StyleValueHandle[] handles, int index, out T value)
        {
            bool result = false;
            value = default(T);
            if (index < handles.Length)
            {
                var handle = handles[index];
                if (handle.valueType == type && handle.valueIndex >= 0 && handle.valueIndex < list.Length)
                {
                    value = list[handle.valueIndex];
                    result = true;
                }
                else
                {
                    Debug.LogErrorFormat("Trying to read value of type {0} while reading a value of type {1}", type, handle.valueType);
                }
            }
            return result;
        }

        static T CheckAccess<T>(T[] list, StyleValueType type, StyleValueHandle handle)
        {
            T value = default(T);
            if (handle.valueType != type)
            {
                Debug.LogErrorFormat("Trying to read value of type {0} while reading a value of type {1}", type, handle.valueType);
            }
            else if (handle.valueIndex < 0 && handle.valueIndex >= list.Length)
            {
                Debug.LogError("Accessing invalid property");
            }
            else
            {
                value = list[handle.valueIndex];
            }
            return value;
        }

        void OnEnable()
        {
            SetupReferences();
        }

        void SetupReferences()
        {
            if (complexSelectors == null || rules == null)
                return;

            for (int i = 0; i < complexSelectors.Length; i++)
            {
                // Here we set-up runtime-only pointers
                StyleComplexSelector complexSel = complexSelectors[i];

                if (complexSel.ruleIndex < rules.Length)
                {
                    complexSel.rule = rules[complexSel.ruleIndex];
                }
            }
        }

        public StyleValueKeyword ReadKeyword(StyleValueHandle handle)
        {
            return (StyleValueKeyword)handle.valueIndex;
        }

        public float ReadFloat(StyleValueHandle handle)
        {
            return CheckAccess(floats, StyleValueType.Float, handle);
        }

        public bool TryReadFloat(StyleValueHandle[] handles, int index, out float value)
        {
            return TryCheckAccess(floats, StyleValueType.Float, handles, index, out value);
        }

        public Color ReadColor(StyleValueHandle handle)
        {
            return CheckAccess(colors, StyleValueType.Color, handle);
        }

        public bool TryReadColor(StyleValueHandle[] handles, int index, out Color value)
        {
            return TryCheckAccess(colors, StyleValueType.Color, handles, index, out value);
        }

        public string ReadString(StyleValueHandle handle)
        {
            return CheckAccess(strings, StyleValueType.String, handle);
        }

        public bool TryReadString(StyleValueHandle[] handles, int index, out string value)
        {
            return TryCheckAccess(strings, StyleValueType.String, handles, index, out value);
        }

        public string ReadEnum(StyleValueHandle handle)
        {
            return CheckAccess(strings, StyleValueType.Enum, handle);
        }

        public bool TryReadEnum(StyleValueHandle[] handles, int index, out string value)
        {
            return TryCheckAccess(strings, StyleValueType.Enum, handles, index, out value);
        }

        public string ReadResourcePath(StyleValueHandle handle)
        {
            return CheckAccess(strings, StyleValueType.ResourcePath, handle);
        }

        public bool TryReadResourcePath(StyleValueHandle[] handles, int index, out string value)
        {
            return TryCheckAccess(strings, StyleValueType.ResourcePath, handles, index, out value);
        }
    }
}
