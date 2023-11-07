// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Samples
{
    internal class LabelSnippet : ElementSnippet<LabelSnippet>
    {
        internal override void Apply(VisualElement container)
        {
            #region sample
            // Get a reference to the label from UXML and update its text.
            var uxmlLabel = container.Q<Label>("the-uxml-label");
            uxmlLabel.text += " (Updated in C#)";

            // Create a new label and give it a style class.
            var csharpLabel = new Label("C# Label");
            csharpLabel.AddToClassList("some-styled-label");
            container.Add(csharpLabel);
            #endregion
        }
    }
}
