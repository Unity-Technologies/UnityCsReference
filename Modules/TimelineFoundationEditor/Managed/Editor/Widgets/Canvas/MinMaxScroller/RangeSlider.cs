// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets.Internals
{
    class RangeSlider : MinMaxSlider
    {
        const string k_UnitySliderDragElement = "unity-dragger";
        const string k_Thumb = "unity-thumb";
        const string k_UnityThumbMin = k_Thumb + "-min";
        const string k_UnityThumbMax = k_Thumb + "-max";
        const string k_UnityTracker = "unity-tracker";
        const string k_RangeHandle = "rangeHandle";
        const string k_ClassName = "timeRangeSlider";

        static readonly StylesheetResource k_Stylesheet = UIResources.StylesheetFactory.Get(
            $"{nameof(TimeRangeScroller)}/{nameof(RangeSlider)}");

        VisualElement m_UnitySliderDragElement;
        VisualElement m_UnityMinThumb;
        VisualElement m_SliderBody;
        VisualElement m_UnityMaxThumb;


        public RangeSlider()
        {
            k_Stylesheet.ApplyTo(this);
            this.AddToTimelineClassList(k_ClassName);
            m_UnitySliderDragElement = this.Q<VisualElement>(k_UnitySliderDragElement);
            if (m_UnitySliderDragElement == null)
                throw new NullReferenceException($"{typeof(MinMaxSlider).FullName} missing child element '{k_UnitySliderDragElement}'");

            m_UnitySliderDragElement.style.backgroundImage = null;
            m_UnitySliderDragElement.AddToTimelineClassList($"{k_ClassName}__dragger");

            m_SliderBody = new VisualElement() { name = "body" };
            m_SliderBody.AddToTimelineClassList($"{k_ClassName}-body");
            m_SliderBody.pickingMode = PickingMode.Ignore;
            m_UnitySliderDragElement.Insert(1, m_SliderBody);

            this.Q<VisualElement>(k_UnityTracker).RemoveFromHierarchy();

            m_UnityMinThumb = this.Q<VisualElement>(k_UnityThumbMin);
            if (m_UnityMinThumb == null)
                throw new NullReferenceException($"{typeof(MinMaxSlider).FullName} missing child element '{k_UnityThumbMin}'");
            m_UnityMinThumb.AddToTimelineClassList(k_RangeHandle);
            m_UnityMinThumb.AddToTimelineClassList($"{k_RangeHandle}__min");
            m_UnityMinThumb.RegisterCallback<CustomStyleResolvedEvent>(ThumbCustomStyleResolved);

            m_UnityMaxThumb = this.Q<VisualElement>(k_UnityThumbMax);
            if (m_UnityMaxThumb == null)
                throw new NullReferenceException($"{typeof(MinMaxSlider).FullName} missing child element '{k_UnityThumbMax}'");
            m_UnityMaxThumb.AddToTimelineClassList(k_RangeHandle);
            m_UnityMaxThumb.AddToTimelineClassList($"{k_RangeHandle}__max");
            m_UnityMaxThumb.RegisterCallback<CustomStyleResolvedEvent>(ThumbCustomStyleResolved);

            m_UnitySliderDragElement.RegisterCallback<GeometryChangedEvent>(GeometryChanged);

            minValue = lowLimit = 0;
            maxValue = highLimit = 100;
        }

        static readonly CustomStyleProperty<float> k_RangeHandleWidth = new("--range-handle-min-width");
        void ThumbCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (evt.target is VisualElement thumb)
            {
                if (thumb.customStyle.TryGetValue(k_RangeHandleWidth, out float w))
                {
                    thumb.style.minWidth = w;
                    thumb.style.width = w;
                }
            }
        }

        void GeometryChanged(GeometryChangedEvent evt)
        {
            m_SliderBody.style.width = m_UnitySliderDragElement.layout.width - (m_UnityMinThumb.layout.width + m_UnityMaxThumb.layout.width);
        }

        public void SetRange(float min, float max)
        {
            if (max > highLimit)
            {
                highLimit = max;
            }

            SetValueWithoutNotify(new Vector2(min, max));
        }

        public void SetLimits(float low, float high)
        {
            lowLimit = low;
            if (high >= maxValue)
            {
                highLimit = high;
            }
        }
    }
}
