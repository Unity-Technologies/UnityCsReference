// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Makes a field for entering MaterialDefinition.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal class MaterialDefinitionField : BaseField<MaterialDefinition>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BaseField<MaterialDefinition>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<MaterialDefinition>.UxmlSerializedData.Register();
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), Array.Empty<UxmlAttributeNames>(), true);
            }

            public override object CreateInstance() => new MaterialDefinitionField();
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-material-definition-field";

        ObjectField m_ObjectField;

        public ObjectField objectField => m_ObjectField;

        public MaterialDefinitionField() : this(null) { }

        public MaterialDefinitionField(string label) : base(label, null)
        {
            AddToClassList(ussClassName);

            m_ObjectField = new ObjectField
            {
                objectType = typeof(Material)
            };
            m_ObjectField.RegisterValueChangedCallback(OnObjectValueChange);

            visualInput.Add(m_ObjectField);
        }

        void OnObjectValueChange(ChangeEvent<Object> e)
        {
            value = MaterialDefinition.FromObject(e.newValue);
            e.StopImmediatePropagation();
        }

        public override void SetValueWithoutNotify(MaterialDefinition newValue)
        {
            base.SetValueWithoutNotify(newValue);

            var obj = newValue.material;
            m_ObjectField.SetValueWithoutNotify(obj);
        }
    }
}
