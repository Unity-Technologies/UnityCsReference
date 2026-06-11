// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.UIElements.StyleSheets.Syntax;

namespace UnityEngine.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal readonly struct StyleVariable
    {
        // UniqueStyleString id for the variable name; lets TryFindVariable compare ints
        // instead of running string equality on every backward-scan step.
        public readonly int nameId;
        // Identity hash combining nameId + sheet + handles, precomputed at construction
        // so StyleVariableContext can dedup and order-hash without re-hashing per Add and
        // without a parallel hash list.
        public readonly int hash;
        public readonly StyleSheet sheet;
        public readonly StyleValueHandle[] handles;

        // Recovered from nameId via UniqueStyleString's id→string table; kept as a
        // computed property for inspector / authoring callers that still want the name.
        // Negative ids never resolve to a string (the id table is 0-based), so return
        // null rather than indexing out of range.
        public string name => nameId < 0 ? null : new UniqueStyleString(nameId).value;

        public StyleVariable(int nameId, StyleSheet sheet, StyleValueHandle[] handles)
        {
            this.nameId = nameId;
            this.sheet = sheet;
            this.handles = handles;
            unchecked
            {
                int h = nameId;
                h = (h * 397) ^ sheet.GetHashCode();
                h = (h * 397) ^ handles.GetHashCode();
                this.hash = h;
            }
        }

        public override int GetHashCode() => hash;
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal class StyleVariableContext
    {
        public static readonly StyleVariableContext none = new StyleVariableContext();

        private int m_VariableHash;
        private List<StyleVariable> m_Variables;
        private List<int> m_SortedHash;

        public List<StyleVariable> variables => m_Variables;

        public void Add(StyleVariable sv)
        {
            var hash = sv.hash;
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
                // Reading via a Span avoids the 24B struct copy that List<T>.this[int] does on
                // every read; the span is only used to locate the duplicate and we break out
                // before any mutation, so it never goes stale.
                var span = NoAllocHelpers.CreateReadOnlySpan(m_Variables);

                // The new variable is already at the end. Nothing to update.
                var variableIndex = span.Length - 1;
                if (span[variableIndex].hash == hash)
                    return;

                // Look for the variable starting from the end. If this is a variable that gets redefined a lot,
                // it has more chances to be near the end.
                for (variableIndex--; variableIndex >= 0; variableIndex--)
                    if (span[variableIndex].hash == hash)
                    {
                        // Found the variable: Remove it and let it be re-added at the end.
                        m_VariableHash ^= ComputeOrderSensitiveHash(variableIndex);
                        m_Variables.RemoveAt(variableIndex);
                        break;
                    }
            }
            else
            {
                m_SortedHash.Insert(~hashIndex, hash);
            }

            m_VariableHash ^= ComputeOrderSensitiveHash(m_Variables.Count);
            m_Variables.Add(sv);
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

        public bool TryFindVariable(int nameId, out StyleVariable v)
        {
            // -1 is the sentinel for "no id assigned yet" on both StyleProperty.customNameId
            // and StyleSheet.ReadVariableId. Treat it as a miss instead of letting the scan
            // collide with an entry that legitimately has that id.
            if (nameId < 0)
            {
                v = default(StyleVariable);
                return false;
            }

            // Span access skips the List<T>.this[int] per-iteration struct copy: critical
            // because this scan fires per var() resolution during style updates.
            var span = NoAllocHelpers.CreateReadOnlySpan(m_Variables);
            for (int i = span.Length - 1; i >= 0; --i)
            {
                if (span[i].nameId == nameId)
                {
                    v = span[i];
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
        private Stack<int> m_ResolvedVarStack = new Stack<int>();

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

            ParseVarFunction(currentSheet, currentHandles, ref index, out var argc, out var varNameId);

            var result = ResolveVarFunction(ref index, argc, varNameId);
            return result == Result.Valid;
        }

        private Result ResolveVarFunction(ref int index, int argc, int varNameId)
        {
            var result = ResolveVariable(varNameId);

            // Always consume all tokens and move the index to the end of the var() function
            if (argc > 1)
            {
                var handle = currentHandles[++index];
                Debug.Assert(handle.valueType == StyleValueType.CommaSeparator,
                    $"Unexpected value type {handle.valueType} in var() fallback; expected CommaSeparator.");
                if (handle.valueType == StyleValueType.CommaSeparator && index + 1 < currentHandles.Length)
                {
                    ++index;
                    // If variable not found, look for fallback; otherwise skip it
                    result = ResolveFallback(ref index, appendValues: result == Result.NotFound);
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

        private Result ResolveVariable(int variableNameId)
        {
            // Every var() name reachable here has been pre-registered by StyleSheet.SetupReferences
            // and addressed by its UniqueStyleString id. An id that no Add ever stored (including
            // the -1 sentinel from a sheet that never went through SetupReferences) simply misses
            // the scan and returns NotFound — same outcome as before.
            if (!variableContext.TryFindVariable(variableNameId, out var sv))
                return Result.NotFound;

            if (m_ResolvedVarStack.Contains(sv.nameId))
            {
                // Cyclic dependencies : https://drafts.csswg.org/css-variables/#cycles
                return Result.NotFound;
            }

            m_ResolvedVarStack.Push(sv.nameId);
            var result = Result.Valid;
            for (int i = 0; i < sv.handles.Length && result == Result.Valid; ++i)
            {
                if (m_ResolvedValues.Count + 1 > kMaxResolves)
                    return Result.Invalid;

                var h = sv.handles[i];
                if (h.IsVarFunction())
                {
                    PushContext(sv.sheet, sv.handles);

                    ParseVarFunction(sv.sheet, sv.handles, ref i, out var argc, out var varNameId);
                    result = ResolveVarFunction(ref i, argc, varNameId);

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

        private Result ResolveFallback(ref int index, bool appendValues)
        {
            var result = Result.Valid;

            for (; index < currentHandles.Length && result == Result.Valid; ++index)
            {
                var handle = currentHandles[index];

                if (handle.IsVarFunction())
                {
                    ParseVarFunction(currentSheet, currentHandles, ref index, out var argc, out var varNameId);

                    if (appendValues)
                    {
                        var nestedVarFunctionResult = ResolveVarFunction(ref index, argc, varNameId);
                        if (nestedVarFunctionResult != Result.Valid)
                            result = nestedVarFunctionResult;
                    }
                    else
                    {
                        // Nested fallback like : var(--unknown, var(--unknown2, 10px))
                        if (argc > 1)
                        {
                            var separator = currentHandles[++index];
                            Debug.Assert(separator.valueType == StyleValueType.CommaSeparator);
                            if (separator.valueType == StyleValueType.CommaSeparator && index + 1 < currentHandles.Length)
                            {
                                ++index;
                                ResolveFallback(ref index, appendValues: false);
                            }
                        }
                    }
                }
                else if (appendValues)
                {
                    m_ResolvedValues.Add(new StylePropertyValue() { sheet = currentSheet, handle = handle });
                }
            }

            return result;
        }

        private static void ParseVarFunction(StyleSheet sheet, StyleValueHandle[] handles, ref int index,
            out int argCount, out int variableNameId)
        {
            argCount = (int)sheet.ReadFloat(handles[++index]);
            variableNameId = sheet.ReadVariableId(handles[++index]);
        }
    }
}
