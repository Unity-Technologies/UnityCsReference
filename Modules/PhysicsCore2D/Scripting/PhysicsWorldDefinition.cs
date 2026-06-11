// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
using static Unity.U2D.Physics.Scripting2D;

namespace Unity.U2D.Physics
{
    /// <summary>
    /// A <see cref="PhysicsWorld"/> definition used to specify important initial properties.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [MovedFrom(autoUpdateAPI: ScriptUpdateConstants.AutoUpdateAPI, sourceNamespace: ScriptUpdateConstants.SourceNamespace, sourceAssembly: ScriptUpdateConstants.SourceAssembly)]
    public partial struct PhysicsWorldDefinition
    {
        /// <summary>
        /// Create a default <see cref="PhysicsWorld"/> definition.
        /// </summary>
        public PhysicsWorldDefinition() { this = defaultDefinition; }

        /// <summary>
        /// Create a default <see cref="PhysicsWorld"/> definition.
        /// </summary>
        /// <param name="useSettings">Controls whether the default settings come from the physics settings or not.</param>
        public PhysicsWorldDefinition(bool useSettings) { this = PhysicsWorld_GetDefaultDefinition(useSettings); }

        /// <summary>
        /// Get a default <see cref="PhysicsWorld"/> definition.
        /// </summary>
        public static PhysicsWorldDefinition defaultDefinition => PhysicsWorld_GetDefaultDefinition(true);

        /// <summary>
        /// Get/Set the gravity vector applied to all bodies in the world, usually in m/s^2.
        /// See <see cref="PhysicsWorld.gravity"/>.
        /// </summary>
        public Vector2 gravity { readonly get => m_Gravity; set => m_Gravity = value; }

        /// <summary>
        /// Get/Set the simulation mode which controls when or if the simulation will be automatically simulated.
        /// See <see cref="PhysicsWorld.SimulationType"/> and <see cref="PhysicsWorld.Simulate(float)"/>.
        /// </summary>
        public PhysicsWorld.SimulationType simulationType { readonly get => m_SimulationType; set => m_SimulationType = value; }

        /// <summary>
        /// Get/Set the simulation sub-steps to use during simulation.
        /// See <see cref="PhysicsWorld.Simulate(float)"/>.
        /// See <see cref="PhysicsWorld.simulationSubSteps"/>.
        /// </summary>
        public int simulationSubSteps { readonly get => m_SimulationSubSteps; set => m_SimulationSubSteps = Mathf.Max(1, value); }

        /// <summary>
        /// Get/Set the simulation worker count for the world.
        /// A single simulation worker is always used for simulation therefore a worker count of one means single thread simulation only.
        /// The actual quantity of workers used will always be capped to those available on the current device and reading the property will return the number of workers actually being used by the device.
        /// Changing the worker count continuously is not recommend and will impact performance as it requires the task queue be recreated.
        /// See <see cref="PhysicsWorld.simulationWorkers"/>.
        /// </summary>
        public int simulationWorkers { readonly get => m_SimulationWorkers; set => m_SimulationWorkers = Mathf.Clamp(value, 0, PhysicsConstants.MaxWorkers); }

        /// <summary>
        /// Controls how transform writing is handled.
        /// Only bodies that have their <see cref="PhysicsBody.transformWriteMode"/> active and produce a <see cref="PhysicsEvents.BodyUpdateEvent"/> will write to a transform.
        /// See <see cref="PhysicsWorld.TransformWriteMode"/>.
        /// </summary>
        public PhysicsWorld.TransformWriteMode transformWriteMode { readonly get => m_TransformWriteMode; set => m_TransformWriteMode = value; }

        /// <summary>
        /// Controls the transform plane that the world uses when writing transforms.
        /// See <see cref="PhysicsWorld.transformWriteMode"/>.
        /// See <see cref="PhysicsWorld.transformPlane"/>.
        /// </summary>
        public PhysicsWorld.TransformPlane transformPlane { readonly get => m_TransformPlane; set => m_TransformPlane = value; }

        /// <summary>
        /// Controls the transformation for the <see cref="PhysicsWorld.TransformPlane.Custom"/> to allow transformation writing and reading to/from a custom space.
        /// See <see cref="PhysicsWorld.TransformPlaneCustom"/>.
        /// </summary>
        public PhysicsWorld.TransformPlaneCustom transformPlaneCustom { readonly get => m_TransformPlaneCustom; set => m_TransformPlaneCustom = value; }

