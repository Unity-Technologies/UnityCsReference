// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Unity.Collections;
using UnityEngine.Serialization;
using static UnityEngine.LowLevelPhysics2D.PhysicsLowLevelScripting2D;

namespace UnityEngine.LowLevelPhysics2D
{
    /// <summary>
    /// A shape is attached to a body and defines an area to which two distinct types of behaviour are handled:
    /// 
    ///- Collision: Contacts between shapes produce a collision response on their respective bodies, assuming their body type is Dynamic.
    ///- Trigger: Contacts between shapes do not produce a collision response, only the fact that they're overlapping is reported.
    ///
    /// An unlimited number of shapes can be attached to a single body, known as a compound body.
    /// A shape is automatically destroyed when the body it is attached to is destroyed. A shape cannot exist unattached from a body.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly partial struct PhysicsShape : IEquatable<PhysicsShape>
    {
        #region Id

        readonly Int32 m_Index1;
        readonly UInt16 m_World0;
        readonly UInt16 m_Generation;

        /// <undoc/>
        public override readonly string ToString() => isValid ? $"type={shapeType}, index={m_Index1}, world={m_World0}, generation={m_Generation}" : "<INVALID>";

        #endregion

        #region Equality

        /// <undoc/>
        public override bool Equals(object obj) { return base.Equals(obj); }

        /// <undoc/>
        public bool Equals(PhysicsShape other) { return m_Index1 == other.m_Index1 && m_World0 == other.m_World0 && m_Generation == other.m_Generation; }

        /// <undoc/>
        public static bool operator ==(PhysicsShape lhs, PhysicsShape rhs) => lhs.Equals(rhs);

        /// <undoc/>
        public static bool operator !=(PhysicsShape lhs, PhysicsShape rhs) => !(lhs == rhs);

        /// <undoc/>
        public override int GetHashCode() { return HashCode.Combine(m_Index1, m_World0, m_Generation); }

        #endregion

        /// <summary>
        /// The type of shape.
        /// Some shapes are "closed" meaning they have an interior which will produce contacts.
        /// Some shapes are "open" meaning they do not have an interior and will only produce contacts when their boundary is intersected.
        /// </summary>
        public enum ShapeType
        {
            /// <summary>
            /// A circle with an offset. This is a closed shape.
            /// See <see cref="LowLevelPhysics2D.CircleGeometry"/>.
            /// </summary>
            Circle,

            /// <summary>
            /// A capsule is an extruded circle. This is a closed shape.
            /// See <see cref="LowLevelPhysics2D.CapsuleGeometry"/>.
            /// </summary>
            Capsule,

            /// <summary>
            /// A line segment. This is an open shape.
            /// See <see cref="LowLevelPhysics2D.SegmentGeometry"/>.
            /// </summary>
            Segment,

            /// <summary>
            /// A convex polygon. This is a closed shape.
            /// See <see cref="LowLevelPhysics2D.PolygonGeometry"/>.
            /// </summary>
            Polygon,

            /// <summary>
            /// A Chain of line segments that are joined together with other line segments. This is an open shape.
            /// This is indirectly created and owned by a chain.
            /// See <see cref="LowLevelPhysics2D.ChainSegmentGeometry"/> and <see cref="LowLevelPhysics2D.ChainGeometry"/>.
            /// </summary>
            ChainSegment
        }

        /// <summary>
        /// Defines the dynamics of a surface on a shape.
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public partial struct SurfaceMaterial
        {
            /// <summary>
            /// The method used to mix friction or bounciness values.
            /// </summary>
            public enum MixingMode
            {
                /// <summary>
                /// The average of both surface values.
                /// </summary>
                Average = 0,

                /// <summary>
                /// The geometric mean of both surface values.
                /// </summary>
                Mean,

                /// <summary>
                /// The product of both surface values.
                /// </summary>
                Multiply,

                /// <summary>
                /// The minium of both surface values.
                /// </summary>
                Minimum,

                /// <summary>
                /// The maximum of both surface values.
                /// </summary>
                Maximum
            }

            /// <summary>
            /// Create a default surface material.
            /// </summary>
            public SurfaceMaterial() { this = Default; }

            /// <summary>
            /// Get the default surface material.
            /// </summary>
            public static SurfaceMaterial Default => PhysicsShape_GetDefaultSurfaceMaterial();

            /// <summary>
            /// The Coulomb (dry) friction coefficient, usually in the range [0, 1].
            /// </summary>
            public float friction { readonly get => m_Friction; set => m_Friction = Mathf.Max(0.0f, value); }

            /// <summary>
	        /// The bounciness (coefficient of restitution) usually in the range [0, 1].
            /// </summary>
            public float bounciness { readonly get => m_Bounciness; set => m_Bounciness = Mathf.Max(0.0f, value); }

            /// <summary>
            /// Defines the method used when mixing the friction values of two shapes to form a contact.
            /// </summary>
            public MixingMode frictionMixing { readonly get => m_FrictionMixing; set => m_FrictionMixing = value; }

            /// <summary>
            /// Defines the method used when mixing the bounciness values of two shapes to form a contact.
            /// </summary>
            public MixingMode bouncinessMixing { readonly get => m_BouncinessMixing; set => m_BouncinessMixing = value; }

            /// <summary>
            /// The priority for mixing the <see cref="LowLevelPhysics2D.PhysicsShape.friction"/> properties when two shapes come into contact.
            /// If the priority of one shape is higher than the other shape then the higher priority <see cref="LowLevelPhysics2D.PhysicsShape.SurfaceMaterial.frictionMixing"/> will be used.
            /// If the priority of both shapes are the same then simply the higher enumeration value of <see cref="LowLevelPhysics2D.PhysicsShape.SurfaceMaterial.MixingMode"/> from both shapes will be used.
            /// </summary>
            public UInt16 frictionPriority {  readonly get => m_FrictionPriority; set => m_FrictionPriority = value;}

            /// <summary>
            /// The priority for mixing the <see cref="LowLevelPhysics2D.PhysicsShape.bounciness"/> properties when two shapes come into contact.
            /// If the priority of one shape is higher than the other shape then the higher priority <see cref="LowLevelPhysics2D.PhysicsShape.SurfaceMaterial.bouncinessMixing"/> will be used.
            /// If the priority of both shapes are the same then simply the higher enumeration value of <see cref="LowLevelPhysics2D.PhysicsShape.SurfaceMaterial.MixingMode"/> from both shapes will be used.
            /// </summary>
            public UInt16 bouncinessPriority {  readonly get => m_BouncinessPriority; set => m_BouncinessPriority = value;}

            /// <summary>
	        /// The rolling resistance usually in the range [0, 1].
            /// </summary>
            public float rollingResistance { readonly get => m_RollingResistance; set => m_RollingResistance = Mathf.Max(0.0f, value); }

            /// <summary>
	        /// The tangent (surface) speed.
            /// </summary>
	        public float tangentSpeed { readonly get => m_TangentSpeed; set => m_TangentSpeed = value; }

            /// <summary>
            /// Custom debug draw color. Any color value other than (0,0,0,0) will be used to render the shape.
            /// This value is passed back when using the PhysicsWorld debug drawing. The alpha value here is always ignored.
            /// This is only used in the Unity Editor or in a Development Player.
            /// See <see cref="UnityEngine.Color.clear"/> and <see cref="LowLevelPhysics2D.PhysicsWorld.Draw"/>.
            /// </summary>
            public Color32 customColor { readonly get => m_CustomColor; set => m_CustomColor = value; }

            #region Internal
            
            [SerializeField] [Min(0.0f)] float m_Friction;
            [SerializeField] [Min(0.0f)] float m_Bounciness;
            [FormerlySerializedAs("m_FrictionCombine")][SerializeField] MixingMode m_FrictionMixing;
            [FormerlySerializedAs("m_BouncinessCombine")][SerializeField] MixingMode m_BouncinessMixing;
            [SerializeField] [Range(0, UInt16.MaxValue)] UInt16 m_FrictionPriority;
            [SerializeField] [Range(0, UInt16.MaxValue)] UInt16 m_BouncinessPriority;
            [SerializeField] [Min(0.0f)] float m_RollingResistance;
	        [SerializeField] float m_TangentSpeed;
            [SerializeField] Color32 m_CustomColor;

            #endregion
        }

        /// <summary>
        /// A contact manifold describes the contact points between colliding shapes.
        /// Speculative collision is used so some contact points may be separated, a property available per-contact.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct ContactManifold : IEnumerable<ContactManifold.ManifoldPoint>
        {
            /// <summary>
            /// The unit normal vector in world space, points from shape A to bodyB
            /// </summary>
            public readonly Vector2 normal => m_Normal;

            /// <summary>
	        /// Angular impulse applied for rolling resistance (N * m * s = kg * m^2 / s).
            /// </summary>
	        public readonly float rollingImpulse => m_RollingImpulse;

            /// <summary>
            /// The manifold points, up to two are possible.
            /// </summary>
            public readonly ManifoldPointArray points => m_Points;

            /// <summary>
            /// The number of manifold points available, in the range [0, 2].
            /// </summary>
            public readonly int pointCount => m_PointCount;

            /// <summary>
            /// The number of manifold points available that are speculative, in the range [0, 2].
            /// </summary>
            public readonly int speculativePointCount => m_Points.speculativePointCount;

            /// <summary>
            /// Contains all the detail related to the geometry and dynamics of the contact.
            /// You may use the <see cref="ManifoldPoint.totalNormalImpulse"/> to determine if there was an interaction during the time step.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            public readonly struct ManifoldPoint
            {
                /// <summary>
                /// Location of the contact point in world space. Subject to precision loss at large coordinates.
                /// </summary>
                public readonly Vector2 point => m_Point;

                /// <summary>
                /// Location of the contact point relative to shapeA's origin in world space.
                /// </summary>
                public readonly Vector2 anchorA => m_AnchorA;

                /// <summary>
                /// Location of the contact point relative to shapeB's origin in world space.
                /// </summary>
                public readonly Vector2 anchorB => m_AnchorB;

                /// <summary>
                /// The separation of the contact point, negative if penetrating.
                /// </summary>
                public readonly float separation => m_Separation;

                /// <summary>
                /// The impulse along the manifold normal vector.
                /// </summary>
                public readonly float normalImpulse => m_NormalImpulse;

                /// <summary>
                /// The friction impulse.
                /// </summary>
                public readonly float tangentImpulse => m_TangentImpulse;

                /// <summary>
	            /// The total normal impulse applied across sub-stepping and restitution.
                /// This can be used to identify speculative contact points that had an interaction during the simulation step.
                /// </summary>
	            public readonly float totalNormalImpulse => m_TotalNormalImpulse;

                /// <summary>
                /// Relative normal velocity pre-solve. Used for hit events.
                /// If the normal impulse is zero then there was no hit. Negative means shapes are approaching.
                /// </summary>
                public readonly float normalVelocity => m_NormalVelocity;

                /// <summary>
                /// Uniquely identifies a contact point between two shapes.
                /// This should not be confused with <see cref="LowLevelPhysics2D.PhysicsShape.ContactId"/>.
                /// </summary>
                public readonly UInt16 id => m_Id;

                /// <summary>
                /// Did this contact point exist the previous step?
                /// </summary>
                public readonly bool persisted => m_Persisted;

                /// <summary>
                /// Is the contact point speculative i.e. not currently interacting?
                /// </summary>
                public readonly bool speculative => totalNormalImpulse > 0.0f;

                #region Internal

                readonly Vector2 m_Point;
                readonly Vector2 m_AnchorA;
                readonly Vector2 m_AnchorB;
                readonly float m_Separation;
                readonly float m_NormalImpulse;
                readonly float m_TangentImpulse;
	            readonly float m_TotalNormalImpulse;
                readonly float m_NormalVelocity;
                readonly UInt16 m_Id;
                readonly bool m_Persisted;

                #endregion
            }

            /// <summary>
            /// Fixed-sized manifold point array.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            public readonly struct ManifoldPointArray
            {
                /// <summary>
                /// Manifold Point #0.
                /// </summary>
                public readonly ManifoldPoint contactInfo0 => m_ContactInfo0;

                /// <summary>
                /// Manifold Point #1.
                /// </summary>
                public readonly ManifoldPoint contactInfo1 => m_ContactInfo1;

                /// <summary>
                /// Indexer to access the manifold points in the array.
                /// </summary>
                /// <param name="index">The index of the manifold point required (must be 0 or 1).</param>
                /// <returns>The specified manifold point.</returns>
                /// <exception cref="System.IndexOutOfRangeException">Thrown if the index is not 0 or 1.</exception>
                public readonly unsafe ManifoldPoint this[int index]
                {
                    /// <summary>
                    /// Accessor for the manifold points.
                    /// Some or all of these contacts may not be valid and be at default.
                    /// See <see cref="ContactManifold.pointCount"/>.
                    /// </summary>
                    /// <param name="index">The index to access.</param>
                    /// <returns>The manifold point.</returns>
                    /// <exception cref="System.IndexOutOfRangeException">Thrown if the index is not 0 or 1.</exception>
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get
                    {
                        if (index >= 0 && index < 2)
                        {
                            fixed (ManifoldPoint* pThis = &m_ContactInfo0)
                            {
                                return pThis[index];
                            }
                        }

                        throw new IndexOutOfRangeException($"{index} must be in the range [0, 1]");
                    }
                }

                /// <summary>
                /// The number of manifold points available that are speculative, in the range [0, 2].
                /// </summary>
                public readonly int speculativePointCount => (m_ContactInfo0.speculative ? 1 : 0) + (m_ContactInfo1.speculative ? 1 : 0);

                #region Internal

                readonly ManifoldPoint m_ContactInfo0;
                readonly ManifoldPoint m_ContactInfo1;

                #endregion
            }

            /// <summary>
            /// Indexer to access the manifold points.
            /// </summary>
            /// <param name="index">The index of the manifold point required (must be 0 or 1).</param>
            /// <returns>The specified manifold point.</returns>
            /// <exception cref="System.IndexOutOfRangeException">Thrown if the index is not 0 or 1.</exception>
            public readonly ManifoldPoint this[int index]
            {
                /// <summary>
                /// Accessor for the manifold points.
                /// </summary>
                /// <param name="index">The index to access.</param>
                /// <returns>The manifold point.</returns>
                /// <exception cref="System.IndexOutOfRangeException">Thrown if the index is < 0 or beyond the <see cref="ContactManifold.pointCount"/> - 1.</exception>
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    if (index >= 0 && index < pointCount)
                        return points[index];

                    throw new IndexOutOfRangeException($"{index} is not valid. The current number of valid points is {pointCount}");
                }
            }

            #region Internal

            readonly Vector2 m_Normal;
	        readonly float m_RollingImpulse;
            readonly ManifoldPointArray m_Points;
            readonly int m_PointCount;

            #endregion

            #region Enumeration

            /// <undoc/>
            public readonly IEnumerator<ManifoldPoint> GetEnumerator() => new ManifoldPointIterator(this);

            /// <undoc/>
            readonly IEnumerator IEnumerable.GetEnumerator() => new ManifoldPointIterator(this);

            /// <undoc/>
            public struct ManifoldPointIterator : IEnumerator<ManifoldPoint>
            {
                private ContactManifold m_ContactManifold;
                private int m_PointIndex;

                /// <undoc/>
                public ManifoldPointIterator(ContactManifold contactManifold)
                {
                    m_ContactManifold = contactManifold;
                    m_PointIndex = -1;
                }

                /// <undoc/>
                readonly ManifoldPoint IEnumerator<ManifoldPoint>.Current => m_ContactManifold[m_PointIndex];

                /// <undoc/>
                readonly object Current => m_ContactManifold[m_PointIndex];

                /// <undoc/>
                readonly object IEnumerator.Current => Current;

                /// <undoc/>
                bool IEnumerator.MoveNext() => ++m_PointIndex < m_ContactManifold.pointCount;

                /// <undoc/>
                void IEnumerator.Reset() => m_PointIndex = -1;

                /// <undoc/>
                readonly void IDisposable.Dispose() { }
            }

            #endregion
        }

