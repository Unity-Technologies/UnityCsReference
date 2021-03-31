using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.UIElements;

namespace UnityEngine.UIElements
{
    internal struct RuleMatcher
    {
        public StyleSheet sheet;
        public StyleComplexSelector complexSelector;

        public RuleMatcher(StyleSheet sheet, StyleComplexSelector complexSelector, int styleSheetIndexInStack)
        {
            this.sheet = sheet;
            this.complexSelector = complexSelector;
        }

        public override string ToString()
        {
            return complexSelector.ToString();
        }
    }

    /// <summary>
    /// UQuery is a set of extension methods allowing you to select individual or collection of visualElements inside a complex hierarchy.
    /// </summary>
    public static class UQuery
    {
        //This scheme saves us 20 bytes instead of saving a Func<object, bool> directly (12 vs 32 bytes)
        internal interface IVisualPredicateWrapper
        {
            bool Predicate(object e);
        }

        internal class IsOfType<T> : IVisualPredicateWrapper where T : VisualElement
        {
            public static IsOfType<T> s_Instance = new IsOfType<T>();

            public bool Predicate(object e)
            {
                return e is T;
            }
        }

        internal class PredicateWrapper<T> : IVisualPredicateWrapper where T : VisualElement
        {
            private Func<T, bool> predicate;
            public PredicateWrapper(Func<T, bool> p)
            {
                predicate = p;
            }

            public bool Predicate(object e)
            {
                T element = e as T;
                if (element != null)
                {
                    return predicate(element);
                }
                return false;
            }
        }

        internal abstract class UQueryMatcher : HierarchyTraversal
        {
            internal List<RuleMatcher> m_Matchers;

            protected UQueryMatcher()
            {
            }

            public override void Traverse(VisualElement element)
            {
                base.Traverse(element);
            }

            protected virtual bool OnRuleMatchedElement(RuleMatcher matcher, VisualElement element)
            {
                return false;
            }

            static void NoProcessResult(VisualElement e, MatchResultInfo i) {}

            public override void TraverseRecursive(VisualElement element, int depth)
            {
                int originalCount = m_Matchers.Count;

                int count = m_Matchers.Count; // changes while we iterate so save

                for (int j = 0; j < count; j++)
                {
                    RuleMatcher matcher = m_Matchers[j];

                    if (StyleSelectorHelper.MatchRightToLeft(element, matcher.complexSelector, (e, i) => NoProcessResult(e, i)))
                    {
                        // use by uQuery to determine if we need to stop
                        if (OnRuleMatchedElement(matcher, element))
                        {
                            return;
                        }
                    }
                }

                Recurse(element, depth);

                // Remove all matchers that we could possibly have added at this level of recursion
                if (m_Matchers.Count > originalCount)
                {
                    m_Matchers.RemoveRange(originalCount, m_Matchers.Count - originalCount);
                }
            }

            public virtual void Run(VisualElement root, List<RuleMatcher> matchers)
            {
                m_Matchers = matchers;
                Traverse(root);
            }
        }

        internal abstract class SingleQueryMatcher : UQueryMatcher
        {
            public VisualElement match { get; set; }

            public override void Run(VisualElement root, List<RuleMatcher> matchers)
            {
                match = null;
                base.Run(root, matchers);
                m_Matchers = null;
            }

            public bool IsInUse()
            {
                return m_Matchers != null;
            }

            public abstract SingleQueryMatcher CreateNew();
        }

        internal class FirstQueryMatcher : SingleQueryMatcher
        {
            public static readonly FirstQueryMatcher Instance = new FirstQueryMatcher();
            protected override bool OnRuleMatchedElement(RuleMatcher matcher, VisualElement element)
            {
                if (match == null)
                    match = element;
                return true;
            }

            public override SingleQueryMatcher CreateNew() => new FirstQueryMatcher();
        }

        internal class LastQueryMatcher : SingleQueryMatcher
        {
            public static readonly LastQueryMatcher Instance = new LastQueryMatcher();

            protected override bool OnRuleMatchedElement(RuleMatcher matcher, VisualElement element)
            {
                match = element;
                return false;
            }

            public override SingleQueryMatcher CreateNew() => new LastQueryMatcher();
        }

        internal class IndexQueryMatcher : SingleQueryMatcher
        {
            public static readonly IndexQueryMatcher Instance = new IndexQueryMatcher();

            private int matchCount = -1;
            private int _matchIndex;

            public int matchIndex
            {
                get { return _matchIndex; }
                set
                {
                    matchCount = -1;
                    _matchIndex = value;
                }
            }

            public override void Run(VisualElement root, List<RuleMatcher> matchers)
            {
                matchCount = -1;
                base.Run(root, matchers);
            }

            protected override bool OnRuleMatchedElement(RuleMatcher matcher, VisualElement element)
            {
                ++matchCount;
                if (matchCount == _matchIndex)
                {
                    match = element;
                }

                return matchCount >= _matchIndex;
            }

            public override SingleQueryMatcher CreateNew() => new IndexQueryMatcher();
        }
    }

