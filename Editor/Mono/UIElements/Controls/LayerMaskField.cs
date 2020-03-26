// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    public class LayerMaskField : MaskField
    {
        public new class UxmlFactory : UxmlFactory<LayerMaskField, UxmlTraits> {}

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

        public new static readonly string ussClassName = "unity-layer-mask-field";
        public new static readonly string labelUssClassName = ussClassName + "__label";
        public new static readonly string inputUssClassName = ussClassName + "__input";


        public LayerMaskField(int defaultMask)
            : this(null, defaultMask) {}

        public LayerMaskField(string label, int defaultMask)
            : this(label)
        {
            SetValueWithoutNotify(defaultMask);
        }

        public LayerMaskField()
            : this(null) {}


        public LayerMaskField(string label)
            : base(label)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);

            UpdateLayersInfo();
        }

        internal override void AddMenuItems(GenericMenu menu)
        {
            // We must update the choices and the values since we don't know if they changed...
            UpdateLayersInfo();

            // Create the menu the usual way...
            base.AddMenuItems(menu);
        }
    }
}
