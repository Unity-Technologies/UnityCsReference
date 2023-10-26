// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine
{
    //TODO: We should move this type into the VehicleModule assembly when possible.
    // WheelFrictionCurve is used by the WheelCollider to describe friction properties of the wheel tire.
    public struct WheelFrictionCurve
    {
        private float m_ExtremumSlip;
        private float m_ExtremumValue;
        private float m_AsymptoteSlip;
        private float m_AsymptoteValue;
        private float m_Stiffness;

        public float extremumSlip { get { return m_ExtremumSlip; } set { m_ExtremumSlip = value; } }
        public float extremumValue { get { return m_ExtremumValue; } set { m_ExtremumValue = value; } }
        public float asymptoteSlip { get { return m_AsymptoteSlip; } set { m_AsymptoteSlip = value; } }
        public float asymptoteValue { get { return m_AsymptoteValue; } set { m_AsymptoteValue = value; } }
        public float stiffness { get { return m_Stiffness; } set { m_Stiffness = value; } }
    }
}
