// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.ShortcutManagement
{
    interface IDiscoveryIdentifierConflictHandler
    {
        void IdentifierConflictDetected(IShortcutEntryDiscoveryInfo entry);
    }

    class DiscoveryIdentifierConflictHandler : IDiscoveryIdentifierConflictHandler
    {
        public void IdentifierConflictDetected(IShortcutEntryDiscoveryInfo entry)
        {
            var assetPath = entry.GetFilePath();
            Object asset = GetRelatedAsset(assetPath);

            string msg;
            if (File.Exists(assetPath))
            {
                msg = string.Format("Duplicate Shortcut Identifier found: {0}. Ignoring {1} defined at {2}:{3}", entry.GetShortcutEntry().identifier.path, entry.GetFullMemberName(), entry.GetFilePath(), entry.GetLineNumber());
            }
            else
            {
                msg = string.Format("Duplicate Shortcut Identifier found: {0}. Ignoring {1} ", entry.GetShortcutEntry().identifier.path, entry.GetFullMemberName());
            }

            Debug.LogWarningFormat(asset, msg);
        }

        internal static Object GetRelatedAsset(string assetPath)
        {
            if (assetPath != null)
            {
                var uriAssetsFolder = new Uri(Application.dataPath);
                var filePath = new Uri(assetPath);
                var relFileUri = uriAssetsFolder.MakeRelativeUri(filePath);

                var relativeAssetPath = Uri.UnescapeDataString(relFileUri.ToString());
                return AssetDatabase.LoadMainAssetAtPath(relativeAssetPath);
            }

            return null;
        }
    }
}
