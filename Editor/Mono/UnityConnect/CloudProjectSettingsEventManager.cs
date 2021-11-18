// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Connect;
using UnityEngine;

namespace UnityEditor
{
    /// <summary>
    /// Manages the events related to the project state
    /// </summary>
    public class CloudProjectSettingsEventManager
    {
        /// <summary>
        /// The instance of the Cloud Project Settings event manager.
        /// </summary>
        public static CloudProjectSettingsEventManager instance { get; } = new CloudProjectSettingsEventManager();

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

        ~CloudProjectSettingsEventManager()
        {
            UnregisterToUnityConnectEvents();
        }

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
