// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.DataModel;

// Struct to store the information about a library assembly
internal readonly record struct LibraryLookupEntry(string FilePath, int ReaderIndex)
{
    // This value is to indicate that a reader is invalid or that there isn't one assigned yet to the assembly.
    internal const int InvalidReaderIndex = -1;

    // Path to the assembly file
    internal readonly string FilePath = FilePath;

    // Index of the reader if there is one already. InvalidReaderIndex otherwise
    internal readonly int ReaderIndex = ReaderIndex;
}
