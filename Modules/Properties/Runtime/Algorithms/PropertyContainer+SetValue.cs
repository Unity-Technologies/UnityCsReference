// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Properties
{
    public static partial class PropertyContainer
    {
        internal class SetValueVisitor<TSrcValue> : PathVisitor
        {
            public static readonly UnityEngine.Pool.ObjectPool<SetValueVisitor<TSrcValue>> Pool = new UnityEngine.Pool.ObjectPool<SetValueVisitor<TSrcValue>>(() => new SetValueVisitor<TSrcValue>(), null, v => v.Reset());
            public TSrcValue Value;

            public override void Reset()
            {
                base.Reset();
                Value = default;
            }

            protected override void VisitPath<TContainer, TValue>(Property<TContainer, TValue> property,
                ref TContainer container, ref TValue value)
            {
                if (property.IsReadOnly)
                {
                    ReturnCode = VisitReturnCode.AccessViolation;
                    return;
                }

                if (TypeConversion.TryConvert(ref Value, out TValue v))
                {
                    property.SetValue(ref container, v);
                }
                else
                {
                    ReturnCode = VisitReturnCode.InvalidCast;
                }
            }
        }

        /// <summary>
        /// Sets the value of a property by name to the given value.
        /// </summary>
        /// <remarks>
        /// This method is NOT thread safe.
        /// </remarks>
        /// <param name="container">The container whose property will be set.</param>
        /// <param name="name">The name of the property to set.</param>
        /// <typeparam name="TContainer">The container type to set the value on.</typeparam>
        /// <param name="value">The value to assign to the property.</param>
        /// <typeparam name="TValue">The value type to set.</typeparam>
        /// <exception cref="ArgumentNullException">The specified container or path is null.</exception>
        /// <exception cref="InvalidContainerTypeException">The specified container type is not valid for visitation.</exception>
        /// <exception cref="MissingPropertyBagException">The specified container type has no property bag associated with it.</exception>
        /// <exception cref="InvalidCastException">The specified <typeparamref name="TValue"/> could not be assigned to the property.</exception>
        /// <exception cref="InvalidPathException">The specified <paramref name="name"/> was not found or could not be resolved.</exception>
        public static void SetValue<TContainer, TValue>(TContainer container, string name, TValue value)
            => SetValue(ref container, name, value);

        /// <summary>
        /// Sets the value of a property by name to the given value.
        /// </summary>
        /// <remarks>
        /// This method is NOT thread safe.
        /// </remarks>
        /// <param name="container">The container whose property will be set.</param>
        /// <param name="name">The name of the property to set.</param>
        /// <param name="value">The value to assign to the property.</param>
        /// <typeparam name="TContainer">The container type to set the value on.</typeparam>
        /// <typeparam name="TValue">The value type to set.</typeparam>
        /// <exception cref="ArgumentNullException">The specified container or path is null.</exception>
        /// <exception cref="InvalidContainerTypeException">The specified container type is not valid for visitation.</exception>
        /// <exception cref="MissingPropertyBagException">The specified container type has no property bag associated with it.</exception>
        /// <exception cref="InvalidCastException">The specified <typeparamref name="TValue"/> could not be assigned to the property.</exception>
        /// <exception cref="InvalidPathException">The specified <paramref name="name"/> was not found or could not be resolved.</exception>
        public static void SetValue<TContainer, TValue>(ref TContainer container, string name, TValue value)
        {
            var path = new PropertyPath(name);
            SetValue(ref container, path, value);
        }

        /// <summary>
        /// Sets the value of a property at the given path to the given value.
        /// </summary>
        /// <remarks>
        /// This method is NOT thread safe.
        /// </remarks>
        /// <param name="container">The container whose property will be set.</param>
        /// <param name="path">The path of the property to set.</param>
        /// <param name="value">The value to assign to the property.</param>
        /// <typeparam name="TContainer">The container type to set the value on.</typeparam>
        /// <typeparam name="TValue">The value type to set.</typeparam>
        /// <exception cref="ArgumentNullException">The specified container or path is null.</exception>
        /// <exception cref="InvalidContainerTypeException">The specified container type is not valid for visitation.</exception>
        /// <exception cref="MissingPropertyBagException">The specified container type has no property bag associated with it.</exception>
        /// <exception cref="InvalidCastException">The specified <typeparamref name="TValue"/> could not be assigned to the property.</exception>
        /// <exception cref="InvalidPathException">The specified <paramref name="path"/> was not found or could not be resolved.</exception>
        public static void SetValue<TContainer, TValue>(TContainer container, in PropertyPath path, TValue value)
            => SetValue(ref container, path, value);

        /// <summary>
        /// Sets the value of a property at the given path to the given value.
        /// </summary>
        /// <remarks>
        /// This method is NOT thread safe.
        /// </remarks>
        /// <param name="container">The container whose property will be set.</param>
        /// <param name="path">The path of the property to set.</param>
        /// <param name="value">The value to assign to the property.</param>
        /// <typeparam name="TContainer">The container type to set the value on.</typeparam>
        /// <typeparam name="TValue">The value type to set.</typeparam>
        /// <exception cref="ArgumentNullException">The specified container or path is null.</exception>
        /// <exception cref="InvalidContainerTypeException">The specified container type is not valid for visitation.</exception>
        /// <exception cref="MissingPropertyBagException">The specified container type has no property bag associated with it.</exception>
        /// <exception cref="InvalidCastException">The specified <typeparamref name="TValue"/> could not be assigned to the property.</exception>
        /// <exception cref="InvalidPathException">The specified <paramref name="path"/> was not found or could not be resolved.</exception>
        /// <exception cref="AccessViolationException">The specified <paramref name="path"/> is read-only.</exception>
        public static void SetValue<TContainer, TValue>(ref TContainer container, in PropertyPath path, TValue value)
        {
            if (path.Length == 0)
                throw new ArgumentNullException(nameof(path));

            if (path.Length <= 0)
                throw new InvalidPathException("The specified PropertyPath is empty.");

            if (TrySetValue(ref container, path, value, out var returnCode))
                return;

            switch (returnCode)
            {
                case VisitReturnCode.NullContainer:
                    throw new ArgumentNullException(nameof(container));
                case VisitReturnCode.InvalidContainerType:
                    throw new InvalidContainerTypeException(container.GetType());
                case VisitReturnCode.MissingPropertyBag:
                    throw new MissingPropertyBagException(container.GetType());
                case VisitReturnCode.InvalidCast:
                    throw new InvalidCastException($"Failed to SetValue of Type=[{typeof(TValue).Name}] for property with path=[{path}]");
                case VisitReturnCode.InvalidPath:
                    throw new InvalidPathException($"Failed to SetValue for property with Path=[{path}]");
                case VisitReturnCode.AccessViolation:
                    throw new AccessViolationException($"Failed to SetValue for read-only property with Path=[{path}]");
                default:
                    throw new Exception($"Unexpected {nameof(VisitReturnCode)}=[{returnCode}]");
            }
        }

        /// <summary>
        /// Tries to set the value of a property at the given path to the given value.
        /// </summary>
        /// <remarks>
        /// This method is NOT thread safe.
        /// </remarks>
        /// <param name="container">The container whose property will be set.</param>
        /// <param name="name">The name of the property to set.</param>
        /// <param name="value">The value to assign to the property.</param>
        /// <typeparam name="TContainer">The container type to set the value on.</typeparam>
        /// <typeparam name="TValue">The value type to set.</typeparam>
        /// <returns><see langword="true"/> if the value was set correctly; <see langword="false"/> otherwise.</returns>
        public static bool TrySetValue<TContainer, TValue>(TContainer container, string name, TValue value)
            => TrySetValue(ref container, name, value);

        /// <summary>
        /// Tries to set the value of a property at the given path to the given value.
        /// </summary>
        /// <remarks>
        /// This method is NOT thread safe.
        /// </remarks>
        /// <param name="container">The container whose property will be set.</param>
        /// <param name="name">The name of the property to set.</param>
        /// <param name="value">The value to assign to the property.</param>
        /// <typeparam name="TContainer">The container type to set the value on.</typeparam>
        /// <typeparam name="TValue">The value type to set.</typeparam>
        /// <returns><see langword="true"/> if the value was set correctly; <see langword="false"/> otherwise.</returns>
        public static bool TrySetValue<TContainer, TValue>(ref TContainer container, string name, TValue value)
        {
            var path = new PropertyPath(name);
            return TrySetValue(ref container, path, value);
        }

        /// <summary>
        /// Tries to set the value of a property at the given path to the given value.
        /// </summary>
        /// <remarks>
        /// This method is NOT thread safe.
        /// </remarks>
        /// <param name="container">The container whose property will be set.</param>
        /// <param name="path">The path of the property to set.</param>
        /// <param name="value">The value to assign to the property.</param>
        /// <typeparam name="TContainer">The container type to set the value on.</typeparam>
        /// <typeparam name="TValue">The value type to set.</typeparam>
        /// <returns><see langword="true"/> if the value was set correctly; <see langword="false"/> otherwise.</returns>
        public static bool TrySetValue<TContainer, TValue>(TContainer container, in PropertyPath path, TValue value)
            => TrySetValue(ref container, path, value);

        /// <summary>
        /// Tries to set the value of a property at the given path to the given value.
        /// </summary>
        /// <remarks>
        /// This method is NOT thread safe.
        /// </remarks>
        /// <param name="container">The container whose property will be set.</param>
        /// <param name="path">The path of the property to set.</param>
        /// <param name="value">The value to assign to the property.</param>
        /// <typeparam name="TContainer">The container type to set the value on.</typeparam>
        /// <typeparam name="TValue">The value type to set.</typeparam>
        /// <returns><see langword="true"/> if the value was set correctly; <see langword="false"/> otherwise.</returns>
        public static bool TrySetValue<TContainer, TValue>(ref TContainer container, in PropertyPath path, TValue value)
            => TrySetValue(ref container, path, value, out _);

        /// <summary>
        /// Tries to set the value of a property at the given path to the given value.
        /// </summary>
        /// <remarks>
        /// This method is NOT thread safe.
        /// </remarks>
        /// <param name="container">The container whose property will be set.</param>
        /// <param name="path">The path of the property to set.</param>
        /// <param name="value">The value to assign to the property.</param>
        /// <param name="returnCode">When this method returns, contains the return code.</param>
        /// <typeparam name="TContainer">The container type to set the value on.</typeparam>
        /// <typeparam name="TValue">The value type to set.</typeparam>
        /// <returns><see langword="true"/> if the value was set correctly; <see langword="false"/> otherwise.</returns>
        public static bool TrySetValue<TContainer, TValue>(ref TContainer container, in PropertyPath path, TValue value, out VisitReturnCode returnCode)
        {
            if (path.IsEmpty)
            {
                returnCode = VisitReturnCode.InvalidPath;
                return false;
            }

            var visitor = SetValueVisitor<TValue>.Pool.Get();
            visitor.Path = path;
            visitor.Value = value;
            try
            {
                if (!TryAccept(visitor, ref container, out returnCode))
                {
                    return false;
                }

                returnCode = visitor.ReturnCode;
            }
            finally
            {
                SetValueVisitor<TValue>.Pool.Release(visitor);
            }

            return returnCode == VisitReturnCode.Ok;
        }
    }
}
