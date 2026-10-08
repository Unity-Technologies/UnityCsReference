// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using JetBrains.Annotations;
using Unity.UIToolkit.Editor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    // Composite field for BackgroundGradient — wraps the built-in GradientField (color/alpha
    // key editing) with the type / angle / radial controls it doesn't expose. Gradient's
    // 8+8 keys are sampled down to BackgroundGradient's MaxStops-per-side model.
    [UsedImplicitly]
    [UxmlElement]
    internal partial class BackgroundGradientField : BaseField<BackgroundGradient>
    {
        const string k_FieldClassName = "unity-background-gradient-style-field";
        const string k_RowClassName = "unity-background-gradient-style-field__row";
        const string k_RadialOnlyClassName = "unity-background-gradient-style-field__radial-only";

        readonly EnumField m_TypeField;
        readonly GradientField m_GradientField;
        readonly FloatField m_AngleField;
        readonly VisualElement m_RadialOnlyContainer;
        readonly EnumField m_ShapeField;
        readonly EnumField m_SizeField;
        readonly Vector2Field m_PositionField;

        StyleFieldPopupWindow m_StopsPopup;
        BackgroundGradientStopsEditor m_StopsEditor;

        bool m_SuppressChangeEvents;

        // Host hooks forwarded to the stops editor: resolves a bound var name to the StyleSheet
        // defining it (null = unresolved), the element scoping the available variables, and
        // editor-extension mode. Set by the Builder inspector on field refresh.
        internal Func<string, StyleSheet> resolveVariableSheet { get; set; }
        internal Func<string, Color?> resolveVariableColor { get; set; }
        internal Func<VisualElement> getCurrentVisualElement { get; set; }
        internal Func<bool> getEditorExtensionMode { get; set; }

        readonly GradientVarBindingTracker m_VarBindings = new();

        public BackgroundGradientField() : this(null) {}

        public BackgroundGradientField(string label) : base(label, new VisualElement())
        {
            AddToClassList(BuilderConstants.InspectorContainerClassName);
            AddToClassList(k_FieldClassName);

            visualInput.style.flexDirection = FlexDirection.Column;

            m_TypeField = new EnumField("Type", GradientType.Linear);
            m_TypeField.AddToClassList(k_RowClassName);
            // Switching type invalidates every slot mapping, so all vars go.
            m_TypeField.RegisterValueChangedCallback(_ => OnControlChanged(m_VarBindings.ClearAll));
            visualInput.Add(m_TypeField);

            m_GradientField = new GradientField("Gradient Stops")
            {
                value = new Gradient(),
                tooltip = "The color stops of the gradient. Click to edit. Up to 4 stops are supported.",
            };
            m_GradientField.AddToClassList(k_RowClassName);
            // The swatch editor resamples the stops, so per-stop vars lose their anchor.
            m_GradientField.RegisterValueChangedCallback(_ => OnControlChanged(m_VarBindings.ClearStops));
            // Intercept the interactions that would open the built-in gradient picker
            // (see GradientField.HandleEventBubbleUp) and open the stops popup instead.
            m_GradientField.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == (int)MouseButton.LeftMouse)
                {
                    OpenStopsPopup();
                    evt.StopPropagation();
                }
            }, TrickleDown.TrickleDown);
            m_GradientField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode is KeyCode.Space or KeyCode.KeypadEnter or KeyCode.Return)
                {
                    OpenStopsPopup();
                    evt.StopPropagation();
                }
            }, TrickleDown.TrickleDown);
            visualInput.Add(m_GradientField);

            m_AngleField = new FloatField("Angle (deg)") { value = 180f };
            m_AngleField.AddToClassList(k_RowClassName);
            m_AngleField.RegisterValueChangedCallback(_ => OnControlChanged(() => m_VarBindings.angleVarName = null));
            visualInput.Add(m_AngleField);

            m_RadialOnlyContainer = new VisualElement();
            m_RadialOnlyContainer.AddToClassList(k_RadialOnlyClassName);

            // The shape is never var-bound, so there is no var name to clear.
            m_ShapeField = new EnumField("Shape", BackgroundGradientShape.Ellipse);
            m_ShapeField.AddToClassList(k_RowClassName);
            m_ShapeField.RegisterValueChangedCallback(_ => OnControlChanged(() => {}));
            m_RadialOnlyContainer.Add(m_ShapeField);

            m_SizeField = new EnumField("Extent", BackgroundGradientSize.FarthestCorner);
            m_SizeField.AddToClassList(k_RowClassName);
            m_SizeField.RegisterValueChangedCallback(_ => OnControlChanged(() => m_VarBindings.extentVarName = null));
            m_RadialOnlyContainer.Add(m_SizeField);

            m_PositionField = new Vector2Field("Position") { value = new Vector2(0.5f, 0.5f) };
            m_PositionField.AddToClassList(k_RowClassName);
            m_PositionField.RegisterValueChangedCallback(_ => OnControlChanged(() =>
            {
                m_VarBindings.positionXVarName = null;
                m_VarBindings.positionYVarName = null;
            }));
            m_RadialOnlyContainer.Add(m_PositionField);

            visualInput.Add(m_RadialOnlyContainer);

            UpdateModeVisibility(GradientType.Linear);
        }

        // Force-dispatches a ChangeEvent with the current value, bypassing the equality guard.
        internal void NotifyCurrentValue()
        {
            using var evt = ChangeEvent<BackgroundGradient>.GetPooled(value, value);
            evt.target = this;
            SendEvent(evt);
        }

        public override void SetValueWithoutNotify(BackgroundGradient newValue)
        {
            base.SetValueWithoutNotify(newValue);
            PushModelToControls(newValue);
        }

        // Sensible starting gradient (CSS "to bottom", black → white) shown when nothing is set.
        internal static BackgroundGradient defaultAuthoringGradient
            => UnityEditor.UIElements.BackgroundField.defaultAuthoringGradient;

        void PushModelToControls(in BackgroundGradient model)
        {
            // Substitute the authoring default so empty gradients don't show as 0°/(0,0).
            var effective = model.IsEmpty() ? defaultAuthoringGradient : model;
            m_SuppressChangeEvents = true;
            try
            {
                m_TypeField.SetValueWithoutNotify(effective.type);
                m_AngleField.SetValueWithoutNotify(effective.angle * Mathf.Rad2Deg);
                m_ShapeField.SetValueWithoutNotify(effective.shape);
                m_SizeField.SetValueWithoutNotify(effective.size);
                m_PositionField.SetValueWithoutNotify(effective.position);
                m_GradientField.SetValueWithoutNotify(BackgroundGradientToUnityGradient(effective));
                UpdateModeVisibility(effective.type);
            }
            finally
            {
                m_SuppressChangeEvents = false;
            }
        }

        void OnControlChanged(Action clearOverriddenVarNames)
        {
            if (m_SuppressChangeEvents)
                return;

            clearOverriddenVarNames();

            var type = (GradientType)m_TypeField.value;
            UpdateModeVisibility(type);

            var built = new BackgroundGradient
            {
                type = type,
                angle = m_AngleField.value * Mathf.Deg2Rad,
                shape = (BackgroundGradientShape)m_ShapeField.value,
                size = (BackgroundGradientSize)m_SizeField.value,
                position = m_PositionField.value,
                stops = UnityGradientToBackgroundStops(m_GradientField.value),
            };
            m_VarBindings.SyncStopCount(built.stops.Length);
            value = built;
        }

        // Decodes the slot-indexed bindings read from the style property into named vars,
        // anchored on the currently shown gradient.
        internal void SetVarBindings(in StyleProperty.GradientVarBindings bindings)
        {
            m_VarBindings.SetVarBindings(bindings, value.IsEmpty() ? defaultAuthoringGradient : value);

            // The inspector refresh lands here after every write; mirror the resolved
            // stops and bindings into the stops popup if it is open.
            RefreshStopsEditor();
        }

        void RefreshStopsEditor()
        {
            if (m_StopsEditor == null)
                return;

            var effective = value.IsEmpty() ? defaultAuthoringGradient : value;
            m_StopsEditor.SetStops(m_VarBindings.CreateStopData(effective.stops));
        }

        // Re-encodes the named vars into the slot scheme for the write path.
        internal StyleProperty.GradientVarBindings BuildVarBindings()
        {
            return m_VarBindings.BuildVarBindings((GradientType)m_TypeField.value == GradientType.Linear);
        }

        void OpenStopsPopup()
        {
            if (m_StopsPopup != null)
            {
                m_StopsPopup.Close();
                return;
            }

            m_StopsEditor = new BackgroundGradientStopsEditor();
            m_StopsEditor.resolveVariableSheet = name => resolveVariableSheet?.Invoke(name);
            m_StopsEditor.resolveVariableColor = name => resolveVariableColor?.Invoke(name);
            m_StopsEditor.getCurrentVisualElement = () => getCurrentVisualElement?.Invoke();
            m_StopsEditor.getEditorExtensionMode = () => getEditorExtensionMode?.Invoke() ?? false;
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
            var anchor = GUIUtility.GUIToScreenRect(m_GradientField.worldBound);
            popup.position = new Rect(anchor.x, anchor.yMax, 400, 200);
            popup.minSize = new Vector2(280, 200);
            popup.maxSize = new Vector2(4000, 4000);
            popup.ShowAuxWindow();
            popup.content = m_StopsEditor;
            m_StopsPopup = popup;
        }

        void OnStopsEdited(BackgroundGradientStopsEditor.StopData[] stopDatas)
        {
            bool varNamesChanged = m_VarBindings.SetStopNames(stopDatas);

            var stops = new BackgroundGradientStop[stopDatas.Length];
            for (int i = 0; i < stopDatas.Length; ++i)
                stops[i] = stopDatas[i].stop;

            var built = new BackgroundGradient
            {
                type = (GradientType)m_TypeField.value,
                angle = m_AngleField.value * Mathf.Deg2Rad,
                shape = (BackgroundGradientShape)m_ShapeField.value,
                size = (BackgroundGradientSize)m_SizeField.value,
                position = m_PositionField.value,
                stops = stops,
            };

            // Binding/unbinding a var leaves the resolved gradient unchanged, so the
            // BaseField equality guard would swallow the event and nothing gets written.
            bool sameResolvedValue = value.Equals(built);
            value = built;
            if (sameResolvedValue && varNamesChanged)
                NotifyCurrentValue();
        }

        void UpdateModeVisibility(GradientType type)
        {
            bool isLinear = type == GradientType.Linear;
            m_AngleField.style.display = isLinear ? DisplayStyle.Flex : DisplayStyle.None;
            m_RadialOnlyContainer.style.display = isLinear ? DisplayStyle.None : DisplayStyle.Flex;
        }

        // Model conversion between BackgroundGradient stops and UnityEngine.Gradient keys
        // lives on UnityEditor.UIElements.BackgroundField (see BackgroundGradientToUnityGradient
        // / UnityGradientToBackgroundStops there); we call into the shared implementation to
        // avoid the two copies drifting.
        static Gradient BackgroundGradientToUnityGradient(in BackgroundGradient bg)
            => UnityEditor.UIElements.BackgroundField.BackgroundGradientToUnityGradient(bg);
        static BackgroundGradientStop[] UnityGradientToBackgroundStops(Gradient g)
            => UnityEditor.UIElements.BackgroundField.UnityGradientToBackgroundStops(g);
    }
}
