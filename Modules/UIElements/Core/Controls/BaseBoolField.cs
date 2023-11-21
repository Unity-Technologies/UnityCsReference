// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A BaseBoolField is a clickable element that represents a boolean value.
    /// </summary>
    public abstract class BaseBoolField : BaseField<bool>
    {
        internal static readonly BindingId textProperty = nameof(text);

        protected Label m_Label;
        protected readonly VisualElement m_CheckMark;

        internal Clickable m_Clickable;

        // Needed by the UIBuilder for authoring in the viewport
        internal Label boolFieldLabelElement => m_Label;

        /// <summary>
        /// Creates a <see cref="BaseBoolField"/> with a Label and a default manipulator.
        /// </summary>
        /// <remarks>
        /// The default manipulator makes it possible to activate the BaseBoolField with a left mouse click.
        /// </remarks>
        /// <param name="label">The Label text.</param>
        public BaseBoolField(string label)
            : base(label, null)
        {
            // Allocate and add the checkmark to the hierarchy
            m_CheckMark = new VisualElement() { name = "unity-checkmark", pickingMode = PickingMode.Ignore };
            visualInput.Add(m_CheckMark);

            // The picking mode needs to be Position in order to have the Pseudostate Hover applied...
            visualInput.pickingMode = PickingMode.Position;

            // Set-up the label and text...
            text = null;
            this.AddManipulator(m_Clickable = new Clickable(OnClickEvent));

            RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);
        }

        private void OnNavigationSubmit(NavigationSubmitEvent evt)
        {
            ToggleValue();
            evt.StopPropagation();
        }

        private string m_OriginalText;

        /// <summary>
        /// Optional text that appears after the BaseBoolField.
        /// </summary>
        /// <remarks>
        /// Unity creates a <see cref="Label"/> automatically if one does not exist.
        /// </remarks>
        [CreateProperty]
        public string text
        {
            get { return m_Label?.text; }
            set
            {
                if (string.CompareOrdinal(m_Label?.text, value) == 0)
                    return;

                if (!string.IsNullOrEmpty(value))
                {
                    // Lazy allocation of label if needed...
                    if (m_Label == null)
                    {
                        InitLabel();
                    }

                    m_Label.text = value;
                }
                else if (m_Label != null)
                {
                    m_Label.RemoveFromHierarchy();
                    m_Label = null;
                }
                NotifyPropertyChanged(textProperty);
            }
        }

        /// <summary>
        /// Initializes the Label element whenever the <see cref="BaseBoolField.text"/> property changes.
        /// </summary>
        /// <remarks>
        /// Override this method to modify the Label after its creation.
        /// You must call the base implementation of this method before trying to access to <see cref="m_Label"/> property in your own implementation.
        /// </remarks>
        protected virtual void InitLabel()
        {
            m_Label = new Label();

            if (m_CheckMark.hierarchy.parent != visualInput)
            {
                visualInput.Add(m_Label);
            }
            else
            {
                var checkmarkIndex = visualInput.IndexOf(m_CheckMark);
                visualInput.Insert(checkmarkIndex + 1, m_Label);
            }
        }

        /// <summary>
        /// Sets the value of the <see cref="BaseBoolField"/>, but does not notify the rest of the hierarchy of the change.
        /// </summary>
        /// <remarks>
        /// This method is useful when you want to change the control's value without triggering events. For example, you can use it when you initialize UI
        /// to avoid triggering unnecessary events, and to prevent situations where changing the value of one control triggers an event that tries to update
        /// another control that hasn't been initialized, and may not exist yet.
        ///
        /// This method is also useful for preventing circular updates. Let's say you link two controls so they always have the same value. When a user changes
        /// the value of the first control, it fires an event to update the value of the second control. If you update the second control's value "normally,"
        /// as though a user changed it, it will fire another event to update the first control's value, which will fire an event to update the second control's
        /// value again, and so on. If one control update's the other's value using SetValueWithoutNotify, the update does not trigger an event, which prevents
        /// the circular update loop.
        /// </remarks>
        /// <param name="newValue"></param>
        public override void SetValueWithoutNotify(bool newValue)
        {
            if (newValue)
            {
                visualInput.pseudoStates |= PseudoStates.Checked;
                pseudoStates |= PseudoStates.Checked;
            }
            else
            {
                visualInput.pseudoStates &= ~PseudoStates.Checked;
                pseudoStates &= ~PseudoStates.Checked;
            }
            base.SetValueWithoutNotify(newValue);
        }

        void OnClickEvent(EventBase evt)
        {
            if (evt.eventTypeId == MouseUpEvent.TypeId())
            {
                var ce = (IMouseEvent)evt;
                if (ce.button == (int)MouseButton.LeftMouse)
                {
                    ToggleValue();
                }
            }
            else if (evt.eventTypeId == PointerUpEvent.TypeId() || evt.eventTypeId == ClickEvent.TypeId())
            {
                var ce = (IPointerEvent)evt;
                if (ce.button == (int)MouseButton.LeftMouse)
                {
                    ToggleValue();
                }
            }
        }

        /// <summary>
        /// Inverts the <see cref="BaseBoolField.value"/> property.
        /// </summary>
        /// <remarks>
        /// Override this method to change the logic of toggling the value in your subclass.
        /// </remarks>
        protected virtual void ToggleValue()
        {
            value = !value;
        }

        protected override void UpdateMixedValueContent()
        {
            if (showMixedValue)
            {
                visualInput.pseudoStates &= ~PseudoStates.Checked;
                pseudoStates &= ~PseudoStates.Checked;

                m_CheckMark.RemoveFromHierarchy();
                visualInput.Add(mixedValueLabel);
                m_OriginalText = text;
                text = "";
            }
            else
            {
                mixedValueLabel.RemoveFromHierarchy();
                visualInput.Add(m_CheckMark);
                if (m_OriginalText != null)
                    text = m_OriginalText;
            }
        }

        internal override void RegisterEditingCallbacks()
        {
            RegisterCallback<PointerUpEvent>(StartEditing);
            RegisterCallback<FocusOutEvent>(EndEditing);
        }

        internal override void UnregisterEditingCallbacks()
        {
            RegisterCallback<PointerUpEvent>(StartEditing);
            RegisterCallback<FocusOutEvent>(EndEditing);
        }

    }
}
