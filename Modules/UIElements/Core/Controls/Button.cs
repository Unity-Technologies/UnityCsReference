// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Properties;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// This is a clickable button.
    /// </summary>
    /// <remarks>
    /// A Button has a text label element that can respond to pointer and mouse events.
    /// You can add an icon to pair it with the text by assigning a Background (Texture, RenderTexture, Sprite or Vector Image)
    /// to the iconImage API or icon-image UXML property. Please note that by providing an icon image, this will
    /// automatically update the Button's hierarchy to contain an <see cref="Image"/> and a text label element.
    ///
    /// By default, a single left mouse click activates the Button's <see cref="Clickable"/> property button.
    /// To remove this activator, or add more activators, modify the <c>clickable.activators</c> property.
    /// For details, see <see cref="ManipulatorActivationFilter"/>.
    ///
    /// To bind a Button's text value to the contents of a variable, set the <c>binding-path</c> property in the
    /// UXML file, or the <c>bindingPath</c> property in the C# code, to a string that contains the variable name.
    ///
    /// For more information, refer to [[wiki:UIE-uxml-element-Button|UXML element Button]].
    /// </remarks>
    public class Button : TextElement
    {
        internal static readonly BindingId iconImageProperty = nameof(iconImage);

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : TextElement.UxmlSerializedData
        {
            #pragma warning disable 649
            [ImageFieldValueDecorator]
            [SerializeField, UxmlAttribute("icon-image"), UxmlAttributeBindingPath(nameof(iconImage))] Object iconImageReference;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags iconImageReference_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new Button();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                if (ShouldWriteAttributeValue(iconImageReference_UxmlAttributeFlags))
                {
                    var e = (Button)obj;
                    e.iconImageReference = iconImageReference;
                }
            }
        }

        /// <summary>
        /// Instantiates a <see cref="Button"/> using data from a UXML file.
        /// </summary>
        /// <remarks>
        /// This class is added to every <see cref="VisualElement"/> that is created from UXML.
        /// </remarks>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlFactory : UxmlFactory<Button, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Button"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the properties of a Button element that you can
        /// use in a UXML asset.
        /// </remarks>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlTraits : TextElement.UxmlTraits
        {
            private readonly UxmlImageAttributeDescription m_IconImage = new() { name = "icon-image" };

            /// <summary>
            /// Constructor.
            /// </summary>
            public UxmlTraits()
            {
                focusable.defaultValue = true;
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var button = (Button)ve;
                button.iconImage = m_IconImage.GetValueFromBag(bag, cc);
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every instance of the Button element. Any styling applied to
        /// this class affects every button located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public new static readonly string ussClassName = "unity-button";

        /// <summary>
        /// The USS class name for Button elements with an icon.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to an instance of the Button element if the instance's
        /// <see cref="Button.iconImage"/> property contains a valid Texture. Any styling applied to this class
        /// affects every button with an icon located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string iconUssClassName = ussClassName + "--with-icon";

        /// <summary>
        /// The USS class name for Button elements with an icon only, no text.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to an instance of the Button element if the instance's
        /// <see cref="Button.iconImage"/> property contains a valid Texture and no text is set. Any styling applied to
        /// this class affects every button with an icon located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string iconOnlyUssClassName = ussClassName + "--with-icon-only";

        /// <summary>
        /// The USS class name of the image element that will be used to display the icon texture.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to an instance of the Image element that will be used to display the
        /// <see cref="Button.iconImage"/> property value. Any styling applied to this class will affect
        /// image elements inside a Button that contains this class.
        /// </remarks>
        public static readonly string imageUSSClassName = ussClassName + "__image";

        private Clickable m_Clickable;

        /// <summary>
        /// Clickable MouseManipulator for this Button.
        /// </summary>
        /// <remarks>
        /// The default <see cref="Clickable"/> object provides a list of actions that are called using
        /// one or more activation filters.
        ///\\
        ///\\
        /// To add or remove activation triggers, modify [[MouseManipulator.activators|clickable.activators]].
        /// An activation trigger can be any mouse button, pressed any number of times, with any modifier key.
        /// </remarks>
        /// <example>
        /// <code lang="cs">
        /// myButton.clickable.activators.Add(new ManipulatorActivationFilter(...))
        /// </code>
        /// </example>
        /// <example>
        /// <code lang="cs">myButton.clickable.activators.Clear()</code>
        /// </example>
        /// <remarks>
        /// SA: [[ManipulatorActivationFilter]]
        /// </remarks>
        public Clickable clickable
        {
            get
            {
                return m_Clickable;
            }
            set
            {
                if (m_Clickable != null && m_Clickable.target == this)
                {
                    this.RemoveManipulator(m_Clickable);
                }

                m_Clickable = value;

                if (m_Clickable != null)
                {
                    this.AddManipulator(m_Clickable);
                }
            }
        }

        /// <summary>
        /// Obsolete. Use <see cref="clicked"/> instead.
        /// </summary>
        [Obsolete("onClick is obsolete. Use clicked instead (UnityUpgradable) -> clicked", true)]
        public event Action onClick
        {
            add
            {
                clicked += value;
            }
            remove
            {
                clicked -= value;
            }
        }

        /// <summary>
        /// Callback triggered when the button is clicked.
        /// </summary>
        /// <remarks>
        /// This is a shortcut for modifying <seealso cref="Clickable.clicked"/>. It is provided as a convenience. When you add or remove actions from clicked, it adds or removes them from <c>Clickable.clicked</c> automatically.
        /// </remarks>
        /// <example>
        /// The following example shows how to use the clicked event to print a message to the console when the button is clicked.
        /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/Button_clicked.cs"/>
        /// </example>
        public event Action clicked
        {
            add
            {
                if (m_Clickable == null)
                {
                    clickable = new Clickable(value);
                }
                else
                {
                    m_Clickable.clicked += value;
                }
            }
            remove
            {
                if (m_Clickable != null)
                {
                    m_Clickable.clicked -= value;
                }
            }
        }

        // Used privately to help the serializer convert the Unity Object to the appropriate asset type.
        Object iconImageReference
        {
            get => iconImage.GetSelectedImage();
            set => iconImage = Background.FromObject(value);
        }

        // The element designed to hold the Button's text when an icon is preset (sibling of image).
        TextElement m_TextElement;

        // The element that will hold and render the icon within the button (sibling of text element).
        Image m_ImageElement;

        // Holds the corresponding icon value of said type (Texture, Sprite, VectorImage).
        Background m_IconImage;

        /// <summary>
        /// The Texture, Sprite, or VectorImage that will represent an icon within a Button element.
        /// </summary>
        [CreateProperty]
        public Background iconImage
        {
            get => m_IconImage;
            set
            {
                if (value.IsEmpty() && m_ImageElement == null || value == m_IconImage)
                    return;

                if (value.IsEmpty())
                {
                    m_IconImage = value;
                    ResetButtonHierarchy();
                    NotifyPropertyChanged(iconImageProperty);

                    return;
                }

                if (m_ImageElement == null)
                    UpdateButtonHierarchy();

                // The image control will reset the other values to null
                if (value.texture)
                    m_ImageElement.image = value.texture;
                else if (value.sprite)
                    m_ImageElement.sprite = value.sprite;
                else if (value.renderTexture)
                    m_ImageElement.image = value.renderTexture;
                else
                    m_ImageElement.vectorImage = value.vectorImage;

                m_IconImage = value;
                EnableInClassList(iconOnlyUssClassName, string.IsNullOrEmpty(text));
                NotifyPropertyChanged(iconImageProperty);
            }
        }

        private string m_Text = String.Empty;
        public override string text
        {
            get => m_Text ?? string.Empty;
            set
            {
                m_Text = value;
                EnableInClassList(iconOnlyUssClassName, !m_IconImage.IsEmpty() && string.IsNullOrEmpty(text));

                if (m_TextElement != null)
                {
                    // Make sure we clear the Button's text, otherwise it will show the same string twice
                    base.text = String.Empty;

                    if (m_TextElement.text == m_Text)
                        return;

                    m_TextElement.text = m_Text;
                    return;
                }

                if (base.text == m_Text)
                    return;

                base.text = m_Text;
            }
        }

        /// <summary>
        /// Constructs a Button.
        /// </summary>
        public Button() : this(default, null)
        {
        }

        /// <summary>
        /// Constructs a button with a <see cref="Background"/> and an Action. The image definition will be used
        /// to represent an icon while the Action is triggered when the button is clicked.
        /// </summary>
        /// <param name="iconImage">The image value that will be rendered as an icon.</param>
        /// <param name="clickEvent">The action triggered when the button is clicked.</param>
        /// <remarks>Action is the standard C# System.Action.</remarks>
        public Button(Background iconImage, Action clickEvent = null) : this(clickEvent)
        {
            this.iconImage = iconImage;
        }

        /// <summary>
        /// Constructs a button with an Action that is triggered when the button is clicked.
        /// </summary>
        /// <param name="clickEvent">The action triggered when the button is clicked.</param>
        /// <remarks>
        /// Action is the standard C# System.Action.
        /// By default, a single left mouse click triggers the Action. To change the activator, modify <see cref="clickable"/>.
        /// </remarks>
        public Button(Action clickEvent)
        {
            AddToClassList(ussClassName);

            // Click-once behaviour
            clickable = new Clickable(clickEvent);
            focusable = true;
            tabIndex = 0;

            RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);
        }

        private void OnNavigationSubmit(NavigationSubmitEvent evt)
        {
            clickable?.SimulateSingleClick(evt);
            evt.StopPropagation();
        }

        private static readonly string NonEmptyString = " ";
        protected internal override Vector2 DoMeasure(float desiredWidth, MeasureMode widthMode, float desiredHeight,
            MeasureMode heightMode)
        {
            var textToMeasure = text;
            if (string.IsNullOrEmpty(textToMeasure))
            {
                textToMeasure = NonEmptyString;
            }
            return MeasureTextSize(textToMeasure, desiredWidth, widthMode, desiredHeight, heightMode);
        }

        private void UpdateButtonHierarchy()
        {
            if (m_ImageElement == null)
            {
                m_ImageElement = new Image { classList = { imageUSSClassName } };
                Add(m_ImageElement);
                AddToClassList(iconUssClassName);
            }

            if (m_TextElement == null)
            {
                m_TextElement = new TextElement {text = text};
                m_Text = text;
                base.text = String.Empty;
                Add(m_TextElement);
            }
        }

        private void ResetButtonHierarchy()
        {
            if (m_ImageElement != null)
            {
                m_ImageElement.RemoveFromHierarchy();
                m_ImageElement = null;
                RemoveFromClassList(iconUssClassName);
                RemoveFromClassList(iconOnlyUssClassName);
            }

            if (m_TextElement != null)
            {
                var restoredText = m_TextElement.text;
                m_TextElement.RemoveFromHierarchy();
                m_TextElement = null;
                text = restoredText;
            }
        }
    }
}
