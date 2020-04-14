// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using UnityEditor.VersionControl;

namespace UnityEditorInternal.VersionControl
{
    public class Overlay
    {
        private static Texture2D s_BlueLeftParan;
        private static Texture2D s_BlueRightParan;
        private static Texture2D s_RedLeftParan;
        private static Texture2D s_RedRightParan;
        private static Texture2D s_DisconnectedIcon;

        public static Rect GetOverlayRect(Rect itemRect)
        {
            if (itemRect.width > itemRect.height)
            {
                itemRect.x += 16;
                itemRect.width = 20;
            }
            else
            {
                itemRect.width = 12;
            }
            itemRect.height = itemRect.width;
            return itemRect;
        }

        internal static void DrawProjectOverlay(Asset asset, Asset metaAsset, Rect itemRect)
        {
            DrawOverlay(asset, metaAsset, itemRect, false, true, false);
        }

        internal static void DrawHierarchyOverlay(Asset asset, Asset metaAsset, Rect itemRect)
        {
            DrawOverlay(asset, metaAsset, itemRect, true, false, false);
        }

        internal static void DrawOtherOverlay(Asset asset, Asset metaAsset, Rect itemRect)
        {
            DrawOverlay(asset, metaAsset, itemRect, false, false, true);
        }

        public static void DrawOverlay(Asset asset, Asset metaAsset, Rect itemRect)
        {
            if (asset == null || metaAsset == null)
                return;

            if (Event.current.type != EventType.Repaint)
                return;

            DrawOverlays(asset, metaAsset, itemRect);
        }

        public static void DrawOverlay(Asset asset, Rect itemRect)
        {
            if (asset == null)
                return;

            if (Event.current.type != EventType.Repaint)
                return;

            DrawOverlays(asset, null, itemRect, false, false, true);
        }

        internal static void DrawOverlay(Asset asset, Asset metaAsset, Rect itemRect, bool isHierarchy = false, bool isProject = false, bool otherIcons = false)
        {
            if (asset == null || metaAsset == null)
                return;

            if (Event.current.type != EventType.Repaint)
                return;

            DrawOverlays(asset, metaAsset, itemRect, isHierarchy, isProject, otherIcons);
        }

        static void DrawMetaOverlay(Rect iconRect, bool isRemote)
        {
            var offset = Mathf.FloorToInt(iconRect.width / 3);
            var pos1 = iconRect.x - offset;
            var pos2 = iconRect.x + iconRect.width + offset - iconRect.width + 1;
            iconRect.x = pos1;
            GUI.DrawTexture(iconRect, isRemote ? s_BlueLeftParan : s_RedLeftParan);
            iconRect.x = pos2;
            GUI.DrawTexture(iconRect, isRemote ? s_BlueRightParan : s_RedRightParan);
        }

        static void DrawOverlay(Asset.States state, Rect iconRect)
        {
            Rect atlasUV = Provider.GetAtlasRectForState((int)state);
            if (atlasUV.width == 0f)
                return; // no overlay

            Texture2D atlas = Provider.overlayAtlas;
            if (atlas == null)
                return;

            GUI.DrawTextureWithTexCoords(iconRect, atlas, atlasUV);
        }

