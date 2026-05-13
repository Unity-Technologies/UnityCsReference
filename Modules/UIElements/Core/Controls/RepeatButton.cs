// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A button that executes an action repeatedly while it is pressed. For more information, refer to [[wiki:UIE-uxml-element-RepeatButton|UXML element RepeatButton]].
    /// </summary>
    [UxmlElement]
    public partial class RepeatButton : TextElement
    {
        private Clickable m_Clickable;

        long m_Delay;

        [UxmlAttribute]
        internal long delay
        {
            get => m_Delay;
            set
            {
                if (m_Delay == value)
                    return;
                m_Delay = value;
                SetAction(null, m_Delay, m_Interval);
            }
        }

        long m_Interval;

        [UxmlAttribute]
        internal long interval
        {
            get => m_Interval;
            set
            {
                if (m_Interval == value)
                    return;
                m_Interval = value;
                SetAction(null, m_Delay, m_Interval);
            }
        }

        private bool m_AcceptClicksIfDisabled;

        internal bool acceptClicksIfDisabled
        {
            get => m_AcceptClicksIfDisabled;
            set
            {
                if (m_AcceptClicksIfDisabled == value)
                    return;

                m_AcceptClicksIfDisabled = value;

                if (m_Clickable != null)
                    m_Clickable.acceptClicksIfDisabled = value;
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-repeat-button";
        internal new static readonly UniqueStyleString ussClassNameUnique = new(ussClassName);

        /// <summary>
        /// Constructor.
        /// </summary>
        public RepeatButton()
        {
            AddToClassList(ussClassNameUnique);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="clickEvent">The action to execute when the button is pressed.</param>
        /// <param name="delay">The initial delay before the action is executed for the first time. Value is defined in milliseconds.</param>
        /// <param name="interval">The interval between each execution of the action. Value is defined in milliseconds.</param>
        public RepeatButton(System.Action clickEvent, long delay, long interval) : this()
        {
            SetAction(clickEvent, delay, interval);
        }

        /// <summary>
        /// Set the action that should be executed when the button is pressed.
        /// </summary>
        /// <param name="clickEvent">The action to execute.</param>
        /// <param name="delay">The initial delay before the action is executed for the first time. Value is defined in milliseconds.</param>
        /// <param name="interval">The interval between each execution of the action. Value is defined in milliseconds.</param>
        public void SetAction(System.Action clickEvent, long delay, long interval)
        {
            this.RemoveManipulator(m_Clickable);
            m_Clickable = new Clickable(clickEvent, delay, interval);
            this.AddManipulator(m_Clickable);
        }

        internal void AddAction(Action clickEvent)
        {
            m_Clickable.clicked += clickEvent;
        }
    }
}
