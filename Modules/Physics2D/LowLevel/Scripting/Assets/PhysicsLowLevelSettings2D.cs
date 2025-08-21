// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;

namespace UnityEngine.LowLevelPhysics2D
{
    /// <summary>
    /// Physics LowLevel Settings Asset.
    /// This contains all the global physics options along with the default values for the following definitions:
    ///
    ///- <see cref="LowLevelPhysics2D.PhysicsWorldDefinition"/>
    ///- <see cref="LowLevelPhysics2D.PhysicsBodyDefinition"/>
    ///- <see cref="LowLevelPhysics2D.PhysicsShape"/>
    ///- <see cref="LowLevelPhysics2D.PhysicsChainDefinition"/>
    ///
    /// </summary>
    [RequiredByNativeCode]
    [Serializable]
    public sealed class PhysicsLowLevelSettings2D : ScriptableObject
    {
        /// <undoc/>
        public PhysicsLowLevelSettings2D()
        {
            Reset();
        }

        void Reset()
        {
            m_PhysicsWorldDefinition = new PhysicsWorldDefinition(useSettings: false);
            m_PhysicsBodyDefinition = new PhysicsBodyDefinition(useSettings: false);
            m_PhysicsShapeDefinition = new PhysicsShapeDefinition(useSettings: false);
            m_PhysicsChainDefinition = new PhysicsChainDefinition(useSettings: false);
            m_PhysicsDistanceJointDefinition = new PhysicsDistanceJointDefinition(useSettings: false);
            m_PhysicsFixedJointDefinition = new PhysicsFixedJointDefinition(useSettings: false);
            m_PhysicsHingeJointDefinition = new PhysicsHingeJointDefinition(useSettings: false);
            m_PhysicsRelativeJointDefinition = new PhysicsRelativeJointDefinition(useSettings: false);
            m_PhysicsSliderJointDefinition = new PhysicsSliderJointDefinition(useSettings: false);
            m_PhysicsWheelJointDefinition = new PhysicsWheelJointDefinition(useSettings: false);

            m_PhysicsLayerNames = PhysicsLayers.LayerNames.DefaultLayerNames;
            m_ConcurrentSimulations = 2;
            m_LengthUnitsPerMeter = 1.0f;
            m_DrawInBuild = false;
            m_BypassLowLevel = false;
        }

        void OnValidate()
        {
            if (UnityEditor.LowLevelPhysics2D.PhysicsEditor.lowLevelSettings == this)
                UnityEditor.LowLevelPhysics2D.PhysicsEditor.ReadProjectSettings();
        }

        /// <summary>
        /// A set of 64 "layer" names associated with each bit in a <see cref="LowLevelPhysics2D.PhysicsMask"/> when used for contacts and queries.
        /// </summary>
        public PhysicsLayers.LayerNames physicsLayerNames { get => m_PhysicsLayerNames; set => m_PhysicsLayerNames = value; }

        /// <summary>
        /// Controls if full 64-bit layers are used based upon <see cref="LowLevelPhysics2D.PhysicsLowLevelSettings2D.physicsLayerNames"/> or if not, the standard 32-bit layers based upon <see cref="UnityEngine.LayerMask"/>.
        /// If a <see cref="LowLevelPhysics2D.PhysicsLowLevelSettings2D"/> asset is assigned then the full layers (<see cref="LowLevelPhysics2D.PhysicsLowLevelSettings2D.physicsLayerNames"/>) will be used if <see cref="LowLevelPhysics2D.PhysicsLowLevelSettings2D.useFullLayers"/> is also active.
        /// If no <see cref="LowLevelPhysics2D.PhysicsLowLevelSettings2D"/> asset is assigned then the global layers (See <see cref="UnityEngine.LayerMask"/>) will be used.
        /// </summary>
        public bool useFullLayers { get => m_UseFullLayers; set => m_UseFullLayers = value; }

        /// <summary>
        /// Get/Set the <see cref="LowLevelPhysics2D.PhysicsWorldDefinition"/>.
        /// </summary>        
        public PhysicsWorldDefinition physicsWorldDefinition { get => m_PhysicsWorldDefinition; set => m_PhysicsWorldDefinition = value; }

