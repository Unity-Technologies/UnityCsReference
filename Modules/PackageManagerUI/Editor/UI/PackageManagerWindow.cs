// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEditor.UIElements;

using UnityEditor.PackageManager.UI.Internal;

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

        /// <summary>
        /// Open Package Manager Window and select specified filter.
        /// The string used to identify the filter can be any of the following:
        /// <list type="bullet">
        /// <item><description>filterTab (e.g. "InProject")</description></item>
        /// <item><description>filterTab/subPage (e.g. "InProject/Services")</description></item>
        /// <item><description>null (no specific filterTab to focus)</description></item>
        /// </list>
        /// </summary>
        /// <param name="filterAndSubPageToSelect">Filter tab and subpage (optional) to select. If filter tab cannot be found, last select tab will be selected, if subpage cannot be found, first subpage will be selected</param>
        internal static void OpenFilter(string filterAndSubPageToSelect)
        {
            PackageManagerWindow.OpenPackageManagerOnFilter(filterAndSubPageToSelect);
        }
    }

    [EditorWindowTitle(title = "Package Manager", icon = "Package Manager")]
    internal class PackageManagerWindow : EditorWindow
    {
        internal static PackageManagerWindow instance { get; private set; }

        private PackageManagerWindowRoot m_Root;

        internal const string k_UpmUrl = "com.unity3d.kharma:upmpackage/";

        // This event is currently only used by integration tests to know when the package manager window is ready
        public static event Action onPackageManagerReady = delegate { };

        void OnEnable()
        {
            this.SetAntiAliasing(4);
            if (instance == null) instance = this;
            if (instance != this)
                return;

            titleContent = GetLocalizedTitleContent();

            BuildGUI();

            Events.registeredPackages += OnRegisteredPackages;
        }

        private void BuildGUI()
        {
            var container = ServicesContainer.instance;
            var resourceLoader = container.Resolve<ResourceLoader>();
            var extensionManager = container.Resolve<ExtensionManager>();
            var selection = container.Resolve<SelectionProxy>();
            var packageFiltering = container.Resolve<PackageFiltering>();
            var packageManagerPrefs = container.Resolve<PackageManagerPrefs>();
            var packageDatabase = container.Resolve<PackageDatabase>();
            var pageManager = container.Resolve<PageManager>();
            var settingsProxy = container.Resolve<PackageManagerProjectSettingsProxy>();
            var unityConnectProxy = container.Resolve<UnityConnectProxy>();
            var applicationProxy = container.Resolve<ApplicationProxy>();
            var upmClient = container.Resolve<UpmClient>();
            var assetStoreCachePathProxy = container.Resolve<AssetStoreCachePathProxy>();

            m_Root = new PackageManagerWindowRoot(resourceLoader, extensionManager, selection, packageFiltering, packageManagerPrefs, packageDatabase, pageManager, settingsProxy, unityConnectProxy, applicationProxy, upmClient, assetStoreCachePathProxy);
            try
            {
                m_Root.OnEnable();
                rootVisualElement.Add(m_Root);
            }
            catch (ResourceLoaderException)
            {
                // Do nothing, defer it to CreateGUI
            }
            catch (TargetInvocationException e)
            {
                CheckInnerException<ResourceLoaderException>(e);
            }

            if (pageManager.IsInitialFetchingDone())
                OnFirstRefreshOperationFinish();
            else
                pageManager.onRefreshOperationFinish += OnFirstRefreshOperationFinish;
        }

        void CreateGUI()
        {
            if (m_Root == null)
                return;

            if (!rootVisualElement.Contains(m_Root))
            {
                try
                {
                    m_Root.OnEnable();
                    rootVisualElement.Add(m_Root);
                }
                catch (ResourceLoaderException)
                {
                    Debug.LogError(L10n.Tr("[Package Manager Window] Unable to load resource, window can't be displayed.)"));
                    return;
                }
                catch (TargetInvocationException e)
                {
                    CheckInnerException<ResourceLoaderException>(e);
                    Debug.LogError(L10n.Tr("[Package Manager Window] Unable to load resource, window can't be displayed.)"));
                    return;
                }
            }

            m_Root.OnCreateGUI();
        }

        private void OnFirstRefreshOperationFinish()
        {
            var container = ServicesContainer.instance;
            var pageManager = container.Resolve<PageManager>();
            pageManager.onRefreshOperationFinish -= OnFirstRefreshOperationFinish;
            onPackageManagerReady?.Invoke();
        }

        void OnDisable()
        {
            if (instance == null) instance = this;
            if (instance != this)
                return;

            m_Root?.OnDisable();

            Events.registeredPackages -= OnRegisteredPackages;
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

            // com.unity3d.kharma:content/11111                       => AssetStore url
            // com.unity3d.kharma:upmpackage/com.unity.xxx@1.2.2      => Upm url
            if (url.StartsWith(k_UpmUrl))
            {
                SelectPackageAndFilterStatic(string.Empty, PackageFilterTab.InProject);
                EditorApplication.delayCall += () => OpenAddPackageByName(url);
            }
            else
            {
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
        }

        private static void OpenAddPackageByName(string url)
        {
            if (float.IsNaN(instance.position.x) || float.IsNaN(instance.position.y))
            {
                EditorApplication.delayCall += () => OpenAddPackageByName(url);
                return;
            }
            instance.Focus();
            instance.m_Root.OpenAddPackageByNameDropdown(url);
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
                    var packageDatabase = ServicesContainer.instance.Resolve<Internal.PackageDatabase>();
                    Internal.IPackageVersion version;
                    Internal.IPackage package;
                    packageDatabase.GetPackageAndVersionByIdOrName(packageToSelect, out package, out version);

                    packageId = version?.uniqueId ?? package?.versions.primary.uniqueId ?? string.Format("{0}@primary", packageToSelect);
                }
                PackageManagerWindowAnalytics.SendEvent("openWindow", packageId);
            }
        }

        internal static void OpenPackageManagerOnFilter(string filterAndSubPageToSelect)
        {
            var isWindowAlreadyVisible = Resources.FindObjectsOfTypeAll<PackageManagerWindow>()?.FirstOrDefault() != null;

            SelectFilterSubPageStatic(filterAndSubPageToSelect);
            if (!isWindowAlreadyVisible)
                PackageManagerWindowAnalytics.SendEvent("openWindowOnFilter", filterAndSubPageToSelect);
        }

        [UsedByNativeCode("PackageManagerUI_OnPackageManagerResolve")]
        internal static void OnPackageManagerResolve()
        {
            var packageDatabase = ServicesContainer.instance.Resolve<Internal.PackageDatabase>();
            packageDatabase?.ClearSamplesCache();

            var applicationProxy = ServicesContainer.instance.Resolve<ApplicationProxy>();
            if (applicationProxy.isBatchMode)
                return;

            var upmRegistryClient = ServicesContainer.instance.Resolve<UpmRegistryClient>();
            upmRegistryClient.CheckRegistriesChanged();

            var upmClient = ServicesContainer.instance.Resolve<UpmClient>();
            upmClient.List(true);
        }

        [UsedByNativeCode]
        internal static void OnEditorFinishLoadingProject()
        {
            var servicesContainer = ServicesContainer.instance;
            var applicationProxy = servicesContainer.Resolve<ApplicationProxy>();
            if (!applicationProxy.isBatchMode && applicationProxy.isUpmRunning)
            {
                var upmClient = servicesContainer.Resolve<UpmClient>();
                EntitlementsErrorChecker.ManagePackageManagerEntitlementError(upmClient);
                upmClient.List();
            }
        }

        private static void OnRegisteredPackages(PackageRegistrationEventArgs args)
        {
            var applicationProxy = ServicesContainer.instance.Resolve<ApplicationProxy>();
            if (applicationProxy.isBatchMode)
                return;

            var pageManager = ServicesContainer.instance.Resolve<PageManager>();
            pageManager.Refresh(RefreshOptions.UpmListOffline);
        }

        internal static void SelectFilterSubPageStatic(string filterTabOrSubPage = "")
        {
            instance = GetWindow<PackageManagerWindow>();
            instance.minSize = new Vector2(800, 250);
            instance.m_Root.SelectFilterSubPage(filterTabOrSubPage);
            instance.Show();
        }

        internal static void SelectPackageAndFilterStatic(string packageToSelect, PackageFilterTab? filterTab = null, bool refresh = false, string searchText = "")
        {
            instance = GetWindow<PackageManagerWindow>();
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

            instance = null;
        }

        private static void CheckInnerException<T>(TargetInvocationException e) where T : Exception
        {
            var originalException = e;
            while (e.InnerException is TargetInvocationException)
                e = e.InnerException as TargetInvocationException;
            if (!(e.InnerException is T))
                throw originalException;
        }
    }
}
