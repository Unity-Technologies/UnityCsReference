// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace UnityEditor.Networking
{
    // Mirror of the metadata contract in Modules/UnityWebRequest/Profiler/WebRequestProfilerCapture.h.
    // Keep both sides in sync: the row struct is read straight out of the metadata blob.
    static class WebRequestProfilerData
    {
        // Native writes StringToGUID("7b3f9c2e5a1d4e88b6c0f2a94d7e153c"). Unity's 32-char GUID text
        // stores each byte with its nibbles swapped relative to the raw bytes, and System.Guid then
        // reads its first three fields little-endian, so the same GUID spells differently here.
        // MetadataGuidBytes below is the raw byte sequence both spellings must produce, and
        // WebRequestProfilerModuleTests pins it - a typo on either side would otherwise just show an
        // empty details view.
        public static readonly Guid MetadataGuid = new Guid("e2c9f3b7-d1a5-88e4-6b0c-2f9ad4e751c3");

        public static readonly byte[] MetadataGuidBytes =
        {
            0xb7, 0xf3, 0xc9, 0xe2, 0xa5, 0xd1, 0xe4, 0x88,
            0x6b, 0x0c, 0x2f, 0x9a, 0xd4, 0xe7, 0x51, 0xc3,
        };

        public const int TagRequests = 0;
        public const int TagStrings = 1;
        public const int TagHeaders = 2;
        public const int TagTimings = 3;
        public const int TagBodies = 4;
        public const int TagBodyBytes = 5;
    }

    // Must match WebRequestProfilerBodyCapture in WebRequestProfilerCapture.h. Carried on in-flight
    // rows too: a request that started with capture off will never produce a body.
    [Flags]
    enum WebRequestProfilerBodyCapture : byte
    {
        None = 0,
        Enabled = 1 << 0,
        Supported = 1 << 1,
        TransportKnown = 1 << 2,
    }

    // Must match WebRequestProfilerBodyKind in WebRequestProfilerCapture.h.
    enum WebRequestProfilerBodyKind : byte
    {
        Request = 0,
        Response = 1,
    }

    // Must match WebRequestProfilerBodyFlag in WebRequestProfilerCapture.h. Why a captured body is
    // short or empty, which reads differently from a request that carried none.
    [Flags]
    enum WebRequestProfilerBodyFlags : byte
    {
        None = 0,
        Truncated = 1 << 0,
        Binary = 1 << 1,
    }

    // Must match WebRequestProfilerSource in WebRequestProfilerCapture.h.
    enum WebRequestProfilerSource : byte
    {
        UnityWebRequest = 0,
        HttpClient = 1,
    }

    // Must match WebRequestProfilerHeaderKind in WebRequestProfilerCapture.h.
    enum WebRequestProfilerHeaderKind : byte
    {
        Request = 0,
        Response = 1,
    }

    enum WebRequestProfilerState : byte
    {
        InFlight = 0,
        Completed = 1,
        Failed = 2,
    }

    // Must match WebRequestProfilerMeasurement in WebRequestProfilerCapture.h. Without it, zero means
    // both "transferred nothing" and "nothing here counts bytes".
    [Flags]
    enum WebRequestProfilerMeasurement : byte
    {
        None = 0,
        Download = 1 << 0,
        Upload = 1 << 1,
    }

    // Must match struct WebRequestProfilerRow in WebRequestProfilerCapture.h (64 bytes, no padding).
    [StructLayout(LayoutKind.Sequential)]
    struct WebRequestProfilerRow
    {
        public ulong requestId;
        public ulong bytesDownloaded;
        public ulong bytesUploaded;
        public ulong durationNs;
        public uint urlOffset;
        public uint urlLength;
        public uint methodOffset;
        public uint methodLength;
        public uint contentTypeOffset;
        public uint contentTypeLength;
        public ushort statusCode;
        public byte state;
        public byte source;
        public byte measured;
        public byte bodyCapture;
        public byte reserved0;
        public byte reserved1;
    }

    // Must match struct WebRequestProfilerBodyRow in WebRequestProfilerCapture.h (40 bytes). The bytes
    // are in the body blob, tag 5, which this slices.
    [StructLayout(LayoutKind.Sequential)]
    struct WebRequestProfilerBodyRow
    {
        public ulong requestId;
        public uint bodyOffset;
        public uint bodyLength;
        // What the body was. Larger than bodyLength once truncated, so the view can say how much.
        public ulong totalLength;
        // The body's own content type, slicing the string blob: text-like is not UTF-8, and the reader
        // needs the charset.
        public uint contentTypeOffset;
        public uint contentTypeLength;
        public byte kind;
        public byte flags;
        public byte reserved0;
        public byte reserved1;
        public byte reserved2;
        public byte reserved3;
        public byte reserved4;
        public byte reserved5;
    }

    // Must match struct WebRequestProfilerHeaderRow in WebRequestProfilerCapture.h (32 bytes).
    [StructLayout(LayoutKind.Sequential)]
    struct WebRequestProfilerHeaderRow
    {
        public ulong requestId;
        public uint nameOffset;
        public uint nameLength;
        public uint valueOffset;
        public uint valueLength;
        public byte kind;
        public byte reserved0;
        public byte reserved1;
        public byte reserved2;
        public byte reserved3;
        public byte reserved4;
        public byte reserved5;
        public byte reserved6;
    }

    // Must match struct WebRequestProfilerTimingRow in WebRequestProfilerCapture.h (72 bytes).
    //
    // The phase marks are microseconds from the start of the transfer, each at least as large as the
    // one before it, as the transport measured them. A zero means the phase was never reached rather
    // than that it was instant: no TLS handshake on plain HTTP, and none of the three connection phases on
    // a reused connection.
    //
    // 64-bit because a request with no timeout can outlast 32 bits of microseconds, which runs out
    // after 71m35s - and a saturating mark is worse than a missing one, since the phases either side
    // of it then report zero.
    [StructLayout(LayoutKind.Sequential)]
    struct WebRequestProfilerTimingRow
    {
        public ulong requestId;
        public ulong queuedUsec;
        public ulong nameLookupUsec;
        public ulong connectUsec;
        public ulong tlsHandshakeUsec;
        public ulong preTransferUsec;
        public ulong startTransferUsec;
        public ulong totalUsec;

        // WebRequestProfilerPhaseMark bits saying which marks the transport could read. A mark that is
        // absent here is unmeasured, which the view has to say differently from a phase that was
        // skipped - zero means both otherwise.
        public ulong availableMarks;
    }
}
