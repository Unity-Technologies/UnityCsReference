// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
//using TableType=System.Collections.Generic.SortedList<string, UnityEngine.StyleSheets.StyleComplexSelector>;
using TableType = System.Collections.Generic.Dictionary<string, UnityEngine.StyleSheets.StyleComplexSelector>;
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


        [SerializeField]
        internal Object[] assets;


        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        [NonSerialized]
        internal TableType orderedNameSelectors;

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        [NonSerialized]
        internal TableType orderedTypeSelectors;

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        [NonSerialized]
        internal TableType orderedClassSelectors;

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
            else if (list == null || handle.valueIndex < 0 || handle.valueIndex >= list.Length)
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

            orderedClassSelectors = new TableType(StringComparer.Ordinal);
            orderedNameSelectors = new TableType(StringComparer.Ordinal);
            orderedTypeSelectors = new TableType(StringComparer.Ordinal);

            for (int i = 0; i < complexSelectors.Length; i++)
            {
                // Here we set-up runtime-only pointers
                StyleComplexSelector complexSel = complexSelectors[i];

                if (complexSel.ruleIndex < rules.Length)
                {
                    complexSel.rule = rules[complexSel.ruleIndex];
                }

                complexSel.orderInStyleSheet = i;

                StyleSelector lastSelector = complexSel.selectors[complexSel.selectors.Length - 1];
                StyleSelectorPart part = lastSelector.parts[0];

                string key = part.value;

                TableType tableToUse = null;

                switch (part.type)
                {
                    case StyleSelectorType.Class:
                        tableToUse = orderedClassSelectors;
                        break;
                    case StyleSelectorType.ID:
                        tableToUse = orderedNameSelectors;
                        break;
                    case StyleSelectorType.Type:
                    case StyleSelectorType.Wildcard:
                        key = part.value ?? "*";
                        tableToUse = orderedTypeSelectors;
                        break;
                    // in this case we assume a wildcard selector
                    // since a selector such as ":selected" applies to all elements
                    case StyleSelectorType.PseudoClass:
                        key = "*";
                        tableToUse = orderedTypeSelectors;
                        break;
                    default:
                        Debug.LogError($"Invalid first part type {part.type}");
                        break;
                }

                if (tableToUse != null)
                {
                    StyleComplexSelector previous;
                    if (tableToUse.TryGetValue(key, out previous))
                    {
                        complexSel.nextInTable = previous;
                    }
                    tableToUse[key] = complexSel;
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

        public Object ReadAssetReference(StyleValueHandle handle)
        {
            return CheckAccess(assets, StyleValueType.AssetReference, handle);
        }

        public bool TryReadAssetReference(StyleValueHandle[] handles, int index, out Object value)
        {
            return TryCheckAccess(assets, StyleValueType.AssetReference, handles, index, out value);
        }
    }
}
