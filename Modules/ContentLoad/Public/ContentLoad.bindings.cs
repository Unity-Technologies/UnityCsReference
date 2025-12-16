// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine.Bindings;
using Unity.Jobs;
using UnityEngine.SceneManagement;
using Unity.Content;

namespace Unity.Loading
{
    enum ContentFileReservedID
    {
        None = 0,
        ResolveReferencesWithPM = 1
    }

    ///<summary>The loading status of a <see cref="Unity.Loading.ContentFile" />.</summary>
    public enum LoadingStatus
    {
        ///<summary>The <see cref="Unity.Loading.ContentFile" /> is actively loading.</summary>
        InProgress,
        ///<summary>The <see cref="Unity.Loading.ContentFile" /> has loaded successfully.</summary>
        Completed,
        ///<summary>The <see cref="Unity.Loading.ContentFile" /> failed to load. Be sure to still call <see cref="Unity.Loading.ContentFile.UnloadAsync" /> to free internal resources.</summary>
        Failed
    }

    ///<summary>A handle that can be used to track the progress of an unload operation. This is returned from <see cref="Unity.Loading.ContentFile.UnloadAsync" />.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ContentFileUnloadHandle
    {
        internal ContentFile Id;
        ///<summary>Returns true if the unload operation has completed.</summary>
        public bool IsCompleted { get { return ContentLoadInterface.ContentFile_IsUnloadComplete(Id); } }
        ///<summary>Blocks on the main thread until the unload operation completes. This function can be slow and so should be used carefully to avoid frame rate stuttering.</summary>
        ///<param name="timeoutMs">The maximum time in milliseconds this function will wait before returning. Pass 0 to block indefinitely until completion.</param>
        ///<returns>Returns false if the timeout was reached before ContentFile completed loading.</returns>
        public bool WaitForCompletion(int timeoutMs) { return ContentLoadInterface.WaitForUnloadCompletion(Id, timeoutMs); }
    }

    ///<summary>This struct acts like a handle for accessing a file loaded by <see cref="Unity.Loading.ContentLoadInterface.LoadContentFileAsync(ContentNamespace, string, NativeArray&lt;ContentFile&gt;, JobHandle)" />. You can use it to access the status and results of the load operation.</summary>
    [StructLayout(LayoutKind.Sequential)]
    unsafe public struct ContentFile
    {
        internal UInt64 Id;


        ///<summary>Begin an asynchronous unload of the <see cref="Unity.Loading.ContentFile" />.</summary>
        ///<remarks>If you call this while a load is in progress, it will attempt to cancel the loading process.
        ///
        ///This must be called in order to release resources, even when the <see cref="Unity.Loading.ContentFile" /> load operation was not successful.
        ///
        ///An exception will be thrown if this <see cref="Unity.Loading.ContentFile" /> is being used as a dependency by another <see cref="Unity.Loading.ContentFile" />. In this case, the other file must be unloaded first.</remarks>
        ///<returns>A handle that can be used to track the status of the unload operation.</returns>
        public ContentFileUnloadHandle UnloadAsync()
        {
            ThrowIfInvalidHandle();
            ContentLoadInterface.ContentFile_UnloadAsync(this);
            return new ContentFileUnloadHandle { Id = this };
        }

        ///<summary>This function can be used to access all the Objects loaded in the <see cref="Unity.Loading.ContentFile" />.</summary>
        ///<remarks>This method will raise an exception if when called <see cref="Unity.Loading.ContentFile.LoadingStatus" /> is not <see cref="Unity.Loading.LoadingStatus.Completed" />.</remarks>
        ///<returns>All the Objects within the <see cref="Unity.Loading.ContentFile" />.</returns>
        public UnityEngine.Object[] GetObjects()
        {
            ThrowIfNotComplete();
            return ContentLoadInterface.ContentFile_GetObjects(this);
        }

