// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Scripting;

namespace Unity.Properties
{
    /// <summary>
    /// Describes how a new instance is created.
    /// </summary>
    public enum InstantiationKind
    {
        /// <summary>
        /// The type instantiation will be done using <see cref="Activator"/>.
        /// </summary>
        Activator,

        /// <summary>
        /// The type instantiation will be done via a method override in <see cref="PropertyBag{TContainer}"/>
        /// </summary>
        PropertyBagOverride,

        /// <summary>
        /// Not type instantiation should be performed for this type.
        /// </summary>
        NotInstantiatable
    }

    interface IConstructor
    {
        /// <summary>
        /// Returns <see langword="true"/> if the type can be instantiated.
        /// </summary>
        InstantiationKind InstantiationKind { get; }
    }

    /// <summary>
    /// The <see cref="IConstructor{T}"/> provides a type instantiation implementation for a given <typeparamref name="T"/> type. This is an internal interface.
    /// </summary>
    /// <typeparam name="T">The type to be instantiated.</typeparam>
    interface IConstructor<out T> : IConstructor
    {
        /// <summary>
        /// Construct an instance of <typeparamref name="T"/> and returns it.
        /// </summary>
        /// <returns>A new instance of type <typeparamref name="T"/>.</returns>
        T Instantiate();
    }

    /// <summary>
    /// The <see cref="IConstructorWithCount{T}"/> provides type instantiation for a collection <typeparamref name="T"/> type with a count. This is an internal interface.
    /// </summary>
    /// <typeparam name="T">The type to be instantiated.</typeparam>
    interface IConstructorWithCount<out T> : IConstructor
    {
        /// <summary>
        /// Construct an instance of <typeparamref name="T"/> and returns it.
        /// </summary>
        /// <returns>A new instance of type <typeparamref name="T"/>.</returns>
        T InstantiateWithCount(int count);
    }

    /// <summary>
    /// Utility class around <see cref="System.Type"/>.
    /// </summary>
    public static class TypeUtility
    {
        interface ITypeConstructor
        {
            /// <summary>
            /// Returns <see langword="true"/> if the type can be instantiated.
            /// </summary>
            bool CanBeInstantiated { get; }

            /// <summary>
            /// Construct an instance of the underlying type.
            /// </summary>
            /// <returns>A new instance of concrete type.</returns>
            object Instantiate();
        }

        interface ITypeConstructor<T> : ITypeConstructor
        {
            /// <summary>
            /// Construct an instance of <typeparamref name="T"/> and returns it.
            /// </summary>
            /// <returns>A new instance of type <typeparamref name="T"/>.</returns>
            new T Instantiate();

            /// <summary>
            /// Sets an explicit instantiation method for the <typeparamref name="T"/> type.
            /// </summary>
            /// <param name="constructor">The instantiation method.</param>
            void SetExplicitConstructor(Func<T> constructor);
        }

        class TypeConstructor<T> : ITypeConstructor<T>
        {
            /// <summary>
            /// An explicit user defined constructor for <typeparamref name="T"/>.
            /// </summary>
            Func<T> m_ExplicitConstructor;

            /// <summary>
            /// An implicit constructor relying on <see cref="Activator.CreateInstance{T}"/>
            /// </summary>
            Func<T> m_ImplicitConstructor;

            /// <summary>
            /// An explicit constructor provided by an interface implementation. This is used to provide type construction through property bags.
            /// </summary>
            IConstructor<T> m_OverrideConstructor;

            /// <inheritdoc/>
            bool ITypeConstructor.CanBeInstantiated
            {
                get
                {
                    if (null != m_ExplicitConstructor)
                        return true;

                    if (null != m_OverrideConstructor)
                    {
                        if (m_OverrideConstructor.InstantiationKind == InstantiationKind.NotInstantiatable)
                            return false;

                        if (m_OverrideConstructor.InstantiationKind == InstantiationKind.PropertyBagOverride)
                            return true;
                    }

                    return null != m_ImplicitConstructor;
                }
            }

