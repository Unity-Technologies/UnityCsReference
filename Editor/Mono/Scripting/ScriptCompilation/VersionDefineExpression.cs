// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Scripting.ScriptCompilation
{
    internal struct VersionDefineExpression
    {
        private Func<SemVersion, SemVersion, SemVersion, bool> m_IsValid;
        public SemVersion Left { get; }
        public SemVersion Right { get; }

        private string m_AppliedRule;
        public string AppliedRule
        {
            get { return string.Format(m_AppliedRule, Left, Right); }
            set { m_AppliedRule = value; }
        }

        public VersionDefineExpression(Func<SemVersion, SemVersion, SemVersion, bool> isValid, SemVersion leftSemVersion, SemVersion rightSemVersion)
        {
            m_IsValid = isValid;
            m_AppliedRule = null;
            Left = leftSemVersion;
            Right = rightSemVersion;
        }

        public bool IsValid(SemVersion version)
        {
            if (!version.IsInitialized)
            {
                throw new ArgumentNullException(nameof(version));
            }

            return m_IsValid(Left, Right, version);
        }
    }
}
