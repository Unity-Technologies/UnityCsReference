// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A part that shows a <see cref="TransitionCounter"/> inside a <see cref="TransitionArrow"/>.
    /// </summary>
    [UnityRestricted]
    internal class TransitionCounterPart : GraphElementPart
    {
        TransitionCounter m_Counter;
        int m_LastCount = -1;

        /// <summary>
        /// Creates a new instance of the <see cref="TransitionCounterPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model associated with the part.</param>
        /// <param name="ownerElement">The owner element of the part.</param>
        /// <param name="parentClassName">The parent class name of the part.</param>
        /// <returns>The created part.</returns>
        public static TransitionCounterPart Create(string name, Model model, ChildView ownerElement, string parentClassName)
        {
            if (model is TransitionSupportModel)
                return new TransitionCounterPart(name, model, ownerElement, parentClassName);

            return null;
        }

        /// <inheritdoc />
        public override VisualElement Root => m_Counter;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransitionCounterPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model associated with the part.</param>
        /// <param name="ownerElement">The owner element of the part.</param>
        /// <param name="parentClassName">The parent class name of the part.</param>
        TransitionCounterPart(string name, Model model, ChildView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName)
        {
        }

        /// <inheritdoc />
        protected override void BuildUI(VisualElement container)
        {
            m_Counter = new TransitionCounter { name = PartName };
            m_Counter.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            container.Add(m_Counter);
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            var transitionModel = m_Model as TransitionSupportModel;
            var newCount = transitionModel != null ? transitionModel.Transitions.Count : 0;

            if (newCount != m_LastCount)
            {
                if (transitionModel != null)
                {
                    m_Counter.SetCount(newCount, !transitionModel.IsSingleStateTransition);
                }
                else
                {
                    m_Counter.SetCount(newCount, false);
                }

                m_LastCount = newCount;
            }
            else if (visitor.ChangeHints.HasChange(ChangeHint.Layout))
            {
                m_Counter.UpdateLayout();
            }
        }
    }
}
