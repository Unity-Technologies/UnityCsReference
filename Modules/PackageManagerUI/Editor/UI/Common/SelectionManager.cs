// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI
{
    internal sealed class SelectionManager
    {
        static ISelectionManager s_Instance = null;
        public static ISelectionManager instance { get { return s_Instance ?? SelectionManagerInternal.instance; } }

        /// <summary>
        /// Information to store to survive domain reload
        /// </summary>
        [Serializable]
        private class SelectionManagerInternal : ScriptableSingleton<SelectionManagerInternal>, ISelectionManager
        {
            public event Action<IEnumerable<IPackageVersion>> onSelectionChanged = delegate {};

            // we divide all selectable items into groups by filter tabs
            // each tab has its own separate group, selection change in one group won't affect others
            private List<SelectionGroup> m_SelectionGroups = new List<SelectionGroup>();

            private SelectionGroup currentGroup
            {
                get
                {
                    var filterTabIndex = (int)PackageFiltering.instance.currentFilterTab;
                    while (instance.m_SelectionGroups.Count <= filterTabIndex)
                        instance.m_SelectionGroups.Add(new SelectionGroup());
                    return instance.m_SelectionGroups[filterTabIndex];
                }
            }

            public IEnumerable<IPackageVersion> GetSelections()
            {
                return currentGroup.selections;
            }

            public void ClearSelection()
            {
                SetSelected((IPackage)null);
            }

            public void SetSelected(IPackage package, IPackageVersion version = null)
            {
                if (IsSelected(package, version))
                    return;

                version = version ?? package?.primaryVersion;
                currentGroup.SetSelected(package?.uniqueId, version?.uniqueId);
                onSelectionChanged(currentGroup.selections);
            }

            public bool IsSelected(IPackage package, IPackageVersion version = null)
            {
                version = version ?? package?.primaryVersion;
                return currentGroup.IsSelected(package?.uniqueId, version?.uniqueId);
            }

            public void SetSeeAllVersions(IPackage package, bool value)
            {
                currentGroup.SetSeeAllVersions(package?.uniqueId, value);
            }

            public bool IsSeeAllVersions(IPackage package)
            {
                return currentGroup.IsSeeAllVersions(package.uniqueId);
            }

            public void SetExpanded(IPackage package, bool value)
            {
                currentGroup.SetExpanded(package?.uniqueId, value);
            }

            public bool IsExpanded(IPackage package)
            {
                return currentGroup.IsExpanded(package.uniqueId);
            }

            private void OnInstalledOrUninstalled(IPackage package, IPackageVersion installedVersion = null)
            {
                if (package != null)
                    SetSelected(package, installedVersion);
            }

            public void Setup()
            {
                PackageDatabase.instance.onInstallSuccess += OnInstalledOrUninstalled;
                PackageDatabase.instance.onUninstallSuccess += package => OnInstalledOrUninstalled(package);
            }
        }

        [Serializable]
        private class SelectionGroup
        {
            [Serializable]
            internal class ExpansionState
            {
                public string packageUniqueId;
                public bool expanded;
                public bool seeAllVersion;
            }

            [Serializable]
            internal class SelectionState
            {
                public string packageUniqueId;
                public string versionUniqueId;
            }

            private List<ExpansionState> m_ExpansionStates = new List<ExpansionState>();
            private List<SelectionState> m_SelectionStates = new List<SelectionState>();

            internal IEnumerable<IPackageVersion> selections
            {
                get
                {
                    return m_SelectionStates.Select(s =>
                    {
                        IPackage package;
                        IPackageVersion version;
                        PackageDatabase.instance.GetPackageAndVersion(s.packageUniqueId, s.versionUniqueId, out package, out version);
                        return version ?? package?.primaryVersion;
                    }).Where(p => p != null);
                }
            }

            public void SetSelected(string packageUniqueId, string versionUniqueId)
            {
                if (IsSelected(packageUniqueId, versionUniqueId))
                    return;
                m_SelectionStates.Clear();

                if (!string.IsNullOrEmpty(packageUniqueId) && !string.IsNullOrEmpty(versionUniqueId))
                    m_SelectionStates.Add(new SelectionState { packageUniqueId = packageUniqueId, versionUniqueId = versionUniqueId});
            }

            public bool IsSelected(string packageUniqueId, string versionUniqueId)
            {
                return m_SelectionStates.Any(s => s.packageUniqueId == packageUniqueId && s.versionUniqueId == versionUniqueId);
            }

            public void SetSeeAllVersions(string packageUniqueId, bool value)
            {
                var state = GetExpansionState(packageUniqueId, value);
                if (state != null)
                    state.seeAllVersion = value;
            }

            public bool IsSeeAllVersions(string packageUniqueId)
            {
                return GetExpansionState(packageUniqueId)?.seeAllVersion ?? false;
            }

            public void SetExpanded(string packageUniqueId, bool value)
            {
                var state = GetExpansionState(packageUniqueId, value);
                if (state != null)
                {
                    state.expanded = value;
                    if (!state.expanded)
                        m_ExpansionStates.Remove(state);
                }
            }

            public bool IsExpanded(string packageUniqueId)
            {
                return GetExpansionState(packageUniqueId)?.expanded ?? false;
            }

            private ExpansionState GetExpansionState(string packageUniqueId, bool createNewIfNotFound = false)
            {
                if (string.IsNullOrEmpty(packageUniqueId))
                    return null;
                var state = m_ExpansionStates.FirstOrDefault(s => s.packageUniqueId == packageUniqueId);
                if (state == null && createNewIfNotFound)
                {
                    state = new ExpansionState { packageUniqueId = packageUniqueId, expanded = false, seeAllVersion = false };
                    m_ExpansionStates.Add(state);
                }
                return state;
            }
        }
    }
}
