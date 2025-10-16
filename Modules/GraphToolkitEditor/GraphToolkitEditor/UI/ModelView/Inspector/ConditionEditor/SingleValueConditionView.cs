// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// <see cref="ConditionView"/> where the condition has a single serialized field value.
    /// </summary>
    [UnityRestricted]
    internal abstract class SingleValueConditionView : SingleConditionView
    {
        /// <summary>
        /// The USS class name added to this element.
        /// </summary>
        public new static readonly string ussClassName = "ge-singe-value-condition-view";

        /// <summary>
        /// The USS class name added to the value field.
        /// </summary>
        public static readonly string valueUssClassName = ussClassName.WithUssElement("value");

        FieldInfo m_FieldInfo;
        string m_DisplayName;
        string m_Tooltip;
        BaseModelPropertyField m_Field;

        /// <summary>
        /// Creates a new instance of the <see cref="SingleValueConditionView"/> class.
        /// </summary>
        /// <param name="fieldInfo">The <paramref name="fieldInfo"/> of the single field.</param>
        /// <param name="displayName">An optional custom display name for the field.</param>
        /// <param name="tooltip">An optional tooltip for the field.</param>
        public SingleValueConditionView(FieldInfo fieldInfo, string displayName = null, string tooltip = null)
        {
            m_FieldInfo = fieldInfo;

            var isSerializable = (fieldInfo.Attributes & FieldAttributes.Public) == FieldAttributes.Public ||
                (fieldInfo.Attributes & FieldAttributes.Private) == FieldAttributes.Private &&
                fieldInfo.GetCustomAttribute<SerializeField>() != null;
            isSerializable &= !fieldInfo.IsNotSerialized;

            if (!isSerializable)
            {
                Debug.LogError("Field in SingleValueConditionView must be serializable.");
            }

            m_DisplayName = string.IsNullOrEmpty(displayName) ? ObjectNames.NicifyVariableName(fieldInfo.Name) : displayName;
            m_Tooltip = string.IsNullOrEmpty(tooltip) ? fieldInfo.GetCustomAttribute<TooltipAttribute>()?.tooltip : tooltip;
        }

        /// <inheritdoc />
        protected override void BuildUI()
        {
            AddToClassList(ussClassName);
            base.BuildUI();

            var modelArray = new[] { Model };

            var modelFieldFieldType = typeof(ModelSerializedFieldField<>).MakeGenericType(m_FieldInfo.FieldType);
            m_Field = Activator.CreateInstance(
                    modelFieldFieldType, RootView, modelArray, modelArray, m_FieldInfo, m_Tooltip, m_DisplayName)
                as BaseModelPropertyField;
            if (m_Field != null)
            {
                m_Field.AddToClassList(valueUssClassName);
                m_Container.Add(m_Field);
            }
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            base.UpdateUIFromModel(visitor);

            m_Field?.UpdateDisplayedValue();
        }
    }
}
