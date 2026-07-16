// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor.Build.Player;
using UnityEngine;
using UnityEngine.Bindings;

[assembly: InternalsVisibleTo("Unity.ScriptableBuildPipeline.Editor")]
[assembly: InternalsVisibleTo("Unity.ScriptableBuildPipeline.Editor.Tests")]

namespace UnityEditor.Build.Content
{
    ///<summary>Dependency calculation flags to control what is returned, and how it operates.</summary>
    [Flags]
    [NativeHeader("Modules/ContentBuild/Editor/Public/ContentDependencyCollector.h")]
    public enum DependencyType
    {
        ///<summary>Depencency calculation is recursive. For each new valid reference discovered during calculation, the dependencies of those references will also be included in the returned results.</summary>
        RecursiveOperation = 1 << 0,
        ///<summary>Object dependencies returned for only missing references.</summary>
        MissingReferences = 1 << 1,
        ///<summary>Depencency calculation is not recursive. Only dependencies of the initial passed in set will be returned.</summary>
        ValidReferences = 1 << 2,
        ///<summary>Default mode. Dependencies are calculated recursively, and only valid references are returned.</summary>
        DefaultDependencies = RecursiveOperation | ValidReferences
    }

    ///<summary>Low level interface for building content for Unity.</summary>
    ///<remarks>Note: this class and its members exist to provide low-level support for the **Scriptable Build Pipeline** package. This is intended for internal use only; use the &lt;a href="https://docs.unity3d.com/Packages/com.unity.scriptablebuildpipeline@latest/index.html"&gt;Scriptable Build Pipeline package&lt;/a&gt; to implement a fully featured build pipeline. You can install this via the [Package Manager window](/upm-ui.md).</remarks>
    [NativeHeader("Modules/ContentBuild/Editor/SBPSupport/ContentBuildTypes.h")]
    [NativeHeader("Modules/ContentBuild/Editor/SBPSupport/ContentBuildInterface.bindings.h")]
    [NativeHeader("Modules/ContentBuild/Editor/SBPSupport/ContentBuildInterfaceProfile.h")]
    [NativeHeader("Modules/ContentBuild/Editor/SBPSupport/TypeTreeExtractionUtilities.h")]
    [NativeHeader("Modules/ContentBuild/Editor/Public/BuildUtilities.h")]
    [NativeHeader("Modules/ContentBuild/Editor/Public/TraceEventProfile.h")]
    [NativeHeader("Modules/ContentBuild/Editor/BuildUsage/ComputeBuildUsageTagOnObjects.h")]
    [StaticAccessor("BuildPipeline", StaticAccessorType.DoubleColon)]
    public static partial class ContentBuildInterface
    {
        internal static extern void SetGarbageCollectionMemoryIncreaseThreshold(int mbThreshold);
        ///<summary>Returns an array of AssetBundleBuild structs that detail the current AssetBundle layout, as set through the Inspector and stored in the AssetDatabase.</summary>
        ///<remarks>This API does not generate a build, instead it is a convenient function to populate the build definition into the format expected by some signatures of <see cref="BuildPipeline.BuildAssetBundles" />, or the Scriptable Build Pipeline.</remarks>
        ///<seealso cref="BuildPipeline.BuildAssetBundles" />
        ///<seealso cref="AssetDatabase.GetAllAssetBundleNames" />
        ///<seealso cref="AssetDatabase.GetAssetPathsFromAssetBundle" />
        public static extern AssetBundleBuild[] GenerateAssetBundleBuilds();

        ///<summary>Returns the global usage information calculated by the Shader Stripping section of Graphics Settings.</summary>
        ///<remarks>Internal use only. See note on <see cref="Build.Content.ContentBuildInterface" />.</remarks>
        public static extern BuildUsageTagGlobal GetGlobalUsageFromGraphicsSettings();

