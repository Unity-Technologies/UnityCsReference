// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;

namespace UnityEditor
{
    ///<summary>Provide various options to control the behavior of <see cref="BuildPipeline.BuildAssetBundles" />.</summary>
    ///<seealso cref="BuildPipeline" />
    ///<seealso cref="EditorUserBuildSettings" />
    ///<seealso cref="EditorBuildSettings" />
    ///<seealso cref="BuildPlayerOptions" />
    [StructLayout(LayoutKind.Sequential)]
    public struct BuildAssetBundlesParameters
    {
        ///<summary>Output path for the AssetBundles.</summary>
        ///<remarks>This can be an absolute path, or a path relative to the current project.  This folder is not created automatically and <see cref="BuildPipeline.BuildAssetBundles" /> fails if it doesn't already exist.</remarks>
        public string outputPath { get; set; }
        ///<summary>Array defining the name and contents of each AssetBundle. (optional)</summary>
        ///<remarks>An array of <see cref="AssetBundleBuild" /> structs that define the names and contents of each AssetBundle, e.g. the "Build Map".
        ///                    When provided Unity builds only the AssetBundles as specified, and ignores any AssetBundle assignments that had been made in the Editor user interface.
        ///                    This approach makes it convenient to programmatically define AssetBundle contents or to perform builds with different content within the same project.
        ///
        ///                    This field can be left unassigned, e.g. null, in which case <see cref="BuildPipeline.BuildAssetBundles" /> uses the AssetBundle assignments
        ///                    made in the Editor to determine the AssetBundles and their contents.  Those assignments are stored in the AssetDatabase and in the .meta
        ///                    files and can also be accessed programmatically, see <see cref="AssetImporter.assetBundleName" />, <see cref="AssetDatabase.GetAssetPathsFromAssetBundle" /> and <see cref="AssetDatabase.GetImplicitAssetBundleName" />.</remarks>
        public AssetBundleBuild[] bundleDefinitions { get; set; }
        ///<summary>Flags from the <see cref="BuildAssetBundleOptions" /> enum. (optional)</summary>
        public BuildAssetBundleOptions options { get; set; }
        ///<summary>The <see cref="BuildTarget" /> to build. (optional)</summary>
        ///<remarks>An AssetBundle is only compatible with the specific platform that it was built for, so you must produce different builds of a given bundle to use
        ///                    the assets on different platforms.
        ///
        ///                    If targetPlatform is not specified, e.g. it is 0, then the targetPlatform and subtarget fields will all be determined from the current build settings.</remarks>
        ///<seealso cref="EditorUserBuildSettings.activeBuildTarget" />
        public BuildTarget targetPlatform { get; set; }
        ///<summary>The subtarget to build. (optional)</summary>
        ///<remarks>For certain platforms, setting a non-zero value for this property can modify the build behaviour.
        ///
        ///Both this property and <see cref="BuildAssetBundlesParameters.targetPlatform" /> should remain unset to automatically use the target and subtarget as defined in the current Build Profile.
        ///
        ///The valid values for this property correspond to target-specific enums, which are cast to ints.  Examples of these enums include <see cref="StandaloneBuildSubtarget" />, <see cref="MobileTextureSubtarget" />, <see cref="WebGLTextureSubtarget" /> and <see cref="XboxBuildSubtarget" />.
        ///
        ///Usage examples:
        ///
        ///* For building a headless server, when building for <see cref="BuildTarget.StandaloneWindows" />, <see cref="BuildTarget.StandaloneOSX" /> or <see cref="BuildTarget.StandaloneLinux64" />, specify <see cref="StandaloneBuildSubtarget.Server" /> (numeric value 1).
        ///* For ETC2 texture compression, when building for <see cref="BuildTarget.Android" />, or another mobile platform, specify <see cref="MobileTextureSubtarget.ETC2" /> (numeric value 5).</remarks>
        ///<seealso cref="BuildPlayerOptions.subtarget" />
        ///<seealso cref="EditorUserBuildSettings.standaloneBuildSubtarget" />
        ///<seealso cref="EditorUserBuildSettings.androidBuildSubtarget" />
        ///<seealso cref="EditorUserBuildSettings.webGLBuildSubtarget" />
        ///<seealso cref="EditorUserBuildSettings.ps4BuildSubtarget" />
        public int subtarget { get; set; }
        ///<summary>User-specified preprocessor defines used while compiling assemblies during the AssetBundle build. (optional)</summary>
        ///<remarks>Preprocessor defines may be used to exclude serialized fields from class definitions, so this can be have an influence on how objects are serialized during the build process.
        ///                    Typically value passed here should match any extra scripting defines passed during the player build.</remarks>
        ///<seealso cref="BuildPlayerOptions.extraScriptingDefines" />
        public string[] extraScriptingDefines { get; set; }
    }
}
