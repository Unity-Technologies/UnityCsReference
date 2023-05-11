// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Samples
{
    internal class LongFieldSnippet : ElementSnippet<LongFieldSnippet>
    {
        internal override void Apply(VisualElement container)
        {
            /// <sample>
            // Get a reference to the field from UXML and assign a value to it.
            var uxmlField = container.Q<LongField>("the-uxml-field");
            uxmlField.value = 42;

            // Create a new field, disable it, and give it a style class.
            var csharpField = new LongField("C# Field");
            csharpField.SetEnabled(false);
            csharpField.AddToClassList("some-styled-field");
            csharpField.value = uxmlField.value;
            container.Add(csharpField);

            // Mirror the value of the UXML field into the C# field.
            uxmlField.RegisterCallback<ChangeEvent<long>>((evt) =>
            {
                csharpField.value = evt.newValue;
            });
            /// </sample>
        }
    }
}