        /// <summary>
        /// The contact between two shapes. By convention the manifold normal points from shape A to shape B.
        /// See <see cref="LowLevelPhysics2D.PhysicsBody.GetContacts"/> and <see cref="LowLevelPhysics2D.PhysicsShape.GetContacts"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct Contact
        {
            /// <summary>
	        /// The unique Id of this contact.
            /// This contact is volatile and may be destroyed automatically when the world is modified or simulated therefore it should always be checked for validity with <see cref="ContactId.isValid"/>.
            /// </summary>
            public readonly ContactId contactId => m_ContactId;

            /// <summary>
            /// One of the shapes involved in the contact.
            /// </summary>
            public readonly PhysicsShape shapeA => m_ShapeA;

            /// <summary>
            /// The other shape involved in the contact.
            /// </summary>
            public readonly PhysicsShape shapeB => m_ShapeB;

            /// <summary>
            /// The contact manifold describing the contact.
            /// </summary>
            public readonly ContactManifold manifold => m_Manifold;

            #region Internal

            readonly ContactId m_ContactId;
            readonly PhysicsShape m_ShapeA;
            readonly PhysicsShape m_ShapeB;
            readonly ContactManifold m_Manifold;

            #endregion
        }

        /// <summary>
	    /// The unique Id of the contact.
        /// This contact is volatile and may be destroyed automatically when the world is modified or simulated therefore it should always be checked for validity.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct ContactId
        {
            #region Id

            private readonly Int32 m_IndexId;
            private readonly UInt16 m_WorldId;
            private readonly UInt16 m_Padding;
            private readonly Int32 m_GenerationId;

            /// <undoc/>
            public override readonly string ToString() => isValid ? $"index={m_IndexId}, world={m_WorldId}, generation={m_GenerationId}" : "<INVALID>";

            #endregion

            /// <summary>
            /// Check if the contact is valid or not.
            /// </summary>
            public readonly bool isValid => PhysicsContactId_IsValid(this);

            /// <summary>
            /// Get the contact.
            /// </summary>
            public readonly Contact contact => PhysicsContactId_GetContact(this);
        }

