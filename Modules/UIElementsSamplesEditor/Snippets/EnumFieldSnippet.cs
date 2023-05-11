// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Samples
{
    internal class EnumFieldSnippet : ElementSnippet<EnumFieldSnippet>
    {
        internal override void Apply(VisualElement container)
        {
            /// <sample>
            // Get a reference to the field from UXML,
            // initialize it with an Enum type,
            // and assign a value to it.
            var uxmlField = container.Q<EnumField>("the-uxml-field");
            uxmlField.Init(TextAlignment.Center);
            uxmlField.value = TextAlignment.Left;

            // Create a new field, disable it, and give it a style class.
            var csharpField = new EnumField("C# Field", TextAlignment.Center);
            csharpField.SetEnabled(false);
            csharpField.AddToClassList("some-styled-field");
            csharpField.value = uxmlField.value;
            container.Add(csharpField);

            // Mirror the value of the UXML field into the C# field.
            uxmlField.RegisterCallback<ChangeEvent<Enum>>((evt) =>
            {
                csharpField.value = evt.newValue;
            });
            /// </sample>
        }
    }
}
