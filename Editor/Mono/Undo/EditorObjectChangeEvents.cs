// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.SceneManagement;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor
{
    public enum ObjectChangeKind : ushort
    {
        None = 0,
        // The contents of the scene have changed in a way where no information on what specifically has changed can be provided
        ChangeScene = 1,

        CreateGameObjectHierarchy = 2,

        // The contents of anything in a sub-hierarchy has changed
        // (The game object and children might have been deleted, a new game object addded, parenting might have changed)
        // Any prefab merge results in StructuralHierarchy change on all instances
        ChangeGameObjectStructureHierarchy = 3,

        // The contents of a game object has changed. Components were removed, added or changed.
        ChangeGameObjectStructure = 4,

        // The parent of the game object has changed
        ChangeGameObjectParent = 5,

        // A property of a loaded component or game object has changed
        ChangeGameObjectOrComponentProperties = 6,

        // The entire hierarchy will be destroyed after the callback is invoked
        DestroyGameObjectHierarchy = 7,

        // The Object (Any asset type) has been created
        CreateAssetObject = 8,
        // The Object (Any asset type) will be destroyed after the callback is invoked
        DestroyAssetObject = 9,
        // A property of a loaded asset object has changed
        ChangeAssetObjectProperties = 10,
        // An instance of a prefab was updated
        UpdatePrefabInstances = 11,
    }

    public static class ObjectChangeEvents
    {
        public delegate void ObjectChangeEventsHandler(ref ObjectChangeEventStream stream);

        public static event ObjectChangeEventsHandler changesPublished;

        // TODO: Once Burst supports internal/external functions in static initializers, this can become
        //   static readonly int s_staticSafetyId = AtomicSafetyHandle.NewStaticSafetyId<ObjectChangeEventStream>();
        // and InitStaticSafetyId() can be replaced with a call to AtomicSafetyHandle.SetStaticSafetyId();
        static int s_staticSafetyId;
        [BurstDiscard]
        static void InitStaticSafetyId(ref AtomicSafetyHandle handle1, ref AtomicSafetyHandle handle2)
        {
            if (s_staticSafetyId == 0)
                s_staticSafetyId = AtomicSafetyHandle.NewStaticSafetyId<ObjectChangeEventStream>();
            AtomicSafetyHandle.SetStaticSafetyId(ref handle1, s_staticSafetyId);
            AtomicSafetyHandle.SetStaticSafetyId(ref handle2, s_staticSafetyId);
        }


        [RequiredByNativeCode(GenerateProxy = true)]
        static void InvokeChangeEvent(IntPtr events, int eventsCount, IntPtr payLoad, int payLoadLength)
        {
            if (changesPublished == null || eventsCount == 0)
                return;

            var stream = new ObjectChangeEventStream(events, eventsCount, payLoad, payLoadLength);

            var ash1 = AtomicSafetyHandle.Create();
            var ash2 = AtomicSafetyHandle.Create();
            InitStaticSafetyId(ref ash1, ref ash2);
            stream.OverrideSafetyHandle(ash1, ash2);

            changesPublished.Invoke(ref stream);

            var result1 = AtomicSafetyHandle.EnforceAllBufferJobsHaveCompletedAndRelease(ash1);
            var result2 = AtomicSafetyHandle.EnforceAllBufferJobsHaveCompletedAndRelease(ash2);
            if (result1 == EnforceJobResult.DidSyncRunningJobs || result2 == EnforceJobResult.DidSyncRunningJobs)
            {
                UnityEngine.Debug.LogError(
                    $"You cannot use the {nameof(ObjectChangeEventStream)} instance provided by {nameof(changesPublished)} in a job unless you complete the job before your callback finishes.");
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ChangeGameObjectParentEventArgs
    {
        public int instanceId => m_InstanceId;
        public int previousParentInstanceId => m_PreviousParentInstanceId;
        public int newParentInstanceId => m_NewParentInstanceId;
        public Scene previousScene => m_PreviousScene;
        public Scene newScene => m_NewScene;

        public ChangeGameObjectParentEventArgs(int instanceId, Scene previousScene, int previousParentInstanceId, Scene newScene, int newParentInstanceId)
        {
            m_InstanceId = instanceId;
            m_PreviousParentInstanceId = previousParentInstanceId;
            m_NewParentInstanceId = newParentInstanceId;
            m_PreviousScene = previousScene;
            m_NewScene = newScene;
        }

        private int m_InstanceId;
        private int m_PreviousParentInstanceId;
        private int m_NewParentInstanceId;
        private Scene m_PreviousScene;
        private Scene m_NewScene;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ChangeSceneEventArgs
    {
        public Scene scene => m_Scene;

        public ChangeSceneEventArgs(Scene scene)
        {
            m_Scene = scene;
        }

        private Scene m_Scene;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CreateGameObjectHierarchyEventArgs
    {
        public int instanceId => m_InstanceId;
        public Scene scene => m_Scene;

        public CreateGameObjectHierarchyEventArgs(int instanceId, Scene scene)
        {
            m_InstanceId = instanceId;
            m_Scene = scene;
        }

        private int m_InstanceId;
        private Scene m_Scene;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ChangeGameObjectStructureHierarchyEventArgs
    {
        public int instanceId => m_InstanceId;
        public Scene scene => m_Scene;

        public ChangeGameObjectStructureHierarchyEventArgs(int instanceId, Scene scene)
        {
            m_InstanceId = instanceId;
            m_Scene = scene;
        }

        private int m_InstanceId;
        private Scene m_Scene;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ChangeGameObjectStructureEventArgs
    {
        public int instanceId => m_InstanceId;
        public Scene scene => m_Scene;

        public ChangeGameObjectStructureEventArgs(int instanceId, Scene scene)
        {
            m_InstanceId = instanceId;
            m_Scene = scene;
        }

        private int m_InstanceId;
        private Scene m_Scene;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ChangeGameObjectOrComponentPropertiesEventArgs
    {
        public int instanceId => m_InstanceId;
        public Scene scene => m_Scene;

        public ChangeGameObjectOrComponentPropertiesEventArgs(int instanceId, Scene scene)
        {
            m_InstanceId = instanceId;
            m_Scene = scene;
        }

        private int m_InstanceId;
        private Scene m_Scene;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DestroyGameObjectHierarchyEventArgs
    {
        public int instanceId => m_InstanceId;
        public Scene scene => m_Scene;

        public DestroyGameObjectHierarchyEventArgs(int instanceId, Scene scene)
        {
            m_InstanceId = instanceId;
            m_Scene = scene;
        }

        private int m_InstanceId;
        private Scene m_Scene;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CreateAssetObjectEventArgs
    {
        public GUID guid => m_Guid;
        public int instanceId => m_InstanceId;
        public Scene scene => m_Scene;

        public CreateAssetObjectEventArgs(GUID guid, int instanceId, Scene scene)
        {
            m_Guid = guid;
            m_InstanceId = instanceId;
            m_Scene = scene;
        }

        private GUID m_Guid;
        private int m_InstanceId;
        private Scene m_Scene;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DestroyAssetObjectEventArgs
    {
        public GUID guid => m_Guid;
        public int instanceId => m_InstanceId;
        public Scene scene => m_Scene;

        public DestroyAssetObjectEventArgs(GUID guid, int instanceId, Scene scene)
        {
            m_Guid = guid;
            m_InstanceId = instanceId;
            m_Scene = scene;
        }

        private GUID m_Guid;
        private int m_InstanceId;
        private Scene m_Scene;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ChangeAssetObjectPropertiesEventArgs
    {
        public GUID guid => m_Guid;
        public int instanceId => m_InstanceId;
        public Scene scene => m_Scene;

        public ChangeAssetObjectPropertiesEventArgs(GUID guid, int instanceId, Scene scene)
        {
            m_Guid = guid;
            m_InstanceId = instanceId;
            m_Scene = scene;
        }

        private GUID m_Guid;
        private int m_InstanceId;
        private Scene m_Scene;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UpdatePrefabInstancesEventArgs
    {
        public Scene scene => m_Scene;
        public NativeArray<int>.ReadOnly instanceIds => m_InstanceIds;

        public UpdatePrefabInstancesEventArgs(Scene scene, NativeArray<int>.ReadOnly instanceIds)
        {
            m_Scene = scene;
            m_InstanceIds = instanceIds;
        }

        private Scene m_Scene;
        private NativeArray<int>.ReadOnly m_InstanceIds;
    }

    public struct ObjectChangeEventStream : IDisposable
    {
        [ReadOnly]
        private NativeArray<byte> m_Payload;
        [ReadOnly]
        private NativeArray<Event> m_Events;

#pragma warning disable 0649
        [StructLayout(LayoutKind.Sequential)]
        struct Event
        {
            // size in bytes and offset into payload array
            public int offset, size;
            public ObjectChangeKind ChangeKind;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct UpdatePrefabInstancesHeader
        {
            public Scene scene;
        }
#pragma warning restore 0649

        internal unsafe ObjectChangeEventStream(IntPtr events, int eventsCount, IntPtr payload, int payloadLength)
        {
            m_Payload = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(payload.ToPointer(), payloadLength, Allocator.Invalid);
            m_Events = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Event>(events.ToPointer(), eventsCount, Allocator.Invalid);
        }

        internal void OverrideSafetyHandle(AtomicSafetyHandle handle1, AtomicSafetyHandle handle2)
        {
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref m_Payload, handle1);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref m_Events, handle2);
        }


        ObjectChangeEventStream(NativeArray<Event> events, NativeArray<byte> payload)
        {
            m_Payload = payload;
            m_Events = events;
        }

        public int length => m_Events.Length;
        public bool isCreated => m_Events.IsCreated;

        public ObjectChangeKind GetEventType(int eventIdx) => m_Events[eventIdx].ChangeKind;

        public void GetChangeSceneEvent(int eventIdx, out ChangeSceneEventArgs data) =>
            ExtractEvent(eventIdx, ObjectChangeKind.ChangeScene, out data);

        public void GetCreateGameObjectHierarchyEvent(int eventIdx, out CreateGameObjectHierarchyEventArgs data) =>
            ExtractEvent(eventIdx, ObjectChangeKind.CreateGameObjectHierarchy, out data);

        public void GetDestroyGameObjectHierarchyEvent(int eventIdx, out DestroyGameObjectHierarchyEventArgs data) =>
            ExtractEvent(eventIdx, ObjectChangeKind.DestroyGameObjectHierarchy, out data);

        public void GetChangeGameObjectStructureHierarchyEvent(int eventIdx, out ChangeGameObjectStructureHierarchyEventArgs data) =>
            ExtractEvent(eventIdx, ObjectChangeKind.ChangeGameObjectStructureHierarchy, out data);

        public void GetChangeGameObjectStructureEvent(int eventIdx, out ChangeGameObjectStructureEventArgs data) =>
            ExtractEvent(eventIdx, ObjectChangeKind.ChangeGameObjectStructure, out data);

        public void GetChangeGameObjectParentEvent(int eventIdx, out ChangeGameObjectParentEventArgs data) =>
            ExtractEvent(eventIdx, ObjectChangeKind.ChangeGameObjectParent, out data);

        public void GetChangeGameObjectOrComponentPropertiesEvent(int eventIdx, out ChangeGameObjectOrComponentPropertiesEventArgs data) =>
            ExtractEvent(eventIdx, ObjectChangeKind.ChangeGameObjectOrComponentProperties, out data);

        public void GetCreateAssetObjectEvent(int eventIdx, out CreateAssetObjectEventArgs data) =>
            ExtractEvent(eventIdx, ObjectChangeKind.CreateAssetObject, out data);

        public void GetDestroyAssetObjectEvent(int eventIdx, out DestroyAssetObjectEventArgs data) =>
            ExtractEvent(eventIdx, ObjectChangeKind.DestroyAssetObject, out data);

        public void GetChangeAssetObjectPropertiesEvent(int eventIdx, out ChangeAssetObjectPropertiesEventArgs data) =>
            ExtractEvent(eventIdx, ObjectChangeKind.ChangeAssetObjectProperties, out data);

        public void GetUpdatePrefabInstancesEvent(int eventIdx, out UpdatePrefabInstancesEventArgs data)
        {
            data = default;
            var evt = ExtractEvent(eventIdx, ObjectChangeKind.UpdatePrefabInstances, out UpdatePrefabInstancesHeader header);
            unsafe
            {
                int s = sizeof(UpdatePrefabInstancesHeader);
                void* offset = (byte*)m_Payload.m_Buffer + evt.offset + s;
                int elements = (evt.size - s) / sizeof(int);
                var arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(offset, elements, Allocator.Invalid);
                var ash = NativeArrayUnsafeUtility.GetAtomicSafetyHandle(m_Payload);
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arr, ash);
                data = new UpdatePrefabInstancesEventArgs(header.scene, arr.AsReadOnly());
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckEventType(ObjectChangeKind actual, ObjectChangeKind expected)
        {
            if (actual != expected)
                throw new InvalidOperationException($"Asked for event kind {expected}, but found {actual} instead.");
        }

        unsafe Event ExtractEvent<T>(int eventIdx, ObjectChangeKind kind, out T data) where T : struct
        {
            var evt = m_Events[eventIdx];
            CheckEventType(evt.ChangeKind, kind);
            var ptr = (byte*)m_Payload.GetUnsafePtr() + evt.offset;
            UnsafeUtility.CopyPtrToStructure(ptr, out data);
            return evt;
        }

        public ObjectChangeEventStream Clone(Allocator allocator) =>
            new ObjectChangeEventStream(new NativeArray<Event>(m_Events, allocator), new NativeArray<byte>(m_Payload, allocator));

        public void Dispose()
        {
            m_Events.Dispose();
            m_Payload.Dispose();
        }

        public struct Builder : IDisposable
        {
            private NativeArray<Event> m_Events;
            private NativeArray<byte> m_Payload;
            private int m_EventCount;
            private int m_PayloadSize;

            public int eventCount => m_EventCount;

            public Builder(Allocator allocator)
            {
                m_Events = new NativeArray<Event>(16, allocator, NativeArrayOptions.UninitializedMemory);
                m_EventCount = 0;
                m_Payload = new NativeArray<byte>(256, allocator, NativeArrayOptions.UninitializedMemory);
                m_PayloadSize = 0;
            }

            public ObjectChangeEventStream ToStream(Allocator allocator)
            {
                var events = new NativeArray<Event>(m_EventCount, allocator, NativeArrayOptions.UninitializedMemory);
                var payload = new NativeArray<byte>(m_PayloadSize, allocator, NativeArrayOptions.UninitializedMemory);
                NativeArray<Event>.Copy(m_Events, 0, events, 0, m_EventCount);
                NativeArray<byte>.Copy(m_Payload, 0, payload, 0, m_PayloadSize);
                return new ObjectChangeEventStream(events, payload);
            }

            public void Dispose()
            {
                m_Events.Dispose();
                m_Payload.Dispose();
            }

            public void PushChangeSceneEvent(ref ChangeSceneEventArgs data) =>
                PushEvent(ObjectChangeKind.ChangeScene, ref data);

            public void PushCreateGameObjectHierarchyEvent(ref CreateGameObjectHierarchyEventArgs data) =>
                PushEvent(ObjectChangeKind.CreateGameObjectHierarchy, ref data);

            public void PushDestroyGameObjectHierarchyEvent(ref DestroyGameObjectHierarchyEventArgs data) =>
                PushEvent(ObjectChangeKind.DestroyGameObjectHierarchy, ref data);

            public void PushChangeGameObjectStructureHierarchyEvent(ref ChangeGameObjectStructureHierarchyEventArgs data) =>
                PushEvent(ObjectChangeKind.ChangeGameObjectStructureHierarchy, ref data);

            public void PushChangeGameObjectStructureEvent(ref ChangeGameObjectStructureEventArgs data) =>
                PushEvent(ObjectChangeKind.ChangeGameObjectStructure, ref data);

            public void PushChangeGameObjectParentEvent(ref ChangeGameObjectParentEventArgs data) =>
                PushEvent(ObjectChangeKind.ChangeGameObjectParent, ref data);

            public void PushChangeGameObjectOrComponentPropertiesEvent(ref ChangeGameObjectOrComponentPropertiesEventArgs data) =>
                PushEvent(ObjectChangeKind.ChangeGameObjectOrComponentProperties, ref data);

            public void PushCreateAssetObjectEvent(ref CreateAssetObjectEventArgs data) =>
                PushEvent(ObjectChangeKind.CreateAssetObject, ref data);

            public void PushDestroyAssetObjectEvent(ref DestroyAssetObjectEventArgs data) =>
                PushEvent(ObjectChangeKind.DestroyAssetObject, ref data);

            public void PushChangeAssetObjectPropertiesEvent(ref ChangeAssetObjectPropertiesEventArgs data) =>
                PushEvent(ObjectChangeKind.ChangeAssetObjectProperties, ref data);

            public void PushUpdatePrefabInstancesEvent(ref UpdatePrefabInstancesEventArgs data)
            {
                Scene scene = data.scene;
                PushEvent(ObjectChangeKind.UpdatePrefabInstances, ref scene);
                AtomicSafetyHandle.CheckReadAndThrow(data.instanceIds.m_Safety);
                unsafe
                {
                    AppendToLastEvent(data.instanceIds.m_Buffer, data.instanceIds.m_Length * sizeof(int));
                }
            }

            unsafe void PushEvent<T>(ObjectChangeKind kind, ref T data) where T : unmanaged
            {
                PushEvent(kind, UnsafeUtility.AddressOf(ref data), sizeof(T));
            }

            unsafe void PushEvent(ObjectChangeKind kind, void* data , int size)
            {
                ReallocMaybe(ref m_Events, m_EventCount, 1);
                m_Events[m_EventCount] = new Event
                {
                    ChangeKind = kind,
                    offset = m_PayloadSize,
                    size = size
                };
                m_EventCount += 1;
                AppendPayLoad(data, size);
            }

            unsafe void AppendToLastEvent(void* data, int size)
            {
                AppendPayLoad(data, size);
                var evt = m_Events[m_EventCount - 1];
                evt.size += size;
                m_Events[m_EventCount - 1] = evt;
            }

            unsafe void AppendPayLoad(void* data, int size)
            {
                ReallocMaybe(ref m_Payload, m_PayloadSize, size);
                var ptr = (byte*)m_Payload.GetUnsafePtr() + m_PayloadSize;
                UnsafeUtility.MemCpy(ptr, data, size);
                m_PayloadSize += size;
            }

            static void ReallocMaybe<T>(ref NativeArray<T> arr, int size, int additional) where T : unmanaged
            {
                if (size + additional < arr.Length)
                    return;
                int capacity = size + additional;
                if (capacity < 2 * arr.Length)
                    capacity = 2 * arr.Length;
                var newArr = new NativeArray<T>(capacity, arr.m_AllocatorLabel, NativeArrayOptions.UninitializedMemory);
                NativeArray<T>.Copy(arr, 0, newArr, 0, size);
                arr.Dispose();
                arr = newArr;
            }
        }
    }
}
