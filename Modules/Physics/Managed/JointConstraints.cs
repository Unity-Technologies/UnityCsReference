// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;

namespace UnityEngine
{
    // The definition of limits that can be applied to either ConfigurableJoint or CharacterJoint
    public partial struct SoftJointLimit
    {
        private float m_Limit;
        private float m_Bounciness;
        private float m_ContactDistance;

        public float limit { get { return m_Limit; } set { m_Limit = value; } }
        public float bounciness { get { return m_Bounciness; } set { m_Bounciness = value; } }
        public float contactDistance { get { return m_ContactDistance; } set { m_ContactDistance = value; } }
    }

    public struct SoftJointLimitSpring
    {
        private float m_Spring;
        private float m_Damper;

        public float spring { get { return m_Spring; } set { m_Spring = value; } }
        public float damper { get { return m_Damper; } set { m_Damper = value; } }
    }

    // How the joint's movement will behave along a local axis
    public partial struct JointDrive
    {
        private float m_PositionSpring;
        private float m_PositionDamper;
        private float m_MaximumForce;
        private int m_UseAcceleration;

        public float positionSpring { get { return m_PositionSpring; } set { m_PositionSpring = value; } }
        public float positionDamper { get { return m_PositionDamper; } set { m_PositionDamper = value; } }
        public float maximumForce { get { return m_MaximumForce; } set { m_MaximumForce = value; } }
        public bool useAcceleration { get { return m_UseAcceleration == 1; } set { m_UseAcceleration = value ? 1 : 0; } }
    }

    // The JointMotor is used to motorize a joint.
    public struct JointMotor
    {
        private float m_TargetVelocity;
        private float m_Force;
        private int m_FreeSpin;

        public float targetVelocity { get { return m_TargetVelocity; } set { m_TargetVelocity = value; } }
        public float force { get { return m_Force; } set { m_Force = value; } }
        public bool freeSpin { get { return m_FreeSpin == 1; } set { m_FreeSpin = value ? 1 : 0; } }
    }

    // JointSpring is used add a spring force to HingeJoint.
    public struct JointSpring
    {
        public float spring;
        public float damper;
        public float targetPosition;

        // We have to keep those as public variables because of a bug in the C# raycast sample.
    }

    public struct JointLimits
    {
        private float m_Min;
        private float m_Max;
        private float m_Bounciness;
        private float m_BounceMinVelocity;
        private float m_ContactDistance;

        public float min { get { return m_Min; } set { m_Min = value; } }
        public float max { get { return m_Max; } set { m_Max = value; } }
        public float bounciness { get { return m_Bounciness; } set { m_Bounciness = value; } }
        public float bounceMinVelocity { get { return m_BounceMinVelocity; } set { m_BounceMinVelocity = value; } }
        public float contactDistance { get { return m_ContactDistance; } set { m_ContactDistance = value; } }

        // NB - member fields can't be in other partial structs, so we cannot move this out; work out a plan to remove them then
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("minBounce and maxBounce are replaced by a single JointLimits.bounciness for both limit ends.", true)]
        public float minBounce;

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("minBounce and maxBounce are replaced by a single JointLimits.bounciness for both limit ends.", true)]
        public float maxBounce;
    }
}
