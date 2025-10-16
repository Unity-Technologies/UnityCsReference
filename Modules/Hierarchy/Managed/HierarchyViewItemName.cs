// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.Hierarchy
{
    [UxmlElement, VisibleToOtherModules("UnityEditor.HierarchyModule")]
    partial class HierarchyViewItemName : VisualElement
    {
        internal const string k_StyleName = "hierarchy-item__name";

        public string Text
        {
            get => Label.text;
            set => Label.text = value;
        }

        internal bool IsRenaming { get; set; }

        public event Action OnBeginRename;
        public event Action<string, bool> OnEndRename;

        public Label Label { get; } = new();
        TextField TextField { get; } = new();

        bool m_PrewarmControl;

        public HierarchyViewItemName()
        {
            AddToClassList(k_StyleName);

            focusable = true;
            delegatesFocus = false;
            m_PrewarmControl = false;

            Add(Label);
            Add(TextField);

            TextField.selectAllOnFocus = true;
            TextField.selectAllOnMouseUp = false;
            TextField.style.display = DisplayStyle.None;

            TextField.RegisterCallback<MouseUpEvent>(OnMouseUpEvent);
            TextField.RegisterCallback<KeyDownEvent>(OnInterceptKeyDownEvent, TrickleDown.TrickleDown);
            TextField.RegisterCallback<KeyDownEvent>(OnKeyDownEvent, TrickleDown.NoTrickleDown);
            TextField.RegisterCallback<BlurEvent>(OnBlurEvent);
        }

        public void BeginRename()
        {
            if (IsRenaming)
                return;

            IsRenaming = true;
            delegatesFocus = true;
            m_PrewarmControl = true;

            Label.style.display = DisplayStyle.None;
            TextField.style.display = DisplayStyle.Flex;

            TextField.value = Text;
            TextField.Q<TextElement>().Focus();

            OnBeginRename?.Invoke();
        }

        public void CancelRename()
        {
            if (IsRenaming)
                EndRename(true);
        }

        void EndRename(bool canceled = false)
        {
            IsRenaming = false;
            delegatesFocus = false;
            m_PrewarmControl = false;

            schedule.Execute(Focus);

            TextField.style.display = DisplayStyle.None;
            Label.style.display = DisplayStyle.Flex;

            // When the rename is canceled, the label keep its current value.
            if (!canceled && !string.IsNullOrEmpty(TextField.value))
                Label.text = TextField.value;

            OnEndRename?.Invoke(Text, canceled);
        }

        void OnMouseUpEvent(MouseUpEvent evt)
        {
            if (!IsRenaming)
                return;

            TextField.Q<TextElement>().Focus();
            evt.StopPropagation();
        }

        void OnInterceptKeyDownEvent(KeyDownEvent evt)
        {
            if (!m_PrewarmControl)
                return;

            if (evt.keyCode == KeyCode.None)
            {
                // The OS sends multiple keyboard-related events on every key press.
                // First, it sends the physical key pressed in the form of a key code. Then, it sends
                // whatever character should be printed out as a result of that key press. Since keyboard
                // shortcuts are processed based on the key code, if we give focus to a text field immediately
                // it will immediately receive a key down event with a character. In our case, this is bad
                // for 2 reasons:
                //   1. On mac, the default shortcut to rename is Enter/Return which will instantly validate the
                //      rename and result in the text field immediately losing focus again.
                //   2. To simplify the rename, the text field is created with all its contents selected, so any
                //      key bound to the Rename command will replace the current name of the item.
                //
                // So we eat all characters that are received without a keycode when the control gains focus
                // to avoid those situations.
                evt.StopPropagation();
            }
            else
            {
                m_PrewarmControl = false;
            }
        }

        void OnKeyDownEvent(KeyDownEvent evt)
        {
            if (IsRenaming && evt.keyCode == KeyCode.Escape)
                EndRename(true);

            evt.StopPropagation();
        }

        void OnBlurEvent(BlurEvent evt)
        {
            if (!IsRenaming)
                return;

            EndRename();
        }
    }
}
