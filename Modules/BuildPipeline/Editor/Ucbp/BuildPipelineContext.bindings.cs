// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.Build
{
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class BuildCallbackVersionAttribute : System.Attribute
    {
        public uint Version { get; internal set; }

        public BuildCallbackVersionAttribute(uint version)
        {
            Version = version;
        }
    }

    [NativeHeader("Modules/BuildPipeline/Editor/Ucbp/BuildPipelineContext.h")]
    [StaticAccessor("BuildPipelineContext", StaticAccessorType.DoubleColon)]
    public static class BuildPipelineContext
    {
        public extern static void DependOnPath(string path);
        public extern static void DependOnAsset([NotNull] Object asset);
    }
}
