// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: GraphView not yet converted
using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor.Search;

namespace UnityEditor.Experimental.GraphView
{
    [Serializable]
    internal class TemplateSearchViewModel : SearchViewModel
    {
        public TemplateSearchViewModel(SearchViewState state) : base(state) { }

        public event Action<IEnumerable<SearchItem>> incomingItemsCallback;
        public event Action refreshDoneCallback;

        public override void SetSearchText(string searchText, TextCursorPlacement moveCursor = TextCursorPlacement.MoveLineEnd)
        {
            context.searchText = searchText;
            RefreshItems(incomingItemsCallback, refreshDoneCallback);
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
