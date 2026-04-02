// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Timeline.Foundation.ViewModel;
using Unity.Timeline.Foundation.Widgets;
using UnityEngine;

namespace Unity.Timeline.Foundation.View
{
    class MarkerElement : ItemElement, ISelectableElement
    {
        const string k_ClassName = "markerElement";
        static readonly StylesheetResource k_Stylesheet = Internals.UIResources.StylesheetFactory.Get<MarkerElement>();

        protected Item marker => item;

        protected MarkerElement(ItemElementContext context) : base(context)
        {
            k_Stylesheet.ApplyTo(this);
            this.AddToTimelineClassList(k_ClassName);
        }

        public bool supportsMultiSelect => true;

        public virtual void OnSelectionStateChanged(bool selected)
        {
            this.selected = selected;
        }

        public override void PositionInCanvas(CanvasTransform canvasTransform)
        {
            float pixelLeft = canvasTransform.TimeToPixel(item.start);
            style.left = pixelLeft;
        }
    }
}