        static void DrawOverlays(Asset asset, Asset metaAsset, Rect itemRect, bool isHierarchy = false,
            bool isProject = false, bool otherIcons = false)
        {
            if (!EditorUserSettings.overlayIcons && !EditorUserSettings.hierarchyOverlayIcons &&
                !EditorUserSettings.otherOverlayIcons)
                return;

            if (isHierarchy && !EditorUserSettings.hierarchyOverlayIcons)
                return;

            if (isProject && !EditorUserSettings.overlayIcons)
                return;

            if (otherIcons && !EditorUserSettings.otherOverlayIcons)
                return;

            CreateStaticResources();
            float iconWidth = Mathf.Min(itemRect.height / 2 + 4, 16);
            // slightly offset icons since their texture space is not filled with content entirely
            float offsetX = 0;
            float offsetY = 2;

            Rect topLeft = new Rect(itemRect.x - offsetX, itemRect.y - offsetY, iconWidth, iconWidth);
            Rect topRight = new Rect(itemRect.xMax - iconWidth + offsetX, itemRect.y - offsetY, iconWidth, iconWidth);
            Rect bottomLeft = new Rect(itemRect.x - offsetX, itemRect.yMax - iconWidth + offsetY, iconWidth, iconWidth);
            Rect bottomRight = new Rect(itemRect.xMax - iconWidth + offsetX, itemRect.yMax - iconWidth + offsetY, iconWidth, iconWidth);

            Asset.States assetState = asset.state;
            Asset.States metaState = metaAsset != null ? metaAsset.state : Asset.States.None;

            Asset.States unmodifiedState = Asset.States.Local | Asset.States.MetaFile | Asset.States.ReadOnly | Asset.States.Synced;
            bool isMetaUnmodifiedState = metaAsset == null || (metaState & unmodifiedState) == unmodifiedState;

            Asset.States localMetaState = metaAsset == null ? Asset.States.None : metaState & (Asset.States.AddedLocal | Asset.States.CheckedOutLocal | Asset.States.DeletedLocal | Asset.States.LockedLocal);
            Asset.States remoteMetaState = metaAsset == null ? Asset.States.None : metaState & (Asset.States.AddedRemote | Asset.States.CheckedOutRemote | Asset.States.DeletedRemote | Asset.States.LockedRemote);

            bool keepFolderMetaParans = asset.isFolder && Provider.isVersioningFolders;

            bool providerActive = Provider.isActive;

            if (!isHierarchy)
            {
                // Local state overlay
                if (!providerActive)
                {
                    GUI.DrawTexture(topLeft, s_DisconnectedIcon);
                }
                else if (Asset.IsState(assetState, Asset.States.AddedLocal) && !isHierarchy)
                {
                    DrawOverlay(Asset.States.AddedLocal, topLeft);

                    // Meta overlay if meta file is not added or already in repo and unmodified.
                    if (metaAsset != null && (localMetaState & Asset.States.AddedLocal) == 0 && !isMetaUnmodifiedState)
                        DrawMetaOverlay(topLeft, false);
                }
                else if (Asset.IsState(assetState, Asset.States.DeletedLocal))
                {
                    DrawOverlay(Asset.States.DeletedLocal, topLeft);

                    // Meta overlay if meta file is not deleted but asset is and meta file is still present or missing (ie. should have been there)
                    if (metaAsset != null && (localMetaState & Asset.States.DeletedLocal) == 0 &&
                        Asset.IsState(metaState, Asset.States.Local | Asset.States.Missing))
                        DrawMetaOverlay(topLeft, false);
                }
                else if (Asset.IsState(assetState, Asset.States.LockedLocal) && !isHierarchy)
                {
                    DrawOverlay(Asset.States.LockedLocal, topLeft);

                    // Meta overlay if meta file is not locked or unmodified.
                    if (metaAsset != null && (localMetaState & Asset.States.LockedLocal) == 0 && !isMetaUnmodifiedState)
                        DrawMetaOverlay(topLeft, false);
                }
                else if (Asset.IsState(assetState, Asset.States.CheckedOutLocal) && !isHierarchy)
                {
                    DrawOverlay(Asset.States.CheckedOutLocal, topLeft);

                    // Meta overlay if meta file is not checked out or unmodified.
                    if (metaAsset != null && (localMetaState & Asset.States.CheckedOutLocal) == 0 && !isMetaUnmodifiedState)
                        DrawMetaOverlay(topLeft, false);
                }
                else if (Asset.IsState(assetState, Asset.States.Local) &&
                         !(Asset.IsState(assetState, Asset.States.OutOfSync) ||
                           Asset.IsState(assetState, Asset.States.Synced)))
                {
                    DrawOverlay(Asset.States.Local, bottomLeft);

                    // Meta overlay if meta file is not local only or unmodified.
                    if (metaAsset != null &&
                        (metaAsset.IsUnderVersionControl || !Asset.IsState(metaState, Asset.States.Local)))
                        DrawMetaOverlay(bottomLeft, false);
                }
                // From here the local asset have no state that need a local state overlay. We use the meta state if there is one instead.
                else if (metaAsset != null && Asset.IsState(metaState, Asset.States.AddedLocal))
                {
                    DrawOverlay(Asset.States.AddedLocal, topLeft);
                    if (keepFolderMetaParans)
                        DrawMetaOverlay(topLeft, false);
                }
                else if (metaAsset != null && Asset.IsState(metaState, Asset.States.DeletedLocal))
                {
                    DrawOverlay(Asset.States.DeletedLocal, topLeft);
                    if (keepFolderMetaParans)
                        DrawMetaOverlay(topLeft, false);
                }
                else if (metaAsset != null && Asset.IsState(metaState, Asset.States.LockedLocal))
                {
                    DrawOverlay(Asset.States.LockedLocal, topLeft);
                    if (keepFolderMetaParans)
                        DrawMetaOverlay(topLeft, false);
                }
                else if (metaAsset != null && Asset.IsState(metaState, Asset.States.CheckedOutLocal))
                {
                    DrawOverlay(Asset.States.CheckedOutLocal, topLeft);
                    if (keepFolderMetaParans)
                        DrawMetaOverlay(topLeft, false);
                }
                else if (metaAsset != null && Asset.IsState(metaState, Asset.States.Local) &&
                         !(Asset.IsState(metaState, Asset.States.OutOfSync) ||
                           Asset.IsState(metaState, Asset.States.Synced))
                         && !(Asset.IsState(assetState, Asset.States.Conflicted) ||
                              (metaAsset != null && Asset.IsState(metaState, Asset.States.Conflicted))))
                {
                    DrawOverlay(Asset.States.Local, bottomLeft);
                    if (keepFolderMetaParans)
                        DrawMetaOverlay(bottomLeft, false);
                }
            }

            if (Asset.IsState(assetState, Asset.States.Conflicted) ||
                (metaAsset != null && Asset.IsState(metaState, Asset.States.Conflicted)))
            {
                if (isHierarchy)
                    bottomLeft.x += 5;

                DrawOverlay(Asset.States.Conflicted, bottomLeft);
            }

            if (providerActive && !isHierarchy)
                if ((asset.isFolder == false && Asset.IsState(assetState, Asset.States.Updating)) || (metaAsset != null && Asset.IsState(metaState, Asset.States.Updating)))
                    DrawOverlay(Asset.States.Updating, bottomRight);

            // Remote state overlay
            if (!providerActive)
            {
                // nothing; disconnected icon was already drawn for local overlay above
            }
            else if (Asset.IsState(assetState, Asset.States.AddedRemote))
            {
                DrawOverlay(Asset.States.AddedRemote, topRight);

                // Meta overlay if meta file is not added or already in repo and unmodified.
                if (metaAsset != null && (remoteMetaState & Asset.States.AddedRemote) == 0)
                    DrawMetaOverlay(topRight, true);
            }
            else if (Asset.IsState(assetState, Asset.States.DeletedRemote))
            {
                DrawOverlay(Asset.States.DeletedRemote, topRight);

                // Meta overlay if meta file is not deleted but asset is and meta file is still present or missing (ie. should have been there)
                if (metaAsset != null && (remoteMetaState & Asset.States.DeletedRemote) == 0)
                    DrawMetaOverlay(topRight, true);
            }
            else if (Asset.IsState(assetState, Asset.States.LockedRemote))
            {
                DrawOverlay(Asset.States.LockedRemote, topRight);

                // Meta overlay if meta file is not locked or unmodified.
                if (metaAsset != null && (remoteMetaState & Asset.States.LockedRemote) == 0)
                    DrawMetaOverlay(topRight, true);
            }
            else if (Asset.IsState(assetState, Asset.States.CheckedOutRemote))
            {
                DrawOverlay(Asset.States.CheckedOutRemote, topRight);

                // Meta overlay if meta file is not checked out or unmodified.
                if (metaAsset != null && (remoteMetaState & Asset.States.CheckedOutRemote) == 0)
                    DrawMetaOverlay(topRight, true);
            }
            // From here the remote asset have no state that need a remote state overlay. We use the meta state if there is one instead.
            else if (metaAsset != null && Asset.IsState(metaState, Asset.States.AddedRemote))
            {
                DrawOverlay(Asset.States.AddedRemote, topRight);
                if (keepFolderMetaParans)
                    DrawMetaOverlay(topRight, true);
            }
            else if (metaAsset != null && Asset.IsState(metaState, Asset.States.DeletedRemote))
            {
                DrawOverlay(Asset.States.DeletedRemote, topRight);
                if (keepFolderMetaParans)
                    DrawMetaOverlay(topRight, true);
            }
            else if (metaAsset != null && Asset.IsState(metaState, Asset.States.LockedRemote))
            {
                DrawOverlay(Asset.States.LockedRemote, topRight);
                if (keepFolderMetaParans)
                    DrawMetaOverlay(topRight, true);
            }
            else if (metaAsset != null && Asset.IsState(metaState, Asset.States.CheckedOutRemote))
            {
                DrawOverlay(Asset.States.CheckedOutRemote, topRight);
                if (keepFolderMetaParans)
                    DrawMetaOverlay(topRight, true);
            }

            if (Asset.IsState(assetState, Asset.States.OutOfSync) || (metaAsset != null && Asset.IsState(metaState, Asset.States.OutOfSync)))
            {
                if (isHierarchy)
                {
                    bottomLeft.x += 5;
                    DrawOverlay(Asset.States.OutOfSync, bottomLeft);
                }
                else
                    DrawOverlay(Asset.States.OutOfSync, bottomRight);
            }
        }

        private static void CreateStaticResources()
        {
            if (s_BlueLeftParan == null)
            {
                s_BlueLeftParan = EditorGUIUtility.LoadIcon("VersionControl/P4_BlueLeftParenthesis");
                s_BlueLeftParan.hideFlags = HideFlags.HideAndDontSave;

                s_BlueRightParan = EditorGUIUtility.LoadIcon("VersionControl/P4_BlueRightParenthesis");
                s_BlueRightParan.hideFlags = HideFlags.HideAndDontSave;

                s_RedLeftParan = EditorGUIUtility.LoadIcon("VersionControl/P4_RedLeftParenthesis");
                s_RedLeftParan.hideFlags = HideFlags.HideAndDontSave;

                s_RedRightParan = EditorGUIUtility.LoadIcon("VersionControl/P4_RedRightParenthesis");
                s_RedRightParan.hideFlags = HideFlags.HideAndDontSave;

                s_DisconnectedIcon = EditorGUIUtility.LoadIcon("VersionControl/P4_Offline");
                s_DisconnectedIcon.hideFlags = HideFlags.HideAndDontSave;
            }
        }
    }
}
