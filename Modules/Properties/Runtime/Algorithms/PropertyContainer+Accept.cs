// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties.Internal;

namespace Unity.Properties
{
    /// <summary>
    /// Flags used to specify a set of exceptions.
    /// </summary>
    [Flags]
    public enum VisitExceptionKind
    {
        /// <summary>
        /// Flag used to specify no exception types.
        /// </summary>
        None = 0,

        /// <summary>
        /// Flag used to specify internal exceptions thrown by the core visitation.
        /// </summary>
        Internal = 1 << 0,

        /// <summary>
        /// Use this flag to specify exceptions thrown from the visitor code itself.
        /// </summary>
        Visitor = 1 << 1,

        /// <summary>
        /// Use this flag to specify all exceptions.
        /// </summary>
        All = Internal | Visitor
    }

    /// <summary>
    /// Custom parameters to use for visitation.
    /// </summary>
    public struct VisitParameters
    {
        /// <summary>
        /// Use this options to ignore specific exceptions during visitation.
        /// </summary>
        public VisitExceptionKind IgnoreExceptions { get; set; }
    }

    public static partial class PropertyContainer
    {
        /// <summary>
        /// Visit the specified <paramref name="container"/> using the specified <paramref name="visitor"/>.
        /// </summary>
        /// <param name="visitor">The visitor.</param>
        /// <param name="container">The container to visit.</param>
        /// <param name="parameters">The visit parameters to use.</param>
        /// <exception cref="ArgumentNullException">The container is null.</exception>
        /// <exception cref="InvalidContainerTypeException">The given container type is not valid for visitation.</exception>
        /// <exception cref="MissingPropertyBagException">No property bag was found for the given container.</exception>
        public static void Accept<TContainer>(IPropertyBagVisitor visitor, TContainer container, VisitParameters parameters = default)
        {
            var returnCode = VisitReturnCode.Ok;

            try
            {
                if (TryAccept(visitor, ref container, out returnCode, parameters))
                    return;
            }
            catch (Exception)
            {
                if ((parameters.IgnoreExceptions & VisitExceptionKind.Visitor) == 0)
                    throw;
            }

            if ((parameters.IgnoreExceptions & VisitExceptionKind.Internal) != 0)
                return;

            switch (returnCode)
            {
                case VisitReturnCode.Ok:
                case VisitReturnCode.InvalidContainerType:
                    break;
                case VisitReturnCode.NullContainer:
                    throw new ArgumentException("The given container was null. Visitation only works for valid non-null containers.");
                case VisitReturnCode.MissingPropertyBag:
                    throw new MissingPropertyBagException(container.GetType());
                default:
                    throw new Exception($"Unexpected {nameof(VisitReturnCode)}=[{returnCode}]");
            }
        }

        /// <summary>
        /// Visit the specified <paramref name="container"/> using the specified <paramref name="visitor"/>.
        /// </summary>
        /// <param name="visitor">The visitor.</param>
        /// <param name="container">The container to visit.</param>
        /// <param name="parameters">The visit parameters to use.</param>
        /// <typeparam name="TContainer">The declared container type.</typeparam>
        /// <exception cref="ArgumentNullException">The container is null.</exception>
        /// <exception cref="InvalidContainerTypeException">The container is null.</exception>
        /// <exception cref="MissingPropertyBagException">No property bag was found for the given container.</exception>
        public static void Accept<TContainer>(IPropertyBagVisitor visitor, ref TContainer container, VisitParameters parameters = default)
        {
            var returnCode = VisitReturnCode.Ok;

            try
            {
                if (TryAccept(visitor, ref container, out returnCode, parameters))
                    return;
            }
            catch (Exception)
            {
                if ((parameters.IgnoreExceptions & VisitExceptionKind.Visitor) == 0)
                    throw;
            }

            if ((parameters.IgnoreExceptions & VisitExceptionKind.Internal) != 0)
                return;

            switch (returnCode)
            {
                case VisitReturnCode.Ok:
                case VisitReturnCode.InvalidContainerType:
                    break;
                case VisitReturnCode.NullContainer:
                    throw new ArgumentException("The given container was null. Visitation only works for valid non-null containers.");
                case VisitReturnCode.MissingPropertyBag:
                    throw new MissingPropertyBagException(container.GetType());
                default:
                    throw new Exception($"Unexpected {nameof(VisitReturnCode)}=[{returnCode}]");
            }
        }

        /// <summary>
        /// Tries to visit the specified <paramref name="container"/> by ref using the specified <paramref name="visitor"/>.
        /// </summary>
        /// <param name="visitor">The visitor.</param>
        /// <param name="container">The container to visit.</param>
        /// <param name="parameters">The visit parameters to use.</param>
        /// <typeparam name="TContainer">The declared container type.</typeparam>
        /// <returns><see langword="true"/> if the visitation succeeded; <see langword="false"/> otherwise.</returns>
        public static bool TryAccept<TContainer>(IPropertyBagVisitor visitor, ref TContainer container, VisitParameters parameters = default)
        {
            return TryAccept(visitor, ref container, out _, parameters);
        }