            public TypeConstructor()
            {
                // Try to get a construction provider through the property bag.
                m_OverrideConstructor = Internal.PropertyBagStore.GetPropertyBag<T>() as IConstructor<T>;

                SetImplicitConstructor();
            }

            void SetImplicitConstructor()
            {
                var type = typeof(T);

                if (type.IsValueType)
                {
                    m_ImplicitConstructor = CreateValueTypeInstance;
                    return;
                }

                if (type.IsAbstract)
                {
                    return;
                }

                if (typeof(UnityEngine.ScriptableObject).IsAssignableFrom(type))
                {
                    m_ImplicitConstructor = CreateScriptableObjectInstance;
                    return;
                }

                if (null != type.GetConstructor(Array.Empty<Type>()))
                {
                    m_ImplicitConstructor = CreateClassInstance;
                }
            }

            static T CreateValueTypeInstance()
            {
                return default;
            }

            static T CreateScriptableObjectInstance()
            {
                return (T) (object) UnityEngine.ScriptableObject.CreateInstance(typeof(T));
            }

            static T CreateClassInstance()
            {
                return Activator.CreateInstance<T>();
            }

            /// <inheritdoc/>
            public void SetExplicitConstructor(Func<T> constructor)
            {
                m_ExplicitConstructor = constructor;
            }

            /// <inheritdoc/>
            T ITypeConstructor<T>.Instantiate()
            {
                // First try an explicit constructor set by users.
                if (null != m_ExplicitConstructor)
                    return m_ExplicitConstructor.Invoke();

                // Try custom constructor provided by the property bag.
                if (null != m_OverrideConstructor)
                {
                    if (m_OverrideConstructor.InstantiationKind == InstantiationKind.NotInstantiatable)
                        throw new InvalidOperationException($"The type '{typeof(T).Name}' is not constructable.");

                    if (m_OverrideConstructor.InstantiationKind == InstantiationKind.PropertyBagOverride)
                    {
                        return m_OverrideConstructor.Instantiate();
                    }
                }

                // Use the implicit construction provided by Activator.
                if (null != m_ImplicitConstructor)
                    return m_ImplicitConstructor.Invoke();

                throw new InvalidOperationException($"The type '{typeof(T).Name}' is not constructable.");
            }

            /// <inheritdoc/>
            object ITypeConstructor.Instantiate() => ((ITypeConstructor<T>) this).Instantiate();
        }

        /// <summary>
        /// The <see cref="NonConstructable"/> class can be used when we can't fully resolve a <see cref="TypeConstructor{T}"/> for a given type.
        /// This can happen if a given type has no property bag and we don't have a strong type to work with.
        /// </summary>
        class NonConstructable : ITypeConstructor
        {
            bool ITypeConstructor.CanBeInstantiated => false;
            public object Instantiate() => throw new InvalidOperationException($"The type is not instantiatable.");
        }

        /// <summary>
        /// The <see cref="Cache{T}"/> represents a strongly typed reference to a type constructor.
        /// </summary>
        /// <typeparam name="T">The type the constructor can initialize.</typeparam>
        /// <remarks>
        /// Any types in this set are also present in the <see cref="TypeUtility.s_TypeConstructors"/> set.
        /// </remarks>
        struct Cache<T>
        {
            /// <summary>
            /// Reference to the strongly typed <see cref="ITypeConstructor{TType}"/> for this type. This allows direct access without any dictionary lookups.
            /// </summary>
            public static ITypeConstructor<T> TypeConstructor;
        }

        /// <summary>
        /// The <see cref="TypeConstructorVisitor"/> is used to
        /// </summary>
        class TypeConstructorVisitor : ITypeVisitor
        {
            public ITypeConstructor TypeConstructor;

            public void Visit<TContainer>()
                => TypeConstructor = CreateTypeConstructor<TContainer>();
        }

        /// <summary>
        /// Provides untyped references to the <see cref="ITypeConstructor{TType}"/> implementations.
        /// </summary>
        /// <remarks>
        /// Any types in this set are also present in the <see cref="Cache{T}"/>.
        /// </remarks>
        static readonly ConcurrentDictionary<Type, ITypeConstructor> s_TypeConstructors = new ConcurrentDictionary<Type, ITypeConstructor>();

