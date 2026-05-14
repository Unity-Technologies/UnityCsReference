// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Result of synchronizing a serialized property path with UXML assets.
/// </summary>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal struct SynchronizePathResult
{
    public bool success { get; set; }
    public UxmlAsset uxmlAsset { get; set; }
    public object serializedData { get; set; }
    public UxmlSerializedDataDescription dataDescription { get; set; }
    public UxmlSerializedAttributeDescription attributeDescription { get; set; }
    public object attributeOwner { get; set; }
    public string propertyPath { get; set; }
}

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal static class UxmlAssetUtilities
{
    const string k_ArraySizePart = "size";
    static readonly Dictionary<string, string[]> s_PathPartsCache = new();
    static readonly List<UxmlObjectAsset> s_TempUxmlAssets = new();
    static readonly object[] s_SingleUxmlSerializedData = new object[1];

    /// <summary>
    /// Checks if a visual element is the Builder document root.
    /// </summary>
    static bool IsBuilderDocumentElement(VisualElement element)
    {
        return element.name == "document" && element.ClassListContains("unity-builder-viewport__document");
    }

    /// <summary>
    /// Synchronizes the UXML serialized data to the current UXML asset and sub-UXML objects that are part of the path.
    /// </summary>
    /// <param name="visualTreeAsset">The visual tree asset being edited.</param>
    /// <param name="uxmlSerializedData">The root serialized data being edited.</param>
    /// <param name="elementAsset">The visual element asset that owns the attributes.</param>
    /// <param name="serializedBasePath">The base path in the serialized object.</param>
    /// <param name="propertyPath">The full serialized property path.</param>
    /// <param name="changeUxmlAssets">Whether to add/remove missing UXML assets in the path. Default is true.</param>
    /// <param name="element">Optional: The visual element instance for tracking attribute owner.</param>
    /// <param name="onRecordUndo">Optional callback to record undo operations.</param>
    /// <param name="isInTemplateInstance">Whether the element is inside a template instance.</param>
    /// <param name="onDeserializeElement">Optional callback to deserialize elements after template override.</param>
    /// <param name="getVisualElementAsset">Optional callback to get visual element assets (for custom property support).</param>
    /// <returns>The synchronization result.</returns>
    public static SynchronizePathResult SynchronizePath(
        VisualTreeAsset visualTreeAsset,
        UxmlSerializedData uxmlSerializedData,
        UxmlAsset elementAsset,
        string serializedBasePath,
        string propertyPath,
        bool changeUxmlAssets = true,
        object element = null,
        Action onRecordUndo = null,
        bool isInTemplateInstance = false,
        Action<VisualTreeAsset, VisualElement> onDeserializeElement = null,
        Func<VisualElement, VisualElementAsset> getVisualElementAsset = null)
    {
        SynchronizePathResult result = default;

        if (string.IsNullOrEmpty(propertyPath) || !propertyPath.StartsWith(serializedBasePath, StringComparison.Ordinal))
            return result;

        // Cache the split so we don't have to do it every time.
        if (!s_PathPartsCache.TryGetValue(propertyPath, out var pathParts))
        {
            if (serializedBasePath == propertyPath)
            {
                pathParts = Array.Empty<string>();
            }
            else
            {
                pathParts = propertyPath[(serializedBasePath.Length + 1)..].Split('.');
            }

            s_PathPartsCache[propertyPath] = pathParts;
        }

        object currentUxmlSerializedData = uxmlSerializedData;
        UxmlAsset currentAttributesUxmlOwner = elementAsset;
        result.attributeOwner = element;
        result.propertyPath = propertyPath;

        for (int i = 0; i < pathParts.Length; ++i)
        {
            if (currentUxmlSerializedData == null)
            {
                continue;
            }

            // Is the current value a list?
            if (currentUxmlSerializedData is IList serializedDataList)
            {
                // Find the item index from the path and extract it.
                var dataPath = pathParts[i + 1];

                // Targeting the Array.size, nothing more to sync. The array size was synchronized in the previous
                // loop and we don't want to return anything here because the result at this path is not part of UxmlSerializedData.
                if (dataPath == k_ArraySizePart)
                {
                    result.success = false;
                    return result;
                }

                var arrayItemIndexStart = dataPath.IndexOf('[') + 1;
                var arrayItemIndexEnd = dataPath.IndexOf(']');
                var indexString = dataPath.Substring(arrayItemIndexStart, arrayItemIndexEnd - arrayItemIndexStart);
                var listIndex = int.Parse(indexString);

                currentAttributesUxmlOwner = s_TempUxmlAssets[listIndex];
                currentUxmlSerializedData = serializedDataList[listIndex];

                if (result.attributeOwner is IList attributeOwnerList && listIndex < attributeOwnerList.Count)
                {
                    result.attributeOwner = attributeOwnerList[listIndex];
                }
                else
                {
                    result.attributeOwner = null;
                }

                i += 1;
                continue;
            }

            result.dataDescription = UxmlSerializedDataRegistry.GetDescription(currentUxmlSerializedData.GetType().DeclaringType.FullName);

            var name = pathParts[i];
            result.attributeDescription = result.dataDescription.FindAttributeWithPropertyName(name);
            var attributeObjectDescription = result.attributeDescription as UxmlSerializedUxmlObjectAttributeDescription;
            if (attributeObjectDescription == null)
                break;

            if (result.attributeOwner != null)
            {
                result.attributeDescription.TryGetValueFromObject(result.attributeOwner, out var updatedAttributeOwner);
                result.attributeOwner = updatedAttributeOwner;
            }

            var parentUxmlSerializedData = currentUxmlSerializedData as UxmlSerializedData;
            currentUxmlSerializedData = result.attributeDescription.GetSerializedValue(parentUxmlSerializedData);
            var uxmlSerializedDataList = currentUxmlSerializedData as IList;

            // If we are not syncing a list then its a single field but we still treat it as a list.
            if (uxmlSerializedDataList == null)
            {
                s_SingleUxmlSerializedData[0] = currentUxmlSerializedData;
                uxmlSerializedDataList = s_SingleUxmlSerializedData;
            }

            if (!SyncUxmlAssetsFromSerializedData(visualTreeAsset, uxmlSerializedDataList, parentUxmlSerializedData, currentAttributesUxmlOwner, attributeObjectDescription, changeUxmlAssets, onRecordUndo, isInTemplateInstance, element, onDeserializeElement, getVisualElementAsset))
            {
                if (!changeUxmlAssets)
                {
                    result.uxmlAsset = currentAttributesUxmlOwner;
                    result.serializedData = currentUxmlSerializedData;
                    result.success = false;
                    return result;
                }
            }

            if (!attributeObjectDescription.isList)
                currentAttributesUxmlOwner = currentUxmlSerializedData == null ? null : s_TempUxmlAssets[0];
        }

        result.uxmlAsset = currentAttributesUxmlOwner;
        result.serializedData = currentUxmlSerializedData;
        result.success = true;
        return result;
    }

    static bool SyncUxmlAssetsFromSerializedData(
        VisualTreeAsset visualTreeAsset,
        IList uxmlSerializedData,
        UxmlSerializedData parentUxmlSerialized,
        UxmlAsset parentAsset,
        UxmlSerializedUxmlObjectAttributeDescription attributeDescription,
        bool canMakeChanges,
        Action onRecordUndo,
        bool isInTemplateInstance,
        object element,
        Action<VisualTreeAsset, VisualElement> onDeserializeElement,
        Func<VisualElement, VisualElementAsset> getVisualElementAsset)
    {
        bool contentsChanged = false;

        s_TempUxmlAssets.Clear();

        using var listPool = ListPool<UxmlObjectAsset>.Get(out var collectedUxmlAssets);
        parentAsset?.CollectUxmlObjectAssets(attributeDescription.rootName, collectedUxmlAssets);

        // Sync the list by checking each item is at the expected index and moving/adding items as needed.
        using var hashSetPool = HashSetPool<int>.Get(out var duplicateIds);
        for (int j = 0; j < uxmlSerializedData.Count; ++j)
        {
            var currentSerializedData = uxmlSerializedData[j] as UxmlSerializedData;

            // Avoid adding null uxml objects when attribute description is not a list
            if (!attributeDescription.isList && currentSerializedData == null)
            {
                continue;
            }

            if (currentSerializedData != null && currentSerializedData.uxmlAssetId != 0)
            {
                // When a list element is copied it may also copy the id of the original element.
                // If the id has already been used we clear it so a new one can be assigned.
                if (duplicateIds.Contains(currentSerializedData.uxmlAssetId))
                    currentSerializedData.uxmlAssetId = 0;
            }

            // Find matching UxmlObjectAsset
            if (!ExtractOrCreateUxmlSerializedDataUxmlAsset(visualTreeAsset, currentSerializedData, parentUxmlSerialized, parentAsset,
                attributeDescription, canMakeChanges, collectedUxmlAssets, out var foundUxmlAsset, j, onRecordUndo, isInTemplateInstance, element, onDeserializeElement, getVisualElementAsset))
            {
                if (!canMakeChanges)
                    return false;
                contentsChanged = true;
            }

            duplicateIds.Add(foundUxmlAsset.id);
            s_TempUxmlAssets.Add(foundUxmlAsset);
        }

        var acceptedTypes = attributeDescription.uxmlObjectAcceptedTypes;

        // If we have uxml assets remaining then the serialized data must have been removed and we should do the same.
        foreach (var collectedUxmlAsset in collectedUxmlAssets)
        {
            if (collectedUxmlAsset == null)
                continue;

            var isAcceptedType = false;
            foreach (var acceptedType in acceptedTypes)
            {
                if (acceptedType.DeclaringType?.FullName == collectedUxmlAsset.fullTypeName)
                {
                    isAcceptedType = true;
                    break;
                }
            }

            // Do not delete the asset if it's not an accepted type.
            // This avoid deleting other objects of different types when having multiple UxmlObjectReferences with no name like in MultiColumnListView/TreeView
            if (!isAcceptedType && collectedUxmlAsset.fullTypeName != UxmlAsset.NullNodeType)
            {
                s_TempUxmlAssets.Add(collectedUxmlAsset);
                continue;
            }

            contentsChanged = true;
            onRecordUndo?.Invoke();

            // We need to do this to ensure that any dependencies are also removed.
            collectedUxmlAsset.RemoveAssetAndFieldParentIfEmpty();
        }

        if (contentsChanged)
            parentAsset.SetUxmlObjectAssets(attributeDescription.rootName, s_TempUxmlAssets);

        return true;
    }

    static bool ExtractOrCreateUxmlSerializedDataUxmlAsset(
        VisualTreeAsset visualTreeAsset,
        UxmlSerializedData uxmlSerializedData,
        UxmlSerializedData parentUxmlSerialized,
        UxmlAsset parentAsset,
        UxmlSerializedUxmlObjectAttributeDescription attributeDescription,
        bool canMakeChanges,
        List<UxmlObjectAsset> uxmlObjectAssets,
        out UxmlObjectAsset uxmlAsset,
        int expectedIndex,
        Action onRecordUndo,
        bool isInTemplateInstance,
        object element,
        Action<VisualTreeAsset, VisualElement> onDeserializeElement,
        Func<VisualElement, VisualElementAsset> getVisualElementAsset)
    {
        // If the asset id is 0 then we do not currently have a UxmlAsset for this serialized data
        if (uxmlSerializedData?.uxmlAssetId != 0)
        {
            // Check if the data is at the expected index.
            if (expectedIndex < uxmlObjectAssets.Count)
            {
                if (uxmlObjectAssets[expectedIndex] != null &&
                    ((uxmlSerializedData == null && uxmlObjectAssets[expectedIndex]?.isNull == true) ||
                    uxmlSerializedData?.uxmlAssetId == uxmlObjectAssets[expectedIndex]?.id))
                {
                    uxmlAsset = uxmlObjectAssets[expectedIndex];

                    // We dont remove the asset from the list as it will break the expected index but we do set it to null
                    uxmlObjectAssets[expectedIndex] = null;
                    return true;
                }
            }

            if (!canMakeChanges)
            {
                uxmlAsset = null;
                return false;
            }

            onRecordUndo?.Invoke();

            // See if we can find it at another index
            for (int i = 0; i < uxmlObjectAssets.Count; ++i)
            {
                if (uxmlObjectAssets[i] == null)
                    continue;

                if ((uxmlSerializedData == null && uxmlObjectAssets[i].isNull) ||
                    uxmlSerializedData?.uxmlAssetId == uxmlObjectAssets[i].id)
                {
                    uxmlAsset = uxmlObjectAssets[i];
                    uxmlObjectAssets[i] = null;
                    return false;
                }
            }
        }

        if (!canMakeChanges)
        {
            uxmlAsset = null;
            return false;
        }

        onRecordUndo?.Invoke();

        attributeDescription.SetSerializedValueAttributeFlags(parentUxmlSerialized, UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml);

        // We could not find the asset so we need to create a new one.
        uxmlAsset = CreateUxmlObjectAsset(visualTreeAsset, attributeDescription, uxmlSerializedData, parentAsset, isInTemplateInstance, onRecordUndo, element, onDeserializeElement, getVisualElementAsset);

        return false;
    }

    static UxmlObjectAsset CreateUxmlObjectAsset(
        VisualTreeAsset visualTreeAsset,
        UxmlSerializedUxmlObjectAttributeDescription attribute,
        UxmlSerializedData serializedData,
        UxmlAsset parentAsset,
        bool isInTemplateInstance = false,
        Action onRecordUndo = null,
        object element = null,
        Action<VisualTreeAsset, VisualElement> onDeserializeElement = null,
        Func<VisualElement, VisualElementAsset> getVisualElementAsset = null)
    {
        var fullTypeName = serializedData == null ? UxmlAsset.NullNodeType : serializedData.GetType().DeclaringType.FullName;
        var xmlns = visualTreeAsset.FindUxmlNamespaceDefinitionForTypeName(parentAsset, fullTypeName);
        var uxmlAsset = visualTreeAsset.AddUxmlObject(parentAsset, attribute.rootName, fullTypeName, xmlns);

        // Assign the new asset id to the serialized data
        if (serializedData != null)
        {
            // Recursively sync nested UXML objects and non-default attributes
            SyncSerializedDataToNewUxmlAsset(visualTreeAsset, serializedData, uxmlAsset, isInTemplateInstance, onRecordUndo, element, onDeserializeElement, getVisualElementAsset);
            serializedData.uxmlAssetId = uxmlAsset.id;
        }

        return uxmlAsset;
    }

    /// <summary>
    /// Sets an attribute value on a UXML asset with support for template instance overrides.
    /// </summary>
    /// <param name="attributeName">The name of the attribute to set.</param>
    /// <param name="value">The string value to set.</param>
    /// <param name="visualTreeAsset">The visual tree asset being edited.</param>
    /// <param name="uxmlAsset">The UXML asset to set the attribute on.</param>
    /// <param name="isInTemplateInstance">Whether the element is inside a template instance.</param>
    /// <param name="element">The visual element instance (required for template overrides).</param>
    /// <param name="onRecordUndo">Optional callback to record undo operations.</param>
    /// <param name="onDeserializeElement">Optional callback to deserialize elements after template override. Called with (visualTreeAsset, element).</param>
    /// <param name="getVisualElementAsset">Optional callback to get visual element assets (for custom property support).</param>
    public static void PostAttributeValueChange(
        string attributeName,
        string value,
        VisualTreeAsset visualTreeAsset,
        UxmlAsset uxmlAsset,
        bool isInTemplateInstance = false,
        object element = null,
        Action onRecordUndo = null,
        Action<VisualTreeAsset, VisualElement> onDeserializeElement = null,
        Func<VisualElement, VisualElementAsset> getVisualElementAsset = null)
    {
        onRecordUndo?.Invoke();

        // Set value in asset.
        if (isInTemplateInstance && element is VisualElement visualElement)
        {
            var templateContainerParent = GetVisualElementRootTemplate(visualElement, getVisualElementAsset);

            if (templateContainerParent != null)
            {
                var templateAsset = (getVisualElementAsset != null ? getVisualElementAsset(templateContainerParent) : templateContainerParent.visualElementAsset) as TemplateAsset;
                var currentVisualElementName = visualElement.name;

                if (!string.IsNullOrEmpty(currentVisualElementName) && templateAsset != null)
                {
                    var pathToTemplateAsset = GetPathToTemplateAsset(templateAsset, visualElement, getVisualElementAsset);
                    templateAsset.SetAttributeOverride(attributeName, value, pathToTemplateAsset);

                    var elementsToChange = templateContainerParent.Query(currentVisualElementName).Where(v => v.GetType() == visualElement.GetType());
                    elementsToChange.ForEach(x =>
                    {
                        var templateVea = x.visualElementAsset;

                        if (templateVea == null)
                            return;

                        UxmlSerializer.CreateSerializedDataOverrides(visualTreeAsset);
                        onDeserializeElement?.Invoke(visualTreeAsset, x);
                    });
                }
            }
        }
        else
        {
            uxmlAsset.SetAttribute(attributeName, value);
        }
    }

    /// <summary>
    /// Checks if any serialized data fields are not set to their default values. If any are not, apply those changes to the UXML asset.
    /// This recursively processes nested UXML objects.
    /// </summary>
    /// <param name="visualTreeAsset">The visual tree asset being edited.</param>
    /// <param name="uxmlSerializedData">The serialized data to check for non-default values.</param>
    /// <param name="uxmlAsset">The asset to apply the uxml attributes to.</param>
    /// <param name="isInTemplateInstance">Whether the element is inside a template instance.</param>
    /// <param name="onRecordUndo">Optional callback to record undo operations.</param>
    /// <param name="element">The visual element instance (required for template overrides).</param>
    /// <param name="onDeserializeElement">Optional callback to deserialize elements after template override.</param>
    /// <param name="getVisualElementAsset">Optional callback to get visual element assets (for custom property support).</param>
    static void SyncSerializedDataToNewUxmlAsset(
        VisualTreeAsset visualTreeAsset,
        UxmlSerializedData uxmlSerializedData,
        UxmlAsset uxmlAsset,
        bool isInTemplateInstance = false,
        Action onRecordUndo = null,
        object element = null,
        Action<VisualTreeAsset, VisualElement> onDeserializeElement = null,
        Func<VisualElement, VisualElementAsset> getVisualElementAsset = null)
    {
        if (uxmlSerializedData == null)
            return;

        var description = UxmlSerializedDataRegistry.GetDescription(uxmlSerializedData.GetType().DeclaringType.FullName);
        foreach (var attribute in description.serializedAttributes)
        {
            if (attribute.isUxmlObject)
            {
                var attributeUxmlObjectDescription = attribute as UxmlSerializedUxmlObjectAttributeDescription;
                attribute.SetSerializedValueAttributeFlags(uxmlSerializedData, UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml);
                
                if (attribute.isList)
                {
                    // Extract the serialized data list
                    var serializedDataList = (IList)attribute.GetSerializedValue(uxmlSerializedData);
                    foreach (UxmlSerializedData serializedDataItem in serializedDataList)
                    {
                        CreateUxmlObjectAsset(visualTreeAsset, attributeUxmlObjectDescription, serializedDataItem, uxmlAsset);
                    }
                }
                else
                {
                    var serializedData = attribute.GetSerializedValue(uxmlSerializedData) as UxmlSerializedData;

                    // Avoid creating null objects when attribute description is not a list
                    if (serializedData != null)
                    {
                        CreateUxmlObjectAsset(visualTreeAsset, attributeUxmlObjectDescription, serializedData, uxmlAsset);
                    }
                }
            }
            else
            {
                var attributeValue = attribute.GetSerializedValue(uxmlSerializedData);
                if (!UxmlAttributeComparison.ObjectEquals(attributeValue, attribute.defaultValue))
                {
                    attribute.SetSerializedValueAttributeFlags(uxmlSerializedData, UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml);

                    if (attributeValue == null || !UxmlAttributeConverter.TryConvertToString(attributeValue, visualTreeAsset, out var stringValue))
                        stringValue = attributeValue?.ToString();

                    PostAttributeValueChange(attribute.name, stringValue, visualTreeAsset, uxmlAsset, isInTemplateInstance, element, onRecordUndo, onDeserializeElement, getVisualElementAsset);
                }
            }
        }
    }

    /// <summary>
    /// Gets the path from a visual element to a template asset.
    /// </summary>
    /// <param name="templateAsset">The template asset to find the path to.</param>
    /// <param name="element">The visual element to start from.</param>
    /// <param name="getVisualElementAsset">Optional callback to get visual element assets (for custom property support).</param>
    public static string[] GetPathToTemplateAsset(TemplateAsset templateAsset, VisualElement element, Func<VisualElement, VisualElementAsset> getVisualElementAsset = null)
    {
        var path = new List<string> { element.name };
        var parent = element.parent;
        var parentAsset = getVisualElementAsset != null ? getVisualElementAsset(parent) : parent?.visualElementAsset;

        while (parent != null && parentAsset != templateAsset)
        {
            if (!string.IsNullOrEmpty(parent.name) && parent is TemplateContainer)
            {
                path.Insert(0, parent.name);
            }

            parent = parent.parent;
            parentAsset = getVisualElementAsset != null ? getVisualElementAsset(parent) : parent?.visualElementAsset;
        }

        return parentAsset != templateAsset ? null : path.ToArray();
    }

    /// <summary>
    /// Gets the root template container for a visual element.
    /// </summary>
    /// <param name="visualElement">The visual element to find the root template for.</param>
    /// <param name="getVisualElementAsset">Optional callback to get visual element assets (for custom property support).</param>
    /// <returns>The root template container, or null if not found.</returns>
    public static TemplateContainer GetVisualElementRootTemplate(VisualElement visualElement, Func<VisualElement, VisualElementAsset> getVisualElementAsset = null)
    {
        TemplateContainer templateContainerParent = null;
        var parent = visualElement.parent;

        while (parent != null)
        {
            // Check if it's a TemplateContainer with a visual element asset
            if (parent is TemplateContainer templateContainer)
            {
                var asset = getVisualElementAsset != null ? getVisualElementAsset(templateContainer) : templateContainer.visualElementAsset;
                if (asset != null)
                {
                    templateContainerParent = templateContainer;
                }
            }

            if (IsBuilderDocumentElement(parent))
            {
                break;
            }

            parent = parent.parent;
        }

        return templateContainerParent;
    }

    /// <summary>
    /// Clears the path parts cache. Call this when switching between different documents.
    /// </summary>
    public static void ClearCache()
    {
        s_PathPartsCache.Clear();
    }
}
