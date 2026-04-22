// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Removes UXML attributes that the element type doesn't recognize.
    /// </summary>
    internal class RedundantAttributeUpgrader : IUxmlUpgrader
    {
        internal const string k_Name = "Remove Unrecognized Attributes";
        internal const string k_Description = "Removes UXML attributes that are not part of the element's definition. These may be from old versions, typos, or unsupported attributes. Disable this upgrader if external tools depend on custom attributes.";

        static readonly HashSet<string> s_SpecialAttributes =
        [
            "name",
            "class",
            "style",
            "slot",
            "slot-name",
            "content-container",
            "contentContainer",
            "authoring-id"
        ];

        public string name => k_Name;

        public string description => k_Description;

        public bool Upgrade(VisualTreeAsset asset)
        {
            int countAssetsProcessed = 0;
            int countAssetsModified = 0;
            foreach (var uxmlAsset in asset.DepthFirstTraversal())
            {
                if (uxmlAsset is VisualElementAsset vea)
                {
                    countAssetsProcessed++;
                    var description = UxmlSerializedDataRegistry.GetDescription(vea.fullTypeName);
                    if (description != null && UpgradeElement(vea, description))
                        countAssetsModified++;
                }
            }

            return countAssetsModified > 0;
        }

        bool UpgradeElement(VisualElementAsset element, UxmlSerializedDataDescription description)
        {
            if (element.properties == null || element.properties.Count == 0)
                return false;

            using var _ = ListPool<string>.Get(out var unrecognizedAttributes);

            // Check each attribute on the element
            foreach (var property in element.properties)
            {
                var attrName = property.name;

                if (IsSpecialAttribute(attrName) || description.HasAttribute(attrName))
                    continue;

                unrecognizedAttributes.Add(attrName);
            }

            if (unrecognizedAttributes.Count > 0)
            {
                foreach (var attrName in unrecognizedAttributes)
                {
                    element.RemoveAttribute(attrName);
                }
                return true;
            }

            return false;
        }

        bool IsSpecialAttribute(string name) => s_SpecialAttributes.Contains(name);
    }
}
