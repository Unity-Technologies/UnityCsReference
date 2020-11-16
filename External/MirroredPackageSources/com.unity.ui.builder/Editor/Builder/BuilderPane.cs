using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderPane : VisualElement
    {
        static readonly string s_UssClassName = "unity-builder-pane";

        Label m_Title;
        Label m_SubTitle;
        Label m_SubTitlePrefix;
        VisualElement m_Container;
        VisualElement m_Toolbar;
        ToolbarMenu m_EllipsisMenu;

        public new class UxmlFactory : UxmlFactory<BuilderPane, UxmlTraits> {}

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
                ((BuilderPane)ve).title = m_Title.GetValueFromBag(bag, cc);
            }
        }

        public string title
        {
            get => m_Title.text;
            set => m_Title.text = value;
        }

        public string subTitle
        {
            get => m_SubTitle.text;
            set
            {
                m_SubTitle.text = value;
                m_SubTitlePrefix.style.display = string.IsNullOrEmpty(m_SubTitle.text) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        public VisualElement toolbar => m_Toolbar;

        public BuilderPane()
        {
            AddToClassList(s_UssClassName);

            var visualAsset = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/BuilderPane.uxml");
            visualAsset.CloneTree(this);

            m_Title = this.Q<Label>("title");
            m_SubTitle = this.Q<Label>("sub-title");
            m_SubTitlePrefix = this.Q<Label>("sub-title-prefix");
            m_Container = this.Q("content-container");
            m_Toolbar = this.Q("toolbar");

            focusable = true;
            m_SubTitlePrefix.text = BuilderConstants.SubtitlePrefix;
            subTitle = string.Empty;

            m_EllipsisMenu = this.Q<ToolbarMenu>("ellipsis-menu");
            m_EllipsisMenu.style.display = DisplayStyle.None;
        }

        public void AppendActionToEllipsisMenu(string actionName,
            Action<DropdownMenuAction> action,
            Func<DropdownMenuAction, DropdownMenuAction.Status> actionStatusCallback,
            object userData = null)
        {
            m_EllipsisMenu.style.display = DisplayStyle.Flex;
            m_EllipsisMenu.menu.AppendAction(actionName, action, actionStatusCallback, userData);
        }

        public override VisualElement contentContainer => m_Container == null ? this : m_Container;
    }
}
