// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.CommandStateObserver;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A field to edit the value of an <see cref="Constant{T}"/>.
    /// </summary>
    class ConstantField : CustomizableModelPropertyField
    {
        public static readonly string connectedModifierUssClassName = ussClassName.WithUssModifier("connected");

        ICustomPropertyFieldBuilder m_CustomFieldBuilder;

        /// <summary>
        /// The root element of the UI.
        /// </summary>
        protected VisualElement m_Field;

        /// <summary>
        /// The constants edited by the field.
        /// </summary>
        public IEnumerable<Constant> ConstantModels { get; }

        /// <summary>
        /// The owners of the <see cref="ConstantModels"/>.
        /// </summary>
        /// <remarks>The owners will be passed to the command that is dispatched when the constant value is modified,
        /// giving them the opportunity to update themselves.</remarks>
        public IEnumerable<GraphElementModel> Owners { get; }

        public ConstantField(IEnumerable<Constant> constantModels, IEnumerable<GraphElementModel> owners,
            ICommandTarget commandTarget, string label = null)
            : base(commandTarget, label)
        {
            ConstantModels = constantModels;
            Owners = owners;
            m_Field = CreateField();

            SetFieldChangedCallback();

            this.AddStylesheet_Internal("ConstantField.uss");

            if (m_Field != null)
            {
                hierarchy.Add(m_Field);
            }
        }

        void SetFieldChangedCallback()
        {
            if (m_Field == null)
                return;

            var registerCallbackMethod = GetRegisterCallback_Internal();
            if (registerCallbackMethod != null)
            {
                void EventCallback(IChangeEvent e, ConstantField f)
                {
                    if (e != null) // Enum editor sends null
                    {
                        if (m_IgnoreEventBecauseMixedIsBuggish)
                            return;
                        var newValue = GetNewValue(e);
                        var command = new UpdateConstantsValueCommand(ConstantModels, newValue, Owners);
                        f.CommandTarget.Dispatch(command);
                    }
                }

                registerCallbackMethod.Invoke(m_Field, new object[] { (EventCallback<IChangeEvent, ConstantField>)EventCallback, this, TrickleDown.NoTrickleDown });
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

        internal MethodInfo GetRegisterCallback_Internal()
        {
            // PF TODO when this is a module, submit modifications to UIToolkit to avoid having to do reflection.

            var registerCallbackMethod = typeof(CallbackEventHandler)
                .GetMethods(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance)
                .SingleOrDefault(m => m.Name == nameof(RegisterCallback) && m.GetGenericArguments().Length == 2);

            if (registerCallbackMethod == null)
                return null;

            var t = ConstantModels.First().Type == typeof(EnumValueReference) ? typeof(Enum) : ModelHelpers.GetCommonBaseType(ConstantModels.Select(t => t.ObjectValue));

            // Fix for: https://jira.unity3d.com/projects/GTF/issues/GTF-748
            // objects such as Texture2D that inherit from UnityEngine.Object send a ChangeEvent<UnityEngine.Object>
            if (t.IsSubclassOf(typeof(Object)))
                t = typeof(Object);

            var changeEventType = typeof(ChangeEvent<>).MakeGenericType(t);
            return registerCallbackMethod.MakeGenericMethod(changeEventType, typeof(ConstantField));
        }

        bool m_IgnoreEventBecauseMixedIsBuggish = false;

        /// <inheritdoc />
        public override bool UpdateDisplayedValue()
        {
            if (m_Field == null)
                return false;


            bool isConnected = false;
            if (Owners != null)
            {
                foreach (var owner in Owners)
                {
                    var portModel = owner as PortModel;
                    if (portModel != null && portModel.IsConnected() && portModel.GetConnectedPorts().Any(p => p.NodeModel.State == ModelState.Enabled))
                    {
                        isConnected = true;
                        break;
                    }
                }
            }

            m_Field.EnableInClassList(connectedModifierUssClassName, isConnected);
            m_Field.SetEnabled(!isConnected);

            // PF TODO when this is a module, submit modifications to UIToolkit to avoid having to do reflection.
            var field = m_Field.SafeQ(null, BaseField<int>.ussClassName);
            var fieldType = field.GetType();
            try
            {
                bool same = true;

                var displayedValue = ConstantModels.First();

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
                        foreach (var subField in field.Query().ToList().OfType<IMixedValueSupport>())
                        {
                            subField.showMixedValue = true;
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
                    var setValueMethod = fieldType.GetMethod("SetValueWithoutNotify");
                    var value = displayedValue.Type == typeof(EnumValueReference) ?
                        ((EnumValueReference)displayedValue.ObjectValue).ValueAsEnum() : displayedValue.ObjectValue;
                    setValueMethod?.Invoke(field, new[] { value });
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        VisualElement CreateField()
        {
            var fieldType = ConstantModels.First().GetTypeHandle().Resolve();

            string tooltipString = null;
            if (Owners != null && Owners.OfType<PortModel>().Any())
            {
                tooltipString = Owners.OfType<PortModel>().First().ToolTip;

                foreach (var port in Owners.OfType<PortModel>().Skip(1))
                {
                    if (port.ToolTip != tooltipString)
                    {
                        tooltipString = "";
                        break;
                    }
                }
            }

            if (m_CustomFieldBuilder == null)
            {
                TryCreateCustomPropertyFieldBuilder(fieldType, out m_CustomFieldBuilder);
            }

            return m_CustomFieldBuilder?.Build(CommandTarget, Label, tooltipString, ConstantModels.Select(t => t.ObjectValue), nameof(Constant.ObjectValue)) ?? CreateDefaultFieldForType(fieldType, tooltipString);
        }
    }
}
