// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;

namespace Unity.U2D.Physics.Editor
{
    /// <summary>
    /// The slider joint definition: the auto-axis flag, then the spring, motor and limit blocks.
    /// </summary>
    [CustomPropertyDrawer(typeof(PhysicsSliderJointDefinition))]
    sealed class PhysicsSliderJointDefinitionPropertyDrawer : PhysicsJointDefinitionPropertyDrawer
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
                        new Group(k_SpringTitle, k_EnableSpring, k_SpringTargetTranslation, k_SpringFrequency, k_SpringDamping),
                        new Group(k_MotorTitle, k_EnableMotor, k_MotorSpeed, k_MaxMotorForce),
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

        // The slider bakes the auto axis into both frame rotations (anchor B's is the held relative-rotation reference), so both rotation rows hide while it is set.
        protected override (string autoAxisField, bool usesRotationB)? axisFields
        {
            get { return (k_AutoAxis, true); }
        }

        const string k_AutoAxis = nameof(PhysicsSliderJointDefinition.m_AutoAxis);
        const string k_CollideConnected = nameof(PhysicsSliderJointDefinition.m_CollideConnected);
        const string k_LocalAnchorA = nameof(PhysicsSliderJointDefinition.m_LocalAnchorA);
        const string k_LocalAnchorB = nameof(PhysicsSliderJointDefinition.m_LocalAnchorB);
        const string k_AutoAnchorA = nameof(PhysicsSliderJointDefinition.m_AutoAnchorA);
        const string k_AutoAnchorB = nameof(PhysicsSliderJointDefinition.m_AutoAnchorB);
        const string k_EnableSpring = nameof(PhysicsSliderJointDefinition.m_EnableSpring);
        const string k_SpringTargetTranslation = nameof(PhysicsSliderJointDefinition.m_SpringTargetTranslation);
        const string k_SpringFrequency = nameof(PhysicsSliderJointDefinition.m_SpringFrequency);
        const string k_SpringDamping = nameof(PhysicsSliderJointDefinition.m_SpringDamping);
        const string k_EnableMotor = nameof(PhysicsSliderJointDefinition.m_EnableMotor);
        const string k_MotorSpeed = nameof(PhysicsSliderJointDefinition.m_MotorSpeed);
        const string k_MaxMotorForce = nameof(PhysicsSliderJointDefinition.m_MaxMotorForce);
        const string k_EnableLimit = nameof(PhysicsSliderJointDefinition.m_EnableLimit);
        const string k_LowerTranslationLimit = nameof(PhysicsSliderJointDefinition.m_LowerTranslationLimit);
        const string k_UpperTranslationLimit = nameof(PhysicsSliderJointDefinition.m_UpperTranslationLimit);
        const string k_ForceThreshold = nameof(PhysicsSliderJointDefinition.m_ForceThreshold);
        const string k_TorqueThreshold = nameof(PhysicsSliderJointDefinition.m_TorqueThreshold);
        const string k_TuningFrequency = nameof(PhysicsSliderJointDefinition.m_TuningFrequency);
        const string k_TuningDamping = nameof(PhysicsSliderJointDefinition.m_TuningDamping);
        const string k_DrawScale = nameof(PhysicsSliderJointDefinition.m_DrawScale);
        const string k_WorldDrawing = nameof(PhysicsSliderJointDefinition.m_WorldDrawing);

        Group[] m_Groups;
    }
}
