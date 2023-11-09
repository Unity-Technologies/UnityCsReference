// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Samples
{
    internal class ButtonSnippet : ElementSnippet<ButtonSnippet>
    {
        internal override void Apply(VisualElement container)
        {
            #region sample
            // Action to perform when button is pressed.
            // Toggles the text on all buttons in 'container'.
            Action action = () =>
            {
                container.Query<Button>().ForEach((button) =>
                {
                    button.text = button.text.EndsWith("Button") ? "Button (Clicked)" : "Button";
                });
            };

            // Get a reference to the Button from UXML and assign it its action.
            var uxmlButton = container.Q<Button>("the-uxml-button");
            uxmlButton.RegisterCallback<MouseUpEvent>((evt) => action());

            // Create a new Button with an action and give it a style class.
            var csharpButton = new Button(action) { text = "C# Button" };
            csharpButton.AddToClassList("some-styled-button");
            container.Add(csharpButton);
            #endregion
        }
    }
}
