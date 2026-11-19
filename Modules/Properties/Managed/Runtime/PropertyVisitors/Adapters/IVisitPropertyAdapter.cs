// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Properties
{
    /// <summary>
    /// Implement this interface to intercept the visitation for a specific <typeparamref name="TContainer"/> and <typeparamref name="TValue"/> pair.
    /// </summary>
    /// <remarks>
    /// * <seealso cref="IVisitPropertyAdapter{TValue}"/>
    /// * <seealso cref="IVisitPropertyAdapter"/>
    /// </remarks>
    /// <typeparam name="TContainer">The container type being visited.</typeparam>
    /// <typeparam name="TValue">The value type being visited.</typeparam>
    public interface IVisitPropertyAdapter<TContainer, TValue> : IPropertyVisitorAdapter
    {
        /// <summary>
        /// Invoked when the visitor encounters specific a <typeparamref name="TContainer"/> and <typeparamref name="TValue"/> pair.
        /// </summary>
        /// <param name="context">The context being visited.</param>
        /// <param name="container">The container being visited.</param>
        /// <param name="value">The value being visited.</param>
        void Visit(in VisitContext<TContainer, TValue> context, ref TContainer container, ref TValue value);
    }

    /// <summary>
    /// Implement this interface to intercept the visitation for a specific <typeparamref name="TValue"/> type.
    /// </summary>
    /// <remarks>
    /// <seealso cref="IVisitPropertyAdapter{TContainer,TValue}"/>
    /// <seealso cref="IVisitPropertyAdapter"/>
    /// </remarks>
    /// <typeparam name="TValue">The value type being visited.</typeparam>
    public interface IVisitPropertyAdapter<TValue> : IPropertyVisitorAdapter
    {
        /// <summary>
        /// Invoked when the visitor encounters specific <typeparamref name="TValue"/> type with any container.
        /// </summary>
        /// <param name="context">The context being visited.</param>
        /// <param name="container">The container being visited.</param>
        /// <param name="value">The value being visited.</param>
        /// <typeparam name="TContainer">The container type being visited.</typeparam>
        void Visit<TContainer>(in VisitContext<TContainer, TValue> context, ref TContainer container, ref TValue value);
    }

    /// <summary>
    /// Implement this interface to handle visitation for all properties.
    /// </summary>
    /// <remarks>
    /// <seealso cref="IVisitPropertyAdapter{TContainer,TValue}"/>
    /// <seealso cref="IVisitPropertyAdapter{TValue}"/>
    /// </remarks>
    public interface IVisitPropertyAdapter : IPropertyVisitorAdapter
    {
        /// <summary>
        /// Invoked when the visitor encounters any property.
        /// </summary>
        /// <param name="context">The context being visited.</param>
        /// <param name="container">The container being visited.</param>
        /// <param name="value">The value being visited.</param>
        /// <typeparam name="TValue">The value type being visited.</typeparam>
        /// <typeparam name="TContainer">The container type being visited.</typeparam>
        void Visit<TContainer, TValue>(in VisitContext<TContainer, TValue> context, ref TContainer container, ref TValue value);
    }

    /// <summary>
    /// Implement this interface to intercept the visitation for a specific <typeparamref name="TContainer"/> and <typeparamref name="TValue"/> pair.
    /// </summary>
    /// <remarks>
    /// * <seealso cref="IVisitContravariantPropertyAdapter{TValue}"/>
    /// * <seealso cref="IVisitPropertyAdapter"/>
    /// </remarks>
    /// <typeparam name="TContainer">The container type being visited.</typeparam>
    /// <typeparam name="TValue">The value type being visited.</typeparam>
    public interface IVisitContravariantPropertyAdapter<TContainer, in TValue> : IPropertyVisitorAdapter
    {
        /// <summary>
        /// Invoked when the visitor encounters specific a <typeparamref name="TContainer"/> and <typeparamref name="TValue"/> pair.
        /// </summary>
        /// <param name="context">The context being visited.</param>
        /// <param name="container">The container being visited.</param>
        /// <param name="value">The value being visited.</param>
        void Visit(in VisitContext<TContainer> context, ref TContainer container, TValue value);
    }

    /// <summary>
    /// Implement this interface to intercept the visitation for a specific <typeparamref name="TValue"/> type.
    /// </summary>
    /// <remarks>
    /// <seealso cref="IVisitContravariantPropertyAdapter{TContainer,TValue}"/>
    /// <seealso cref="IVisitPropertyAdapter"/>
    /// </remarks>
    /// <typeparam name="TValue">The value type being visited.</typeparam>
    public interface IVisitContravariantPropertyAdapter<in TValue> : IPropertyVisitorAdapter
    {
        /// <summary>
        /// Invoked when the visitor encounters specific <typeparamref name="TValue"/> type with any container.
        /// </summary>
        /// <param name="context">The context being visited.</param>
        /// <param name="container">The container being visited.</param>
        /// <param name="value">The value being visited.</param>
        /// <typeparam name="TContainer">The container type being visited.</typeparam>
        void Visit<TContainer>(in VisitContext<TContainer> context, ref TContainer container, TValue value);
    }
}
