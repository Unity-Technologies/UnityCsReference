// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Samples
{
    internal class Mask64FieldSnippet : ElementSnippet<Mask64FieldSnippet>
    {
        internal override void Apply(VisualElement container)
        {
            #region sample
            /// <sample>
            var choices = new List<string> { "First", "Second", "Third" };

            // Get a reference to the field from UXML and assign a value to it.
            var uxmlField = container.Q<Mask64Field>("the-uxml-field");
            uxmlField.value = 1;
            uxmlField.choices = choices;

            // Create a new field, disable it, and give it a style class.
            var csharpField = new Mask64Field("C# Field", choices, (UInt64)0);
            csharpField.SetEnabled(false);
            csharpField.AddToClassList("some-styled-field");
            csharpField.value = uxmlField.value;
            container.Add(csharpField);

            // Mirror the value of the UXML field into the C# field.
            uxmlField.RegisterCallback<ChangeEvent<UInt64>>((evt) =>
            {
                csharpField.value = evt.newValue;
            });
            /// </sample>
            #endregion
        }
    }
}