        ///<summary>Used to access objects within the <see cref="Unity.Loading.ContentFile" /> by local file identifier.</summary>
        ///<param name="localIdentifierInFile">The local file identifier.</param>
        ///<remarks>This method will raise an exception if when called <see cref="Unity.Loading.ContentFile.LoadingStatus" /> is not <see cref="Unity.Loading.LoadingStatus.Completed" />.</remarks>
        ///<returns>The loaded Object from the  <see cref="Unity.Loading.ContentFile" />.</returns>
        public UnityEngine.Object GetObject(UInt64 localIdentifierInFile)
        {
            ThrowIfNotComplete();
            return ContentLoadInterface.ContentFile_GetObject(this, localIdentifierInFile);
        }

        private void ThrowIfInvalidHandle()
        {
            if (!IsValid)
                throw new Exception("The ContentFile operation cannot be performed because the handle is invalid. Did you already unload it?");
        }

        private void ThrowIfNotComplete()
        {
            LoadingStatus status = LoadingStatus;
            if (status == LoadingStatus.Failed)
                throw new Exception("Cannot use a failed ContentFile operation.");
            if (status == LoadingStatus.InProgress)
                throw new Exception("This ContentFile functionality is not supported while loading is in progress");
        }

        ///<summary>Returns true if the <see cref="Unity.Loading.ContentFile" /> handle is valid.</summary>
        ///<remarks>This will be true immediately after the handle is returned from <see cref="Unity.Loading.ContentLoadInterface.LoadContentFileAsync(ContentNamespace, string, NativeArray&lt;ContentFile&gt;, JobHandle)" />. It becomes false after <see cref="Unity.Loading.ContentFile.UnloadAsync" /> is called.</remarks>
        public bool IsValid { get { return ContentLoadInterface.ContentFile_IsHandleValid(this); } }

        ///<summary>The loading status of the <see cref="Unity.Loading.ContentFile" />.</summary>
        public LoadingStatus LoadingStatus {
            get {
                ThrowIfInvalidHandle();
                return ContentLoadInterface.ContentFile_GetLoadingStatus(this);
            }
        }

        ///<summary>Blocks on the main thread until the load operation completes. This function can be slow and so should be used carefully to avoid frame rate stuttering.</summary>
        ///<param name="timeoutMs">The maximum time in milliseconds this function will wait before returning. Pass 0 to block indefinitely until completion.</param>
        ///<returns>Returns false if the timeout was reached before ContentFile completed loading.</returns>
        public bool WaitForCompletion(int timeoutMs)
        {
            ThrowIfInvalidHandle();
            return ContentLoadInterface.WaitForLoadCompletion(this, timeoutMs);
        }

        ///<summary>This <see cref="ContentFile" /> can be passed as a dependency to <see cref="Unity.Loading.ContentLoadInterface.LoadContentFileAsync(ContentNamespace, string, NativeArray&lt;ContentFile&gt;, JobHandle)" /> or <see cref="Unity.Loading.ContentLoadInterface.LoadSceneAsync(ContentNamespace, string, string, ContentSceneParameters, NativeArray&lt;ContentFile&gt;, JobHandle)" /> to indicate that the external file dependencies should be resolved through the global PersistentManager table. For example, this could be used when the <see cref="ContentFile" /> references a file loaded through the PersistentManager such as "unity default resources".</summary>
        public static ContentFile GlobalTableDependency
        {
            get { return new ContentFile { Id = (int)ContentFileReservedID.ResolveReferencesWithPM }; }
        }
    }

    ///<summary>The loading status of a <see cref="Unity.Loading.ContentSceneFile" />. This is accessed by <see cref="ContentSceneFile.Status" />.</summary>
    public enum SceneLoadingStatus
    {
        ///<summary>Indicates that the scene has not started loading. This is the default value for <see cref="ContentSceneFile.Status" /> before any load operation is initiated.
        ///                    Use this value to detect uninitialized or invalid <see cref="ContentSceneFile" /> instances, such as those not returned by <see cref="Unity.Loading.ContentLoadInterface.LoadSceneAsync(ContentNamespace, string, string, ContentSceneParameters, NativeArray&lt;ContentFile&gt;, JobHandle)" /> or after unloading.</summary>
        NotLoaded,
        ///<summary>The scene load is in progress.</summary>
        InProgress,
        ///<summary>The asynchronous part of the scene loading is complete. You can now safely call <see cref="Unity.Loading.ContentSceneFile.IntegrateAtEndOfFrame" /> when you are ready to activate the scene.</summary>
        WaitingForIntegrate,
        ///<summary>The scene will integrate at the end of the current frame.</summary>
        WillIntegrateNextFrame,
        ///<summary>The scene has been loaded and integrated successfully.</summary>
        Complete,
        ///<summary>A failure occured in the scene loading process. See log for details.
        ///Be sure to still call <see cref="Unity.Loading.ContentSceneFile.UnloadAtEndOfFrame" /> to release internal resources.</summary>
        Failed
    }