        ///<summary>Gets information about the lighting and render settings in the active scene.</summary>
        ///<remarks>For internal use only. See note on <see cref="Build.Content.ContentBuildInterface" />.</remarks>
        ///<param name="target">The target platform.</param>
        ///<returns>An object containing the lighting and fog settings for the active scene on the specified platform.</returns>
        public static extern BuildUsageTagGlobal GetGlobalUsageFromActiveScene(BuildTarget target);

        ///<summary>Validates if the object is supported in the build.</summary>
        ///<param name="targetObject">The target object to validate.</param>
        ///<returns>Returns True if the passed in target object is a valid runtime object.</returns>
        public static extern bool ObjectIsSupportedInBuild(UnityEngine.Object targetObject);

        ///<summary>Calculates the Scene dependency information.</summary>
        ///<remarks>Internal use only. See note on <see cref="Build.Content.ContentBuildInterface" />.</remarks>
        ///<param name="scenePath">Input path of the Scene for dependency calculation.</param>
        ///<param name="settings">Settings for dependency calculation.</param>
        ///<param name="usageSet">Output usage tags generated from dependency calculation.</param>
        ///<returns>Dependency information for the Scene.</returns>
        public static SceneDependencyInfo CalculatePlayerDependenciesForScene(string scenePath, BuildSettings settings, BuildUsageTagSet usageSet)
        {
            if (IsBuildInProgress())
                throw new InvalidOperationException("Cannot call CalculatePlayerDependenciesForScene while a build is in progress");
            return CalculatePlayerDependenciesForSceneInternal(scenePath, settings, usageSet, null, DependencyType.DefaultDependencies);
        }

        ///<summary>Calculates the Scene dependency information.</summary>
        ///<remarks>Internal use only. See note on <see cref="Build.Content.ContentBuildInterface" />.</remarks>
        ///<param name="scenePath">Input path of the Scene for dependency calculation.</param>
        ///<param name="settings">Settings for dependency calculation.</param>
        ///<param name="usageSet">Output usage tags generated from dependency calculation.</param>
        ///<param name="usageCache">Optional cache object to use for improving performance with multiple calls to this api.</param>
        ///<returns>Dependency information for the Scene.</returns>
        public static SceneDependencyInfo CalculatePlayerDependenciesForScene(string scenePath, BuildSettings settings, BuildUsageTagSet usageSet, BuildUsageCache usageCache)
        {
            if (IsBuildInProgress())
                throw new InvalidOperationException("Cannot call CalculatePlayerDependenciesForScene while a build is in progress");
            return CalculatePlayerDependenciesForSceneInternal(scenePath, settings, usageSet, usageCache, DependencyType.DefaultDependencies);
        }

        ///<summary>Calculates the Scene dependency information.</summary>
        ///<remarks>Internal use only. See note on <see cref="Build.Content.ContentBuildInterface" />.</remarks>
        ///<param name="scenePath">Input path of the Scene for dependency calculation.</param>
        ///<param name="settings">Settings for dependency calculation.</param>
        ///<param name="usageSet">Output usage tags generated from dependency calculation.</param>
        ///<param name="usageCache">Optional cache object to use for improving performance with multiple calls to this api.</param>
        ///<param name="mode">Specifies how to calculate dependencies between internal scenes and game assets.</param>
        ///<returns>Dependency information for the Scene.</returns>
        public static SceneDependencyInfo CalculatePlayerDependenciesForScene(string scenePath, BuildSettings settings, BuildUsageTagSet usageSet, BuildUsageCache usageCache, DependencyType mode)
        {
            if (IsBuildInProgress())
                throw new InvalidOperationException("Cannot call CalculatePlayerDependenciesForScene while a build is in progress");
            return CalculatePlayerDependenciesForSceneInternal(scenePath, settings, usageSet, usageCache, mode);
        }

        [FreeFunction("CalculatePlayerDependenciesForScene")]
        static extern SceneDependencyInfo CalculatePlayerDependenciesForSceneInternal(string scenePath, BuildSettings settings, BuildUsageTagSet usageSet, BuildUsageCache usageCache, DependencyType mode);

