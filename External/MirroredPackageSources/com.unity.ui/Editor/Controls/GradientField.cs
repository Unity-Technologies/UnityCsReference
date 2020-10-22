using UnityEngine;
using UnityEditorInternal;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Makes a field for editing an <see cref="Gradient"/>.
    /// </summary>
    public class GradientField : BaseField<Gradient>
    {
        static readonly GradientColorKey k_WhiteKeyBegin = new GradientColorKey(Color.white, 0);
        static readonly GradientColorKey k_WhiteKeyEnd = new GradientColorKey(Color.white, 1);
        static readonly GradientAlphaKey k_AlphaKeyBegin = new GradientAlphaKey(1, 0);
        static readonly GradientAlphaKey k_AlphaKeyEnd = new GradientAlphaKey(1, 1);
        /// <summary>
        /// Instantiates a <see cref="GradientField"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<GradientField, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="GradientField"/>.
        /// </summary>
        public new class UxmlTraits : BaseField<Gradient>.UxmlTraits {}

        private bool m_ValueNull;
        /// <summary>
        /// The <see cref="Gradient"/> currently being exposed by the field.
        /// </summary>
        /// <remarks>
        /// Note that changing this will not trigger a change event to be sent.
        /// </remarks>
        public override Gradient value
        {
            get
            {
                if (m_ValueNull) return null;

                return GradientCopy(rawValue);
            }
            set
            {
                if (value != null || !m_ValueNull)  // let's not reinitialize an initialized gradient
                {
                    using (ChangeEvent<Gradient> evt = ChangeEvent<Gradient>.GetPooled(rawValue, value))
                    {
                        evt.target = this;
                        SetValueWithoutNotify(value);
                        SendEvent(evt);
                    }
                }
            }
        }

        /// <summary>
        /// The color space currently used by the field.
        /// </summary>
        public ColorSpace colorSpace { get; set; }

        internal static Gradient GradientCopy(Gradient other)
        {
            Gradient gradientCopy = new Gradient();
            gradientCopy.colorKeys = other.colorKeys;
            gradientCopy.alphaKeys = other.alphaKeys;
            gradientCopy.mode = other.mode;
            return gradientCopy;
        }

        /// <summary>
        /// USS class name for elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-gradient-field";
        /// <summary>
        /// USS class name for labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name for input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";
        /// <summary>
        /// USS class name for the content for the gradient visual in the <see cref="GradientField"/> element.
        /// </summary>
        public static readonly string contentUssClassName = ussClassName + "__content";

        /// <summary>
        /// USS class name for border elements in elements of this type.
        /// </summary>
        public static readonly string borderUssClassName = ussClassName + "__border";

        VisualElement m_GradientTextureImage;
        readonly Background m_DefaultBackground = new Background();

        /// <summary>
        /// Constructor.
        /// </summary>
        public GradientField()
            : this(null) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        public GradientField(string label)
            : base(label, null)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);

            m_GradientTextureImage = new VisualElement() { pickingMode = PickingMode.Ignore };
            m_GradientTextureImage.AddToClassList(contentUssClassName);
            visualInput.Add(m_GradientTextureImage);

            VisualElement borderElement = new VisualElement() { name = "unity-border", pickingMode = PickingMode.Ignore };
            borderElement.AddToClassList(borderUssClassName);
            visualInput.Add(borderElement);
            rawValue = new Gradient();
        }

        protected override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt == null)
            {
                return;
            }

            var showGradientPicker = false;
            KeyDownEvent kde = (evt as KeyDownEvent);
            if (kde != null)
            {
                if ((kde.keyCode == KeyCode.Space) ||
                    (kde.keyCode == KeyCode.KeypadEnter) ||
                    (kde.keyCode == KeyCode.Return))
                {
                    showGradientPicker = true;
                }
            }
            else if ((evt as MouseDownEvent)?.button == (int)MouseButton.LeftMouse)
            {
                var mde = (MouseDownEvent)evt;
                if (visualInput.ContainsPoint(visualInput.WorldToLocal(mde.mousePosition)))
                {
                    showGradientPicker = true;
                }
            }

            if (showGradientPicker)
            {
                ShowGradientPicker();
            }
            else if (evt.eventTypeId == DetachFromPanelEvent.TypeId())
                OnDetach();
            else if (evt.eventTypeId == AttachToPanelEvent.TypeId())
                OnAttach();
        }

        void OnDetach()
        {
            if (style.backgroundImage.value.texture != null)
            {
                Object.DestroyImmediate(style.backgroundImage.value.texture);
                style.backgroundImage = new Background();
            }
        }

        void OnAttach()
        {
            if (panel != null)
                UpdateGradientTexture();
        }

        void ShowGradientPicker()
        {
            GradientPicker.Show(rawValue, true, colorSpace, OnGradientChanged);
        }

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();
            UpdateGradientTexture();
        }

        void UpdateGradientTexture()
        {
            if (m_ValueNull || showMixedValue)
            {
                visualInput.style.backgroundImage = m_DefaultBackground;
            }
            else
            {
                Texture2D gradientTexture = UnityEditorInternal.GradientPreviewCache.GenerateGradientPreview(value, resolvedStyle.backgroundImage.texture, colorSpace);
                m_GradientTextureImage.style.backgroundImage = gradientTexture;

                IncrementVersion(VersionChangeType.Repaint); // since the Texture2D object can be reused, force dirty because the backgroundImage change will only trigger the Dirty if the Texture2D objects are different.
            }
        }

        void OnGradientChanged(Gradient newValue)
        {
            value = newValue;

            GradientPreviewCache.ClearCache(); // needed because GradientEditor itself uses the cache and will no invalidate it on changes.
            IncrementVersion(VersionChangeType.Repaint);
        }

        public override void SetValueWithoutNotify(Gradient newValue)
        {
            m_ValueNull = newValue == null;
            if (newValue != null)
            {
                rawValue.colorKeys = newValue.colorKeys;
                rawValue.alphaKeys = newValue.alphaKeys;
                rawValue.mode = newValue.mode;
            }
            else // restore the internal gradient to the default state.
            {
                rawValue.colorKeys = new[] { k_WhiteKeyBegin, k_WhiteKeyEnd };
                rawValue.alphaKeys = new[] { k_AlphaKeyBegin, k_AlphaKeyEnd };
                rawValue.mode = GradientMode.Blend;
            }
            UpdateGradientTexture();
        }

        protected override void UpdateMixedValueContent()
        {
            if (showMixedValue)
            {
                visualInput.style.backgroundImage = m_DefaultBackground;
                visualInput.Add(mixedValueLabel);
            }
            else
            {
                UpdateGradientTexture();
                mixedValueLabel.RemoveFromHierarchy();
            }
        }
    }
}
