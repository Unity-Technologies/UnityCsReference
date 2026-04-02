// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    interface IResultView : IDisposable
    {
        delegate void SelectionChangedEventHandler(ReadOnlySpan<int> selectedIndices);
        delegate void PopulateItemsContextMenuHandler(SearchItem searchItem, DropdownMenu menu);

        string ViewId { get; }
        bool ShowNoResultMessage { get; }
        bool UpdateNeeded { get; }

        event SelectionChangedEventHandler SelectionChanged;
        event PopulateItemsContextMenuHandler PopulateItemsContextMenu;

        void Refresh(RefreshFlags flags = RefreshFlags.Default);
        void OnGroupChanged(string prevGroupId, string newGroupId);
        void OnItemSourceChanged(ISearchList itemSource);
        void AddSaveQueryMenuItems(SearchContext context, GenericMenu menu);
        void Focus();
        void UpdateView();
        bool UpdateViewIncremental();
        bool UpdateViewIncrementalTimed(TimeSpan timeLimit);
        void SetSearchItemComparer(IComparer<SearchItem> searchItemComparer);
        void SetSelectionWithoutNotify(SearchSelection selection);

        internal int ComputeVisibleItemCapacity(float size, float height);
    }
}
