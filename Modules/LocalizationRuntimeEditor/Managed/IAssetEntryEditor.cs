// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.Localization.Editor;

/// <summary>
/// Edits one kind of localized asset entry in the Resource Tables window.
/// </summary>
/// <remarks>
/// Implement this interface and mark the implementation with <see cref="AssetEntryEditorAttribute"/> to add a custom
/// asset entry kind to the table editor. The implementation builds the field that edits an entry's value and supplies
/// the labels the window shows for the kind. The stored asset reference lives on the entry itself, so there is no
/// separate registry to keep in sync. The built-in kinds store a direct object reference or a Resources path; a custom
/// implementation stores its asset however it needs.
/// </remarks>
/// <seealso cref="AssetEntryEditorAttribute"/>
/// <seealso cref="IAssetEntry"/>
public interface IAssetEntryEditor
{
    /// <summary>
    /// The object type the field accepts.
    /// </summary>
    Type AssetType { get; }

    /// <summary>
    /// A short label for the storage kind, shown as a row indicator and in the Add Entry menu.
    /// </summary>
    string KindLabel { get; }

    /// <summary>
    /// A one-line explanation of how the kind stores and loads its asset, shown as the row indicator's tooltip.
    /// </summary>
    string KindTooltip { get; }

    /// <summary>
    /// Builds the field that edits the entry's default value.
    /// </summary>
    /// <param name="entry">The asset entry to edit.</param>
    /// <param name="table">The table that owns the entry, used for undo and dirtying.</param>
    /// <returns>The visual element that edits the default value.</returns>
    VisualElement CreateDefaultField(IAssetEntry entry, ResourceTable table);

    /// <summary>
    /// Builds the field that edits one variant value of the entry.
    /// </summary>
    /// <param name="entry">The asset entry to edit.</param>
    /// <param name="variantKey">The variant key the field edits.</param>
    /// <param name="table">The table that owns the entry, used for undo and dirtying.</param>
    /// <returns>The visual element that edits the variant value.</returns>
    VisualElement CreateVariantField(IAssetEntry entry, string variantKey, ResourceTable table);
}
