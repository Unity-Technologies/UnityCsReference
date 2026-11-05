// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.U2D.Physics.Editor
{
    // The UITK 32-bit layer mask field the drawers bind to a 64-bit PhysicsMask.bitMask.
    // A LayerMaskField holds an int, and the binding writes an int field into a ulong property through intValue, which copies bit 31 into the upper 32 bits.
    // This wraps a LayerMaskField and exposes its value as a ulong, so the binding writes ulongValue and the layers are stored zero-extended in one write.
    internal sealed class LayerMaskField32 : BindableElement, INotifyValueChanged<ulong>, IMixedValueSupport
    {
        public LayerMaskField32(string label)
        {
            m_Field = new LayerMaskField(label);
            m_Field.AddToClassList(LayerMaskField.alignedFieldUssClassName);
            m_Field.RegisterValueChangedCallback(OnFieldValueChanged);
            Add(m_Field);
        }

        public void SetValueWithoutNotify(ulong newValue) => m_Field.SetValueWithoutNotify((int)(uint)newValue);

        // Forward the inner int change as a ulong change, zero-extended, for the binding to write.
        // The int change stops here so nothing above reads it as the mask's value.
        private void OnFieldValueChanged(ChangeEvent<int> evt)
        {
            evt.StopPropagation();

            using var changeEvent = ChangeEvent<ulong>.GetPooled((uint)evt.previousValue, (uint)evt.newValue);
            changeEvent.target = this;
            SendEvent(changeEvent);
        }

        public ulong value
        {
            get => (uint)m_Field.value;
            set
            {
                var previousValue = this.value;
                SetValueWithoutNotify(value);

                if (previousValue == this.value)
                    return;

                using var changeEvent = ChangeEvent<ulong>.GetPooled(previousValue, this.value);
                changeEvent.target = this;
                SendEvent(changeEvent);
            }
        }

        public bool showMixedValue
        {
            get => m_Field.showMixedValue;
            set => m_Field.showMixedValue = value;
        }

        #region Internal

        private readonly LayerMaskField m_Field;

        #endregion
    }
}
