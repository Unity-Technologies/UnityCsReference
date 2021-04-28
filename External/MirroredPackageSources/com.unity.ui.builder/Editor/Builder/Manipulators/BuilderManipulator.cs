using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    class BuilderManipulator : BuilderTracker
    {
        protected static readonly string s_WidthStyleName = "width";
        protected static readonly string s_HeightStyleName = "height";
        protected static readonly string s_LeftStyleName = "left";
        protected static readonly string s_TopStyleName = "top";
        protected static readonly string s_RightStyleName = "right";
        protected static readonly string s_BottomStyleName = "bottom";

        protected enum TrackedStyle
        {
            Width,
            Height,
            Left,
            Top,
            Right,
            Bottom
        }

        BuilderPaneWindow m_PaneWindow;
        protected BuilderSelection m_Selection;
        protected VisualTreeAsset m_VisualTreeAsset;

        protected List<VisualElement> m_AbsoluteOnlyHandleElements;

        public BuilderManipulator()
        {
            m_AbsoluteOnlyHandleElements = new List<VisualElement>();
        }

        public virtual void Activate(BuilderPaneWindow paneWindow, BuilderSelection selection, VisualTreeAsset visualTreeAsset, VisualElement target)
        {
            base.Activate(target);

            if (target == null)
                return;

            m_PaneWindow = paneWindow;
            m_Selection = selection;
            m_VisualTreeAsset = visualTreeAsset;
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

        protected string GetStyleName(TrackedStyle trackedStyle)
        {
            switch (trackedStyle)
            {
                case TrackedStyle.Width: return s_WidthStyleName;
                case TrackedStyle.Height: return s_HeightStyleName;
                case TrackedStyle.Left: return s_LeftStyleName;
                case TrackedStyle.Top: return s_TopStyleName;
                case TrackedStyle.Right: return s_RightStyleName;
                case TrackedStyle.Bottom: return s_BottomStyleName;
            }
            return null;
        }

        protected float GetResolvedStyleValue(TrackedStyle trackedStyle)
        {
            return GetResolvedStyleFloat(trackedStyle, m_Target);
        }

        protected float GetResolvedStyleFloat(TrackedStyle trackedStyle, VisualElement target)
        {
            if (target == null)
                return 0;

            switch (trackedStyle)
            {
                case TrackedStyle.Width: return target.resolvedStyle.width;
                case TrackedStyle.Height: return target.resolvedStyle.height;
                case TrackedStyle.Left: return target.resolvedStyle.left;
                case TrackedStyle.Top: return target.resolvedStyle.top;
                case TrackedStyle.Right: return target.resolvedStyle.right;
                case TrackedStyle.Bottom: return target.resolvedStyle.bottom;
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

        protected bool IsNoneOrAuto(TrackedStyle trackedStyle)
        {
            if (m_Target == null)
                return false;

            switch (trackedStyle)
            {
                case TrackedStyle.Width: return IsComputedStyleNoneOrAuto(m_Target.computedStyle.width);
                case TrackedStyle.Height: return IsComputedStyleNoneOrAuto(m_Target.computedStyle.height);
                case TrackedStyle.Left: return IsComputedStyleNoneOrAuto(m_Target.computedStyle.left);
                case TrackedStyle.Top: return IsComputedStyleNoneOrAuto(m_Target.computedStyle.top);
                case TrackedStyle.Right: return IsComputedStyleNoneOrAuto(m_Target.computedStyle.right);
                case TrackedStyle.Bottom: return IsComputedStyleNoneOrAuto(m_Target.computedStyle.bottom);
            }

            return false;
        }

        protected float GetMargineResolvedStyleFloat(TrackedStyle trackedStyle)
        {
            var target = m_Target;
            if (target == null)
                return 0;

            switch (trackedStyle)
            {
                case TrackedStyle.Left: return target.resolvedStyle.marginLeft;
                case TrackedStyle.Top: return target.resolvedStyle.marginTop;
                case TrackedStyle.Right: return target.resolvedStyle.marginRight;
                case TrackedStyle.Bottom: return target.resolvedStyle.marginBottom;
            }

            return 0;
        }

        protected float GetBorderResolvedStyleFloat(TrackedStyle trackedStyle, VisualElement target)
        {
            if (target == null)
                return 0;

            switch (trackedStyle)
            {
                case TrackedStyle.Left: return target.resolvedStyle.borderLeftWidth;
                case TrackedStyle.Top: return target.resolvedStyle.borderTopWidth;
                case TrackedStyle.Right: return target.resolvedStyle.borderRightWidth;
                case TrackedStyle.Bottom: return target.resolvedStyle.borderBottomWidth;
            }

            return 0;
        }

        protected TrackedStyle GetOppositeStyle(TrackedStyle trackedStyle)
        {
            switch (trackedStyle)
            {
                case TrackedStyle.Width: return TrackedStyle.Height;
                case TrackedStyle.Height: return TrackedStyle.Width;
                case TrackedStyle.Left: return TrackedStyle.Right;
                case TrackedStyle.Top: return TrackedStyle.Bottom;
                case TrackedStyle.Right: return TrackedStyle.Left;
                case TrackedStyle.Bottom: return TrackedStyle.Top;
            }

            throw new Exception("Invalid tracked style.");
        }

        protected TrackedStyle GetLengthStyle(TrackedStyle trackedStyle)
        {
            switch (trackedStyle)
            {
                case TrackedStyle.Width: return TrackedStyle.Width;
                case TrackedStyle.Height: return TrackedStyle.Height;
                case TrackedStyle.Left: return TrackedStyle.Width;
                case TrackedStyle.Top: return TrackedStyle.Height;
                case TrackedStyle.Right: return TrackedStyle.Width;
                case TrackedStyle.Bottom: return TrackedStyle.Height;
            }

            throw new Exception("Invalid tracked style.");
        }

        ///

        protected float GetStyleSheetFloat(TrackedStyle trackedStyle)
        {
            var name = GetStyleName(trackedStyle);

            if (IsNoneOrAuto(trackedStyle))
                return GetResolvedStyleFloat(trackedStyle, m_Target);
            else
                return GetStyleSheetFloat(name);
        }

        protected float GetStyleSheetFloat(string styleName)
        {
            var vea = m_Target.GetVisualElementAsset();
            var rule = m_VisualTreeAsset.GetOrCreateInlineStyleRule(vea);
            var styleSheet = m_VisualTreeAsset.inlineSheet;

            var styleProperty = styleSheet.FindProperty(rule, styleName);
            if (styleProperty == null)
                styleProperty = styleSheet.AddProperty(rule, styleName);

            if (styleProperty.values.Length == 0)
                return 0;
            else // TODO: Assume only one value.
                return styleSheet.GetFloat(styleProperty.values[0]);
        }

        protected void SetStyleSheetValue(TrackedStyle trackedStyle, float value)
        {
            var name = GetStyleName(trackedStyle);
            SetStyleSheetValue(name, value);
        }

        protected void SetStyleSheetValue(string styleName, float value)
        {
            // Remove temporary min-size element.
            m_Target.RemoveMinSizeSpecialElement();

            BuilderStyleUtilities.SetInlineStyleValue(m_VisualTreeAsset, m_Target, styleName, Mathf.Round(value));
        }

        protected void RemoveStyleSheetValue(string styleName)
        {
            var vea = m_Target.GetVisualElementAsset();
            var rule = m_VisualTreeAsset.GetOrCreateInlineStyleRule(vea);
            var styleSheet = m_VisualTreeAsset.inlineSheet;

            var styleProperty = styleSheet.FindProperty(rule, styleName);
            if (styleProperty == null)
                return;

            // TODO: Assume only one value.
            styleSheet.RemoveProperty(rule, styleProperty);
        }
    }
}
