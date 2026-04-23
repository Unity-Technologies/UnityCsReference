// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Applies URL fixes that were detected during import.
    /// When URLs are updated during import, this upgrader ensures those changes are persisted to disk.
    /// </summary>
    class UrlUpgrader : IUxmlUpgrader
    {
        internal const string k_Name = "Apply URL Fixes";
        internal const string k_Description = "Saves URL corrections that were automatically detected during import. These changes update asset paths to resolve file references and prevent warnings.";

        public string name => k_Name;

        public string description => k_Description;

        public bool Upgrade(VisualTreeAsset asset)
        {
            // The URLs have already been fixed during import
            // We just need to save the file if they were updated
            return asset != null && asset.importerWithUpdatedUrls;
        }
    }
}
