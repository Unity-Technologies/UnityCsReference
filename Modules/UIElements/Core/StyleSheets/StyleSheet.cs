// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
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
        [Flags]
        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal enum RebuildOptions
        {
            None,
            Synchronous
        }

        [NonSerialized]
        bool m_RequiresRebuild = true;

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

        internal StyleRule[] rules
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
            get => m_Rules;
        }

        // Only the importer should write to these fields
        // Normal usage should only go through ReadXXX methods
        [SerializeField]
        internal ResourcePath[] resourcePaths = Array.Empty<ResourcePath>();

        [SerializeField]
        internal float[] floats = Array.Empty<float>();

        [SerializeField]
        internal Dimension[] dimensions = Array.Empty<Dimension>();

        [SerializeField]
        internal Color[] colors = Array.Empty<Color>();

        [SerializeField]
        internal string[] strings = Array.Empty<string>();

        // Cached UniqueStyleString ids for strings referenced by var() functions, indexed by
        // string-table index. -1 means "not (yet) referenced by a var()". Populated by
        // SetupReferences so the variable resolver can compare ints without paying
        // UniqueStyleString.TryGet per resolution.
        [NonSerialized]
        private int[] variableNameIds = Array.Empty<int>();

        [SerializeField]
        internal Object[] assets = Array.Empty<Object>();

        [Serializable]
        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal struct ImportStruct
        {
            public StyleSheet styleSheet;
            public string[] mediaQueries;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
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

        [NonSerialized]
        private bool m_IsDefaultStyleSheet;

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
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
            UIElementsUtility.MarkStyleSheetAsLoaded(this);
        }

        internal virtual void OnDisable()
        {
            UIElementsUtility.MarkStyleSheetAsUnloaded(this);
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

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal StyleRule AddRule() => AddRuleAtIndex(-1);

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal StyleRule AddRuleAtIndex(int index) => AddRuleAtIndex(index, null);

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal StyleRule AddRule(string selector) => AddRuleAtIndex(-1, selector);

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal StyleRule AddRuleAtIndex(int index, string selector)
        {
            if (index == -1)
                index = rules.Length;

            var rule = new StyleRule(this);
            if (!string.IsNullOrEmpty(selector))
                rule.AddSelector(selector);
            InsertValueInArray(ref m_Rules, index, rule);
            RequestRebuild();
            return rule;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal bool RemoveRule(StyleRule rule)
        {
            if (rule.styleSheet != this)
                return false;

            var index = Array.IndexOf(m_Rules, rule);
            if (index < 0)
                return false;

            RemoveRule(index);
            return true;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void RemoveRule(int ruleIndex)
        {
            if (ruleIndex < 0 || ruleIndex >= m_Rules.Length)
                throw new ArgumentOutOfRangeException(nameof(ruleIndex));

            var rule = rules[ruleIndex];
            Unity.Collections.CollectionExtensions.RemoveFromArray(ref m_Rules, ruleIndex);

            rule.styleSheet = null;
            RequestRebuild();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void SetRules(StyleRule[] newRules)
        {
            m_Rules = newRules;
            SetupReferences();
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal StyleSheet GetStyleSheetImportAtIndex(int index)
        {
            if (index < 0 || index >= imports.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            return imports[index].styleSheet;
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal void SetStyleSheetImportAtIndex(int index, StyleSheet styleSheet)
        {
            if (index < 0 || index >= imports.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            imports[index].styleSheet = styleSheet;
            RequestRebuild();
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal void AddImportAtIndex(int index, ImportStruct import)
        {
            if (index == -1)
                index = imports.Length;

            InsertValueInArray(ref imports, index, import);
            RequestRebuild();
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal void RemoveImport(int index)
        {
            if (index < 0 || index >= imports.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            var import = imports[index];
            Unity.Collections.CollectionExtensions.RemoveFromArray(ref imports, index);

            import.styleSheet = null;
            RequestRebuild();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal void RequestRebuild(RebuildOptions options = RebuildOptions.None)
        {
            m_RequiresRebuild = true;
            MarkAsChanged();
            if ((options & RebuildOptions.Synchronous) == RebuildOptions.Synchronous)
                RebuildIfNecessary();
        }

        internal void RebuildIfNecessary()
        {
            if (m_RequiresRebuild)
                SetupReferences();
        }

        internal void SetupReferences()
        {
            if (rules == null || rules.Length == 0)
            {
                m_RequiresRebuild = false;
                return;
            }

            // Reset the variable-name id cache for this sheet's strings table. -1 marks
            // entries that aren't referenced as var() names; SetupReferences fills the rest
            // by walking the var() handle triples below.
            if (variableNameIds.Length != strings.Length)
                variableNameIds = new int[strings.Length];
            for (int i = 0; i < variableNameIds.Length; i++)
                variableNameIds[i] = -1;

            var orderInStyleSheet = 0;
            for (var index = 0; index < rules.Length; index++)
            {
                var rule = rules[index];
                rule.styleSheet = this;
                if (rule.complexSelectors == null)
                    continue;

                foreach (var complexSelector in rule.complexSelectors)
                {
                    complexSelector.rule = rule;
                    complexSelector.ruleIndex = index;
                    complexSelector.CachePseudoStateMasks(this);
                    complexSelector.CalculateHashes();
                    complexSelector.orderInStyleSheet = orderInStyleSheet++;
                }

                rule.customPropertiesCount = 0;
                foreach (var property in rule.properties)
                {
                    if (property.isCustomProperty)
                    {
                        ++rule.customPropertiesCount;
                        property.customNameId = new UniqueStyleString(property.name).id;
                    }

                    // A var() function is three flat handles: VarFunction, argCount, name.
                    // Pre-register every var()'d name with UniqueStyleString and cache its id
                    // so the resolver can pass ints to TryFindVariable without a per-call
                    // dictionary lookup.
                    var values = property.values;
                    int i = 0;
                    while (i < values.Length)
                    {
                        if (!values[i].IsVarFunction())
                        {
                            ++i;
                            continue;
                        }

                        property.requireVariableResolve = true;
                        Debug.Assert(i + 2 < values.Length, "Malformed var() triple: argCount or name handle missing");

                        // valueIndex is the slot in this sheet's strings[]; UniqueStyleString.id is a
                        // separate, process-global integer that we cache into the same-sized
                        // variableNameIds[] so the resolver can address it by string-table index.
                        int strIdx = values[i + 2].valueIndex;
                        variableNameIds[strIdx] = new UniqueStyleString(strings[strIdx]).id;

                        i += 3;
                    }
                }
            }
            m_RequiresRebuild = false;
        }

        int AddValueToArray<T>(ref T[] array, T value)
        {
            Unity.Collections.CollectionExtensions.AddToArray(ref array, value);
            MarkAsChanged();
            return array.Length - 1;
        }

        int InsertValueInArray<T>(ref T[] array, int index, T value)
        {
            Unity.Collections.CollectionExtensions.InsertIntoArray(ref array, index, value);
            MarkAsChanged();
            return index;
        }

        internal int AddValue(StyleValueKeyword keyword)
        {
            // Keywords are not stored as a regular enum. Instead, we use the
            // integer value of the keyword as the index.
            MarkAsChanged();
            return (int)keyword;
        }

        internal int AddValue(StyleValueFunction function)
        {
            // Functions are not stored as a regular enum. Instead, we use the
            // integer value of the function as the index.
            MarkAsChanged();
            return (int)function;
        }

        internal int AddValue(float value) => AddValueToArray(ref floats, value);

        internal int AddValue(Dimension value) => AddValueToArray(ref dimensions, value);

        internal int AddValue(Color value) => AddValueToArray(ref colors, value);

        internal int AddValue(ScalableImage value) => AddValueToArray(ref scalableImages, value);

        internal int AddValue(string value) => AddValueToArray(ref strings, value);

        internal int AddValue(ResourcePath value) => AddValueToArray(ref resourcePaths, value);

        internal int AddValue(ResolvedResourcePath value)
        {
            var handle = default(StyleValueHandle);
            WriteResourcePath(ref handle, value);
            return handle.valueIndex;
        }

        internal int AddValue(Object value) => AddValueToArray(ref assets, value);

        internal int AddValue(Enum value)
        {
            var valueStr = StyleSheetUtility.GetEnumExportString(value);
            return AddValueToArray(ref strings, valueStr);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
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

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
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

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
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

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal bool TryReadDimension(StyleValueHandle handle, out Dimension value)
        {
            if (TryCheckAccess(dimensions, StyleValueType.Dimension, handle, out value))
                return true;

            // If the value is 0 (without unit) it's stored as a float
            var isFloat = TryCheckAccess(floats, StyleValueType.Float, handle, out var floatValue);
            value = new Dimension(floatValue, Dimension.Unit.Unitless);
            return isFloat;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal Color ReadColor(StyleValueHandle handle)
        {
            if (handle.valueType == StyleValueType.Enum)
            {
                var colorName = ReadEnum(handle);
                StyleSheetColor.TryGetColor(colorName, out var value);
                return value;
            }

            return CheckAccess(colors, StyleValueType.Color, handle);
        }

        internal bool TryReadColor(StyleValueHandle handle, out Color value)
        {
            if (TryCheckAccess(colors, StyleValueType.Color, handle, out value))
                return true;

            if (TryCheckAccess(strings, StyleValueType.Enum, handle, out var colorName))
                return StyleSheetColor.TryGetColor(colorName, out value);

            value = default;
            return false;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal string ReadString(StyleValueHandle handle)
        {
            return CheckAccess(strings, StyleValueType.String, handle);
        }

        internal bool TryReadString(StyleValueHandle handle, out string value)
        {
            return TryCheckAccess(strings, StyleValueType.String, handle, out value);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
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
            where TEnum : struct, Enum
        {
            var enumStr = ReadEnum(handle);
            return Enum.TryParse(StyleSheetUtility.ConvertDashToHungarian(enumStr), out TEnum value)
                ? value
                : default;
        }

        internal bool TryReadEnum<TEnum>(StyleValueHandle handle, out TEnum value)
            where TEnum : struct, Enum
        {
            if (TryReadEnum(handle, out var enumString) &&
                Enum.TryParse(StyleSheetUtility.ConvertDashToHungarian(enumString), out value))
            {
                return true;
            }

            value = default;
            return false;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal string ReadVariable(StyleValueHandle handle)
        {
            return CheckAccess(strings, StyleValueType.Variable, handle);
        }

        // Returns the UniqueStyleString id pre-cached by SetupReferences for this
        // variable-name handle, or -1 if the slot was never populated (e.g. the sheet
        // never went through SetupReferences, or the handle isn't a Variable handle).
        internal int ReadVariableId(StyleValueHandle handle)
        {
            int idx = handle.valueIndex;
            return idx < variableNameIds.Length ? variableNameIds[idx] : -1;
        }

        internal bool TryReadVariable(StyleValueHandle handle, out string value)
        {
            return TryCheckAccess(strings, StyleValueType.Variable, handle, out value);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal ResolvedResourcePath ReadResourcePath(StyleValueHandle handle)
        {
            var resourcePath = CheckAccess(resourcePaths, StyleValueType.ResourcePath, handle);

            if (!resourcePath.isValid)
                return default;

            var path = strings[resourcePath.pathIndex];
            var subAssetName = resourcePath.subAssetNameIndex >= 0 ? strings[resourcePath.subAssetNameIndex] : null;
            return new ResolvedResourcePath(path, subAssetName);
        }

        internal bool TryReadResourcePath(StyleValueHandle handle, out ResolvedResourcePath value)
        {
            if (TryCheckAccess(resourcePaths, StyleValueType.ResourcePath, handle, out var resourcePath))
            {
                var path = strings[resourcePath.pathIndex];
                var subAssetName = resourcePath.subAssetNameIndex >= 0 ? strings[resourcePath.subAssetNameIndex] : null;
                value = new ResolvedResourcePath(path, subAssetName);
                return true;
            }

            value = default;
            return false;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
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

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
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

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
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

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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

        internal Ratio ReadRatio(StyleValueHandle handle)
        {
            return TryReadRatio(handle, out var ratio) ? ratio : default;
        }

        internal bool TryReadRatio(StyleValueHandle handle, out Ratio ratio)
        {
            if (TryReadEnum(handle, out StyleValueKeyword keyword) && keyword == StyleValueKeyword.Auto)
            {
                ratio = Ratio.Auto();
                return true;
            }

            if (TryReadFloat(handle, out var value))
            {
                ratio = new Ratio(value);
                return true;
            }

            ratio = default;
            return false;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal void WriteKeyword(ref StyleValueHandle handle, StyleValueKeyword value)
        {
            handle.valueType = StyleValueType.Keyword;
            handle.valueIndex = (int)value;
            MarkAsChanged();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
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

            MarkAsChanged();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
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

            MarkAsChanged();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
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

            MarkAsChanged();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
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

            MarkAsChanged();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void WriteEnum<TEnum>(ref StyleValueHandle handle, TEnum value)
            where TEnum : Enum
        {
            var valueStr = StyleSheetUtility.GetEnumExportString(value);
            WriteEnumAsString(ref handle, valueStr);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal void WriteEnumAsString(ref StyleValueHandle handle, string valueStr)
        {
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

            MarkAsChanged();
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

            MarkAsChanged();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void WriteResourcePath(ref StyleValueHandle handle, ResolvedResourcePath resolvedResource)
        {
            if (handle.valueType == StyleValueType.ResourcePath)
            {
                var resourcePathData = resourcePaths[handle.valueIndex];
                strings[resourcePathData.pathIndex] = resolvedResource.path;
                if (!resolvedResource.hasSubAssetName)
                {
                    resourcePathData.subAssetNameIndex = -1;
                }
                else
                {
                    if (resourcePathData.subAssetNameIndex >= 0)
                        strings[resourcePathData.subAssetNameIndex] = resolvedResource.subAssetName;
                    else
                        resourcePathData.subAssetNameIndex = AddValue(resolvedResource.subAssetName);
                }

                resourcePaths[handle.valueIndex] = resourcePathData;
            }
            else
            {
                var valueIndex = AddValue(new ResourcePath
                {
                    pathIndex = AddValue(resolvedResource.path),
                    subAssetNameIndex = resolvedResource.hasSubAssetName ? AddValue(resolvedResource.subAssetName) : -1
                });
                handle.valueType = StyleValueType.ResourcePath;
                handle.valueIndex = valueIndex;
            }

            MarkAsChanged();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
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

            MarkAsChanged();
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

            MarkAsChanged();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void WriteFunction(ref StyleValueHandle handle, StyleValueFunction function)
        {
            handle.valueType = StyleValueType.Function;
            handle.valueIndex = (int)function;
            MarkAsChanged();
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

            MarkAsChanged();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void WriteStylePropertyName(ref StyleValueHandle handle, StylePropertyName propertyName)
        {
            var exportName = propertyName.id != StylePropertyId.Unknown
                ? propertyName.ToString()
                : "ignored";
            if (handle.valueType == StyleValueType.Enum)
            {
                strings[handle.valueIndex] = exportName;
            }
            else
            {
                var valueIndex = AddValue(exportName);
                handle.valueType = StyleValueType.Enum;
                handle.valueIndex = valueIndex;
            }

            MarkAsChanged();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void WriteCommaSeparator(ref StyleValueHandle handle)
        {
            handle.valueIndex = 0;
            handle.valueType = StyleValueType.CommaSeparator;
            MarkAsChanged();
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal void WriteLength(ref StyleValueHandle handle, Length value)
        {
            if (value.IsAuto())
                WriteKeyword(ref handle, StyleValueKeyword.Auto);
            else if (value.IsNone())
                WriteKeyword(ref handle, StyleValueKeyword.None);
            else
                WriteDimension(ref handle, value.ToDimension());
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal void WriteAngle(ref StyleValueHandle handle, Angle value)
        {
            if (value.IsNone())
                WriteKeyword(ref handle, StyleValueKeyword.None);
            else
                WriteDimension(ref handle, value.ToDimension());
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal void WriteTimeValue(ref StyleValueHandle handle, TimeValue value)
        {
            WriteDimension(ref handle, value.ToDimension());
        }

        internal void WriteRatio(ref StyleValueHandle handle, Ratio value)
        {
            if (value.IsAuto())
                WriteKeyword(ref handle, StyleValueKeyword.Auto);
            else
                WriteFloat(ref handle, value.value);
        }

        internal void MarkAsChanged()
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

            UIElementsUtility.MarkStyleSheetAsChanged(this);
        }
    }
}