        /// <summary>
        /// Controls if and how Transform tweens are calculated and/or written.
        /// Transform tweening is where bodies that have their <see cref="PhysicsBody.transformObject"/> set, write to the <see cref="UnityEngine.Transform"/> each frame
        /// depending on the specific body <see cref="PhysicsBody.TransformWriteMode"/> set.
        /// Regardless of this setting, Transform tweening is never used if the <see cref="PhysicsWorld.simulationType"/> is <see cref="PhysicsWorld.SimulationType.Update"/> or <see cref="PhysicsWorld.transformWriteMode"/> is <see cref="PhysicsWorld.TransformWriteMode.Off"/>.
        /// </summary>
        public PhysicsWorld.TransformTweenMode transformTweenMode { readonly get => m_TransformTweenMode; set => m_TransformTweenMode = value; }

        /// <summary>
        /// Controls if an extra write pass prior to the script fixed-update callback is made for any interpolation tweens to ensure that transforms are synchronized to the final body pose.
        /// Because this is an extra write pass, it has an impact on overall performance so only enable if you require transforms synchronized this way.
        ///
        /// NOTE: This only affects <see cref="PhysicsBody"/> that have their <see cref="PhysicsBody.transformWriteMode"/> set to <see cref="PhysicsBody.TransformWriteMode.Interpolate"/>.
        /// </summary>
        public bool syncInterpolation { readonly get => m_SyncInterpolation; set => m_SyncInterpolation = value; }

        /// <summary>
        /// Controls if bodies go to sleep when not moving and not interacting.
        /// Sleeping can provide a significant performance improvement when many Dynamic or Kinematic bodies are in the world.
        /// See <see cref="PhysicsWorld.sleepingAllowed"/>
        /// </summary>
        public bool sleepingAllowed { readonly get => m_SleepingAllowed; set => m_SleepingAllowed = value; }

        /// <summary>
        /// Controls if continuous collision detection will be used between Dynamic and Static bodies.
        /// Generally you should keep continuous collision enabled to prevent fast moving objects from going through Static objects.
        /// The performance gain from disabling continuous collision is minor.
        /// See <see cref="PhysicsWorld.continuousAllowed"/>
        /// </summary>
        public bool continuousAllowed { readonly get => m_ContinuousAllowed; set => m_ContinuousAllowed = value; }

        /// <summary>
        /// Controls if contact filter callbacks will be called.
        /// A contact filter callback allows direct control over whether a contact will be created between a pair of shapes.
        /// This applies to both triggers and non-triggers but only with Dynamic bodies.
        /// These are relatively expensive so disabling them can provide a significant performance benefit.
        /// A contact filter callback will call the <see cref="PhysicsShape.callbackTarget"/> for both shapes involved if they implement <see cref="PhysicsCallbacks.IContactFilterCallback"/>.
        /// </summary>
        public bool contactFilterCallbacks { readonly get => m_ContactFilterCallbacks; set => m_ContactFilterCallbacks = value; }

        /// <summary>
        /// Controls if pre-solve callbacks will be called.
        /// This only applies to Dynamic bodies and is ignored for triggers.
        /// These are relatively expensive so disabling them can provide a significant performance benefit.
        /// A pre-solve callback will call the <see cref="PhysicsShape.callbackTarget"/> for both shapes involved if they implement <see cref="PhysicsCallbacks.IPreSolveCallback"/>.
        /// </summary>
        public bool preSolveCallbacks { readonly get => m_PreSolveCallbacks; set => m_PreSolveCallbacks = value; }

        /// <summary>
        /// Controls if body update callback targets are automatically called.
        /// See <see cref="PhysicsWorld.SendBodyUpdateCallbacks"/>.
        /// </summary>
        public bool autoBodyUpdateCallbacks { readonly get => m_AutoBodyUpdateCallbacks; set => m_AutoBodyUpdateCallbacks = value; }

        /// <summary>
        /// Controls if shape contact callback targets are automatically called.
        /// See <see cref="PhysicsWorld.SendContactCallbacks"/>.
        /// </summary>
        public bool autoContactCallbacks { readonly get => m_AutoContactCallbacks; set => m_AutoContactCallbacks = value; }

        /// <summary>
        /// Controls if shape trigger callback targets are automatically called.
        /// See <see cref="PhysicsWorld.SendTriggerCallbacks"/>.
        /// </summary>
        public bool autoTriggerCallbacks { readonly get => m_AutoTriggerCallbacks; set => m_AutoTriggerCallbacks = value; }

        /// <summary>
        /// Controls if joint threshold callback targets are automatically called.
        /// See <see cref="PhysicsWorld.SendJointThresholdCallbacks"/>.
        /// </summary>
        public bool autoJointThresholdCallbacks { readonly get => m_AutoJointThresholdCallbacks; set => m_AutoJointThresholdCallbacks = value; }

