// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class BuilderClassPill : VisualElement
    {
        static readonly string ussClassName = "unity-builder-class-pill";

        // Hierarchy elements
        Label m_Label;
        Button m_DeleteButton;

        public Label labelElement => m_Label;
        public string selectorAsString { get; set; }
        public VisualElement selectorElement { get; set; }
        public Clickable onDeleteClickable => m_DeleteButton.clickable;
        public bool isDragged;

        // Set's the Label element's text
        public string text
        {
            get => m_Label.text;
            set => m_Label.text = value;
        }

        public bool canBeRemoved
        {
            get => m_DeleteButton.style.display == DisplayStyle.Flex;
            set
            {
                m_DeleteButton.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public BuilderClassPill()
        {
            var visualAsset = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(BuilderConstants.UIBuilderPackagePath + "/BuilderClassPill.uxml");
            visualAsset.CloneTree(this);
            AddToClassList(ussClassName);

            m_Label = this.Q<Label>("class-name-label");
            m_DeleteButton = this.Q<Button>("delete-class-button");
        }

        public void SetDeleteButtonUserData(object data)
        {
            m_DeleteButton.userData = data;
        }
    }
}
