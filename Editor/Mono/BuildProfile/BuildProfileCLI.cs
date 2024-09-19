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
