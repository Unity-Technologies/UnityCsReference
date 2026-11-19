// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;

namespace Unity.U2D.Physics.Editor
{
    /// <summary>
    /// The distance joint definition: the rest distance, then the spring, motor and limit blocks.
    /// </summary>
    [CustomPropertyDrawer(typeof(PhysicsDistanceJointDefinition))]
    sealed class PhysicsDistanceJointDefinitionPropertyDrawer : PhysicsJointDefinitionPropertyDrawer
    {
        protected override Group[] groups
        {
            get
            {
                if (m_Groups == null)
                {
                    m_Groups = Order(
                        Group.HiddenWhenSet(k_AutoDistance, k_Distance),
                        new Group(null, null, k_CollideConnected),
                        Group.Anchors(k_LocalAnchorA, k_LocalAnchorB),
                        new Group(k_SpringTitle, k_EnableSpring, k_SpringFrequency, k_SpringDamping, k_SpringLowerForce, k_SpringUpperForce),
                        new Group(k_MotorTitle, k_EnableMotor, k_MotorSpeed, k_MaxMotorForce),
                        new Group(k_LimitTitle, k_EnableLimit, k_MinDistanceLimit, k_MaxDistanceLimit),
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

        const string k_AutoDistance = nameof(PhysicsDistanceJointDefinition.m_AutoDistance);
        const string k_Distance = nameof(PhysicsDistanceJointDefinition.m_Distance);
        const string k_CollideConnected = nameof(PhysicsDistanceJointDefinition.m_CollideConnected);
        const string k_LocalAnchorA = nameof(PhysicsDistanceJointDefinition.m_LocalAnchorA);
        const string k_LocalAnchorB = nameof(PhysicsDistanceJointDefinition.m_LocalAnchorB);
        const string k_AutoAnchorA = nameof(PhysicsDistanceJointDefinition.m_AutoAnchorA);
        const string k_AutoAnchorB = nameof(PhysicsDistanceJointDefinition.m_AutoAnchorB);
        const string k_EnableSpring = nameof(PhysicsDistanceJointDefinition.m_EnableSpring);
        const string k_SpringFrequency = nameof(PhysicsDistanceJointDefinition.m_SpringFrequency);
        const string k_SpringDamping = nameof(PhysicsDistanceJointDefinition.m_SpringDamping);
        const string k_SpringLowerForce = nameof(PhysicsDistanceJointDefinition.m_SpringLowerForce);
        const string k_SpringUpperForce = nameof(PhysicsDistanceJointDefinition.m_SpringUpperForce);
        const string k_EnableMotor = nameof(PhysicsDistanceJointDefinition.m_EnableMotor);
        const string k_MotorSpeed = nameof(PhysicsDistanceJointDefinition.m_MotorSpeed);
        const string k_MaxMotorForce = nameof(PhysicsDistanceJointDefinition.m_MaxMotorForce);
        const string k_EnableLimit = nameof(PhysicsDistanceJointDefinition.m_EnableLimit);
        const string k_MinDistanceLimit = nameof(PhysicsDistanceJointDefinition.m_MinDistanceLimit);
        const string k_MaxDistanceLimit = nameof(PhysicsDistanceJointDefinition.m_MaxDistanceLimit);
        const string k_ForceThreshold = nameof(PhysicsDistanceJointDefinition.m_ForceThreshold);
        const string k_TorqueThreshold = nameof(PhysicsDistanceJointDefinition.m_TorqueThreshold);
        const string k_TuningFrequency = nameof(PhysicsDistanceJointDefinition.m_TuningFrequency);
        const string k_TuningDamping = nameof(PhysicsDistanceJointDefinition.m_TuningDamping);
        const string k_DrawScale = nameof(PhysicsDistanceJointDefinition.m_DrawScale);
        const string k_WorldDrawing = nameof(PhysicsDistanceJointDefinition.m_WorldDrawing);

        Group[] m_Groups;
    }
}
