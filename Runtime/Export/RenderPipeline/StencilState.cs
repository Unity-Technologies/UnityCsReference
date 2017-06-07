// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    // Must match GfxStencilState on C++ side
    [StructLayout(LayoutKind.Sequential)]
    public struct StencilState
    {
        public static StencilState Default
        {
            // Passing a single parameter here to force non-default constructor
            get { return new StencilState(false); }
        }

        public StencilState(
            bool enabled = false,
            byte readMask = 255,
            byte writeMask = 255,
            CompareFunction compareFunction = CompareFunction.Always,
            StencilOp passOperation = StencilOp.Keep,
            StencilOp failOperation = StencilOp.Keep,
            StencilOp zFailOperation = StencilOp.Keep)
            : this(enabled, readMask, writeMask, compareFunction, passOperation, failOperation, zFailOperation, compareFunction, passOperation, failOperation, zFailOperation) {}

        public StencilState(
            bool enabled,
            byte readMask,
            byte writeMask,
            CompareFunction compareFunctionFront,
            StencilOp passOperationFront,
            StencilOp failOperationFront,
            StencilOp zFailOperationFront,
            CompareFunction compareFunctionBack,
            StencilOp passOperationBack,
            StencilOp failOperationBack,
            StencilOp zFailOperationBack)
        {
            m_Enabled = Convert.ToByte(enabled);
            m_ReadMask = readMask;
            m_WriteMask = writeMask;
            m_Padding = 0;
            m_CompareFunctionFront = (byte)compareFunctionFront;
            m_PassOperationFront = (byte)passOperationFront;
            m_FailOperationFront = (byte)failOperationFront;
            m_ZFailOperationFront = (byte)zFailOperationFront;
            m_CompareFunctionBack = (byte)compareFunctionBack;
            m_PassOperationBack = (byte)passOperationBack;
            m_FailOperationBack = (byte)failOperationBack;
            m_ZFailOperationBack = (byte)zFailOperationBack;
        }

        public bool enabled
        {
            get { return Convert.ToBoolean(m_Enabled); }
            set { m_Enabled = Convert.ToByte(value); }
        }

        public byte readMask
        {
            get { return m_ReadMask; }
            set { m_ReadMask = value; }
        }

        public byte writeMask
        {
            get { return m_WriteMask; }
            set { m_WriteMask = value; }
        }

        public CompareFunction compareFunction
        {
            set
            {
                compareFunctionFront = value;
                compareFunctionBack = value;
            }
        }

        public StencilOp passOperation
        {
            set
            {
                passOperationFront = value;
                passOperationBack = value;
            }
        }

        public StencilOp failOperation
        {
            set
            {
                failOperationFront = value;
                failOperationBack = value;
            }
        }

        public StencilOp zFailOperation
        {
            set
            {
                zFailOperationFront = value;
                zFailOperationBack = value;
            }
        }

        public CompareFunction compareFunctionFront
        {
            get { return (CompareFunction)m_CompareFunctionFront; }
            set { m_CompareFunctionFront = (byte)value; }
        }

        public StencilOp passOperationFront
        {
            get { return (StencilOp)m_PassOperationFront; }
            set { m_PassOperationFront = (byte)value; }
        }

        public StencilOp failOperationFront
        {
            get { return (StencilOp)m_FailOperationFront; }
            set { m_FailOperationFront = (byte)value; }
        }

        public StencilOp zFailOperationFront
        {
            get { return (StencilOp)m_ZFailOperationFront; }
            set { m_ZFailOperationFront = (byte)value; }
        }

        public CompareFunction compareFunctionBack
        {
            get { return (CompareFunction)m_CompareFunctionBack; }
            set { m_CompareFunctionBack = (byte)value; }
        }

        public StencilOp passOperationBack
        {
            get { return (StencilOp)m_PassOperationBack; }
            set { m_PassOperationBack = (byte)value; }
        }

        public StencilOp failOperationBack
        {
            get { return (StencilOp)m_FailOperationBack; }
            set { m_FailOperationBack = (byte)value; }
        }

        public StencilOp zFailOperationBack
        {
            get { return (StencilOp)m_ZFailOperationBack; }
            set { m_ZFailOperationBack = (byte)value; }
        }

        byte m_Enabled;
        byte m_ReadMask;
        byte m_WriteMask;
        byte m_Padding;
        byte m_CompareFunctionFront;
        byte m_PassOperationFront;
        byte m_FailOperationFront;
        byte m_ZFailOperationFront;
        byte m_CompareFunctionBack;
        byte m_PassOperationBack;
        byte m_FailOperationBack;
        byte m_ZFailOperationBack;
    }
}
