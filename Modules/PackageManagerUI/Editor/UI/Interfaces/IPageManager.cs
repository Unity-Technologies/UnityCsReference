// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI
{
    [Flags]
    internal enum RefreshOptions : uint
    {
        None             = 0,

        UpmListOffline   = 1 << 0,
        UpmList          = 1 << 1,
        UpmSearchOffline = 1 << 2,
        UpmSearch        = 1 << 3,
        Purchased        = 1 << 4,
        PurchasedOffline = 1 << 5,

        CurrentFilter    = 1 << 6,

        // combinations
        AllOnline        = UpmList | UpmSearch,
        AllOffline       = UpmListOffline | UpmSearchOffline,
        All              = AllOnline | AllOffline
    }

    internal interface IPageManager
    {
        bool isInitialized { get; }

        event Action<IPackageVersion> onSelectionChanged;

        // arg1: the updated page, arg2: packages added/updated in the page, arg3: packages removed from the page
        event Action<IPage, IEnumerable<IPackage>, IEnumerable<IPackage>> onPageUpdate;
        event Action<IPage> onPageRebuild;

        event Action<IEnumerable<VisualState>> onVisualStateChange;

        IPackageVersion GetSelectedVersion();

        void ClearSelection();

        void SetSelected(IPackage package, IPackageVersion version = null);

        void SetSeeAllVersions(IPackage package, bool value);

        void SetExpanded(IPackage package, bool value);

        VisualState GetVisualState(IPackage package);

        void Setup();

        void RegisterEvents();

        void UnregisterEvents();

        void Refresh(PackageFilterTab tab);

        void Refresh(RefreshOptions options);

        void Fetch(string uniqueId);

        IPage GetCurrentPage();

        bool HasFetchedPageForFilterTab(PackageFilterTab? tab = null);

        void LoadMore();
    }
}
