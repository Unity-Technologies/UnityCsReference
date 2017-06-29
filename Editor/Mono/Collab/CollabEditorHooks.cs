// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor.Web;
using UnityEditor.Connect;

namespace UnityEditor.Collaboration
{
    // Display hooks for the main project window.  Icons are overlayed to show the version control state.
    internal class CollabProjectHook
    {
        // GUI callback for each item visible in the project window/object list area
        public static void OnProjectWindowIconOverlay(Rect iconRect, string guid, bool isListMode)
        {
            DrawProjectBrowserIconOverlay(iconRect, guid, isListMode);
        }

        // Draw icons in the Favorites/Asset Folder area of the project browser
        public static void OnProjectBrowserNavPanelIconOverlay(Rect iconRect, string guid)
        {
            DrawProjectBrowserIconOverlay(iconRect, guid, true);
        }

        private static void DrawProjectBrowserIconOverlay(Rect iconRect, string guid, bool isListMode)
        {
            if (Collab.instance.IsCollabEnabledForCurrentProject())
            {
                Collab.CollabStates assetState = GetAssetState(guid);
                Overlay.DrawOverlays(assetState, iconRect, isListMode);
            }
        }

        public static Collab.CollabStates GetAssetState(String assetGuid)
        {
            if (!Collab.instance.IsCollabEnabledForCurrentProject())
            {
                return Collab.CollabStates.kCollabNone;
            }

            Collab.CollabStates assetState = Collab.instance.GetAssetState(assetGuid);
            return assetState;
        }
    }

    internal class Overlay
    {
        public const double k_OverlaySizeOnSmallIcon = 0.6;
        public const double k_OverlaySizeOnLargeIcon = 0.35;

        private static readonly Dictionary<Collab.CollabStates, GUIContent> s_Overlays = new Dictionary<Collab.CollabStates, GUIContent>();

        protected static void LoadOverlays()
        {
            // Order of priority must match GetLocalStatus (CollabClient.h)
            s_Overlays.Clear();
            s_Overlays.Add(Collab.CollabStates.kCollabIgnored, EditorGUIUtility.IconContent("CollabExclude Icon"));
            s_Overlays.Add(Collab.CollabStates.kCollabConflicted, EditorGUIUtility.IconContent("CollabConflict Icon"));
            s_Overlays.Add(Collab.CollabStates.kCollabPendingMerge, EditorGUIUtility.IconContent("CollabConflict Icon"));
            s_Overlays.Add(Collab.CollabStates.kCollabMovedLocal, EditorGUIUtility.IconContent("CollabMoved Icon"));
            s_Overlays.Add(Collab.CollabStates.kCollabCheckedOutLocal | Collab.CollabStates.kCollabMovedLocal, EditorGUIUtility.IconContent("CollabMoved Icon"));
            s_Overlays.Add(Collab.CollabStates.kCollabCheckedOutLocal, EditorGUIUtility.IconContent("CollabEdit Icon"));
            s_Overlays.Add(Collab.CollabStates.kCollabAddedLocal, EditorGUIUtility.IconContent("CollabCreate Icon"));
            s_Overlays.Add(Collab.CollabStates.kCollabDeletedLocal, EditorGUIUtility.IconContent("CollabDeleted Icon"));

            // The folder overlay should take precedence on the folder content's status.
            s_Overlays.Add(Collab.CollabStates.KCollabContentConflicted, EditorGUIUtility.IconContent("CollabChangesConflict Icon"));
            s_Overlays.Add(Collab.CollabStates.KCollabContentChanged, EditorGUIUtility.IconContent("CollabChanges Icon"));
            s_Overlays.Add(Collab.CollabStates.KCollabContentDeleted, EditorGUIUtility.IconContent("CollabChangesDeleted Icon"));
        }

        protected static bool AreOverlaysLoaded()
        {
            if (s_Overlays.Count == 0)
                return false;

            foreach (var icon in s_Overlays.Values)
            {
                if (icon == null)
                    return false;
            }

            return true;
        }

        protected static Collab.CollabStates GetOverlayStateForAsset(Collab.CollabStates assetStates)
        {
            foreach (var state in s_Overlays.Keys)
            {
                if (HasState(assetStates, state))
                    return state;
            }

            return Collab.CollabStates.kCollabNone;
        }

        protected static void DrawOverlayElement(Collab.CollabStates singleState, Rect itemRect)
        {
            GUIContent content;
            if (s_Overlays.TryGetValue(singleState, out content))
            {
                Texture overlay = content.image;
                if (overlay != null)
                {
                    GUI.DrawTexture(itemRect, overlay, ScaleMode.ScaleToFit);
                }
            }
        }

        protected static bool HasState(Collab.CollabStates assetStates, Collab.CollabStates includesState)
        {
            return ((assetStates & includesState) == includesState);
        }

        public static void DrawOverlays(Collab.CollabStates assetState, Rect itemRect, bool isListMode)
        {
            if (assetState == Collab.CollabStates.kCollabInvalidState || assetState == Collab.CollabStates.kCollabNone)
                return;

            if (Event.current.type != EventType.Repaint)
                return;

            if (!AreOverlaysLoaded())
                LoadOverlays();

            var state = GetOverlayStateForAsset(assetState);
            DrawOverlayElement(state, GetRectForTopRight(itemRect, GetScale(itemRect, isListMode)));
        }

        // Return a new Rect with its width and height scaled, and converted to the ceiling.
        public static Rect ScaleRect(Rect rect, double scale)
        {
            Rect scaledRect = new Rect(rect);
            scaledRect.width = Convert.ToInt32(Math.Ceiling(rect.width * scale));
            scaledRect.height = Convert.ToInt32(Math.Ceiling(rect.height * scale));
            return scaledRect;
        }

        public static double GetScale(Rect rect, bool isListMode)
        {
            double scale = k_OverlaySizeOnLargeIcon;
            if (isListMode)
            {
                scale = k_OverlaySizeOnSmallIcon;
            }
            return scale;
        }

        public static Rect GetRectForTopRight(Rect projectBrowserDrawRect, double scale)
        {
            Rect scaledRect = ScaleRect(projectBrowserDrawRect, scale);
            scaledRect.x += (projectBrowserDrawRect.width - scaledRect.width);
            return scaledRect;
        }

        public static Rect GetRectForBottomRight(Rect projectBrowserDrawRect, double scale)
        {
            Rect scaledRect = ScaleRect(projectBrowserDrawRect, scale);
            scaledRect.x += (projectBrowserDrawRect.width - scaledRect.width);
            scaledRect.y += (projectBrowserDrawRect.height - scaledRect.height);
            return scaledRect;
        }
    }

    internal static class TextureUtility
    {
        public static Texture2D LoadTextureFromApplicationContents(string path)
        {
            var tex = new Texture2D(2, 2);

            string resourcesPath = Path.Combine(Path.Combine(Path.Combine(EditorApplication.applicationContentsPath, "Resources"), "Collab"), "overlays");
            path = Path.Combine(resourcesPath, path);

            try
            {
                var fs = File.OpenRead(path);
                var bytes = new byte[fs.Length];
                fs.Read(bytes, 0, (int)fs.Length);
                if (!tex.LoadImage(bytes)) return null;
            }
            catch (Exception)
            {
                Debug.LogWarning("Collab Overlay Texture load fail, path: " + path);
                return null;
            }

            return tex;
        }
    }
}
