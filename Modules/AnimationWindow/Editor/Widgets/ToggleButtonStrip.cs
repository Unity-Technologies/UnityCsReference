// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.Animations.AnimationWindow.Widgets
{
    interface IToggleButtonItem
    {
        string Name { get; }
        string Tooltip { get; }
    }

    // Wrapper around ToggleButtonGroup that deals with list of items instead of indices for convenience
    [UxmlElement]
    partial class ToggleButtonStrip : ToggleButtonGroup
    {
        List<IToggleButtonItem> m_Items;
        int m_NumItems;

        public IEnumerable<IToggleButtonItem> items
        {
            get => m_Items;
            set
            {
                Clear();
                if (value == null)
                    return;

                m_NumItems = 0;
                foreach (var item in value)
                {
                    Add(new Button(){ text = item.Name, tooltip = item.Tooltip });
                    ++m_NumItems;
                }

                m_Items = new List<IToggleButtonItem>(value);
            }
        }

        public IToggleButtonItem Value
        {
            get => GetItem(value);
            set
            {
                var itemIndex = GetItemIndex(value);
                if (itemIndex < 0)
                    return;

                this.value = CreateToggleState(itemIndex);
            }
        }

        public ToggleButtonStrip() : this(null, null) {}

        public ToggleButtonStrip(string label, IList<IToggleButtonItem> items) : base(label)
        {
            this.items = items;
        }

        public void SetValueWithoutNotify(IToggleButtonItem newValue) =>
            SetValueWithoutNotify(GetItemIndex(newValue));

        public void SetValueWithoutNotify(int index)
        {
            if (index < 0)
                return;

            base.SetValueWithoutNotify(CreateToggleState(index));
        }

        public void RegisterValueChangedCallback(EventCallback<ChangeEvent<IToggleButtonItem>> callback)
        {
            INotifyValueChangedExtensions.RegisterValueChangedCallback(this, evt =>
            {
                callback(ChangeEvent<IToggleButtonItem>.GetPooled(
                    GetItem(evt.previousValue),
                    GetItem(evt.newValue)));
            });
        }

        ToggleButtonGroupState CreateToggleState(int selectedItem)
        {
            var state = new ToggleButtonGroupState(0, m_NumItems);

            if (selectedItem >= 0 && selectedItem < m_NumItems)
                state[selectedItem] = true;

            return state;
        }

        int GetItemIndex(IToggleButtonItem item)
        {
            if (m_Items == null)
                return -1;

            var index = 0;
            foreach (var candidate in m_Items)
            {
                if (item.Name == candidate.Name)
                    return index;

                ++index;
            }

            return -1;
        }

        IToggleButtonItem GetItem(ToggleButtonGroupState toggleState)
        {
            if (m_Items == null)
                return null;

            var itemIndex = 0;
            foreach (var item in m_Items)
            {
                if (toggleState[itemIndex])
                    return item;

                ++itemIndex;
            }

            return null;
        }
    }
}
