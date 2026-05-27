// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Properties;
using UnityEngine;
using UnityEditorInternal;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Makes a field for editing an <see cref="Gradient"/>. For more information, refer to [[wiki:UIE-uxml-element-GradientField|UXML element GradientField]].
    /// </summary>
    [Icon("UIToolkit/Icons/GradientField.png")]
    public partial class GradientField : BaseField<Gradient>
    {
        internal static readonly BindingId colorSpaceProperty = nameof(colorSpace);
        internal static readonly BindingId hdrProperty = nameof(hdr);

        static readonly GradientColorKey k_WhiteKeyBegin = new GradientColorKey(Color.white, 0);
        static readonly GradientColorKey k_WhiteKeyEnd = new GradientColorKey(Color.white, 1);
        static readonly GradientAlphaKey k_AlphaKeyBegin = new GradientAlphaKey(1, 0);
        static readonly GradientAlphaKey k_AlphaKeyEnd = new GradientAlphaKey(1, 1);

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BaseField<Gradient>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<Gradient>.UxmlSerializedData.Register();
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), Array.Empty<UxmlAttributeNames>(), true);
            }

            public override object CreateInstance() => new GradientField();
        }

        private bool m_ValueNull;
        // The GradientPicker will change the values in the arrays directly and will send commands to keep everything
        // in sync. Here, we're using this flag to force the value to be set when it is technically the same.
        private bool m_ForceSendEvent = false;
        private int m_PickerUndoGroup;
        private bool m_PickerHasChanges;

        /// <summary>
        /// The <see cref="Gradient"/> currently being exposed by the field.
        /// </summary>
        /// <remarks>
        /// __Note__: Changing this doesn't trigger sending a change event.
        /// </remarks>
        public override Gradient value
        {
            get => m_ValueNull ? null : GradientCopy(rawValue);
            set
            {
                if (value != null || !m_ValueNull)  // let's not reinitialize an initialized gradient
                {
                    base.value = value;
                }
            }
        }

        private ColorSpace m_ColorSpace;

        /// <summary>
        /// The color space currently used by the field.
        /// </summary>
        [CreateProperty]
        public ColorSpace colorSpace
        {
            get => m_ColorSpace;
            set
            {
                if (m_ColorSpace == value)
                    return;
                m_ColorSpace = value;
                NotifyPropertyChanged(colorSpaceProperty);
            }
        }

        private bool m_Hdr;

        /// <summary>
        /// If true, treats the color as an HDR value. If false, treats the color as a standard LDR value.
        /// </summary>
        [CreateProperty]
        public bool hdr
        {
            get => m_Hdr;
            set
            {
                if (m_Hdr == value)
                    return;
                m_Hdr = value;
                NotifyPropertyChanged(hdrProperty);
            }
        }

        internal static Gradient GradientCopy(Gradient other)
        {
            Gradient gradientCopy = new Gradient();
            gradientCopy.colorKeys = other.colorKeys;
            gradientCopy.alphaKeys = other.alphaKeys;
            gradientCopy.mode = other.mode;
            gradientCopy.colorSpace = other.colorSpace;
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
        /// USS class name for the background of the gradient visual in the <see cref="GradientField"/> element.
        /// </summary>
        public static readonly string backgroundUssClassName = ussClassName + "__background";

        /// <summary>
        /// USS class name for border elements in elements of this type.
        /// </summary>
        [Obsolete("borderUssClass is not used anymore", true)]
        public static readonly string borderUssClassName = ussClassName + "__border";

        VisualElement m_GradientTextureImage;
        readonly Background m_DefaultBackground = new Background();

        bool isShowingGradientPicker => GradientPicker.visible && rawValue != null && ReferenceEquals(GradientPicker.gradient, rawValue);

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

            var background = new VisualElement { pickingMode = PickingMode.Ignore };
            background.AddToClassList(backgroundUssClassName);
            visualInput.Add(background);

            m_GradientTextureImage = new VisualElement { pickingMode = PickingMode.Ignore };
            m_GradientTextureImage.AddToClassList(contentUssClassName);
            background.Add(m_GradientTextureImage);

            rawValue = new Gradient();
        }

        [EventInterest(typeof(KeyDownEvent), typeof(MouseDownEvent),
            typeof(DetachFromPanelEvent), typeof(AttachToPanelEvent))]
        protected override void HandleEventBubbleUp(EventBase evt)
        {
            base.HandleEventBubbleUp(evt);

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
                evt.StopPropagation();
            }
            else if (evt.eventTypeId == DetachFromPanelEvent.TypeId())
                OnDetach();
            else if (evt.eventTypeId == AttachToPanelEvent.TypeId())
                OnAttach();
        }

        [EventInterest(EventInterestOptions.Inherit)]
        [Obsolete("ExecuteDefaultAction override has been removed because default event handling was migrated to HandleEventBubbleUp. Please use HandleEventBubbleUp.", false)]
        protected override void ExecuteDefaultAction(EventBase evt)
        {
        }

        [EventInterest(EventInterestOptions.Inherit)]
        [Obsolete("ExecuteDefaultActionAtTarget override has been removed because default event handling was migrated to HandleEventBubbleUp. Please use HandleEventBubbleUp.", false)]
        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
        }

        void OnDetach()
        {
            if (isShowingGradientPicker)
                GradientPicker.CloseWindow(false);

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
            // Re-clicking the field while the picker is already shown for it must NOT advance the
            // undo group or reset our bookkeeping. GradientPicker.Show() suppresses the previous
            // OnPickerClosed in that case, so overwriting m_PickerUndoGroup here would leave the
            // earlier in-session entries permanently un-collapsed on the eventual close.
            if (!isShowingGradientPicker)
            {
                Undo.IncrementCurrentGroup();
                m_PickerUndoGroup = Undo.GetCurrentGroup();
                m_PickerHasChanges = false;
            }
            GradientPicker.Show(rawValue, hdr, colorSpace, OnGradientChanged, OnPickerClosed);
        }

        void OnPickerClosed()
        {
            // Increment and name a new group before collapsing so that any redo entries created
            // by mid-session undos are truncated from the stack. (UUM-142114)
            if (m_PickerHasChanges)
            {
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("Modify Gradient");
            }
            Undo.CollapseUndoOperations(m_PickerUndoGroup);
        }

        // We dont want Undo operations to be combined when the picker is still open so we will handle the collapsing. (UUM-142114)
        internal override void RegisterEditingCallbacks() {}
        internal override void UnregisterEditingCallbacks() {}

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
                Texture2D gradientTexture = UnityEditorInternal.GradientPreviewCache.GenerateGradientPreview(value, resolvedStyle.backgroundImage.texture, colorSpace == ColorSpace.Linear);
                m_GradientTextureImage.style.backgroundImage = gradientTexture;

                IncrementVersion(VersionChangeType.Repaint); // since the Texture2D object can be reused, force dirty because the backgroundImage change will only trigger the Dirty if the Texture2D objects are different.
            }
        }

        void OnGradientChanged(Gradient newValue)
        {
            m_PickerHasChanges = true;
            m_ForceSendEvent = true;
            try
            {
                value = newValue;
            }
            finally
            {
                m_ForceSendEvent = false;
            }

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
                rawValue.colorSpace = newValue.colorSpace;
            }
            else // restore the internal gradient to the default state.
            {
                rawValue.colorKeys = new[] { k_WhiteKeyBegin, k_WhiteKeyEnd };
                rawValue.alphaKeys = new[] { k_AlphaKeyBegin, k_AlphaKeyEnd };
                rawValue.mode = GradientMode.Blend;
                rawValue.colorSpace = ColorSpace.Uninitialized;
            }
            UpdateGradientTexture();

            // Update the GradientPicker if it's open and not currently being interacted with. (UUM-100664)
            if (isShowingGradientPicker && GUIUtility.hotControl == 0)
            {
                GradientPicker.RefreshGradientData();
            }
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

        internal override bool EqualsCurrentValue(Gradient v)
        {
            if (m_ForceSendEvent)
                return false;

            if (v == null && m_ValueNull)
                return true;

            if (v == null ^ m_ValueNull)
                return false;

            if (rawValue.colorKeys.Length != v.colorKeys.Length ||
                rawValue.alphaKeys.Length != v.alphaKeys.Length ||
                rawValue.mode != v.mode)
                return false;

            for (var i = 0; i < rawValue.colorKeys.Length; ++i)
            {
                var current = rawValue.colorKeys[i];
                var next = v.colorKeys[i];
                if (current.color != next.color || !Mathf.Approximately(current.time, next.time))
                    return false;
            }

            for (var i = 0; i < rawValue.alphaKeys.Length; ++i)
            {
                var current = rawValue.alphaKeys[i];
                var next = v.alphaKeys[i];
                if (!Mathf.Approximately(current.alpha, next.alpha) || !Mathf.Approximately(current.time, next.time))
                    return false;
            }

            return rawValue.colorSpace == v.colorSpace;
        }
    }
}
