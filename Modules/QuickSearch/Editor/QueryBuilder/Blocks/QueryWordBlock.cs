// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Search
{
    class QueryAndOrBlock : QueryWordBlock
    {
        public QueryAndOrBlock(IQuerySource source, string combine)
            : base(source, combine)
        {
        }

        internal override Rect openRect => drawRect;

        internal override Color GetBackgroundColor()
        {
            return QueryColors.combine;
        }

        internal override IBlockEditor OpenEditor(in Rect rect)
        {
            return QuerySelector.Open(rect, this);
        }

        internal override IEnumerable<SearchProposition> FetchPropositions()
        {
            return BuiltInQueryBuilderPropositions(null);
        }

        public override void Apply(in SearchProposition searchProposition)
        {
            value = searchProposition.replacement;
            source.Apply();
        }

        public static IEnumerable<SearchProposition> BuiltInQueryBuilderPropositions(string category = "Operators")
        {
            var icon = Utils.LoadIcon("LayoutElement Icon");
            yield return new SearchProposition(category: category, label: "OR", replacement: "or", icon: icon, color: QueryColors.combine);
            yield return new SearchProposition(category: category, label: "AND", replacement: "and", icon: icon, color: QueryColors.combine);
        }
    }

    class QueryWordBlock : QueryBlock
    {
        public QueryWordBlock(IQuerySource source, SearchNode node)
            : this(source, node.searchValue)
        {
            if (node.rawSearchValueStringView.HasQuotes())
                explicitQuotes = true;
        }

        public QueryWordBlock(IQuerySource source, string searchValue)
            : base(source)
        {
            name = string.Empty;
            value = searchValue;
        }

        internal override Rect Layout(in Vector2 at, in float availableSpace)
        {
            var wordContent = Styles.QueryBuilder.label.CreateContent(value);
            var widgetWidth = wordContent.expandedWidth;
            return GetRect(at, widgetWidth, blockHeight);
        }

        internal override void Draw(in Rect widgetRect, in Vector2 mousePosition)
        {
            var wordContent = Styles.QueryBuilder.label.CreateContent(value);
            var widgetWidth = wordContent.expandedWidth;

            DrawBackground(widgetRect, mousePosition);
            var wordRect = new Rect(widgetRect.x + wordContent.style.margin.left, widgetRect.y - 1f, widgetWidth, widgetRect.height);
            wordContent.Draw(wordRect, mousePosition);
            DrawBorders(widgetRect, mousePosition);
        }

        internal override IBlockEditor OpenEditor(in Rect rect)
        {
            var screenRect = new Rect(rect.position + context.searchView.position.position, rect.size);
            return QueryTextBlockEditor.Open(screenRect, this);
        }

        internal override Color GetBackgroundColor()
        {
            return QueryColors.word;
        }

        public override string ToString()
        {
            return EscapeLiteralString(value);
        }
    }

    class QueryToggleBlock : QueryWordBlock
    {
        public bool active { get; set; }

        public QueryToggleBlock(IQuerySource source, string toggle)
            : base(source, toggle)
        {
            active = true;
        }

        internal override bool canExclude => false;
        internal override bool canOpenEditorOnValueClicked => false;
        internal override IBlockEditor OpenEditor(in Rect rect)
        {
            active = !active;
            source.Apply();
            return null;
        }

        internal override void Draw(in Rect widgetRect, in Vector2 mousePosition)
        {
            var oldColor = GUI.color;
            if (!active)
                GUI.color *= new Color(1f, 1f, 1f, 0.5f);

            base.Draw(widgetRect, mousePosition);
            GUI.color = oldColor;
        }

        public override string ToString()
        {
            if (!active)
                return null;
            return base.ToString();
        }

        internal override Rect openRect => drawRect;
        internal override Color GetBackgroundColor() => QueryColors.toggle;
    }
}
