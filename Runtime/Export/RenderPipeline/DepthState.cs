// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering
{
    // Must match GfxDepthState on C++ side
    [StructLayout(LayoutKind.Sequential)]
    public struct DepthState : IEquatable<DepthState>
    {
        public static DepthState defaultValue
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

        public bool Equals(DepthState other)
        {
            return m_WriteEnabled == other.m_WriteEnabled && m_CompareFunction == other.m_CompareFunction;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is DepthState && Equals((DepthState)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (m_WriteEnabled.GetHashCode() * 397) ^ m_CompareFunction.GetHashCode();
            }
        }

        public static bool operator==(DepthState left, DepthState right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(DepthState left, DepthState right)
        {
            return !left.Equals(right);
        }
    }
}
