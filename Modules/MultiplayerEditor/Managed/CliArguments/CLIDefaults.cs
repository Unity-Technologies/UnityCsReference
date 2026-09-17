// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEngine;
using Unity.Multiplayer.Internal;

namespace Unity.DedicatedServer
{
    class CLIDefaults
    {
        class SettingsContent
        {
            public static readonly GUIContent listenPort = L10n.TextContent("Port", "Port server listens on", null, null);
            public static readonly GUIContent targetFramerate = L10n.TextContent("Target Framerate", "rate server ticks", null, null);
            public static readonly GUIContent logLevel = L10n.TextContent("Log level", "minimum log level", null, null);
            public static readonly GUIContent logPath = L10n.TextContent("Log path", "directory for log files", null, null);
            public static readonly GUIContent queryPort = L10n.TextContent("Query port", "Port for server status queries", null, null);
            public static readonly GUIContent queryType = L10n.TextContent("Query type", "protocol for server status queries", null, null);
            public static readonly GUIContent argumentErrorPolicy = L10n.TextContent("Argument error policy", "how arguement errors are to be handled", null, null);
        }

        class CLIDefault
        {
            public string name;
            public string defaultValue;
            public GUIContent content;

            public CLIDefault(string Name, string DefaultValue, GUIContent Content)
            {
                name = Name;
                defaultValue = DefaultValue;
                content = Content;
            }

            public virtual void DisplayProperty()
            {
            }

            public string GetDefaultValue()
            {
                var defValue = EditorUserBuildSettings.GetPlatformSettings(NamedBuildTarget.Server.TargetName, $"arg-default-{name}");
                if (string.IsNullOrEmpty(defValue))
                {
                    defValue = defaultValue;
                    SetDefaultValue(defValue);
                }
                return defValue;
            }

            protected void SetDefaultValue(string defValue)
            {
                EditorUserBuildSettings.SetPlatformSettings(NamedBuildTarget.Server.TargetName, $"arg-default-{name}", defValue);
            }
        };

        class CLIDefaultInt : CLIDefault
        {
            public CLIDefaultInt(string Name, int DefaultValue, GUIContent Content)
                : base(Name, $"{DefaultValue}", Content)
            {
            }

            public override void DisplayProperty()
            {
                var val = GetDefaultValue();
                int.TryParse(val, out var intVal);
                var newVal = EditorGUILayout.IntField(content, intVal);
                if (newVal != intVal)
                    SetDefaultValue($"{newVal}");
            }
        }

        class CLIDefaultString : CLIDefault
        {
            public CLIDefaultString(string Name, string DefaultValue, GUIContent Content)
                : base(Name, DefaultValue, Content)
            {
            }

            public override void DisplayProperty()
            {
                var strVal = GetDefaultValue();
                var newVal = EditorGUILayout.TextField(content, strVal);
                if (newVal != strVal)
                    SetDefaultValue(newVal);
            }
        }

        class CLIDefaultEnum<T> : CLIDefault where T : Enum
        {
            public string[] enumValues;

            public CLIDefaultEnum(string Name, T DefaultValue, GUIContent Content)
                : base(Name, $"{Convert.ToInt32(DefaultValue)}", Content)
            {
                enumValues = Enum.GetNames(DefaultValue.GetType());
            }

            public override void DisplayProperty()
            {
                var val = GetDefaultValue();
                int.TryParse(val, out var selected);
                var newVal = EditorGUILayout.Popup(content, selected, enumValues);
                if (newVal != selected)
                    SetDefaultValue($"{newVal}");
            }
        }

        [NoAutoStaticsCleanup] // fixed configuration array, allocated once; stable across reloads
        static readonly CLIDefault[] Defaults =
        {
            new CLIDefaultInt("port",       7777,     SettingsContent.listenPort),
            new CLIDefaultInt("framerate",  30,       SettingsContent.targetFramerate),
            new CLIDefaultString("logpath",    "logs",   SettingsContent.logPath),
            new CLIDefaultInt("queryport",  20000,    SettingsContent.queryPort),
            new CLIDefaultString("querytype",  "SQP",    SettingsContent.queryType),
            new CLIDefaultEnum<UnityEngine.DedicatedServer.Arguments.ArgumentErrorPolicy>
                ("argument-error-policy",  UnityEngine.DedicatedServer.Arguments.ArgumentErrorPolicy.Warn,  SettingsContent.argumentErrorPolicy),
        };

        public static void OnGUI()
        {
            foreach (var def in Defaults)
                def.DisplayProperty();
        }

        private static void UpdateBootConfigData(BuildTarget target, string bootConfigFullPath)
        {
            BootConfig bootConfig = new BootConfig(target, bootConfigFullPath);
            if (bootConfig.Read())
            {
                foreach (var def in Defaults)
                {
                    bootConfig.Add($"dedicatedServer-{def.name}", def.GetDefaultValue());
                }
                bootConfig.Write();
            }
        }

        private static NamedBuildTarget NamedBuildTargetFromActiveSettings(BuildTarget target)
        {
            BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(target);

            if (targetGroup == BuildTargetGroup.Standalone)
            {
                if (EditorUserBuildSettings.standaloneBuildSubtarget == StandaloneBuildSubtarget.Server)
                    return NamedBuildTarget.Server;

                return NamedBuildTarget.Standalone;
            }

            return NamedBuildTarget.FromBuildTargetGroup(targetGroup);
        }

        [PostProcessBuild]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (!DedicatedServerMigrationUtility.ShouldEnableDedicatedServer())
                return;

            var namedBuildTarget = NamedBuildTargetFromActiveSettings(target);
            if (namedBuildTarget == NamedBuildTarget.Server)
                UpdateBootConfigData(target, pathToBuiltProject);
        }
    }
}