    /// <summary>
    /// Query object containing all the selection rules. The object can be saved and rerun later without re-allocating memory.
    /// </summary>
    public struct UQueryState<T> : IEnumerable<T>, IEquatable<UQueryState<T>> where T : VisualElement
    {
        //this makes it non-thread safe. But saves on allocations...
        private static ActionQueryMatcher s_Action = new ActionQueryMatcher();

        private readonly VisualElement m_Element;
        internal readonly List<RuleMatcher> m_Matchers;

        internal UQueryState(VisualElement element, List<RuleMatcher> matchers)
        {
            m_Element = element;
            m_Matchers = matchers;
        }

        /// <summary>
        /// Creates a new QueryState with the same selection rules, applied on another VisualElement.
        /// </summary>
        /// <param name="element">The element on which to apply the selection rules.</param>
        /// <returns>A new QueryState with the same selection rules, applied on this element.</returns>
        public UQueryState<T> RebuildOn(VisualElement element)
        {
            return new UQueryState<T>(element, m_Matchers);
        }

        private T Single(UQuery.SingleQueryMatcher matcher)
        {
            if (matcher.IsInUse())  //Prevent reentrance issues
            {
                matcher = matcher.CreateNew();
            }

            matcher.Run(m_Element, m_Matchers);
            var match = matcher.match as T;

            // We need to make sure we don't leak a ref to the VisualElement.
            matcher.match = null;
            return match;
        }

        /// <summary>
        /// The first element matching all the criteria, or null if none was found.
        /// </summary>
        /// <returns>The first element matching all the criteria, or null if none was found.</returns>
        public T First() => Single(UQuery.FirstQueryMatcher.Instance);

        /// <summary>
        /// The last element matching all the criteria, or null if none was found.
        /// </summary>
        /// <returns>The last element matching all the criteria, or null if none was found.</returns>
        public T Last() => Single(UQuery.LastQueryMatcher.Instance);

        private class ListQueryMatcher<TElement> : UQuery.UQueryMatcher where TElement : VisualElement
        {
            public List<TElement> matches { get; set; }

            protected override bool OnRuleMatchedElement(RuleMatcher matcher, VisualElement element)
            {
                matches.Add(element as TElement);
                return false;
            }

            public void Reset()
            {
                matches = null;
            }
        }

        private static readonly ListQueryMatcher<T> s_List = new ListQueryMatcher<T>();

        /// <summary>
        /// Adds all elements satisfying selection rules to the list.
        /// </summary>
        /// <param name="results">Adds all elements satisfying selection rules to the list.</param>
        public void ToList(List<T> results)
        {
            s_List.matches = results;
            s_List.Run(m_Element, m_Matchers);
            s_List.Reset();
        }

        /// <summary>
        /// Returns a list containing elements satisfying selection rules.
        /// </summary>
        /// <returns>A list containing elements satisfying selection rules.</returns>
        public List<T> ToList()
        {
            List<T> result = new List<T>();
            ToList(result);
            return result;
        }

        /// <summary>
        /// Selects the nth element matching all the criteria, or null if not enough elements were found.
        /// </summary>
        /// <param name="index">The index of the matched element.</param>
        /// <returns>The match element at the specified index.</returns>
        public T AtIndex(int index)
        {
            var indexMatcher = UQuery.IndexQueryMatcher.Instance;
            indexMatcher.matchIndex = index;
            return Single(indexMatcher);
        }

        //Convoluted trick so save on allocating memory for delegates or lambdas
        private class ActionQueryMatcher : UQuery.UQueryMatcher
        {
            internal Action<T> callBack { get; set; }

            protected override bool OnRuleMatchedElement(RuleMatcher matcher, VisualElement element)
            {
                if (element is T castedElement)
                {
                    callBack(castedElement);
                }

                return false;
            }
        }

        /// <summary>
        /// Invokes function on all elements matching the query.
        /// </summary>
        /// <param name="funcCall">The action to be invoked with each matching element.</param>
        public void ForEach(Action<T> funcCall)
        {
            var act = s_Action;

            if (act.callBack != null)
            {
                //we're inside a ForEach callback already. we need to allocate :(
                act = new ActionQueryMatcher();
            }

            try
            {
                act.callBack = funcCall;
                act.Run(m_Element, m_Matchers);
            }
            finally
            {
                act.callBack = null;
            }
        }

        private class DelegateQueryMatcher<TReturnType> : UQuery.UQueryMatcher
        {
            public Func<T, TReturnType> callBack { get; set; }

            public List<TReturnType> result { get; set; }

            public static DelegateQueryMatcher<TReturnType> s_Instance = new DelegateQueryMatcher<TReturnType>();

