// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// TODO EPIC: https://jira.unity3d.com/browse/PLAT-5745
    /// </summary>
    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [VisibleToOtherModules]
    internal sealed partial class BuildProfile : ScriptableObject
    {
        /// <summary>
        /// Build Target used to fetch module and build profile extension.
        /// </summary>
        [SerializeField] BuildTarget m_BuildTarget = BuildTarget.NoTarget;
        public BuildTarget buildTarget
        {
            get => m_BuildTarget;
            internal set => m_BuildTarget = value;
        }

        /// <summary>
        /// Subtarget, Default for all non-Standalone platforms.
        /// </summary>
        [SerializeField] StandaloneBuildSubtarget m_Subtarget;
        public StandaloneBuildSubtarget subtarget
        {
            get => m_Subtarget;
            internal set => m_Subtarget = value;
        }

        /// <summary>
        /// Module name used to fetch build profiles.
        /// </summary>
        [SerializeField] string m_ModuleName;
        public string moduleName
        {
            get => m_ModuleName;
            internal set => m_ModuleName = value;
        }

        /// <summary>
        /// Platform module specific build settings; e.g. AndroidBuildSettings.
        /// </summary>
        [SerializeReference] BuildProfilePlatformSettingsBase m_PlatformBuildProfile;
        public BuildProfilePlatformSettingsBase platformBuildProfile
        {
            get => m_PlatformBuildProfile;
            internal set => m_PlatformBuildProfile = value;
        }

        [SerializeField] private EditorBuildSettingsScene[] m_Scenes = Array.Empty<EditorBuildSettingsScene>();
        public EditorBuildSettingsScene[] scenes
        {
            get
            {
                CheckSceneListConsistency();
                return m_Scenes;
            }
            set
            {
                if (m_Scenes == value)
                    return;

                m_Scenes = value;
                CheckSceneListConsistency();

                if (this == BuildProfileContext.instance.activeProfile)
                    EditorBuildSettings.SceneListChanged();
            }
        }

        void OnEnable()
        {
            // Check if the platform support module has been installed,
            // and try to set an uninitialized platform settings.
            if (platformBuildProfile == null)
                TryCreatePlatformSettings();

            CheckSceneListConsistency();
            onBuildProfileEnable?.Invoke(this);
        }

        /// <summary>
        /// EditorBuildSettingsScene stores both path and GUID. Path can become
        /// invalid when scenes are moved or renamed and must be recalculated.
        /// </summary>
        /// <see cref="EditorBuildSettings"/> native function, EnsureScenesAreValid.
        void CheckSceneListConsistency()
        {
            int length = m_Scenes.Length;
            for (int i = length - 1; i >= 0; i--)
            {
                var scene = m_Scenes[i];

                // EditorBuildSettingScene entry may be null.
                if (scene == null)
                {
                    RemoveAt(i);
                    continue;
                }

                bool isGuidValid = !scene.guid.Empty();
                if (isGuidValid)
                {
                    // Scene may have been moved or renamed.
                    scene.path = AssetDatabase.GUIDToAssetPath(scene.guid);
                }

                if (string.IsNullOrEmpty(scene.path))
                {
                    // Scene Object may have been deleted from disk.
                    RemoveAt(i);
                    continue;
                }
                
                if (!isGuidValid)
                    scene.guid = AssetDatabase.GUIDFromAssetPath(scene.path);

                // Asset may have been deleted. AssetDatabase may cache GUID to/from
                // path mapping.
                if (AssetDatabase.GetMainAssetTypeFromGUID(scene.guid) is null)
                    RemoveAt(i);
            }

            if (length == m_Scenes.Length)
                return;

            var result = new EditorBuildSettingsScene[length];
            Array.Copy(m_Scenes, result, length);
            m_Scenes = result;
            EditorUtility.SetDirty(this);
            return;

            void RemoveAt(int index)
            {
                length--;
                Array.Copy(m_Scenes, index + 1, m_Scenes, index, length - index);
            }
        }
    }
}
