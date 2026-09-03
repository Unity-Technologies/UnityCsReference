// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitFramework not yet converted
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Makes a field for entering Font.
    /// </summary>
    [UxmlElement]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal partial class FontField : BaseField<Font>
    {
        /// <summary>
        /// USS class name of the object field in elements of this type.
        /// </summary>
        public static readonly string objectFieldUssClassName = "unity-multi-type-field__object-field";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = "unity-multi-type-field__visual-input";

        readonly ObjectField m_ObjectField;

        public ObjectField objectField => m_ObjectField;

        /// <summary>
        /// Constructor.
        /// </summary>
        public FontField()
            : this(null) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        public FontField(string label)
            : base(label, null)
        {
            m_ObjectField = new ObjectField().WithClassList(objectFieldUssClassName);
            m_ObjectField.objectType = typeof(Font);
            m_ObjectField.RegisterValueChangedCallback(OnObjectValueChange);

            visualInput.AddToClassList(inputUssClassName);
            visualInput.Add(m_ObjectField);
        }

        void OnObjectValueChange(ChangeEvent<Object> evt)
        {
            value = evt.newValue as Font;
            evt.StopImmediatePropagation();
        }

        public override void SetValueWithoutNotify(Font newValue)
        {
            m_ObjectField.SetValueWithoutNotify(newValue);
            base.SetValueWithoutNotify(newValue);
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
