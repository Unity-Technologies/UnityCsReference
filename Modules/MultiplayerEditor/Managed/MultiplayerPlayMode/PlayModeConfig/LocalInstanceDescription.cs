// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Build.Profile;
using UnityEditor.Multiplayer.Internal;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    class LocalInstanceDescription : InstanceDescription, IBuildableInstanceDescription
    {
        internal const string k_LocalInstanceTypeName = "Local";

        //[Tooltip("Select the Build profile that this instance will be based  as.")]
        [SerializeField] private BuildProfile m_BuildProfile;
        [SerializeField]
        private ServerSettings m_ServerSettings = new()
        {
            CliSettings = ServerCliSettings.Default,
            SimulatorSettings = SimulatorSettings.Default,
        };
        [SerializeField] private AdvancedConfig advancedConfiguration = new();

        [Serializable]
        public class AdvancedConfig
        {
            [Tooltip("Box checked : The logs will be streamed from local instance to the editor logs \nUnchecked : The logs will be captured from local instance into the logfile under {InstanceName}.txt")]
            [SerializeField] private bool m_StreamLogsToMainEditor = true;
            [SerializeField] private Color m_LogsColor = new Color(0.3643f, 0.581f, 0.8679f);
            [SerializeField, HideInInspector, FormerlySerializedAs("m_Arguments")] private string m_LocalArguments = "-screen-fullscreen 0 -screen-width 1024 -screen-height 720";
            [SerializeField, HideInInspector] private string m_SimulatedArguments = "-screen-fullscreen 0 -screen-width 1024 -screen-height 720";
            [SerializeField, HideInInspector] private string m_DeviceID = "";
            [SerializeField, HideInInspector] private string m_DeviceName = "";

            public bool StreamLogsToMainEditor
            {
                get => m_StreamLogsToMainEditor;
                set => m_StreamLogsToMainEditor = value;
            }

            public string DeviceID
            {
                get => m_DeviceID;
                set => m_DeviceID = value;
            }

            public string DeviceName
            {
                get => m_DeviceName;
                set => m_DeviceName = value;
            }

            public string LocalArguments
            {
                get => m_LocalArguments;
                set => m_LocalArguments = value;
            }

            public string SimulatedArguments
            {
                get => m_SimulatedArguments;
                set => m_SimulatedArguments = value;
            }

            public string GetArgumentsForMode(ServerSettings.ServerDeployMode deployMode)
            {
                return deployMode switch
                {
                    ServerSettings.ServerDeployMode.Local => m_LocalArguments,
                    ServerSettings.ServerDeployMode.Simulated => m_SimulatedArguments,
                    _ => m_LocalArguments
                };
            }

            public void SetArgumentsForMode(ServerSettings.ServerDeployMode deployMode, string arguments)
            {
                switch (deployMode)
                {
                    case ServerSettings.ServerDeployMode.Local:
                        m_LocalArguments = arguments;
                        break;
                    case ServerSettings.ServerDeployMode.Simulated:
                        m_SimulatedArguments = arguments;
                        break;
                }
            }

            public Color LogsColor
            {
                get => m_LogsColor;
                set => m_LogsColor = value;
            }
        }

        public BuildProfile BuildProfile
        {
            get => m_BuildProfile;
            set => m_BuildProfile = value;
        }

        internal ServerSettings ServerSettings
        {
            get => m_ServerSettings;
            set => m_ServerSettings = value;
        }

        public AdvancedConfig AdvancedConfiguration
        {
            get => advancedConfiguration;
            set => advancedConfiguration = value;
        }

        public string GetCurrentModeArguments()
        {
            return advancedConfiguration.GetArgumentsForMode(m_ServerSettings.DeployMode);
        }

        public void SetCurrentModeArguments(string arguments)
        {
            advancedConfiguration.SetArgumentsForMode(m_ServerSettings.DeployMode, arguments);
        }

        internal override string InstanceTypeName => k_LocalInstanceTypeName;
        internal override string BuildTargetType => InternalUtilities.GetBuildTargetType(m_BuildProfile);
        internal override string MultiplayerRole => m_BuildProfile == null
            ? string.Empty
            : MultiplayerRolesSettings.instance.GetMultiplayerRoleForBuildProfile(m_BuildProfile).ToString();

        internal virtual bool IsServer()
        {
            return LocalDeploymentUtility.IsServerProfileOrRole(m_BuildProfile);
        }
    }
}
