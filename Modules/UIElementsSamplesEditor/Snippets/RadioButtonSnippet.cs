// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Samples
{
    internal class RadioButtonSnippet : ElementSnippet<RadioButtonSnippet>
    {
        internal override void Apply(VisualElement container)
        {
            #region sample
            /// <sample>
            // Note: See also RadioButtonGroup in the ChoiceField section of UI Toolkit Samples

            // Get a reference to the first radio button from UXML and assign it its value.
            var uxmlField1 = container.Q<RadioButton>("the-uxml-field1");
            var uxmlField2 = container.Q<RadioButton>("the-uxml-field2");
            uxmlField1.value = true;

            // Create two RadioButtons in a separate group, disable them, and give them a style class.
            var groupBox = new GroupBox();
            container.Add(groupBox);

            var csharpField1 = new RadioButton("C# Field 1");
            csharpField1.SetEnabled(false);
            csharpField1.AddToClassList("some-styled-field");
            groupBox.Add(csharpField1);

            var csharpField2 = new RadioButton("C# Field 2");
            csharpField2.SetEnabled(false);
            csharpField2.AddToClassList("some-styled-field");
            groupBox.Add(csharpField2);

            csharpField1.value = uxmlField1.value;

            // Mirror value of uxml field into the C# field.
            uxmlField1.RegisterCallback<ChangeEvent<bool>>((evt) =>
            {
                csharpField1.value = evt.newValue;
            });
            uxmlField2.RegisterCallback<ChangeEvent<bool>>((evt) =>
            {
                csharpField2.value = evt.newValue;
            });
            /// </sample>
            #endregion
        }
    }
}
