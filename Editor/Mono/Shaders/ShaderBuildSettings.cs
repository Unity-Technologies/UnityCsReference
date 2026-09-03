// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace UnityEditor.Shaders
{
    [NativeHeader("Modules/ShaderBuildSettingsEditor/Native/ShaderBuildSettings.h")]
    [StaticAccessor("ShaderBuildSettingsScripting", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode (GenerateProxy = false)]
    [Serializable]
    public struct ShaderBuildSettings
    {
        public enum ShaderVariantGenerationMode
        {
            Default,
            MaterialUsageBasedVariants,
            AllVariants,
            SingleVariantWithDynamicBranching
        }

        [UsedByNativeCode]
        internal enum ShaderCompilerToolchain
        {
            Default,
            FXC,
            DXC
        }

        [UsedByNativeCode]
        internal enum ShaderOptimizationLevel
        {
            Default,
            Disabled,
            Low,
            Medium,
            High
        }

        internal static bool IsEmptyKeyword(string keyword)
        {
            if (keyword.Length == 0)
                return false;

            bool isEmptyKeyword = true;
            foreach (var c in keyword)
            {
                if (!c.Equals('_'))
                    isEmptyKeyword = false;
            }
            return isEmptyKeyword;
        }

        static bool ArrayValuesEqual<T>(T[] lhs, T[] rhs, Func<T, T, bool> elementsEqual)
        {
            int lhsLength = lhs != null ? lhs.Length : 0;
            int rhsLength = rhs != null ? rhs.Length : 0;
            if (lhsLength != rhsLength)
                return false;

            for (int i = 0; i < lhsLength; ++i)
            {
                if (!elementsEqual(lhs[i], rhs[i]))
                    return false;
            }

            return true;
        }

        private static bool IsValidIdentifierChar(char c)
        {
            return ('A' <= c && c <= 'Z') || ('a' <= c && c <= 'z')
                || ('0' <= c && c <= '9') || (c == '_');
        }

        private static bool IsValidIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return false;

            bool firstChar = true;
            foreach (char c in identifier)
            {
                if (firstChar)
                {
                    if (char.IsDigit(c))
                        return false;

                    firstChar = false;
                }

                if (!IsValidIdentifierChar(c))
                    return false;
            }
            return true;
        }

        [RequiredByNativeCode(GenerateProxy = false)]
        [Serializable]
        public struct KeywordOverrideInfo
        {
            public KeywordOverrideInfo(string name, bool keepInBuild)
            {
                this.name = name;
                this.keepInBuild = keepInBuild;
            }

            public bool IsValid()
            {
                return IsValidIdentifier(name);
            }

            [SerializeField] public string name;
            [SerializeField] public bool keepInBuild;

            internal bool ValueEquals(KeywordOverrideInfo other)
            {
                return name == other.name && keepInBuild == other.keepInBuild;
            }

            [UsedByNativeCode, RequiredMember]
            internal static void DeconstructKeywordOverrideInfoArrayElementRaw(KeywordOverrideInfo[] array, int index, out string name, out bool keepInBuild)
            {
                ref KeywordOverrideInfo tmp = ref array[index];
                name = tmp.name;
                keepInBuild = tmp.keepInBuild;
            }

            [UsedByNativeCode, RequiredMember]
            internal static void ReconstructKeywordOverrideInfoArrayElementRaw(KeywordOverrideInfo[] array, int index, string name, bool keepInBuild)
            {
                ref KeywordOverrideInfo tmp = ref array[index];
                tmp.name = name;
                tmp.keepInBuild = keepInBuild;
            }
        }

        [RequiredByNativeCode (GenerateProxy = false)]
        [Serializable]
        public struct KeywordDeclarationOverride
        {
            public KeywordDeclarationOverride() {}

            public bool IsValid(out string msg)
            {
                // Array validity
                if (keywords == null || keywords.Length == 0)
                {
                    msg = "Keyword declaration override cannot be empty.";
                    return false;
                }

                // Individual keyword validity
                foreach (var kw in keywords)
                {
                    if (!kw.IsValid())
                    {
                        string name = kw.name != null ? "'" + kw.name + "'" : "<null>";
                        msg = name + " is not a valid keyword.";
                        return false;
                    }
                }

                // Duplicate detection
                for (int i = 0, n = keywords.Length; i < n; ++i)
                {
                    bool isEmptyKeyword = IsEmptyKeyword(keywords[i].name);

                    for (int j = i + 1; j < n; ++j)
                    {
                        if ((keywords[i].name == keywords[j].name) ||
                            (isEmptyKeyword && IsEmptyKeyword(keywords[j].name)))
                        {
                            msg = "Duplicate keywords: " + keywords[i].name;
                            return false;
                        }
                    }
                }

                msg = string.Empty;
                return true;
            }

            internal bool FindMatchingKeyword(string keyword, out KeywordOverrideInfo foundElement)
            {
                if (keyword.Length > 0 && keywords != null)
                {
                    bool isEmptyKeyword = IsEmptyKeyword(keyword);

                    foreach (var k in keywords)
                    {
                        if ((k.name == keyword) ||
                            (isEmptyKeyword && IsEmptyKeyword(k.name)))
                        {
                            foundElement = k;
                            return true;
                        }
                    }
                }

                foundElement = new KeywordOverrideInfo();
                return false;
            }

            internal bool EqualKeywords(KeywordDeclarationOverride other)
            {
                if (keywords == null || other.keywords == null)
                {
                    return keywords == other.keywords;
                }

                if (keywords.Length != other.keywords.Length)
                    return false;

                for (int i = 0, n = keywords.Length; i < n; ++i)
                {
                    if (!other.FindMatchingKeyword(keywords[i].name, out _))
                        return false;
                }

                return true;
            }

            internal bool ValueEquals(KeywordDeclarationOverride other)
            {
                if (variantGenerationMode != other.variantGenerationMode)
                    return false;

                return ArrayValuesEqual(keywords, other.keywords, static (a, b) => a.ValueEquals(b));
            }

            [SerializeField] public KeywordOverrideInfo[] keywords = Array.Empty<KeywordOverrideInfo>();
            [SerializeField] public ShaderVariantGenerationMode variantGenerationMode = ShaderVariantGenerationMode.Default;

            [UsedByNativeCode, RequiredMember]
            internal static void DeconstructKeywordDeclarationOverrideArrayElementRaw(KeywordDeclarationOverride[] array, int index, out KeywordOverrideInfo[] keywords, out int variantGenerationMode)
            {
                ref KeywordDeclarationOverride tmp = ref array[index];
                keywords = tmp.keywords;
                variantGenerationMode = (int)tmp.variantGenerationMode;
            }

            [UsedByNativeCode, RequiredMember]
            internal static void ReconstructKeywordDeclarationOverrideArrayElementRaw(KeywordDeclarationOverride[] array, int index, KeywordOverrideInfo[] keywords, int variantGenerationMode)
            {
                ref KeywordDeclarationOverride tmp = ref array[index];
                tmp.keywords = keywords;
                tmp.variantGenerationMode = (ShaderVariantGenerationMode)variantGenerationMode;
            }
        }

        // Per-API toolchain (backend) selection plus its debug-symbol and optimization-level settings.
        [RequiredByNativeCode(GenerateProxy = false)]
        [Serializable]
        internal struct ShaderCompilerSettings
        {
            [SerializeField] public GraphicsDeviceType graphicsAPI;
            [SerializeField] public ShaderCompilerToolchain compilerToolchainOverride;
            [SerializeField] public ShaderOptimizationLevel optimizationLevel;
            [SerializeField] public bool enableDebugSymbols;

            internal bool ValueEquals(ShaderCompilerSettings other)
            {
                return graphicsAPI == other.graphicsAPI
                    && compilerToolchainOverride == other.compilerToolchainOverride
                    && optimizationLevel == other.optimizationLevel
                    && enableDebugSymbols == other.enableDebugSymbols;
            }

            [UsedByNativeCode, RequiredMember]
            internal static void DeconstructCompilerSettingsArrayElementRaw(ShaderCompilerSettings[] array, int index, out int graphicsAPI, out int compiler, out int optimizationLevel, out bool enableDebugSymbols)
            {
                ref ShaderCompilerSettings tmp = ref array[index];
                graphicsAPI = (int)tmp.graphicsAPI;
                compiler = (int)tmp.compilerToolchainOverride;
                optimizationLevel = (int)tmp.optimizationLevel;
                enableDebugSymbols = tmp.enableDebugSymbols;
            }

            [UsedByNativeCode, RequiredMember]
            internal static void ReconstructCompilerSettingsArrayElementRaw(ShaderCompilerSettings[] array, int index, int graphicsAPI, int compiler, int optimizationLevel, bool enableDebugSymbols)
            {
                ref ShaderCompilerSettings tmp = ref array[index];
                tmp.graphicsAPI = (GraphicsDeviceType)graphicsAPI;
                tmp.compilerToolchainOverride = (ShaderCompilerToolchain)compiler;
                tmp.optimizationLevel = (ShaderOptimizationLevel)optimizationLevel;
                tmp.enableDebugSymbols = enableDebugSymbols;
            }
        }

        public static bool ValidateKeywordDeclarationOverrides(KeywordDeclarationOverride[] overrides, out string msg)
        {
            if (overrides == null)
            {
                msg = "Null keyword declaration override array.";
                return false;
            }

            for (int i = 0, n = overrides.Length; i < n; ++i)
            {
                if (!overrides[i].IsValid(out msg))
                {
                    msg = "Invalid keyword declaration override at index " + i + ": " + msg;
                    return false;
                }
            }

            for (int i = 0, n = overrides.Length; i < n; ++i)
            {
                for (int j = i + 1; j < n; ++j)
                {
                    if (overrides[i].EqualKeywords(overrides[j]))
                    {
                        msg = "Duplicate keyword declaration overrides at indices " + i + " and " + j;
                        return false;
                    }
                }
            }

            msg = string.Empty;
            return true;
        }

        public ShaderBuildSettings() {}

        [SerializeField] internal KeywordDeclarationOverride[] keywordDeclarationOverrides = Array.Empty<KeywordDeclarationOverride>();
        public KeywordDeclarationOverride[] KeywordDeclarationOverrides
        {
            set
            {
                string msg;
                if (ValidateKeywordDeclarationOverrides(value, out msg))
                    keywordDeclarationOverrides = value;
                else
                    throw new ArgumentException(msg);
            }
        }

        public KeywordDeclarationOverride[] GetKeywordDeclarationOverridesCopy()
        {
            return (KeywordDeclarationOverride[])keywordDeclarationOverrides.Clone();
        }

        [SerializeField] internal string[] defines = Array.Empty<string>();
        [SerializeField] private uint numInternalDefines = 0;

        internal string[] GetAllDefinesCopy()
        {
            return (string[])defines.Clone();
        }

        internal uint GetNumInternalDefines()
        {
            return numInternalDefines;
        }

        internal bool ValueEquals(ShaderBuildSettings other)
        {
            if (numInternalDefines != other.numInternalDefines)
                return false;

            if (!ArrayValuesEqual(defines, other.defines, static (a, b) => a == b))
                return false;

            if (!ArrayValuesEqual(compilerSettings, other.compilerSettings, static (a, b) => a.ValueEquals(b)))
                return false;

            return ArrayValuesEqual(keywordDeclarationOverrides, other.keywordDeclarationOverrides,
                static (a, b) => a.ValueEquals(b));
        }

        internal void AddInternalDefine(string define)
        {
            int numDefines = 1;
            if (defines != null)
                numDefines += defines.Length;
            var defineList = new List<string>(numDefines);
            defineList.Add(define); // keep internal defines at the start of the array
            if (defines != null && defines.Length > 0)
                defineList.AddRange(defines);
            defines = defineList.ToArray();
            numInternalDefines++;
        }

        internal static bool SplitAndValidateDefine(string define, out string identifier, out string value, out string msg)
        {
            identifier = "";
            value = "";

            if (string.IsNullOrWhiteSpace(define))
            {
                msg = "Invalid empty define. Use identifier and numeric value pair separated with a whitespace.";
                return false;
            }

            var sections = define.Split((char[])null, StringSplitOptions.RemoveEmptyEntries); // null catches all whitespace variants

            if (sections.Length != 2)
            {
                msg = "Invalid define '" + define + "'. Use identifier and numeric value pair separated with a whitespace.";
                return false;
            }

            if (!IsValidIdentifier(sections[0]))
            {
                msg = "Invalid define: '" + define + "'. Please follow HLSL identifier rules.";
                return false;
            }

            string val = sections[1];
            char c = val[val.Length - 1];

            // Check the valid postfix chars
            if (c == 'h' || c == 'H' || c == 'f' || c == 'F' || c == 'u' || c == 'U' || c == 'l' ||  c == 'L')
            {
                // TODO @ SHADERS-1314: Uncomment below to accept UL suffix variations when the preprocessor
                // has been fixed to support them.
                /*
                char c2 = val[val.Length - 2];

                // Check also valid UL combinations
                if (((c == 'u' || c == 'U') && (c2 == 'l' || c2 == 'L')) ||
                    ((c == 'l' || c == 'L') && (c2 == 'u' || c2 == 'U')))
                {
                    val = sections[1].Substring(0, val.Length - 2);
                }
                else*/
                {
                    val = sections[1].Substring(0, val.Length - 1);
                }
            }

            // Try parsing as numeric value. Integer style first.
            long longValue;
            if (!long.TryParse(val, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out longValue))
            {
                // Next floating point
                NumberStyles floatStyles = NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign;
                decimal decimalValue;
                if (!decimal.TryParse(val, floatStyles, CultureInfo.InvariantCulture, out decimalValue))
                {
                    // Last hex format. TryParse does not accept the prefix so we parse it manually.
                    if(!(val.StartsWith("0x", StringComparison.OrdinalIgnoreCase) && long.TryParse(val.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out longValue)))
                    {
                        // If the value was none of the above formats we return a validation error.
                        msg = "Invalid define: '" + define + "'. Only numeric values are allowed.";
                        return false;
                    }
                }
            }

            identifier = sections[0];
            value = sections[1];
            msg = string.Empty;
            return true;
        }

        internal static bool ValidateDefinesInternal(string[] defines, uint numInternalDefines, out string msg)
        {
            for (int i = 0, n = defines.Length; i < n; ++i)
            {
                string identifier;
                string value;

                // Define syntax validity checks
                if (!SplitAndValidateDefine(defines[i], out identifier, out value, out msg))
                {
                    return false;
                }

                // Duplication checks
                string nameWithSpace = identifier + " ";
                for (int j = 0; j < i; ++j)
                {
                    if (defines[j].TrimStart().StartsWith(nameWithSpace))
                    {
                        if (i >= numInternalDefines && j >= numInternalDefines)
                            msg = "Duplicate definitions of '" + identifier + "'.";
                        else
                            msg = "Cannot redefine a built-in define '" + identifier + "'.";

                        return false;
                    }
                }
            }

            msg = string.Empty;
            return true;
        }

        public static bool ValidateDefines(string[] defines, out string msg)
        {
            return ValidateDefinesInternal(defines, 0, out msg);
        }

        public string[] Defines
        {
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                string[] newDefines = new string[value.Length + numInternalDefines];
                if (numInternalDefines > 0)
                {
                    Array.Copy(defines, newDefines, numInternalDefines);
                }
                Array.Copy(value, 0, newDefines, numInternalDefines, value.Length);

                string msg;
                if (ValidateDefinesInternal(newDefines, numInternalDefines, out msg))
                    defines = newDefines;
                else
                    throw new ArgumentException(msg);

            }
        }

        public string[] GetDefinesCopy()
        {
            string[] defineArray = new string[defines.Length - numInternalDefines];
            Array.Copy(defines, numInternalDefines, defineArray, 0, defineArray.Length);
            return defineArray;
        }

        static bool IsToolchainSupportedForAPI(GraphicsDeviceType api, ShaderCompilerToolchain toolchain, int index, out string msg)
        {
            ShaderCompilerToolchain[] supported = GetSupportedCompilerToolchainsForAPI(api);
            if (Array.IndexOf(supported, toolchain) == -1)
            {
                msg = "Compiler '" + toolchain + "' is not supported for graphics API '"
                    + api + "' at index " + index + ".";
                return false;
            }
            msg = string.Empty;
            return true;
        }

        static bool IsValidGraphicsAPI(GraphicsDeviceType api, int index, out string msg)
        {
            if (api == GraphicsDeviceType.Null || !Enum.IsDefined(typeof(GraphicsDeviceType), api))
            {
                msg = "Invalid graphics API '" + api + "' at index " + index + ".";
                return false;
            }
            msg = string.Empty;
            return true;
        }

        static bool IsWellFormedCompilerSettingsEntry(ShaderCompilerSettings entry, int index, out string msg)
        {
            if (!IsValidGraphicsAPI(entry.graphicsAPI, index, out msg))
                return false;

            if (!Enum.IsDefined(typeof(ShaderOptimizationLevel), entry.optimizationLevel))
            {
                msg = "Invalid optimization level '" + (int)entry.optimizationLevel + "' for graphics API '"
                    + entry.graphicsAPI + "' at index " + index + ".";
                return false;
            }

            msg = string.Empty;
            return true;
        }

        static bool IsValidCompilerSettingsEntry(ShaderCompilerSettings entry, int index, out string msg)
        {
            if (!IsWellFormedCompilerSettingsEntry(entry, index, out msg))
                return false;

            if (entry.compilerToolchainOverride != ShaderCompilerToolchain.Default
                && !IsToolchainSupportedForAPI(entry.graphicsAPI, entry.compilerToolchainOverride, index, out msg))
                return false;

            msg = string.Empty;
            return true;
        }

        internal static bool ValidateShaderCompilerSettings(ShaderCompilerSettings[] settings, out string msg)
        {
            if (settings == null)
            {
                msg = "Null shader compiler settings array.";
                return false;
            }

            var processedAPIs = new HashSet<GraphicsDeviceType>();
            for (int i = 0, n = settings.Length; i < n; ++i)
            {
                if (!IsValidCompilerSettingsEntry(settings[i], i, out msg))
                    return false;

                if (!processedAPIs.Add(settings[i].graphicsAPI))
                {
                    msg = "Duplicate compiler settings entries for graphics API " + settings[i].graphicsAPI
                        + " at index " + i + ".";
                    return false;
                }
            }

            msg = string.Empty;
            return true;
        }

        static ShaderCompilerSettings MergeDuplicateCompilerSettings(ShaderCompilerSettings existing, ShaderCompilerSettings later)
        {
            if (later.compilerToolchainOverride != ShaderCompilerToolchain.Default)
                existing.compilerToolchainOverride = later.compilerToolchainOverride;
            existing.optimizationLevel = later.optimizationLevel;
            existing.enableDebugSymbols = later.enableDebugSymbols;
            return existing;
        }

        // Heals bad persisted data on write, which callers can't do themselves because compilerSettings is internal.
        internal static ShaderCompilerSettings[] SanitizeShaderCompilerSettings(ShaderCompilerSettings[] settings)
        {
            if (settings == null)
                return Array.Empty<ShaderCompilerSettings>();

            var kept = new List<ShaderCompilerSettings>(settings.Length);
            var indexByAPI = new Dictionary<GraphicsDeviceType, int>();
            bool changed = false;

            for (int i = 0, n = settings.Length; i < n; ++i)
            {
                ShaderCompilerSettings entry = settings[i];

                bool keep = SupportsCompilerSettings(entry.graphicsAPI)
                    ? IsValidCompilerSettingsEntry(entry, i, out _)
                    : IsWellFormedCompilerSettingsEntry(entry, i, out _);

                if (!keep)
                {
                    changed = true;
                }
                else if (indexByAPI.TryGetValue(entry.graphicsAPI, out int index))
                {
                    changed = true;
                    kept[index] = MergeDuplicateCompilerSettings(kept[index], entry);
                }
                else
                {
                    indexByAPI.Add(entry.graphicsAPI, kept.Count);
                    kept.Add(entry);
                }
            }

            return changed ? kept.ToArray() : settings;
        }

        static Dictionary<GraphicsDeviceType, ShaderCompilerSettings> MapRowsByGraphicsApi(IEnumerable<ShaderCompilerSettings> rows)
        {
            var map = new Dictionary<GraphicsDeviceType, ShaderCompilerSettings>();
            if (rows != null)
            {
                foreach (var row in rows)
                    map[row.graphicsAPI] = row;
            }
            return map;
        }

        static void ApplyDebugAndOptimizationToSupportingApis(
            Dictionary<GraphicsDeviceType, ShaderCompilerSettings> rowsByApi,
            bool enableDebugSymbols,
            ShaderOptimizationLevel optimizationLevel,
            IReadOnlyList<GraphicsDeviceType> enabledApis)
        {
            if (enabledApis == null)
                return;

            for (int i = 0, n = enabledApis.Count; i < n; ++i)
            {
                var api = enabledApis[i];
                bool supDebug = SupportsDebugSymbols(api);
                bool supOpt = SupportsOptimizationLevel(api);
                if (!supDebug && !supOpt)
                    continue;
                if (!rowsByApi.TryGetValue(api, out var s))
                {
                    s = new ShaderCompilerSettings
                    {
                        graphicsAPI = api,
                        compilerToolchainOverride = ShaderCompilerToolchain.Default,
                        optimizationLevel = ShaderOptimizationLevel.Default,
                        enableDebugSymbols = false,
                    };
                }
                if (supDebug) s.enableDebugSymbols = enableDebugSymbols;
                if (supOpt) s.optimizationLevel = optimizationLevel;
                rowsByApi[api] = s;
            }
        }

        static bool IsAllDefaultRow(ShaderCompilerSettings row)
        {
            return row.compilerToolchainOverride == ShaderCompilerToolchain.Default
                && row.optimizationLevel == ShaderOptimizationLevel.Default
                && !row.enableDebugSymbols;
        }

        static ShaderCompilerSettings[] CollectNonDefaultRows(Dictionary<GraphicsDeviceType, ShaderCompilerSettings> rowsByApi)
        {
            var result = new List<ShaderCompilerSettings>(rowsByApi.Count);
            foreach (var kv in rowsByApi)
            {
                if (!IsAllDefaultRow(kv.Value))
                    result.Add(kv.Value);
            }
            return result.ToArray();
        }

        // Sanitizes because the SerializedProperty save path bypasses SetShaderBuildSettings().
        internal static ShaderCompilerSettings[] MergeCompilerSettings(
            IEnumerable<ShaderCompilerSettings> existingRows,
            bool enableDebugSymbols,
            ShaderOptimizationLevel optimizationLevel,
            IReadOnlyList<GraphicsDeviceType> enabledApis)
        {
            var rowsByApi = MapRowsByGraphicsApi(existingRows);
            ApplyDebugAndOptimizationToSupportingApis(rowsByApi, enableDebugSymbols, optimizationLevel, enabledApis);
            return SanitizeShaderCompilerSettings(CollectNonDefaultRows(rowsByApi));
        }

        [SerializeField] internal ShaderCompilerSettings[] compilerSettings = Array.Empty<ShaderCompilerSettings>();

        internal static extern ShaderCompilerToolchain[] GetSupportedCompilerToolchainsForAPI(GraphicsDeviceType api);

        internal static extern bool SupportsCompilerSettings(GraphicsDeviceType api);

        internal static extern bool SupportsCompilerToolchainOverride(GraphicsDeviceType api);

        internal static extern bool SupportsOptimizationLevel(GraphicsDeviceType api);

        internal static extern bool SupportsDebugSymbols(GraphicsDeviceType api);

        internal static extern string GetCompilerDisplayNameForAPI(GraphicsDeviceType api, ShaderCompilerToolchain compiler);

        internal static extern string GetCompilerTooltipForAPI(GraphicsDeviceType api, ShaderCompilerToolchain compiler);

        internal static extern string GetDisplayNameForAPI(GraphicsDeviceType api);

        internal static extern string GetDebugSymbolsTooltipForAPI(GraphicsDeviceType api);
    }
}
