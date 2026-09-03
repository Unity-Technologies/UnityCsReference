// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIBuilder not yet converted
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

        #pragma warning disable UAL0015 // this side effect does not outlive the current call (global trigger / lazily-loaded asset re-fetched on next access); a stale reference is harmlessly replaced
        public CursorStyleField() : this(null) { }
        #pragma warning restore UAL0015

        #pragma warning disable UAL0015 // rebuilt/resubscribed wholesale on the next reload via this object's own lifecycle; a stale value in the interim is never observed
        public CursorStyleField(string label) : base(label, new ObjectField())
        #pragma warning restore UAL0015
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
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
