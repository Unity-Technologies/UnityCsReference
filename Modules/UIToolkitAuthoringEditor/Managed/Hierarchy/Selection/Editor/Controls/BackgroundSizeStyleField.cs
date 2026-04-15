// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [UsedImplicitly]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class BackgroundSizeStyleField : BaseField<BackgroundSize>
    {
        [Serializable]
        public new class UxmlSerializedData : BaseField<BackgroundSize>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<BackgroundSize>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new BackgroundSizeStyleField();
        }

        static readonly string s_FieldClassName = "unity-background-size-style-field";
        static readonly string s_UxmlPath = "UIToolkitAuthoring/Inspector/Controls/BackgroundSizeStyleField.uxml";
        public static readonly string s_BackgroundSizeWidthFieldName = "width";
        public static readonly string s_BackgroundSizeHeightFieldName = "height";
        public static readonly string InspectorContainerClassName = "unity-ui-inspector__container";

        LengthField m_BackgroundSizeXField;
        LengthField m_BackgroundSizeYField;
        BackgroundSizeType m_SizeType;

        public BackgroundSizeStyleField() : this(null) { }

        public BackgroundSizeStyleField(string label) : base(label)
        {
            AddToClassList(InspectorContainerClassName);
            AddToClassList(s_FieldClassName);

            var template = EditorGUIUtility.Load(s_UxmlPath) as VisualTreeAsset;
            template.CloneTree(this);

            m_BackgroundSizeXField = this.Q<LengthField>(s_BackgroundSizeWidthFieldName);
            m_BackgroundSizeYField = this.Q<LengthField>(s_BackgroundSizeHeightFieldName);
            m_SizeType = BackgroundSizeType.Cover;
            m_BackgroundSizeXField.RegisterValueChangedCallback(e =>
            {
                UpdateBackgroundSizeField();
                e.StopPropagation();
            });

            m_BackgroundSizeYField.RegisterValueChangedCallback(e =>
            {
                UpdateBackgroundSizeField();
                e.StopPropagation();
            });

            value = new BackgroundSize();
        }

        public override void SetValueWithoutNotify(BackgroundSize newValue)
        {
            base.SetValueWithoutNotify(newValue);
            RefreshSubFields();
        }

        void RefreshSubFields()
        {
            if (value.sizeType == BackgroundSizeType.Cover)
            {
                m_SizeType = BackgroundSizeType.Cover;
                m_BackgroundSizeYField.SetEnabled(false);
            }
            else if (value.sizeType == BackgroundSizeType.Contain)
            {
                m_SizeType = BackgroundSizeType.Contain;
                m_BackgroundSizeYField.SetEnabled(false);
            }
            else
            {
                m_SizeType = BackgroundSizeType.Length;
                m_BackgroundSizeYField.SetEnabled(true);
                m_BackgroundSizeXField.SetValueWithoutNotify(value.x);
                m_BackgroundSizeYField.SetValueWithoutNotify(value.y);
            }
        }

        void UpdateBackgroundSizeField()
        {
            // Rebuild value from sub fields

            var newSize = new BackgroundSize();

            if (m_SizeType != BackgroundSizeType.Length)
            {
                switch (m_SizeType)
                {
                    case BackgroundSizeType.Cover:
                        newSize.sizeType = BackgroundSizeType.Cover;
                        value = newSize;
                        return;
                    case BackgroundSizeType.Contain:
                        newSize.sizeType = BackgroundSizeType.Contain;
                        value = newSize;
                        return;
                }
            }

            value = new BackgroundSize(m_BackgroundSizeXField.value, m_BackgroundSizeYField.value);
        }
    }
}
