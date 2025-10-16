// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal static class VisualElementExtensions
    {
        static readonly List<string> s_SkippedAttributeNames = new List<string>()
        {
            "content-container",
            "class",
            "style",
            "template",
        };

        internal static string GetUxmlFullTypeName(this VisualElement element)
        {
            if (null == element)
                return null;

            var description = UxmlSerializedDataRegistry.GetDescription(element.fullTypeName);
            if (description != null)
                return description.uxmlFullName;

            if (VisualElementFactoryRegistry.TryGetValue(element.GetType(), out var factories))
            {
                return factories[0].uxmlQualifiedName;
            }

            return null;
        }

        public static Dictionary<string, string> GetOverriddenAttributes(this VisualElement ve)
        {
            var attributeList = ve.GetAttributeDescriptions();
            var overriddenAttributes = new Dictionary<string, string>();

            foreach (var attribute in attributeList)
            {
                if (attribute?.name == null)
                    continue;

                if (attribute is UxmlSerializedAttributeDescription attributeDescription)
                {
                    // UxmlSerializedData
                    if (attributeDescription.TryGetValueFromObject(ve, out var value) &&
                        UxmlAttributeComparison.ObjectEquals(value, attributeDescription.defaultValue))
                    {
                        continue;
                    }

                    string valueAsString = null;
                    if (value != null)
                        UxmlAttributeConverter.TryConvertToString(value, ve.visualTreeAssetSource, out valueAsString);
                    overriddenAttributes.Add(attribute.name, valueAsString);
                }
                else
                {
                    // UxmlTraits
                    var veType = ve.GetType();
                    var camel = StyleSheetUtility.ConvertDashToCamel(attribute.name);
                    var fieldInfo = veType.GetProperty(camel);
                    if (fieldInfo != null)
                    {
                        var veValueAbstract = fieldInfo.GetValue(ve, null);
                        if (veValueAbstract == null)
                            continue;

                        var veValueStr = veValueAbstract.ToString();
                        if (veValueStr == "False")
                            veValueStr = "false";
                        else if (veValueStr == "True")
                            veValueStr = "true";

                        // The result of Type.ToString is not enough for us to find the correct Type.
                        if (veValueAbstract is Type type)
                            veValueStr = $"{type.FullName}, {type.Assembly.GetName().Name}";

                        if (veValueAbstract is IEnumerable<string> enumerable)
                            veValueStr = string.Join(",", enumerable);

                        var attributeValueStr = attribute.defaultValueAsString;
                        if (veValueStr == attributeValueStr)
                            continue;

                        overriddenAttributes.Add(attribute.name, veValueStr);
                    }
                    // This is a special patch that allows to search for built-in elements' attribute specifically
                    // without needing to add to the public API.
                    // Allowing to search for internal/private properties in all cases could lead to unforeseen issues.
                    else if (ve is EnumField or EnumFlagsField && camel == "type")
                    {
                        fieldInfo = veType.GetProperty(camel, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        var veValueAbstract = fieldInfo.GetValue(ve, null);
                        if (!(veValueAbstract is Type type))
                            continue;

                        var veValueStr = $"{type.FullName}, {type.Assembly.GetName().Name}";
                        var attributeValueStr = attribute.defaultValueAsString;
                        if (veValueStr == attributeValueStr)
                            continue;
                        overriddenAttributes.Add(attribute.name, veValueStr);
                    }
                }
            }

            return overriddenAttributes;
        }

        public static List<UxmlSerializedAttributeDescription> GetAttributeDescriptions(this VisualElement ve, bool useTraits = false)
        {
            var uxmlQualifiedName = GetUxmlQualifiedName(ve);

            var desc = UxmlSerializedDataRegistry.GetDescription(uxmlQualifiedName);
            if (desc != null && !useTraits)
                return new List<UxmlSerializedAttributeDescription>(desc.serializedAttributes);
            return new List<UxmlSerializedAttributeDescription>();
        }

        static string GetUxmlQualifiedName(VisualElement ve)
        {
            var uxmlQualifiedName = ve.GetType().FullName;

            // Try get uxmlQualifiedName from the UxmlFactory.
            var factoryTypeName = $"{ve.GetType().FullName}+UxmlFactory";
            var asm = ve.GetType().Assembly;
            var factoryType = asm.GetType(factoryTypeName);
            if (factoryType != null)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                var factoryTypeInstance = (IUxmlFactory)Activator.CreateInstance(factoryType);
                if (factoryTypeInstance != null)
                {
                    uxmlQualifiedName = factoryTypeInstance.uxmlQualifiedName;
                }
#pragma warning restore CS0618 // Type or member is obsolete
            }

            return uxmlQualifiedName;
        }
    }
}