            protected override bool OnRuleMatchedElement(RuleMatcher matcher, VisualElement element)
            {
                if (element is T castedElement)
                {
                    result.Add(callBack(castedElement));
                }

                return false;
            }
        }
        /// <summary>
        /// Invokes function on all elements matching the query.
        /// </summary>
        /// <param name="result">Each return value will be added to this list.</param>
        /// <param name="funcCall">The function to be invoked with each matching element.</param>
        public void ForEach<T2>(List<T2> result, Func<T, T2> funcCall)
        {
            var matcher = DelegateQueryMatcher<T2>.s_Instance;

            if (matcher.callBack != null)
            {
                //we're inside a call to ForEach already!, we need to allocate :(
                matcher = new DelegateQueryMatcher<T2>();
            }

            try
            {
                matcher.callBack = funcCall;
                matcher.result = result;
                matcher.Run(m_Element, m_Matchers);
            }
            finally
            {
                matcher.callBack = null;
                matcher.result = null;
            }
        }

        /// <summary>
        /// Invokes function on all elements matching the query.
        /// </summary>
        /// <param name="funcCall">The action to be invoked with each matching element.</param>
        public List<T2> ForEach<T2>(Func<T, T2> funcCall)
        {
            List<T2> result = new List<T2>();
            ForEach(result, funcCall);
            return result;
        }

        /// <summary>
        /// Allows traversing the results of the query with `foreach` without creating GC allocations.
        /// </summary>
        /// <returns>A <see cref="UQueryState{T}.Enumerator"/> instance configured to traverse the results.</returns>
        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private static readonly ListQueryMatcher<VisualElement> s_EnumerationList = new ListQueryMatcher<VisualElement>();

        /// <undoc/>
        public struct Enumerator : IEnumerator<T>
        {
            private List<VisualElement> iterationList;
            private int currentIndex;

            internal Enumerator(UQueryState<T> queryState)
            {
                iterationList =  VisualElementListPool.Get();
                s_EnumerationList.matches = iterationList;
                s_EnumerationList.Run(queryState.m_Element, queryState.m_Matchers);
                s_EnumerationList.Reset();
                currentIndex = -1;
            }

            /// <undoc/>
            public T Current => (T)iterationList[currentIndex];

            /// <undoc/>
            object IEnumerator.Current => Current;

            /// <undoc/>
            public bool MoveNext()
            {
                return (++currentIndex < iterationList.Count); // increment current position and check if reached end of buffer
            }

            /// <undoc/>
            public void Reset()
            {
                currentIndex = -1;
            }

            /// <undoc/>
            public void Dispose()
            {
                VisualElementListPool.Release(iterationList);
                iterationList = null;
            }
        }

        /// <undoc/>
        public bool Equals(UQueryState<T> other)
        {
            return ReferenceEquals(m_Element, other.m_Element) &&
                EqualityComparer<List<RuleMatcher>>.Default.Equals(m_Matchers, other.m_Matchers);
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            if (!(obj is UQueryState<T>))
            {
                return false;
            }

            return Equals((UQueryState<T>)obj);
        }

        public override int GetHashCode()
        {
            var hashCode = 488160421;
            hashCode = hashCode * -1521134295 + EqualityComparer<VisualElement>.Default.GetHashCode(m_Element);
            hashCode = hashCode * -1521134295 + EqualityComparer<List<RuleMatcher>>.Default.GetHashCode(m_Matchers);
            return hashCode;
        }

        /// <undoc/>
        public static bool operator==(UQueryState<T> state1, UQueryState<T> state2)
        {
            return state1.Equals(state2);
        }

        /// <undoc/>
        public static bool operator!=(UQueryState<T> state1, UQueryState<T> state2)
        {
            return !(state1 == state2);
        }
    }

    /// <summary>
    /// Utility Object that contructs a set of selection rules to be ran on a root visual element.
    /// </summary>
    public struct UQueryBuilder<T> : IEquatable<UQueryBuilder<T>> where T : VisualElement
    {
        private List<StyleSelector> m_StyleSelectors;
        private List<StyleSelector> styleSelectors { get { return m_StyleSelectors ?? (m_StyleSelectors = new List<StyleSelector>()); } }

        private List<StyleSelectorPart> m_Parts;
        private List<StyleSelectorPart> parts { get { return m_Parts ?? (m_Parts = new List<StyleSelectorPart>()); } }
        private VisualElement m_Element;
        private List<RuleMatcher> m_Matchers;
        private StyleSelectorRelationship m_Relationship;

        private int pseudoStatesMask;
        private int negatedPseudoStatesMask;

        /// <summary>
        /// Initializes a QueryBuilder.
        /// </summary>
        /// <param name="visualElement">The root element on which to condfuct the search query.</param>
        public UQueryBuilder(VisualElement visualElement)
            : this()
        {
            m_Element = visualElement;
            m_Parts = null;
            m_StyleSelectors = null;
            m_Relationship = StyleSelectorRelationship.None;
            m_Matchers = new List<RuleMatcher>();
            pseudoStatesMask = negatedPseudoStatesMask = 0;
        }

        /// <summary>
        /// Selects all elements with the specified class in the class list, as specified with the `class` attribute in a UXML file or added with <see cref="VisualElement.AddToClassList(string)"/> method.
        /// </summary>
        /// <param name="classname">The class to use in the query.</param>
        /// <remarks>
        /// This method can be called multiple times in order to select elements with multiple classes.
        /// To select elements by their C# type, use <see cref="OfType{T2}(string,string[])"/>.
        /// </remarks>
        public UQueryBuilder<T> Class(string classname)
        {
            AddClass(classname);
            return this;
        }

