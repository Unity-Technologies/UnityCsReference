// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditorInternal;
using UnityEngine;
using System.Diagnostics.CodeAnalysis;
using UnityEngine.Networking;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IApplicationProxy : IService
    {
        event Action<bool> onInternetReachabilityChange;
        event Action onFinishCompiling;
        event Action<PlayModeStateChange> onPlayModeStateChanged;
        event Action update;

        string userAppDataPath { get; }
        bool isInternetReachable { get; }
        bool isBatchMode { get; }
        bool isUpmRunning { get; }
        bool isCompiling { get; }

        string unityVersion { get; }
        string shortUnityVersion { get; }
        string docsUrlWithShortUnityVersion { get; }
        bool isDeveloperBuild { get; }

        void OpenURL(string url);
        void RevealInFinder(string path);
        string OpenFilePanelWithFilters(string title, string directory, string[] filters);
        string OpenFolderPanel(string title, string folder);
        bool DisplayDialog(string idForAnalytics, string title, string message, string ok, string cancel = "");
        int DisplayDialogComplex(string idForAnalytics, string title, string message, string ok, string cancel, string alt);
        void CheckUrlValidity(string uri, Action success, Action failure);
    }

    [Serializable]
    [ExcludeFromCodeCoverage]
    internal class ApplicationProxy : BaseService<IApplicationProxy>, IApplicationProxy
    {
        [SerializeField]
        private bool m_CheckingCompilation = false;

        [SerializeField]
        private bool m_IsInternetReachable;

        [SerializeField]
        private double m_LastInternetCheck;

        public event Action<bool> onInternetReachabilityChange = delegate {};
        public event Action onFinishCompiling = delegate {};
        public event Action<PlayModeStateChange> onPlayModeStateChanged = delegate {};
        public event Action update = delegate {};

        public string userAppDataPath => InternalEditorUtility.userAppDataFolder;

        public bool isInternetReachable => m_IsInternetReachable;

        public bool isBatchMode => Application.isBatchMode;

        public bool isUpmRunning => !EditorApplication.isPackageManagerDisabled;

        public bool isCompiling
        {
            get
            {
                var result = EditorApplication.isCompiling;
                if (result && !m_CheckingCompilation)
                {
                    EditorApplication.update -= CheckCompilationStatus;
                    EditorApplication.update += CheckCompilationStatus;
                    m_CheckingCompilation = true;
                }
                return result;
            }
        }

        public string unityVersion => Application.unityVersion;

        public string shortUnityVersion
        {
            get
            {
                var unityVersionParts = Application.unityVersion.Split('.');
                return $"{unityVersionParts[0]}.{unityVersionParts[1]}";
            }
        }

        public const string k_UnityDocsUrl = "https://docs.unity3d.com/";
        public string docsUrlWithShortUnityVersion => $"{k_UnityDocsUrl}{shortUnityVersion}/";

        public bool isDeveloperBuild => Unsupported.IsDeveloperBuild();

        public override void OnEnable()
        {
            m_IsInternetReachable = Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork;
            m_LastInternetCheck = EditorApplication.timeSinceStartup;
            EditorApplication.update += OnUpdate;
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
        }

        public override void OnDisable()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.playModeStateChanged -= PlayModeStateChanged;
        }

        private void PlayModeStateChanged(PlayModeStateChange state)
        {
            onPlayModeStateChanged?.Invoke(state);
        }

        private void OnUpdate()
        {
            CheckInternetReachability();
            update?.Invoke();
        }

        private void CheckInternetReachability()
        {
            if (EditorApplication.timeSinceStartup - m_LastInternetCheck < 2.0)
                return;

            m_LastInternetCheck = EditorApplication.timeSinceStartup;
            var isInternetReachable = Application.internetReachability != NetworkReachability.NotReachable;
            if (isInternetReachable != m_IsInternetReachable)
            {
                m_IsInternetReachable = isInternetReachable;
                onInternetReachabilityChange?.Invoke(m_IsInternetReachable);
            }
        }

        private void CheckCompilationStatus()
        {
            if (EditorApplication.isCompiling)
                return;

            m_CheckingCompilation = false;
            EditorApplication.update -= CheckCompilationStatus;

            onFinishCompiling();
        }

        // Eventually we want a better way of opening urls, to be further addressed in  https://jira.unity3d.com/browse/PAX-2592
        public void OpenURL(string url)
        {
            Application.OpenURL(url);
        }

        public void RevealInFinder(string path)
        {
            EditorUtility.RevealInFinder(path);
        }

        public string OpenFilePanelWithFilters(string title, string directory, string[] filters)
        {
            return EditorUtility.OpenFilePanelWithFilters(title, directory, filters);
        }

        public string OpenFolderPanel(string title, string folder)
        {
            return EditorUtility.OpenFolderPanel(title, folder, string.Empty);
        }

        public bool DisplayDialog(string idForAnalytics, string title, string message, string ok, string cancel = "")
        {
            var result = EditorUtility.DisplayDialog(title, message, ok, cancel);
            PackageManagerDialogAnalytics.SendEvent(idForAnalytics, title, message, result ? ok : cancel);
            return result;
        }

        public int DisplayDialogComplex(string idForAnalytics, string title, string message, string ok, string cancel, string alt)
        {
            var result = EditorUtility.DisplayDialogComplex(title, message, ok, cancel, alt);
            PackageManagerDialogAnalytics.SendEvent(idForAnalytics, title, message, result == 1 ? cancel : (result == 2 ? alt : ok));
            return result;
        }

        public void CheckUrlValidity(string uri, Action success, Action failure)
        {
            var request = UnityWebRequest.Head(uri);
            var operation = request.SendWebRequest();
            try
            {
                operation.completed += _ =>
                {
                    if (request.responseCode is >= 200 and < 300)
                        success?.Invoke();
                    else
                        failure?.Invoke();
                };
            }
            catch (InvalidOperationException e)
            {
                if (e.Message != "Insecure connection not allowed")
                    throw e;
            }
        }
    }
}
