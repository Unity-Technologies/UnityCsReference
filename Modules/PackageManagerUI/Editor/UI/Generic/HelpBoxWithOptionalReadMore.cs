// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class HelpBoxWithOptionalReadMore : HelpBox
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
                });
            }

#pragma warning disable 649
            [SerializeField, MultilineTextField] string readMoreUrl;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags readMoreUrl_UxmlAttributeFlags;
#pragma warning restore 649

            public override object CreateInstance() => new HelpBoxWithOptionalReadMore();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (HelpBoxWithOptionalReadMore)obj;
                if (ShouldWriteAttributeValue(readMoreUrl_UxmlAttributeFlags))
                    e.readMoreUrl = readMoreUrl;
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
        }
    }
}
