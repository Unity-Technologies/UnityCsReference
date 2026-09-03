// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Connect;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor
{
    /// <summary>
    /// Manages the events related to the project state
    /// </summary>
    public partial class CloudProjectSettingsEventManager
    {
        [AutoStaticsCleanupOnCodeReload]
        static CloudProjectSettingsEventManager s_Instance;

        /// <summary>
        /// The instance of the Cloud Project Settings event manager.
        /// </summary>
        public static CloudProjectSettingsEventManager instance
        {
            get
            {
                EnsureInstanceInitialized();
                return s_Instance;
            }
        }

        // Created lazily by the getter rather than in the field initializer: the constructor
        // subscribes to UnityConnect.instance, which is also reset on code reload, so the
        // subscription must happen after the cleanup phase has completed — and first access
        // through the getter always is.
        static void EnsureInstanceInitialized()
        {
            if (s_Instance == null)
            {
                s_Instance = new CloudProjectSettingsEventManager();
            }
        }

        /// <summary>
        /// The event fired when the state of the project changes.
        /// </summary>
        public event Action projectStateChanged;

        /// <summary>
        /// The event fired when the state of the project is refreshed.
        /// </summary>
        public event Action projectRefreshed;

        CloudProjectSettingsEventManager()
        {
            RegisterToUnityConnectEvents();
        }

#pragma warning disable UA5000 // The Avoid Finalizer Analyzer produces compile errors for any new finalizers. This pre-existing finalizer declaration has been suppressed, but should be rewritten if possible.
        ~CloudProjectSettingsEventManager()
        {
            UnregisterToUnityConnectEvents();
        }
#pragma warning restore UA5000

        void RegisterToUnityConnectEvents()
        {
            UnityConnect.instance.ProjectStateChanged += OnProjectStateChanged;
            UnityConnect.instance.ProjectRefreshed += OnProjectRefreshed;
        }

        void UnregisterToUnityConnectEvents()
        {
            UnityConnect.instance.ProjectStateChanged -= OnProjectStateChanged;
            UnityConnect.instance.ProjectRefreshed -= OnProjectRefreshed;
        }

        void OnProjectStateChanged(ProjectInfo projectInfo)
        {
            projectStateChanged?.Invoke();
        }

        void OnProjectRefreshed(ProjectInfo projectInfo)
        {
            projectRefreshed?.Invoke();
        }
    }
}
