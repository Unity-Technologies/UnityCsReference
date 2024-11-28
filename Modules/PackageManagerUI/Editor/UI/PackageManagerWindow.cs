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
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    /// <summary>
    /// PackageManager Window helper class
    /// </summary>
    public static class Window
    {
        [MenuItem("Window/Package Management/Package Manager", priority = 1500)]
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
            PackageManagerWindow.OpenAndSelectPackage(packageToSelect);
        }
    }

    [EditorWindowTitle(title = "Package Manager", icon = "Package Manager")]
    internal class PackageManagerWindow : EditorWindow
    {
        internal static PackageManagerWindow instance { get; private set; }

        private PackageManagerWindowRoot m_Root;

        internal const string k_UpmUrl = "com.unity3d.kharma:upmpackage/";

        void OnEnable()
        {
            this.SetAntiAliasing(4);
            if (instance == null) instance = this;
            if (instance != this)
                return;

            titleContent = GetLocalizedTitleContent();

            minSize = new Vector2(748, 250);
            BuildGUI();

            Events.registeredPackages += OnRegisteredPackages;
        }

        private void BuildGUI()
        {
            var container = ServicesContainer.instance;
            var resourceLoader = container.Resolve<IResourceLoader>();
            var extensionManager = container.Resolve<IExtensionManager>();
            var selection = container.Resolve<ISelectionProxy>();
            var packageManagerPrefs = container.Resolve<IPackageManagerPrefs>();
            var packageDatabase = container.Resolve<IPackageDatabase>();
            var pageManager = container.Resolve<IPageManager>();
            var unityConnectProxy = container.Resolve<IUnityConnectProxy>();
            var applicationProxy = container.Resolve<IApplicationProxy>();
            var upmClient = container.Resolve<IUpmClient>();
            var assetStoreCachePathProxy = container.Resolve<IAssetStoreCachePathProxy>();
            var pageRefreshHandler = container.Resolve<IPageRefreshHandler>();
            var operationDispatcher = container.Resolve<IPackageOperationDispatcher>();
            var delayedSelectionHandler = container.Resolve<IDelayedSelectionHandler>();

            // Adding the ScrollView object here because it really need to be the first child under rootVisualElement for it to work properly.
            m_Root = new PackageManagerWindowRoot(resourceLoader, extensionManager, selection, packageManagerPrefs, packageDatabase, pageManager, unityConnectProxy, applicationProxy, upmClient, assetStoreCachePathProxy, pageRefreshHandler, operationDispatcher, delayedSelectionHandler);
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

            if (pageRefreshHandler.IsInitialFetchingDone(pageManager.activePage))
                OnFirstRefreshOperationFinish();
            else
                pageRefreshHandler.onRefreshOperationFinish += OnFirstRefreshOperationFinish;
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
            var pageRefreshHandler = container.Resolve<IPageRefreshHandler>();
            pageRefreshHandler.onRefreshOperationFinish -= OnFirstRefreshOperationFinish;
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

        internal Rect CalculateDropdownPosition(VisualElement anchorElement)
        {
            // If a background GUI painted before, the coordinates got could be different.
            // Repaint the package manager to ensure the coordinates retrieved is from Package Manager Window.
            RepaintImmediately();
            return GUIUtility.GUIToScreenRect(anchorElement.worldBound);
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
                SelectPageStatic(pageId: InProjectPage.k_Id);
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

                    SelectPackageStatic(id, MyAssetsPage.k_Id);
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
            instance.m_Root.OpenAddPackageByNameDropdown(url, instance);
        }

        [UsedByNativeCode]
        internal static void OpenAndSelectPackage(string packageToSelect, string pageId = null)
        {
            var isWindowAlreadyVisible = Resources.FindObjectsOfTypeAll<PackageManagerWindow>()?.FirstOrDefault() != null;

            SelectPackageStatic(packageToSelect, pageId);
            if (isWindowAlreadyVisible)
                return;

            string packageId = null;
            if (!string.IsNullOrEmpty(packageToSelect))
            {
                var packageDatabase = ServicesContainer.instance.Resolve<IPackageDatabase>();
                packageDatabase.GetPackageAndVersionByIdOrName(packageToSelect, out var package, out var version, true);
                packageId = version?.uniqueId ?? package?.versions.primary.uniqueId ?? $"{packageToSelect}@primary";
            }
            PackageManagerWindowAnalytics.SendEvent("openWindow", packageId);
        }

        internal static void OpenAndSelectPage(string pageId, string searchText = null)
        {
            var isWindowAlreadyVisible = Resources.FindObjectsOfTypeAll<PackageManagerWindow>()?.FirstOrDefault() != null;

            SelectPageStatic(pageId, searchText);
            if (!isWindowAlreadyVisible)
                PackageManagerWindowAnalytics.SendEvent("openWindowOnFilter", pageId);
        }

        [UsedByNativeCode("PackageManagerUI_OnPackageManagerResolve")]
        internal static void OnPackageManagerResolve()
        {
            var packageDatabase = ServicesContainer.instance.Resolve<IPackageDatabase>();
            packageDatabase?.ClearSamplesCache();

            var applicationProxy = ServicesContainer.instance.Resolve<IApplicationProxy>();
            if (applicationProxy.isBatchMode || !applicationProxy.isUpmRunning)
                return;

            var upmRegistryClient = ServicesContainer.instance.Resolve<IUpmRegistryClient>();
            upmRegistryClient.CheckRegistriesChanged();

            var upmClient = ServicesContainer.instance.Resolve<IUpmClient>();
            upmClient.List(true);
        }

        [InitializeOnLoadMethod]
        private static void EditorInitializedInSafeMode()
        {
            if (EditorUtility.isInSafeMode)
                OnEditorFinishLoadingProject();
        }

        [UsedByNativeCode]
        internal static void OnEditorFinishLoadingProject()
        {
            var servicesContainer = ServicesContainer.instance;
            var applicationProxy = servicesContainer.Resolve<IApplicationProxy>();
            if (!applicationProxy.isBatchMode && applicationProxy.isUpmRunning)
            {
                var upmClient = servicesContainer.Resolve<IUpmClient>();
                EntitlementsErrorAndDeprecationChecker.ManagePackageManagerEntitlementErrorAndDeprecation(upmClient);
                upmClient.List();
            }
        }

        private static void OnRegisteredPackages(PackageRegistrationEventArgs args)
        {
            var applicationProxy = ServicesContainer.instance.Resolve<IApplicationProxy>();
            if (applicationProxy.isBatchMode)
                return;

            var pageRefreshHandler = ServicesContainer.instance.Resolve<IPageRefreshHandler>();
            pageRefreshHandler.Refresh(RefreshOptions.UpmListOffline);
        }

        private static void SelectPackageStatic(string packageToSelect = null, string pageId = null)
        {
            // We want to make sure the window is shown first, otherwise our package and page selection
            // might get overriden by page selection in the window initialization code
            ShowWindow();

            // We use DelayedSelectionHandler to handle the case where the package is not yet available when the
            // selection is set. That could happen when we want to open Package Manager and select a package, but
            // the refresh call is not yet finished. It could also happen when we create a package and the newly
            // crated package is not yet in the database until after package resolution.
            ServicesContainer.instance.Resolve<IDelayedSelectionHandler>().SelectPackage(packageToSelect, pageId);
        }

        private static void SelectPageStatic(string pageId = null, string searchText = "")
        {
            // We want to make sure the window is shown first, otherwise our package and page selection
            // might get overriden by page selection in the window initialization code
            ShowWindow();

            ServicesContainer.instance.Resolve<IDelayedSelectionHandler>().SelectPage(pageId, searchText);
        }

        private static void ShowWindow()
        {
            instance = GetWindow<PackageManagerWindow>();
            instance.minSize = new Vector2(748, 250);
            instance.Show();
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
