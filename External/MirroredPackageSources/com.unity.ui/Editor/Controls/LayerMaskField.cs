using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Make a field for layer as masks.
    /// </summary>
    public class LayerMaskField : MaskField
    {
        /// <summary>
        /// Instantiates a <see cref="LayerMaskField"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<LayerMaskField, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="LayerMaskField"/>.
        /// </summary>
        public new class UxmlTraits : BasePopupField<int, UxmlIntAttributeDescription>.UxmlTraits
        {
            readonly UxmlIntAttributeDescription m_MaskValue = new UxmlIntAttributeDescription { name = "value" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                var layerMaskField = (LayerMaskField)ve;

                // The mask is simply an int
                layerMaskField.SetValueWithoutNotify(m_MaskValue.GetValueFromBag(bag, cc));
                base.Init(ve, bag, cc);
            }
        }

        /// <summary>
        /// Unsupported.
        /// </summary>
        public override Func<string, string> formatSelectedValueCallback
        {
            get { return null; }
            set
            {
                if (value != null)
                {
                    Debug.LogWarning(L10n.Tr("LayerMaskField doesn't support the formatting of the selected value."));
                }

                m_FormatSelectedValueCallback = null;
            }
        }

        /// <summary>
        /// Unsupported.
        /// </summary>
        public override Func<string, string> formatListItemCallback
        {
            get { return null; }
            set
            {
                if (value != null)
                {
                    Debug.LogWarning(L10n.Tr("LayerMaskField doesn't support the formatting of the list items."));
                }

                m_FormatListItemCallback = null;
            }
        }

        void UpdateLayersInfo()
        {
            // Get the layers : names and values
            string[] layerNames = null;
            int[] layerValues = null;
            TagManager.GetDefinedLayers(ref layerNames, ref layerValues);

            // Create the appropriate lists...
            choices = new List<string>(layerNames);
            choicesMasks = new List<int>(layerValues);
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-layer-mask-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";


        /// <summary>
        /// Constructor of the field.
        /// </summary>
        /// <param name="defaultMask">The mask to use for a first selection.</param>
        public LayerMaskField(int defaultMask)
            : this(null, defaultMask) {}

        /// <summary>
        /// Constructor of the field.
        /// </summary>
        /// <param name="label">The label to prefix the <see cref="LayerMaskField"/>.</param>
        /// <param name="defaultMask">The mask to use for a first selection.</param>
        public LayerMaskField(string label, int defaultMask)
            : this(label)
        {
            SetValueWithoutNotify(defaultMask);
        }

        /// <summary>
        /// Constructor of the field.
        /// </summary>
        public LayerMaskField()
            : this(null) {}


        /// <summary>
        /// Constructor of the field.
        /// </summary>
        /// <param name="label">The label to prefix the <see cref="LayerMaskField"/>.</param>
        public LayerMaskField(string label)
            : base(label)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);

            UpdateLayersInfo();
        }

        internal override void AddMenuItems(IGenericMenu menu)
        {
            // We must update the choices and the values since we don't know if they changed...
            UpdateLayersInfo();

            // Create the menu the usual way...
            base.AddMenuItems(menu);
        }
    }
}
