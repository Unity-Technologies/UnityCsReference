// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;

namespace Unity.U2D.Physics.Editor
{
    /// <summary>
    /// The hinge joint definition: the unpinned switch, then the spring, motor and limit blocks.
    /// </summary>
    [CustomPropertyDrawer(typeof(PhysicsHingeJointDefinition))]
    sealed class PhysicsHingeJointDefinitionPropertyDrawer : PhysicsJointDefinitionPropertyDrawer
    {
        protected override Group[] groups
        {
            get
            {
                if (m_Groups == null)
                {
                    m_Groups = Order(
                        new Group(null, null, k_EnableUnpinned),
                        new Group(null, null, k_CollideConnected),
                        Group.Anchors(k_LocalAnchorA, k_LocalAnchorB),
                        new Group(k_SpringTitle, k_EnableSpring, k_SpringTargetAngle, k_SpringFrequency, k_SpringDamping),
                        new Group(k_MotorTitle, k_EnableMotor, k_MotorSpeed, k_MaxMotorTorque),
                        new Group(k_LimitTitle, k_EnableLimit, k_LowerAngleLimit, k_UpperAngleLimit),
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

        const string k_EnableUnpinned = nameof(PhysicsHingeJointDefinition.m_EnableUnpinned);
        const string k_CollideConnected = nameof(PhysicsHingeJointDefinition.m_CollideConnected);
        const string k_LocalAnchorA = nameof(PhysicsHingeJointDefinition.m_LocalAnchorA);
        const string k_LocalAnchorB = nameof(PhysicsHingeJointDefinition.m_LocalAnchorB);
        const string k_AutoAnchorA = nameof(PhysicsHingeJointDefinition.m_AutoAnchorA);
        const string k_AutoAnchorB = nameof(PhysicsHingeJointDefinition.m_AutoAnchorB);
        const string k_EnableSpring = nameof(PhysicsHingeJointDefinition.m_EnableSpring);
        const string k_SpringTargetAngle = nameof(PhysicsHingeJointDefinition.m_SpringTargetAngle);
        const string k_SpringFrequency = nameof(PhysicsHingeJointDefinition.m_SpringFrequency);
        const string k_SpringDamping = nameof(PhysicsHingeJointDefinition.m_SpringDamping);
        const string k_EnableMotor = nameof(PhysicsHingeJointDefinition.m_EnableMotor);
        const string k_MotorSpeed = nameof(PhysicsHingeJointDefinition.m_MotorSpeed);
        const string k_MaxMotorTorque = nameof(PhysicsHingeJointDefinition.m_MaxMotorTorque);
        const string k_EnableLimit = nameof(PhysicsHingeJointDefinition.m_EnableLimit);
        const string k_LowerAngleLimit = nameof(PhysicsHingeJointDefinition.m_LowerAngleLimit);
        const string k_UpperAngleLimit = nameof(PhysicsHingeJointDefinition.m_UpperAngleLimit);
        const string k_ForceThreshold = nameof(PhysicsHingeJointDefinition.m_ForceThreshold);
        const string k_TorqueThreshold = nameof(PhysicsHingeJointDefinition.m_TorqueThreshold);
        const string k_TuningFrequency = nameof(PhysicsHingeJointDefinition.m_TuningFrequency);
        const string k_TuningDamping = nameof(PhysicsHingeJointDefinition.m_TuningDamping);
        const string k_DrawScale = nameof(PhysicsHingeJointDefinition.m_DrawScale);
        const string k_WorldDrawing = nameof(PhysicsHingeJointDefinition.m_WorldDrawing);

        Group[] m_Groups;
    }
}