        /// <summary>
        /// Selects element with this name.
        /// </summary>
        public UQueryBuilder<T> Name(string id)
        {
            AddName(id);
            return this;
        }

        /// <summary>
        /// Selects all elements that are descendants of currently matching ancestors.
        /// </summary>
        public UQueryBuilder<T2> Descendents<T2>(string name = null, params string[] classNames) where T2 : VisualElement
        {
            FinishCurrentSelector();
            AddType<T2>();
            AddName(name);
            AddClasses(classNames);
            return AddRelationship<T2>(StyleSelectorRelationship.Descendent);
        }

        /// <summary>
        /// Selects all elements that are descendants of currently matching ancestors.
        /// </summary>
        public UQueryBuilder<T2> Descendents<T2>(string name = null, string classname = null) where T2 : VisualElement
        {
            FinishCurrentSelector();
            AddType<T2>();
            AddName(name);
            AddClass(classname);
            return AddRelationship<T2>(StyleSelectorRelationship.Descendent);
        }

        /// <summary>
        /// Selects all direct child elements of elements matching the previous rules.
        /// </summary>
        public UQueryBuilder<T2> Children<T2>(string name = null, params string[] classes) where T2 : VisualElement
        {
            FinishCurrentSelector();
            AddType<T2>();
            AddName(name);
            AddClasses(classes);
            return AddRelationship<T2>(StyleSelectorRelationship.Child);
        }

        /// <summary>
        /// Selects all direct child elements of elements matching the previous rules.
        /// </summary>
        public UQueryBuilder<T2> Children<T2>(string name = null, string className = null) where T2 : VisualElement
        {
            FinishCurrentSelector();
            AddType<T2>();
            AddName(name);
            AddClass(className);
            return AddRelationship<T2>(StyleSelectorRelationship.Child);
        }

        /// <summary>
        /// Selects all elements of the specified Type (eg: Label, Button, ScrollView, etc).
        /// </summary>
        /// <param name="name">If specified, will select elements with this name.</param>
        /// <param name="classes">If specified, will select elements with the given class (not to be confused with Type).</param>
        /// <returns>QueryBuilder configured with the associated selection rules.</returns>
        public UQueryBuilder<T2> OfType<T2>(string name = null, params string[] classes) where T2 : VisualElement
        {
            AddType<T2>();
            AddName(name);
            AddClasses(classes);
            return AddRelationship<T2>(StyleSelectorRelationship.None);
        }

        /// <summary>
        /// Selects all elements of the specified Type (eg: Label, Button, ScrollView, etc).
        /// </summary>
        /// <param name="name">If specified, will select elements with this name.</param>
        /// <param name="className">If specified, will select elements with the given class (not to be confused with Type).</param>
        /// <returns>QueryBuilder configured with the associated selection rules.</returns>
        public UQueryBuilder<T2> OfType<T2>(string name = null, string className = null) where T2 : VisualElement
        {
            AddType<T2>();
            AddName(name);
            AddClass(className);
            return AddRelationship<T2>(StyleSelectorRelationship.None);
        }

        //Only used to avoid allocations in Q<>() Don't use this unless you know what you're doing
        internal UQueryBuilder<T> SingleBaseType()
        {
            parts.Add(StyleSelectorPart.CreatePredicate(UQuery.IsOfType<T>.s_Instance));
            return this;
        }

        /// <summary>
        /// Selects all elements satifying the predicate.
        /// </summary>
        /// <param name="selectorPredicate">Predicate that must return true for selected elements.</param>
        /// <returns>QueryBuilder configured with the associated selection rules.</returns>
        public UQueryBuilder<T> Where(Func<T, bool> selectorPredicate)
        {
            //we can't use a static instance as in the QueryState<T>.ForEach below since the query might be long lived
            parts.Add(StyleSelectorPart.CreatePredicate(new UQuery.PredicateWrapper<T>(selectorPredicate)));
            return this;
        }

        private void AddClass(string c)
        {
            if (c != null)
                parts.Add(StyleSelectorPart.CreateClass(c));
        }

        private void AddClasses(params string[] classes)
        {
            if (classes != null)
            {
                for (int i = 0; i < classes.Length; i++)
                    AddClass(classes[i]);
            }
        }

        private void AddName(string id)
        {
            if (id != null)
                parts.Add(StyleSelectorPart.CreateId(id));
        }

        private void AddType<T2>() where T2 : VisualElement
        {
            if (typeof(T2) != typeof(VisualElement))
                parts.Add(StyleSelectorPart.CreatePredicate(UQuery.IsOfType<T2>.s_Instance));
        }

        private UQueryBuilder<T> AddPseudoState(PseudoStates s)
        {
            pseudoStatesMask = pseudoStatesMask | (int)s;
            return this;
        }

        private UQueryBuilder<T> AddNegativePseudoState(PseudoStates s)
        {
            negatedPseudoStatesMask = negatedPseudoStatesMask | (int)s;
            return this;
        }

