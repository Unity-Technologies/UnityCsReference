// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Properties
{
    /// <summary>
    /// Implement this interface to filter visitation for a specific <see cref="TContainer"/> and <see cref="TValue"/> pair.
    /// </summary>
    /// <typeparam name="TContainer">The container type being visited.</typeparam>
    /// <typeparam name="TValue">The value type being visited.</typeparam>
    public interface IExcludePropertyAdapter<TContainer, TValue> : IPropertyVisitorAdapter
    {
        /// <summary>
        /// Invoked when the visitor encounters specific a <see cref="TContainer"/> and <see cref="TValue"/> pair.
        /// </summary>
        /// <param name="context">The context being visited.</param>
        /// <param name="container">The container being visited.</param>
        /// <param name="value">The value being visited.</param>
        /// <returns><see langword="true"/> if visitation should be skipped, <see langword="false"/> otherwise.</returns>
        bool IsExcluded(in ExcludeContext<TContainer, TValue> context, ref TContainer container, ref TValue value);
    }

    /// <summary>
    /// Implement this interface to filter visitation for a specific <see cref="TValue"/> type.
    /// </summary>
    /// <typeparam name="TValue">The value type.</typeparam>
    public interface IExcludePropertyAdapter<TValue> : IPropertyVisitorAdapter
    {
        /// <summary>
        /// Invoked when the visitor encounters specific a <see cref="TValue"/>.
        /// </summary>
        /// <param name="context">The context being visited.</param>
        /// <param name="container">The container being visited.</param>
        /// <param name="value">The value being visited.</param>
        /// <typeparam name="TContainer">The container type being visited.</typeparam>
        /// <returns><see langword="true"/> if visitation should be skipped, <see langword="false"/> otherwise.</returns>
        bool IsExcluded<TContainer>(in ExcludeContext<TContainer, TValue> context, ref TContainer container, ref TValue value);
    }

    /// <summary>
    /// Implement this interface to filter visitation.
    /// </summary>
    public interface IExcludePropertyAdapter : IPropertyVisitorAdapter
    {
        /// <summary>
        /// Invoked when the visitor encounters any property.
        /// </summary>
        /// <param name="context">The context being visited.</param>
        /// <param name="container">The container being visited.</param>
        /// <param name="value">The value being visited.</param>
        /// <typeparam name="TContainer">The container type being visited.</typeparam>
        /// <typeparam name="TValue">The value type being visited.</typeparam>
        /// <returns><see langword="true"/> if visitation should be skipped, <see langword="false"/> otherwise.</returns>
        bool IsExcluded<TContainer, TValue>(in ExcludeContext<TContainer, TValue> context, ref TContainer container, ref TValue value);
    }

    /// <summary>
    /// Implement this interface to filter visitation for a specific <see cref="TContainer"/> and <see cref="TValue"/> pair.
    /// </summary>
    /// <typeparam name="TContainer">The container type being visited.</typeparam>
    /// <typeparam name="TValue">The value type being visited.</typeparam>
    public interface IExcludeContravariantPropertyAdapter<TContainer, in TValue> : IPropertyVisitorAdapter
    {
        /// <summary>
        /// Invoked when the visitor encounters specific a <see cref="TContainer"/> and <see cref="TValue"/> pair.
        /// </summary>
        /// <param name="context">The context being visited.</param>
        /// <param name="container">The container being visited.</param>
        /// <param name="value">The value being visited.</param>
        /// <returns><see langword="true"/> if visitation should be skipped, <see langword="false"/> otherwise.</returns>
        bool IsExcluded(in ExcludeContext<TContainer> context, ref TContainer container, TValue value);
    }

    /// <summary>
    /// Implement this interface to filter visitation for a specific <see cref="TValue"/> type.
    /// </summary>
    /// <typeparam name="TValue">The value type.</typeparam>
    public interface IExcludeContravariantPropertyAdapter<in TValue> : IPropertyVisitorAdapter
    {
        /// <summary>
        /// Invoked when the visitor encounters any property.
        /// </summary>
        /// <param name="context">The context being visited.</param>
        /// <param name="container">The container being visited.</param>
        /// <param name="value">The value being visited.</param>
        /// <typeparam name="TContainer">The container type being visited.</typeparam>
        /// <returns><see langword="true"/> if visitation should be skipped, <see langword="false"/> otherwise.</returns>
        bool IsExcluded<TContainer>(in ExcludeContext<TContainer> context, ref TContainer container, TValue value);
    }
}
