// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/FoundryHandle.h")]
    [FoundryAPI]
    internal struct FoundryHandle
    {
        private UInt32 m_Handle;

        internal extern bool IsValid { [NativeMethod("IsValid")] get; }
        internal extern static FoundryHandle Invalid();
        internal bool ReferenceEquals(FoundryHandle other)
        {
            return m_Handle == other.m_Handle;
        }

        public override bool Equals(object obj) => obj is FoundryHandle other && this.Equals(other);
        public bool Equals(FoundryHandle other) => this.ReferenceEquals(other);
        public override int GetHashCode() => (m_Handle).GetHashCode();
        public static bool operator==(FoundryHandle lhs, FoundryHandle rhs) => lhs.ReferenceEquals(rhs);
        public static bool operator!=(FoundryHandle lhs, FoundryHandle rhs) => !lhs.ReferenceEquals(rhs);

        public override string ToString() { return $"{m_Handle}"; }

        // This is extremely temporary!!  Use at your own risk as it will go away soon.
        internal UInt32 Handle { get { return m_Handle; } set { m_Handle = value; }}
    }
}
