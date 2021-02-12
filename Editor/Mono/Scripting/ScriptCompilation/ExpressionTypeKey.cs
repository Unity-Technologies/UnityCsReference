// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Scripting.ScriptCompilation
{
    internal struct ExpressionTypeKey
    {
        public bool Equals(ExpressionTypeKey other)
        {
            return m_LeftSymbol == other.m_LeftSymbol && m_RightSymbol == other.m_RightSymbol && m_HasSeparator == other.m_HasSeparator && m_HasLeftVersion == other.m_HasLeftVersion && m_HasRightVersion == other.m_HasRightVersion;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ExpressionTypeKey && Equals((ExpressionTypeKey)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = m_LeftSymbol.GetHashCode();
                hashCode = (hashCode * 397) ^ m_RightSymbol.GetHashCode();
                hashCode = (hashCode * 397) ^ m_HasSeparator.GetHashCode();
                hashCode = (hashCode * 397) ^ m_HasLeftVersion.GetHashCode();
                hashCode = (hashCode * 397) ^ m_HasRightVersion.GetHashCode();
                return hashCode;
            }
        }

        private readonly char m_LeftSymbol;
        private readonly char m_RightSymbol;
        private readonly bool m_HasSeparator;
        private readonly bool m_HasLeftVersion;
        private readonly bool m_HasRightVersion;

        public ExpressionTypeKey(char leftSymbol = default(char), char rightSymbol = default(char), bool hasSeparator = false, bool hasLeftVersion = false, bool hasRightVersion = false)
        {
            m_LeftSymbol = leftSymbol;
            m_RightSymbol = rightSymbol;
            m_HasSeparator = hasSeparator;
            m_HasLeftVersion = hasLeftVersion;
            m_HasRightVersion = hasRightVersion;
        }
    }
}
