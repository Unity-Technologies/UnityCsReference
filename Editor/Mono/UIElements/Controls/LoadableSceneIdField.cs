// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Makes a field for editing a <see cref="LoadableSceneId"/>.
    /// </summary>
    [Icon("UIToolkit/Icons/ObjectField.png")]
    internal sealed class LoadableSceneIdField : BaseField<Object>
    {
        readonly ObjectField m_ObjectField;

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-loadable-scene-id-field";

        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";

        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public LoadableSceneIdField(string label)
            : base(label, null)
        {
            string objectTooltip = LoadableObjectIdEditorUtility.GetLoadableObjectIdTooltip();
            m_ObjectField = new ObjectField
            {
                allowSceneObjects = false,
                objectType = typeof(SceneAsset),
                tooltip = objectTooltip
            };
            m_ObjectField.RegisterValueChangedCallback(OnObjectFieldValueChanged);

            visualInput.Add(m_ObjectField);
            visualInput.AddToClassList(inputUssClassName);
            labelElement.AddToClassList(labelUssClassName);
            AddToClassList(ussClassName);
        }

        void OnObjectFieldValueChanged(ChangeEvent<Object> evt)
        {
            value = evt.newValue;
            evt.StopImmediatePropagation();
        }

        public override void SetValueWithoutNotify(Object newValue)
        {
            base.SetValueWithoutNotify(newValue);
            m_ObjectField.SetValueWithoutNotify(newValue);
        }

        /// <inheritdoc />
        protected override void UpdateMixedValueContent()
        {
            if (m_ObjectField != null)
                m_ObjectField.showMixedValue = showMixedValue;
        }
    }
}
