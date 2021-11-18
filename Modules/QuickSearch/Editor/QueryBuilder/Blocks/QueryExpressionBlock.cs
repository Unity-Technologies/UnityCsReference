// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Search
{
    interface IQueryExpressionBlock
    {
        QueryBuilder builder { get; }
        IQuerySource source { get; }
        IBlockEditor editor { get; }

        void CloseEditor();
        void ApplyExpression(string searchText);
        void DrawArrow(in Rect blockRect, in Vector2 mousePosition, QueryContent arrayContent);
    }

    static class ExpressionBlock
    {
        public static QueryBuilder Create(in string expression)
        {
            var embeddedBuilder = new QueryBuilder(expression) { drawBackground = false, @readonly = true };
            foreach (var b in embeddedBuilder.blocks)
            {
                b.@readonly = true;
                b.disableHovering = true;
            }
            return embeddedBuilder;
        }

        public static float Layout(in QueryBuilder builder, in float availableSpace)
        {
            builder.LayoutBlocks(availableSpace);
            return builder.width;
        }

        public static Rect Draw(in float x, in Rect blockRect, in QueryBuilder builder)
        {
            var valueRect = new Rect(x - 5f, blockRect.yMin - 5f, builder.width + 24f, builder.height);
            builder.Draw(Event.current, valueRect, createLayout: false);
            return valueRect;
        }
    }

    class QueryExpressionBlock : QueryBlock
    {
        private SearchExpression m_Expression;
        private List<QueryBuilder> m_ArgumentBuilders;

        public override bool canDisable => false;
        public override bool canExclude => false;
        public override bool wantsEvents => true;
        //public QueryBuilder builder => m_Builder;

        public QueryExpressionBlock(IQuerySource source, SearchExpression expression)
            : base(source)
        {
            name = ObjectNames.NicifyVariableName(expression.name);
            value = expression.outerText.ToString();

            m_Expression = expression;
            m_ArgumentBuilders = expression.parameters.Select(p => ExpressionBlock.Create(p.outerText.ToString())).ToList();
        }

        public override string ToString() => m_Expression.outerText.ToString();

        public override IBlockEditor OpenEditor(in Rect rect)
        {
            return null;
           // var screenRect = new Rect(rect.position + context.searchView.position.position, rect.size);
           // return QueryExpressionBlockEditor.Open(screenRect, this);
        }

        protected override Color GetBackgroundColor() => QueryColors.expression;

        public override Rect Layout(in Vector2 at, in float availableSpace)
        {
            var labelStyle = Styles.QueryBuilder.label;
            var nameContent = labelStyle.CreateContent(name);

            var editorWidth = 0f;
            foreach (var b in m_ArgumentBuilders)
            {
                b.LayoutBlocks(availableSpace);
                editorWidth += b.width;
            }

            var blockWidth = nameContent.width + editorWidth + labelStyle.margin.horizontal * 2f + blockExtraPadding;
            return GetRect(at, blockWidth, blockHeight);
        }

        protected override void Draw(in Rect blockRect, in Vector2 mousePosition)
        {
            var extendedBlockRect = blockRect;
            var labelStyle = Styles.QueryBuilder.label;
            var nameContent = labelStyle.CreateContent(name);

            if (Event.current.type == EventType.Repaint)
                DrawBackground(extendedBlockRect, mousePosition);

            var nameRect = DrawName(blockRect, mousePosition, nameContent);
            var x = nameRect.xMax;
            foreach (var b in m_ArgumentBuilders)
            {
                var builderRect = ExpressionBlock.Draw(x, blockRect, b);
                x = builderRect.xMax;
            }

//             var addNewArgRect = new Rect(blockRect.xMax - 22f, blockRect.yMin, 20f, blockRect.height-2f);
//             if (EditorGUI.DropdownButton(addNewArgRect, Styles.QueryBuilder.createContent, FocusType.Passive, Styles.dropdownItem))
//                 Debug.LogWarning("TODO: Add argument");

            if (Event.current.type == EventType.Repaint)
                DrawBorders(extendedBlockRect, mousePosition);
        }
    }

    class QueryExpressionBlockEditor : QuickSearch, IBlockEditor
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
                UnityEngine.Search.SearchViewFlags.DisableInspectorPreview);
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
            block.CloseEditor();
            block.source.Apply();
        }
    }
}
