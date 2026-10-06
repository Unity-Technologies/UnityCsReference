// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Makes a style field for editing a StyleBackground.
    /// </summary>
    [UxmlElement]
    internal partial class StyleBackgroundField : StylePropertyField<StyleBackground, BackgroundField, Background>,
        IStylePropertyDataField<StyleBackground, Background>
    {
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-background-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        StyleFieldPopupWindow m_StopsPopup;
        BackgroundGradientStopsEditor m_StopsEditor;

        // Element scoping the vars available to the stops editor; refreshed by the style property binding.
        VisualElement m_VarContextElement;
        Action<StyleProperty, StyleSheet, StyleBackground> m_SetterOverride;

        readonly GradientVarBindingTracker m_VarBindings = new();

        /// <summary>
        /// Constructor.
        /// </summary>
        public StyleBackgroundField()
            : this(null) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        public StyleBackgroundField(string label)
            : base(label, new BackgroundField())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);

            // Intercept the interactions that would open the built-in gradient picker
            // (see GradientField.HandleEventBubbleUp) and open the stops popup instead.
            var gradientStrip = valueField.gradientColorsField;
            gradientStrip.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == (int)MouseButton.LeftMouse)
                {
                    OpenStopsPopup();
                    evt.StopPropagation();
                }
            }, TrickleDown.TrickleDown);
            gradientStrip.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode is KeyCode.Space or KeyCode.KeypadEnter or KeyCode.Return)
                {
                    OpenStopsPopup();
                    evt.StopPropagation();
                }
            }, TrickleDown.TrickleDown);

            // The base class writes the style from its own bubble-up change callback; TrickleDown
            // makes this bookkeeping run first so BuildVarBindings sees the dropped slots. Stops
            // can only change through the popup (the built-in picker is intercepted), which
            // maintains its own var names in OnStopsEdited, so a control edit arriving here can
            // only override the type/angle/extent/position slots.
            valueField.RegisterCallback<ChangeEvent<Background>>(
                evt => m_VarBindings.DropOverridden(evt.previousValue.gradient, evt.newValue.gradient),
                TrickleDown.TrickleDown);
        }

        protected override BackgroundField CreateValueField()
        {
            return new BackgroundField();
        }

        protected override StyleBackground CreateStyleValue(Background v)
        {
            return v;
        }

        internal override bool EqualsCurrentValue(StyleBackground v)
        {
            return value == v;
        }

        public override void SetValueWithoutNotify(StyleBackground newValue)
        {
            base.SetValueWithoutNotify(newValue);
            RefreshStopsEditor();
        }

        // Force-dispatches a ChangeEvent with the current value, bypassing the equality guard.
        internal void NotifyCurrentValue()
        {
            using var evt = ChangeEvent<StyleBackground>.GetPooled(value, value);
            evt.target = this;
            SendEvent(evt);
        }

        // Substitutes the authoring default so an empty gradient doesn't open an empty editor.
        BackgroundGradient effectiveGradient
        {
            get
            {
                var gradient = valueField.value.gradient;
                return gradient.IsEmpty() ? BackgroundField.defaultAuthoringGradient : gradient;
            }
        }

        // Decodes the slot-indexed bindings read from the style property into named vars,
        // anchored on the currently shown gradient.
        internal void SetVarBindings(in StyleProperty.GradientVarBindings bindings)
        {
            m_VarBindings.SetVarBindings(bindings, effectiveGradient);

            // The inspector refresh lands here after every write; mirror the resolved
            // stops and bindings into the stops popup if it is open.
            RefreshStopsEditor();
        }

        // Re-encodes the named vars into the slot scheme for the write path.
        internal StyleProperty.GradientVarBindings BuildVarBindings()
        {
            return m_VarBindings.BuildVarBindings(effectiveGradient.type == GradientType.Linear);
        }

        // The generated SetBackground setter writes gradients with GradientVarBindings.none, wiping stored
        // var() references. Re-route gradient writes through a setter that carries the field's bindings.
        Action<StyleProperty, StyleSheet, StyleBackground> IStylePropertyDataField<StyleBackground, Background>.setterOverride
            => m_SetterOverride ??= WriteBackgroundProperty;

        void WriteBackgroundProperty(StyleProperty property, StyleSheet sheet, StyleBackground v)
        {
            if (v.value.gradient.IsEmpty())
                StylePropertyBinding.SetBackground(property, sheet, v);
            else
                property.SetBackgroundGradient(sheet, v.value.gradient, BuildVarBindings());
        }

        void IStylePropertyDataField<StyleBackground, Background>.SetValueFromStyleData(
            in StylePropertyData<StyleBackground, Background> data, VisualElement currentTarget, StyleSheet currentStyleSheet)
        {
            value = data.computedValue;

            // In VisualElement context currentStyleSheet is the UXML inline sheet, so the same
            // decode covers inline styles authored with var().
            var gradient = valueField.value.gradient;
            var bindings = StyleProperty.GradientVarBindings.none;
            if (data.uxmlValue.isInlined && currentStyleSheet is { } gradientSheet)
            {
                data.uxmlValue.inlineProperty.TryReadGradientVarBindings(gradientSheet, gradient, out bindings);
            }
            else if (data.selector.sheet != null
                     && data.selector.complexSelector?.rule?.FindLastProperty(data.id) is { } selectorProperty
                     && selectorProperty.TryReadGradientVarBindings(data.selector.sheet, gradient, out var selectorBindings))
            {
                // Raw handles index the selector's sheet, but an edit writes to the inline
                // sheet; carry only the names so fresh var() constructs are emitted.
                bindings = selectorBindings.WithoutHandles();
            }
            SetVarBindings(bindings);

            m_VarContextElement = currentTarget;
        }

        VariableInfo ResolveVariable(string varName)
        {
            if (m_VarContextElement == null)
                return default;
            return StyleVariableUtility.FindVariable(m_VarContextElement, varName, false);
        }

        Color? ResolveVariableColor(string varName)
        {
            var varInfo = ResolveVariable(varName);
            var handles = varInfo.StyleVariable.handles;
            if (varInfo.Sheet == null || handles == null || handles.Length == 0)
                return null;

            if (handles[0].valueType == StyleValueType.Color)
                return varInfo.Sheet.ReadColor(handles[0]);
            if (handles[0].valueType == StyleValueType.Enum
                && StyleSheetColor.TryGetColor(varInfo.Sheet.ReadAsString(handles[0]).ToLowerInvariant(), out var namedColor))
                return namedColor;
            return null;
        }

        void OpenStopsPopup()
        {
            if (m_StopsPopup != null)
            {
                m_StopsPopup.Close();
                return;
            }

            m_StopsEditor = new BackgroundGradientStopsEditor();
            m_StopsEditor.resolveVariableSheet = name => ResolveVariable(name).Sheet;
            m_StopsEditor.resolveVariableColor = ResolveVariableColor;
            m_StopsEditor.getCurrentVisualElement = () => m_VarContextElement;
            m_StopsEditor.getEditorExtensionMode = () => false;
            RefreshStopsEditor();
            m_StopsEditor.stopsChanged += OnStopsEdited;

            var popup = ScriptableObject.CreateInstance<StyleFieldPopupWindow>();
            popup.titleContent = new GUIContent("Gradient Editor");
            popup.resizable = true;
            popup.closed += () =>
            {
                m_StopsPopup = null;
                m_StopsEditor = null;
            };

            // ShowAsDropDown pins minSize == maxSize at creation, and the native side strips
            // the resize border (WS_THICKFRAME) when they're equal — never adds it back.
            // Set position and min/max BEFORE ShowAuxWindow so the aux window is born resizable.
            var anchor = GUIUtility.GUIToScreenRect(valueField.gradientColorsField.worldBound);
            popup.position = new Rect(anchor.x, anchor.yMax, 400, 200);
            popup.minSize = new Vector2(280, 200);
            popup.maxSize = new Vector2(4000, 4000);
            popup.ShowAuxWindow();
            popup.content = m_StopsEditor;
            m_StopsPopup = popup;
        }

        void RefreshStopsEditor()
        {
            if (m_StopsEditor == null)
                return;

            m_StopsEditor.SetStops(m_VarBindings.CreateStopData(effectiveGradient.stops));
        }

        void OnStopsEdited(BackgroundGradientStopsEditor.StopData[] stopDatas)
        {
            bool varNamesChanged = m_VarBindings.SetStopNames(stopDatas);

            var stops = new BackgroundGradientStop[stopDatas.Length];
            for (int i = 0; i < stopDatas.Length; ++i)
                stops[i] = stopDatas[i].stop;

            var effective = effectiveGradient;
            var built = new BackgroundGradient
            {
                type = effective.type,
                angle = effective.angle,
                shape = effective.shape,
                size = effective.size,
                position = effective.position,
                stops = stops,
            };

            // Route through the value field so the regular change callback writes the style.
            var newBackground = Background.FromGradient(built);
            bool sameResolvedValue = valueField.value == newBackground;
            valueField.value = newBackground;

            // Binding/unbinding a var leaves the resolved gradient unchanged, so the
            // BaseField equality guard would swallow the event and nothing gets written.
            if (sameResolvedValue && varNamesChanged)
                NotifyCurrentValue();
        }
    }
}
