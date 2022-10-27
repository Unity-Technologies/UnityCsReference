// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.CommandStateObserver;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Class to display a UI to edit a property on a <see cref="Model"/>. When the field is edited, a command
    /// of type <typeparamref name="TCommand"/> is sent.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to display.</typeparam>
    /// <typeparam name="TCommand">The type of the command to send when the field is edited.</typeparam>
    class ModelPropertyField<TValue, TCommand> : ModelPropertyField<TValue>
        where TCommand : class, ICommand
    {
        public ModelPropertyField(
            RootView commandTarget,
            IEnumerable<Model> models,
            string propertyName,
            string label,
            string fieldTooltip,
            Func<Model, TValue> valueGetter = null)
            : base(commandTarget, models, propertyName, label, fieldTooltip)
        {
            SetValueGetterOrDefault(propertyName, valueGetter);
            SetOnChangeDispatchCommand<TCommand>();
        }
    }

    /// <summary>
    /// Class to display a UI to edit a property on a <see cref="Model"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to display.</typeparam>
    class ModelPropertyField<TValue> : CustomizableModelPropertyField
    {
        ICustomPropertyFieldBuilder<TValue> m_CustomFieldBuilder;

        /// <summary>
        /// The root element of the UI.
        /// </summary>
        protected VisualElement m_Field;

        /// <summary>
        /// A function to get the current value of the property.
        /// </summary>
        protected Func<TValue> m_ValueGetter;

        /// <summary>
        /// The model that owns the property.
        /// </summary>
        public IEnumerable<Model> Models { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelPropertyField{TValue}"/> class.
        /// </summary>
        /// <param name="commandTarget">The view to use to dispatch commands when the field is edited.</param>
        /// <param name="models">The models that owns the field.</param>
        /// <param name="propertyName">The name of the property to edit.</param>
        /// <param name="label">The label for the field. If null, the <paramref name="propertyName"/> will be used.</param>
        /// <param name="fieldTooltip">The tooltip for the field.</param>
        protected ModelPropertyField(
            ICommandTarget commandTarget,
            IEnumerable<Model> models,
            string propertyName,
            string label,
            string fieldTooltip
        )
            : base(commandTarget, label ?? ObjectNames.NicifyVariableName(propertyName))
        {
            Models = models;
            m_Field = CreateFieldFromProperty(propertyName, fieldTooltip);
            if (m_Field != null)
                hierarchy.Add(m_Field);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelPropertyField{TValue}"/> class.
        /// </summary>
        /// <param name="commandTarget">The view to use to dispatch commands when the field is edited.</param>
        /// <param name="models">The models that owns the field.</param>
        /// <param name="propertyName">The name of the property to edit.</param>
        /// <param name="label">The label for the field. If null, the <paramref name="propertyName"/> will be used.</param>
        /// <param name="fieldTooltip">The tooltip for the field.</param>
        /// <param name="onValueChanged">The action to execute when the value is edited. If null, nothing will be done.</param>
        /// <param name="valueGetter">A function used to retrieve the current property value on <paramref name="models"/>. If null, the property getter will be used.</param>
        public ModelPropertyField(
            ICommandTarget commandTarget,
            IEnumerable<Model> models,
            string propertyName,
            string label,
            string fieldTooltip,
            Action<TValue, ModelPropertyField<TValue>> onValueChanged = null,
            Func<Model, TValue> valueGetter = null)
            : this(commandTarget, models, propertyName, label, fieldTooltip)
        {
            SetValueGetterOrDefault(propertyName, valueGetter);

            if (onValueChanged != null)
            {
                switch (m_Field)
                {
                    case PopupField<string> _:
                        m_Field.RegisterCallback<ChangeEvent<string>, ModelPropertyField<TValue>>(
                            (e, f) =>
                            {
                                var newValue = (TValue)Enum.Parse(typeof(TValue), e.newValue);
                                onValueChanged(newValue, f);
                            }, this);
                        break;

                    case BaseField<Enum> _:
                        m_Field.RegisterCallback<ChangeEvent<Enum>, ModelPropertyField<TValue>>(
                            (e, f) => onValueChanged((TValue)(object)e.newValue, f), this);
                        break;

                    default:
                        m_Field.RegisterCallback<ChangeEvent<TValue>, ModelPropertyField<TValue>>(
                            (e, f) => onValueChanged(e.newValue, f), this);
                        break;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelPropertyField{TValue}"/> class.
        /// </summary>
        /// <param name="commandTarget">The view to use to dispatch commands when the field is edited.</param>
        /// <param name="models">The models that owns the field.</param>
        /// <param name="propertyName">The name of the property to edit.</param>
        /// <param name="label">The label for the field. If null, the <paramref name="propertyName"/> will be used.</param>
        /// <param name="fieldTooltip">The tooltip for the field.</param>
        /// <param name="commandType">The command type to dispatch when the value is edited. The type should implement <see cref="ICommand"/> and have a constructor that accepts a <typeparamref name="TValue"/> and a <see cref="Model"/>.</param>
        /// <param name="valueGetter">A function used to retrieve the current property value on <paramref name="models"/>. If null, the property getter will be used.</param>
        public ModelPropertyField(
            ICommandTarget commandTarget,
            IEnumerable<Model> models,
            string propertyName,
            string label,
            string fieldTooltip,
            Type commandType,
            Func<Model, TValue> valueGetter = null)
            : this(commandTarget, models, propertyName, label, fieldTooltip)
        {
            SetValueGetterOrDefault(propertyName, valueGetter);

            if (commandType != null)
            {
                if (!typeof(ICommand).IsAssignableFrom(commandType))
                    throw new ArgumentException($"{commandType} is not a {typeof(ICommand)}.", nameof(commandType));

                SetOnChangeDispatchCommand(commandType);
            }
        }

        /// <summary>
        /// Sets the method to retrieve the value of <paramref name="propertyName"/> from <see cref="Models"/>.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="valueGetter">The function to use. If null, use the property getter method.</param>
        protected void SetValueGetterOrDefault(string propertyName, Func<Model, TValue> valueGetter)
        {
            m_ValueGetter = valueGetter != null ? () => valueGetter(Models.First()) : MakePropertyValueGetter(Models, propertyName);
        }


        /// <summary>
        /// Sets the <see cref="ChangeEvent{T}"/> callback on the field to dispatch a command of type <typeparamref name="TCommand"/>.
        /// </summary>
        /// <typeparam name="TCommand">The type of command to dispatch.</typeparam>
        protected void SetOnChangeDispatchCommand<TCommand>() where TCommand : ICommand
        {
            SetOnChangeDispatchCommand(typeof(TCommand));
        }

        /// <summary>
        /// Sets the <see cref="ChangeEvent{T}"/> callback on the field to dispatch a command of type <paramref name="commandType"/>.
        /// </summary>
        /// <param name="commandType">The type of command to dispatch.</param>
        protected void SetOnChangeDispatchCommand(Type commandType)
        {
            if (m_Field == null)
                return;

            switch (m_Field)
            {
                case PopupField<string> _:
                    m_Field.RegisterCallback<ChangeEvent<string>, ModelPropertyField<TValue>>((e, f) =>
                    {
                        var value = (TValue)Enum.Parse(typeof(TValue), e.newValue);
                        if (Activator.CreateInstance(commandType, value, f.Models) is ICommand command)
                            f.CommandTarget.Dispatch(command);
                    }, this);
                    break;

                case BaseField<Enum> _:
                    m_Field.RegisterCallback<ChangeEvent<Enum>, ModelPropertyField<TValue>>((e, f) =>
                    {
                        if (Activator.CreateInstance(commandType, e.newValue, f.Models) is ICommand command)
                            f.CommandTarget.Dispatch(command);
                    }, this);
                    break;

                default:
                    m_Field.RegisterCallback<ChangeEvent<TValue>, ModelPropertyField<TValue>>((e, f) =>
                    {
                        if (Activator.CreateInstance(commandType, e.newValue, f.Models) is ICommand command)
                            f.CommandTarget.Dispatch(command);
                    }, this);
                    break;
            }
        }

        protected static readonly Func<TValue> k_GetMixed = () => default;

        static Func<TValue> MakePropertyValueGetter(IEnumerable<Model> models, string propertyName)
        {

            var baseType = ModelHelpers.GetCommonBaseType(models);

            var getterInfo = baseType.GetProperty(propertyName)?.GetGetMethod();
            if (getterInfo != null)
            {
                Debug.Assert(typeof(TValue) == getterInfo.ReturnType);

                var firstValue = getterInfo.Invoke(models.First(),null);
                bool allSame = models.Skip(1).All(t => Equals(firstValue,getterInfo.Invoke(t,null)));

                if (allSame)
                {
                    var del = Delegate.CreateDelegate(typeof(Func<TValue>), models.First(), getterInfo);
                    return del as Func<TValue>;
                }

                return k_GetMixed;
            }

            return null;
        }

        /// <inheritdoc />
        public override bool UpdateDisplayedValue()
        {
            if (m_ValueGetter != null)
            {
                if (m_ValueGetter == k_GetMixed)
                {
                    if (m_CustomFieldBuilder != null)
                    {
                        m_CustomFieldBuilder.SetMixed();
                        return true;
                    }

                    switch (m_Field)
                    {
                        case PopupField<string> popupField:
                            popupField.showMixedValue = true;
                            return true;

                        case BaseField<Enum> baseField:
                            baseField.showMixedValue = true;
                            return true;

                        case BaseField<TValue> baseField:
                            baseField.showMixedValue = true;
                            return true;
                    }
                }
                else
                {
                    if (m_CustomFieldBuilder != null)
                    {
                        return m_CustomFieldBuilder.UpdateDisplayedValue(m_ValueGetter.Invoke());
                    }

                    switch (m_Field)
                    {
                        case PopupField<string> popupField:
                            popupField.SetValueWithoutNotify(m_ValueGetter.Invoke().ToString());
                            return true;

                        case BaseField<Enum> baseField:
                            baseField.SetValueWithoutNotify(m_ValueGetter.Invoke() as Enum);
                            return true;

                        case BaseField<TValue> baseField:
                            baseField.SetValueWithoutNotify(m_ValueGetter.Invoke());
                            return true;
                    }
                }

            }

            return false;
        }

        VisualElement CreateFieldFromProperty(string propertyName, string fieldTooltip)
        {
            if (m_CustomFieldBuilder == null)
            {
                TryCreateCustomPropertyFieldBuilder(out m_CustomFieldBuilder);
            }

            return m_CustomFieldBuilder?.Build(CommandTarget, Label, fieldTooltip, Models, propertyName) ?? CreateDefaultFieldForType(typeof(TValue), fieldTooltip);
        }
    }
}
