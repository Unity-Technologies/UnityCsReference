// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Scripting.APIUpdating;
using static Unity.U2D.Physics.Scripting2D;

namespace Unity.U2D.Physics
{
    /// <summary>
    /// A dedicated shape that produces a chain of shapes connected together to produce a continuous surface.
    /// Chain shapes provide a smooth, continuous surface that will not produce "ghost" collisions.
    /// A <see cref="PhysicsChain"/> is automatically destroyed when the body it is in is destroyed. A <see cref="PhysicsChain"/>  cannot exist unattached from a body.
    /// 
    /// This will produce shapes of type <see cref="PhysicsShape.ShapeType.ChainSegment"/>.
    ///
    ///- Chains are one-sided.
    ///- A chain has no mass therefore should ideally be used on static bodies.
    ///- A chain can have a counter-clockwise winding order (normal points right of segment direction).
    ///- A chain is either a loop or open.
    ///- A chain must have at least 4 points.
    ///- The distance between any two points must be greater than <see cref="PhysicsWorld.linearSlop"/>.
    ///- A chain should not self intersect (this is not validated).
    ///- An open chain has no collision on the first and final edge.
    ///- You may overlap two open chains on their first three and/or last three points to get smooth collision.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [MovedFrom(autoUpdateAPI: ScriptUpdateConstants.AutoUpdateAPI, sourceNamespace: ScriptUpdateConstants.SourceNamespace, sourceAssembly: ScriptUpdateConstants.SourceAssembly)]
    public readonly partial struct PhysicsChain : IEquatable<PhysicsChain>
    {
        #region Id

        readonly Int32 m_Index1;
        readonly UInt16 m_World0;
        readonly UInt16 m_Generation;

        /// <undoc/>
        public override readonly string ToString() => isValid ? $"index={m_Index1}, world={m_World0}, generation={m_Generation}" : "<INVALID>";

        #endregion

        #region Equality

        /// <undoc/>
        public override bool Equals(object obj) { return base.Equals(obj); }

        /// <undoc/>
        public bool Equals(PhysicsChain other) { return m_Index1 == other.m_Index1 && m_World0 == other.m_World0 && m_Generation == other.m_Generation; }

        /// <undoc/>
        public static bool operator ==(PhysicsChain lhs, PhysicsChain rhs) => lhs.Equals(rhs);

        /// <undoc/>
        public static bool operator !=(PhysicsChain lhs, PhysicsChain rhs) => !(lhs == rhs);

        /// <undoc/>
        public override int GetHashCode() { return HashCode.Combine(m_Index1, m_World0, m_Generation); }

        #endregion

        /// <summary>
        /// Create a Chain of multiple shapes attached to the specified body which itself is within a world.
        /// </summary>
        /// <param name="body">The body to attach the shape(s) to.</param>
        /// <param name="geometry">The shape geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <returns>The created shape.</returns>
        public static PhysicsChain Create(PhysicsBody body, ChainGeometry geometry, PhysicsChainDefinition definition) => Create(body, geometry.vertices, definition);

        /// <summary>
        /// Create a Chain of multiple shapes attached to the specified body which itself is within a world.
        /// </summary>
        /// <param name="body">The body to attach the shape(s) to.</param>
        /// <param name="vertices">The vertices that will create the ChainSegment shapes.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <returns>The created chain.</returns>
        public static PhysicsChain Create(PhysicsBody body, ReadOnlySpan<Vector2> vertices, PhysicsChainDefinition definition) => PhysicsChain_Create(body, vertices, definition);

        /// <summary>
        /// Destroy the <see cref="PhysicsChain"/> and all the <see cref="PhysicsShape.ShapeType.ChainSegment"/> it owns.
        /// If the object is owned with <see cref="PhysicsChain.SetOwner(UnityEngine.Object)"/> then you must provide the owner key it returned. Failing to do so will return a warning and the chain will not be destroyed.
        /// The lifetime of the specified owner object is not linked to this chain i.e. this chain will still be owned by the owner object, even if it is destroyed.
        /// This is the only way to destroy shapes of type <see cref="PhysicsShape.ShapeType.ChainSegment"/> if they were created by a <see cref="PhysicsChain"/>.
        /// </summary>
        /// <param name="ownerKey">Optional owner key returned when using <see cref="PhysicsChain.SetOwner(UnityEngine.Object)"/>.</param>
        /// <returns>If the chain was destroyed or not.</returns>
        public readonly bool Destroy(int ownerKey = 0) => PhysicsChain_Destroy(this, ownerKey);

        /// <summary>
        /// Update the existing ChainSegment shapes with the provided vertices.
        /// Modifying the vertices will cause contacts to be recalculated however it may cause overlaps and/or collision tunnelling if not used carefully.
        /// The number of vertices provided and looping option should be the same as was used when the Chain was originally created.
        /// Any mismatch between the two will result in a warning.
        /// </summary>
        /// <param name="vertices">The vertices used to update the existing ChainSegment shapes.</param>
        /// <param name="isLoop">Indicates a closed chain formed by connecting the first and last vertices specified. This should match what was originally specified in <see cref="PhysicsChainDefinition.isLoop"/>.</param>
        public readonly void UpdateVertices(ReadOnlySpan<Vector2> vertices, bool isLoop) => PhysicsChain_UpdateVertices(this, vertices, isLoop);

        /// <summary>
        /// Check if the shape is valid.
        /// </summary>
        public readonly bool isValid => PhysicsChain_IsValid(this);

        /// <summary>
        /// Get the world the chain is attached to.
        /// </summary>
        public readonly PhysicsWorld world => PhysicsChain_GetWorld(this);

        /// <summary>
        /// The body which the chain is attached to.
        /// </summary>
        public readonly PhysicsBody body => PhysicsChain_GetBody(this);

        /// <summary>
        /// Get the world AABB that bounds this chain.
        /// The bounds of the shape is inflated slightly due to speculative collision detection.
        /// The inflation is smaller on Static shape types however it is not zero due to time-of-impact collision detection.
        /// If an exact AABB is required then you can retrieve that via the shape geometry.
        /// </summary>
        public readonly PhysicsAABB aabb => PhysicsChain_CalculateAABB(this);

        /// <summary>
        /// Calculate the closest point on this chain to the specified point.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <param name="chainSegmentShape">A reference to the chain segment shape that the query found.</param>
        /// <returns>The closest point on the shape to the specified point.</returns>
        public readonly Vector2 ClosestPoint(Vector2 point, out PhysicsShape chainSegmentShape) => PhysicsChain_ClosestPoint(this, point, out chainSegmentShape);

        /// <summary>
        /// Check if a ray intersects the chain.
        /// See <see cref="PhysicsQuery.CastResult"/>.
        /// </summary>
        /// <param name="castRayInput">The configuration of the ray to cast.</param>
        /// <param name="chainSegmentShape">A reference to the chain segment shape that the query found.</param>
        /// <returns>The intersection details, if any, that were found.</returns>
        public readonly PhysicsQuery.CastResult CastRay(PhysicsQuery.CastRayInput castRayInput, out PhysicsShape chainSegmentShape) => PhysicsChain_CastRay(this, castRayInput, out chainSegmentShape);

        /// <summary>
        /// Calculate if a cast shape intersects the chain.
        /// See <see cref="PhysicsQuery.CastShapeInput"/> and <see cref="PhysicsQuery.CastResult"/>.
        /// </summary>
        /// <param name="input">The cast shape input used to check for intersection.</param>
        /// <param name="chainSegmentShape">A reference to the chain segment shape that the query found.</param>
        /// <returns>The results of the intersection test.</returns>
        public readonly PhysicsQuery.CastResult CastShape(PhysicsQuery.CastShapeInput input, out PhysicsShape chainSegmentShape) => PhysicsChain_CastShape(this, input, out chainSegmentShape);

        /// <summary>
        /// Controls whether this chain is automatically drawn when the world is drawn.
        /// </summary>
        public readonly bool worldDrawing { get => PhysicsChain_GetWorldDrawing(this); set => PhysicsChain_SetWorldDrawing(this, value); }

        /// <summary>
        /// The friction of the owned chain shapes.
        /// Usually this is within the range [0, 1]. Values higher than 1 will result in energy being added which can lead to an unstable simulation.
        /// </summary>
        public readonly float friction { get => PhysicsChain_GetFriction(this); set => PhysicsChain_SetFriction(this, value); }

        /// <summary>
        /// The bounciness of the chain.
        /// Usually this is within the range [0, 1]. Values higher than 1 will result in energy being added which can lead to an unstable simulation.
        /// </summary>
        public readonly float bounciness { get => PhysicsChain_GetBounciness(this); set => PhysicsChain_SetBounciness(this, value); }

        /// <summary>
        /// Defines the method used when mixing the friction values of two shapes to form a shape contact.
        /// This is assigned to the current <see cref="PhysicsShape.surfaceMaterial"/>.
        /// </summary>
        public readonly PhysicsShape.SurfaceMaterial.MixingMode frictionMixing { get => PhysicsChain_GetFrictionMixing(this); set => PhysicsChain_SetFrictionMixing(this, value); }

        /// <summary>
        /// Defines the method used when mixing the friction values of two shapes to form a shape contact.
        /// This is assigned to the current <see cref="PhysicsShape.surfaceMaterial"/>.
        /// </summary>
        public readonly PhysicsShape.SurfaceMaterial.MixingMode bouncinessMixing { get => PhysicsChain_GetBouncinessMixing(this); set => PhysicsChain_SetBouncinessMixing(this, value); }

        /// <summary>
        /// Get the number of Chain segments that this chain has created and owns.
        /// See <see cref="PhysicsShape.ShapeType.ChainSegment"/>.
        /// </summary>
        public readonly int segmentCount => PhysicsChain_GetSegmentCount(this);

        /// <summary>
        /// Get all the Chain segments that this chain has created and owns.
        /// </summary>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The chain segments that this chain has created and owns. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsShape> GetSegments(Allocator allocator = Allocator.Temp) => PhysicsChain_GetSegments(this, allocator).ToNativeArray<PhysicsShape>();

        /// <summary>
        /// Get the index of the specified Chain Segment PhysicsShape.
        /// </summary>
        /// <param name="chainSegmentShape">The chain segment shape to find the index of.</param>
        /// <returns>The index of the chain segment shape in its parent chain. This is a value of zero to the number of chain segment shapes - 1.</returns>
        /// <exception cref="System.ArgumentException">Thrown if the chain segment shape is not a chain segment shape or does not belong to the current chain.</exception>
        public readonly int GetSegmentIndex(PhysicsShape chainSegmentShape)
        {
            if (!chainSegmentShape.isChainSegment || chainSegmentShape.chain != this)
                throw new ArgumentException("The specified chain segment shape is not a chain segment or does not belong to the current chain shape.", nameof(chainSegmentShape));

            return PhysicsChain_GetSegmentIndex(this, chainSegmentShape);
        }

        /// <summary>
        /// Set the owner object using the specified owner key.
        /// You can only set the owner once, multiple attempts will produce a warning.
        /// This call does not bind the lifetime of the specified owner object, it is simply a reference.
        /// Whilst it is valid to not specify an owner object (NULL), it is recommended for debugging purposes.
        /// </summary>
        /// <param name="chains">The chains to set ownership for.</param>
        /// <param name="owner">The object that owns this key. Whilst it is valid to not specify an owner object (NULL), it is recommended for debugging purposes.</param>
        /// <param name="ownerKey">The owner key to be used. The value must be non-zero. You can use <see cref="PhysicsWorld.CreateOwnerKey(UnityEngine.Object)"/> for this value although any non-zero integer will work.</param>
        public static void SetOwner(ReadOnlySpan<PhysicsChain> chains, UnityEngine.Object owner, int ownerKey) => PhysicsChain_SetOwner(chains, owner, ownerKey);

        /// <summary>
        /// Set the owner object using the specified owner key.
        /// You can only set the owner once, multiple attempts will produce a warning.
        /// This call does not bind the lifetime of the specified owner object, it is simply a reference.
        /// It is also valid to not specify an owner object (NULL) to simply gain an owner key however it can be useful, if simply for debugging purposes and discovery, to know which object is the owner.
        /// </summary>
        /// <param name="owner">The object that owns this key. This can be NULL if not required but is recommended as the key is formed in part by the hash-code of the owner object.</param>
        /// <param name="ownerKey">The owner key to be used. If zero then a new owner key is created. You can use <see cref="PhysicsWorld.CreateOwnerKey(UnityEngine.Object)"/> for this value although any non-zero integer will work.</param>
        public unsafe readonly void SetOwner(UnityEngine.Object owner, int ownerKey)
        {
            var chain = this;
            SetOwner(new ReadOnlySpan<PhysicsChain>(&chain, 1), owner, ownerKey);
        }

        /// <summary>
        /// Set the owner object using the specified owner key.
        /// You can only set the owner once, multiple attempts will produce a warning.
        /// This call does not bind the lifetime of the specified owner object, it is simply a reference.
        /// It is also valid to not specify an owner object (NULL) to simply gain an owner key however it can be useful, if simply for debugging purposes and discovery, to know which object is the owner.
        /// </summary>
        /// <param name="owner">The object that owns this key. This can be NULL if not required but is recommended as the key is formed in part by the hash-code of the owner object.</param>
        /// <returns>The owner key assigned.</returns>
        public readonly int SetOwner(UnityEngine.Object owner)
        {
            var ownerKey = PhysicsWorld.CreateOwnerKey(owner);
            SetOwner(owner, ownerKey);
            return ownerKey;
        }

        /// <summary>
        /// Get the owner object associated with this chain as specified using <see cref="PhysicsChain.SetOwner(UnityEngine.Object)"/>.
        /// </summary>
        /// <returns>The owner object associated with this chain or NULL if no owner has been specified.</returns>
        public readonly UnityEngine.Object GetOwner() => PhysicsChain_GetOwner(this);

        /// <summary>
        /// Get if the chain is owned.
        /// See <see cref="PhysicsChain.SetOwner(UnityEngine.Object)"/>.
        /// </summary>
        public readonly bool isOwned => PhysicsChain_IsOwned(this);

        /// <summary>
        /// Get/Set the <see cref="System.Object"/> that callbacks for the shapes owned by this chain will be sent to.
        /// Care should be taken with any <see cref="System.Object"/> assigned as a callback target that isn't a <see cref="UnityEngine.Object"/> as this assignment will not in itself keep the object alive and can be garbage collected.
        /// To avoid this, you should have at least a single reference to the object in your code.
        /// To remove the object assigned here, set the callback target to NULL.
        /// 
        /// This includes the following events:
        /// 
        ///- A <see cref="PhysicsEvents.ContactFilterEvent"/> with call <see cref="PhysicsCallbacks.IContactFilterCallback"/>.
        ///- A <see cref="PhysicsEvents.PreSolveEvent"/> with call <see cref="PhysicsCallbacks.IPreSolveCallback"/>.
        ///- A <see cref="PhysicsEvents.TriggerBeginEvent"/> with call <see cref="PhysicsCallbacks.ITriggerCallback"/>.
        ///- A <see cref="PhysicsEvents.TriggerEndEvent"/> with call <see cref="PhysicsCallbacks.ITriggerCallback"/>.
        ///- A <see cref="PhysicsEvents.ContactBeginEvent"/> with call <see cref="PhysicsCallbacks.IContactCallback"/>.
        ///- A <see cref="PhysicsEvents.ContactEndEvent"/> with call <see cref="PhysicsCallbacks.IContactCallback"/>.
        /// </summary>
        public readonly System.Object callbackTarget { get => PhysicsChain_GetCallbackTarget(this); set => PhysicsChain_SetCallbackTarget(this, value); }

        /// <summary>
        /// Get/Set <see cref="PhysicsUserData"/> that can be used for any purpose.
        /// The physics system doesn't use this data, it is entirely for custom use.
        /// </summary>
        public readonly PhysicsUserData userData { get => PhysicsChain_GetUserData(this); set => PhysicsChain_SetUserData(this, value); }

        /// <summary>
        /// Get <see cref="PhysicsUserData"/> that can be used for any purpose, typically by the owner only.
        /// </summary>
        public readonly PhysicsUserData ownerUserData { get => PhysicsChain_GetOwnerUserData(this); }

        /// <summary>
        /// Set <see cref="PhysicsUserData"/> that can be used for any purpose, typically by the owner only.
        /// </summary>
        /// <param name="physicsUserData">The user data to set.</param>
        /// <param name="ownerKey">Optional owner key returned when using <see cref="PhysicsChain.SetOwner(UnityEngine.Object)"/>.</param>
        public readonly void SetOwnerUserData(PhysicsUserData physicsUserData, int ownerKey = 0) => PhysicsChain_SetOwnerUserData(this, physicsUserData, ownerKey);
    }
}
