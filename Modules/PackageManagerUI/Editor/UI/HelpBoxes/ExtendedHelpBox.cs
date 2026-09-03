// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.Scripting;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace UnityEditor.PackageManager.UI.Internal
{
    [UxmlElement]
    internal partial class ExtendedHelpBox : VisualElement
    {
        private const string k_WithLinksUssClass = "with-links";
        private const string k_ReadMoreLinkId = "readMore";
        internal static string embeddedLinkColor => EditorGUIUtility.isProSkin ? "#4f80f8" : "#0808fc";

        protected readonly IApplicationProxy m_Application;
        private readonly HelpBox m_HelpBox;

        public ExtendedHelpBox() : this(ServicesContainer.instance.Resolve<IApplicationProxy>())
        {
        }

        public ExtendedHelpBox(IApplicationProxy application)
        {
            m_Application = application;
            m_HelpBox = new HelpBox();
            Add(m_HelpBox);
        }

        private readonly Dictionary<string, string> m_LinkIdToUrlMap = new ();
        private bool m_HasRegisteredLinkCallbacks = false;
        private string m_Text = string.Empty;

        [UxmlAttribute, MultilineTextField]
        public string text
        {
            get => m_Text;
            set
            {
                var newValue = value ?? string.Empty;
                if (newValue == m_Text)
                    return;

                m_Text = newValue;
                BuildTextWithLinkTags();
            }
        }

        private void BuildTextWithLinkTags()
        {
            var composed = m_Text;
            if (!string.IsNullOrEmpty(m_ReadMoreUrl))
            {
                var id = string.IsNullOrEmpty(m_ReadMoreAnalyticsId) ? k_ReadMoreLinkId : m_ReadMoreAnalyticsId;
                var separator = string.IsNullOrEmpty(m_Text) ? string.Empty : " ";
                composed = $"{m_Text}{separator}<link id=\"{id}\" url=\"{m_ReadMoreUrl}\">{m_ReadMoreText}</link>";
            }

            const string linkTagPattern = @"<link\s+id=""(?<id>[^""]+)""\s+url=""(?<url>[^""]+)"">(?<text>.*?)</link>";

            m_LinkIdToUrlMap.Clear();
            // We remove the url from the original text and store it in a dictionary with the id as key, so that we can use it later when the link is clicked.
            // Leaving it in the string will cause the link tag to be rendered as a normal text and the url will be visible to users.
            // We also wrap the link text with a color to make it look like a link since we cannot have uss for link tags.
            var finalText = Regex.Replace(composed, linkTagPattern, match =>
            {
                var id = match.Groups["id"].Value;
                var url = match.Groups["url"].Value;
                var linkDisplayText = match.Groups["text"].Value;
                // In case of two links having the same ID, we correct it for the code to work properly,
                // but log a message internally to raise the issue
                var suffix = 1;
                var uniqueId = id;
                while (m_LinkIdToUrlMap.ContainsKey(uniqueId))
                    uniqueId = $"{id}-{suffix++}";
                m_LinkIdToUrlMap[uniqueId] = url;
                if (Unsupported.IsDeveloperBuild() && uniqueId != id)
                    Debug.LogError("[Package Manager Window - Internal] Link ID" + id +
                                   " is used more than once in an ExtendedHelpBox text, use different IDs for different links.");

                return $"<link=\"{uniqueId}\"><color={embeddedLinkColor}>{linkDisplayText}</color></link>";
            });

            m_HelpBox.text = finalText;

            if (m_LinkIdToUrlMap.Count == 0 || m_HasRegisteredLinkCallbacks)
                return;
            // There could be multiple labels in the helpBox, we do this to make sure we get the correct one
            var mainLabel = m_HelpBox.Query<Label>().Where(i => i.text == finalText).First();
            if (mainLabel == null)
                return;

            mainLabel.RegisterCallback<PointerUpLinkTagEvent>(evt => OnPointerUpLinkTagEvent(evt.linkID));
            mainLabel.RegisterCallback<PointerOverLinkTagEvent>(_ => mainLabel.AddToClassList("link-hover"));
            mainLabel.RegisterCallback<PointerOutLinkTagEvent>(_ => mainLabel.RemoveFromClassList("link-hover"));
            m_HasRegisteredLinkCallbacks = true;
        }

        // This function is made internal to be used in tests.
        internal void OnPointerUpLinkTagEvent(string linkId)
        {
            if (!m_LinkIdToUrlMap.TryGetValue(linkId, out var url))
                return;
            m_Application.OpenURL(url);
            PackageManagerReadMoreClickedAnalytics.SendEvent(linkId, url);
        }

        [UxmlAttribute]
        public HelpBoxMessageType messageType
        {
            get => m_HelpBox.messageType;
            set
            {
                if (value == m_HelpBox.messageType)
                    return;

                m_HelpBox.messageType = value;

                if (value != HelpBoxMessageType.None && m_CustomIcon != Icon.None)
                {
                    m_CustomIcon = Icon.None;
                    UpdateCustomIcon(Icon.None);
                }
            }
        }

        private VisualElement m_CustomIconElement;

        private Icon m_CustomIcon = Icon.None;

        [UxmlAttribute]
        public Icon customIcon
        {
            get => m_CustomIcon;
            set
            {
                if (value == m_CustomIcon)
                    return;

                if (value != Icon.None && m_HelpBox.messageType != HelpBoxMessageType.None)
                    m_HelpBox.messageType = HelpBoxMessageType.None;

                m_CustomIcon = value;
                UpdateCustomIcon(value);
            }
        }

        private string m_CustomIconClass;

        private void UpdateCustomIcon(Icon customIcon)
        {
            if (!string.IsNullOrEmpty(m_CustomIconClass))
                m_CustomIconElement?.RemoveFromClassList(m_CustomIconClass);

            m_CustomIconClass = customIcon == Icon.None ? null : customIcon.ClassName();
            if (string.IsNullOrEmpty(m_CustomIconClass))
                m_CustomIconElement?.RemoveFromHierarchy();
            else
            {
                if (m_CustomIconElement == null)
                {
                    m_CustomIconElement = new VisualElement();
                    m_CustomIconElement.AddToClassList(HelpBox.iconUssClassName);
                }

                m_CustomIconElement.AddToClassList(m_CustomIconClass);
                if (m_CustomIconElement.parent == null)
                    m_HelpBox.Q<VisualElement>(className: "unity-help-box__top-container")?.Insert(0, m_CustomIconElement);
            }
        }

        private string m_ReadMoreText = L10n.Tr("Learn More", null);

        [UxmlAttribute, MultilineTextField]
        public string readMoreText
        {
            get => m_ReadMoreText;
            set
            {
                var newValue = value ?? L10n.Tr("Learn More", null);
                if ((m_ReadMoreText ?? string.Empty) == newValue)
                    return;
                m_ReadMoreText = newValue;
                BuildTextWithLinkTags();
            }
        }

        private string m_ReadMoreUrl;

        [UxmlAttribute, MultilineTextField]
        public string readMoreUrl
        {
            get => m_ReadMoreUrl;
            set
            {
                var newValue = value ?? string.Empty;
                if ((m_ReadMoreUrl ?? string.Empty) == newValue)
                    return;
                m_ReadMoreUrl = newValue;
                BuildTextWithLinkTags();
            }
        }

        private string m_ReadMoreAnalyticsId;

        [UxmlAttribute]
        public string readMoreAnalyticsId
        {
            get => m_ReadMoreAnalyticsId;
            set
            {
                var newValue = value ?? string.Empty;
                if ((m_ReadMoreAnalyticsId ?? string.Empty) == newValue)
                    return;
                m_ReadMoreAnalyticsId = newValue;
                BuildTextWithLinkTags();
            }
        }

        private Button m_CustomLinkButton;
        private VisualElement m_CustomLinkContainer;

        public void SetCustomLinkButton(string linkButtonText, Action onClick, string linkButtonTooltip = "")
        {
            RemoveLinkFromHierarchy(m_CustomLinkButton);
            var showLinkCustomButton = !string.IsNullOrEmpty(linkButtonText) && onClick != null;
            m_HelpBox.EnableInClassList(k_WithLinksUssClass, showLinkCustomButton);
            if (!showLinkCustomButton)
                return;

            m_CustomLinkButton = new Button { text = linkButtonText }.WithClassList("link");
            m_CustomLinkButton.clickable.clicked += onClick;
            m_CustomLinkButton.tooltip = linkButtonTooltip;
            AddLinkToHierarchy(m_CustomLinkButton);
        }

        private void AddLinkToHierarchy(Button linkButton)
        {
            if (m_CustomLinkContainer == null)
            {
                m_CustomLinkContainer = new VisualElement { name = "customLinkContainer" };
                m_HelpBox.Insert(m_HelpBox.childCount - 1, m_CustomLinkContainer);
            }

            m_CustomLinkContainer.Add(linkButton);
        }

        private void RemoveLinkFromHierarchy(Button linkButton)
        {
            linkButton?.RemoveFromHierarchy();
            if (m_CustomLinkContainer?.childCount != 0)
                return;
            m_CustomLinkContainer.RemoveFromHierarchy();
            m_CustomLinkContainer = null;
        }
    }
}
