// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Samples
{
    internal class ToggleSnippet : ElementSnippet<ToggleSnippet>
    {
        internal override void Apply(VisualElement container)
        {
            #region sample
            /// <sample>
            // Get a reference to the field from UXML and assign a value to it.
            var uxmlField = container.Q<Toggle>("the-uxml-field");
            uxmlField.value = true;

            // Create a new field, disable it, and give it a style class.
            var csharpField = new Toggle("C# Field");
            csharpField.value = false;
            csharpField.SetEnabled(false);
            csharpField.AddToClassList("some-styled-field");
            csharpField.value = uxmlField.value;
            container.Add(csharpField);

            // Mirror the value of the UXML field into the C# field.
            uxmlField.RegisterCallback<ChangeEvent<bool>>((evt) =>
            {
                csharpField.value = evt.newValue;
            });
            /// </sample>
            #endregion
        }
    }
}
