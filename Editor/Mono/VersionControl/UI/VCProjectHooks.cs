// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor.VersionControl;
using UnityEditor;
using UnityEditor.Experimental;
using System.Collections.Generic;

namespace UnityEditorInternal.VersionControl
{
    // Display hooks for the main project window.  Icons are overlayed to show the version control state.
    internal class ProjectHooks
    {
        const float k_AnimatedProgressImageUpdateInterval = 15.0f; // 15 updates/sec
        const double k_AnimatedProgressImageTimeout = 0.5; // Stop animation repaints when the are no updates for 500 ms
        static double s_LastInvalidArtifactHashTime = 0;
        static readonly List<Action> s_ProgressRepainters = new List<Action>();
        static readonly CallbackController s_CallbackController = new CallbackController(RepaintProgress, k_AnimatedProgressImageUpdateInterval);

        static void RepaintProgress()
        {
            if (s_ProgressRepainters == null)
                return;

            foreach (var p in s_ProgressRepainters)
                p();
        }

        // GUI callback for each item visible in the project window
        public static void OnProjectWindowItem(string guid, Rect drawRect, Action repaintAction)
        {
            HandleVersionControlOverlays(guid, drawRect);
            HandleOndemandProgressOverlay(guid, drawRect, repaintAction);
        }

        static void HandleVersionControlOverlays(string guid, Rect drawRect)
        {
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

        static void HandleOndemandProgressOverlay(string guid, Rect drawRect, Action repaintAction)
        {
            var now = EditorApplication.timeSinceStartup;
            if (repaintAction != null)
            {
                GUID lookupGUID = new GUID(guid);
                var hash = AssetDatabaseExperimental.LookupArtifact(new ArtifactKey(lookupGUID));
                if (!hash.isValid)
                {
                    if (s_ProgressRepainters.IndexOf(repaintAction) == -1)
                        s_ProgressRepainters.Add(repaintAction);

                    s_LastInvalidArtifactHashTime = now;

                    if (!s_CallbackController.active)
                        s_CallbackController.Start();

                    var texture = InternalEditorUtility.animatedProgressImage.image;
                    var xOffset = (drawRect.width - texture.width) / 2.0f;
                    if (xOffset < 0)
                        xOffset = 0;
                    var yOffset = (drawRect.height - texture.height) / 2.0f;
                    if (yOffset < 0)
                        yOffset = 0;
                    var width = texture.width <= drawRect.width ? texture.width : drawRect.width;
                    var height = texture.height <= drawRect.height ? texture.height : drawRect.height;
                    var rect = new Rect(drawRect.x + xOffset, drawRect.y + yOffset, width, height);
                    GUI.DrawTexture(rect, texture);
                }
            }

            if (s_CallbackController.active && (s_LastInvalidArtifactHashTime + k_AnimatedProgressImageTimeout) < now)
            {
                s_CallbackController.Stop();
                s_ProgressRepainters.Clear();
            }
        }

        public static Rect GetOverlayRect(Rect drawRect)
        {
            return Overlay.GetOverlayRect(drawRect);
        }
    }
}
