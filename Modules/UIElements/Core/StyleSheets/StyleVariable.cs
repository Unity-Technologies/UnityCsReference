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
        private List<int> m_UnsortedHash;

        public List<StyleVariable> variables => m_Variables;

        public void Add(StyleVariable sv)
        {
            var hash = sv.GetHashCode();
            int ComputeOrderSensitiveHash(int index)
            {
                unchecked
                {
                    // This needs to be reversible and order-sensitive. We use (index + 1) to avoid multiplying by 0.
                    return (index + 1) * hash;
                }
            }

            // Avoid duplicates. Otherwise the variable context explodes as hierarchy gets deeper.
            var hashIndex = m_SortedHash.BinarySearch(hash);
            if (hashIndex >= 0)
            {
                // UUM-32738: if the variable is already there, we need to move it to the end.

                // The new variable is already at the end. Nothing to update.
                var variableIndex = m_Variables.Count - 1;
                if (m_UnsortedHash[variableIndex] == hash)
                    return;

                // Look for the variable starting from the end. If this is a variable that gets redefined a lot,
                // it has more chances to be near the end.
                for (variableIndex--; variableIndex >= 0; variableIndex--)
                    if (m_UnsortedHash[variableIndex] == hash)
                    {
                        // Found the variable: Remove it and let it be re-added at the end.
                        m_VariableHash ^= ComputeOrderSensitiveHash(variableIndex);
                        m_Variables.RemoveAt(variableIndex);
                        m_UnsortedHash.RemoveAt(variableIndex);
                        break;
                    }
            }
            else
            {
                m_SortedHash.Insert(~hashIndex, hash);
            }

            m_VariableHash ^= ComputeOrderSensitiveHash(m_Variables.Count);
            m_Variables.Add(sv);
            m_UnsortedHash.Add(hash);
        }

        public void AddInitialRange(StyleVariableContext other)
        {
            if (other.m_Variables.Count > 0)
            {
                Debug.Assert(m_Variables.Count == 0);

                m_VariableHash = other.m_VariableHash;
                m_Variables.AddRange(other.m_Variables);
                m_SortedHash.AddRange(other.m_SortedHash);
                m_UnsortedHash.AddRange(other.m_UnsortedHash);
            }
        }

        public void Clear()
        {
            if (m_Variables.Count > 0)
            {
                m_Variables.Clear();
                m_VariableHash = 0;
                m_SortedHash.Clear();
                m_UnsortedHash.Clear();
            }
        }

        public StyleVariableContext()
        {
            m_Variables = new List<StyleVariable>();
            m_VariableHash = 0;
            m_SortedHash = new List<int>();
            m_UnsortedHash = new List<int>();
        }

        public StyleVariableContext(StyleVariableContext other)
        {
            m_Variables = new List<StyleVariable>(other.m_Variables);
            m_VariableHash = other.m_VariableHash;
            m_SortedHash = new List<int>(other.m_SortedHash);
            m_UnsortedHash = new List<int>(other.m_UnsortedHash);
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
        private enum Result
        {
            Valid,
            Invalid,
            NotFound
        }

        private struct ResolveContext
        {
            public StyleSheet sheet;
            public StyleValueHandle[] handles;
        }

        // Max resolves is to protect against long variables : https://drafts.csswg.org/css-variables/#long-variables
        internal const int kMaxResolves = 100;
        private static StyleSyntaxParser s_SyntaxParser = new StyleSyntaxParser();

        private StylePropertyValueMatcher m_Matcher = new StylePropertyValueMatcher();
        private List<StylePropertyValue> m_ResolvedValues = new List<StylePropertyValue>();
        private Stack<string> m_ResolvedVarStack = new Stack<string>();

        private StyleProperty m_Property;
        private Stack<ResolveContext> m_ContextStack = new Stack<ResolveContext>();
        private ResolveContext m_CurrentContext;

        private StyleSheet currentSheet => m_CurrentContext.sheet;
        private StyleValueHandle[] currentHandles => m_CurrentContext.handles;

        public List<StylePropertyValue> resolvedValues => m_ResolvedValues;
        public StyleVariableContext variableContext { get; set; }

        public void Init(StyleProperty property, StyleSheet sheet, StyleValueHandle[] handles)
        {
            m_ResolvedValues.Clear();
            m_ContextStack.Clear();

            m_Property = property;
            PushContext(sheet, handles);
        }

        private void PushContext(StyleSheet sheet, StyleValueHandle[] handles)
        {
            m_CurrentContext = new ResolveContext() { sheet = sheet, handles = handles };
            m_ContextStack.Push(m_CurrentContext);
        }

        private void PopContext()
        {
            m_ContextStack.Pop();
            m_CurrentContext = m_ContextStack.Peek();
        }

        public void AddValue(StyleValueHandle handle)
        {
            m_ResolvedValues.Add(new StylePropertyValue() {sheet = currentSheet, handle = handle});
        }

        public bool ResolveVarFunction(ref int index)
        {
            m_ResolvedVarStack.Clear();

            ParseVarFunction(currentSheet, currentHandles, ref index, out var argc, out var varName);

            var result = ResolveVarFunction(ref index, argc, varName);
            return result == Result.Valid;
        }

        private Result ResolveVarFunction(ref int index, int argc, string varName)
        {
            var result = ResolveVariable(varName);
            if (result == Result.NotFound && argc > 1)
            {
                // var() fallback
                var h = currentHandles[++index];
                Debug.Assert(h.valueType == StyleValueType.CommaSeparator, $"Unexpected value type {h.valueType} in var function");
                if (h.valueType == StyleValueType.CommaSeparator && index + 1 < currentHandles.Length)
                {
                    ++index;
                    result = ResolveFallback(ref index);
                }
            }

            return result;
        }

        public bool ValidateResolvedValues()
        {
            // Cannot validate custom property before it's assigned so it's always true in this case
            if (m_Property.isCustomProperty)
                return true;

            if (!StylePropertyCache.TryGetSyntax(m_Property.name, out var syntax))
            {
                Debug.LogAssertion($"Unknown style property {m_Property.name}");
                return false;
            }

            var validationExpression = s_SyntaxParser.Parse(syntax);
            var result = m_Matcher.Match(validationExpression, m_ResolvedValues);
            return result.success;
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
                if (m_ResolvedValues.Count + 1 > kMaxResolves)
                    return Result.Invalid;

                var h = sv.handles[i];
                if (h.IsVarFunction())
                {
                    PushContext(sv.sheet, sv.handles);

                    ParseVarFunction(sv.sheet, sv.handles, ref i, out var argc, out var varName);
                    result = ResolveVarFunction(ref i, argc, varName);

                    PopContext();
                }
                else
                {
                    m_ResolvedValues.Add(new StylePropertyValue() { sheet = sv.sheet, handle = h});
                }
            }
            m_ResolvedVarStack.Pop();

            return result;
        }

        private Result ResolveFallback(ref int index)
        {
            var result = Result.Valid;
            for (; index < currentHandles.Length && result == Result.Valid; ++index)
            {
                var h = currentHandles[index];
                if (h.IsVarFunction())
                {
                    ParseVarFunction(currentSheet, currentHandles, ref index, out var argc, out var varName);
                    result = ResolveVariable(varName);

                    if (result == Result.NotFound)
                    {
                        // Nested fallback like : var(--unknown, var(--unknown2, 10px))
                        if (argc > 1)
                        {
                            h = currentHandles[++index];
                            Debug.Assert(h.valueType == StyleValueType.CommaSeparator, $"Unexpected value type {h.valueType} in var function");
                            if (h.valueType == StyleValueType.CommaSeparator && index + 1 < currentHandles.Length)
                            {
                                ++index;
                                result = ResolveFallback(ref index);
                            }
                        }
                    }
                }
                else
                {
                    m_ResolvedValues.Add(new StylePropertyValue() { sheet = currentSheet, handle = h});
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
