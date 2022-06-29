// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;

namespace Unity.Properties
{
    /// <summary>
    /// Helper class to avoid paying the cost of runtime type lookups.
    /// </summary>
    public static class TypeTraits
    {
        /// <summary>
        /// Returns <see lanword="true"/> if the given type can be treated as a container. i.e. not primitive, pointer, enum or string.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns><see lanword="true"/> if the given type is a container type; <see lanword="false"/> otherwise.</returns>
        /// <exception cref="ArgumentNullException">The given type is null.</exception>
        public static bool IsContainer(Type type)
        {
            if (null == type)
                throw new ArgumentNullException(nameof(type));
            return !(type.IsPrimitive || type.IsPointer || type.IsEnum || type == typeof(string));
        }
    }

    /// <summary>
    /// Helper class to avoid paying the cost of runtime type lookups.
    ///
    /// This is also used to abstract underlying type info in the runtime (e.g. RuntimeTypeHandle vs StaticTypeReg)
    /// </summary>
    public static class TypeTraits<T>
    {
        /// <summary>
        /// Gets a value indicating whether <see cref="T"/> is a value type.
        /// </summary>
        public static bool IsValueType { get; }

        /// <summary>
        /// Gets a value indicating whether <see cref="T"/> is a primitive type.
        /// </summary>
        public static bool IsPrimitive { get; }

        /// <summary>
        /// Gets a value indicating whether <see cref="T"/> is an interface type.
        /// </summary>
        public static bool IsInterface { get; }

        /// <summary>
        /// Gets a value indicating whether <see cref="T"/> is an abstract type.
        /// </summary>
        public static bool IsAbstract { get; }

        /// <summary>
        /// Gets a value indicating whether <see cref="T"/> is an array type.
        /// </summary>
        public static bool IsArray { get; }

        /// <summary>
        /// Gets a value indicating whether <see cref="T"/> is a multidimensional array type.
        /// </summary>
        public static bool IsMultidimensionalArray { get; }

        /// <summary>
        /// Gets a value indicating whether <see cref="T"/> is an enum type.
        /// </summary>
        public static bool IsEnum { get; }

        /// <summary>
        /// Gets a value indicating whether <see cref="T"/> is an flags enum type.
        /// </summary>
        public static bool IsEnumFlags { get; }

        /// <summary>
        /// Gets a value indicating whether <see cref="T"/> is a nullable type.
        /// </summary>
        public static bool IsNullable { get; }

        /// <summary>
        /// Gets a value indicating whether <see cref="T"/> is <see cref="Object"/> type.
        /// </summary>
        public static bool IsObject { get; }

        /// <summary>
        /// Gets a value indicating whether <see cref="T"/> is <see cref="string"/> type.
        /// </summary>
        public static bool IsString { get; }

        /// <summary>
        /// Gets a value indicating whether <see cref="T"/> is a property container type.
        /// </summary>
        public static bool IsContainer { get; }

        /// <summary>
        /// Gets a value indicating whether <see cref="T"/> can be null. i.e. The type is an object or nullable.
        /// </summary>
        public static bool CanBeNull { get; }

        /// <summary>
        /// Gets a value indicating whether <see cref="T"/> is a primitive or <see cref="string"/> type.
        /// </summary>
        public static bool IsPrimitiveOrString { get; }

        /// <summary>
        /// Gets a value indicating whether <see cref="T"/> is an abstract or interface type.
        /// </summary>
        public static bool IsAbstractOrInterface { get; }

        /// <summary>
        /// Gets a value indicating whether <see cref="T"/> is a <see cref="UnityEngine.Object"/> type.
        /// </summary>
        public static bool IsUnityObject { get; }

        /// <summary>
        /// Gets a value indicating whether <see cref="T"/> is a <see cref="UnityEngine.LazyLoadReference{TValue}"/> type.
        /// </summary>
        public static bool IsLazyLoadReference { get; }

        static TypeTraits()
        {
            var type = typeof(T);
            IsValueType = type.IsValueType;
            IsPrimitive = type.IsPrimitive;
            IsInterface = type.IsInterface;
            IsAbstract = type.IsAbstract;
            IsArray = type.IsArray;
            IsEnum = type.IsEnum;

            IsEnumFlags = IsEnum && null != type.GetCustomAttribute<FlagsAttribute>();
            IsNullable = Nullable.GetUnderlyingType(typeof(T)) != null;
            IsMultidimensionalArray = IsArray && typeof(T).GetArrayRank() != 1;
            IsObject = type == typeof(object);
            IsString = type == typeof(string);
            IsContainer = TypeTraits.IsContainer(type);

            CanBeNull = !IsValueType;
            IsPrimitiveOrString = IsPrimitive || IsString;
            IsAbstractOrInterface = IsAbstract || IsInterface;

            CanBeNull |= IsNullable;

            IsLazyLoadReference = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(UnityEngine.LazyLoadReference<>);
            IsUnityObject = typeof(UnityEngine.Object).IsAssignableFrom(type);
        }
    }
}
