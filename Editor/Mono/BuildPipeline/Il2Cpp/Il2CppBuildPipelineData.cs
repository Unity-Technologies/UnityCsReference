// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Il2Cpp
{
    /// <summary>
    /// Data exposed during IRunIL2CPP callbacks
    /// </summary>
    public sealed class Il2CppBuildPipelineData
    {
        public readonly BuildTarget target;
        public readonly string inputDirectory;

        public Il2CppBuildPipelineData(BuildTarget target, string inputDirectory)
        {
            this.target = target;
            this.inputDirectory = inputDirectory;
        }
    }
}
