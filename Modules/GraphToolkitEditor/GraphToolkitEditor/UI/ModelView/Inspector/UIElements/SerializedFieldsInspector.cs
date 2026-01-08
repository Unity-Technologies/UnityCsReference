// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Inspector for the serializable fields of a <see cref="GraphElementModel"/> or its surrogate, if it implements <see cref="IHasInspectorSurrogate"/>.
    /// Will Display in this order:
    /// - Fields without [<see cref="InspectorFieldOrderAttribute"/>] matching m_Filter.
    /// - Fields from GetCustomFields.
    /// - Ordered Fields With [InspectorFieldOrderAttribute] matching m_Filter.
    /// </summary>
    /// <remarks>Makes use of those attributes : <see cref="DisplayNameAttribute"/>, <see cref="OverrideForFieldAttribute"/> , <see cref="InspectorFieldOrderAttribute"/>, <see cref="BoolDropDownAttribute"/>, <see cref="InvertToggleAttribute"/>.</remarks>
    [UnityRestricted]
    internal class SerializedFieldsInspector : FieldsInspector
    {
        const BindingFlags k_FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        /// <summary>
        /// Determines if the field can be inspected. A field can be inspected if it is public or if it has the
        /// <see cref="SerializeField"/> attribute. In addition, it must not have the <see cref="HideInInspector"/>
        /// attribute nor the <see cref="NonSerializedAttribute"/> attribute.
        /// </summary>
        /// <param name="f">The field to inspect.</param>
        /// <returns>True if the field can be inspected, false otherwise.</returns>
        public static bool CanBeInspected(FieldInfo f)
        {
            if (f != null)
            {
                var isSerializable = IsSerialized(f);

                if (isSerializable
                    && f.GetCustomAttribute<HideInInspector>() == null
                    && f.GetCustomAttribute<ObsoleteAttribute>() == null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if the field will be serialized. A field will be serialized if it is public or if it has the
        /// <see cref="SerializeField"/> attribute.
        /// </summary>
        /// <param name="f">The field to inspect.</param>
        /// <returns>True if the field will be serialized, false otherwise.</returns>
        public static bool IsSerialized(FieldInfo f)
        {
            return ((f.Attributes & FieldAttributes.Public) == FieldAttributes.Public ||
                (f.Attributes & FieldAttributes.Private) == FieldAttributes.Private &&
                f.GetCustomAttribute<SerializeField>() != null)
                && !f.IsNotSerialized;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="SerializedFieldsInspector"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="models">The models displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="filter">A filter function to select which fields are displayed in the inspector. If null, defaults to <see cref="CanBeInspected"/>.</param>
        /// <returns>A new instance of <see cref="SerializedFieldsInspector"/>.</returns>
        public static SerializedFieldsInspector Create(string name, IReadOnlyList<Model> models, ChildView ownerElement,
            string parentClassName, Func<FieldInfo, bool> filter = null)
        {
            return new SerializedFieldsInspector(name, models, ownerElement, parentClassName, filter);
        }

        Func<FieldInfo, bool> m_Filter;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializedFieldsInspector"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="models">The models displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="filter">A filter function to select which fields are displayed in the inspector. If null, defaults to <see cref="CanBeInspected"/>.</param>
        protected SerializedFieldsInspector(string name, IReadOnlyList<Model> models, ChildView ownerElement,
                                            string parentClassName, Func<FieldInfo, bool> filter)
            : base(name, models, ownerElement, parentClassName)
        {
            m_Filter = filter ?? CanBeInspected;
        }

        /// <summary>
        /// Gets the objects displayed by the inspector. It is usually the model passed to the constructor, unless the
        /// model implements <see cref="IHasInspectorSurrogate"/>, in which case it is the surrogate object.
        /// </summary>
        /// <returns>The inspected object.</returns>
        public IEnumerable<object> GetInspectedObjects()
        {
            // ReSharper disable once SuspiciousTypeConversion.Global : IHasInspectorSurrogate is for use by clients.
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return m_Models.Select(t => t is IHasInspectorSurrogate surrogate ? surrogate.Surrogate : t);
#pragma warning restore RS0030
        }

        /// <summary>
        /// Allow adding field to display between the default fields and the default field with a <see cref="InspectorFieldOrderAttribute"/>.
        /// </summary>
        /// <param name="outFieldList">The list to which to add the fields.</param>
        protected virtual void GetCustomFields(List<BaseModelPropertyField> outFieldList)
        {
        }

        /// <inheritdoc />
        protected override IReadOnlyList<BaseModelPropertyField> GetFields()
        {
            var fieldList = new List<BaseModelPropertyField>();
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var targets = GetInspectedObjects().ToList();
#pragma warning restore RS0030

            var inspectorOrderFields = new SortedDictionary<int, List<BaseModelPropertyField>>();

            AddFieldsFromTypes(targets, inspectorOrderFields, fieldList);
            GetCustomFields(fieldList);
            foreach (var fieldAtPositionList in inspectorOrderFields.Values)
            {
                fieldList.AddRange(fieldAtPositionList);
            }

            return fieldList;
        }

        protected void AddFieldsFromTypes(IReadOnlyList<object> targets,
            SortedDictionary<int, List<BaseModelPropertyField>> inspectorOrderFields, List<BaseModelPropertyField> outFieldList)
        {
            var type = ModelHelpers.GetCommonBaseType(targets);

            if (type == null)
                return;

            var typeList = new List<Type>();
            while (type != null)
            {
                typeList.Insert(0, type);
                type = type.BaseType;
            }

            var overrideFields = new Dictionary<FieldInfo, FieldInfo>();

            foreach (var t in typeList)
            {
                var fields = t.GetFields(k_FieldFlags);

                foreach (var fieldInfo in fields)
                {
                    if (!IsSerialized(fieldInfo))
                        continue;

                    var overrideForFields = fieldInfo.GetCustomAttributes<OverrideForFieldAttribute>();
                    #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    if (overrideForFields.Count() > 0)
#pragma warning restore RS0030
                    {
                        if (fieldInfo.FieldType != typeof(bool))
                        {
                            Debug.LogWarning($"Field {fieldInfo.Name} of type {t.FullName} with OverrideForFieldAttribute must be of type bool");
                            continue;
                        }
                        #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                        var overField = t.GetField(overrideForFields.First().FieldName, k_FieldFlags);
#pragma warning restore RS0030
                        if (overField != null)
                        {
                            overrideFields.Add(overField, fieldInfo);
                        }
                    }
                }
            }

            foreach (var t in typeList)
            {
                var fields = t.GetFields(k_FieldFlags);
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                foreach (var fieldInfo in fields.Where(m_Filter))
#pragma warning restore RS0030
                {
                    #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    if (fieldInfo.GetCustomAttributes<OverrideForFieldAttribute>().Count() > 0)
#pragma warning restore RS0030
                        continue;
                    var moveAfter = fieldInfo.GetCustomAttribute<InspectorFieldOrderAttribute>();


                    BaseModelPropertyField field;

                    var tooltip = "";
                    var tooltipAttribute = fieldInfo.GetCustomAttribute<TooltipAttribute>();
                    if (tooltipAttribute != null)
                    {
                        tooltip = tooltipAttribute.tooltip;
                    }

                    string displayName = null;
                    var displayNameAttribute = fieldInfo.GetCustomAttribute<DisplayNameAttribute>();
                    if (displayNameAttribute != null)
                    {
                        displayName = displayNameAttribute.DisplayName;
                    }

                    if (overrideFields.TryGetValue(fieldInfo, out var overrideField))
                    {
                        field = GetOverrideFieldFromFieldInfo(fieldInfo, overrideField, tooltip, displayName);
                    }
                    else
                    {
                        field = GetFieldFromFieldInfo(fieldInfo, tooltip, displayName);
                    }
                    var disableAttribute = fieldInfo.GetCustomAttribute<DisableInInspectorAttribute>();
                    if (disableAttribute != null && field != null)
                    {
                        field.SetEnabled(false);
                        field.Field?.SetEnabled(false);
                    }

                    if (moveAfter != null)
                    {
                        AddFieldToInspectorOrderFields(moveAfter.Order, field, inspectorOrderFields);
                        continue;
                    }

                    outFieldList.Add(field);
                }
            }

            BaseModelPropertyField GetFieldFromFieldInfo(FieldInfo fieldInfo1, string tooltip, string displayName)
            {

                var modelFieldFieldType = typeof(ModelSerializedFieldField<>).MakeGenericType(fieldInfo1.FieldType);
                var baseModelPropertyField = Activator.CreateInstance(
                    modelFieldFieldType, OwnerRootView, m_Models, targets, fieldInfo1, tooltip, displayName)
                    as BaseModelPropertyField;


                return baseModelPropertyField;
            }

            BaseModelPropertyField GetOverrideFieldFromFieldInfo(FieldInfo inspectedField, FieldInfo overrideField, string tooltip, string displayName)
            {
                var modelFieldFieldType = typeof(OverrideModelSerializedFieldField<>).MakeGenericType(inspectedField.FieldType);
                var baseModelPropertyField = Activator.CreateInstance(
                        modelFieldFieldType, OwnerRootView, m_Models, targets, inspectedField, overrideField, tooltip, displayName ?? overrideField.Name)
                    as BaseModelPropertyField;

                return baseModelPropertyField;
            }
        }

        protected static void AddFieldToInspectorOrderFields(int order, BaseModelPropertyField fieldToAdd, SortedDictionary<int, List<BaseModelPropertyField>> inspectorOrderFields)
        {
            if (!inspectorOrderFields.TryGetValue(order, out var fieldsAtPosition))
            {
                fieldsAtPosition = new List<BaseModelPropertyField>();
                inspectorOrderFields[order] = fieldsAtPosition;
            }
            fieldsAtPosition.Add(fieldToAdd);
        }
    }
}
