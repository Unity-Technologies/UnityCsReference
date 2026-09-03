// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.UIElements;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// An inherited (parent-document) stylesheet together with the document that owns it.
/// </summary>
internal readonly struct ParentStyleSheet
{
    public readonly StyleSheet StyleSheet;
    public readonly VisualTreeAsset OwningDocument;

    public ParentStyleSheet(StyleSheet styleSheet, VisualTreeAsset owningDocument)
    {
        StyleSheet = styleSheet;
        OwningDocument = owningDocument;
    }
}

/// <summary>
/// The document whose stylesheets are displayed (<see cref="Document"/>), whether that display is editable
/// or read-only (<see cref="IsReadOnly"/>), and its inherited parent stylesheets (<see cref="ParentStyleSheets"/>).
/// </summary>
internal readonly struct StyleSheetsContext
{
    [NoAutoStaticsCleanup] // immutable default sentinel, safe to persist
    public static readonly StyleSheetsContext None = default;

    /// <summary>The document whose stylesheets are displayed, or null when there is nothing to display.</summary>
    public readonly VisualTreeAsset Document;

    /// <summary>When true, the displayed stylesheets (and any selected rule) are read-only.</summary>
    public readonly bool IsReadOnly;

    readonly ParentStyleSheet[] m_ParentStyleSheets;

    public StyleSheetsContext(VisualTreeAsset document, ParentStyleSheet[] parentStyleSheets, bool isReadOnly = false)
    {
        Document = document;
        m_ParentStyleSheets = parentStyleSheets;
        IsReadOnly = isReadOnly;
    }

    /// <summary>The document whose stylesheets can be edited, or null when the display is read-only (or empty).</summary>
    public VisualTreeAsset EditedAsset => IsReadOnly ? null : Document;

    /// <summary>Inherited (read-only) parent-document stylesheets.</summary>
    public IReadOnlyList<ParentStyleSheet> ParentStyleSheets => m_ParentStyleSheets ?? Array.Empty<ParentStyleSheet>();

    /// <summary>
    /// Whether this context displays exactly the same thing as <paramref name="other"/>: the same document,
    /// with the same editability and the same inherited stylesheets. Rebuilding the display is pointless while
    /// it holds, and necessary as soon as it does not.
    /// </summary>
    public bool DisplaysSameContentAs(in StyleSheetsContext other)
    {
        if (Document != other.Document || IsReadOnly != other.IsReadOnly)
            return false;

        var parents = ParentStyleSheets;
        var otherParents = other.ParentStyleSheets;
        if (parents.Count != otherParents.Count)
            return false;

        for (var i = 0; i < parents.Count; i++)
        {
            if (parents[i].StyleSheet != otherParents[i].StyleSheet
                || parents[i].OwningDocument != otherParents[i].OwningDocument)
                return false;
        }

        return true;
    }
}

/// <summary>
/// Builds <see cref="StyleSheetsContext"/> instances from their source: a UI <see cref="VisualElementEditingStage"/>
/// or the document referenced by a selected panel component.
/// </summary>
internal static class StyleSheetsContextFactory
{
    public static StyleSheetsContext FromStage(VisualElementEditingStage stage)
    {
        if (stage == null)
            return StyleSheetsContext.None;

        return new StyleSheetsContext(stage.EditedVisualTreeAsset, CollectParents(stage.Context), isReadOnly: false);
    }

    /// <summary>
    /// Builds the context for a document referenced by a panel component (a <c>PanelRenderer</c> or
    /// <c>UIDocument</c>) that is displayed outside the UI Stage. Its stylesheets are editable exactly when the
    /// scene documents themselves are — while Main Stage authoring is active — and read-only otherwise.
    /// </summary>
    public static StyleSheetsContext FromSelectedDocument(VisualTreeAsset document)
    {
        if (document == null)
            return StyleSheetsContext.None;

        return new StyleSheetsContext(document, Array.Empty<ParentStyleSheet>(),
            isReadOnly: !UIToolkitStageUtility.IsAuthoringActiveInMainStage);
    }

    static ParentStyleSheet[] CollectParents(VisualTreeAssetEditingContext context)
    {
        if (context.SubDocumentOptions != SubDocumentOptions.InContext
            || context.SubDocumentPath == null
            || context.SubDocumentPath.Length == 0)
        {
            return [];
        }

        using var _ = HashSetPool<(VisualTreeAsset, StyleSheet)>.Get(out var collected);
        var parents = new List<ParentStyleSheet>();
        CollectFrom(context.RootVisualTreeAsset, collected, parents);
        for (var i = 0; i < context.SubDocumentPath.Length - 1; i++)
            CollectFrom(context.SubDocumentPath[i]?.ResolveTemplate(), collected, parents);

        return parents.ToArray();
    }

    static void CollectFrom(VisualTreeAsset vta, HashSet<(VisualTreeAsset, StyleSheet)> collected, List<ParentStyleSheet> parents)
    {
        if (vta == null)
            return;

        using var _ = ListPool<StyleSheet>.Get(out var sheets);
        vta.GetAllReferencedStyleSheets(sheets);
        foreach (var sheet in sheets)
        {
            if (sheet != null && collected.Add((vta, sheet)))
                parents.Add(new ParentStyleSheet(sheet, vta));
        }
    }
}
