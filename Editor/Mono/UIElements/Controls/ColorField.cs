// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Makes a field for selecting a color.
    /// </summary>
    public class ColorField : BaseField<Color>
    {
        /// <summary>
        /// Instantiates a <see cref="ColorField"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<ColorField, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="ColorField"/>.
        /// </summary>
        public new class UxmlTraits : BaseFieldTraits<Color, UxmlColorAttributeDescription>
        {
            UxmlBoolAttributeDescription m_ShowEyeDropper = new UxmlBoolAttributeDescription { name = "show-eye-dropper", defaultValue = true };
            UxmlBoolAttributeDescription m_ShowAlpha = new UxmlBoolAttributeDescription { name = "show-alpha", defaultValue = true };
            UxmlBoolAttributeDescription m_Hdr = new UxmlBoolAttributeDescription { name = "hdr" };

            /// <summary>
            /// Initialize <see cref="ColorField"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            /// <param name="cc">The creation context; unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                ((ColorField)ve).showEyeDropper = m_ShowEyeDropper.GetValueFromBag(bag, cc);
                ((ColorField)ve).showAlpha = m_ShowAlpha.GetValueFromBag(bag, cc);
                ((ColorField)ve).hdr = m_Hdr.GetValueFromBag(bag, cc);
            }
        }

        /// <summary>
        /// If true, the color picker will show the eyedropper control. If false, the color picker won't show the eyedropper control.
        /// </summary>
        public bool showEyeDropper
        {
            get => m_ShowEyeDropper;
            set
            {
                m_ShowEyeDropper = value;
                if (m_EyeDropperElement != null)
                    m_EyeDropperElement.style.display = m_ShowEyeDropper ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>
        /// If true, allows the user to set an alpha value for the color. If false, hides the alpha component.
        /// </summary>
        public bool showAlpha
        {
            get => m_ShowAlpha;
            set
            {
                m_ShowAlpha = value;
                if (m_AlphaElement != null)
                    m_AlphaElement.style.display = m_ShowAlpha ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>
        /// If true, treats the color as an HDR value. If false, treats the color as a standard LDR value.
        /// </summary>
        public bool hdr
        {
            get => m_HDR;
            set
            {
                m_HDR = value;
                if (m_HDRLabel != null)
                    m_HDRLabel.style.display = m_HDR ? DisplayStyle.Flex : DisplayStyle.None;
                if (m_GradientContainer != null)
                    m_GradientContainer.style.display = m_HDR ? DisplayStyle.Flex : DisplayStyle.None;
                if (m_AlphaGradientContainer != null)
                    m_AlphaGradientContainer.style.display = m_HDR ? DisplayStyle.Flex : DisplayStyle.None;
                UpdateColorProperties(this.value);
            }
        }

        /// <summary>
        /// The <see cref="Color"/> currently being exposed by the field.
        /// </summary>
        public override Color value
        {
            get => rawValue;
            set
            {
                if (value != rawValue)
                {
                    using (ChangeEvent<Color> evt = ChangeEvent<Color>.GetPooled(rawValue, value))
                    {
                        evt.target = this;
                        SetValueWithoutNotify(value);
                        SendEvent(evt);
                    }
                    rawValue = value;
                }
                UpdateColorProperties(rawValue);
            }
        }

        bool m_ShowAlpha;
        bool m_ShowEyeDropper;
        bool m_HDR;
        Color m_ColorBeforeEyeDrop;
        IVisualElementScheduledItem m_EyeDropperScheduler;

        Label m_HDRLabel;
        VisualElement m_ColorContainer;
        VisualElement m_GradientContainer;
        VisualElement m_LeftGradient;
        VisualElement m_RightGradient;
        VisualElement m_AlphaGradientContainer;
        VisualElement m_LeftAlphaGradient;
        VisualElement m_RightAlphaGradient;
        VisualElement m_ColorElement;
        ProgressBar m_AlphaElement;
        VisualElement m_EyeDropperElement;

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-color-field";

        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";

        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        /// <summary>
        /// USS class name of color container elements in elements of this type.
        /// </summary>
        public static readonly string colorContainerUssClassName = ussClassName + "__color-container";

        /// <summary>
        /// USS class name of color elements in elements of this type.
        /// </summary>
        public static readonly string colorUssClassName = ussClassName + "__color";

        /// <summary>
        /// USS class name of color elements in elements of this type when showing mixed values.
        /// </summary>
        public static readonly string mixedValueColorUssClassName = colorUssClassName + "--mixed-value";

        /// <summary>
        /// USS class name of eyedropper elements in elements of this type.
        /// </summary>
        public static readonly string eyeDropperUssClassName = ussClassName + "__eyedropper";

        /// <summary>
        /// USS class name of hdr label elements in elements of this type.
        /// </summary>
        public static readonly string hdrLabelUssClassName = ussClassName + "__hdr";

        /// <summary>
        /// USS class name of gradient container elements in elements of this type.
        /// </summary>
        public static readonly string gradientContainerUssClassName = ussClassName + "__gradient-container";

        internal static readonly string internalColorFieldName = "unity-internal-color-field";

        /// <summary>
        /// Initializes and returns an instance of ColorField.
        /// </summary>
        public ColorField()
            : this(null) {}

        /// <summary>
        /// Initializes and returns an instance of ColorField.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        public ColorField(string label)
            : base(label, null)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);

            m_ColorContainer = new VisualElement() { name = "unity-color-container" };
            m_ColorElement = new VisualElement() { name = internalColorFieldName };
            m_AlphaElement = new ProgressBar() { name = "unity-alpha-progress", title = string.Empty, lowValue = 0.0f, highValue = 100.0f };
            m_HDRLabel = new Label("HDR") { name = "unity-hdr-label" };
            m_EyeDropperElement = new VisualElement() { name = "unity-eyedropper" };

            m_GradientContainer = new VisualElement() { name = "unity-gradient-container" };
            m_LeftGradient = new VisualElement() { name = "unity-gradient-left" };
            m_RightGradient = new VisualElement() { name = "unity-gradient-right" };

            m_AlphaGradientContainer = new VisualElement() { name = "unity-alpha-gradient-container" };
            m_LeftAlphaGradient = new VisualElement() { name = "unity-alpha-gradient-left" };
            m_RightAlphaGradient = new VisualElement() { name = "unity-alpha-gradient-right" };
            mixedValueLabel.name = "unity-mixed-value-label";

            visualInput.AddToClassList(inputUssClassName);
            m_ColorContainer.AddToClassList(colorContainerUssClassName);
            m_ColorElement.AddToClassList(colorUssClassName);
            m_HDRLabel.AddToClassList(hdrLabelUssClassName);
            m_EyeDropperElement.AddToClassList(eyeDropperUssClassName);
            m_GradientContainer.AddToClassList(gradientContainerUssClassName);
            m_AlphaGradientContainer.AddToClassList(gradientContainerUssClassName);

            m_GradientContainer.Add(m_LeftGradient);
            m_GradientContainer.Add(new VisualElement()); // empty middle VisualElement
            m_GradientContainer.Add(m_RightGradient);

            m_AlphaGradientContainer.Add(m_LeftAlphaGradient);
            m_AlphaGradientContainer.Add(new VisualElement()); // empty middle VisualElement
            m_AlphaGradientContainer.Add(m_RightAlphaGradient);

            m_ColorContainer.Add(m_ColorElement);
            m_ColorContainer.Add(m_GradientContainer);
            m_ColorContainer.Add(m_AlphaGradientContainer);
            m_ColorContainer.Add(m_HDRLabel);
            m_ColorContainer.Add(mixedValueLabel);
            m_ColorContainer.Add(m_AlphaElement);

            visualInput.Add(m_ColorContainer);
            visualInput.Add(m_EyeDropperElement);

            m_ColorContainer.RegisterCallback<AttachToPanelEvent>(OnAttach);
            m_ColorContainer.RegisterCallback<PointerDownEvent>(OnColorFieldClicked);
            visualInput.RegisterCallback<KeyDownEvent>(OnColorFieldKeyDown);
            m_EyeDropperElement.RegisterCallback<PointerDownEvent>(OnEyeDropperClicked);
            RegisterCallback<ExecuteCommandEvent>(OnCommandExecute);

            labelElement.focusable = false;
            showAlpha = true;
            hdr = false;

            mixedValueLabel.style.display = showMixedValue ? DisplayStyle.Flex : DisplayStyle.None;
            mixedValueLabel.tabIndex = 0;
            mixedValueLabel.focusable = false;
            rawValue = new Color();
        }

        void OnColorFieldClicked(PointerDownEvent evt)
        {
            if (evt.button == (int) MouseButton.LeftMouse)
            {
                ShowColorPicker();
                evt.StopPropagation();
                return;
            }

            if (evt.button == (int)MouseButton.RightMouse)
            {
                var menu = new DropdownMenu();
                menu.AppendAction(
                    "Copy",
                    a => Clipboard.colorValue = value);

                menu.AppendAction(
                    "Paste",
                    a => value = Clipboard.colorValue,
                    Clipboard.hasColor
                        ? DropdownMenuAction.Status.Normal
                        : DropdownMenuAction.Status.Disabled);
                menu.DoDisplayEditorMenu(evt);
                evt.StopPropagation();
            }
        }

        void ShowColorPicker()
        {
            ColorPicker.Show((c) =>
                {
                    showMixedValue = false;
                    value = c;
                },
                value, m_ShowAlpha, m_HDR);
        }

        void OnColorFieldKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode is KeyCode.Space or KeyCode.KeypadEnter or KeyCode.Return)
            {
                ShowColorPicker();
                evt.StopPropagation();
            }
        }

        void OnEyeDropperClicked(PointerDownEvent evt)
        {
            if (EyeDropper.IsOpened || evt.button != (int)MouseButton.LeftMouse)
                return;

            m_ColorBeforeEyeDrop = value;
            EyeDropper.Start(UpdateColorProperties);
            m_EyeDropperScheduler = schedule.Execute(OnEyeDropperMove).Every(10).StartingIn(10)
                .Until(ShouldStopWatchingEyeDropper);
            evt.StopPropagation();
        }

        bool ShouldStopWatchingEyeDropper()
        {
            if (EyeDropper.IsOpened)
                return false;
            if (EyeDropper.IsCancelled)
                value = m_ColorBeforeEyeDrop;
            else
            {
                Color pickedColor = EyeDropper.GetPickedColor();
                // Eyedropper color picking should not impact the previous color alpha.
                pickedColor.a = value.a;
                value = pickedColor;
            }

            if (m_EyeDropperScheduler != null)
            {
                m_EyeDropperScheduler.Pause();
                m_EyeDropperScheduler = null;
            }
            return true;
        }

        void OnCommandExecute(ExecuteCommandEvent evt)
        {
            if (evt?.commandName == EventCommandNames.EyeDropperUpdate)
            {
                IncrementVersion(VersionChangeType.Repaint);
                evt.StopPropagation();
            }
        }

        void UpdateColorProperties(Color color)
        {
            if (panel == null || showMixedValue)
                return;

            if (m_AlphaElement != null)
                m_AlphaElement.value = color.a * 100.0f;

            color = new Color(color.r, color.g, color.b, 1.0f);

            if (hdr)
            {
                color = color.gamma;
                ColorMutator.DecomposeHdrColor(color.linear, out var baseColor, out _);
                Color gradientColor = ((Color)baseColor).gamma;

                if (m_LeftGradient != null)
                    m_LeftGradient.style.backgroundColor = gradientColor;
                if (m_RightGradient != null)
                    m_RightGradient.style.backgroundColor = gradientColor;

                if (m_LeftAlphaGradient != null)
                {
                    var leftAlphaTex = ColorPicker.GetGradientTextureWithAlpha0To1();
                    m_LeftAlphaGradient.style.backgroundImage = leftAlphaTex;
                    m_LeftAlphaGradient.style.unityBackgroundImageTintColor = color;
                }

                if (m_RightAlphaGradient != null)
                {
                    var rightAlphaTex = ColorPicker.GetGradientTextureWithAlpha1To0();
                    m_RightAlphaGradient.style.backgroundImage = rightAlphaTex;
                    m_RightAlphaGradient.style.unityBackgroundImageTintColor = color;
                }
            }

            if (m_ColorElement != null)
                m_ColorElement.style.backgroundColor = color;
        }

        void OnEyeDropperMove(TimerState state)
        {
            var pickerColor = EyeDropper.GetPickedColor();
            if (pickerColor != value)
            {
                pickerColor.a = rawValue.a;
                UpdateColorProperties(pickerColor);
            }
        }

        void OnAttach(AttachToPanelEvent evt)
        {
            UpdateColorProperties(value);
        }

        protected override void UpdateMixedValueContent()
        {
            m_ColorElement.EnableInClassList(mixedValueColorUssClassName, showMixedValue);

            if (showMixedValue)
            {
                mixedValueLabel.style.display = DisplayStyle.Flex;
                m_GradientContainer.style.display = DisplayStyle.None;
                m_AlphaGradientContainer.style.display = DisplayStyle.None;
                m_AlphaElement.style.display = DisplayStyle.None;
                m_HDRLabel.style.display = DisplayStyle.None;
            }
            else
            {
                mixedValueLabel.style.display = DisplayStyle.None;
                m_GradientContainer.style.display = hdr ? DisplayStyle.Flex : DisplayStyle.None;
                m_AlphaGradientContainer.style.display = hdr ? DisplayStyle.Flex : DisplayStyle.None;
                m_AlphaElement.style.display = showAlpha ? DisplayStyle.Flex : DisplayStyle.None;
                m_HDRLabel.style.display = hdr ? DisplayStyle.Flex : DisplayStyle.None;

                UpdateColorProperties(value);
            }
        }
    }
}
