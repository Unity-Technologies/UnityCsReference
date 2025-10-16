// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Makes a style field for editing a StyleLength.
    /// </summary>
    internal class StyleLengthField : StylePropertyField<StyleLength, LengthField, Length>
    {
        public static readonly BindingId showUnitAsDropdownProperty = nameof(showUnitAsDropdown);

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StylePropertyField<StyleLength, LengthField, Length>.UxmlSerializedData
        {
            #pragma warning disable 649
            [SerializeField] bool showUnitAsDropdown;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags showUnitAsDropdown_UxmlAttributeFlags;
            #pragma warning restore 649

            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StylePropertyField<StyleLength, LengthField, Length>.UxmlSerializedData.Register();
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new (nameof(showUnitAsDropdown), "show-unit-as-dropdown"),
                }, true);
            }

            public override object CreateInstance() => new StyleLengthField();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (StyleLengthField)obj;
                if (ShouldWriteAttributeValue(showUnitAsDropdown_UxmlAttributeFlags))
                    e.showUnitAsDropdown = showUnitAsDropdown;
            }
        }

        [CreateProperty]
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

        public override void UpdateValidation()
        {
            valueField.SetValidation(GetValidation());
        }

        protected override bool Validate(Length previousValue, Length newValue)
        {
            return valueField.Validate(previousValue, newValue);
        }

        private static void PropagateEvents(PropertyChangedEvent evt, StyleLengthField field)
        {
            if (evt.property == LengthField.showUnitAsDropdownProperty)
                field.NotifyPropertyChanged(showUnitAsDropdownProperty);
        }
    }
}