        static readonly System.Reflection.MethodInfo s_CreateTypeConstructor;

        static readonly ConcurrentDictionary<Type, string> s_CachedResolvedName;

        static readonly UnityEngine.Pool.ObjectPool<StringBuilder> s_Builders;
        private static readonly object syncedPoolObject = new object();


        static TypeUtility()
        {
            s_CachedResolvedName = new ConcurrentDictionary<Type, string>();
            s_Builders = new UnityEngine.Pool.ObjectPool<StringBuilder>(()=> new StringBuilder(), null, sb => sb.Clear());

            SetExplicitInstantiationMethod(() => string.Empty);
            foreach (var method in typeof(TypeUtility).GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static))
            {
                if (method.Name != nameof(CreateTypeConstructor) || !method.IsGenericMethod)
                    continue;

                s_CreateTypeConstructor = method;
                break;
            }

            if (null == s_CreateTypeConstructor)
                throw new InvalidProgramException();
        }

        /// <summary>
        /// Utility method to get the name of a type which includes the parent type(s).
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> we want the name of.</param>
        /// <returns>The display name of the type.</returns>
        public static string GetTypeDisplayName(Type type)
        {
            if (s_CachedResolvedName.TryGetValue(type, out var name))
                return name;

            var index = 0;
            name = GetTypeDisplayName(type, type.GetGenericArguments(), ref index);
            s_CachedResolvedName[type] = name;
            return name;
        }

        static string GetTypeDisplayName(Type type, IReadOnlyList<Type> args, ref int argIndex)
        {
            if (type == typeof(int))
                return "int";
            if (type == typeof(uint))
                return "uint";
            if (type == typeof(short))
                return "short";
            if (type == typeof(ushort))
                return "ushort";
            if (type == typeof(byte))
                return "byte";
            if (type == typeof(char))
                return "char";
            if (type == typeof(bool))
                return "bool";
            if (type == typeof(long))
                return "long";
            if (type == typeof(ulong))
                return "ulong";
            if (type == typeof(float))
                return "float";
            if (type == typeof(double))
                return "double";
            if (type == typeof(string))
                return "string";

            var name = type.Name;

            if (type.IsGenericParameter)
            {
                return name;
            }

            if (type.IsNested)
            {
                name = $"{GetTypeDisplayName(type.DeclaringType, args, ref argIndex)}.{name}";
            }

            if (!type.IsGenericType)
                return name;

            var tickIndex = name.IndexOf('`');

            var count = type.GetGenericArguments().Length;

            if (tickIndex > -1)
            {
                count = int.Parse(name.Substring(tickIndex + 1));
                name = name.Remove(tickIndex);
            }

            var genericTypeNames = default(StringBuilder);
            lock (syncedPoolObject)
            {
                genericTypeNames = s_Builders.Get();
            }

            try
            {
                for (var i = 0; i < count && argIndex < args.Count; i++, argIndex++)
                {
                    if (i != 0) genericTypeNames.Append(", ");
                    genericTypeNames.Append(GetTypeDisplayName(args[argIndex]));
                }

                if (genericTypeNames.Length > 0)
                {
                    name = $"{name}<{genericTypeNames}>";
                }
            }
            finally
            {
                lock (syncedPoolObject)
                {
                    s_Builders.Release(genericTypeNames);
                }
            }

            return name;
        }

        /// <summary>
        /// Utility method to return the base type.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> for which we want the base type.</param>
        /// <returns>The base type.</returns>
        public static Type GetRootType(this Type type)
        {
            if (type.IsInterface)
                return null;

            var baseType = type.IsValueType ? typeof(ValueType) : typeof(object);
            while (baseType != type.BaseType)
            {
                type = type.BaseType;
            }

            return type;
        }

