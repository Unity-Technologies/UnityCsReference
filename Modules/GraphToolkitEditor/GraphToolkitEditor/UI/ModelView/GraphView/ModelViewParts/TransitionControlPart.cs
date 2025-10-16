// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A part that shows a <see cref="TransitionControl"/>.
    /// </summary>
    [UnityRestricted]
    internal class TransitionControlPart : GraphElementPart
    {
        public static readonly string ussClassName = "ge-transition-control";

        TransitionControl m_TransitionControl;
        TransitionArrow m_TransitionArrow;

        /// <inheritdoc />
        public override VisualElement Root => m_TransitionControl;

        /// <summary>
        /// The transition arrow.
        /// </summary>
        protected TransitionArrow TransitionArrow
        {
            get
            {
                if (m_TransitionArrow == null)
                {
                    var wireControlPart = PartList.GetPart(Transition.transitionArrowPartName);
                    m_TransitionArrow = wireControlPart?.Root as TransitionArrow;
                }

                return m_TransitionArrow;
            }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TransitionControlPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model associated with the part.</param>
        /// <param name="ownerElement">The owner element of the part.</param>
        /// <param name="parentClassName">The parent class name of the part.</param>
        /// <returns>The created part.</returns>
        public static TransitionControlPart Create(string name, Model model, ChildView ownerElement, string parentClassName)
        {
            if (model is TransitionSupportModel)
            {
                return new TransitionControlPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransitionControlPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model associated with the part.</param>
        /// <param name="ownerElement">The owner element of the part.</param>
        /// <param name="parentClassName">The parent class name of the part.</param>
        protected TransitionControlPart(string name, Model model, ChildView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName)
        {
            PartList.AppendPart(TransitionArrowPart.Create(Transition.transitionArrowPartName, model, m_OwnerElement, ussClassName));
        }

        /// <inheritdoc />
        protected override void BuildUI(VisualElement container)
        {
            m_TransitionControl = new TransitionControl(m_OwnerElement as Transition) { name = PartName };
            m_TransitionControl.AddToClassList(ussClassName);
            m_TransitionControl.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            container.Add(m_TransitionControl);
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            m_TransitionControl.TransitionArrow = (TransitionArrow)PartList.GetPart(Transition.transitionArrowPartName).Root;
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            if (visitor.ChangeHints.HasChange(ChangeHint.Layout) || visitor.ChangeHints.HasChange(ChangeHint.Data))
            {
                m_TransitionControl.UpdateLayout();
                TransitionArrow?.UpdateLayout();
            }
        }

        /// <inheritdoc />
        public override void SetLevelOfDetail(float zoom, GraphViewZoomMode newZoomMode, GraphViewZoomMode oldZoomMode)
        {
            m_TransitionControl.Zoom = zoom;
        }
    }
}
