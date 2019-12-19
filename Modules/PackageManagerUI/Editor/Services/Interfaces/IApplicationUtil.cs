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

        bool isPreReleaseVersion { get; }

        string shortUnityVersion { get; }

        bool isInternetReachable { get; }

        bool isCompiling { get; }

        bool isUserLoggedIn { get; }

        string userAppDataPath { get; }

        void ShowLogin();

        void OpenURL(string url);

        IAsyncHTTPClient GetASyncHTTPClient(string url, string method = null);

        void GetAuthorizationCodeAsync(string clientId, Action<UnityOAuth.AuthCodeResponse> callback);

        int CalculateNumberOfElementsInsideContainerToDisplay(VisualElement container, float elementHeight);
        string GetTranslationForText(string text);

        void TranslateTextElement(TextElement textElement);
    }
}
