using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A clickable button.
    /// </summary>
    public class Button : TextElement
    {
        /// <summary>
        /// Instantiates a <see cref="Button"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<Button, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Button"/>.
        /// </summary>
        public new class UxmlTraits : TextElement.UxmlTraits {}

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-button";
        private Clickable m_Clickable;

        /// <summary>
        /// Clickable MouseManipulator for this Button.
        /// </summary>
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
        /// <seealso cref="Clickable.clicked"/>
        public event Action clicked
        {
            add
            {
                if (m_Clickable == null)
                {
                    clickable = new PointerClickable(value);
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
        /// Constructs a Button.
        /// </summary>
        /// <param name="clickEvent">Action triggered when the button is clicked.</param>
        public Button(System.Action clickEvent)
        {
            AddToClassList(ussClassName);

            // Click-once behaviour
            clickable = new PointerClickable(clickEvent);
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
