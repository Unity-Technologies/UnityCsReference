// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.UnityLinker
{
    /// <summary>
    /// Data exposed during IRunUnityLinker callbacks
    /// </summary>
    public sealed class UnityLinkerBuildPipelineData
    {
        public readonly BuildTarget target;
        public readonly string inputDirectory;

        public UnityLinkerBuildPipelineData(BuildTarget target, string inputDirectory)
        {
            this.target = target;
            this.inputDirectory = inputDirectory;
        }
    }
}
