// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Scripting.ScriptCompilation
{
    class VersionRangesFactory<TVersion> where TVersion : struct, IVersion<TVersion>
    {
        VersionRanges<TVersion> m_VersionRanges;

        public VersionRangesFactory()
        {
            m_VersionRanges = new VersionRanges<TVersion>(ExpressionTypeFactory<TVersion>.Create());
        }

        public VersionDefineExpression<TVersion> GetExpression(string expression)
        {
            return m_VersionRanges.GetExpression(expression);
        }
    }
}
