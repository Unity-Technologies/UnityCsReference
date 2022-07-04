// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;

using System.Reflection.Emit;

namespace Unity.Properties
{
    /// <summary>
    /// Common interface between <see cref="FieldInfo"/> and <see cref="PropertyInfo"/> for getting and setting values.
    /// </summary>
    interface IMemberInfo
    {
        /// <summary>
        /// Gets the reflected <see cref="MemberInfo"/> name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the value indicating if this member is read-only.
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        /// Gets the value type for the getter and setter.
        /// </summary>
        Type ValueType { get; }

        /// <summary>
        /// Gets the value of the member for the given object.
        /// </summary>
        /// <param name="obj">The object whose member value will be returned.</param>
        /// <returns>An object containing the value of the member reflected by this instance.</returns>
        object GetValue(object obj);

        /// <summary>
        /// Sets the value of the member for the given object to the given value.
        /// </summary>
        /// <param name="obj">The object whose member value will be set.</param>
        /// <param name="value">The value to assign to the member.</param>
        void SetValue(object obj, object value);

        /// <summary>
        /// Retrieves a collection of custom attributes that are applied to this member.
        /// </summary>
        /// <returns>A collection of the custom attributes that are applied this member, or an empty collection if no such attributes exist.</returns>
        IEnumerable<Attribute> GetCustomAttributes();
    }

    readonly struct FieldMember : IMemberInfo
    {
        internal readonly FieldInfo m_FieldInfo;

        /// <summary>
        /// Initializes a new <see cref="FieldMember"/> instance.
        /// </summary>
        /// <param name="fieldInfo">The backing <see cref="FieldInfo"/> object.</param>
        public FieldMember(FieldInfo fieldInfo)
        {
            m_FieldInfo = fieldInfo;
        }

        /// <inheritdoc/>
        public string Name => m_FieldInfo.Name;

        /// <inheritdoc/>
        public bool IsReadOnly => m_FieldInfo.IsInitOnly;

        /// <inheritdoc/>
        public Type ValueType => m_FieldInfo.FieldType;

        /// <inheritdoc/>
        public object GetValue(object obj) => m_FieldInfo.GetValue(obj);

        /// <inheritdoc/>
        public void SetValue(object obj, object value) => m_FieldInfo.SetValue(obj, value);

        /// <inheritdoc/>
        public IEnumerable<Attribute> GetCustomAttributes() => m_FieldInfo.GetCustomAttributes();
    }

    readonly struct PropertyMember : IMemberInfo
    {
        internal readonly PropertyInfo m_PropertyInfo;

        /// <inheritdoc/>
        public string Name => m_PropertyInfo.Name;

        /// <inheritdoc/>
        public bool IsReadOnly => !m_PropertyInfo.CanWrite;

        /// <inheritdoc/>
        public Type ValueType => m_PropertyInfo.PropertyType;

        /// <summary>
        /// Initializes a new <see cref="PropertyMember"/> instance.
        /// </summary>
        /// <param name="propertyInfo">The backing <see cref="PropertyInfo"/> object.</param>
        public PropertyMember(PropertyInfo propertyInfo) => m_PropertyInfo = propertyInfo;

        /// <inheritdoc/>
        public object GetValue(object obj) => m_PropertyInfo.GetValue(obj);

        /// <inheritdoc/>
        public void SetValue(object obj, object value) => m_PropertyInfo.SetValue(obj, value);

        /// <inheritdoc/>
        public IEnumerable<Attribute> GetCustomAttributes() => m_PropertyInfo.GetCustomAttributes();
    }

    /// <summary>
    /// A <see cref="ReflectedMemberProperty{TContainer,TValue}"/> provides strongly typed access to an underlying <see cref="FieldInfo"/> or <see cref="PropertyInfo"/> object.
    /// </summary>
    /// <remarks>
    /// The implementation uses slow reflection calls internally. This is intended to be used as an intermediate solution for quick editor iteration.
    /// </remarks>
    /// <typeparam name="TContainer">The container type for this property.</typeparam>
    /// <typeparam name="TValue">The value type for this property.</typeparam>
    public class ReflectedMemberProperty<TContainer, TValue> : Property<TContainer, TValue>
    {
        readonly IMemberInfo m_Info;
        readonly bool m_IsStructContainerType;

        delegate TValue GetStructValueAction(ref TContainer container);
        delegate void SetStructValueAction(ref TContainer container, TValue value);
        delegate TValue GetClassValueAction(TContainer container);
        delegate void SetClassValueAction(TContainer container, TValue value);

        GetStructValueAction m_GetStructValueAction;
        SetStructValueAction m_SetStructValueAction;
        GetClassValueAction m_GetClassValueAction;
        SetClassValueAction m_SetClassValueAction;

        /// <inheritdoc/>
        public override string Name { get; }

        /// <inheritdoc/>
        public override bool IsReadOnly { get; }

        /// <summary>
        /// Initializes a new <see cref="ReflectedMemberProperty{TContainer,TValue}"/> instance for the specified <see cref="FieldInfo"/>.
        /// </summary>
        /// <param name="info">The system reflection field info.</param>
        /// <param name="name">Use this name property--this might override the MemberInfo name</param>
        public ReflectedMemberProperty(FieldInfo info, string name) : this(new FieldMember(info), name)
        {

        }

