// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Serialization;
using static UnityEngine.LowLevelPhysics2D.PhysicsLowLevelScripting2D;

namespace UnityEngine.LowLevelPhysics2D
{
    /// <summary>
    /// A <see cref="LowLevelPhysics2D.PhysicsWorld"/> definition used to specify important initial properties.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct PhysicsWorldDefinition
    {
        /// <summary>
        /// Create a default <see cref="LowLevelPhysics2D.PhysicsWorld"/> definition.
        /// </summary>
        public PhysicsWorldDefinition() { this = defaultDefinition; }

        /// <summary>
        /// Create a default <see cref="LowLevelPhysics2D.PhysicsWorld"/> definition.
        /// </summary>
        /// <param name="useSettings">Controls whether the default settings come from the physics settings or not.</param>
        public PhysicsWorldDefinition(bool useSettings) { this = PhysicsWorld_GetDefaultDefinition(useSettings); }

        /// <summary>
        /// Get a default <see cref="LowLevelPhysics2D.PhysicsWorld"/> definition.
        /// </summary>
        public static PhysicsWorldDefinition defaultDefinition { get => PhysicsWorld_GetDefaultDefinition(true); }

        /// <summary>
        /// Get/Set the simulation worker count for the world.
        /// The actual quantity of workers used will always be capped to those available on the current device and reading the property will return the number of workers actually being used by the device.
        /// Changing the worker count continuously is not recommend and will impact performance as it requires the task queue be recreated.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.gravity"/>.
        /// </summary>
        public Vector2 gravity { readonly get => m_Gravity; set => m_Gravity = value; }

        /// <summary>
        /// Get/Set the simulation mode which controls when or if the simulation will be automatically simulated.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.SimulationType"/> and <see cref="LowLevelPhysics2D.PhysicsWorld.Simulate(float)"/>.
        /// </summary>
        public PhysicsWorld.SimulationType simulateType { readonly get => m_SimulationType; set => m_SimulationType = value; }

        /// <summary>
        /// Get/Set the simulation sub-steps to use during simulation.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.Simulate(float)"/>.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.simulationSubSteps"/>.
        /// </summary>
        public int simulationSubSteps { readonly get => m_SimulationSubSteps; set => m_SimulationSubSteps = Mathf.Max(1, value); }

        /// <summary>
        /// Get/Set the simulation worker count for the world.
        /// The actual quantity of workers used will always be capped to those available on the current device and reading the property will return the number of workers actually being used by the device.
        /// Changing the worker count continuously is not recommend and will impact performance as it requires the task queue be recreated.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.simulationWorkers"/>.
        /// </summary>
        public int simulationWorkers { readonly get => m_SimulationWorkers; set => m_SimulationWorkers = Mathf.Clamp(value, 0, PhysicsConstants.MaxWorkers); }

        /// <summary>
        /// Controls how transform writing is handled.
        /// Only bodies that have their <see cref="LowLevelPhysics2D.PhysicsBody.transformWriteMode"/> active and produce a <see cref="LowLevelPhysics2D.PhysicsEvents.BodyUpdateEvent"/> will write to a transform.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.TransformWriteMode"/>.
        /// </summary>
        public PhysicsWorld.TransformWriteMode transformWriteMode { readonly get => m_TransformWriteMode; set => m_TransformWriteMode = value; }

        /// <summary>
        /// Controls the transform plane that the world uses when writing transforms.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.transformWriteMode"/>.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.transformPlane"/>.
        /// </summary>
        public PhysicsWorld.TransformPlane transformPlane { readonly get => m_TransformPlane; set => m_TransformPlane = value; }

        /// <summary>
        /// Controls if Transform tweening is used. Transform tweening is where bodies that have their <see cref="LowLevelPhysics2D.PhysicsBody.transformObject"/> set, write to the <see cref="UnityEngine.Transform"/> each frame
        /// depending on the specific body <see cref="LowLevelPhysics2D.PhysicsBody.TransformWriteMode"/> set.
        /// Regardless of this setting, Transform tweening is never used if the <see cref="LowLevelPhysics2D.PhysicsWorld.simulationType"/> set to <see cref="LowLevelPhysics2D.PhysicsWorld.SimulationType.Update"/> or <see cref="LowLevelPhysics2D.PhysicsWorld.transformWriteMode"/> is <see cref="LowLevelPhysics2D.PhysicsWorld.TransformWriteMode.Off"/>.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.transformTweening"/>.
        /// </summary>
        public bool transformTweening { readonly get => m_TransformTweening; set => m_TransformTweening = value; }

        /// <summary>
        /// Controls if bodies go to sleep when not moving and not interacting.
        /// Sleeping can provide a significant performance improvement when many Dynamic or Kinematic bodies are in the world.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.sleepingAllowed"/>
        /// </summary>
        public bool sleepingAllowed { readonly get => m_SleepingAllowed; set => m_SleepingAllowed = value; }

        /// <summary>
        /// Controls if continuous collision detection will be used between Dynamic and Static bodies.
        /// Generally you should keep continuous collision enabled to prevent fast moving objects from going through Static objects.
        /// The performance gain from disabling continuous collision is minor.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.continuousAllowed"/>
        /// </summary>
        public bool continuousAllowed { readonly get => m_ContinuousAllowed; set => m_ContinuousAllowed = value; }

        /// <summary>
        /// Controls if contact filter callbacks will be called.
        /// A contact filter callback allows direct control over whether a contact will be created between a pair of shapes.
        /// This applies to both triggers and non-triggers but only with Dynamic bodies.
        /// These are relatively expensive so disabling them can provide a significant performance benefit.
        /// A contact filter callback will call the <see cref="LowLevelPhysics2D.PhysicsShape.callbackTarget"/> for both shapes involved if they implement <see cref="LowLevelPhysics2D.PhysicsCallbacks.IContactFilterCallback"/>.
        /// </summary>
        public bool contactFilterCallbacks { readonly get => m_ContactFilterCallbacks; set => m_ContactFilterCallbacks = value; }

        /// <summary>
        /// Controls if pre-solve callbacks will be called.
        /// This only applies to Dynamic bodies and is ignored for triggers.
        /// These are relatively expensive so disabling them can provide a significant performance benefit.
        /// A pre-solve callback will call the <see cref="LowLevelPhysics2D.PhysicsShape.callbackTarget"/> for both shapes involved if they implement <see cref="LowLevelPhysics2D.PhysicsCallbacks.IPreSolveCallback"/>.
        /// </summary>
        public bool preSolveCallbacks { readonly get => m_PreSolveCallbacks; set => m_PreSolveCallbacks = value; }

        /// <summary>
        /// Controls if body update callback targets are automatically called.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.SendBodyUpdateCallbacks"/>.
        /// </summary>
        public bool autoBodyUpdateCallbacks { readonly get => m_AutoBodyUpdateCallbacks; set => m_AutoBodyUpdateCallbacks = value; }

        /// <summary>
        /// Controls if shape contact callback targets are automatically called.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.SendContactCallbacks"/>.
        /// </summary>
        public bool autoContactCallbacks { readonly get => m_AutoContactCallbacks; set => m_AutoContactCallbacks = value; }

        /// <summary>
        /// Controls if shape trigger callback targets are automatically called.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.SendTriggerCallbacks"/>.
        /// </summary>
        public bool autoTriggerCallbacks { readonly get => m_AutoTriggerCallbacks; set => m_AutoTriggerCallbacks = value; }

        /// <summary>
        /// Controls if joint threshold callback targets are automatically called.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.SendJointThresholdCallbacks"/>.
        /// </summary>
        public bool autoJointThresholdCallbacks { readonly get => m_AutoJointThresholdCallbacks; set => m_AutoJointThresholdCallbacks = value; }

        /// <summary>
        /// Adjust the bounce threshold, usually in meters per second. It is recommended not to make this value very small because it will prevent bodies from sleeping.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.bounceThreshold"/>.
        /// </summary>
        public float bounceThreshold { readonly get => m_BounceThreshold; set => m_BounceThreshold = Mathf.Max(0f, value); }

        /// <summary>
        /// The contact hit event threshold controls the collision speed needed to generate a contact hit event, usually in meters per second.
        /// See <see cref="LowLevelPhysics2D.PhysicsEvents.ContactHitEvent"/>.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.contactHitEventThreshold"/>.
        /// </summary>
        public float contactHitEventThreshold { readonly get => m_ContactHitEventThreshold; set => m_ContactHitEventThreshold = Mathf.Max(0f, value); }

        /// <summary>
        /// The contact stiffness, in cycles per second.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.contactFrequency"/>.
        /// </summary>
        public float contactFrequency { readonly get => m_ContactFrequency; set => m_ContactFrequency = Mathf.Max(0f, value); }

        /// <summary>
        /// The contact bounciness with 1 being critical damping (non-dimensional).
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.contactDamping"/>.
        /// </summary>
        public float contactDamping { readonly get => m_ContactDamping; set => m_ContactDamping = Mathf.Max(0f, value); }

        /// <summary>
        /// The contact speed used to solve overlaps, in meters per second.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.contactSpeed"/>.
        /// </summary>
        public float contactSpeed { readonly get => m_ContactSpeed; set => m_ContactSpeed = Mathf.Max(0f, value); }

        /// <summary>
        /// Get/Set the maximum linear speed.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.maximumLinearSpeed"/>.
        /// </summary>
        public float maximumLinearSpeed { readonly get => m_MaximumLinearSpeed; set => m_MaximumLinearSpeed = Mathf.Max(0f, value); }

        /// <summary>
        /// Draw Options used to control what is drawn using <see cref="LowLevelPhysics2D.PhysicsWorld.Draw"/>.
        /// This is only used in the Unity Editor or in a Development Player.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.DrawOptions"/>.
        /// </summary>
        public PhysicsWorld.DrawOptions drawOptions { readonly get => m_DrawOptions; set => m_DrawOptions = value; }

        /// <summary>
        /// Controls what aspects of is drawn using <see cref="LowLevelPhysics2D.PhysicsWorld.Draw"/>.
        /// This is only used in the Unity Editor or in a Development Player.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.DrawFillOptions"/>.
        /// </summary>
        public PhysicsWorld.DrawFillOptions drawFillOptions { readonly get => m_DrawFillOptions; set => m_DrawFillOptions = value; }

        /// <summary>
        /// Controls the draw thickness (outline and orientation).
        /// This is only used in the Unity Editor or in a Development Player.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.drawThickness"/>.
        /// </summary>
        public float drawThickness { readonly get => m_DrawThickness; set => m_DrawThickness = Mathf.Clamp(value, 1f, 5f); }

        /// <summary>
        /// Controls the draw fill alpha. This is used to scale the interior fill alpha and is only used when <see cref="LowLevelPhysics2D.PhysicsWorld.DrawFillOptions.Outline"/> is used so that the interior color can be distinguished from the outline color by transparency.
        /// This is only used in the Unity Editor or in a Development Player.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.drawFillAlpha"/>.
        /// </summary>
        public float drawFillAlpha { readonly get => m_DrawFillAlpha; set => m_DrawFillAlpha = Mathf.Clamp01(value); }

        /// <summary>
        /// Controls the draw point scale used when drawing points.
        /// This is only used in the Unity Editor or in a Development Player.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.drawPointScale"/>.
        /// </summary>
        public float drawPointScale { readonly get => m_DrawPointScale; set => m_DrawPointScale = Mathf.Clamp(value, 0.001f, 10f); }

        /// <summary>
        /// Controls the joint contact normal scale used when drawing contact normals.
        /// This is only used in the Unity Editor or in a Development Player.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.drawNormalScale"/>.
        /// </summary>
        public float drawNormalScale { readonly get => m_DrawNormalScale; set => m_DrawNormalScale = Mathf.Clamp(value, 0.001f, 10f); }

        /// <summary>
        /// Controls the joint contact impulse scale used when drawing contact impulses.
        /// This is only used in the Unity Editor or in a Development Player.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.drawImpulseScale"/>.
        /// </summary>
        public float drawImpulseScale { readonly get => m_DrawImpulseScale; set => m_DrawImpulseScale = Mathf.Clamp(value, 0.001f, 10f); }

        /// <summary>
        /// Controls the draw capacity.
        /// The draw capacity of the buffers when drawing are initially zero however increasing this value will mean buffers won't be resized when more elements are drawn and therefore no GC allocations will occur.
        /// Changes won't take effect until exiting play mode. This value directly controls the capacity for each element type drawn.
        /// This is only used in the Unity Editor or in a Development Player.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.drawCapacity"/>.
        /// </summary>
        public int drawCapacity { readonly get => m_DrawCapacity; set => m_DrawCapacity = Mathf.Max(0, value); }

        /// <summary>
        /// Controls what colors are used to draw <see cref="LowLevelPhysics2D.PhysicsBody"/>, <see cref="LowLevelPhysics2D.PhysicsShape"/>, <see cref="LowLevelPhysics2D.PhysicsJoint"/> etc.
        /// This is only used in the Unity Editor or in a Development Player.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.DrawColors"/>.
        /// </summary>
        public PhysicsWorld.DrawColors drawColors { readonly get => m_DrawColors; set => m_DrawColors = value; }

        #region Internal

        [SerializeField] Vector2 m_Gravity;
        [FormerlySerializedAs("m_SimulationMode")][SerializeField] PhysicsWorld.SimulationType m_SimulationType;
        [SerializeField] [Min(1)] int m_SimulationSubSteps;
        [SerializeField] [Range(0, PhysicsConstants.MaxWorkers)] int m_SimulationWorkers;
        [SerializeField] PhysicsWorld.TransformWriteMode m_TransformWriteMode;
        [SerializeField] PhysicsWorld.TransformPlane m_TransformPlane;
        [SerializeField] bool m_TransformTweening;
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
        [SerializeField] [Min(0.0f)] float m_MaximumLinearSpeed;
        [SerializeField] PhysicsWorld.DrawOptions m_DrawOptions;
        [SerializeField] PhysicsWorld.DrawFillOptions m_DrawFillOptions;
        [SerializeField] [Range(1f, 5f)] float m_DrawThickness;
        [SerializeField] [Range(0f, 1f)] float m_DrawFillAlpha;
        [SerializeField] [Range(0.001f, 10f)] float m_DrawPointScale;
        [SerializeField] [Range(0.001f, 10f)] float m_DrawNormalScale;
        [SerializeField] [Range(0.001f, 10f)] float m_DrawImpulseScale;
        [SerializeField] [Min(0)] int m_DrawCapacity;
        [SerializeField] PhysicsWorld.DrawColors m_DrawColors;

        #endregion
    }
}