        /// <summary>
        /// Adjust the bounce threshold, usually in meters per second. It is recommended not to make this value very small because it will prevent bodies from sleeping.
        /// See <see cref="PhysicsWorld.bounceThreshold"/>.
        /// </summary>
        public float bounceThreshold { readonly get => m_BounceThreshold; set => m_BounceThreshold = Mathf.Max(0f, value); }

        /// <summary>
        /// The contact hit event threshold controls the collision speed needed to generate a contact hit event, usually in meters per second.
        /// See <see cref="PhysicsEvents.ContactHitEvent"/>.
        /// See <see cref="PhysicsWorld.contactHitEventThreshold"/>.
        /// </summary>
        public float contactHitEventThreshold { readonly get => m_ContactHitEventThreshold; set => m_ContactHitEventThreshold = Mathf.Max(0f, value); }

        /// <summary>
        /// The contact stiffness, in cycles per second.
        /// See <see cref="PhysicsWorld.contactFrequency"/>.
        /// </summary>
        public float contactFrequency { readonly get => m_ContactFrequency; set => m_ContactFrequency = Mathf.Max(0f, value); }

        /// <summary>
        /// The contact bounciness with 1 being critical damping (non-dimensional).
        /// See <see cref="PhysicsWorld.contactDamping"/>.
        /// </summary>
        public float contactDamping { readonly get => m_ContactDamping; set => m_ContactDamping = Mathf.Max(0f, value); }

        /// <summary>
        /// The contact speed used to solve overlaps, in meters per second.
        /// See <see cref="PhysicsWorld.contactSpeed"/>.
        /// </summary>
        public float contactSpeed { readonly get => m_ContactSpeed; set => m_ContactSpeed = Mathf.Max(0f, value); }

        /// <summary>
        /// The contact recycle distance, in meters.
        /// Setting this to zero disables contact point recycling.
        /// Contact recycling reuses contact points across simulation time-steps when the relative movement is small.
        /// This feature improves stability and performance by around 25% (approximately).
        /// Contact points are not recalculated until shapes move more than 5cm (default) relative to each other.
        /// Contact recycling skips some updates such as friction, pre-solve (etc) until the contacts are no longer recycled.
        /// See <see cref="PhysicsWorld.contactRecycleDistance"/>.
        /// </summary>
        public float contactRecycleDistance { readonly get => m_ContactRecycleDistance; set => m_ContactRecycleDistance = Mathf.Max(0f, value); }

        /// <summary>
        /// Get/Set the maximum linear speed.
        /// See <see cref="PhysicsWorld.maximumLinearSpeed"/>.
        /// </summary>
        public float maximumLinearSpeed { readonly get => m_MaximumLinearSpeed; set => m_MaximumLinearSpeed = Mathf.Max(0f, value); }

        /// <summary>
        /// Limits what gets drawn to a broad selection.
        /// See <see cref="PhysicsWorld.DrawOptions"/>.
        /// </summary>
        public PhysicsWorld.DrawOptions drawOptions { readonly get => m_DrawOptions; set => m_DrawOptions = value; }

        /// <summary>
        /// Controls how shape geometry is filled when drawing.
        /// See <see cref="PhysicsWorld.DrawFillOptions"/>.
        /// </summary>
        public PhysicsWorld.DrawFillOptions drawFillOptions { readonly get => m_DrawFillOptions; set => m_DrawFillOptions = value; }

        /// <summary>
        /// Controls how contact points are drawn.
        /// See <see cref="PhysicsWorld.DrawContactType"/>.
        /// </summary>
        public PhysicsWorld.DrawContactType drawContactType { readonly get => m_DrawContactType; set => m_DrawContactType = value; }

        /// <summary>
        /// Limits what gets drawn to a narrow selection.
        /// This only affects <see cref="PhysicsWorld.DrawOptions"/> that are drawing all bodies, shapes etc.
        /// It does not affect selected elements or custom drawing.
        /// See <see cref="PhysicsWorld.IgnoreFilter"/>.
        /// </summary>
        public PhysicsWorld.IgnoreFilter drawFilter { readonly get => m_DrawFilter; set => m_DrawFilter = value; }

        /// <summary>
        /// Controls the draw thickness (outline and orientation).
        /// See <see cref="PhysicsWorld.drawThickness"/>.
        /// </summary>
        public float drawThickness { readonly get => m_DrawThickness; set => m_DrawThickness = Mathf.Clamp(value, 1f, 5f); }

