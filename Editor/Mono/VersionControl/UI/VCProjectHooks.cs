// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.VersionControl;

namespace UnityEditorInternal.VersionControl
{
    // Display hooks for the main project window.  Icons are overlayed to show the version control state.
    internal class ProjectHooks
    {
        // GUI callback for each item visible in the project window
        public static void OnProjectWindowItem(string guid, Rect drawRect)
        {
            if (!Provider.isActive)
                return;

            Asset asset = Provider.GetAssetByGUID(guid);
            if (asset != null)
            {
                string metaPath = asset.path.Trim('/') + ".meta";
                Asset metaAsset = Provider.GetAssetByPath(metaPath);
                Overlay.DrawOverlay(asset, metaAsset, drawRect);
            }
        }

        public static Rect GetOverlayRect(Rect drawRect)
        {
            return Overlay.GetOverlayRect(drawRect);
        }
    }
}
