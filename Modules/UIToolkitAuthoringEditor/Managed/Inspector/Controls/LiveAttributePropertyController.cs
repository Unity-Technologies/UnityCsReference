// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

class LiveAttributePropertyController
{
    public abstract class PropertyDriver<T> : ScriptableObject where T : PropertyDriver<T>
    {
        static T s_Driver;

        public static T instance => s_Driver;

        public static T GetOrCreateInstance()
        {
            if (s_Driver == null)
            {
                s_Driver = CreateInstance<T>();
                s_Driver.hideFlags = HideFlags.HideAndDontSave;
            }
            return s_Driver;
        }
    }

    public class LivePropertyDriver : PropertyDriver<LivePropertyDriver> { }
    public class BoundPropertyDriver : PropertyDriver<BoundPropertyDriver> { }

    enum LiveAttributePropertyModificationValueType
    {
        BoxedValue,
        ManagedReferenceValue,
        ManagedReferenceArrayValue,
    }

    struct LiveAttributePropertyModification
    {
        public UxmlSerializedAttributeDescription attribute;
        public SerializedProperty property;
        public LiveAttributePropertyModificationValueType valueType;
        public object value;
    }

    string m_UxmlSerializedDataSnapshot;

    public UxmlAttributesEditingContext context { get; set; }

    /// <summary>
    /// Unregisters all driven properties, reverting the inspector to display UXML data instead of live or bound values.
    /// Call <seealso cref="SyncLiveProperties"/> to sync live or bound values to the serialized data again.
    /// </summary>
    public void RemoveLiveProperties()
    {
        if (BoundPropertyDriver.instance != null)
            DrivenPropertyManager.UnregisterProperties(BoundPropertyDriver.instance);
        if (LivePropertyDriver.instance != null)
            DrivenPropertyManager.UnregisterProperties(LivePropertyDriver.instance);

        if (m_UxmlSerializedDataSnapshot != null)
        {
            // Revert unsupported changes by restoring the serialized data from the snapshot
            EditorJsonUtility.FromJsonOverwrite(m_UxmlSerializedDataSnapshot, context.uxmlSerializedData);
            context.rootSerializedObject.Update();
            m_UxmlSerializedDataSnapshot = null;
        }
        else
        {
            context.rootSerializedObject?.UpdateIfRequiredOrScript();
        }
    }

