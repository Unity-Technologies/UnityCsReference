using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A button that executes an action repeatedly while it is pressed.
    /// </summary>
    public class RepeatButton : TextElement
    {
        /// <summary>
        /// Instantiates a <see cref="RepeatButton"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<RepeatButton, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="RepeatButton"/>.
        /// </summary>
        public new class UxmlTraits : TextElement.UxmlTraits
        {
            UxmlLongAttributeDescription m_Delay = new UxmlLongAttributeDescription { name = "delay" };
            UxmlLongAttributeDescription m_Interval = new UxmlLongAttributeDescription { name = "interval" };

            /// <summary>
            /// Initialize <see cref="RepeatButton"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            /// <param name="cc">The creation context; unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                RepeatButton repeatButton = (RepeatButton)ve;
                repeatButton.SetAction(null, m_Delay.GetValueFromBag(bag, cc), m_Interval.GetValueFromBag(bag, cc));
            }
        }

        private PointerClickable m_Clickable;

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-repeat-button";

        /// <summary>
        /// Constructor.
        /// </summary>
        public RepeatButton()
        {
            AddToClassList(ussClassName);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="clickEvent">The action to execute when the button is pressed.</param>
        /// <param name="delay">The initial delay before the action is executed for the first time.</param>
        /// <param name="interval">The interval between each execution of the action.</param>
        public RepeatButton(System.Action clickEvent, long delay, long interval) : this()
        {
            SetAction(clickEvent, delay, interval);
        }

        /// <summary>
        /// Set the action that should be executed when the button is pressed.
        /// </summary>
        /// <param name="clickEvent">The action to execute.</param>
        /// <param name="delay">The initial delay before the action is executed for the first time.</param>
        /// <param name="interval">The interval between each execution of the action.</param>
        public void SetAction(System.Action clickEvent, long delay, long interval)
        {
            this.RemoveManipulator(m_Clickable);
            m_Clickable = new PointerClickable(clickEvent, delay, interval);
            this.AddManipulator(m_Clickable);
        }
    }
}
