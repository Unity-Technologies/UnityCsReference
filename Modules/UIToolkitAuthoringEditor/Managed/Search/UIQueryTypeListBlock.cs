// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitAuthoringFramework not yet converted
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Search;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Unity.Scripting.LifecycleManagement;

namespace Unity.UIToolkit.Editor;

[QueryListBlock("VisualElement", "type", "uit", ":")]
internal sealed class UIQueryTypeListBlock : QueryListBlock
{
    [NoAutoStaticsCleanup] // scratch list cleared per use, safe to persist
    private static readonly List<SearchProposition> k_ElementTypePropositions = new ();

    public UIQueryTypeListBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr)
        : base(source, id, value, attr)
    {
        icon = UIResources.GetIconForType(typeof(VisualElement), UIResources.RequestSize.Px16).texture;
    }

    public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags f = SearchPropositionFlags.None)
    {
        return GetOrGenerateSearchPropositions();
    }

    public override void Apply(in SearchProposition searchProposition)
    {
        value = searchProposition.label;
        if (searchProposition.data is Type t)
            icon = UIResources.GetIconForType(t, UIResources.RequestSize.Px16).texture;
        else
            icon = UIResources.GetIconForType(typeof(VisualElement), UIResources.RequestSize.Px16).texture;

        ApplyChanges();
    }

    static IEnumerable<SearchProposition> GetOrGenerateSearchPropositions()
    {
        if (k_ElementTypePropositions.Count > 0)
            return k_ElementTypePropositions;

        foreach (var kvp in UxmlSerializedDataRegistry.SerializedDataTypes)
        {
            var elementType = kvp.Value.DeclaringType;
            var elementTypeName = GetOrCreateTypeData(elementType);
            k_ElementTypePropositions.Add(new SearchProposition(
                label:$"{elementTypeName.typeName}",
                replacement:$"{UISearchTokens.TypeSearchToken}:{elementTypeName.typeName}",
                category:"UI",
                help:"Search by element type",
                priority:-1,
                icon:UIResources.GetIconForType(elementType, UIResources.RequestSize.Px16).texture,
                data:elementType));
        }

        return k_ElementTypePropositions;
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
