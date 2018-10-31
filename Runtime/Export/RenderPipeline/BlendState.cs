// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering
{
    // Must match GfxBlendState on C++ side
    [StructLayout(LayoutKind.Sequential)]
    public struct BlendState : IEquatable<BlendState>
    {
        public static BlendState defaultValue
        {
            // Passing a single parameter here to force non-default constructor
            get { return new BlendState(false); }
        }

        public BlendState(bool separateMRTBlend = false, bool alphaToMask = false)
        {
            m_BlendState0 = RenderTargetBlendState.defaultValue;
            m_BlendState1 = RenderTargetBlendState.defaultValue;
            m_BlendState2 = RenderTargetBlendState.defaultValue;
            m_BlendState3 = RenderTargetBlendState.defaultValue;
            m_BlendState4 = RenderTargetBlendState.defaultValue;
            m_BlendState5 = RenderTargetBlendState.defaultValue;
            m_BlendState6 = RenderTargetBlendState.defaultValue;
            m_BlendState7 = RenderTargetBlendState.defaultValue;
            m_SeparateMRTBlendStates = Convert.ToByte(separateMRTBlend);
            m_AlphaToMask = Convert.ToByte(alphaToMask);
            m_Padding = 0;
        }

        public bool separateMRTBlendStates
        {
            get { return Convert.ToBoolean(m_SeparateMRTBlendStates); }
            set { m_SeparateMRTBlendStates = Convert.ToByte(value); }
        }

        public bool alphaToMask
        {
            get { return Convert.ToBoolean(m_AlphaToMask); }
            set { m_AlphaToMask = Convert.ToByte(value); }
        }

        public RenderTargetBlendState blendState0
        {
            get { return m_BlendState0; }
            set { m_BlendState0 = value; }
        }

        public RenderTargetBlendState blendState1
        {
            get { return m_BlendState1; }
            set { m_BlendState1 = value; }
        }

        public RenderTargetBlendState blendState2
        {
            get { return m_BlendState2; }
            set { m_BlendState2 = value; }
        }

        public RenderTargetBlendState blendState3
        {
            get { return m_BlendState3; }
            set { m_BlendState3 = value; }
        }

        public RenderTargetBlendState blendState4
        {
            get { return m_BlendState4; }
            set { m_BlendState4 = value; }
        }

        public RenderTargetBlendState blendState5
        {
            get { return m_BlendState5; }
            set { m_BlendState5 = value; }
        }

        public RenderTargetBlendState blendState6
        {
            get { return m_BlendState6; }
            set { m_BlendState6 = value; }
        }

        public RenderTargetBlendState blendState7
        {
            get { return m_BlendState7; }
            set { m_BlendState7 = value; }
        }

        RenderTargetBlendState m_BlendState0;
        RenderTargetBlendState m_BlendState1;
        RenderTargetBlendState m_BlendState2;
        RenderTargetBlendState m_BlendState3;
        RenderTargetBlendState m_BlendState4;
        RenderTargetBlendState m_BlendState5;
        RenderTargetBlendState m_BlendState6;
        RenderTargetBlendState m_BlendState7;
        byte m_SeparateMRTBlendStates;
        byte m_AlphaToMask;
        short m_Padding;

        public bool Equals(BlendState other)
        {
            return m_BlendState0.Equals(other.m_BlendState0) && m_BlendState1.Equals(other.m_BlendState1) && m_BlendState2.Equals(other.m_BlendState2) && m_BlendState3.Equals(other.m_BlendState3) && m_BlendState4.Equals(other.m_BlendState4) && m_BlendState5.Equals(other.m_BlendState5) && m_BlendState6.Equals(other.m_BlendState6) && m_BlendState7.Equals(other.m_BlendState7) && m_SeparateMRTBlendStates == other.m_SeparateMRTBlendStates && m_AlphaToMask == other.m_AlphaToMask;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is BlendState && Equals((BlendState)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = m_BlendState0.GetHashCode();
                hashCode = (hashCode * 397) ^ m_BlendState1.GetHashCode();
                hashCode = (hashCode * 397) ^ m_BlendState2.GetHashCode();
                hashCode = (hashCode * 397) ^ m_BlendState3.GetHashCode();
                hashCode = (hashCode * 397) ^ m_BlendState4.GetHashCode();
                hashCode = (hashCode * 397) ^ m_BlendState5.GetHashCode();
                hashCode = (hashCode * 397) ^ m_BlendState6.GetHashCode();
                hashCode = (hashCode * 397) ^ m_BlendState7.GetHashCode();
                hashCode = (hashCode * 397) ^ m_SeparateMRTBlendStates.GetHashCode();
                hashCode = (hashCode * 397) ^ m_AlphaToMask.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator==(BlendState left, BlendState right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(BlendState left, BlendState right)
        {
            return !left.Equals(right);
        }
    }
}
