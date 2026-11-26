// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    // Input of the editor themes build (RunBuiltEditorHelper.jam.cs): keep only what feeds the hash in this file.
    static class StyleSheetSerializationLayout
    {
        internal static readonly int hash = ComputeHash();

        // Handles serialize StyleValueType as an int, so its members are part of the layout. FNV-1a: string.GetHashCode is randomized per process.
        static int ComputeHash()
        {
            const uint fnvPrime = 16777619;
            unchecked
            {
                var hash = (uint)StylePropertyUtil.k_SerializationLayoutHash;
                foreach (StyleValueType type in Enum.GetValues(typeof(StyleValueType)))
                {
                    foreach (var c in (int)type + ":" + type)
                    {
                        hash ^= c;
                        hash *= fnvPrime;
                    }
                    hash ^= '\n';
                    hash *= fnvPrime;
                }
                return (int)hash;
            }
        }
    }
}
