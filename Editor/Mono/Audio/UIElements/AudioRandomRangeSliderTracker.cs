// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Transactions;
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

    static readonly CustomStyleProperty<Color> s_TrackerEnabledColorProperty = new("--tracker-color");

    Slider m_ParentSlider;
    Vector2 m_Range = Vector2.zero;
    Color m_TrackerEnabledColor;
    Color m_TrackerDisabledColor = Color.gray;

    static void CustomStylesResolved(CustomStyleResolvedEvent evt)
    {
        var element = (AudioRandomRangeSliderTracker)evt.currentTarget;

        element.UpdateCustomStyles();
    }

    void UpdateCustomStyles()
    {
        if (customStyle.TryGetValue(s_TrackerEnabledColorProperty, out var trackerColor))
        {
            m_TrackerEnabledColor = trackerColor;
        }
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

        rangeTracker.SetRange(range);
        rangeTracker.m_ParentSlider = parentSlider;
        rangeTracker.generateVisualContent += GenerateVisualContent;
        rangeTracker.RegisterCallback<CustomStyleResolvedEvent>(CustomStylesResolved);
        rangeTracker.m_ParentSlider.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        rangeTracker.RegisterCallback<PropertyChangedEvent>(OnPropertyChanged);

        return rangeTracker;
    }

    internal void SetRange(Vector2 range)
    {
        m_Range = range;

        MarkDirtyRepaint();
    }

    static void OnGeometryChanged(GeometryChangedEvent evt)
    {
        var sliderTracker = UIToolkitUtilities.GetChildByClassName<AudioRandomRangeSliderTracker>(evt.elementTarget, "unity-audio-random-range-slider-tracker");

        sliderTracker.SetRange(sliderTracker.m_Range);
    }

    static void OnPropertyChanged(PropertyChangedEvent evt)
    {
        var sliderTracker = evt.elementTarget;

        if (evt.property == "enabledSelf")
        {
            sliderTracker.MarkDirtyRepaint();
        }
    }

    // Maps 'x' from the range '[x_min; x_max]' to the range '[y_min; y_max]'.
    static float Map(float x, float x_min, float x_max, float y_min, float y_max)
    {
        var a = (x_max - x) / (x_max - x_min);
        var b = (x - x_min) / (x_max - x_min);

        return a * y_min + b * y_max;
    }

    static void GenerateVisualContent(MeshGenerationContext context)
    {
        var painter2D = context.painter2D;
        var sliderTracker = context.visualElement as AudioRandomRangeSliderTracker;
        var range = sliderTracker.m_Range;
        var parentSlider = sliderTracker.m_ParentSlider;
        var contentRect = context.visualElement.contentRect;

        // Offset the range so it is centered around the parent slider's current value.
        range.x += parentSlider.value;
        range.y += parentSlider.value;

        // Measured from a screenshot of the slider. The value is in pixels.
        var sliderHeadWidth = 10.0f;

        // Map the range from the slider value range (e.g. dB) to the horizontal span of the content-rect (px).
        var left  = Map(range.y, parentSlider.lowValue, parentSlider.highValue, contentRect.xMin + sliderHeadWidth / 2.0f, contentRect.xMax - sliderHeadWidth / 2.0f);
        var right = Map(range.x, parentSlider.lowValue, parentSlider.highValue, contentRect.xMin + sliderHeadWidth / 2.0f, contentRect.xMax - sliderHeadWidth / 2.0f);

        // Clamp the mapped range so that it lies within the boundaries of the content-rect.
        left =  Mathf.Clamp(left, contentRect.xMin, contentRect.xMax);
        right = Mathf.Clamp(right, contentRect.xMin, contentRect.xMax);

        // Draw the tracker.
        painter2D.fillColor = sliderTracker.enabledSelf ? sliderTracker.m_TrackerEnabledColor : sliderTracker.m_TrackerDisabledColor;
        painter2D.BeginPath();
        painter2D.MoveTo(new Vector2(left, contentRect.yMin));
        painter2D.LineTo(new Vector2(right, contentRect.yMin));
        painter2D.LineTo(new Vector2(right, contentRect.yMax));
        painter2D.LineTo(new Vector2(left, contentRect.yMax));
        painter2D.ClosePath();
        painter2D.Fill();
    }
}
