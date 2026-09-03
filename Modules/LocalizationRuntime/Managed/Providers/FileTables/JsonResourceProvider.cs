// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Localization.Providers.FileTables;

/// <summary>
/// A file table provider that reads tables from JSON, the default player format.
/// </summary>
/// <remarks>
/// Assign a collection to this provider to have its tables generated to JSON at build time and read back at runtime
/// through <see cref="JsonTableReader"/>. Add it to the asset provider chain the same way as any other
/// <see cref="FileTableProvider"/>.
/// </remarks>
/// <seealso cref="FileTableProvider"/>
/// <seealso cref="JsonTableReader"/>
[Serializable]
public sealed class JsonResourceProvider : FileTableProvider
{
    /// <inheritdoc/>
    protected override ITableFileReader Reader => JsonTableReader.Instance;
}
