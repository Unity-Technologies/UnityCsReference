// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;

namespace Unity.U2D.Physics.Editor
{
    /// <summary>
    /// The relative joint definition: the velocity drive loose, then its spring.
    /// The solver runs the two as separate branches, and the spring's own maximum force and torque only act within that branch, so they sit inside the spring group.
    /// </summary>
    [CustomPropertyDrawer(typeof(PhysicsRelativeJointDefinition))]
    sealed class PhysicsRelativeJointDefinitionPropertyDrawer : PhysicsJointDefinitionPropertyDrawer
    {
        protected override Group[] groups
        {
            get
            {
                if (m_Groups == null)
                {
                    m_Groups = Order(
                        new Group(null, null, k_LinearVelocity, k_AngularVelocity, k_MaxForce, k_MaxTorque),
                        new Group(null, null, k_CollideConnected),
                        Group.Anchors(k_LocalAnchorA, k_LocalAnchorB),
                        new Group(k_SpringTitle, null, k_SpringLinearFrequency, k_SpringAngularFrequency, k_SpringLinearDamping, k_SpringAngularDamping, k_SpringMaxForce, k_SpringMaxTorque),
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

        const string k_LinearVelocity = nameof(PhysicsRelativeJointDefinition.m_LinearVelocity);
        const string k_AngularVelocity = nameof(PhysicsRelativeJointDefinition.m_AngularVelocity);
        const string k_MaxForce = nameof(PhysicsRelativeJointDefinition.m_MaxForce);
        const string k_MaxTorque = nameof(PhysicsRelativeJointDefinition.m_MaxTorque);
        const string k_CollideConnected = nameof(PhysicsRelativeJointDefinition.m_CollideConnected);
        const string k_LocalAnchorA = nameof(PhysicsRelativeJointDefinition.m_LocalAnchorA);
        const string k_LocalAnchorB = nameof(PhysicsRelativeJointDefinition.m_LocalAnchorB);
        const string k_AutoAnchorA = nameof(PhysicsRelativeJointDefinition.m_AutoAnchorA);
        const string k_AutoAnchorB = nameof(PhysicsRelativeJointDefinition.m_AutoAnchorB);
        const string k_SpringLinearFrequency = nameof(PhysicsRelativeJointDefinition.m_SpringLinearFrequency);
        const string k_SpringAngularFrequency = nameof(PhysicsRelativeJointDefinition.m_SpringAngularFrequency);
        const string k_SpringLinearDamping = nameof(PhysicsRelativeJointDefinition.m_SpringLinearDamping);
        const string k_SpringAngularDamping = nameof(PhysicsRelativeJointDefinition.m_SpringAngularDamping);
        const string k_SpringMaxForce = nameof(PhysicsRelativeJointDefinition.m_SpringMaxForce);
        const string k_SpringMaxTorque = nameof(PhysicsRelativeJointDefinition.m_SpringMaxTorque);
        const string k_ForceThreshold = nameof(PhysicsRelativeJointDefinition.m_ForceThreshold);
        const string k_TorqueThreshold = nameof(PhysicsRelativeJointDefinition.m_TorqueThreshold);
        const string k_TuningFrequency = nameof(PhysicsRelativeJointDefinition.m_TuningFrequency);
        const string k_TuningDamping = nameof(PhysicsRelativeJointDefinition.m_TuningDamping);
        const string k_DrawScale = nameof(PhysicsRelativeJointDefinition.m_DrawScale);
        const string k_WorldDrawing = nameof(PhysicsRelativeJointDefinition.m_WorldDrawing);

        Group[] m_Groups;
    }
}