        /// <summary>
        /// Creates a new strongly typed <see cref="TypeConstructor{TType}"/> for the specified <paramref name="type"/>.
        /// </summary>
        /// <remarks>
        /// This method will attempt to use properties to get the strongly typed reference. If no property bag exists it will fallback to a reflection based approach.
        /// </remarks>
        /// <param name="type">The type to create a constructor for.</param>
        /// <returns>A <see cref="TypeConstructor{TType}"/> for the specified type.</returns>
        [Preserve]
        static ITypeConstructor CreateTypeConstructor(Type type)
        {
            var properties = Internal.PropertyBagStore.GetPropertyBag(type);

            // Attempt to use properties double dispatch to call the strongly typed create method. This avoids expensive reflection calls.
            if (null != properties)
            {
                var visitor = new TypeConstructorVisitor();
                properties.Accept(visitor);
                return visitor.TypeConstructor;
            }

            if (type.ContainsGenericParameters)
            {
                var constructor = new NonConstructable();
                s_TypeConstructors[type] = constructor;
                return constructor;
            }

            // This type has no property bag associated with it. Fallback to reflection to create our type constructor.
            return s_CreateTypeConstructor
                .MakeGenericMethod(type)
                .Invoke(null, null) as ITypeConstructor;
        }

        /// <summary>
        /// Creates a new strongly typed <see cref="TypeConstructor{TType}"/> for the specified <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to create a constructor for.</typeparam>
        /// <returns>A <see cref="TypeConstructor{TType}"/> for the specified type.</returns>
        static ITypeConstructor<T> CreateTypeConstructor<T>()
        {
            var constructor = new TypeConstructor<T>();
            Cache<T>.TypeConstructor = constructor;
            s_TypeConstructors[typeof(T)] = constructor;
            return constructor;
        }

        /// <summary>
        /// Gets the internal <see cref="ITypeConstructor"/> for the specified <paramref name="type"/>.
        /// </summary>
        /// <remarks>
        /// This method will return null if the type is not constructable on the current platform.
        /// </remarks>
        /// <param name="type">The type to get a constructor for.</param>
        /// <returns>A <see cref="ITypeConstructor"/> for the specified type.</returns>
        static ITypeConstructor GetTypeConstructor(Type type)
        {
            return s_TypeConstructors.TryGetValue(type, out var constructor)
                ? constructor
                : CreateTypeConstructor(type);
        }

        /// <summary>
        /// Gets the internal <see cref="ITypeConstructor"/> for the specified <typeparamref name="T"/>.
        /// </summary>
        /// <remarks>
        /// This method will return null if the type is not constructable on the current platform.
        /// </remarks>
        /// <typeparam name="T">The type to create a constructor for.</typeparam>
        /// <returns>A <see cref="ITypeConstructor{TType}"/> for the specified type.</returns>
        static ITypeConstructor<T> GetTypeConstructor<T>()
        {
            return null != Cache<T>.TypeConstructor
                ? Cache<T>.TypeConstructor
                : CreateTypeConstructor<T>();
        }

        /// <summary>
        /// Returns <see langword="true"/> if the specified type is instantiatable.
        /// </summary>
        /// <remarks>
        /// Instantiatable is defined as either having a default or implicit constructor or having a registered instantiation method.
        /// </remarks>
        /// <param name="type">The type to query.</param>
        /// <returns><see langword="true"/> if the given type is instantiatable.</returns>
        public static bool CanBeInstantiated(Type type)
            => GetTypeConstructor(type).CanBeInstantiated;

        /// <summary>
        /// Returns <see langword="true"/> if type <typeparamref name="T"/> is instantiatable.
        /// </summary>
        /// <remarks>
        /// Instantiatable is defined as either having a default or implicit constructor or having a registered instantiation method.
        /// </remarks>
        /// <typeparam name="T">The type to query.</typeparam>
        /// <returns><see langword="true"/> if type <typeparamref name="T"/> is instantiatable.</returns>
        public static bool CanBeInstantiated<T>()
            => GetTypeConstructor<T>().CanBeInstantiated;

        /// <summary>
        /// Sets an explicit instantiation method for the <typeparamref name="T"/> type.
        /// </summary>
        /// <param name="constructor">The instantiation method.</param>
        /// <typeparam name="T">The type to set the explicit instantiation method.</typeparam>
        public static void SetExplicitInstantiationMethod<T>(Func<T> constructor)
            => GetTypeConstructor<T>().SetExplicitConstructor(constructor);

