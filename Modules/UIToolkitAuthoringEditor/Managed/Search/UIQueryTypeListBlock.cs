// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.Search;
using UnityEditor.UIElements;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[QueryListBlock("VisualElement", "type", "uit", ":")]
internal sealed class UIQueryTypeListBlock : QueryListBlock
{
    private static readonly List<SearchProposition> k_ElementTypePropositions = new ();

    [InitializeOnLoadMethod, UsedImplicitly]
    static void RegisterTypes()
    {
        k_ElementTypePropositions.Clear();
        // [TODO] MP: Force sync. Can be removed once we switch to the generate attribute descriptions.
        UxmlSerializedDataRegistry.GetDescription("UnityEngine.UIElements.VisualElement");

        // [TODO] MP: Populate a list of runtime-compatible and editor-only types separately.
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
    }

    public UIQueryTypeListBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr)
        : base(source, id, value, attr)
    {
        icon = UIResources.GetIconForType(typeof(VisualElement), UIResources.RequestSize.Px16).texture;
    }

    public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags f = SearchPropositionFlags.None)
    {
        return k_ElementTypePropositions;
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
}
