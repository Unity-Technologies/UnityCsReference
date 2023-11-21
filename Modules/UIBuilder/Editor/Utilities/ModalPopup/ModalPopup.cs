// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class ModalPopup : VisualElement
    {
        static readonly string s_UssClassName = "unity-modal-popup";
        static readonly string s_InvisibleClassName = "unity-modal-popup--invisible";

        Label m_Title;
        VisualElement m_Container;

        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            #pragma warning disable 649
            [SerializeField] string title;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags title_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new ModalPopup();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                if (ShouldWriteAttributeValue(title_UxmlAttributeFlags))
                {
                    var e = (ModalPopup)obj;
                    e.title = title;
                }
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
