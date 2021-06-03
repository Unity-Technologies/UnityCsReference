// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IPage
    {
        event Action<IPackageVersion> onSelectionChanged;
        // triggered when the state of the UI item is updated (expanded, hidden, see all versions toggled)
        event Action<IEnumerable<VisualState>> onVisualStateChange;
        // triggered when packages are added/updated or removed
        event Action<IPage, IEnumerable<IPackage>, IEnumerable<IPackage>, bool> onListUpdate;
        event Action<IPage> onListRebuild;
        event Action<IPage> onSubPageAdded;
        PageFilters filters { get; }
        PackageFilterTab tab { get; }
        PageCapability capability { get; }

        long numTotalItems { get; }
        long numCurrentItems { get; }

        IEnumerable<SubPage> subPages { get; }
        SubPage currentSubPage { get; set; }
        void AddSubPage(SubPage subPage);

        // an ordered list of `packageUniqueIds`
        IEnumerable<VisualState> visualStates { get; }
        bool isFullyLoaded { get; }

        VisualState GetVisualState(string packageUniqueId);
        VisualState GetSelectedVisualState();
        void LoadMore(long numberOfPackages);
        void ClearFilters();
        void UpdateFilters(PageFilters filters);

        IPackageVersion GetSelectedVersion();

        void GetSelectedPackageAndVersion(out IPackage package, out IPackageVersion version);

        void OnPackagesChanged(IEnumerable<IPackage> added, IEnumerable<IPackage> removed, IEnumerable<IPackage> preUpdate, IEnumerable<IPackage> postUpdate);

        void Rebuild();
        void Load(IPackage package, IPackageVersion version = null);

        void SetSelected(IPackage package, IPackageVersion version = null);
        void SetSelected(string packageUniqueId, string versionUniqueId);
        void RefreshSelected();

        void SetExpanded(string packageUniqueId, bool value);
        void SetExpanded(IPackage package, bool value);
        void SetSeeAllVersions(string packageUniqueId, bool value);
        void SetSeeAllVersions(IPackage package, bool value);
        bool IsGroupExpanded(string groupName);
        void SetGroupExpanded(string groupName, bool value);
        string GetGroupName(IPackage package);

        bool Contains(IPackage package);
        bool Contains(string packageUniqueId);
        void SetPackagesUserUnlockedState(IEnumerable<string> packageUniqueIds, bool unlocked);
        void ResetUserUnlockedState();
        bool GetDefaultLockState(IPackage package);
    }
}
