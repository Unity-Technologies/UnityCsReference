// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Samples
{
    internal class SliderSnippet : ElementSnippet<SliderSnippet>
    {
        internal override void Apply(VisualElement container)
        {
            #region sample
            // Get a reference to the slider from UXML and assign it its value.
            var uxmlSlider = container.Q<Slider>("the-uxml-slider");
            uxmlSlider.value = 42.2f;

            // Create a new slider, disable it, and give it a style class.
            var csharpSlider = new Slider("C# Slider", 0, 100);
            csharpSlider.SetEnabled(false);
            csharpSlider.AddToClassList("some-styled-slider");
            csharpSlider.value = uxmlSlider.value;
            container.Add(csharpSlider);

            // Mirror value of uxml slider into the C# field.
            uxmlSlider.RegisterCallback<ChangeEvent<float>>((evt) =>
            {
                csharpSlider.value = evt.newValue;
            });
            #endregion
        }
    }
}
