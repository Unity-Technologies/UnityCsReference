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

        AbstractNodeModel nodeModel => m_Model as AbstractNodeModel;
        VisualElement m_Root;
        string[] m_AdditionalUSS = null;
        bool m_OverriddenByCaller = false;

        const int k_SegmentWidth = 60;
        const float k_AnimationSpeedMultiplier = 0.5f;
        float m_AnimationSpeed;
        VisualElement m_MovingSegment;
        bool m_IsAnimating = false;

        float m_FillAmount = 0;

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

            m_MovingSegment = new VisualElement { name = "colorLineFillAmount" };
            m_MovingSegment.AddToClassList(m_ParentClassName.WithUssElement(colorLineName));

            m_MovingSegment.style.width = k_SegmentWidth;
            m_Root.Add(m_MovingSegment);

            m_Root.RegisterCallbackOnce<GeometryChangedEvent>(OnGeometryChanged);
            container.Add(m_Root);

            HideFillAmount();
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            SetFillAmount(nodeModel.FillAmount);
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            if (m_Root == null || m_OverriddenByCaller || nodeModel == null)
                return;

            if (visitor.ChangeHints.HasChange(ChangeHint.Style))
            {
                SetColor(nodeModel.ElementColor.HasUserColor ? nodeModel.ElementColor.Color : nodeModel.DefaultColor);
                SetFillAmount(nodeModel.FillAmount);
                onUpdateCallback?.Invoke();
            }
        }

        internal void SetFillAmount(float fillAmount)
        {
            m_FillAmount = Mathf.Clamp(fillAmount, -100f, 100f);
            if (m_FillAmount == 0f)
            {
                if (nodeModel.FillAmount != 0)
                    SetFillAmount(nodeModel.FillAmount);
                else
                    HideFillAmount();
                return;
            }

            float colorLineWidth = m_Root.resolvedStyle.width;
            float width = colorLineWidth * (Mathf.Abs(m_FillAmount)/ 100f);

            // When fill amount is between [-100, 0], accent fills from right to left.
            // When fill amount is between [0, 100], accent fills from left to right.
            if (m_FillAmount < 0)
                SetFillAmountFromRightToLeft();
            else SetFillAmountFromLeftToRight();

            m_MovingSegment.style.width = width;
            m_MovingSegment.style.translate = new Translate( 0, 0, 0);

            ShowFillAmount();
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

        internal void ResetFillAmount()
        {
            m_FillAmount = 0f;
            HideFillAmount();
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
            m_OverriddenByCaller = true;
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
            m_OverriddenByCaller = true;
        }

        public void PlayAnimation(float animationSpeed)
        {
            m_AnimationSpeed = animationSpeed;
            SetFillAmountFromLeftToRight();
            m_MovingSegment.style.translate = new Translate(-k_SegmentWidth, 0, 0);
            m_IsAnimating = true;
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

        public void StopAnimation()
        {
            m_IsAnimating = false;
            m_MovingSegment.style.translate = new Translate(-k_SegmentWidth, 0, 0);
            if (nodeModel != null && nodeModel.FillAmount != 0f)
                SetFillAmount(nodeModel.FillAmount);
            else
                HideFillAmount();
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
