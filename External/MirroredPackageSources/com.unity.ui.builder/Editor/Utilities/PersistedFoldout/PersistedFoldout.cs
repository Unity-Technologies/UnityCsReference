using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class PersistedFoldout : BindableElement, INotifyValueChanged<bool>
    {
        public new class UxmlFactory : UxmlFactory<PersistedFoldout, UxmlTraits> { }

        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription { name = "text" };
            UxmlBoolAttributeDescription m_Value = new UxmlBoolAttributeDescription { name = "value" };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                ((PersistedFoldout)ve).text = m_Text.GetValueFromBag(bag, cc);
                ((PersistedFoldout)ve).value = m_Value.GetValueFromBag(bag, cc);
            }
        }

        protected VisualElement m_Header;
        protected Toggle m_Toggle;
        VisualElement m_OverrideBox;
        VisualElement m_Container;

        public VisualElement header => m_Header;
        public Toggle toggle => m_Toggle;

        public override VisualElement contentContainer
        {
            get
            {
                return m_Container;
            }
        }

        public virtual string text
        {
            get
            {
                return m_Toggle.text;
            }
            set
            {
                m_Toggle.text = value;
            }
        }

        [SerializeField]
        protected bool m_Value;

        public bool value
        {
            get
            {
                return m_Value;
            }
            set
            {
                if (m_Value == value)
                    return;

                using (ChangeEvent<bool> evt = ChangeEvent<bool>.GetPooled(m_Value, value))
                {
                    evt.target = this;
                    SetValueWithoutNotify(value);
                    SendEvent(evt);
                    SaveViewData();
                }
            }
        }
        public void SetValueWithoutNotify(bool newValue)
        {
            m_Value = newValue;
            m_Toggle.value = m_Value;
            contentContainer.style.display = newValue ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public static readonly string ussClassName = "unity-foldout";
        public static readonly string toggleUssClassName = ussClassName + "__toggle";
        public static readonly string headerUssClassName = ussClassName + "__header";
        public static readonly string contentUssClassName = ussClassName + "__content";

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();

            string key = GetFullHierarchicalViewDataKey();

            OverwriteFromViewData(this, key);
            SetValueWithoutNotify(m_Value);
        }

        public PersistedFoldout()
        {
            m_Value = true;

            AddToClassList(ussClassName);

            m_Header = new VisualElement()
            {
                name = "unity-header",
            };
            m_Header.AddToClassList(headerUssClassName);
            hierarchy.Add(m_Header);

            m_OverrideBox = new VisualElement() { };

            m_OverrideBox.AddToClassList(BuilderConstants.BuilderStyleRowBlueOverrideBoxClassName);
            header.hierarchy.Add(m_OverrideBox);

            m_Toggle = new Toggle
            {
                value = true
            };
            m_Toggle.RegisterValueChangedCallback((evt) =>
            {
                value = m_Toggle.value;
                evt.StopPropagation();
            });
            m_Toggle.AddToClassList(toggleUssClassName);
            m_Header.hierarchy.Add(m_Toggle);

            m_Container = new VisualElement()
            {
                name = "unity-content",
            };
            m_Container.AddToClassList(contentUssClassName);
            hierarchy.Add(m_Container);
        }

        protected void ReAssignTooltipToHeaderLabel()
        {
            var toggleInput = m_Toggle.Q<VisualElement>(classes: "unity-toggle__input");

            if (toggleInput != null && !string.IsNullOrEmpty(tooltip))
            {
                var tooltipTemp = tooltip;
                tooltip = null;
                toggleInput.tooltip = tooltipTemp;
            }
        }
    }
}