        /// <summary>
        /// Selects all elements that are active.
        /// </summary>
        /// <returns>A QueryBuilder with the selection rules.</returns>
        public UQueryBuilder<T> Active()
        {
            return AddPseudoState(PseudoStates.Active);
        }

        /// <summary>
        /// Selects all elements that are not active.
        /// </summary>
        public UQueryBuilder<T> NotActive()
        {
            return AddNegativePseudoState(PseudoStates.Active);
        }

        /// <summary>
        /// Selects all elements that are not visible.
        /// </summary>
        public UQueryBuilder<T> Visible()
        {
            return Where(e => e.visible);
        }

        /// <summary>
        /// Selects all elements that are not visible.
        /// </summary>
        public UQueryBuilder<T> NotVisible()
        {
            return Where(e => !e.visible);
        }

        /// <summary>
        /// Selects all elements that are hovered.
        /// </summary>
        public UQueryBuilder<T> Hovered()
        {
            return AddPseudoState(PseudoStates.Hover);
        }

        /// <summary>
        /// Selects all elements that are not hovered.
        /// </summary>
        public UQueryBuilder<T> NotHovered()
        {
            return AddNegativePseudoState(PseudoStates.Hover);
        }

        /// <summary>
        /// Selects all elements that are checked.
        /// </summary>
        public UQueryBuilder<T> Checked()
        {
            return AddPseudoState(PseudoStates.Checked);
        }

        /// <summary>
        /// Selects all elements that npot checked.
        /// </summary>
        public UQueryBuilder<T> NotChecked()
        {
            return AddNegativePseudoState(PseudoStates.Checked);
        }

        /// <summary>
        /// Selects all elements that are not selected.
        /// </summary>
        [Obsolete("Use Checked() instead")]
        public UQueryBuilder<T> Selected()
        {
            return AddPseudoState(PseudoStates.Checked);
        }

        /// <summary>
        /// Selects all elements that are not selected.
        /// </summary>
        [Obsolete("Use NotChecked() instead")]
        public UQueryBuilder<T> NotSelected()
        {
            return AddNegativePseudoState(PseudoStates.Checked);
        }

        /// <summary>
        /// Selects all elements that are enabled.
        /// </summary>
        public UQueryBuilder<T> Enabled()
        {
            return AddNegativePseudoState(PseudoStates.Disabled);
        }

        /// <summary>
        /// Selects all elements that are not enabled.
        /// </summary>
        public UQueryBuilder<T> NotEnabled()
        {
            return AddPseudoState(PseudoStates.Disabled);
        }

        /// <summary>
        /// Selects all elements that are enabled.
        /// </summary>
        public UQueryBuilder<T> Focused()
        {
            return AddPseudoState(PseudoStates.Focus);
        }

        /// <summary>
        /// Selects all elements that don't currently own the focus.
        /// </summary>
        public UQueryBuilder<T> NotFocused()
        {
            return AddNegativePseudoState(PseudoStates.Focus);
        }

        private UQueryBuilder<T2> AddRelationship<T2>(StyleSelectorRelationship relationship) where T2 : VisualElement
        {
            return new UQueryBuilder<T2>(m_Element)
            {
                m_Matchers = m_Matchers,
                m_Parts = m_Parts,
                m_StyleSelectors = m_StyleSelectors,
                m_Relationship = relationship == StyleSelectorRelationship.None ? m_Relationship : relationship,
                pseudoStatesMask = pseudoStatesMask,
                negatedPseudoStatesMask = negatedPseudoStatesMask
            };
        }

        void AddPseudoStatesRuleIfNecessasy()
        {
            if (pseudoStatesMask != 0 ||
                negatedPseudoStatesMask != 0)
            {
                parts.Add(new StyleSelectorPart() {type = StyleSelectorType.PseudoClass});
            }
        }

        private void FinishSelector()
        {
            FinishCurrentSelector();
            if (styleSelectors.Count > 0)
            {
                var selector = new StyleComplexSelector();
                selector.selectors = styleSelectors.ToArray();
                styleSelectors.Clear();
                m_Matchers.Add(new RuleMatcher { complexSelector = selector });
            }
        }

        private bool CurrentSelectorEmpty()
        {
            return parts.Count == 0 &&
                m_Relationship == StyleSelectorRelationship.None &&
                pseudoStatesMask == 0 &&
                negatedPseudoStatesMask == 0;
        }

        private void FinishCurrentSelector()
        {
            if (!CurrentSelectorEmpty())
            {
                StyleSelector sel = new StyleSelector();
                sel.previousRelationship = m_Relationship;

                AddPseudoStatesRuleIfNecessasy();

                sel.parts = m_Parts.ToArray();
                sel.pseudoStateMask = pseudoStatesMask;
                sel.negatedPseudoStateMask = negatedPseudoStatesMask;
                styleSelectors.Add(sel);
                m_Parts.Clear();
                pseudoStatesMask = negatedPseudoStatesMask = 0;
            }
        }

