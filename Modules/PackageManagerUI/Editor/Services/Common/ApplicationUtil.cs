// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    internal sealed class ApplicationUtil
    {
        public static readonly string k_ResetPackagesMenuName = "Reset Packages to defaults";
        public static readonly string k_ResetPackagesMenuPath = "Help/" + k_ResetPackagesMenuName;

        static IApplicationUtil s_Instance = null;
        public static IApplicationUtil instance { get { return s_Instance ?? ApplicationUtilInternal.instance; } }

        private class ApplicationUtilInternal : IApplicationUtil
        {
            static ApplicationUtilInternal s_Instance = null;
            public static ApplicationUtilInternal instance { get { return s_Instance ?? (s_Instance = new ApplicationUtilInternal()); } }

            public event Action onFinishCompiling = delegate {};
            private bool m_CheckingCompilation = false;

            public bool isPreReleaseVersion
            {
                get
                {
                    var lastToken = Application.unityVersion.Split('.').LastOrDefault();
                    return lastToken.Contains("a") || lastToken.Contains("b");
                }
            }

            public string shortUnityVersion
            {
                get
                {
                    var unityVersionParts = Application.unityVersion.Split('.');
                    return $"{unityVersionParts[0]}.{unityVersionParts[1]}";
                }
            }

            public bool isInternetReachable
            {
                get { return Application.internetReachability != NetworkReachability.NotReachable; }
            }

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

            private void CheckCompilationStatus()
            {
                if (EditorApplication.isCompiling)
                    return;

                m_CheckingCompilation = false;
                EditorApplication.update -= CheckCompilationStatus;

                onFinishCompiling();
            }
        }
    }
}
