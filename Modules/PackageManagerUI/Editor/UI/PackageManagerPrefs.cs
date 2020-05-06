// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class PackageManagerPrefs
    {
        private const string k_SkipRemoveConfirmationPrefs = "PackageManager.SkipRemoveConfirmation";
        private const string k_SkipDisableConfirmationPrefs = "PackageManager.SkipDisableConfirmation";
        private const string k_ShowPackageDependenciesPrefs = "PackageManager.ShowPackageDependencies";
        private const string k_LastUsedFilterPrefsPrefix = "PackageManager.Filter_";

        public event Action<bool> onShowDependenciesChanged = delegate {};

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

        [SerializeField]
        private bool m_DismissPreviewPackagesInUse;
        public bool dismissPreviewPackagesInUse
        {
            get => m_DismissPreviewPackagesInUse;
            set => m_DismissPreviewPackagesInUse = value;
        }

        public virtual bool skipDisableConfirmation
        {
            get { return EditorPrefs.GetBool(k_SkipDisableConfirmationPrefs, false); }
            set { EditorPrefs.SetBool(k_SkipDisableConfirmationPrefs, value); }
        }

        public virtual bool showPackageDependencies
        {
            get { return EditorPrefs.GetBool(k_ShowPackageDependenciesPrefs, false); }
            set
            {
                var oldValue = showPackageDependencies;
                EditorPrefs.SetBool(k_ShowPackageDependenciesPrefs, value);
                if (oldValue != value)
                    onShowDependenciesChanged(value);
            }
        }

        public virtual PackageFilterTab? lastUsedPackageFilter
        {
            get
            {
                try
                {
                    return (PackageFilterTab)Enum.Parse(typeof(PackageFilterTab), EditorPrefs.GetString(lastUsedFilterForProjectPerfs, defaultFilterTab.ToString()));
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

        public virtual PackageFilterTab defaultFilterTab
        {
            get { return PackageFilterTab.InProject; }
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
        private bool m_DependenciesExpanded = false;
        public virtual bool dependenciesExpanded
        {
            get => m_DependenciesExpanded;
            set => m_DependenciesExpanded = value;
        }

        [SerializeField]
        private bool m_SamplesExpanded = false;
        public virtual bool samplesExpanded
        {
            get => m_SamplesExpanded;
            set => m_SamplesExpanded = value;
        }
    }
}
