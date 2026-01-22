// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.UnityLinker
{
    /// <summary>
    /// Contains information for various IUnityLinkerProcessor callbacks.
    /// </summary>
    public sealed class UnityLinkerBuildPipelineData
    {
        ///<summary>The build target.</summary>
        public readonly BuildTarget target;
        ///<summary>The directory containing the assemblies that UnityLinker will process.</summary>
        [Obsolete("For platforms using the new incremental build pipeline, inputDirectory will no longer contain any files", false)]
        public readonly string inputDirectory;

        ///<summary>Creates a new instance of an UnityLinkerBuildPipelineData.</summary>
        public UnityLinkerBuildPipelineData(BuildTarget target, string inputDirectory)
        {
            this.target = target;
#pragma warning disable 618
            this.inputDirectory = inputDirectory;
#pragma warning restore
        }
    }
}
