// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using Unity.Loading;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Makes a field for editing a <see cref="LoadableObjectId"/>.
    /// </summary>
    [Icon("UIToolkit/Icons/ObjectField.png")]
    [VisibleToOtherModules("UnityEditor.ContentLoadModule")]
    internal class LoadableObjectIdField : BaseField<LoadableObjectId>
    {
        private ObjectField m_ObjectField;

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-loadable-object-id-field";

        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";

        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        /// <summary>
        /// Initializes and returns an instance of LoadableObjectIdField.
        /// </summary>
        public LoadableObjectIdField()
            : this((string)null) {}

        /// <summary>
        /// Initializes and returns an instance of LoadableObjectIdField.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        public LoadableObjectIdField(string label)
            : this(label, null) {}

        /// <summary>
        /// Initializes and returns an instance of LoadableObjectIdField.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        /// <param name="objectType">The type of objects that can be assigned. When null or <see cref="UnityEngine.Object"/>, any asset object is allowed except <see cref="SceneAsset"/> (use <see cref="LoadableSceneIdField"/> for scenes).</param>
        public LoadableObjectIdField(string label, Type objectType)
            : base(label, null)
        {
            string objectTooltip = LoadableObjectIdEditorUtility.GetLoadableObjectIdTooltip();
            m_ObjectField = new ObjectField
            {
                allowSceneObjects = false,
                excludeSceneAssets = true,
                objectType = objectType ?? typeof(Object),
                tooltip = objectTooltip
            };
            m_ObjectField.RegisterValueChangedCallback(OnObjectFieldValueChanged);

            visualInput.Add(m_ObjectField);
            visualInput.AddToClassList(inputUssClassName);
            labelElement.AddToClassList(labelUssClassName);
            AddToClassList(ussClassName);
        }

        private void OnObjectFieldValueChanged(ChangeEvent<Object> evt)
        {
            try
            {
                LoadableObjectId newLoadableObjectId = LoadableObjectIdEditorUtility.CreateLoadableObjectId(evt.newValue);
                if (evt.newValue != null && !newLoadableObjectId.IsValid)
                {
                    Debug.LogWarning(L10n.Tr("The selected object cannot be used as a LoadableObjectId."));
                    m_ObjectField.SetValueWithoutNotify(LoadableObjectIdEditorUtility.LoadableObjectIdToObject(value));
                    return;
                }

                value = newLoadableObjectId;
            }
            catch (ArgumentException e)
            {
                Debug.LogWarning(string.Format(L10n.Tr("The selected object cannot be used as a LoadableObjectId: {0}"), e.Message));
                m_ObjectField.SetValueWithoutNotify(LoadableObjectIdEditorUtility.LoadableObjectIdToObject(value));
                return;
            }

            evt.StopImmediatePropagation();
        }

        public override void SetValueWithoutNotify(LoadableObjectId newValue)
        {
            base.SetValueWithoutNotify(newValue);

            var obj = LoadableObjectIdEditorUtility.LoadableObjectIdToObject(value);
            m_ObjectField.SetValueWithoutNotify(obj);
        }

        /// <inheritdoc />
        protected override void UpdateMixedValueContent()
        {
            if (m_ObjectField != null)
                m_ObjectField.showMixedValue = showMixedValue;
        }
    }
}
