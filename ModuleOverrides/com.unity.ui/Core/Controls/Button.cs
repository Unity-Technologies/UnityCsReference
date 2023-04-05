// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// This is a clickable button.
    /// </summary>
    /// <remarks>
    /// A Button has a text label element that can respond to pointer and mouse events.
    /// You can customize a button by adding child elements to its hierarchy.
    /// For example, to use a separate image as an icon for the button, you can add an <see cref="Image"/>
    /// element as a child of the button.
    ///
    /// By default, a single left mouse click activates the Button's <see cref="Clickable"/> property button.
    /// To remove this activator, or add more activators, modify the <c>clickable.activators</c> property.
    /// For details, see <see cref="ManipulatorActivationFilter"/>.
    ///
    /// To bind a Button's text value to the contents of a variable, set the <c>binding-path</c> property in the
    /// UXML file, or the <c>bindingPath</c> property in the C# code, to a string that contains the variable name.
    /// </remarks>
    public class Button : TextElement
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : TextElement.UxmlSerializedData
        {
            public override object CreateInstance() => new Button();
        }

        /// <summary>
        /// Instantiates a <see cref="Button"/> using data from a UXML file.
        /// </summary>
        /// <remarks>
        /// This class is added to every <see cref="VisualElement"/> that is created from UXML.
        /// </remarks>
        public new class UxmlFactory : UxmlFactory<Button, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Button"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the properties of a Button element that you can
        /// use in a UXML asset.
        /// </remarks>
        public new class UxmlTraits : TextElement.UxmlTraits
        {
            /// <summary>
            /// Constructor.
            /// </summary>
            public UxmlTraits()
            {
                focusable.defaultValue = true;
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
        private Clickable m_Clickable;

        /// <summary>
        /// Clickable MouseManipulator for this Button.
        /// </summary>
        /// <remarks>
        /// <example>
        /// The default <see cref="Clickable"/> object provides a list of actions that are called using
        /// one or more activation filters.
        ///
        /// To add or remove activation triggers, modify <see cref="Clickable.activators"/>.
        /// An activation trigger can be any mouse button, pressed any number of times, with any modifier key.
        /// For details, see <see cref="ManipulatorActivationFilter"/>.
        /// <code>clickable.activators.Add(new ManipulatorActivationFilter(...))</code>
        /// or
        /// <code>clickable.activators.Clear()</code>
        /// </example>
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
        /// Obsolete. Use <see cref="Button.clicked"/> instead.
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

        /// <summary>
        /// Constructs a Button.
        /// </summary>
        public Button() : this(null)
        {
        }

        /// <summary>
        /// Constructs a button with an Action that is triggered when the button is clicked.
        /// </summary>
        /// <param name="clickEvent">The action triggered when the button is clicked.</param>
        /// <remarks>
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
    }
}
