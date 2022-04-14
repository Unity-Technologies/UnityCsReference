// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Diagnostics.CodeAnalysis;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    [ExcludeFromCodeCoverage]
    internal class ApplicationProxy
    {
        [SerializeField]
        private bool m_CheckingCompilation = false;

        [SerializeField]
        private bool m_IsInternetReachable;

        [SerializeField]
        private double m_LastInternetCheck;

        public virtual event Action<bool> onInternetReachabilityChange = delegate {};
        public virtual event Action onFinishCompiling = delegate {};
        public virtual event Action<PlayModeStateChange> onPlayModeStateChanged = delegate {};
        public virtual event Action update = delegate {};

        public virtual string userAppDataPath => InternalEditorUtility.userAppDataFolder;

        public virtual bool isInternetReachable => m_IsInternetReachable;

        public virtual bool isBatchMode => Application.isBatchMode;

        public virtual bool isUpmRunning => !EditorApplication.isPackageManagerDisabled;

        public virtual bool isCompiling
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

        public virtual string unityVersion => Application.unityVersion;

        public virtual string shortUnityVersion
        {
            get
            {
                var unityVersionParts = Application.unityVersion.Split('.');
                return $"{unityVersionParts[0]}.{unityVersionParts[1]}";
            }
        }

        public virtual bool isDeveloperBuild => Unsupported.IsDeveloperBuild();

        public void OnEnable()
        {
            m_IsInternetReachable = Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork;
            m_LastInternetCheck = EditorApplication.timeSinceStartup;
            EditorApplication.update += OnUpdate;
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
        }

        public void OnDisable()
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

        public virtual void OpenURL(string url)
        {
            Application.OpenURL(url);
        }

        public virtual void RevealInFinder(string path)
        {
            EditorUtility.RevealInFinder(path);
        }

        public virtual string OpenFilePanelWithFilters(string title, string directory, string[] filters)
        {
            return EditorUtility.OpenFilePanelWithFilters(title, directory, filters);
        }

        public virtual string OpenFolderPanel(string title, string folder)
        {
            return EditorUtility.OpenFolderPanel(title, folder, string.Empty);
        }

        public virtual bool DisplayDialog(string idForAnalytics, string title, string message, string ok, string cancel = "")
        {
            var result = EditorUtility.DisplayDialog(title, message, ok, cancel);
            PackageManagerDialogAnalytics.SendEvent(idForAnalytics, title, message, result ? ok : cancel);
            return result;
        }

        public virtual int DisplayDialogComplex(string idForAnalytics, string title, string message, string ok, string cancel, string alt)
        {
            var result = EditorUtility.DisplayDialogComplex(title, message, ok, cancel, alt);
            PackageManagerDialogAnalytics.SendEvent(idForAnalytics, title, message, result == 1 ? cancel : (result == 2 ? alt : ok));
            return result;
        }

        public virtual T Load<T>(string path) where T : Object
        {
            return EditorGUIUtility.Load(path) as T;
        }
    }
}
