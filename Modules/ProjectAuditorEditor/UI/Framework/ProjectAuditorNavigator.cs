// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    internal static partial class ProjectAuditorNavigator
    {
        [AutoStaticsCleanupOnCodeReload]
        static Action s_OnNavigationRequested;

        public static event Action OnNavigationRequested
        {
            add => s_OnNavigationRequested += value;
            remove => s_OnNavigationRequested -= value;
        }

        [AutoStaticsCleanupOnCodeReload]
        static PageId s_RequestedPage = PageId.None;

        [AutoStaticsCleanupOnCodeReload]
        static HomePage.Tab? s_RequestedHomeTab;

        public static PageId RequestedPage => s_RequestedPage;
        public static HomePage.Tab? RequestedHomeTab => s_RequestedHomeTab;

        public static void RequestHomePage(HomePage.Tab tab)
        {
            s_RequestedPage = PageId.Home;
            s_RequestedHomeTab = tab;
            s_OnNavigationRequested?.Invoke();
        }

        public static void RequestPage(PageId pageId)
        {
            s_RequestedPage = pageId;
            s_RequestedHomeTab = null;
            s_OnNavigationRequested?.Invoke();
        }

        public static void ClearRequest()
        {
            s_RequestedPage = PageId.None;
            s_RequestedHomeTab = null;
        }
    }
}
