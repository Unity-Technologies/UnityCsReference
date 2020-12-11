using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class ModalPopup : VisualElement
    {
        static readonly string s_UssClassName = "unity-modal-popup";
        static readonly string s_InvisibleClassName = "unity-modal-popup--invisible";

        Label m_Title;
        VisualElement m_Container;

        public new class UxmlFactory : UxmlFactory<ModalPopup, UxmlTraits> {}

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Title = new UxmlStringAttributeDescription { name = "title" };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                ((ModalPopup)ve).title = m_Title.GetValueFromBag(bag, cc);
            }
        }

        public string title
        {
            get { return m_Title.text; }
            set { m_Title.text = value; }
        }

        public override VisualElement contentContainer => m_Container == null ? this : m_Container;

        public ModalPopup()
        {
            AddToClassList(s_UssClassName);
            AddToClassList(s_InvisibleClassName);

            // Load styles.
            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.UtilitiesPath + "/ModalPopup/ModalPopup.uss"));
            if (EditorGUIUtility.isProSkin)
                styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.UtilitiesPath + "/ModalPopup/ModalPopupDark.uss"));
            else
                styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.UtilitiesPath + "/ModalPopup/ModalPopupLight.uss"));

            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(BuilderConstants.UtilitiesPath + "/ModalPopup/ModalPopup.uxml");
            template.CloneTree(this);

            m_Title = this.Q<Label>("title");
            m_Container = this.Q("content-container");

            var window = this.Q("window");
            window.RegisterCallback<MouseUpEvent>(StopPropagation);

            this.RegisterCallback<MouseUpEvent>(HideOnClick);
        }

        public void Show()
        {
            RemoveFromClassList(s_InvisibleClassName);
        }

        public void Hide()
        {
            AddToClassList(s_InvisibleClassName);
        }

        void HideOnClick(MouseUpEvent evt)
        {
            Hide();
        }

        void StopPropagation(MouseUpEvent evt)
        {
            evt.StopPropagation();
        }
    }
}
