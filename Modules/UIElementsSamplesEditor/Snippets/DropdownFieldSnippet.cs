// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Samples
{
    internal class DropdownFieldSnippet : ElementSnippet<DropdownFieldSnippet>
    {
        internal override void Apply(VisualElement container)
        {
            /// <sample>
            // You can provide the list of choices by code, or by comma separated values in UXML
            // <DropdownField .... choices="Option 1,Option 2,Option 3" .... />
            var choices = new List<string> { "Option 1", "Option 2", "Option 3" };

            // Get a reference to the dropdown field from UXML and assign a value to it.
            var uxmlField = container.Q<DropdownField>("the-uxml-field");
            uxmlField.choices = choices;
            uxmlField.value = choices[0];

            // Create a new dropdown with the same choices, disable it, and give it a style class.
            var csharpField = new DropdownField("C# Field", choices, 0);
            csharpField.SetEnabled(false);
            csharpField.AddToClassList("some-styled-field");
            container.Add(csharpField);

            // Mirror the value of the UXML field into the C# field.
            uxmlField.RegisterCallback<ChangeEvent<string>>((evt) =>
            {
                csharpField.value = evt.newValue;
            });
            /// </sample>
        }
    }
}
