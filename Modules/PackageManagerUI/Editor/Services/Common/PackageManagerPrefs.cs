// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class PackageManagerPrefs
    {
        private const string k_SkipRemoveConfirmationPrefs = "PackageManager.SkipRemoveConfirmation";
        private const string k_SkipMultiSelectRemoveConfirmationPrefs = "PackageManager.SkipMultiSelectRemoveConfirmation";
        private const string k_SkipDisableConfirmationPrefs = "PackageManager.SkipDisableConfirmation";
        private const string k_LeftContainerWidthPrefs = "PackageManager.LeftContainerWidthPrefs";
        private const string k_SidebarWidthPrefs = "PackageManager.SidebarWidthPrefs";
        private const string k_LastActivePageIdPrefsPrefix = "PackageManager.PageId_";

        public const int k_DefaultPageSize = 25;

        // PlayerSettings.productGUID is already used as LocalProjectID by Analytics, so we use it too
        private static string lastActivePageIdForProjectPrefs => k_LastActivePageIdPrefsPrefix + PlayerSettings.productGUID;

        public virtual bool skipRemoveConfirmation
        {
            get => EditorPrefs.GetBool(k_SkipRemoveConfirmationPrefs, false);
            set => EditorPrefs.SetBool(k_SkipRemoveConfirmationPrefs, value);
        }

        public virtual bool skipMultiSelectRemoveConfirmation
        {
            get => EditorPrefs.GetBool(k_SkipMultiSelectRemoveConfirmationPrefs, false);
            set => EditorPrefs.SetBool(k_SkipMultiSelectRemoveConfirmationPrefs, value);
        }

        public virtual bool skipDisableConfirmation
        {
            get => EditorPrefs.GetBool(k_SkipDisableConfirmationPrefs, false);
            set => EditorPrefs.SetBool(k_SkipDisableConfirmationPrefs, value);
        }

        public virtual float leftContainerWidth
        {
            get => EditorPrefs.GetFloat(k_LeftContainerWidthPrefs, 300);
            set
            {
                if (float.IsNaN(value) || float.IsInfinity(value))
                    return;
                EditorPrefs.SetFloat(k_LeftContainerWidthPrefs, value);
            }
        }

        public virtual float sidebarWidth
        {
            get => EditorPrefs.GetFloat(k_SidebarWidthPrefs, 225);
            set
            {
                if (float.IsNaN(value) || float.IsInfinity(value))
                    return;
                EditorPrefs.SetFloat(k_SidebarWidthPrefs, value);
            }
        }

        public virtual string activePageIdFromLastUnitySession
        {
            get => EditorPrefs.GetString(lastActivePageIdForProjectPrefs, null);
            set => EditorPrefs.SetString(lastActivePageIdForProjectPrefs, value);
        }

        [SerializeField]
        private int m_NumItemsPerPage;
        // The number of items per page is used to decide how many items to fetch in the initial refresh and it should always be a positive number
        // When the number is set to 0, we consider this value not set (null)
        public virtual int? numItemsPerPage
        {
            get => m_NumItemsPerPage <= 0 ? null : m_NumItemsPerPage;
            set => m_NumItemsPerPage = value ?? 0;
        }

        [SerializeField]
        private string m_SelectedFeatureDependency;
        public virtual string selectedFeatureDependency
        {
            get => m_SelectedFeatureDependency;
            set => m_SelectedFeatureDependency = value;
        }

        [SerializeField]
        private bool m_OverviewFoldoutExpanded = true;
        public virtual bool overviewFoldoutExpanded
        {
            get => m_OverviewFoldoutExpanded;
            set => m_OverviewFoldoutExpanded = value;
        }

        [SerializeField]
        private float m_PackageDetailVerticalScrollOffset;
        public float packageDetailVerticalScrollOffset
        {
            get => m_PackageDetailVerticalScrollOffset;
            set => m_PackageDetailVerticalScrollOffset = value;
        }

        [SerializeField]
        private string m_SelectedPackageDetailsTabIdentifier;
        public string selectedPackageDetailsTabIdentifier
        {
            get =>  m_SelectedPackageDetailsTabIdentifier;
            set => m_SelectedPackageDetailsTabIdentifier = value;
        }

        [SerializeField]
        private SortedColumn[] m_ImportedAssetsSortedColumns = Array.Empty<SortedColumn>();

        public virtual SortedColumn[] importedAssetsSortedColumns
        {
            get => m_ImportedAssetsSortedColumns;
            set => m_ImportedAssetsSortedColumns = value ?? Array.Empty<SortedColumn>();
        }

        [SerializeField]
        private List<string> m_ExpandedDetailsExtensions = new();
        public virtual bool IsDetailsExtensionExpanded(string extensionTitle)
        {
            return !string.IsNullOrEmpty(extensionTitle) && m_ExpandedDetailsExtensions.Contains(extensionTitle);
        }

        public virtual void SetDetailsExtensionExpanded(string extensionTitle, bool value)
        {
            if (string.IsNullOrEmpty(extensionTitle))
                return;

            var index = m_ExpandedDetailsExtensions.IndexOf(extensionTitle);
            if (value && index < 0)
                m_ExpandedDetailsExtensions.Add(extensionTitle);
            else if (!value && index >= 0)
                m_ExpandedDetailsExtensions.RemoveAt(index);
        }

        [SerializeField]
        private string m_PackageDisplayedInVersionHistoryTab;
        public virtual string packageDisplayedInVersionHistoryTab
        {
            get => m_PackageDisplayedInVersionHistoryTab;
            set => m_PackageDisplayedInVersionHistoryTab = value;
        }

        [SerializeField]
        private List<string> m_ExpandedVersionHistoryItems = new();
        public virtual void SetVersionHistoryItemExpanded(string uniqueId, bool expanded)
        {
            if (string.IsNullOrEmpty(uniqueId))
                return;

            var index = m_ExpandedVersionHistoryItems.IndexOf(uniqueId);
            if (expanded && index < 0)
                m_ExpandedVersionHistoryItems.Add(uniqueId);
            else if (!expanded && index >= 0)
                m_ExpandedVersionHistoryItems.RemoveAt(index);
        }

        public virtual bool IsVersionHistoryItemExpanded(string uniqueId)
        {
            return !string.IsNullOrEmpty(uniqueId) && m_ExpandedVersionHistoryItems.Contains(uniqueId);
        }

        public virtual void ClearExpandedVersionHistoryItems()
        {
            m_ExpandedVersionHistoryItems.Clear();
        }
    }
}
