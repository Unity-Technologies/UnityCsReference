// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine.TextCore;

namespace UnityEngine.TextCore.Text
{
    internal struct GlyphProxy
    {
        public uint index;
        public GlyphRect glyphRect;
        public GlyphMetrics metrics;
        public int atlasIndex;
    }
}
