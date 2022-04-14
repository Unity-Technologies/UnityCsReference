// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Search
{
    class QueryAreaBlock : QueryBlock
    {
        public const string title = "Select Search Area";
        public string filterId { get; private set; }

        internal override bool canExclude => false;
        internal override bool canDisable => false;
        internal override bool draggable => false;

        public QueryAreaBlock(IQuerySource source, in SearchProvider provider)
            : this(source, provider.name, provider.filterId)
        {
            editorTitle = title;
        }

        public QueryAreaBlock(in IQuerySource source, in string providerName, in string filterId)
            : base(source)
        {
            name = "in";
            Apply(providerName, filterId);
        }

        public override void Apply(in SearchProposition searchProposition)
        {
            if (searchProposition.data is SearchProvider provider)
                Apply(provider.name, provider.filterId);
            source.Apply();
        }

        private void Apply(in string providerName, in string filterId)
        {
            this.value = providerName;
            this.filterId = filterId;
        }

        internal override IEnumerable<SearchProposition> FetchPropositions()
        {
            return FetchPropositions(context);
        }

        public static IEnumerable<SearchProposition> FetchPropositions(SearchContext context)
        {
            foreach (var p in context.GetProviders().Where(p => !p.isExplicitProvider).Concat(
                context.GetProviders().Where(p => p.isExplicitProvider)))
            {
                if (ExcludeProviderProposition(p.id))
                    continue;
                yield return new SearchProposition($"{p.name} ({p.filterId})", p.filterId, p.id, p.priority, Icons.quicksearch, p, color: QueryColors.area);
            }
        }

        private static bool ExcludeProviderProposition(in string id)
        {
            return string.Equals(id, "expression", System.StringComparison.Ordinal);
        }

        internal override Color GetBackgroundColor()
        {
            return QueryColors.area;
        }

        public override string ToString()
        {
            return filterId;
        }
    }
}
