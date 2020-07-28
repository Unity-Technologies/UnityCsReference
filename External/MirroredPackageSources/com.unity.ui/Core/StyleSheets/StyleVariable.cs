using System.Collections.Generic;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.UIElements.StyleSheets.Syntax;

namespace UnityEngine.UIElements
{
    internal struct StyleVariable
    {
        public readonly string name;
        public readonly StyleSheet sheet;
        public readonly StyleValueHandle[] handles;

        public StyleVariable(string name, StyleSheet sheet, StyleValueHandle[] handles)
        {
            this.name = name;
            this.sheet = sheet;
            this.handles = handles;
        }

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
        private List<StyleVariable> m_Variables;
        private List<int> m_SortedHash;

        public void Add(StyleVariable sv)
        {
            // Avoid duplicates. Otherwise the variable context explodes as hierarchy gets deeper.
            var hash = sv.GetHashCode();
            var hashIndex = m_SortedHash.BinarySearch(hash);
            if (hashIndex >= 0)
                return;

            m_SortedHash.Insert(~hashIndex, hash);

            m_Variables.Add(sv);

            unchecked
            {
                m_VariableHash = m_Variables.Count == 0 ? sv.GetHashCode() : (m_VariableHash * 397) ^ sv.GetHashCode();
            }
        }

        public void AddInitialRange(StyleVariableContext other)
        {
            if (other.m_Variables.Count > 0)
            {
                Debug.Assert(m_Variables.Count == 0);

                m_VariableHash = other.m_VariableHash;
                m_Variables.AddRange(other.m_Variables);
                m_SortedHash.AddRange(other.m_SortedHash);
            }
        }

        public void Clear()
        {
            if (m_Variables.Count > 0)
            {
                m_Variables.Clear();
                m_VariableHash = 0;
                m_SortedHash.Clear();
            }
        }

        public StyleVariableContext()
        {
            m_Variables = new List<StyleVariable>();
            m_VariableHash = 0;
            m_SortedHash = new List<int>();
        }

        public StyleVariableContext(StyleVariableContext other)
        {
            m_Variables = new List<StyleVariable>(other.m_Variables);
            m_VariableHash = other.m_VariableHash;
            m_SortedHash = new List<int>(other.m_SortedHash);
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
            return m_VariableHash;
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
        private Expression m_ValidationExpression;

        private StyleProperty m_Property;
        private StyleSheet m_Sheet;
        private StyleValueHandle[] m_Handles;

        public List<StylePropertyValue> resolvedValues => m_ResolvedValues;
        public StyleVariableContext variableContext { get; set; }

        public void Init(StyleProperty property, StyleSheet sheet, StyleValueHandle[] handles)
        {
            m_ResolvedValues.Clear();
            m_Sheet = sheet;
            m_Property = property;
            m_Handles = handles;
        }

        public void AddValue(StyleValueHandle handle)
        {
            m_ResolvedValues.Add(new StylePropertyValue() {sheet = m_Sheet, handle = handle});
        }

        public Result ResolveVarFunction(ref int index)
        {
            m_ResolvedVarStack.Clear();
            m_ValidationExpression = null;

            if (!m_Property.isCustomProperty)
            {
                string syntax;
                if (!StylePropertyCache.TryGetSyntax(m_Property.name, out syntax))
                {
                    Debug.LogAssertion($"Unknown style property {m_Property.name}");
                    return Result.Invalid;
                }

                m_ValidationExpression = s_SyntaxParser.Parse(syntax);
            }

            int argc;
            string varName;
            ParseVarFunction(m_Sheet, m_Handles, ref index, out argc, out varName);

            var result = ResolveVariable(varName);
            if (result != Result.Valid)
            {
                // var() fallback
                if (result == Result.NotFound && argc > 1 && !m_Property.isCustomProperty)
                {
                    var h = m_Handles[++index];
                    Debug.Assert(h.valueType == StyleValueType.FunctionSeparator, $"Unexpected value type {h.valueType} in var function");
                    if (h.valueType == StyleValueType.FunctionSeparator && index + 1 < m_Handles.Length)
                    {
                        ++index;
                        result = ResolveFallback(ref index);
                    }
                }
                else
                {
                    m_ResolvedValues.Clear();
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
            if (m_Property.isCustomProperty)
                return Result.Valid;

            var result = m_Matcher.Match(m_ValidationExpression, m_ResolvedValues);
            if (!result.success)
                m_ResolvedValues.RemoveAt(m_ResolvedValues.Count - 1);
            return result.success ? Result.Valid : Result.Invalid;
        }

        private Result ResolveFallback(ref int index)
        {
            var result = Result.Valid;
            for (; index < m_Handles.Length && result == Result.Valid; ++index)
            {
                var h = m_Handles[index];
                if (h.IsVarFunction())
                {
                    int argc;
                    string varName;
                    ParseVarFunction(m_Sheet, m_Handles, ref index, out argc, out varName);
                    result = ResolveVariable(varName);

                    if (result == Result.NotFound)
                    {
                        // Nested fallback like : var(--unknown, var(--unknown2, 10px))
                        if (argc > 1)
                        {
                            h = m_Handles[++index];
                            Debug.Assert(h.valueType == StyleValueType.FunctionSeparator, $"Unexpected value type {h.valueType} in var function");
                            if (h.valueType == StyleValueType.FunctionSeparator && index + 1 < m_Handles.Length)
                            {
                                ++index;
                                result = ResolveFallback(ref index);
                            }
                        }
                    }
                }
                else
                {
                    var spv = new StylePropertyValue() { sheet = m_Sheet, handle = h};
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
