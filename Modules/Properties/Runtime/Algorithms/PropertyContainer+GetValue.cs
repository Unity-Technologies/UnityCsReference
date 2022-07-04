// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Properties
{
    public static partial class PropertyContainer
    {
        class GetValueVisitor<TSrcValue> : PathVisitor
        {
            public static readonly UnityEngine.Pool.ObjectPool<GetValueVisitor<TSrcValue>> Pool = new UnityEngine.Pool.ObjectPool<GetValueVisitor<TSrcValue>>(() => new GetValueVisitor<TSrcValue>(), null, v => v.Reset());
            public TSrcValue Value;

            public override void Reset()
            {
                base.Reset();
                Value = default;
                ReadonlyVisit = true;
            }

            protected override void VisitPath<TContainer, TValue>(Property<TContainer, TValue> property, ref TContainer container, ref TValue value)
            {
                if (!TypeConversion.TryConvert(ref value, out Value))
                {
                    ReturnCode = VisitReturnCode.InvalidCast;
                }
            }
        }

        /// <summary>
        /// Gets the value of a property by name.
        /// </summary>
        /// <param name="container">The container whose property value will be returned.</param>
        /// <param name="name">The name of the property to get.</param>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <typeparam name="TContainer">The container type.</typeparam>
        /// <returns>The value for the specified name.</returns>
        /// <exception cref="ArgumentNullException">The specified container or path is null.</exception>
        /// <exception cref="InvalidContainerTypeException">The specified container type is not valid for visitation.</exception>
        /// <exception cref="MissingPropertyBagException">The specified container type has no property bag associated with it.</exception>
        /// <exception cref="InvalidCastException">The specified <typeparamref name="TValue"/> could not be assigned to the property.</exception>
        /// <exception cref="InvalidPathException">The specified <paramref name="name"/> was not found or could not be resolved.</exception>
        public static TValue GetValue<TContainer, TValue>(TContainer container, string name)
            => GetValue<TContainer, TValue>(ref container, name);

        /// <summary>
        /// Gets the value of a property by name.
        /// </summary>
        /// <param name="container">The container whose property value will be returned.</param>
        /// <param name="name">The name of the property to get.</param>
        /// <typeparam name="TContainer">The container type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <returns>The value for the specified name.</returns>
        /// <exception cref="ArgumentNullException">The specified container or path is null.</exception>
        /// <exception cref="InvalidContainerTypeException">The specified container type is not valid for visitation.</exception>
        /// <exception cref="MissingPropertyBagException">The specified container type has no property bag associated with it.</exception>
        /// <exception cref="InvalidCastException">The specified <typeparamref name="TValue"/> could not be assigned to the property.</exception>
        /// <exception cref="InvalidPathException">The specified <paramref name="name"/> was not found or could not be resolved.</exception>
        public static TValue GetValue<TContainer, TValue>(ref TContainer container, string name)
        {
            var path = new PropertyPath(name);
            return GetValue<TContainer, TValue>(ref container, path);
        }

        /// <summary>
        /// Gets the value of a property by path.
        /// </summary>
        /// <param name="container">The container whose property value will be returned.</param>
        /// <param name="path">The path of the property to get.</param>
        /// <typeparam name="TContainer">The container type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <returns>The value at the specified path.</returns>
        /// <exception cref="ArgumentNullException">The specified container or path is null.</exception>
        /// <exception cref="InvalidContainerTypeException">The specified container type is not valid for visitation.</exception>
        /// <exception cref="MissingPropertyBagException">The specified container type has no property bag associated with it.</exception>
        /// <exception cref="InvalidCastException">The specified <typeparamref name="TValue"/> could not be assigned to the property.</exception>
        /// <exception cref="InvalidPathException">The specified <paramref name="path"/> was not found or could not be resolved.</exception>
        public static TValue GetValue<TContainer, TValue>(TContainer container, in PropertyPath path)
            => GetValue<TContainer, TValue>(ref container, path);

        /// <summary>
        /// Gets the value of a property by path.
        /// </summary>
        /// <param name="container">The container whose property value will be returned.</param>
        /// <param name="path">The path of the property to get.</param>
        /// <typeparam name="TContainer">The container type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <returns>The value at the specified path.</returns>
        /// <exception cref="ArgumentNullException">The specified container or path is null.</exception>
        /// <exception cref="InvalidContainerTypeException">The specified container type is not valid for visitation.</exception>
        /// <exception cref="MissingPropertyBagException">The specified container type has no property bag associated with it.</exception>
        /// <exception cref="InvalidCastException">The specified <typeparamref name="TValue"/> could not be assigned to the property.</exception>
        /// <exception cref="InvalidPathException">The specified <paramref name="path"/> was not found or could not be resolved.</exception>
        public static TValue GetValue<TContainer, TValue>(ref TContainer container, in PropertyPath path)
        {
            if (path.IsEmpty)
                throw new InvalidPathException("The specified PropertyPath is empty.");

            if (TryGetValue(ref container, path, out TValue value, out var returnCode))
                return value;

            switch (returnCode)
            {
                case VisitReturnCode.NullContainer:
                    throw new ArgumentNullException(nameof(container));
                case VisitReturnCode.InvalidContainerType:
                    throw new InvalidContainerTypeException(container.GetType());
                case VisitReturnCode.MissingPropertyBag:
                    throw new MissingPropertyBagException(container.GetType());
                case VisitReturnCode.InvalidCast:
                    throw new InvalidCastException($"Failed to GetValue of Type=[{typeof(TValue).Name}] for property with path=[{path}]");
                case VisitReturnCode.InvalidPath:
                    throw new InvalidPathException($"Failed to GetValue for property with Path=[{path}]");
                default:
                    throw new Exception($"Unexpected {nameof(VisitReturnCode)}=[{returnCode}]");
            }
        }

        /// <summary>
        /// Gets the value of a property by name.
        /// </summary>
        /// <param name="container">The container whose property value will be returned.</param>
        /// <param name="name">The name of the property to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified name, if the property is found. otherwise the default value for the <typeparamref name="TValue"/>.</param>
        /// <typeparam name="TContainer">The container type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <returns><see langword="true"/> if the value exists for the specified name; otherwise, <see langword="false"/>.</returns>
        public static bool TryGetValue<TContainer, TValue>(TContainer container, string name, out TValue value)
            => TryGetValue(ref container, name, out value);

        /// <summary>
        /// Gets the value of a property by name.
        /// </summary>
        /// <param name="container">The container whose property value will be returned.</param>
        /// <param name="name">The name of the property to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified name, if the property is found. otherwise the default value for the <typeparamref name="TValue"/>.</param>
        /// <typeparam name="TContainer">The container type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <returns><see langword="true"/> if the value exists for the specified name; otherwise, <see langword="false"/>.</returns>
        public static bool TryGetValue<TContainer, TValue>(ref TContainer container, string name, out TValue value)
        {
            var path = new PropertyPath(name);
            return TryGetValue(ref container, path, out value, out _);
        }

        /// <summary>
        /// Gets the value of a property by path.
        /// </summary>
        /// <param name="container">The container whose property value will be returned.</param>
        /// <param name="path">The path of the property to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified path, if the property is found. otherwise the default value for the <typeparamref name="TValue"/>.</param>
        /// <returns>The property value of the given container.</returns>
        /// <typeparam name="TContainer">The container type.</typeparam>
        /// <typeparam name="TValue">The value type to set.</typeparam>
        /// <returns><see langword="true"/> if the value exists at the specified path; otherwise, <see langword="false"/>.</returns>
        public static bool TryGetValue<TContainer, TValue>(TContainer container, in PropertyPath path, out TValue value)
            => TryGetValue(ref container, path, out value, out _);

        /// <summary>
        /// Gets the value of a property by path.
        /// </summary>
        /// <param name="container">The container whose property value will be returned.</param>
        /// <param name="path">The path of the property to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified path, if the property is found. otherwise the default value for the <typeparamref name="TValue"/>.</param>
        /// <typeparam name="TContainer">The container type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <returns><see langword="true"/> if the value exists at the specified path; otherwise, <see langword="false"/>.</returns>
        public static bool TryGetValue<TContainer, TValue>(ref TContainer container, in PropertyPath path, out TValue value)
            => TryGetValue(ref container, path, out value, out _);

        /// <summary>
        /// Gets the value of a property by path.
        /// </summary>
        /// <param name="container">The container whose property value will be returned.</param>
        /// <param name="path">The path of the property to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified path, if the property is found. otherwise the default value for the <typeparamref name="TValue"/>.</param>
        /// <param name="returnCode">When this method returns, contains the return code.</param>
        /// <typeparam name="TContainer">The container type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <returns><see langword="true"/> if the value exists at the specified path; otherwise, <see langword="false"/>.</returns>
        public static bool TryGetValue<TContainer, TValue>(ref TContainer container, in PropertyPath path, out TValue value, out VisitReturnCode returnCode)
        {
            if (path.IsEmpty)
            {
                returnCode = VisitReturnCode.InvalidPath;
                value = default;
                return false;
            }

            var visitor = GetValueVisitor<TValue>.Pool.Get();
            visitor.Path = path;
            visitor.ReadonlyVisit = true;

            try
            {
                if (!TryAccept(visitor, ref container, out returnCode))
                {
                    value = default;
                    return false;
                }

                value = visitor.Value;
                returnCode = visitor.ReturnCode;
            }
            finally
            {
                GetValueVisitor<TValue>.Pool.Release(visitor);
            }

            return returnCode == VisitReturnCode.Ok;
        }
    }
}
