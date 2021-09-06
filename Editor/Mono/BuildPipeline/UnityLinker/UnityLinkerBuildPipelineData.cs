// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.UnityLinker
{
    /// <summary>
    /// Data exposed during IRunUnityLinker callbacks
    /// </summary>
    public sealed class UnityLinkerBuildPipelineData
    {
        public readonly BuildTarget target;
        [Obsolete("For platforms using the new incremental build pipeline, inputDirectory will no longer contain any files", false)]
        public readonly string inputDirectory;

        public UnityLinkerBuildPipelineData(BuildTarget target, string inputDirectory)
        {
            this.target = target;
#pragma warning disable 618
            this.inputDirectory = inputDirectory;
#pragma warning restore
        }
    }
}
