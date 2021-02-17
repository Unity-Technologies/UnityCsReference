// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Samples
{
    internal class PopupFieldSnippet : ElementSnippet<PopupFieldSnippet>
    {
        internal override void Apply(VisualElement container)
        {
            /// <sample>
            // Note: PopupField has no UXML support because it is a generic type. See DropdownField instead.

            var choices = new List<string> { "First", "Second", "Third" };

            // Create a new field and assign it its value.
            var normalField = new PopupField<string>("Normal Field", choices, 0);
            normalField.value = "Second";
            container.Add(normalField);

            // Create a new field, disable it, and give it a style class.
            var styledField = new PopupField<string>("Styled Field", choices, 0);
            styledField.SetEnabled(false);
            styledField.AddToClassList("some-styled-field");
            styledField.value = normalField.value;
            container.Add(styledField);

            // Mirror value of uxml field into the C# field.
            normalField.RegisterCallback<ChangeEvent<string>>((evt) =>
            {
                styledField.value = evt.newValue;
            });
            /// </sample>
        }
    }
}
