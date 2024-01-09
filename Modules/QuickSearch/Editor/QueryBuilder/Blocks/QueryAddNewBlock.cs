// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    class QueryAddNewBlock : QueryBlock, IBlockSource
    {
        internal override bool wantsEvents => true;
        internal override bool draggable => false;

        static GUIContent createContent = EditorGUIUtility.IconContent("Toolbar Plus More", "|Add new query block (Tab)");

        public override string ToString() => null;
        internal override IBlockEditor OpenEditor(in Rect rect) => AddBlock(rect);

        public QueryAddNewBlock(IQuerySource source)
            : base(source)
        {
            hideMenu = true;
        }

        internal override Color GetBackgroundColor()
        {
            return Color.clear;
        }

        internal override void UpdateBackgroundColor(bool hovered = false)
        {
            if (hovered)
                style.backgroundColor = QueryColors.backgroundHoverTint;
            else
                style.backgroundColor = GetBackgroundColor();
        }

        internal override void CreateBlockElement(VisualElement container)
        {
            AddImageButton(container, createContent.image, createContent.tooltip, evt => AddBlock(container.worldBound));
        }

        private IBlockEditor AddBlock(in Rect buttonRect)
        {
            var title = source.context.empty ? QueryAreaBlock.title : "Add Search Filter";
            return QuerySelector.Open(buttonRect, this, title);
        }

        public override void Apply(in SearchProposition searchProposition)
        {
            source.AddProposition(searchProposition);
        }

        IEnumerable<SearchProposition> IBlockSource.FetchPropositions()
        {
            var options = new SearchPropositionOptions(string.Empty,
                SearchPropositionFlags.IgnoreRecents |
                SearchPropositionFlags.QueryBuilder |
                (source.context.empty ? SearchPropositionFlags.ForceAllProviders : SearchPropositionFlags.None));
            if (source.context.empty)
            {
                var areaPropositions = QueryAreaBlock.FetchPropositions(context);
                var allOtherPropositions = new[] { SearchProposition.CreateSeparator() }.Concat(SearchProposition.Fetch(context, options).OrderBy(p => p));
                return areaPropositions.Count() > 0 ?
                    areaPropositions.Concat(allOtherPropositions) :
                    allOtherPropositions;
            }
            else
            {
                return SearchProposition.Fetch(context, options).OrderBy(p => p);
            }
        }
    }
}
