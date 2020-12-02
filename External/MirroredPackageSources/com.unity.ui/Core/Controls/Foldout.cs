namespace UnityEngine.UIElements
{
    /// <summary>
    /// A Foldout control is a collapsible section of a user interface. When toggled, it expands or collapses, which hides or reveals the elements it contains.
    /// </summary>
    /// <remarks>
    /// A Foldout consists of a <see cref="Toggle"/> sub-element and an empty <see cref="VisualElement"/>. The empty VisualElement
    /// is a container for the elements to show/hide when you expand/collapse the foldout. Foldout element's Toggle sub-element uses
    /// an arrow sprite instead of the <see cref="Toggle"/> control's usual checkbox. The arrow points right when the toggle is
    /// collapsed and down when it is expanded.
    /// </remarks>
    public class Foldout : BindableElement, INotifyValueChanged<bool>
    {
        /// <summary>
        /// Instantiates a <see cref="Foldout"/> using the data from a UXML file.
        /// </summary>
        /// <remarks>
        /// This class is added to every <see cref="VisualElement"/> created from UXML.
        /// </remarks>
        public new class UxmlFactory : UxmlFactory<Foldout, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Foldout"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the Foldout element properties that you can use in a UXML asset.
        /// </remarks>
        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription { name = "text" };
            UxmlBoolAttributeDescription m_Value = new UxmlBoolAttributeDescription { name = "value", defaultValue = true };

            /// <summary>
            /// Initializes <see cref="Foldout"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            /// <param name="cc">The creation context; unused.</param>
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

        /// <summary>
        /// This element contains the elements that are shown or hidden when you toggle the <see cref="Foldout"/>.
        /// </summary>
        public override VisualElement contentContainer
        {
            get
            {
                return m_Container;
            }
        }

        /// <summary>
        /// This is the text of the toggle's label.
        /// </summary>
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
        /// This is the state of the Foldout's toggle. It is true if the <see cref="Foldout"/> is open and its contents are
        /// visible, and false if the Foldout is closed, and its contents are hidden.
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

        /// <summary>
        /// Sets the value of the Foldout's Toggle sub-element, but does not notify the rest of the hierarchy of the change.
        /// </summary>
        /// <remarks>
        /// This is useful when you want to change the Foldout's Toggle value without triggering events. For example, let's say you
        /// set up a Foldout to trigger an animation, but you only want to trigger the animation when a user clicks the Foldout's Toggle,
        /// not when you change the Toggle's value via code (for example, inside another validation). You could use this method
        /// change the value "silently".        /// </remarks>
        /// <param name="newValue">The new value of the foldout</param>
        public void SetValueWithoutNotify(bool newValue)
        {
            m_Value = newValue;
            m_Toggle.value = m_Value;
            contentContainer.style.display = newValue ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        /// The USS class name for Foldout elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every instance of a <see cref="Foldout"/>. Any styling applied to
        /// this class affects every Foldout located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string ussClassName = "unity-foldout";
        /// <summary>
        /// The USS class name of Toggle sub-elements in Foldout elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the <see cref="Toggle"/> sub-element of every <see cref="Foldout"/>.
        /// Any styling applied to this class affects every Toggle sub-element located beside, or below the
        /// stylesheet in the visual tree.
        /// </remarks>
        public static readonly string toggleUssClassName = ussClassName + "__toggle";
        /// <summary>
        /// The USS class name for the content element in a Foldout.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the <see cref="VisualElement"/> that contains the elements to be shown
        /// or hidden. Any styling applied to this class affects every foldout container located beside, or
        /// below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string contentUssClassName = ussClassName + "__content";

        internal static readonly string ussFoldoutDepthClassName = ussClassName + "--depth-";
        internal static readonly int ussFoldoutMaxDepth = 4;

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();

            string key = GetFullHierarchicalViewDataKey();

            OverwriteFromViewData(this, key);
            SetValueWithoutNotify(m_Value);
        }

        /// <summary>
        /// Constructs a Foldout element.
        /// </summary>
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
            // Remove from all the depth classes...
            for (int i = 0; i <= ussFoldoutMaxDepth; i++)
            {
                RemoveFromClassList(ussFoldoutDepthClassName + i);
            }
            RemoveFromClassList(ussFoldoutDepthClassName + "max");

            // Figure out the real depth of this actual Foldout...
            var depth = this.GetFoldoutDepth();

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
