// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

using Unity.CommandStateObserver;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    class TextAlignmentFieldBuilder : ICustomPropertyFieldBuilder<TextAlignment>
    {
        /// <summary>
        /// The class name for this TextAlignment Fields.
        /// </summary>
        public static readonly string ussClassName = "ge-text-alignment-field";

        ToggleButtonStrip m_ToggleButtonStrip;

        /// <inheritdoc />
        public bool UpdateDisplayedValue(TextAlignment value)
        {
            m_ToggleButtonStrip.SetValueWithoutNotify(value.ToString());

            return true;
        }

        public void SetMixed()
        {
            m_ToggleButtonStrip.showMixedValue = true;
        }

        void OnValueChanged(ChangeEvent<string> e)
        {
            Enum.TryParse(e.previousValue, out TextAlignment previous);
            Enum.TryParse(e.newValue, out TextAlignment newValue);

            using (var ee = ChangeEvent<TextAlignment>.GetPooled(previous, newValue))
            {
                ee.target = m_ToggleButtonStrip;
                m_ToggleButtonStrip.SendEvent(ee);
            }

            e.StopPropagation();
        }

        /// <inheritdoc />
        public VisualElement Build(ICommandTarget commandTargetView, string label, string fieldTooltip, IEnumerable<object> objs, string propertyName)
        {
            m_ToggleButtonStrip = new ToggleButtonStrip() { label = ObjectNames.NicifyVariableName(propertyName), choices = Enum.GetNames(typeof(TextAlignment)) };
            m_ToggleButtonStrip.RegisterCallback<ChangeEvent<string>>(OnValueChanged);
            m_ToggleButtonStrip.AddToClassList(ussClassName);

            return m_ToggleButtonStrip;
        }
    }
}
