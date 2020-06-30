using System;
using System.Collections.Generic;
using TableType = System.Collections.Generic.Dictionary<string, UnityEngine.UIElements.StyleComplexSelector>;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Style sheets are applied to visual elements in order to control the layout and visual appearance of the user interface.
    /// </summary>
    /// <remarks>
    /// The <see cref="StyleSheet"/> class holds the imported data of USS files in your project.
    /// Once loaded, a style sheet can be attached to a <see cref="VisualElement"/> object to affect the element itself and its descendants.
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// public class MyEditorWindow : EditorWindow
    /// {
    ///     void OnEnable()
    ///     {
    ///         rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/styles.uss"));
    ///     }
    /// }
    /// ]]>
    /// </code>
    /// </example>
    [Serializable]
    public class StyleSheet : ScriptableObject
    {
        [SerializeField]
        StyleRule[] m_Rules;

        internal StyleRule[] rules
        {
            get { return m_Rules; }
            set
            {
                m_Rules = value;
                SetupReferences();
            }
        }

        [SerializeField]
        StyleComplexSelector[] m_ComplexSelectors;

        internal StyleComplexSelector[] complexSelectors
        {
            get { return m_ComplexSelectors; }
            set
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
        internal Dimension[] dimensions;

        [SerializeField]
        internal Color[] colors;

        [SerializeField]
        internal string[] strings;

        [SerializeField]
        internal Object[] assets;

        [Serializable]
        internal struct ImportStruct
        {
            public StyleSheet styleSheet;
            public string[] mediaQueries;
        }

        [SerializeField]
        internal ImportStruct[] imports;

        [SerializeField]
        List<StyleSheet> m_FlattenedImportedStyleSheets;
        internal List<StyleSheet> flattenedRecursiveImports
        {
            get { return m_FlattenedImportedStyleSheets; }
        }

        [SerializeField]
        private int m_ContentHash;

        /// <summary>
        /// A hash value computed from the stylesheet content.
        /// </summary>
        public int contentHash
        {
            get { return m_ContentHash; }
            set { m_ContentHash = value; }
        }

        [SerializeField]
        internal ScalableImage[] scalableImages;

        [NonSerialized]
        internal TableType orderedNameSelectors;

        [NonSerialized]
        internal TableType orderedTypeSelectors;

        [NonSerialized]
        internal TableType orderedClassSelectors;

        [NonSerialized]
        internal bool isUnityStyleSheet;

        static string kCustomPropertyMarker = "--";

        static bool TryCheckAccess<T>(T[] list, StyleValueType type, StyleValueHandle handle, out T value)
        {
            bool result = false;
            value = default(T);

            if (handle.valueType == type && handle.valueIndex >= 0 && handle.valueIndex < list.Length)
            {
                value = list[handle.valueIndex];
                result = true;
            }
            else
            {
                Debug.LogErrorFormat("Trying to read value of type {0} while reading a value of type {1}", type, handle.valueType);
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

        internal void FlattenImportedStyleSheetsRecursive()
        {
            m_FlattenedImportedStyleSheets = new List<StyleSheet>();
            FlattenImportedStyleSheetsRecursive(this);
        }

        void FlattenImportedStyleSheetsRecursive(StyleSheet sheet)
        {
            if (sheet.imports == null)
                return;

            for (var i = 0; i < sheet.imports.Length; i++)
            {
                var importedStyleSheet = sheet.imports[i].styleSheet;
                if (importedStyleSheet == null)
                    continue;

                FlattenImportedStyleSheetsRecursive(importedStyleSheet);
                m_FlattenedImportedStyleSheets.Add(importedStyleSheet);
            }
        }

        void SetupReferences()
        {
            if (complexSelectors == null || rules == null)
                return;

            // Setup rules and properties for var
            foreach (var rule in rules)
            {
                foreach (var property in rule.properties)
                {
                    if (CustomStartsWith(property.name, kCustomPropertyMarker))
                    {
                        ++rule.customPropertiesCount;
                        property.isCustomProperty = true;
                    }

                    foreach (var handle in property.values)
                    {
                        if (handle.IsVarFunction())
                        {
                            property.requireVariableResolve = true;
                            break;
                        }
                    }
                }
            }

            for (int i = 0, count = complexSelectors.Length; i < count; i++)
            {
                complexSelectors[i].CachePseudoStateMasks();
            }

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

        internal StyleValueKeyword ReadKeyword(StyleValueHandle handle)
        {
            return (StyleValueKeyword)handle.valueIndex;
        }

        internal float ReadFloat(StyleValueHandle handle)
        {
            // Handle dimension for properties with optional unit
            if (handle.valueType == StyleValueType.Dimension)
            {
                Dimension dimension = CheckAccess(dimensions, StyleValueType.Dimension, handle);
                return dimension.value;
            }
            return CheckAccess(floats, StyleValueType.Float, handle);
        }

        internal bool TryReadFloat(StyleValueHandle handle, out float value)
        {
            if (TryCheckAccess(floats, StyleValueType.Float, handle, out value))
                return true;

            // Handle dimension for properties with optional unit
            Dimension dimensionValue;
            bool isDimension = TryCheckAccess(dimensions, StyleValueType.Float, handle, out dimensionValue);
            value = dimensionValue.value;
            return isDimension;
        }

        internal Dimension ReadDimension(StyleValueHandle handle)
        {
            // If the value is 0 (without unit) it's stored as a float
            if (handle.valueType == StyleValueType.Float)
            {
                float value = CheckAccess(floats, StyleValueType.Float, handle);
                return new Dimension(value, Dimension.Unit.Unitless);
            }
            return CheckAccess(dimensions, StyleValueType.Dimension, handle);
        }

        internal bool TryReadDimension(StyleValueHandle handle, out Dimension value)
        {
            if (TryCheckAccess(dimensions, StyleValueType.Dimension, handle, out value))
                return true;

            // If the value is 0 (without unit) it's stored as a float
            float floatValue = 0f;
            bool isFloat = TryCheckAccess(floats, StyleValueType.Float, handle, out floatValue);
            value = new Dimension(floatValue, Dimension.Unit.Unitless);
            return isFloat;
        }

        internal Color ReadColor(StyleValueHandle handle)
        {
            return CheckAccess(colors, StyleValueType.Color, handle);
        }

        internal bool TryReadColor(StyleValueHandle handle, out Color value)
        {
            return TryCheckAccess(colors, StyleValueType.Color, handle, out value);
        }

        internal string ReadString(StyleValueHandle handle)
        {
            return CheckAccess(strings, StyleValueType.String, handle);
        }

        internal bool TryReadString(StyleValueHandle handle, out string value)
        {
            return TryCheckAccess(strings, StyleValueType.String, handle, out value);
        }

        internal string ReadEnum(StyleValueHandle handle)
        {
            return CheckAccess(strings, StyleValueType.Enum, handle);
        }

        internal bool TryReadEnum(StyleValueHandle handle, out string value)
        {
            return TryCheckAccess(strings, StyleValueType.Enum, handle, out value);
        }

        internal string ReadVariable(StyleValueHandle handle)
        {
            return CheckAccess(strings, StyleValueType.Variable, handle);
        }

        internal bool TryReadVariable(StyleValueHandle handle, out string value)
        {
            return TryCheckAccess(strings, StyleValueType.Variable, handle, out value);
        }

        internal string ReadResourcePath(StyleValueHandle handle)
        {
            return CheckAccess(strings, StyleValueType.ResourcePath, handle);
        }

        internal bool TryReadResourcePath(StyleValueHandle handle, out string value)
        {
            return TryCheckAccess(strings, StyleValueType.ResourcePath, handle, out value);
        }

        internal Object ReadAssetReference(StyleValueHandle handle)
        {
            return CheckAccess(assets, StyleValueType.AssetReference, handle);
        }

        internal string ReadMissingAssetReferenceUrl(StyleValueHandle handle)
        {
            return CheckAccess(strings, StyleValueType.MissingAssetReference, handle);
        }

        internal bool TryReadAssetReference(StyleValueHandle handle, out Object value)
        {
            return TryCheckAccess(assets, StyleValueType.AssetReference, handle, out value);
        }

        internal StyleValueFunction ReadFunction(StyleValueHandle handle)
        {
            return (StyleValueFunction)handle.valueIndex;
        }

        internal string ReadFunctionName(StyleValueHandle handle)
        {
            if (handle.valueType != StyleValueType.Function)
            {
                Debug.LogErrorFormat($"Trying to read value of type {StyleValueType.Function} while reading a value of type {handle.valueType}");
                return string.Empty;
            }

            var svf = (StyleValueFunction)handle.valueIndex;
            return svf.ToUssString();
        }

        internal ScalableImage ReadScalableImage(StyleValueHandle handle)
        {
            return CheckAccess(scalableImages, StyleValueType.ScalableImage, handle);
        }

        private static bool CustomStartsWith(string originalString, string pattern)
        {
            int originalLength = originalString.Length;
            int patternLength = pattern.Length;
            int originalPos = 0;
            int patternPos = 0;

            while (originalPos < originalLength && patternPos < patternLength && originalString[originalPos] == pattern[patternPos])
            {
                originalPos++;
                patternPos++;
            }

            return (patternPos == patternLength && originalLength >= patternLength) || (originalPos == originalLength && patternLength >= originalLength);
        }
    }
}
