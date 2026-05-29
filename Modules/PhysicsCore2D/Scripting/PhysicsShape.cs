// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Unity.Collections;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
using static Unity.U2D.Physics.Scripting2D;

namespace Unity.U2D.Physics
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
    [MovedFrom(autoUpdateAPI: ScriptUpdateConstants.AutoUpdateAPI, sourceNamespace: ScriptUpdateConstants.SourceNamespace, sourceAssembly: ScriptUpdateConstants.SourceAssembly)]
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

        /// <undoc/>
        public static implicit operator ShapeProxy(PhysicsShape physicsShape) => physicsShape.CreateShapeProxy();

        /// <summary>
        /// The type of shape.
        /// Some shapes are "closed" meaning they have an interior which will produce contacts.
        /// Some shapes are "open" meaning they do not have an interior and will only produce contacts when their boundary is intersected.
        /// </summary>
        public enum ShapeType
        {
            /// <summary>
            /// A circle with an offset. This is a closed shape.
            /// See <see cref="CircleGeometry"/>.
            /// </summary>
            Circle,

            /// <summary>
            /// A capsule is an extruded circle. This is a closed shape.
            /// See <see cref="CapsuleGeometry"/>.
            /// </summary>
            Capsule,

            /// <summary>
            /// A line segment. This is an open shape.
            /// See <see cref="SegmentGeometry"/>.
            /// </summary>
            Segment,

            /// <summary>
            /// A convex polygon. This is a closed shape.
            /// See <see cref="PolygonGeometry"/>.
            /// </summary>
            Polygon,

            /// <summary>
            /// A Chain of line segments that are joined together with other line segments. This is an open shape.
            /// This is indirectly created and owned by a chain.
            /// See <see cref="ChainSegmentGeometry"/> and <see cref="ChainGeometry"/>.
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
            public SurfaceMaterial() { this = defaultMaterial; }

            /// <summary>
            /// Create a default surface material.
            /// </summary>
            /// <param name="useSettings">Controls whether the default come settings from the physics settings or not.</param>
            public SurfaceMaterial(bool useSettings) { this = PhysicsShape_GetDefaultSurfaceMaterial(useSettings); }

            /// <summary>
            /// Get the default surface material.
            /// </summary>
            public static SurfaceMaterial defaultMaterial => PhysicsShape_GetDefaultSurfaceMaterial(true);

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
            /// The priority for mixing the <see cref="PhysicsShape.friction"/> properties when two shapes come into contact.
            /// If the priority of one shape is higher than the other shape then the higher priority <see cref="PhysicsShape.SurfaceMaterial.frictionMixing"/> will be used.
            /// If the priority of both shapes are the same then simply the higher enumeration value of <see cref="PhysicsShape.SurfaceMaterial.MixingMode"/> from both shapes will be used.
            /// </summary>
            public UInt16 frictionPriority {  readonly get => m_FrictionPriority; set => m_FrictionPriority = value;}

            /// <summary>
            /// The priority for mixing the <see cref="PhysicsShape.bounciness"/> properties when two shapes come into contact.
            /// If the priority of one shape is higher than the other shape then the higher priority <see cref="PhysicsShape.SurfaceMaterial.bouncinessMixing"/> will be used.
            /// If the priority of both shapes are the same then simply the higher enumeration value of <see cref="PhysicsShape.SurfaceMaterial.MixingMode"/> from both shapes will be used.
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
            /// This value is passed back when using the <see cref="PhysicsWorld"/> drawing.
            /// The alpha value here is always ignored.
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
                /// Location of the contact point in world space.
                /// Subject to precision loss at large coordinates.
                /// This point lags behind when contact recycling is used.
                /// Preference should be to use anchorA and/or anchorB for game logic.
                /// This is also known as the "clip" point.
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
                /// This includes the warm starting impulse, the sub-step delta impulse, and the restitution impulse.
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
                /// This should not be confused with <see cref="PhysicsShape.ContactId"/>.
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
                readonly float m_BaseSeparation; // We don't want to expose this as it's not that useful.
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
        /// See <see cref="PhysicsBody.GetContacts"/> and <see cref="PhysicsShape.GetContacts"/>.
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
        /// The mode used for the <see cref="PhysicsShape.ContactFilter"/> when determining if two <see cref="PhysicsShape"/> can contact.
        /// See <see cref="PhysicsCoreSettings2D.contactFilterMode"/>.
        /// </summary>
        public enum ContactFilterMode
        {
            /// <summary>
            /// This mode will produce a contact if both <see cref="PhysicsShape"/> agree, effectively an AND operation.
            /// A contact will be produced if the following is true: (contactFilterA.contacts AND-MASK contactFilterB.categories) AND (contactFilterA.categories AND-MASK contactFilterB.contacts).
            /// How the <see cref="PhysicsShape.ContactFilter.groupIndex"/> is used is determined by <see cref="PhysicsShape.ContactFilterGroupMode"/>.
            /// </summary>
            Both,

            /// <summary>
            /// This mode will produce a contact if either <see cref="PhysicsShape"/> agree, effectively an OR operation.
            /// A contact will be produced if the following is true: (contactFilterA.contacts AND-MASK contactFilterB.categories ) OR (contactFilterA.categories AND-MASK contactFilterB.contacts).
            /// How the <see cref="PhysicsShape.ContactFilter.groupIndex"/> is used is determined by <see cref="PhysicsShape.ContactFilterGroupMode"/>.
            /// </summary>
            Either
        }

        /// <summary>
        /// The mode used to determine how <see cref="PhysicsShape.ContactFilter.groupIndex"/> is used.
        /// </summary>
        public enum ContactFilterGroupMode
        {
            /// <summary>
            /// In this mode, the <see cref="PhysicsShape.ContactFilter.groupIndex"/> is used to control if contacts are never created (negative) or always created (positive).
            /// A non-zero group always overrides the <see cref="PhysicsShape.ContactFilter.categories"/> and <see cref="PhysicsShape.ContactFilter.contacts"/> masks.
            /// A group of zero has no effect.
            /// 
            /// The rules for two shapes coming into contact are:
            /// 
            ///- If either shape has a group of zero then the group is ignored and the <see cref="PhysicsShape.ContactFilter.categories"/> and <see cref="PhysicsShape.ContactFilter.contacts"/> masks are used.
            ///- If both shapes have a non-zero but different group then the <see cref="PhysicsShape.ContactFilter.categories"/> and <see cref="PhysicsShape.ContactFilter.contacts"/> masks are used.
            ///- If both shapes have an identical and positive group then they will always produce a contact.
            ///- If both shapes have an identical and negative group then they will never produce a contact.
            /// </summary>
            Group,

            /// <summary>
            /// In this mode, the <see cref="PhysicsShape.ContactFilter.groupIndex"/> is used to filter if contacts are allowed to be created by the <see cref="PhysicsShape.ContactFilter.categories"/> and <see cref="PhysicsShape.ContactFilter.contacts"/> masks.
            /// 
            /// The rules for two shapes coming into contact are:
            ///
            ///- If both shapes have an identical group then the <see cref="PhysicsShape.ContactFilter.categories"/> and <see cref="PhysicsShape.ContactFilter.contacts"/> masks are used.
            ///- If both shapes have a different group then they will never produce a contact irrelevant of the <see cref="PhysicsShape.ContactFilter.categories"/> and <see cref="PhysicsShape.ContactFilter.contacts"/> mask configuration.
            ///- A group of zero is used like any other group but is also the default therefore if unchanged, the <see cref="PhysicsShape.ContactFilter.categories"/> and <see cref="PhysicsShape.ContactFilter.contacts"/> masks are used by default.
            /// </summary>
            Filtering
        }

        /// <summary>
        /// A contact filter is used to control what contacts are created when intersecting other shapes.
        /// A contact filter contains a filter with the addition of a group index allowing overrides to the filter.
        ///
        /// See <see cref="ContactFilterMode"/>.
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct ContactFilter
        {
            /// <summary>
            /// Create a contact filter.
            /// See <see cref="PhysicsShape.ContactFilterMode"/>.
            /// </summary>
            /// <param name="categories">A <see cref="PhysicsMask"/> defining the categories this object is in.</param>
            /// <param name="contacts">A <see cref="PhysicsMask"/> defining the categories this object will produce contacts with.</param>
            /// <param name="groupIndex">The group index this filter belongs to. How this is used is determined by <see cref="PhysicsShape.ContactFilterGroupMode"/>.</param>
            public ContactFilter(PhysicsMask categories, PhysicsMask contacts, Int32 groupIndex = 0)
            {
                m_Categories = categories;
                m_Contacts = contacts;
                m_GroupIndex = groupIndex;
            }

            /// <summary>
            /// The categories this object is in.
            /// Usually you would only set one bit but multiple are allowed.
            /// </summary>
            public PhysicsMask categories { readonly get => m_Categories; set => m_Categories = value; }

            /// <summary>
            /// The categories this object will produce contacts with.
            /// </summary>
            public PhysicsMask contacts { readonly get => m_Contacts; set => m_Contacts = value; }

            /// <summary>
            /// The group which the contact filter uses to determine if the categories and contact masks are used.
            /// See <see cref="PhysicsShape.ContactFilterGroupMode"/> for more information.
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

            /// <summary>
            /// Will this contact filter produce a contact with the specified contact filter.
            /// The term "contact" here means that if these filters were used on two <see cref="PhysicsShape"/>, would a contact be produced.
            /// </summary>
            /// <param name="filter">The other contact filter to compare against.</param>
            /// <returns>Whether a contact would be produced by both contact filters or not.</returns>
            public readonly bool CanContact(ContactFilter filter) => PhysicsShape_ContactFilter_CanContact(this, filter);

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
            /// Construct with the specified vertices.
            /// </summary>
            /// <param name="vertices">The vertices to set.</param>
            /// <exception cref="ArgumentOutOfRangeException">Thrown if the number of vertices provided is out of range.</exception>
            public ShapeArray(ReadOnlySpan<Vector2> vertices)
            {
                if (vertices.Length < 3 || vertices.Length > PhysicsConstants.MaxPolygonVertices)
                    throw new ArgumentOutOfRangeException(nameof(vertices), $"Vertex count is out of range, expected 3 to {PhysicsConstants.MaxPolygonVertices}.");

                // Copy the vertices.
                vertices.CopyTo(AsSpan(vertices.Length));
            }

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
            /// <exception cref="System.IndexOutOfRangeException">Thrown if the index is not in the range [0, <see cref="PhysicsConstants.MaxPolygonVertices"/> -1].</exception>
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

            /// <summary>
            /// Get the shape array as a span.
            /// </summary>
            /// <param name="count">The number of shape array elements to return.</param>
            /// <returns>The span representing the shape array.</returns>
            /// <exception cref="System.IndexOutOfRangeException">Thrown if the count is not in the range [0, <see cref="PhysicsConstants.MaxPolygonVertices"/>].</exception>
            public unsafe Span<Vector2> AsSpan(int count = PhysicsConstants.MaxPolygonVertices)
            {
                if (count > 0 && count <= PhysicsConstants.MaxPolygonVertices)
                {
                    ref Vector2 vertex0 = ref m_Vertex0;
                    fixed (Vector2* pThis = &vertex0)
                    {
                        return new Span<Vector2>(pThis, count);
                    }
                }

                throw new IndexOutOfRangeException($"{count} must be in the range [0, {PhysicsConstants.MaxPolygonVertices}]");
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
        /// The mover data assigned to a <see cref="PhysicsShape.moverData"/>.
        /// This is used when <see cref="PhysicsShape"/> are encountered when using <see cref="PhysicsWorld.CastMover(PhysicsQuery.WorldMoverInput)"/>.
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
            public float pushLimit { readonly get => m_PushLimit; set => m_PushLimit = Mathf.Max(0.0f, value); }

            /// <summary>
            /// Controls if this shape can clip the mover velocity.
            /// </summary>
            public bool clipVelocity { readonly get => m_ClipVelocity; set => m_ClipVelocity = value; }

            #region Internal

            [SerializeField][Min(0f)] float m_PushLimit;
            [SerializeField] bool m_ClipVelocity;

            #endregion
        }

        /// <summary>
        /// Collision results optionally returned from <see cref="PhysicsWorld.CastMover(PhysicsQuery.WorldMoverInput)"/> in <see cref="PhysicsQuery.WorldMoverResult"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct MoverCollision
        {
            /// <summary>
            /// The shape the mover collided with.
            /// </summary>
            public readonly PhysicsShape shape => m_Shape;

            /// <summary>
            /// The collision point on the shape.
            /// </summary>
            public readonly Vector2 point => m_Point;

            /// <summary>
            /// The collision normal at the collision point on the shape.
            /// </summary>
            public readonly Vector2 normal => m_Normal;

            #region Internal

            readonly PhysicsShape m_Shape;
            readonly Vector2 m_Point;
            readonly Vector2 m_Normal;

            #endregion
        }

        /// <summary>
        /// A proxy of a shape in a generic form suited to representing all support shape types.
        /// You can provide between 1 and <see cref="PhysicsConstants.MaxPolygonVertices"/>and a radius.
        /// 
        ///- A <see cref="CircleGeometry"/> is a single point with a non-zero radius (zero radius is allowed however and defines a point).
        ///- A <see cref="CapsuleGeometry"/> is two points with a non-zero radius.
        ///- A <see cref="PolygonGeometry"/> box is the points with and an optional radius.
        ///- A <see cref="SegmentGeometry"/> is two points with a zero radius.
        ///- A <see cref="ChainSegmentGeometry"/> is two points with a zero radius.
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
                m_ShapeType = ShapeType.Circle;
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
                m_ShapeType = ShapeType.Circle;
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
                m_ShapeType = ShapeType.Capsule;
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
                m_ShapeType = ShapeType.Polygon;
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
                m_ShapeType = ShapeType.Segment;
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
                m_ShapeType = ShapeType.ChainSegment;
            }

            /// <summary>
            /// Get a <see cref="CircleGeometry"/> from the shape proxy.
            /// The <see cref="PhysicsShape.ShapeProxy.count"/> must be 1.
            /// </summary>
            /// <exception cref="InvalidOperationException">Thrown if the <see cref="PhysicsShape.ShapeProxy.count"/> is not 1.</exception>
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
            /// Get a <see cref="CapsuleGeometry"/> from the shape proxy.
            /// The <see cref="PhysicsShape.ShapeProxy.count"/> must be 2.
            /// </summary>
            /// <exception cref="InvalidOperationException">Thrown if the <see cref="PhysicsShape.ShapeProxy.count"/> is not 2.</exception>
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
            /// Get a <see cref="PolygonGeometry"/> from the shape proxy.
            /// </summary>
            /// <exception cref="System.InvalidOperationException">Thrown if the <see cref="PhysicsShape.ShapeProxy.count"/> is not in the range [3, <see cref="PhysicsConstants.MaxPolygonVertices"/>].</exception>
            public unsafe PolygonGeometry polygonGeometry
            {
                get
                {
                    if (m_Count >= 3 && m_Count <= PhysicsConstants.MaxPolygonVertices)
                    {
                        fixed (Vector2* pVertices = &m_Vertices.m_Vertex0)
                        {
                            return PolygonGeometry.Create(AsSpan(), m_Radius);
                        }
                    }

                    throw new InvalidOperationException($"Expected a vertex count in the range [3 to {PhysicsConstants.MaxPolygonVertices}].");
                }
            }

            /// <summary>
            /// Get a <see cref="SegmentGeometry"/> from the shape proxy.
            /// The <see cref="PhysicsShape.ShapeProxy.count"/> must be 2.
            /// </summary>
            /// <exception cref="InvalidOperationException">Thrown if the <see cref="PhysicsShape.ShapeProxy.count"/> is not 2.</exception>
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

            /// <summary>
            /// The shape type represented.
            /// </summary>
            public ShapeType shapeType { readonly get => m_ShapeType; set => m_ShapeType = value; }

            /// <summary>
            /// Check if the shape proxy is valid.
            /// </summary>
            public bool isValid => m_ShapeType switch
            {
                ShapeType.Circle => m_Count == 1,
                ShapeType.Capsule => m_Count == 2 && radius > 0.0f,
                ShapeType.Polygon => m_Count > 3 && count <= PhysicsConstants.MaxPolygonVertices,
                ShapeType.Segment => m_Count == 2,
                ShapeType.ChainSegment => m_Count == 2,
                _ => throw new NotImplementedException()
            };

            /// <summary>
            /// Get the convex-hull vertices as a span.
            /// </summary>
            /// <returns>The span representing the vertices in the convex-hull.</returns>
            public unsafe Span<Vector2> AsSpan() => vertices.AsSpan(m_Count);

            #region Internal

            [SerializeField] ShapeArray m_Vertices;
            [SerializeField] [Min(1)] int m_Count;
            [SerializeField] [Min(0.0f)] float m_Radius;
            [SerializeField] ShapeType m_ShapeType;

            #endregion
        }

        /// <summary>
        /// Create a Circle shape, using its default definition, attached to the specified body.
        /// See <see cref="PhysicsBody.CreateShape(CircleGeometry, PhysicsShapeDefinition)"/>.
        /// </summary>
        /// <param name="body">The body to attach the shape to.</param>
        /// <param name="geometry">The shape geometry to use.</param>
        /// <returns>The created shape.</returns>
        public static PhysicsShape CreateShape(PhysicsBody body, CircleGeometry geometry) => CreateShape(body, geometry, PhysicsShapeDefinition.defaultDefinition);

        /// <summary>
        /// Create a Circle shape attached to the specified body.
        /// See <see cref="PhysicsBody.CreateShape(CircleGeometry, PhysicsShapeDefinition)"/>.
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
        /// See <see cref="PhysicsBody.CreateShape(PolygonGeometry, PhysicsShapeDefinition)"/>.
        /// </summary>
        /// <param name="body">The body to attach the shape to.</param>
        /// <param name="geometry">The shape geometry to use.</param>
        /// <returns>The created shape.</returns>
        public static PhysicsShape CreateShape(PhysicsBody body, PolygonGeometry geometry) => CreateShape(body, geometry, PhysicsShapeDefinition.defaultDefinition);

        /// <summary>
        /// Create a Polygon shape attached to the specified body.
        /// See <see cref="PhysicsBody.CreateShape(PolygonGeometry, PhysicsShapeDefinition)"/>.
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
        /// See <see cref="PhysicsBody.CreateShape(CapsuleGeometry, PhysicsShapeDefinition)"/>.
        /// </summary>
        /// <param name="body">The body to attach the shape to.</param>
        /// <param name="geometry">The shape geometry to use.</param>
        /// <returns>The created shape.</returns>
        public static PhysicsShape CreateShape(PhysicsBody body, CapsuleGeometry geometry) => CreateShape(body, geometry, PhysicsShapeDefinition.defaultDefinition);

        /// <summary>
        /// Create a Capsule shape attached to the specified body.
        /// See <see cref="PhysicsBody.CreateShape(CapsuleGeometry, PhysicsShapeDefinition)"/>.
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
        /// See <see cref="PhysicsBody.CreateShape(SegmentGeometry, PhysicsShapeDefinition)"/>.
        /// </summary>
        /// <param name="body">The body to attach the shape to.</param>
        /// <param name="geometry">The shape geometry to use.</param>
        /// <returns>The created shape.</returns>
        public static PhysicsShape CreateShape(PhysicsBody body, SegmentGeometry geometry) => CreateShape(body, geometry, PhysicsShapeDefinition.defaultDefinition);

        /// <summary>
        /// Create a Segment shape attached to the specified body.
        /// See <see cref="PhysicsBody.CreateShape(SegmentGeometry, PhysicsShapeDefinition)"/>.
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
        /// See <see cref="PhysicsBody.CreateShape(ChainSegmentGeometry, PhysicsShapeDefinition)"/>.
        /// </summary>
        /// <param name="body">The body to attach the shape to.</param>
        /// <param name="geometry">The shape geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <returns>The created shape.</returns>
        public static PhysicsShape CreateShape(PhysicsBody body, ChainSegmentGeometry geometry, PhysicsShapeDefinition definition) => PhysicsShape_CreateChainSegmenShapet(body, geometry, definition);

        /// <summary>
        /// Create a Chain Segment shape, using its default definition, attached to the specified body.
        /// See <see cref="PhysicsBody.CreateShape(ChainSegmentGeometry, PhysicsShapeDefinition)"/>.
        /// </summary>
        /// <param name="body">The body to attach the shape to.</param>
        /// <param name="geometry">The shape geometry to use.</param>
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
        /// Destroy the shape, destroying all <see cref="PhysicsShape.Contact"/> the shape is involved in.
        /// If the object is owned with <see cref="PhysicsShape.SetOwner(UnityEngine.Object)"/> then you must provide the owner key it returned. Failing to do so will return a warning and the shape will not be destroyed.
        /// The lifetime of the specified owner object is not linked to this shape i.e. this shape will still be owned by the owner object, even if it is destroyed.
        /// Shapes of type Chain cannot be destroyed here, they must be destroyed by their owning chain.
        /// See <see cref="PhysicsChain"/> and <see cref="PhysicsBody.MassConfiguration"/>.
        /// </summary>
        /// <param name="updateBodyMass">Optional flag indicating if the body mass configuration should be updated. Not doing so is faster, especially when destroying multiple shapes.</param>
        /// <param name="ownerKey">Optional owner key returned when using <see cref="PhysicsShape.SetOwner(UnityEngine.Object)"/>.</param>
        /// <returns>If the shape was destroyed or not.</returns>
        public readonly bool Destroy(bool updateBodyMass = true, int ownerKey = 0) => PhysicsShape_Destroy(this, updateBodyMass, ownerKey);

        /// <summary>
        /// Destroy a batch of shapes, destroying all <see cref="Contact"/> the shapes are involved in.
        /// Any invalid shapes will be ignored including chain segment shapes created via a <see cref="PhysicsChain"/> (the chain must be destroyed)."
        /// Owned shapes will produce a warning and will not be destroyed (See <see cref="PhysicsShape.SetOwner(UnityEngine.Object)"/>).
        /// See <see cref="PhysicsBody.MassConfiguration"/>.
        /// </summary>
        /// <param name="shapes">The shapes to destroy.</param>
        /// <param name="updateBodyMass">Whether to update the body mass configuration. Not doing so is faster, especially when destroying multiple shapes.</param>
        public static void DestroyBatch(ReadOnlySpan<PhysicsShape> shapes, bool updateBodyMass) => PhysicsShape_DestroyBatch(shapes, updateBodyMass);

        /// <summary>
        /// Get/Set a shape definition by accessing all of its current properties.
        /// This is provided as convenience only and should not be used when performance is important as all the properties defined in the definition are accessed sequentially.
        /// You should try to only use the specific properties you need rather than using this feature.
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
        /// See <see cref="PhysicsShapeDefinition.isTrigger"/>.
        /// </summary>
        public readonly bool isTrigger { get => PhysicsShape_GetIsTrigger(this); set => PhysicsShape_SetIsTrigger(this, value); }

        /// <summary>
        /// The type of shape. See <see cref="PhysicsShape.ShapeType"/>.
        /// </summary>
        public readonly ShapeType shapeType => PhysicsShape_GetShapeType(this);

        /// <summary>
        /// Get the shape transform.
        /// This is simply the body transform. See <see cref="PhysicsBody.transform"/>.
        /// </summary>
        public readonly PhysicsTransform transform => body.transform;

        /// <summary>
        /// Set the shape density.
        /// See <see cref="PhysicsBody.massConfiguration"/>.
        /// </summary>
        /// <param name="density">The density to set.</param>
        /// <param name="updateBodyMass">Whether to update the body mass configuration. Not doing so is faster, especially when setting multiple shapes.</param>
        public readonly void SetDensity(float density, bool updateBodyMass) => PhysicsShape_SetDensity(this, density, updateBodyMass);

        /// <summary>
        /// Get the shape density.
        /// See <see cref="PhysicsBody.massConfiguration"/>.
        /// </summary>
        /// <returns>The density of the shape.</returns>
        public readonly float GetDensity() => PhysicsShape_GetDensity(this);

        /// <summary>
        /// The shape mass configuration. Normally this only used on a body where the total of all shapes is used.
        /// This allows the calculation of this specific shape in isolation.
        /// See <see cref="PhysicsBody.MassConfiguration"/>.
        /// </summary>
        public readonly PhysicsBody.MassConfiguration massConfiguration => PhysicsShape_GetMassConfiguration(this);

        /// <summary>
	    /// The Coulomb (dry) friction coefficient, usually in the range [0, 1].
        /// This is assigned to the current <see cref="PhysicsShape.surfaceMaterial"/>.
        /// </summary>
        public readonly float friction { get => PhysicsShape_GetFriction(this); set => PhysicsShape_SetFriction(this, value); }

        /// <summary>
	    /// The bounciness (coefficient of restitution) usually in the range [0, 1].
        /// Values higher than 1 will result in energy being added which can lead to an unstable simulation.
        /// This is assigned to the current <see cref="PhysicsShape.surfaceMaterial"/>.
        /// </summary>
        public readonly float bounciness { get => PhysicsShape_GetBounciness(this); set => PhysicsShape_SetBounciness(this, value); }

        /// <summary>
        /// Defines the method used when mixing the friction values of two shapes to form a contact.
        /// This is assigned to the current <see cref="PhysicsShape.surfaceMaterial"/>.
        /// </summary>
        public readonly SurfaceMaterial.MixingMode frictionMixing { get => PhysicsShape_GetFrictionMixing(this); set => PhysicsShape_SetFrictionMixing(this, value); }

        /// <summary>
        /// Defines the method used when mixing the friction values of two shapes to form a contact.
        /// This is assigned to the current <see cref="PhysicsShape.surfaceMaterial"/>.
        /// </summary>
        public readonly SurfaceMaterial.MixingMode bouncinessMixing { get => PhysicsShape_GetBouncinessMixing(this); set => PhysicsShape_SetBouncinessMixing(this, value); }

        /// <summary>
        /// The priority for combining the <see cref="PhysicsShape.friction"/> properties when two shapes come into contact.
        /// If the priority of one shape is higher than the other shape then the higher priority <see cref="PhysicsShape.SurfaceMaterial.frictionCombine"/> will be used.
        /// If the priority of both shapes are the same then simply the higher enumeration value of <see cref="UnityEngine.PhysicsMaterialCombine2D"/> from both shapes will be used.
        /// This is assigned to the current <see cref="PhysicsShape.surfaceMaterial"/>.
        /// </summary>
        public readonly UInt16 frictionPriority { get => PhysicsShape_GetFrictionPriority(this); set => PhysicsShape_SetFrictionPriority(this, value); }

        /// <summary>
        /// The priority for combining the <see cref="PhysicsShape.bounciness"/> properties when two shapes come into contact.
        /// If the priority of one shape is higher than the other shape then the higher priority <see cref="PhysicsShape.SurfaceMaterial.bouncinessCombine"/> will be used.
        /// If the priority of both shapes are the same then simply the higher enumeration value of <see cref="UnityEngine.PhysicsMaterialCombine2D"/> from both shapes will be used.
        /// This is assigned to the current <see cref="PhysicsShape.surfaceMaterial"/>.
        /// </summary>
        public readonly UInt16 bouncinessPriority { get => PhysicsShape_GetBouncinessPriority(this); set => PhysicsShape_SetBouncinessPriority(this, value); }

        /// <summary>
	    /// The rolling resistance usually in the range [0, 1].
        /// This is assigned to the current <see cref="PhysicsShape.surfaceMaterial"/>.
        /// </summary>
        public readonly float rollingResistance { get => PhysicsShape_GetRollingResistance(this); set => PhysicsShape_SetRollingResistance(this, value); }

        /// <summary>
	    /// The tangent (surface) speed.
        /// This is assigned to the current <see cref="PhysicsShape.surfaceMaterial"/>.
        /// </summary>
        public readonly float tangentSpeed { get => PhysicsShape_GetTangentSpeed(this); set => PhysicsShape_SetTangentSpeed(this, value); }

        /// <summary>
        /// Custom debug draw color. Any color value other than <see cref="UnityEngine.Color.clear"/> (RGBA=0) will be used to render the shape..
        /// This value is passed back when using the <see cref="PhysicsWorld"/> drawing.
        /// The alpha value here is always ignored.
        /// This is assigned to the current <see cref="PhysicsShape.surfaceMaterial"/>.
        /// </summary>
        public readonly Color32 customColor { get => PhysicsShape_GetCustomColor(this); set => PhysicsShape_SetCustomColor(this, value); }

        /// <summary>
        /// The surface material for the shape comprising of many properties such as friction, bounciness, rolling resistance etc.
        /// Setting the surface material overrides any individual settings for friction, bounciness, rolling resistance etc.
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
        /// A trigger event will produce a <see cref="PhysicsCallbacks.ITriggerCallback"/> to the <see cref="PhysicsShape.callbackTarget"/> for both shapes involved.
        /// </summary>
        public readonly bool triggerEvents { get => PhysicsShape_GetTriggerEvents(this); set => PhysicsShape_SetTriggerEvents(this, value); }

        /// <summary>
        /// Controls whether this shape produces contact events which can be retrieved after the simulation has completed.
        /// Any contact events can be used to call the assigned <see cref="PhysicsShape.callbackTarget"/>.
        /// A contact event is produced if either shapes involved have contactEvents enabled.
        /// A contact event will produce a <see cref="PhysicsCallbacks.IContactCallback"/> to the <see cref="PhysicsShape.callbackTarget"/> for both shapes involved.
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
        /// A contact filter callback will call the <see cref="PhysicsShape.callbackTarget"/> for both shapes involved if they implement <see cref="PhysicsCallbacks.IContactFilterCallback"/>.
        /// </summary>
        public readonly bool contactFilterCallbacks { get => PhysicsShape_GetContactFilterCallbacks(this); set => PhysicsShape_SetContacFiltertCallbacks(this, value); }

        /// <summary>
        /// Controls whether this shape produces pre-solve callbacks.
        /// This only applies to Dynamic bodies and is ignored for triggers.
        /// These are relatively expensive so disabling them can provide a significant performance benefit.
        /// A pre-solve callback will call the <see cref="PhysicsShape.callbackTarget"/> for both shapes involved if they implement <see cref="PhysicsCallbacks.IPreSolveCallback"/>.
        /// </summary>
        public readonly bool preSolveCallbacks { get => PhysicsShape_GetPreSolveCallbacks(this); set => PhysicsShape_SetPreSolveCallbacks(this, value); }

        /// <summary>
        /// Normally shapes on Static bodies don't create contacts when they are added to the world.
        /// This overrides that behavior and causes contact creation.
        /// This significantly slows down Static body creation which can be important when there are many Static shapes.
        /// This is implicitly always true for Triggers, Dynamic bodies and Kinematic bodies.
        ///
        /// See <see cref="PhysicsShapeDefinition.startStaticContacts"/>.
        /// </summary>
        public readonly bool startStaticContacts => PhysicsShape_GetStartStaticContacts(this);

        /// <summary>
        /// Should the body update its mass properties when this shape is created.
        /// Disabling this improves performance when multiple shapes are being added to the same body.
        /// The mass of a body can then be explicitly updated by calling <see cref="PhysicsBody.ApplyMassFromShapes"/>
        ///
        /// See <see cref="PhysicsShapeDefinition.startMassUpdate"/>.
        /// </summary>
        public readonly bool startMassUpdate => PhysicsShape_GetStartMassUpdate(this);

        /// <summary>
        /// Check if a point intersects the shape.
        /// This will only work on "closed" shapes.
        /// See<see cref="PhysicsShape.ShapeType"/>.
        /// </summary>
        /// <param name="point">The world point to check.</param>
        /// <returns>Whether an intersection was found or not.</returns>
        public readonly bool OverlapPoint(Vector2 point) => PhysicsShape_OverlapPoint(this, point);

        /// <summary>
        /// Calculate the closest point on this shape to the specified point.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>The closest point on the shape to the specified point.</returns>
        public readonly Vector2 ClosestPoint(Vector2 point) => PhysicsShape_ClosestPoint(this, point);

        /// <summary>
        /// Check if a ray intersects the shape.
        /// See <see cref="PhysicsQuery.CastResult"/>.
        /// </summary>
        /// <param name="castRayInput">The configuration of the ray to cast.</param>
        /// <returns>The intersection details, if any, that were found.</returns>
        public readonly PhysicsQuery.CastResult CastRay(PhysicsQuery.CastRayInput castRayInput) => PhysicsShape_CastRay(this, castRayInput);

        /// <summary>
        /// Calculate if a cast shape intersects the shape.
        /// Initially touching shapes are treated as a miss. You should check for overlap first if initial overlap is required.
        /// See <see cref="PhysicsQuery.CastShapeInput"/> and <see cref="PhysicsQuery.CastResult"/>.
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
        /// Get the minimum distance between this shape and the specified shape.
        /// </summary>
        /// <param name="otherShape">The other shape to check the distance of.</param>
        /// <param name="useRadii">Whether to use the radii of both shapes or not.</param>
        /// <returns>The distance result.</returns>
        public unsafe readonly PhysicsQuery.DistanceResult Distance(PhysicsShape otherShape, bool useRadii = true) => Distance(otherShape, otherShape.body.transform, useRadii);

        /// <summary>
        /// Get the minimum distance between this shape and the specified shape.
        /// </summary>
        /// <param name="otherShape">The other shape to check the distance of.</param>
        /// <param name="otherTransform">The transform used to specify where the other shape is positioned.</param>
        /// <param name="useRadii">Whether to use the radii of both shapes or not.</param>
        /// <returns>The distance result.</returns>
        public readonly PhysicsQuery.DistanceResult Distance(PhysicsShape otherShape, PhysicsTransform otherTransform, bool useRadii = true)
        {
            return PhysicsQuery.ShapeDistance(
                new PhysicsQuery.DistanceInput
                {
                    shapeProxyA = this,
                    shapeProxyB = otherShape,
                    transformA = body.transform,
                    transformB = otherTransform,
                    useRadii = useRadii
                });
        }

        /// <summary>
        /// Get the minimum distance between this shape and the specified shape(s) span.
        /// </summary>
        /// <param name="otherShapes">A read-only span of the other shape to check the distance of.</param>
        /// <param name="useRadii">Whether to use the radii of both shapes or not.</param>
        /// <returns>The distance result.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the current shape is invalid.</exception>
        /// <exception cref="ArgumentException">Thrown is any of the specified shapes are invalid.</exception>
        public readonly PhysicsQuery.DistanceResult Distance(ReadOnlySpan<PhysicsShape> otherShapes, bool useRadii = true)
        {
            if (!isValid)
                throw new InvalidOperationException("The current shape is invalid.");

            var bestDistance = float.MaxValue;
            PhysicsQuery.DistanceResult bestResult = default;

            // Fetch shape A details.
            var shapeProxyA = CreateShapeProxy();
            var transformA = body.transform;

            // Iterate all the shapes.
            foreach (var otherShape in otherShapes)
            {
                if (!otherShape.isValid)
                    throw new ArgumentException(nameof(otherShapes));

                // Skip if the same shape.
                if (otherShape == this)
                    continue;

                // Query the distance.
                var result = PhysicsQuery.ShapeDistance(
                    new PhysicsQuery.DistanceInput
                    {
                        shapeProxyA = shapeProxyA,
                        transformA = transformA,
                        shapeProxyB = otherShape.CreateShapeProxy(),
                        transformB = otherShape.body.transform,
                        useRadii = useRadii
                    });

                // Ignore if further away.
                if (result.distance >= bestDistance)
                    continue;

                // Found a better distance.
                bestDistance = result.distance;
                bestResult = result;
            }

            return bestResult;
        }

        /// <summary>
        /// Get the minimum distance between this shape and all the shapes attached to the specified body.
        /// </summary>
        /// <param name="physicsBody">The body whose attached shape(s) will be used to check the distance of.</param>
        /// <param name="useRadii">Whether to use the radii of all shapes or not.</param>
        /// <returns></returns>
        public readonly PhysicsQuery.DistanceResult Distance(PhysicsBody physicsBody, bool useRadii = true)
        {
            // Get all the body shapes.
            using var otherShapes = physicsBody.GetShapes();

            // Find the minimum distance to them all.
            return Distance(otherShapes, useRadii);
        }

        /// <summary>
        /// Get/Set the Circle associated with this shape.
        /// When getting the shape geometry, the shape type must match the geometry type otherwise a warning will be produced and invalid geometry will be returned.
        /// Setting the geometry will change the type of shape represented even if the shape type was different before.
        /// Setting the geometry will result in waking the body the shape is attached to.
        /// </summary>
        public readonly CircleGeometry circleGeometry { get => PhysicsShape_GetCircleGeometry(this); set => PhysicsShape_SetCircleGeometry(this, value); }

        /// <summary>
        /// Get/Set the Capsule associated with this shape.
        /// When getting the shape geometry, the shape type must match the geometry type otherwise a warning will be produced and invalid geometry will be returned.
        /// Setting the geometry will change the type of shape represented even if the shape type was different before.
        /// Setting the geometry will also result in waking the body the shape is attached to.
        /// </summary>
        public readonly CapsuleGeometry capsuleGeometry { get => PhysicsShape_GetCapsuleGeometry(this); set => PhysicsShape_SetCapsuleGeometry(this, value); }

        /// <summary>
        /// Get/Set the Polygon associated with this shape.
        /// When getting the shape geometry, the shape type must match the geometry type otherwise a warning will be produced and invalid geometry will be returned.
        /// Setting the geometry will change the type of shape represented even if the shape type was different before.
        /// Setting the geometry will also result in waking the body the shape is attached to.
        /// </summary>
        public readonly PolygonGeometry polygonGeometry { get => PhysicsShape_GetPolygonGeometry(this); set => PhysicsShape_SetPolygonGeometry(this, value); }

        /// <summary>
        /// Get/Set the Segment associated with this shape.
        /// When getting the shape geometry, the shape type must match the geometry type otherwise a warning will be produced and invalid geometry will be returned.
        /// Setting the geometry will change the type of shape represented even if the shape type was different before.
        /// Setting the geometry will also result in waking the body the shape is attached to.
        /// </summary>
        public readonly SegmentGeometry segmentGeometry { get => PhysicsShape_GetSegmentGeometry(this); set => PhysicsShape_SetSegmentGeometry(this, value); }

        /// <summary>
        /// Get/Set the Chain Segment associated with this shape.
        /// When getting the shape geometry, the shape type must match the geometry type otherwise a warning will be produced and invalid geometry will be returned.
        /// Setting the geometry will change the type of shape represented even if the shape type was different before.
        /// Setting the geometry will also result in waking the body the shape is attached to.
        /// </summary>
        public readonly ChainSegmentGeometry chainSegmentGeometry { get => PhysicsShape_GetChainSegmentGeometry(this); set => PhysicsShape_SetChainSegmentGeometry(this, value); }

        /// <summary>
        /// Check if the shape is a Chain type. A Chain type is owned by a chain.
        /// See <see cref="PhysicsShape.chain"/> and <see cref="PhysicsChain"/>.
        /// </summary>
        public readonly bool isChainSegment { get => PhysicsShape_IsChainSegmentShape(this); }

        /// <summary>
        /// Get the owning chain. The type of shape must be <see cref="PhysicsShape.ShapeType.ChainSegment"/> otherwise a warning will be produced.
        /// See <see cref="PhysicsShape.isChainSegment"/> and <see cref="PhysicsChain"/>.
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
        /// See <see cref="CircleGeometry.CalculateAABB(PhysicsTransform)"/>, <see cref="CapsuleGeometry.CalculateAABB(PhysicsTransform)"/>, <see cref="PolygonGeometry.CalculateAABB(PhysicsTransform)"/> and <see cref="SegmentGeometry.CalculateAABB(PhysicsTransform)"/>.
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
        /// Set the owner object using the specified owner key.
        /// You can only set the owner once, multiple attempts will produce a warning.
        /// This call does not bind the lifetime of the specified owner object, it is simply a reference.
        /// Whilst it is valid to not specify an owner object (NULL), it is recommended for debugging purposes.
        /// </summary>
        /// <param name="shapes">The shapes to set ownership for.</param>
        /// <param name="owner">The object that owns this key. Whilst it is valid to not specify an owner object (NULL), it is recommended for debugging purposes.</param>
        /// <param name="ownerKey">The owner key to be used. The value must be non-zero. You can use <see cref="PhysicsWorld.CreateOwnerKey(UnityEngine.Object)"/> for this value although any non-zero integer will work.</param>
        public static void SetOwner(ReadOnlySpan<PhysicsShape> shapes, UnityEngine.Object owner, int ownerKey) => PhysicsShape_SetOwner(shapes, owner, ownerKey);

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
            var shape = this;
            SetOwner(new ReadOnlySpan<PhysicsShape>(&shape, 1), owner, ownerKey);
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
        /// Get the owner object associated with this shape as specified using <see cref="PhysicsShape.SetOwner(UnityEngine.Object)"/>.
        /// </summary>
        /// <returns>The owner object associated with this shape or NULL if no owner has been specified.</returns>
        public readonly UnityEngine.Object GetOwner() => PhysicsShape_GetOwner(this);

        /// <summary>
        /// Get if the shape is owned.
        /// See <see cref="PhysicsShape.SetOwner(UnityEngine.Object)"/>.
        /// </summary>
        public readonly bool isOwned => PhysicsShape_IsOwned(this);

        /// <summary>
        /// Get/Set the <see cref="System.Object"/> that callbacks for this shape will be sent to.
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
        public readonly System.Object callbackTarget { get => PhysicsShape_GetCallbackTarget(this); set => PhysicsShape_SetCallbackTarget(this, value); }

        /// <summary>
        /// Get/Set <see cref="PhysicsUserData"/> that can be used for any purpose.
        /// The physics system doesn't use this data, it is entirely for custom use.
        /// </summary>
        public readonly PhysicsUserData userData { get => PhysicsShape_GetUserData(this); set => PhysicsShape_SetUserData(this, value); }

        /// <summary>
        /// Get <see cref="PhysicsUserData"/> that can be used for any purpose, typically by the owner only.
        /// </summary>
        public readonly PhysicsUserData ownerUserData { get => PhysicsShape_GetOwnerUserData(this); }

        /// <summary>
        /// Set <see cref="PhysicsUserData"/> that can be used for any purpose, typically by the owner only.
        /// </summary>
        /// <param name="physicsUserData">The user data to set.</param>
        /// <param name="ownerKey">Optional owner key returned when using <see cref="PhysicsShape.SetOwner(UnityEngine.Object)"/>.</param>
        public readonly void SetOwnerUserData(PhysicsUserData physicsUserData, int ownerKey = 0) => PhysicsShape_SetOwnerUserData(this, physicsUserData, ownerKey);

        /// <summary>
        /// Create a shape proxy from the shape.
        /// </summary>
        /// <param name="useWorldSpace">Whether to create the shape proxy in world-space or not. World-space will transform by the body origin the shape is attached to.</param>
        /// <exception cref="System.ArgumentException">Thrown if the shape is not valid.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if the shape type is unknown.</exception>
        public readonly ShapeProxy CreateShapeProxy(bool useWorldSpace = false)
        {
            if (!isValid)
                throw new ArgumentException("PhysicsShape is not valid.");

            // Extract the appropriate geometry from the shape.
            return shapeType switch
            {
                ShapeType.Circle => new ShapeProxy(useWorldSpace ? circleGeometry.Transform(body.transform) : circleGeometry),
                ShapeType.Capsule => new ShapeProxy(useWorldSpace ? capsuleGeometry.Transform(body.transform) : capsuleGeometry),
                ShapeType.Polygon => new ShapeProxy(useWorldSpace ? polygonGeometry.Transform(body.transform) : polygonGeometry),
                ShapeType.Segment => new ShapeProxy(useWorldSpace ? segmentGeometry.Transform(body.transform) : segmentGeometry),
                ShapeType.ChainSegment => new ShapeProxy(chainSegmentGeometry),
                _ => throw new ArgumentException("PhysicsShape type is unknown."),
            };
        }

        #region Debugging

        /// <summary>
        /// Controls whether this shape is automatically drawn when the world is drawn.
        /// </summary>
        public readonly bool worldDrawing { get => PhysicsShape_GetWorldDrawing(this); set => PhysicsShape_SetWorldDrawing(this, value); }

        /// <summary>
        /// Draw the PhysicsShape that visually represents its current state in the world.
        /// </summary>
        public readonly void Draw() => PhysicsShape_Draw(this);

        #endregion
    }
}
