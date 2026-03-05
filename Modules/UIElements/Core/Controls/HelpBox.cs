// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
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
    /// Makes a help box with a message to the user. For more information, refer to [[wiki:UIE-uxml-element-HelpBox|UXML element HelpBox]].
    /// </summary>
    /// <remarks>
    /// {img UIElementsHelpBox.png}
    /// </remarks>
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
    [Icon("UIToolkit/Icons/HelpBox.png")]
    public partial class HelpBox : VisualElement
    {
        internal static readonly BindingId textProperty = nameof(text);
        internal static readonly BindingId messageTypeProperty = nameof(messageType);
        internal static readonly BindingId buttonTextProperty = nameof(buttonText);
        internal static readonly BindingId linkTextProperty = nameof(linkText);
        internal static readonly BindingId linkHrefProperty = nameof(linkHref);

        /// <summary>
        /// The USS class name for Elements of this type.
        /// </summary>
        public static readonly string ussClassName = "unity-help-box";
        /// <summary>
        /// The USS class name for the top content container of this Elements type.
        /// </summary>
        public static readonly string topContainerUssClassName = ussClassName + "__top-container";
        /// <summary>
        /// The USS class name for the bottom content container of this Elements type.
        /// </summary>
        public static readonly string bottomContainerUssClassName = ussClassName + "__bottom-container";
        /// <summary>
        /// The USS class name for the action link for this Elements type.
        /// </summary>
        public static readonly string linkUssClassName = ussClassName + "__link";
        /// <summary>
        /// The USS class name for the action button of this Elements type.
        /// </summary>
        public static readonly string buttonUssClassName = ussClassName + "__button";
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
        [Obsolete("Renamed to match the other style class names. Please use iconWarningUssClassName instead (UnityUpgradable) -> iconwarningUssClassName", false)]
        public static readonly string iconwarningUssClassName = iconWarningUssClassName;
        /// <summary>
        /// The USS class name for the <see cref="HelpBoxMessageType.Warning"/> state in Elements of this type.
        /// </summary>
        public static readonly string iconWarningUssClassName = iconUssClassName + "--warning";
        /// <summary>
        /// The USS class name for the <see cref="HelpBoxMessageType.Error"/> state in Elements of this type.
        /// </summary>
        public static readonly string iconErrorUssClassName = iconUssClassName + "--error";

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new (nameof(text), "text"),
                    new (nameof(messageType), "message-type"),
                    new (nameof(buttonText), "button-text"),
                    new (nameof(linkText), "link-text"),
                    new (nameof(linkHref), "link-href"),
                }, false);
            }

            #pragma warning disable 649
            [SerializeField, MultilineTextField] string text;
            [SerializeField, MultilineTextField] string buttonText;
            [SerializeField, MultilineTextField] string linkText;
            [SerializeField] string linkHref;
            [SerializeField] HelpBoxMessageType messageType;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags text_UxmlAttributeFlags;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags buttonText_UxmlAttributeFlags;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags linkText_UxmlAttributeFlags;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags linkHref_UxmlAttributeFlags;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags messageType_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new HelpBox();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (HelpBox)obj;
                if (ShouldWriteAttributeValue(text_UxmlAttributeFlags))
                    e.text = text;
                if (ShouldWriteAttributeValue(messageType_UxmlAttributeFlags))
                    e.messageType = messageType;
                if (ShouldWriteAttributeValue(buttonText_UxmlAttributeFlags))
                    e.buttonText = buttonText;
                if (ShouldWriteAttributeValue(linkText_UxmlAttributeFlags))
                    e.linkText = linkText;
                if (ShouldWriteAttributeValue(linkHref_UxmlAttributeFlags))
                    e.linkHref = linkHref;
            }
        }

        // This class is used to push the label down a few pixels when an icon is present.
        static readonly string k_LabelWithIconClassName = labelUssClassName + "--with-icon";

        HelpBoxMessageType m_HelpBoxMessageType;
        VisualElement m_Icon;
        VisualElement m_TopContainer;
        VisualElement m_BottomContainer;
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

        Button m_CallToActionButton;
        string m_ButtonText = string.Empty;

        /// <summary>
        /// The action button's text value.
        /// </summary>
        /// <remarks>
        /// Provide a value to create a new <see cref="Button"/> or update the text of an existing one.
        /// </remarks>
        [CreateProperty]
        public string buttonText
        {
            get => m_ButtonText;
            set
            {
                if (string.CompareOrdinal(m_ButtonText, value) == 0)
                {
                    return;
                }

                m_ButtonText = value;

                if (string.IsNullOrEmpty(value))
                {
                    m_CallToActionButton?.RemoveFromHierarchy();
                    m_CallToActionButton = null;
                }
                else
                {
                    if (m_CallToActionButton == null)
                    {
                        CreateAndInsertButtonToBottomContainer(value);
                    }
                    else
                    {
                        m_CallToActionButton.text = value;
                    }
                }

                NotifyPropertyChanged(buttonTextProperty);
            }
        }

        /// <summary>
        /// Callback triggered when the action button is clicked.
        /// </summary>
        /// <remarks>
        /// Provide a callback to automatically create a <see cref="Button"/> if none exists, or update the button's <c>Clickable.clicked</c> callback.
        /// </remarks>
        public event Action onButtonClicked
        {
            add
            {
                if (m_CallToActionButton == null)
                {
                    CreateAndInsertButtonToBottomContainer();
                }

                m_CallToActionButton.clicked += value;
            }
            remove
            {
                if (m_CallToActionButton != null)
                {
                    m_CallToActionButton.clicked -= value;
                }
            }
        }

        Label m_CallToActionLink;
        string m_LinkText = string.Empty;

        /// <summary>
        /// The hyperlink's text value.
        /// </summary>
        /// <remarks>
        /// Provide a value to create a <see cref="Label"/> if none exists. This property represents the readable string of the hyperlink. If a <c>HelpBox.linkHref</c>
        /// is provided, it renders as a hyperlink using rich text tags; otherwise, it displays as plain text. This property does not return the hyperlink rich text tag.
        /// </remarks>
        [CreateProperty]
        public string linkText
        {
            get => m_LinkText;
            set
            {
                if (string.CompareOrdinal(m_LinkText, value) == 0)
                {
                    return;
                }

                m_LinkText = value;
                UpdateLink();
                NotifyPropertyChanged(linkTextProperty);
            }
        }

        string m_LinkHref = string.Empty;

        /// <summary>
        /// The hyperlink's reference value.
        /// </summary>
        /// <remarks>
        /// Provide a value to create a <see cref="Label"/> element with a hyperlink rich text tag as its value if none exists. The value is used as the hyperlink
        /// reference value. If no <c>HelpBox.linkText</c> is provided, this value is used as the hyperlink's text. This property does not return the hyperlink rich text tag.
        /// </remarks>
        [CreateProperty]
        public string linkHref
        {
            get => m_LinkHref;
            set
            {
                if (string.CompareOrdinal(linkHref, value) == 0)
                {
                    return;
                }

                m_LinkHref = value;
                UpdateLink();
                NotifyPropertyChanged(linkHrefProperty);
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

            m_TopContainer = new VisualElement();
            m_TopContainer.AddToClassList(topContainerUssClassName);

            m_Label = new Label(text);
            m_Label.AddToClassList(labelUssClassName);
            m_Label.selection.isSelectable = true;
            m_TopContainer.Add(m_Label);

            m_Icon = new VisualElement();
            m_Icon.AddToClassList(iconUssClassName);
            UpdateIcon(messageType);

            m_BottomContainer = new VisualElement();
            m_BottomContainer.AddToClassList(bottomContainerUssClassName);

            Add(m_TopContainer);
            Add(m_BottomContainer);
        }

        string GetIconClass(HelpBoxMessageType messageType)
        {
            switch (messageType)
            {
                case HelpBoxMessageType.Info:    return iconInfoUssClassName;
                case HelpBoxMessageType.Warning: return iconWarningUssClassName;
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
                m_Label.RemoveFromClassList(k_LabelWithIconClassName);
            }
            else
            {
                m_Label.AddToClassList(k_LabelWithIconClassName);
                m_Icon.AddToClassList(m_IconClass);
                if (m_Icon.parent == null)
                {
                    m_TopContainer.Insert(0, m_Icon);
                }
            }
        }

        void UpdateLink()
        {
            if (string.IsNullOrEmpty(linkText) && string.IsNullOrEmpty(linkHref))
            {
                m_CallToActionLink?.RemoveFromHierarchy();
                m_CallToActionLink = null;
                return;
            }

            if (m_CallToActionLink == null)
            {
                m_CallToActionLink = new Label();
                m_CallToActionLink.AddToClassList(linkUssClassName);
                m_BottomContainer.Add(m_CallToActionLink);
            }

            // Plain text if there is no href provided
            if (string.IsNullOrEmpty(linkHref))
            {
                m_CallToActionLink.text = linkText;
                return;
            }

            // If no text is provided, we use the href as the text
            if (string.IsNullOrEmpty(linkText))
            {
                m_CallToActionLink.text = $"<a href={linkHref}>{linkHref}</a>";
                return;
            }

            m_CallToActionLink.text = $"<a href={linkHref}>{linkText}</a>";
        }

        void CreateAndInsertButtonToBottomContainer(string labelText = "Action Button")
        {
            m_CallToActionButton = new Button() { text = labelText };
            m_CallToActionButton.AddToClassList(buttonUssClassName);
            m_BottomContainer.Insert(0, m_CallToActionButton);
        }
    }
}
