// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A part to add a <see cref="ResizableElement"/> to a model UI.
    /// </summary>
    class FourWayResizerPart : BaseModelViewPart
    {
        /// <summary>
        /// Creates a new instance of the <see cref="FourWayResizerPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="FourWayResizerPart"/>.</returns>
        public static FourWayResizerPart Create(string name, Model model, ModelView ownerElement, string parentClassName)
        {
            if (model is IResizable)
            {
                return new FourWayResizerPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        /// <inheritdoc />
        public override VisualElement Root => m_ResizableElement;

        protected ResizableElement m_ResizableElement;

        /// <summary>
        /// Initializes a new instance of the <see cref="FourWayResizerPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected FourWayResizerPart(string name, Model model, ModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement container)
        {
            if ((m_Model as GraphElementModel)?.IsResizable() ?? false)
            {
                m_ResizableElement = new ResizableElement { name = PartName };
                m_ResizableElement.AddToClassList(m_ParentClassName.WithUssElement(PartName));
                container.Add(m_ResizableElement);
            }
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            if ((m_Model as GraphElementModel)?.IsResizable() ?? false)
            {
                if (m_ResizableElement != null)
                    m_ResizableElement.style.visibility = StyleKeyword.Null;
            }
            else
            {
                if (m_ResizableElement != null)
                    m_ResizableElement.style.visibility = Visibility.Hidden;
            }
        }
    }
}
