// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI
{
    internal interface IPageManager
    {
        bool isInitialized { get; }

        event Action<IPackageVersion> onSelectionChanged;
        event Action<IEnumerable<VisualState>> onVisualStateChange;

        // arg1: the updated page, arg2: packages added/updated in the page, arg3: packages removed from the page
        event Action<IPage, IEnumerable<IPackage>, IEnumerable<IPackage>, bool> onListUpdate;
        event Action<IPage> onListRebuild;

        event Action onRefreshOperationStart;
        event Action onRefreshOperationFinish;
        event Action<UIError> onRefreshOperationError;

        IPackageVersion GetSelectedVersion();
        void GetSelectedPackageAndVersion(out IPackage package, out IPackageVersion version);

        void ClearSelection();

        void SetSelected(IPackage package, IPackageVersion version = null, bool forceSelectInInspector = false);

        void SetSeeAllVersions(IPackage package, bool value);

        void SetExpanded(IPackage package, bool value);

        VisualState GetVisualState(IPackage package);

        void Setup();

        void RegisterEvents();

        void UnregisterEvents();

        void Refresh(PackageFilterTab? tab = null, int pageSize = 25);

        void Refresh(RefreshOptions options, int pageSize = 25);

        void Reload();

        void Fetch(string uniqueId);

        IPage GetCurrentPage();

        IPage GetPage(PackageFilterTab tab);

        PackageFilterTab FindTab(IPackage package, IPackageVersion version = null);

        long GetRefreshTimestamp(PackageFilterTab? tab = null);
        UIError GetRefreshError(PackageFilterTab? tab = null);
        bool IsRefreshInProgress(PackageFilterTab? tab = null);
        bool IsInitialFetchingDone(PackageFilterTab? tab = null);

        void LoadMore(int numberOfPackages);

        PackageSelectionObject CreatePackageSelectionObject(IPackage package, IPackageVersion version = null);
    }
}
