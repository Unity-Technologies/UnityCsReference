// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Text.RegularExpressions;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Center.Editor
{
    [UxmlElement]
    partial class HyperLink : Label
    {
        const string k_RichTextHyperLink="(<a\\s+href=\")(?<url>[^\"]+)\">(?<displayName>[^\\<]+)<(\\/a>)";

        [CreateProperty, UxmlAttribute] public string URL { get; set; }

        public HyperLink()
        {
            this.AddManipulator(new Clickable(OpenURL));

            this.RegisterValueChangedCallback(Callback);
        }


        public override VisualElement contentContainer => this;

        void Callback(ChangeEvent<string> evt)
        {
            if (Regex.Matches(evt.newValue,k_RichTextHyperLink)

                is { Count: > 0 } matches)
            {
                text = matches[0].Groups["displayName"].Value;
                URL = matches[0].Groups["url"].Value;
                evt.StopPropagation();
                NotifyPropertyChanged(nameof(text));
            }
        }

        private void OpenURL()
        {
            Application.OpenURL(URL);
        }
    }
}