        ///<summary>Returns a list of objects directly contained inside of an asset.</summary>
        ///<remarks>Internal use only. See note on <see cref="Build.Content.ContentBuildInterface" />.</remarks>
        public static extern ObjectIdentifier[] GetPlayerObjectIdentifiersInAsset(GUID asset, BuildTarget target);

        ///<summary>Returns a list of objects directly contained inside of a loose serialized file.</summary>
        ///<remarks>Internal use only. See note on <see cref="Build.Content.ContentBuildInterface" />.</remarks>
        public static extern ObjectIdentifier[] GetPlayerObjectIdentifiersInSerializedFile(string filePath, BuildTarget target);


        ///<summary>Returns a list of objects referenced by an object.</summary>
        ///<param name="objectID">The specific object.</param>
        /// ///<param name="target">The target platform.</param>
        ///<param name="typeDB">The user script TypeDB for the player.</param>
        ///<remarks>Internal use only. See note on <see cref="Build.Content.ContentBuildInterface" />.</remarks>
        public static ObjectIdentifier[] GetPlayerDependenciesForObject(ObjectIdentifier objectID, BuildTarget target, TypeDB typeDB)
        {
            return GetPlayerDependencies_ObjectID(objectID, target, typeDB, DependencyType.DefaultDependencies);
        }

        ///<summary>Returns a list of objects referenced by an object.</summary>
        ///<param name="objectID">The specific object.</param>
        /// ///<param name="target">The target platform.</param>
        ///<param name="typeDB">The user script TypeDB for the player.</param>
        ///<param name="mode">Specifies how to calculate dependencies between internal objects and game assets.</param>
        ///<remarks>Internal use only. See note on <see cref="Build.Content.ContentBuildInterface" />.</remarks>
        public static ObjectIdentifier[] GetPlayerDependenciesForObject(ObjectIdentifier objectID, BuildTarget target, TypeDB typeDB, DependencyType mode)
        {
            return GetPlayerDependencies_ObjectID(objectID, target, typeDB, mode);
        }

        [FreeFunction("GetPlayerDependenciesForObjectID")]
        static extern ObjectIdentifier[] GetPlayerDependencies_ObjectID(ObjectIdentifier objectID, BuildTarget target, TypeDB typeDB, DependencyType mode);


        ///<summary>Returns a list of objects referenced by an object.</summary>
        ///<param name="targetObject">The specific object.</param>
        /// ///<param name="target">The target platform.</param>
        ///<param name="typeDB">The user script TypeDB for the player.</param>
        ///<remarks>Internal use only. See note on <see cref="Build.Content.ContentBuildInterface" />.</remarks>
        public static ObjectIdentifier[] GetPlayerDependenciesForObject(UnityEngine.Object targetObject, BuildTarget target, TypeDB typeDB)
        {
            return GetPlayerDependencies_Object(targetObject, target, typeDB, DependencyType.DefaultDependencies);
        }

        ///<summary>Returns a list of objects referenced by an object.</summary>
        ///<param name="targetObject">The specific object.</param>
        /// ///<param name="target">The target platform.</param>
        ///<param name="typeDB">The user script TypeDB for the player.</param>
        ///<param name="mode">Specifies how to calculate dependencies between internal objects and game assets.</param>
        ///<remarks>Internal use only. See note on <see cref="Build.Content.ContentBuildInterface" />.</remarks>
        public static ObjectIdentifier[] GetPlayerDependenciesForObject(UnityEngine.Object targetObject, BuildTarget target, TypeDB typeDB, DependencyType mode)
        {
            return GetPlayerDependencies_Object(targetObject, target, typeDB, mode);
        }

        [FreeFunction("GetPlayerDependenciesForObject")]
        static extern ObjectIdentifier[] GetPlayerDependencies_Object(UnityEngine.Object targetObject, BuildTarget target, TypeDB typeDB, DependencyType mode);


