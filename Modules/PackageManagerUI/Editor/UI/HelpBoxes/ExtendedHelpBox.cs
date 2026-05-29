// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace UnityEditor.PackageManager.UI.Internal
{
    [UxmlElement]
    internal partial class ExtendedHelpBox : HelpBox
    {
        private const string k_WithLinksUssClass = "with-links";
        internal static string embeddedLinkColor => EditorGUIUtility.isProSkin ? "#4f80f8" : "#0808fc";

        protected readonly IApplicationProxy m_Application;

        public ExtendedHelpBox() : this(ServicesContainer.instance.Resolve<IApplicationProxy>())
        {
        }

        public ExtendedHelpBox(IApplicationProxy application)
        {
            m_Application = application;
        }

        private readonly Dictionary<string, string> m_LinkIdToUrlMap = new ();
        private bool m_HasRegisteredLinkCallbacks = false;
        public new string text
        {
            get => base.text;
            set
            {
                if (value == base.text)
                    return;

                ReplaceLinkTagsAndRegisterEventsIfNeeded(value);
            }
        }

        private void ReplaceLinkTagsAndRegisterEventsIfNeeded(string value)
        {
            const string linkTagPattern = @"<link\s+id=""(?<id>[^""]+)""\s+url=""(?<url>[^""]+)"">(?<text>.*?)</link>";

            m_LinkIdToUrlMap.Clear();
            // We remove the url from the original text and store it in a dictionary with the id as key, so that we can use it later when the link is clicked.
            // Leaving it in the string will cause the link tag to be rendered as a normal text and the url will be visible to users.
            // We also wrap the link text with a color to make it look like a link since we cannot have uss for link tags.
            var finalText = Regex.Replace(value, linkTagPattern, match =>
            {
                var id = match.Groups["id"].Value;
                var url = match.Groups["url"].Value;
                var linkDisplayText = match.Groups["text"].Value;
                m_LinkIdToUrlMap[id] = url;
                return $"<link=\"{id}\"><color={embeddedLinkColor}>{linkDisplayText}</color></link>";
            });

            base.text = finalText;
            if (m_LinkIdToUrlMap.Count == 0 || m_HasRegisteredLinkCallbacks)
                return;

            // There could be multiple labels in the helpBox, we do this to make sure we get the correct one
            var mainLabel = this.Query<Label>().Where(i => i.text == text).First();
            if (mainLabel == null)
                return;

            mainLabel.RegisterCallback<PointerUpLinkTagEvent>(evt =>
            {
                if (!m_LinkIdToUrlMap.TryGetValue(evt.linkID, out var url))
                    return;
                m_Application.OpenURL(url);
                PackageManagerReadMoreClickedAnalytics.SendEvent(evt.linkID, url);
            });
            mainLabel.RegisterCallback<PointerOverLinkTagEvent>(_ => mainLabel.AddToClassList("link-hover"));
            mainLabel.RegisterCallback<PointerOutLinkTagEvent>(_ => mainLabel.RemoveFromClassList("link-hover"));
            m_HasRegisteredLinkCallbacks = true;
        }

        public new HelpBoxMessageType messageType
        {
            get => base.messageType;
            set
            {
                if (value == base.messageType)
                    return;

                base.messageType = value;

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

                if (value != Icon.None && base.messageType != HelpBoxMessageType.None)
                    base.messageType = HelpBoxMessageType.None;

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
                    m_CustomIconElement.AddToClassList(iconUssClassName);
                }

                m_CustomIconElement.AddToClassList(m_CustomIconClass);
                if (m_CustomIconElement.parent == null)
                    this.Q<VisualElement>(className: "unity-help-box__top-container")?.Insert(0, m_CustomIconElement);
            }
        }

        private string m_ReadMoreText = L10n.Tr("Learn More");

        [UxmlAttribute, MultilineTextField]
        public string readMoreText
        {
            get => m_ReadMoreText;
            set
            {
                var newValue = value ?? L10n.Tr("Learn More");
                if ((m_ReadMoreText ?? string.Empty) == newValue)
                    return;
                m_ReadMoreText = newValue;
                if (m_ReadMoreButton != null)
                    m_ReadMoreButton.text = m_ReadMoreText;
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
                OnReadMoreUrlChanged();
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
            }
        }

        private Button m_ReadMoreButton;

        private Button m_CustomLinkButton;
        private VisualElement m_CustomLinkContainer;

        private void OnReadMoreUrlChanged()
        {
            var showReadMoreButton = !string.IsNullOrEmpty(m_ReadMoreUrl);
            if (showReadMoreButton)
            {
                if (m_ReadMoreButton == null)
                {
                    m_ReadMoreButton = new Button { text = m_ReadMoreText }.WithClassList("link");
                    m_ReadMoreButton.clickable.clicked += OnReadMoreClicked;
                }

                m_ReadMoreButton.tooltip = m_ReadMoreUrl;
                if (m_ReadMoreButton.parent == null)
                    AddLinkToHierarchy(m_ReadMoreButton);

            }
            else
                RemoveLinkFromHierarchy(m_ReadMoreButton);
            EnableInClassList(k_WithLinksUssClass, showReadMoreButton);
        }

        public void SetCustomLinkButton(string linkButtonText, Action onClick, string linkButtonTooltip = "")
        {
            RemoveLinkFromHierarchy(m_CustomLinkButton);
            var showLinkCustomButton = !string.IsNullOrEmpty(linkButtonText) && onClick != null;
            EnableInClassList(k_WithLinksUssClass, showLinkCustomButton);
            if (!showLinkCustomButton)
                return;

            m_CustomLinkButton = new Button { text = linkButtonText }.WithClassList("link");
            m_CustomLinkButton.clickable.clicked += onClick;
            m_CustomLinkButton.tooltip = linkButtonTooltip;
            AddLinkToHierarchy(m_CustomLinkButton);
        }

        private void OnReadMoreClicked()
        {
            if (string.IsNullOrEmpty(readMoreUrl))
                return;

            m_Application.OpenURL(readMoreUrl);
            PackageManagerReadMoreClickedAnalytics.SendEvent(readMoreAnalyticsId, readMoreUrl);
        }

        private void AddLinkToHierarchy(Button linkButton)
        {
            if (m_CustomLinkContainer == null)
            {
                m_CustomLinkContainer = new VisualElement { name = "customLinkContainer" };
                Insert(childCount - 1, m_CustomLinkContainer);
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
