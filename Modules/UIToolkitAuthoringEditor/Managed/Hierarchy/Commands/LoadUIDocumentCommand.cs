// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine.Bindings;

namespace Unity.UIToolkit.Editor;

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal enum SubDocumentOptions
{
    None,
    InContext,
    Isolation,
}

[System.Serializable]
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal class LoadUIDocumentCommand
{
    public const string CommandId = "UIToolkit__LoadDocument__CommandId";

    public List<VisualTreeAsset> subDocuments;
    public List<TemplateAsset> contextInstances;
    public SubDocumentOptions subDocumentOptions;
    public int selectedId = -1;

    /// <summary>
    /// Innermost-first TemplateAsset ids of the selected element's enclosing instances, up to but
    /// excluding the instance of the opened document.
    /// </summary>
    /// <remarks>
    /// Senders that record no chain leave it empty, which the JSON round-trip also produces from null.
    /// </remarks>
    public List<int> selectedInstanceIds;

    /// <summary>
    /// The document containing the selected element's asset, which resolution also requires to match.
    /// </summary>
    /// <remarks>
    /// Asset ids are unique only within one document. Senders that record none leave it null, which
    /// skips the check.
    /// </remarks>
    public VisualTreeAsset selectedSourceDocument;

    /// <summary>
    /// Opens the document in the UI Builder, handing this command over for it to consume.
    /// </summary>
    public void OpenInBuilder(VisualTreeAsset document)
    {
        SessionState.SetString(CommandId, EditorJsonUtility.ToJson(this));
        try
        {
            AssetDatabase.OpenAsset(document.GetEntityId());
        }
        finally
        {
            SessionState.EraseString(CommandId);
        }
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