        /// <summary>
        /// Get/Set the <see cref="LowLevelPhysics2D.PhysicsBodyDefinition"/>.
        /// </summary>
        public PhysicsBodyDefinition physicsBodyDefinition { get => m_PhysicsBodyDefinition; set => m_PhysicsBodyDefinition = value; }

        /// <summary>
        /// Get/Set the <see cref="LowLevelPhysics2D.PhysicsShapeDefinition"/>.
        /// </summary>
        public PhysicsShapeDefinition physicsShapeDefinition { get => m_PhysicsShapeDefinition; set => m_PhysicsShapeDefinition = value; }

        /// <summary>
        /// Get/Set the <see cref="LowLevelPhysics2D.PhysicsChainDefinition"/>.
        /// </summary>
        public PhysicsChainDefinition physicsChainDefinition { get => m_PhysicsChainDefinition; set => m_PhysicsChainDefinition = value; }

        /// <summary>
        /// Controls how many simulations can be started in parallel.
        /// Each one is started on its own worker and acts as its own main-thread.
        /// Workers should ideally be left free for the solver otherwise it may degrade solving performance.
        /// The actual quantity of workers used will always be capped to those available on the current device.
        ///
        /// If the total number of workers available is below 4 then parallel simulation won't occur however parallel solving using workers will.
        /// This should not be confused with the quantity of workers used when solving a simulation.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorldDefinition.simulationWorkers"/>.
        /// </summary>        
        [Range(1, PhysicsConstants.MaxWorkers)]
        public int concurrentSimulations { get => m_ConcurrentSimulations; set => m_ConcurrentSimulations = Mathf.Clamp(value, 1, PhysicsConstants.MaxWorkers); }

        /// <summary>
        /// The internal length units per meter.
        /// 
        /// The physics system bases all length units on meters but you may need different units for your project.
        /// You can set this value to use different units but it should only be modified before any other calls to the physics system occur and only modified once.
        /// Changing this value after any physics object has been created can result in severe simulation instabilities.
        ///
        /// Essentially there are some internal tolerances, such as how close two shapes need to be before they are considered to be touching or when two vertices of a hull are so close that they should be considered the same point.
        /// For example, internally a value of 5mm (0.005 meters) is used as a value tuned to work well with most situations with game-sized objects described in meters.
        /// If you decide to work in a different unit system (such as pixels) then 0.005 pixels is not a good value for this constant and would be too precise, leading to numerical problems, especially far from the origin.
        /// Instead you should determine roughly how many pixels you have per meter. For example, say you want 32 pixels per meter then you should set the `lengthUnitsPerMeter` to be 32.0f.
        /// Setting a value of (say) 32.05 would result in the 5mm being scaled up to 0.16 meters, which is a more reasonable value for determining if shapes are touching and hull vertices are too close.
        /// 
        /// A good rule of thumb is to pass the pixel height of your player character to this function, so if your player character is 32 pixels high, then pass 32 to this function.
        /// Then you may confidently use pixels for all the length values sent to the physics system.
        /// All length values returned from the physics system will also then naturally be in pixels because the physics system does not do any scaling internally, however, you are now responsible for creating appropriate values for gravity, density, and forces.
        /// </summary>
        public float lengthUnitsPerMeter { get => m_LengthUnitsPerMeter; set => m_LengthUnitsPerMeter = Mathf.Max(0.00001f, value); }

        /// <summary>
        /// Controls if the debug drawer can be used in a Player Development Build.
        /// </summary>
        public bool drawInBuild { get => m_DrawInBuild; set => m_DrawInBuild = value; }

        /// <summary>
        /// Controls the simulation and rendering of any <see cref="LowLevelPhysics2D.PhysicsWorld"/>.
        /// When true, no automatic simulation or rendering will occur (bypassed).
        /// When false, normal operation occurs with automatic simulation and rendering.
        /// The only case for this to be true is when the low-level physics is not being used at all so this would remove any overhead of simulation or rendering but in most cases, this should be false which is the default.
        /// </summary>
        public bool bypassLowLevel { get => m_BypassLowLevel; set => m_BypassLowLevel = value; }

        #region Internal

