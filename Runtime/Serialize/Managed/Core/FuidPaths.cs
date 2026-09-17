// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.Serialization;

// Managed serialization V2: property-path (FUID) identifier building.
//
// Templates carry path structure (a packed segment table and name pool,
// composed in Core/PathSegments.cpp), and this fragment resolves it into the
// v1-format identifier string on demand at FUID consumer sites.
//
// Collection indices are recovered from the executor's frame stack: publish
// stamps each collection segment with its loop's frame depth and element
// stride (V2StampFuidSegmentFacts), so the live index is
// (frames[frameDepth].Offset - ArrayDataOffset) / elementStride and the
// identifier handed to native contains no "%d" holes. That satisfies
// FormatFieldUniqueIdentifier's hole-count == index-stack-depth invariant
// with an empty index stack, and the identifier passes through it unchanged
// (the SR refid prepend, when active, applies as usual).
internal static unsafe partial class SerializationBackendManagedCommands
{
    // Property-path segment, the packed 16-byte mirror of Commands.h.
    [StructLayout(LayoutKind.Sequential)]
    internal struct V2PathSegment            // 16 bytes
    {
        public uint parent;                  // segment index; kV2PathSegmentNoParent = template root
        public uint nameOffsetAndKind;       // name-pool offset (low 31 bits); bit 31 = collection field
        public uint elementStride;           // publish-stamped index-recovery facts for collection
        public ushort frameDepth;            // segments on a consumer's chain; zero elsewhere
        public ushort pad;
    }

    private const uint kV2PathSegmentNoParent = 0xFFFFFFFFu;
    private const uint kV2NoPathSegment = 0xFFFFFFFFu;
    private const uint kV2PathSegmentCollectionBit = 0x80000000u;
    private const uint kV2PathSegmentNameMask = 0x7FFFFFFFu;

    // Longest segment chain the builder accepts. Real chains are bounded by
    // the serialization depth limit plus collection hops; this cap only
    // bounds the stackalloc.
    private const int kV2MaxPathChain = 64;

    private const string kV2ArrayDataPrefix = ".Array.data[";

    // Per-thread scratch for BuildV2FuidIdentifier; the contents are dead
    // after the call and hold no user references, so it may persist.
    [NoAutoStaticsCleanup]
    [ThreadStatic] private static byte[] s_V2FuidIdentifierBuffer;

    // On-demand FUID identifier access for element-source extension handlers
    // (doc §2.6). Wraps the template's segment and name tables plus the live
    // frame stack, so handlers build the resolved path only when they need
    // it.
    private unsafe ref struct V2FuidBuilder
    {
        private readonly byte* m_PathSegments;
        private readonly byte* m_PathNames;
        private readonly uint m_SegmentIndex;
        private readonly Span<V2ObjectFrame> m_Frames;

        public V2FuidBuilder(byte* pathSegments, byte* pathNames, uint segmentIndex, Span<V2ObjectFrame> frames)
        {
            m_PathSegments = pathSegments;
            m_PathNames = pathNames;
            m_SegmentIndex = segmentIndex;
            m_Frames = frames;
        }

        public bool HasSegment => m_SegmentIndex != kV2NoPathSegment && m_PathSegments != null;

        // Returns the resolved NUL-terminated UTF-8 identifier in a
        // thread-static buffer, or null when there is no segment or on a
        // structural mismatch (fail closed, like an absent template).
        public byte[] Build()
        {
            return HasSegment
                ? BuildV2FuidIdentifier(m_PathSegments, m_PathNames, m_SegmentIndex, m_Frames, -1)
                : null;
        }
    }

