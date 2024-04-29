// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal static class TemplateAssetExtensions
    {
        public static void SetAttributeOverride(
            this TemplateAsset ta, VisualElement element, string attributeName, string value, string[] pathToTemplateAsset = null)
        {
            var isTraitsElement = UxmlSerializedDataRegistry.GetDescription(element.fullTypeName) == null;

            if (isTraitsElement)
            {
                pathToTemplateAsset = new[] { element.name };
            }
            else
            {
                pathToTemplateAsset ??= GetPathToTemplateAsset(element, ta);
            }

            var overrideName = isTraitsElement ? element.name : string.Join(" ", pathToTemplateAsset);

            // See if the override already exists.
            for (int i = 0; i < ta.attributeOverrides.Count; ++i)
            {
                var over = ta.attributeOverrides[i];

                if (over.NamesPathMatchesElementNamesPath(pathToTemplateAsset) && over.m_AttributeName == attributeName)
                {
                    // If we have a more complex path, add a new override.
                    if (over.m_ElementName != overrideName)
                    {
                        continue;
                    }

                    over.m_ElementName = overrideName;
                    over.m_AttributeName = attributeName;
                    over.m_Value = value;

                    ta.attributeOverrides[i] = over;

                    return;
                }
            }

            // If the override does not exist, add it.
            var attributeOverride = new TemplateAsset.AttributeOverride
            {
                m_ElementName = overrideName,
                m_NamesPath = pathToTemplateAsset,
                m_AttributeName = attributeName,
                m_Value = value
            };
            ta.attributeOverrides.Add(attributeOverride);
        }

        public static string[] GetPathToTemplateAsset(VisualElement element, TemplateAsset ta)
        {
            var path = new List<string> { element.name };
            var parent = element.parent;
            var parentAsset = parent?.GetVisualElementAsset();

            while (parent != null && parentAsset != ta)
            {
                if (!string.IsNullOrEmpty(parent.name) && parent is TemplateContainer)
                {
                    path.Insert(0, parent.name);
                }

                parent = parent.parent;
                parentAsset = parent.GetVisualElementAsset();
            }

            return parentAsset != ta ? null : path.ToArray();
        }

        public static void RemoveAttributeOverride(
            this TemplateAsset ta, VisualElement element, string attributeName)
        {
            var pathToTemplateAsset = GetPathToTemplateAsset(element, ta);
            var overrideName = string.Join(" ", pathToTemplateAsset);

            // See if the override already exists.
            for (int i = 0; i < ta.attributeOverrides.Count; ++i)
            {
                var over = ta.attributeOverrides[i];
                if (over.NamesPathMatchesElementNamesPath(pathToTemplateAsset) && over.m_AttributeName == attributeName)
                {
                    ta.attributeOverrides.RemoveAt(i);

                    return;
                }
            }
        }
    }
}
