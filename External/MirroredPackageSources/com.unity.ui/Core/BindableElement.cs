using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Element that can be bound to a property.
    /// </summary>
    public class BindableElement : VisualElement, IBindable
    {
        /// <summary>
        /// Instantiates a <see cref="BindableElement"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<BindableElement, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="BindableElement"/>.
        /// </summary>
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_PropertyPath;

            /// <summary>
            /// Constructor.
            /// </summary>
            public UxmlTraits()
            {
                m_PropertyPath = new UxmlStringAttributeDescription { name = "binding-path" };
            }

            /// <summary>
            /// Initialize <see cref="BindableElement"/> properties using values from the attribute bag.
            /// </summary>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                string propPath = m_PropertyPath.GetValueFromBag(bag, cc);

                if (!string.IsNullOrEmpty(propPath))
                {
                    var field = ve as IBindable;
                    if (field != null)
                    {
                        field.bindingPath = propPath;
                    }
                }
            }
        }

        /// <summary>
        /// Binding object that will be updated.
        /// </summary>
        public IBinding binding { get; set; }
        /// <summary>
        /// Path of the target property to be bound.
        /// </summary>
        public string bindingPath { get; set; }
    }
}
