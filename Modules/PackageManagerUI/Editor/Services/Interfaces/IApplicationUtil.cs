// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Connect;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal interface IApplicationUtil
    {
        event Action<bool> onUserLoginStateChange;

        event Action<bool> onInternetReachabilityChange;

        event Action onFinishCompiling;

        event Action onEditorSelectionChanged;

        UnityEngine.Object activeSelection { get; set; }

        bool isPreReleaseVersion { get; }

        string shortUnityVersion { get; }

        bool isInternetReachable { get; }

        bool isBatchMode { get; }

        bool isUpmRunning { get; }

        bool isCompiling { get; }

        bool isUserLoggedIn { get; }

        bool isUserInfoReady { get; }

        string userAppDataPath { get; }

        void ShowLogin();

        void OpenURL(string url);

        IAsyncHTTPClient GetASyncHTTPClient(string url);

        IAsyncHTTPClient PostASyncHTTPClient(string url, string postData);

        void AbortASyncHTTPClientByTag(string tag);

        void GetAuthorizationCodeAsync(string clientId, Action<UnityOAuth.AuthCodeResponse> callback);

        int CalculateNumberOfElementsInsideContainerToDisplay(VisualElement container, float elementHeight);

        string GetTranslationForText(string text);

        void TranslateTextElement(TextElement textElement);

        string OpenFilePanelWithFilters(string title, string directory, string[] filters);

        string GetFileName(string path);
    }
}
