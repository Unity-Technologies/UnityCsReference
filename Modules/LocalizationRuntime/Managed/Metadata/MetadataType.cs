// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Localization;

/// <summary>
/// Identifies where a kind of metadata may be attached in a localized project.
/// </summary>
/// <remarks>
/// The value is a bit mask, so several targets can be combined. The editor reads it from <see cref="MetadataAttribute"/> to
/// filter the "Add Metadata" menu, offering a metadata kind only on the targets it declares. The named targets correspond to
/// a <see cref="SharedTableData"/>, a <see cref="ResourceTable"/>, a shared key, and a per-locale entry.
/// </remarks>
/// <example>
/// <para>Test whether a value allows attaching metadata to a shared table entry.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Metadata/MetadataTypeExample.cs"/>
/// </example>
/// <seealso cref="MetadataAttribute"/>
/// <seealso cref="IMetadata"/>
/// <seealso cref="SharedTableData"/>
/// <seealso cref="ResourceTable"/>
[Flags]
public enum MetadataType
{
    /// <summary>
    /// Not attachable anywhere.
    /// </summary>
    None = 0,

    /// <summary>
    /// Attachable to the shared table data, which covers the whole collection.
    /// </summary>
    SharedTableData = 1 << 0,

    /// <summary>
    /// Attachable to a resource table, which holds the entries for one locale.
    /// </summary>
    ResourceTable = 1 << 1,

    /// <summary>
    /// Attachable to a shared key, which is common to all locales.
    /// </summary>
    SharedTableEntry = 1 << 2,

    /// <summary>
    /// Attachable to a per-locale entry.
    /// </summary>
    ResourceEntry = 1 << 3,

    /// <summary>
    /// Attachable to every target.
    /// </summary>
    All = SharedTableData | ResourceTable | SharedTableEntry | ResourceEntry
}

/// <summary>
/// Declares where a metadata kind may be attached and how it appears in the editor's "Add Metadata" menu.
/// </summary>
/// <remarks>
/// Apply this attribute to an <see cref="IMetadata"/> implementation. <see cref="AllowedTypes"/> limits the targets the
/// editor offers the metadata on, <see cref="MenuItem"/> sets the menu label, and <see cref="AllowMultiple"/> controls
/// whether more than one item of the kind can be added to the same target.
/// </remarks>
/// <example>
/// <para>Restrict a custom metadata kind to shared table entries and give it a menu label.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Metadata/MetadataAttributeExample.cs"/>
/// </example>
/// <seealso cref="MetadataType"/>
/// <seealso cref="IMetadata"/>
/// <seealso cref="Comment"/>
[AttributeUsage(AttributeTargets.Class)]
public class MetadataAttribute : Attribute
{
    /// <summary>
    /// Where this metadata may be attached.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="MetadataType.All"/>.
    /// </remarks>
    public MetadataType AllowedTypes { get; set; } = MetadataType.All;

    /// <summary>
    /// The menu path shown in the "Add Metadata" menu.
    /// </summary>
    /// <remarks>
    /// When null, the menu uses the type name.
    /// </remarks>
    public string MenuItem { get; set; }

    /// <summary>
    /// Whether more than one of this metadata kind may be added to the same target.
    /// </summary>
    /// <remarks>
    /// Defaults to <see langword="true"/>.
    /// </remarks>
    public bool AllowMultiple { get; set; } = true;
}
