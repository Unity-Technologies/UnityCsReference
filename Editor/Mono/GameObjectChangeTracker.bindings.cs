// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/Utility/GameObjectChangeTracker.h")]
    internal static class GameObjectChangeTracker
    {
        static GameObjectChangeTracker() => Init();

        [StaticAccessor("GameObjectChangeTracker", StaticAccessorType.DoubleColon)]
        extern static void Init();

        public static event GameObjectChangeTrackerEventHandler GameObjectsChanged
        {
            add => m_GameObjectsChanged.Add(value);
            remove => m_GameObjectsChanged.Remove(value);
        }

        public static unsafe void PublishEvents(NativeArray<GameObjectChangeTrackerEvent> events)
            => OnGameObjectsChanged((IntPtr)events.GetUnsafeReadOnlyPtr(), events.Length);

        private static EventWithPerformanceTracker<GameObjectChangeTrackerEventHandler> m_GameObjectsChanged = new EventWithPerformanceTracker<GameObjectChangeTrackerEventHandler>($"{nameof(GameObjectChangeTracker)}.{nameof(GameObjectsChanged)}");

        [RequiredByNativeCode(GenerateProxy = true)]
        static unsafe void OnGameObjectsChanged(IntPtr events, int eventsCount)
        {
            if (!m_GameObjectsChanged.hasSubscribers || eventsCount == 0)
                return;

            var nativeArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<GameObjectChangeTrackerEvent>(events.ToPointer(), eventsCount, Allocator.Invalid);

            var ash = AtomicSafetyHandle.Create();
            InitStaticSafetyId(ref ash);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref nativeArray, ash);
            try
            {
                foreach (var evt in m_GameObjectsChanged)
                    evt(nativeArray);
            }
            finally
            {
                var result = AtomicSafetyHandle.EnforceAllBufferJobsHaveCompletedAndRelease(ash);
                if (result == EnforceJobResult.DidSyncRunningJobs)
                {
                    UnityEngine.Debug.LogError(
                        $"You cannot use the {nameof(GameObjectChangeTracker)} instance provided by {nameof(GameObjectsChanged)} in a job unless you complete the job before your callback finishes.");
                }
            }
        }

        // TODO: Once Burst supports internal/external functions in static initializers, this can become
        //   static readonly int s_staticSafetyId = AtomicSafetyHandle.NewStaticSafetyId<GameObjectChangeTrackerEvents>();
        // and InitStaticSafetyId() can be replaced with a call to AtomicSafetyHandle.SetStaticSafetyId();
        static int s_staticSafetyId;
        [BurstDiscard]
        static void InitStaticSafetyId(ref AtomicSafetyHandle handle1)
        {
            if (s_staticSafetyId == 0)
                s_staticSafetyId = AtomicSafetyHandle.NewStaticSafetyId<GameObjectChangeTrackerEvent>();
            AtomicSafetyHandle.SetStaticSafetyId(ref handle1, s_staticSafetyId);
        }

    }

    internal delegate void GameObjectChangeTrackerEventHandler(in NativeArray<GameObjectChangeTrackerEvent> events);

    [StructLayout(layoutKind: LayoutKind.Sequential)]
    internal readonly struct GameObjectChangeTrackerEvent
    {
        public GameObjectChangeTrackerEvent(int instanceId, GameObjectChangeTrackerEventType eventType)
        {
            InstanceId = instanceId;
            EventType = eventType;
        }

        public GameObjectChangeTrackerEvent(GameObjectChangeTrackerEventType eventType)
        {
            InstanceId = 0;
            EventType = eventType;
        }

        public readonly int InstanceId;
        public readonly GameObjectChangeTrackerEventType EventType;
    }

    [Flags]
    internal enum GameObjectChangeTrackerEventType : ushort
    {
        CreatedOrChanged = 1 << 0,
        Destroyed = 1 << 1,
        OrderChanged = 1 << 2,
        ChangedParent = 1 << 3,
        ChangedScene = 1 << 4,
        SceneOrderChanged = 1 << 5
    }

}
