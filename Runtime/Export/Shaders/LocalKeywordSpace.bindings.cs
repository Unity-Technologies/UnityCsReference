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
    public readonly struct LocalKeywordSpace
    {
        [FreeFunction("keywords::GetKeywords", HasExplicitThis = true)] extern private LocalKeyword[] GetKeywords();
        [FreeFunction("keywords::GetKeywordNames", HasExplicitThis = true)] extern private string[] GetKeywordNames();
        [FreeFunction("keywords::GetKeywordCount", HasExplicitThis = true)] extern private uint GetKeywordCount();

        public LocalKeyword[] keywords { get { return GetKeywords(); } }
        public string[] keywordNames { get { return GetKeywordNames(); } }
        public uint keywordCount { get { return GetKeywordCount(); } }

        private readonly IntPtr m_KeywordSpace;
    }
}
