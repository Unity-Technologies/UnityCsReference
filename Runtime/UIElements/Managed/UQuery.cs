// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.StyleSheets;

namespace UnityEngine.Experimental.UIElements
{
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

        private abstract class UQueryMatcher : HierarchyTraversal
        {
            public override bool ShouldSkipElement(VisualElement element)
            {
                return false;
            }

            protected override bool MatchSelectorPart(VisualElement element, StyleSelector selector, StyleSelectorPart part)
            {
                if (part.type == StyleSelectorType.Predicate)
                {
                    IVisualPredicateWrapper w = part.tempData as IVisualPredicateWrapper;

                    return w != null && w.Predicate(element);
                }
                return base.MatchSelectorPart(element, selector, part);
            }

            public virtual void Run(VisualElement root, List<RuleMatcher> ruleMatchers)
            {
                Traverse(root, 0, ruleMatchers);
            }
        }

        private abstract class SingleQueryMatcher : UQueryMatcher
        {
            public VisualElement match { get; set; }

            public override void Run(VisualElement root, List<RuleMatcher> ruleMatchers)
            {
                match = null;
                base.Run(root, ruleMatchers);
            }
        }

        private class FirstQueryMatcher : SingleQueryMatcher
        {
            public override bool OnRuleMatchedElement(RuleMatcher matcher, VisualElement element)
            {
                if (match == null)
                    match = element;
                return true;
            }
        }

        private class LastQueryMatcher : SingleQueryMatcher
        {
            public override bool OnRuleMatchedElement(RuleMatcher matcher, VisualElement element)
            {
                match = element;
                return false;
            }
        }

        private class IndexQueryMatcher : SingleQueryMatcher
        {
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

            public override void Run(VisualElement root, List<RuleMatcher> ruleMatchers)
            {
                matchCount = -1;
                base.Run(root, ruleMatchers);
            }

            public override bool OnRuleMatchedElement(RuleMatcher matcher, VisualElement element)
            {
                ++matchCount;
                if (matchCount == _matchIndex)
                {
                    match = element;
                }

                return matchCount >= _matchIndex;
            }
        }


        //this makes it non-thread safe. But saves on allocations...
        private static FirstQueryMatcher s_First = new FirstQueryMatcher();
        private static LastQueryMatcher s_Last = new LastQueryMatcher();
        private static IndexQueryMatcher s_Index = new IndexQueryMatcher();

        public struct QueryBuilder<T> where T : VisualElement
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

            public QueryBuilder(VisualElement visualElement)
                : this()
            {
                m_Element = visualElement;
                m_Parts = null;
                m_StyleSelectors = null;
                m_Relationship = StyleSelectorRelationship.None;
                m_Matchers = new List<RuleMatcher>();
                pseudoStatesMask = negatedPseudoStatesMask = 0;
            }

            public QueryBuilder<T> Class(string classname)
            {
                AddClass(classname);
                return this;
            }

            public QueryBuilder<T> Name(string id)
            {
                AddName(id);
                return this;
            }

            public QueryBuilder<T2> Descendents<T2>(string name = null, params string[] classNames) where T2 : VisualElement
            {
                FinishCurrentSelector();
                AddType<T2>();
                AddName(name);
                AddClasses(classNames);
                return AddRelationship<T2>(StyleSelectorRelationship.Descendent);
            }

            public QueryBuilder<T2> Descendents<T2>(string name = null, string classname = null) where T2 : VisualElement
            {
                FinishCurrentSelector();
                AddType<T2>();
                AddName(name);
                AddClass(classname);
                return AddRelationship<T2>(StyleSelectorRelationship.Descendent);
            }

            public QueryBuilder<T2> Children<T2>(string name = null, params string[] classes) where T2 : VisualElement
            {
                FinishCurrentSelector();
                AddType<T2>();
                AddName(name);
                AddClasses(classes);
                return AddRelationship<T2>(StyleSelectorRelationship.Child);
            }

            public QueryBuilder<T2> Children<T2>(string name = null, string className = null) where T2 : VisualElement
            {
                FinishCurrentSelector();
                AddType<T2>();
                AddName(name);
                AddClass(className);
                return AddRelationship<T2>(StyleSelectorRelationship.Child);
            }

            public QueryBuilder<T2> OfType<T2>(string name = null, params string[] classes) where T2 : VisualElement
            {
                AddType<T2>();
                AddName(name);
                AddClasses(classes);
                return AddRelationship<T2>(StyleSelectorRelationship.None);
            }

            public QueryBuilder<T2> OfType<T2>(string name = null, string className = null) where T2 : VisualElement
            {
                AddType<T2>();
                AddName(name);
                AddClass(className);
                return AddRelationship<T2>(StyleSelectorRelationship.None);
            }