    // Builds the v1-format FUID identifier for the segment chain ending at
    // leafSegment: root-to-leaf names joined with '.', with
    // ".Array.data[<i>]" after each collection-typed ancestor, where <i> is
    // recovered from the ancestor's publish-stamped frame depth and stride
    // against the live frame stack. The leaf's own collection suffix is
    // excluded (v1's storage-key shape). Handler-owned collections (the SR
    // ExternalArray, whose loop has no frame) pass an empty frames span and
    // the element index directly; exactly one collection ancestor is then
    // supported and enclosing loops fail closed. Returns a NUL-terminated
    // UTF-8 identifier in a thread-static buffer, or null on a structural
    // mismatch (an unstamped ancestor), in which case the caller skips
    // duplicate-row tracking. Cold path: at most once per element-source
    // field per transfer, and only with a resolved hosting entity.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe byte[] BuildV2FuidIdentifier(
        byte* pathSegments, byte* pathNames, uint leafSegment,
        Span<V2ObjectFrame> frames, int handlerElementIndex)
    {
        var segments = (V2PathSegment*)pathSegments;

        // Collect the chain from leaf to root; chain[chainLength - 1] is the root.
        uint* chain = stackalloc uint[kV2MaxPathChain];
        int chainLength = 0;
        for (uint idx = leafSegment; idx != kV2PathSegmentNoParent; idx = segments[idx].parent)
        {
            if (chainLength == kV2MaxPathChain)
                return null;
            chain[chainLength++] = idx;
        }

        // Capacity bound: the name bytes plus a worst-case collection hop
        // (prefix, 10 digits, ']') and separator per segment, plus the NUL.
        int capacity = 1;
        for (int i = 0; i < chainLength; ++i)
        {
            byte* name = pathNames + (segments[chain[i]].nameOffsetAndKind & kV2PathSegmentNameMask);
            while (*name != 0)
            {
                ++capacity;
                ++name;
            }
            capacity += kV2ArrayDataPrefix.Length + 10 + 1 + 1;
        }

        byte[] buffer = s_V2FuidIdentifierBuffer;
        if (buffer == null || buffer.Length < capacity)
            s_V2FuidIdentifierBuffer = buffer = new byte[Math.Max(capacity, 128)];

        // Fill from root to leaf. Every collection ancestor of an executing
        // consumer carries publish-stamped recovery facts; an unstamped one
        // (zero stride) fails closed.
        int p = 0;
        int handlerLevels = 0;
        for (int i = chainLength - 1; i >= 0; --i)
        {
            ref V2PathSegment segment = ref segments[chain[i]];
            byte* name = pathNames + (segment.nameOffsetAndKind & kV2PathSegmentNameMask);
            while (*name != 0)
                buffer[p++] = *name++;

            bool isLeaf = i == 0;
            if ((segment.nameOffsetAndKind & kV2PathSegmentCollectionBit) != 0 && !isLeaf)
            {
                int index;
                if (!frames.IsEmpty)
                {
                    if (segment.elementStride == 0)
                        return null;
                    index = (int)((frames[segment.frameDepth].Offset - V2LayoutFacts.ArrayDataOffset)
                        / (nint)segment.elementStride);
                }
                else
                {
                    // Handler-owned index serves exactly one level.
                    if (++handlerLevels > 1)
                        return null;
                    index = handlerElementIndex;
                }
                for (int c = 0; c < kV2ArrayDataPrefix.Length; ++c)
                    buffer[p++] = (byte)kV2ArrayDataPrefix[c];
                p += V2WriteDecimal(buffer, p, index);
                buffer[p++] = (byte)']';
            }

            if (!isLeaf)
                buffer[p++] = (byte)'.';
        }
        buffer[p] = 0;

        return buffer;
    }

    // Writes a non-negative int as decimal digits at buffer[offset] and
    // returns the digit count, matching native's snprintf("%d", index).
    private static int V2WriteDecimal(byte[] buffer, int offset, int value)
    {
        int digits = 1;
        for (int probe = value; probe >= 10; probe /= 10)
            ++digits;
        for (int i = digits - 1; i >= 0; --i)
        {
            buffer[offset + i] = (byte)('0' + value % 10);
            value /= 10;
        }
        return digits;
    }
}
