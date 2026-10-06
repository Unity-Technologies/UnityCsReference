// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Text;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace Unity.Localization.Providers.FileTables;

/// <summary>
/// The default table file reader, reading table snapshots from JSON.
/// </summary>
/// <remarks>
/// Reads a <see cref="ResourceTableData"/> with <see cref="UnityEngine.JsonUtility"/>. It is stateless, so callers
/// share the single <see cref="Instance"/>. Pair it with <see cref="JsonResourceProvider"/>, the built-in JSON
/// <see cref="FileTableProvider"/>.
/// </remarks>
/// <seealso cref="ITableFileReader"/>
/// <seealso cref="JsonResourceProvider"/>
/// <seealso cref="ResourceTableData"/>
public sealed class JsonTableReader : ITableFileReader
{
    /// <summary>
    /// The shared stateless reader instance.
    /// </summary>
    [NoAutoStaticsCleanup] // stateless: the shared reader holds nothing a reload could invalidate
    public static readonly JsonTableReader Instance = new();

    /// <inheritdoc/>
    public string FileExtension => "json";

    /// <inheritdoc/>
    public ResourceTableData Read(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, leaveOpen: true);

        // JsonUtility matches member names case-sensitively and leaves a string member it cannot place untouched, so
        // starting from nulls tells us which of those the file really supplied. A collection member is materialised
        // either way, so an absent one cannot be told apart from an empty one.
        var data = new ResourceTableData { SchemaVersion = null, CollectionName = null, CollectionGuid = null, LocaleCode = null };
        JsonUtility.FromJsonOverwrite(reader.ReadToEnd(), data);

        if (string.IsNullOrEmpty(data.SchemaVersion) || string.IsNullOrEmpty(data.LocaleCode))
            throw new FormatException($"The JSON does not describe a {nameof(ResourceTableData)}: it is missing a schema version or a locale. Check that the member names match, including their case.");

        if (data.SchemaVersion != ResourceTableData.CurrentSchemaVersion)
            throw new FormatException($"The table file is schema version '{data.SchemaVersion}', and this Unity reads version '{ResourceTableData.CurrentSchemaVersion}'.");

        return data;
    }
}
