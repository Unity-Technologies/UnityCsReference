// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using JetBrains.Annotations;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.UIElements.Cursor;
using Object = UnityEngine.Object;

namespace Unity.UI.Builder
{
    [UsedImplicitly]
    [UxmlElement]
    internal partial class CursorStyleField : BaseField<Cursor>
    {
        static readonly string s_FieldClassName = "unity-cursor-style-field";

        private readonly ObjectField m_CursorAssetField;

        public CursorStyleField() : this(null) { }

        public CursorStyleField(string label) : base(label, new ObjectField())
        {
            AddToClassList(BuilderConstants.InspectorContainerClassName);
            AddToClassList(s_FieldClassName);

            m_CursorAssetField = (ObjectField)visualInput;
            m_CursorAssetField.objectType = typeof(Texture2D);

            m_CursorAssetField.RegisterValueChangedCallback(OnAssetChanged);
            Add(m_CursorAssetField);

            value = new Cursor();
        }

        private void OnAssetChanged(ChangeEvent<Object> evt)
        {
            var newValue = new Cursor
            {
                texture = (Texture2D)evt.newValue, hotspot = value.hotspot
            };
            value = newValue;
        }

        public override void SetValueWithoutNotify(Cursor newValue)
        {
            base.SetValueWithoutNotify(newValue);
            m_CursorAssetField.SetValueWithoutNotify(newValue.texture);
        }
    }
}
