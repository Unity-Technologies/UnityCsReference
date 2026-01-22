// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile.Elements
{
    /// <summary>
    /// Player Setting overrides stored as embedded YAML within a <see cref="BuildProfile"/>. Some setting changes
    /// should always prompt for an editor restart.
    /// </summary>
    class PlayerSettingsProvider : IBuildProfileSettingsProvider
    {
        public string GetDisplayName() => TrText.playerSettings;

        public string GetTooltip() => TrText.playerSettingsInfo;

        /// <summary>
        /// Player settings requires platform module support.
        /// </summary>
        public bool CanAddSettings(BuildProfile profile) => BuildProfileModuleUtil.IsModuleInstalled(profile.platformGuid);

        public bool HasSettings(BuildProfile profile) =>
            BuildProfileModuleUtil.IsModuleInstalled(profile.platformGuid)
            && BuildProfileModuleUtil.HasSerializedPlayerSettings(profile);

        public void OnAdd(BuildProfile profile)
        {
            if (profile.playerSettings != null)
            {
                return;
            }

            BuildProfileModuleUtil.CreatePlayerSettingsFromGlobal(profile);
        }

        public void OnReset(BuildProfile profile)
        {
            var targetName = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(profile.buildTarget));
            var customScriptingDefines = PlayerSettings.GetScriptingDefineSymbols(targetName);
            var customAdditionalCompilerArguments = PlayerSettings.GetAdditionalCompilerArguments(targetName);

            if (profile == BuildProfileContext.activeProfile)
            {
                // we should check if the player setting overrides we're updating differ from the project settings
                // in that case we should check for any setting that requires an editor restart to take effect
                // if it does, we should a restart prompt. if the user cancels, we cancel the resetting action
                var isSuccess = BuildProfileModuleUtil.HandlePlayerSettingsChanged(profile, null);
                if (!isSuccess)
                {
                    return;
                }
            }

            PlayerSettingsEditor.DiscardPendingChangesForAllEditors(profile.playerSettings);
            BuildProfileModuleUtil.RemovePlayerSettings(profile);
            OnAdd(profile);
            EditorUtility.SetDirty(profile);

            CheckPropertiesThatRequireRecompilation(profile, targetName, customScriptingDefines, customAdditionalCompilerArguments);
        }

        public void OnRemove(BuildProfile profile)
        {
            var targetName = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(profile.buildTarget));
            var customScriptingDefines = PlayerSettings.GetScriptingDefineSymbols(targetName);
            var customAdditionalCompilerArguments = PlayerSettings.GetAdditionalCompilerArguments(targetName);

            // we only want to show the restart editor prompt when making changes to an active profile
            // otherwise we continue normally with removing the player settings
            if (profile == BuildProfileContext.activeProfile)
            {
                // success is we either found no settings to restart or we did and the user agreed to restart the editor
                // failure here is if we found settings requiring restart but the user declined to cancel
                // so we don't continue with the action
                var isSuccess = BuildProfileModuleUtil.HandlePlayerSettingsChanged(profile, null);
                if (!isSuccess)
                {
                    return;
                }
            }

            BuildProfileModuleUtil.RemovePlayerSettings(profile);
            BuildProfileModuleUtil.SerializePlayerSettings(profile);
            EditorUtility.SetDirty(profile);

            CheckPropertiesThatRequireRecompilation(profile, targetName, customScriptingDefines, customAdditionalCompilerArguments);
        }

        public Action<BuildProfile> GetResetAction() => OnReset;

        public VisualElement CreateInspectorGUI(BuildProfile profile, SerializedObject serializedObject)
        {
            return new PlayerSettingsVisualElement(profile, serializedObject);
        }

        static void CheckPropertiesThatRequireRecompilation(
            BuildProfile profile,
            NamedBuildTarget targetName,
            string customScriptingDefines,
            string[] customAdditionalCompilerArguments)
        {
            if (BuildProfile.GetActiveBuildProfile() != profile)
                return;

            // Check if current global player settings that we switched to has different script defines
            // when compared to the custom player settings
            var additionalCompilerArguments = PlayerSettings.GetAdditionalCompilerArguments(targetName);
            var scriptingDefines = PlayerSettings.GetScriptingDefineSymbols(targetName);
            if (customScriptingDefines != scriptingDefines || !ArrayUtility.ArrayEquals(customAdditionalCompilerArguments, additionalCompilerArguments))
            {
                profile.scriptingDefines = BuildProfileModuleUtil.RemoveInvalidScriptingDefines(profile.scriptingDefines);
                BuildProfileModuleUtil.RequestScriptCompilation(profile);
            }
        }
    }
}
