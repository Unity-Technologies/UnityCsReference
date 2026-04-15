// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Samples
{
    internal class HelpBoxSnippet : ElementSnippet<HelpBoxSnippet>
    {
        internal override void Apply(VisualElement container)
        {
            /// <sample>
            #region sample
            // Get a reference to the help box from UXML and update its text.
            var uxmlHelpBox = container.Q<HelpBox>("the-uxml-help-box");
            uxmlHelpBox.text += " (Updated in C#)";

            // Create a new help box and give it a style class.
            var csharpHelpBox = new HelpBox("This is a styled warning type help box.", HelpBoxMessageType.Warning);
            csharpHelpBox.AddToClassList("some-styled-help-box");
            container.Add(csharpHelpBox);

            // Create an Error Type help box.
            var errorHelpBox = new HelpBox("This is an error type help box.", HelpBoxMessageType.Error);
            container.Add(errorHelpBox);

            // Create a help box with no icon.
            var standardHelpBox = new HelpBox("This is a help box without an icon.", HelpBoxMessageType.None);
            container.Add(standardHelpBox);

            // Create an Info Type help box with some long text.
            var infoHelpBox = new HelpBox("This is a help box with a short paragraph. The content here should help wrap the help box when the window size is small enough. Feel free to resize the sample window to see this wrap and unwrap.", HelpBoxMessageType.Info);
            container.Add(infoHelpBox);

            // Create a typed help box with a call-to-action button.
            var helpBoxWithCTAButton = new HelpBox("This is a help box with a call to action button.", HelpBoxMessageType.Warning);
            helpBoxWithCTAButton.buttonText = "Action";
            helpBoxWithCTAButton.onButtonClicked += () => Debug.Log("Action Button Clicked");
            container.Add(helpBoxWithCTAButton);

            // Create a typed help box with a call-to-action button and link.
            var helpBoxWithCTAButtonAndLink = new HelpBox("This is a help box with a call to action button and a link.", HelpBoxMessageType.Warning);
            helpBoxWithCTAButtonAndLink.buttonText = "Action";
            helpBoxWithCTAButtonAndLink.onButtonClicked += () => Debug.Log("Action Button Clicked");
            helpBoxWithCTAButtonAndLink.linkHref = "https://www.unity.com";
            helpBoxWithCTAButtonAndLink.linkText = "Visit Unity!";
            container.Add(helpBoxWithCTAButtonAndLink);
            #endregion
            /// </sample>
        }
    }
}
