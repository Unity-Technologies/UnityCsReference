// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Modules;
using UnityEngine;

namespace UnityEditor.Build.Profile
{
    internal sealed partial class BuildProfile
    {
        internal static BuildProfile CreateInstance(BuildTarget buildTarget, StandaloneBuildSubtarget subtarget)
        {
            string moduleName = ModuleManager.GetTargetStringFrom(buildTarget);
            IBuildProfileExtension buildProfileExtension = ModuleManager.GetBuildProfileExtension(moduleName);
            if (buildProfileExtension == null)
            {
                throw new ArgumentException(
                    $"Attempted to create a build profile for a platform that does not have a build profile extension or is not installed. Platform: {buildTarget}");
            }

            var buildProfile = ScriptableObject.CreateInstance<BuildProfile>();
            buildProfile.buildTarget = buildTarget;
            buildProfile.subtarget = subtarget;
            buildProfile.moduleName = moduleName;
            buildProfile.platformBuildProfile = buildProfileExtension.CreateBuildProfilePlatformSettings();
            return buildProfile;
        }
    }
}
