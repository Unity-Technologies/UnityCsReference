// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.GraphToolkit.CSO;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Base class for commands that set the value of a field on a model.
    /// </summary>
    /// <remarks>
    /// This class provides the base implementation for commands dispatched when users modify a field in the inspector for the currently selected objects.
    /// Derived classes include <see cref="SetInspectedModelFieldCommand"/> and <see cref="SetInspectedGraphModelFieldCommand"/>.
    /// </remarks>
    [UnityRestricted]
    internal abstract class SetInspectedObjectFieldCommand : UndoableCommand
    {
        const string k_UndoStringSingular = "Set Property";

        /// <summary>
        /// The value of the field represented as an <see cref="object"/>.
        /// </summary>
        public object Value;

        /// <summary>
        /// A read-only list of <see cref="object"/>s that own the field.
        /// </summary>
        public IReadOnlyList<object> InspectedObjects;

        /// <summary>
        /// The <see cref="FieldInfo"/> of the field.
        /// </summary>
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
        public SetInspectedObjectFieldCommand(object value, IReadOnlyList<object> inspectedObjects, FieldInfo field)
        {
            UndoString = k_UndoStringSingular;

            Value = value;
            InspectedObjects = inspectedObjects ?? Array.Empty<object>();
            Field = field;

            var baseType = ModelHelpers.GetCommonBaseType(InspectedObjects);

            if (InspectedObjects is { Count: > 0 } && Field != null)
            {
                var useMethodAttribute = Field.GetCustomAttribute<InspectorUseSetterMethodAttribute>();
                if (useMethodAttribute != null)
                {
                    m_MethodInfo = baseType.GetMethod(useMethodAttribute.MethodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

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

        static object GetDataObject(object inspectedObject)
        {
            return inspectedObject is IHasInspectorSurrogate hasInspectorSurrogate
                ? hasInspectorSurrogate.Surrogate
                : inspectedObject;
        }

        /// <summary>
        /// Sets the field on an object according to the data held in <paramref name="command"/>.
        /// </summary>
        /// <param name="updater">The updater to use to mark modified objects.</param>
        /// <param name="command">The command that holds the object, field and new field value.</param>
        protected static void SetField(GraphModelStateComponent.StateUpdater updater, SetInspectedObjectFieldCommand command)
        {
            if (command.InspectedObjects is { Count: > 0 } && command.Field != null)
            {
                if (command.m_MethodInfo != null)
                {
                    var parameters = new[] { command.Value };
                    foreach (var obj in command.InspectedObjects)
                    {
                        var dataObject = GetDataObject(obj);
                        command.m_MethodInfo.Invoke(dataObject, parameters);
                    }
                }
                else if (command.m_PropertyInfo != null)
                {
                    foreach (var obj in command.InspectedObjects)
                    {
                        var dataObject = GetDataObject(obj);
                        command.m_PropertyInfo.SetMethod.Invoke(dataObject, new[] { command.Value });

                        if (obj is Model model)
                            updater.MarkChanged(model.Guid, ChangeHint.Data);
                    }
                }
                else
                {
                    foreach (var obj in command.InspectedObjects)
                    {
                        var dataObject = GetDataObject(obj);
                        command.Field.SetValue(dataObject, command.Value);

                        if (obj is GraphElementModel graphElementModel)
                        {
                            graphElementModel.GraphModel?.CurrentGraphChangeDescription.AddChangedModel(graphElementModel, ChangeHint.Data);
                        }

                        if (obj is Model model)
                        {
                            updater.MarkChanged(model.Guid, ChangeHint.Data);
                        }
                    }
                }
            }
        }
    }
}
