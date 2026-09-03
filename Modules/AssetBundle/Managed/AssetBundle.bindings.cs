// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngineInternal;

namespace UnityEngine
{
    ///<summary>The result of an Asset Bundle Load or Recompress Operation.</summary>
    public enum AssetBundleLoadResult
    {
        ///<summary>The operation completed successfully.</summary>
        Success,
        ///<summary>The operation was cancelled.</summary>
        Cancelled,
        ///<summary>The decompressed Asset data did not match the precomputed CRC. This may suggest that the AssetBundle did not download correctly.</summary>
        NotMatchingCrc,
        ///<summary>The Asset Bundle was not successfully cached.</summary>
        FailedCache,
        ///<summary>This does not appear to be a valid Asset Bundle.</summary>
        NotValidAssetBundle,
        ///<summary>The Asset Bundle does not contain any serialized data. It may be empty, or corrupt.</summary>
        NoSerializedData,
        ///<summary>The AssetBundle is incompatible with this version of Unity.</summary>
        NotCompatible,
        ///<summary>The Asset Bundle is already loaded.</summary>
        AlreadyLoaded,
        ///<summary>Failed to read the Asset Bundle file.</summary>
        FailedRead,
        ///<summary>Failed to decompress the Asset Bundle.</summary>
        FailedDecompression,
        ///<summary>Failed to write to the file system.</summary>
        FailedWrite,
        ///<summary>The target path given for the Recompression operation could not be deleted for swap with recompressed bundle file.</summary>
        FailedDeleteRecompressionTarget,
        ///<summary>The target path given for the Recompression operation is an Archive that is currently loaded.</summary>
        RecompressionTargetIsLoaded,
        ///<summary>The target path given for the Recompression operation exists but is not an Archive container.</summary>
        RecompressionTargetExistsButNotArchive
    }

    ///<summary>API for accessing the content of AssetBundle files.</summary>
    ///<remarks>
    ///  <para>This class exposes an API, via static methods, for loading and managing AssetBundles.
    ///
    ///This same class offers non-static methods and properties that expose the contents of a specific loaded AssetBundle, including the ability to load an Asset from within an AssetBundle.
    ///
    ///Create AssetBundles by calling <see cref="M:UnityEditor.BuildPipeline.BuildAssetBundles" /> or using the &lt;a href="http://docs.unity3d.com/Packages/com.unity.addressables@latest/index.html"&gt;Addressables package&lt;/a&gt;.
    ///The build process generates one or more AssetBundle files, and each AssetBundle file contains a serialized instance of this class.
    ///
    ///</para>
    ///  <para>**Scenes inside AssetBundles**
    ///
    ///* An AssetBundle can contain scenes or assets, but not a mix of both types.
    ///* <see cref="AssetBundle.LoadAsset" />, and the other Load methods, do not support loading scenes from AssetBundles.
    ///* Scenes can be loaded from AssetBundles using the <see cref="T:UnityEngine.SceneManagement.SceneManager" />.  When running in the Player, or Play mode in the Editor, first load the AssetBundle containing scenes.  Then call <see cref="M:UnityEngine.SceneManagement.SceneManager.LoadScene" /> or <see cref="M:UnityEngine.SceneManagement.SceneManager.LoadSceneAsync" /> with the scene path or name.
    ///* When the Editor is in Edit mode, it does not support loading scenes from AssetBundles. Calls to <see cref="M:UnityEditor.SceneManagement.EditorSceneManager.OpenScene" /> with the path of a scene inside a loaded AssetBundle fail and log an error stating that the scene file is not found.</para>
    ///</remarks>
    ///<example>
    ///  <code><![CDATA[
    ///using System.Collections;
    ///using UnityEngine;
    ///using UnityEngine.Networking;
    ///
    ///public class SampleBehaviour : MonoBehaviour
    ///{
    ///    IEnumerator Start()
    ///    {
    ///        var uwr = UnityWebRequestAssetBundle.GetAssetBundle("https://myserver/myBundle.unity3d");
    ///        yield return uwr.SendWebRequest();
    ///
    ///        // Get an asset from the bundle and instantiate it.
    ///        AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(uwr);
    ///        var loadAsset = bundle.LoadAssetAsync<GameObject>("Assets/Players/MainPlayer.prefab");
    ///        yield return loadAsset;
    ///
    ///        Instantiate(loadAsset.asset);
    ///
    ///        bundle.Unload(true);
    ///    }
    ///}
    ///]]></code>
    ///</example>
    ///<example>
    ///  <code><![CDATA[
    /// //This example shows how to build a scene into an AssetBundle, and then build a Player with that AssetBundle included.
    /// //When the Player starts it loads the scene and then unloads after a few seconds.
    /// //
    /// //To try this example:
    /// // - Save it into a file, for example "Assets/AssetBundleSceneLoader.cs".  The source file name needs to match the name of the MonoBehaviour.
    /// // - From the Editor Menu select "Example" / "Scene in AssetBundle Example".
    /// //
    /// //It is also possible to try it in Play mode in the Editor:
    /// // - Run the menu at least once to create the scenes and AssetBundle
    /// // - Open "Assets/Scenes/StartingScene.unity"
    /// // - Enter Play mode
    ///
    ///using System.IO;
    ///using System.Collections;
    ///using UnityEngine;
    ///using UnityEngine.SceneManagement;
    ///
    ///#if UNITY_EDITOR
    ///using UnityEditor;
    ///using UnityEditor.Build.Reporting;
    ///using UnityEditor.SceneManagement;
    ///#endif
    ///
    ///public class Constants
    ///{
    ///    // Scene in the project that is intended for an AssetBundle
    ///    public static readonly string SceneForAssetBundle = "Assets/Scenes/SceneForBundle.unity";
    ///
    ///    // Scene for the Player build that contains the "AssetBundleSceneLoader" MonoBehaviour
    ///    public static readonly string StartingSceneForPlayer = "Assets/Scenes/StartingScene.unity";
    ///
    ///    // Note: AssetBundles are always created lower case
    ///    public static readonly string AssetBundleFileName = "scenebundle";
    ///
    ///    // Path for AssetBundle (relative to the StreamingAsset location)
    ///    public static readonly string AssetBundlePath = "/AssetBundles";
    ///
    ///    // Output directory for the player build (Relative to project and not inside Assets)
    ///    public static readonly string PlayerBuildPath = "PlayerBuild";
    ///
    ///    // Name of the player executable inside PlayerBuildPath
    ///    public static readonly string PlayerExecutable = "PlayerBuild";
    ///}
    ///
    ///#if UNITY_EDITOR
    /// // Note: Typically this would be in its own source file, in an Editor-only assembly.
    ///public class BuildBundleWithScene
    ///{
    ///    [MenuItem("Example/Scene in AssetBundle Example")]
    ///    public static void BuildAssetBundle()
    ///    {
    ///        // Location inside StreamingAssets so the AssetBundle content is included in the Player
    ///        string AssetBundleBuildPath = Application.streamingAssetsPath + Constants.AssetBundlePath;
    ///
    ///        // Create the content expected by this example
    ///        CreateStartingScene();
    ///        CreateSceneForAssetBundle();
    ///
    ///        var buildTargetPlatform = EditorUserBuildSettings.activeBuildTarget;
    ///
    ///        if (!Directory.Exists(AssetBundleBuildPath))
    ///            Directory.CreateDirectory(AssetBundleBuildPath);
    ///
    ///        // Define an AssetBundle containing the Scene
    ///        var bundleContents = new AssetBundleBuild[]
    ///        {
    ///            new AssetBundleBuild()
    ///            {
    ///                assetBundleName = Constants.AssetBundleFileName,
    ///                assetNames = new string[]
    ///                {
    ///                    Constants.SceneForAssetBundle
    ///                }
    ///            }
    ///        };
    ///
    ///        var buildAssetBundlesParameters = new BuildAssetBundlesParameters()
    ///        {
    ///            targetPlatform = buildTargetPlatform,
    ///            bundleDefinitions = bundleContents,
    ///            outputPath = AssetBundleBuildPath
    ///        };
    ///        BuildPipeline.BuildAssetBundles(buildAssetBundlesParameters);
    ///
    ///        var buildReport = BuildReport.GetLatestReport();
    ///        if (buildReport.summary.result != BuildResult.Succeeded)
    ///        {
    ///            Debug.Log("AssetBundle Build failed.");
    ///            return;
    ///        }
    ///
    ///        // Perform a Player build.  It will include the content of the
    ///        // StreamingAssets folder.
    ///        if (!Directory.Exists(Constants.PlayerBuildPath))
    ///            Directory.CreateDirectory(Constants.PlayerBuildPath);
    ///
    ///        var buildOutput = Constants.PlayerBuildPath + "/" + Constants.PlayerExecutable;
    ///        if (buildTargetPlatform == BuildTarget.StandaloneWindows64)
    ///            buildOutput += ".exe";
    ///
    ///        var buildPlayerParameters = new BuildPlayerOptions()
    ///        {
    ///            scenes = new string[] { Constants.StartingSceneForPlayer },
    ///            target = buildTargetPlatform,
    ///            locationPathName = buildOutput,
    ///            options = BuildOptions.Development | BuildOptions.AutoRunPlayer,
    ///            assetBundleManifestPath = AssetBundleBuildPath + "/AssetBundles.manifest"
    ///        };
    ///
    ///        if (buildTargetPlatform == BuildTarget.StandaloneWindows64)
    ///            buildPlayerParameters.locationPathName += ".exe";
    ///
    ///        var playerBuildReport = BuildPipeline.BuildPlayer(buildPlayerParameters);
    ///        if (playerBuildReport.summary.result != BuildResult.Succeeded)
    ///        {
    ///            Debug.Log($"Player Build failed. {playerBuildReport.SummarizeErrors()}");
    ///            return;
    ///        }
    ///    }
    ///
    ///    static void CreateStartingScene()
    ///    {
    ///        var startingScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
    ///        var go = new GameObject();
    ///        go.AddComponent<AssetBundleSceneLoader>();
    ///        GameObject.CreatePrimitive(PrimitiveType.Sphere);
    ///        EditorSceneManager.SaveScene(startingScene, Constants.StartingSceneForPlayer);
    ///    }
    ///
    ///    static void CreateSceneForAssetBundle()
    ///    {
    ///        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
    ///        GameObject.CreatePrimitive(PrimitiveType.Cube);
    ///        EditorSceneManager.SaveScene(scene, Constants.SceneForAssetBundle);
    ///    }
    ///}
    ///#endif
    ///
    /// // MonoBehaviour that is included in the starting scene.
    ///public class AssetBundleSceneLoader : MonoBehaviour
    ///{
    ///    AssetBundle sceneBundle = null;
    ///    bool sceneLoaded = false;
    ///
    ///    // Triggered when the scene containing this MonoBehaviour is loaded
    ///    void Start()
    ///    {
    ///        StartCoroutine(LoadAssetBundleAndScene());
    ///        StartCoroutine(CleanupAfterDelay());
    ///    }
    ///
    ///    IEnumerator LoadAssetBundleAndScene()
    ///    {
    ///        // Determine the path to the AssetBundle.
    ///        // Application.streamingAssetsPath is used so that this works in both the Player and Play mode in the Editor.
    ///        string AssetBundleBuildPath = Application.streamingAssetsPath + Constants.AssetBundlePath;
    ///        var bundlePath = AssetBundleBuildPath + "/" + Constants.AssetBundleFileName;
    ///
    ///        var op = AssetBundle.LoadFromFileAsync(bundlePath);
    ///        yield return op;
    ///
    ///        sceneBundle = op.assetBundle;
    ///        if (sceneBundle == null)
    ///        {
    ///            Debug.LogError("Failed to load AssetBundle: " + Constants.AssetBundleFileName);
    ///        }
    ///        else
    ///        {
    ///            var sceneLoadOp = SceneManager.LoadSceneAsync(Constants.SceneForAssetBundle, LoadSceneMode.Additive);
    ///
    ///            if (sceneLoadOp == null)
    ///                Debug.Log($"Failed to load {Constants.SceneForAssetBundle}");
    ///            else
    ///            {
    ///                yield return sceneLoadOp;
    ///                Scene sceneLookup = SceneManager.GetSceneByPath(Constants.SceneForAssetBundle);
    ///
    ///                //Will report "Finished loading SceneForBundle (index -1)."
    ///                Debug.Log($"Finished loading {sceneLookup.name} (index {sceneLookup.buildIndex}).");
    ///                sceneLoaded = true;
    ///            }
    ///        }
    ///    }
    ///
    ///    IEnumerator CleanupAfterDelay()
    ///    {
    ///        yield return new WaitForSeconds(3.0f);
    ///
    ///        if (sceneLoaded)
    ///            yield return SceneManager.UnloadSceneAsync(Constants.SceneForAssetBundle);
    ///        sceneLoaded = false;
    ///
    ///        if (sceneBundle != null)
    ///            yield return sceneBundle.UnloadAsync(true);
    ///        sceneBundle = null;
    ///
    ///        Debug.Log("Finished unloading Content");
    ///    }
    ///}
    ///]]></code>
    ///</example>
    ///<seealso href="xref:AssetBundlesIntro">Intro to AssetBundles</seealso>
    ///<seealso cref="M:UnityEngine.Networking.UnityWebRequestAssetBundle.GetAssetBundle" />
    ///<seealso cref="M:UnityEditor.BuildPipeline.BuildAssetBundles" />
    [NativeHeader("Modules/AssetBundle/Public/AssetBundleLoadFromFileAsyncOperation.h")]
    [NativeHeader("Modules/AssetBundle/Public/AssetBundleLoadFromMemoryAsyncOperation.h")]
    [NativeHeader("Modules/AssetBundle/Public/AssetBundleLoadFromManagedStreamAsyncOperation.h")]
    [NativeHeader("Modules/AssetBundle/Public/AssetBundleLoadAssetOperation.h")]
    [NativeHeader("Runtime/Scripting/ScriptingExportUtility.h")]
    [NativeHeader("Scripting/ScriptingUtility.h")]
    [NativeHeader("AssetBundleScriptingClasses.h")]
    [NativeHeader("Modules/AssetBundle/Public/AssetBundleSaveAndLoadHelper.h")]
    [NativeHeader("Modules/AssetBundle/Public/AssetBundleUtility.h")]
    [NativeHeader("Modules/AssetBundle/Public/AssetBundleLoadAssetUtility.h")]
    [global::UnityEngine.NativeClass("AssetBundle", PersistentTypeId = 142)]
    [ExcludeFromPreset]
    public partial class AssetBundle : Object
    {
        private AssetBundle() {}

