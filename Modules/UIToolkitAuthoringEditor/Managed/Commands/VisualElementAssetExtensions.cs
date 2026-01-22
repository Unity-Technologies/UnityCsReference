// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal static class VisualElementAssetExtensions
{
    public static UxmlObjectAsset FindUxmlBinding(this VisualElementAsset element, string property)
    {
        using var _ = ListPool<UxmlObjectAsset>.Get(out var uxmlObjectAssets);
        element.GetChildrenUxmlObjectAssets(uxmlObjectAssets);

        if (uxmlObjectAssets.Count == 0)
            return null;

        var description = UxmlSerializedDataRegistry.GetDescription(typeof(VisualElement).FullName);
        var attributeDescription = description.FindAttributeWithPropertyName("bindings");

        foreach (var obj in uxmlObjectAssets)
        {
            var fullType = obj.fullTypeName;
            var rootName = (attributeDescription as UxmlSerializedUxmlObjectAttributeDescription)?.rootName ??
                attributeDescription.name;

            if (obj.isField && fullType == rootName)
            {
                using var listPool = ListPool<UxmlObjectAsset>.Get(out var bindingsUxmlObjectAssets);
                obj.GetChildrenUxmlObjectAssets(bindingsUxmlObjectAssets);

                foreach (var bindingObj in bindingsUxmlObjectAssets)
                {
                    if (bindingObj.GetAttributeValue("property") == property)
                        return bindingObj;
                }
            }
            else if (obj.GetAttributeValue("property") == property)
            {
                return obj;
            }
        }

        return null;
    }

    public static void RemoveBinding(this VisualElementAsset element, string property)
    {
        var uxmlBinding = element.FindUxmlBinding(property);
        if (uxmlBinding == null) return;
        uxmlBinding.RemoveAssetAndFieldParentIfEmpty();
    }

    public static void FindAllStyleBindings(this VisualElementAsset element, List<string> styleBindings)
    {
        using var _ = ListPool<UxmlObjectAsset>.Get(out var uxmlObjectAssets);
        element.GetChildrenUxmlObjectAssets(uxmlObjectAssets);

        if (uxmlObjectAssets.Count == 0)
            return;

        var description = UxmlSerializedDataRegistry.GetDescription(typeof(VisualElement).FullName);
        var attributeDescription = description.FindAttributeWithPropertyName("bindings");

        foreach (var obj in uxmlObjectAssets)
        {
            var fullType = obj.fullTypeName;
            var rootName = (attributeDescription as UxmlSerializedUxmlObjectAttributeDescription)?.rootName ??
                attributeDescription.name;

            if (obj.isField && fullType == rootName)
            {
                using var listPool = ListPool<UxmlObjectAsset>.Get(out var bindingsUxmlObjectAssets);
                obj.GetChildrenUxmlObjectAssets(bindingsUxmlObjectAssets);

                foreach (var bindingObj in bindingsUxmlObjectAssets)
                {
                    var property = bindingObj.GetAttributeValue("property");
                    if (property != null && property.StartsWith("style."))
                        styleBindings.Add(property);
                }
            }
            else
            {
                var property = obj.GetAttributeValue("property");
                if (property != null && property.StartsWith("style."))
                    styleBindings.Add(property);
            }
        }
    }
}
