// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Audio.UIElements;

class AudioRandomRangeSliderTracker : VisualElement
{
    [Serializable]
    public new class UxmlSerializedData : VisualElement.UxmlSerializedData
    {
        public override object CreateInstance() => new AudioRandomRangeSliderTracker();
    }

    Slider m_ParentSlider;
    float m_PreviousWidth;
    Vector2 m_Range = Vector2.zero;
    
    internal static AudioRandomRangeSliderTracker Create(Slider parentSlider, Vector2 range)
    {
        var dragContainer = UIToolkitUtilities.GetChildByName<VisualElement>(parentSlider, "unity-drag-container");
        var rangeTrackerAsset = UIToolkitUtilities.LoadUxml("UXML/Audio/AudioRandomRangeSliderTracker.uxml");
        var baseTracker = UIToolkitUtilities.GetChildByName<VisualElement>(parentSlider, "unity-tracker");
        var insertionIndex = dragContainer.IndexOf(baseTracker) + 1;
        var templateContainer = rangeTrackerAsset.Instantiate();
        dragContainer.Insert(insertionIndex, templateContainer);
        var rangeTracker = UIToolkitUtilities.GetChildAtIndex<AudioRandomRangeSliderTracker>(templateContainer, 0);
        rangeTracker.m_ParentSlider = parentSlider;
        rangeTracker.m_ParentSlider.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        rangeTracker.SetRange(range);
        return rangeTracker;
    }

    internal void SetRange(Vector2 range)
    {
        m_Range = range;
        var minValue = m_ParentSlider.value - Math.Abs(m_Range.x);
        minValue = Mathf.Clamp(minValue, m_ParentSlider.lowValue, m_ParentSlider.highValue);
        var maxValue = m_ParentSlider.value + Mathf.Abs(m_Range.y);
        maxValue = Mathf.Clamp(maxValue, m_ParentSlider.lowValue, m_ParentSlider.highValue);

        if (Mathf.Approximately(parent.contentRect.width, 0) || m_Range == Vector2.zero || Mathf.Approximately(minValue ,maxValue))
        {
            style.display = DisplayStyle.None;
            return;
        }

        var minValueDelta = minValue - m_ParentSlider.lowValue;
        var maxValueDelta = maxValue - m_ParentSlider.lowValue;

        var pxPerVal = parent.contentRect.width / m_ParentSlider.range;
        var translate = style.translate.value;
        translate.x = pxPerVal * minValueDelta;
        style.translate = translate;
        style.width = (maxValueDelta - minValueDelta) * pxPerVal;
        style.display = DisplayStyle.Flex;
    }

    static void OnGeometryChanged(GeometryChangedEvent evt)
    {
        var sliderTracker = UIToolkitUtilities.GetChildByClassName<AudioRandomRangeSliderTracker>(evt.elementTarget, "unity-audio-random-range-slider-tracker");
        if (Mathf.Approximately(sliderTracker.m_PreviousWidth, sliderTracker.parent.contentRect.width))
        {
            return;
        }

        sliderTracker.m_PreviousWidth = sliderTracker.parent.contentRect.width;
        sliderTracker.SetRange(sliderTracker.m_Range);
    }
}
