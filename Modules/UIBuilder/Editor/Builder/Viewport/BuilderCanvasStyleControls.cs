// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderCanvasStyleControls : VisualElement
    {
        // Strings
        const string k_UssClassName = "unity-builder-canvas-style-controls";
        private const string k_ButtonUssClassName = k_UssClassName + "__button";
        private const string k_ButtonForBoundStyleUssClassName = k_ButtonUssClassName + "--value-bound";
        private const string k_OriginalTooltipProperty = "originalTooltip";

        const string k_FlexDirectionName = "flex-direction";
        const string k_AlignItemsName = "align-items";
        const string k_JustifyContentName = "justify-content";
        const string k_TextAlignName = "-unity-text-align";
        const string k_TextOverflowName = "text-overflow";
        const string k_TextWrapName = "white-space";
        const string k_AlignSelfName = "align-self";

        public const string k_FlexDirectionButtonName = "flex-direction-button";
        public const string k_AlignItemsButtonName = "align-items-button";
        public const string k_JustifyContentButtonName = "justify-content-button";
        public const string k_HorizontalTextAlignButtonName = "horizontal-text-align-button";
        public const string k_VerticalTextAlignButtonName = "vertical-text-align-button";
        public const string k_TextWrapButtonName = "text-wrap-button";
        public const string k_TextOverflowButtonName = "text-overflow-button";
        public const string k_AlignSelfButtonName = "align-self-button";

        // Buttons
        List<Button> m_AllButtons = new List<Button>();
        List<Button> m_FlexAlignButtons = new List<Button>();
        Button m_FlexDirectionButton;
        Button m_AlignItemsButton;
        Button m_JustifyContentButton;
        Button m_HorizontalTextAlignButton;
        Button m_VerticalTextAlignButton;
        Button m_TextWrapButton;
        Button m_TextOverflowButton;
        Button m_AlignSelfButton;

        BuilderSelection m_Selection;
        VisualTreeAsset m_VisualTreeAsset;
        VisualElement m_Target;

        private BuilderBindingsCacheSubscriber m_BindingsCacheSubscriber;

        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new BuilderCanvasStyleControls();
        }

        public BuilderCanvasStyleControls()
        {
            this.AddToClassList(k_UssClassName);

            m_BindingsCacheSubscriber = new BuilderBindingsCacheSubscriber((_, _) => UpdateButtonsFromBindings());

            // Load Template
            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/Viewport/BuilderCanvasStyleControls.uxml");
            template.CloneTree(this);

            // Load Styles
            var uss = BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(
                BuilderConstants.UIBuilderPackagePath + "/Viewport/BuilderCanvasStyleControls.uss");
            this.styleSheets.Add(uss);

            // Fetch buttons.
            m_FlexDirectionButton = QueryAndBindButton(k_FlexDirectionButtonName, "flex-direction", FlexDirectionOnToggle);
            m_AlignItemsButton = QueryAndBindButton(k_AlignItemsButtonName, "align-items", AlignItemsOnToggle);
            m_JustifyContentButton = QueryAndBindButton(k_JustifyContentButtonName, "justify-content", JustifyContentOnToggle);
            m_HorizontalTextAlignButton = QueryAndBindButton(k_HorizontalTextAlignButtonName, "-unity-text-align", HorizontalTextAlignOnToggle);
            m_VerticalTextAlignButton = QueryAndBindButton(k_VerticalTextAlignButtonName, "-unity-text-align", VerticalTextAlignOnToggle);
            m_TextWrapButton = QueryAndBindButton(k_TextWrapButtonName, "white-space", TextWrapOnToggle);
            m_TextOverflowButton = QueryAndBindButton(k_TextOverflowButtonName, "text-overflow", TextOverflowOnToggle);
            m_AlignSelfButton = QueryAndBindButton(k_AlignSelfButtonName, "align-self", AlignSelfOnToggle);

            // Group special buttons.
            m_FlexAlignButtons.Add(m_FlexDirectionButton);
            m_FlexAlignButtons.Add(m_AlignItemsButton);
            m_FlexAlignButtons.Add(m_JustifyContentButton);
            m_FlexAlignButtons.Add(m_AlignSelfButton);
        }

        void RefreshAfterFirstInit(GeometryChangedEvent evt)
        {
            if (m_Target == null)
                return;

            m_Target.UnregisterCallback<GeometryChangedEvent>(RefreshAfterFirstInit);
            ActivateInner();
        }

        public void Activate(BuilderSelection selection, VisualTreeAsset visualTreeAsset, VisualElement target, BuilderBindingsCache bindingsCache)
        {
            m_Selection = selection;
            m_VisualTreeAsset = visualTreeAsset;
            m_Target = target;
            m_BindingsCacheSubscriber.cache = bindingsCache;

            UpdateButtonsFromBindings();

            // On the first RefreshUI, if an element is already selected, we need to make sure it
            // has a valid style. If not, we need to delay our UI building until it is properly initialized.
            if (m_Target != null && float.IsNaN(m_Target.layout.width))
            {
                m_Target.RegisterCallback<GeometryChangedEvent>(RefreshAfterFirstInit);
                return;
            }

            ActivateInner();
        }

        void UpdateButtonsFromBindings()
        {
            void UpdateButtonForBindings(Button button)
            {
                var styleName = button.GetProperty(BuilderConstants.InspectorStylePropertyNameVEPropertyName) as string;
                var styleProperty = BuilderNameUtilities.ConvertStyleUssNameToCSharpName(styleName);
                var bindingProperty = BuilderConstants.StylePropertyPathPrefix + styleProperty;
                var bound = m_BindingsCacheSubscriber.cache.HasResolvedBinding(m_Target, bindingProperty);

                button.EnableInClassList(k_ButtonForBoundStyleUssClassName, bound);
                button.SetEnabled(!bound);

                // tooltip
                var originalTooltip = button.tooltip;

                if (!button.HasProperty(k_OriginalTooltipProperty))
                     button.SetProperty(k_OriginalTooltipProperty, originalTooltip);

                originalTooltip = button.GetProperty(k_OriginalTooltipProperty) as string;
                button.tooltip = bound ? $"{originalTooltip}: Cannot edit bound style property '{styleName}'" : originalTooltip;
            }

            foreach (var button in m_AllButtons)
            {
                UpdateButtonForBindings(button);
            }
        }

        void ActivateInner()
        {
            // Hide all buttons.
            foreach (var button in m_AllButtons)
                button.style.display = DisplayStyle.None;

            if (!m_Target.IsPartOfCurrentDocument())
                return;

            // if the target is of type VisualElement or has children.
            if (m_Target.GetType() == typeof(VisualElement) || m_Target.childCount > 0)
            {
                m_FlexDirectionButton.style.display = DisplayStyle.Flex;
                FlexDirectionUpdateToggleIcon();

                m_AlignItemsButton.style.display = DisplayStyle.Flex;
                AlignItemsUpdateToggleIcon();

                m_JustifyContentButton.style.display = DisplayStyle.Flex;
                JustifyContentUpdateToggleIcon();

                m_AlignSelfButton.style.display = DisplayStyle.Flex;
                AlignSelfUpdateToggleIcon();
            }

            // Text elements.
            if (m_Target is TextElement)
            {
                m_HorizontalTextAlignButton.style.display = DisplayStyle.Flex;
                HorizontalTextAlignUpdateToggleIcon();

                m_VerticalTextAlignButton.style.display = DisplayStyle.Flex;
                VerticalTextAlignUpdateToggleIcon();

                m_TextWrapButton.style.display = DisplayStyle.Flex;
                TextWrapUpdateToggleIcon();

                m_TextOverflowButton.style.display = DisplayStyle.Flex;
                TextOverflowUpdateToggleIcon();
            }
        }

        public void Deactivate()
        {
            m_Selection = null;
            m_VisualTreeAsset = null;
            m_Target = null;
            m_BindingsCacheSubscriber.cache = null;
        }

        public void UpdateButtonIcons(List<string> styles)
        {
            if (m_Target == null)
                return;

            foreach (var style in styles)
            {
                switch (style)
                {
                    case k_FlexDirectionName: FlexDirectionUpdateToggleIcon(); break;
                    case k_AlignItemsName: AlignItemsUpdateToggleIcon(); break;
                    case k_JustifyContentName: JustifyContentUpdateToggleIcon(); break;
                    case k_AlignSelfName: AlignSelfUpdateToggleIcon(); break;
                    case k_TextAlignName:
                        HorizontalTextAlignUpdateToggleIcon();
                        VerticalTextAlignUpdateToggleIcon();
                        break;
                    case k_TextOverflowName: TextOverflowUpdateToggleIcon(); break;
                    case k_TextWrapName: TextWrapUpdateToggleIcon(); break;
                }
            }
        }

        //
        // Utilities
        //

        Button QueryAndBindButton(string name, string styleName, Action action)
        {
            var button = this.Q<Button>(name);
            button.clickable.clicked += action;
            button.SetProperty(BuilderConstants.InspectorStylePropertyNameVEPropertyName, styleName);
            m_AllButtons.Add(button);
            m_BindingsCacheSubscriber.filteredProperties.Add(BuilderConstants.StylePropertyPathPrefix +  BuilderNameUtilities.ConvertStyleUssNameToCSharpName(styleName));
            return button;
        }

        //
        // Flex Direction
        //

        void FlexDirectionUpdateToggleIcon()
        {
            FlexDirectionUpdateToggleIcon(m_Target.resolvedStyle.flexDirection);
        }

        void FlexDirectionUpdateToggleIcon(FlexDirection resolvedStyle)
        {
            foreach (var button in m_FlexAlignButtons)
            {
                button.RemoveFromClassList("flex-column");
                button.RemoveFromClassList("flex-column-reverse");
                button.RemoveFromClassList("flex-row");
                button.RemoveFromClassList("flex-row-reverse");

                switch (resolvedStyle)
                {
                    case FlexDirection.Column: button.AddToClassList("flex-column"); break;
                    case FlexDirection.ColumnReverse: button.AddToClassList("flex-column-reverse"); break;
                    case FlexDirection.Row: button.AddToClassList("flex-row"); break;
                    case FlexDirection.RowReverse: button.AddToClassList("flex-row-reverse"); break;
                }
            }
        }

        void FlexDirectionOnToggle()
        {
            var result = FlexDirection.Column;
            switch (m_Target.resolvedStyle.flexDirection)
            {
                case FlexDirection.Column: result = FlexDirection.ColumnReverse; break;
                case FlexDirection.ColumnReverse: result = FlexDirection.Row; break;
                case FlexDirection.Row: result = FlexDirection.RowReverse; break;
                case FlexDirection.RowReverse: result = FlexDirection.Column; break;
            }
            FlexDirectionUpdateToggleIcon(result);
            BuilderStyleUtilities.SetInlineStyleValue(m_VisualTreeAsset, m_Target, k_FlexDirectionName, result);
            m_Selection.NotifyOfHierarchyChange(null, m_Target, BuilderHierarchyChangeType.InlineStyle | BuilderHierarchyChangeType.FullRefresh);
            m_Selection.NotifyOfStylingChange(null, new List<string>() { k_FlexDirectionName });
        }

        //
        // Align Items
        //

        void AlignItemsUpdateToggleIcon()
        {
            AlignItemsUpdateToggleIcon(m_Target.resolvedStyle.alignItems);
        }

        void AlignItemsUpdateToggleIcon(Align resolveStyle)
        {
            var button = m_AlignItemsButton;

            button.RemoveFromClassList("auto");
            button.RemoveFromClassList("flex-start");
            button.RemoveFromClassList("center");
            button.RemoveFromClassList("flex-end");
            button.RemoveFromClassList("stretch");

            switch (resolveStyle)
            {
                case Align.Auto: button.AddToClassList("auto"); break;
                case Align.FlexStart: button.AddToClassList("flex-start"); break;
                case Align.Center: button.AddToClassList("center"); break;
                case Align.FlexEnd: button.AddToClassList("flex-end"); break;
                case Align.Stretch: button.AddToClassList("stretch"); break;
            }
        }

        void AlignItemsOnToggle()
        {
            var result = Align.Auto;
            switch (m_Target.resolvedStyle.alignItems)
            {
                case Align.Auto: result = Align.FlexStart; break;
                case Align.FlexStart: result = Align.Center; break;
                case Align.Center: result = Align.FlexEnd; break;
                case Align.FlexEnd: result = Align.Stretch; break;
                case Align.Stretch: result = Align.Auto; break;
            }
            AlignItemsUpdateToggleIcon(result);
            BuilderStyleUtilities.SetInlineStyleValue(m_VisualTreeAsset, m_Target, k_AlignItemsName, result);
            m_Selection.NotifyOfHierarchyChange(null, m_Target, BuilderHierarchyChangeType.InlineStyle | BuilderHierarchyChangeType.FullRefresh);
            m_Selection.NotifyOfStylingChange(null, new List<string>() { k_AlignItemsName });
        }

        //
        // Justify Content
        //

        void JustifyContentUpdateToggleIcon()
        {
            JustifyContentUpdateToggleIcon(m_Target.resolvedStyle.justifyContent);
        }

        void JustifyContentUpdateToggleIcon(Justify resolvedStyle)
        {
            var button = m_JustifyContentButton;

            button.RemoveFromClassList("flex-start");
            button.RemoveFromClassList("center");
            button.RemoveFromClassList("flex-end");
            button.RemoveFromClassList("space-between");
            button.RemoveFromClassList("space-around");
            button.RemoveFromClassList("space-evenly");

            switch (resolvedStyle)
            {
                case Justify.FlexStart: button.AddToClassList("flex-start"); break;
                case Justify.Center: button.AddToClassList("center"); break;
                case Justify.FlexEnd: button.AddToClassList("flex-end"); break;
                case Justify.SpaceBetween: button.AddToClassList("space-between"); break;
                case Justify.SpaceAround: button.AddToClassList("space-around"); break;
                case Justify.SpaceEvenly: button.AddToClassList("space-evenly"); break;
            }
        }

        void JustifyContentOnToggle()
        {
            var result = Justify.FlexStart;
            switch (m_Target.resolvedStyle.justifyContent)
            {
                case Justify.FlexStart: result = Justify.Center; break;
                case Justify.Center: result = Justify.FlexEnd; break;
                case Justify.FlexEnd: result = Justify.SpaceBetween; break;
                case Justify.SpaceBetween: result = Justify.SpaceAround; break;
                case Justify.SpaceAround: result = Justify.SpaceEvenly; break;
                case Justify.SpaceEvenly: result = Justify.FlexStart; break;
            }
            JustifyContentUpdateToggleIcon(result);
            BuilderStyleUtilities.SetInlineStyleValue(m_VisualTreeAsset, m_Target, k_JustifyContentName, result);
            m_Selection.NotifyOfHierarchyChange(null, m_Target, BuilderHierarchyChangeType.InlineStyle | BuilderHierarchyChangeType.FullRefresh);
            m_Selection.NotifyOfStylingChange(null, new List<string>() { k_JustifyContentName });
        }

        //
        // Horizontal Text Align
        //

        void HorizontalTextAlignUpdateToggleIcon()
        {
            HorizontalTextAlignUpdateToggleIcon(m_Target.resolvedStyle.unityTextAlign);
        }

        void HorizontalTextAlignUpdateToggleIcon(TextAnchor resolveStyle)
        {
            var button = m_HorizontalTextAlignButton;

            button.RemoveFromClassList("left");
            button.RemoveFromClassList("center");
            button.RemoveFromClassList("right");

            switch (resolveStyle)
            {
                case TextAnchor.UpperLeft: button.AddToClassList("left"); break;
                case TextAnchor.UpperCenter: button.AddToClassList("center"); break;
                case TextAnchor.UpperRight: button.AddToClassList("right"); break;
                case TextAnchor.MiddleLeft: button.AddToClassList("left"); break;
                case TextAnchor.MiddleCenter: button.AddToClassList("center"); break;
                case TextAnchor.MiddleRight: button.AddToClassList("right"); break;
                case TextAnchor.LowerLeft: button.AddToClassList("left"); break;
                case TextAnchor.LowerCenter: button.AddToClassList("center"); break;
                case TextAnchor.LowerRight: button.AddToClassList("right"); break;
            }
        }

        void HorizontalTextAlignOnToggle()
        {
            var result = TextAnchor.UpperLeft;
            switch (m_Target.resolvedStyle.unityTextAlign)
            {
                case TextAnchor.UpperLeft: result = TextAnchor.UpperCenter; break;
                case TextAnchor.UpperCenter: result = TextAnchor.UpperRight; break;
                case TextAnchor.UpperRight: result = TextAnchor.UpperLeft; break;
                case TextAnchor.MiddleLeft: result = TextAnchor.MiddleCenter; break;
                case TextAnchor.MiddleCenter: result = TextAnchor.MiddleRight; break;
                case TextAnchor.MiddleRight: result = TextAnchor.MiddleLeft; break;
                case TextAnchor.LowerLeft: result = TextAnchor.LowerCenter; break;
                case TextAnchor.LowerCenter: result = TextAnchor.LowerRight; break;
                case TextAnchor.LowerRight: result = TextAnchor.LowerLeft; break;
            }
            HorizontalTextAlignUpdateToggleIcon(result);
            BuilderStyleUtilities.SetInlineStyleValue(m_VisualTreeAsset, m_Target, k_TextAlignName, result);
            m_Selection.NotifyOfHierarchyChange(null, m_Target, BuilderHierarchyChangeType.InlineStyle | BuilderHierarchyChangeType.FullRefresh);
            m_Selection.NotifyOfStylingChange(null, new List<string>() { k_TextAlignName });
        }

        //
        // Vertical Text Align
        //

        void VerticalTextAlignUpdateToggleIcon()
        {
            VerticalTextAlignUpdateToggleIcon(m_Target.resolvedStyle.unityTextAlign);
        }

        void VerticalTextAlignUpdateToggleIcon(TextAnchor resolveStyle)
        {
            var button = m_VerticalTextAlignButton;

            button.RemoveFromClassList("upper");
            button.RemoveFromClassList("middle");
            button.RemoveFromClassList("lower");

            switch (resolveStyle)
            {
                case TextAnchor.UpperLeft: button.AddToClassList("upper"); break;
                case TextAnchor.UpperCenter: button.AddToClassList("upper"); break;
                case TextAnchor.UpperRight: button.AddToClassList("upper"); break;
                case TextAnchor.MiddleLeft: button.AddToClassList("middle"); break;
                case TextAnchor.MiddleCenter: button.AddToClassList("middle"); break;
                case TextAnchor.MiddleRight: button.AddToClassList("middle"); break;
                case TextAnchor.LowerLeft: button.AddToClassList("lower"); break;
                case TextAnchor.LowerCenter: button.AddToClassList("lower"); break;
                case TextAnchor.LowerRight: button.AddToClassList("lower"); break;
            }
        }

        void VerticalTextAlignOnToggle()
        {
            var result = TextAnchor.UpperLeft;
            switch (m_Target.resolvedStyle.unityTextAlign)
            {
                case TextAnchor.UpperLeft: result = TextAnchor.MiddleLeft; break;
                case TextAnchor.UpperCenter: result = TextAnchor.MiddleCenter; break;
                case TextAnchor.UpperRight: result = TextAnchor.MiddleRight; break;
                case TextAnchor.MiddleLeft: result = TextAnchor.LowerLeft; break;
                case TextAnchor.MiddleCenter: result = TextAnchor.LowerCenter; break;
                case TextAnchor.MiddleRight: result = TextAnchor.LowerRight; break;
                case TextAnchor.LowerLeft: result = TextAnchor.UpperLeft; break;
                case TextAnchor.LowerCenter: result = TextAnchor.UpperCenter; break;
                case TextAnchor.LowerRight: result = TextAnchor.UpperRight; break;
            }
            VerticalTextAlignUpdateToggleIcon(result);
            BuilderStyleUtilities.SetInlineStyleValue(m_VisualTreeAsset, m_Target, k_TextAlignName, result);
            m_Selection.NotifyOfHierarchyChange(null, m_Target, BuilderHierarchyChangeType.InlineStyle | BuilderHierarchyChangeType.FullRefresh);
            m_Selection.NotifyOfStylingChange(null, new List<string>() { k_TextAlignName });
        }

        void TextOverflowUpdateToggleIcon()
        {
            TextOverflowUpdateToggleIcon(m_Target.resolvedStyle.textOverflow);
        }

        void TextOverflowUpdateToggleIcon(TextOverflow resolveStyle)
        {
            m_TextOverflowButton.EnableInClassList("clip", resolveStyle == TextOverflow.Clip);
            m_TextOverflowButton.EnableInClassList("ellipsis", resolveStyle == TextOverflow.Ellipsis);
        }

        //
        // Text Wrap
        //

        void TextWrapUpdateToggleIcon()
        {
            TextWrapUpdateToggleIcon(m_Target.resolvedStyle.whiteSpace);
        }

        void TextWrapUpdateToggleIcon(WhiteSpace resolveStyle)
        {
            var button = m_TextWrapButton;

            button.RemoveFromClassList("normal");
            button.RemoveFromClassList("nowrap");

            switch (resolveStyle)
            {
                case WhiteSpace.Normal: button.AddToClassList("normal"); break;
                case WhiteSpace.NoWrap: button.AddToClassList("nowrap"); break;
            }
        }

        void TextOverflowOnToggle()
        {
            var result = TextOverflow.Clip;
            switch (m_Target.resolvedStyle.textOverflow)
            {
                case TextOverflow.Clip: result = TextOverflow.Ellipsis; break;
                case TextOverflow.Ellipsis: result = TextOverflow.Clip; break;
            }
            TextOverflowUpdateToggleIcon(result);
            BuilderStyleUtilities.SetInlineStyleValue(m_VisualTreeAsset, m_Target, k_TextOverflowName, result);
            m_Selection.NotifyOfHierarchyChange(null, m_Target, BuilderHierarchyChangeType.InlineStyle | BuilderHierarchyChangeType.FullRefresh);
            m_Selection.NotifyOfStylingChange(null, new List<string>() { k_TextOverflowName });
        }

        void TextWrapOnToggle()
        {
            var result = WhiteSpace.NoWrap;
            switch (m_Target.resolvedStyle.whiteSpace)
            {
                case WhiteSpace.Normal: result = WhiteSpace.NoWrap; break;
                case WhiteSpace.NoWrap: result = WhiteSpace.Normal; break;
            }
            TextWrapUpdateToggleIcon(result);
            BuilderStyleUtilities.SetInlineStyleValue(m_VisualTreeAsset, m_Target, k_TextWrapName, result);
            m_Selection.NotifyOfHierarchyChange(null, m_Target, BuilderHierarchyChangeType.InlineStyle | BuilderHierarchyChangeType.FullRefresh);
            m_Selection.NotifyOfStylingChange(null, new List<string>() { k_TextWrapName });
        }

        //
        // Align Self
        //

        void AlignSelfUpdateToggleIcon()
        {
            AlignSelfUpdateToggleIcon(m_Target.resolvedStyle.alignSelf);
        }

        void AlignSelfUpdateToggleIcon(Align resolvedStyle)
        {
            var button = m_AlignSelfButton;

            button.EnableInClassList("auto", resolvedStyle == Align.Auto);
            button.EnableInClassList("flex-start", resolvedStyle == Align.FlexStart);
            button.EnableInClassList("center", resolvedStyle == Align.Center);
            button.EnableInClassList("flex-end", resolvedStyle == Align.FlexEnd);
            button.EnableInClassList("stretch", resolvedStyle == Align.Stretch);
        }

        void AlignSelfOnToggle()
        {
            var result = Align.Auto;
            switch (m_Target.resolvedStyle.alignSelf)
            {
                case Align.Auto: result = Align.FlexStart; break;
                case Align.FlexStart: result = Align.Center; break;
                case Align.Center: result = Align.FlexEnd; break;
                case Align.FlexEnd: result = Align.Stretch; break;
                case Align.Stretch: result = Align.Auto; break;
            }
            AlignSelfUpdateToggleIcon(result);
            BuilderStyleUtilities.SetInlineStyleValue(m_VisualTreeAsset, m_Target, k_AlignSelfName, result);
            m_Selection.NotifyOfHierarchyChange(null, m_Target, BuilderHierarchyChangeType.InlineStyle);
            m_Selection.NotifyOfStylingChange(null, new List<string>() { k_AlignSelfName });
        }
    }
}
