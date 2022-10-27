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
                    m_MethodInfo = baseType.GetMethod(useMethodAttribute.MethodName);

                    if (m_MethodInfo != null)
                    {
                        var parameters = m_MethodInfo.GetParameters();
                        Debug.Assert(parameters[0].ParameterType.IsInstanceOfType(Value));
                        Debug.Assert(parameters[1].IsOut &&
                            parameters[1].ParameterType.GetElementType() == typeof(IEnumerable<GraphElementModel>));
                        Debug.Assert(parameters[2].IsOut &&
                            parameters[2].ParameterType.GetElementType() == typeof(IEnumerable<GraphElementModel>));
                        Debug.Assert(parameters[3].IsOut &&
                            parameters[3].ParameterType.GetElementType() == typeof(IEnumerable<GraphElementModel>));
                        Debug.Assert(parameters.Length == 4);
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
        /// <param name="newModels">On exit, contains the models that were added as the result of setting the field.</param>
        /// <param name="changedModels">On exit, contains the models that were modified as the result of setting the field (excluding the object on which the field is set).</param>
        /// <param name="deletedModels">On exit, contains the models that were deleted as the result of setting the field.</param>
        protected static void SetField(SetInspectedObjectFieldCommand command,
            out IEnumerable<GraphElementModel> newModels,
            out IEnumerable<GraphElementModel> changedModels,
            out IEnumerable<GraphElementModel> deletedModels)
        {
            newModels = null;
            changedModels = null;
            deletedModels = null;

            if (command.InspectedObjects != null && command.Field != null && command.InspectedObjects.Any())
            {
                if (command.m_MethodInfo != null)
                {
                    newModels = Enumerable.Empty<GraphElementModel>();
                    changedModels = Enumerable.Empty<GraphElementModel>();
                    deletedModels = Enumerable.Empty<GraphElementModel>();

                    var parameters = new[] { command.Value, null, null, null };
                    foreach (var inspectedObject in command.InspectedObjects)
                    {
                        command.m_MethodInfo.Invoke(inspectedObject, parameters);
                        if (parameters[1] != null)
                            newModels = newModels.Concat((IEnumerable<GraphElementModel>)parameters[1]);
                        if (parameters[2] != null)
                            changedModels = changedModels.Concat((IEnumerable<GraphElementModel>)parameters[2]);
                        if (parameters[3] != null)
                            deletedModels = deletedModels.Concat((IEnumerable<GraphElementModel>)parameters[3]);
                    }
                }
                else if (command.m_PropertyInfo != null)
                {
                    foreach (var obj in command.InspectedObjects)
                    {
                        command.m_PropertyInfo.SetMethod.Invoke(obj, new[] { command.Value });
                    }

                    changedModels = command.InspectedObjects.OfType<GraphElementModel>();
                }
                else
                {
                    foreach (var obj in command.InspectedObjects)
                    {
                        command.Field.SetValue(obj, command.Value);
                    }

                    changedModels = command.InspectedObjects.OfType<GraphElementModel>();
                }
            }
        }
    }
}
