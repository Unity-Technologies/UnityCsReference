// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Samples
{
    internal class UnsignedIntegerFieldSnippet : ElementSnippet<UnsignedIntegerFieldSnippet>
    {
        internal override void Apply(VisualElement container)
        {
            /// <sample>
            // Get a reference to the field from UXML and assign a value to it.
            var uxmlField = container.Q<UnsignedIntegerField>("the-uxml-field");
            uxmlField.value = 42;

            // Create a new field, disable it, and give it a style class.
            var csharpField = new UnsignedIntegerField("C# Field");
            csharpField.SetEnabled(false);
            csharpField.AddToClassList("some-styled-field");
            csharpField.value = uxmlField.value;
            container.Add(csharpField);

            // Mirror the value of UXML field into the C# field.
            uxmlField.RegisterCallback<ChangeEvent<uint>>((evt) =>
            {
                csharpField.value = evt.newValue;
            });
            /// </sample>
        }
    }
}
