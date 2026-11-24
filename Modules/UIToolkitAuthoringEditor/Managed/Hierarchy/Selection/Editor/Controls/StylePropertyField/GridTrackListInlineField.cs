// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor
{
    [UxmlElement]
    internal partial class GridTrackListInlineField : VisualElement, IPropertyMappedAffordanceField,
        IStylePropertyDataField<StyleList<GridTrackSize>, List<GridTrackSize>>,
        INotifyCompositeStylePropertyChanged<StyleList<GridTrackSize>>
    {
        public static readonly BindingId valueProperty = nameof(value);

        readonly GridTrackAxisEditor m_Axis;
        readonly FieldAffordanceElement m_Affordance;
        readonly OverrideBarManipulator m_OverrideBar;
        readonly HashSet<ITrackablePropertyProvider> m_TrackedProviders = new();

        List<GridTrackSize> m_Value = new();
        readonly List<GridTrackTemplateEntry> m_AuthoredTemplate = new();
        readonly List<GridTrackSize> m_FlattenedTemplate = new();
        Action<StyleProperty, StyleSheet, StyleList<GridTrackSize>> m_SetterOverride;

        const string k_UssPath = "UIToolkitAuthoring/Inspector/Controls/GridTrackList.uss";

        [UxmlAttribute]
        public string label
        {
            get => m_Axis.title;
            set => m_Axis.title = value;
        }

        // grid-auto-columns / -rows (implicit tracks) cannot use repeat() per the spec, so implicit fields
        // set this false to hide the per-row repeat selector.
        [UxmlAttribute]
        public bool allowRepeat
        {
            get => m_Axis.allowRepeat;
            set => m_Axis.allowRepeat = value;
        }

        public GridTrackListInlineField()
        {
            AddToClassList(ussClassName);

            if (EditorGUIUtility.Load(k_UssPath) is StyleSheet uss)
                styleSheets.Add(uss);

            m_Axis = new GridTrackAxisEditor(string.Empty); // title is set via the label attribute
            m_Axis.changed += OnAxisChanged;
            Add(m_Axis);

            m_Affordance = new FieldAffordanceElement();
            m_Axis.affordanceSlot.Add(m_Affordance);
            m_Axis.AddManipulator(new ContextualMenuManipulator(evt => m_Affordance.OnContextualMenuPopulate(evt)));

            m_OverrideBar = new OverrideBarManipulator();
            m_Axis.AddManipulator(m_OverrideBar);

            RegisterCallback<TrackPropertyEvent>(OnTrackProperty);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        [CreateProperty]
        public List<GridTrackSize> value
        {
            get => m_Value;
            set
            {
                if (SequenceEqual(m_Value, value))
                    return;
                m_Value = value != null ? new List<GridTrackSize>(value) : new List<GridTrackSize>();
                m_Axis.SetTracks(m_Value);
            }
        }

        // Only one property is bound to this field, so it owns the single affordance regardless of the id.
        public void GetAffordanceElements(StylePropertyId propertyId, List<FieldAffordanceElement> elements)
            => elements.Add(m_Affordance);

        void OnAxisChanged(List<GridTrackSize> list)
        {
            var previous = m_Value;
            m_Value = new List<GridTrackSize>(list);
            // A repeat row can change without changing the expanded list, so always notify.
            this.NotifyStylePropertyChanged(this, valueProperty, new StyleList<GridTrackSize>(previous), new StyleList<GridTrackSize>(m_Value));
            NotifyPropertyChanged(valueProperty);
        }

        Action<StyleProperty, StyleSheet, StyleList<GridTrackSize>> IStylePropertyDataField<StyleList<GridTrackSize>, List<GridTrackSize>>.setterOverride
            => m_SetterOverride ??= WriteTracks;

        // The generated setter only knows the expanded list; write the editor's rows so repeat(<count>, …) survives.
        void WriteTracks(StyleProperty property, StyleSheet sheet, StyleList<GridTrackSize> styleValue)
        {
            var template = m_Axis.GetTemplate();
            GridTrackTemplateEntry.Flatten(template, m_FlattenedTemplate);
            if (styleValue.keyword == StyleKeyword.Undefined && SequenceEqual(m_FlattenedTemplate, styleValue.value))
                property.SetGridTrackTemplate(sheet, template);
            else
                StylePropertyBinding.SetGridTrackSizeList(property, sheet, styleValue);
        }

        void IStylePropertyDataField<StyleList<GridTrackSize>, List<GridTrackSize>>.SetValueFromStyleData(
            in StylePropertyData<StyleList<GridTrackSize>, List<GridTrackSize>> data, VisualElement currentTarget, StyleSheet currentStyleSheet)
        {
            var computed = data.computedValue;
            m_Value = computed != null ? new List<GridTrackSize>(computed) : new List<GridTrackSize>();

            if (TryReadAuthoredTemplate(data, currentStyleSheet))
            {
                GridTrackTemplateEntry.Flatten(m_AuthoredTemplate, m_FlattenedTemplate);
                if (SequenceEqual(m_FlattenedTemplate, m_Value))
                {
                    m_Axis.SetTemplate(m_AuthoredTemplate);
                    return;
                }
            }
            m_Axis.SetTracks(m_Value.ToArray(), preserveRepeatRows: false);
        }

        // The rule being edited first, else the selector that supplies the computed value.
        bool TryReadAuthoredTemplate(in StylePropertyData<StyleList<GridTrackSize>, List<GridTrackSize>> data, StyleSheet currentStyleSheet)
        {
            if (data.uxmlValue.isInlined)
                return data.uxmlValue.inlineProperty.TryGetGridTrackTemplate(currentStyleSheet, m_AuthoredTemplate);

            var rule = data.selector.complexSelector?.rule;
            if (rule == null || data.selector.sheet == null)
                return false;
            // A rule keeps duplicate declarations and the last one wins.
            var properties = rule.properties;
            for (int i = properties.Length - 1; i >= 0; --i)
            {
                if (properties[i].id == data.id)
                    return properties[i].TryGetGridTrackTemplate(data.selector.sheet, m_AuthoredTemplate);
            }
            return false;
        }

        void INotifyCompositeStylePropertyChanged<StyleList<GridTrackSize>>.SetValue(BindingId id, StyleList<GridTrackSize> v, bool notify)
        {
            var previous = m_Value;
            m_Value = v.value != null ? new List<GridTrackSize>(v.value) : new List<GridTrackSize>();

            // Keep the shown repeat rows when they already expand to the value, so they are what gets written.
            GridTrackTemplateEntry.Flatten(m_Axis.GetTemplate(), m_FlattenedTemplate);
            if (!SequenceEqual(m_FlattenedTemplate, m_Value))
                m_Axis.SetTracks(m_Value.ToArray(), preserveRepeatRows: false);

            // Set as Inline Value writes the value the field already shows, so notify even when nothing changed.
            if (notify)
            {
                this.NotifyStylePropertyChanged(this, valueProperty, new StyleList<GridTrackSize>(previous), new StyleList<GridTrackSize>(m_Value));
                NotifyPropertyChanged(valueProperty);
            }
        }

        void INotifyCompositeStylePropertyChanged<StyleList<GridTrackSize>>.NotifyStylePropertyChanged(BindingId id, StyleList<GridTrackSize> previousValue, StyleList<GridTrackSize> newValue)
            => this.NotifyStylePropertyChanged(this, id, previousValue, newValue);

        void OnTrackProperty(TrackPropertyEvent evt)
        {
            if (evt.provider != null && m_TrackedProviders.Add(evt.provider))
                evt.provider.OnTrackedPropertyChanged += OnTrackedPropertyChanged;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            foreach (var provider in m_TrackedProviders)
                provider.OnTrackedPropertyChanged -= OnTrackedPropertyChanged;
            m_TrackedProviders.Clear();
        }

        // Single bound property, so any tracked-property signal here is this field's -> drive the one bar.
        void OnTrackedPropertyChanged(ITrackablePropertyProvider provider, string propertyName, TrackedPropertyType type)
            => m_OverrideBar.IsOverridden = type == TrackedPropertyType.MarkOverride;

        static bool SequenceEqual(List<GridTrackSize> a, List<GridTrackSize> b)
        {
            if (a == null || b == null)
                return ReferenceEquals(a, b);
            if (a.Count != b.Count)
                return false;
            for (int i = 0; i < a.Count; ++i)
                if (!a[i].Equals(b[i]))
                    return false;
            return true;
        }

        public static readonly string ussClassName = "grid-track-list-inline-field";
    }
}
