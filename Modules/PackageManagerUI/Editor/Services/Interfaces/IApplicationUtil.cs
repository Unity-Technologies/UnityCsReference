// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI
{
    internal interface IApplicationUtil
    {
        event Action<bool> onUserLoginStateChange;

        event Action<bool> onInternetReachabilityChange;

        event Action onFinishCompiling;

        bool isPreReleaseVersion { get; }

        string shortUnityVersion { get; }

        bool isInternetReachable { get; }

        bool isCompiling { get; }

        bool isUserLoggedIn { get; }

        bool isUserInfoReady { get; }

        string userAppDataPath { get; }

        void ShowLogin();

        void OpenURL(string url);

        string OpenFilePanelWithFilters(string title, string directory, string[] filters);

        string GetFileName(string path);
    }
}
