// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

static class ElementHierarchyUtility
{
    public static UxmlSerializedDataDescription GetDescription(Type elementType)
    {
        if (elementType == null || !typeof(VisualElement).IsAssignableFrom(elementType))
            return null;
        return UxmlSerializedDataRegistry.GetDescription(elementType.FullName);
    }

    public static bool AcceptsChildrenOf(UxmlSerializedDataDescription description, UxmlAsset asset)
    {
        for (var i = 0; i < asset.childCount; ++i)
        {
            if (asset[i] is VisualElementAsset child && !AcceptsChild(description, child))
                return false;
        }
        return true;
    }

    public static bool AcceptsChild(UxmlSerializedDataDescription description, VisualElementAsset child)
    {
        var childType = UxmlSerializedDataRegistry.GetDescription(child.fullTypeName)?.serializedDataType?.DeclaringType;
        return childType == null || description.IsSupportedChild(childType);
    }

    public static bool AcceptsChildOfType(UxmlAsset parent, Type childType)
    {
        var description = parent is VisualElementAsset parentAsset
            ? UxmlSerializedDataRegistry.GetDescription(parentAsset.fullTypeName)
            : null;
        return description == null || description.IsSupportedChild(childType);
    }
}