    ///<summary>This struct collects all the <see cref="Unity.Loading.ContentLoadInterface.LoadSceneAsync(ContentNamespace, string, string, ContentSceneParameters, NativeArray&lt;ContentFile&gt;, JobHandle)" /> parameters in to a single place.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ContentSceneParameters
    {
        [NativeName("LoadSceneMode")]
        internal LoadSceneMode m_LoadSceneMode;
        [NativeName("LocalPhysicsMode")]
        internal LocalPhysicsMode m_LocalPhysicsMode;
        [NativeName("AutoIntegrate")]
        internal bool m_AutoIntegrate;

        ///<summary>See <see cref="LoadSceneMode" />.</summary>
        public LoadSceneMode loadSceneMode { get { return m_LoadSceneMode; } set { m_LoadSceneMode = value; } }
        ///<summary>See <see cref="LocalPhysicsMode" />.</summary>
        public LocalPhysicsMode localPhysicsMode { get { return m_LocalPhysicsMode; } set { m_LocalPhysicsMode = value; } }
        ///<summary>True if the scene should be automatically integrated after the load completes. If this is set to false, the user must call <see cref="Unity.Loading.ContentSceneFile.IntegrateAtEndOfFrame" /> when they are ready to integrate.</summary>
        public bool autoIntegrate { get { return m_AutoIntegrate; } set { m_AutoIntegrate = value; } }
    }

    ///<summary>The handle returned from <see cref="Unity.Loading.ContentLoadInterface.LoadSceneAsync(ContentNamespace, string, string, ContentSceneParameters, NativeArray&lt;ContentFile&gt;, JobHandle)" />. You can use this handle to access the status and results of the load operation.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ContentSceneFile
    {
        internal UInt64 Id;
        ///<summary>The Scene object being loaded. This is accessible both before and after the load operation completes.</summary>
        public Scene Scene { get { ThrowIfInvalidHandle(); return ContentLoadInterface.ContentSceneFile_GetScene(this); } }

        ///<summary>Calling this will cause the scene to integrate at the end of the frame.</summary>
        ///<remarks>It's unnecessary to call this if you set the <see cref="Unity.Loading.ContentSceneParameters.autoIntegrate" /> property to true.
        ///This should only be called when the <see cref="Unity.Loading.ContentSceneFile.Status" /> is <see cref="Unity.Loading.SceneLoadingStatus.WaitingForIntegrate" />.</remarks>
        public void IntegrateAtEndOfFrame() { ThrowIfInvalidHandle(); ContentLoadInterface.ContentSceneFile_IntegrateAtEndOfFrame(this); }
        ///<summary>The loading status of the scene.</summary>
        public SceneLoadingStatus Status { get { ThrowIfInvalidHandle(); return ContentLoadInterface.ContentSceneFile_GetStatus(this); } }

        ///<summary>Will trigger the scene to unload at the end of the frame.</summary>
        ///<remarks>This must be called on a <see cref="Unity.Loading.ContentSceneFile" /> even if the status is <see cref="Unity.Loading.SceneLoadingStatus.Failed" /> in order to release internal resources.
        ///
        ///The call will fail if the scene is not in an unloadable state. For example, it is actively being loaded or unloaded.</remarks>
        ///<returns>True if successful. False if the scene is not in a state that can be unloaded.</returns>
        public bool UnloadAtEndOfFrame() { ThrowIfInvalidHandle(); return ContentLoadInterface.ContentSceneFile_UnloadAtEndOfFrame(this); }
        ///<summary>Blocks on the main thread until the load operation completes. This function can be slow and so should be used carefully to avoid frame rate stuttering.</summary>
        ///<param name="timeoutMs">The maximum time in milliseconds this function will wait before returning. Pass 0 to block indefinitely until completion.</param>
        ///<returns>Returns false if the timeout was reached before ContentFile completed loading.</returns>
        public bool WaitForLoadCompletion(int timeoutMs) { ThrowIfInvalidHandle(); return ContentLoadInterface.ContentSceneFile_WaitForCompletion(this, timeoutMs); }

        ///<summary>Returns true if the <see cref="Unity.Loading.ContentSceneFile" /> handle is valid. A handle becomes invalid after the file is unloaded.</summary>
        public bool IsValid { get { return ContentLoadInterface.ContentSceneFile_IsHandleValid(this); } }

        private void ThrowIfInvalidHandle()
        {
            if (!IsValid)
                throw new Exception("The ContentSceneFile operation cannot be performed because the handle is invalid. Did you already unload it?");
        }
    }

