// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Samples
{
    internal class BoundsFieldSnippet : ElementSnippet<BoundsFieldSnippet>
    {
        internal override void Apply(VisualElement container)
        {
            #region sample
            /// <sample>
            // Get a reference to the field from UXML and assign it its value.
            var uxmlField = container.Q<BoundsField>("the-uxml-field");
            uxmlField.value = new Bounds(new Vector3(1.1f, 2.2f, 3.3f), new Vector3(2.2f, 4.4f, 6.6f));

            // Create a new field, disable it, and give it a style class.
            var csharpField = new BoundsField("C# Field");
            csharpField.SetEnabled(false);
            csharpField.AddToClassList("some-styled-field");
            csharpField.value = uxmlField.value;
            container.Add(csharpField);

            // Mirror value of uxml field into the C# field.
            uxmlField.RegisterCallback<ChangeEvent<Bounds>>((evt) =>
            {
                csharpField.value = evt.newValue;
            });
            /// </sample>
            #endregion
        }
    }
}
