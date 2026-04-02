// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.View.Internals;
using Unity.Timeline.Foundation.ViewModel;
using Unity.Timeline.Foundation.Widgets;
using UnityEngine;

namespace Unity.Timeline.Foundation.View
{
    /// <summary>
    /// Use this class to customize the appearance of an item on a track.
    /// </summary>
    abstract class ItemElement : CanvasElement, IItemElement
    {
        const string k_Style = "itemElement";
        static readonly StylesheetResource k_Stylesheet = Internals.UIResources.StylesheetFactory.Get<ItemElement>();

        public Item item { get; private set; }

        protected ItemElement(ItemElementContext context)
        {
            k_Stylesheet.ApplyTo(this);
            this.AddToTimelineClassList(k_Style);

            item = context.item;
        }

        public override void PositionInCanvas(CanvasTransform canvasTransform)
        {
            float pixelLeft = canvasTransform.TimeToPixel(item.start);
            float width = canvasTransform.DurationToPixelWidth(item.duration);

            Vector3 pos = resolvedStyle.translate;
            style.translate = new Vector3(pixelLeft, pos.y, pos.z);
            style.width = width;
        }

        //TODO : ATL-1323 - Marker Track Should use new version of Track/Item Managers
        public void SetItem(Item newItem)
        {
            item = newItem;
            OnItemChanged();
        }

        public virtual void OnItemChanged() { }

        public bool selected { get; protected set; }

        public UniqueID ID => item.ID;

        public virtual void OnItemContentChanged() { }
    }
}