        /// <summary>
        /// Tries to visit the specified <paramref name="container"/> by ref using the specified <paramref name="visitor"/>.
        /// </summary>
        /// <param name="visitor">The visitor.</param>
        /// <param name="container">The container to visit.</param>
        /// <param name="parameters">The visit parameters to use.</param>
        /// <param name="returnCode">When this method returns, contains the return code.</param>
        /// <typeparam name="TContainer">The declared container type.</typeparam>
        /// <returns><see langword="true"/> if the visitation succeeded; <see langword="false"/> otherwise.</returns>
        public static bool TryAccept<TContainer>(IPropertyBagVisitor visitor, ref TContainer container, out VisitReturnCode returnCode, VisitParameters parameters = default)
        {
            if (!TypeTraits<TContainer>.IsContainer)
            {
                returnCode = VisitReturnCode.InvalidContainerType;
                return false;
            }

            // Can not visit a null container.
            if (TypeTraits<TContainer>.CanBeNull)
            {
                if (EqualityComparer<TContainer>.Default.Equals(container, default))
                {
                    returnCode = VisitReturnCode.NullContainer;
                    return false;
                }
            }

            if (!TypeTraits<TContainer>.IsValueType && typeof(TContainer) != container.GetType())
            {
                if (!TypeTraits.IsContainer(container.GetType()))
                {
                    returnCode = VisitReturnCode.InvalidContainerType;
                    return false;
                }

                var properties = PropertyBagStore.GetPropertyBag(container.GetType());

                if (null == properties)
                {
                    returnCode = VisitReturnCode.MissingPropertyBag;
                    return false;
                }

                // At this point the generic parameter is useless to us since it's not the correct type.
                // Instead we need to retrieve the untyped property bag and accept on that. Since we don't know the type
                // We need to box the container and let the property bag cast it internally.
                var boxed = (object) container;
                properties.Accept(visitor, ref boxed);
                container = (TContainer) boxed;
            }
            else
            {
                var properties = PropertyBagStore.GetPropertyBag<TContainer>();

                if (null == properties)
                {
                    returnCode = VisitReturnCode.MissingPropertyBag;
                    return false;
                }

                PropertyBag.AcceptWithSpecializedVisitor(properties, visitor, ref container);
            }

            returnCode = VisitReturnCode.Ok;
            return true;
        }

        /// <summary>
        /// Visit the specified <paramref name="container"/> using the specified <paramref name="visitor"/> at the given <see cref="PropertyPath"/>.
        /// </summary>
        /// <param name="visitor">The visitor.</param>
        /// <param name="container">The container to visit.</param>
        /// <param name="path">The property path to visit.</param>
        /// <param name="parameters">The visit parameters to use.</param>
        /// <typeparam name="TContainer">The container type.</typeparam>
        /// <exception cref="ArgumentNullException">The container is null.</exception>
        /// <exception cref="InvalidContainerTypeException">The given container type is not valid for visitation.</exception>
        /// <exception cref="MissingPropertyBagException">No property bag was found for the given container.</exception>
        public static void Accept<TContainer>(IPropertyVisitor visitor, ref TContainer container, in PropertyPath path, VisitParameters parameters = default)
        {
            var visitAtPath = ValueAtPathVisitor.Pool.Get();
            try
            {
                visitAtPath.Path = path;
                visitAtPath.Visitor = visitor;

                Accept(visitAtPath, ref container, parameters);

                if ((parameters.IgnoreExceptions & VisitExceptionKind.Internal) == 0)
                {
                    switch (visitAtPath.ReturnCode)
                    {
                        case VisitReturnCode.Ok:
                            break;
                        case VisitReturnCode.InvalidPath:
                            throw new InvalidPathException($"Failed to Visit at Path=[{path}]");
                        default:
                            throw new Exception($"Unexpected {nameof(VisitReturnCode)}=[{visitAtPath.ReturnCode}]");
                    }
                }
            }
            finally
            {
                ValueAtPathVisitor.Pool.Release(visitAtPath);
            }
        }

        /// <summary>
        /// Visit the specified <paramref name="container"/> using the specified <paramref name="visitor"/> at the given <see cref="PropertyPath"/>.
        /// </summary>
        /// <param name="visitor">The visitor.</param>
        /// <param name="container">The container to visit.</param>
        /// <param name="path">The property path to visit.</param>
        /// <param name="returnCode">When this method returns, contains the return code.</param>
        /// <param name="parameters">The visit parameters to use.</param>
        /// <typeparam name="TContainer">The container type.</typeparam>
        /// <exception cref="ArgumentNullException">The container is null.</exception>
        /// <exception cref="InvalidContainerTypeException">The given container type is not valid for visitation.</exception>
        /// <exception cref="MissingPropertyBagException">No property bag was found for the given container.</exception>
        public static bool TryAccept<TContainer>(IPropertyVisitor visitor, ref TContainer container, in PropertyPath path, out VisitReturnCode returnCode, VisitParameters parameters = default)
        {
            var visitAtPath = ValueAtPathVisitor.Pool.Get();
            try
            {
                visitAtPath.Path = path;
                visitAtPath.Visitor = visitor;

                return TryAccept(visitAtPath, ref container, out returnCode, parameters);
            }
            finally
            {
                ValueAtPathVisitor.Pool.Release(visitAtPath);
            }
        }
    }
}
