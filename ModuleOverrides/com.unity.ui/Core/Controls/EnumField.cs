// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using static UnityEngine.EnumDataUtility;

namespace UnityEngine.UIElements
{
    static class EnumFieldHelpers
    {
        internal static readonly UxmlTypeAttributeDescription<Enum> type = new UxmlTypeAttributeDescription<Enum> { name = "type" };
        internal static readonly UxmlStringAttributeDescription value = new UxmlStringAttributeDescription { name = "value" };
        internal static readonly UxmlBoolAttributeDescription includeObsoleteValues = new UxmlBoolAttributeDescription() { name = "include-obsolete-values", defaultValue = false };

        internal static bool ExtractValue(IUxmlAttributes bag, CreationContext cc, out Type resEnumType, out Enum resEnumValue, out bool resIncludeObsoleteValues)
        {
            resIncludeObsoleteValues = false;
            resEnumValue = null;

            resEnumType = type.GetValueFromBag(bag, cc);
            if (resEnumType == null)
            {
                return false;
            }

            string specifiedValue = null;
            object resEnumValueObject = null;
            if (value.TryGetValueFromBag(bag, cc, ref specifiedValue) && !Enum.TryParse(resEnumType, specifiedValue, false, out resEnumValueObject))
            {
                Debug.LogErrorFormat("EnumField: Could not parse value of '{0}', because it isn't defined in the {1} enum.", specifiedValue, resEnumType.FullName);
                return false;
            }

            resEnumValue = specifiedValue != null && resEnumValueObject != null ? (Enum)resEnumValueObject : (Enum)Enum.ToObject(resEnumType, 0);
            resIncludeObsoleteValues = includeObsoleteValues.GetValueFromBag(bag, cc);

            return true;
        }
    }

    /// <summary>
    /// Makes a dropdown for switching between enum values.
    /// </summary>
    [MovedFrom(true, UpgradeConstants.EditorNamespace, UpgradeConstants.EditorAssembly)]
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

                if (EnumFieldHelpers.ExtractValue(bag, cc, out var resEnumType, out var resEnumValue, out var resIncludeObsoleteValues))
                {
                    EnumField enumField = (EnumField)ve;
                    enumField.Init(resEnumValue, resIncludeObsoleteValues);
                }
                // If we didn't have a valid value, try to set the type.
                else if (null != resEnumType)
                {
                    EnumField enumField = (EnumField)ve;

                    enumField.m_EnumType = resEnumType;
                    if (enumField.m_EnumType != null)
                        enumField.PopulateDataFromType(enumField.m_EnumType);
                    enumField.value = null;
                }
                else
                {
                    var enumField = (EnumField)ve;
                    enumField.m_EnumType = null;
                    enumField.value = null;
                }
            }
        }

        private Type m_EnumType;
        private bool m_IncludeObsoleteValues;
        private TextElement m_TextElement;
        private VisualElement m_ArrowElement;
        private EnumData m_EnumData;

        // These properties exist so that the UIBuilder can read them.
        internal Type type => m_EnumType;
        internal bool includeObsoleteValues => m_IncludeObsoleteValues;

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
            RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);
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

            m_IncludeObsoleteValues = includeObsoleteValues;
            PopulateDataFromType(defaultValue.GetType());

            // If the value is the same then we just need to ensure that the value label is
            // updated as m_EnumType may have been null when the value was set. (UUM-28904)
            if (!Enum.Equals(rawValue, defaultValue))
                SetValueWithoutNotify(defaultValue);
            else
                UpdateValueLabel(defaultValue);
        }

        internal void PopulateDataFromType(Type enumType)
        {
            m_EnumType = enumType;
            m_EnumData = GetCachedEnumData(m_EnumType, includeObsoleteValues ? CachedType.IncludeObsoleteExceptErrors : CachedType.ExcludeObsolete);
        }

        public override void SetValueWithoutNotify(Enum newValue)
        {
            if (!Enum.Equals(rawValue, newValue))
            {
                base.SetValueWithoutNotify(newValue);

                if (m_EnumType == null)
                    return;

                UpdateValueLabel(newValue);
            }
        }

        void UpdateValueLabel(Enum value)
        {
            int idx = Array.IndexOf(m_EnumData.values, value);

            if (idx >= 0 & idx < m_EnumData.values.Length)
            {
                m_TextElement.text = m_EnumData.displayNames[idx];
            }
            else
            {
                m_TextElement.text = string.Empty;
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

        bool ContainsPointer(int pointerId)
        {
            var elementUnderPointer = elementPanel.GetTopElementUnderPointer(pointerId);
            return this == elementUnderPointer || visualInput == elementUnderPointer;
        }

        void ProcessPointerDown<T>(PointerEventBase<T> evt) where T : PointerEventBase<T>, new()
        {
            if (evt.button == (int)MouseButton.LeftMouse)
            {
                if (ContainsPointer(evt.pointerId))
                {
                    ShowMenu();
                    evt.StopPropagation();
                }
            }
        }

        void OnNavigationSubmit(NavigationSubmitEvent evt)
        {
            ShowMenu();
            evt.StopPropagation();
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
            else
            {
                UpdateValueLabel(value);
            }

            m_TextElement.EnableInClassList(labelUssClassName, showMixedValue);
            m_TextElement.EnableInClassList(mixedValueLabelUssClassName, showMixedValue);
        }
    }
}
