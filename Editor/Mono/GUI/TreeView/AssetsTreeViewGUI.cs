// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.IMGUI.Controls;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using UnityEditorInternal.VersionControl;
using UnityEditor.VersionControl;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEditor
{
    internal class AssetsTreeViewGUI : TreeViewGUI
    {
        static bool s_VCEnabled;
        const float k_IconOverlayPadding = 7f;

        internal delegate void OnAssetIconDrawDelegate(Rect iconRect, string guid);
        internal static event OnAssetIconDrawDelegate postAssetIconDrawCallback = null;

        internal delegate bool OnAssetLabelDrawDelegate(Rect drawRect, string guid);
        internal static event OnAssetLabelDrawDelegate postAssetLabelDrawCallback = null;

        private static IDictionary<int, string> s_GUIDCache = null;

        public AssetsTreeViewGUI(TreeViewController treeView)
            : base(treeView)
        {
            iconOverlayGUI += OnIconOverlayGUI;
            labelOverlayGUI += OnLabelOverlayGUI;
            k_TopRowMargin = 4f;
        }

        // ---------------------
        // OnGUI section

        override public void BeginRowGUI()
        {
            s_VCEnabled = Provider.isActive;
            iconLeftPadding = iconRightPadding = s_VCEnabled ? k_IconOverlayPadding : 0f;
            base.BeginRowGUI();
        }

        //-------------------
        // Create asset and Rename asset section

        protected CreateAssetUtility GetCreateAssetUtility()
        {
            return ((TreeViewStateWithAssetUtility)m_TreeView.state).createAssetUtility;
        }

        virtual protected bool IsCreatingNewAsset(int instanceID)
        {
            return GetCreateAssetUtility().IsCreatingNewAsset() && IsRenaming(instanceID);
        }

        override protected void ClearRenameAndNewItemState()
        {
            GetCreateAssetUtility().Clear();
            base.ClearRenameAndNewItemState();
        }

        override protected void RenameEnded()
        {
            string name = string.IsNullOrEmpty(GetRenameOverlay().name) ? GetRenameOverlay().originalName : GetRenameOverlay().name;
            int instanceID = GetRenameOverlay().userData;
            bool isCreating = GetCreateAssetUtility().IsCreatingNewAsset();
            bool userAccepted = GetRenameOverlay().userAcceptedRename;

            if (userAccepted)
            {
                if (isCreating)
                {
                    // Create a new asset
                    GetCreateAssetUtility().EndNewAssetCreation(name);
                }
                else
                {
                    // Rename an existing asset
                    ObjectNames.SetNameSmartWithInstanceID(instanceID, name);
                }
            }
        }

        override protected void SyncFakeItem()
        {
            if (!m_TreeView.data.HasFakeItem() && GetCreateAssetUtility().IsCreatingNewAsset())
            {
                int parentInstanceID = AssetDatabase.GetMainAssetInstanceID(GetCreateAssetUtility().folder);
                m_TreeView.data.InsertFakeItem(GetCreateAssetUtility().instanceID, parentInstanceID, GetCreateAssetUtility().originalName, GetCreateAssetUtility().icon);
            }

            if (m_TreeView.data.HasFakeItem() && !GetCreateAssetUtility().IsCreatingNewAsset())
            {
                m_TreeView.data.RemoveFakeItem();
            }
        }

        // Not part of interface because it is very specific to creating assets
        virtual public void BeginCreateNewAsset(int instanceID, EndNameEditAction endAction, string pathName, Texture2D icon, string resourceFile)
        {
            ClearRenameAndNewItemState();

            if (GetCreateAssetUtility().BeginNewAssetCreation(instanceID, endAction, pathName, icon, resourceFile))
            {
                SyncFakeItem();

                // Start nameing the asset
                bool renameStarted = GetRenameOverlay().BeginRename(GetCreateAssetUtility().originalName, instanceID, 0f);
                if (!renameStarted)
                    Debug.LogError("Rename not started (when creating new asset)");
            }
        }

        // Handles fetching rename icon or cached asset database icon
        protected override Texture GetIconForItem(TreeViewItem item)
        {
            if (item == null)
                return null;

            Texture icon = null;
            if (IsCreatingNewAsset(item.id))
                icon = GetCreateAssetUtility().icon;

            if (icon == null)
                icon = item.icon;

            if (icon == null && item.id != 0)
            {
                string path = AssetDatabase.GetAssetPath(item.id);
                icon = AssetDatabase.GetCachedIcon(path);
            }
            return icon;
        }

        private void OnIconOverlayGUI(TreeViewItem item, Rect overlayRect)
        {
            if (postAssetIconDrawCallback != null && AssetDatabase.IsMainAsset(item.id))
            {
                string guid = GetGUIDForInstanceID(item.id);
                postAssetIconDrawCallback(overlayRect, guid);
            }

            // Draw vcs icons
            if (s_VCEnabled && AssetDatabase.IsMainAsset(item.id))
            {
                string guid = GetGUIDForInstanceID(item.id);
                ProjectHooks.OnProjectWindowItem(guid, overlayRect);
            }
        }

        private void OnLabelOverlayGUI(TreeViewItem item, Rect labelRect)
        {
            if (postAssetLabelDrawCallback != null && AssetDatabase.IsMainAsset(item.id))
            {
                string guid = GetGUIDForInstanceID(item.id);
                postAssetLabelDrawCallback(labelRect, guid);
            }
        }

        // Returns a previously stored GUID for the given ID,
        // else retrieves it from the asset database and stores it.
        private static string GetGUIDForInstanceID(int instanceID)
        {
            if (s_GUIDCache == null)
            {
                s_GUIDCache = new Dictionary<int, string>();
            }

            string GUID = null;
            if (!s_GUIDCache.TryGetValue(instanceID, out GUID))
            {
                string path = AssetDatabase.GetAssetPath(instanceID);
                GUID = AssetDatabase.AssetPathToGUID(path);
                Assert.IsTrue(!string.IsNullOrEmpty(GUID));
                s_GUIDCache.Add(instanceID, GUID);
            }

            return GUID;
        }
    }


    [System.Serializable]
    internal class TreeViewStateWithAssetUtility : TreeViewState
    {
        [SerializeField]
        CreateAssetUtility m_CreateAssetUtility = new CreateAssetUtility();

        internal CreateAssetUtility createAssetUtility { get { return m_CreateAssetUtility; } set { m_CreateAssetUtility = value; } }

        internal override void OnAwake()
        {
            base.OnAwake();

            // Clear state that should not survive closing/starting Unity (If TreeViewState is in EditorWindow that are serialized in a layout file)
            m_CreateAssetUtility.Clear();
        }
    }
}
