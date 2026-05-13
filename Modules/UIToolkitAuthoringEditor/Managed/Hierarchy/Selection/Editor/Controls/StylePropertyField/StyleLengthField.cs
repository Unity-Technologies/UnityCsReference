// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Makes a style field for editing a StyleLength.
    /// </summary>
    [UxmlElement]
    internal partial class StyleLengthField : StylePropertyField<StyleLength, LengthField, Length>
    {
        public static readonly BindingId showUnitAsDropdownProperty = nameof(showUnitAsDropdown);

        [UxmlAttribute, CreateProperty]
        public bool showUnitAsDropdown
        {
            get => valueField.showUnitAsDropdown;
            set => valueField.showUnitAsDropdown = value;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public StyleLengthField() : this(null) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        /// <param name="containsAffordance">Whether the style property field is preceded by an affordance.</param>
        public StyleLengthField(string label, bool containsAffordance = true) : base(label, new LengthField(), containsAffordance)
        {
            valueField.showUnitAsDropdown = true;
            valueField.RegisterCallback<PropertyChangedEvent, StyleLengthField>(PropagateEvents, this);
        }

        [EventInterest(typeof(AttachToPanelEvent))]
        protected override void HandleEventBubbleUp(EventBase evt)
        {
            base.HandleEventBubbleUp(evt);

            if (evt.eventTypeId != AttachToPanelEvent.TypeId())
                return;

            UpdateValidation();
        }

        internal override bool EqualsCurrentValue(StyleLength v)
        {
            return value == v;
        }

        public override void UpdateValidation()
        {
            valueField.SetValidation(GetValidation());
        }

        protected override bool Validate(Length previousValue, Length newValue)
        {
            return valueField.Validate(previousValue, newValue);
        }

        protected override LengthField CreateValueField()
        {
            return new LengthField();
        }

        protected override StyleLength CreateStyleValue(Length v)
        {
            return v;
        }

        private static void PropagateEvents(PropertyChangedEvent evt, StyleLengthField field)
        {
            if (evt.property == LengthField.showUnitAsDropdownProperty)
                field.NotifyPropertyChanged(showUnitAsDropdownProperty);
        }
    }
}
