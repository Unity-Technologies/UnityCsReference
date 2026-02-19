// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using UnityEngine.Internal;
using Unity.U2D.Physics;

namespace UnityEngine.LowLevelPhysics2D
{
    /// <undoc/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [ExcludeFromDocs]
    [Serializable]
    public sealed class PhysicsLowLevelSettings2D : ScriptableObject
    {
        public PhysicsLayers.LayerNames physicsLayerNames { get => m_PhysicsLayerNames; set => m_PhysicsLayerNames = value; }
        public bool useFullLayers { get => m_UseFullLayers; set => m_UseFullLayers = value; }
        public PhysicsWorldDefinition physicsWorldDefinition { get => m_PhysicsWorldDefinition; set => m_PhysicsWorldDefinition = value; }
        public PhysicsBodyDefinition physicsBodyDefinition { get => m_PhysicsBodyDefinition; set => m_PhysicsBodyDefinition = value; }
        public PhysicsShapeDefinition physicsShapeDefinition { get => m_PhysicsShapeDefinition; set => m_PhysicsShapeDefinition = value; }
        public PhysicsChainDefinition physicsChainDefinition { get => m_PhysicsChainDefinition; set => m_PhysicsChainDefinition = value; }
        internal PhysicsDistanceJointDefinition physicsDistanceJointDefinition { get => m_PhysicsDistanceJointDefinition; set => m_PhysicsDistanceJointDefinition = value; }
        internal PhysicsFixedJointDefinition physicsFixedJointDefinition { get => m_PhysicsFixedJointDefinition; set => m_PhysicsFixedJointDefinition = value; }
        internal PhysicsHingeJointDefinition physicsHingeJointDefinition { get => m_PhysicsHingeJointDefinition; set => m_PhysicsHingeJointDefinition = value; }
        internal PhysicsRelativeJointDefinition physicsRelativeJointDefinition { get => m_PhysicsRelativeJointDefinition; set => m_PhysicsRelativeJointDefinition = value; }
        internal PhysicsSliderJointDefinition physicsSliderJointDefinition { get => m_PhysicsSliderJointDefinition; set => m_PhysicsSliderJointDefinition = value; }
        internal PhysicsWheelJointDefinition physicsWheelJointDefinition { get => m_PhysicsWheelJointDefinition; set => m_PhysicsWheelJointDefinition = value; }
        [Range(1, PhysicsConstants.MaxWorkers)]
        public int concurrentSimulations { get => m_ConcurrentSimulations; set => m_ConcurrentSimulations = Mathf.Clamp(value, 1, PhysicsConstants.MaxWorkers); }
        public float lengthUnitsPerMeter { get => m_LengthUnitsPerMeter; set => m_LengthUnitsPerMeter = Mathf.Max(0.00001f, value); }
        public bool drawInBuild { get => m_DrawInBuild; set => m_DrawInBuild = value; }
        public bool bypassLowLevel { get => m_BypassLowLevel; set => m_BypassLowLevel = value; }

        #region Internal

        [SerializeField][Header("Layers")] PhysicsLayers.LayerNames m_PhysicsLayerNames;
        [SerializeField] bool m_UseFullLayers;
        [SerializeField][Header("Default Definitions")] PhysicsWorldDefinition m_PhysicsWorldDefinition;
        [SerializeField] PhysicsBodyDefinition m_PhysicsBodyDefinition;
        [SerializeField] PhysicsShapeDefinition m_PhysicsShapeDefinition;
        [SerializeField] PhysicsChainDefinition m_PhysicsChainDefinition;
        [SerializeField] PhysicsDistanceJointDefinition m_PhysicsDistanceJointDefinition;
        [SerializeField] PhysicsFixedJointDefinition m_PhysicsFixedJointDefinition;
        [SerializeField] PhysicsHingeJointDefinition m_PhysicsHingeJointDefinition;
        [SerializeField] PhysicsRelativeJointDefinition m_PhysicsRelativeJointDefinition;
        [SerializeField] PhysicsSliderJointDefinition m_PhysicsSliderJointDefinition;
        [SerializeField] PhysicsWheelJointDefinition m_PhysicsWheelJointDefinition;
        [SerializeField][Range(1, PhysicsConstants.MaxWorkers)][Header("Globals")] int m_ConcurrentSimulations;
        [SerializeField][Min(0.00001f)] float m_LengthUnitsPerMeter;
        [SerializeField] bool m_DrawInBuild;
        [SerializeField] bool m_BypassLowLevel;

        #endregion

        #region Legacy

        internal PhysicsCoreSettings2D ToPhysicsCoreSettings()
        {
            var physicsCoreSettings2D = CreateInstance<PhysicsCoreSettings2D>();

            physicsCoreSettings2D.physicsLayerNames = physicsLayerNames;
            physicsCoreSettings2D.usePhysicsLayers = useFullLayers;
            physicsCoreSettings2D.physicsWorldDefinition = physicsWorldDefinition;
            physicsCoreSettings2D.physicsBodyDefinition = physicsBodyDefinition;
            physicsCoreSettings2D.physicsShapeDefinition = physicsShapeDefinition;
            physicsCoreSettings2D.physicsChainDefinition = physicsChainDefinition;
            physicsCoreSettings2D.physicsDistanceJointDefinition = physicsDistanceJointDefinition;
            physicsCoreSettings2D.physicsFixedJointDefinition = physicsFixedJointDefinition;
            physicsCoreSettings2D.physicsHingeJointDefinition = physicsHingeJointDefinition;
            physicsCoreSettings2D.physicsRelativeJointDefinition = physicsRelativeJointDefinition;
            physicsCoreSettings2D.physicsSliderJointDefinition = physicsSliderJointDefinition;
            physicsCoreSettings2D.physicsWheelJointDefinition = physicsWheelJointDefinition;
            physicsCoreSettings2D.concurrentSimulations = concurrentSimulations;
            physicsCoreSettings2D.lengthUnitsPerMeter = lengthUnitsPerMeter;
            physicsCoreSettings2D.renderingMode = drawInBuild ? PhysicsWorld.RenderingMode.DevelopmentPlayer : PhysicsWorld.RenderingMode.EditorOnly;
            physicsCoreSettings2D.disableSimulation = bypassLowLevel;
            physicsCoreSettings2D.alwaysDrawWorlds = false;

            return physicsCoreSettings2D;
        }

        #endregion
    }
}
