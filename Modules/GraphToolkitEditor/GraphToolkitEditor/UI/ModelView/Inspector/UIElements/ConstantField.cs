// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.GraphToolkit.CSO;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A field to edit the value of an <see cref="Constant{T}"/>.
    /// </summary>
    [UnityRestricted]
    internal class ConstantField : CustomizableModelPropertyField
    {
        /// <summary>
        /// The USS class name added to the <see cref="ConstantField"/> when the associated port is connected.
        /// </summary>
        public static readonly string connectedUssClassName = ussClassName.WithUssModifier(GraphElementHelper.connectedUssModifier);

        ICustomPropertyFieldBuilder m_CustomFieldBuilder;
        CustomPropertyDrawerAdapter m_CustomPropertyDrawerAdapter;
        Type m_CommonConstantType;

        /// <summary>
        /// The constants edited by the field.
        /// </summary>
        public IReadOnlyList<Constant> ConstantModels { get; }

        /// <summary>
        /// The owners of the <see cref="ConstantModels"/>.
        /// </summary>
        /// <remarks>The owners will be passed to the command that is dispatched when the constant value is modified,
        /// giving them the opportunity to update themselves.</remarks>
        public IReadOnlyList<GraphElementModel> Owners { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstantField"/> class.
        /// </summary>
        /// <param name="constantModels">The constants edited by the field.</param>
        /// <param name="owners">The owners of the constants.</param>
        /// <param name="commandTarget">The target to dispatch commands to when the field is edited.</param>
        /// <param name="label">The label for the field.</param>
        public ConstantField(IReadOnlyList<Constant> constantModels, IReadOnlyList<GraphElementModel> owners,
                             ICommandTarget commandTarget, string label = null)
            : base(commandTarget, label, null)
        {
            ConstantModels = new List<Constant>(constantModels);
            Owners = owners == null ? Array.Empty<GraphElementModel>() : new List<GraphElementModel>(owners);
            m_CommonConstantType = ModelHelpers.GetCommonBaseType(ConstantModels.Select(
                t => t.ObjectValue != null ? t.ObjectValue.GetType() : t.Type));

            CreateField();
            SetFieldChangedCallback();

            this.AddPackageStylesheet("Field.uss");

            if (Field != null)
            {
                hierarchy.Add(Field);
            }
        }

        void SetFieldChangedCallback()
        {
            if (Field == null)
                return;

            var registerCallbackMethod = GetRegisterCallback();
            if (registerCallbackMethod != null)
            {
                void EventCallback(IChangeEvent e, ConstantField f)
                {
                    if (e != null) // Enum editor sends null
                    {
                        if (m_IgnoreEventBecauseMixedIsBuggish)
                            return;
                        var newValue = GetNewValue(e);
                        newValue = ValueFromDisplay(newValue);

                        var command = new UpdateConstantsValueCommand(ConstantModels, newValue);
                        f.CommandTarget.Dispatch(command);
                    }
                }

                registerCallbackMethod.Invoke(Field, new object[] { (EventCallback<IChangeEvent, ConstantField>)EventCallback, this, TrickleDown.NoTrickleDown });
            }
        }

        /// <summary>
        /// Extracts the new value of a <see cref="IChangeEvent"/>.
        /// </summary>
        /// <param name="e">The event.</param>
        /// <returns>The value of the `newValue` property of the event.</returns>
        public static object GetNewValue(IChangeEvent e)
        {
            // PF TODO when this is a module, submit modifications to UIToolkit to avoid having to do reflection.
            var p = e.GetType().GetProperty(nameof(ChangeEvent<object>.newValue));
            return p?.GetValue(e);
        }

        internal MethodInfo GetRegisterCallback()
        {
            // PF TODO when this is a module, submit modifications to UIToolkit to avoid having to do reflection.

            var registerCallbackMethod = typeof(CallbackEventHandler)
                .GetMethods(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance)
                .SingleOrDefault(m => m.Name == nameof(RegisterCallback) && m.GetGenericArguments().Length == 2);

            if (registerCallbackMethod == null)
                return null;

            var t = DisplayFieldValueType ?? m_CommonConstantType;

            // Fix for: https://jira.unity3d.com/projects/GTF/issues/GTF-748
            // objects such as Texture2D that inherit from UnityEngine.Object send a ChangeEvent<UnityEngine.Object>
            if (t.IsSubclassOf(typeof(Object)))
                t = typeof(Object);

            var changeEventType = typeof(ChangeEvent<>).MakeGenericType(t);
            return registerCallbackMethod.MakeGenericMethod(changeEventType, typeof(ConstantField));
        }

        bool m_IgnoreEventBecauseMixedIsBuggish;
        bool m_LogUpdateException = true;

        /// <inheritdoc />
        public override void UpdateDisplayedValue()
        {
            if (Field == null)
                return;


            bool isConnected = false;
            if (Owners != null)
            {
                foreach (var owner in Owners)
                {
                    if (owner is PortModel portModel && portModel.IsConnected() && portModel.GetConnectedPorts().Any(p => p.NodeModel.State == ModelState.Enabled))
                    {
                        isConnected = true;
                        break;
                    }
                }
            }

            Field.EnableInClassList(connectedUssClassName, isConnected);
            Field.SetEnabled(!isConnected);

            bool same = true;

            var displayedValue = ConstantModels[0];

            var enu = ConstantModels.GetEnumerator();
            enu.MoveNext();

            while (enu.MoveNext())
            {
                var current = enu.Current;
                if (current != null && !Equals(current.ObjectValue, displayedValue.ObjectValue))
                {
                    same = false;
                    break;
                }
            }

            enu.Dispose();

            if (m_CustomPropertyDrawerAdapter != null)
            {
                if (!isConnected && same)
                    m_CustomPropertyDrawerAdapter.UpdateDisplayedValue(displayedValue.ObjectValue);
                return;
            }

            // PF TODO when this is a module, submit modifications to UIToolkit to avoid having to do reflection.
            var field = Field.SafeQ(null, BaseField<int>.ussClassName);
            var fieldType = field.GetType();

            try
            {
                if (isConnected || !same)
                {
                    var t = fieldType;
                    bool isBaseCompositeField = false;
                    while (t != null && t != typeof(object))
                    {
                        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(BaseCompositeField<,,>))
                        {
                            isBaseCompositeField = true;
                            break;
                        }

                        t = t.BaseType;
                    }

                    if (isBaseCompositeField)
                    {
                        // PF TODO module: UIToolkit should properly support mixed values for composite fields.
                        foreach (var subField in field.Query().ToList())
                        {
                            if (subField is not IMixedValueSupport subFieldMvs)
                                continue;

                            subFieldMvs.showMixedValue = true;
                        }
                    }

                    if (field is IMixedValueSupport mixedValueSupport)
                    {
                        m_IgnoreEventBecauseMixedIsBuggish = true;
                        mixedValueSupport.showMixedValue = true;
                        m_IgnoreEventBecauseMixedIsBuggish = false;
                    }
                }
                else
                {
                    if (field is IMixedValueSupport mixedValueSupport && mixedValueSupport.showMixedValue)
                    {
                        m_IgnoreEventBecauseMixedIsBuggish = true;
                        mixedValueSupport.showMixedValue = false;
                        m_IgnoreEventBecauseMixedIsBuggish = false;
                    }

                    SetFieldValue(field, fieldType, displayedValue);
                }
            }
            catch (Exception exception)
            {
                if (m_LogUpdateException)
                {
                    Debug.Log($"Exception caught while updating constant field {Label}.");
                    Debug.LogException(exception);
                    m_LogUpdateException = false;
                }
            }
        }

        /// <summary>
        /// Sets the field value.
        /// </summary>
        /// <param name="field">The VisualElement representing the field to set the value for</param>
        /// <param name="fieldType">The type of the field</param>
        /// <param name="displayedValue">The value to set for the field.</param>
        /// <remarks>
        /// Use this method to update a field's value based on user input or external changes.
        /// Override it to customize how values are applied to different field types.
        /// </remarks>
        protected virtual void SetFieldValue(VisualElement field, Type fieldType, Constant displayedValue)
        {
            var setValueMethod = fieldType.GetMethod("SetValueWithoutNotify");

            var value = ValueToDisplay(displayedValue.ObjectValue);
            setValueMethod?.Invoke(field, new[] { value });
        }

        void CreateField()
        {
            var fieldType = ConstantModels[0].GetTypeHandle().Resolve();

            string tooltipString = null;
            IReadOnlyList<Attribute> attributes = null;

            if (Owners != null)
            {
                PortModel firstPortModel = null;
                int i;
                for (i = 0; i < Owners.Count; i++)
                {
                    // Default to using delayed fields for non-user-defined port fields
                    if (Owners[i] is VariableDeclarationModelBase || Owners[i] is ConstantNodeModel)
                    {
                        attributes = new List<Attribute> { new DelayedAttribute() };
                        break;
                    }

                    if (Owners[i] is not PortModel port)
                        continue;

                    if (firstPortModel == null)
                    {
                        firstPortModel = port;
                        tooltipString = firstPortModel.ToolTip;
                        attributes = firstPortModel.Attributes;
                        continue;
                    }

                    if (port.ToolTip != tooltipString)
                    {
                        tooltipString = "";
                    }

                    if (port.Attributes != null && attributes != null)
                    {
                        for (var index = 0; index < port.Attributes.Count; index++)
                        {
                            var portAttribute = port.Attributes[index];
                            if (!attributes.Contains(portAttribute))
                            {
                                attributes = null;
                                break;
                            }
                        }
                    }
                }
            }

            if (m_CustomFieldBuilder == null && m_CustomPropertyDrawerAdapter == null)
            {
                TryCreateCustomPropertyFieldBuilder(fieldType, out m_CustomFieldBuilder);
                if (m_CustomFieldBuilder == null)
                {
                    m_CustomPropertyDrawerAdapter = CustomPropertyDrawerAdapter.Create(ConstantModels, CommandTarget);
                }
            }

            if (m_CustomFieldBuilder != null)
            {
                var objects = new List<object>(ConstantModels.Count);
                for (var index = 0; index < ConstantModels.Count; index++)
                {
                    var constantModel = ConstantModels[index];
                    objects.Add(constantModel.ObjectValue);
                }

                var (label, field) = m_CustomFieldBuilder.Build(CommandTarget, Label, tooltipString, objects, nameof(Constant.ObjectValue));
                Setup(label, field, tooltipString);
            }

            if (m_CustomPropertyDrawerAdapter != null)
            {
                var field = m_CustomPropertyDrawerAdapter.Build(Label);
                Setup(null, field, tooltipString);
            }

            if (Field == null)
            {
                CreateDefaultFieldForType(fieldType, tooltipString, attributes);
            }
        }

        /// <summary>
        /// If this <see cref="ConstantField"/> displays a single Port value, this method will enable or disable the sub-fields based on the connection state of the sub ports.
        /// The base implementation handles the following types of Fields:
        /// <see cref="Vector2Field"/>,
        /// <see cref="Vector3Field"/>,
        /// <see cref="Vector4Field"/>,
        /// <see cref="Vector2IntField"/>,
        /// <see cref="Vector3IntField"/>,
        /// <see cref="RectField"/>,
        /// <see cref="RectIntField"/>,
        /// <see cref="BoundsField"/>,
        /// <see cref="BoundsIntField"/>
        /// for matching <see cref="TypeHandle"/>s.
        /// </summary>
        /// <returns>Whether the type was supported and this method could enable or disable sub-fields.</returns>
        public virtual bool HandleEnabledStateWithWiredSubPorts()
        {
            if (Owners.Count() != 1 || Owners[0] is not PortModel portModel)
                return false;
            bool markConnectedPortSubFieldMixed = (Owners[0].GraphModel?.HideConnectedPortsEditor ?? true);

            if (portModel.DataTypeHandle == TypeHandle.Vector2 && Field is Vector2Field v2field)
            {
                return HandleCompositeField(v2field, portModel, markConnectedPortSubFieldMixed);
            }
            if (portModel.DataTypeHandle == TypeHandle.Vector3 && Field is Vector3Field v3field)
            {
                return HandleCompositeField(v3field, portModel, markConnectedPortSubFieldMixed);
            }
            if (portModel.DataTypeHandle == TypeHandle.Vector4 && Field is Vector4Field v4field)
            {
                return HandleCompositeField(v4field, portModel, markConnectedPortSubFieldMixed);
            }
            if (portModel.DataTypeHandle == typeof(Vector2Int).GenerateTypeHandle() && Field is Vector2IntField v2iField)
            {
                return HandleCompositeField(v2iField, portModel, markConnectedPortSubFieldMixed);
            }
            if (portModel.DataTypeHandle == typeof(Vector3Int).GenerateTypeHandle() && Field is Vector3IntField v3iField)
            {
                return HandleCompositeField(v3iField, portModel, markConnectedPortSubFieldMixed);
            }
            if (portModel.DataTypeHandle == typeof(Rect).GenerateTypeHandle() && Field is RectField rectField)
            {
                return HandleCompositeField(rectField, portModel, markConnectedPortSubFieldMixed);
            }
            if (portModel.DataTypeHandle == typeof(RectInt).GenerateTypeHandle() && Field is RectIntField rectIField)
            {
                return HandleCompositeField(rectIField, portModel, markConnectedPortSubFieldMixed);
            }
            if (portModel.DataTypeHandle == typeof(Bounds).GenerateTypeHandle() && Field is BoundsField boundsField)
            {
                return HandleBoundsField<Bounds, Vector3Field, Vector3, FloatField, float>(boundsField, portModel, markConnectedPortSubFieldMixed);
            }
            if (portModel.DataTypeHandle == typeof(BoundsInt).GenerateTypeHandle() && Field is BoundsIntField boundsIField)
            {
                return HandleBoundsField<BoundsInt, Vector3IntField, Vector3Int, IntegerField, int>(boundsIField, portModel, markConnectedPortSubFieldMixed);
            }

            return false;
        }

        static bool HandleBoundsField<TValueType, TVectorField, TVectorType, TField, TFieldValue>(BaseField<TValueType> boundsField, PortModel portModel, bool markConnectedPortSubFieldMixed)
            where TValueType : struct
            where TField : TextValueField<TFieldValue>, new()
            where TVectorField : BaseCompositeField<TVectorType, TField, TFieldValue>
        {
            var input = boundsField.Q(null, "unity-base-field__input");
            if (input == null || input.childCount != 2 || portModel.SubPorts.Count != 2)
                return false;

            if (input.ElementAt(0) is not TVectorField centerField || input.ElementAt(1) is not TVectorField extentField)
                return false;

            bool result = HandleSubField(portModel.SubPorts[0], centerField);
            bool result2 = HandleSubField(portModel.SubPorts[1], extentField);

            return result && result2;

            bool HandleSubField(PortModel subPort, TVectorField subField)
            {
                if (subPort.IsConnected())
                {
                    subField.SetEnabled(false);
                    if (markConnectedPortSubFieldMixed)
                        subField.showMixedValue = true;
                    return true;
                }

                subField.SetEnabled(true);
                if (markConnectedPortSubFieldMixed)
                    subField.showMixedValue = false;
                return HandleCompositeField(subField, subPort, markConnectedPortSubFieldMixed);
            }
        }

        static bool HandleCompositeField<TValueType, TField, TFieldValue>(BaseCompositeField<TValueType, TField, TFieldValue> field, PortModel portModel, bool markConnectedPortSubFieldMixed) where TField : TextValueField<TFieldValue>, new()
        {
            //This might break is BaseCompositeField changes
            var subPorts = portModel.SubPorts;
            if (subPorts.Count == 0)
                return false;

            var input = field.Q(null, "unity-base-field__input");
            if (input == null)
                return false;

            using var pooledFieldInputChildList = ListPool<VisualElement>.Get(out var fieldInputChildren);
            foreach (var child in input.Children())
            {
                // If the element is a spacer, skip it.
                if (child.ClassListContains(BaseCompositeField<TValueType, TField, TFieldValue>.spacerUssClassName))
                    continue;

                fieldInputChildren.Add(child);
            }

            using var pooledFieldList = ListPool<TField>.Get(out var fields);
            if (fieldInputChildren.Count != subPorts.Count)
            {
                fields.Capacity = subPorts.Count;
                if (fieldInputChildren.Count < subPorts.Count) //This might be a composite field with groups
                {
                    foreach (var groupElement in fieldInputChildren)
                    {
                        foreach (var elem in groupElement.Children())
                        {
                            // If the element is a spacer, skip it.
                            if (elem.ClassListContains(BaseCompositeField<TValueType, TField, TFieldValue>.spacerUssClassName))
                                continue;

                            if (elem is TField subField)
                                fields.Add(subField);
                            else
                                return false;
                        }
                    }

                    if (fields.Count != subPorts.Count)
                    {
                        return false;
                    }
                }
                else return false;
            }
            else
            {
                foreach (var elem in fieldInputChildren)
                {
                    if (elem is TField subField)
                        fields.Add(subField);
                    else
                        return false;
                }
            }

            for (var i = 0; i < fields.Count; i++)
            {
                var subPort = subPorts[i];
                var subField = fields[i];

                if (subPort.PortDataType == typeof(TFieldValue))
                {
                    subField.SetEnabled(!subPort.IsConnected());
                    if (markConnectedPortSubFieldMixed)
                        subField.showMixedValue = subPort.IsConnected();
                }
            }

            return true;
        }
    }
}
