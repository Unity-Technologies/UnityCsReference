// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Interface used to build and configure node options in a fluent manner.
    /// </summary>
    /// <remarks>
    /// Provided by the <see cref="Node.IOptionDefinitionContext"/> in a node's <see cref="Node.OnDefineOptions"/> method.
    /// </remarks>
    /// <example>
    /// <code>
    /// protected override void OnDefineOptions(IOptionDefinitionContext context)
    /// {
    ///     context.AddOption("MyOption", typeof(int)))
    ///         .WithDefaultValue(2)
    ///         .Delayed();
    /// }
    /// </code>
    /// </example>
    public interface IOptionBuilder
    {
        /// <summary>
        /// Builds and returns the final <see cref="INodeOption"/> instance based on the current configuration of the builder.
        /// </summary>
        /// <returns>The constructed <see cref="INodeOption"/>.</returns>
        /// <remarks>
        /// This method is optional. All options are automatically built when the node's <see cref="Node.OnDefineOptions"/> method completes.
        /// <br/><br/>
        /// Calling this method releases the memory associated with this option back into the pool immediately.
        /// You can choose to call this method if there are lots of options being defined to reduce peak memory usage.
        /// <br/><br/>
        /// Only call this after setting all desired configuration options using the builder methods.
        /// </remarks>
        /// <example>
        /// <code>
        /// protected override void OnDefineOptions(IOptionDefinitionContext context)
        /// {
        ///     context.AddOption("MyOption", typeof(int)))
        ///         .WithDefaultValue(2)
        ///         .Delayed()
        ///         .Build();
        /// }
        /// </code>
        /// </example>
        INodeOption Build();

        /// <summary>
        /// Configures the display name of the option being built.
        /// </summary>
        /// <param name="displayName">The display name to assign to the option.</param>
        /// <remarks>
        /// The display name doesn't affect functionality; it can improve usability and readability.
        /// If not set explicitly using this method, the name passed during creation (calling <see cref="Node.IOptionDefinitionContext.AddOption"/>) is used as the default display name.
        /// </remarks>
        IOptionBuilder WithDisplayName(string displayName);

        /// <summary>
        /// Configures the tooltip text for the option being built.
        /// </summary>
        /// <param name="tooltip">The tooltip text to assign to the option.</param>
        IOptionBuilder WithTooltip(string tooltip);

        /// <summary>
        /// Configures the default value for the option being built.
        /// </summary>
        /// <param name="defaultValue">The default value to assign to the option.</param>
        IOptionBuilder WithDefaultValue(object defaultValue);

        /// <summary>
        /// Configures the input port to use the <see cref="UnityEngine.DelayedAttribute"/>.
        /// </summary>
        /// <remarks>
        /// Apply this setting when the option’s value should only update after the user finishes editing input in the UI.
        /// This is useful for optimizing performance or avoiding intermediate updates during data entry.
        /// </remarks>
        IOptionBuilder Delayed();

        /// <summary>
        /// Configures the option to be shown only in the inspector, not in the node header.
        /// </summary>
        IOptionBuilder ShowInInspectorOnly();
    }


    /// <summary>
    /// Interface used to build and configure node options in a fluent manner.
    /// </summary>
    /// <remarks>
    /// Provided by the <see cref="Node.IOptionDefinitionContext"/> in a node's <see cref="Node.OnDefineOptions"/> method.
    /// </remarks>
    public interface IOptionBuilder<in TData>
    {
        /// <summary>
        /// Builds and returns the final <see cref="INodeOption"/> instance based on the current configuration of the builder.
        /// </summary>
        /// <returns>The constructed <see cref="INodeOption"/>.</returns>
        /// <remarks>
        /// This method is optional. All options are automatically built when the node's <see cref="Node.OnDefineOptions"/> method completes.
        /// <br/><br/>
        /// Calling this method releases the memory associated with this option back into the pool immediately.
        /// You can choose to call this method if there are lots of options being defined to reduce peak memory usage.
        /// <br/><br/>
        /// Only call this after setting all desired configuration options using the builder methods.
        /// </remarks>
        /// <example>
        /// <code>
        /// protected override void OnDefineOptions(IOptionDefinitionContext context)
        /// {
        ///     context.AddOption("MyOption", typeof(int)))
        ///         .WithDefaultValue(2)
        ///         .Delayed()
        ///         .Build();
        /// }
        /// </code>
        /// </example>
        INodeOption Build();

        /// <summary>
        /// Configures the display name of the option being built.
        /// </summary>
        /// <param name="displayName">The display name to assign to the option.</param>
        /// <remarks>
        /// The display name doesn't affect functionality; it can improve usability and readability.
        /// If not set explicitly using this method, the name passed during creation (calling <see cref="Node.IOptionDefinitionContext.AddOption"/>) is used as the default display name.
        /// </remarks>
        IOptionBuilder<TData> WithDisplayName(string displayName);

        /// <summary>
        /// Configures the tooltip text for the option being built.
        /// </summary>
        /// <param name="tooltip">The tooltip text to assign to the option.</param>
        IOptionBuilder<TData> WithTooltip(string tooltip);

        /// <summary>
        /// Configures the default value for the option being built.
        /// </summary>
        /// <param name="defaultValue">The default value to assign to the option.</param>
        IOptionBuilder<TData> WithDefaultValue(TData defaultValue);

        /// <summary>
        /// Configures the input port to use the <see cref="UnityEngine.DelayedAttribute"/>.
        /// </summary>
        /// <remarks>
        /// Apply this setting when the option’s value should only update after the user finishes editing input in the UI.
        /// This is useful for optimizing performance or avoiding intermediate updates during data entry.
        /// </remarks>
        IOptionBuilder<TData> Delayed();

        /// <summary>
        /// Configures the option to be shown only in the inspector, not in the node header.
        /// </summary>
        IOptionBuilder<TData> ShowInInspectorOnly();
    }
}
