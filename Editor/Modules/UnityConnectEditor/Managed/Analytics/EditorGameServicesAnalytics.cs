// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.Connect
{
    [InitializeOnLoad]
    static class EditorGameServicesAnalytics
    {
        const string k_ComponentProjectBind = "Project Bind";
        const string k_ComponentToolbar = "Toolbar";
        const string k_ComponentTopMenu = "Top Menu";

        const string k_ActionCloud = "Cloud";
        const string k_ActionDisplay = "Display";
        const string k_ActionExplore = "Explore";
        const string k_ActionGeneralSettings = "General Settings";
        const string k_ActionServices = "Services";

        const string k_EditorSource = "Unity Editor";

        static EditorGameServicesAnalytics()
        {
            EditorAnalytics.RegisterEventEditorGameService();
        }

        internal static void SendProjectBindDisplayEvent()
        {
            SendEvent(k_ComponentProjectBind, k_ActionDisplay);
        }

        internal static void SendToolbarCloudEvent()
        {
            SendEvent(k_ComponentToolbar, k_ActionCloud);
        }

        internal static void SendTopMenuExploreEvent()
        {
            SendEvent(k_ComponentTopMenu, k_ActionExplore);
        }

        internal static void SendTopMenuGeneralSettingsEvent()
        {
            SendEvent(k_ComponentTopMenu, k_ActionGeneralSettings);
        }

        internal static void SendTopMenuServicesEvent()
        {
            SendEvent(k_ComponentTopMenu, k_ActionServices);
        }

        static void SendEvent(string component, string action)
        {
            EditorAnalytics.SendEventEditorGameService(new EditorGameServiceEvent
            {
                action = action,
                assembly_info = "",
                component = component,
                package = k_EditorSource,
                package_ver = ""
            });
        }

        [Serializable]
        internal struct EditorGameServiceEvent
        {
            public string action;
            public string assembly_info;
            public string component;
            public string package;
            public string package_ver;
        }
    }
}