        ///<summary>Returns a list of objects referenced by an object.</summary>
        ///<param name="objectIDs">The specific object identifiers.</param>
        /// ///<param name="target">The target platform.</param>
        ///<param name="typeDB">The user script TypeDB for the player.</param>
        ///<remarks>Internal use only. See note on <see cref="Build.Content.ContentBuildInterface" />.</remarks>
        public static ObjectIdentifier[] GetPlayerDependenciesForObjects(ObjectIdentifier[] objectIDs, BuildTarget target, TypeDB typeDB)
        {
            return GetPlayerDependencies_ObjectIDs(objectIDs, target, typeDB, DependencyType.DefaultDependencies);
        }

        ///<summary>Returns a list of objects referenced by an object.</summary>
        ///<param name="objectIDs">The specific object identifiers.</param>
        /// ///<param name="target">The target platform.</param>
        ///<param name="typeDB">The user script TypeDB for the player.</param>
        ///<param name="mode">Specifies how to calculate dependencies between internal objects and game assets.</param>
        ///<remarks>Internal use only. See note on <see cref="Build.Content.ContentBuildInterface" />.</remarks>
        public static ObjectIdentifier[] GetPlayerDependenciesForObjects(ObjectIdentifier[] objectIDs, BuildTarget target, TypeDB typeDB, DependencyType mode)
        {
            return GetPlayerDependencies_ObjectIDs(objectIDs, target, typeDB, mode);
        }


        [FreeFunction("GetPlayerDependenciesForObjectIDs")]
        static extern ObjectIdentifier[] GetPlayerDependencies_ObjectIDs(ObjectIdentifier[] objectIDs, BuildTarget target, TypeDB typeDB, DependencyType mode);

        ///<summary>Returns a list of objects referenced by an object.</summary>
        ///<param name="objects">The specific objects.</param>
        /// ///<param name="target">The target platform.</param>
        ///<param name="typeDB">The user script TypeDB for the player.</param>
        ///<remarks>Internal use only. See note on <see cref="Build.Content.ContentBuildInterface" />.</remarks>
        public static ObjectIdentifier[] GetPlayerDependenciesForObjects(UnityEngine.Object[] objects, BuildTarget target, TypeDB typeDB)
        {
            return GetPlayerDependencies_Objects(objects, target, typeDB, DependencyType.DefaultDependencies);
        }

        ///<summary>Returns a list of objects referenced by an object.</summary>
        ///<param name="objects">The specific objects.</param>
        /// ///<param name="target">The target platform.</param>
        ///<param name="typeDB">The user script TypeDB for the player.</param>
        ///<param name="mode">Specifies how to calculate dependencies between internal objects and game assets.</param>
        ///<remarks>Internal use only. See note on <see cref="Build.Content.ContentBuildInterface" />.</remarks>
        public static ObjectIdentifier[] GetPlayerDependenciesForObjects(UnityEngine.Object[] objects, BuildTarget target, TypeDB typeDB, DependencyType mode)
        {
            return GetPlayerDependencies_Objects(objects, target, typeDB, mode);
        }

        [FreeFunction("GetPlayerDependenciesForObjects")]
        static extern ObjectIdentifier[] GetPlayerDependencies_Objects(UnityEngine.Object[] objects, BuildTarget target, TypeDB typeDB, DependencyType mode);

        ///<summary>Returns a list of visible objects directly contained inside of an asset.</summary>
        ///<remarks>The returned objects are not loaded, and thus this method is more performant than <see cref="AssetDatabase.LoadAllAssetRepresentationsAtPath" />.</remarks>
        public static extern ObjectIdentifier[] GetPlayerAssetRepresentations(GUID asset, BuildTarget target);


