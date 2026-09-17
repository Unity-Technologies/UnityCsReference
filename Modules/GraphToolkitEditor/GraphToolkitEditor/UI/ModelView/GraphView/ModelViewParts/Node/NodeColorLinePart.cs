// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    [UnityRestricted]
    internal class NodeColorLinePart : BaseModelViewPart
    {
        public static readonly string ussClassName = "ge-color-line-part";

        /// <summary>
        /// The name of the <see cref="VisualElement"/> of the colored line.
        /// </summary>
        public static readonly string colorLineName = "color-line";

        /// <summary>
        /// The name of the <see cref="VisualElement"/> of the bar drawn on top of the color line.
        /// </summary>
        public static readonly string fillAmountName = "colorLineFill";

        public static readonly string fillName = "fill";
        public static readonly string fillMarkerUssClassName = ussClassName.WithUssElement(fillName).WithUssModifier("marker");
        public static readonly string fillMarkerReversedUssClassName = ussClassName.WithUssElement(fillName).WithUssModifier("marker-reversed");

        const int k_SegmentWidth = 60;

        AbstractNodeModel nodeModel => m_Model as AbstractNodeModel;
        VisualElement m_Root;
        VisualElement m_MovingSegment;

        string[] m_AdditionalUSS = null;

        float m_AnimationSpeed;
        float m_FillAmount;
        internal float FillAmount => m_FillAmount; // Used by unit tests

        bool m_IsAnimating;
        bool m_ColorOverridden;
        bool m_FillAmountOverridden;

        protected virtual bool HasMarker => false;

        // for testing
        internal Action onUpdateCallback;

        public static NodeColorLinePart Create(string name, Model model, ChildView ownerElement, string parentClassName,
            string[] extraUSS = null)
        {
            if (model is AbstractNodeModel)
                return new NodeColorLinePart(name, model, ownerElement, parentClassName, extraUSS);
            return null;
        }

        protected NodeColorLinePart(string name, Model model, ChildView ownerElement, string parentClassName,
            string[] extraUSS = null)
            : base(name, model, ownerElement, parentClassName)
        {
            m_AdditionalUSS = extraUSS;
        }

        /// <inheritdoc />
        public override VisualElement Root => m_Root;

        /// <inheritdoc />
        protected override void BuildUI(VisualElement container)
        {
            m_Root = new VisualElement { name = PartName };
            m_Root.AddToClassList(ussClassName);
            m_Root.AddToClassList(m_ParentClassName.WithUssElement(colorLineName));

            if (m_AdditionalUSS != null)
            {
                foreach (var uss in m_AdditionalUSS)
                    m_Root.AddToClassList(uss);
            }

            m_MovingSegment = new VisualElement { name = fillAmountName };
            m_MovingSegment.AddToClassList(ussClassName.WithUssElement(fillName));
            m_MovingSegment.AddToClassList(m_ParentClassName.WithUssElement(colorLineName));

            m_Root.Add(m_MovingSegment);

            container.Add(m_Root);

            ApplyFillAmount(nodeModel?.FillAmount ?? 0f);
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            if (m_Root == null || nodeModel == null)
                return;

            if (visitor.ChangeHints.HasChange(ChangeHint.Style))
            {
                if (!m_ColorOverridden)
                    SetColor(nodeModel.ElementColor.Color);

                if (!m_FillAmountOverridden)
                    ApplyFillAmount(nodeModel.FillAmount);

                onUpdateCallback?.Invoke();
            }
        }

        internal void OverrideFillAmount(float fillAmount)
        {
            m_FillAmountOverridden = true;
            ApplyFillAmount(fillAmount);
        }

        void ApplyFillAmount(float fillAmount)
        {
            if (m_IsAnimating)
                return;

            m_FillAmount = Mathf.Clamp(fillAmount, -100f, 100f);

            if (m_FillAmount == 0f)
            {
                HideFillAmount();
                UpdateMarker();
                return;
            }

            // When fill amount is between [-100, 0], accent fills from right to left.
            // When fill amount is between [0, 100], accent fills from left to right.
            if (m_FillAmount < 0)
                SetFillAmountFromRightToLeft();
            else SetFillAmountFromLeftToRight();

            m_MovingSegment.style.width = new StyleLength(Length.Percent(Mathf.Abs(m_FillAmount)));
            m_MovingSegment.style.translate = new Translate( 0, 0, 0);

            ShowFillAmount();
            UpdateMarker();
            m_MovingSegment.MarkDirtyRepaint();
        }

        void SetFillAmountFromLeftToRight()
        {
            m_Root.style.alignItems = Align.FlexStart;
        }

        void SetFillAmountFromRightToLeft()
        {
            m_Root.style.alignItems = Align.FlexEnd;
        }

        internal void ClearFillAmountOverride()
        {
            m_FillAmountOverridden = false;
            ApplyFillAmount(nodeModel?.FillAmount ?? 0f);
        }

        void SetColor(Color color)
        {
            m_Root.style.backgroundColor = color;
            m_MovingSegment.style.backgroundColor = new StyleColor(GetFillAmountColor(color));
        }

        internal static Color GetFillAmountColor(Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);

            if (v < 0.6f)
                v += 0.25f;
            else
                v -= 0.25f;

            return Color.HSVToRGB(h, s, v);
        }

        /// <summary>
        /// Overrides the color assigned to this part.
        /// </summary>
        /// <remarks>
        /// Once called, this part will stop updating from the model.
        /// This method has no parameter. The color is expected to be resolved from a stylesheet instead.
        /// Reset the override flag to re-enable this function.
        /// </remarks>
        public void OverrideColor()
        {
            m_ColorOverridden = true;
        }

        /// <summary>
        /// Overrides the color assigned to this part.
        /// </summary>
        /// <remarks>
        /// Once called, this part will stop updating from the model.
        /// The color set by this method will be the new color of the part, and won't be changed anymore by the part itself until the override is reset.
        /// </remarks>
        /// <param name="color"></param>
        public void OverrideColor(Color color)
        {
            SetColor(color);
            m_ColorOverridden = true;
        }

        /// <summary>
        /// Starts the looping animation, in which a segment travels across the colored line.
        /// </summary>
        public void PlayAnimation(float animationSpeed)
        {
            m_AnimationSpeed = animationSpeed;
            SetFillAmountFromLeftToRight();

            m_MovingSegment.style.width = k_SegmentWidth;
            m_MovingSegment.style.translate = new Translate(-k_SegmentWidth, 0, 0);
            m_MovingSegment.usageHints |= UsageHints.DynamicTransform;

            m_IsAnimating = true;

            UpdateMarker();
            ShowFillAmount();
        }

        public void UpdateAnimation(double deltaTime)
        {
            if (m_IsAnimating)
            {
                float translateDelta = m_Root.resolvedStyle.width * (float)deltaTime * m_AnimationSpeed;
                float newX = m_MovingSegment.style.translate.value.x.value + translateDelta;
                m_MovingSegment.style.translate = new Translate(newX, 0, 0);

                if (newX > m_Root.resolvedStyle.width + k_SegmentWidth)
                {
                    m_MovingSegment.style.translate = new Translate(-k_SegmentWidth, 0, 0);
                }
            }
        }

        /// <summary>
        /// Stops the looping animation and restores the fill amount defined on the model, if any.
        /// </summary>
        public void StopAnimation()
        {
            m_IsAnimating = false;
            m_MovingSegment.style.translate = new Translate(-k_SegmentWidth, 0, 0);
            m_MovingSegment.usageHints &= ~UsageHints.DynamicTransform;

            ApplyFillAmount(m_FillAmountOverridden ? m_FillAmount : (nodeModel?.FillAmount ?? 0f));
        }

        void UpdateMarker()
        {
            var showMarker = HasMarker && !m_IsAnimating && Mathf.Abs(m_FillAmount) > 0f && Mathf.Abs(m_FillAmount) < 100f;

            m_MovingSegment.EnableInClassList(fillMarkerUssClassName, showMarker && m_FillAmount > 0f);
            m_MovingSegment.EnableInClassList(fillMarkerReversedUssClassName, showMarker && m_FillAmount < 0f);
        }

        void ShowFillAmount()
        {
            m_MovingSegment.style.display = DisplayStyle.Flex;
        }

        void HideFillAmount()
        {
            if (m_IsAnimating)
                return;

            m_MovingSegment.style.display = DisplayStyle.None;
        }

        public class TestAccess
        {
            public readonly NodeColorLinePart colorLinePart;

            public TestAccess(NodeColorLinePart nodeColorLinePart)
            {
                this.colorLinePart = nodeColorLinePart;
            }

            public void BuildUI(VisualElement container) => colorLinePart.BuildUI(container);
        }
    }
}
