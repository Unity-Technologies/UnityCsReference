// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitFramework not yet converted
#pragma warning disable UAL0010,UAL0011,UAL0012,UAL0013,UAL0014 // AutoStaticsCleanup: UIToolkitFramework not yet converted
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements
{
    [UxmlElement]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal partial class BackgroundField : BaseField<Background>
    {
        /// <summary>
        /// USS class name of the object field in elements of this type.
        /// </summary>
        public static readonly string objectFieldUssClassName = "unity-multi-type-field__object-field";
        /// <summary>
        /// USS class name of the options popup in elements of this type.
        /// </summary>
        public static readonly string optionsPopupContainerName = "unity-multi-type-field__options-popup-container";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = "unity-multi-type-field__visual-input";

        // Non-Type popup entry — swaps the sub-control to the gradient editor.
        internal const string k_GradientTypeName = "Gradient";
        // Popup entry the field falls back to when the value holds neither an asset nor a gradient.
        internal const string k_TextureTypeName = "Texture";

        readonly ObjectField m_ObjectField;
        readonly Dictionary<string, Type> m_TypeOptions;
        readonly PopupField<string> m_TypePopup;
        readonly VisualElement m_GradientContainer;
        readonly EnumField m_GradientTypeField;
        readonly GradientField m_GradientColorsField;
        readonly FloatField m_GradientAngleField;
        readonly VisualElement m_GradientRadialOnlyContainer;
        readonly EnumField m_GradientSizeField;
        readonly Vector2Field m_GradientPositionField;
        bool m_SuppressGradientChange;

        public ObjectField objectField => m_ObjectField;
        public PopupField<string> typePopup => m_TypePopup;

        public BackgroundField() : this(null) {}

        public BackgroundField(string label) : base(label, null)
        {
            m_TypeOptions = new Dictionary<string, Type>();
            m_ObjectField = new ObjectField().WithClassList(objectFieldUssClassName);
            m_ObjectField.allowBuiltinResources = false;
            m_ObjectField.RegisterValueChangedCallback(OnObjectValueChange);

            m_ObjectField.objectFieldDisplay.RegisterDefaultDragAndDrop(new List<Type>() { typeof(Texture2D), typeof(RenderTexture), typeof(Sprite), typeof(VectorImage) });

            var popupContainer = new VisualElement() {name = optionsPopupContainerName }.WithClassList(optionsPopupContainerName);
            m_TypePopup = new PopupField<string> { formatSelectedValueCallback = OnFormatSelectedValue };
            popupContainer.Add(m_TypePopup);

            visualInput.AddToClassList(inputUssClassName);
            // Column layout — gradient sub-controls stack under the object-field row.
            visualInput.style.flexDirection = FlexDirection.Column;

            var topRow = new VisualElement();
            topRow.style.flexDirection = FlexDirection.Row;
            topRow.Add(m_ObjectField);
            topRow.Add(popupContainer);
            visualInput.Add(topRow);

            AddType(typeof(Texture2D), k_TextureTypeName);
            AddType(typeof(RenderTexture), "Render Texture");
            AddType(typeof(Sprite), "Sprite");
            AddType(typeof(VectorImage), "Vector");

            // Gradient sub-controls — shown when the popup is on "Gradient".
            m_GradientContainer = new VisualElement();
            m_GradientContainer.style.flexDirection = FlexDirection.Column;
            m_GradientContainer.style.display = DisplayStyle.None;

            m_GradientTypeField = new EnumField("Type", GradientType.Linear);
            m_GradientTypeField.RegisterValueChangedCallback(_ => OnGradientControlChanged());
            m_GradientContainer.Add(m_GradientTypeField);

            m_GradientColorsField = new GradientField("Color")
            {
                value = new Gradient(),
                tooltip = "The colors and alpha stops of the gradient. Up to 4 stops are supported.",
            };
            m_GradientColorsField.RegisterValueChangedCallback(_ => OnGradientControlChanged());
            m_GradientContainer.Add(m_GradientColorsField);

            m_GradientAngleField = new FloatField("Angle (deg)") { value = 180f };
            m_GradientAngleField.RegisterValueChangedCallback(_ => OnGradientControlChanged());
            m_GradientContainer.Add(m_GradientAngleField);

            m_GradientRadialOnlyContainer = new VisualElement();
            m_GradientSizeField = new EnumField("Extent", BackgroundGradientSize.FarthestCorner);
            m_GradientSizeField.RegisterValueChangedCallback(_ => OnGradientControlChanged());
            m_GradientRadialOnlyContainer.Add(m_GradientSizeField);
            m_GradientPositionField = new Vector2Field("Position") { value = new Vector2(0.5f, 0.5f) };
            m_GradientPositionField.RegisterValueChangedCallback(_ => OnGradientControlChanged());
            m_GradientRadialOnlyContainer.Add(m_GradientPositionField);
            m_GradientContainer.Add(m_GradientRadialOnlyContainer);

            visualInput.Add(m_GradientContainer);

            if (!m_TypePopup.choices.Contains(k_GradientTypeName))
                m_TypePopup.choices.Add(k_GradientTypeName);

            m_TypePopup.RegisterValueChangedCallback(evt =>
            {
                UpdateGradientVisibility();
                // On mode swap, emit through the destination writer so the style property updates.
                if (evt.newValue == k_GradientTypeName && evt.previousValue != k_GradientTypeName)
                    OnGradientControlChanged();
                else if (evt.previousValue == k_GradientTypeName && evt.newValue != k_GradientTypeName)
                    value = Background.FromObject(m_ObjectField.value);
            });
            UpdateGradientVisibility();
        }

        void UpdateGradientVisibility()
        {
            bool isGradient = m_TypePopup.value == k_GradientTypeName;
            m_GradientContainer.style.display = isGradient ? DisplayStyle.Flex : DisplayStyle.None;
            m_ObjectField.style.display = isGradient ? DisplayStyle.None : DisplayStyle.Flex;
            bool isLinear = (GradientType)m_GradientTypeField.value == GradientType.Linear;
            m_GradientAngleField.style.display = isLinear ? DisplayStyle.Flex : DisplayStyle.None;
            m_GradientRadialOnlyContainer.style.display = isLinear ? DisplayStyle.None : DisplayStyle.Flex;
        }

        void OnGradientControlChanged()
        {
            if (m_SuppressGradientChange) return;
            UpdateGradientVisibility();

            var gradient = new BackgroundGradient
            {
                type = (GradientType)m_GradientTypeField.value,
                angle = m_GradientAngleField.value * Mathf.Deg2Rad,
                shape = BackgroundGradientShape.Ellipse, // Circle disabled
                size = (BackgroundGradientSize)m_GradientSizeField.value,
                position = m_GradientPositionField.value,
                stops = UnityGradientToBackgroundStops(m_GradientColorsField.value),
            };

            value = Background.FromGradient(gradient);
        }

        // Sensible starting gradient (CSS "to bottom", white → white) shown when nothing is set.
        internal static BackgroundGradient defaultAuthoringGradient
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            get => BackgroundGradient.Linear(Mathf.PI,
                BackgroundGradientStop.Percent(Color.white, 0f),
                BackgroundGradientStop.Percent(Color.white, 1f));
        }

        void PushGradientToControls(in BackgroundGradient g)
        {
            m_SuppressGradientChange = true;
            try
            {
                m_GradientTypeField.SetValueWithoutNotify(g.type);
                m_GradientAngleField.SetValueWithoutNotify(g.angle * Mathf.Rad2Deg);
                m_GradientSizeField.SetValueWithoutNotify(g.size);
                m_GradientPositionField.SetValueWithoutNotify(g.position);
                m_GradientColorsField.SetValueWithoutNotify(BackgroundGradientToUnityGradient(g));
            }
            finally
            {
                m_SuppressGradientChange = false;
            }
        }

        // Guards the color-key spam Laila flagged when dragging in the gradient picker.
        // Shared between this field and BackgroundGradientField. Reset on reload so the
        // warning can re-arm instead of being permanently suppressed.
        [AutoStaticsCleanupOnCodeReload]
        static bool s_WarnedTooManyGradientKeys;

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static Gradient BackgroundGradientToUnityGradient(in BackgroundGradient bg)
        {
            var stops = bg.stops;
            if (stops == null || stops.Length == 0)
            {
                return new Gradient
                {
                    colorKeys = new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) },
                };
            }
            var colorKeys = new GradientColorKey[stops.Length];
            var alphaKeys = new GradientAlphaKey[stops.Length];
            for (int i = 0; i < stops.Length; ++i)
            {
                float t = stops[i].positionIsPercent
                    ? Mathf.Clamp01(stops[i].position)
                    : Mathf.Clamp01((float)i / Mathf.Max(1, stops.Length - 1));
                colorKeys[i] = new GradientColorKey(new Color(stops[i].color.r, stops[i].color.g, stops[i].color.b, 1f), t);
                alphaKeys[i] = new GradientAlphaKey(stops[i].color.a, t);
            }
            return new Gradient { colorKeys = colorKeys, alphaKeys = alphaKeys };
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static BackgroundGradientStop[] UnityGradientToBackgroundStops(Gradient g)
        {
            if (g == null)
                return new[] { BackgroundGradientStop.Percent(Color.white, 0f), BackgroundGradientStop.Percent(Color.white, 1f) };

            var times = new SortedSet<float>();
            foreach (var k in g.colorKeys) times.Add(k.time);
            foreach (var k in g.alphaKeys) times.Add(k.time);
            if (times.Count == 0) { times.Add(0f); times.Add(1f); }

            int max = UnmanagedBackgroundGradient.MaxStops;
            var output = new List<BackgroundGradientStop>(times.Count);
            foreach (var t in times)
            {
                output.Add(BackgroundGradientStop.Percent(g.Evaluate(t), Mathf.Clamp01(t)));
                if (output.Count >= max) break;
            }
            if (output.Count < times.Count && !s_WarnedTooManyGradientKeys)
            {
                s_WarnedTooManyGradientKeys = true;
                UnityEngine.Debug.LogWarning(
                    $"Background gradient has {times.Count} unique key times; only the first {max} are kept. " +
                    $"Reduce the number of color/alpha keys to silence this warning.");
            }

            return output.ToArray();
        }

        void OnObjectValueChange(ChangeEvent<Object> evt)
        {
            value = Background.FromObject(evt.newValue);
            evt.StopImmediatePropagation();
        }

        protected void AddType(Type type)
        {
            AddType(type, type.Name);
        }

        protected void AddType(Type type, string displayName)
        {
            if (m_TypeOptions.ContainsKey(displayName))
                throw new ArgumentException($"Item with the name: {displayName} already exists.", nameof(displayName));

            m_TypeOptions.Add(displayName, type);
            m_TypePopup.choices.Add(displayName);

            m_TypePopup.style.display = m_TypeOptions.Count <= 1
                ? DisplayStyle.None
                : DisplayStyle.Flex;

            if (string.IsNullOrEmpty(m_TypePopup.value))
                m_TypePopup.value = displayName;
        }

        string OnFormatSelectedValue(string formatValue)
        {
            // Pass non-Type entries (e.g. "Gradient") through unchanged.
            if (m_TypeOptions.Count > 0 && m_TypeOptions.TryGetValue(formatValue, out var t))
            {
                m_ObjectField.objectType = t;
                if (!m_ObjectField.value) return formatValue;
                if (!m_ObjectField.objectType.IsInstanceOfType(m_ObjectField.value))
                    m_ObjectField.value = null;
            }

            return formatValue;
        }

        public override void SetValueWithoutNotify(Background newValue)
        {
            // Check gradient first — otherwise the baked VectorImage routes us into the asset branch.
            if (!newValue.gradient.IsEmpty())
            {
                m_ObjectField.SetValueWithoutNotify(null);
                PushGradientToControls(newValue.gradient);
                m_TypePopup.SetValueWithoutNotify(k_GradientTypeName);
            }
            else
            {
                var obj = newValue.GetSelectedImage();
                m_ObjectField.SetValueWithoutNotify(obj);
                if (obj)
                {
                    foreach (var pair in m_TypeOptions)
                    {
                        // Match on the selected asset, not the Background struct.
                        if (pair.Value.IsInstanceOfType(obj))
                        {
                            m_TypePopup.SetValueWithoutNotify(pair.Key);
                            break;
                        }
                    }
                }
                else if (m_TypePopup.value == k_GradientTypeName)
                {
                    m_TypePopup.SetValueWithoutNotify(k_TextureTypeName);
                }

                // The field outlives the selection, so drop the previous gradient rather than
                // offering it as the starting point for whatever is selected next.
                PushGradientToControls(defaultAuthoringGradient);
            }

            UpdateGradientVisibility();

            base.SetValueWithoutNotify(newValue);
        }
    }
}
#pragma warning restore UAL0010,UAL0011,UAL0012,UAL0013,UAL0014
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