        ///<exclude />
        [Obsolete("mainAsset has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.")]
        public Object mainAsset
        {
            get { return returnMainAsset(this); }
        }

        [FreeFunction("LoadMainObjectFromAssetBundle", true)]
        internal static extern Object returnMainAsset([NotNull] AssetBundle bundle);

        ///<summary>Unloads all currently loaded AssetBundles.</summary>
        ///<remarks>When <c>unloadAllObjects</c> is false, tracking data structures and any memory buffers holding content of the AssetBundle will be freed. But any instances of objects loaded from this bundle will remain intact.
        ///
        ///When <c>unloadAllObjects</c> is true, all objects that were loaded from the currently loaded bundles will be destroyed as well. If there are GameObjects in your Scene referencing those assets, the references to them will become missing.
        ///
        ///In either case you won't be able to load any more objects from the currently loaded bundles unless they are reloaded.
        ///
        ///**Note:** Passing a value of <c>false</c> for <c>unloadAllObjects</c> can cause unexpected behavior in the Editor. For example, the [Mip Map Streaming](xref:TextureStreaming) system might still reference textures loaded from a bundle after exiting play mode. This means when the Mip Map streaming system tries to update each texture's mipmaps, it can't access the unloaded bundle and displays errors in the console. To avoid this, use [conditional compilation](xref:platform-dependent-compilation) to pass <c>true</c> in the Editor, and <c>false</c> in builds.
        ///See [AssetBundles compression](xref:AssetBundles-Cache) for a description of the different compression formats used and their impact on memory while loaded.</remarks>
        ///<param name="unloadAllObjects">Determines whether the current instances of objects loaded from AssetBundles will also be unloaded.</param>
        ///<seealso cref="Unload" />
        ///<seealso cref="UnloadAsync" />
        ///<seealso href="xref:AssetBundles-Native">Using AssetBundles Natively</seealso>
        [FreeFunction("UnloadAllAssetBundles")]
        public extern static void UnloadAllAssetBundles(bool unloadAllObjects);

        [FreeFunction("GetAllAssetBundles")]
        internal extern static AssetBundle[] GetAllLoadedAssetBundles_Native();
        ///<summary>Get an enumeration of all the currently loaded AssetBundles.</summary>
        ///<returns>An enumeration of all the AssetBundles that are currently loaded.</returns>
        public static IEnumerable<AssetBundle> GetAllLoadedAssetBundles()
        {
            return GetAllLoadedAssetBundles_Native();
        }

        [FreeFunction("LoadFromFileAsync")]
        internal extern static AssetBundleCreateRequest LoadFromFileAsync_Internal(string path, uint crc, ulong offset);

        ///<summary>Asynchronously loads an AssetBundle from a file on disk.</summary>
        ///<remarks>
        ///  <para>The function supports bundles of any compression type.
        ///In case of **LZMA** compression, the data will be decompressed to the memory. See [AssetBundles compression](xref:AssetBundles-Cache) for more details.
        ///
        ///This is the fastest way to load an AssetBundle.</para>
        ///  <para />
        ///</remarks>
        ///<param name="path">Path of the file on disk.</param>
        ///<returns>Asynchronous load request for an AssetBundle. Use <see cref="AssetBundleCreateRequest.assetBundle" /> property to get an AssetBundle once it is loaded.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///using System.IO;
        ///
        ///public class LoadFromFileAsyncExample : MonoBehaviour
        ///{
        ///    IEnumerator Start()
        ///    {
        ///        var bundleLoadRequest = AssetBundle.LoadFromFileAsync(Path.Combine(Application.streamingAssetsPath, "myassetBundle"));
        ///        yield return bundleLoadRequest;
        ///
        ///        var myLoadedAssetBundle = bundleLoadRequest.assetBundle;
        ///        if (myLoadedAssetBundle == null)
        ///        {
        ///            Debug.Log("Failed to load AssetBundle!");
        ///            yield break;
        ///        }
        ///
        ///        var assetLoadRequest = myLoadedAssetBundle.LoadAssetAsync<GameObject>("MyObject");
        ///        yield return assetLoadRequest;
        ///
        ///        GameObject prefab = assetLoadRequest.asset as GameObject;
        ///        Instantiate(prefab);
        ///
        ///        myLoadedAssetBundle.Unload(false);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="AssetBundleCreateRequest" />
        ///<seealso cref="LoadFromFile" />
        public static AssetBundleCreateRequest LoadFromFileAsync(string path)
        {
            return LoadFromFileAsync_Internal(path, 0, 0);
        }

        ///<summary>Asynchronously loads an AssetBundle from a file on disk.</summary>
        ///<remarks>
        ///  <para>The function supports bundles of any compression type.
        ///In case of **LZMA** compression, the data will be decompressed to the memory. See [AssetBundles compression](xref:AssetBundles-Cache) for more details.
        ///
        ///This is the fastest way to load an AssetBundle.</para>
        ///  <para />
        ///</remarks>
        ///<param name="path">Path of the file on disk.</param>
        ///<param name="crc">An optional CRC-32 checksum of the uncompressed content. If this is non-zero, then the content will be compared against the checksum before loading it, and give an error if it does not match.</param>
        ///<returns>Asynchronous load request for an AssetBundle. Use <see cref="AssetBundleCreateRequest.assetBundle" /> property to get an AssetBundle once it is loaded.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///using System.IO;
        ///
        ///public class LoadFromFileAsyncExample : MonoBehaviour
        ///{
        ///    IEnumerator Start()
        ///    {
        ///        var bundleLoadRequest = AssetBundle.LoadFromFileAsync(Path.Combine(Application.streamingAssetsPath, "myassetBundle"));
        ///        yield return bundleLoadRequest;
        ///
        ///        var myLoadedAssetBundle = bundleLoadRequest.assetBundle;
        ///        if (myLoadedAssetBundle == null)
        ///        {
        ///            Debug.Log("Failed to load AssetBundle!");
        ///            yield break;
        ///        }
        ///
        ///        var assetLoadRequest = myLoadedAssetBundle.LoadAssetAsync<GameObject>("MyObject");
        ///        yield return assetLoadRequest;
        ///
        ///        GameObject prefab = assetLoadRequest.asset as GameObject;
        ///        Instantiate(prefab);
        ///
        ///        myLoadedAssetBundle.Unload(false);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="AssetBundleCreateRequest" />
        ///<seealso cref="LoadFromFile" />
        public static AssetBundleCreateRequest LoadFromFileAsync(string path, uint crc)
        {
            return LoadFromFileAsync_Internal(path, crc, 0);
        }

