// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEditor.UIElements;

namespace UnityEditor.PackageManager.UI
{
    /// <summary>
    /// PackageManager Window helper class
    /// </summary>
    public static class Window
    {
        [MenuItem("Window/Package Manager", priority = 1500)]
        internal static void ShowPackageManagerWindow(MenuCommand item)
        {
            Open(item.context?.name);
        }

        /// <summary>
        /// Open Package Manager Window and select specified package(if any).
        /// The string used to identify the package can be any of the following:
        /// <list type="bullet">
        /// <item><description>productId (e.g. 12345)</description></item>
        /// <item><description>packageName (e.g. com.unity.x)</description></item>
        /// <item><description>packageId (e.g. com.unity.x@1.0.0)</description></item>
        /// <item><description>displayName (e.g. 2D Common)</description></item>
        /// <item><description>null (no specific package to focus)</description></item>
        /// </list>
        /// </summary>
        /// <param name="packageToSelect">packageToSelect can be identified by packageName, displayName, packageId, productId or null</param>
        public static void Open(string packageToSelect)
        {
            PackageManagerWindow.OpenPackageManager(packageToSelect);
        }
    }

    [EditorWindowTitle(title = "Package Manager", icon = "Package Manager")]
    internal class PackageManagerWindow : EditorWindow
    {
        internal static PackageManagerWindow instance { get; private set; }

        private PackageManagerWindowRoot m_Root;

        void OnEnable()
        {
            this.SetAntiAliasing(4);
            if (instance == null) instance = this;
            if (instance != this)
                return;

            titleContent = GetLocalizedTitleContent();

            var container = ServicesContainer.instance;
            var resourceLoader = container.Resolve<ResourceLoader>();
            var selection = container.Resolve<SelectionProxy>();
            var packageFiltering = container.Resolve<PackageFiltering>();
            var packageManagerPrefs = container.Resolve<PackageManagerPrefs>();
            var packageDatabase = container.Resolve<PackageDatabase>();
            var pageManager = container.Resolve<PageManager>();
            var settingsProxy = container.Resolve<PackageManagerProjectSettingsProxy>();
            var unityConnectProxy = container.Resolve<UnityConnectProxy>();

            m_Root = new PackageManagerWindowRoot(resourceLoader, selection, packageFiltering, packageManagerPrefs, packageDatabase, pageManager, settingsProxy, unityConnectProxy);
            rootVisualElement.Add(m_Root);

            m_Root.OnEnable();
        }

        void OnDisable()
        {
            if (instance == null) instance = this;
            if (instance != this)
                return;

            m_Root?.OnDisable();
        }

        void OnDestroy()
        {
            m_Root?.OnDestroy();

            instance = null;
        }

        void OnFocus()
        {
            m_Root?.OnFocus();
        }

        void OnLostFocus()
        {
            m_Root?.OnLostFocus();
        }

        [UsedByNativeCode]
        internal static void OpenURL(string url)
        {
            if (string.IsNullOrEmpty(url))
                return;

            var startIndex = url.LastIndexOf('/');
            if (startIndex > 0)
            {
                var id = url.Substring(startIndex + 1);
                var endIndex = id.IndexOf('?');
                if (endIndex > 0)
                    id = id.Substring(0, endIndex);
                SelectPackageAndFilterStatic(id, PackageFilterTab.AssetStore);
            }
        }

        [UsedByNativeCode]
        internal static void OpenPackageManager(string packageToSelect)
        {
            var isWindowAlreadyVisible = Resources.FindObjectsOfTypeAll<PackageManagerWindow>()?.FirstOrDefault() != null;

            SelectPackageAndFilterStatic(packageToSelect);

            if (!isWindowAlreadyVisible)
            {
                string packageId = null;
                if (!string.IsNullOrEmpty(packageToSelect))
                {
                    var packageDatabase = ServicesContainer.instance.Resolve<PackageDatabase>();
                    IPackageVersion version;
                    IPackage package;
                    packageDatabase.GetPackageAndVersionByIdOrName(packageToSelect, out package, out version);

                    packageId = version?.uniqueId ?? package?.versions.primary.uniqueId ?? string.Format("{0}@primary", packageToSelect);
                }
                PackageManagerWindowAnalytics.SendEvent("openWindow", packageId);
            }
        }

        [UsedByNativeCode]
        internal static void OnPackageManagerResolve()
        {
            // we don't want to refresh at all if window refresh hasn't happened yet
            var pageManager = ServicesContainer.instance.Resolve<PageManager>();
            if (pageManager.GetRefreshTimestamp(RefreshOptions.UpmList | RefreshOptions.UpmListOffline) == 0)
                return;
            var upmCache = ServicesContainer.instance.Resolve<UpmCache>();
            upmCache.SetInstalledPackageInfos(PackageInfo.GetAll());
        }

        internal static void SelectPackageAndFilterStatic(string packageToSelect, PackageFilterTab? filterTab = null, bool refresh = false, string searchText = "")
        {
            instance = GetWindow<PackageManagerWindow>(typeof(SceneView));
            instance.minSize = new Vector2(800, 250);
            instance.m_Root.SelectPackageAndFilter(packageToSelect, filterTab, refresh, searchText);
            instance.Show();
        }

        internal static void CloseAll()
        {
            var windows = Resources.FindObjectsOfTypeAll<PackageManagerWindow>();
            if (windows == null)
                return;

            foreach (var window in windows)
                window.Close();
        }

        [MenuItem("internal:Packages/Reset Package Database")]
        public static void ResetPackageDatabase()
        {
            var pageManager = ServicesContainer.instance.Resolve<PageManager>();

            CloseAll();
            pageManager.Reload();
        }
    }
}
