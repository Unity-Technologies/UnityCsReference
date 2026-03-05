// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Makes a field for editing a <see cref="LoadableReference"/>.
    /// </summary>
    [Icon("UIToolkit/Icons/ObjectField.png")]
    [VisibleToOtherModules("UnityEditor.ContentLoadModule")]
    internal class LoadableReferenceField : BaseField<LoadableReference>
    {
        private ObjectField m_ObjectField;

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-loadable-reference-field";

        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";

        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        /// <summary>
        /// Initializes and returns an instance of LoadableReferenceField.
        /// </summary>
        public LoadableReferenceField()
            : this((string)null) {}

        /// <summary>
        /// Initializes and returns an instance of LoadableReferenceField.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        public LoadableReferenceField(string label)
            : this(label, null) {}

        /// <summary>
        /// Initializes and returns an instance of LoadableReferenceField.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        /// <param name="objectType">The type of objects that can be assigned. When null, any UnityEngine.Object is allowed.</param>
        public LoadableReferenceField(string label, Type objectType)
            : base(label, null)
        {
            m_ObjectField = new ObjectField
            {
                allowSceneObjects = false,
                objectType = objectType ?? typeof(Object)
            };
            m_ObjectField.RegisterValueChangedCallback(OnObjectFieldValueChanged);

            visualInput.Add(m_ObjectField);
            visualInput.AddToClassList(inputUssClassName);
            labelElement.AddToClassList(labelUssClassName);
            AddToClassList(ussClassName);
        }

        private void OnObjectFieldValueChanged(ChangeEvent<Object> evt)
        {
            LoadableReference newLoadableRef = LoadableReferenceEditorUtility.ObjectToLoadableReference(evt.newValue);
            if (evt.newValue != null && !newLoadableRef.isValid)
            {
                UnityEngine.Debug.LogWarning(L10n.Tr("The selected object cannot be used as a LoadableReference."));
                m_ObjectField.SetValueWithoutNotify(LoadableReferenceEditorUtility.LoadableReferenceToObject(value));
                return;
            }

            value = newLoadableRef;
            evt.StopImmediatePropagation();
        }

        public override void SetValueWithoutNotify(LoadableReference newValue)
        {
            base.SetValueWithoutNotify(newValue);

            var obj = LoadableReferenceEditorUtility.LoadableReferenceToObject(value);
            m_ObjectField.SetValueWithoutNotify(obj);
        }
    }
}
