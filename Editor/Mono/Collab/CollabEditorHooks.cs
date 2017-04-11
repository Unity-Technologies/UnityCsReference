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
        // GUI callback for each item visible in the project window
        public static void OnProjectWindowItemIconOverlay(string guid, Rect drawRect)
        {
            Collab.CollabStates assetState = GetAssetState(guid);
            Overlay.DrawOverlays(assetState, drawRect);
        }

        public static Collab.CollabStates GetAssetState(String assetGuid)
        {
            if (!CollabAccess.Instance.IsServiceEnabled())
            {
                return Collab.CollabStates.kCollabNone;
            }

            Collab.CollabStates assetState = Collab.instance.GetAssetState(assetGuid);
            return assetState;
        }
    }

    internal class Overlay
    {
        private static double OverlaySizeOnSmallIcon = 0.6;
        private static double OverlaySizeOnLargeIcon = 0.35;
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
                    var rect = itemRect;        // Clone because we will modify it

                    var scale = OverlaySizeOnLargeIcon;
                    if (rect.width <= 24)
                    {
                        scale = OverlaySizeOnSmallIcon;
                    }

                    rect.width = Convert.ToInt32(Math.Ceiling(rect.width * scale));
                    rect.height = Convert.ToInt32(Math.Ceiling(rect.height * scale));
                    rect.x += itemRect.width - rect.width;
                    GUI.DrawTexture(rect, overlay, ScaleMode.ScaleToFit);
                }
            }
        }

        protected static bool HasState(Collab.CollabStates assetStates, Collab.CollabStates includesState)
        {
            return ((assetStates & includesState) == includesState);
        }

        public static void DrawOverlays(Collab.CollabStates assetState, Rect itemRect)
        {
            if (assetState == Collab.CollabStates.kCollabInvalidState || assetState == Collab.CollabStates.kCollabNone)
                return;

            if (Event.current.type != EventType.Repaint)
                return;

            if (!AreOverlaysLoaded())
                LoadOverlays();

            var state = GetOverlayStateForAsset(assetState);
            DrawOverlayElement(state, itemRect);
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
