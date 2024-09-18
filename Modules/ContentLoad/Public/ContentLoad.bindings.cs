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

    public enum LoadingStatus
    {
        InProgress,
        Completed,
        Failed
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ContentFileUnloadHandle
    {
        internal ContentFile Id;
        public bool IsCompleted { get { return ContentLoadInterface.ContentFile_IsUnloadComplete(Id); } }
        public bool WaitForCompletion(int timeoutMs) { return ContentLoadInterface.WaitForUnloadCompletion(Id, timeoutMs); }
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe public struct ContentFile
    {
        internal UInt64 Id;


        public ContentFileUnloadHandle UnloadAsync()
        {
            ThrowIfInvalidHandle();
            ContentLoadInterface.ContentFile_UnloadAsync(this);
            return new ContentFileUnloadHandle { Id = this };
        }

        public UnityEngine.Object[] GetObjects()
        {
            ThrowIfNotComplete();
            return ContentLoadInterface.ContentFile_GetObjects(this);
        }

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

        public bool IsValid { get { return ContentLoadInterface.ContentFile_IsHandleValid(this); } }

        public LoadingStatus LoadingStatus {
            get {
                ThrowIfInvalidHandle();
                return ContentLoadInterface.ContentFile_GetLoadingStatus(this);
            }
        }

        public bool WaitForCompletion(int timeoutMs)
        {
            ThrowIfInvalidHandle();
            return ContentLoadInterface.WaitForLoadCompletion(this, timeoutMs);
        }

        public static ContentFile GlobalTableDependency
        {
            get { return new ContentFile { Id = (int)ContentFileReservedID.ResolveReferencesWithPM }; }
        }
    }

    public enum SceneLoadingStatus
    {
        InProgress,
        WaitingForIntegrate,
        WillIntegrateNextFrame,
        Complete,
        Failed
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ContentSceneParameters
    {
        [NativeName("LoadSceneMode")]
        internal LoadSceneMode m_LoadSceneMode;
        [NativeName("LocalPhysicsMode")]
        internal LocalPhysicsMode m_LocalPhysicsMode;
        [NativeName("AutoIntegrate")]
        internal bool m_AutoIntegrate;

        public LoadSceneMode loadSceneMode { get { return m_LoadSceneMode; } set { m_LoadSceneMode = value; } }
        public LocalPhysicsMode localPhysicsMode { get { return m_LocalPhysicsMode; } set { m_LocalPhysicsMode = value; } }
        public bool autoIntegrate { get { return m_AutoIntegrate; } set { m_AutoIntegrate = value; } }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ContentSceneFile
    {
        internal UInt64 Id;
        public Scene Scene { get { ThrowIfInvalidHandle(); return ContentLoadInterface.ContentSceneFile_GetScene(this); } }

        public void IntegrateAtEndOfFrame() { ThrowIfInvalidHandle(); ContentLoadInterface.ContentSceneFile_IntegrateAtEndOfFrame(this); }
        public SceneLoadingStatus Status { get { ThrowIfInvalidHandle(); return ContentLoadInterface.ContentSceneFile_GetStatus(this); } }

        public bool UnloadAtEndOfFrame() { ThrowIfInvalidHandle(); return ContentLoadInterface.ContentSceneFile_UnloadAtEndOfFrame(this); }
        public bool WaitForLoadCompletion(int timeoutMs) { ThrowIfInvalidHandle(); return ContentLoadInterface.ContentSceneFile_WaitForCompletion(this, timeoutMs); }

        public bool IsValid { get { return ContentLoadInterface.ContentSceneFile_IsHandleValid(this); } }

        private void ThrowIfInvalidHandle()
        {
            if (!IsValid)
                throw new Exception("The ContentSceneFile operation cannot be performed because the handle is invalid. Did you already unload it?");
        }
    }

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

        internal extern static float IntegrationTimeMS { get; set; }

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

        public static ContentSceneFile LoadSceneAsync(ContentNamespace nameSpace, string filename, string sceneName, ContentSceneParameters sceneParams, NativeArray<ContentFile> dependencies, JobHandle dependentFence = new JobHandle())
        {
            unsafe
            {
                return LoadSceneAsync(nameSpace, filename, sceneName, sceneParams, (ContentFile*)dependencies.m_Buffer, dependencies.Length, dependentFence);
            }
        }


        public static ContentFile LoadContentFileAsync(ContentNamespace nameSpace, string filename, NativeArray<ContentFile> dependencies, JobHandle dependentFence = new JobHandle())
        {
            unsafe
            {
                return ContentLoadInterface.LoadContentFileAsync(nameSpace, filename, dependencies.m_Buffer, dependencies.Length, dependentFence, false);
            }
        }

        public static extern ContentFile[] GetContentFiles(ContentNamespace nameSpace);
        public static extern ContentSceneFile[] GetSceneFiles(ContentNamespace nameSpace);

        public static float GetIntegrationTimeMS()
        {
            return IntegrationTimeMS ;
        }

        public static void SetIntegrationTimeMS(float integrationTimeMS)
        {
            if (integrationTimeMS <= 0)
                throw new ArgumentOutOfRangeException("integrationTimeMS", "integrationTimeMS was out of range. Must be greater than zero.");

            IntegrationTimeMS  = integrationTimeMS;
        }
    }
}
