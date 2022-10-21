// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    interface IQueryExpressionBlock
    {
        QueryBuilder builder { get; }
        IQuerySource source { get; }
        IBlockEditor editor { get; }

        void CloseEditor();
        void ApplyExpression(string searchText);
        void ApplyChanges();
    }

    static class ExpressionBlock
    {
        public static QueryBuilder Create(in string expression)
        {
            var embeddedBuilder = new QueryBuilder(expression) { @readonly = true };
            foreach (var b in embeddedBuilder.blocks)
            {
                b.@readonly = true;
                b.disableHovering = true;
            }
            return embeddedBuilder;
        }
    }

    class QueryExpressionBlock : QueryBlock
    {
        private SearchExpression m_Expression;
        private List<QueryBuilder> m_ArgumentBuilders;

        internal override bool canDisable => false;
        internal override bool canExclude => false;
        internal override bool wantsEvents => true;

        public QueryExpressionBlock(IQuerySource source, SearchExpression expression)
            : base(source)
        {
            name = ObjectNames.NicifyVariableName(expression.name);
            value = expression.outerText.ToString();

            m_Expression = expression;
            m_ArgumentBuilders = expression.parameters.Select(p => ExpressionBlock.Create(p.outerText.ToString())).ToList();
        }

        public override string ToString() => m_Expression.outerText.ToString();

        internal override IBlockEditor OpenEditor(in Rect rect)
        {
            return null;
        }

        internal override Color GetBackgroundColor() => QueryColors.expression;

        internal override void CreateBlockElement(VisualElement container)
        {
            AddLabel(container, name);
            AddSeparator(container);

            foreach (var builder in m_ArgumentBuilders)
            {
                foreach(var b in builder.EnumerateBlocks())
                   container.Add(b.CreateGUI());
                AddSeparator(container);
            }
        }
    }

    class QueryExpressionBlockEditor : SearchWindow, IBlockEditor
    {
        public EditorWindow window => this;
        public IQueryExpressionBlock block { get; protected set; }

        public static IBlockEditor Open(in Rect rect, IQueryExpressionBlock block)
        {
            var qb = block.builder;
            var searchFlags = SearchFlags.None;
            var searchContext = SearchService.CreateContext(qb.searchText, searchFlags);
            var viewState = new SearchViewState(searchContext,
                UnityEngine.Search.SearchViewFlags.OpenInBuilderMode |
                UnityEngine.Search.SearchViewFlags.DisableSavedSearchQuery |
                UnityEngine.Search.SearchViewFlags.Borderless |
                UnityEngine.Search.SearchViewFlags.DisableInspectorPreview)
            {
                ignoreSaveSearches = true
            };
            var w = Create<QueryExpressionBlockEditor>(viewState) as QueryExpressionBlockEditor;
            w.block = block;
            w.minSize = Vector2.zero;
            w.maxSize = new Vector2(600, 300);
            var popupRect = new Rect(rect.x, rect.yMax, rect.width, rect.height);
            var windowRect = new Rect(rect.x, rect.yMax + rect.height, rect.width, rect.height);
            w.ShowAsDropDown(popupRect, w.maxSize);
            w.position = windowRect;
            w.m_Parent.window.m_DontSaveToLayout = true;
            return w;
        }

        internal new void OnDisable()
        {
            Apply();
            block.CloseEditor();
            base.OnDisable();
        }

        protected override void UpdateAsyncResults()
        {
            base.UpdateAsyncResults();
            Apply();
        }

        void Apply()
        {
            block.ApplyExpression(context.searchText);
            block.ApplyChanges();
        }
    }
}
