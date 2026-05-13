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

            container.Add(m_Root);
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            if (m_Root == null || m_OverriddenByCaller || nodeModel == null)
                return;

            if (visitor.ChangeHints.HasChange(ChangeHint.Style))
            {
                SetColor(nodeModel.ElementColor.HasUserColor ? nodeModel.ElementColor.Color : nodeModel.DefaultColor);
                onUpdateCallback?.Invoke();
            }
        }

        void SetColor(Color color)
        {
            m_Root.style.backgroundColor = color;
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
