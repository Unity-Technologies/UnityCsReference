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
        private const string k_SplitterFlexGrowPrefs = "PackageManager.SplitterFlexGrowPrefs";
        private const string k_LastUsedFilterPrefsPrefix = "PackageManager.Filter_";

        private static string projectIdentifier
        {
            get
            {
                // PlayerSettings.productGUID is already used as LocalProjectID by Analytics, so we use it too
                return PlayerSettings.productGUID.ToString();
            }
        }

        private static string lastUsedFilterForProjectPerfs { get { return k_LastUsedFilterPrefsPrefix + projectIdentifier; } }

        public virtual bool skipRemoveConfirmation
        {
            get { return EditorPrefs.GetBool(k_SkipRemoveConfirmationPrefs, false); }
            set { EditorPrefs.SetBool(k_SkipRemoveConfirmationPrefs, value); }
        }

        public virtual bool skipMultiSelectRemoveConfirmation
        {
            get { return EditorPrefs.GetBool(k_SkipMultiSelectRemoveConfirmationPrefs, false); }
            set { EditorPrefs.SetBool(k_SkipMultiSelectRemoveConfirmationPrefs, value); }
        }

        public virtual bool skipDisableConfirmation
        {
            get { return EditorPrefs.GetBool(k_SkipDisableConfirmationPrefs, false); }
            set { EditorPrefs.SetBool(k_SkipDisableConfirmationPrefs, value); }
        }

        public virtual float splitterFlexGrow
        {
            get { return EditorPrefs.GetFloat(k_SplitterFlexGrowPrefs, 0.3f); }
            set
            {
                if (float.IsNaN(value) || float.IsInfinity(value))
                    return;
                EditorPrefs.SetFloat(k_SplitterFlexGrowPrefs, value);
            }
        }

        public virtual PackageFilterTab? lastUsedPackageFilter
        {
            get
            {
                try
                {
                    return (PackageFilterTab)Enum.Parse(typeof(PackageFilterTab), EditorPrefs.GetString(lastUsedFilterForProjectPerfs, PackageFiltering.k_DefaultFilterTab.ToString()));
                }
                catch (Exception)
                {
                    return null;
                }
            }
            set
            {
                EditorPrefs.SetString(lastUsedFilterForProjectPerfs, value?.ToString());
            }
        }

        [SerializeField]
        private int m_NumItemsPerPage = 0;
        // The number of items per page is used to decide how many items to fetch in the initial refresh and it should always be a positive number
        // When the number is set to 0, we consider this value not set (null)
        public virtual int? numItemsPerPage
        {
            get { return m_NumItemsPerPage <= 0 ? (int?)null : m_NumItemsPerPage; }
            set { m_NumItemsPerPage = value ?? 0; }
        }

        [SerializeField]
        private bool m_DependenciesExpanded = true;
        public virtual bool dependenciesExpanded
        {
            get => m_DependenciesExpanded;
            set => m_DependenciesExpanded = value;
        }

        [SerializeField]
        private string m_SelectedFeatureDependency;
        public virtual string selectedFeatureDependency
        {
            get => m_SelectedFeatureDependency;
            set => m_SelectedFeatureDependency = value;
        }

        [SerializeField]
        private bool m_SamplesExpanded = true;
        public virtual bool samplesExpanded
        {
            get => m_SamplesExpanded;
            set => m_SamplesExpanded = value;
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
        public string selectedPackageDetailsTabIdentifier {
            get =>  m_SelectedPackageDetailsTabIdentifier;
            set => m_SelectedPackageDetailsTabIdentifier = value;
        }

        [SerializeField]
        private List<string> m_ExpandedDetailsExtensions = new List<string>();
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
        private List<string> m_ExpandedVersionHistoryItems = new List<string>();
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
            if (string.IsNullOrEmpty(uniqueId))
                return false;

            return m_ExpandedVersionHistoryItems.Contains(uniqueId);
        }

        public virtual void ClearExpandedVersionHistoryItems()
        {
            m_ExpandedVersionHistoryItems.Clear();
        }
    }
}
