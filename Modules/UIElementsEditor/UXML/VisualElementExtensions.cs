// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal static class VisualElementExtensions
    {
        internal static string GetUxmlFullTypeName(this VisualElement element)
        {
            if (null == element)
                return null;
            return UxmlSerializedDataRegistry.GetDescription(element.fullTypeName)?.uxmlFullName;
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
            }

            return overriddenAttributes;
        }

        public static List<UxmlSerializedAttributeDescription> GetAttributeDescriptions(this VisualElement ve)
        {
            var desc = UxmlSerializedDataRegistry.GetDescription(ve.GetType().FullName);
            if (desc != null)
                return new List<UxmlSerializedAttributeDescription>(desc.serializedAttributes);
            return new List<UxmlSerializedAttributeDescription>();
        }
    }
}
