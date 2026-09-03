// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitFramework not yet converted
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Loading;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Makes a field for editing a <see cref="LoadableSceneId"/>.
    /// </summary>
    [Icon("UIToolkit/Icons/ObjectField.png")]
    internal class LoadableSceneIdField : BaseField<LoadableSceneId>
    {
        private ObjectField m_ObjectField;

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

        /// <summary>
        /// Initializes and returns an instance of LoadableSceneIdField.
        /// </summary>
        public LoadableSceneIdField()
            : this(null) {}

        /// <summary>
        /// Initializes and returns an instance of LoadableSceneIdField.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
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

        private void OnObjectFieldValueChanged(ChangeEvent<Object> evt)
        {
            LoadableSceneId newLoadableSceneId = ObjectToLoadableSceneId(evt.newValue);
            if (evt.newValue != null && !newLoadableSceneId.IsValid)
            {
                Debug.LogWarning(L10n.Tr("The selected object cannot be used as a LoadableSceneId.", null));
                m_ObjectField.SetValueWithoutNotify(LoadableSceneIdEditorUtility.LoadableSceneIdToScene(value));
                return;
            }

            value = newLoadableSceneId;
            evt.StopImmediatePropagation();
        }

        public override void SetValueWithoutNotify(LoadableSceneId newValue)
        {
            base.SetValueWithoutNotify(newValue);
            m_ObjectField.SetValueWithoutNotify(LoadableSceneIdEditorUtility.LoadableSceneIdToScene(value));
        }

        /// <inheritdoc />
        protected override void UpdateMixedValueContent()
        {
            if (m_ObjectField != null)
                m_ObjectField.showMixedValue = showMixedValue;
        }

        static LoadableSceneId ObjectToLoadableSceneId(Object obj)
        {
            if (obj == null)
                return default;
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guidStr, out _))
                return LoadableSceneIdEditorUtility.CreateLoadableSceneId(new GUID(guidStr));
            return default;
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
