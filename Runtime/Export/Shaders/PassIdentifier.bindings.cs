// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    [NativeHeader("Runtime/Shaders/PassIdentifier.h")]
    public readonly struct PassIdentifier : IEquatable<PassIdentifier>
    {
        public uint SubshaderIndex { get { return m_SubShaderIndex; } }
        public uint PassIndex { get { return m_PassIndex; } }

        public override bool Equals(object o)
        {
            return o is PassIdentifier other && this.Equals(other);
        }

        public bool Equals(PassIdentifier rhs)
        {
            return m_SubShaderIndex == rhs.m_SubShaderIndex && m_PassIndex == rhs.m_PassIndex;
        }

        public static bool operator==(PassIdentifier lhs, PassIdentifier rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator!=(PassIdentifier lhs, PassIdentifier rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode()
        {
            return m_SubShaderIndex.GetHashCode() ^ m_PassIndex.GetHashCode();
        }

        internal readonly uint m_SubShaderIndex;
        internal readonly uint m_PassIndex;
    }
}
