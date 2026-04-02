// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A button that executes an action repeatedly while it is pressed. For more information, refer to [[wiki:UIE-uxml-element-RepeatButton|UXML element RepeatButton]].
    /// </summary>
    public partial class RepeatButton : TextElement
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : TextElement.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new (nameof(delay), "delay"),
                    new (nameof(interval), "interval"),
                }, false);
            }

            #pragma warning disable 649
            [SerializeField] long delay;
            [SerializeField] long interval;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags delay_UxmlAttributeFlags;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags interval_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new RepeatButton();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                if (ShouldWriteAttributeValue(delay_UxmlAttributeFlags) || ShouldWriteAttributeValue(interval_UxmlAttributeFlags))
                {
                    var e = (RepeatButton)obj;
                    e.SetAction(null, delay, interval);
                }
            }
        }

        private Clickable m_Clickable;

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
