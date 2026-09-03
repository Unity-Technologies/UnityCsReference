// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Localization;

/// <summary>
/// Marks a key or entry so that it is left out of the files generated for a built player.
/// </summary>
/// <remarks>
/// Use this to keep an entry out of the generated player file tables while still shipping it: the entry stays in the
/// table asset and is served from there at runtime, the same way an object-reference asset entry is. It implements
/// <see cref="IMetadata"/>, so it is stored in a <see cref="MetadataCollection"/>, and it carries no data of its own:
/// its presence is the flag. The <see cref="MetadataAttribute"/> on this type allows it on shared keys and per-locale
/// entries, and limits each target to a single instance.
/// </remarks>
/// <example>
/// <para>Flag an entry so the player file generation leaves it in the table asset.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Metadata/ExcludeEntryFromExportExample.cs"/>
/// </example>
/// <seealso cref="ExcludeEntryFromEditorExport"/>
/// <seealso cref="IMetadata"/>
/// <seealso cref="MetadataCollection"/>
[Metadata(AllowedTypes = MetadataType.SharedTableEntry | MetadataType.ResourceEntry, MenuItem = "Exclude From Player Export", AllowMultiple = false)]
[Serializable]
public class ExcludeEntryFromPlayerExport : IMetadata
{
}
