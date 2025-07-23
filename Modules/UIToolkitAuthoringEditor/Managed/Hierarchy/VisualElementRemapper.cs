// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly struct VisualElementRemap
{
    public VisualElementRemap(VisualElement previous, VisualElement remapped)
    {
        Previous = previous;
        Remapped = remapped;
    }

    public readonly VisualElement Previous;
    public readonly VisualElement Remapped;
}

internal static class VisualElementRemapper
{
    internal static readonly PropertyName k_UIDocumentId = "unity-ui-document-global-id";

    private readonly struct RemapContext : IEquatable<RemapContext>
    {
        private readonly VisualTreeAsset m_VisualTreeAsset;
        private readonly TemplateAsset m_TemplateAsset;
        private readonly VisualElementAsset m_VisualElementAsset;
        private readonly GlobalObjectId m_Document;
        public bool Remappable { get; }

        public RemapContext(VisualElement element)
        {
            m_VisualTreeAsset = element.visualTreeAssetSource;
            m_TemplateAsset = element.templateAsset;
            m_VisualElementAsset = element.visualElementAsset;
            m_Document = ExtractGlobalObjectID(element);
            Remappable = element.visualTreeAssetSource || !m_Document.Equals(default);
        }

        public bool Equals(RemapContext other)
        {
            return m_VisualTreeAsset == other.m_VisualTreeAsset &&
                   m_TemplateAsset == other.m_TemplateAsset &&
                   m_VisualElementAsset == other.m_VisualElementAsset &&
                   m_Document.Equals(other.m_Document);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(m_VisualTreeAsset, m_TemplateAsset, m_VisualElementAsset, m_Document);
        }

        static GlobalObjectId ExtractGlobalObjectID(VisualElement element)
        {
            if (!element.HasProperty(k_UIDocumentId))
                return default;
            return (GlobalObjectId)element.GetProperty(k_UIDocumentId);
        }
    }

    public static void Remap(HashSet<VisualElement> addedOrMoved, HashSet<VisualElement> removed, List<VisualElementRemap> remappings)
    {
        remappings.Clear();

        using var handle = DictionaryPool<RemapContext, VisualElement>.Get(out var remapContext);
        using var conflictsHandle = DictionaryPool<RemapContext, List<VisualElement>>.Get(out var remapConflicts);

        foreach (var element in removed)
        {
            var remap = new RemapContext(element);
            if (remap.Remappable)
            {
                if (!remapContext.TryAdd(remap, element))
                {
                    if (!remapConflicts.TryGetValue(remap, out var conflictList))
                    {
                        remapConflicts.Add(remap, conflictList = new List<VisualElement>{remapContext[remap]});
                    }
                    conflictList.Add(element);
                }
            }
        }

        // We need to take a deeper look at conflicts. For now, don't remap those.
        foreach (var conflict in remapConflicts.Keys)
        {
            remapContext.Remove(conflict);
        }

        foreach (var element in addedOrMoved)
        {
            if (element is UIDocumentRootElement uiDocumentRootElement)
            {
                element.SetProperty(k_UIDocumentId, GlobalObjectId.GetGlobalObjectIdSlow(uiDocumentRootElement.document));
            }

            var context = new RemapContext(element);
            if (!context.Remappable)
                continue;

            if (remapContext.TryGetValue(context, out var p))
            {
                remappings.Add(new VisualElementRemap(p, element));
            }
        }

        foreach (var list in remapConflicts.Values)
        {
            ListPool<VisualElement>.Release(list);
        }
    }
}
