// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.Build
{
    ///<summary>Attribute to provide a version number for IProcessSceneWithReport callbacks.</summary>
    ///<remarks>This attribute is used to inform the build system when the callback implementation changes and the build result needs to be regenerated. Therefore, developers should bump the version number when changing the implementation of the callback. If the attribute is not specified, the implied version number is 1.</remarks>
    ///<seealso cref="Build.IProcessSceneWithReport" />
    ///<seealso cref="EditorBuildSettings.UseParallelAssetBundleBuilding" />
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class BuildCallbackVersionAttribute : System.Attribute
    {
        ///<summary>Version number.</summary>
        public uint Version { get; internal set; }

        ///<summary>Constructor taking the version number for the callback.</summary>
        ///<param name="version">Version number.</param>
        public BuildCallbackVersionAttribute(uint version)
        {
            Version = version;
        }
    }

    ///<summary>Defines the build context for IProcessSceneWithReport during a build event.</summary>
    ///<remarks>This class contains static methods for declaring additional scene dependencies for the build system. These dependencies are used to trigger scene rebuilds in cases where dependencies are not explicit in the scene itself.
    ///
    ///                For example, if the implementation of <see cref="Build.IProcessSceneWithReport" /> loads an Asset programmatically then <see cref="Build.BuildPipelineContext.DependOnAsset" /> should be called, unless the same Asset is also referenced by the Scene.
    ///                Then, if the Asset is changed and the build run again, Unity will retrigger the callback and save the latest scene state instead of reusing an out-of-date cached result.
    ///
    ///                Dependency tracking applies to <see cref="BuildPipeline.BuildContentDirectory" />. It does not apply to <see cref="BuildPipeline.BuildPlayer" /> or <see cref="BuildPipeline.BuildAssetBundles" /> .</remarks>
    ///<seealso cref="AssetDatabase.LoadAssetAtPath" />
    [NativeHeader("Modules/ContentBuild/Editor/Ucbp/BuildPipelineContext.h")]
    [StaticAccessor("BuildPipelineContext", StaticAccessorType.DoubleColon)]
    public static class BuildPipelineContext
    {
        ///<summary>Allows you to specify that a Scene depends on contents of a source asset at the provided path.</summary>
        ///<remarks>Scene rebuild will be triggered if either of the conditions are true:
        /// * The asset at the path changes
        /// * The asset at the path moves.</remarks>
        ///<param name="path">The path of the dependency.</param>
        public extern static void DependOnPath(string path);
        ///<summary>Allows you to specify that a Scene depends on contents of an asset directly provided.</summary>
        ///<remarks>Scene rebuild will be triggered if the condition is true:
        /// * if the asset or any of its dependencies changes.</remarks>
        ///<param name="asset">The Unity Object from an asset.</param>
        public extern static void DependOnAsset([NotNull] Object asset);
    }
}