        /// <summary>
        /// Creates a new instance of the specified <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type we want to create a new instance of.</typeparam>
        /// <returns>A new instance of the <typeparamref name="T"/>.</returns>
        /// <exception cref="InvalidOperationException">The specified <typeparamref name="T"/> has no available instantiation method.</exception>
        public static T Instantiate<T>()
        {
            var constructor = GetTypeConstructor<T>();

            CheckCanBeInstantiated(constructor);

            return constructor.Instantiate();
        }

        /// <summary>
        /// Creates a new instance of the specified <typeparamref name="T"/>.
        /// </summary>
        /// <param name="instance">When this method returns, contains the created instance, if type instantiation succeeded; otherwise, the default value for <typeparamref name="T"/>.</param>
        /// <typeparam name="T">The type to create an instance of.</typeparam>
        /// <returns><see langword="true"/> if a new instance of type <typeparamref name="T"/> was created; otherwise, <see langword="false"/>.</returns>
        public static bool TryInstantiate<T>(out T instance)
        {
            var constructor = GetTypeConstructor<T>();

            if (constructor.CanBeInstantiated)
            {
                instance = constructor.Instantiate();
                return true;
            }

            instance = default;
            return false;
        }

        /// <summary>
        /// Creates a new instance of the given type type and returns it as <typeparamref name="T"/>.
        /// </summary>
        /// <param name="derivedType">The type we want to create a new instance of.</param>
        /// <typeparam name="T">The type we want to create a new instance of.</typeparam>
        /// <returns>a new instance of the <typeparamref name="T"/> type.</returns>
        /// <exception cref="ArgumentException">Thrown when the given type is not assignable to <typeparamref name="T"/>.</exception>
        public static T Instantiate<T>(Type derivedType)
        {
            var constructor = GetTypeConstructor(derivedType);

            CheckIsAssignableFrom(typeof(T), derivedType);
            CheckCanBeInstantiated(constructor, derivedType);

            return (T) constructor.Instantiate();
        }

        /// <summary>
        /// Tries to create a new instance of the given type type and returns it as <typeparamref name="T"/>.
        /// </summary>
        /// <param name="derivedType">The type we want to create a new instance of.</param>
        /// <param name="value">When this method returns, contains the created instance, if type instantiation succeeded; otherwise, the default value for <typeparamref name="T"/>.</param>
        /// <typeparam name="T">The type we want to create a new instance of.</typeparam>
        /// <returns><see langword="true"/> if a new instance of the given type could be created.</returns>
        public static bool TryInstantiate<T>(Type derivedType, out T value)
        {
            if (!typeof(T).IsAssignableFrom(derivedType))
            {
                value = default;
                value = default;
                return false;
            }

            var constructor = GetTypeConstructor(derivedType);

            if (!constructor.CanBeInstantiated)
            {
                value = default;
                return false;
            }

            value = (T) constructor.Instantiate();
            return true;
        }

        /// <summary>
        /// Creates a new instance of an array with the given count.
        /// </summary>
        /// <param name="count">The size of the array to instantiate.</param>
        /// <typeparam name="TArray">The array type to instantiate.</typeparam>
        /// <returns>The array newly created array.</returns>
        /// <exception cref="ArgumentException">Thrown is count is negative or if <typeparamref name="TArray"/> is not an array type.</exception>
        public static TArray InstantiateArray<TArray>(int count = 0)
        {
            if (count < 0)
            {
                throw new ArgumentException($"{nameof(TypeUtility)}: Cannot construct an array with {nameof(count)}={count}");
            }

            var properties = Internal.PropertyBagStore.GetPropertyBag<TArray>();

            if (properties is IConstructorWithCount<TArray> constructor)
            {
                return constructor.InstantiateWithCount(count);
            }

            var type = typeof(TArray);

            if (!type.IsArray)
            {
                throw new ArgumentException($"{nameof(TypeUtility)}: Cannot construct an array, since {typeof(TArray).Name} is not an array type.");
            }

            var elementType = type.GetElementType();
            if (null == elementType)
            {
                throw new ArgumentException($"{nameof(TypeUtility)}: Cannot construct an array, since {typeof(TArray).Name}.{nameof(Type.GetElementType)}() returned null.");
            }

            return (TArray) (object) Array.CreateInstance(elementType, count);
        }

