// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor.Shaders
{
    [RequiredByNativeCode (GenerateProxy = true)]
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

        [RequiredByNativeCode(GenerateProxy = true)]
        [Serializable]
        public struct KeywordOverrideInfo
        {
            public KeywordOverrideInfo(string name, bool keepInBuild)
            {
                this.name = name;
                this.keepInBuild = keepInBuild;
            }

            private bool IsValidKeywordNameChar(char c)
            {
                return char.IsLetterOrDigit(c) || c == '_';
            }

            public bool IsValid()
            {
                if (string.IsNullOrEmpty(name))
                    return false;

                bool firstChar = true;
                foreach (char c in name)
                {
                    if (firstChar)
                    {
                        if (char.IsDigit(c))
                            return false;

                        firstChar = false;
                    }

                    if (!IsValidKeywordNameChar(c))
                        return false;
                }
                return true;
            }

            [SerializeField] public string name;
            [SerializeField] public bool keepInBuild;
        }

        [RequiredByNativeCode (GenerateProxy = true)]
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

            [SerializeField] public KeywordOverrideInfo[] keywords = { };
            [SerializeField] public ShaderVariantGenerationMode variantGenerationMode = ShaderVariantGenerationMode.Default;
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

        [SerializeField] private KeywordDeclarationOverride[] keywordDeclarationOverrides;
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
    }
}
