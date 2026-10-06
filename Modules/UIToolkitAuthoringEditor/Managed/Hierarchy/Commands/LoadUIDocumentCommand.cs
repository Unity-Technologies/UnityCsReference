// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal enum SubDocumentOptions
{
    None,
    InContext,
    Isolation,
}

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal sealed class LoadUIDocumentCommand : Command<LoadUIDocumentCommand>
{
    public VisualTreeAsset Document { get; private set; }
    public List<VisualTreeAsset> SubDocuments { get; private set; }
    public List<TemplateAsset> ContextInstances { get; private set; }
    public SubDocumentOptions Options { get; private set; }
    public int SelectedId { get; private set; }

    /// <summary>
    /// Innermost-first TemplateAsset ids of the selected element's enclosing instances, up to but
    /// excluding the instance of the opened document.
    /// </summary>
    /// <remarks>
    /// Senders that record no chain leave this null, and <see cref="FindSelectedElement"/> falls back
    /// to the first id match.
    /// </remarks>
    public List<int> SelectedInstanceIds { get; private set; }

    /// <summary>
    /// The document containing the selected element's asset, which resolution also requires to match.
    /// </summary>
    /// <remarks>
    /// Asset ids are unique only within one document. Senders that record none leave this null, which
    /// skips the check.
    /// </remarks>
    public VisualTreeAsset SelectedSourceDocument { get; private set; }

    public static LoadUIDocumentCommand GetPooled(
        object source,
        VisualTreeAsset document,
        int selectedId = -1,
        VisualTreeAsset selectedSourceDocument = null,
        List<int> selectedInstanceIds = null,
        SubDocumentOptions subDocumentOptions = SubDocumentOptions.None,
        List<VisualTreeAsset> subDocuments = null,
        List<TemplateAsset> contextInstances = null)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.Document = document;
        cmd.SelectedId = selectedId;
        cmd.SelectedSourceDocument = selectedSourceDocument;
        cmd.SelectedInstanceIds = selectedInstanceIds;
        cmd.Options = subDocumentOptions;
        cmd.SubDocuments = subDocuments;
        cmd.ContextInstances = contextInstances;
        return cmd;
    }

    public static void Execute(
        object source,
        VisualTreeAsset document,
        int selectedId = -1,
        VisualTreeAsset selectedSourceDocument = null,
        List<int> selectedInstanceIds = null,
        SubDocumentOptions subDocumentOptions = SubDocumentOptions.None,
        List<VisualTreeAsset> subDocuments = null,
        List<TemplateAsset> contextInstances = null)
    {
        using var command = GetPooled(source, document, selectedId, selectedSourceDocument,
            selectedInstanceIds, subDocumentOptions, subDocuments, contextInstances);
        UICommandQueue.Execute(command);
    }

    protected override void Init()
    {
        base.Init();
        Document = null;
        SubDocuments = null;
        ContextInstances = null;
        Options = SubDocumentOptions.None;
        SelectedId = -1;
        SelectedInstanceIds = null;
        SelectedSourceDocument = null;
    }

    public override bool Validate()
    {
        if (Document == null || Document.importedWithErrors)
            return false;

        if (Options != SubDocumentOptions.None)
        {
            if (SubDocuments == null || SubDocuments.Count == 0)
                return false;
            if (Options == SubDocumentOptions.InContext &&
                (ContextInstances == null || ContextInstances.Count < SubDocuments.Count))
                return false;
        }

        if (SubDocuments != null)
        {
            foreach (var doc in SubDocuments)
            {
                if (doc == null || doc.importedWithErrors)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Collects the enclosing-instance ids of the element, innermost first, stopping below the
    /// enclosing instance of the document about to be opened.
    /// </summary>
    public static List<int> GetInstanceIds(VisualElement element, VisualTreeAsset document)
    {
        var ids = new List<int>();
        for (var ancestor = element?.hierarchy.parent; ancestor != null; ancestor = ancestor.hierarchy.parent)
        {
            if (ancestor is IPanelComponentRootElement)
                break;

            // Ordinary ancestors are not instance boundaries; only the panel root or the opened
            // document's own instance ends the chain.
            if (ancestor is not TemplateContainer container)
                continue;

            if (container.templateSource == document)
                break;

            if (container.visualElementAsset is TemplateAsset templateAsset)
                ids.Add(templateAsset.id);
        }

        return ids;
    }

    /// <summary>
    /// Resolves the selected element under <paramref name="root"/>: the id match whose
    /// enclosing-instance chain below the root equals <paramref name="instanceIds"/>, falling back
    /// to the first id match for senders that record no chain. When
    /// <paramref name="selectedSourceDocument"/> is given, only elements of that document match.
    /// </summary>
    public static VisualElement FindSelectedElement(VisualElement root, int selectedId, List<int> instanceIds,
        VisualTreeAsset selectedSourceDocument = null)
    {
        if (root == null)
            return null;

        VisualElement fallback = null;
        return Search(root) ?? fallback;

        VisualElement Search(VisualElement candidate)
        {
            // The root stands for the canvas, never for the selected element.
            if (candidate != root && candidate.visualElementAsset is { } candidateAsset
                && candidateAsset.id == selectedId
                && (selectedSourceDocument == null || candidateAsset.visualTreeAsset == selectedSourceDocument))
            {
                fallback ??= candidate;
                if (InstanceChainMatches(candidate, root, instanceIds))
                    return candidate;
            }

            for (var i = 0; i < candidate.hierarchy.childCount; ++i)
            {
                if (Search(candidate.hierarchy[i]) is { } found)
                    return found;
            }

            return null;
        }
    }

    static bool InstanceChainMatches(VisualElement element, VisualElement root, List<int> instanceIds)
    {
        if (instanceIds == null)
            return false;

        int index = 0;
        for (var ancestor = element.hierarchy.parent; ancestor != null && ancestor != root; ancestor = ancestor.hierarchy.parent)
        {
            if (ancestor is not TemplateContainer { visualElementAsset: TemplateAsset templateAsset })
                continue;

            if (index >= instanceIds.Count || instanceIds[index] != templateAsset.id)
                return false;

            ++index;
        }

        return index == instanceIds.Count;
    }
}
