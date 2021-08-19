// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/Shaders/Keywords/KeywordSpaceScriptBindings.h")]
    public readonly struct LocalKeywordSpace : IEquatable<LocalKeywordSpace>
    {
        [FreeFunction("keywords::GetKeywords", HasExplicitThis = true)] extern private LocalKeyword[] GetKeywords();
        [FreeFunction("keywords::GetKeywordNames", HasExplicitThis = true)] extern private string[] GetKeywordNames();
        [FreeFunction("keywords::GetKeywordCount", HasExplicitThis = true)] extern private uint GetKeywordCount();
        [FreeFunction("keywords::GetKeyword", HasExplicitThis = true)] extern private LocalKeyword GetKeyword(string name);

        public LocalKeyword[] keywords { get { return GetKeywords(); } }
        public string[] keywordNames { get { return GetKeywordNames(); } }
        public uint keywordCount { get { return GetKeywordCount(); } }

        public LocalKeyword FindKeyword(string name)
        {
            return GetKeyword(name);
        }

        public override bool Equals(object o)
        {
            return o is LocalKeywordSpace other && this.Equals(other);
        }

        public bool Equals(LocalKeywordSpace rhs)
        {
            return m_KeywordSpace == rhs.m_KeywordSpace;
        }

        public static bool operator==(LocalKeywordSpace lhs, LocalKeywordSpace rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator!=(LocalKeywordSpace lhs, LocalKeywordSpace rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode()
        {
            return m_KeywordSpace.GetHashCode();
        }

        private readonly IntPtr m_KeywordSpace;
    }
}
