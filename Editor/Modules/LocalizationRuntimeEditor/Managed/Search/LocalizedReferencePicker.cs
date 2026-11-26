// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine.Search;

namespace Unity.Localization.Editor.Search;

static class LocalizedReferencePicker
{
    internal static void Show(string title, Type entryInterface, Type assetType, Action<ResourceTableCollection, long> onPick)
    {
        var provider = new LocalizedReferenceSearchProvider(entryInterface, assetType);
        // The single-provider overload takes no flags, so go through the enumerable one.
        var context = SearchService.CreateContext(new[] { provider }, string.Empty, SearchFlags.UseSessionSettings);
        var state = new SearchViewState(context)
        {
            title = title,
            ignoreSaveSearches = true,
            hideTabs = true,
            queryBuilderEnabled = true,
            flags = SearchViewFlags.ObjectPicker | SearchViewFlags.DisableInspectorPreview,
            selectHandler = (item, cancelled) =>
            {
                if (cancelled)
                    return;
                // A null item is the picker's None row.
                if (item?.data is LocalizedReferenceSearchItem picked)
                    onPick(picked.Collection, picked.Entry.Id);
                else
                    onPick(null, 0);
            }
        };
        SearchService.ShowPicker(state);
    }
}
