// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor
{
    // Holds a flag set. Can be used with enum flags.
    // Has semantic value in separating a single value of an enum versus the flag set
    [Serializable]
    internal struct FlagSet<T>  where T : IConvertible
    {
        private ulong m_Flags;

        public bool HasFlags(T flags)
        {
            return (m_Flags & Convert.ToUInt64(flags)) != 0;
        }

        public void SetFlags(T flags, bool value)
        {
            if (value)
                m_Flags |= Convert.ToUInt64(flags);
            else
                m_Flags &= ~Convert.ToUInt64(flags);
        }

        public FlagSet(T flags)
        {
            m_Flags = Convert.ToUInt64(flags);
        }

        public static implicit operator FlagSet<T>(T flags) {return new FlagSet<T>(flags); }
    }
}
