// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Field that can accept any object, including non-Unity objects.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class AnyObjectField : ObjectField
    {
        public static readonly string UssClassName = "unity-any-object-field";
        const string k_DisplayUssClassName = "unity-object-field-display";
        const string k_DisplayIconUssClassName = k_DisplayUssClassName + "__icon";
        const string k_DisplayLabelUssClassName = k_DisplayUssClassName + "__label";
        const string k_UnnamedValue = "<No Name>";

        NonUnityObjectValue m_NonUnityObjectValue;
        Image m_ObjectIcon;
        Label m_ObjectLabel;

        // To be able to change the value to null, we need to set the value of the object field to a non null value first.
        public class NonUnityObjectValue : ScriptableObject
        {
            public object data { get; set; }
        }

        /// <summary>
        /// Constructs an AnyObjectField.
        /// </summary>
        public AnyObjectField()
        {
            AddToClassList(UssClassName);

            m_ObjectIcon = this.Q<Image>(classes: k_DisplayIconUssClassName);
            m_ObjectLabel = this.Q<Label>(classes: k_DisplayLabelUssClassName);
            RegisterCallback<DetachFromPanelEvent>((e) =>
            {
                if (m_NonUnityObjectValue != null)
                {
                    ScriptableObject.DestroyImmediate(m_NonUnityObjectValue);
                    m_NonUnityObjectValue = null;
                }
            });
        }

        /// <summary>
        /// Sets a non-Unity object as the value of the field.
        /// </summary>
        /// <param name="obj">The non-Unity object value</param>
        public void SetNonUnityObject(object obj)
        {
            var valueChanged = m_NonUnityObjectValue == null || m_NonUnityObjectValue.data == null ||
                               !EqualityComparer<object>.Default.Equals(m_NonUnityObjectValue.data, obj);

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
                var objName = GetNameByReflection(m_NonUnityObjectValue.data);

                m_ObjectIcon.image = AssetPreview.GetMiniTypeThumbnail(typeof(DefaultAsset));
                m_ObjectLabel.text = $"{objName} ({type.Name})";
            }
            else
            {
                base.UpdateDisplay();
            }
        }

        static string GetNameByReflection(object obj)
        {
            const string k_NameProperty = "name";

            if (obj == null)
                return null;

            var type = obj.GetType();

            // Look for a name property
            var nameProperty = type.GetProperty(k_NameProperty);
            var objName = k_UnnamedValue;
            object nameValue = null;

            if (nameProperty != null)
            {
                nameValue = nameProperty.GetValue(obj);
            }
            else
            {
                var nameField = type.GetField((k_NameProperty));

                if (nameField != null)
                    nameValue = nameField.GetValue(obj);
            }

            if (nameValue != null)
                objName = nameValue.ToString();

            if (string.IsNullOrEmpty(objName))
                objName = k_UnnamedValue;
            return objName;
        }
    }
}