        /// <summary>
        /// Tries to create a new instance of an array with the given count.
        /// </summary>
        /// <param name="count">The count the array should have.</param>
        /// <param name="instance">When this method returns, contains the created instance, if type instantiation succeeded; otherwise, the default value for <typeparamref name="TArray"/>.</param>
        /// <typeparam name="TArray">The array type.</typeparam>
        /// <returns><see langword="true"/> if the type was instantiated; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentException">Thrown is count is negative or if <typeparamref name="TArray"/> is not an array type.</exception>
        public static bool TryInstantiateArray<TArray>(int count, out TArray instance)
        {
            if (count < 0)
            {
                instance = default;
                return false;
            }

            var properties = Internal.PropertyBagStore.GetPropertyBag<TArray>();

            if (properties is IConstructorWithCount<TArray> constructor)
            {
                try
                {
                    instance = constructor.InstantiateWithCount(count);
                    return true;
                }
                catch
                {
                    // continue
                }
            }

            var type = typeof(TArray);

            if (!type.IsArray)
            {
                instance = default;
                return false;
            }

            var elementType = type.GetElementType();

            if (null == elementType)
            {
                instance = default;
                return false;
            }

            instance = (TArray) (object) Array.CreateInstance(elementType, count);
            return true;
        }

        /// <summary>
        /// Creates a new instance of an array with the given type and given count.
        /// </summary>
        /// <param name="derivedType">The type we want to create a new instance of.</param>
        /// <param name="count">The size of the array to instantiate.</param>
        /// <typeparam name="TArray">The array type to instantiate.</typeparam>
        /// <returns>The array newly created array.</returns>
        /// <exception cref="ArgumentException">Thrown is count is negative or if <typeparamref name="TArray"/> is not an array type.</exception>
        public static TArray InstantiateArray<TArray>(Type derivedType, int count = 0)
        {
            if (count < 0)
            {
                throw new ArgumentException($"{nameof(TypeUtility)}: Cannot instantiate an array with {nameof(count)}={count}");
            }

            var properties = Internal.PropertyBagStore.GetPropertyBag(derivedType);

            if (properties is IConstructorWithCount<TArray> constructor)
            {
                return constructor.InstantiateWithCount(count);
            }

            var type = typeof(TArray);

            if (!type.IsArray)
            {
                throw new ArgumentException($"{nameof(TypeUtility)}: Cannot instantiate an array, since {typeof(TArray).Name} is not an array type.");
            }

            var elementType = type.GetElementType();
            if (null == elementType)
            {
                throw new ArgumentException($"{nameof(TypeUtility)}: Cannot instantiate an array, since {typeof(TArray).Name}.{nameof(Type.GetElementType)}() returned null.");
            }

            return (TArray) (object) Array.CreateInstance(elementType, count);
        }

        static void CheckIsAssignableFrom(Type type, Type derivedType)
        {
            if (!type.IsAssignableFrom(derivedType))
                throw new ArgumentException($"Could not create instance of type `{derivedType.Name}` and convert to `{type.Name}`: The given type is not assignable to target type.");
        }

        static void CheckCanBeInstantiated<T>(ITypeConstructor<T> constructor)
        {
            if (!constructor.CanBeInstantiated)
                throw new InvalidOperationException($"Type `{typeof(T).Name}` could not be instantiated. A parameter-less constructor or an explicit construction method is required.");
        }

        static void CheckCanBeInstantiated(ITypeConstructor constructor, Type type)
        {
            if (!constructor.CanBeInstantiated)
            {
                throw new InvalidOperationException($"Type `{type.Name}` could not be instantiated. A parameter-less constructor or an explicit construction method is required.");
            }
        }
    }
}
