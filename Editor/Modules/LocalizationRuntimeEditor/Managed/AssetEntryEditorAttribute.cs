// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Localization.Editor;

/// <summary>
/// Associates an asset entry editor with the asset entry type it edits.
/// </summary>
/// <remarks>
/// Apply this attribute to a class that implements <see cref="IAssetEntryEditor"/> to register it for a concrete asset
/// entry type. The Resource Tables window discovers the editor through <see cref="UnityEditor.TypeCache"/>, so a custom
/// asset entry kind appears in the table editor with no manual registration. Register one editor per entry type; the
/// editor for the nearest base type is used when no exact match exists.
/// </remarks>
/// <seealso cref="IAssetEntryEditor"/>
/// <seealso cref="IAssetEntry"/>
[AttributeUsage(AttributeTargets.Class)]
public sealed class AssetEntryEditorAttribute : Attribute
{
    /// <summary>
    /// The asset entry type this editor serves.
    /// </summary>
    public Type EntryType { get; }

    /// <summary>
    /// Associates the editor with an asset entry type.
    /// </summary>
    /// <param name="entryType">The concrete asset entry type the editor renders.</param>
    public AssetEntryEditorAttribute(Type entryType) => EntryType = entryType;
}
