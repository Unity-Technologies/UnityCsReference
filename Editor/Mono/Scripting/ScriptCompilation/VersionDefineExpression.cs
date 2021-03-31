// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Scripting.ScriptCompilation
{
    internal struct VersionDefineExpression<TVersion> where TVersion : struct, IVersion<TVersion>
    {
        private Func<TVersion, TVersion, TVersion, bool> m_IsValid;
        public TVersion Left { get; }
        public TVersion Right { get; }

        private string m_AppliedRule;
        public string AppliedRule
        {
            get { return string.Format(m_AppliedRule, Left, Right); }
            set { m_AppliedRule = value; }
        }

        public VersionDefineExpression(Func<TVersion, TVersion, TVersion, bool> isValid, TVersion leftVersion, TVersion rightVersion)
        {
            m_IsValid = isValid;
            m_AppliedRule = null;
            Left = leftVersion;
            Right = rightVersion;
        }

        public bool IsValid(TVersion version)
        {
            if (!version.IsInitialized)
            {
                throw new ArgumentNullException(nameof(version));
            }

            return m_IsValid(Left, Right, version);
        }

        public static VersionDefineExpression<TVersion> Invalid { get; } = new VersionDefineExpression<TVersion>(
            (unused1, unused2, unused3) => false, default(TVersion), default(TVersion));
    }
}
