// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Animations.AnimationWindow.Widgets
{
    [UxmlElement]
    partial class ClipDropdownField : BaseField<IAnimationWindowClip>, IDisposable
    {
        // UXML serialization requires a string-typed attribute named "value" because no
        // UxmlAttributeConverter exists for IAnimationWindowClip.
        [UxmlAttribute("value"), HideInInspector]
        internal string valueOverride { get; set; }

        const float k_MaxHeightFraction = 0.7f;

        // Base selectors coming from BasePopupField
        const string k_UssClassNameBasePopupField = "unity-base-popup-field";
        const string k_TextUssClassNameBasePopupField = k_UssClassNameBasePopupField + "__text";
        const string k_ArrowUssClassNameBasePopupField = k_UssClassNameBasePopupField + "__arrow";
        const string k_LabelUssClassNameBasePopupField = k_UssClassNameBasePopupField + "__label";
        const string k_InputUssClassNameBasePopupField = k_UssClassNameBasePopupField + "__input";

        // Base selectors coming from PopupField
        const string k_UssClassNamePopupField = "unity-popup-field";
        const string k_LabelUssClassNamePopupField = k_UssClassNamePopupField + "__label";
        const string k_InputUssClassNamePopupField = k_UssClassNamePopupField + "__input";

        private AnimationWindowState m_State;
        private TextElement m_TextElement;

        public ClipDropdownField() : this(null)
        {
        }

        public ClipDropdownField(string label) : base(label, null)
        {
            AddToClassList(k_UssClassNameBasePopupField);
            AddToClassList(k_UssClassNamePopupField);

            labelElement.AddToClassList(k_LabelUssClassNameBasePopupField);
            labelElement.AddToClassList(k_LabelUssClassNamePopupField);

            visualInput.AddToClassList(k_InputUssClassNameBasePopupField);
            visualInput.AddToClassList(k_InputUssClassNamePopupField);

            m_TextElement = new PopupTextElement
            {
                pickingMode = PickingMode.Ignore
            };
            m_TextElement.AddToClassList(k_TextUssClassNameBasePopupField);
            m_TextElement.style.overflow = Overflow.Hidden;
            m_TextElement.style.textOverflow = TextOverflow.Ellipsis;
            m_TextElement.style.unityTextOverflowPosition = TextOverflowPosition.Middle;

            visualInput.Add(m_TextElement);

            var arrow = new VisualElement();
            arrow.AddToClassList(k_ArrowUssClassNameBasePopupField);
            arrow.pickingMode = PickingMode.Ignore;
            visualInput.Add(arrow);
        }

        public void Initialize(AnimationWindowState state)
        {
            m_State = state;

            SetValueWithoutNotify(m_State.activeClip);
            m_State.onRefresh += OnRefresh;

            OnRefresh();
        }

        public void Dispose()
        {
            if (m_State != null)
            {
                m_State.onRefresh -= OnRefresh;
            }
        }

        private List<IAnimationWindowClip> GetOrderedClipList()
        {
            var clips = new List<IAnimationWindowClip>(m_State.selection.GetClips());
            clips.Sort((clip1, clip2) => EditorUtility.NaturalCompare(clip1.name, clip2.name));
            return clips;
        }

        [EventInterest(typeof(MouseDownEvent), typeof(KeyDownEvent))]
        protected override void HandleEventBubbleUp(EventBase evt)
        {
            base.HandleEventBubbleUp(evt);

            if (evt == null)
                return;

            bool showPopup = false;

            if (evt is KeyDownEvent keyDownEvent)
            {
                if (keyDownEvent.keyCode == KeyCode.Space ||
                    keyDownEvent.keyCode == KeyCode.KeypadEnter ||
                    keyDownEvent.keyCode == KeyCode.Return)
                {
                    showPopup = true;
                }
            }
            else if (evt is MouseDownEvent mouseDownEvent)
            {
                if (mouseDownEvent.button == (int)MouseButton.LeftMouse)
                {
                    if (visualInput.ContainsPoint(visualInput.WorldToLocal(mouseDownEvent.mousePosition)))
                    {
                        showPopup = true;
                    }
                }
            }

            if (!showPopup)
                return;

            if (!enabledInHierarchy || !m_State.selection.canChangeClip)
                return;

            ShowSearchablePopup();
            evt.StopPropagation();
        }

        private void ShowSearchablePopup()
        {
            var clips = GetOrderedClipList();
            var canCreateNewClip = m_State.selection.canChangeClip;

            // Calculate available height based on Animation window size
            var availableHeight = GetAvailableHeight();

            var windowContent = new ClipDropdownWindowContent(
                clips,
                value,
                canCreateNewClip,
                availableHeight,
                OnClipSelected,
                OnCreateNewClip
            );

            PopupWindow.Show(visualInput.worldBound, windowContent);
        }

        private float GetAvailableHeight()
        {
            // Get the root visual element container height
            var root = panel?.visualTree;
            if (root != null)
            {
                float containerHeight = root.layout.height;
                float maxHeight = Mathf.Clamp(containerHeight * k_MaxHeightFraction, 200f, 600f);
                return maxHeight;
            }

            // Fallback to default
            return 300f;
        }

        private void OnClipSelected(IAnimationWindowClip clip)
        {
            if (!m_State.animEditor.DisplayUnsavedChangesDialogIfNecessary())
                return;
            value = clip;
            m_State.activeClip = value;
        }

        private void OnCreateNewClip(string suggestedName)
        {
            if (!m_State.animEditor.DisplayUnsavedChangesDialogIfNecessary())
                return;

            var newClip = m_State.selection.CreateNewClip(suggestedName);

            if (newClip != null)
            {
                value = newClip;
                m_State.activeClip = newClip;
            }
        }

        string GetClipName(IAnimationWindowClip clip)
        {
            if (!clip?.isValid ?? true)
                return "[No Clip]";

            string name = clip.name;

            if (clip.isReadOnly)
                name += " (Read-Only)";

            return name;
        }

        private void OnRefresh()
        {
            SetEnabled(!m_State.disabled && m_State.selection.canChangeClip);
            SetValueWithoutNotify(m_State.activeClip);
        }

        public override void SetValueWithoutNotify(IAnimationWindowClip newValue)
        {
            base.SetValueWithoutNotify(newValue);
            var clipName = newValue != null ? GetClipName(newValue) : String.Empty;
            m_TextElement.text = clipName;
            tooltip = clipName;
        }

        class PopupTextElement : TextElement
        {
            protected internal override Vector2 DoMeasure(float desiredWidth, MeasureMode widthMode, float desiredHeight, MeasureMode heightMode)
            {
                var textToMeasure = text;
                if (string.IsNullOrEmpty(textToMeasure))
                {
                    textToMeasure = " ";
                }

                return MeasureTextSize(textToMeasure, desiredWidth, widthMode, desiredHeight, heightMode);
            }
        }
    }

}
