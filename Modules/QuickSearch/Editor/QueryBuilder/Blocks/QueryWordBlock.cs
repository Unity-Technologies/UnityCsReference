// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    class QueryAndOrBlock : QueryWordBlock
    {
        public QueryAndOrBlock(IQuerySource source, string combine)
            : base(source, combine)
        {
        }

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
            ApplyChanges();
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

        internal override void CreateBlockElement(VisualElement container)
        {
            var label = AddLabel(container, value);
            if (!@readonly)
            {
                label.AddToClassList(QueryBlock.arrowButtonClassName);
                label.RegisterCallback<ClickEvent>(OnOpenBlockEditor);
            }
        }

        internal override IBlockEditor OpenEditor(in Rect rect)
        {
            if (@readonly)
                return null;
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
        public QueryToggleBlock(IQuerySource source, string toggle)
            : base(source, toggle)
        {
            disabled = false;
        }

        internal override bool canExclude => false;
        internal override bool canOpenEditorOnValueClicked => false;
        internal override IBlockEditor OpenEditor(in Rect rect)
        {
            disabled = !disabled;
            ApplyChanges();
            return null;
        }

        public override string ToString()
        {
            if (disabled)
                return null;
            return base.ToString();
        }

        internal override Color GetBackgroundColor() => QueryColors.toggle;
    }
}
