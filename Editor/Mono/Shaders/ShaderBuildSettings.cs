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

                msg = "";
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

        /// <summary>
        /// Per-API compiler settings. Contains compiler-related settings that can be configured per graphics API.
        /// </summary>
        [RequiredByNativeCode(GenerateProxy = false)]
        [Serializable]
        internal struct ShaderCompilerSettings
        {
            [SerializeField] public GraphicsDeviceType graphicsAPI;
            [SerializeField] public ShaderCompilerToolchain compilerToolchainOverride;

            [UsedByNativeCode, RequiredMember]
            internal static void DeconstructCompilerSettingsArrayElementRaw(ShaderCompilerSettings[] array, int index, out int graphicsAPI, out int compiler)
            {
                ref ShaderCompilerSettings tmp = ref array[index];
                graphicsAPI = (int)tmp.graphicsAPI;
                compiler = (int)tmp.compilerToolchainOverride;
            }

            [UsedByNativeCode, RequiredMember]
            internal static void ReconstructCompilerSettingsArrayElementRaw(ShaderCompilerSettings[] array, int index, int graphicsAPI, int compiler)
            {
                ref ShaderCompilerSettings tmp = ref array[index];
                tmp.graphicsAPI = (GraphicsDeviceType)graphicsAPI;
                tmp.compilerToolchainOverride = (ShaderCompilerToolchain)compiler;
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

            msg = "";
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
            var sections = define.Split((char[])null, StringSplitOptions.RemoveEmptyEntries); // null catches all whitespace variants
            identifier = "";
            value = "";

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
            msg = "";
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

            msg = "";
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

        internal static bool ValidateShaderCompilerSettings(ShaderCompilerSettings[] settings, out string msg)
        {
            if (settings == null)
            {
                msg = "Null shader compiler settings array.";
                return false;
            }

            for (int i = 0, n = settings.Length; i < n; ++i)
            {
                for (int j = i + 1; j < n; ++j)
                {
                    if (settings[i].graphicsAPI == settings[j].graphicsAPI)
                    {
                        msg = "Duplicate compiler settings entries for graphics API " + settings[i].graphicsAPI
                            + " at indices " + i + " and " + j + ".";
                        return false;
                    }
                }

                if (settings[i].compilerToolchainOverride != ShaderCompilerToolchain.Default)
                {
                    ShaderCompilerToolchain[] supported = GetSupportedCompilersForAPI(settings[i].graphicsAPI);
                    if (Array.IndexOf(supported, settings[i].compilerToolchainOverride) == -1)
                    {
                        msg = "Compiler '" + settings[i].compilerToolchainOverride + "' is not supported for graphics API '"
                            + settings[i].graphicsAPI + "' at index " + i + ".";
                        return false;
                    }
                }
            }

            msg = "";
            return true;
        }

        [SerializeField] internal ShaderCompilerSettings[] compilerSettings = Array.Empty<ShaderCompilerSettings>();
        internal ShaderCompilerSettings[] CompilerSettings
        {
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                string msg;
                if (ValidateShaderCompilerSettings(value, out msg))
                    compilerSettings = value;
                else
                    throw new ArgumentException(msg);
            }
        }

        internal ShaderCompilerSettings[] GetCompilerSettingsCopy()
        {
            return (ShaderCompilerSettings[])compilerSettings.Clone();
        }

        /// <summary>
        /// Returns the list of shader compilers available for the specified graphics API.
        /// Always returns at least a single-element array containing <see cref="ShaderCompiler.Default"/>;
        /// use <see cref="SupportsCompilerOverride"/> to check whether the API exposes a real choice.
        /// </summary>
        internal static extern ShaderCompilerToolchain[] GetSupportedCompilersForAPI(GraphicsDeviceType api);

        /// <summary>
        /// Returns true if the specified graphics API supports shader compiler selection.
        /// </summary>
        internal static extern bool SupportsCompilerOverride(GraphicsDeviceType api);
    }
}
