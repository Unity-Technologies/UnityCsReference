// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Samples
{
    internal class ScrollerSnippet : ElementSnippet<ScrollerSnippet>
    {
        internal override void Apply(VisualElement container)
        {
            /// <sample>
            // Get a reference to the scroller from UXML and assign it its value.
            var uxmlField = container.Q<Scroller>("the-uxml-scroller");
            uxmlField.valueChanged += (v) => {};
            uxmlField.value = 42;

            // Create a new scroller, disable it, and give it a style class.
            var csharpField = new Scroller(0, 100, (v) => {}, SliderDirection.Vertical);
            csharpField.SetEnabled(false);
            csharpField.AddToClassList("some-styled-scroller");
            csharpField.value = uxmlField.value;
            container.Add(csharpField);

            // Mirror value of uxml scroller into the C# field.
            uxmlField.RegisterCallback<ChangeEvent<float>>((evt) =>
            {
                csharpField.value = evt.newValue;
            });
            /// </sample>
        }
    }
}
