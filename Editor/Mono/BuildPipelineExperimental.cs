// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Experimental
{
    public static class BuildOptionsExperimental
    {
        public static readonly BuildOptions DatalessPlayer = BuildOptions.Reserved1;
    }

    public static class BuildPipelineExperimental
    {
        public static string GetSessionIdForBuildTarget(BuildTarget target)
        {
            return BuildPipeline.GetSessionIdForBuildTarget(target);
        }
    }
}