        /// <summary>
        /// Compiles the selection rules into a QueryState object.
        /// </summary>
        public UQueryState<T> Build()
        {
            FinishSelector();

            if (m_Matchers.Count == 0)
            {
                // an empty query should match everything
                parts.Add(new StyleSelectorPart() {type = StyleSelectorType.Wildcard});
                FinishSelector();
            }
            return new UQueryState<T>(m_Element, m_Matchers);
        }

        // Quick One-liners accessors
        /// <undoc/>
        public static implicit operator T(UQueryBuilder<T> s)
        {
            return s.First();
        }

        /// <undoc/>
        public static bool operator==(UQueryBuilder<T> builder1, UQueryBuilder<T> builder2)
        {
            return builder1.Equals(builder2);
        }

        /// <undoc/>
        public static bool operator!=(UQueryBuilder<T> builder1, UQueryBuilder<T> builder2)
        {
            return !(builder1 == builder2);
        }

        /// <summary>
        /// Convenience overload, shorthand for Build().First().
        /// </summary>
        /// <returns>The first element matching all the criteria, or null if none was found.</returns>
        /// <seealso cref="UQueryState{T}.First"/>
        public T First()
        {
            return Build().First();
        }

        /// <summary>
        /// Convenience overload, shorthand for Build().Last().
        /// </summary>
        /// <returns>The last element matching all the criteria, or null if none was found.</returns>
        public T Last()
        {
            return Build().Last();
        }

        /// <summary>
        /// Convenience method. shorthand for Build().ToList.
        /// </summary>
        /// <returns>A list containing elements satisfying selection rules.</returns>
        public List<T> ToList()
        {
            return Build().ToList();
        }

        /// <summary>
        /// Convenience method. Shorthand gor Build().ToList().
        /// </summary>
        /// <param name="results">Adds all elements satisfying selection rules to the list.</param>
        public void ToList(List<T> results)
        {
            Build().ToList(results);
        }

        /// <summary>
        /// Convenience overload, shorthand for Build().AtIndex().
        /// </summary>
        /// <seealso cref="UQueryState{T}.AtIndex"/>
        public T AtIndex(int index)
        {
            return Build().AtIndex(index);
        }

        /// <summary>
        /// Convenience overload, shorthand for Build().ForEach().
        /// </summary>
        /// <param name="result">Each return value will be added to this list.</param>
        /// <param name="funcCall">The function to be invoked with each matching element.</param>
        public void ForEach<T2>(List<T2> result, Func<T, T2> funcCall)
        {
            Build().ForEach(result, funcCall);
        }

        /// <summary>
        /// Convenience overload, shorthand for Build().ForEach().
        /// </summary>
        /// <param name="funcCall">The function to be invoked with each matching element.</param>
        public List<T2> ForEach<T2>(Func<T, T2> funcCall)
        {
            return Build().ForEach(funcCall);
        }

        /// <summary>
        /// Convenience overload, shorthand for Build().ForEach().
        /// </summary>
        /// <param name="funcCall">The function to be invoked with each matching element.</param>
        public void ForEach(Action<T> funcCall)
        {
            Build().ForEach(funcCall);
        }

        /// <undoc/>
        public bool Equals(UQueryBuilder<T> other)
        {
            return EqualityComparer<List<StyleSelector>>.Default.Equals(m_StyleSelectors, other.m_StyleSelectors) &&
                EqualityComparer<List<StyleSelector>>.Default.Equals(styleSelectors, other.styleSelectors) &&
                EqualityComparer<List<StyleSelectorPart>>.Default.Equals(m_Parts, other.m_Parts) &&
                EqualityComparer<List<StyleSelectorPart>>.Default.Equals(parts, other.parts) && ReferenceEquals(m_Element, other.m_Element) &&
                EqualityComparer<List<RuleMatcher>>.Default.Equals(m_Matchers, other.m_Matchers) &&
                m_Relationship == other.m_Relationship &&
                pseudoStatesMask == other.pseudoStatesMask &&
                negatedPseudoStatesMask == other.negatedPseudoStatesMask;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            if (!(obj is UQueryBuilder<T>))
            {
                return false;
            }

            return Equals((UQueryBuilder<T>)obj);
        }

        public override int GetHashCode()
        {
            var hashCode = -949812380;
            hashCode = hashCode * -1521134295 + EqualityComparer<List<StyleSelector>>.Default.GetHashCode(m_StyleSelectors);
            hashCode = hashCode * -1521134295 + EqualityComparer<List<StyleSelector>>.Default.GetHashCode(styleSelectors);
            hashCode = hashCode * -1521134295 + EqualityComparer<List<StyleSelectorPart>>.Default.GetHashCode(m_Parts);
            hashCode = hashCode * -1521134295 + EqualityComparer<List<StyleSelectorPart>>.Default.GetHashCode(parts);
            hashCode = hashCode * -1521134295 + EqualityComparer<VisualElement>.Default.GetHashCode(m_Element);
            hashCode = hashCode * -1521134295 + EqualityComparer<List<RuleMatcher>>.Default.GetHashCode(m_Matchers);
            hashCode = hashCode * -1521134295 + m_Relationship.GetHashCode();
            hashCode = hashCode * -1521134295 + pseudoStatesMask.GetHashCode();
            hashCode = hashCode * -1521134295 + negatedPseudoStatesMask.GetHashCode();
            return hashCode;
        }
    }

