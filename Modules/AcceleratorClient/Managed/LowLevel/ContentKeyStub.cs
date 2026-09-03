// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#nullable enable
using System;

namespace Unity.AcceleratorClient.LowLevel
{
    // ContentKey.cs/ContentHash.cs are excluded from the current compile; this stub keeps the
    // (unreachable-with-defaults) verify call sites compiling.
    internal sealed class DownloadHasherStub
    {
        public void Append(ReadOnlySpan<byte> data) { }
    }

    internal static class ContentKeyStub
    {
        public static UInt128 Unsupported() =>
            throw new NotSupportedException(
                "Content-hash verification is not available in this build configuration " +
                "(ContentKey/System.IO.Hashing is excluded). " +
                "VerifyContentHashOnUpload/Download must remain false.");
    }
}