        ///<summary>Calculates the build usage of a set of objects.</summary>
        ///<remarks>Internal use only. See note on <see cref="Build.Content.ContentBuildInterface" />.
        ///
        ///To calculate how any given Object is being used in a build, we need two pieces of information. First, we need to know that Object's dependents, or in other words, what references that Object. For example, for a Shader, we would need to know the list Materials that reference that shader. Second, we need the combined lighting information for Scenes where the Object can be used. Using these two pieces of information, we calculate the correct usage information for an Object, and then store that information in the <see cref="BuildUsageTagSet" />.</remarks>
        ///<param name="objectIDs">Objects that will have their build usage calculated.</param>
        ///<param name="dependentObjectIDs">Objects that reference the Objects being calculated.</param>
        ///<param name="globalUsage">Lighting information used by the build.</param>
        ///<param name="usageSet">The BuildUsageTagSet where the calculated usage information will be stored.</param>
        public static void CalculateBuildUsageTags(ObjectIdentifier[] objectIDs, ObjectIdentifier[] dependentObjectIDs, BuildUsageTagGlobal globalUsage, BuildUsageTagSet usageSet)
        {
            CalculateBuildUsageTags(objectIDs, dependentObjectIDs, globalUsage, usageSet, null);
        }

        ///<summary>Calculates the build usage of a set of objects.</summary>
        ///<remarks>Internal use only. See note on <see cref="Build.Content.ContentBuildInterface" />.
        ///
        ///To calculate how any given Object is being used in a build, we need two pieces of information. First, we need to know that Object's dependents, or in other words, what references that Object. For example, for a Shader, we would need to know the list Materials that reference that shader. Second, we need the combined lighting information for Scenes where the Object can be used. Using these two pieces of information, we calculate the correct usage information for an Object, and then store that information in the <see cref="BuildUsageTagSet" />.</remarks>
        ///<param name="objectIDs">Objects that will have their build usage calculated.</param>
        ///<param name="dependentObjectIDs">Objects that reference the Objects being calculated.</param>
        ///<param name="globalUsage">Lighting information used by the build.</param>
        ///<param name="usageSet">The BuildUsageTagSet where the calculated usage information will be stored.</param>
        ///<param name="usageCache">Optional cache object to use for improving performance with multiple calls to this api.</param>
        public static extern void CalculateBuildUsageTags(ObjectIdentifier[] objectIDs, ObjectIdentifier[] dependentObjectIDs, BuildUsageTagGlobal globalUsage, BuildUsageTagSet usageSet, BuildUsageCache usageCache);

        ///<summary>Returns the System.Type of the <see cref="ObjectIdentifier" /> specified by <c>objectID</c>.</summary>
        ///<remarks>Internal use only. See note on <see cref="Build.Content.ContentBuildInterface" />.</remarks>
        ///<param name="objectID">The specific object.</param>
        ///<returns>The type of the object.</returns>
        public static extern Type GetTypeForObject(ObjectIdentifier objectID);
        ///<summary>Returns the System.Type of the <see cref="ObjectIdentifier" /> and the referenced <see cref="SerializeReference" /> class types specified by <c>objectID</c>.</summary>
        ///<remarks>Internal use only. See note on <see cref="Build.Content.ContentBuildInterface" />.</remarks>
        ///<param name="objectID">The specific object.</param>
        ///<returns>The array of unique types.</returns>
        public static extern Type[] GetTypesForObject(ObjectIdentifier objectID);
        ///<summary>Returns the System.Type of the <see cref="ObjectIdentifier" />s and the referenced <see cref="SerializeReference" /> class types specified by <c>objectIDs</c>.</summary>
        ///<remarks>The results do not directly correspond to the input <see cref="ObjectIdentifiers" /> by index as a single <see cref="ObjectIdentifier" /> can return multiple types.
        ///Use <see cref="Build.Content.ContentBuildInterface.GetTypesForObject" /> or <see cref="Build.Content.ContentBuildInterface.GetTypeForObject" /> if you want that information.
        ///Internal use only. See note on <see cref="Build.Content.ContentBuildInterface" />.</remarks>
        ///<param name="objectIDs">The specific objects.</param>
        ///<returns>The array of unique types.</returns>
        public static extern Type[] GetTypeForObjects(ObjectIdentifier[] objectIDs);

