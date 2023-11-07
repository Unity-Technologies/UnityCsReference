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
        [SerializeField] BuildTarget m_BuildTarget;
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
    }
}