        ///<summary>Asynchronously loads an AssetBundle from a file on disk.</summary>
        ///<remarks>
        ///  <para>The function supports bundles of any compression type.
        ///In case of **LZMA** compression, the data will be decompressed to the memory. See [AssetBundles compression](xref:AssetBundles-Cache) for more details.
        ///
        ///This is the fastest way to load an AssetBundle.</para>
        ///  <para />
        ///</remarks>
        ///<param name="path">Path of the file on disk.</param>
        ///<param name="crc">An optional CRC-32 checksum of the uncompressed content. If this is non-zero, then the content will be compared against the checksum before loading it, and give an error if it does not match.</param>
        ///<param name="offset">An optional byte offset. This value specifies where to start reading the AssetBundle from.</param>
        ///<returns>Asynchronous load request for an AssetBundle. Use <see cref="AssetBundleCreateRequest.assetBundle" /> property to get an AssetBundle once it is loaded.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///using System.IO;
        ///
        ///public class LoadFromFileAsyncExample : MonoBehaviour
        ///{
        ///    IEnumerator Start()
        ///    {
        ///        var bundleLoadRequest = AssetBundle.LoadFromFileAsync(Path.Combine(Application.streamingAssetsPath, "myassetBundle"));
        ///        yield return bundleLoadRequest;
        ///
        ///        var myLoadedAssetBundle = bundleLoadRequest.assetBundle;
        ///        if (myLoadedAssetBundle == null)
        ///        {
        ///            Debug.Log("Failed to load AssetBundle!");
        ///            yield break;
        ///        }
        ///
        ///        var assetLoadRequest = myLoadedAssetBundle.LoadAssetAsync<GameObject>("MyObject");
        ///        yield return assetLoadRequest;
        ///
        ///        GameObject prefab = assetLoadRequest.asset as GameObject;
        ///        Instantiate(prefab);
        ///
        ///        myLoadedAssetBundle.Unload(false);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="AssetBundleCreateRequest" />
        ///<seealso cref="LoadFromFile" />
        public static AssetBundleCreateRequest LoadFromFileAsync(string path, uint crc, ulong offset)
        {
            return LoadFromFileAsync_Internal(path, crc, offset);
        }

        [FreeFunction("LoadFromFile")]
        internal extern static AssetBundle LoadFromFile_Internal(string path, uint crc, ulong offset);

        ///<summary>Synchronously loads an AssetBundle from a file on disk.</summary>
        ///<remarks>
        ///  <para>The function supports bundles of any compression type.
        ///In case of **LZMA** compression, the file content will be fully decompressed into memory and loaded from there. See [AssetBundles compression](xref:AssetBundles-Cache) for more details.
        ///
        ///Compared to <see cref="LoadFromFileAsync" />, this version is synchronous and will not return until it is done creating the AssetBundle object.
        ///
        ///This is the fastest way to load an AssetBundle.</para>
        ///  <para />
        ///</remarks>
        ///<param name="path">Path of the file on disk.</param>
        ///<returns>Loaded AssetBundle object or null if failed.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///using System.IO;
        ///
        ///public class LoadFromFileExample : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        var myLoadedAssetBundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "myassetBundle"));
        ///        if (myLoadedAssetBundle == null)
        ///        {
        ///            Debug.Log("Failed to load AssetBundle!");
        ///            return;
        ///        }
        ///
        ///        var prefab = myLoadedAssetBundle.LoadAsset<GameObject>("MyObject");
        ///        Instantiate(prefab);
        ///
        ///        myLoadedAssetBundle.Unload(false);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="AssetBundle" />
        ///<seealso cref="LoadFromFileAsync" />
        public static AssetBundle LoadFromFile(string path)
        {
            return LoadFromFile_Internal(path, 0, 0);
        }

        ///<summary>Synchronously loads an AssetBundle from a file on disk.</summary>
        ///<remarks>
        ///  <para>The function supports bundles of any compression type.
        ///In case of **LZMA** compression, the file content will be fully decompressed into memory and loaded from there. See [AssetBundles compression](xref:AssetBundles-Cache) for more details.
        ///
        ///Compared to <see cref="LoadFromFileAsync" />, this version is synchronous and will not return until it is done creating the AssetBundle object.
        ///
        ///This is the fastest way to load an AssetBundle.</para>
        ///  <para />
        ///</remarks>
        ///<param name="path">Path of the file on disk.</param>
        ///<param name="crc">An optional CRC-32 checksum of the uncompressed content. If this is non-zero, then the content will be compared against the checksum before loading it, and give an error if it does not match.</param>
        ///<returns>Loaded AssetBundle object or null if failed.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///using System.IO;
        ///
        ///public class LoadFromFileExample : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        var myLoadedAssetBundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "myassetBundle"));
        ///        if (myLoadedAssetBundle == null)
        ///        {
        ///            Debug.Log("Failed to load AssetBundle!");
        ///            return;
        ///        }
        ///
        ///        var prefab = myLoadedAssetBundle.LoadAsset<GameObject>("MyObject");
        ///        Instantiate(prefab);
        ///
        ///        myLoadedAssetBundle.Unload(false);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="AssetBundle" />
        ///<seealso cref="LoadFromFileAsync" />
        public static AssetBundle LoadFromFile(string path, uint crc)
        {
            return LoadFromFile_Internal(path, crc, 0);
        }

        ///<summary>Synchronously loads an AssetBundle from a file on disk.</summary>
        ///<remarks>
        ///  <para>The function supports bundles of any compression type.
        ///In case of **LZMA** compression, the file content will be fully decompressed into memory and loaded from there. See [AssetBundles compression](xref:AssetBundles-Cache) for more details.
        ///
        ///Compared to <see cref="LoadFromFileAsync" />, this version is synchronous and will not return until it is done creating the AssetBundle object.
        ///
        ///This is the fastest way to load an AssetBundle.</para>
        ///  <para />
        ///</remarks>
        ///<param name="path">Path of the file on disk.</param>
        ///<param name="crc">An optional CRC-32 checksum of the uncompressed content. If this is non-zero, then the content will be compared against the checksum before loading it, and give an error if it does not match.</param>
        ///<param name="offset">An optional byte offset. This value specifies where to start reading the AssetBundle from.</param>
        ///<returns>Loaded AssetBundle object or null if failed.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///using System.IO;
        ///
        ///public class LoadFromFileExample : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        var myLoadedAssetBundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "myassetBundle"));
        ///        if (myLoadedAssetBundle == null)
        ///        {
        ///            Debug.Log("Failed to load AssetBundle!");
        ///            return;
        ///        }
        ///
        ///        var prefab = myLoadedAssetBundle.LoadAsset<GameObject>("MyObject");
        ///        Instantiate(prefab);
        ///
        ///        myLoadedAssetBundle.Unload(false);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="AssetBundle" />
        ///<seealso cref="LoadFromFileAsync" />
        public static AssetBundle LoadFromFile(string path, uint crc, ulong offset)
        {
            return LoadFromFile_Internal(path, crc, offset);
        }

        [FreeFunction("LoadFromMemoryAsync")]
        internal extern static AssetBundleCreateRequest LoadFromMemoryAsync_Internal(byte[] binary, uint crc);

        ///<summary>Asynchronously load an AssetBundle from a memory region.</summary>
        ///<remarks>
        ///  <para>Use this method to load an AssetBundle from an array of bytes asynchronously. This is useful when you have downloaded the data with encryption using UnityWebRequest and have the unencrypted bytes in memory instead of stored in a file.
        ///
        ///Compared to <see cref="LoadFromMemory" />, this version will perform AssetBundle decompression on a background thread, and will not create the AssetBundle object immediately.
        ///
        ///The content of the provided byte array is copied to create a temporary AssetBundle file in Memory, and that file is then loaded. Depending on the compression of the original AssetBundle, and the setting for <see cref="P:UnityEngine.Caching.compressionEnabled" />,
        ///this may also involve converting the content to LZ4 or uncompressed format.  See [AssetBundles compression](xref:AssetBundles-Cache) for more details.
        ///
        ///The following example shows how to use this method.  Note, for the sake of keeping the example simple it reads the bytes from disk, which means it has no advantage over calling AssetBundle.LoadFromFileAsync directly.</para>
        ///  <para />
        ///</remarks>
        ///<param name="binary">Array of bytes with the AssetBundle data.</param>
        ///<returns>Asynchronous load request for an AssetBundle. Use <see cref="AssetBundleCreateRequest.assetBundle" /> property to get an AssetBundle once it is loaded.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///using System.IO;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    IEnumerator LoadFromMemoryAsync(string path)
        ///    {
        ///        AssetBundleCreateRequest createRequest = AssetBundle.LoadFromMemoryAsync(File.ReadAllBytes(path));
        ///        yield return createRequest;
        ///        AssetBundle bundle = createRequest.assetBundle;
        ///        var prefab = bundle.LoadAsset<GameObject>("MyObject");
        ///        Instantiate(prefab);
        ///
        ///        bundle.Unload(true);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="AssetBundleCreateRequest" />
        ///<seealso cref="LoadFromMemory" />
        ///<seealso cref="LoadFromFileAsync" />
        public static AssetBundleCreateRequest LoadFromMemoryAsync(byte[] binary)
        {
            return LoadFromMemoryAsync_Internal(binary, 0);
        }

        ///<summary>Asynchronously load an AssetBundle from a memory region.</summary>
        ///<remarks>
        ///  <para>Use this method to load an AssetBundle from an array of bytes asynchronously. This is useful when you have downloaded the data with encryption using UnityWebRequest and have the unencrypted bytes in memory instead of stored in a file.
        ///
        ///Compared to <see cref="LoadFromMemory" />, this version will perform AssetBundle decompression on a background thread, and will not create the AssetBundle object immediately.
        ///
        ///The content of the provided byte array is copied to create a temporary AssetBundle file in Memory, and that file is then loaded. Depending on the compression of the original AssetBundle, and the setting for <see cref="P:UnityEngine.Caching.compressionEnabled" />,
        ///this may also involve converting the content to LZ4 or uncompressed format.  See [AssetBundles compression](xref:AssetBundles-Cache) for more details.
        ///
        ///The following example shows how to use this method.  Note, for the sake of keeping the example simple it reads the bytes from disk, which means it has no advantage over calling AssetBundle.LoadFromFileAsync directly.</para>
        ///  <para />
        ///</remarks>
        ///<param name="binary">Array of bytes with the AssetBundle data.</param>
        ///<param name="crc">An optional CRC-32 checksum of the uncompressed content. If this is non-zero, then the content will be compared against the checksum before loading it, and give an error if it does not match.</param>
        ///<returns>Asynchronous load request for an AssetBundle. Use <see cref="AssetBundleCreateRequest.assetBundle" /> property to get an AssetBundle once it is loaded.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///using System.IO;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    IEnumerator LoadFromMemoryAsync(string path)
        ///    {
        ///        AssetBundleCreateRequest createRequest = AssetBundle.LoadFromMemoryAsync(File.ReadAllBytes(path));
        ///        yield return createRequest;
        ///        AssetBundle bundle = createRequest.assetBundle;
        ///        var prefab = bundle.LoadAsset<GameObject>("MyObject");
        ///        Instantiate(prefab);
        ///
        ///        bundle.Unload(true);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="AssetBundleCreateRequest" />
        ///<seealso cref="LoadFromMemory" />
        ///<seealso cref="LoadFromFileAsync" />
        public static AssetBundleCreateRequest LoadFromMemoryAsync(byte[] binary, uint crc)
        {
            return LoadFromMemoryAsync_Internal(binary, crc);
        }

