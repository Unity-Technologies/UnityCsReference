// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    // Must match GfxDepthState on C++ side
    [StructLayout(LayoutKind.Sequential)]
    public struct DepthState
    {
        public static DepthState Default
        {
            // Passing a single parameter here to force non-default constructor
            get { return new DepthState(true); }
        }

        public DepthState(
            bool writeEnabled = true,
            CompareFunction compareFunction = CompareFunction.Less)
        {
            m_WriteEnabled = Convert.ToByte(writeEnabled);
            m_CompareFunction = (sbyte)compareFunction;
        }

        public bool writeEnabled
        {
            get { return Convert.ToBoolean(m_WriteEnabled); }
            set { m_WriteEnabled = Convert.ToByte(value); }
        }

        public CompareFunction compareFunction
        {
            get { return (CompareFunction)m_CompareFunction; }
            set { m_CompareFunction = (sbyte)value; }
        }

        byte m_WriteEnabled;
        sbyte m_CompareFunction;
    }
}
