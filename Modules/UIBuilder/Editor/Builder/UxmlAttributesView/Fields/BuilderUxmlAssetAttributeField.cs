// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UI.Builder
{
    class BuilderObjectField : ObjectField
    {
        static readonly string displayUssClassName = "unity-object-field-display";
        static readonly string displayIconUssClassName = displayUssClassName + "__icon";
        static readonly string displayLabelUssClassName = displayUssClassName + "__label";
        private NonUnityObjectValue m_NonUnityObjectValue;
        private Image m_ObjectIcon;
        private Label m_ObjectLabel;

        // To be able to change the value to null, we need to set the value of the object field to a non null value first.
        public class NonUnityObjectValue : ScriptableObject
        {
            public object data { get; set;  }
        }

        public BuilderObjectField()
        {
            m_ObjectIcon = this.Q<Image>(classes: displayIconUssClassName);
            m_ObjectLabel = this.Q<Label>(classes: displayLabelUssClassName);
            RegisterCallback<DetachFromPanelEvent>((e) =>
            {
                if (m_NonUnityObjectValue != null)
                    ScriptableObject.DestroyImmediate(m_NonUnityObjectValue);
            });
        }

        public void SetNonUnityObject(object obj)
        {
            var valueChanged = m_NonUnityObjectValue == null || m_NonUnityObjectValue.data == null || !EqualityComparer<object>.Default.Equals(m_NonUnityObjectValue.data, obj);

            if (valueChanged)
            {
                if (m_NonUnityObjectValue == null)
                    m_NonUnityObjectValue = ScriptableObject.CreateInstance<NonUnityObjectValue>();
                m_NonUnityObjectValue.data = obj;
                SetValueWithoutNotify(m_NonUnityObjectValue);
                UpdateDisplay();
            }
        }

        public void SetObjectWithoutNotify(object obj)
        {
            if (obj == null || obj is Object)
            {
                SetValueWithoutNotify(obj as Object);
            }
            else
            {
                SetNonUnityObject(obj);
            }
        }

        public override void SetValueWithoutNotify(Object obj)
        {
            if (m_NonUnityObjectValue != null && m_NonUnityObjectValue != obj)
                m_NonUnityObjectValue.data = null;
            base.SetValueWithoutNotify(obj);
        }

        internal override void UpdateDisplay()
        {
            if (m_NonUnityObjectValue != null && m_NonUnityObjectValue.data != null)
            {
                var type = m_NonUnityObjectValue.data.GetType();
                var objName = BuilderNameUtilities.GetNameByReflection(m_NonUnityObjectValue.data);

                m_ObjectIcon.image = AssetPreview.GetMiniTypeThumbnail(typeof(DefaultAsset));
                m_ObjectLabel.text = $"{objName} ({type.Name})";
            }
            else
            {
                base.UpdateDisplay();
            }
        }
    }
}