    /// <summary>
    /// UQuery is a set of extension methods allowing you to select individual or collection of visualElements inside a complex hierarchy.
    /// </summary>
    public static class UQueryExtensions
    {
        private static UQueryState<VisualElement> SingleElementEmptyQuery = new UQueryBuilder<VisualElement>(null).Build();

        private static UQueryState<VisualElement> SingleElementNameQuery = new UQueryBuilder<VisualElement>(null).Name(String.Empty).Build();
        private static UQueryState<VisualElement> SingleElementClassQuery = new UQueryBuilder<VisualElement>(null).Class(String.Empty).Build();
        private static UQueryState<VisualElement> SingleElementNameAndClassQuery = new UQueryBuilder<VisualElement>(null).Name(String.Empty).Class(String.Empty).Build();

        private static UQueryState<VisualElement> SingleElementTypeQuery = new UQueryBuilder<VisualElement>(null).SingleBaseType().Build();
        private static UQueryState<VisualElement> SingleElementTypeAndNameQuery = new UQueryBuilder<VisualElement>(null).SingleBaseType().Name(String.Empty).Build();
        private static UQueryState<VisualElement> SingleElementTypeAndClassQuery = new UQueryBuilder<VisualElement>(null).SingleBaseType().Class(String.Empty).Build();
        private static UQueryState<VisualElement> SingleElementTypeAndNameAndClassQuery = new UQueryBuilder<VisualElement>(null).SingleBaseType().Name(String.Empty).Class(String.Empty).Build();

        /// <summary>
        /// Convenience overload, shorthand for `Query&lt;T&gt;.Build().First().`
        /// </summary>
        /// <param name="e">Root VisualElement on which the selector will be applied.</param>
        /// <param name="name">If specified, will select elements with this name.</param>
        /// <param name="classes">If specified, will select elements with the given class (not to be confused with Type).</param>
        /// <returns>The first element matching all the criteria, or null if none was found.</returns>
        public static T Q<T>(this VisualElement e, string name = null, params string[] classes) where T : VisualElement
        {
            return e.Query<T>(name, classes).Build().First();
        }

        /// <summary>
        /// Convenience overload, shorthand for `Query&lt;T&gt;.Build().First().`
        /// </summary>
        /// <param name="e">Root VisualElement on which the selector will be applied.</param>
        /// <param name="name">If specified, will select elements with this name.</param>
        /// <param name="classes">If specified, will select elements with the given class (not to be confused with Type).</param>
        /// <returns>The first element matching all the criteria, or null if none was found.</returns>
        public static VisualElement Q(this VisualElement e, string name = null, params string[] classes)
        {
            return e.Query<VisualElement>(name, classes).Build().First();
        }

        /// <summary>
        /// Convenience overload, shorthand for `Query&lt;T&gt;.Build().First().`
        /// </summary>
        /// <param name="e">Root VisualElement on which the selector will be applied.</param>
        /// <param name="name">If specified, will select elements with this name.</param>
        /// <param name="className">If specified, will select elements with the given class (not to be confused with Type).</param>
        /// <returns>The first element matching all the criteria, or null if none was found.</returns>
        public static T Q<T>(this VisualElement e, string name = null, string className = null) where T : VisualElement
        {
            if (typeof(T) == typeof(VisualElement))
            {
                return e.Q(name, className) as T;
            }

            UQueryState<VisualElement> query;

            if (name == null)
            {
                if (className == null)
                {
                    query = SingleElementTypeQuery.RebuildOn(e);
                    query.m_Matchers[0].complexSelector.selectors[0].parts[0] = StyleSelectorPart.CreatePredicate(UQuery.IsOfType<T>.s_Instance);
                    return query.First() as T;
                }

                query = SingleElementTypeAndClassQuery.RebuildOn(e);
                query.m_Matchers[0].complexSelector.selectors[0].parts[0] = StyleSelectorPart.CreatePredicate(UQuery.IsOfType<T>.s_Instance);
                query.m_Matchers[0].complexSelector.selectors[0].parts[1] = StyleSelectorPart.CreateClass(className);
                return query.First() as T;
            }

            if (className == null)
            {
                query = SingleElementTypeAndNameQuery.RebuildOn(e);
                query.m_Matchers[0].complexSelector.selectors[0].parts[0] = StyleSelectorPart.CreatePredicate(UQuery.IsOfType<T>.s_Instance);
                query.m_Matchers[0].complexSelector.selectors[0].parts[1] = StyleSelectorPart.CreateId(name);
                return query.First() as T;
            }


            query = SingleElementTypeAndNameAndClassQuery.RebuildOn(e);
            query.m_Matchers[0].complexSelector.selectors[0].parts[0] = StyleSelectorPart.CreatePredicate(UQuery.IsOfType<T>.s_Instance);
            query.m_Matchers[0].complexSelector.selectors[0].parts[1] = StyleSelectorPart.CreateId(name);
            query.m_Matchers[0].complexSelector.selectors[0].parts[2] = StyleSelectorPart.CreateClass(className);
            return query.First() as T;
        }

