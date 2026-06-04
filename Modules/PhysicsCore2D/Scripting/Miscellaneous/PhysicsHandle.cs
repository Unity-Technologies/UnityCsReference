// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections;

using static Unity.U2D.Physics.Scripting2D;

namespace Unity.U2D.Physics
{
    /// <summary>
    /// Defines a common handle interface.
    /// This is a helper implementation interface (used for commonality/consistency) and should not be used to access a handle.
    /// </summary>
    interface IPhysicsHandle<T> : IEquatable<T>
    {
        /// <summary>
        /// Get the physics handle.
        /// </summary>
        PhysicsHandle physicsHandle { get; }
    }

    /// <summary>
    /// An abstract handle that can be used for custom purposes such as handling miscellaneous physics object types abstractly.
    /// You can create a handle with <see cref="PhysicsHandle.Create"/> or <see cref="PhysicsHandle.CreateBatch(int, Allocator)"/>.
    /// You can destroy a handle with <see cref="PhysicsHandle.Destroy"/> or <see cref="PhysicsHandle.DestroyBatch(ReadOnlySpan{PhysicsHandle})"/>.
    /// 
    /// You can also get a handle from one of the following physics objects:
    /// <see cref="PhysicsBody.physicsHandle"/>
    /// <see cref="PhysicsShape.physicsHandle"/>
    /// <see cref="PhysicsChain.physicsHandle"/>
    /// <see cref="PhysicsJoint.physicsHandle"/>
    /// <see cref="PhysicsDistanceJoint.physicsHandle"/>
    /// <see cref="PhysicsFixedJoint.physicsHandle"/>
    /// <see cref="PhysicsHingeJoint.physicsHandle"/>
    /// <see cref="PhysicsIgnoreJoint.physicsHandle"/>
    /// <see cref="PhysicsRelativeJoint.physicsHandle"/>
    /// <see cref="PhysicsSliderJoint.physicsHandle"/>
    /// <see cref="PhysicsWheelJoint.physicsHandle"/>
    ///
    /// NOTE: When retrieving the handle from another physics object, the object type is not encoded so that must be handled separately.
    /// Because of this, it's entirely possible for two handles to be equal, differing only by the type they came from so care must be taken or the object type explicitly stored against handles.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct PhysicsHandle : IEquatable<PhysicsHandle>
    {
        #region Id

        readonly Int32 m_Index1;
        readonly UInt16 m_World0;
        readonly UInt16 m_Generation;

        /// <undoc/>
        public override readonly string ToString() => $"index={m_Index1}, world={m_World0}, generation={m_Generation}";

        #endregion

        #region Equality

        /// <undoc/>
        public override bool Equals(object obj) { return obj is PhysicsHandle other && Equals(other); }

        /// <undoc/>
        public bool Equals(PhysicsHandle other) { return m_Index1 == other.m_Index1 && m_World0 == other.m_World0 && m_Generation == other.m_Generation; }

        /// <undoc/>
        public static bool operator ==(PhysicsHandle lhs, PhysicsHandle rhs) => lhs.Equals(rhs);

        /// <undoc/>
        public static bool operator !=(PhysicsHandle lhs, PhysicsHandle rhs) => !(lhs == rhs);

        /// <undoc/>
        public override int GetHashCode() { return HashCode.Combine(m_Index1, m_World0, m_Generation); }

        #endregion

        /// <summary>
        /// Create a <see cref="PhysicsHandle"/>.
        /// </summary>
        /// <returns>The created physics handle.</returns>
        public static PhysicsHandle Create() => PhysicsHandle_Create();

        /// <summary>
        /// Create a batch of <see cref="PhysicsHandle"/>.
        /// </summary>
        /// <param name="handleCount">The quantity of physics handles to create, in the range 1 to 100000.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The physics handles. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public static NativeArray<PhysicsHandle> CreateBatch(int handleCount, Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsHandle_CreateBatch(handleCount, allocator).ToNativeArray<PhysicsHandle>();

        /// <summary>
        /// Destroy the handle.
        /// This will only work correctly if the <see cref="PhysicsHandle"/> was explicitly created with <see cref="PhysicsHandle.Create"/> or <see cref="PhysicsHandle.CreateBatch(int, Allocator)"/>.
        /// 
        /// NOTE: If the handle comes from another physics object, it will not destroy that object and a warning will be issued.
        /// </summary>
        public unsafe readonly void Destroy() { var handle = this; DestroyBatch(new ReadOnlySpan<PhysicsHandle>(&handle, 1)); }

        /// <summary>
        /// Destroy the specified span of <see cref="PhysicsHandle"/>.
        /// 
        /// NOTE: If any of the handles come from another physics object, it will not destroy that object and a warning will be issued.
        /// </summary>
        /// <param name="physicsHandles">The physics handles to destroy.</param>
        public static void DestroyBatch(ReadOnlySpan<PhysicsHandle> physicsHandles) => PhysicsHandle_DestroyBatch(physicsHandles);

        /// <summary>
        /// Checks if the physics handle is valid in the physics handle pool.
        /// This will only work correctly if the <see cref="PhysicsHandle"/> was explicitly created with <see cref="PhysicsHandle.Create"/> or <see cref="PhysicsHandle.CreateBatch(int, Allocator)"/>.
        /// If the handle comes from another physics object, it will not validate that object and a warning will be issued.
        /// </summary>
        public readonly bool isValid => PhysicsHandle_IsValid(this);

        /// <summary>
        /// Checks if the physics handle is from the physics handle pool or not.
        /// This will return false unless  the<see cref="PhysicsHandle"/> was explicitly created with <see cref="PhysicsHandle.Create"/> or <see cref="PhysicsHandle.CreateBatch(int, Allocator)"/>.
        /// </summary>
        public readonly bool isPoolHandle => m_World0 == UInt16.MaxValue;

        /// <summary>
        /// Get the handle index.
        /// </summary>
        public readonly Int32 index => m_Index1;

        /// <summary>
        /// Get the handle generation.
        /// </summary>
        public readonly UInt16 generation => m_Generation;
    }
}
