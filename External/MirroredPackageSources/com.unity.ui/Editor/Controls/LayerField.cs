using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditorInternal;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// A <see cref="LayerField"/> editor.
    /// </summary>
    public class LayerField : PopupField<int>
    {
        /// <summary>
        /// Instantiates a <see cref="LayerField"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<LayerField, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="LayerField"/>.
        /// </summary>
        public new class UxmlTraits : PopupField<int>.UxmlTraits
        {
            UxmlIntAttributeDescription m_Value = new UxmlIntAttributeDescription { name = "value" };

            /// <summary>
            /// Initialize the traits.
            /// </summary>
            /// <param name="ve">VisualElement that will be created and populated.</param>
            /// <param name="bag">Bag of attributes where the data comes from.</param>
            /// <param name="cc">Creation context, unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var lf = (LayerField)ve;
                lf.SetValueWithoutNotify(m_Value.GetValueFromBag(bag, cc));
            }
        }


        internal override string GetValueToDisplay()
        {
            return InternalEditorUtility.GetLayerName(rawValue);
        }

        public override int value
        {
            get { return base.value; }
            set
            {
                if (m_Choices.Contains(value))
                {
                    base.value = value;
                }
            }
        }

        /// <summary>
        /// Unsupported.
        /// </summary>
        public override Func<int, string> formatSelectedValueCallback
        {
            get { return null; }
            set
            {
                if (value != null)
                {
                    Debug.LogWarning(L10n.Tr("LayerField doesn't support the formatting of the selected value."));
                }

                m_FormatSelectedValueCallback = null;
            }
        }

        /// <summary>
        /// Unsupported.
        /// </summary>
        public override Func<int, string> formatListItemCallback
        {
            get { return null; }
            set
            {
                if (value != null)
                {
                    Debug.LogWarning(L10n.Tr("LayerField doesn't support the formatting of the list items."));
                }

                m_FormatListItemCallback = null;
            }
        }

        public override void SetValueWithoutNotify(int newValue)
        {
            if (m_Choices.Contains(newValue))
            {
                base.SetValueWithoutNotify(newValue);
            }
        }

        static List<int> InitializeLayers()
        {
            var listOfIndex = new List<int>();
            for (var i = 0; i < 32; i++)
            {
                if (InternalEditorUtility.GetLayerName(i).Length != 0)
                {
                    listOfIndex.Add(i);
                }
            }
            return listOfIndex;
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-layer-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";


        public LayerField(string label)
            : base(label, InitializeLayers(), 0)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
            SetValueWithoutNotify(0);
        }

        public LayerField()
            : this(null) {}

        public LayerField(int defaultValue)
            : this(null, defaultValue)
        {
            SetValueWithoutNotify(defaultValue);
        }

        public LayerField(string label, int defaultValue)
            : this(label)
        {
            SetValueWithoutNotify(defaultValue);
        }

        internal override void AddMenuItems(IGenericMenu menu)
        {
            if (menu == null)
            {
                throw new ArgumentNullException(nameof(menu));
            }

            choices = InitializeLayers();
            string[] layerList = InternalEditorUtility.GetLayersWithId();
            for (var i = 0; i < layerList.Length; i++)
            {
                var item = layerList[i];
                var menuItemIndex = m_Choices[i];
                var isSelected = (menuItemIndex == value);
                menu.AddItem(item, isSelected, () => ChangeValueFromMenu(menuItemIndex));
            }
            menu.AddSeparator(String.Empty);
            menu.AddItem(L10n.Tr("Add Layer..."), false, OpenLayerInspector);
        }

        void ChangeValueFromMenu(int menuItemIndex)
        {
            value = menuItemIndex;
        }

        static void OpenLayerInspector()
        {
            TagManagerInspector.ShowWithInitialExpansion(TagManagerInspector.InitialExpansionState.Layers);
        }
    }
}
