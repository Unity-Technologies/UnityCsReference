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

        public HierarchyViewItemName()
        {
            AddToClassList(k_StyleName);

            focusable = true;
            delegatesFocus = false;

            Add(Label);
            Add(TextField);

            TextField.selectAllOnFocus = true;
            TextField.selectAllOnMouseUp = false;
            TextField.style.display = DisplayStyle.None;

            TextField.RegisterCallback<MouseUpEvent>(OnMouseUpEvent);
            TextField.RegisterCallback<KeyDownEvent>(OnKeyDownEvent);
            TextField.RegisterCallback<BlurEvent>(OnBlurEvent);
        }

        public void BeginRename()
        {
            if (IsRenaming)
                return;

            IsRenaming = true;
            delegatesFocus = true;

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
