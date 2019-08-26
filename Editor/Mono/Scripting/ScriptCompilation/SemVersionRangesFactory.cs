// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Scripting.ScriptCompilation
{
    class SemVersionRangesFactory
    {
        SemVersionRanges m_SemVersionRanges;

        public SemVersionRangesFactory()
        {
            m_SemVersionRanges = new SemVersionRanges(ExpressionTypeFactory.Create());
        }

        public VersionDefineExpression GetExpression(string expression)
        {
            return m_SemVersionRanges.GetExpression(expression);
        }
    }
}
