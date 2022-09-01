// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Experimental
{
    [Obsolete("BuildPipelineExperimental is no longer supported and will be removed")]
    public static class BuildPipelineExperimental
    {
        public static string GetSessionIdForBuildTarget(BuildTarget target)
        {
            return BuildPipeline.GetSessionIdForBuildTarget(target, 0);
        }
    }
}
