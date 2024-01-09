// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Search
{
    class QueryAddNewBlock : QueryBlock, IBlockSource
    {
        internal override bool wantsEvents => true;
        internal override bool draggable => false;

        public override string ToString() => null;
        internal override IBlockEditor OpenEditor(in Rect rect) => AddBlock(rect);

        public QueryAddNewBlock(IQuerySource source)
            : base(source)
        {
            hideMenu = true;
        }

        internal override Rect Layout(in Vector2 at, in float availableSpace)
        {
            return GetRect(at, 20f, 20f);
        }

        internal override void Draw(in Rect blockRect, in Vector2 mousePosition)
        {
            if (EditorGUI.DropdownButton(blockRect, Styles.QueryBuilder.createContent, FocusType.Passive, Styles.dropdownItem))
                AddBlock(blockRect);
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

    class QueryInsertBlock : IBlockSource
    {
        private readonly IBlockSource insertAfter;
        private readonly IBlockSource insertWith;

        public QueryInsertBlock(IBlockSource insertAfter, IBlockSource insertWith)
        {
            this.insertAfter = insertAfter;
            this.insertWith = insertWith;
        }

        public string name => insertAfter.name;
        public string editorTitle => insertAfter.editorTitle;
        public SearchContext context => insertAfter.context;
        public bool formatNames => insertAfter.formatNames;

        public IEnumerable<SearchProposition> FetchPropositions() => insertWith.FetchPropositions();
        public void Apply(in SearchProposition searchProposition) => insertWith.Apply(searchProposition);
        public void CloseEditor() => insertAfter.CloseEditor();
    }
}
