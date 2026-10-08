// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.SmartStrings.Editor;

static class ManagedReferenceUI
{
    public static ListView CreateList(string title, string tooltip) => new ListView
    {
        showFoldoutHeader = true,
        headerTitle = title,
        tooltip = tooltip,
        showAddRemoveFooter = true,
        reorderable = true,
        reorderMode = ListViewReorderMode.Animated,
        showBoundCollectionSize = false,
        horizontalScrollingEnabled = false,
        virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
        selectionType = SelectionType.Single,
        style = { marginTop = 4, flexShrink = 1, minWidth = 0 }
    };

    public static List<Type> ConcreteTypes(Type baseType)
    {
        var types = new List<Type>();
        foreach (var type in TypeCache.GetTypesDerivedFrom(baseType))
        {
            if (type.IsAbstract || type.IsGenericType)
                continue;
            // Managed references cannot be UnityEngine.Object instances.
            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                continue;
            if (Attribute.IsDefined(type, typeof(ObsoleteAttribute)))
                continue;
            // Needs a parameterless constructor to be instantiated and serialized as a managed reference.
            if (type.GetConstructor(Type.EmptyTypes) == null)
                continue;
            types.Add(type);
        }
        types.Sort((a, b) => string.Compare(DisplayName(a), DisplayName(b), StringComparison.Ordinal));
        return types;
    }

    public static string DisplayName(Type type) => L10n.Tr(ObjectNames.NicifyVariableName(type.Name), null);
}
