// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Properties
{
    public static partial class PropertyContainer
    {
        class GetPropertyVisitor : PathVisitor
        {
            public static readonly UnityEngine.Pool.ObjectPool<GetPropertyVisitor> Pool = new UnityEngine.Pool.ObjectPool<GetPropertyVisitor>(() => new GetPropertyVisitor(), null, v => v.Reset());

            public IProperty Property;

            public override void Reset()
            {
                base.Reset();
                Property = default;
                ReadonlyVisit = true;
            }

            protected override void VisitPath<TContainer, TValue>(Property<TContainer, TValue> property,
                ref TContainer container, ref TValue value)
            {
                Property = property;
            }
        }

        /// <summary>
        /// Gets an <see cref="IProperty"/> on the specified container for the given <see cref="PropertyPath"/>.
        /// </summary>
        /// <remarks>
        /// While the container data is not actually read from or written to. The container itself is needed to resolve polymorphic fields and list elements.
        /// </remarks>
        /// <param name="container">The container tree to search.</param>
        /// <param name="path">The property path to resolve.</param>
        /// <typeparam name="TContainer">The strongly typed container.</typeparam>
        /// <returns>The <see cref="IProperty"/> for the given path.</returns>
        /// <exception cref="ArgumentNullException">The specified container or path is null.</exception>
        /// <exception cref="InvalidContainerTypeException">The specified container type is not valid for visitation.</exception>
        /// <exception cref="MissingPropertyBagException">The specified container type has no property bag associated with it.</exception>
        /// <exception cref="InvalidPathException">The specified <paramref name="path"/> was not found or could not be resolved.</exception>
        public static IProperty GetProperty<TContainer>(TContainer container, in PropertyPath path)
            => GetProperty(ref container, path);

        /// <summary>
        /// Gets an <see cref="IProperty"/> on the specified container for the given <see cref="PropertyPath"/>.
        /// </summary>
        /// <remarks>
        /// While the container data is not actually read from or written to. The container itself is needed to resolve polymorphic fields and list elements.
        /// </remarks>
        /// <param name="container">The container tree to search.</param>
        /// <param name="path">The property path to resolve.</param>
        /// <typeparam name="TContainer">The strongly typed container.</typeparam>
        /// <returns>The <see cref="IProperty"/> for the given path.</returns>
        /// <exception cref="ArgumentNullException">The specified container or path is null.</exception>
        /// <exception cref="InvalidContainerTypeException">The specified container type is not valid for visitation.</exception>
        /// <exception cref="MissingPropertyBagException">The specified container type has no property bag associated with it.</exception>
        /// <exception cref="InvalidPathException">The specified <paramref name="path"/> was not found or could not be resolved.</exception>
        public static IProperty GetProperty<TContainer>(ref TContainer container, in PropertyPath path)
        {
            if (TryGetProperty(ref container, path, out var property, out var returnCode))
            {
                return property;
            }

            switch (returnCode)
            {
                case VisitReturnCode.NullContainer:
                    throw new ArgumentNullException(nameof(container));
                case VisitReturnCode.InvalidContainerType:
                    throw new InvalidContainerTypeException(container.GetType());
                case VisitReturnCode.MissingPropertyBag:
                    throw new MissingPropertyBagException(container.GetType());
                case VisitReturnCode.InvalidPath:
                    throw new ArgumentException($"Failed to get property for path=[{path}]");
                default:
                    throw new Exception($"Unexpected {nameof(VisitReturnCode)}=[{returnCode}]");
            }
        }

        /// <summary>
        /// Gets an <see cref="IProperty"/> on the specified container for the given <see cref="PropertyPath"/>.
        /// </summary>
        /// <remarks>
        /// While the container data is not actually read from or written to. The container itself is needed to resolve polymorphic fields and list elements.
        /// </remarks>
        /// <param name="container">The container tree to search.</param>
        /// <param name="path">The property path to resolve.</param>
        /// <param name="property">When this method returns, contains the property associated with the specified path, if the property is found; otherwise, null.</param>
        /// <typeparam name="TContainer">The strongly typed container.</typeparam>
        /// <returns><see langword="true"/> if the property was found at the specified path; otherwise, <see langword="false"/>.</returns>
        public static bool TryGetProperty<TContainer>(TContainer container, in PropertyPath path, out IProperty property)
            => TryGetProperty(ref container, path, out property, out _);

        /// <summary>
        /// Gets an <see cref="IProperty"/> on the specified container for the given <see cref="PropertyPath"/>.
        /// </summary>
        /// <remarks>
        /// While the container data is not actually read from or written to. The container itself is needed to resolve polymorphic fields and list elements.
        /// </remarks>
        /// <param name="container">The container tree to search.</param>
        /// <param name="path">The property path to resolve.</param>
        /// <param name="property">When this method returns, contains the property associated with the specified path, if the property is found; otherwise, null.</param>
        /// <typeparam name="TContainer">The strongly typed container.</typeparam>
        /// <returns><see langword="true"/> if the property was found at the specified path; otherwise, <see langword="false"/>.</returns>
        public static bool TryGetProperty<TContainer>(ref TContainer container, in PropertyPath path, out IProperty property)
            => TryGetProperty(ref container, path, out property, out _);

        /// <summary>
        /// Gets an <see cref="IProperty"/> on the specified container for the given <see cref="PropertyPath"/>.
        /// </summary>
        /// <remarks>
        /// While the container data is not actually read from or written to. The container itself is needed to resolve polymorphic fields and list elements.
        /// </remarks>
        /// <param name="container">The container tree to search.</param>
        /// <param name="path">The property path to resolve.</param>
        /// <param name="property">When this method returns, contains the property associated with the specified path, if the property is found; otherwise, null.</param>
        /// <param name="returnCode">When this method returns, contains the return code.</param>
        /// <typeparam name="TContainer">The strongly typed container.</typeparam>
        /// <returns><see langword="true"/> if the property was found at the specified path; otherwise, <see langword="false"/>.</returns>
        public static bool TryGetProperty<TContainer>(ref TContainer container, in PropertyPath path, out IProperty property, out VisitReturnCode returnCode)
        {
            var getPropertyVisitor = GetPropertyVisitor.Pool.Get();
            try
            {
                getPropertyVisitor.Path = path;
                if (!TryAccept(getPropertyVisitor, ref container, out returnCode))
                {
                    property = default;
                    return false;
                }
                returnCode = getPropertyVisitor.ReturnCode;
                property = getPropertyVisitor.Property;
                return returnCode == VisitReturnCode.Ok;
            }
            finally
            {
                GetPropertyVisitor.Pool.Release(getPropertyVisitor);
            }
        }
    }
}