            public QueryBuilder<T> Where(Func<T, bool> selectorPredicate)
            {
                //we can't use a static instance as in the QueryState<T>.ForEach below since the query might be long lived
                parts.Add(StyleSelectorPart.CreatePredicate(new PredicateWrapper<T>(selectorPredicate)));
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
                    parts.Add(StyleSelectorPart.CreatePredicate(IsOfType<T2>.s_Instance));
            }

            private QueryBuilder<T> AddPseudoState(PseudoStates s)
            {
                pseudoStatesMask = pseudoStatesMask | (int)s;
                return this;
            }

            private QueryBuilder<T> AddNegativePseudoState(PseudoStates s)
            {
                negatedPseudoStatesMask = negatedPseudoStatesMask | (int)s;
                return this;
            }

            public QueryBuilder<T> Active()
            {
                return AddPseudoState(PseudoStates.Active);
            }

            public QueryBuilder<T> NotActive()
            {
                return AddNegativePseudoState(PseudoStates.Active);
            }

            public QueryBuilder<T> Visible()
            {
                return AddNegativePseudoState(PseudoStates.Invisible);
            }

            public QueryBuilder<T> NotVisible()
            {
                return AddPseudoState(PseudoStates.Invisible);
            }

            public QueryBuilder<T> Hovered()
            {
                return AddPseudoState(PseudoStates.Hover);
            }

            public QueryBuilder<T> NotHovered()
            {
                return AddNegativePseudoState(PseudoStates.Hover);
            }

            public QueryBuilder<T> Checked()
            {
                return AddPseudoState(PseudoStates.Checked);
            }

            public QueryBuilder<T> NotChecked()
            {
                return AddNegativePseudoState(PseudoStates.Checked);
            }

            public QueryBuilder<T> Selected()
            {
                return AddPseudoState(PseudoStates.Selected);
            }

            public QueryBuilder<T> NotSelected()
            {
                return AddNegativePseudoState(PseudoStates.Selected);
            }

            public QueryBuilder<T> Enabled()
            {
                return AddNegativePseudoState(PseudoStates.Disabled);
            }

            public QueryBuilder<T> NotEnabled()
            {
                return AddPseudoState(PseudoStates.Disabled);
            }

            public QueryBuilder<T> Focused()
            {
                return AddPseudoState(PseudoStates.Focus);
            }

            public QueryBuilder<T> NotFocused()
            {
                return AddNegativePseudoState(PseudoStates.Focus);
            }

