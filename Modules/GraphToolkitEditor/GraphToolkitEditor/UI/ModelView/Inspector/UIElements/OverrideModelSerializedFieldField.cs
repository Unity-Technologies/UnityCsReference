// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.GraphToolkit.CSO;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    [UnityRestricted]
    internal class OverrideModelSerializedFieldField<T> : ModelSerializedFieldField<T>
    {
        /// <summary>
        /// The USS class added to a <see cref="OverrideModelSerializedFieldField{T}"/>.
        /// </summary>
        public static new readonly string ussClassName = "ge-override-model-serialized-field";

        /// <summary>
        /// The USS class added to a <see cref="OverrideModelSerializedFieldField{T}"/> overrides.
        /// </summary>
        public static readonly string overrideUssClassName = ussClassName.WithUssElement("override");

        Label m_Label;
        Toggle m_OverrideToggle;
        protected Func<Model, bool> m_OverrideGetter;

        public OverrideModelSerializedFieldField(ICommandTarget commandTarget, IReadOnlyList<Model> models, IReadOnlyList<object> inspectedObjects, FieldInfo inspectedField, FieldInfo overrideField, string fieldTooltip, string displayName)
            : base(commandTarget, models, inspectedObjects, inspectedField, fieldTooltip, displayName)
        {
            m_OverrideGetter = m => (bool)overrideField.GetValue(m);

            m_OverrideToggle = new Toggle();
            m_OverrideToggle.RegisterCallback<ChangeEvent<bool>>(evt =>
                commandTarget.Dispatch(new SetInspectedModelFieldCommand(evt.newValue, inspectedObjects, overrideField)));

            m_OverrideToggle.AddToClassList(overrideUssClassName);

            m_Label = new Label(Label);
            m_Label.AddToClassList(labelUssClassName);
            Insert(0, m_Label);
            Insert(1, m_OverrideToggle);
            if (LabelElement != null)
            {
                LabelElement.text = String.Empty;
                LabelElement.RemoveFromClassList(labelUssClassName);
            }

            AddToClassList(ussClassName);
            LabelElement = m_Label;
        }

        public override void UpdateDisplayedValue()
        {
            base.UpdateDisplayedValue();

            bool overrideValue = m_OverrideGetter(Models[0]);
            m_OverrideToggle.SetValueWithoutNotify(overrideValue);

            Field.style.display = overrideValue ? StyleKeyword.Null : DisplayStyle.None;
        }
    }
}
