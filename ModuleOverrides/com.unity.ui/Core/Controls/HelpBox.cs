// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Properties;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// User message types.
    /// </summary>
    public enum HelpBoxMessageType
    {
        /// <summary>
        /// Neutral message.
        /// </summary>
        None = 0,
        /// <summary>
        /// Info message.
        /// </summary>
        Info = 1,
        /// <summary>
        /// Warning message.
        /// </summary>
        Warning = 2,
        /// <summary>
        /// Error message.
        /// </summary>
        Error = 3
    }

    /// <summary>
    /// Makes a help box with a message to the user.
    /// </summary>
    /// <example>
    /// <code>
    /// public class HelpBoxExample : EditorWindow
    /// {
    ///     [MenuItem("Example/Help Box")]
    ///     static void ShowWindow()
    ///     {
    ///         HelpBoxExample window = (HelpBoxExample)EditorWindow.GetWindow(typeof(HelpBoxExample));
    ///         window.Show();
    ///     }
    ///
    ///     void OnEnable()
    ///     {
    ///         rootVisualElement.Add(new HelpBox("This is a help box", HelpBoxMessageType.Info));
    ///     }
    /// }
    /// </code>
    /// </example>
    public class HelpBox : VisualElement
    {
        internal static readonly DataBindingProperty textProperty = nameof(text);
        internal static readonly DataBindingProperty messageTypeProperty = nameof(messageType);

        /// <summary>
        /// The USS class name for Elements of this type.
        /// </summary>
        public static readonly string ussClassName = "unity-help-box";
        /// <summary>
        /// The USS class name for labels in Elements of this type.
        /// </summary>
        public static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// The USS class name for images in Elements of this type.
        /// </summary>
        public static readonly string iconUssClassName = ussClassName + "__icon";
        /// <summary>
        /// The USS class name for the <see cref="HelpBoxMessageType.Info"/> state in Elements of this type.
        /// </summary>
        public static readonly string iconInfoUssClassName = iconUssClassName + "--info";
        /// <summary>
        /// The USS class name for the <see cref="HelpBoxMessageType.Warning"/> state in Elements of this type.
        /// </summary>
        public static readonly string iconwarningUssClassName = iconUssClassName + "--warning";
        /// <summary>
        /// The USS class name for the <see cref="HelpBoxMessageType.Error"/> state in Elements of this type.
        /// </summary>
        public static readonly string iconErrorUssClassName = iconUssClassName + "--error";

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            #pragma warning disable 649
            [SerializeField] private string text;
            [SerializeField] private HelpBoxMessageType messageType;
            #pragma warning restore 649

            public override object CreateInstance() => new HelpBox();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (HelpBox)obj;
                e.text = text;
                e.messageType = messageType;
            }
        }

        /// <summary>
        /// Instantiates a <see cref="HelpBox"/> with data from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<HelpBox, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="HelpBox"/>.
        /// </summary>
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription { name = "text" };
            UxmlEnumAttributeDescription<HelpBoxMessageType> m_MessageType = new UxmlEnumAttributeDescription<HelpBoxMessageType>(){ name = "message-type", defaultValue = HelpBoxMessageType.None };

            /// <summary>
            /// Initializes <see cref="HelpBox"/> properties with values from an attribute bag.
            /// </summary>
            /// <param name="ve">The Element to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            /// <param name="cc">The creation context; unused.</param>
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

        /// <summary>
        /// The message text.
        /// </summary>
        [CreateProperty]
        public string text
        {
            get { return m_Label.text; }
            set
            {
                var previous = text;
                m_Label.text = value;

                if (string.CompareOrdinal(previous, text) != 0)
                    NotifyPropertyChanged(textProperty);
            }
        }

        /// <summary>
        /// The type of message.
        /// </summary>
        [CreateProperty]
        public HelpBoxMessageType messageType
        {
            get { return m_HelpBoxMessageType; }
            set
            {
                if (value != m_HelpBoxMessageType)
                {
                    m_HelpBoxMessageType = value;
                    UpdateIcon(value);
                    NotifyPropertyChanged(messageTypeProperty);
                }
            }
        }

        /// <summary>
        /// Creates a new HelpBox.
        /// </summary>
        public HelpBox() : this(string.Empty, HelpBoxMessageType.None) {}

        /// <summary>
        /// Creates a new HelpBox.
        /// </summary>
        /// <param name="text">The message text.</param>
        /// <param name="messageType">The type of message.</param>
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