            private QueryBuilder<T2> AddRelationship<T2>(StyleSelectorRelationship relationship) where T2 : VisualElement
            {
                return new QueryBuilder<T2>(m_Element)
                {
                    m_Matchers = m_Matchers,
                    m_Parts = m_Parts,
                    m_StyleSelectors = m_StyleSelectors,
                    m_Relationship = relationship,
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
                    m_Matchers.Add(new RuleMatcher { complexSelector = selector, simpleSelectorIndex = 0, depth = Int32.MaxValue });
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

            public QueryState<T> Build()
            {
                FinishSelector();
                return new QueryState<T>(m_Element, m_Matchers);
            }

            // Quick One-liners accessors
            public static implicit operator T(QueryBuilder<T> s)
            {
                return s.First();
            }

            public T First()
            {
                return Build().First();
            }

            public T Last()
            {
                return Build().Last();
            }

            public List<T> ToList()
            {
                return Build().ToList();
            }

            public void ToList(List<T> results)
            {
                Build().ToList(results);
            }

            public T AtIndex(int index)
            {
                return Build().AtIndex(index);
            }

            public void ForEach<T2>(List<T2> result, Func<T, T2> funcCall)
            {
                Build().ForEach(result, funcCall);
            }

            public List<T2> ForEach<T2>(Func<T, T2> funcCall)
            {
                return Build().ForEach(funcCall);
            }

            public void ForEach(Action<T> funcCall)
            {
                Build().ForEach(funcCall);
            }
        }

        public struct QueryState<T> where T : VisualElement
        {
            private readonly VisualElement m_Element;
            private readonly List<RuleMatcher> m_Matchers;

            internal QueryState(VisualElement element, List<RuleMatcher> matchers)
            {
                m_Element = element;
                m_Matchers = matchers;
            }

            public QueryState<T> RebuildOn(VisualElement element)
            {
                return new QueryState<T>(element, m_Matchers);
            }

            public T First()
            {
                s_First.Run(m_Element, m_Matchers);

                // We need to make sure we don't leak a ref to the VisualElement.
                var match = s_First.match as T;
                s_First.match = null;
                return match;
            }

            public T Last()
            {
                s_Last.Run(m_Element, m_Matchers);

                // We need to make sure we don't leak a ref to the VisualElement.
                var match = s_Last.match as T;
                s_Last.match = null;
                return match;
            }

            private class ListQueryMatcher : UQueryMatcher
            {
                public List<T> matches { get; set; }

                public override bool OnRuleMatchedElement(RuleMatcher matcher, VisualElement element)
                {
                    matches.Add(element as T);
                    return false;
                }

                public void Reset()
                {
                    matches = null;
                }
            }

            private static readonly ListQueryMatcher s_List = new ListQueryMatcher();

            public void ToList(List<T> results)
            {
                s_List.matches = results;
                s_List.Run(m_Element, m_Matchers);
                s_List.Reset();
            }

            public List<T> ToList()
            {
                List<T> result = new List<T>();
                ToList(result);
                return result;
            }

            public T AtIndex(int index)
            {
                s_Index.matchIndex = index;
                s_Index.Run(m_Element, m_Matchers);

                // We need to make sure we don't leak a ref to the VisualElement.
                var match = s_Index.match as T;
                s_Index.match = null;
                return match;
            }

            //Convoluted trick so save on allocating memory for delegates or lambdas
            private class ActionQueryMatcher : UQueryMatcher
            {
                internal Action<T> callBack { get; set; }

                public override bool OnRuleMatchedElement(RuleMatcher matcher, VisualElement element)
                {
                    T castedElement = element as T;

                    if (castedElement != null)
                    {
                        callBack(castedElement);
                    }

                    return false;
                }
            };

            private static ActionQueryMatcher s_Action = new ActionQueryMatcher();


            public void ForEach(Action<T> funcCall)
            {
                s_Action.callBack = funcCall;

                s_Action.Run(m_Element, m_Matchers);
                s_Action.callBack = null;
            }

            private class DelegateQueryMatcher<TReturnType> : UQueryMatcher
            {
                public Func<T, TReturnType> callBack { get; set; }

                public List<TReturnType> result { get; set; }

                public static DelegateQueryMatcher<TReturnType> s_Instance = new DelegateQueryMatcher<TReturnType>();

                public override bool OnRuleMatchedElement(RuleMatcher matcher, VisualElement element)
                {
                    T castedElement = element as T;

                    if (castedElement != null)
                    {
                        result.Add(callBack(castedElement));
                    }

                    return false;
                }
            }
            public void ForEach<T2>(List<T2> result, Func<T, T2> funcCall)
            {
                var matcher = DelegateQueryMatcher<T2>.s_Instance;

                matcher.callBack = funcCall;
                matcher.result = result;
                matcher.Run(m_Element, m_Matchers);
                matcher.callBack = null;
                matcher.result = null;
            }

            public List<T2> ForEach<T2>(Func<T, T2> funcCall)
            {
                List<T2> result = new List<T2>();
                ForEach(result, funcCall);
                return result;
            }
        }
    }

    public static class UQueryExtensions
    {
        public static T Q<T>(this VisualElement e, string name = null, params string[] classes) where T : VisualElement
        {
            return e.Query<T>(name, classes).Build().First();
        }

        public static T Q<T>(this VisualElement e, string name = null, string className = null) where T : VisualElement
        {
            return e.Query<T>(name, className).Build().First();
        }

        public static VisualElement Q(this VisualElement e, string name = null, params string[] classes)
        {
            return e.Query<VisualElement>(name, classes).Build().First();
        }

        public static VisualElement Q(this VisualElement e, string name = null, string className = null)
        {
            return e.Query<VisualElement>(name, className).Build().First();
        }

        public static UQuery.QueryBuilder<VisualElement> Query(this VisualElement e, string name = null, params string[] classes)
        {
            return e.Query<VisualElement>(name, classes);
        }

        public static UQuery.QueryBuilder<VisualElement> Query(this VisualElement e, string name = null, string className = null)
        {
            return e.Query<VisualElement>(name, className);
        }

        public static UQuery.QueryBuilder<T> Query<T>(this VisualElement e, string name = null, params string[] classes) where T : VisualElement
        {
            var queryBuilder = new UQuery.QueryBuilder<VisualElement>(e).OfType<T>(name, classes);
            return queryBuilder;
        }

        public static UQuery.QueryBuilder<T> Query<T>(this VisualElement e, string name = null, string className = null) where T : VisualElement
        {
            var queryBuilder = new UQuery.QueryBuilder<VisualElement>(e).OfType<T>(name, className);
            return queryBuilder;
        }

        public static UQuery.QueryBuilder<VisualElement> Query(this VisualElement e)
        {
            return new UQuery.QueryBuilder<VisualElement>(e);
        }
    }
}
