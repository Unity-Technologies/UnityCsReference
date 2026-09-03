// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using Unity.Localization.Providers.FileTables;

namespace Unity.Localization.Editor;

/// <summary>
/// Writes a table snapshot to a file, so a file-backed provider can ship its tables as data instead of assets.
/// </summary>
/// <remarks>
/// A writer turns a <see cref="ResourceTableData"/> snapshot into the bytes a matching
/// <see cref="ITableFileReader"/> reads back at runtime. Writing runs in the editor when player data is generated, so
/// no serialization code ships in a player. Pair an implementation with a <see cref="FileTableProviderEditor"/>
/// subclass to have Unity generate the files for a custom format.
/// </remarks>
/// <example>
/// <para>Write a text format that the matching reader parses back.</para>
/// <code source="../../../../Modules/LocalizationRuntimeEditor/Tests/UTFTests/Editor/Localization.Samples/CustomFileTableProviderEditorExample.cs"/>
/// </example>
/// <seealso cref="FileTableProviderEditor"/>
/// <seealso cref="ITableFileReader"/>
/// <seealso cref="ResourceTableData"/>
public interface ITableFileWriter
{
    /// <summary>
    /// Writes a table snapshot to a stream.
    /// </summary>
    /// <remarks>
    /// The stream is left open for the caller to dispose.
    /// </remarks>
    /// <param name="data">The snapshot to write.</param>
    /// <param name="stream">The destination stream to write to.</param>
    void Write(ResourceTableData data, Stream stream);
}
