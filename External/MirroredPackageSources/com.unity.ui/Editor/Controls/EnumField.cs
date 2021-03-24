using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    static class EnumFieldHelpers
    {
        internal static readonly UxmlTypeAttributeDescription<Enum> type = new UxmlTypeAttributeDescription<Enum> { name = "type" };
        internal static readonly UxmlStringAttributeDescription value = new UxmlStringAttributeDescription { name = "value" };
        internal static readonly UxmlBoolAttributeDescription includeObsoleteValues = new UxmlBoolAttributeDescription() { name = "include-obsolete-values", defaultValue = false };

        internal static bool ExtractValue(IUxmlAttributes bag, CreationContext cc, out Enum resEnumValue, out bool resIncludeObsoleteValues)
        {
            resIncludeObsoleteValues = false;
            resEnumValue = null;

            var systemType = type.GetValueFromBag(bag, cc);
            if (systemType == null)
            {
                return false;
            }

            string specifiedValue = null;
            if (value.TryGetValueFromBag(bag, cc, ref specifiedValue) && !Enum.IsDefined(systemType, specifiedValue))
            {
                Debug.LogErrorFormat("EnumField: Could not parse value of '{0}', because it isn't defined in the {1} enum.", specifiedValue, systemType.FullName);
                return false;
            }

            resEnumValue = specifiedValue != null ? (Enum)Enum.Parse(systemType, specifiedValue) : (Enum)Enum.ToObject(systemType, 0);
            resIncludeObsoleteValues = includeObsoleteValues.GetValueFromBag(bag, cc);

            return true;
        }
    }

    /// <summary>
    /// Makes a dropdown for switching between enum values.
    /// </summary>
    public class EnumField : BaseField<Enum>
    {
        /// <summary>
        /// Instantiates an <see cref="EnumField"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<EnumField, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="EnumField"/>.
        /// </summary>
        public new class UxmlTraits : BaseField<Enum>.UxmlTraits
        {
#pragma warning disable 414
            private UxmlTypeAttributeDescription<Enum> m_Type = EnumFieldHelpers.type;
            private UxmlStringAttributeDescription m_Value = EnumFieldHelpers.value;
            private UxmlBoolAttributeDescription m_IncludeObsoleteValues = EnumFieldHelpers.includeObsoleteValues;
#pragma warning restore 414

            /// <summary>
            /// Initialize <see cref="EnumField"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            /// <param name="cc">The creation context; unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                Enum resEnumValue;
                bool resIncludeObsoleteValues;
                if (EnumFieldHelpers.ExtractValue(bag, cc, out resEnumValue, out resIncludeObsoleteValues))
                {
                    EnumField enumField = (EnumField)ve;
                    enumField.Init(resEnumValue, resIncludeObsoleteValues);
                }
            }
        }

        private Type m_EnumType;
        private TextElement m_TextElement;
        private VisualElement m_ArrowElement;
        private EnumData m_EnumData;

        // Set this callback to provide a specific implementation of the menu.
        internal Func<IGenericMenu> createMenuCallback;

        /// <summary>
        /// Return the text value of the currently selected enum.
        /// </summary>
        public string text
        {
            get { return m_TextElement.text; }
        }

        private void Initialize(Enum defaultValue)
        {
            m_TextElement = new TextElement();
            m_TextElement.AddToClassList(textUssClassName);
            m_TextElement.pickingMode = PickingMode.Ignore;
            visualInput.Add(m_TextElement);

            m_ArrowElement = new VisualElement();
            m_ArrowElement.AddToClassList(arrowUssClassName);
            m_ArrowElement.pickingMode = PickingMode.Ignore;
            visualInput.Add(m_ArrowElement);
            if (defaultValue != null)
            {
                Init(defaultValue);
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-enum-field";
        /// <summary>
        /// USS class name of text elements in elements of this type.
        /// </summary>
        public static readonly string textUssClassName = ussClassName + "__text";
        /// <summary>
        /// USS class name of arrow indicators in elements of this type.
        /// </summary>
        public static readonly string arrowUssClassName = ussClassName + "__arrow";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        /// <summary>
        /// Construct an EnumField.
        /// </summary>
        public EnumField()
            : this((string)null) {}

        /// <summary>
        /// Construct an EnumField.
        /// </summary>
        /// <param name="defaultValue">Initial value. Also used to detect Enum type.</param>
        public EnumField(Enum defaultValue)
            : this(null, defaultValue) {}

        /// <summary>
        /// Construct an EnumField.
        /// </summary>
        /// <param name="defaultValue">Initial value. Also used to detect Enum type.</param>
        public EnumField(string label, Enum defaultValue = null)
            : base(label, null)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
            Initialize(defaultValue);

            RegisterCallback<PointerDownEvent>(OnPointerDownEvent);
            RegisterCallback<PointerMoveEvent>(OnPointerMoveEvent);
            RegisterCallback<MouseDownEvent>(e =>
            {
                if (e.button == (int)MouseButton.LeftMouse)
                    e.StopPropagation();
            });
        }

        /// <summary>
        /// Initializes the EnumField with a default value, and initializes its underlying type.
        /// </summary>
        /// <param name="defaultValue">The typed enum value.</param>
        public void Init(Enum defaultValue)
        {
            Init(defaultValue, false);
        }

        /// <summary>
        /// Initializes the EnumField with a default value, and initializes its underlying type.
        /// </summary>
        /// <param name="defaultValue">The typed enum value.</param>
        /// <param name="includeObsoleteValues">Set to true to display obsolete values as choices.</param>
        public void Init(Enum defaultValue, bool includeObsoleteValues)
        {
            if (defaultValue == null)
            {
                throw new ArgumentNullException(nameof(defaultValue));
            }

            m_EnumType = defaultValue.GetType();
            m_EnumData = EnumDataUtility.GetCachedEnumData(m_EnumType, !includeObsoleteValues);

            SetValueWithoutNotify(defaultValue);
        }

        public override void SetValueWithoutNotify(Enum newValue)
        {
            if (rawValue != newValue)
            {
                base.SetValueWithoutNotify(newValue);

                if (m_EnumType == null)
                    return;

                int idx = Array.IndexOf(m_EnumData.values, newValue);

                if (idx >= 0 & idx < m_EnumData.values.Length)
                {
                    m_TextElement.text = m_EnumData.displayNames[idx];
                }
            }
        }

        void OnPointerDownEvent(PointerDownEvent evt)
        {
            ProcessPointerDown(evt);
        }

        void OnPointerMoveEvent(PointerMoveEvent evt)
        {
            // Support cases where PointerMove corresponds to a MouseDown or MouseUp event with multiple buttons.
            if (evt.button == (int)MouseButton.LeftMouse)
            {
                if ((evt.pressedButtons & (1 << (int)MouseButton.LeftMouse)) != 0)
                {
                    ProcessPointerDown(evt);
                }
            }
        }

        void ProcessPointerDown<T>(PointerEventBase<T> evt) where T : PointerEventBase<T>, new()
        {
            if (evt.button == (int)MouseButton.LeftMouse)
            {
                if (visualInput.ContainsPoint(visualInput.WorldToLocal(evt.originalMousePosition)))
                {
                    ShowMenu();
                    evt.StopPropagation();
                }
            }
        }

        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            if (evt == null)
            {
                return;
            }

            KeyDownEvent kde = (evt as KeyDownEvent);
            if (kde != null)
            {
                if ((kde.keyCode == KeyCode.Space) ||
                    (kde.keyCode == KeyCode.KeypadEnter) ||
                    (kde.keyCode == KeyCode.Return))
                {
                    ShowMenu();
                    evt.StopPropagation();
                }
            }
        }

        private void ShowMenu()
        {
            if (m_EnumType == null)
                return;

            IGenericMenu menu;
            if (createMenuCallback != null)
            {
                menu = createMenuCallback.Invoke();
            }
            else
            {
                menu = elementPanel?.contextType == ContextType.Player ? new GenericDropdownMenu() : DropdownUtility.CreateDropdown();
            }

            int selectedIndex = Array.IndexOf(m_EnumData.values, value);

            for (int i = 0; i < m_EnumData.values.Length; ++i)
            {
                bool isSelected = selectedIndex == i;
                menu.AddItem(m_EnumData.displayNames[i], isSelected, contentView => ChangeValueFromMenu(contentView), m_EnumData.values[i]);
            }

            menu.DropDown(visualInput.worldBound, this, true);
        }

        private void ChangeValueFromMenu(object menuItem)
        {
            value = menuItem as Enum;
        }

        protected override void UpdateMixedValueContent()
        {
            if (showMixedValue)
            {
                m_TextElement.text = mixedValueString;
            }

            m_TextElement.EnableInClassList(labelUssClassName, showMixedValue);
            m_TextElement.EnableInClassList(mixedValueLabelUssClassName, showMixedValue);
        }
    }
}
