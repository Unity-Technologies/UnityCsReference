// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Samples
{
    internal class MaskFieldSnippet : ElementSnippet<MaskFieldSnippet>
    {
        internal override void Apply(VisualElement container)
        {
            #region sample
            /// <sample>
            var choices = new List<string> { "First", "Second", "Third" };

            // Get a reference to the field from UXML and assign a value to it.
            var uxmlField = container.Q<MaskField>("the-uxml-field");
            uxmlField.value = 1;
            uxmlField.choices = choices;

            // Create a new field, disable it, and give it a style class.
            var csharpField = new MaskField("C# Field", choices, 0);
            csharpField.SetEnabled(false);
            csharpField.AddToClassList("some-styled-field");
            csharpField.value = uxmlField.value;
            container.Add(csharpField);

            // Mirror the value of the UXML field into the C# field.
            uxmlField.RegisterCallback<ChangeEvent<int>>((evt) =>
            {
                csharpField.value = evt.newValue;
            });
            /// </sample>
            #endregion
        }
    }
}
