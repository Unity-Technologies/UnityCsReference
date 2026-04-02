// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Build.Profile;

namespace UnityEditor
{
    ///<summary>Provide various options to control the behavior of <see cref="BuildPipeline.BuildPlayer" /> when using a build profile.</summary>
    public struct BuildPlayerWithProfileOptions
    {
        ///<summary>The build profile to build.</summary>
        ///<seealso cref="BuildProfile" />
        public BuildProfile buildProfile { get; set; }
        ///<summary>The path where the application will be built.</summary>
        ///<remarks>The path you specify must end with the correct extension for the file format you will build. This path won't be changed by Unity. Set the file format using <see cref="EditorUserBuildSettings.buildAppBundle" />. 
        ///
        ///
        ///* If your file is an Android Package, end <c>locationPathName</c> with <c>.apk</c>.
        /// 
        ///* If your file is an Android App Bundle, end <c>locationPathName</c> with <c>.aab</c>.</remarks>
        ///<seealso cref="EditorUserBuildSettings.GetBuildLocation" />
        ///<seealso href="xref:um-build-path-requirements">Build path requirements for target platforms</seealso>
        public string locationPathName { get; set; }
        ///<summary>The path to a manifest file describing all the AssetBundles used in the build (optional).</summary>
        ///<remarks>When you call <see cref="BuildPipeline.BuildAssetBundles" /> to create your AssetBundles, Unity will also generate a .manifest file with a file name matching the parent directory.
        ///You can assign the path to this manifest file to <c>assetBundleManifestPath</c> to ensure that a player build doesn't strip any types used in your AssetBundles.
        ///
        ///You don't need to set this property when using <c>link.xml</c> files, or when generating AssetBundles using the &lt;a href="https://docs.unity3d.com/Packages/com.unity.addressables@latest"&gt;Addressables&lt;/a&gt; package.
        ///
        ///For more information about code stripping, refer to [Managed code stripping](xref:um-managed-code-stripping).</remarks>
        public string assetBundleManifestPath { get; set; }
        ///<summary>Additional <see cref="BuildOptions" />, like whether to run the built player.</summary>
        public BuildOptions options { get; set; }
    }
}
