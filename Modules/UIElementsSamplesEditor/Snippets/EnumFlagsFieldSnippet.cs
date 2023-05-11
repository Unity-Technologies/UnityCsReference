// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Samples
{
    internal class EnumFlagsFieldSnippet : ElementSnippet<EnumFlagsFieldSnippet>
    {
        [Flags]
        enum EnumFlags
        {
            First = 1,
            Second = 2,
            Third = 4
        }

        internal override void Apply(VisualElement container)
        {
            /// <sample>
            // Get a reference to the field from UXML,
            // initialize it with an Enum type,
            // and assign a value to it.
            var uxmlField = container.Q<EnumFlagsField>("the-uxml-field");
            uxmlField.Init(EnumFlags.First);
            uxmlField.value = EnumFlags.Second;

            // Create a new field, disable it, and give it a style class.
            var csharpField = new EnumFlagsField("C# Field", uxmlField.value);
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
