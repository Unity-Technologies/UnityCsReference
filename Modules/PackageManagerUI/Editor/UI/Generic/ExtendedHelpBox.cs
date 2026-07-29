// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class ExtendedHelpBox : HelpBox
    {
        private static readonly string k_WithLinksUssClass = "with-links";
        internal static string embeddedLinkColor => EditorGUIUtility.isProSkin ? "#4f80f8" : "#0808fc";

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : HelpBox.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                HelpBox.UxmlSerializedData.Register();
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new (nameof(readMoreUrl), "read-more-url"),
                    new (nameof(readMoreText), "read-more-text"),
                    new (nameof(customIcon), "custom-icon"),
                    new (nameof(readMoreAnalyticsId), "read-more-analytics-id")
                }, true);
            }

#pragma warning disable 649
            [SerializeField, MultilineTextField] string readMoreUrl;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags readMoreUrl_UxmlAttributeFlags;
            [SerializeField, MultilineTextField] string readMoreText;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags readMoreText_UxmlAttributeFlags;
            [SerializeField] Icon customIcon;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags customIcon_UxmlAttributeFlags;
            [SerializeField] string readMoreAnalyticsId;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags readMoreAnalyticsId_UxmlAttributeFlags;
#pragma warning restore 649

            public override object CreateInstance() => new ExtendedHelpBox();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (ExtendedHelpBox)obj;
                if (ShouldWriteAttributeValue(readMoreUrl_UxmlAttributeFlags))
                    e.readMoreUrl = readMoreUrl;
                if (ShouldWriteAttributeValue(readMoreText_UxmlAttributeFlags))
                    e.readMoreText = readMoreText;
                if (ShouldWriteAttributeValue(customIcon_UxmlAttributeFlags))
                    e.customIcon = customIcon;
                if (ShouldWriteAttributeValue(readMoreAnalyticsId_UxmlAttributeFlags))
                    e.readMoreAnalyticsId = readMoreAnalyticsId;
            }
        }

        private readonly Dictionary<string, string> m_LinkIdToUrlMap = new();
        private bool m_HasRegisteredLinkCallbacks;
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

            var mainLabel = this.Query<Label>().Where(i => i.text == text).First();
            if (mainLabel == null)
                return;

            var application = ServicesContainer.instance.Resolve<IApplicationProxy>();
            mainLabel.RegisterCallback<PointerUpLinkTagEvent>(evt =>
            {
                if (!m_LinkIdToUrlMap.TryGetValue(evt.linkID, out var url))
                    return;
                application.OpenURL(url);
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
                    Insert(0, m_CustomIconElement);
            }
        }

        private string m_ReadMoreText = L10n.Tr("Learn More");
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
                    // The `unity-theme-env-variables` class is needed as we want to use theme variable `--unity-font-size-small` to make the text small
                    m_ReadMoreButton = new Button { text = m_ReadMoreText, classList = { "link", "unity-theme-env-variables" } };
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

            m_CustomLinkButton = new Button { text = linkButtonText, classList = { "link", "unity-theme-env-variables" } };
            m_CustomLinkButton.clickable.clicked += onClick;
            m_CustomLinkButton.tooltip = linkButtonTooltip;
            AddLinkToHierarchy(m_CustomLinkButton);
        }

        private void OnReadMoreClicked()
        {
            if (string.IsNullOrEmpty(readMoreUrl))
                return;

            ServicesContainer.instance.Resolve<IApplicationProxy>().OpenURL(readMoreUrl);
            PackageManagerReadMoreClickedAnalytics.SendEvent(readMoreAnalyticsId, readMoreUrl);
        }

        private void AddLinkToHierarchy(Button linkButton)
        {
            if (m_CustomLinkContainer == null)
            {
                m_CustomLinkContainer = new VisualElement { name = "customLinkContainer" };
                Add(m_CustomLinkContainer);
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
