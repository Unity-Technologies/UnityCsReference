// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using System.Diagnostics;
using JetBrains.Annotations;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.UIElements.Cursor;
using Object = UnityEngine.Object;

namespace Unity.UI.Builder
{
    [UsedImplicitly]
    internal class CursorStyleField : BaseField<Cursor>
    {
        public class CursorConverter : UxmlAttributeConverter<Cursor>
        {
            public override Cursor FromString(string value) => throw new NotImplementedException();
            public override string ToString(Cursor value) => throw new NotImplementedException();
        }

        [Serializable]
        public new class UxmlSerializedData : BaseField<Cursor>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<Cursor>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new CursorStyleField();
        }

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
