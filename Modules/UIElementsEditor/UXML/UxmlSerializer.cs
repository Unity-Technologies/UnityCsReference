// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    #pragma warning disable CS0618 // Type or member is obsolete.
    internal class UxmlSerializedAttributeDescription : UxmlAttributeDescription
    #pragma warning restore CS0618 // Type or member is obsolete
    {
        static readonly Dictionary<Type, string> k_BaseTypes = new()
        {
            { typeof(short), "short" },
            { typeof(int), "int" },
            { typeof(long), "long" },
            { typeof(float), "float" },
            { typeof(double), "double" },
            { typeof(bool), "boolean" }
        };


        // Lazy loaded fields that are mostly used in the UI Builder.
        IList<Type> m_UxmlObjectAcceptedTypes;
        object m_ObjectField;
        bool? m_IsUnityObject;
        bool? m_IsList;
        bool? m_IsUxmlObject;
        bool m_DefaultValueSet;
        object m_DefaultValue;
        string m_BindingPath;

        /// <summary>
        /// The <see cref="UxmlAttributeDescription"/> that this attribute is part of.
        /// </summary>
        public UxmlSerializedDataDescription dataDescription { get; set; }

        public new Type type { get; set; }

        /// <summary>
        /// The FieldInfo for the UxmlSerializedData.
        /// </summary>
        public FieldInfo serializedField { get; set; }

        /// <summary>
        /// The FieldInfo for the UxmlSerializedData attribute flags.
        /// </summary>
        public FieldInfo serializedFieldAttributeFlags { get; set; }

        /// <summary>
        /// If this attribute is overriding then this will contain the name of the original property/field.
        /// </summary>
        public string overriddenFieldName { get; set; }

        /// <summary>
        /// The name of the field/property that data bindings should use.
        /// </summary>
        public string bindingPath
        {
            get
            {
                if (m_BindingPath == null)
                {
                    var bindingPathAttribute = serializedField.GetCustomAttribute<UxmlAttributeBindingPathAttribute>();
                    if (bindingPathAttribute != null)
                        m_BindingPath = bindingPathAttribute.path;
                    else
                        m_BindingPath = overriddenFieldName ?? serializedField.Name;
                }

                return m_BindingPath;
            }
        }

        /// <summary>
        /// The field/property value for the VisualElement/UxmlObject.
        /// </summary>
        public object objectField
        {
            get
            {
                if (m_ObjectField == null)
                {
                    var fieldName = serializedField.Name;

                    const BindingFlags publicFlags = BindingFlags.Instance | BindingFlags.Public;

                    // First check for a public version of the field/property.
                    m_ObjectField = elementType.GetProperty(fieldName, publicFlags) ?? (object)elementType.GetField(fieldName, publicFlags);
                    if (m_ObjectField != null)
                        return m_ObjectField;

                    // If we did not find a public version of the field/property, check for a private version.
                    // We need to walk through the type hierarchy to find the field/property as we can not get base private properties using GetProperty/GetField.
                    const BindingFlags privateFlags = BindingFlags.Instance | BindingFlags.NonPublic;

                    // We need to walk through the type hierarchy to find the field/property as we can not get base private properties using GetProperty/GetField.
                    var oType = elementType;
                    while (m_ObjectField == null && oType != null)
                    {
                        m_ObjectField = oType.GetProperty(fieldName, privateFlags) ?? (object)oType.GetField(fieldName, privateFlags);

                        if (m_ObjectField == null)
                            oType = oType.BaseType;
                    }
                }
                return m_ObjectField;
            }
        }

        public object defaultValue
        {
            get
            {
                if (!m_DefaultValueSet)
                {
                    dataDescription.InitializeDefaultValues();
                    Debug.AssertFormat(m_DefaultValueSet, "Expected default value to be set for {0} after calling InitializeDefaultValues.", name);
                }
                return m_DefaultValue;
            }
            set
            {
                m_DefaultValueSet = true;
                m_DefaultValue = value;
            }
        }

        public object defaultValueClone => UxmlUtility.CloneObject(defaultValue);

        public override string defaultValueAsString => Convert.ToString(defaultValue, CultureInfo.InvariantCulture) ?? "";

        public bool isUnityObject
        {
            get
            {
                m_IsUnityObject ??= typeof(Object).IsAssignableFrom(type);
                return m_IsUnityObject.Value;
            }
        }

        public bool isUxmlObject
        {
            get
            {
                m_IsUxmlObject ??= serializedField.GetCustomAttribute<UxmlObjectReferenceAttribute>() != null;
                return m_IsUxmlObject.Value;
            }
        }

        public bool isList
        {
            get
            {
                m_IsList ??= typeof(IList).IsAssignableFrom(type) && (type.IsArray || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>));
                return m_IsList.Value;
            }
        }

        /// <summary>
        /// The type of element or object that this attribute belongs to.
        /// </summary>
        public Type elementType { get; set; }

        /// <summary>
        /// The UxmlObject types that can be applied to this attribute.
        /// </summary>
        public IList<Type> uxmlObjectAcceptedTypes
        {
            get
            {
                if (m_UxmlObjectAcceptedTypes == null)
                {
                    // Were the types set in the UxmlObjectAttribute?
                    var attribute = serializedField.GetCustomAttribute<UxmlObjectReferenceAttribute>();
                    if (attribute.types != null)
                    {
                        m_UxmlObjectAcceptedTypes = attribute.types;
                        return m_UxmlObjectAcceptedTypes;
                    }

                    var acceptedTypes = new List<Type>();

                    void AddType(Type type)
                    {
                        if (type.IsAbstract || type.IsGenericType)
                            return;

                        // We only accept UxmlObjectAttribute types.
                        if (type.GetCustomAttribute<UxmlObjectAttribute>() == null)
                            return;

                        var uxmlSerializedDataType = type.GetNestedType(nameof(UxmlSerializedData));
                        if (uxmlSerializedDataType == null)
                            return;

                        acceptedTypes.Add(uxmlSerializedDataType);
                    }

                    // We need to query the source data as the type may be an interface which we dont store in the serialized data.
                    Type uxmlObjectType = null;
                    if (objectField is PropertyInfo propertyField)
                        uxmlObjectType = propertyField.PropertyType;
                    else if (objectField is FieldInfo fieldInfo)
                        uxmlObjectType = fieldInfo.FieldType;

                    if (isList)
                        uxmlObjectType = uxmlObjectType.GetArrayOrListElementType();

                    var foundTypes = TypeCache.GetTypesDerivedFrom(uxmlObjectType);

                    AddType(uxmlObjectType); // Add the base type
                    foreach (var t in foundTypes)
                    {
                        AddType(t);
                    }

                    m_UxmlObjectAcceptedTypes = acceptedTypes;
                }

                return m_UxmlObjectAcceptedTypes;
            }
        }

        /// <summary>
        /// Copies the value from the VisualElement/UxmlObject to the UxmlSerializedData.
        /// </summary>
        /// <param name="obj">The instance of the VisualElement/UxmlObject to copy the value from.</param>
        /// <param name="uxmlSerializedData">The instance of the UxmlSerializedData to copy the value to.</param>
        public void SyncSerializedData(object obj, object uxmlSerializedData)
        {
            try
            {
                if (!TryGetValueFromObject(obj, out var value))
                    value = defaultValueClone;

                if (isUxmlObject && value != null)
                {
                    var previousValue = GetSerializedValue(uxmlSerializedData);

                    if (value is IList list)
                    {
                        // We try to use the old data so that we can preserve the uxml asset id.
                        var previousList = previousValue as IList;

                        var serializedList = serializedField.FieldType.IsArray ? Array.CreateInstance(serializedField.FieldType.GetElementType(), list.Count) : Activator.CreateInstance(serializedField.FieldType) as IList;
                        for (int i = 0; i < list.Count; i++)
                        {
                            var item = list[i];
                            if (item != null)
                            {
                                var previousData = previousList != null && i < previousList.Count ? previousList[i] as UxmlSerializedData : null;

                                var desc = UxmlSerializedDataRegistry.GetDescription(item.GetType().FullName);
                                var data = previousData ?? desc.CreateSerializedData();
                                desc.SyncSerializedData(item, data);

                                if (serializedField.FieldType.IsArray)
                                    serializedList[i] = data;
                                else
                                    serializedList.Add(data);
                            }
                            else
                            {
                                if (!serializedField.FieldType.IsArray)
                                    serializedList.Add(null);
                            }
                        }

                        value = serializedList;
                    }
                    else
                    {
                        var desc = UxmlSerializedDataRegistry.GetDescription(value.GetType().FullName);
                        var data = previousValue ?? desc.CreateSerializedData();
                        desc.SyncSerializedData(value, data);
                        value = data;
                    }
                }
                else
                {
                    value = UxmlUtility.CloneObject(value);
                }

                SetSerializedValue(uxmlSerializedData, value);

                // We dont set Ignore here because the field may be overridden with the default value.
                if (!UxmlAttributeComparison.ObjectEquals(value, defaultValue))
                    SetSerializedValueAttributeFlags(uxmlSerializedData, UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml);
            }
            catch (Exception ex)
            {
                Debug.LogException(new Exception($"Failed to sync {name} to {serializedField.Name}", ex));
            }
        }

        public void SyncDefaultValue(object uxmlSerializedData, bool removeOverride)
        {
            try
            {
                if (removeOverride || GetSerializedValueAttributeFlags(uxmlSerializedData) == UxmlSerializedData.UxmlAttributeFlags.Ignore)
                {
                    SetSerializedValue(uxmlSerializedData, defaultValueClone, UxmlSerializedData.UxmlAttributeFlags.DefaultValue);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(new Exception($"Failed to sync {serializedField.Name} default value", ex));
            }
        }

        public bool TryGetValueFromObject(object objInstance, out object value)
        {
            try
            {
                value = null;
                if (objInstance == null)
                    return false;

                if (objectField is PropertyInfo propertyField)
                {
                    value = propertyField.GetValue(objInstance);
                    return true;
                }
                else if (objectField is FieldInfo fieldInfo)
                {
                    value = fieldInfo.GetValue(objInstance);
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {

                Debug.LogException(new Exception($"Failed to extract {name} value from {dataDescription.uxmlFullName}", e));
                value = default;
                return false;
            }
        }

        public void SetValueToObject(object objInstance, object value)
        {
            try
            {
                if (objectField is PropertyInfo propertyField)
                {
                    propertyField.SetValue(objInstance, value);
                }
                else if (objectField is FieldInfo fieldInfo)
                {
                    fieldInfo.SetValue(objInstance, value);
                }
                else
                {
                    Debug.LogError($"Could not set {name} with value {value}.");
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void SetSerializedValue(object uxmlSerializedData, object value)
        {
            if (serializedField.FieldType == typeof(string) && value is Type)
            {
                // Attempt to convert the field to a string
                if (UxmlAttributeConverter.TryConvertToString<Type>(value, null, out var stringValue))
                    value = stringValue;
            }

            if (isUnityObject && value is Object obj && obj.GetType() != serializedField.FieldType)
            {
                // Missing asset reference fields will throw an exception because we're trying to write an Object,
                // so we have to leave the old value as is.
                var entityId = obj.GetEntityId();
                if (!obj && entityId != EntityId.None)
                {
                    return;
                }
            }

            serializedField.SetValue(uxmlSerializedData, value);
        }

        public void SetSerializedValue(object uxmlSerializedData, object value, UxmlSerializedData.UxmlAttributeFlags flags)
        {
            SetSerializedValue(uxmlSerializedData, value);
            SetSerializedValueAttributeFlags(uxmlSerializedData, flags);
        }

        public object GetSerializedValue(object uxmlSerializedData)
        {
            try
            {
                return serializedField.GetValue(uxmlSerializedData);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return defaultValue;
            }
        }

        public void SetSerializedValueAttributeFlags(object uxmlSerializedData, UxmlSerializedData.UxmlAttributeFlags value)
        {
            serializedFieldAttributeFlags.SetValue(uxmlSerializedData, value);
        }

        public UxmlSerializedData.UxmlAttributeFlags GetSerializedValueAttributeFlags(object uxmlSerializedData)
        {
            return (UxmlSerializedData.UxmlAttributeFlags)serializedFieldAttributeFlags.GetValue(uxmlSerializedData);
        }

        public void UpdateSchemaRestriction()
        {
            if (type is { IsEnum: true })
            {
                var enumRestriction = new UxmlEnumeration();

                var values = new List<string>();
                foreach (var item in Enum.GetValues(type))
                {
                    values.Add(item.ToString());
                }

                enumRestriction.values = values;
                restriction = enumRestriction;
            }
            else if (serializedField.GetCustomAttribute<RangeAttribute>() != null)
            {
                var attribute = serializedField.GetCustomAttribute<RangeAttribute>();
                var uxmlValueBoundsRestriction = new UxmlValueBounds
                {
                    max = attribute.max.ToString(CultureInfo.InvariantCulture),
                    min = attribute.min.ToString(CultureInfo.InvariantCulture)
                };

                restriction = uxmlValueBoundsRestriction;
            }
        }

        public override string ToString() => $"{serializedField.DeclaringType.ReflectedType.Name}.{serializedField.Name} ({serializedField.FieldType})";
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal class UxmlSerializedUxmlObjectAttributeDescription : UxmlSerializedAttributeDescription
    {
        internal const string k_MultipleUxmlObjectsWarning = "Multiple UxmlObjects found for UxmlObjectReference field {0}. " +
            "Only the first UxmlObject will be used in the current configuration. " +
            "If you intend to use multiple UxmlObjects, it is recommended to convert the field into a list.";
        internal const string k_UxmlObjectWithNoFieldWarning = "UxmlObject {0} could not find a matching UxmlObjectReference field in {1}. This object will be ignored.";
        internal const string k_UxmlObjectMismatchFieldHint = "A potential match was found. The field {0} can accept this UxmlObject, but it currently contains a root name. To be valid, the UxmlObject must be nested under an element named {1}.";

        public string rootName { get; set; }
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal static class UxmlSerializer
    {
        public static bool TryParseSerializedAttribute(string name, string value, UxmlSerializedData uxmlSerializedData, CreationContext cc)
        {
            var desc = UxmlSerializedDataRegistry.GetDescription(uxmlSerializedData.GetType().DeclaringType.FullName);
            if (desc.FindAttributeWithUxmlName(name) is { } attributeDesc)
                return TryParseSerializedAttribute(value, uxmlSerializedData, attributeDesc, cc);
            return false;
        }

        public static bool TryParseSerializedAttribute(string value, UxmlSerializedData uxmlSerializedData, UxmlSerializedAttributeDescription attributeDescription, CreationContext cc)
        {
            if (!UxmlAttributeConverter.TryConvertFromString(attributeDescription.type, value, cc, out var parsedValue))
                return false;

            if (parsedValue is Type type)
            {
                // Validate that type can be assigned
                var typeRefAttribute = attributeDescription.serializedField.GetCustomAttribute<UxmlTypeReferenceAttribute>();
                if (typeRefAttribute != null && typeRefAttribute.baseType != null && !typeRefAttribute.baseType.IsAssignableFrom(type))
                {
                    Debug.LogError($"Type: Invalid type \"{type}\". Type must derive from {typeRefAttribute.baseType.FullName}.");
                    return false;
                }

                parsedValue = value;
            }

            try
            {
                attributeDescription.SetSerializedValue(uxmlSerializedData, parsedValue, UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml);
            }
            catch (Exception e)
            {
                Debug.LogException(new Exception($"Could not set value for {attributeDescription.serializedField.Name} with {parsedValue}", e), cc.visualTreeAsset);
            }

            return true;
        }

        public static void CreateSerializedDataOverrides(VisualTreeAsset vta)
        {
            foreach (var asset in vta.DepthFirstTraversalOfType<TemplateAsset>())
            {
                var namesPath = new List<string>();
                var idsList = new List<int>();
                asset.serializedDataOverrides.Clear();
                CreateSerializedDataOverride(asset, vta, asset, new CreationContext(vta), namesPath, idsList);
            }
        }

        static void CreateSerializedDataOverride(TemplateAsset rootTemplate, VisualTreeAsset vta, TemplateAsset templateAsset, CreationContext cc, List<string> namesPath, List<int> idsList)
        {
            // If there's no attribute override no need to create additional serialized data.
            if (!templateAsset.hasAttributeOverride && cc.attributeOverrides == null)
                return;

            var bagOverrides = templateAsset.attributeOverrides;
            var contextOverrides = cc.attributeOverrides;

            List<CreationContext.AttributeOverrideRange> overrideRanges = null;
            if (bagOverrides != null || contextOverrides != null)
            {
                // We want to add contextOverrides first here, then bagOverrides, as we
                // want parent instances to always override child instances.
                overrideRanges = new List<CreationContext.AttributeOverrideRange>();
                if (contextOverrides != null)
                    overrideRanges.AddRange(contextOverrides);
                if (bagOverrides != null)
                    overrideRanges.Add(new CreationContext.AttributeOverrideRange(vta, bagOverrides));
            }

            var templateVta = vta.ResolveTemplate(templateAsset.templateAlias);

            // If the template reference is invalid simply skip it.
            // On instantiation it will get replaced by a label showing that the template is unknown.
            if (templateVta == null)
                return;

            // For each element with an override create a serialized data stored in the template asset
            foreach (var asset in templateVta.DepthFirstTraversal())
            {
                if (asset is not VisualElementAsset vea ||
                    vea.serializedData is not VisualElement.UxmlSerializedData elementSerializedData)
                    continue;

                var elementName = elementSerializedData.nameValue;
                if (string.IsNullOrEmpty(elementName))
                    continue;

                var namesCopy = new List<string>(namesPath);
                var idsCopy = new List<int>(idsList);

                var parentAsset = vea.parentAsset;
                var nameInsertIndex = namesCopy.Count;
                var idInsertIndex = idsCopy.Count;
                var rootElement = templateVta.visualTreeNoAlloc;

                // For attribute overrides, we need to take into account template assets in the hierarchy
                // from the root to the current element.
                while (parentAsset != rootElement)
                {
                    if (parentAsset is TemplateAsset parentTemplateAsset)
                    {
                        if (parentTemplateAsset.serializedData is VisualElement.UxmlSerializedData parentElementSerializedData
                            && !string.IsNullOrEmpty(parentElementSerializedData.nameValue))
                        {
                            namesCopy.Insert(nameInsertIndex, parentElementSerializedData.nameValue);
                        }

                        idsCopy.Insert(idInsertIndex, parentAsset.id);
                    }

                    parentAsset = parentAsset.parentAsset;
                }

                namesCopy.Add(elementName);
                idsCopy.Add(vea.id);

                var desc = UxmlSerializedDataRegistry.GetDescription(vea.fullTypeName);
                if (desc == null)
                    continue;

                UxmlSerializedData serializedDataOverride = null;
                var templateContext = new CreationContext(null, overrideRanges, null, vta, null, null, namesCopy, cc.templateAsset);

                var foundOverrides = DictionaryPool<string, (int pathLength, string attributeValue)>.Get();
                var handledAttributes = HashSetPool<string>.Get();

                // Find all the attribute overrides that match this element.
                // Making sure to filter any duplicates so that we can filter out less specific overrides (shorter names paths).
                foreach (var attributeOverride in overrideRanges)
                {
                    foreach (var ao in attributeOverride.attributeOverrides)
                    {
                        if (!ao.NamesPathMatchesElementNamesPath(namesCopy))
                            continue;

                        if (foundOverrides.TryGetValue(ao.m_AttributeName, out var existingOverride))
                        {
                            // If we already have an override for this attribute we should check which one is more specific.
                            if (ao.m_NamesPath.Length > existingOverride.pathLength)
                            {
                                // Longer paths are more specific.
                                foundOverrides[ao.m_AttributeName] = (ao.m_NamesPath.Length, ao.m_Value);
                            }
                        }
                        else
                        {
                            foundOverrides[ao.m_AttributeName] = (ao.m_NamesPath.Length, ao.m_Value);
                        }
                    }
                }

                // Now apply the found overrides.
                foreach (var ao in foundOverrides)
                {
                    // We may find that some obsolete attributes map to the same current attribute.
                    if (handledAttributes.Contains(ao.Key))
                        continue;

                    serializedDataOverride ??= desc.CreateSerializedData();

                    if (desc.FindAttributeWithUxmlName(ao.Key) is { } attributeDescription)
                    {
                        TryParseSerializedAttribute(ao.Value.attributeValue, serializedDataOverride, attributeDescription, templateContext);
                        handledAttributes.Add(ao.Key);
                    }
                    else
                    {
                        foreach (var obsoleteAttribute in desc.FindAttributesWithObsoleteUxmlName(ao.Key))
                        {
                            if (handledAttributes.Contains(obsoleteAttribute.name))
                                continue;
                            handledAttributes.Add(obsoleteAttribute.name);
                            TryParseSerializedAttribute(ao.Value.attributeValue, serializedDataOverride, obsoleteAttribute, cc);
                        }
                    }
                }

                DictionaryPool<string, (int pathLength, string attributeValue)>.Release(foundOverrides);
                HashSetPool<string>.Release(handledAttributes);

                if (serializedDataOverride != null)
                {
                    rootTemplate.serializedDataOverrides.Add(new TemplateAsset.UxmlSerializedDataOverride()
                    {
                        m_ElementId = vea.id,
                        m_ElementIdsPath = idsCopy,
                        m_SerializedData = serializedDataOverride
                    });
                }
            }

            foreach (var ta in templateVta.DepthFirstTraversalOfType<TemplateAsset>())
            {
                var idsCopy = new List<int>(idsList);
                var namesCopy = new List<string>(namesPath);

                if (ta.serializedData is VisualElement.UxmlSerializedData taSerializedData
                    && !string.IsNullOrEmpty(taSerializedData.nameValue))
                {
                    namesCopy.Add(taSerializedData.nameValue);
                }

                idsCopy.Add(ta.id);

                var templateContext = new CreationContext(null, overrideRanges, null, vta, null, null, namesCopy, cc.templateAsset);

                CreateSerializedDataOverride(rootTemplate, templateVta, ta, templateContext, namesCopy, idsCopy);
            }
        }
    }
}