    ///<summary>API Interface for loading and unloading Content files.</summary>
    [NativeHeader("Modules/ContentLoad/Public/ContentLoadFrontend.h")]
    [StaticAccessor("GetContentLoadFrontend()", StaticAccessorType.Dot)]
    public static class ContentLoadInterface
    {

        [NativeThrows]
        unsafe internal static extern ContentFile LoadContentFileAsync(ContentNamespace nameSpace, string filename, void *dependencies, int dependencyCount, JobHandle dependentFence, bool useUnsafe = false);

        [NativeThrows]
        internal extern static void ContentFile_UnloadAsync(ContentFile handle);

        internal extern static UnityEngine.Object ContentFile_GetObject(ContentFile handle, UInt64 localIdentifierInFile);

        internal extern static UnityEngine.Object [] ContentFile_GetObjects(ContentFile handle);

        internal extern static LoadingStatus ContentFile_GetLoadingStatus(ContentFile handle);

        internal extern static bool ContentFile_IsHandleValid(ContentFile handle);

        internal extern static float IntegrationTimeMS  { get; set; }

        internal extern static bool WaitForLoadCompletion(ContentFile handle, int timeoutMs);

        internal extern static bool WaitForUnloadCompletion(ContentFile handle, int timeoutMs);

        internal extern static bool ContentFile_IsUnloadComplete(ContentFile handle);

        [NativeThrows]
        extern unsafe internal static ContentSceneFile LoadSceneAsync(ContentNamespace nameSpace, string filename, string sceneName, ContentSceneParameters sceneParams, ContentFile *dependencies, int dependencyCount, JobHandle dependentFence);

        extern internal static Scene ContentSceneFile_GetScene(ContentSceneFile handle);
        extern internal static SceneLoadingStatus ContentSceneFile_GetStatus(ContentSceneFile handle);
        [NativeThrows]
        extern internal static void ContentSceneFile_IntegrateAtEndOfFrame(ContentSceneFile handle);

        extern internal static bool ContentSceneFile_UnloadAtEndOfFrame(ContentSceneFile handle);
        internal extern static bool ContentSceneFile_IsHandleValid(ContentSceneFile handle);
        internal extern static bool ContentSceneFile_WaitForCompletion(ContentSceneFile handle, int timeoutMs);

