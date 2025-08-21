// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Multiplayer.Internal;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    class RemoteInstanceDescription : InstanceDescription, IBuildableInstanceDescription
    {
        private const string k_DefaultIdentifier = "LinuxServer";
        private const string k_DefaultRegion = "North America";
        private const int k_DefaultCores = 1;
        private const int k_DefaultMemory = 800;
        private const int k_DefaultCpuFrequency = 750;

        private const string k_NamePrefix = "CreatedFromTheUnityEditor";
        internal const string k_RemoteInstanceTypeName = "Remote";

        private const string k_IdentifierTooltip = "The string that names the Multiplay build, fleet, and server. This name uses the format `CreatedFromTheUnityEditor-[identifier]-[username]` Don't use any special characters.";
        internal const string k_ServerNameTooltip = "The name that the build, fleet, and server appear under on the Unity Cloud dashboard.";
        private const string k_FleetRegionTooltip = "The name of the build configuration in the Unity Cloud dashboard.";
        private const string k_InstanceAmountOfCoresTooltip = "The number of CPU cores this server instance requests.";
        private const string k_InstanceAmountOfMemoryMBTooltip = "The amount of memory, in MegaBytes, that this server instance requests.";
        private const string k_InstanceCpuFrequencyMHzTooltip = "The amount of CPU time, in Mega Hertz, that this server instance requests.";
        private const string k_StreamLogsToMainEditorTooltip = "Enable to stream logs from the remote instance to capture and display logs in the editor.";
        private const string k_LogsColorTooltip = "The color that logs appear in the editor.";
        private const string k_ArgumentsTooltip = "Arguments that are passed along the application when it launches. \n Don't modify these values unless you are familiar with the Multiplay argument requirements. Refer to the Multiplay documentation for more information.";

        [SerializeField]
        [Tooltip("For a remote instance, it is recommended to use a profile based on the dedicated server platform.")]
        BuildProfile m_BuildProfile;
        public BuildProfile BuildProfile
        {
            get => m_BuildProfile;
            set => m_BuildProfile = value;
        }

        [SerializeField] private AdvancedConfig advancedConfiguration;
        public AdvancedConfig AdvancedConfiguration
        {
            get
            {
                if (advancedConfiguration == null)
                    advancedConfiguration = new AdvancedConfig();

                return advancedConfiguration;
            }
            set => advancedConfiguration = value;
        }

        [Serializable]
        public class AdvancedConfig
        {
            // Multiplay specific parameters.
            [Tooltip(k_IdentifierTooltip)]
            [SerializeField, FormerlySerializedAs("m_OriginalName")]
            private string m_Identifier = k_DefaultIdentifier;

            [Tooltip(k_FleetRegionTooltip)]
            [SerializeField]
            private string m_FleetRegion = k_DefaultRegion;

            [Tooltip(k_InstanceAmountOfCoresTooltip)]
            [SerializeField, Range(1, 8)]
            private int m_InstanceAmountOfCores = k_DefaultCores;

            [Tooltip(k_InstanceAmountOfMemoryMBTooltip)]
            [SerializeField]
            private int m_InstanceAmountOfMemoryMB = k_DefaultMemory;

            [Tooltip(k_InstanceCpuFrequencyMHzTooltip)]
            [SerializeField]
            private int m_InstanceCpuFrequencyMHz = k_DefaultCpuFrequency;


            // Generic parameters
            [Tooltip(k_StreamLogsToMainEditorTooltip)]
            [SerializeField] private bool m_StreamLogsToMainEditor;

            //[Todo] Should only appear when m_StreamLogsToMainEditor is true.
            [Tooltip(k_LogsColorTooltip)]
            [SerializeField] private Color m_LogsColor = new Color(0.3643f, 0.866f, 0.5130f);

            [Tooltip(k_ArgumentsTooltip)]
            [SerializeField] private string m_Arguments = "-port $$port$$ -queryport $$query_port$$ -logFile $$log_dir$$/Engine.log";

            public string Identifier
            {
                get => m_Identifier;
                set => m_Identifier = value;
            }
            public string FleetRegion
            {
                get => m_FleetRegion;
                set => m_FleetRegion = value;
            }
            public int InstanceAmountOfCores
            {
                get => m_InstanceAmountOfCores;
                set => m_InstanceAmountOfCores = value;
            }
            public int InstanceAmountOfMemoryMB
            {
                get => m_InstanceAmountOfMemoryMB;
                set => m_InstanceAmountOfMemoryMB = value;
            }
            public int InstanceCpuFrequencyMHz
            {
                get => m_InstanceCpuFrequencyMHz;
                set => m_InstanceCpuFrequencyMHz = value;
            }
            public bool StreamLogsToMainEditor
            {
                get => m_StreamLogsToMainEditor;
                set => m_StreamLogsToMainEditor = value;
            }
            public Color LogsColor
            {
                get => m_LogsColor;
                set => m_LogsColor = value;
            }
            public string Arguments
            {
                get => m_Arguments;
                set => m_Arguments = value;
            }
        }

        internal BuildConfigurationSettings GetBuildConfigurationSettings()
        {
            if (advancedConfiguration == null)
                advancedConfiguration = new AdvancedConfig();

            return new BuildConfigurationSettings
            {
                CommandLineArguments = advancedConfiguration.Arguments,
                CoresCount = advancedConfiguration.InstanceAmountOfCores,
                MemoryMiB = advancedConfiguration.InstanceAmountOfMemoryMB,
                SpeedMhz = advancedConfiguration.InstanceCpuFrequencyMHz
            };
        }

        internal static string GetUserIdentifier()
        {
            var userName = CloudProjectSettings.userName;

            if (userName.Contains('@'))
                userName = userName[..userName.IndexOf('@')];

            return userName;
        }

        private static string EscapeIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                identifier = "unnamed";

            identifier = identifier.Replace(" ", "_");
            return Regex.Replace(identifier, "[^a-zA-Z0-9-_]", string.Empty);
        }

        internal static string ComputeMultiplayName(string serverIdentifier, string userIdentifier)
            => $"{k_NamePrefix}-{EscapeIdentifier(serverIdentifier)}-{EscapeIdentifier(userIdentifier)}";
        internal static string ComputeMultiplayName(string serverIdentifier)
            => ComputeMultiplayName(serverIdentifier, GetUserIdentifier());

        internal override string InstanceTypeName => k_RemoteInstanceTypeName;

        internal override string BuildTargetType => InternalUtilities.GetBuildTargetType(m_BuildProfile);
        internal override string MultiplayerRole => m_BuildProfile == null
            ? string.Empty
            : MultiplayerRolesSettings.instance.GetMultiplayerRoleForBuildProfile(m_BuildProfile).ToString();
    }
}
