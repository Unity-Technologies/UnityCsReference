// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Unity.Entities
{
    /// <summary>
    /// A system that resets the world update allocator by rewinding its memories.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ThinClientSimulation | WorldSystemFilterFlags.Editor)]
    public partial struct WorldUpdateAllocatorResetSystem : ISystem
    {
        /// <summary>
        /// Executes world update allocator reset system to rewind memories of the world update allocator.
        /// </summary>
        /// <param name="state">The SystemState of the system.</param>
        public void OnUpdate(ref SystemState state)
        {
            state.m_WorldUnmanaged.ResetUpdateAllocator();
        }
    }
}
