// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI
{
    internal interface IPage
    {
        event Action<IPackageVersion> onSelectionChanged;
        // triggered when the state of the UI item is updated (expanded, hidden, see all versions toggled)
        event Action<IEnumerable<VisualState>> onVisualStateChange;
        // triggered when packages are added/updated or removed
        event Action<IPage, IEnumerable<IPackage>, IEnumerable<IPackage>, bool> onListUpdate;
        event Action<IPage> onListRebuild;
        PageFilters filters { get; }
        PackageFilterTab tab { get; }
        PageCapability capability { get; }

        long numTotalItems { get; }
        long numCurrentItems { get; }

        // an ordered list of `packageUniqueIds`
        IEnumerable<VisualState> visualStates { get; }
        bool isFullyLoaded { get; }

        VisualState GetVisualState(string packageUniqueId);
        void LoadMore(int numberOfPackages);
        void ClearFilters();
        void UpdateFilters(PageFilters filters);

        IPackageVersion GetSelectedVersion();

        void GetSelectedPackageAndVersion(out IPackage package, out IPackageVersion version);

        void OnPackagesChanged(IEnumerable<IPackage> added, IEnumerable<IPackage> removed, IEnumerable<IPackage> preUpdate, IEnumerable<IPackage> postUpdate);

        void Rebuild();
        void Load(IPackage package, IPackageVersion version = null);

        void SetSelected(IPackage package, IPackageVersion version = null);
        void SetSelected(string packageUniqueId, string versionUniqueId);
        void SetExpanded(string packageUniqueId, bool value);
        void SetExpanded(IPackage package, bool value);
        void SetSeeAllVersions(string packageUniqueId, bool value);
        void SetSeeAllVersions(IPackage package, bool value);

        bool Contains(IPackage package);
        bool Contains(string packageUniqueId);
    }
}