        [SerializeField] [Header("Layers")] PhysicsLayers.LayerNames m_PhysicsLayerNames;
        [SerializeField] bool m_UseFullLayers;
        [SerializeField] [Header("Default Definitions")] PhysicsWorldDefinition m_PhysicsWorldDefinition;
        [SerializeField] PhysicsBodyDefinition m_PhysicsBodyDefinition;
        [SerializeField] PhysicsShapeDefinition m_PhysicsShapeDefinition;
        [SerializeField] PhysicsChainDefinition m_PhysicsChainDefinition;
        [SerializeField] PhysicsDistanceJointDefinition m_PhysicsDistanceJointDefinition;
        [SerializeField] PhysicsFixedJointDefinition m_PhysicsFixedJointDefinition;
        [SerializeField] PhysicsHingeJointDefinition m_PhysicsHingeJointDefinition;
        [SerializeField] PhysicsRelativeJointDefinition m_PhysicsRelativeJointDefinition;
        [SerializeField] PhysicsSliderJointDefinition m_PhysicsSliderJointDefinition;
        [SerializeField] PhysicsWheelJointDefinition m_PhysicsWheelJointDefinition;
        [SerializeField] [Range(1, PhysicsConstants.MaxWorkers)] [Header("Globals")] int m_ConcurrentSimulations;
        [SerializeField] [Min(0.00001f)] float m_LengthUnitsPerMeter;
        [SerializeField] bool m_DrawInBuild;
        [SerializeField] bool m_BypassLowLevel;

        #endregion

        #region Native Accessors

        /// <undoc/>
        [RequiredByNativeCode]
        void GetPhysicsLayerNames(out PhysicsLayers.LayerNames layerNames) => layerNames = m_PhysicsLayerNames;

        /// <undoc/>
        [RequiredByNativeCode]
        void GetPhysicsWorldDefinition(out PhysicsWorldDefinition definition) => definition = m_PhysicsWorldDefinition;

        /// <undoc/>
        [RequiredByNativeCode]
        void GetPhysicsBodyDefinition(out PhysicsBodyDefinition definition) => definition = m_PhysicsBodyDefinition;

        /// <undoc/>
        [RequiredByNativeCode]
        void GetPhysicsShapeDefinition(out PhysicsShapeDefinition definition) => definition = m_PhysicsShapeDefinition;

        /// <undoc/>
        [RequiredByNativeCode]
        void GetPhysicsChainDefinition(out PhysicsChainDefinition definition) => definition = m_PhysicsChainDefinition;

        /// <undoc/>
        [RequiredByNativeCode]
        void GetPhysicsDistanceJointDefinition(out PhysicsDistanceJointDefinition definition) => definition = m_PhysicsDistanceJointDefinition;

        /// <undoc/>
        [RequiredByNativeCode]
        void GetPhysicsFixedJointDefinition(out PhysicsFixedJointDefinition definition) => definition = m_PhysicsFixedJointDefinition;

        /// <undoc/>
        [RequiredByNativeCode]
        void GetPhysicsHingeJointDefinition(out PhysicsHingeJointDefinition definition) => definition = m_PhysicsHingeJointDefinition;

        /// <undoc/>
        [RequiredByNativeCode]
        void GetPhysicsRelativeJointDefinition(out PhysicsRelativeJointDefinition definition) => definition = m_PhysicsRelativeJointDefinition;

        /// <undoc/>
        [RequiredByNativeCode]
        void GetPhysicsSliderJointDefinition(out PhysicsSliderJointDefinition definition) => definition = m_PhysicsSliderJointDefinition;

        /// <undoc/>
        [RequiredByNativeCode]
        void GetPhysicsWheelJointDefinition(out PhysicsWheelJointDefinition definition) => definition = m_PhysicsWheelJointDefinition;

        /// <undoc/>
        [RequiredByNativeCode]
        int GetConcurrentSimulations() => m_ConcurrentSimulations;

        /// <undoc/>
        [RequiredByNativeCode]
        float GetLengthUnitsPerMeter() => m_LengthUnitsPerMeter;

        /// <undoc/>
        [RequiredByNativeCode]
        bool GetDrawInBuild() => m_DrawInBuild;

        /// <undoc/>
        [RequiredByNativeCode]
        bool GetBypassLowLevel() => m_BypassLowLevel;

        /// <undoc/>
        [RequiredByNativeCode]
        bool GetUseFullLayers() => m_UseFullLayers;

        #endregion
    }
}