        /// <summary>
        /// A contact filter is used to control what contacts are created when intersecting other shapes.
        /// A contact filter contains a filter with the addition of a group index allowing overrides to the filter.
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct ContactFilter
        {
            /// <summary>
            /// Create a contact filter.
            /// </summary>
            /// <param name="categories">A <see cref="LowLevelPhysics2D.PhysicsMask"/> defining the categories this object is in.</param>
            /// <param name="contacts">A <see cref="LowLevelPhysics2D.PhysicsMask"/> defining the categories this object will produce contacts with.</param>
            /// <param name="groupIndex">The override group this filter belongs to with zero indicating no group. Groups allow a certain group of objects to never collide (negative) or always collide (positive).</param>
            public ContactFilter(PhysicsMask categories, PhysicsMask contacts, Int32 groupIndex = 0)
            {
                m_Categories = categories;
                m_Contacts = contacts;
                m_GroupIndex = groupIndex;
            }

            /// <summary>
            /// The categories this object is in. Usually you would only set one bit but multiple are allowed.
            /// </summary>
            public PhysicsMask categories { readonly get => m_Categories; set => m_Categories = value; }

            /// <summary>
            /// The categories this object will produce contacts with.
            /// </summary>
            public PhysicsMask contacts { readonly get => m_Contacts; set => m_Contacts = value; }

            /// <summary>
            /// Collision groups allow a certain group of objects to never collide (negative) or always collide (positive).
            /// A group index of zero has no effect. A non-zero group always overrides the category/contacts masks.
            /// 
            /// The rules for two shapes coming into contact are:
            /// 
            ///- If either shape has a group of zero then the group is ignored and the category/contacts masks are used.
            ///- If both shapes have a non-zero but different group then the category/contacts masks are used.
            ///- If both shapes have an identical and positive group then they will always produce a contact.
            ///- If both shapes have an identical and negative group then they will never produce a contact.
            /// </summary>
            public Int32 groupIndex { readonly get => m_GroupIndex; set => m_GroupIndex = value; }

            /// <summary>
            /// The default categories used.
            /// </summary>
            public static PhysicsMask DefaultCategories = PhysicsMask.One;

            /// <summary>
            /// The default contacts used.
            /// </summary>
            public static PhysicsMask DefaultContacts = PhysicsMask.All;

            /// <summary>
            /// Get a contact filter that is all categories and contacts everything.
            /// </summary>
            public static ContactFilter Everything = new(PhysicsMask.All, PhysicsMask.All);

            /// <summary>
            /// Get a default contact filter that contacts everything.
            /// </summary>
            public static ContactFilter defaultFilter = new(DefaultCategories, DefaultContacts);

            #region Internal

            [SerializeField] internal PhysicsMask m_Categories;
            [SerializeField] internal PhysicsMask m_Contacts;
            [SerializeField] internal Int32 m_GroupIndex;

            #endregion
        }

