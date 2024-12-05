// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

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

        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new (nameof(title), "title")
                });
            }

            #pragma warning disable 649
            [SerializeField] string title;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags title_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new BuilderPane();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                if (ShouldWriteAttributeValue(title_UxmlAttributeFlags))
                {
                    var e = (BuilderPane)obj;
                    e.title = title;
                }
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