        /// <summary>
        /// Initializes a new <see cref="ReflectedMemberProperty{TContainer,TValue}"/> instance for the specified <see cref="PropertyInfo"/>.
        /// </summary>
        /// <param name="info">The system reflection property info.</param>
        /// <param name="name">Use this name property--this might override the MemberInfo name</param>
        public ReflectedMemberProperty(PropertyInfo info, string name) : this(new PropertyMember(info), name)
        {

        }

        /// <summary>
        /// Initializes a new <see cref="ReflectedMemberProperty{TContainer,TValue}"/> instance. This is an internal constructor.
        /// </summary>
        /// <param name="info">The reflected info object backing this property.</param>
        /// <param name="name">Use this name property--this might override the MemberInfo name</param>
        internal ReflectedMemberProperty(IMemberInfo info, string name)
        {
            Name = name;
            m_Info = info;
            m_IsStructContainerType = TypeTraits<TContainer>.IsValueType;

            AddAttributes(info.GetCustomAttributes());
            var isReadOnly = m_Info.IsReadOnly || HasAttribute<ReadOnlyAttribute>();
            IsReadOnly = isReadOnly;

            if (m_Info is FieldMember fieldMember)
            {
                // TODO: optimize for NET_STANDARD, where DynamicMethod is not available by default
                var fieldInfo = fieldMember.m_FieldInfo;

                // getter
                var dynamicMethod = new DynamicMethod(string.Empty, fieldInfo.FieldType, new Type[]
                {
                    m_IsStructContainerType ? fieldInfo.ReflectedType.MakeByRefType() : fieldInfo.ReflectedType
                }, true);

                var ilGenerator = dynamicMethod.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldfld, fieldInfo);
                ilGenerator.Emit(OpCodes.Ret);

                if (m_IsStructContainerType)
                    m_GetStructValueAction = (GetStructValueAction)dynamicMethod.CreateDelegate(typeof(GetStructValueAction));
                else
                    m_GetClassValueAction = (GetClassValueAction)dynamicMethod.CreateDelegate(typeof(GetClassValueAction));

                // settter
                if (!isReadOnly)
                {
                    dynamicMethod = new DynamicMethod(string.Empty, typeof(void), new Type[]
                    {
                        m_IsStructContainerType ? fieldInfo.ReflectedType.MakeByRefType() : fieldInfo.ReflectedType,
                        fieldInfo.FieldType
                    }, true);

                    ilGenerator = dynamicMethod.GetILGenerator();
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    ilGenerator.Emit(OpCodes.Ldarg_1);
                    ilGenerator.Emit(OpCodes.Stfld, fieldInfo);
                    ilGenerator.Emit(OpCodes.Ret);

                    if (m_IsStructContainerType)
                        m_SetStructValueAction = (SetStructValueAction)dynamicMethod.CreateDelegate(typeof(SetStructValueAction));
                    else
                        m_SetClassValueAction = (SetClassValueAction)dynamicMethod.CreateDelegate(typeof(SetClassValueAction));
                }
            }
            else if (m_Info is PropertyMember propertyMember)
            {
                if (m_IsStructContainerType)
                {
                    var getMethod = propertyMember.m_PropertyInfo.GetGetMethod(true);
                    m_GetStructValueAction = (GetStructValueAction)Delegate.CreateDelegate(typeof(GetStructValueAction), getMethod);
                    if (!isReadOnly)
                    {
                        var setMethod = propertyMember.m_PropertyInfo.GetSetMethod(true);
                        m_SetStructValueAction = (SetStructValueAction)Delegate.CreateDelegate(typeof(SetStructValueAction), setMethod);
                    }
                }
                else
                {
                    var getMethod = propertyMember.m_PropertyInfo.GetGetMethod(true);
                    m_GetClassValueAction = (GetClassValueAction)Delegate.CreateDelegate(typeof(GetClassValueAction), getMethod);
                    if (!isReadOnly)
                    {
                        var setMethod = propertyMember.m_PropertyInfo.GetSetMethod(true);
                        m_SetClassValueAction = (SetClassValueAction)Delegate.CreateDelegate(typeof(SetClassValueAction), setMethod);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override TValue GetValue(ref TContainer container)
        {
            if (m_IsStructContainerType)
            {
                return m_GetStructValueAction == null
                    ? (TValue)m_Info.GetValue(container) // boxing
                    : m_GetStructValueAction(ref container); // no boxing
            }
            else
            {
                return m_GetClassValueAction == null
                    ? (TValue)m_Info.GetValue(container) // boxing
                    : m_GetClassValueAction(container); // no boxing
            }
        }

        /// <inheritdoc/>
        public override void SetValue(ref TContainer container, TValue value)
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException("Property is ReadOnly.");
            }

            if (m_IsStructContainerType)
            {
                if (m_SetStructValueAction == null)
                {
                    // boxing
                    var boxed = (object)container;
                    m_Info.SetValue(boxed, value);
                    container = (TContainer)boxed;
                }
                else
                {
                    // no boxing
                    m_SetStructValueAction(ref container, value);
                }
            }
            else
            {
                if (m_SetClassValueAction == null)
                {
                    // boxing
                    m_Info.SetValue(container, value);
                }
                else
                {
                    // no boxing
                    m_SetClassValueAction(container, value);
                }
            }
        }
    }
}