        /// <summary>
        /// Controls the draw fill alpha. This is used to scale the interior fill alpha and is only used when <see cref="PhysicsWorld.DrawFillOptions.Outline"/> is used so that the interior color can be distinguished from the outline color by transparency.
        /// See <see cref="PhysicsWorld.drawFillAlpha"/>.
        /// </summary>
        public float drawFillAlpha { readonly get => m_DrawFillAlpha; set => m_DrawFillAlpha = Mathf.Clamp01(value); }

        /// <summary>
        /// Controls the draw point scale used when drawing points.
        /// See <see cref="PhysicsWorld.drawPointScale"/>.
        /// </summary>
        public float drawPointScale { readonly get => m_DrawPointScale; set => m_DrawPointScale = Mathf.Clamp(value, 0.001f, 10f); }

        /// <summary>
        /// Controls the joint contact normal scale used when drawing contact normals.
        /// See <see cref="PhysicsWorld.drawNormalScale"/>.
        /// </summary>
        public float drawNormalScale { readonly get => m_DrawNormalScale; set => m_DrawNormalScale = Mathf.Clamp(value, 0.0001f, 10f); }

        /// <summary>
        /// Controls the joint contact force scale used when drawing contact forces.
        /// See <see cref="PhysicsWorld.drawForceScale"/>.
        /// </summary>
        public float drawForceScale { readonly get => m_DrawForceScale; set => m_DrawForceScale = Mathf.Clamp(value, 0.0001f, 10f); }

        /// <summary>
        /// Controls what colors are used to draw <see cref="PhysicsBody"/>, <see cref="PhysicsShape"/>, <see cref="PhysicsJoint"/> etc.
        /// See <see cref="PhysicsWorld.DrawColors"/>.
        /// </summary>
        public PhysicsWorld.DrawColors drawColors { readonly get => m_DrawColors; set => m_DrawColors = value; }

        #region Internal

        [SerializeField] Vector2 m_Gravity;
        [SerializeField] [FormerlySerializedAs("m_SimulationMode")] PhysicsWorld.SimulationType m_SimulationType;
        [SerializeField] [Min(1)] int m_SimulationSubSteps;
        [SerializeField] [Range(1, PhysicsConstants.MaxWorkers)] int m_SimulationWorkers;
        [SerializeField] PhysicsWorld.TransformWriteMode m_TransformWriteMode;
        [SerializeField] [FormerlySerializedAs("m_TransformTweening")] PhysicsWorld.TransformTweenMode m_TransformTweenMode;
        [SerializeField] PhysicsWorld.TransformPlane m_TransformPlane;
        [SerializeField] PhysicsWorld.TransformPlaneCustom m_TransformPlaneCustom;
        [SerializeField] bool m_SyncInterpolation;
        [SerializeField] bool m_SleepingAllowed;
        [SerializeField] bool m_ContinuousAllowed;
        [SerializeField] bool m_ContactFilterCallbacks;
        [SerializeField] bool m_PreSolveCallbacks;
        [SerializeField] bool m_AutoBodyUpdateCallbacks;
        [SerializeField] bool m_AutoContactCallbacks;
        [SerializeField] bool m_AutoTriggerCallbacks;
        [SerializeField] bool m_AutoJointThresholdCallbacks;
        [SerializeField] [Min(0.0f)] float m_BounceThreshold;
        [SerializeField] [Min(0.0f)] float m_ContactHitEventThreshold;
        [SerializeField] [Min(0.0f)] float m_ContactFrequency;
        [SerializeField] [Min(0.0f)] float m_ContactDamping;
        [SerializeField] [Min(0.0f)] float m_ContactSpeed;
        [SerializeField] [Min(0.0f)] float m_ContactRecycleDistance;
        [SerializeField] [Min(0.0f)] float m_MaximumLinearSpeed;
        [SerializeField] PhysicsWorld.DrawOptions m_DrawOptions;
        [SerializeField] PhysicsWorld.DrawFillOptions m_DrawFillOptions;
        [SerializeField] PhysicsWorld.DrawContactType m_DrawContactType;
        [SerializeField] PhysicsWorld.IgnoreFilter m_DrawFilter;
        [SerializeField] [Range(1f, 5f)] float m_DrawThickness;
        [SerializeField] [Range(0f, 1f)] float m_DrawFillAlpha;
        [SerializeField] [Range(0.0001f, 10f)] float m_DrawPointScale;
        [SerializeField] [Range(0.0001f, 10f)] float m_DrawNormalScale;
        [SerializeField] [FormerlySerializedAs("m_DrawImpulseScale")] [Range(0.0001f, 10f)] float m_DrawForceScale;
        [SerializeField] PhysicsWorld.DrawColors m_DrawColors;

        #endregion
    }
}
