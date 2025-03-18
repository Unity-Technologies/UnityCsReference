// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class BuilderManipulator : BuilderTracker
    {
        [Serializable]
        public new class UxmlSerializedData : BuilderTracker.UxmlSerializedData
        {
            public override object CreateInstance() => new BuilderManipulator();
        }

        protected static readonly string s_WidthStyleName = "width";
        protected static readonly string s_HeightStyleName = "height";
        protected static readonly string s_LeftStyleName = "left";
        protected static readonly string s_TopStyleName = "top";
        protected static readonly string s_RightStyleName = "right";
        protected static readonly string s_BottomStyleName = "bottom";

        [Flags]
        protected enum TrackedStyles
        {
            None = 0,
            Width = 1,
            Height = 2,
            Left = 4,
            Top = 8,
            Right = 16,
            Bottom = 32,
            All = Width | Height | Left | Top | Right | Bottom
        }

        private static readonly TrackedStyles[] s_TrackedStyles = { TrackedStyles.Width, TrackedStyles.Height, TrackedStyles.Left, TrackedStyles.Top, TrackedStyles.Right, TrackedStyles.Bottom };

        BuilderPaneWindow m_PaneWindow;
        protected BuilderSelection m_Selection;
        protected VisualTreeAsset m_VisualTreeAsset;
        protected BuilderBindingsCacheSubscriber m_BindingsCacheSubscriber;

        protected List<VisualElement> m_AbsoluteOnlyHandleElements;

        protected TrackedStyles m_BoundStyles = TrackedStyles.None;

        public BuilderManipulator()
        {
            m_AbsoluteOnlyHandleElements = new List<VisualElement>();
            m_BindingsCacheSubscriber = new BuilderBindingsCacheSubscriber((_, _) =>  UpdateBoundStyles());
            foreach (var trackedStyle in s_TrackedStyles)
            {
                var styleName = GetStyleName(trackedStyle);
                var propertyPath = BuilderConstants.StylePropertyPathPrefix + BuilderNameUtilities.ConvertStyleUssNameToCSharpName(styleName);

                m_BindingsCacheSubscriber.filteredProperties.Add(propertyPath);
            }
        }

        protected virtual void UpdateBoundStyles()
        {
            m_BoundStyles = TrackedStyles.None;

            if (m_BindingsCacheSubscriber.cache != null)
            {
                foreach (var trackedStyle in s_TrackedStyles)
                {
                    var styleName = GetStyleName(trackedStyle);
                    var propertyPath = BuilderConstants.StylePropertyPathPrefix +
                                       BuilderNameUtilities.ConvertStyleUssNameToCSharpName(styleName);

                    if (m_BindingsCacheSubscriber.cache.HasResolvedBinding(m_Target, propertyPath))
                        m_BoundStyles |= trackedStyle;
                }
            }
        }

        protected bool AreStylesBound(TrackedStyles styles)
        {
            return (m_BoundStyles & styles) != 0;
        }

        public virtual void Activate(BuilderPaneWindow paneWindow, BuilderSelection selection, VisualTreeAsset visualTreeAsset, VisualElement target, BuilderBindingsCache bindingsCache)
        {
            base.Activate(target);

            if (target == null)
                return;

            m_PaneWindow = paneWindow;
            m_Selection = selection;
            m_VisualTreeAsset = visualTreeAsset;
            m_BindingsCacheSubscriber.cache = bindingsCache;
            UpdateBoundStyles();
        }

        public override void Deactivate()
        {
            base.Deactivate();
            m_BindingsCacheSubscriber.cache = null;
        }

        protected override void SetStylesFromTargetStyles()
        {
            if (m_Target == null)
                return;

            if (m_Target.resolvedStyle.display == DisplayStyle.None ||
                BuilderSharedStyles.IsDocumentElement(m_Target) ||
                m_Target.GetVisualElementAsset() == null ||
                !m_Target.IsPartOfActiveVisualTreeAsset(m_PaneWindow?.document))
            {
                this.RemoveFromClassList(s_ActiveClassName);
                return;
            }
            else
            {
                this.AddToClassList(s_ActiveClassName);
            }

            if (m_Target.resolvedStyle.position == Position.Relative)
            {
                foreach (var element in m_AbsoluteOnlyHandleElements)
                    element.style.display = DisplayStyle.None;
            }
            else
            {
                foreach (var element in m_AbsoluteOnlyHandleElements)
                    element.style.display = DisplayStyle.Flex;
            }
        }

        ///

        protected static string GetStyleName(TrackedStyles trackedStyles)
        {
            switch (trackedStyles)
            {
                case TrackedStyles.Width: return s_WidthStyleName;
                case TrackedStyles.Height: return s_HeightStyleName;
                case TrackedStyles.Left: return s_LeftStyleName;
                case TrackedStyles.Top: return s_TopStyleName;
                case TrackedStyles.Right: return s_RightStyleName;
                case TrackedStyles.Bottom: return s_BottomStyleName;
            }
            return null;
        }

        protected float GetResolvedStyleValue(TrackedStyles trackedStyles)
        {
            return GetResolvedStyleFloat(trackedStyles, m_Target);
        }

        protected float GetResolvedStyleFloat(TrackedStyles trackedStyles, VisualElement target)
        {
            if (target == null)
                return 0;

            switch (trackedStyles)
            {
                case TrackedStyles.Width: return target.resolvedStyle.width;
                case TrackedStyles.Height: return target.resolvedStyle.height;
                case TrackedStyles.Left: return target.resolvedStyle.left;
                case TrackedStyles.Top: return target.resolvedStyle.top;
                case TrackedStyles.Right: return target.resolvedStyle.right;
                case TrackedStyles.Bottom: return target.resolvedStyle.bottom;
            }
            return 0;
        }

        bool IsComputedStyleNoneOrAuto(Length length)
        {
            return length.CallBoolMethodByReflection("IsNone") || length.CallBoolMethodByReflection("IsAuto");
        }

        bool IsComputedStyleNoneOrAuto(StyleLength styleLength)
        {
            return styleLength == StyleKeyword.None || styleLength == StyleKeyword.Auto;
        }

        protected bool IsNoneOrAuto(TrackedStyles trackedStyles)
        {
            if (m_Target == null)
                return false;

            switch (trackedStyles)
            {
                case TrackedStyles.Width: return IsComputedStyleNoneOrAuto(m_Target.computedStyle.width);
                case TrackedStyles.Height: return IsComputedStyleNoneOrAuto(m_Target.computedStyle.height);
                case TrackedStyles.Left: return IsComputedStyleNoneOrAuto(m_Target.computedStyle.left);
                case TrackedStyles.Top: return IsComputedStyleNoneOrAuto(m_Target.computedStyle.top);
                case TrackedStyles.Right: return IsComputedStyleNoneOrAuto(m_Target.computedStyle.right);
                case TrackedStyles.Bottom: return IsComputedStyleNoneOrAuto(m_Target.computedStyle.bottom);
            }

            return false;
        }

        protected float GetMarginResolvedStyleFloat(TrackedStyles trackedStyles)
        {
            if (m_Target == null)
                return 0;

            switch (trackedStyles)
            {
                case TrackedStyles.Left: return m_Target.resolvedStyle.marginLeft;
                case TrackedStyles.Top: return m_Target.resolvedStyle.marginTop;
                case TrackedStyles.Right: return m_Target.resolvedStyle.marginRight;
                case TrackedStyles.Bottom: return m_Target.resolvedStyle.marginBottom;
            }

            return 0;
        }

        protected float GetBorderResolvedStyleFloat(TrackedStyles trackedStyles, VisualElement target)
        {
            if (target == null)
                return 0;

            switch (trackedStyles)
            {
                case TrackedStyles.Left: return target.resolvedStyle.borderLeftWidth;
                case TrackedStyles.Top: return target.resolvedStyle.borderTopWidth;
                case TrackedStyles.Right: return target.resolvedStyle.borderRightWidth;
                case TrackedStyles.Bottom: return target.resolvedStyle.borderBottomWidth;
            }

            return 0;
        }

        protected TrackedStyles GetOppositeStyle(TrackedStyles trackedStyles)
        {
            switch (trackedStyles)
            {
                case TrackedStyles.Width: return TrackedStyles.Height;
                case TrackedStyles.Height: return TrackedStyles.Width;
                case TrackedStyles.Left: return TrackedStyles.Right;
                case TrackedStyles.Top: return TrackedStyles.Bottom;
                case TrackedStyles.Right: return TrackedStyles.Left;
                case TrackedStyles.Bottom: return TrackedStyles.Top;
            }

            throw new Exception("Invalid tracked style.");
        }

        protected TrackedStyles GetLengthStyle(TrackedStyles trackedStyles)
        {
            switch (trackedStyles)
            {
                case TrackedStyles.Width: return TrackedStyles.Width;
                case TrackedStyles.Height: return TrackedStyles.Height;
                case TrackedStyles.Left: return TrackedStyles.Width;
                case TrackedStyles.Top: return TrackedStyles.Height;
                case TrackedStyles.Right: return TrackedStyles.Width;
                case TrackedStyles.Bottom: return TrackedStyles.Height;
            }

            throw new Exception("Invalid tracked style.");
        }

        ///

        protected float GetStyleSheetFloat(TrackedStyles trackedStyles)
        {
            var name = GetStyleName(trackedStyles);

            if (IsNoneOrAuto(trackedStyles))
                return GetResolvedStyleFloat(trackedStyles, m_Target);
            else
                return GetStyleSheetFloat(name);
        }

        protected float GetStyleSheetFloat(string styleName)
        {
            var vea = m_Target.GetVisualElementAsset();
            var rule = m_VisualTreeAsset.GetOrCreateInlineStyleRule(vea);
            var styleSheet = m_VisualTreeAsset.inlineSheet;

            var styleProperty = styleSheet.FindLastProperty(rule, styleName);
            if (styleProperty == null)
                styleProperty = styleSheet.AddProperty(rule, styleName);

            if (styleProperty.values.Length == 0)
                return 0;
            else // TODO: Assume only one value.
                return styleSheet.GetFloat(styleProperty.values[0]);
        }

        protected void SetStyleSheetValue(TrackedStyles trackedStyles, float value)
        {
            if (AreStylesBound(trackedStyles))
                return;

            var name = GetStyleName(trackedStyles);
            SetStyleSheetValue(name, value);
        }

        void SetStyleSheetValue(string styleName, float value)
        {
            BuilderStyleUtilities.SetInlineStyleValue(m_VisualTreeAsset, m_Target, styleName, Mathf.Round(value));
        }

        protected void RemoveStyleSheetValue(string styleName)
        {
            var vea = m_Target.GetVisualElementAsset();
            var rule = m_VisualTreeAsset.GetOrCreateInlineStyleRule(vea);
            var styleSheet = m_VisualTreeAsset.inlineSheet;

            var styleProperty = styleSheet.FindLastProperty(rule, styleName);
            if (styleProperty == null)
                return;

            // TODO: Assume only one value.
            styleSheet.RemoveProperty(rule, styleProperty);
        }
    }
}
