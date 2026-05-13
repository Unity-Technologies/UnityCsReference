// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine;
using System;

namespace Unity.UI.Builder
{
    [UxmlElement]
    internal partial class BuilderTooltipPreview : VisualElement
    {
        public const string UssPath = BuilderConstants.UIBuilderPackagePath + "/BuilderTooltipPreview.uss";
        public const string UssPath_Dark = BuilderConstants.UIBuilderPackagePath + "/BuilderTooltipPreviewDark.uss";
        public const string UssPath_Light = BuilderConstants.UIBuilderPackagePath + "/BuilderTooltipPreviewLight.uss";
        public static string UssPath_Themed => EditorGUIUtility.isProSkin ? UssPath_Dark : UssPath_Light;

        static readonly string s_UssClassName = "unity-builder-tooltip-preview";
        static readonly string s_EnablerClassName = "unity-builder-tooltip-preview__enabler";
        static readonly string s_ContainerClassName = "unity-builder-tooltip-preview__container";
        public static readonly string s_EnabledElementName = "enabler";

        VisualElement m_Enabler;
        VisualElement m_Container;

        public bool isShowing => m_Enabler.resolvedStyle.display == DisplayStyle.Flex;

        public override VisualElement contentContainer => m_Container == null ? this : m_Container;

        public event Action onShow;
        public event Action onHide;

        public BuilderTooltipPreview()
        {
            AddToClassList(s_UssClassName);
            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(UssPath));
            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(UssPath_Themed));

            m_Enabler = new VisualElement();
            m_Enabler.name = s_EnabledElementName;
            m_Enabler.AddToClassList(s_EnablerClassName);
            hierarchy.Add(m_Enabler);

            m_Container = new VisualElement();
            m_Container.name = "content-container";
            m_Container.AddToClassList(s_ContainerClassName);
            m_Enabler.Add(m_Container);
        }

        public void Show()
        {
            onShow?.Invoke();
            m_Enabler.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            m_Enabler.style.display = DisplayStyle.None;
            onHide?.Invoke();
        }

        public void Enable()
        {
            this.style.display = DisplayStyle.Flex;
        }

        public void Disable()
        {
            this.style.display = DisplayStyle.None;
        }

        public virtual Vector2 GetAdjustedPosition()
        {
            const float PopupAndWindowEdgesMargin = 10f;

            return new Vector2(Mathf.Min(style.left.value.value, parent.layout.width - resolvedStyle.width - PopupAndWindowEdgesMargin),
                Mathf.Min(style.top.value.value, parent.layout.height - resolvedStyle.height - PopupAndWindowEdgesMargin));
        }
    }
}