        [FreeFunction("LoadFromMemory")]
        internal extern static AssetBundle LoadFromMemory_Internal(byte[] binary, uint crc);

        ///<summary>Synchronously load an AssetBundle from a memory region.</summary>
        ///<remarks>
        ///  <para>Use this method to load an AssetBundle from an array of bytes. This is useful when you have downloaded the data with encryption and need to load the AssetBundle from the decrypted bytes.
        ///
        ///Compared to <see cref="LoadFromMemoryAsync" />, this version is synchronous and will not return until it is done creating the AssetBundle object.</para>
        ///  <para />
        ///</remarks>
        ///<param name="binary">Array of bytes with the AssetBundle data.</param>
        ///<returns>Loaded AssetBundle object or null if failed.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    byte[] MyDecrypt(byte[] binary)
        ///    {
        ///        // ...Perform some decryption process here to transform the input...
        ///        return binary;
        ///    }
        ///
        ///    IEnumerator Start()
        ///    {
        ///        var uwr = UnityWebRequest.Get("https://myserver/myBundle.unity3d");
        ///        yield return uwr.SendWebRequest();
        ///        byte[] decryptedBytes = MyDecrypt(uwr.downloadHandler.data);
        ///        AssetBundle.LoadFromMemory(decryptedBytes);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="AssetBundle" />
        ///<seealso cref="LoadFromMemoryAsync" />
        ///<seealso cref="LoadFromFile" />
        public static AssetBundle LoadFromMemory(byte[] binary)
        {
            return LoadFromMemory_Internal(binary, 0);
        }

        ///<summary>Synchronously load an AssetBundle from a memory region.</summary>
        ///<remarks>
        ///  <para>Use this method to load an AssetBundle from an array of bytes. This is useful when you have downloaded the data with encryption and need to load the AssetBundle from the decrypted bytes.
        ///
        ///Compared to <see cref="LoadFromMemoryAsync" />, this version is synchronous and will not return until it is done creating the AssetBundle object.</para>
        ///  <para />
        ///</remarks>
        ///<param name="binary">Array of bytes with the AssetBundle data.</param>
        ///<param name="crc">An optional CRC-32 checksum of the uncompressed content. If this is non-zero, then the content will be compared against the checksum before loading it, and give an error if it does not match.</param>
        ///<returns>Loaded AssetBundle object or null if failed.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    byte[] MyDecrypt(byte[] binary)
        ///    {
        ///        // ...Perform some decryption process here to transform the input...
        ///        return binary;
        ///    }
        ///
        ///    IEnumerator Start()
        ///    {
        ///        var uwr = UnityWebRequest.Get("https://myserver/myBundle.unity3d");
        ///        yield return uwr.SendWebRequest();
        ///        byte[] decryptedBytes = MyDecrypt(uwr.downloadHandler.data);
        ///        AssetBundle.LoadFromMemory(decryptedBytes);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="AssetBundle" />
        ///<seealso cref="LoadFromMemoryAsync" />
        ///<seealso cref="LoadFromFile" />
        public static AssetBundle LoadFromMemory(byte[] binary, uint crc)
        {
            return LoadFromMemory_Internal(binary, crc);
        }

        internal static void ValidateLoadFromStream(System.IO.Stream stream)
        {
            if (stream == null)
                throw new System.ArgumentNullException("ManagedStream object must be non-null", "stream");
            if (!stream.CanRead)
                throw new System.ArgumentException("ManagedStream object must be readable (stream.CanRead must return true)", "stream");
            if (!stream.CanSeek)
                throw new System.ArgumentException("ManagedStream object must be seekable (stream.CanSeek must return true)", "stream");
        }

        ///<summary>Asynchronously loads an AssetBundle from a managed Stream.</summary>
        ///<remarks>The function supports bundles of any compression type.
        ///**lzma** compressed data is decompressed to memory, while uncompressed and chunk-compressed bundles are read directly from the Stream.
        ///
        ///Unlike <see cref="LoadFromStream" />, this function is asynchronous.
        ///
        ///Unlike <see cref="LoadFromFileAsync" />, the data for the AssetBundle is supplied by a managed Stream object.
        ///
        ///The following are restrictions on a Stream object to optimize AssetBundle data loading:
        ///
        ///
        ///1. The AssetBundle data must start at stream position zero.
        ///
        ///2. Unity sets the seek position to zero before it loads the AssetBundle data.
        ///
        ///3. Unity assumes the read position in the stream is not altered by any other process. This allows the Unity process to read from the stream without having to call Seek() before every read.
        ///
        ///4. stream.CanRead must return true.
        ///
        ///5. stream.CanSeek must return true.
        ///
        ///6. It must be accessible from threads different to the main thread. Seek() and Read() can be called from any Unity native thread.
        ///
        ///7. In certain circumstances Unity will try to read passed the size of the AssetBundle data. The Stream implementation must gracefully handle this without throwing exceptions. The Stream implementation must also return the actual number of bytes read (not including any bytes passed the end of the AssetBundle data).
        ///
        ///8. When starting at the end of the AssetBundle data and trying to read data the Stream implementation must return 0 bytes read and not throw exceptions.
        ///
        ///To reduce the number of calls from native to managed code the data is read from the Stream using a buffered reader with a buffer size of **managedReadBufferSize**.
        ///
        ///* Changing **managedReadBufferSize** may change the loading performance, especially on mobile devices.
        ///
        ///* The optimal value for **managedReadBufferSize** varies from project to project and potentially from Asset Bundle to Asset Bundle.
        ///
        ///* A good range of values to experiment with is: 8KB, 16KB, 32KB, 64KB, 128KB.
        ///
        ///* Larger values might be better for compressed Asset Bundles or if the Asset Bundle contains large sized assets or if the Asset Bundle does not contain many assets and they are loaded sequentially from the Asset Bundle.
        ///
        ///* Smaller values might be better for uncompressed Asset Bundles and reading lots of small assets or if the Asset Bundles has lots of assets in it and the asset are loaded in a random order.
        ///
        ///
        ///Do not dispose the Stream object while loading the AssetBundle or any assets from the bundle. Its lifetime should be longer than the AssetBundle. This means you dispose the Stream object after calling <see cref="AssetBundle.Unload" />.</remarks>
        ///<param name="stream">The managed Stream object. Unity calls Read(), Seek() and the Length property on this object to load the AssetBundle data.</param>
        ///<param name="crc">An optional CRC-32 checksum of the uncompressed content.</param>
        ///<param name="managedReadBufferSize">You can use this to override the size of the read buffer Unity uses while loading data. The default size is 32KB.</param>
        ///<returns>Asynchronous load request for an AssetBundle. Use <see cref="AssetBundleCreateRequest.assetBundle" /> property to get an AssetBundle once it is loaded.</returns>
        ///<seealso cref="AssetBundle" />
        ///<seealso cref="LoadFromStream" />
        ///<seealso cref="LoadFromFileAsync" />
        public static AssetBundleCreateRequest LoadFromStreamAsync(System.IO.Stream stream, uint crc, uint managedReadBufferSize)
        {
            ValidateLoadFromStream(stream);
            return LoadFromStreamAsyncInternal(stream, crc, managedReadBufferSize);
        }

        ///<summary>Asynchronously loads an AssetBundle from a managed Stream.</summary>
        ///<remarks>The function supports bundles of any compression type.
        ///**lzma** compressed data is decompressed to memory, while uncompressed and chunk-compressed bundles are read directly from the Stream.
        ///
        ///Unlike <see cref="LoadFromStream" />, this function is asynchronous.
        ///
        ///Unlike <see cref="LoadFromFileAsync" />, the data for the AssetBundle is supplied by a managed Stream object.
        ///
        ///The following are restrictions on a Stream object to optimize AssetBundle data loading:
        ///
        ///
        ///1. The AssetBundle data must start at stream position zero.
        ///
        ///2. Unity sets the seek position to zero before it loads the AssetBundle data.
        ///
        ///3. Unity assumes the read position in the stream is not altered by any other process. This allows the Unity process to read from the stream without having to call Seek() before every read.
        ///
        ///4. stream.CanRead must return true.
        ///
        ///5. stream.CanSeek must return true.
        ///
        ///6. It must be accessible from threads different to the main thread. Seek() and Read() can be called from any Unity native thread.
        ///
        ///7. In certain circumstances Unity will try to read passed the size of the AssetBundle data. The Stream implementation must gracefully handle this without throwing exceptions. The Stream implementation must also return the actual number of bytes read (not including any bytes passed the end of the AssetBundle data).
        ///
        ///8. When starting at the end of the AssetBundle data and trying to read data the Stream implementation must return 0 bytes read and not throw exceptions.
        ///
        ///To reduce the number of calls from native to managed code the data is read from the Stream using a buffered reader with a buffer size of **managedReadBufferSize**.
        ///
        ///* Changing **managedReadBufferSize** may change the loading performance, especially on mobile devices.
        ///
        ///* The optimal value for **managedReadBufferSize** varies from project to project and potentially from Asset Bundle to Asset Bundle.
        ///
        ///* A good range of values to experiment with is: 8KB, 16KB, 32KB, 64KB, 128KB.
        ///
        ///* Larger values might be better for compressed Asset Bundles or if the Asset Bundle contains large sized assets or if the Asset Bundle does not contain many assets and they are loaded sequentially from the Asset Bundle.
        ///
        ///* Smaller values might be better for uncompressed Asset Bundles and reading lots of small assets or if the Asset Bundles has lots of assets in it and the asset are loaded in a random order.
        ///
        ///
        ///Do not dispose the Stream object while loading the AssetBundle or any assets from the bundle. Its lifetime should be longer than the AssetBundle. This means you dispose the Stream object after calling <see cref="AssetBundle.Unload" />.</remarks>
        ///<param name="stream">The managed Stream object. Unity calls Read(), Seek() and the Length property on this object to load the AssetBundle data.</param>
        ///<param name="crc">An optional CRC-32 checksum of the uncompressed content.</param>
        ///<returns>Asynchronous load request for an AssetBundle. Use <see cref="AssetBundleCreateRequest.assetBundle" /> property to get an AssetBundle once it is loaded.</returns>
        ///<seealso cref="AssetBundle" />
        ///<seealso cref="LoadFromStream" />
        ///<seealso cref="LoadFromFileAsync" />
        public static AssetBundleCreateRequest LoadFromStreamAsync(System.IO.Stream stream, uint crc)
        {
            ValidateLoadFromStream(stream);
            return LoadFromStreamAsyncInternal(stream, crc, 0);
        }

