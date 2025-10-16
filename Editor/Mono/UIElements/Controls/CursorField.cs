// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using Cursor = UnityEngine.UIElements.Cursor;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Makes a field for entering Cursor.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal class CursorField : BaseField<Cursor>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BaseField<Cursor>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<Cursor>.UxmlSerializedData.Register();
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), Array.Empty<UxmlAttributeNames>(), true);
            }

            public override object CreateInstance() => new CursorField();
        }

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
        public CursorField() : this(null) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        public CursorField(string label) : base(label, null)
        {
            m_ObjectField = new ObjectField() { classList = { objectFieldUssClassName }};
            m_ObjectField.objectType = typeof(Texture2D);
            m_ObjectField.RegisterValueChangedCallback(OnObjectValueChange);

            visualInput.AddToClassList(inputUssClassName);
            visualInput.Add(m_ObjectField);
        }

        void OnObjectValueChange(ChangeEvent<Object> evt)
        {
            value = new Cursor { texture = evt.newValue as Texture2D };
            evt.StopImmediatePropagation();
        }

        public override void SetValueWithoutNotify(Cursor newValue)
        {
            m_ObjectField.SetValueWithoutNotify(newValue.texture);
            base.SetValueWithoutNotify(newValue);
        }
    }
}
