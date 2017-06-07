// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    // Must match GfxRenderTargetBlendState on C++ side
    [StructLayout(LayoutKind.Sequential)]
    public struct RenderTargetBlendState
    {
        public static RenderTargetBlendState Default
        {
            // Passing a single parameter here to force non-default constructor
            get { return new RenderTargetBlendState(ColorWriteMask.All); }
        }

        public RenderTargetBlendState(
            ColorWriteMask writeMask = ColorWriteMask.All,
            BlendMode sourceColorBlendMode = BlendMode.One,
            BlendMode destinationColorBlendMode = BlendMode.Zero,
            BlendMode sourceAlphaBlendMode = BlendMode.One,
            BlendMode destinationAlphaBlendMode = BlendMode.Zero,
            BlendOp colorBlendOperation = BlendOp.Add,
            BlendOp alphaBlendOperation = BlendOp.Add)
        {
            m_WriteMask = (byte)writeMask;
            m_SourceColorBlendMode = (byte)sourceColorBlendMode;
            m_DestinationColorBlendMode = (byte)destinationColorBlendMode;
            m_SourceAlphaBlendMode = (byte)sourceAlphaBlendMode;
            m_DestinationAlphaBlendMode = (byte)destinationAlphaBlendMode;
            m_ColorBlendOperation = (byte)colorBlendOperation;
            m_AlphaBlendOperation = (byte)alphaBlendOperation;
            m_Padding = 0;
        }

        public ColorWriteMask writeMask
        {
            get { return (ColorWriteMask)m_WriteMask; }
            set { m_WriteMask = (byte)value; }
        }

        public BlendMode sourceColorBlendMode
        {
            get { return (BlendMode)m_SourceColorBlendMode; }
            set { m_SourceColorBlendMode = (byte)value; }
        }

        public BlendMode destinationColorBlendMode
        {
            get { return (BlendMode)m_DestinationColorBlendMode; }
            set { m_DestinationColorBlendMode = (byte)value; }
        }

        public BlendMode sourceAlphaBlendMode
        {
            get { return (BlendMode)m_SourceAlphaBlendMode; }
            set { m_SourceAlphaBlendMode = (byte)value; }
        }

        public BlendMode destinationAlphaBlendMode
        {
            get { return (BlendMode)m_DestinationAlphaBlendMode; }
            set { m_DestinationAlphaBlendMode = (byte)value; }
        }

        public BlendOp colorBlendOperation
        {
            get { return (BlendOp)m_ColorBlendOperation; }
            set { m_ColorBlendOperation = (byte)value; }
        }

        public BlendOp alphaBlendOperation
        {
            get { return (BlendOp)m_AlphaBlendOperation; }
            set { m_AlphaBlendOperation = (byte)value; }
        }

        byte m_WriteMask;
        byte m_SourceColorBlendMode;
        byte m_DestinationColorBlendMode;
        byte m_SourceAlphaBlendMode;
        byte m_DestinationAlphaBlendMode;
        byte m_ColorBlendOperation;
        byte m_AlphaBlendOperation;
        byte m_Padding;
    }
}
