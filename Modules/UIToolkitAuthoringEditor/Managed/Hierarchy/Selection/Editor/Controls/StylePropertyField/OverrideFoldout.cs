// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [UxmlElement]
    internal partial class OverrideFoldout : OverrideRow, INotifyValueChanged<bool>
    {
        internal static readonly BindingId textProperty = nameof(text);
        internal static readonly BindingId valueProperty = nameof(value);

        internal new static readonly string ussClassName = "unity-override-foldout";
        internal static readonly string headerUssClassName = ussClassName + "__header";
        public static readonly string toggleUssClassName = ussClassName + "__toggle";
        internal static readonly string contentContainerUssClassName = ussClassName + "__content-container";
        internal new static readonly string isOverriddenUssClassName = ussClassName + "--overridden";

        protected VisualElement m_Header;
        protected Toggle m_Toggle;
        VisualElement m_Container;
        KeyboardNavigationManipulator m_NavigationManipulator;

        public override VisualElement contentContainer { get; }

        public VisualElement header => m_Header;
        public Toggle toggle => m_Toggle;

        protected internal override string GetIsOverriddenClassName() => isOverriddenUssClassName;

        protected override void OnTrackedPropertySourceChanged(ITrackablePropertyProvider provider, string propertyName, bool hasVariable, bool hasBinding)
        {
            // Foldouts are category containers — variable/binding styling applies only to leaf property rows.
        }

        [UxmlAttribute]
        [CreateProperty]
        public string text
        {
            get => m_Toggle.text;
            set
            {
                m_Toggle.text = value;
                NotifyPropertyChanged(textProperty);
            }
        }

        [SerializeField, DontCreateProperty]
        protected bool m_Value;

        [UxmlAttribute]
        [CreateProperty]
        public bool value
        {
            get
            {
                return  m_Toggle.value;
            }
            set
            {
                m_Toggle.value = value;
            }
        }

        public void SetValueWithoutNotify(bool newValue)
        {
            m_Toggle.SetValueWithoutNotify(newValue);
            contentContainer.style.display = newValue ? DisplayStyle.Flex : DisplayStyle.None;
            SetCheckedPseudoState(newValue);
        }

        public OverrideFoldout()
        {
            AddToClassList(ussClassName);
            AddToClassList(Foldout.ussClassName);

            m_Header = new VisualElement()
            {
                name = "unity-header",
            };
            m_Header.AddToClassList(headerUssClassName);
            hierarchy.Add(m_Header);

            m_Toggle = new Toggle
            {
                value = false,
                acceptClicksIfDisabled = true
            };
            m_Toggle.RegisterValueChangedCallback((evt) =>
            {
                value = m_Toggle.value;
                evt.StopPropagation();
            });
            m_Toggle.AddToClassList(toggleUssClassName);
            m_Toggle.AddToClassList(Foldout.toggleUssClassName);
            m_Header.hierarchy.Add(m_Toggle);

            m_Toggle.AddManipulator(m_NavigationManipulator = new KeyboardNavigationManipulator(Apply));

            m_Container = new VisualElement()
            {
                name = "unity-content",
            };
            m_Container.AddToClassList(contentContainerUssClassName);
            m_Container.AddToClassList(Foldout.contentUssClassName);
            hierarchy.Add(m_Container);

            m_Toggle.RegisterValueChangedCallback(evt =>
            {
                using (ChangeEvent<bool> e = ChangeEvent<bool>.GetPooled(evt.previousValue, evt.newValue))
                {
                    e.elementTarget = this;
                    SetValueWithoutNotify(value);
                    SendEvent(e);
                    SaveViewData();
                    NotifyPropertyChanged(valueProperty);
                    m_Value = evt.newValue;
                }
            });

            overrideContainer = m_Header;
            contentContainer = m_Container;
            value = true;
        }

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();

            var key = GetFullHierarchicalViewDataKey();

            OverwriteFromViewData(this, key);
            SetValueWithoutNotify(m_Value);
        }

        private void Apply(KeyboardNavigationOperation op, EventBase sourceEvent)
        {
            if (Apply(op))
            {
                sourceEvent.StopPropagation();
                focusController.IgnoreEvent(sourceEvent);
            }
        }

        private bool Apply(KeyboardNavigationOperation op)
        {
            switch (op)
            {
                case KeyboardNavigationOperation.Previous:
                case KeyboardNavigationOperation.Next:
                case KeyboardNavigationOperation.SelectAll:
                case KeyboardNavigationOperation.Cancel:
                case KeyboardNavigationOperation.Submit:
                case KeyboardNavigationOperation.Begin:
                case KeyboardNavigationOperation.End:
                case KeyboardNavigationOperation.PageDown:
                case KeyboardNavigationOperation.PageUp:
                    break; // Allow focus to move outside the Foldout
                case KeyboardNavigationOperation.MoveRight:
                    SetValueWithoutNotify(true);
                    return true;
                case KeyboardNavigationOperation.MoveLeft:
                    SetValueWithoutNotify(false);
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }

            return false;
        }
    }
}
