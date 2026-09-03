// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: Packman not yet converted
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal;

internal interface IDropdownHandler : IService
{
    void ShowAddPackageByNameDropdown(VisualElement anchorElement, string packageName = null, string packageVersion = null);
    void ShowCreatePackageDropdown(VisualElement anchorElement);
    void ShowGenericInputDropdown(VisualElement anchorElement, InputDropdownArgs args);

    void ShowInProgressDropdown(VisualElement anchorElement);
}

internal partial class DropdownHandler : BaseService<IDropdownHandler>, IDropdownHandler
{
    internal partial class DropdownWindow : EditorWindow
    {
        #pragma warning disable UAL0015 // this side effect does not outlive the current call (global trigger / lazily-loaded asset re-fetched on next access); a stale reference is harmlessly replaced
        internal DropdownWindow() {}
        #pragma warning restore UAL0015

        [AutoStaticsCleanupOnCodeReload]
        private static List<DropdownWindow> s_OpenedWindows = new List<DropdownWindow>();

        private DropdownContent m_Content;

        private void OnEnable()
        {
            s_OpenedWindows.Add(this);
            hideFlags = HideFlags.DontSave;
        }

        private void OnDisable()
        {
            m_Content?.OnDropdownClosed();
            m_Content = null;
            s_OpenedWindows.Remove(this);
        }

        public static void ShowDropdown(VisualElement anchorElement, DropdownContent content)
        {
            if (anchorElement == null || content == null)
                return;

            if (float.IsNaN(anchorElement.rect.x) || float.IsNaN(anchorElement.rect.y) || float.IsNaN(anchorElement.rect.width) || float.IsNaN(anchorElement.rect.height))
            {
                EditorApplication.delayCall += () => ShowDropdown(anchorElement, content);
                return;
            }

            if (s_OpenedWindows.Count > 0)
                foreach (var window in s_OpenedWindows.ToArray())
                    window.Close();

            var instance = CreateInstance<DropdownWindow>();
            instance.rootVisualElement.Add(content);

            content.container = instance;
            instance.m_Content = content;

            var position = EditorMenuExtensions.GUIToScreenRect(anchorElement, anchorElement.worldBound);
            instance.ShowAsDropDown(position, content.windowSize);
            content.OnDropdownShown();
        }
    }

    private readonly IResourceLoader m_ResourceLoader;
    private readonly IUpmClient m_UpmClient;
    private readonly IAssetStoreDownloadManager m_AssetStoreDownloadManager;
    private readonly IPackageDatabase m_PackageDatabase;
    private readonly IPageManager m_PageManager;
    private readonly IPackageOperationDispatcher m_OperationDispatcher;
    private readonly ICustomDisplayDialog m_CustomDisplayDialog;
    private readonly IPackageCreator m_PackageCreator;
    private readonly IApplicationProxy m_Application;
    public DropdownHandler(IResourceLoader resourceLoader,
        IUpmClient upmClient,
        IAssetStoreDownloadManager assetStoreDownloadManager,
        IPackageDatabase packageDatabase,
        IPageManager pageManager,
        IPackageOperationDispatcher operationDispatcher,
        ICustomDisplayDialog customDisplayDialog,
        IPackageCreator packageCreator,
        IApplicationProxy application)
    {
        m_ResourceLoader = RegisterDependency(resourceLoader);
        m_UpmClient = RegisterDependency(upmClient);
        m_AssetStoreDownloadManager = RegisterDependency(assetStoreDownloadManager);
        m_PackageDatabase = RegisterDependency(packageDatabase);
        m_PageManager = RegisterDependency(pageManager);
        m_OperationDispatcher = RegisterDependency(operationDispatcher);
        m_CustomDisplayDialog = RegisterDependency(customDisplayDialog);
        m_PackageCreator = RegisterDependency(packageCreator);
        m_Application = RegisterDependency(application);
    }

    public void ShowAddPackageByNameDropdown(VisualElement anchorElement, string packageName = null, string packageVersion = null)
    {
        var dropdown = new AddPackageByNameDropdown(m_ResourceLoader, m_UpmClient, m_PackageDatabase, m_PageManager, m_OperationDispatcher, m_CustomDisplayDialog, m_Application)
        {
            packageNameInitialValue = packageName,
            packageVersionInitialValue = packageVersion
        };
        DropdownWindow.ShowDropdown(anchorElement, dropdown);
    }

    public void ShowCreatePackageDropdown(VisualElement anchorElement)
    {
        DropdownWindow.ShowDropdown(anchorElement, new CreatePackageDropdown(m_ResourceLoader, m_PackageCreator, m_Application));
    }

    public void ShowGenericInputDropdown(VisualElement anchorElement, InputDropdownArgs args)
    {
        DropdownWindow.ShowDropdown(anchorElement, new GenericInputDropdown(m_ResourceLoader, args));
    }

    public void ShowInProgressDropdown(VisualElement anchorElement)
    {
        var dropdown = new InProgressDropdown(m_ResourceLoader, m_UpmClient, m_AssetStoreDownloadManager, m_PackageDatabase, m_PageManager);
        DropdownWindow.ShowDropdown(anchorElement, dropdown);
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
