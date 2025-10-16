// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A part that shows a <see cref="TransitionArrow"/>.
    /// </summary>
    [UnityRestricted]
    internal class TransitionArrowPart : GraphElementPart
    {
        public static readonly string ussClassName = "ge-transition-arrow";
        public static readonly string longArrowUssClassName = ussClassName.WithUssModifier("long");
        public static readonly string shortArrowUssClassName = ussClassName.WithUssModifier("short");

        public static readonly string transitionIconPartName = "transition-icon";
        public static readonly string transitionCounterPartName = "transition-counter";

        TransitionArrow m_Arrow;
        string m_ShortLongClassName;

        /// <summary>
        /// Creates a new instance of the <see cref="TransitionArrowPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model associated with the part.</param>
        /// <param name="ownerElement">The owner element of the part.</param>
        /// <param name="parentClassName">The parent class name of the part.</param>
        /// <returns>The created part.</returns>
        public static TransitionArrowPart Create(string name, Model model, ChildView ownerElement, string parentClassName)
        {
            if (model is TransitionSupportModel)
            {
                return new TransitionArrowPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        /// <inheritdoc />
        public override VisualElement Root => m_Arrow;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransitionArrowPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model associated with the part.</param>
        /// <param name="ownerElement">The owner element of the part.</param>
        /// <param name="parentClassName">The parent class name of the part.</param>
        protected TransitionArrowPart(string name, Model model, ChildView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName)
        {
            if (m_Model is TransitionSupportModel { IsSingleStateTransition: true })
            {
                PartList.AppendPart(TransitionIconPart.Create(transitionIconPartName, m_Model, m_OwnerElement, ussClassName));
            }

            PartList.AppendPart(TransitionCounterPart.Create(transitionCounterPartName, m_Model, m_OwnerElement, ussClassName));
        }

        /// <inheritdoc />
        protected override void BuildUI(VisualElement container)
        {
            m_Arrow = new TransitionArrow { name = ussClassName };
            m_Arrow.AddToClassList(ussClassName);
            m_Arrow.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            container.Add(m_Arrow);
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            var isLong = m_Model is TransitionSupportModel transitionModel && transitionModel.Transitions.Count > 1;

            if (isLong)
            {
                m_Arrow.ReplaceAndCacheClassName(longArrowUssClassName, ref m_ShortLongClassName);
            }
            else
            {
                m_Arrow.ReplaceAndCacheClassName(shortArrowUssClassName, ref m_ShortLongClassName);
            }

            if (visitor.ChangeHints.HasChange(ChangeHint.Layout) || visitor.ChangeHints.HasChange(ChangeHint.Data))
            {
                m_Arrow.UpdateLayout();
            }
        }
    }
}