        internal static extern bool IsBuildInProgress();

        ///<summary>Writes objects to a serialized file on disk.</summary>
        ///<remarks>Internal use only. See note on <see cref="Build.Content.ContentBuildInterface" />.</remarks>
        public static WriteResult WriteSerializedFile(string outputFolder, WriteParameters parameters)
        {
            if (IsBuildInProgress())
                throw new InvalidOperationException("Cannot call WriteSerializedFile while a build is in progress");
            if (string.IsNullOrEmpty(outputFolder))
                throw new ArgumentException("String is null or empty.", "outputFolder");
            if (parameters.writeCommand == null)
                throw new ArgumentNullException("parameters.writeCommand");
            if (parameters.referenceMap == null)
                throw new ArgumentNullException("parameters.referenceMap");
            if (parameters.settings.buildFlags.HasFlag(ContentBuildFlags.ExtractTypeTree) &&
                parameters.settings.buildFlags.HasFlag(ContentBuildFlags.DisableWriteTypeTree))
                throw new ArgumentException("Incompatible ContentBuildFlags - ExtractTypeTree cannot be used with DisableWriteTypeTree.");

            return WriteSerializedFile_Internal(outputFolder, parameters.writeCommand, parameters.settings, parameters.globalUsage, parameters.usageSet, parameters.referenceMap, parameters.preloadInfo?.preloadObjects,  parameters.bundleInfo?.bundleName, parameters.bundleInfo?.bundleAssets);
        }

        [NativeMethod(ThrowsException = true)]
        static extern WriteResult WriteSerializedFile_Internal(string outputFolder, WriteCommand writeCommand, BuildSettings settings, BuildUsageTagGlobal globalUsage, BuildUsageTagSet usageSet, BuildReferenceMap referenceMap, List<ObjectIdentifier> preloadObjects, string bundleName, List<AssetLoadInfo> bundleAssets);
        ///<summary>Writes Scene objects to a serialized file on disk.</summary>
        ///<remarks>Internal use only. See note on <see cref="Build.Content.ContentBuildInterface" />.</remarks>
        ///<param name="outputFolder">The location to write the file to.</param>
        ///<param name="parameters">The set of parameters used to write the file.</param>
        ///<returns>The detailed results from writing the file.</returns>
        public static WriteResult WriteSceneSerializedFile(string outputFolder, WriteSceneParameters parameters)
        {
            if (IsBuildInProgress())
                throw new InvalidOperationException("Cannot call WriteSceneSerializedFile while a build is in progress");
            if (string.IsNullOrEmpty(outputFolder))
                throw new ArgumentException("String is null or empty.", "outputFolder");
            if (parameters.writeCommand == null)
                throw new ArgumentNullException("parameters.writeCommand");
            if (parameters.referenceMap == null)
                throw new ArgumentNullException("parameters.referenceMap");
            if (parameters.settings.buildFlags.HasFlag(ContentBuildFlags.ExtractTypeTree) &&
                parameters.settings.buildFlags.HasFlag(ContentBuildFlags.DisableWriteTypeTree))
                throw new ArgumentException("Incompatible ContentBuildFlags - ExtractTypeTree cannot be used with DisableWriteTypeTree.");

            return WriteSceneSerializedFile_Internal(outputFolder, parameters.scenePath, parameters.writeCommand, parameters.settings, parameters.globalUsage, parameters.usageSet, parameters.referenceMap, parameters.preloadInfo?.preloadObjects, parameters.sceneBundleInfo?.bundleName, parameters.sceneBundleInfo?.bundleScenes);
        }

