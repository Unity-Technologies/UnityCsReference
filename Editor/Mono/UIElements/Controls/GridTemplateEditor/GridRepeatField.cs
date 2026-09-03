// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitFramework not yet converted
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.UIElements;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.UIElements
{
    [NoAutoStaticsCleanup] // Immutable repeat-mode lookup table; safe to persist across reloads.
    internal class GridRepeatField : VisualElement
    {
        static readonly List<GridTemplateRepeatMode> k_Modes = new()
        {
            GridTemplateRepeatMode.None, GridTemplateRepeatMode.AutoFill,
            GridTemplateRepeatMode.AutoFit, GridTemplateRepeatMode.Count
        };

        static string ModeLabel(GridTemplateRepeatMode m) => m switch
        {
            GridTemplateRepeatMode.None => "none",
            GridTemplateRepeatMode.AutoFill => "auto-fill",
            GridTemplateRepeatMode.AutoFit => "auto-fit",
            GridTemplateRepeatMode.Count => "repeat",
            _ => m.ToString()
        };

        readonly TextField m_Value;
        readonly PopupField<GridTemplateRepeatMode> m_Mode;
        int m_Count = 2;                 // remembered count across mode switches
        bool m_Notify = true;

        public event Action changed;

        public GridRepeatField()
        {
            AddToClassList(ussClassName);
            style.flexDirection = FlexDirection.Row;

            m_Value = new TextField { isDelayed = true };
            m_Value.AddToClassList("unity-style-field__text-field");
            m_Value.RegisterValueChangedCallback(OnTextChanged);
            Add(m_Value);

            var unitContainer = new VisualElement();
            unitContainer.AddToClassList("unity-style-field__options-popup-container");
            m_Mode = new PopupField<GridTemplateRepeatMode>(k_Modes, GridTemplateRepeatMode.None, ModeLabel, ModeLabel);
            m_Mode.AddToClassList("unity-style-field__options-popup");
            m_Mode.style.width = 64; // fits "auto-fill" / "auto-fit"
            m_Mode.RegisterValueChangedCallback(OnModeChanged);
            unitContainer.Add(m_Mode);
            Add(unitContainer);

            UpdateValueDisplay();
        }

        public GridTemplateRepeatMode mode => m_Mode.value;
        public int count => Math.Max(1, m_Count);

        public void SetValueWithoutNotify(GridTemplateRepeatMode repeatMode, int repeatCount)
        {
            m_Notify = false;
            try
            {
                m_Count = Math.Max(1, repeatCount);
                m_Mode.SetValueWithoutNotify(repeatMode);
                UpdateValueDisplay();
            }
            finally
            {
                m_Notify = true;
            }
        }

        void OnTextChanged(ChangeEvent<string> evt)
        {
            evt.StopPropagation();
            if (int.TryParse(evt.newValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n >= 1)
            {
                m_Count = n;
                // Entering a count means the user wants a fixed repeat.
                if (m_Mode.value != GridTemplateRepeatMode.Count)
                    m_Mode.SetValueWithoutNotify(GridTemplateRepeatMode.Count);
                UpdateValueDisplay();
                Notify();
            }
            else
            {
                UpdateValueDisplay(); // restore the last good display
            }
        }

        void OnModeChanged(ChangeEvent<GridTemplateRepeatMode> evt)
        {
            evt.StopPropagation();
            UpdateValueDisplay();
            Notify();
        }

        void UpdateValueDisplay()
        {
            // Only the "repeat" mode has a count; other modes clear the value (count is kept in memory).
            m_Value.SetValueWithoutNotify(m_Mode.value == GridTemplateRepeatMode.Count
                ? m_Count.ToString(CultureInfo.InvariantCulture)
                : string.Empty);
        }

        void Notify()
        {
            if (m_Notify)
                changed?.Invoke();
        }

        public static readonly string ussClassName = "grid-repeat-field";
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
