// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Utils;
using UnityEditorInternal;
using UnityEngine.UIElements;
using CachePathConfig = UnityEditorInternal.AssetStoreCachePathManager.CachePathConfig;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageManagerUserSettingsProvider : SettingsProvider
    {
        private const string k_UserSettingsStylesheet = "StyleSheets/PackageManager/PackageManagerUserSettings.uss";
        private const string k_CommonStylesheet = "StyleSheets/Extensions/base/common.uss";
        private const string k_DarkStylesheet = "StyleSheets/Extensions/base/dark.uss";
        private const string k_LightStylesheet = "StyleSheets/Extensions/base/light.uss";
        internal const string k_PackageManagerUserSettingsPath = "Preferences/Package Manager";
        private const string k_PackageManagerUserSettingsTemplate = "PackageManagerUserSettings.uxml";
        private const string k_AssetStoreFolder = "Asset Store-5.x";

        private static readonly string k_OpenFolder = L10n.Tr("Open Containing Folder");
        private static readonly string k_ChangeLocation = L10n.Tr("Change Location");
        private static readonly string k_ResetToDefaultLocation = L10n.Tr("Reset to Default Location");

        private VisualElement m_RootVisualElement;

        [NonSerialized]
        private CachePathConfig m_CurrentAssetStoreConfig = new CachePathConfig();
        private string currentAssetStoreNormalizedPath => m_CurrentAssetStoreConfig?.path.NormalizePath() ?? string.Empty;

        [NonSerialized]
        private CacheRootConfig m_CurrentPackagesConfig = new CacheRootConfig();
        private string currentPackagesNormalizedPath => m_CurrentPackagesConfig?.path.NormalizePath() ?? string.Empty;

        private IResourceLoader m_ResourceLoader;
        private IAssetStoreCachePathProxy m_AssetStoreCachePathProxy;
        private IUpmCacheRootClient m_UpmCacheRootClient;
        private IApplicationProxy m_ApplicationProxy;
        private IClientProxy m_ClientProxy;
        private IAssetStoreDownloadManager m_AssetStoreDownloadManager;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<IResourceLoader>();
            m_AssetStoreCachePathProxy = container.Resolve<IAssetStoreCachePathProxy>();
            m_UpmCacheRootClient = container.Resolve<IUpmCacheRootClient>();
            m_ApplicationProxy = container.Resolve<IApplicationProxy>();
            m_AssetStoreDownloadManager = container.Resolve<IAssetStoreDownloadManager>();
            m_ClientProxy = container.Resolve<IClientProxy>();
        }

        private PackageManagerUserSettingsProvider(string path, IEnumerable<string> keywords = null) : base(path, SettingsScope.User, keywords)
        {
            activateHandler = (text, element) =>
            {
                ResolveDependencies();

                // Create a child to make sure all the style sheets are not added to the root.
                m_RootVisualElement = new ScrollView();
                m_RootVisualElement.StretchToParentSize();
                m_RootVisualElement.AddStyleSheetPath(k_UserSettingsStylesheet);
                m_RootVisualElement.AddStyleSheetPath(EditorGUIUtility.isProSkin ? k_DarkStylesheet : k_LightStylesheet);
                m_RootVisualElement.AddStyleSheetPath(k_CommonStylesheet);
                m_RootVisualElement.styleSheets.Add(m_ResourceLoader.packageManagerCommonStyleSheet);

                element.Add(m_RootVisualElement);

                var root = m_ResourceLoader.GetTemplate(k_PackageManagerUserSettingsTemplate);
                m_RootVisualElement.Add(root);
                m_Cache = new VisualElementCache(root);

                DisplayPackagesCacheSetting();
                DisplayAssetStoreCachePathSetting();

                EditorApplication.focusChanged += OnEditorApplicationFocusChanged;
            };
            deactivateHandler = () =>
            {
                EditorApplication.focusChanged -= OnEditorApplicationFocusChanged;

                if (m_UpmCacheRootClient != null)
                {
                    m_UpmCacheRootClient.onGetCacheRootOperationError -= OnPackagesGetCacheRootOperationError;
                    m_UpmCacheRootClient.onGetCacheRootOperationResult -= OnPackagesGetCacheRootOperationResult;
                    m_UpmCacheRootClient.onSetCacheRootOperationError -= OnPackagesSetCacheRootOperationError;
                    m_UpmCacheRootClient.onSetCacheRootOperationResult -= OnPackagesSetCacheRootOperationResult;
                    m_UpmCacheRootClient.onClearCacheRootOperationError -= OnPackagesClearCacheRootOperationError;
                    m_UpmCacheRootClient.onClearCacheRootOperationResult -= OnPackagesClearCacheRootOperationResult;
                }

                if (m_AssetStoreCachePathProxy != null)
                {
                    m_AssetStoreCachePathProxy.onConfigChanged -= RefreshAssetStoreCachePathConfig;
                }
            };
        }

        private void OnEditorApplicationFocusChanged(bool hasFocus)
        {
            if (hasFocus)
            {
                if (!m_ApplicationProxy.isBatchMode && m_ApplicationProxy.isUpmRunning)
                    m_UpmCacheRootClient.GetCacheRoot();
                GetAssetStoreCacheConfig();
            }
        }

        private void DisplayPackagesCacheSetting()
        {
            packagesCacheDropdown.SetIcon(Icon.Folder);
            var packagesCacheDropdownMenu = new DropdownMenu();
            packagesCacheDropdownMenu.AppendAction(k_OpenFolder, action =>
            {
                if (!string.IsNullOrWhiteSpace(currentPackagesNormalizedPath))
                    m_ApplicationProxy.RevealInFinder(currentPackagesNormalizedPath);
            }, action => DropdownMenuAction.Status.Normal, "openLocation");
            packagesCacheDropdownMenu.AppendAction(k_ChangeLocation, action =>
            {
                var path = m_ApplicationProxy.OpenFolderPanel(L10n.Tr("Select Packages Cache Location"), currentPackagesNormalizedPath);
                path = path.NormalizePath();
                if (!string.IsNullOrWhiteSpace(path) && string.CompareOrdinal(path, currentPackagesNormalizedPath) != 0)
                    m_UpmCacheRootClient.SetCacheRoot(path);
            }, action => m_CurrentPackagesConfig.source != ConfigSource.Environment ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled, "selectLocation");
            packagesCacheDropdownMenu.AppendAction(k_ResetToDefaultLocation, action =>
            {
                m_UpmCacheRootClient.ClearCacheRoot();
            }, action => m_CurrentPackagesConfig.source == ConfigSource.User ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled, "resetLocation");
            packagesCacheDropdown.menu = packagesCacheDropdownMenu;

            m_UpmCacheRootClient.onGetCacheRootOperationError += OnPackagesGetCacheRootOperationError;
            m_UpmCacheRootClient.onGetCacheRootOperationResult += OnPackagesGetCacheRootOperationResult;
            m_UpmCacheRootClient.onSetCacheRootOperationError += OnPackagesSetCacheRootOperationError;
            m_UpmCacheRootClient.onSetCacheRootOperationResult += OnPackagesSetCacheRootOperationResult;
            m_UpmCacheRootClient.onClearCacheRootOperationError += OnPackagesClearCacheRootOperationError;
            m_UpmCacheRootClient.onClearCacheRootOperationResult += OnPackagesClearCacheRootOperationResult;

            if (!m_ApplicationProxy.isBatchMode && m_ApplicationProxy.isUpmRunning)
            {
                packagesCachePath.text = string.Empty;
                packagesCacheDropdown.SetEnabled(false);

                UIUtils.SetElementDisplay(packagesCacheErrorBox, false);

                m_UpmCacheRootClient.GetCacheRoot();
            }
            else
            {
                packagesCachePath.text = string.Empty;
                packagesCacheDropdown.SetEnabled(false);

                DisplayPackagesCacheErrorBox(HelpBoxMessageType.Error, L10n.Tr("Cannot get the Packages Cache location, UPM server is not running."));
            }
        }

        private void DisplayAssetStoreCachePathSetting()
        {
            assetsCacheLocationInfo.enableRichText = true;
            assetsCacheLocationInfo.text = string.Format(L10n.Tr("Your assets will be stored in <b>{0}</b> subfolder."), k_AssetStoreFolder);
            assetsCacheDropdown.SetIcon(Icon.Folder);
            var assetsCacheDropdownMenu = new DropdownMenu();
            assetsCacheDropdownMenu.AppendAction(k_OpenFolder, action =>
            {
                if (!string.IsNullOrWhiteSpace(currentAssetStoreNormalizedPath))
                    m_ApplicationProxy.RevealInFinder(Paths.Combine(currentAssetStoreNormalizedPath, k_AssetStoreFolder));
            }, action => m_CurrentAssetStoreConfig.status == AssetStoreCachePathManager.ConfigStatus.InvalidPath ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal, "openLocation");
            assetsCacheDropdownMenu.AppendAction(k_ChangeLocation, action =>
            {
                var path = m_ApplicationProxy.OpenFolderPanel(L10n.Tr("Select Assets Cache Location"), Paths.Combine(currentAssetStoreNormalizedPath, k_AssetStoreFolder));
                if (!string.IsNullOrWhiteSpace(path))
                {
                    path = path.NormalizePath();
                    if (path.EndsWith(Path.DirectorySeparatorChar + k_AssetStoreFolder))
                        path = path.Substring(0, path.Length - k_AssetStoreFolder.Length - 1);

                    if (string.CompareOrdinal(path, currentAssetStoreNormalizedPath) == 0)
                        return;

                    if (!CancelDownloadInProgress())
                        return;

                    var oldStatus = m_CurrentAssetStoreConfig?.status.ToString() ?? string.Empty;
                    var oldSource = m_CurrentAssetStoreConfig?.source.ToString() ?? string.Empty;
                    var status = m_AssetStoreCachePathProxy.SetConfig(path);

                    // Send analytics
                    PackageCacheManagementAnalytics.SendAssetStoreEvent("changePath",
                        new []{ oldSource, oldStatus },
                        new []{ m_CurrentAssetStoreConfig.source.ToString(), m_CurrentAssetStoreConfig.status.ToString() });

                    if (status == AssetStoreCachePathManager.ConfigStatus.Failed)
                        DisplayAssetsCacheErrorBox(HelpBoxMessageType.Error, L10n.Tr($"Cannot set the Assets Cache location, \"{path}\" is invalid or inaccessible."));
                }
            }, action => m_CurrentAssetStoreConfig.source != ConfigSource.Environment ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled, "selectLocation");
            assetsCacheDropdownMenu.AppendAction(k_ResetToDefaultLocation, action =>
            {
                if (!CancelDownloadInProgress())
                    return;

                var oldStatus = m_CurrentAssetStoreConfig?.status.ToString() ?? string.Empty;
                var oldSource = m_CurrentAssetStoreConfig?.source.ToString() ?? string.Empty;
                var status = m_AssetStoreCachePathProxy.ResetConfig();

                // Send analytics
                PackageCacheManagementAnalytics.SendAssetStoreEvent("resetPath",
                    new []{ oldSource, oldStatus },
                    new []{ m_CurrentAssetStoreConfig.source.ToString(), m_CurrentAssetStoreConfig.status.ToString()});

                if (status == AssetStoreCachePathManager.ConfigStatus.Failed)
                    DisplayAssetsCacheErrorBox(HelpBoxMessageType.Error, L10n.Tr("Cannot reset the Assets Cache location to default."));
            }, action => m_CurrentAssetStoreConfig.source == ConfigSource.User ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled, "resetLocation");
            assetsCacheDropdown.menu = assetsCacheDropdownMenu;

            GetAssetStoreCacheConfig();
            m_AssetStoreCachePathProxy.onConfigChanged += RefreshAssetStoreCachePathConfig;
        }

        private bool CancelDownloadInProgress()
        {
            if (!m_AssetStoreDownloadManager.IsAnyDownloadInProgressOrPause())
                return true;

            if (m_ApplicationProxy.isBatchMode || !m_ApplicationProxy.DisplayDialog("abortDownloadBeforeChangeAssetsCacheLocation",
                L10n.Tr("Changing Assets Cache location"),
                L10n.Tr("Changing the Assets Cache location will cancel all downloads in progress."),
                L10n.Tr("Continue"), L10n.Tr("Cancel")))
                return false;

            m_AssetStoreDownloadManager.AbortAllDownloads();
            return true;
        }

        private void GetAssetStoreCacheConfig()
        {
            RefreshAssetStoreCachePathConfig(m_AssetStoreCachePathProxy.GetConfig());
        }

        private void RefreshAssetStoreCachePathConfig(CachePathConfig config)
        {
            m_CurrentAssetStoreConfig = config;
            assetsCachePath.text = currentAssetStoreNormalizedPath;
            UIUtils.SetElementDisplay(assetsCacheErrorBox, false);

            if (m_CurrentAssetStoreConfig.source == ConfigSource.Environment)
            {
                if (m_CurrentAssetStoreConfig.status == AssetStoreCachePathManager.ConfigStatus.ReadOnly)
                {
                    DisplayAssetsCacheErrorBox(HelpBoxMessageType.Warning, L10n.Tr("The Assets Cache location set by environment variable ASSETSTORE_CACHE_PATH is read only. Download or update of assets won't be possible."));
                }
                else if (m_CurrentAssetStoreConfig.status == AssetStoreCachePathManager.ConfigStatus.InvalidPath)
                {
                    assetsCacheDropdown.SetEnabled(false);
                    DisplayAssetsCacheErrorBox(HelpBoxMessageType.Error, L10n.Tr("The Assets Cache location set by environment variable ASSETSTORE_CACHE_PATH is invalid or inaccessible."));
                }
                else
                    DisplayAssetsCacheErrorBox(HelpBoxMessageType.Info, L10n.Tr("The Assets Cache location is set by environment variable ASSETSTORE_CACHE_PATH, you cannot change it."));
            }
            else
            {
                if (m_CurrentAssetStoreConfig.status == AssetStoreCachePathManager.ConfigStatus.ReadOnly)
                    DisplayAssetsCacheErrorBox(HelpBoxMessageType.Warning, L10n.Tr("The Assets Cache location is read only. Download or update of assets won't be possible."));
                else if (m_CurrentAssetStoreConfig.status != AssetStoreCachePathManager.ConfigStatus.Success)
                    DisplayAssetsCacheErrorBox(HelpBoxMessageType.Error, L10n.Tr("The Assets Cache location is invalid or inaccessible. Change location or reset it to default location."));
            }
        }

        private void DisplayAssetsCacheErrorBox(HelpBoxMessageType type, string message)
        {
            assetsCacheErrorBox.messageType = type;
            assetsCacheErrorBox.text = message;
            UIUtils.SetElementDisplay(assetsCacheErrorBox, true);
        }

        private void OnPackagesGetCacheRootOperationResult(CacheRootConfig config)
        {
            RefreshCurrentPackagesConfig(config);
            if (m_CurrentPackagesConfig.source == ConfigSource.Environment)
                DisplayPackagesCacheErrorBox(HelpBoxMessageType.Info, L10n.Tr("The Packages Cache location is set by environment variable UPM_CACHE_ROOT, you cannot change it."));
        }

        private void OnPackagesSetCacheRootOperationResult(CacheRootConfig config)
        {
            // Send analytics
            PackageCacheManagementAnalytics.SendUpmEvent("changePath",
                new []{ m_CurrentPackagesConfig.source.ToString() },
                new []{ config.source.ToString()});

            RefreshCurrentPackagesConfig(config);
            m_ClientProxy.Resolve();
        }

        private void OnPackagesClearCacheRootOperationResult(CacheRootConfig config)
        {
            // Send analytics
            PackageCacheManagementAnalytics.SendUpmEvent("resetPath",
                new []{ m_CurrentPackagesConfig.source.ToString() },
                new []{ config.source.ToString()});

            RefreshCurrentPackagesConfig(config);
            m_ClientProxy.Resolve();
        }

        private void RefreshCurrentPackagesConfig(CacheRootConfig config)
        {
            m_CurrentPackagesConfig = config;
            packagesCachePath.text = currentPackagesNormalizedPath;
            packagesCacheDropdown.SetEnabled(true);
            UIUtils.SetElementDisplay(packagesCacheErrorBox, false);
        }

        private void OnPackagesGetCacheRootOperationError(UIError error)
        {
            packagesCacheDropdown.SetEnabled(false);
            DisplayPackagesCacheErrorBox(HelpBoxMessageType.Error, string.Format(L10n.Tr("Cannot get the Packages Cache location, reason: {0}."), error.message));
        }

        private void OnPackagesSetCacheRootOperationError(UIError error, string path)
        {
            DisplayPackagesCacheErrorBox(HelpBoxMessageType.Error, string.Format(L10n.Tr("Cannot set the Packages Cache location to '{0}', reason: {1}."), path, error.message));
        }

        private void OnPackagesClearCacheRootOperationError(UIError error)
        {
            DisplayPackagesCacheErrorBox(HelpBoxMessageType.Error, string.Format(L10n.Tr("Cannot reset the Packages Cache location to default, reason: {0}."), error.message));
        }

        private void DisplayPackagesCacheErrorBox(HelpBoxMessageType type, string message)
        {
            packagesCacheErrorBox.messageType = type;
            packagesCacheErrorBox.text = message;
            UIUtils.SetElementDisplay(packagesCacheErrorBox, true);
        }

        [SettingsProvider]
        public static SettingsProvider CreateUserSettingsProvider()
        {
            return new PackageManagerUserSettingsProvider(k_PackageManagerUserSettingsPath, new List<string>
            {
                L10n.Tr("cache"),
                L10n.Tr("assetstore"),
                L10n.Tr("packages"),
                L10n.Tr("UPM_CACHE_ROOT"),
                L10n.Tr("ASSETSTORE_CACHE_PATH")
            });
        }

        private VisualElementCache m_Cache;

        private HelpBox packagesCacheErrorBox => m_Cache.Get<HelpBox>("packagesCacheErrorBox");
        private HelpBox assetsCacheErrorBox => m_Cache.Get<HelpBox>("assetsCacheErrorBox");
        private SelectableLabel packagesCachePath => m_Cache.Get<SelectableLabel>("packagesCachePath");
        private SelectableLabel assetsCachePath => m_Cache.Get<SelectableLabel>("assetsCachePath");
        private DropdownButton packagesCacheDropdown => m_Cache.Get<DropdownButton>("packagesCacheDropdown");
        private DropdownButton assetsCacheDropdown => m_Cache.Get<DropdownButton>("assetsCacheDropdown");
        private Label assetsCacheLocationInfo => m_Cache.Get<Label>("assetsCacheLocationInfo");
    }
}
