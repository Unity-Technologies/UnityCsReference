// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;


namespace UnityEngine.TextCore.Text
{
    /// <summary>
    /// Enumeration of currently supported OpenType Layout features.
    /// </summary>
    [Obsolete("Font feature tables and OTL feature tags are obsolete. OpenType layout is now handled natively by Advanced Text Generator (ATG).", false)]
    public enum OTL_FeatureTag : uint
    {
        kern = 'k' << 24 | 'e' << 16 | 'r' << 8 | 'n',
        liga = 'l' << 24 | 'i' << 16 | 'g' << 8 | 'a',
        mark = 'm' << 24 | 'a' << 16 | 'r' << 8 | 'k',
        mkmk = 'm' << 24 | 'k' << 16 | 'm' << 8 | 'k',
    }
}
