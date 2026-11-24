// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;

namespace Unity.U2D.Physics.Editor
{
    /// <summary>
    /// The fixed joint definition: two independent stiffness pairs, linear and angular, with no switch to head either, so all four stay loose.
    /// </summary>
    [CustomPropertyDrawer(typeof(PhysicsFixedJointDefinition))]
    sealed class PhysicsFixedJointDefinitionPropertyDrawer : PhysicsJointDefinitionPropertyDrawer
    {
        protected override Group[] groups
        {
            get
            {
                if (m_Groups == null)
                {
                    m_Groups = Order(
                        new Group(null, null, k_LinearFrequency, k_LinearDamping, k_AngularFrequency, k_AngularDamping),
                        new Group(null, null, k_CollideConnected),
                        Group.Anchors(k_LocalAnchorA, k_LocalAnchorB),
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

        const string k_LinearFrequency = nameof(PhysicsFixedJointDefinition.m_LinearFrequency);
        const string k_LinearDamping = nameof(PhysicsFixedJointDefinition.m_LinearDamping);
        const string k_AngularFrequency = nameof(PhysicsFixedJointDefinition.m_AngularFrequency);
        const string k_AngularDamping = nameof(PhysicsFixedJointDefinition.m_AngularDamping);
        const string k_CollideConnected = nameof(PhysicsFixedJointDefinition.m_CollideConnected);
        const string k_LocalAnchorA = nameof(PhysicsFixedJointDefinition.m_LocalAnchorA);
        const string k_LocalAnchorB = nameof(PhysicsFixedJointDefinition.m_LocalAnchorB);
        const string k_AutoAnchorA = nameof(PhysicsFixedJointDefinition.m_AutoAnchorA);
        const string k_AutoAnchorB = nameof(PhysicsFixedJointDefinition.m_AutoAnchorB);
        const string k_ForceThreshold = nameof(PhysicsFixedJointDefinition.m_ForceThreshold);
        const string k_TorqueThreshold = nameof(PhysicsFixedJointDefinition.m_TorqueThreshold);
        const string k_TuningFrequency = nameof(PhysicsFixedJointDefinition.m_TuningFrequency);
        const string k_TuningDamping = nameof(PhysicsFixedJointDefinition.m_TuningDamping);
        const string k_DrawScale = nameof(PhysicsFixedJointDefinition.m_DrawScale);
        const string k_WorldDrawing = nameof(PhysicsFixedJointDefinition.m_WorldDrawing);

        Group[] m_Groups;
    }
}
