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
    }

    // Must match WebRequestProfilerSource in WebRequestProfilerCapture.h.
    enum WebRequestProfilerSource : byte
    {
        UnityWebRequest = 0,
        HttpClient = 1,
    }

    enum WebRequestProfilerState : byte
    {
        InFlight = 0,
        Completed = 1,
        Failed = 2,
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
        public uint reserved;
    }
}
