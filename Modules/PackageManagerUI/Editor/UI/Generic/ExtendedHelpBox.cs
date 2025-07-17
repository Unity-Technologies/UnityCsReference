// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class ExtendedHelpBox : HelpBox
    {
        private static readonly string k_WithReadMoreUssClass = "with-read-more";

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
                    new (nameof(customIcon), "custom-icon"),
                    new (nameof(analyticsId), "analytics-id")
                }, true);
            }

#pragma warning disable 649
            [SerializeField, MultilineTextField] string readMoreUrl;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags readMoreUrl_UxmlAttributeFlags;
            [SerializeField] Icon customIcon;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags customIcon_UxmlAttributeFlags;
            [SerializeField] string analyticsId;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags analyticsId_UxmlAttributeFlags;
#pragma warning restore 649

            public override object CreateInstance() => new ExtendedHelpBox();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (ExtendedHelpBox)obj;
                if (ShouldWriteAttributeValue(readMoreUrl_UxmlAttributeFlags))
                    e.readMoreUrl = readMoreUrl;
                if (ShouldWriteAttributeValue(customIcon_UxmlAttributeFlags))
                    e.customIcon = customIcon;
                if (ShouldWriteAttributeValue(analyticsId_UxmlAttributeFlags))
                    e.analyticsId = analyticsId;
            }
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

        private string m_AnalyticsId;
        public string analyticsId
        {
            get => m_AnalyticsId;
            set
            {
                var newValue = value ?? string.Empty;
                if ((m_AnalyticsId ?? string.Empty) == newValue)
                    return;
                m_AnalyticsId = newValue;
            }
        }

        private Button m_ReadMoreButton;

        private void OnReadMoreUrlChanged()
        {
            var showReadMoreButton = !string.IsNullOrEmpty(m_ReadMoreUrl);
            if (showReadMoreButton)
            {
                if (m_ReadMoreButton == null)
                {
                    // The `unity-theme-env-variables` class is needed as we want to use theme variable `--unity-font-size-small` to make the text small
                    m_ReadMoreButton = new Button { text = L10n.Tr("Read more"), classList = { "link", "unity-theme-env-variables" } };
                    m_ReadMoreButton.clickable.clicked += OnReadMoreClicked;
                }

                m_ReadMoreButton.tooltip = m_ReadMoreUrl;
                if (m_ReadMoreButton.parent == null)
                    Add(m_ReadMoreButton);
            }
            else
                m_ReadMoreButton?.RemoveFromHierarchy();
            EnableInClassList(k_WithReadMoreUssClass, showReadMoreButton);
        }

        private void OnReadMoreClicked()
        {
            if (string.IsNullOrEmpty(readMoreUrl))
                return;

            ServicesContainer.instance.Resolve<IApplicationProxy>().OpenURL(readMoreUrl);
            PackageManagerReadMoreClickedAnalytics.SendEvent(analyticsId, readMoreUrl);
        }
    }
}
