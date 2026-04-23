// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Upgrades renamed UXML attributes to their new names.
    /// </summary>
    class AttributeRenameUpgrader : IUxmlUpgrader
    {
        internal const string k_Name = "Rename Deprecated Attributes";
        internal const string k_Description = "Updates attributes that have been renamed to their new names.";

        public string name => k_Name;

        public string description => k_Description;

        public bool Upgrade(VisualTreeAsset asset)
        {
            // The attribute names have already been fixed during import
            // We just need to save the file if they were updated
            return asset.importedWithObsoleteAttributeNames;
        }
    }
}
