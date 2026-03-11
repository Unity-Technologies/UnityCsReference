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

            minSize = new Vector2(280, 250);
            BuildGUI();

            Events.registeredPackages += OnRegisteredPackages;
        }

        private void BuildGUI()
        {
            var container = ServicesContainer.instance;
            // Adding the ScrollView object here because it really needs to be the first child under rootVisualElement for it to work properly.
            m_Root = new PackageManagerWindowRoot(
                container.Resolve<IResourceLoader>(),
                container.Resolve<IExtensionManager>(),
                container.Resolve<ISelectionProxy>(),
                container.Resolve<IPackageManagerPrefs>(),
                container.Resolve<IPackageDatabase>(),
                container.Resolve<IPageManager>(),
                container.Resolve<IUnityConnectProxy>(),
                container.Resolve<IApplicationProxy>(),
                container.Resolve<IUpmClient>(),
                container.Resolve<IAssetStoreCachePathProxy>(),
                container.Resolve<IPageRefreshHandler>(),
                container.Resolve<IDelayedSelectionHandler>(),
                container.Resolve<IDropdownHandler>());
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

        void OnDisable()
        {
            instance ??= this;
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

        // The internal modifier is used (instead of private) to give our test project access to these properties/methods
        internal static bool TryExtractUpmPackageInfoFromUrl(string url, out string technicalName, out string version)
        {
            technicalName = string.Empty;
            version = string.Empty;

            if (url.StartsWith(k_UpmUrl))
            {
                var index = url.IndexOf('@', k_UpmUrl.Length);
                if (index < 0)
                    technicalName = url.Substring(k_UpmUrl.Length);
                else
                {
                    technicalName = url.Substring(k_UpmUrl.Length, index - k_UpmUrl.Length);
                    version = url.Substring(index + 1);
                }
                return true;
            }
            return false;
        }

        [UsedByNativeCode]
        internal static void OpenURL(string url)
        {
            if (string.IsNullOrEmpty(url))
                return;

            // com.unity3d.kharma:content/11111                       => AssetStore url
            // com.unity3d.kharma:upmpackage/com.unity.xxx@1.2.2      => Upm url
            if (TryExtractUpmPackageInfoFromUrl(url, out var technicalName, out var version))
            {
                var packageDatabase = ServicesContainer.instance.Resolve<IPackageDatabase>();
                var package = packageDatabase.GetPackageByIdOrName(technicalName);
                if (package != null)
                    SelectPackageStatic(technicalName);
                else
                    SelectPageStatic(pageId: InProjectPage.k_Id);

                if (!string.IsNullOrEmpty(version) || package == null)
                    EditorApplication.delayCall += () => OpenAddPackageByName(technicalName, version);
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

        private static void OpenAddPackageByName(string technicalName, string version)
        {
            ShowWindow();
            instance.Focus();
            instance.m_Root.OpenAddPackageByNameDropdown(technicalName, version);
        }

        [UsedByNativeCode]
        internal static void OpenCreatePackageDropdown()
        {
            ShowWindow();
            instance.Focus();
            instance.m_Root.OpenCreatePackageDropdown();
        }

        [UsedByNativeCode]
        internal static void OpenAndSelectPackage(string packageToSelect, string pageId = null)
        {
            var isWindowAlreadyVisible = Resources.FindObjectsOfTypeAll<PackageManagerWindow>()?.FirstOrDefault() != null;

            SelectPackageStatic(packageToSelect, pageId);
            if (!isWindowAlreadyVisible)
                PackageManagerWindowAnalytics.SendEvent("openWindow", packageToSelect);
        }

        [UsedByNativeCode]
        internal static void OpenExportPackageWindow(string packageName)
        {
            var packageDatabase = ServicesContainer.instance.Resolve<IPackageDatabase>();
            var modalManager = ServicesContainer.instance.Resolve<IModalManager>();
            var package = packageDatabase.GetPackageByIdOrName(packageName);

            if (package == null)
            {
                Debug.LogError(L10n.Tr($"[Package Manager Window] Unable to open the Export window. Try opening the Package Manager Window first and exporting from there."));
                return;
            }


            // There is a flickering effect on the project browser if we don't repaint it before showing the modal.
            // https://jira.unity3d.com/browse/UUM-113810
            Resources.FindObjectsOfTypeAll<ProjectBrowser>().FirstOrDefault()?.RepaintImmediately();
            var version = package.versions.installed;
            modalManager.ShowExportModal(version);
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
            ServicesContainer.instance.Resolve<IInProjectPackagesMonitor>().OnPackageManagerResolve();
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
            ServicesContainer.instance.Resolve<IInProjectPackagesMonitor>().OnEditorFinishLoadingProject();
        }

        private static void OnRegisteredPackages(PackageRegistrationEventArgs args)
        {
            ServicesContainer.instance.Resolve<IInProjectPackagesMonitor>().OnRegisteredPackages(args);
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
            instance.minSize = new Vector2(280, 250);
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
