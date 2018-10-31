// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering
{
    // Must match GfxRenderTargetBlendState on C++ side
    [StructLayout(LayoutKind.Sequential)]
    public struct RenderTargetBlendState : IEquatable<RenderTargetBlendState>
    {
        public static RenderTargetBlendState defaultValue
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

        public bool Equals(RenderTargetBlendState other)
        {
            return m_WriteMask == other.m_WriteMask && m_SourceColorBlendMode == other.m_SourceColorBlendMode && m_DestinationColorBlendMode == other.m_DestinationColorBlendMode && m_SourceAlphaBlendMode == other.m_SourceAlphaBlendMode && m_DestinationAlphaBlendMode == other.m_DestinationAlphaBlendMode && m_ColorBlendOperation == other.m_ColorBlendOperation && m_AlphaBlendOperation == other.m_AlphaBlendOperation;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is RenderTargetBlendState && Equals((RenderTargetBlendState)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = m_WriteMask.GetHashCode();
                hashCode = (hashCode * 397) ^ m_SourceColorBlendMode.GetHashCode();
                hashCode = (hashCode * 397) ^ m_DestinationColorBlendMode.GetHashCode();
                hashCode = (hashCode * 397) ^ m_SourceAlphaBlendMode.GetHashCode();
                hashCode = (hashCode * 397) ^ m_DestinationAlphaBlendMode.GetHashCode();
                hashCode = (hashCode * 397) ^ m_ColorBlendOperation.GetHashCode();
                hashCode = (hashCode * 397) ^ m_AlphaBlendOperation.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator==(RenderTargetBlendState left, RenderTargetBlendState right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(RenderTargetBlendState left, RenderTargetBlendState right)
        {
            return !left.Equals(right);
        }
    }
}
