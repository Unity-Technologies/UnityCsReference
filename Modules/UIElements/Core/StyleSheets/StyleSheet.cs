// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
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
    [HelpURL("UIE-USS")]
    [Serializable]
    public class StyleSheet : ScriptableObject
    {
        [SerializeField]
        bool m_ImportedWithErrors;

        /// <summary>
        /// Whether there were errors encountered while importing the StyleSheet
        /// </summary>
        public bool importedWithErrors
        {
            get { return m_ImportedWithErrors; }
            internal set { m_ImportedWithErrors = value; }
        }

        [SerializeField]
        bool m_ImportedWithWarnings;

        /// <summary>
        /// Whether there were warnings encountered while importing the StyleSheet
        /// </summary>
        public bool importedWithWarnings
        {
            get { return m_ImportedWithWarnings; }
            internal set { m_ImportedWithWarnings = value; }
        }

        [SerializeField]
        StyleRule[] m_Rules = Array.Empty<StyleRule>();

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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
        StyleComplexSelector[] m_ComplexSelectors = Array.Empty<StyleComplexSelector>();

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        [SerializeField]
        internal float[] floats = Array.Empty<float>();

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        [SerializeField]
        internal Dimension[] dimensions = Array.Empty<Dimension>();

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        [SerializeField]
        internal Color[] colors = Array.Empty<Color>();

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        [SerializeField]
        internal string[] strings = Array.Empty<string>();

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        [SerializeField]
        internal Object[] assets = Array.Empty<Object>();

        [Serializable]
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal struct ImportStruct
        {
            public StyleSheet styleSheet;
            public string[] mediaQueries;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        [SerializeField]
        internal ImportStruct[] imports = Array.Empty<ImportStruct>();

        [SerializeField]
        List<StyleSheet> m_FlattenedImportedStyleSheets = new();
        internal List<StyleSheet> flattenedRecursiveImports
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            get => m_FlattenedImportedStyleSheets;
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
        internal ScalableImage[] scalableImages = Array.Empty<ScalableImage>();

        // This enum is used to retrieve a given "TableType" at specific index in the related array
        internal enum OrderedSelectorType
        {
            None = -1,
            Name = 0,
            Type = 1,
            Class = 2,
            Length = 3 // Used to initialize the array
        }

        [NonSerialized]
        internal TableType[] tables;

        [NonSerialized] internal int nonEmptyTablesMask;

        [NonSerialized] internal StyleComplexSelector firstRootSelector;

        [NonSerialized] internal StyleComplexSelector firstWildCardSelector;

        [NonSerialized]
        private bool m_IsDefaultStyleSheet;

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal bool isDefaultStyleSheet
        {
            get { return m_IsDefaultStyleSheet; }
            set
            {
                m_IsDefaultStyleSheet = value;
                if (flattenedRecursiveImports != null)
                {
                    foreach (var importedStyleSheet in flattenedRecursiveImports)
                    {
                        importedStyleSheet.isDefaultStyleSheet = value;
                    }
                }
            }
        }

        static string kCustomPropertyMarker = "--";

        bool TryCheckAccess<T>(T[] list, StyleValueType type, StyleValueHandle handle, out T value)
        {
            if (handle.valueType != type || handle.valueIndex < 0 || handle.valueIndex >= list.Length)
            {
                value = default(T);
                return false;
            }
            value = list[handle.valueIndex];
            return true;
        }

        T CheckAccess<T>(T[] list, StyleValueType type, StyleValueHandle handle)
        {
            T value = default(T);
            if (handle.valueType != type)
            {
                Debug.LogErrorFormat(this, "Trying to read value of type {0} while reading a value of type {1}", type, handle.valueType);
            }
            else if (list == null || handle.valueIndex < 0 || handle.valueIndex >= list.Length)
            {
                Debug.LogError("Accessing invalid property", this);
            }
            else
            {
                value = list[handle.valueIndex];
            }
            return value;
        }

        internal virtual void OnEnable()
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

                importedStyleSheet.isDefaultStyleSheet = isDefaultStyleSheet;
                FlattenImportedStyleSheetsRecursive(importedStyleSheet);
                m_FlattenedImportedStyleSheets.Add(importedStyleSheet);
            }
        }

        void SetupReferences()
        {
            if (complexSelectors == null || rules == null || (complexSelectors.Length == 0 && rules.Length == 0))
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
                complexSelectors[i].CachePseudoStateMasks(this);
            }

            tables = new TableType[(int)OrderedSelectorType.Length];
            tables[(int)OrderedSelectorType.Name] = new TableType(StringComparer.Ordinal);
            tables[(int)OrderedSelectorType.Type] = new TableType(StringComparer.Ordinal);
            tables[(int)OrderedSelectorType.Class] = new TableType(StringComparer.Ordinal);

            nonEmptyTablesMask = 0;

            firstRootSelector = null;
            firstWildCardSelector = null;

            for (int i = 0; i < complexSelectors.Length; i++)
            {
                // Here we set-up runtime-only pointers
                StyleComplexSelector complexSel = complexSelectors[i];

                if (complexSel.ruleIndex < rules.Length)
                {
                    complexSel.rule = rules[complexSel.ruleIndex];
                }

                complexSel.CalculateHashes();

                complexSel.orderInStyleSheet = i;

                StyleSelector lastSelector = complexSel.selectors[complexSel.selectors.Length - 1];
                StyleSelectorPart part = lastSelector.parts[0];

                string key = part.value;

                OrderedSelectorType tableToUse = OrderedSelectorType.None;

                switch (part.type)
                {
                    case StyleSelectorType.Class:
                        tableToUse = OrderedSelectorType.Class;
                        break;
                    case StyleSelectorType.ID:
                        tableToUse = OrderedSelectorType.Name;
                        break;
                    case StyleSelectorType.Type:
                        key = part.value;
                        tableToUse = OrderedSelectorType.Type;
                        break;

                    case StyleSelectorType.Wildcard:
                        if (firstWildCardSelector != null)
                            complexSel.nextInTable = firstWildCardSelector;
                        firstWildCardSelector = complexSel;
                        break;

                    case StyleSelectorType.PseudoClass:
                        // :root selector are put separately because they apply to very few elements
                        if ((lastSelector.pseudoStateMask & (int)PseudoStates.Root) != 0)
                        {
                            if (firstRootSelector != null)
                                complexSel.nextInTable = firstRootSelector;
                            firstRootSelector = complexSel;
                        }
                        // in this case we assume a wildcard selector
                        // since a selector such as ":selected" applies to all elements
                        else
                        {
                            if (firstWildCardSelector != null)
                                complexSel.nextInTable = firstWildCardSelector;
                            firstWildCardSelector = complexSel;
                        }
                        break;
                    default:
                        Debug.LogError($"Invalid first part type {part.type}", this);
                        break;
                }

                if (tableToUse != OrderedSelectorType.None)
                {
                    StyleComplexSelector previous;
                    TableType table = tables[(int)tableToUse];
                    if (table.TryGetValue(key, out previous))
                    {
                        complexSel.nextInTable = previous;
                    }
                    nonEmptyTablesMask |= (1 << (int)tableToUse);
                    table[key] = complexSel;
                }
            }
        }

        int AddValueToArray<T>(ref T[] array, T value)
        {
            Unity.Collections.CollectionExtensions.AddToArray(ref array, value);
            SetTemporaryContentHash();
            return array.Length - 1;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal int AddValue(StyleValueKeyword keyword)
        {
            // Keywords are not stored as a regular enum. Instead, we use the
            // integer value of the keyword as the index.
            SetTemporaryContentHash();
            return (int)keyword;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal int AddValue(StyleValueFunction function)
        {
            // Functions are not stored as a regular enum. Instead, we use the
            // integer value of the function as the index.
            SetTemporaryContentHash();
            return (int)function;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal int AddValue(float value) => AddValueToArray(ref floats, value);

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal int AddValue(Dimension value) => AddValueToArray(ref dimensions, value);

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal int AddValue(Color value) => AddValueToArray(ref colors, value);

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal int AddValue(ScalableImage value) => AddValueToArray(ref scalableImages, value);

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal int AddValue(string value) => AddValueToArray(ref strings, value);

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal int AddValue(Object value) => AddValueToArray(ref assets, value);

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal int AddValue(Enum value)
        {
            var valueStr = StyleSheetUtility.GetEnumExportString(value);
            return AddValueToArray(ref strings, valueStr);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal StyleValueKeyword ReadKeyword(StyleValueHandle handle)
        {
            return (StyleValueKeyword)handle.valueIndex;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal bool TryReadKeyword(StyleValueHandle handle, out StyleValueKeyword value)
        {
            value = (StyleValueKeyword)handle.valueIndex;
            return handle.valueType == StyleValueType.Keyword;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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
            var isDimension = TryCheckAccess(dimensions, StyleValueType.Float, handle, out var dimensionValue);
            value = dimensionValue.value;
            return isDimension;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal Dimension ReadDimension(StyleValueHandle handle)
        {
            // If the value is 0 (without unit) it's stored as a float
            if (handle.valueType == StyleValueType.Float)
            {
                var value = CheckAccess(floats, StyleValueType.Float, handle);
                return new Dimension(value, Dimension.Unit.Unitless);
            }
            return CheckAccess(dimensions, StyleValueType.Dimension, handle);
        }

        internal bool TryReadDimension(StyleValueHandle handle, out Dimension value)
        {
            if (TryCheckAccess(dimensions, StyleValueType.Dimension, handle, out value))
                return true;

            // If the value is 0 (without unit) it's stored as a float
            var isFloat = TryCheckAccess(floats, StyleValueType.Float, handle, out var floatValue);
            value = new Dimension(floatValue, Dimension.Unit.Unitless);
            return isFloat;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal Color ReadColor(StyleValueHandle handle)
        {
            if (handle.valueType == StyleValueType.Enum)
            {
                var colorName = ReadEnum(handle);
                StyleSheetColor.TryGetColor(colorName.ToLowerInvariant(), out var value);
                return value;
            }

            return CheckAccess(colors, StyleValueType.Color, handle);
        }

        internal bool TryReadColor(StyleValueHandle handle, out Color value)
        {
            if (TryCheckAccess(colors, StyleValueType.Color, handle, out value))
                return true;

            if (TryCheckAccess(strings, StyleValueType.Enum, handle, out var colorName))
                return StyleSheetColor.TryGetColor(colorName.ToLowerInvariant(), out value);

            value = default;
            return false;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal string ReadString(StyleValueHandle handle)
        {
            return CheckAccess(strings, StyleValueType.String, handle);
        }

        internal bool TryReadString(StyleValueHandle handle, out string value)
        {
            return TryCheckAccess(strings, StyleValueType.String, handle, out value);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal string ReadEnum(StyleValueHandle handle)
        {
            return CheckAccess(strings, StyleValueType.Enum, handle);
        }

        internal bool TryReadEnum(StyleValueHandle handle, out string value)
        {
            return TryCheckAccess(strings, StyleValueType.Enum, handle, out value);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal TEnum ReadEnum<TEnum>(StyleValueHandle handle)
            where TEnum: struct, Enum
        {
            var enumStr = ReadEnum(handle);
            return Enum.TryParse(StyleSheetUtility.ConvertDashToHungarian(enumStr), out TEnum value)
                ? value
                : default;
        }

        internal bool TryReadEnum<TEnum>(StyleValueHandle handle, out TEnum value)
            where TEnum: struct, Enum
        {
            if (TryReadEnum(handle, out var enumString) &&
                Enum.TryParse(StyleSheetUtility.ConvertDashToHungarian(enumString), out value))
            {
                return true;
            }

            value = default;
            return false;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal string ReadVariable(StyleValueHandle handle)
        {
            return CheckAccess(strings, StyleValueType.Variable, handle);
        }

        internal bool TryReadVariable(StyleValueHandle handle, out string value)
        {
            return TryCheckAccess(strings, StyleValueType.Variable, handle, out value);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal string ReadResourcePath(StyleValueHandle handle)
        {
            return CheckAccess(strings, StyleValueType.ResourcePath, handle);
        }

        internal bool TryReadResourcePath(StyleValueHandle handle, out string value)
        {
            return TryCheckAccess(strings, StyleValueType.ResourcePath, handle, out value);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal Object ReadAssetReference(StyleValueHandle handle)
        {
            return CheckAccess(assets, StyleValueType.AssetReference, handle);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal string ReadMissingAssetReferenceUrl(StyleValueHandle handle)
        {
            return CheckAccess(strings, StyleValueType.MissingAssetReference, handle);
        }

        internal bool TryReadMissingAssetReferenceUrl(StyleValueHandle handle, out string value)
        {
            return TryCheckAccess(strings, StyleValueType.MissingAssetReference, handle, out value);
        }

        internal bool TryReadAssetReference(StyleValueHandle handle, out Object value)
        {
            return TryCheckAccess(assets, StyleValueType.AssetReference, handle, out value);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal StyleValueFunction ReadFunction(StyleValueHandle handle)
        {
            return (StyleValueFunction)handle.valueIndex;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal bool TryReadFunction(StyleValueHandle handle, out StyleValueFunction value)
        {
            value = (StyleValueFunction)handle.valueIndex;
            return handle.valueType == StyleValueType.Function;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal string ReadFunctionName(StyleValueHandle handle)
        {
            if (handle.valueType != StyleValueType.Function)
            {
                Debug.LogErrorFormat(this, $"Trying to read value of type {StyleValueType.Function} while reading a value of type {handle.valueType}");
                return string.Empty;
            }

            var svf = (StyleValueFunction)handle.valueIndex;
            return svf.ToUssString();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal ScalableImage ReadScalableImage(StyleValueHandle handle)
        {
            return CheckAccess(scalableImages, StyleValueType.ScalableImage, handle);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal bool TryReadScalableImage(StyleValueHandle handle, out ScalableImage value)
        {
            return TryCheckAccess(scalableImages, StyleValueType.ScalableImage, handle, out value);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal StylePropertyName ReadStylePropertyName(StyleValueHandle handle)
        {
            return new StylePropertyName(CheckAccess(strings, StyleValueType.Enum, handle));
        }

        internal bool TryReadStylePropertyName(StyleValueHandle handle, out StylePropertyName value)
        {
            if (TryCheckAccess(strings, StyleValueType.Enum, handle, out var strValue))
            {
                value = new StylePropertyName(strValue);
                return true;
            }

            value = default;
            return false;
        }

        internal Length ReadLength(StyleValueHandle handle)
        {
            if (handle.valueType == StyleValueType.Keyword)
            {
                var keyword = ReadKeyword(handle);
                return keyword switch
                {
                    StyleValueKeyword.Auto => Length.Auto(),
                    StyleValueKeyword.None => Length.None(),
                    _ => default
                };
            }
            var dimension = ReadDimension(handle);
            return dimension.IsLength()
                ? dimension.ToLength()
                : default;
        }

        internal bool TryReadLength(StyleValueHandle handle, out Length value)
        {
            if (TryReadKeyword(handle, out var keyword))
            {
                switch (keyword)
                {
                    case StyleValueKeyword.Auto:
                        value = Length.Auto();
                        return true;
                    case StyleValueKeyword.None:
                        value = Length.None();
                        return true;
                    default:
                        value = default;
                        return false;
                }
            }

            if (TryReadDimension(handle, out var dimension) && dimension.IsLength())
            {
                value = dimension.ToLength();
                return true;
            }
            value = default;
            return false;
        }

        internal Angle ReadAngle(StyleValueHandle handle)
        {
            if (handle.valueType == StyleValueType.Keyword)
            {
                var keyword = ReadKeyword(handle);
                return keyword switch
                {
                    StyleValueKeyword.None => Angle.None(),
                    _ => default
                };
            }
            var dimension = ReadDimension(handle);
            return dimension.IsAngle()
                ? dimension.ToAngle()
                : default;
        }

        internal bool TryReadAngle(StyleValueHandle handle, out Angle value)
        {
            if (TryReadKeyword(handle, out var keyword))
            {
                switch (keyword)
                {
                    case StyleValueKeyword.None:
                        value = Angle.None();
                        return true;
                    default:
                        value = default;
                        return false;
                }
            }

            if (TryReadDimension(handle, out var dimension) && dimension.IsAngle())
            {
                value = dimension.ToAngle();
                return true;
            }
            value = default;
            return false;
        }

        internal TimeValue ReadTimeValue(StyleValueHandle handle)
        {
            var dimension = ReadDimension(handle);
            return dimension.IsTimeValue()
                ? dimension.ToTime()
                : default;
        }

        internal bool TryReadTimeValue(StyleValueHandle handle, out TimeValue value)
        {
            if (TryReadDimension(handle, out var dimension) && dimension.IsTimeValue())
            {
                value = dimension.ToTime();
                return true;
            }
            value = default;
            return false;
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

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void WriteKeyword(ref StyleValueHandle handle, StyleValueKeyword value)
        {
            handle.valueType = StyleValueType.Keyword;
            handle.valueIndex = (int)value;
            SetTemporaryContentHash();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void WriteFloat(ref StyleValueHandle handle, float value)
        {
            if (handle.valueType == StyleValueType.Float)
            {
                floats[handle.valueIndex] = value;
            }
            else
            {
                var valueIndex = AddValue(value);
                handle.valueType = StyleValueType.Float;
                handle.valueIndex = valueIndex;
            }
            SetTemporaryContentHash();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void WriteDimension(ref StyleValueHandle handle, Dimension dimension)
        {
            if (handle.valueType == StyleValueType.Dimension)
            {
                dimensions[handle.valueIndex] = dimension;
            }
            else
            {
                var valueIndex = AddValue(dimension);
                handle.valueType = StyleValueType.Dimension;
                handle.valueIndex = valueIndex;
            }
            SetTemporaryContentHash();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void WriteColor(ref StyleValueHandle handle, Color color)
        {
            if (handle.valueType == StyleValueType.Color)
            {
                colors[handle.valueIndex] = color;
            }
            else
            {
                var valueIndex = AddValue(color);
                handle.valueType = StyleValueType.Color;
                handle.valueIndex = valueIndex;
            }

            SetTemporaryContentHash();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void WriteString(ref StyleValueHandle handle, string value)
        {
            if (handle.valueType == StyleValueType.String)
            {
                strings[handle.valueIndex] = value;
            }
            else
            {
                var valueIndex = AddValue(value);
                handle.valueType = StyleValueType.String;
                handle.valueIndex = valueIndex;
            }
            SetTemporaryContentHash();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void WriteEnum<TEnum>(ref StyleValueHandle handle, TEnum value)
            where TEnum : Enum
        {
            var valueStr = StyleSheetUtility.GetEnumExportString(value);
            if (handle.valueType == StyleValueType.Enum)
            {
                strings[handle.valueIndex] = valueStr;
            }
            else
            {
                var valueIndex = AddValue(valueStr);
                handle.valueType = StyleValueType.Enum;
                handle.valueIndex = valueIndex;
            }
            SetTemporaryContentHash();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void WriteVariable(ref StyleValueHandle handle, string variableName)
        {
            if (handle.valueType == StyleValueType.Variable)
            {
                strings[handle.valueIndex] = variableName;
            }
            else
            {
                var valueIndex = AddValue(variableName);
                handle.valueType = StyleValueType.Variable;
                handle.valueIndex = valueIndex;
            }
            SetTemporaryContentHash();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void WriteResourcePath(ref StyleValueHandle handle, string resourcePath)
        {
            if (handle.valueType == StyleValueType.ResourcePath)
            {
                strings[handle.valueIndex] = resourcePath;
            }
            else
            {
                var valueIndex = AddValue(resourcePath);
                handle.valueType = StyleValueType.ResourcePath;
                handle.valueIndex = valueIndex;
            }
            SetTemporaryContentHash();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void WriteAssetReference(ref StyleValueHandle handle, Object value)
        {
            if (handle.valueType == StyleValueType.AssetReference)
            {
                assets[handle.valueIndex] = value;
            }
            else
            {
                var valueIndex = AddValue(value);
                handle.valueType = StyleValueType.AssetReference;
                handle.valueIndex = valueIndex;
            }
            SetTemporaryContentHash();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void WriteMissingAssetReferenceUrl(ref StyleValueHandle handle, string assetReference)
        {
            if (handle.valueType == StyleValueType.MissingAssetReference)
            {
                strings[handle.valueIndex] = assetReference;
            }
            else
            {
                var valueIndex = AddValue(assetReference);
                handle.valueType = StyleValueType.MissingAssetReference;
                handle.valueIndex = valueIndex;
            }
            SetTemporaryContentHash();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void WriteFunction(ref StyleValueHandle handle, StyleValueFunction function)
        {
            handle.valueType = StyleValueType.Function;
            handle.valueIndex = (int)function;
            SetTemporaryContentHash();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void WriteScalableImage(ref StyleValueHandle handle, ScalableImage scalableImage)
        {
            if (handle.valueType == StyleValueType.ScalableImage)
            {
                scalableImages[handle.valueIndex] = scalableImage;
            }
            else
            {
                var valueIndex = AddValue(scalableImage);
                handle.valueType = StyleValueType.ScalableImage;
                handle.valueIndex = valueIndex;
            }
            SetTemporaryContentHash();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void WriteStylePropertyName(ref StyleValueHandle handle, StylePropertyName propertyName)
        {
            if (handle.valueType == StyleValueType.Enum)
            {
                strings[handle.valueIndex] = propertyName.ToString();
            }
            else
            {
                var valueIndex = AddValue(propertyName.ToString());
                handle.valueType = StyleValueType.Enum;
                handle.valueIndex = valueIndex;
            }
            SetTemporaryContentHash();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void WriteCommaSeparator(ref StyleValueHandle handle)
        {
            handle.valueIndex = 0;
            handle.valueType = StyleValueType.CommaSeparator;
            SetTemporaryContentHash();
        }

        internal void WriteLength(ref StyleValueHandle handle, Length value)
        {
            if (value.IsAuto())
                WriteKeyword(ref handle, StyleValueKeyword.Auto);
            else if (value.IsNone())
                WriteKeyword(ref handle, StyleValueKeyword.None);
            else
                WriteDimension(ref handle, value.ToDimension());
        }

        internal void WriteAngle(ref StyleValueHandle handle, Angle value)
        {
            if (value.IsNone())
                WriteKeyword(ref handle, StyleValueKeyword.None);
            else
                WriteDimension(ref handle, value.ToDimension());
        }

        internal void WriteTimeValue(ref StyleValueHandle handle, TimeValue value)
        {
            WriteDimension(ref handle, value.ToDimension());
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void SetTemporaryContentHash()
        {
            // Set the contentHash to 0 if the style sheet is empty
            if (rules == null || rules.Length == 0)
                contentHash = 0;
            else
                // Use a random value instead of computing the real contentHash.
                // This is faster (for large content) and safe enough to avoid conflicts with other style sheets
                // since contentHash is used internally as a optimized way to compare style sheets.
                // However, note that the real contentHash will still be computed on import.
                contentHash = UnityEngine.Random.Range(1, int.MaxValue);
        }
    }
}
