// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Localization;

/// <summary>
/// Marks a key or entry so that editor exporters skip it.
/// </summary>
/// <remarks>
/// Use this to keep editor-only or placeholder strings out of the files exported for translators, for example a
/// collection JSON or CSV export. The entry stays in the collection; only the exported file omits it. It implements
/// <see cref="IMetadata"/>, so it is stored in a <see cref="MetadataCollection"/>, and it carries no data of its own:
/// its presence is the flag. The <see cref="MetadataAttribute"/> on this type allows it on shared keys and per-locale
/// entries, and limits each target to a single instance.
/// </remarks>
/// <example>
/// <para>Flag an entry so the editor export skips it.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Metadata/ExcludeEntryFromExportExample.cs"/>
/// </example>
/// <seealso cref="ExcludeEntryFromPlayerExport"/>
/// <seealso cref="IMetadata"/>
/// <seealso cref="MetadataCollection"/>
[Metadata(AllowedTypes = MetadataType.SharedTableEntry | MetadataType.ResourceEntry, MenuItem = "Exclude From Editor Export", AllowMultiple = false)]
[Serializable]
public class ExcludeEntryFromEditorExport : IMetadata
{
}
