// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor.VersionControl;
using UnityEditor;
using UnityEditor.Experimental;

namespace UnityEditorInternal.VersionControl
{
    // Display hooks for the main project window.  Icons are overlayed to show the version control state.
    internal class ProjectHooks
    {
        static readonly GUIContent s_NotImportedAssetTooltip = L10n.TextContent(string.Empty, "Asset is not available because Unity is in Safe Mode", null, null);

        // GUI callback for each item visible in the project window
        public static void OnProjectWindowItem(string guid, Rect drawRect, Action repaintAction)
        {
            HandleVersionControlOverlays(guid, drawRect);
            HandleNotImportedAssetsInSafeModeOverlay(guid, drawRect);
        }

        static void HandleVersionControlOverlays(string guid, Rect drawRect)
        {
            var vco = VersionControlManager.activeVersionControlObject;
            if (vco != null)
            {
                var extension = vco.GetExtension<IIconOverlayExtension>();
                if (extension != null)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.IsNullOrEmpty(assetPath))
                        extension.DrawOverlay(assetPath, IconOverlayType.Project, drawRect);
                }
                return;
            }

            if (Provider.isActive)
            {
                string vcsType = VersionControlSettings.mode;
                if (vcsType == ExternalVersionControl.Disabled ||
                    vcsType == ExternalVersionControl.AutoDetect ||
                    vcsType == ExternalVersionControl.Generic)
                    return; // no icons for these version control systems

                Asset asset = Provider.GetAssetByGUID(guid);
                if (asset != null)
                {
                    string metaPath = asset.path.Trim('/') + ".meta";
                    Asset metaAsset = Provider.GetAssetByPath(metaPath);
                    Overlay.DrawProjectOverlay(asset, metaAsset, drawRect);
                }
            }
        }

        static void HandleNotImportedAssetsInSafeModeOverlay(string guid, Rect drawRect)
        {
            if (EditorUtility.isInSafeMode)
            {
                GUID lookupGUID = new GUID(guid);
                var hash = AssetDatabaseExperimental.LookupArtifact(AssetDatabaseExperimental.CreateArtifactKey(lookupGUID));
                if (!hash.isValid)
                    GUI.Label(drawRect, s_NotImportedAssetTooltip);
            }
        }

        public static Rect GetOverlayRect(Rect drawRect)
        {
            return Overlay.GetOverlayRect(drawRect);
        }
    }
}
