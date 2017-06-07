// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.Rendering
{
    // Must match GfxBlendState on C++ side
    [StructLayout(LayoutKind.Sequential)]
    public struct BlendState
    {
        public static BlendState Default
        {
            // Passing a single parameter here to force non-default constructor
            get { return new BlendState(false); }
        }

        public BlendState(bool separateMRTBlend = false, bool alphaToMask = false)
        {
            m_BlendState0 = RenderTargetBlendState.Default;
            m_BlendState1 = RenderTargetBlendState.Default;
            m_BlendState2 = RenderTargetBlendState.Default;
            m_BlendState3 = RenderTargetBlendState.Default;
            m_BlendState4 = RenderTargetBlendState.Default;
            m_BlendState5 = RenderTargetBlendState.Default;
            m_BlendState6 = RenderTargetBlendState.Default;
            m_BlendState7 = RenderTargetBlendState.Default;
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
    }
}
