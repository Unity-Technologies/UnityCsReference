// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;

namespace Unity.Localization.Providers.FileTables;

/// <summary>
/// Reads a table snapshot from a file at runtime, so a file-backed provider can rebuild a table without shipping
/// the authored table asset.
/// </summary>
/// <remarks>
/// A reader turns a stream into a <see cref="ResourceTableData"/> snapshot, which the provider
/// rebuilds into a table. Writing the snapshot lives on the editor side, so no serialization code ships in a player.
/// Implement this and pair it with a <see cref="FileTableProvider"/> subclass to add a runtime table format;
/// <see cref="JsonResourceProvider"/> pairs the default <see cref="JsonTableReader"/> for JSON.
/// </remarks>
/// <example>
/// <para>Add a runtime table format by pairing a reader with a provider.</para>
/// <code source="../../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Providers/CustomFileTableProviderExample.cs"/>
/// </example>
/// <seealso cref="FileTableProvider"/>
/// <seealso cref="JsonTableReader"/>
/// <seealso cref="ResourceTableData"/>
public interface ITableFileReader
{
    /// <summary>
    /// The file extension the format uses, without the leading dot.
    /// </summary>
    /// <remarks>
    /// For example, the JSON reader returns "json". A provider builds its file names from this extension.
    /// </remarks>
    string FileExtension { get; }

    /// <summary>
    /// Reads a table snapshot from a stream.
    /// </summary>
    /// <remarks>
    /// The stream is left open for the caller to dispose.
    /// </remarks>
    /// <param name="stream">The source stream to read from.</param>
    /// <returns>The table snapshot read from the stream.</returns>
    ResourceTableData Read(Stream stream);
}
