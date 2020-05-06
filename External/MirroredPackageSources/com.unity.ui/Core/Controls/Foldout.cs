namespace UnityEngine.UIElements
{
    /// <summary>
    /// Collapsable section of UI.
    /// </summary>
    public class Foldout : BindableElement, INotifyValueChanged<bool>
    {
        internal static readonly string ussFoldoutDepthClassName = "unity-foldout--depth-";
        internal static readonly int ussFoldoutMaxDepth = 4;

        /// <summary>
        /// Instantiates a <see cref="Foldout"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<Foldout, UxmlTraits> {}

        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription { name = "text" };
            UxmlBoolAttributeDescription m_Value = new UxmlBoolAttributeDescription { name = "value", defaultValue = true };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                Foldout f = ve as Foldout;
                if (f != null)
                {
                    f.text = m_Text.GetValueFromBag(bag, cc);
                    f.SetValueWithoutNotify(m_Value.GetValueFromBag(bag, cc));
                }
            }
        }

        Toggle m_Toggle;
        VisualElement m_Container;

        public override VisualElement contentContainer
        {
            get
            {
                return m_Container;
            }
        }

        public string text
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
        private bool m_Value;

        /// <summary>
        /// Contains the collapse state. True if the Foldout is open and the contents are visible. False if it's collapsed.
        /// </summary>
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

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string ussClassName = "unity-foldout";
        /// <summary>
        /// USS class name of toggle elements in elements of this type.
        /// </summary>
        public static readonly string toggleUssClassName = ussClassName + "__toggle";
        /// <summary>
        /// USS class name of content element in a Foldout.
        /// </summary>
        public static readonly string contentUssClassName = ussClassName + "__content";

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();

            string key = GetFullHierarchicalViewDataKey();

            OverwriteFromViewData(this, key);
            SetValueWithoutNotify(m_Value);
        }

        public Foldout()
        {
            m_Value = true;

            AddToClassList(ussClassName);

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
            hierarchy.Add(m_Toggle);

            m_Container = new VisualElement()
            {
                name = "unity-content",
            };
            m_Container.AddToClassList(contentUssClassName);
            hierarchy.Add(m_Container);

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            var depth = 0;
            // Remove from all the depth classes...
            for (int i = 0; i <= ussFoldoutMaxDepth; i++)
            {
                RemoveFromClassList(ussFoldoutDepthClassName + i);
            }
            RemoveFromClassList(ussFoldoutDepthClassName + "max");

            // Figure out the real depth of this actual Foldout...
            if (parent != null)
            {
                var curParent = parent;
                while (curParent != null)
                {
                    if (curParent.GetType() == typeof(Foldout))
                    {
                        depth++;
                    }
                    curParent = curParent.parent;
                }
            }

            // Add the class name corresponding to that depth
            if (depth > ussFoldoutMaxDepth)
            {
                AddToClassList(ussFoldoutDepthClassName + "max");
            }
            else
            {
                AddToClassList(ussFoldoutDepthClassName + depth);
            }
        }
    }
}
