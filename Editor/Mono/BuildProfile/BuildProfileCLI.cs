// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor.Build.Profile
{
    internal static class BuildProfileCLI
    {
        [RequiredByNativeCode]
        internal static bool SetActiveBuildProfileFromPath(string buildProfilePath)
        {
            if (LoadFromPath(buildProfilePath, out BuildProfile buildProfile))
            {
                BuildProfileContext.instance.activeProfile = buildProfile;
                return true;
            }

            return false;
        }

        static bool LoadFromPath(string buildProfilePath, out BuildProfile buildProfile)
        {
            if (AssetDatabase.AssetPathExists(buildProfilePath))
            {
                buildProfile = AssetDatabase.LoadAssetAtPath<BuildProfile>(buildProfilePath);
            }
            else
            {
                buildProfile = null;
                Debug.LogError($"Couldn't find build profile asset for path {buildProfilePath}");
            }

            return buildProfile != null;
        }

        [RequiredByNativeCode]
        static void BuildActiveProfileWithPath(string locationPathName)
        {
            var options = new BuildPlayerWithProfileOptions()
            {
                buildProfile = BuildProfile.GetActiveBuildProfile(),
                locationPathName = locationPathName,
                options = BuildOptions.None
            };

            BuildPipeline.BuildPlayer(options);
        }
    }
}