        internal static T MandatoryQ<T>(this VisualElement e, string name, string className = null) where T : VisualElement
        {
            var element = e.Q<T>(name, className);
            if (element == null)
                throw new MissingVisualElementException("Element not found: " + name);
            return element;
        }

        /// <summary>
        /// Convenience overload, shorthand for `Query&lt;T&gt;.Build().First().`
        /// </summary>
        /// <param name="e">Root VisualElement on which the selector will be applied.</param>
        /// <param name="name">If specified, will select elements with this name.</param>
        /// <param name="className">If specified, will select elements with the given class (not to be confused with Type).</param>
        /// <returns>The first element matching all the criteria, or null if none was found.</returns>
        public static VisualElement Q(this VisualElement e, string name = null, string className = null)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            UQueryState<VisualElement> query;

            if (name == null)
            {
                if (className == null)
                {
                    return SingleElementEmptyQuery.RebuildOn(e).First();
                }

                query = SingleElementClassQuery.RebuildOn(e);
                query.m_Matchers[0].complexSelector.selectors[0].parts[0] = StyleSelectorPart.CreateClass(className);
                return query.First();
            }

            if (className == null)
            {
                query = SingleElementNameQuery.RebuildOn(e);
                query.m_Matchers[0].complexSelector.selectors[0].parts[0] = StyleSelectorPart.CreateId(name);
                return query.First();
            }

            query = SingleElementNameAndClassQuery.RebuildOn(e);
            query.m_Matchers[0].complexSelector.selectors[0].parts[0] = StyleSelectorPart.CreateId(name);
            query.m_Matchers[0].complexSelector.selectors[0].parts[1] = StyleSelectorPart.CreateClass(className);
            return query.First();
        }

        internal static VisualElement MandatoryQ(this VisualElement e, string name, string className = null)
        {
            var element = e.Q<VisualElement>(name, className);
            if (element == null)
                throw new MissingVisualElementException("Element not found: " + name);
            return element;
        }

        /// <summary>
        /// Initializes a QueryBuilder with the specified selection rules.
        /// </summary>
        /// <param name="e">Root VisualElement on which the selector will be applied.</param>
        /// <param name="name">If specified, will select elements with this name.</param>
        /// <param name="classes">If specified, will select elements with the given class (not to be confused with Type).</param>
        /// <returns>QueryBuilder configured with the associated selection rules.</returns>
        public static UQueryBuilder<VisualElement> Query(this VisualElement e, string name = null, params string[] classes)
        {
            return e.Query<VisualElement>(name, classes);
        }

        /// <summary>
        /// Initializes a QueryBuilder with the specified selection rules.
        /// </summary>
        /// <param name="e">Root VisualElement on which the selector will be applied.</param>
        /// <param name="name">If specified, will select elements with this name.</param>
        /// <param name="className">If specified, will select elements with the given class (not to be confused with Type).</param>
        /// <returns>QueryBuilder configured with the associated selection rules.</returns>
        public static UQueryBuilder<VisualElement> Query(this VisualElement e, string name = null, string className = null)
        {
            return e.Query<VisualElement>(name, className);
        }

        /// <summary>
        /// Initializes a QueryBuilder with the specified selection rules.
        /// </summary>
        /// <param name="e">Root VisualElement on which the selector will be applied.</param>
        /// <param name="name">If specified, will select elements with this name.</param>
        /// <param name="classes">If specified, will select elements with the given class (not to be confused with Type).</param>
        /// <returns>QueryBuilder configured with the associated selection rules.</returns>
        public static UQueryBuilder<T> Query<T>(this VisualElement e, string name = null, params string[] classes) where T : VisualElement
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            var queryBuilder = new UQueryBuilder<VisualElement>(e).OfType<T>(name, classes);
            return queryBuilder;
        }

        /// <summary>
        /// Initializes a QueryBuilder with the specified selection rules.
        /// </summary>
        /// <param name="e">Root VisualElement on which the selector will be applied.</param>
        /// <param name="name">If specified, will select elements with this name.</param>
        /// <param name="className">If specified, will select elements with the given class (not to be confused with Type).</param>
        /// <returns>QueryBuilder configured with the associated selection rules.</returns>
        public static UQueryBuilder<T> Query<T>(this VisualElement e, string name = null, string className = null) where T : VisualElement
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            var queryBuilder = new UQueryBuilder<VisualElement>(e).OfType<T>(name, className);
            return queryBuilder;
        }

        /// <summary>
        /// Initializes a QueryBuilder with the specified selection rules.
        /// </summary>
        /// <param name="e">Root VisualElement on which the selector will be applied.</param>
        /// <returns>QueryBuilder configured with the associated selection rules.</returns>
        public static UQueryBuilder<VisualElement> Query(this VisualElement e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            return new UQueryBuilder<VisualElement>(e);
        }

        class MissingVisualElementException : Exception
        {
            public MissingVisualElementException()
            {
            }

            public MissingVisualElementException(string message)
                : base(message)
            {
            }
        }
    }
}