        ///<summary>Loads a scene serialized file asynchronously from disk.</summary>
        ///<param name="dependencies">List of the ContentFiles that will be referenced by the file being loaded. The ordering must match the ordering returned from the build process.
        ///<see cref="Unity.Loading.ContentFile.GlobalTableDependency" /> can be used to indicate that the PersistentManager should be used to resolve references. This allows references to files such as "unity default resources".</param>
        ///<param name="nameSpace">The ContentNamespace used to filter the results.</param>
        ///<param name="filename">Path of the file on disk.</param>
        ///<param name="sceneName">The name that will be applied to the scene.</param>
        ///<param name="sceneParams">Struct that collects the various parameters into a single place.</param>
        ///<param name="dependentFence">The load will not begin until this JobHandle completes.</param>
        ///<returns>Handle to access the results of the load process.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using System.Collections;
        ///using Unity.Collections;
        ///using Unity.Content;
        ///using Unity.Loading;
        ///using UnityEngine;
        ///using UnityEngine.SceneManagement;
        ///
        ///public class SampleBehaviour : MonoBehaviour
        ///{
        ///    public IEnumerator Start()
        ///    {
        ///        NativeArray<ContentFile> empty = new NativeArray<ContentFile>();
        ///        ContentFile depFileHandle = ContentLoadInterface.LoadContentFileAsync(ContentNamespace.Default, "path/to/depfile", empty);
        ///
        ///        var sceneParams = new ContentSceneParameters();
        ///        sceneParams.loadSceneMode = LoadSceneMode.Additive;
        ///        sceneParams.localPhysicsMode = LocalPhysicsMode.None;
        ///        sceneParams.autoIntegrate = false;
        ///
        ///        NativeArray<ContentFile> files = new NativeArray<ContentFile>(1, Allocator.Temp, NativeArrayOptions.ClearMemory);
        ///        files[0] = depFileHandle;
        ///        ContentSceneFile op = ContentLoadInterface.LoadSceneAsync(ContentNamespace.Default, "path/to/scene", "TestScene", sceneParams, files);
        ///        files.Dispose();
        ///
        ///        // wait until the async loading process completes
        ///        while (op.Status == SceneLoadingStatus.InProgress)
        ///            yield return null;
        ///
        ///        op.IntegrateAtEndOfFrame();
        ///
        ///        // wait one frame
        ///        yield return null;
        ///
        ///        // scene is now loaded and integrated ...
        ///
        ///        // unload scene
        ///        op.UnloadAtEndOfFrame();
        ///        yield return null;
        ///
        ///        ContentFileUnloadHandle unloadHandleDep = depFileHandle.UnloadAsync();
        ///
        ///        while (!unloadHandleDep.IsCompleted)
        ///            yield return null;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static ContentSceneFile LoadSceneAsync(ContentNamespace nameSpace, string filename, string sceneName, ContentSceneParameters sceneParams, NativeArray<ContentFile> dependencies, JobHandle dependentFence = new JobHandle())
        {
            unsafe
            {
                return LoadSceneAsync(nameSpace, filename, sceneName, sceneParams, (ContentFile*)dependencies.m_Buffer, dependencies.Length, dependentFence);
            }
        }


