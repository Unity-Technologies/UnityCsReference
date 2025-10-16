// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.Search
{
    interface IResultView : IDisposable
    {
        Rect rect { get; }
        float itemSize { get; }
        bool showNoResultMessage { get; }

        void Refresh(RefreshFlags flags = RefreshFlags.Default);
        void OnGroupChanged(string prevGroupId, string newGroupId);
        void OnItemSourceChanged(ISearchList itemSource);
        void AddSaveQueryMenuItems(SearchContext context, GenericMenu menu);
        void Focus();
        void UpdateView();

        internal int ComputeVisibleItemCapacity(float size, float height);
    }
}
