// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditorInternal;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// A LayerField editor.
    /// </summary>
    public class LayerField : PopupField<int>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : PopupField<int>.UxmlSerializedData
        {
            #pragma warning disable 649
            [UxmlAttribute("value")]
            [SerializeField, LayerDecorator] int layer;
            #pragma warning restore 649

            public override object CreateInstance() => new LayerField();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (LayerField)obj;
                e.SetValueWithoutNotify(layer);
            }
        }

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

        internal int layer { get => value; set => this.value = value; }

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

        /// <summary>
        /// Initializes and returns an instance of LayerField.
        /// </summary>
        /// <param name="label">The text to use as a label for the field.</param>
        public LayerField(string label)
            : base(label, InitializeLayers(), 0)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
            SetValueWithoutNotify(0);
        }

        /// <summary>
        /// Initializes and returns an instance of LayerField.
        /// </summary>
        public LayerField()
            : this(null) {}

        /// <summary>
        /// Initializes and returns an instance of LayerField.
        /// </summary>
        /// <param name="defaultValue">The initial layer value this field should use.</param>
        public LayerField(int defaultValue)
            : this(null, defaultValue)
        {
            SetValueWithoutNotify(defaultValue);
        }

        /// <summary>
        /// Initializes and returns an instance of LayerField.
        /// </summary>
        /// <param name="label">The text to use as a label for the field.</param>
        /// <param name="defaultValue">The initial layer value this field should use.</param>
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
