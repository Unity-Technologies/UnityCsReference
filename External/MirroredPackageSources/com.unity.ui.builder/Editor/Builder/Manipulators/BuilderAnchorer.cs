using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class BuilderAnchorer : BuilderManipulator
    {
        static readonly string s_UssClassName = "unity-builder-anchorer";
        static readonly string s_ActiveAnchorClassName = "unity-builder-anchorer--active";

        Dictionary<string, VisualElement> m_HandleElements;

        public new class UxmlFactory : UxmlFactory<BuilderAnchorer, UxmlTraits> {}

        public BuilderAnchorer()
        {
            var builderTemplate = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/Manipulators/BuilderAnchorer.uxml");
            builderTemplate.CloneTree(this);

            AddToClassList(s_UssClassName);

            m_HandleElements = new Dictionary<string, VisualElement>();

            m_HandleElements.Add("top-anchor", this.Q("top-anchor"));
            m_HandleElements.Add("left-anchor", this.Q("left-anchor"));
            m_HandleElements.Add("bottom-anchor", this.Q("bottom-anchor"));
            m_HandleElements.Add("right-anchor", this.Q("right-anchor"));

            m_HandleElements["top-anchor"].AddManipulator(new Clickable(OnAnchorClickTop));
            m_HandleElements["left-anchor"].AddManipulator(new Clickable(OnAnchorClickLeft));
            m_HandleElements["bottom-anchor"].AddManipulator(new Clickable(OnAnchorClickBottom));
            m_HandleElements["right-anchor"].AddManipulator(new Clickable(OnAnchorClickRight));

            m_AbsoluteOnlyHandleElements.Add(m_HandleElements["top-anchor"]);
            m_AbsoluteOnlyHandleElements.Add(m_HandleElements["left-anchor"]);
            m_AbsoluteOnlyHandleElements.Add(m_HandleElements["bottom-anchor"]);
            m_AbsoluteOnlyHandleElements.Add(m_HandleElements["right-anchor"]);
        }

        protected override void SetStylesFromTargetStyles()
        {
            base.SetStylesFromTargetStyles();

            if (m_Target == null)
                return;

            // Set Anchor active states.
            m_HandleElements["top-anchor"].RemoveFromClassList(s_ActiveAnchorClassName);
            m_HandleElements["left-anchor"].RemoveFromClassList(s_ActiveAnchorClassName);
            m_HandleElements["bottom-anchor"].RemoveFromClassList(s_ActiveAnchorClassName);
            m_HandleElements["right-anchor"].RemoveFromClassList(s_ActiveAnchorClassName);
            if (!IsNoneOrAuto(TrackedStyle.Top))
                m_HandleElements["top-anchor"].AddToClassList(s_ActiveAnchorClassName);
            if (!IsNoneOrAuto(TrackedStyle.Left))
                m_HandleElements["left-anchor"].AddToClassList(s_ActiveAnchorClassName);
            if (!IsNoneOrAuto(TrackedStyle.Bottom))
                m_HandleElements["bottom-anchor"].AddToClassList(s_ActiveAnchorClassName);
            if (!IsNoneOrAuto(TrackedStyle.Right))
                m_HandleElements["right-anchor"].AddToClassList(s_ActiveAnchorClassName);
        }

        void SetAnchorHandleState(TrackedStyle style, bool state)
        {
            string anchorName = string.Empty;
            switch (style)
            {
                case TrackedStyle.Top: anchorName = "top-anchor"; break;
                case TrackedStyle.Left: anchorName = "left-anchor"; break;
                case TrackedStyle.Bottom: anchorName = "bottom-anchor"; break;
                case TrackedStyle.Right: anchorName = "right-anchor"; break;
                default: return;
            }

            if (state)
                m_HandleElements[anchorName].AddToClassList(s_ActiveAnchorClassName);
            else
                m_HandleElements[anchorName].RemoveFromClassList(s_ActiveAnchorClassName);
        }

        void OnAnchorClick(TrackedStyle primaryStyle, TrackedStyle oppositeStyle, TrackedStyle lengthStyle)
        {
            var primaryIsUnset = IsNoneOrAuto(primaryStyle); // We can enable primary.
            var oppositeIsSet = !IsNoneOrAuto(oppositeStyle); // We can safely unset primary.

            if (!primaryIsUnset && !oppositeIsSet) // Nothing to do.
                return;

            var parentLength = GetResolvedStyleFloat(lengthStyle, m_Target.parent);
            var parentBorderPrimary = GetBorderResolvedStyleFloat(primaryStyle, m_Target.parent);
            var parentBorderOpposite = GetBorderResolvedStyleFloat(oppositeStyle, m_Target.parent);

            var primary = GetStyleSheetFloat(primaryStyle);
            var opposite = GetStyleSheetFloat(oppositeStyle);
            var length = GetStyleSheetFloat(lengthStyle);

            var primaryName = GetStyleName(primaryStyle);
            var lengthName = GetStyleName(lengthStyle);

            var marginPrimary = GetMargineResolvedStyleFloat(primaryStyle);
            var marginOpposite = GetMargineResolvedStyleFloat(oppositeStyle);
            var totalAxisMargin = marginPrimary + marginOpposite;

            var changeList = new List<string>() { primaryName, lengthName };

            if (primaryIsUnset)
            {
                var newPrimaryValue = parentLength - opposite - length - totalAxisMargin - parentBorderOpposite - parentBorderPrimary;
                SetStyleSheetValue(primaryName, newPrimaryValue);
                RemoveStyleSheetValue(lengthName);

                SetAnchorHandleState(primaryStyle, true);

                m_Selection.NotifyOfStylingChange(this, changeList);
                m_Selection.NotifyOfHierarchyChange(this, m_Target, BuilderHierarchyChangeType.InlineStyle);
            }
            else if (oppositeIsSet)
            {
                var newLengthValue = parentLength - opposite - primary - totalAxisMargin - parentBorderOpposite - parentBorderPrimary;
                SetStyleSheetValue(lengthName, newLengthValue);
                RemoveStyleSheetValue(primaryName);

                SetAnchorHandleState(primaryStyle, false);

                m_Selection.NotifyOfStylingChange(this, changeList);
                m_Selection.NotifyOfHierarchyChange(this, m_Target, BuilderHierarchyChangeType.InlineStyle);
            }
        }

        public void OnAnchorClickTop()
        {
            OnAnchorClick(TrackedStyle.Top, TrackedStyle.Bottom, TrackedStyle.Height);
        }

        public void OnAnchorClickRight()
        {
            OnAnchorClick(TrackedStyle.Right, TrackedStyle.Left, TrackedStyle.Width);
        }

        public void OnAnchorClickBottom()
        {
            OnAnchorClick(TrackedStyle.Bottom, TrackedStyle.Top, TrackedStyle.Height);
        }

        public void OnAnchorClickLeft()
        {
            OnAnchorClick(TrackedStyle.Left, TrackedStyle.Right, TrackedStyle.Width);
        }
    }
}
