// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class PersistedFoldout : BindableElement, INotifyValueChanged<bool>
    {
        [Serializable]
        public new class UxmlSerializedData : BindableElement.UxmlSerializedData
        {
            #pragma warning disable 649
            [SerializeField] string text;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags text_UxmlAttributeFlags;
            [SerializeField] bool value;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags value_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new PersistedFoldout();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (PersistedFoldout)obj;
                if (ShouldWriteAttributeValue(text_UxmlAttributeFlags))
                    e.text = text;
                if (ShouldWriteAttributeValue(value_UxmlAttributeFlags))
                    e.value = value;
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
                    evt.elementTarget = this;
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
        public static readonly string unindentedUssClassName = ussClassName + "--unindented";
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
