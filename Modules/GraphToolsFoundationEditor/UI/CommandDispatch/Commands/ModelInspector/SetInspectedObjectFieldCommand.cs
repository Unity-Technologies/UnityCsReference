// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.CommandStateObserver;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Command to set the value of a field on an model.
    /// </summary>
    abstract class SetInspectedObjectFieldCommand : UndoableCommand
    {
        const string k_UndoStringSingular = "Set Property";

        public object Value;
        public List<object> InspectedObjects;
        public FieldInfo Field;

        PropertyInfo m_PropertyInfo;
        MethodInfo m_MethodInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetInspectedObjectFieldCommand"/> class.
        /// </summary>
        public SetInspectedObjectFieldCommand()
        {
            UndoString = k_UndoStringSingular;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SetInspectedObjectFieldCommand"/> class.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <param name="inspectedObjects">The objects that owns the field.</param>
        /// <param name="field">The field to set.</param>
        public SetInspectedObjectFieldCommand(object value, IEnumerable<object> inspectedObjects, FieldInfo field)
        {
            UndoString = k_UndoStringSingular;

            Value = value;
            InspectedObjects = inspectedObjects?.ToList() ?? new List<object>();
            Field = field;

            var baseType = ModelHelpers.GetCommonBaseType(InspectedObjects);

            if (InspectedObjects != null && InspectedObjects.Any() && Field != null)
            {
                var useMethodAttribute = Field.GetCustomAttribute<InspectorUseSetterMethodAttribute>();
                if (useMethodAttribute != null)
                {
                    m_MethodInfo = baseType.GetMethod(useMethodAttribute.MethodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    if (m_MethodInfo != null)
                    {
                        var parameters = m_MethodInfo.GetParameters();
                        Debug.Assert(parameters[0].ParameterType.IsInstanceOfType(Value));
                        Debug.Assert(parameters.Length == 1);
                    }
                }

                var usePropertyAttribute = Field.GetCustomAttribute<InspectorUsePropertyAttribute>();
                if (usePropertyAttribute != null)
                {
                    m_PropertyInfo = baseType.GetProperty(usePropertyAttribute.PropertyName);
                }
            }
        }

        /// <summary>
        /// Sets the field on an object according to the data held in <paramref name="command"/>.
        /// </summary>
        /// <param name="command">The command that holds the object, field and new field value.</param>
        protected static void SetField(SetInspectedObjectFieldCommand command)
        {
            if (command.InspectedObjects != null && command.Field != null && command.InspectedObjects.Any())
            {
                if (command.m_MethodInfo != null)
                {
                    var parameters = new[] {command.Value};
                    foreach (var inspectedObject in command.InspectedObjects)
                    {
                        command.m_MethodInfo.Invoke(inspectedObject, parameters);
                    }
                }
                else if (command.m_PropertyInfo != null)
                {
                    foreach (var obj in command.InspectedObjects)
                    {
                        command.m_PropertyInfo.SetMethod.Invoke(obj, new[] { command.Value });
                        if (obj is GraphElementModel gem)
                            gem.GraphModel?.CurrentGraphChangeDescription.AddChangedModel(gem, ChangeHint.Data);
                    }
                }
                else
                {
                    foreach (var obj in command.InspectedObjects)
                    {
                        command.Field.SetValue(obj, command.Value);
                        if (obj is GraphElementModel gem)
                            gem.GraphModel?.CurrentGraphChangeDescription.AddChangedModel(gem, ChangeHint.Data);
                    }
                }
            }
        }
    }
}
