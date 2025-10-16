// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A part to show the icon for single state transitions.
    /// </summary>
    [UnityRestricted]
    internal class TransitionIconPart : BaseModelViewPart
    {
        public static readonly string ussClassName = "ge-transition-icon";

        Image m_Icon;

        /// <summary>
        /// Creates a new instance of the <see cref="TransitionIconPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model associated with the part.</param>
        /// <param name="ownerElement">The owner element of the part.</param>
        /// <param name="parentClassName">The parent class name of the part.</param>
        /// <returns>The created part.</returns>
        public static TransitionIconPart Create(string name, Model model, ChildView ownerElement, string parentClassName)
        {
            if (model is TransitionSupportModel)
            {
                return new TransitionIconPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        /// <inheritdoc />
        public override VisualElement Root => m_Icon;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransitionIconPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model associated with the part.</param>
        /// <param name="ownerElement">The owner element of the part.</param>
        /// <param name="parentClassName">The parent class name of the part.</param>
        TransitionIconPart(string name, Model model, ChildView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        /// <inheritdoc />
        protected override void BuildUI(VisualElement container)
        {
            m_Icon = new Image { name = PartName };
            m_Icon.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            container.Add(m_Icon);
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor) { }
    }
}
