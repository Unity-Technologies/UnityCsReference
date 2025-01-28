// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor.Build.Reporting
{
    ///<summary>This class provides constant values for some of the common roles used by files in the build. The role of each file in the build is available in <see cref="BuildFile.role" />.</summary>
    [NativeType(Header = "Modules/BuildReportingEditor/Public/CommonRoles.h")]
    public static class CommonRoles
    {
        ///<summary>The <see cref="BuildFile.role" /> value of a file that contains the packed content of a Scene.</summary>
        [NativeProperty("BuildReporting::CommonRoles::scene", true, TargetType.Field)]
        public static extern string scene { get; }

        ///<summary>The <see cref="BuildFile.role" /> value of a file that contains asset objects which are shared between Scenes. Examples of asset objects are textures, models, and audio.</summary>
        [NativeProperty("BuildReporting::CommonRoles::sharedAssets", true, TargetType.Field)]
        public static extern string sharedAssets { get; }

        ///<summary>The <see cref="BuildFile.role" /> value of the file that contains the contents of the project's "Resources" folder, packed into a single file.</summary>
        [NativeProperty("BuildReporting::CommonRoles::resourcesFile", true, TargetType.Field)]
        public static extern string resourcesFile { get; }

        ///<summary>The <see cref="BuildFile.role" /> value of built AssetBundle files.</summary>
        [NativeProperty("BuildReporting::CommonRoles::assetBundle", true, TargetType.Field)]
        public static extern string assetBundle { get; }

        ///<summary>The <see cref="BuildFile.role" /> value of a manifest AssetBundle, which is an AssetBundle that contains information about other AssetBundles and their dependencies.</summary>
        [NativeProperty("BuildReporting::CommonRoles::manifestAssetBundle", true, TargetType.Field)]
        public static extern string manifestAssetBundle { get; }

        ///<summary>The <see cref="BuildFile.role" /> value of an AssetBundle manifest file, produced during the build process, that contains information about the bundle and its dependencies.</summary>
        [NativeProperty("BuildReporting::CommonRoles::assetBundleTextManifest", true, TargetType.Field)]
        public static extern string assetBundleTextManifest { get; }

        ///<summary>The <see cref="BuildFile.role" /> value of a managed assembly, containing compiled script code.</summary>
        [NativeProperty("BuildReporting::CommonRoles::managedLibrary", true, TargetType.Field)]
        public static extern string managedLibrary { get; }

        ///<summary>The <see cref="BuildFile.role" /> value of a managed library that is present in the build due to being a dependency of a <see cref="CommonRoles.managedLibrary" />.</summary>
        [NativeProperty("BuildReporting::CommonRoles::dependentManagedLibrary", true, TargetType.Field)]
        public static extern string dependentManagedLibrary { get; }

        ///<summary>The <see cref="BuildFile.role" /> value of an executable - the file that will actually be launched on the target device.</summary>
        [NativeProperty("BuildReporting::CommonRoles::executable", true, TargetType.Field)]
        public static extern string executable { get; }

        ///<summary>The <see cref="BuildFile.role" /> value of a file that contains streaming resource data.</summary>
        ///<remarks>For example, when a texture is packed into a build, only metadata about the texture is packed into the <see cref="CommonRoles.sharedAssets" /> file - the actual content of the texture is packed into a streamingResourceFile, where it can be streamed into memory asynchronously at runtime.</remarks>
        [NativeProperty("BuildReporting::CommonRoles::streamingResourceFile", true, TargetType.Field)]
        public static extern string streamingResourceFile { get; }

        ///<summary>The <see cref="BuildFile.role" /> value of files that have been copied into the build without modification from the StreamingAssets folder in the project.</summary>
        [NativeProperty("BuildReporting::CommonRoles::streamingAsset", true, TargetType.Field)]
        public static extern string streamingAsset { get; }

        ///<summary>The <see cref="BuildFile.role" /> value of the file that contains configuration information for the very early stages of engine startup.</summary>
        [NativeProperty("BuildReporting::CommonRoles::bootConfig", true, TargetType.Field)]
        public static extern string bootConfig { get; }

        ///<summary>The <see cref="BuildFile.role" /> value of the file that contains built-in resources for the engine.</summary>
        [NativeProperty("BuildReporting::CommonRoles::builtInResources", true, TargetType.Field)]
        public static extern string builtInResources { get; }

        ///<summary>The <see cref="BuildFile.role" /> value of the file that contains Unity's built-in shaders, such as the Standard shader.</summary>
        [NativeProperty("BuildReporting::CommonRoles::builtInShaders", true, TargetType.Field)]
        public static extern string builtInShaders { get; }

        ///<summary>The <see cref="BuildFile.role" /> value of the file that provides config information used in Low Integrity mode on Windows.</summary>
        [NativeProperty("BuildReporting::CommonRoles::appInfo", true, TargetType.Field)]
        public static extern string appInfo { get; }

        ///<summary>The <see cref="BuildFile.role" /> value of files that provide the managed API for Unity.</summary>
        ///<remarks>These files are referenced by your compiled script DLL and provide the interface layer between your scripts and the engine itself.</remarks>
        [NativeProperty("BuildReporting::CommonRoles::managedEngineAPI", true, TargetType.Field)]
        public static extern string managedEngineApi { get; }

        ///<summary>The <see cref="BuildFile.role" /> value of files that make up the Mono runtime itself.</summary>
        [NativeProperty("BuildReporting::CommonRoles::monoRuntime", true, TargetType.Field)]
        public static extern string monoRuntime { get; }

        ///<summary>The <see cref="BuildFile.role" /> value of files that are used as configuration data by the Mono runtime.</summary>
        [NativeProperty("BuildReporting::CommonRoles::monoConfig", true, TargetType.Field)]
        public static extern string monoConfig { get; }

        ///<summary>The <see cref="BuildFile.role" /> value of files that contain information for debuggers.</summary>
        ///<remarks>These files include debug information, such as symbols, for both managed assemblies and native executables and libraries.</remarks>
        [NativeProperty("BuildReporting::CommonRoles::debugInfo", true, TargetType.Field)]
        public static extern string debugInfo { get; }

        ///<summary>The <see cref="BuildFile.role" /> value of the file that contains global Project Settings data for the player.</summary>
        [NativeProperty("BuildReporting::CommonRoles::globalGameManagers", true, TargetType.Field)]
        public static extern string globalGameManagers { get; }

        ///<summary>The <see cref="BuildFile.role" /> value of the executable that is used to capture crashes from the player.</summary>
        [NativeProperty("BuildReporting::CommonRoles::crashHandler", true, TargetType.Field)]
        public static extern string crashHandler { get; }

        ///<summary>The <see cref="BuildFile.role" /> value of the main Unity runtime when it is built as a separate library.</summary>
        ///<remarks>On most platforms, the Unity runtime is also the main executable for the player, but on some platforms it is split into a separate file, allowing the main executable to be smaller and more easily customized.</remarks>
        [NativeProperty("BuildReporting::CommonRoles::engineLibrary", true, TargetType.Field)]
        public static extern string engineLibrary { get; }
    }
}
