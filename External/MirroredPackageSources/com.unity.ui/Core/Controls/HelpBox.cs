namespace UnityEngine.UIElements
{
    public enum HelpBoxMessageType
    {
        None = 0,
        Info = 1,
        Warning = 2,
        Error = 3
    }

    public class HelpBox : VisualElement
    {
        public static readonly string ussClassName = "unity-help-box";
        public static readonly string labelUssClassName = ussClassName + "__label";
        public static readonly string iconUssClassName = ussClassName + "__icon";
        public static readonly string iconInfoUssClassName = iconUssClassName + "--info";
        public static readonly string iconwarningUssClassName = iconUssClassName + "--warning";
        public static readonly string iconErrorUssClassName = iconUssClassName + "--error";

        public new class UxmlFactory : UxmlFactory<HelpBox, UxmlTraits> {}

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription { name = "text" };
            UxmlEnumAttributeDescription<HelpBoxMessageType> m_MessageType = new UxmlEnumAttributeDescription<HelpBoxMessageType>(){ name = "message-type", defaultValue = HelpBoxMessageType.None };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var helpBox = ve as HelpBox;
                helpBox.text = m_Text.GetValueFromBag(bag, cc);
                helpBox.messageType = m_MessageType.GetValueFromBag(bag, cc);
            }
        }

        HelpBoxMessageType m_HelpBoxMessageType;
        VisualElement m_Icon;
        string m_IconClass;
        Label m_Label;

        public string text
        {
            get { return m_Label.text; }
            set { m_Label.text = value; }
        }

        public HelpBoxMessageType messageType
        {
            get { return m_HelpBoxMessageType; }
            set
            {
                if (value != m_HelpBoxMessageType)
                {
                    m_HelpBoxMessageType = value;
                    UpdateIcon(value);
                }
            }
        }

        public HelpBox() : this(string.Empty, HelpBoxMessageType.None) {}

        public HelpBox(string text, HelpBoxMessageType messageType)
        {
            AddToClassList(ussClassName);

            m_HelpBoxMessageType = messageType;
            m_Label = new Label(text);
            m_Label.AddToClassList(labelUssClassName);
            Add(m_Label);

            m_Icon = new VisualElement();
            m_Icon.AddToClassList(iconUssClassName);
            UpdateIcon(messageType);
        }

        string GetIconClass(HelpBoxMessageType messageType)
        {
            switch (messageType)
            {
                case HelpBoxMessageType.Info:    return iconInfoUssClassName;
                case HelpBoxMessageType.Warning: return iconwarningUssClassName;
                case HelpBoxMessageType.Error:   return iconErrorUssClassName;
            }
            return null;
        }

        void UpdateIcon(HelpBoxMessageType messageType)
        {
            // Remove the old style
            if (!string.IsNullOrEmpty(m_IconClass))
            {
                m_Icon.RemoveFromClassList(m_IconClass);
            }

            m_IconClass = GetIconClass(messageType);

            if (m_IconClass == null)
            {
                m_Icon.RemoveFromHierarchy();
            }
            else
            {
                m_Icon.AddToClassList(m_IconClass);
                if (m_Icon.parent == null)
                {
                    Insert(0, m_Icon);
                }
            }
        }
    }
}