        ///<summary>Asynchronously loads an AssetBundle from a managed Stream.</summary>
        ///<remarks>The function supports bundles of any compression type.
        ///**lzma** compressed data is decompressed to memory, while uncompressed and chunk-compressed bundles are read directly from the Stream.
        ///
        ///Unlike <see cref="LoadFromStream" />, this function is asynchronous.
        ///
        ///Unlike <see cref="LoadFromFileAsync" />, the data for the AssetBundle is supplied by a managed Stream object.
        ///
        ///The following are restrictions on a Stream object to optimize AssetBundle data loading:
        ///
        ///
        ///1. The AssetBundle data must start at stream position zero.
        ///
        ///2. Unity sets the seek position to zero before it loads the AssetBundle data.
        ///
        ///3. Unity assumes the read position in the stream is not altered by any other process. This allows the Unity process to read from the stream without having to call Seek() before every read.
        ///
        ///4. stream.CanRead must return true.
        ///
        ///5. stream.CanSeek must return true.
        ///
        ///6. It must be accessible from threads different to the main thread. Seek() and Read() can be called from any Unity native thread.
        ///
        ///7. In certain circumstances Unity will try to read passed the size of the AssetBundle data. The Stream implementation must gracefully handle this without throwing exceptions. The Stream implementation must also return the actual number of bytes read (not including any bytes passed the end of the AssetBundle data).
        ///
        ///8. When starting at the end of the AssetBundle data and trying to read data the Stream implementation must return 0 bytes read and not throw exceptions.
        ///
        ///To reduce the number of calls from native to managed code the data is read from the Stream using a buffered reader with a buffer size of **managedReadBufferSize**.
        ///
        ///* Changing **managedReadBufferSize** may change the loading performance, especially on mobile devices.
        ///
        ///* The optimal value for **managedReadBufferSize** varies from project to project and potentially from Asset Bundle to Asset Bundle.
        ///
        ///* A good range of values to experiment with is: 8KB, 16KB, 32KB, 64KB, 128KB.
        ///
        ///* Larger values might be better for compressed Asset Bundles or if the Asset Bundle contains large sized assets or if the Asset Bundle does not contain many assets and they are loaded sequentially from the Asset Bundle.
        ///
        ///* Smaller values might be better for uncompressed Asset Bundles and reading lots of small assets or if the Asset Bundles has lots of assets in it and the asset are loaded in a random order.
        ///
        ///
        ///Do not dispose the Stream object while loading the AssetBundle or any assets from the bundle. Its lifetime should be longer than the AssetBundle. This means you dispose the Stream object after calling <see cref="AssetBundle.Unload" />.</remarks>
        ///<param name="stream">The managed Stream object. Unity calls Read(), Seek() and the Length property on this object to load the AssetBundle data.</param>
        ///<returns>Asynchronous load request for an AssetBundle. Use <see cref="AssetBundleCreateRequest.assetBundle" /> property to get an AssetBundle once it is loaded.</returns>
        ///<seealso cref="AssetBundle" />
        ///<seealso cref="LoadFromStream" />
        ///<seealso cref="LoadFromFileAsync" />
        public static AssetBundleCreateRequest LoadFromStreamAsync(System.IO.Stream stream)
        {
            ValidateLoadFromStream(stream);
            return LoadFromStreamAsyncInternal(stream, 0, 0);
        }

        ///<summary>Synchronously loads an AssetBundle from a managed Stream.</summary>
        ///<remarks>
        ///  <para>The function supports bundles of any compression type.
        ///**lzma** compressed data is decompressed to memory, while uncompressed and chunk-compressed bundles are read directly from the Stream.
        ///
        ///The content is compared against the checksum before it is loaded when the checksum is a non-zero value. An error is thrown if it does not match.
        ///
        ///Unlike <see cref="LoadFromStreamAsync" />, this function is synchronous and only returns when it has loaded the AssetBundle object.
        ///
        ///Unlike <see cref="LoadFromFile" />, the data for the AssetBundle is supplied by a managed Stream object.
        ///
        ///The following are restrictions on a Stream object to optimize AssetBundle data loading:
        ///
        ///
        ///1. The AssetBundle data must start at stream position zero.
        ///
        ///2. Unity sets the seek position to zero before it loads the AssetBundle data.
        ///
        ///3. Unity assumes the read position in the stream is not altered by any other process. This allows the Unity process to read from the stream without having to call Seek() before every read.
        ///
        ///4. stream.CanRead must return true.
        ///
        ///5. stream.CanSeek must return true.
        ///
        ///6. It must be accessible from threads different to the main thread. Seek() and Read() can be called from any Unity native thread.
        ///
        ///7. In certain circumstances, Unity tries to read past the size of the AssetBundle data. The Stream implementation must gracefully handle this without throwing exceptions. The Stream implementation must also return the actual number of bytes read (not including any bytes past the end of the AssetBundle data).
        ///
        ///8. When starting at the end of the AssetBundle data and trying to read data the Stream implementation must return 0 bytes read and not throw exceptions.
        ///
        ///To reduce the number of calls from native to managed code the data is read from the Stream using a buffered reader with a buffer size of **managedReadBufferSize**.
        ///
        ///* Changing **managedReadBufferSize** may change the loading performance, especially on mobile devices.
        ///
        ///* The optimal value for **managedReadBufferSize** varies from project to project and potentially from Asset Bundle to Asset Bundle.
        ///
        ///* A good range of values to experiment with is: 8KB, 16KB, 32KB, 64KB, 128KB.
        ///
        ///* Larger values might be better for compressed Asset Bundles or if the Asset Bundle contains large sized assets or if the Asset Bundle does not contain many assets and they are loaded sequentially from the Asset Bundle.
        ///
        ///* Smaller values might be better for uncompressed Asset Bundles and reading lots of small assets or if the Asset Bundles has lots of assets in it and the asset are loaded in a random order.
        ///
        ///
        ///Do not dispose the Stream object while loading the AssetBundle or any assets from the bundle. Its lifetime should be longer than the AssetBundle. This means you dispose the Stream object after calling <see cref="AssetBundle.Unload" />.</para>
        ///  <para />
        ///</remarks>
        ///<param name="stream">The managed Stream object. Unity calls Read(), Seek() and the Length property on this object to load the AssetBundle data.</param>
        ///<param name="crc">An optional CRC-32 checksum of the uncompressed content.</param>
        ///<param name="managedReadBufferSize">You can use this to override the size of the read buffer Unity uses while loading data. The default size is 32KB.</param>
        ///<returns>The loaded AssetBundle object or null when the object fails to load.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///using System;
        ///using System.IO;
        ///
        ///public class LoadFromStreamExample : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        var fileStream = new FileStream(Application.streamingAssetsPath, FileMode.Open, FileAccess.Read);
        ///        var myLoadedAssetBundle = AssetBundle.LoadFromStream(fileStream);
        ///        if (myLoadedAssetBundle == null)
        ///        {
        ///            Debug.Log("Failed to load AssetBundle!");
        ///            return;
        ///        }
        ///
        ///        var prefab = myLoadedAssetBundle.LoadAsset<GameObject>("MyObject");
        ///        Instantiate(prefab);
        ///
        ///        myLoadedAssetBundle.Unload(false);
        ///        fileStream.Dispose();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="AssetBundle" />
        ///<seealso cref="LoadFromStreamAsync" />
        ///<seealso cref="LoadFromFile" />
        public static AssetBundle LoadFromStream(System.IO.Stream stream, uint crc, uint managedReadBufferSize)
        {
            ValidateLoadFromStream(stream);
            return LoadFromStreamInternal(stream, crc, managedReadBufferSize);
        }

        ///<summary>Synchronously loads an AssetBundle from a managed Stream.</summary>
        ///<remarks>
        ///  <para>The function supports bundles of any compression type.
        ///**lzma** compressed data is decompressed to memory, while uncompressed and chunk-compressed bundles are read directly from the Stream.
        ///
        ///The content is compared against the checksum before it is loaded when the checksum is a non-zero value. An error is thrown if it does not match.
        ///
        ///Unlike <see cref="LoadFromStreamAsync" />, this function is synchronous and only returns when it has loaded the AssetBundle object.
        ///
        ///Unlike <see cref="LoadFromFile" />, the data for the AssetBundle is supplied by a managed Stream object.
        ///
        ///The following are restrictions on a Stream object to optimize AssetBundle data loading:
        ///
        ///
        ///1. The AssetBundle data must start at stream position zero.
        ///
        ///2. Unity sets the seek position to zero before it loads the AssetBundle data.
        ///
        ///3. Unity assumes the read position in the stream is not altered by any other process. This allows the Unity process to read from the stream without having to call Seek() before every read.
        ///
        ///4. stream.CanRead must return true.
        ///
        ///5. stream.CanSeek must return true.
        ///
        ///6. It must be accessible from threads different to the main thread. Seek() and Read() can be called from any Unity native thread.
        ///
        ///7. In certain circumstances, Unity tries to read past the size of the AssetBundle data. The Stream implementation must gracefully handle this without throwing exceptions. The Stream implementation must also return the actual number of bytes read (not including any bytes past the end of the AssetBundle data).
        ///
        ///8. When starting at the end of the AssetBundle data and trying to read data the Stream implementation must return 0 bytes read and not throw exceptions.
        ///
        ///To reduce the number of calls from native to managed code the data is read from the Stream using a buffered reader with a buffer size of **managedReadBufferSize**.
        ///
        ///* Changing **managedReadBufferSize** may change the loading performance, especially on mobile devices.
        ///
        ///* The optimal value for **managedReadBufferSize** varies from project to project and potentially from Asset Bundle to Asset Bundle.
        ///
        ///* A good range of values to experiment with is: 8KB, 16KB, 32KB, 64KB, 128KB.
        ///
        ///* Larger values might be better for compressed Asset Bundles or if the Asset Bundle contains large sized assets or if the Asset Bundle does not contain many assets and they are loaded sequentially from the Asset Bundle.
        ///
        ///* Smaller values might be better for uncompressed Asset Bundles and reading lots of small assets or if the Asset Bundles has lots of assets in it and the asset are loaded in a random order.
        ///
        ///
        ///Do not dispose the Stream object while loading the AssetBundle or any assets from the bundle. Its lifetime should be longer than the AssetBundle. This means you dispose the Stream object after calling <see cref="AssetBundle.Unload" />.</para>
        ///  <para />
        ///</remarks>
        ///<param name="stream">The managed Stream object. Unity calls Read(), Seek() and the Length property on this object to load the AssetBundle data.</param>
        ///<param name="crc">An optional CRC-32 checksum of the uncompressed content.</param>
        ///<returns>The loaded AssetBundle object or null when the object fails to load.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///using System;
        ///using System.IO;
        ///
        ///public class LoadFromStreamExample : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        var fileStream = new FileStream(Application.streamingAssetsPath, FileMode.Open, FileAccess.Read);
        ///        var myLoadedAssetBundle = AssetBundle.LoadFromStream(fileStream);
        ///        if (myLoadedAssetBundle == null)
        ///        {
        ///            Debug.Log("Failed to load AssetBundle!");
        ///            return;
        ///        }
        ///
        ///        var prefab = myLoadedAssetBundle.LoadAsset<GameObject>("MyObject");
        ///        Instantiate(prefab);
        ///
        ///        myLoadedAssetBundle.Unload(false);
        ///        fileStream.Dispose();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="AssetBundle" />
        ///<seealso cref="LoadFromStreamAsync" />
        ///<seealso cref="LoadFromFile" />
        public static AssetBundle LoadFromStream(System.IO.Stream stream, uint crc)
        {
            ValidateLoadFromStream(stream);
            return LoadFromStreamInternal(stream, crc, 0);
        }

