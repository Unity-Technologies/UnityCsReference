// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using Unity.UIToolkit.Editor.Importers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Stateless helpers for document editing operations: circular-dependency checks and clipboard paste.
/// </summary>
static class VisualElementEditingUtility
{
    public static bool WillCauseCircularDependency(VisualTreeAsset visualTreeAsset, HashSet<string> visitedPaths)
    {
        if (!visitedPaths.Add(AssetDatabase.GetAssetPath(visualTreeAsset)))
            return true;

        foreach (var template in visualTreeAsset.templateDependencies)
        {
            if (WillCauseCircularDependency(template, visitedPaths))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Whether authoring an instance of <paramref name="template"/> into <paramref name="targetDocument"/>, under
    /// <paramref name="dropParent"/>, would make a document (transitively) instantiate itself.
    /// </summary>
    /// <param name="targetDocument">
    /// The document the instance is authored into — the one a cycle would be created in. This is not always the
    /// document <paramref name="dropParent"/> belongs to: dropping onto the root of a document adds to the
    /// document it hosts, not to the one instantiating it.
    /// </param>
    /// <param name="dropParent">
    /// The live element receiving the instance. Every document it is nested in is checked as well, so the drop is
    /// also refused when <paramref name="template"/> reaches one of them.
    /// </param>
    /// <param name="template">The template being instantiated.</param>
    /// <returns><see langword="true"/> when the drop has to be refused.</returns>
    public static bool WillCauseCircularDependency(VisualTreeAsset targetDocument, VisualElement dropParent,
        VisualTreeAsset template)
    {
        if (template == null)
            return true;

        using var _ = HashSetPool<string>.Get(out var visitedPaths);
        CollectNestingDocuments(targetDocument, dropParent, visitedPaths);

        // Nothing resolved means we cannot prove the drop is safe; refuse it.
        return visitedPaths.Count == 0 || WillCauseCircularDependency(template, visitedPaths);
    }

    /// <summary>
    /// Collects <paramref name="targetDocument"/> and every document <paramref name="dropParent"/> is nested in:
    /// the one it was cloned from, then each document instantiating that one, up to the document rendered by the
    /// panel component hosting it.
    /// </summary>
    static void CollectNestingDocuments(VisualTreeAsset targetDocument, VisualElement dropParent,
        HashSet<string> visitedPaths)
    {
        AddAssetPath(targetDocument, visitedPaths);

        for (var current = dropParent; current != null; current = current.hierarchy.parent)
        {
            // The panel component owns the outermost document; nothing above it belongs to a document.
            if (current is IPanelComponentRootElement rootElement)
            {
                AddAssetPath(rootElement.panelComponent?.visualTreeAsset, visitedPaths);
                break;
            }

            AddAssetPath(current.visualTreeAssetSource, visitedPaths);
        }
    }

    static void AddAssetPath(VisualTreeAsset visualTreeAsset, HashSet<string> visitedPaths)
    {
        if (visualTreeAsset != null)
            visitedPaths.Add(AssetDatabase.GetAssetPath(visualTreeAsset));
    }

    /// <summary>
    /// Whether authoring <paramref name="asset"/> and everything below it into <paramref name="targetDocument"/>,
    /// under <paramref name="parentElement"/>, would leave a document (transitively) instantiating itself.
    /// </summary>
    public static bool WouldCauseCircularDependency(VisualTreeAsset targetDocument, VisualElement parentElement, UxmlAsset asset)
    {
        if (targetDocument == null || asset == null)
            return false;

        using var _ = ListPool<VisualTreeAsset>.Get(out var templates);
        CollectInstantiatedTemplates(asset, templates);

        foreach (var template in templates)
        {
            if (WillCauseCircularDependency(targetDocument, parentElement, template))
                return true;
        }

        return false;
    }

    /// <summary>
    /// The templates <paramref name="asset"/> and everything authored below it instantiate, without duplicates.
    /// </summary>
    public static void CollectInstantiatedTemplates(UxmlAsset asset, List<VisualTreeAsset> templates)
    {
        if (asset is TemplateAsset instance)
        {
            var template = instance.ResolveTemplate();
            if (template && !templates.Contains(template))
                templates.Add(template);
        }

        for (var i = 0; i < asset.childCount; ++i)
            CollectInstantiatedTemplates(asset[i], templates);
    }

    /// <summary>
    /// Whether pasting <paramref name="copiedContent"/> under <paramref name="parentElement"/> would leave
    /// <paramref name="targetDocument"/> (transitively) instantiating itself.
    /// </summary>
    public static bool CopiedContentWouldCauseCircularDependency(VisualElement parentElement,
        VisualTreeAsset targetDocument, string copiedContent)
    {
        if (targetDocument == null)
            return false;

        var importer = new TempVisualTreeAssetImporter();
        importer.ImportXmlFromString(copiedContent, out var pasted);
        if (pasted == null)
            return false;

        try
        {
            return WouldCauseCircularDependency(targetDocument, parentElement, pasted.visualTree);
        }
        finally
        {
            Object.DestroyImmediate(pasted);
        }
    }

    /// <summary>
    /// Resolves where a paste operation should land for the current stage.
    /// </summary>
    public static bool TryResolvePasteParent(VisualElement selected, out VisualElement parentElement,
        out VisualElementAsset parentAsset)
    {
        switch (StageUtility.GetCurrentStage())
        {
            case VisualElementEditingStage uiStage:
                return TryResolvePasteParentInUIStage(uiStage, selected, out parentElement, out parentAsset);
            case MainStage _:
            case PrefabStage _:
                return TryResolvePasteParentInMainStage(selected, out parentElement, out parentAsset);
            default:
                parentElement = null;
                parentAsset = null;
                return false;
        }
    }

    // Paste lands as a sibling of the selected element (into its logical parent), or — when that parent is the
    // document root (a PanelRenderer/UIDocument root element, which has no VisualElementAsset) — directly under
    // the document via VisualTreeAsset.visualTree. With no selection there is no implicit target.
    static bool TryResolvePasteParentInMainStage(VisualElement selected, out VisualElement parentElement,
        out VisualElementAsset parentAsset)
    {
        parentElement = null;
        parentAsset = null;

        if (selected == null)
            return false;

        if (selected is IPanelComponentRootElement)
            parentElement = selected;
        else if (UIToolkitStageUtility.GetEditFlags(selected).IsFullyEditable())
            parentElement = selected.parent != null
                ? GetLogicalParentFromPhysicalParent(selected.parent) ?? selected
                : selected;
        else
            return false;

        parentAsset = parentElement?.visualElementAsset;
        if (parentAsset == null)
        {
            var panelComponent = (parentElement as IPanelComponentRootElement)?.panelComponent
                                 ?? VisualElementSceneViewOverlay.FindPanelComponentForElement(selected);
            parentAsset = panelComponent?.visualTreeAsset?.visualTree;
        }

        return parentElement != null && parentAsset != null;
    }

    // With a selection, paste lands as a sibling of the selected element; with none it falls back to the local
    // root of the (sub-)document being edited. Reparenting onto the local root always targets the edited
    // document's root asset, since the live local root has no backing asset of its own (or, when editing in
    // context, is the parent document's template instance).
    static bool TryResolvePasteParentInUIStage(VisualElementEditingStage stage, VisualElement selected,
        out VisualElement parentElement, out VisualElementAsset parentAsset)
    {
        var localRoot = stage.ResolveLocalRoot();
        var rootAsset = stage.EditedVisualTreeAsset != null ? stage.EditedVisualTreeAsset.visualTree : null;

        parentElement = localRoot;
        parentAsset = rootAsset;

        if (selected != null)
        {
            if (selected.parent != null)
                parentElement = GetLogicalParentFromPhysicalParent(selected.parent) ?? parentElement;
            parentAsset = parentElement?.visualElementAsset ?? parentAsset;
        }

        if (parentElement == localRoot)
            parentAsset = rootAsset;

        return parentElement != null && parentAsset != null;
    }

    static VisualElement GetLogicalParentFromPhysicalParent(VisualElement physicalParent)
        => physicalParent.GetFirstAncestorWhere(ve => ve.contentContainer == physicalParent) ?? physicalParent;

    /// <summary>
    /// Whether the clipboard holds content that can be pasted — either pending cut elements or valid UXML.
    /// </summary>
    public static bool CanPasteContent()
    {
        var cut = Clipboard.GetClipboardForStage()?.GetCutElements();
        return (cut != null && cut.Count > 0) || Clipboard.IsSystemCopyBufferUxml();
    }

    /// <summary>
    /// Pastes the current clipboard UXML content under <paramref name="parentAsset"/>.
    /// Returns <see langword="false"/> when the clipboard is empty, the content is not valid UXML,
    /// or the paste would create a circular template dependency.
    /// </summary>
    public static bool TryPasteCopied(object source, VisualElement parentElement, VisualElementAsset parentAsset)
    {
        if (!Clipboard.IsSystemCopyBufferUxml())
            return false;

        try
        {
            var targetDocument = parentAsset.visualTreeAsset;
            if (CopiedContentWouldCauseCircularDependency(parentElement, targetDocument, Clipboard.SystemCopyBuffer))
                return false;

            PasteElementsCommand.Execute(source, Clipboard.SystemCopyBuffer, parentAsset);
            UIToolkitStageUtility.ScopePendingSelectionRequestsTo(parentElement);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
