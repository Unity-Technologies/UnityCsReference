// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities.LowLevel;

namespace Unity.Entities
{
    partial class StructuralChangesProfiler
    {
        [GenerateTestsForBurstCompatibility(RequiredUnityDefine = "ENABLE_PROFILER")]
        public struct Recorder : IDisposable
        {
            readonly struct Scope
            {
                public readonly SystemHandle ExecutingSystem;
                public readonly long StartTimestamp;
                public readonly ulong WorldSequenceNumber;
                public readonly StructuralChangeType Type;

                public Scope(long timeStamp, StructuralChangeType type, ulong worldSequenceNumber, SystemHandle executingSystem)
                {
                    StartTimestamp = timeStamp;
                    Type = type;
                    WorldSequenceNumber = worldSequenceNumber;
                    ExecutingSystem = executingSystem;
                }
            }

            UnsafeStack<Scope> m_ScopeStack;
            // The package's StructuralChangesProfilerReporting emits these as frame metadata and clears them.
            internal UnsafeList<StructuralChangeData> m_Changes;

            public Recorder(Allocator allocator)
            {
                m_ScopeStack = default;
                m_Changes = default;
                Initialize(allocator);
            }

            public void Dispose()
            {
                m_ScopeStack.Dispose();
                m_Changes.Dispose();
            }

            public void Initialize(Allocator allocator)
            {
                m_ScopeStack = new UnsafeStack<Scope>(8, allocator);
                m_Changes = new UnsafeList<StructuralChangeData>(16, allocator);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public void Begin(StructuralChangeType type, in WorldUnmanaged world)
            {
                if (!s_Initialized) // Enabled test is done at call site
                    return;

                var now = ProfilerClock.GetTimestamp();
                world.GetInfo(out var sequenceNumber, out var executingSystem);
                m_ScopeStack.Push(new Scope(now, type, sequenceNumber, executingSystem));
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public void End()
            {
                if (!s_Initialized || m_ScopeStack.IsEmpty) // Enabled test is done at call site
                    return;

                var now = ProfilerClock.GetTimestamp();
                var scope = m_ScopeStack.Top();
                var elapsed = ProfilerClock.ToNanoseconds(now - scope.StartTimestamp);

                switch (scope.Type)
                {
                    case StructuralChangeType.CreateEntity:
                        Elapsed.CreateEntity += elapsed;
                        break;
                    case StructuralChangeType.DestroyEntity:
                        Elapsed.DestroyEntity += elapsed;
                        break;
                    case StructuralChangeType.AddComponent:
                        Elapsed.AddComponent += elapsed;
                        break;
                    case StructuralChangeType.RemoveComponent:
                        Elapsed.RemoveComponent += elapsed;
                        break;
                    case StructuralChangeType.SetSharedComponent:
                        Elapsed.SetSharedComponent += elapsed;
                        break;
                    default:
                        throw new NotImplementedException($"Structural change type {scope.Type} not implemented.");
                }

                m_Changes.Add(new StructuralChangeData(elapsed, scope.Type, scope.WorldSequenceNumber, scope.ExecutingSystem));
                m_ScopeStack.Pop();
            }

            [ExcludeFromBurstCompatTesting("Calls a managed delegate")]
            public void Flush() => s_FlushChanges?.Invoke(ref m_Changes);

        }
    }
}