        ///<summary>Synchronously loads an AssetBundle from a managed Stream.</summary>
        ///<remarks>
        ///  <para>The function supports bundles of any compression type.
        ///**lzma** compressed data is decompressed to memory, while uncompressed and chunk-compressed bundles are read directly from the Stream.
        ///
        ///The content is compared against the checksum before it is loaded when the checksum is a non-zero value. An error is thrown if it does not match.
        ///
        ///Unlike <see cref="LoadFromStreamAsync" />, this function is synchronous and only returns when it has loaded the AssetBundle object.
        ///
        ///Unlike <see cref="LoadFromFile" />, the data for the AssetBundle is supplied by a managed Stream object.
        ///
        ///The following are restrictions on a Stream object to optimize AssetBundle data loading:
        ///
        ///
        ///1. The AssetBundle data must start at stream position zero.
        ///
        ///2. Unity sets the seek position to zero before it loads the AssetBundle data.
        ///
        ///3. Unity assumes the read position in the stream is not altered by any other process. This allows the Unity process to read from the stream without having to call Seek() before every read.
        ///
        ///4. stream.CanRead must return true.
        ///
        ///5. stream.CanSeek must return true.
        ///
        ///6. It must be accessible from threads different to the main thread. Seek() and Read() can be called from any Unity native thread.
        ///
        ///7. In certain circumstances, Unity tries to read past the size of the AssetBundle data. The Stream implementation must gracefully handle this without throwing exceptions. The Stream implementation must also return the actual number of bytes read (not including any bytes past the end of the AssetBundle data).
        ///
        ///8. When starting at the end of the AssetBundle data and trying to read data the Stream implementation must return 0 bytes read and not throw exceptions.
        ///
        ///To reduce the number of calls from native to managed code the data is read from the Stream using a buffered reader with a buffer size of **managedReadBufferSize**.
        ///
        ///* Changing **managedReadBufferSize** may change the loading performance, especially on mobile devices.
        ///
        ///* The optimal value for **managedReadBufferSize** varies from project to project and potentially from Asset Bundle to Asset Bundle.
        ///
        ///* A good range of values to experiment with is: 8KB, 16KB, 32KB, 64KB, 128KB.
        ///
        ///* Larger values might be better for compressed Asset Bundles or if the Asset Bundle contains large sized assets or if the Asset Bundle does not contain many assets and they are loaded sequentially from the Asset Bundle.
        ///
        ///* Smaller values might be better for uncompressed Asset Bundles and reading lots of small assets or if the Asset Bundles has lots of assets in it and the asset are loaded in a random order.
        ///
        ///
        ///Do not dispose the Stream object while loading the AssetBundle or any assets from the bundle. Its lifetime should be longer than the AssetBundle. This means you dispose the Stream object after calling <see cref="AssetBundle.Unload" />.</para>
        ///  <para />
        ///</remarks>
        ///<param name="stream">The managed Stream object. Unity calls Read(), Seek() and the Length property on this object to load the AssetBundle data.</param>
        ///<returns>The loaded AssetBundle object or null when the object fails to load.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///using System;
        ///using System.IO;
        ///
        ///public class LoadFromStreamExample : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        var fileStream = new FileStream(Application.streamingAssetsPath, FileMode.Open, FileAccess.Read);
        ///        var myLoadedAssetBundle = AssetBundle.LoadFromStream(fileStream);
        ///        if (myLoadedAssetBundle == null)
        ///        {
        ///            Debug.Log("Failed to load AssetBundle!");
        ///            return;
        ///        }
        ///
        ///        var prefab = myLoadedAssetBundle.LoadAsset<GameObject>("MyObject");
        ///        Instantiate(prefab);
        ///
        ///        myLoadedAssetBundle.Unload(false);
        ///        fileStream.Dispose();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="AssetBundle" />
        ///<seealso cref="LoadFromStreamAsync" />
        ///<seealso cref="LoadFromFile" />
        public static AssetBundle LoadFromStream(System.IO.Stream stream)
        {
            ValidateLoadFromStream(stream);
            return LoadFromStreamInternal(stream, 0, 0);
        }

        [FreeFunction("LoadFromStreamAsyncInternal")]
        internal extern static AssetBundleCreateRequest LoadFromStreamAsyncInternal(System.IO.Stream stream, uint crc,
            uint managedReadBufferSize);

        [FreeFunction("LoadFromStreamInternal")]
        internal extern static AssetBundle LoadFromStreamInternal(System.IO.Stream stream, uint crc,
            uint managedReadBufferSize);

        ///<summary>Return true if the AssetBundle contains Unity Scene files</summary>
        ///<remarks>An AssetBundle can store either Scenes or Assets, never a mix of the two.
        ///
        ///A "Streamed Scene AssetBundle" is simply a term for an AssetBundle with one or more Scenes inside it.</remarks>
        ///<seealso cref="AssetBundle.GetAllScenePaths" />
        public extern bool isStreamedSceneAssetBundle
        {
            [NativeMethod("GetIsStreamedSceneAssetBundle")]
            get;
        }

        ///<summary>Check if an AssetBundle contains a specific object.</summary>
        ///<remarks>Returns true if an Asset referred to by <c>name</c> is contained in the AssetBundle, false otherwise.</remarks>
        ///<param name="name">The name of the object to look for.</param>
        ///<returns>True if an asset with the specified name is in the AssetBundle. Otherwise, false.</returns>
        [NativeMethod("Contains")]
        public extern bool Contains(string name);

