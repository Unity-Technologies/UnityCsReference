// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace UnityEditor.Audio.UIElements;

class AudioRandomRangeSliderTracker : VisualElement
{
    [Preserve]
    public new class UxmlFactory : UxmlFactory<AudioRandomRangeSliderTracker, UxmlTraits> { }

    [Preserve]
    public new class UxmlTraits : VisualElement.UxmlTraits { }

    Slider m_ParentSlider;
    float m_PreviousWidth;
    Vector2 m_Range = Vector2.zero;

    public AudioRandomRangeSliderTracker()
    {
        RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }

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
        rangeTracker.SetRange(range);
        return rangeTracker;
    }

    internal void SetRange(Vector2 range)
    {
        m_Range = range;

        if (Mathf.Approximately(parent.contentRect.width, 0) || m_Range == Vector2.zero)
        {
            style.visibility = Visibility.Hidden;
            return;
        }

        var minValue = m_ParentSlider.value - Math.Abs(m_Range.x);
        minValue = Mathf.Clamp(minValue, m_ParentSlider.lowValue, m_ParentSlider.highValue);
        var maxValue = m_ParentSlider.value + Mathf.Abs(m_Range.y);
        maxValue = Mathf.Clamp(maxValue, m_ParentSlider.lowValue, m_ParentSlider.highValue);
        var minValueDelta = minValue - m_ParentSlider.lowValue;
        var maxValueDelta = maxValue - m_ParentSlider.lowValue;
        var minValueFraction = minValueDelta / m_ParentSlider.range;
        var maxValueFraction = maxValueDelta / m_ParentSlider.range;
        style.marginLeft = minValueFraction * parent.contentRect.width;
        style.marginRight = parent.contentRect.width - (maxValueFraction * parent.contentRect.width);
        style.visibility = Visibility.Visible;
    }

    void OnGeometryChanged(GeometryChangedEvent evt)
    {
        if (Mathf.Approximately(m_PreviousWidth, parent.contentRect.width))
        {
            return;
        }

        m_PreviousWidth = parent.contentRect.width;
        SetRange(m_Range);
    }
}
