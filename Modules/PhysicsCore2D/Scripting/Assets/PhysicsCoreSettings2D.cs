// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace Unity.U2D.Physics
{
    /// <summary>
    /// PhysicsCore Settings Asset.
    /// This contains all the global physics options along with the default values for the following definitions:
    ///
    ///- <see cref="PhysicsWorldDefinition"/>
    ///- <see cref="PhysicsBodyDefinition"/>
    ///- <see cref="PhysicsShapeDefinition"/>
    ///- <see cref="PhysicsChainDefinition"/>
    ///- <see cref="PhysicsDistanceJointDefinition"/>
    ///- <see cref="PhysicsFixedJointDefinition"/>
    ///- <see cref="PhysicsHingeJointDefinition"/>
    ///- <see cref="PhysicsRelativeJointDefinition"/>
    ///- <see cref="PhysicsSliderJointDefinition"/>
    ///- <see cref="PhysicsWheelJointDefinition"/>
    ///
    /// </summary>
    [RequiredByNativeCode]
    [Serializable]
    public sealed class PhysicsCoreSettings2D : ScriptableObject
    {
        /// <undoc/>
        public PhysicsCoreSettings2D()
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

            m_RenderingMode = PhysicsWorld.RenderingMode.EditorOnly;
            m_TransformChangeMode = PhysicsWorld.TransformChangeMode.FixedUpdate;
            m_ContactFilterMode = PhysicsShape.ContactFilterMode.Both;
            m_PhysicsLayerNames = PhysicsLayers.LayerNames.DefaultLayerNames;
            m_MaximumWorlds = 128;
            m_ConcurrentSimulations = 2;
            m_LengthUnitsPerMeter = 1.0f;
            m_DisableSimulation = false;
            m_AlwaysDrawWorlds = false;
        }

        void OnValidate()
        {
            if (PhysicsEditorOnly.physicsSettings == this)
                PhysicsEditorOnly.ReadProjectSettings();
        }

        /// <summary>
        /// A set of 64 "layer" names associated with each bit in a <see cref="PhysicsMask"/> when used for contacts and queries.
        /// </summary>
        public PhysicsLayers.LayerNames physicsLayerNames { get => m_PhysicsLayerNames; set => m_PhysicsLayerNames = value; }

        /// <summary>
        /// Controls if the physics 64-bit layers are used based upon <see cref="PhysicsCoreSettings2D.physicsLayerNames"/> or if not, the standard 32-bit layers based upon <see cref="UnityEngine.LayerMask"/>.
        /// If a <see cref="PhysicsCoreSettings2D"/> asset is assigned then the physics layers (<see cref="PhysicsCoreSettings2D.physicsLayerNames"/>) will be used if <see cref="PhysicsCoreSettings2D.usePhysicsLayers"/> is also active.
        /// If no <see cref="PhysicsCoreSettings2D"/> asset is assigned then the global layers (See <see cref="UnityEngine.LayerMask"/>) will be used.
        /// </summary>
        public bool usePhysicsLayers { get => m_UsePhysicsLayers; set => m_UsePhysicsLayers = value; }

        /// <summary>
        /// Get/Set the <see cref="PhysicsWorldDefinition"/>.
        /// </summary>        
        public PhysicsWorldDefinition physicsWorldDefinition { get => m_PhysicsWorldDefinition; set => m_PhysicsWorldDefinition = value; }

        /// <summary>
        /// Get/Set the <see cref="PhysicsBodyDefinition"/>.
        /// </summary>
        public PhysicsBodyDefinition physicsBodyDefinition { get => m_PhysicsBodyDefinition; set => m_PhysicsBodyDefinition = value; }

        /// <summary>
        /// Get/Set the <see cref="PhysicsShapeDefinition"/>.
        /// </summary>
        public PhysicsShapeDefinition physicsShapeDefinition { get => m_PhysicsShapeDefinition; set => m_PhysicsShapeDefinition = value; }

        /// <summary>
        /// Get/Set the <see cref="PhysicsChainDefinition"/>.
        /// </summary>
        public PhysicsChainDefinition physicsChainDefinition { get => m_PhysicsChainDefinition; set => m_PhysicsChainDefinition = value; }

        /// <summary>
        /// Get/Set the <see cref="PhysicsDistanceJointDefinition"/>.
        /// </summary>
        public PhysicsDistanceJointDefinition physicsDistanceJointDefinition { get => m_PhysicsDistanceJointDefinition; set => m_PhysicsDistanceJointDefinition = value; }

        /// <summary>
        /// Get/Set the <see cref="PhysicsFixedJointDefinition"/>.
        /// </summary>
        public PhysicsFixedJointDefinition physicsFixedJointDefinition { get => m_PhysicsFixedJointDefinition; set => m_PhysicsFixedJointDefinition = value; }

        /// <summary>
        /// Get/Set the <see cref="PhysicsHingeJointDefinition"/>.
        /// </summary>
        public PhysicsHingeJointDefinition physicsHingeJointDefinition { get => m_PhysicsHingeJointDefinition; set => m_PhysicsHingeJointDefinition = value; }

        /// <summary>
        /// Get/Set the <see cref="PhysicsRelativeJointDefinition"/>.
        /// </summary>
        public PhysicsRelativeJointDefinition physicsRelativeJointDefinition { get => m_PhysicsRelativeJointDefinition; set => m_PhysicsRelativeJointDefinition = value; }

        /// <summary>
        /// Get/Set the <see cref="PhysicsSliderJointDefinition"/>.
        /// </summary>
        public PhysicsSliderJointDefinition physicsSliderJointDefinition { get => m_PhysicsSliderJointDefinition; set => m_PhysicsSliderJointDefinition = value; }

        /// <summary>
        /// Get/Set the <see cref="PhysicsWheelJointDefinition"/>.
        /// </summary>
        public PhysicsWheelJointDefinition physicsWheelJointDefinition { get => m_PhysicsWheelJointDefinition; set => m_PhysicsWheelJointDefinition = value; }

        /// <summary>
        /// Defines when changes to <see cref="UnityEngine.Transform"/> that has are registered with <see cref="PhysicsWorld.RegisterTransformChange(Transform, PhysicsCallbacks.ITransformChangedCallback)"/> are called.
        /// NOTE: In the Unity Editor when not in Play Mode, Transform change callbacks are always and only sent at the start of the frame for authoring purposes.
        /// See <see cref="PhysicsWorld.TransformChangeMode"/>.
        /// </summary>
        public PhysicsWorld.TransformChangeMode transformChangeMode {  get => m_TransformChangeMode; set => m_TransformChangeMode = value; }

        /// <summary>
        /// The mode used for the <see cref="PhysicsShape.ContactFilter"/> when determining if two <see cref="PhysicsShape"/> can contact.
        /// See <see cref="PhysicsShape.ContactFilterMode"/>.
        /// </summary>
        public PhysicsShape.ContactFilterMode contactFilterMode { get => m_ContactFilterMode; set => m_ContactFilterMode = value; }

        /// <summary>
        /// Get/Set the maximum number of worlds that can be created.
        /// The larger the number of worlds, the more memory that is initially allocated so care must be taken.
        /// Setting this value to one will reduce start-up memory usage to a minimum but will not allow any additional worlds to be created.
        /// The maximum value must be in the range of 1 to 1024.
        /// Any change will only be handled by Exiting Play mode in the Editor or restarting the player build.
        /// 
        /// A single <see cref="PhysicsWorld.defaultWorld"/> is automatically created therefore occupies one of the available worlds.
        /// </summary>
        public int maximumWorlds { get => m_MaximumWorlds; set => m_MaximumWorlds = Mathf.Clamp(value, 1, 1024); }

        /// <summary>
        /// Controls how many simulations can be started in parallel.
        /// Each one is started on its own worker and acts as its own main-thread.
        /// Workers should ideally be left free for the solver otherwise it may degrade solving performance.
        /// The actual quantity of workers used will always be capped to those available on the current device.
        ///
        /// If the total number of workers available is below 4 then parallel simulation won't occur however parallel solving using workers will.
        /// This should not be confused with the quantity of workers used when solving a simulation.
        /// See <see cref="PhysicsWorldDefinition.simulationWorkers"/>.
        /// </summary>        
        [Range(1, PhysicsConstants.MaxWorkers)]
        public int concurrentSimulations { get => m_ConcurrentSimulations; set => m_ConcurrentSimulations = Mathf.Clamp(value, 1, PhysicsConstants.MaxWorkers); }

        /// <summary>
        /// The internal length units per meter.
        /// 
        /// The physics system relates all length units on meters but you may need different units for your project.
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
        /// Controls drawing and rendering is allowed.
        /// 
        /// NOTE: Drawing and rendering are always available in the Unity Editor however rendering requires compute buffer support on any device it is used without which no rendering will occur.
        /// See <see cref="PhysicsWorld.RenderingMode"/>.
        /// </summary>
        public PhysicsWorld.RenderingMode renderingMode { get => m_RenderingMode; set => m_RenderingMode = value; }

        /// <summary>
        /// Controls the simulation of any <see cref="PhysicsWorld"/> temporarily removing simulation overhead.
        /// When true, no automatic simulation will occur.
        /// When false, normal operation occurs with automatic simulation.
        /// </summary>
        public bool disableSimulation { get => m_DisableSimulation; set => m_DisableSimulation = value; }

        /// <summary>
        /// Controls if worlds are always drawn independent of whether rendering is currently active or not as specified by <see cref="PhysicsWorld.renderingMode"/>.
        /// When true, world drawing is always active and a <see cref="PhysicsEvents.WorldDrawResults"/> event is produced containing the <see cref="PhysicsWorld.DrawResults"/>.
        /// When false, world drawing only occurs depending on the <see cref="PhysicsWorld.renderingMode"/> setting.
        ///
        /// CAUTION: Drawing the world has a performance cost associated with it therefore when using this without rendering, that cost can become hidden.
        /// </summary>
        public bool alwaysDrawWorlds { get => m_AlwaysDrawWorlds; set => m_AlwaysDrawWorlds = value; }

        #region Internal

        [SerializeField] internal bool m_UsePhysicsLayers;
        [SerializeField] internal PhysicsLayers.LayerNames m_PhysicsLayerNames;
        [SerializeField] internal PhysicsWorldDefinition m_PhysicsWorldDefinition;
        [SerializeField] internal PhysicsBodyDefinition m_PhysicsBodyDefinition;
        [SerializeField] internal PhysicsShapeDefinition m_PhysicsShapeDefinition;
        [SerializeField] internal PhysicsChainDefinition m_PhysicsChainDefinition;
        [SerializeField] internal PhysicsDistanceJointDefinition m_PhysicsDistanceJointDefinition;
        [SerializeField] internal PhysicsFixedJointDefinition m_PhysicsFixedJointDefinition;
        [SerializeField] internal PhysicsHingeJointDefinition m_PhysicsHingeJointDefinition;
        [SerializeField] internal PhysicsRelativeJointDefinition m_PhysicsRelativeJointDefinition;
        [SerializeField] internal PhysicsSliderJointDefinition m_PhysicsSliderJointDefinition;
        [SerializeField] internal PhysicsWheelJointDefinition m_PhysicsWheelJointDefinition;
        [SerializeField] internal PhysicsWorld.TransformChangeMode m_TransformChangeMode;
        [SerializeField] internal PhysicsShape.ContactFilterMode m_ContactFilterMode;
        [SerializeField][Range(1, 1024)] internal int m_MaximumWorlds;
        [SerializeField][Range(1, PhysicsConstants.MaxWorkers)] internal int m_ConcurrentSimulations;
        [SerializeField][Range(0.00001f, 10000.0f)] internal float m_LengthUnitsPerMeter;
        [SerializeField] internal PhysicsWorld.RenderingMode m_RenderingMode;
        [SerializeField] internal bool m_DisableSimulation;
        [SerializeField] internal bool m_AlwaysDrawWorlds;

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
        PhysicsWorld.TransformChangeMode GetTransformChangeMode() => m_TransformChangeMode;

        /// <undoc/>
        [RequiredByNativeCode]
        PhysicsShape.ContactFilterMode GetContactFilterMode() => m_ContactFilterMode;

        /// <undoc/>
        [RequiredByNativeCode]
        int GetMaximumWorlds() => m_MaximumWorlds;
        
        /// <undoc/>
        [RequiredByNativeCode]
        int GetConcurrentSimulations() => m_ConcurrentSimulations;

        /// <undoc/>
        [RequiredByNativeCode]
        float GetLengthUnitsPerMeter() => m_LengthUnitsPerMeter;

        /// <undoc/>
        [RequiredByNativeCode]
        PhysicsWorld.RenderingMode GetRenderingMode() => m_RenderingMode;

        /// <undoc/>
        [RequiredByNativeCode]
        bool GetDisableSimulation() => m_DisableSimulation;

        /// <undoc/>
        [RequiredByNativeCode]
        bool GetAlwaysDrawWorlds() => m_AlwaysDrawWorlds;

        /// <undoc/>
        [RequiredByNativeCode]
        bool GetUsePhysicsLayers() => m_UsePhysicsLayers;

        #endregion
    }
}