        [NativeMethod(ThrowsException = true)]
        static extern WriteResult WriteSceneSerializedFile_Internal(string outputFolder, string scenePath, WriteCommand writeCommand, BuildSettings settings, BuildUsageTagGlobal globalUsage, BuildUsageTagSet usageSet, BuildReferenceMap referenceMap, List<ObjectIdentifier> preloadObjects, string bundleName, List<SceneLoadInfo> bundleScenes);

        ///<summary>Writes the current settings of internal Unity game manager classes to the 'globalgamemanagers' file on disk.</summary>
        ///<remarks>Internal use only. See note on <see cref="Build.Content.ContentBuildInterface" />.</remarks>
        ///<param name="outputFolder">The location to write the file to.</param>
        ///<param name="parameters">The set of parameters used to write the file.</param>
        ///<returns>The detailed results from writing the file.</returns>
        [Obsolete("WriteGameManagersSerializedFile will be removed in a future version. Calling the function will throw an exception.", false)]
        public static WriteResult WriteGameManagersSerializedFile(string outputFolder, WriteManagerParameters parameters)
        {
            return WriteGameManagersSerializedFileRaw(outputFolder, parameters.settings, parameters.globalUsage, parameters.referenceMap);
        }

        static extern WriteResult WriteGameManagersSerializedFileRaw(string outputFolder, BuildSettings settings, BuildUsageTagGlobal globalUsage, BuildReferenceMap referenceMap);

        ///<summary>Create a Unity archive file, containing the content of one or more resource files.</summary>
        ///<remarks>Generate a Unity Archive file.  This low level API is exposed primarily for use by the **Scriptable Build Pipeline** package.
        ///For example, when building AssetBundles using [[BuildPipeline.BuildAssetBundles]] it is not necessary to call this API because the AssetBundle Archive files are created automatically.
        ///
        /// SA: [[Unity.IO.Archive.ArchiveFileInterface]].</remarks>
        ///<param name="resourceFiles">Array of [[ResourceFile]] structs pointing to the files that should be copied into the Archive.</param>
        ///<param name="outputBundlePath">File path of the output Archive file.</param>
        ///<param name="compression">Type of compression to apply to the content of the Archive.</param>
        ///<returns>The CRC of the archive. Returns 0 if operation failed.</returns>
        public static uint ArchiveAndCompress(ResourceFile[] resourceFiles, string outputBundlePath,
            UnityEngine.BuildCompression compression)
        {
            return ArchiveAndCompress(resourceFiles, outputBundlePath, compression, false);
        }

        // The ThreadSafe attribute was removed, but an Addressable package test checks that ArchiveAndCompress has a ThreadSafe attribute.
        private class ThreadSafeAttribute : Attribute {}

        ///<summary>Create a Unity archive file, containing the content of one or more resource files.</summary>
        ///<remarks>Generate a Unity Archive file.  This low level API is exposed primarily for use by the **Scriptable Build Pipeline** package.
        ///For example, when building AssetBundles using [[BuildPipeline.BuildAssetBundles]] it is not necessary to call this API because the AssetBundle Archive files are created automatically.
        ///
        /// SA: [[Unity.IO.Archive.ArchiveFileInterface]].</remarks>
        ///<param name="resourceFiles">Array of [[ResourceFile]] structs pointing to the files that should be copied into the Archive.</param>
        ///<param name="outputBundlePath">File path of the output Archive file.</param>
        ///<param name="compression">Type of compression to apply to the content of the Archive.</param>
        ///<param name="stripUnityVersion">By default the Archive file will record the version of the Unity Editor that created the Archive.  When true is passed for this parameter the version will not be recorded in the Archive header.
        ///This can be useful when rebuilding AssetBundles after a minor upgrade of the Unity Editor, to make sure otherwise identical AssetBundles generate the exact same full-file content.
        ///Note: The CRC and hash values calculated by Unity for AssetBundles ignore the Archive Header. So it is not necessary to strip the Unity Version in the Archive Header when using those for integrity and version tracking.</param>
        ///<returns>The CRC of the archive. Returns 0 if operation failed.</returns>
        [NativeMethod(IsThreadSafe = true)]
        [ContentBuildInterface.ThreadSafe] // Unused see above
        public static extern uint ArchiveAndCompress(ResourceFile[] resourceFiles, string outputBundlePath, UnityEngine.BuildCompression compression, bool stripUnityVersion);

