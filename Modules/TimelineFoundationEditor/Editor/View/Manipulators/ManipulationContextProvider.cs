// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.ViewModel;
using UnityEngine;

namespace Unity.Timeline.Foundation.View.Internals
{
    class ManipulationContextProvider : IManipulationContextProvider
    {
        readonly SequenceTreeView m_TreeView;
        public ISequenceViewModel viewModel { get; set; }
        public IManipulationHandler manipulationHandler { get; set; }

        public ManipulationContextProvider(SequenceTreeView treeView)
        {
            m_TreeView = treeView;
        }

        public IManipulationHandler GetManipulationHandler() => manipulationHandler;
        public ISequenceViewModel GetViewModel() => viewModel;
        public ItemElement GetElementFor(Item item) => m_TreeView.ContentLookup.GetItemElement(item);
        public TrackElement GetElementFor(Track track) => m_TreeView.ContentLookup.GetTrackElement(track);
        public ManipulationContext GetManipulationContext()
        {
            var builder = new ManipulationContextBuilder();
            SelectionData selectionData = viewModel.selectionData;

            if (selectionData.valid)
            {
                SelectionContainer selection = selectionData.selection;
                foreach (UniqueID clipId in selection.clips)
                {
                    ItemElement itemElement = m_TreeView.ContentLookup.GetItemElement(clipId);
                    if (itemElement != null)
                        builder.AddItem(itemElement.item);
                }
                foreach (UniqueID markerId in selection.markers)
                {
                    ItemElement itemElement = m_TreeView.ContentLookup.GetItemElement(markerId);
                    if (itemElement != null)
                        builder.AddItem(itemElement.item);
                }
            }

            return builder.CreateContext();
        }
        public ViewContext GetViewContext()
        {
            List<Item> visibleItems = new List<Item>();
            foreach (var track in m_TreeView.GetVisibleTracksInViewport())
                visibleItems.AddRange(track.Items);

            DiscreteTime localTime = viewModel.timeData.localToDisplayTimeTransform.IsValid() ?
                viewModel.timeData.DisplayToLocal(viewModel.timeData.displayTime) : DiscreteTime.Zero;

            return new ViewContext(localTime, visibleItems);
        }
    }
}
