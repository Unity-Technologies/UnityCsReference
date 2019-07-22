// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.UIElements.StyleSheets.Syntax;

namespace UnityEngine.UIElements
{
    internal struct StyleVariable
    {
        public string name;
        public StyleSheet sheet;
        public StyleValueHandle[] handles;

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = name.GetHashCode();
                hashCode = (hashCode * 397) ^ sheet.GetHashCode();
                hashCode = (hashCode * 397) ^ handles.GetHashCode();
                return hashCode;
            }
        }
    }

    internal class StyleVariableContext
    {
        public static readonly StyleVariableContext none = new StyleVariableContext();

        private int m_VariableHash;
        private bool m_DirtyVariableHash = true;
        private List<StyleVariable> m_Variables;

        public int count => m_Variables.Count;

        public void Add(StyleVariable sv)
        {
            m_DirtyVariableHash = true;
            m_Variables.Add(sv);
        }

        public void RemoveRange(int i, int c)
        {
            m_DirtyVariableHash = true;
            m_Variables.RemoveRange(i, c);
        }

        public StyleVariableContext()
        {
            m_Variables = new List<StyleVariable>();
        }

        public StyleVariableContext(StyleVariableContext other)
        {
            m_Variables = new List<StyleVariable>(other.m_Variables);
        }

        public bool TryFindVariable(string name, out StyleVariable v)
        {
            for (int i = m_Variables.Count - 1; i >= 0; --i)
            {
                if (m_Variables[i].name == name)
                {
                    v = m_Variables[i];
                    return true;
                }
            }

            v = default(StyleVariable);
            return false;
        }

        public int GetVariableHash()
        {
            if (!m_DirtyVariableHash)
                return m_VariableHash;

            m_DirtyVariableHash = false;

            if (m_Variables.Count == 0)
            {
                m_VariableHash = 0;
                return m_VariableHash;
            }

            unchecked
            {
                m_VariableHash = m_Variables[0].GetHashCode();
                for (int i = 1; i < m_Variables.Count; i++)
                    m_VariableHash = (m_VariableHash * 397) ^ m_Variables[i].GetHashCode();

                return m_VariableHash;
            }
        }
    }

    internal class StyleVariableResolver
    {
        public enum Result
        {
            Valid,
            Invalid,
            NotFound
        }

        // Max resolves is to protect against long variables : https://drafts.csswg.org/css-variables/#long-variables
        internal const int kMaxResolves = 100;
        private static StyleSyntaxParser s_SyntaxParser = new StyleSyntaxParser();

        private StylePropertyValueMatcher m_Matcher = new StylePropertyValueMatcher();
        private List<StylePropertyValue> m_ResolvedValues = new List<StylePropertyValue>();
        private Stack<string> m_ResolvedVarStack = new Stack<string>();
        private StyleProperty m_CurrentProperty;
        private Expression m_ValidationExpression;

        public StyleVariableContext variableContext { get; set; }

        public Result ResolveVarFunction(StyleProperty property, StyleSheet sheet, StyleValueHandle[] handles, ref int index,
            List<StylePropertyValue> resolvedValues)
        {
            m_ResolvedValues = resolvedValues;
            m_ResolvedVarStack.Clear();
            m_CurrentProperty = property;
            m_ValidationExpression = null;

            if (!property.isCustomProperty)
            {
                string syntax;
                if (!StylePropertyCache.TryGetSyntax(property.name, out syntax))
                {
                    Debug.LogAssertion($"Unknown style property {m_CurrentProperty.name}");
                    return Result.Invalid;
                }

                m_ValidationExpression = s_SyntaxParser.Parse(syntax);
            }

            int argc;
            string varName;
            ParseVarFunction(sheet, handles, ref index, out argc, out varName);

            var result = ResolveVariable(varName);
            if (result != Result.Valid)
            {
                // var() fallback
                if (result == Result.NotFound && argc > 1 && !m_CurrentProperty.isCustomProperty)
                {
                    var h = handles[++index];
                    Debug.Assert(h.valueType == StyleValueType.FunctionSeparator, $"Unexpected value type {h.valueType} in var function");
                    if (h.valueType == StyleValueType.FunctionSeparator && index + 1 < handles.Length)
                    {
                        ++index;
                        result = ResolveFallback(sheet, handles, ref index);
                    }
                }
            }

            return result;
        }

        private Result ResolveVariable(string variableName)
        {
            StyleVariable sv;
            if (!variableContext.TryFindVariable(variableName, out sv))
                return Result.NotFound;

            if (m_ResolvedVarStack.Contains(sv.name))
            {
                // Cyclic dependencies : https://drafts.csswg.org/css-variables/#cycles
                sv = new StyleVariable();
                return Result.NotFound;
            }

            m_ResolvedVarStack.Push(sv.name);
            var result = Result.Valid;
            for (int i = 0; i < sv.handles.Length && result == Result.Valid; ++i)
            {
                var h = sv.handles[i];
                if (h.IsVarFunction())
                {
                    int argc;
                    string varName;
                    ParseVarFunction(sv.sheet, sv.handles, ref i, out argc, out varName);
                    result = ResolveVariable(varName);
                }
                else
                {
                    var spv = new StylePropertyValue() { sheet = sv.sheet, handle = h};
                    result = ValidateResolve(spv);
                }
            }
            m_ResolvedVarStack.Pop();

            return result;
        }

        private Result ValidateResolve(StylePropertyValue spv)
        {
            if (m_ResolvedValues.Count + 1 > kMaxResolves)
                return Result.Invalid;

            m_ResolvedValues.Add(spv);

            // Cannot validate custom property before it's assigned so it's always true in this case
            if (m_CurrentProperty.isCustomProperty)
                return Result.Valid;

            var result = m_Matcher.Match(m_ValidationExpression, m_ResolvedValues);
            if (!result.success)
                m_ResolvedValues.RemoveAt(m_ResolvedValues.Count - 1);
            return result.success ? Result.Valid : Result.Invalid;
        }

        private Result ResolveFallback(StyleSheet sheet, StyleValueHandle[] handles, ref int index)
        {
            var result = Result.Valid;
            for (; index < handles.Length && result == Result.Valid; ++index)
            {
                var h = handles[index];
                if (h.IsVarFunction())
                {
                    int argc;
                    string varName;
                    ParseVarFunction(sheet, handles, ref index, out argc, out varName);
                    result = ResolveVariable(varName);

                    if (result == Result.NotFound)
                    {
                        // Nested fallback like : var(--unknown, var(--unknown2, 10px))
                        if (argc > 1)
                        {
                            h = handles[++index];
                            Debug.Assert(h.valueType == StyleValueType.FunctionSeparator, $"Unexpected value type {h.valueType} in var function");
                            if (h.valueType == StyleValueType.FunctionSeparator && index + 1 < handles.Length)
                            {
                                ++index;
                                result = ResolveFallback(sheet, handles, ref index);
                            }
                        }
                    }
                }
                else
                {
                    var spv = new StylePropertyValue() { sheet = sheet, handle = h};
                    result = ValidateResolve(spv);
                }
            }

            return result;
        }

        private static void ParseVarFunction(StyleSheet sheet, StyleValueHandle[] handles, ref int index,
            out int argCount, out string variableName)
        {
            argCount = (int)sheet.ReadFloat(handles[++index]);
            variableName = sheet.ReadVariable(handles[++index]);
        }
    }
}