    /// <summary>
    /// Syncs live attribute values from the element and bound values to the serialized data.
    /// These values are marked as driven properties to make changes non-destructive to the underlying UXML data.
    /// Call <seealso cref="RemoveLiveProperties"/> to revert the inspector to display UXML data instead of live or bound values.
    /// </summary>
    public void SyncLiveProperties()
    {
        var serializedDataProperty = context.rootSerializedObject.FindProperty(context.serializedBasePath);

        using var pooledList = ListPool<LiveAttributePropertyModification>.Get(out var propertyModifications);
        var hasUnsupportedDrivenPropertyChange = false;
        foreach (var attribute in context.uxmlSerializedDataDescription.serializedAttributes)
        {
            var property = serializedDataProperty.FindPropertyRelative(attribute.serializedField.Name);
            CollectLiveAttributeValuePropertyModifications(context.element, context.uxmlSerializedData, attribute, property, propertyModifications, ref hasUnsupportedDrivenPropertyChange);
        }

        if (propertyModifications.Count > 0)
        {
            // Collect existing bindings
            using var listHandle = ListPool<BindingInfo>.Get(out var bindingInfos);
            using var hashHandle = HashSetPool<string>.Get(out var resolvedBindingLookup);
            context.element.GetBindingInfos(bindingInfos);
            foreach (var info in bindingInfos)
            {
                if (context.element.TryGetLastBindingToUIResult(info.bindingId, out var bindingResult) &&
                    bindingResult.status == BindingStatus.Success)
                {
                    resolvedBindingLookup.Add(info.bindingId);
                }
            }

            if (hasUnsupportedDrivenPropertyChange)
            {
                // Take a snapshot of the current serialized data to revert unsupported changes later
                if (m_UxmlSerializedDataSnapshot == null && context.tempSerializedData == null)
                    m_UxmlSerializedDataSnapshot = EditorJsonUtility.ToJson(context.uxmlSerializedData);
            }

            foreach (var modification in propertyModifications)
            {
                bool isBound = resolvedBindingLookup.Contains(modification.property.GetBindingPath());
                DrivenPropertyManager.TryRegisterProperty(
                    isBound ? BoundPropertyDriver.GetOrCreateInstance() : LivePropertyDriver.GetOrCreateInstance(),
                    modification.property.serializedObject.targetObject,
                    modification.property.propertyPath);
                switch (modification.valueType)
                {
                    case LiveAttributePropertyModificationValueType.BoxedValue:
                        modification.property.boxedValue = modification.value;
                        break;
                    case LiveAttributePropertyModificationValueType.ManagedReferenceValue:
                        modification.property.managedReferenceValue = modification.value;
                        break;
                    case LiveAttributePropertyModificationValueType.ManagedReferenceArrayValue:
                        // We cant apply arrays through boxed values so have to do it item by item.
                        var arrayValue = modification.value as IList;
                        modification.property.arraySize = arrayValue?.Count ?? 0;
                        modification.property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                        for (var i = 0; i < modification.property.arraySize; i++)
                        {
                            var elementProperty = modification.property.GetArrayElementAtIndex(i);
                            elementProperty.managedReferenceValue = arrayValue[i];
                        }
                        break;
                }
            }
            context.rootSerializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    void CollectLiveAttributeValuePropertyModifications(object obj, object uxmlSerializedData, UxmlSerializedAttributeDescription attributeDescription, SerializedProperty property, List<LiveAttributePropertyModification> propertyModifications, ref bool hasUnsupportedDrivenPropertyChange)
    {
        try
        {
            if (!attributeDescription.TryGetValueFromObject(obj, out var value))
                return;

            var flags = uxmlSerializedData != null ? attributeDescription.GetSerializedValueAttributeFlags(uxmlSerializedData) : UxmlSerializedData.UxmlAttributeFlags.DefaultValue;
            var uxmlValue = (flags == UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml && uxmlSerializedData != null)
                ? attributeDescription.GetSerializedValue(uxmlSerializedData)
                : attributeDescription.defaultValue;

            if (attributeDescription.isUxmlObject)
            {
                if (attributeDescription.isList)
                {
                    var valueList = value as IList;
                    var uxmlList = uxmlValue as IList;
                    int itemCount = valueList?.Count ?? 0;

                    if (property.arraySize != valueList?.Count)
                    {
                        // Driven properties dont support array size changes so we need to work with a whole copy.
                        hasUnsupportedDrivenPropertyChange = true;

                        // Create a copy of the serialized data to extract the new value
                        var uxmlSerializedDataCopy = UxmlUtility.CloneObject(uxmlSerializedData);
                        attributeDescription.SyncSerializedData(obj, uxmlSerializedDataCopy);

                        // Extract the new value
                        var newValue = attributeDescription.GetSerializedValue(uxmlSerializedDataCopy);

                        propertyModifications.Add(new LiveAttributePropertyModification
                        {
                            attribute = attributeDescription,
                            property = property,
                            valueType = LiveAttributePropertyModificationValueType.ManagedReferenceArrayValue,
                            value = newValue,
                        });
                        return;
                    }

                    for (int i = 0; i < itemCount; i++)
                    {
                        var item = valueList[i];
                        var uxmlItem = uxmlList != null && i < uxmlList.Count ? uxmlList[i] as UxmlSerializedData : null;

                        var elementProperty = property.GetArrayElementAtIndex(i);

                        // Handle null items
                        if (item == null)
                        {
                            // Both null - no difference
                            if (uxmlItem == null)
                                continue;

                            // item is null but uxmlItem is not
                            hasUnsupportedDrivenPropertyChange = true;

                            propertyModifications.Add(new LiveAttributePropertyModification
                            {
                                attribute = attributeDescription,
                                property = elementProperty,
                                valueType = LiveAttributePropertyModificationValueType.ManagedReferenceValue,
                                value = null,
                            });
                            continue;
                        }

                        var desc = UxmlSerializedDataRegistry.GetDescription(item.GetType().FullName);
                        if (desc == null)
                            continue;

                        if (uxmlItem == null || uxmlItem.GetType() != desc.serializedDataType)
                        {
                            // Type mismatch
                            hasUnsupportedDrivenPropertyChange = true;

                            uxmlItem = desc.CreateDefaultSerializedData();
                            desc.SyncSerializedData(item, uxmlItem);

                            propertyModifications.Add(new LiveAttributePropertyModification
                            {
                                attribute = attributeDescription,
                                property = elementProperty,
                                valueType = LiveAttributePropertyModificationValueType.ManagedReferenceValue,
                                value = uxmlItem,
                            });
                            continue;
                        }

                        foreach (var nestedAttr in desc.serializedAttributes)
                        {
                            var nestedProperty = elementProperty.FindPropertyRelative(nestedAttr.serializedField.Name);
                            if (nestedProperty != null)
                                CollectLiveAttributeValuePropertyModifications(item, uxmlItem, nestedAttr, nestedProperty, propertyModifications, ref hasUnsupportedDrivenPropertyChange);
                        }
                    }
                }
                else
                {
                    // Handle single UxmlObject
                    var desc = UxmlSerializedDataRegistry.GetDescription(value.GetType().FullName);
                    if (desc != null)
                    {
                        var uxmlData = uxmlValue as UxmlSerializedData;
                        if (uxmlData == null || uxmlData.GetType() != desc.serializedDataType)
                        {
                            //  Type mismatch
                            hasUnsupportedDrivenPropertyChange = true;

                            uxmlData = desc.CreateDefaultSerializedData();
                            desc.SyncSerializedData(value, uxmlData);

                            propertyModifications.Add(new LiveAttributePropertyModification
                            {
                                attribute = attributeDescription,
                                property = property,
                                valueType = LiveAttributePropertyModificationValueType.ManagedReferenceValue,
                                value = uxmlData,
                            });
                            return;
                        }

                        foreach (var nestedAttr in desc.serializedAttributes)
                        {
                            var nestedProperty = property.FindPropertyRelative(nestedAttr.serializedField.Name);

                            if (nestedProperty != null)
                                CollectLiveAttributeValuePropertyModifications(value, uxmlData, nestedAttr, nestedProperty, propertyModifications, ref hasUnsupportedDrivenPropertyChange);
                        }
                    }
                    else if (uxmlValue != null)
                    {
                        // Handle null items
                        hasUnsupportedDrivenPropertyChange = true;

                        propertyModifications.Add(new LiveAttributePropertyModification
                        {
                            attribute = attributeDescription,
                            property = property,
                            valueType = LiveAttributePropertyModificationValueType.ManagedReferenceValue,
                            value = null,
                        });
                    }
                }

                return;
            }

            // We only sync values that are different
            if (!UxmlAttributeComparison.ObjectEquals(value, uxmlValue))
            {
                propertyModifications.Add(new LiveAttributePropertyModification
                {
                    attribute = attributeDescription,
                    property = property,
                    valueType = LiveAttributePropertyModificationValueType.BoxedValue,
                    value = value,
                });
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(new Exception($"Failed to collect modifications {attributeDescription.name}", ex));
        }
    }
}