        ///<exclude />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Method Load has been deprecated. Script updater cannot update it as the loading behaviour has changed. Please use LoadAsset instead and check the documentation for details.", true)]
        public Object Load(string name) { return null; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Method Load has been deprecated. Script updater cannot update it as the loading behaviour has changed. Please use LoadAsset instead and check the documentation for details.", true)]
        public Object Load<T>(string name) { return null; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Method Load has been deprecated. Script updater cannot update it as the loading behaviour has changed. Please use LoadAsset instead and check the documentation for details.", true)]
        Object Load(string name, Type type) { return null; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Method LoadAsync has been deprecated. Script updater cannot update it as the loading behaviour has changed. Please use LoadAssetAsync instead and check the documentation for details.", true)]
        AssetBundleRequest LoadAsync(string name, Type type) { return null; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Method LoadAll has been deprecated. Script updater cannot update it as the loading behaviour has changed. Please use LoadAllAssets instead and check the documentation for details.", true)]
        Object[] LoadAll(Type type) { return null; }

        ///<exclude />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Method LoadAll has been deprecated. Script updater cannot update it as the loading behaviour has changed. Please use LoadAllAssets instead and check the documentation for details.", true)]
        public UnityEngine.Object[] LoadAll() { return null; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Method LoadAll has been deprecated. Script updater cannot update it as the loading behaviour has changed. Please use LoadAllAssets instead and check the documentation for details.", true)]
        public T[] LoadAll<T>() where T : Object { return null; }

        ///<summary>Synchronously loads an Asset from the AssetBundle.</summary>
        ///<remarks>The LoadAsset&lt;T&gt; signature is recommended, so that the requested type is explicit and no type casting is necessary.
        ///
        ///
        ///                When the signature without type is used the main object of the matching Asset is returned. For example when loading a Prefab this will return the root GameObject.
        ///
        ///
        ///                Note: For Scenes inside AssetBundles call <see cref="M:UnityEngine.SceneManagement.SceneManager.LoadScene" /> instead of this method.</remarks>
        ///<param name="name">Name of the Asset.  For the most precise matching this should be the relative path of the Asset that was built into the AssetBundle, including the file extension.
        ///                The relative path and file extension are optional, and Assets can be found and loaded based on the filename alone.  However this opens the potential for unexpected results if the filename is not unique within the AssetBundle.
        ///                At build time it is also possible to specify a name for the Asset using <see cref="P:UnityEditor.AssetBundleBuild.addressableNames" />.  In that case that specified name will be expected to load the Asset instead of the Asset path.</param>
        ///<returns>The loaded asset that matches the specified name.</returns>
        public Object LoadAsset(string name)
        {
            return LoadAsset(name, typeof(Object));
        }

        ///<summary>Synchronously loads an Asset from the AssetBundle.</summary>
        ///<remarks>The LoadAsset&lt;T&gt; signature is recommended, so that the requested type is explicit and no type casting is necessary.
        ///
        ///
        ///                When the signature without type is used the main object of the matching Asset is returned. For example when loading a Prefab this will return the root GameObject.
        ///
        ///
        ///                Note: For Scenes inside AssetBundles call <see cref="M:UnityEngine.SceneManagement.SceneManager.LoadScene" /> instead of this method.</remarks>
        ///<param name="name">Name of the Asset.  For the most precise matching this should be the relative path of the Asset that was built into the AssetBundle, including the file extension.
        ///                The relative path and file extension are optional, and Assets can be found and loaded based on the filename alone.  However this opens the potential for unexpected results if the filename is not unique within the AssetBundle.
        ///                At build time it is also possible to specify a name for the Asset using <see cref="P:UnityEditor.AssetBundleBuild.addressableNames" />.  In that case that specified name will be expected to load the Asset instead of the Asset path.</param>
        ///<returns>The loaded asset of type <typeparamref name="T" /> that matches the specified name.</returns>
        public T LoadAsset<T>(string name) where T : Object
        {
            return (T)LoadAsset(name, typeof(T));
        }

        ///<summary>Synchronously loads an Asset from the AssetBundle.</summary>
        ///<remarks>The LoadAsset&lt;T&gt; signature is recommended, so that the requested type is explicit and no type casting is necessary.
        ///
        ///
        ///                When the signature without type is used the main object of the matching Asset is returned. For example when loading a Prefab this will return the root GameObject.
        ///
        ///
        ///                Note: For Scenes inside AssetBundles call <see cref="M:UnityEngine.SceneManagement.SceneManager.LoadScene" /> instead of this method.</remarks>
        ///<param name="name">Name of the Asset.  For the most precise matching this should be the relative path of the Asset that was built into the AssetBundle, including the file extension.
        ///                The relative path and file extension are optional, and Assets can be found and loaded based on the filename alone.  However this opens the potential for unexpected results if the filename is not unique within the AssetBundle.
        ///                At build time it is also possible to specify a name for the Asset using <see cref="P:UnityEditor.AssetBundleBuild.addressableNames" />.  In that case that specified name will be expected to load the Asset instead of the Asset path.</param>
        ///<param name="type">The provided type will be checked against the Asset's main object, and if that is not compatible it will be matched against visible objects within the Asset.
        ///                Not all nested objects are visible, for example this will not work to directly retrieve a Transform, MonoBehaviour or other Component.
        ///                In cases where there are multiple matches for the name argument, the requested type can determine which Asset to load.</param>
        ///<returns>The loaded asset that matches the specified name and type.</returns>
        [TypeInferenceRule(TypeInferenceRules.TypeReferencedBySecondArgument)]
        public Object LoadAsset(string name, Type type)
        {
            if (name == null)
            {
                throw new System.NullReferenceException("The input asset name cannot be null.");
            }
            if (name.Length == 0)
            {
                throw new System.ArgumentException("The input asset name cannot be empty.");
            }
            if (type == null)
            {
                throw new System.NullReferenceException("The input type cannot be null.");
            }

            return LoadAsset_Internal(name, type);
        }

        [NativeMethod("LoadAsset_Internal", ThrowsException = true)]
        [TypeInferenceRule(TypeInferenceRules.TypeReferencedBySecondArgument)]
        private extern Object LoadAsset_Internal(string name, Type type);

        ///<summary>Asynchronously loads an Asset from the bundle.</summary>
        ///<remarks>The LoadAssetAsync&lt;T&gt; signature is recommended, so that the requested type is explicit and no type casting is necessary.
        ///
        ///
        ///                Note: For Scenes inside AssetBundles call <see cref="M:UnityEngine.SceneManagement.SceneManager.LoadSceneAsync" /> instead of this method.</remarks>
        ///<param name="name">Name of the Asset.  For the most precise matching this should be the relative path of the Asset that was built into the AssetBundle, including the file extension.
        ///                The relative path and file extension are optional, and Assets can be found and loaded based on the filename alone.  However this opens the potential for unexpected results if the filename is not unique within the AssetBundle.
        ///                At build time it is also possible to specify a name for the Asset using <see cref="P:UnityEditor.AssetBundleBuild.addressableNames" />.  In that case that specified name will be expected to load the Asset instead of the Asset path.</param>
        ///<returns>Asynchronous load request. Use the <see cref="AssetBundleRequest.asset" /> property to get the loaded asset once the operation completes.</returns>
        ///<seealso cref="AssetBundleRequest" />
        public AssetBundleRequest LoadAssetAsync(string name)
        {
            return LoadAssetAsync(name, typeof(UnityEngine.Object));
        }

        ///<summary>Asynchronously loads an Asset from the bundle.</summary>
        ///<remarks>The LoadAssetAsync&lt;T&gt; signature is recommended, so that the requested type is explicit and no type casting is necessary.
        ///
        ///
        ///                Note: For Scenes inside AssetBundles call <see cref="M:UnityEngine.SceneManagement.SceneManager.LoadSceneAsync" /> instead of this method.</remarks>
        ///<param name="name">Name of the Asset.  For the most precise matching this should be the relative path of the Asset that was built into the AssetBundle, including the file extension.
        ///                The relative path and file extension are optional, and Assets can be found and loaded based on the filename alone.  However this opens the potential for unexpected results if the filename is not unique within the AssetBundle.
        ///                At build time it is also possible to specify a name for the Asset using <see cref="P:UnityEditor.AssetBundleBuild.addressableNames" />.  In that case that specified name will be expected to load the Asset instead of the Asset path.</param>
        ///<returns>Asynchronous load request. Use the <see cref="AssetBundleRequest.asset" /> property to get the loaded asset once the operation completes.</returns>
        ///<seealso cref="AssetBundleRequest" />
        public AssetBundleRequest LoadAssetAsync<T>(string name)
        {
            return LoadAssetAsync(name, typeof(T));
        }

        ///<summary>Asynchronously loads an Asset from the bundle.</summary>
        ///<remarks>The LoadAssetAsync&lt;T&gt; signature is recommended, so that the requested type is explicit and no type casting is necessary.
        ///
        ///
        ///                Note: For Scenes inside AssetBundles call <see cref="M:UnityEngine.SceneManagement.SceneManager.LoadSceneAsync" /> instead of this method.</remarks>
        ///<param name="name">Name of the Asset.  For the most precise matching this should be the relative path of the Asset that was built into the AssetBundle, including the file extension.
        ///                The relative path and file extension are optional, and Assets can be found and loaded based on the filename alone.  However this opens the potential for unexpected results if the filename is not unique within the AssetBundle.
        ///                At build time it is also possible to specify a name for the Asset using <see cref="P:UnityEditor.AssetBundleBuild.addressableNames" />.  In that case that specified name will be expected to load the Asset instead of the Asset path.</param>
        ///<param name="type">The provided type will be checked against the Asset's main object, and if that is not compatible it will be matched against visible objects within the Asset.
        ///                Not all nested objects are visible, for example this will not work to directly retrieve a Transform, MonoBehaviour or other Component.
        ///                In cases where there are multiple matches for the name argument, the requested type can determine which Asset to load.</param>
        ///<returns>Asynchronous load request. Use the <see cref="AssetBundleRequest.asset" /> property to get the loaded asset once the operation completes.</returns>
        ///<seealso cref="AssetBundleRequest" />
        public AssetBundleRequest LoadAssetAsync(string name, Type type)
        {
            if (name == null)
            {
                throw new System.NullReferenceException("The input asset name cannot be null.");
            }
            if (name.Length == 0)
            {
                throw new System.ArgumentException("The input asset name cannot be empty.");
            }
            if (type == null)
            {
                throw new System.NullReferenceException("The input type cannot be null.");
            }

            return LoadAssetAsync_Internal(name, type);
        }

        ///<summary>Loads Asset and sub Assets from the AssetBundle synchronously.</summary>
        ///<remarks>Load objects from the Asset and its SubAssets.  If the signatures that specify the type are called then the requested type is matched against the Main object and Visible objects in each Asset.
        ///                Otherwise the main objects of each Asset is returned.  An example usage is to load all sprites from an sprite that uses "Multiple" for its [Sprite Mode](xref:texture-type-sprite).</remarks>
        ///<param name="name">Name of the Asset.</param>
        ///<returns>An array of the loaded asset and its sub-assets.</returns>
        public Object[] LoadAssetWithSubAssets(string name)
        {
            return LoadAssetWithSubAssets(name, typeof(Object));
        }

        internal static T[] ConvertObjects<T>(Object[] rawObjects) where T : Object
        {
            if (rawObjects == null) return null;
            T[] typedObjects = new T[rawObjects.Length];
            for (int i = 0; i < typedObjects.Length; i++)
                typedObjects[i] = (T)rawObjects[i];
            return typedObjects;
        }

        ///<summary>Loads Asset and sub Assets from the AssetBundle synchronously.</summary>
        ///<remarks>Load objects from the Asset and its SubAssets.  If the signatures that specify the type are called then the requested type is matched against the Main object and Visible objects in each Asset.
        ///                Otherwise the main objects of each Asset is returned.  An example usage is to load all sprites from an sprite that uses "Multiple" for its [Sprite Mode](xref:texture-type-sprite).</remarks>
        ///<param name="name">Name of the Asset.</param>
        ///<returns>An array of the loaded asset and its sub-assets of type <typeparamref name="T" />.</returns>
        public T[] LoadAssetWithSubAssets<T>(string name) where T : Object
        {
            return ConvertObjects<T>(LoadAssetWithSubAssets(name, typeof(T)));
        }

        ///<summary>Loads Asset and sub Assets from the AssetBundle synchronously.</summary>
        ///<remarks>Load objects from the Asset and its SubAssets.  If the signatures that specify the type are called then the requested type is matched against the Main object and Visible objects in each Asset.
        ///                Otherwise the main objects of each Asset is returned.  An example usage is to load all sprites from an sprite that uses "Multiple" for its [Sprite Mode](xref:texture-type-sprite).</remarks>
        ///<param name="name">Name of the Asset.</param>
        ///<param name="type">Type to load.</param>
        ///<returns>An array of the loaded asset and its sub-assets that match the specified type.</returns>
        public Object[] LoadAssetWithSubAssets(string name, Type type)
        {
            if (name == null)
            {
                throw new System.NullReferenceException("The input asset name cannot be null.");
            }
            if (name.Length == 0)
            {
                throw new System.ArgumentException("The input asset name cannot be empty.");
            }
            if (type == null)
            {
                throw new System.NullReferenceException("The input type cannot be null.");
            }

            return LoadAssetWithSubAssets_Internal(name, type);
        }

        ///<summary>Loads Asset and sub Assets from the AssetBundle asynchronously.</summary>
        ///<param name="name">Name of the Asset.</param>
        ///<returns>Asynchronous load request. Use the <see cref="AssetBundleRequest.allAssets" /> property to get the loaded asset and its sub-assets once the operation completes.</returns>
        ///<seealso cref="AssetBundleRequest.allAssets" />
        public AssetBundleRequest LoadAssetWithSubAssetsAsync(string name)
        {
            return LoadAssetWithSubAssetsAsync(name, typeof(UnityEngine.Object));
        }

        ///<summary>Loads Asset and sub Assets from the AssetBundle asynchronously.</summary>
        ///<param name="name">Name of the Asset.</param>
        ///<returns>Asynchronous load request. Use the <see cref="AssetBundleRequest.allAssets" /> property to get the loaded asset and its sub-assets once the operation completes.</returns>
        ///<seealso cref="AssetBundleRequest.allAssets" />
        public AssetBundleRequest LoadAssetWithSubAssetsAsync<T>(string name)
        {
            return LoadAssetWithSubAssetsAsync(name, typeof(T));
        }

        ///<summary>Loads Asset and sub Assets from the AssetBundle asynchronously.</summary>
        ///<param name="name">Name of the Asset.</param>
        ///<param name="type">Type to load.</param>
        ///<returns>Asynchronous load request. Use the <see cref="AssetBundleRequest.allAssets" /> property to get the loaded asset and its sub-assets once the operation completes.</returns>
        ///<seealso cref="AssetBundleRequest.allAssets" />
        public AssetBundleRequest LoadAssetWithSubAssetsAsync(string name, Type type)
        {
            if (name == null)
            {
                throw new System.NullReferenceException("The input asset name cannot be null.");
            }
            if (name.Length == 0)
            {
                throw new System.ArgumentException("The input asset name cannot be empty.");
            }
            if (type == null)
            {
                throw new System.NullReferenceException("The input type cannot be null.");
            }

            return LoadAssetWithSubAssetsAsync_Internal(name, type);
        }

        ///<summary>Loads all Assets contained in the AssetBundle synchronously.</summary>
        ///<returns>An array of all the assets in the AssetBundle.</returns>
        public UnityEngine.Object[] LoadAllAssets()
        {
            return LoadAllAssets(typeof(UnityEngine.Object));
        }

        ///<summary>Loads all Assets contained in the AssetBundle synchronously.</summary>
        ///<returns>An array of all the assets of type <typeparamref name="T" /> in the AssetBundle.</returns>
        public T[] LoadAllAssets<T>() where T : Object
        {
            return ConvertObjects<T>(LoadAllAssets(typeof(T)));
        }

        ///<summary>Loads all Assets contained in the AssetBundle synchronously.</summary>
        ///<param name="type">When specified only main or visible objects that derive from the provided type are returned.</param>
        ///<returns>An array of all the assets in the AssetBundle that derive from the specified type.</returns>
        public UnityEngine.Object[] LoadAllAssets(Type type)
        {
            if (type == null)
            {
                throw new System.NullReferenceException("The input type cannot be null.");
            }

            return LoadAssetWithSubAssets_Internal("", type);
        }

        ///<summary>Loads all Assets contained in the AssetBundle asynchronously.</summary>
        ///<returns>Asynchronous load request. Use the <see cref="AssetBundleRequest.allAssets" /> property to get the loaded assets once the operation completes.</returns>
        ///<seealso cref="AssetBundleRequest.allAssets" />
        public AssetBundleRequest LoadAllAssetsAsync()
        {
            return LoadAllAssetsAsync(typeof(UnityEngine.Object));
        }

        ///<summary>Loads all Assets contained in the AssetBundle asynchronously.</summary>
        ///<returns>Asynchronous load request. Use the <see cref="AssetBundleRequest.allAssets" /> property to get the loaded assets once the operation completes.</returns>
        ///<seealso cref="AssetBundleRequest.allAssets" />
        public AssetBundleRequest LoadAllAssetsAsync<T>()
        {
            return LoadAllAssetsAsync(typeof(T));
        }

        ///<summary>Loads all Assets contained in the AssetBundle asynchronously.</summary>
        ///<param name="type">When specified only main or visible objects that derive from the provided type are returned.</param>
        ///<returns>Asynchronous load request. Use the <see cref="AssetBundleRequest.allAssets" /> property to get the loaded assets once the operation completes.</returns>
        ///<seealso cref="AssetBundleRequest.allAssets" />
        public AssetBundleRequest LoadAllAssetsAsync(Type type)
        {
            if (type == null)
            {
                throw new System.NullReferenceException("The input type cannot be null.");
            }

            return LoadAssetWithSubAssetsAsync_Internal("", type);
        }

        ///<exclude />
        [Obsolete("This method is deprecated.Use GetAllAssetNames() instead.", false)]
        public string[] AllAssetNames()
        {
            return GetAllAssetNames();
        }

        [NativeMethod("LoadAssetAsync_Internal", ThrowsException = true)]
        private extern AssetBundleRequest LoadAssetAsync_Internal(string name, Type type);

        ///<summary>Unloads an AssetBundle freeing its data.</summary>
        ///<remarks>When <c>unloadAllLoadedObjects</c> is false, tracking data structures and any memory buffers holding content of the AssetBundle are freed, but any instances of objects loaded from the bundle remain intact.
        ///
        ///When <c>unloadAllLoadedObjects</c> is true, all objects that were loaded from the bundle are destroyed. If any GameObjects in a Scene reference the destroyed assets, these references become missing.
        ///
        ///In either case no more objects can be loaded from from the bundle unless it is reloaded.
        ///
        ///For example, if a Material <c>M</c> is loaded from AssetBundle <c>AB</c>:
        ///
        ///- <c>AB.Unload(true)</c> destroys all instances of <c>M</c> referenced in the active scene.
        ///- <c>AB.Unload(false)</c> keeps <c>M</c> instances in memory but detaches them from <c>AB</c>, causing duplicates if <c>AB</c> is reloaded.
        ///
        ///**Warning:** Unloading an AssetBundle that serves as a dependency for other asset bundles still in use can lead to undefined behavior. This includes serialization errors that may occur even if the dependency AssetBundle is later reloaded. To avoid such issues, ensure that an AssetBundle and all AssetBundles that depend on it are unloaded together.
        ///
        ///For more information on the different compression formats used and their impact on memory while loaded, refer to [AssetBundle compression formats](xref:assetbundles-compression-format) .</remarks>
        ///<param name="unloadAllLoadedObjects">Determines whether the current instances of objects loaded from the AssetBundle will also be unloaded.</param>
        ///<seealso cref="UnloadAllAssetBundles" />
        ///<seealso cref="UnloadAsync" />
        ///<seealso href="xref:AssetBundles-Native">Using AssetBundles Natively</seealso>
        [NativeMethod("Unload", ThrowsException = true)]
        public extern void Unload(bool unloadAllLoadedObjects);

        ///<summary>Unloads assets in the bundle.</summary>
        ///<remarks>When <c>unloadAllLoadedObjects</c> is false, tracking data structures and any memory buffers holding content of the AssetBundle will be freed. But any instances of objects loaded from this bundle will remain intact.
        ///
        ///When <c>unloadAllLoadedObjects</c> is true, all objects that were loaded from this bundle will be destroyed as well. If there are GameObjects in your Scene referencing those assets, the references to them will become missing.
        ///
        ///After calling UnloadAsync on an AssetBundle, you cannot load any more objects from that bundle and other operations on the bundle will throw InvalidOperationException.
        ///
        ///**Warning:** Unloading an asset bundle that serves as a dependency for other asset bundles still in use can lead to undefined behavior. This includes serialization errors that may occur even if the dependency asset bundle is later reloaded. To avoid such issues, ensure that an asset bundle and all asset bundles that depend on it are unloaded together.</remarks>
        ///<param name="unloadAllLoadedObjects">Set to true to also destroy all objects that were loaded from the AssetBundle. Set to false to keep them.</param>
        ///<returns>Asynchronous unload request for an AssetBundle.</returns>
        ///<seealso cref="UnloadAllAssetBundles" />
        ///<seealso cref="Unload" />
        ///<seealso href="xref:AssetBundles-Native">Using AssetBundles Natively</seealso>
        [NativeMethod("UnloadAsync", ThrowsException = true)]
        public extern AssetBundleUnloadOperation UnloadAsync(bool unloadAllLoadedObjects);

        ///<summary>Return all Asset names in the AssetBundle.</summary>
        ///<remarks>The names are the project-relative path of each Asset file, unless a different name was specified at build time.
        ///
        ///                If the AssetBundle contains Scenes this returns an empty string array.</remarks>
        ///<returns>An array of the names of all the assets in the AssetBundle.</returns>
        ///<seealso cref="P:UnityEditor.AssetBundleBuild.addressableNames" />
        [NativeMethod("GetAllAssetNames")]
        public extern string[] GetAllAssetNames();

        ///<summary>Return all the names of Scenes in the AssetBundle.</summary>
        ///<remarks>The names are the project-relative path of each .unity file, unless a different name was specified at build time.
        ///
        ///                An AssetBundle can store either Scenes or Assets, never a mix of the two.  If the AssetBundle contains only Assets this returns an empty string array.</remarks>
        ///<returns>An array of the paths of all the scenes in the AssetBundle.</returns>
        ///<seealso cref="AssetBundle.isStreamedSceneAssetBundle" />
        ///<seealso cref="M:UnityEngine.SceneManagement.SceneManager.LoadScene" />
        ///<seealso cref="P:UnityEditor.AssetBundleBuild.addressableNames" />
        [NativeMethod("GetAllScenePaths")]
        public extern string[] GetAllScenePaths();

        [NativeMethod("LoadAssetWithSubAssets_Internal", ThrowsException = true)]
        internal extern Object[] LoadAssetWithSubAssets_Internal(string name, Type type);

        [NativeMethod("LoadAssetWithSubAssetsAsync_Internal", ThrowsException = true)]
        private extern AssetBundleRequest LoadAssetWithSubAssetsAsync_Internal(string name, Type type);

        ///<summary>Asynchronously recompress a downloaded/stored AssetBundle from one <see cref="BuildCompression" /> to another.</summary>
        ///<remarks>Method must be a <see cref="BuildCompression" /> whose name ends with Runtime, for example LZ4Runtime, otherwise an ArgumentException is thrown.
        ///When the destination <see cref="BuildCompression" /> is the same as the source, this becomes a copy operation internally, and Unity does not compute a CRC of the uncompressed data. Passing in a non-zero expectedCRC in this case raises a warning, and no CRC validation takes place.</remarks>
        ///<param name="inputPath">Path to the <see cref="AssetBundle" /> to recompress.</param>
        ///<param name="outputPath">Path to the recompressed <see cref="AssetBundle" /> to be generated. Can be the same as inputPath.</param>
        ///<param name="method">The compression method, level and blocksize to use during recompression. Only some <see cref="BuildCompression" /> types are supported (see note).</param>
        ///<param name="expectedCRC">CRC of the <see cref="AssetBundle" /> to test against. Testing this requires additional file reading and computation. Pass in 0 to skip this check. Unity does not compute a CRC when the source and destination <see cref="BuildCompression" /> are the same, so no CRC verification takes place (see note).</param>
        ///<param name="priority">The priority at which the recompression operation should run. This sets thread priority during the operation and does not effect the order in which operations are performed. Recompression operations run on a background worker thread.</param>
        ///<returns>Asynchronous recompress operation. Use the <see cref="AssetBundleRecompressOperation" /> to monitor and control the operation.</returns>
        public static AssetBundleRecompressOperation RecompressAssetBundleAsync(string inputPath, string outputPath, BuildCompression method, UInt32 expectedCRC = 0, ThreadPriority priority = ThreadPriority.Low)
        {
            return RecompressAssetBundleAsync_Internal(inputPath, outputPath, method, expectedCRC, priority);
        }

        [FreeFunction("RecompressAssetBundleAsync_Internal", ThrowsException = true)]
        internal static extern AssetBundleRecompressOperation RecompressAssetBundleAsync_Internal(string inputPath, string outputPath, BuildCompression method, UInt32 expectedCRC, ThreadPriority priority);

        ///<summary>Controls the size of the shared AssetBundle loading cache. Default value is 1MB.</summary>
        ///<remarks>Depending on your AssetBundle build and load strategy, sections of the AssetBundle file may be accessed multiple times. To improve loading performance, the AssetBundle loading cache stores recently accessed pages of the AssetBundle file.
        ///The default cache size should be sufficient in most cases, but the optimal cache size may vary depending on your workload. The optimal size can be determined by measuring how different cache sizes affect the AssetBundle loading times of your specific workload. If you load lots of small objects (e.g. 100 addressable prefabs) individually out of an AssetBundle, a larger cache would likely improve performance since future reads of other objects might reuse cached pages.
        ///If your AssetBundle consists of fewer large objects, or if you read all your objects simultaneously with functions like <see cref="AssetBundle.LoadAll" />, a larger cache may not help since the cached pages will likely not be revisited.</remarks>
        public static uint memoryBudgetKB
        {
            get { return AssetBundleLoadingCache.memoryBudgetKB; }
            set { AssetBundleLoadingCache.memoryBudgetKB = value; }
        }
    }
}
