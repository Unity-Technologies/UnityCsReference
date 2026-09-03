// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements.StyleSheets;

// Per-descriptor match statistics accumulated by the native matcher when a USS stats profiling
// pass supplies a buffer (one element per descriptor of the acceleration cache entry). The
// collection code is editor-only; players always pass null and compile the plain loop.
// Layout must remain in sync with the C++ mirror in
// Modules/UIElements/Core/Native/StyleSheets/NativeSelectorMatcher.h
[StructLayout(LayoutKind.Sequential)]
internal struct SelectorMatchStatsInfo
{
    public ulong timeNs;           // exhaustive right-to-left matching time
    public uint testedCount;       // times the descriptor was processed by the loop
    public uint matchedCount;
    public uint fastRejectedCount; // Bloom prefilter rejections (no exhaustive search ran)
}

[NativeHeader("Modules/UIElements/Core/Native/StyleSheets/NativeSelectorMatcher.h")]
internal static class NativeSelectorMatcher
{
    // Editor styling tests toggle this through StyleSelectorHelper.s_VerifyBloomIntegrity;
    // the flag lives natively because the matcher loop is the one that consumes it.
    internal static extern bool verifyBloomIntegrity
    {
        [FreeFunction("UIToolkit::NativeSelectorMatcher::GetVerifyBloomIntegrity", IsThreadSafe = true)]
        get;
        [FreeFunction("UIToolkit::NativeSelectorMatcher::SetVerifyBloomIntegrity", IsThreadSafe = true)]
        set;
    }

    [FreeFunction("UIToolkit::NativeSelectorMatcher::MatchSheetFlat", IsThreadSafe = true)]
    internal static extern unsafe int MatchSheetFlat(
        VisualElementSelectorData* element,
        SelectorRangeDescriptor* allDescriptors,
        int descriptorCount,
        SelectorKeyIndexEntry* keyIndex,
        FlattenedSelector* allSelectors,
        FlattenedSelectorPart* allParts,
        CountingBloomFilter* ancestorFilter,
        SelectorMatcherRanges* ranges,
        bool applyPseudoMasks,
        bool testRootRange,
        int* matchedIndices,
        SelectorMatchStatsInfo* stats);
}