        ///<summary>Loads a serialized file asynchronously from disk.</summary>
        ///<remarks>The status of the load operation can be accessed using the returned <see cref="Unity.Loading.ContentFile" />. Objects loaded with this function will not be garbage collected; the user is responsible for calling <see cref="Unity.Loading.ContentFile.UnloadAsync" /> to free resources when they are no longer required. The user must call <see cref="Unity.Loading.ContentFile.UnloadAsync" /> even if the load fails.</remarks>
        ///<param name="nameSpace">The <see cref="Unity.Content.ContentNamespace" /> used to filter the results.</param>
        ///<param name="filename">Path of the file on disk.</param>
        ///<param name="dependencies">List of the <see cref="Unity.Loading.ContentFile" />s that will be referenced by the file being loaded. The ordering must match the ordering returned from the build process.
        ///<see cref="Unity.Loading.ContentFile.GlobalTableDependency" /> can be used to indicate that the PersistentManager should be used to resolve references. This allows references to files such as "unity default resources".</param>
        ///<param name="dependentFence">The load will not begin until this <see cref="Unity.Jobs.JobHandle" /> completes. A default <see cref="Unity.Jobs.JobHandle" /> can be used if there is no dependency.</param>
        ///<returns>Handle to access the results of the load process.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using System.Collections;
        ///using Unity.Collections;
        ///using Unity.Content;
        ///using Unity.Loading;
        ///using UnityEngine;
        ///
        ///public class SampleBehaviour : MonoBehaviour
        ///{
        ///    public IEnumerator Start()
        ///    {
        ///        NativeArray<ContentFile> empty = new NativeArray<ContentFile>();
        ///        ContentFile depFileHandle = ContentLoadInterface.LoadContentFileAsync(ContentNamespace.Default, "path/to/depfile", empty);
        ///
        ///        NativeArray<ContentFile> depFiles = new NativeArray<ContentFile>(1, Allocator.Temp);
        ///        depFiles[0] = depFileHandle;
        ///        ContentFile rootFileHandle = ContentLoadInterface.LoadContentFileAsync(ContentNamespace.Default, "path/to/rootfile", depFiles);
        ///        depFiles.Dispose();
        ///
        ///        // yield coroutine until loading is complete
        ///        while (rootFileHandle.LoadingStatus == LoadingStatus.InProgress)
        ///            yield return null;
        ///
        ///        ulong localFileIdentifierOfObjectIWant = 25;
        ///        GameObject obj = (GameObject)rootFileHandle.GetObject(localFileIdentifierOfObjectIWant);
        ///
        ///        // When done using obj. unload both files.
        ///        ContentFileUnloadHandle unloadHandleRoot = rootFileHandle.UnloadAsync();
        ///        ContentFileUnloadHandle unloadHandleDep = depFileHandle.UnloadAsync();
        ///
        ///        // yield coroutine until loading is complete
        ///        while (!unloadHandleRoot.IsCompleted || !unloadHandleRoot.IsCompleted)
        ///            yield return null;
        ///
        ///        // file is now completly unloaded. obj has been deleted
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static ContentFile LoadContentFileAsync(ContentNamespace nameSpace, string filename, NativeArray<ContentFile> dependencies, JobHandle dependentFence = new JobHandle())
        {
            unsafe
            {
                return ContentLoadInterface.LoadContentFileAsync(nameSpace, filename, dependencies.m_Buffer, dependencies.Length, dependentFence, false);
            }
        }

        ///<summary>Returns all the <see cref="Unity.Loading.ContentFile" /> handles associated with the provided <see cref="Unity.Content.ContentNamespace" />.</summary>
        ///<remarks>The results will include all <see cref="Unity.Loading.ContentFile" />s regardless of their <see cref="Unity.Loading.ContentFile.LoadingStatus" />.</remarks>
        ///<param name="nameSpace">The ContentNamespace used to filter the results.</param>
        ///<returns>Returns an array of all the <see cref="Unity.Loading.ContentFile" />s belonging to the <see cref="Unity.Content.ContentNamespace" />.</returns>
        public static extern ContentFile[] GetContentFiles(ContentNamespace nameSpace);
        ///<summary>An array of all the <see cref="Unity.Loading.ContentSceneFile" />s associated with the <see cref="Unity.Content.ContentNamespace" />.</summary>
        ///<remarks>The results will include all <see cref="Unity.Loading.ContentFile" />s regardless of their <see cref="Unity.Loading.ContentSceneFile.Status" />.</remarks>
        ///<param name="nameSpace">The <see cref="Unity.Content.ContentNamespace" /> used to filter the results.</param>
        ///<returns>Returns an array of all the <see cref="Unity.Loading.ContentSceneFile" />s belonging to the <see cref="Unity.Content.ContentNamespace" />.</returns>
        public static extern ContentSceneFile[] GetSceneFiles(ContentNamespace nameSpace);

        ///<summary>Gets the target duration allowed per frame to integrate loading or unloading objects, in milliseconds.</summary>
        public static float GetIntegrationTimeMS()
        {
            return IntegrationTimeMS ;
        }

        ///<summary>Sets the target duration allowed per frame to integrate loading or unloading objects, in milliseconds.</summary>
        public static void SetIntegrationTimeMS(float integrationTimeMS)
        {
            if (integrationTimeMS <= 0)
                throw new ArgumentOutOfRangeException("integrationTimeMS", "integrationTimeMS was out of range. Must be greater than zero.");

            IntegrationTimeMS  = integrationTimeMS;
        }
    }
}
