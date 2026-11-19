// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;

namespace Unity.U2D.Physics.Editor
{
    /// <summary>
    /// The wheel joint definition: the same shape as the slider, with a spring that has no target.
    /// </summary>
    [CustomPropertyDrawer(typeof(PhysicsWheelJointDefinition))]
    sealed class PhysicsWheelJointDefinitionPropertyDrawer : PhysicsJointDefinitionPropertyDrawer
    {
        protected override Group[] groups
        {
            get
            {
                if (m_Groups == null)
                {
                    m_Groups = Order(
                        new Group(null, null, k_AutoAxis),
                        new Group(null, null, k_CollideConnected),
                        Group.Anchors(k_LocalAnchorA, k_LocalAnchorB),
                        new Group(k_SpringTitle, k_EnableSpring, k_SpringFrequency, k_SpringDamping),
                        new Group(k_MotorTitle, k_EnableMotor, k_MotorSpeed, k_MaxMotorTorque),
                        new Group(k_LimitTitle, k_EnableLimit, k_LowerTranslationLimit, k_UpperTranslationLimit),
                        new Group(k_ThresholdsTitle, null, k_ForceThreshold, k_TorqueThreshold),
                        new Group(k_TuningTitle, null, k_TuningFrequency, k_TuningDamping),
                        new Group(k_DrawingTitle, null, k_DrawScale, k_WorldDrawing));
                }

                return m_Groups;
            }
        }

        protected override (string localAnchorA, string autoAnchorA, string localAnchorB, string autoAnchorB)? anchorFields
        {
            get { return (k_LocalAnchorA, k_AutoAnchorA, k_LocalAnchorB, k_AutoAnchorB); }
        }

        // The wheel bakes the auto axis into frame A alone, and the solver never reads anchor B's rotation at all, so that row never shows.
        protected override (string autoAxisField, bool usesRotationB)? axisFields
        {
            get { return (k_AutoAxis, false); }
        }

        const string k_AutoAxis = nameof(PhysicsWheelJointDefinition.m_AutoAxis);
        const string k_CollideConnected = nameof(PhysicsWheelJointDefinition.m_CollideConnected);
        const string k_LocalAnchorA = nameof(PhysicsWheelJointDefinition.m_LocalAnchorA);
        const string k_LocalAnchorB = nameof(PhysicsWheelJointDefinition.m_LocalAnchorB);
        const string k_AutoAnchorA = nameof(PhysicsWheelJointDefinition.m_AutoAnchorA);
        const string k_AutoAnchorB = nameof(PhysicsWheelJointDefinition.m_AutoAnchorB);
        const string k_EnableSpring = nameof(PhysicsWheelJointDefinition.m_EnableSpring);
        const string k_SpringFrequency = nameof(PhysicsWheelJointDefinition.m_SpringFrequency);
        const string k_SpringDamping = nameof(PhysicsWheelJointDefinition.m_SpringDamping);
        const string k_EnableMotor = nameof(PhysicsWheelJointDefinition.m_EnableMotor);
        const string k_MotorSpeed = nameof(PhysicsWheelJointDefinition.m_MotorSpeed);
        const string k_MaxMotorTorque = nameof(PhysicsWheelJointDefinition.m_MaxMotorTorque);
        const string k_EnableLimit = nameof(PhysicsWheelJointDefinition.m_EnableLimit);
        const string k_LowerTranslationLimit = nameof(PhysicsWheelJointDefinition.m_LowerTranslationLimit);
        const string k_UpperTranslationLimit = nameof(PhysicsWheelJointDefinition.m_UpperTranslationLimit);
        const string k_ForceThreshold = nameof(PhysicsWheelJointDefinition.m_ForceThreshold);
        const string k_TorqueThreshold = nameof(PhysicsWheelJointDefinition.m_TorqueThreshold);
        const string k_TuningFrequency = nameof(PhysicsWheelJointDefinition.m_TuningFrequency);
        const string k_TuningDamping = nameof(PhysicsWheelJointDefinition.m_TuningDamping);
        const string k_DrawScale = nameof(PhysicsWheelJointDefinition.m_DrawScale);
        const string k_WorldDrawing = nameof(PhysicsWheelJointDefinition.m_WorldDrawing);

        Group[] m_Groups;
    }
}
