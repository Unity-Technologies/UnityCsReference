// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Properties;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class BuilderPropertyPathInfoViewItem : VisualElement
    {
        private const string k_UssClassName = "unity-builder-property-path-info-item";
        private Label m_NameLabel;
        private Label m_TypeLabel;

        public string propertyName
        {
            get => m_NameLabel.text;
            set => m_NameLabel.text = value;
        }

        public Type propertyType
        {
            set
            {
                m_TypeLabel.text = TypeUtility.GetTypeDisplayName(value);
                m_TypeLabel.tooltip = value.GetDisplayFullName();
            }
        }

        public BuilderPropertyPathInfoViewItem()
        {
            AddToClassList(k_UssClassName);

            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/Inspector/PropertyPathInfoViewItem.uxml");
            template.CloneTree(this);

            m_NameLabel = this.Q<Label>("nameLabel");
            m_TypeLabel = this.Q<Label>("typeLabel");
        }
    }
}