        ///<summary>Starts a profile capture to record content build profile events.</summary>
        ///<remarks>Throws an InvalidOperationException if the previous profile capture hasn't stopped.</remarks>
        ///<param name="options">Used to filter captured events.</param>
        [NativeMethod(ThrowsException = true)]
        extern public static void StartProfileCapture(ProfileCaptureOptions options);

        ///<summary>Returns an array of ContentBuildProfileEvent structs that contain information for each occuring event. Also stops the profile capture.</summary>
        ///<remarks>Throws an InvalidOperationException if no profile capture has started.</remarks>
        [NativeMethod(ThrowsException = true)]
        extern public static ContentBuildProfileEvent[] StopProfileCapture();

        ///<summary>Returns a unique hash for a given type's serialized layout.</summary>
        ///<remarks>Passing in null will provide a hash for the serialized layout of the type as it exists in the editor, passing in a valid <see cref="TypeDB" /> for a player will provide a hash for the layout as it exists in the player.
        ///
        ///Internal use only. See note on <see cref="Build.Content.ContentBuildInterface" />.</remarks>
        ///<param name="type">The type of the object.</param>
        ///<param name="typeDB">The user script TypeDB for the player.</param>
        ///<returns>The unique hash for a type's serialized layout.</returns>
        public static extern UnityEngine.Hash128 CalculatePlayerSerializationHashForType(Type type, TypeDB typeDB);

        extern internal static int BeginTraceProfileBlock(string name);
        extern internal static void EndTraceProfileBlock(int index);

        extern internal static void BeginBuildUsageWarningScope();
        extern internal static void EndBuildUsageWarningScope();

        /// <summary>
        /// Combines multiple TypeTree archive files into a single archive.  All duplicated entries are removed.
        /// </summary>
        /// <param name="paths">The paths of the files to combine.</param>
        /// <param name="ouputPath">The path of the combined archive.</param>
        /// <returns>True if the operations succeeds.</returns>
        public static extern bool CombineExtractedTypeTreeDataFiles(string[] paths, string ouputPath);

        /// <summary>
        /// Creates a new TypeTree archive that contains only the hashes and data from the source file that are not in the array of hashes passed in.
        /// </summary>
        /// <param name="hashes">The hashes of the data to exclude.</param>
        /// <param name="srcPath">The path of the source file.</param>
        /// <param name="ouputPath">The path of new file that will contain the stripped set of TypeTree data.</param>
        /// <returns>True if the operation was successful.</returns>
        public static extern bool StripTypeTreeDataFromFile(Hash128[] hashes, string srcPath, string ouputPath);

        /// <summary>
        /// Loads the content hashes from a TypeTree archive file.
        /// </summary>
        /// <param name="srcPath">The path of the TypeTree archive.</param>
        /// <returns>An array of Hash128 hashes.</returns>
        public static extern Hash128[] LoadTypeTreeDataHashesFromFile(string srcPath);
    }

    internal struct ScopeTraceProfileBlock : IDisposable
    {
        private readonly int m_Index;

        public ScopeTraceProfileBlock(string name)
        {
            m_Index = ContentBuildInterface.BeginTraceProfileBlock(name);
        }

        public void Dispose()
        {
            ContentBuildInterface.EndTraceProfileBlock(m_Index);
        }
    }

    internal sealed class BuildUsageWarningScope : IDisposable
    {
        public BuildUsageWarningScope()
        {
            ContentBuildInterface.BeginBuildUsageWarningScope();
        }

        public void Dispose()
        {
            ContentBuildInterface.EndBuildUsageWarningScope();
        }
    }
}
