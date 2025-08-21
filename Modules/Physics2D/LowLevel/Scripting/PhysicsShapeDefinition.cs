// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

using static UnityEngine.LowLevelPhysics2D.PhysicsLowLevelScripting2D;

namespace UnityEngine.LowLevelPhysics2D
{
    /// <summary>
    /// A <see cref="LowLevelPhysics2D.PhysicsShape"/> definition used to specify important initial properties.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct PhysicsShapeDefinition
    {
        /// <summary>
        /// Create a default <see cref="LowLevelPhysics2D.PhysicsShape"/> definition.
        /// </summary>
        public PhysicsShapeDefinition() { this = defaultDefinition; }

        /// <summary>
        /// Create a default <see cref="LowLevelPhysics2D.PhysicsShape"/> definition.
        /// </summary>
        /// <param name="useSettings">Controls whether the default come settings from the physics settings or not.</param>
        public PhysicsShapeDefinition(bool useSettings) { this = PhysicsShape_GetDefaultDefinition(useSettings); }

        /// <summary>
        /// Get a default <see cref="LowLevelPhysics2D.PhysicsShape"/> definition.
        /// </summary>
        public static PhysicsShapeDefinition defaultDefinition => PhysicsShape_GetDefaultDefinition(true);

        /// <summary>
        /// The density, usually in kg/m^2, defaults to 1.
        /// This is not part of the surface material because this is for the interior of the shape, which may have other considerations, such as being hollow.
        /// </summary>
        public float density { readonly get => m_Density; set => m_Density = Mathf.Max(0f, value); }

        /// <summary>
        /// Enable/Disable being a trigger shape. A trigger shape generates overlap events but never generates a collision response.
        /// Triggers do not collide with other triggers and do not have continuous collision, instead, use a ray or shape cast for those scenarios.
        /// Triggers still contribute to the body mass if they have non-zero density.
        /// The default is false.
        /// </summary>
        public bool isTrigger { readonly get => m_IsTrigger; set => m_IsTrigger = value; }

        /// <summary>
        /// Controls whether this shape produces trigger events which can be retrieved after the simulation has completed.
        /// This applies to triggers and non-triggers alike.
        /// </summary>
	    public bool triggerEvents { readonly get => m_TriggerEvents; set => m_TriggerEvents = value; }

        /// <summary>
        /// Controls whether this shape produces contact events which can be retrieved after the simulation has completed.
        /// This only applies to kinematic and dynamic bodies.
        /// Changing this at run-time may lead to lost begin/end events.
        /// </summary>
        public bool contactEvents { readonly get => m_ContactEvents; set => m_ContactEvents = value; }

        /// <summary>
        /// Controls whether this shape produces hit events which can be retrieved after the simulation has completed.
        /// This only applies to kinematic and dynamic bodies.
        /// This is ignored for triggers.
        /// </summary>
        public bool hitEvents { readonly get => m_HitEvents; set => m_HitEvents = value; }

        /// <summary>
        /// Controls whether this shape produces contact filter callbacks.
        /// A contact filter callback allows direct control over whether a contact will be created between a pair of shapes.
        /// This applies to both triggers and non-triggers but only with to Dynamic bodies
        /// These are relatively expensive so disabling them can provide a significant performance benefit.
        /// A contact filter callback will call the <see cref="LowLevelPhysics2D.PhysicsShape.callbackTarget"/> for both shapes involved if they implement <see cref="LowLevelPhysics2D.PhysicsCallbacks.IContactFilterCallback"/>.
        /// </summary>
        public bool contactFilterCallbacks { readonly get => m_ContactFilterCallbacks; set => m_ContactFilterCallbacks = value; }

        /// <summary>
        /// Controls whether this shape produces pre-solve callbacks.
        /// This only applies to Dynamic bodies and is ignored for triggers.
        /// These are relatively expensive so disabling them can provide a significant performance benefit.
        /// A pre-solve callback will call the <see cref="LowLevelPhysics2D.PhysicsShape.callbackTarget"/> for both shapes involved if they implement <see cref="LowLevelPhysics2D.PhysicsCallbacks.IPreSolveCallback"/>.
        /// </summary>
        public bool preSolveCallbacks { readonly get => m_PreSolveCallbacks; set => m_PreSolveCallbacks = value; }

        /// <summary>
        /// Normally shapes onSstatic bodies don't create contacts when they are added to the world.
        /// This overrides that behavior and causes contact creation.
        /// This significantly slows down Static body creation which can be important when there are many Static shapes.
        /// This is implicitly always true for Triggers, Dynamic bodies and Kinematic bodies.
        /// </summary>
        public bool startStaticContacts { readonly get => m_StartStaticContacts; set => m_StartStaticContacts = value; }

        /// <summary>
        /// Should the body update its mass properties when this shape is created.
        /// Disabling this improves performance when multiple shapes are being added to the same body.
        /// The mass of a body can then be explicitly updated by calling <see cref="LowLevelPhysics2D.PhysicsBody.ApplyMassFromShapes"/>
        /// </summary>
        public bool startMassUpdate { readonly get => m_StartMassUpdate; set => m_StartMassUpdate = value; }

        /// <summary>
        /// The surface material for the shape comprising of many properties such as friciton, bounciness, rolling resistance etc.
        /// </summary>
        /// <remarks>
        /// This is exposed directly as a field rather than a property as it is extremely unlikely to ever change and causes usability issues as a property.
        /// </remarks>
        public PhysicsShape.SurfaceMaterial surfaceMaterial;

        /// <summary>
        /// The contact filter used to control which contacts this shape can participate in.
        /// </summary>
        /// <remarks>
        /// This is exposed directly as a field rather than a property as it is extremely unlikely to ever change and causes usability issues as a property.
        /// </remarks>
        public PhysicsShape.ContactFilter contactFilter;

        /// <summary>
        /// The mover data used for the shape mover.
        /// </summary>
        public PhysicsShape.MoverData moverData;

        #region Internal

        [SerializeField] [Min(0.0f)] float m_Density;
        [SerializeField] bool m_IsTrigger;
	    [SerializeField] bool m_TriggerEvents;
        [SerializeField] bool m_ContactEvents;
        [SerializeField] bool m_HitEvents;
        [SerializeField] bool m_ContactFilterCallbacks;
        [SerializeField] bool m_PreSolveCallbacks;
        [SerializeField] bool m_StartStaticContacts;
        [SerializeField] bool m_StartMassUpdate;

        #endregion
    }
}