        /// <summary>
        /// Fixed vertex shape array.
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct ShapeArray
        {
            /// <summary>
            /// Vertex #0.
            /// </summary>
            public Vector2 vertex0 { readonly get => m_Vertex0; set => m_Vertex0 = value; }

            /// <summary>
            /// Vertex #1.
            /// </summary>
            public Vector2 vertex1 { readonly get => m_Vertex1; set => m_Vertex1 = value; }

            /// <summary>
            /// Vertex #2.
            /// </summary>
            public Vector2 vertex2 { readonly get => m_Vertex2; set => m_Vertex2 = value; }

            /// <summary>
            /// Vertex #3.
            /// </summary>
            public Vector2 vertex3 { readonly get => m_Vertex3; set => m_Vertex3 = value; }

            /// <summary>
            /// Vertex #4.
            /// </summary>
            public Vector2 vertex4 { readonly get => m_Vertex4; set => m_Vertex4 = value; }

            /// <summary>
            /// Vertex #5.
            /// </summary>
            public Vector2 vertex5 { readonly get => m_Vertex5; set => m_Vertex5 = value; }

            /// <summary>
            /// Vertex #6.
            /// </summary>
            public Vector2 vertex6 { readonly get => m_Vertex6; set => m_Vertex6 = value; }

            /// <summary>
            /// Vertex #7.
            /// </summary>
            public Vector2 vertex7 { readonly get => m_Vertex7; set => m_Vertex7 = value; }

            /// <summary>
            /// Accessor for the shape array.
            /// </summary>
            /// <param name="index">The index to access.</param>
            /// <returns>The array index value.</returns>
            /// <exception cref="System.IndexOutOfRangeException">Thrown if the index is not in the range [0, <see cref="LowLevelPhysics2D.PhysicsConstants.MaxPolygonVertices"/> -1].</exception>
            public unsafe ref Vector2 this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    if (index >= 0 && index < PhysicsConstants.MaxPolygonVertices)
                    {
                        fixed (Vector2* pThis = &m_Vertex0)
                        {
                            return ref pThis[index];
                        }
                    }

                    throw new IndexOutOfRangeException($"{index} must be in the range [0, {PhysicsConstants.MaxPolygonVertices - 1}]");
                }
            }

            #region Internal

            [SerializeField] internal Vector2 m_Vertex0;
            [SerializeField] Vector2 m_Vertex1;
            [SerializeField] Vector2 m_Vertex2;
            [SerializeField] Vector2 m_Vertex3;
            [SerializeField] Vector2 m_Vertex4;
            [SerializeField] Vector2 m_Vertex5;
            [SerializeField] Vector2 m_Vertex6;
            [SerializeField] Vector2 m_Vertex7;

            #endregion
        }

        /// <summary>
        /// The mover data assigned to a <see cref="LowLevelPhysics2D.PhysicsShape.moverData"/>.
        /// This is used when <see cref="LowLevelPhysics2D.PhysicsShape"/> are encountered when using <see cref="LowLevelPhysics2D.PhysicsWorld.CastMover(PhysicsQuery.WorldMoverInput)"/>.
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct MoverData
        {
            /// <summary>
            /// Create a default mover data.
            /// </summary>
            public MoverData()
            {
                m_PushLimit = float.MaxValue;
                m_ClipVelocity = true;
            }

            /// <summary>
            /// Controls the amount this shape can push against a mover.
            /// To effectively set no limit, use <see cref="System.Single.MaxValue"/>.
            /// </summary>
            public float pushLimit { readonly get => m_PushLimit; set => m_PushLimit = value; }

            /// <summary>
            /// Controls if this shape can clip the mover velocity.
            /// </summary>
            public bool clipVelocity { readonly get => m_ClipVelocity; set => m_ClipVelocity = value; }

            #region Internal

            float m_PushLimit;
            bool m_ClipVelocity;

            #endregion
        }

        /// <summary>
        /// A proxy of a shape in a generic form suited to representing all support shape types.
        /// You can provide between 1 and <see cref="LowLevelPhysics2D.PhysicsConstants.MaxPolygonVertices"/>and a radius.
        /// 
        ///- A <see cref="LowLevelPhysics2D.CircleGeometry"/> is a single point with a non-zero radius (zero radius is allowed however and defines a point).
        ///- A <see cref="LowLevelPhysics2D.CapsuleGeometry"/> is two points with a non-zero radius.
        ///- A <see cref="LowLevelPhysics2D.PolygonGeometry"/> box is the points with and an optional radius.
        ///- A <see cref="LowLevelPhysics2D.SegmentGeometry"/> is two points with a zero radius.
        ///- A <see cref="LowLevelPhysics2D.ChainSegmentGeometry"/> is two points with a zero radius.
        ///
        /// To create a proxy, simply provide the geometry to the constructor.
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct ShapeProxy
        {
            /// <summary>
            /// Create a shape proxy representing a single point.
            /// </summary>
            /// <param name="point">The point to represent.</param>
            public ShapeProxy(Vector2 point)
            {
                m_Vertices = new ShapeArray() { vertex0 = point };
                m_Count = 1;
                m_Radius = 0.0f;
            }

            /// <summary>
            /// Create a shape proxy using the specified Circle.
            /// </summary>
            /// <param name="circleGeometry">The geometry to use.</param>
            public ShapeProxy(CircleGeometry circleGeometry)
            {
                if (!circleGeometry.isValid)
                    throw new ArgumentException(nameof(circleGeometry), "Circle Geometry is not valid.");

                m_Vertices = new ShapeArray() { vertex0 = circleGeometry.center };
                m_Count = 1;
                m_Radius = circleGeometry.radius;
            }

            /// <summary>
            /// Create a shape proxy using the specified Capsule.
            /// </summary>
            /// <param name="capsuleGeometry">The geometry to use.</param>
            public ShapeProxy(CapsuleGeometry capsuleGeometry)
            {
                if (!capsuleGeometry.isValid)
                    throw new ArgumentException(nameof(capsuleGeometry), "Capsule Geometry is not valid.");

                m_Vertices = new ShapeArray() { vertex0 = capsuleGeometry.center1, vertex1 = capsuleGeometry.center2 };
                m_Count = 2;
                m_Radius = capsuleGeometry.radius;
            }

            /// <summary>
            /// Create a shape proxy using the specified Polygon.
            /// </summary>
            /// <param name="polygonGeometry">The geometry to use.</param>
            public ShapeProxy(PolygonGeometry polygonGeometry)
            {
                if (!polygonGeometry.isValid)
                    throw new ArgumentException(nameof(polygonGeometry), "Polygon Geometry is not valid.");

                m_Vertices = polygonGeometry.vertices;
                m_Count = polygonGeometry.count;
                m_Radius = polygonGeometry.radius;
            }

            /// <summary>
            /// Create a shape proxy using the specified Segment.
            /// </summary>
            /// <param name="segmentGeometry">The geometry to use.</param>
            public ShapeProxy(SegmentGeometry segmentGeometry)
            {
                if (!segmentGeometry.isValid)
                    throw new ArgumentException(nameof(segmentGeometry), "Segment Geometry is not valid.");

                m_Vertices = new ShapeArray() { vertex0 = segmentGeometry.point1, vertex1 = segmentGeometry.point2 };
                m_Count = 2;
                m_Radius = 0f;
            }

            /// <summary>
            /// Create a shape proxy using the specified ChainSegment.
            /// </summary>
            /// <param name="chainSegmentGeometry">The geometry to use.</param>
            public ShapeProxy(ChainSegmentGeometry chainSegmentGeometry)
            {
                if (!chainSegmentGeometry.isValid)
                    throw new ArgumentException(nameof(chainSegmentGeometry), "Chain Segment Geometry is not valid.");

                m_Vertices = new ShapeArray() { vertex0 = chainSegmentGeometry.segment.point1, vertex1 = chainSegmentGeometry.segment.point2 };
                m_Count = 2;
                m_Radius = 0.0f;
            }

            /// <summary>
            /// Get a <see cref="LowLevelPhysics2D.CircleGeometry"/> from the shape proxy.
            /// The <see cref="LowLevelPhysics2D.PhysicsShape.ShapeProxy.count"/> must be 1.
            /// </summary>
            /// <exception cref="InvalidOperationException">Thrown if the <see cref="LowLevelPhysics2D.PhysicsShape.ShapeProxy.count"/> is not 1.</exception>
            public CircleGeometry circleGeometry
            {
                get
                {
                    if (m_Count == 1)
                        return new CircleGeometry { center = m_Vertices[0], radius = m_Radius };

                    throw new InvalidOperationException($"Expected a vertex count of 1.");
                }
            }

            /// <summary>
            /// Get a <see cref="LowLevelPhysics2D.CapsuleGeometry"/> from the shape proxy.
            /// The <see cref="LowLevelPhysics2D.PhysicsShape.ShapeProxy.count"/> must be 2.
            /// </summary>
            /// <exception cref="InvalidOperationException">Thrown if the <see cref="LowLevelPhysics2D.PhysicsShape.ShapeProxy.count"/> is not 2.</exception>
            public CapsuleGeometry capsuleGeometry
            {
                get
                {
                    if (m_Count == 2)
                        return new CapsuleGeometry { center1 = m_Vertices[0], center2 = m_Vertices[1], radius = m_Radius };

                    throw new InvalidOperationException($"Expected a vertex count of 2.");
                }
            }

            /// <summary>
            /// Get a <see cref="LowLevelPhysics2D.PolygonGeometry"/> from the shape proxy.
            /// </summary>
            public unsafe PolygonGeometry polygonGeometry
            {
                get
                {
                    fixed (Vector2* pVertices = &m_Vertices.m_Vertex0)
                    {
                        return PolygonGeometry.Create(new ReadOnlySpan<Vector2>(pVertices, m_Count), m_Radius);
                    }
                }
            }

            /// <summary>
            /// Get a <see cref="LowLevelPhysics2D.SegmentGeometry"/> from the shape proxy.
            /// The <see cref="LowLevelPhysics2D.PhysicsShape.ShapeProxy.count"/> must be 2.
            /// </summary>
            /// <exception cref="InvalidOperationException">Thrown if the <see cref="LowLevelPhysics2D.PhysicsShape.ShapeProxy.count"/> is not 2.</exception>
            public SegmentGeometry segmentGeometry
            {
                get
                {
                    if (m_Count == 2)
                        return new SegmentGeometry { point1 = m_Vertices[0], point2 = m_Vertices[1] };

                    throw new InvalidOperationException($"Expected a vertex count of 2.");
                }
            }

            /// <summary>
            /// The shape vertices.
            /// </summary>
            public ShapeArray vertices { readonly get => m_Vertices; set => m_Vertices = value; }

            /// <summary>
            /// The number of vertices.
            /// </summary>
            public int count { readonly get => m_Count; set => m_Count = Mathf.Max(1, value); }

            /// <summary>
            /// The radius around the vertices.
            /// </summary>
            public float radius { readonly get => m_Radius; set => m_Radius = Mathf.Max(0f, value); }

            #region Internal

            [SerializeField] ShapeArray m_Vertices;
            [SerializeField] [Min(1)] int m_Count;
            [SerializeField] [Min(0.0f)] float m_Radius;

            #endregion
        }

        /// <summary>
        /// Create a Circle shape, using its default definition, attached to the specified body.
        /// See <see cref="LowLevelPhysics2D.PhysicsBody.CreateShape(CircleGeometry, PhysicsShapeDefinition)"/>.
        /// </summary>
        /// <param name="body">The body to attach the shape to.</param>
        /// <param name="geometry">The shape geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <returns>The created shape.</returns>
        public static PhysicsShape CreateShape(PhysicsBody body, CircleGeometry geometry) => CreateShape(body, geometry, PhysicsShapeDefinition.defaultDefinition);

        /// <summary>
        /// Create a Circle shape attached to the specified body.
        /// See <see cref="LowLevelPhysics2D.PhysicsBody.CreateShape(CircleGeometry, PhysicsShapeDefinition)"/>.
        /// </summary>
        /// <param name="body">The body to attach the shape to.</param>
        /// <param name="geometry">The shape geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <returns>The created shape.</returns>
        public static PhysicsShape CreateShape(PhysicsBody body, CircleGeometry geometry, PhysicsShapeDefinition definition) => PhysicsShape_CreateCircleShape(body, geometry, definition);

        /// <summary>
        /// Create a batch of Circle shapes attached to the specified body.
        /// </summary>
        /// <param name="body">The body to attach the shape to.</param>
        /// <param name="geometry">The shape geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The created shapes. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public unsafe static NativeArray<PhysicsShape> CreateShapeBatch(PhysicsBody body, ReadOnlySpan<CircleGeometry> geometry, PhysicsShapeDefinition definition, Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsShape_CreateShapeBatch(body, PhysicsBuffer.FromSpan<CircleGeometry>(geometry), PhysicsShape.ShapeType.Circle, definition, allocator).ToNativeArray<PhysicsShape>();

        /// <summary>
        /// Create a Polygon shape, using its default definition, attached to the specified body.
        /// See <see cref="LowLevelPhysics2D.PhysicsBody.CreateShape(PolygonGeometry, PhysicsShapeDefinition)"/>.
        /// </summary>
        /// <param name="body">The body to attach the shape to.</param>
        /// <param name="geometry">The shape geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <returns>The created shape.</returns>
        public static PhysicsShape CreateShape(PhysicsBody body, PolygonGeometry geometry) => CreateShape(body, geometry, PhysicsShapeDefinition.defaultDefinition);

        /// <summary>
        /// Create a Polygon shape attached to the specified body.
        /// See <see cref="LowLevelPhysics2D.PhysicsBody.CreateShape(PolygonGeometry, PhysicsShapeDefinition)"/>.
        /// </summary>
        /// <param name="body">The body to attach the shape to.</param>
        /// <param name="geometry">The shape geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <returns>The created shape.</returns>
        public static PhysicsShape CreateShape(PhysicsBody body, PolygonGeometry geometry, PhysicsShapeDefinition definition) => PhysicsShape_CreatePolygonShape(body, geometry, definition);

        /// <summary>
        /// Create a batch of Polygon shapes attached to the specified body.
        /// </summary>
        /// <param name="body">The body to attach the shape to.</param>
        /// <param name="geometry">The shape geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The created shapes. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public unsafe static NativeArray<PhysicsShape> CreateShapeBatch(PhysicsBody body, ReadOnlySpan<PolygonGeometry> geometry, PhysicsShapeDefinition definition, Allocator allocator = Allocator.Temp) => PhysicsShape_CreateShapeBatch(body, PhysicsBuffer.FromSpan<PolygonGeometry>(geometry), PhysicsShape.ShapeType.Polygon, definition, allocator).ToNativeArray<PhysicsShape>();

        /// <summary>
        /// Create a Capsule shape, using its default definition, attached to the specified body.
        /// See <see cref="LowLevelPhysics2D.PhysicsBody.CreateShape(CapsuleGeometry, PhysicsShapeDefinition)"/>.
        /// </summary>
        /// <param name="body">The body to attach the shape to.</param>
        /// <param name="geometry">The shape geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <returns>The created shape.</returns>
        public static PhysicsShape CreateShape(PhysicsBody body, CapsuleGeometry geometry) => CreateShape(body, geometry, PhysicsShapeDefinition.defaultDefinition);

        /// <summary>
        /// Create a Capsule shape attached to the specified body.
        /// See <see cref="LowLevelPhysics2D.PhysicsBody.CreateShape(CapsuleGeometry, PhysicsShapeDefinition)"/>.
        /// </summary>
        /// <param name="body">The body to attach the shape to.</param>
        /// <param name="geometry">The shape geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <returns>The created shape.</returns>
        public static PhysicsShape CreateShape(PhysicsBody body, CapsuleGeometry geometry, PhysicsShapeDefinition definition) => PhysicsShape_CreateCapsuleShape(body, geometry, definition);

        /// <summary>
        /// Create a batch of Capsule shapes attached to the specified body.
        /// </summary>
        /// <param name="body">The body to attach the shape to.</param>
        /// <param name="geometry">The shape geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The created shapes. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public unsafe static NativeArray<PhysicsShape> CreateShapeBatch(PhysicsBody body, ReadOnlySpan<CapsuleGeometry> geometry, PhysicsShapeDefinition definition, Allocator allocator = Allocator.Temp) => PhysicsShape_CreateShapeBatch(body, PhysicsBuffer.FromSpan<CapsuleGeometry>(geometry), PhysicsShape.ShapeType.Capsule, definition, allocator).ToNativeArray<PhysicsShape>();

        /// <summary>
        /// Create a Segment shape, using its default definition, attached to the specified body.
        /// See <see cref="LowLevelPhysics2D.PhysicsBody.CreateShape(SegmentGeometry, PhysicsShapeDefinition)"/>.
        /// </summary>
        /// <param name="body">The body to attach the shape to.</param>
        /// <param name="geometry">The shape geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <returns>The created shape.</returns>
        public static PhysicsShape CreateShape(PhysicsBody body, SegmentGeometry geometry) => CreateShape(body, geometry, PhysicsShapeDefinition.defaultDefinition);

        /// <summary>
        /// Create a Segment shape attached to the specified body.
        /// See <see cref="LowLevelPhysics2D.PhysicsBody.CreateShape(SegmentGeometry, PhysicsShapeDefinition)"/>.
        /// </summary>
        /// <param name="body">The body to attach the shape to.</param>
        /// <param name="geometry">The shape geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <returns>The created shape.</returns>
        public static PhysicsShape CreateShape(PhysicsBody body, SegmentGeometry geometry, PhysicsShapeDefinition definition) => PhysicsShape_CreateSegmentShape(body, geometry, definition);

        /// <summary>
        /// Create a batch of Segment shapes attached to the specified body.
        /// </summary>
        /// <param name="body">The body to attach the shape to.</param>
        /// <param name="geometry">The shape geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The created shapes. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public unsafe static NativeArray<PhysicsShape> CreateShapeBatch(PhysicsBody body, ReadOnlySpan<SegmentGeometry> geometry, PhysicsShapeDefinition definition, Allocator allocator = Allocator.Temp) => PhysicsShape_CreateShapeBatch(body, PhysicsBuffer.FromSpan<SegmentGeometry>(geometry), PhysicsShape.ShapeType.Segment, definition, allocator).ToNativeArray<PhysicsShape>();

        /// <summary>
        /// Create a Chain Segment shape attached to the specified body.
        /// See <see cref="LowLevelPhysics2D.PhysicsBody.CreateShape(ChainSegmentGeometry, PhysicsShapeDefinition)"/>.
        /// </summary>
        /// <param name="body">The body to attach the shape to.</param>
        /// <param name="geometry">The shape geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <returns>The created shape.</returns>
        public static PhysicsShape CreateShape(PhysicsBody body, ChainSegmentGeometry geometry, PhysicsShapeDefinition definition) => PhysicsShape_CreateChainSegmenShapet(body, geometry, definition);

        /// <summary>
        /// Create a Chain Segment shape, using its default definition, attached to the specified body.
        /// See <see cref="LowLevelPhysics2D.PhysicsBody.CreateShape(ChainSegmentGeometry, PhysicsShapeDefinition)"/>.
        /// </summary>
        /// <param name="body">The body to attach the shape to.</param>
        /// <param name="geometry">The shape geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <returns>The created shape.</returns>
        public static PhysicsShape CreateShape(PhysicsBody body, ChainSegmentGeometry geometry) => CreateShape(body, geometry, PhysicsShapeDefinition.defaultDefinition);

        /// <summary>
        /// Create a batch of Chain Segment shapes attached to the specified body.
        /// </summary>
        /// <param name="body">The body to attach the shape to.</param>
        /// <param name="geometry">The shape geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The created shapes. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public unsafe static NativeArray<PhysicsShape> CreateShapeBatch(PhysicsBody body, ReadOnlySpan<ChainSegmentGeometry> geometry, PhysicsShapeDefinition definition, Allocator allocator = Allocator.Temp) => PhysicsShape_CreateShapeBatch(body, PhysicsBuffer.FromSpan<ChainSegmentGeometry>(geometry), PhysicsShape.ShapeType.ChainSegment, definition, allocator).ToNativeArray<PhysicsShape>();

        /// <summary>
        /// Destroy the shape, destroying all <see cref="LowLevelPhysics2D.PhysicsShape.Contact"/> the shape is involved in.
        /// If the object is owned with <see cref="LowLevelPhysics2D.PhysicsShape.SetOwner(Object)"/> then you must provide the owner key it returned. Failing to do so will return a warning and the shape will not be destroyed.
        /// The lifetime of the specified owner object is not linked to this shape i.e. this shape will still be owned by the owner object, even if it is destroyed.
        /// Shapes of type Chain cannot be destroyed here, they must be destroyed by their owning chain.
        /// See <see cref="LowLevelPhysics2D.PhysicsChain"/> and <see cref="LowLevelPhysics2D.PhysicsBody.MassConfiguration"/>.
        /// </summary>
        /// <param name="updateBodyMass">Optional flag indicating if the body mass configuration should be updated. Not doing so is faster, especially when destroying multiple shapes.</param>
        /// <param name="ownerKey">Optional owner key returned when using <see cref="LowLevelPhysics2D.PhysicsShape.SetOwner(Object)"/>.</param>
        /// <returns>If the shape was destroyed or not.</returns>
        public readonly bool Destroy(bool updateBodyMass = true, int ownerKey = 0) => PhysicsShape_Destroy(this, updateBodyMass, ownerKey);

        /// <summary>
        /// Destroy a batch of shapes, destroying all <see cref="Contact"/> the shapes are involved in.
        /// Any invalid shapes will be ignored including chain segment shapes created via a <see cref="LowLevelPhysics2D.PhysicsChain"/> (the chain must be destroyed)."
        /// Owned shapes will produce a warning and will not be destroyed (See <see cref="LowLevelPhysics2D.PhysicsShape.SetOwner(Object)"/>).
        /// See <see cref="LowLevelPhysics2D.PhysicsBody.MassConfiguration"/>.
        /// </summary>
        /// <param name="shapes">The shapes to destroy.</param>
        /// <param name="updateBodyMass">Whether to update the body mass configuration. Not doing so is faster, especially when destroying multiple shapes.</param>
        public static void DestroyBatch(ReadOnlySpan<PhysicsShape> shapes, bool updateBodyMass) => PhysicsShape_DestroyBatch(shapes, updateBodyMass);

        /// <summary>
        /// Get/Set a shape definition by accessing all of its properties.
        /// This is provided as convenience only and should not be used when performance is important as all the properties defined in the definition are accessed sequentially.
        /// You should try to only use the specific properties you need rather than using this feature.
        /// 
        /// The following properties are not read/written and will be at their defaults:
        /// 
        ///- <see cref="LowLevelPhysics2D.PhysicsShapeDefinition.updateContactsOnCreate"/>
        /// </summary>
        public PhysicsShapeDefinition definition { get => PhysicsShape_ReadDefinition(this); set => PhysicsShape_WriteDefinition(this, value, false); }

        /// <summary>
        /// Check if the shape is valid.
        /// </summary>
        public readonly bool isValid => PhysicsShape_IsValid(this);

        /// <summary>
        /// Get the world the shape is attached to.
        /// </summary>
        public readonly PhysicsWorld world => PhysicsShape_GetWorld(this);

        /// <summary>
        /// The body which the shape is attached to.
        /// </summary>
        public readonly PhysicsBody body => PhysicsShape_GetBody(this);

        /// <summary>
        /// Get/Set if the shape is a trigger.
        /// Changing the state here is relatively expensive and should be avoided.
        /// See <see cref="LowLevelPhysics2D.PhysicsShapeDefinition.isTrigger"/>.
        /// </summary>
        public readonly bool isTrigger { get => PhysicsShape_GetIsTrigger(this); set => PhysicsShape_SetIsTrigger(this, value); }

        /// <summary>
        /// The type of shape. See <see cref="LowLevelPhysics2D.PhysicsShape.ShapeType"/>.
        /// </summary>
        public readonly ShapeType shapeType => PhysicsShape_GetShapeType(this);

        /// <summary>
        /// Get the shape transform.
        /// This is simply the body transform. See <see cref="LowLevelPhysics2D.PhysicsBody.transform"/>.
        /// </summary>
        public readonly PhysicsTransform transform => body.transform;

        /// <summary>
        /// Set the shape density.
        /// See <see cref="LowLevelPhysics2D.PhysicsBody.massConfiguration"/>.
        /// </summary>
        /// <param name="density">The desity to set.</param>
        /// <param name="updateBodyMass">Whether to update the body mass configuration. Not doing so is faster, especially when setting multiple shapes.</param>
        public readonly void SetDensity(float density, bool updateBodyMass) => PhysicsShape_SetDensity(this, density, updateBodyMass);

        /// <summary>
        /// Get the shape density.
        /// See <see cref="LowLevelPhysics2D.PhysicsBody.massConfiguration"/>.
        /// </summary>
        /// <returns>The density of the shape.</returns>
        public readonly float GetDensity() => PhysicsShape_GetDensity(this);

        /// <summary>
        /// The shape mass configuration. Normally this only used on a body where the total of all shapes is used.
        /// This allows the calculation of this specific shape in isolation.
        /// See <see cref="LowLevelPhysics2D.PhysicsBody.MassConfiguration"/>.
        /// </summary>
        public readonly PhysicsBody.MassConfiguration massConfiguration => PhysicsShape_GetMassConfiguration(this);

        /// <summary>
	    /// The Coulomb (dry) friction coefficient, usually in the range [0, 1].
        /// This is assigned to the current <see cref="LowLevelPhysics2D.PhysicsShape.surfaceMaterial"/>.
        /// </summary>
        public readonly float friction { get => PhysicsShape_GetFriction(this); set => PhysicsShape_SetFriction(this, value); }

        /// <summary>
	    /// The bounciness (coefficient of restitution) usually in the range [0, 1].
        /// Values higher than 1 will result in energy being added which can lead to an unstable simulation.
        /// This is assigned to the current <see cref="LowLevelPhysics2D.PhysicsShape.surfaceMaterial"/>.
        /// </summary>
        public readonly float bounciness { get => PhysicsShape_GetBounciness(this); set => PhysicsShape_SetBounciness(this, value); }

        /// <summary>
        /// Defines the method used when mixing the friction values of two shapes to form a contact.
        /// This is assigned to the current <see cref="LowLevelPhysics2D.PhysicsShape.surfaceMaterial"/>.
        /// </summary>
        public readonly SurfaceMaterial.MixingMode frictionMixing { get => PhysicsShape_GetFrictionMixing(this); set => PhysicsShape_SetFrictionMixing(this, value); }

        /// <summary>
        /// Defines the method used when mixing the friction values of two shapes to form a contact.
        /// This is assigned to the current <see cref="LowLevelPhysics2D.PhysicsShape.surfaceMaterial"/>.
        /// </summary>
        public readonly SurfaceMaterial.MixingMode bouncinessMixing { get => PhysicsShape_GetBouncinessMixing(this); set => PhysicsShape_SetBouncinessMixing(this, value); }

        /// <summary>
        /// The priority for combining the <see cref="LowLevelPhysics2D.PhysicsShape.friction"/> properties when two shapes come into contact.
        /// If the priority of one shape is higher than the other shape then the higher priority <see cref="LowLevelPhysics2D.PhysicsShape.SurfaceMaterial.frictionCombine"/> will be used.
        /// If the priority of both shapes are the same then simply the higher enumeration value of <see cref="UnityEngine.PhysicsMaterialCombine2D"/> from both shapes will be used.
        /// This is assigned to the current <see cref="LowLevelPhysics2D.PhysicsShape.surfaceMaterial"/>.
        /// </summary>
        public readonly UInt16 frictionPriority { get => PhysicsShape_GetFrictionPriority(this); set => PhysicsShape_SetFrictionPriority(this, value); }

        /// <summary>
        /// The priority for combining the <see cref="LowLevelPhysics2D.PhysicsShape.bounciness"/> properties when two shapes come into contact.
        /// If the priority of one shape is higher than the other shape then the higher priority <see cref="LowLevelPhysics2D.PhysicsShape.SurfaceMaterial.bouncinessCombine"/> will be used.
        /// If the priority of both shapes are the same then simply the higher enumeration value of <see cref="UnityEngine.PhysicsMaterialCombine2D"/> from both shapes will be used.
        /// This is assigned to the current <see cref="LowLevelPhysics2D.PhysicsShape.surfaceMaterial"/>.
        /// </summary>
        public readonly UInt16 bouncinessPriority { get => PhysicsShape_GetBouncinessPriority(this); set => PhysicsShape_SetBouncinessPriority(this, value); }

        /// <summary>
	    /// The rolling resistance usually in the range [0, 1].
        /// This is assigned to the current <see cref="LowLevelPhysics2D.PhysicsShape.surfaceMaterial"/>.
        /// </summary>
        public readonly float rollingResistance { get => PhysicsShape_GetRollingResistance(this); set => PhysicsShape_SetRollingResistance(this, value); }

        /// <summary>
	    /// The tangent (surface) speed.
        /// This is assigned to the current <see cref="LowLevelPhysics2D.PhysicsShape.surfaceMaterial"/>.
        /// </summary>
        public readonly float tangentSpeed { get => PhysicsShape_GetTangentSpeed(this); set => PhysicsShape_SetTangentSpeed(this, value); }

        /// <summary>
        /// Custom debug draw color. Any color value other than <see cref="UnityEngine.Color.clear"/> (RGBA=0) will be used to render the shape..
        /// This value is passed back when using the PhysicsWorld debug drawing. The alpha value here is always ignored.
        /// This is only used in the Unity Editor or in a Development Player.
        /// This is assigned to the current <see cref="LowLevelPhysics2D.PhysicsShape.surfaceMaterial"/>.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.Draw"/>.
        /// </summary>
        public readonly Color32 customColor { get => PhysicsShape_GetCustomColor(this); set => PhysicsShape_SetCustomColor(this, value); }

        /// <summary>
        /// The surface material for the shape comprising of many properties such as friciton, bounciness, rolling resistance etc.
        /// Setting the surface material overrides any individual settings for friciton, bounciness, rolling resistance etc.
        /// </summary>
        public readonly SurfaceMaterial surfaceMaterial { get => PhysicsShape_GetSurfaceMaterial(this); set => PhysicsShape_SetSurfaceMaterial(this, value); }

        /// <summary>
        /// The filter used when determining what contacts this shape participates in.
        /// </summary>
        public readonly ContactFilter contactFilter { get => PhysicsShape_GetContactFilter(this); set => PhysicsShape_SetContactFilter(this, value); }

        /// <summary>
        /// The mover data for the shape mover.
        /// </summary>
        public readonly MoverData moverData { get => PhysicsShape_GetMoverData(this); set => PhysicsShape_SetMoverData(this, value); }

        /// <summary>
        /// Apply a wind force to the shape body using the density of air
        /// This considers the projected area of the shape in the wind direction.
        /// This also considers the relative velocity of the shape.
        /// This only has an effect if the shape body is <see cref="UnityEngine.RigidbodyType2D.Dynamic"/>.
        /// This only has an effect of shapes of type Circle, Capsule or Polygon.
        /// </summary>
        /// <param name="force">The wind velocity in world-space.</param>
        /// <param name="drag">The drag coefficient which is a force that opposes the relative velocity.</param>
        /// <param name="lift">The lift coefficient which is a force that is perpendicular to the relative velocity.</param>
        /// <param name="wake">Whether the shape body should be woken or not.</param>
        public readonly void ApplyWind(Vector2 force, float drag, float lift, bool wake = true) => PhysicsShape_ApplyWind(this, force, drag, lift, wake);

        /// <summary>
        /// Controls whether this shape produces triggers events which can be retrieved after the simulation has completed.
        /// A trigger event is only produced if both shapes involved have their triggerEvents enabled.
        /// A trigger event will produce a <see cref="LowLevelPhysics2D.PhysicsCallbacks.ITriggerCallback"/> to the <see cref="LowLevelPhysics2D.PhysicsShape.callbackTarget"/> for both shapes involved.
        /// </summary>
        public readonly bool triggerEvents { get => PhysicsShape_GetTriggerEvents(this); set => PhysicsShape_SetTriggerEvents(this, value); }

        /// <summary>
        /// Controls whether this shape produces contact events which can be retrieved after the simulation has completed.
        /// Any contact events can be used to call the assigned <see cref="LowLevelPhysics2D.PhysicsShape.callbackTarget"/>.
        /// A contact event is produced if either shapes involved have theit contactEvents enabled.
        /// A contact event will produce a <see cref="LowLevelPhysics2D.PhysicsCallbacks.IContactCallback"/> to the <see cref="LowLevelPhysics2D.PhysicsShape.callbackTarget"/> for both shapes involved.
        /// </summary>
        public readonly bool contactEvents { get => PhysicsShape_GetContactEvents(this); set => PhysicsShape_SetContactEvents(this, value); }

        /// <summary>
        /// Controls whether this shape produces hit events which can be retrieved after the simulation has completed.
        /// </summary>
        public readonly bool hitEvents { get => PhysicsShape_GetHitEvents(this); set => PhysicsShape_SetHitEvents(this, value); }

        /// <summary>
        /// Controls whether this shape produces contact filter callbacks.
        /// A contact filter callback allows direct control over whether a contact will be created between a pair of shapes.
        /// This applies to both triggers and non-triggers but only with to Dynamic bodies
        /// These are relatively expensive so disabling them can provide a significant performance benefit.
        /// A contact filter callback will call the <see cref="LowLevelPhysics2D.PhysicsShape.callbackTarget"/> for both shapes involved if they implement <see cref="LowLevelPhysics2D.PhysicsCallbacks.IContactFilterCallback"/>.
        /// </summary>
        public readonly bool contactFilterCallbacks { get => PhysicsShape_GetContactFilterCallbacks(this); set => PhysicsShape_SetContacFiltertCallbacks(this, value); }

        /// <summary>
        /// Controls whether this shape produces pre-solve callbacks.
        /// This only applies to Dynamic bodies and is ignored for triggers.
        /// These are relatively expensive so disabling them can provide a significant performance benefit.
        /// A pre-solve callback will call the <see cref="LowLevelPhysics2D.PhysicsShape.callbackTarget"/> for both shapes involved if they implement <see cref="LowLevelPhysics2D.PhysicsCallbacks.IPreSolveCallback"/>.
        /// </summary>
        public readonly bool preSolveCallbacks { get => PhysicsShape_GetPreSolveCallbacks(this); set => PhysicsShape_SetPreSolveCallbacks(this, value); }

        /// <summary>
        /// Check if a point intersects the shape.
        /// This will only work on "closed" shapes.
        /// See<see cref="LowLevelPhysics2D.PhysicsShape.ShapeType"/>.
        /// </summary>
        /// <param name="point">The world point to check.</param>
        /// <returns>Whether an intersection was found or not.</returns>
        public readonly bool OverlapPoint(Vector2 point) => PhysicsShape_OverlapPoint(this, point);

        /// <summary>
        /// Calculate the closest point on this shape to the specified point.
        /// </summary>
        /// <param name="target">The point to check.</param>
        /// <returns>The closest point on the shape to the specified point.</returns>
        public readonly Vector2 ClosestPoint(Vector2 point) => PhysicsShape_ClosestPoint(this, point);

        /// <summary>
        /// Check if a ray intersects the shape.
        /// See <see cref="LowLevelPhysics2D.PhysicsQuery.CastResult"/>.
        /// </summary>
        /// <param name="castRayInput">The configuration of the ray to cast.</param>
        /// <returns>The intersection details, if any, that were found.</returns>
        public readonly PhysicsQuery.CastResult CastRay(PhysicsQuery.CastRayInput castRayInput) => PhysicsShape_CastRay(this, castRayInput);

        /// <summary>
        /// Calculate if a cast shape intersects the shape.
        /// Initially touching shapes are treated as a miss. You should check for overlap first if initial overlap is required.
        /// See <see cref="LowLevelPhysics2D.PhysicsQuery.CastShapeInput"/> and <see cref="LowLevelPhysics2D.PhysicsQuery.CastResult"/>.
        /// </summary>
        /// <param name="input">The cast shape input used to check for intersection.</param>
        /// <returns>The results of the intersection test.</returns>
        public readonly PhysicsQuery.CastResult CastShape(PhysicsQuery.CastShapeInput input) => PhysicsShape_CastShape(this, input);

        /// <summary>
        /// Check the intersection between this shape and another shape.
        /// </summary>
        /// <param name="otherShape">The other shape used to check intersection against.</param>
        /// <returns>The contact manifold fully detailing the intersection.</returns>
        public readonly PhysicsShape.ContactManifold Intersect(PhysicsShape otherShape) => PhysicsQuery.ShapeAndShape(this, body.transform, otherShape, otherShape.body.transform);

        /// <summary>
        /// Check the intersection between this shape and another shape.
        /// </summary>
        /// <param name="transform">The transform used to specify where this shape is positioned.</param>
        /// <param name="otherShape">The other shape used to check intersection against.</param>
        /// <param name="otherTransform">The transform used to specify where the other shape is positioned.</param>
        /// <returns>The contact manifold fully detailing the intersection.</returns>
        public readonly PhysicsShape.ContactManifold Intersect(PhysicsTransform transform, PhysicsShape otherShape, PhysicsTransform otherTransform) => PhysicsQuery.ShapeAndShape(this, transform, otherShape, otherTransform);

        /// <summary>
        /// Get/Set the Circle associated with this shape.
        /// When getting the shape geometry, the shape type must match the geometry type otherwise an warning will be produced and an invalid geometry will be returned.
        /// Setting the geometry will change the type of shape represented even if the shape type was different before.
        /// Setting the geometry will result in waking the body the shape is attached to.
        /// </summary>
        public readonly CircleGeometry circleGeometry { get => PhysicsShape_GetCircleGeometry(this); set => PhysicsShape_SetCircleGeometry(this, value); }

        /// <summary>
        /// Get/Set the Capsule associated with this shape.
        /// When getting the shape geometry, the shape type must match the geometry type otherwise an warning will be produced and an invalid geometry will be returned.
        /// Setting the geometry will change the type of shape represented even if the shape type was different before.
        /// Setting the geometry will also result in waking the body the shape is attached to.
        /// </summary>
        public readonly CapsuleGeometry capsuleGeometry { get => PhysicsShape_GetCapsuleGeometry(this); set => PhysicsShape_SetCapsuleGeometry(this, value); }

        /// <summary>
        /// Get/Set the Polygon associated with this shape.
        /// When getting the shape geometry, the shape type must match the geometry type otherwise an warning will be produced and an invalid geometry will be returned.
        /// Setting the geometry will change the type of shape represented even if the shape type was different before.
        /// Setting the geometry will also result in waking the body the shape is attached to.
        /// </summary>
        public readonly PolygonGeometry polygonGeometry { get => PhysicsShape_GetPolygonGeometry(this); set => PhysicsShape_SetPolygonGeometry(this, value); }

        /// <summary>
        /// Get/Set the Segment associated with this shape.
        /// When getting the shape geometry, the shape type must match the geometry type otherwise an warning will be produced and an invalid geometry will be returned.
        /// Setting the geometry will change the type of shape represented even if the shape type was different before.
        /// Setting the geometry will also result in waking the body the shape is attached to.
        /// </summary>
        public readonly SegmentGeometry segmentGeometry { get => PhysicsShape_GetSegmentGeometry(this); set => PhysicsShape_SetSegmentGeometry(this, value); }

        /// <summary>
        /// Get the Chain Segment Geometry associated with this shape.
        /// When getting the shape geometry, the shape type must match the geometry type otherwise an warning will be produced and an invalid geometry will be returned.
        /// </summary>
        public readonly ChainSegmentGeometry chainSegmentGeometry { get => PhysicsShape_GetChainSegmentGeometry(this); }

        /// <summary>
        /// Check if the shape is a Chain type. A Chain type is owned by a chain.
        /// See <see cref="LowLevelPhysics2D.PhysicsShape.chain"/> and <see cref="LowLevelPhysics2D.PhysicsChain"/>.
        /// </summary>
        public readonly bool isChainSegment { get => PhysicsShape_IsChainSegmentShape(this); }

        /// <summary>
        /// Get the owning chain. The type of shape must be <see cref="LowLevelPhysics2D.PhysicsShape.ShapeType.ChainSegment"/> otherwise a warning will be produced.
        /// See <see cref="LowLevelPhysics2D.PhysicsShape.isChainSegment"/> and <see cref="LowLevelPhysics2D.PhysicsChain"/>.
        /// </summary>
        public readonly PhysicsChain chain { get => PhysicsShape_GetChain(this); }

        /// <summary>
        /// Get all the touching contacts this shape is currently participating in.
        /// </summary>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The touching contacts this shape is currently participating in. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsShape.Contact> GetContacts(Allocator allocator = Allocator.Temp) => PhysicsShape_GetContacts(this, allocator).ToNativeArray<PhysicsShape.Contact>();

        /// <summary>
        /// Get all the trigger visitors for this shape. The shape must be a trigger, if not, no visitors will be returned.
        /// </summary>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The trigger visitors for this shape. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsShape> GetTriggerVisitors(Allocator allocator = Allocator.Temp) => PhysicsShape_GetTriggerVisitors(this, allocator).ToNativeArray<PhysicsShape>();

        /// <summary>
        /// Get the world AABB that bounds this shape.
        /// The bounds of the shape is inflated slightly due to speculative collision detection.
        /// The inflation is smaller on Static shape types however it is not zero due to time-of-impact collision detection.
        /// If an exact AABB is required then you can retrieve that via the shape geometry.
        /// See <see cref="LowLevelPhysics2D.CircleGeometry.CalculateAABB(PhysicsTransform)"/>, <see cref="LowLevelPhysics2D.CapsuleGeometry.CalculateAABB(PhysicsTransform)"/>, <see cref="LowLevelPhysics2D.PolygonGeometry.CalculateAABB(PhysicsTransform)"/> and <see cref="LowLevelPhysics2D.SegmentGeometry.CalculateAABB(PhysicsTransform)"/>.
        /// </summary>
        public readonly PhysicsAABB aabb => PhysicsShape_CalculateAABB(this);

        /// <summary>
        /// Get the center of the shape, in local-space.
        /// </summary>
        public readonly Vector2 localCenter => PhysicsShape_GetLocalCenter(this);

        /// <summary>
        /// Get the length of the perimeter of the shape.
        /// </summary>
        /// <returns>The length of the perimeter of the shape.</returns>
        public readonly float GetPerimeter() => PhysicsShape_GetPerimeter(this);

        /// <summary>
        /// Get the length of the perimeter of the shape projected onto the specified axis.
        /// </summary>
        /// <param name="axis">The axis to project the perimeter of the shape.</param>
        /// <returns>The length of the perimeter of the shape projected onto the specified axis.</returns>
        public readonly float GetPerimeterProjected(Vector2 axis) => PhysicsShape_GetPerimeterProjected(this, axis);

        /// <summary>
        /// Set the (optional) owner object associated with this shape and return an owner key that must be specified when destroying the shape with <see cref="LowLevelPhysics2D.PhysicsShape.Destroy(bool, int)"/>.   
        /// The physics system provides access to all objects, including the ability to destroy them so this feature can be used to stop accidental destruction of objects that are owned by other objects.
        /// You can only set the owner once, multiple attempts will produce a warning.
        /// It is also valid to not specify an owner object (NULL) to simply gain an owner key however it can be useful, if simply for debugging purposes and discovery, to know which object is the owner.
        /// </summary>
        /// <param name="owner">The object that owns this shape. This can be NULL if not required.</param>
        /// <returns>An owner key that must be passed to <see cref="LowLevelPhysics2D.PhysicsShape.Destroy(bool, int)"/> when destroying the shape.</returns>
        public readonly int SetOwner(Object owner) => PhysicsShape_SetOwner(this, owner);

        /// <summary>
        /// Get the owner object associated with this shape as specified using <see cref="LowLevelPhysics2D.PhysicsShape.SetOwner(Object)"/>.
        /// </summary>
        /// <returns>The owner object associated with this shape or NULL if no owner has been specified.</returns>
        public readonly Object GetOwner() => PhysicsShape_GetOwner(this);

        /// <summary>
        /// Get if the shape is owned.
        /// See <see cref="LowLevelPhysics2D.PhysicsShape.SetOwner(Object)"/>.
        /// </summary>
        public readonly bool isOwned => PhysicsShape_IsOwned(this);

        /// <summary>
        /// Get/Set the <see cref="System.Object"/> that callbacks for this shape will be sent to.
        /// 
        /// This includes the following events:
        /// 
        ///- A <see cref="LowLevelPhysics2D.PhysicsEvents.ContactFilterEvent"/> with call <see cref="LowLevelPhysics2D.PhysicsCallbacks.IContactFilterCallback"/>.
        ///- A <see cref="LowLevelPhysics2D.PhysicsEvents.PreSolveEvent"/> with call <see cref="LowLevelPhysics2D.PhysicsCallbacks.IPreSolveCallback"/>.
        ///- A <see cref="LowLevelPhysics2D.PhysicsEvents.TriggerBeginEvent"/> with call <see cref="LowLevelPhysics2D.PhysicsCallbacks.ITriggerCallback"/>.
        ///- A <see cref="LowLevelPhysics2D.PhysicsEvents.TriggerEndEvent"/> with call <see cref="LowLevelPhysics2D.PhysicsCallbacks.ITriggerCallback"/>.
        ///- A <see cref="LowLevelPhysics2D.PhysicsEvents.ContactBeginEvent"/> with call <see cref="LowLevelPhysics2D.PhysicsCallbacks.IContactCallback"/>.
        ///- A <see cref="LowLevelPhysics2D.PhysicsEvents.ContactEndEvent"/> with call <see cref="LowLevelPhysics2D.PhysicsCallbacks.IContactCallback"/>.
        /// </summary>
        public readonly System.Object callbackTarget { get => PhysicsShape_GetCallbackTarget(this); set => PhysicsShape_SetCallbackTarget(this, value); }

        /// <summary>
        /// Get/Set <see cref="LowLevelPhysics2D.PhysicsUserData"/> that can be used for any purpose.
        /// The physics system doesn't use this data, it is entirely for custom use.
        /// </summary>
        public readonly PhysicsUserData userData { get => PhysicsShape_GetUserData(this); set => PhysicsShape_SetUserData(this, value); }

        /// <summary>
        /// Create a shape proxy from the shape.
        /// </summary>
        /// <exception cref="System.ArgumentException">Thrown if the shape is not valid or is a Chain.</exception>
        public readonly ShapeProxy CreateShapeProxy()
        {
            if (!isValid)
                throw new ArgumentException("PhysicsShape is not valid.");

            // Extract the appropriate geometry from the shape.
            return shapeType switch
            {
                PhysicsShape.ShapeType.Circle => new ShapeProxy(circleGeometry),
                PhysicsShape.ShapeType.Capsule => new ShapeProxy(capsuleGeometry),
                PhysicsShape.ShapeType.Segment => new ShapeProxy(segmentGeometry),
                PhysicsShape.ShapeType.Polygon => new ShapeProxy(polygonGeometry),
                _ => throw new ArgumentException("PhysicsShape cannot be a Chain."),
            };
        }

        #region Debugging

        /// <summary>
        /// Draw the PhysicsShape that visually represents its current state in the world.
        /// This is only used in the Unity Editor or in a Development Player.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.DrawResults"/>, <see cref="LowLevelPhysics2D.PhysicsWorld.drawOptions"/> and <see cref="LowLevelPhysics2D.PhysicsWorld.drawResults"/>.
        /// </summary>
        public readonly void Draw() => PhysicsShape_Draw(this);

        #endregion
    }
}
