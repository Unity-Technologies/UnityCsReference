// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;

namespace Unity.Entities
{
    /// <summary>
    /// Interface implemented by unmanaged component systems.
    /// </summary>
    [RequireImplementors]
    public interface ISystem
    {
        /// <summary>
        /// Called when this system is created.
        /// </summary>
        /// <remarks>
        /// Implement an `OnCreate` function to set up system resources when it is created.
        ///
        /// `OnCreate` is invoked before the the first time <see cref="ISystemStartStop.OnStartRunning(ref SystemState)"/>
        /// and <see cref="OnUpdate"/> are invoked.
        /// </remarks>
        /// <param name="state">The <see cref="SystemState"/> backing this system instance</param>
        [RequiredMember]
        void OnCreate(ref SystemState state) { }

        /// <summary>
        /// Called when this system is destroyed.
        /// </summary>
        /// <remarks>
        /// Systems are destroyed when the application shuts down, the World is destroyed, or you
        /// call <see cref="World.DestroySystem"/>. In the Unity Editor, system destruction occurs when you exit
        /// Play Mode and when scripts are reloaded.
        /// </remarks>
        /// <param name="state">The <see cref="SystemState"/> backing this system instance</param>
        [RequiredMember]
        void OnDestroy(ref SystemState state) { }

        /// <summary>
        /// Implement `OnUpdate` to perform the major work of this system.
        /// </summary>
        /// <remarks>
        /// <p>
        /// By default, the system invokes `OnUpdate` once every frame on the main thread.
        /// To skip OnUpdate if all of the system's [EntityQueries] are empty, use the
        /// [RequireMatchingQueriesForUpdateAttribute]. To limit when OnUpdate is invoked, you can
        /// specify components that must exist, or queries that match specific Entities. To do
        /// this, call <see cref="SystemState.RequireForUpdate{T}"/> or
        /// <see cref="SystemState.RequireForUpdate(EntityQuery)"/>
        /// in the system's OnCreate method. For more information, see <see cref="SystemState.ShouldRunSystem"/>.
        /// </p>
        /// <p>
        /// You can instantiate and schedule an <see cref="IJobChunk"/> instance; you can use the
        /// [C# Job System] or you can perform work on the main thread. If you call <see cref="EntityManager"/> methods
        /// that perform structural changes on the main thread, be sure to arrange the system order to minimize the
        /// performance impact of the resulting [sync points].
        /// </p>
        ///
        /// [sync points]: xref:concepts-structural-changes
        /// [C# Job System]: https://docs.unity3d.com/Manual/JobSystem.html
        /// [EntityQueries]: xref:Unity.Entities.EntityQuery
        /// [RequireMatchingQueriesForUpdateAttribute]: xref:Unity.Entities.RequireMatchingQueriesForUpdateAttribute
        /// </remarks>
        /// <param name="state">The <see cref="SystemState"/> backing this system instance</param>
        [RequiredMember]
        void OnUpdate(ref SystemState state) { }
    }

    /// <summary>
    /// Delegates only used by compiler
    /// </summary>
    public static class SystemBaseDelegates
    {
        /// <summary>
        /// Used by compilation pipeline internally to support optional burst compilation of ISystem methods
        /// </summary>
        /// <param name="state">The <see cref="SystemState"/> backing a system instance</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Function(IntPtr systemPtr, IntPtr state);
    }

    /// <summary>
    /// Optional interface for start/stop notifications on systems.
    /// </summary>
    [RequireImplementors]
    public interface ISystemStartStop
    {
        /// <summary>
        /// Called before the first call to OnUpdate and when a system resumes updating after being stopped or disabled.
        /// </summary>
        /// <remarks>
        /// If the <see cref="EntityQuery"/> objects defined for a system do not match any existing entities
        /// then the system skips updates until a successful match is found. Likewise, if you set <see cref="SystemState.Enabled"/>
        /// to false, then the system stops running. In both cases, <see cref="OnStopRunning"/> is
        /// called when a running system stops updating; OnStartRunning is called when it starts updating again.
        /// </remarks>
        /// <param name="state">The <see cref="SystemState"/> backing this system instance</param>
        ///
        [RequiredMember]
        void OnStartRunning(ref SystemState state);

        /// <summary>
        /// Called when this system stops running because no entities match the system's <see cref="EntityQuery"/>
        /// objects or because you change the system <see cref="SystemState.Enabled"/> property to false.
        /// </summary>
        /// <remarks>
        /// If the <see cref="EntityQuery"/> objects defined for a system do not match any existing entities
        /// then the system skips updating until a successful match is found. Likewise, if you set <see cref="SystemState.Enabled"/>
        /// to false, then the system stops running. In both cases, OnStopRunning is
        /// called when a running system stops updating; <see cref="OnStartRunning"/> is called when it starts updating again.
        /// </remarks>
        /// <param name="state">The <see cref="SystemState"/> backing this system instance</param>
        [RequiredMember]
        void OnStopRunning(ref SystemState state);
    }

    /// <summary>
    /// Interface for methods only used by compiler
    /// </summary>
    [RequireImplementors]
    public interface ISystemCompilerGenerated
    {
        /// <summary>
        /// Generated by compilation pipeline and used internally.
        /// </summary>
        /// <param name="state">The <see cref="SystemState"/> backing this system instance</param>
        [RequiredMember]
        void OnCreateForCompiler(ref SystemState state);
    }
}
