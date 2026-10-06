// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Interface used to build and configure state options in a fluent manner.
    /// </summary>
    /// <remarks>
    /// Provided by the <see cref="State.IOptionDefinitionContext"/> in a state's <see cref="State.OnDefineOptions"/> method.
    /// </remarks>
    /// <example>
    /// <code>
    /// protected override void OnDefineOptions(IOptionDefinitionContext context)
    /// {
    ///     context.AddOption("MyOption", typeof(int))
    ///         .WithDefaultValue(2)
    ///         .Delayed();
    /// }
    /// </code>
    /// </example>
    public interface IStateOptionBuilder
    {
        /// <summary>
        /// Builds and returns the final <see cref="IStateOption"/> instance based on the current configuration of the builder.
        /// </summary>
        /// <returns>The constructed <see cref="IStateOption"/>.</returns>
        /// <remarks>
        /// This method is optional. All options are automatically built when the state's <see cref="State.OnDefineOptions"/> method completes.
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
        ///     context.AddOption("MyOption", typeof(int))
        ///         .WithDefaultValue(2)
        ///         .Delayed()
        ///         .Build();
        /// }
        /// </code>
        /// </example>
        IStateOption Build();

        /// <summary>
        /// Configures the display name of the option being built.
        /// </summary>
        /// <param name="displayName">The display name to assign to the option.</param>
        /// <returns>The current builder instance for method chaining.</returns>
        /// <remarks>
        /// The display name doesn't affect functionality; it can improve usability and readability.
        /// If not set explicitly using this method, the name passed during creation (calling <see cref="State.IOptionDefinitionContext.AddOption"/>) is used as the default display name.
        /// </remarks>
        IStateOptionBuilder WithDisplayName(string displayName);

        /// <summary>
        /// Configures the tooltip text for the option being built.
        /// </summary>
        /// <param name="tooltip">The tooltip text to assign to the option.</param>
        /// <returns>The current builder instance for method chaining.</returns>
        IStateOptionBuilder WithTooltip(string tooltip);

        /// <summary>
        /// Configures the default value for the option being built.
        /// </summary>
        /// <param name="defaultValue">The default value to assign to the option.</param>
        /// <returns>The current builder instance for method chaining.</returns>
        IStateOptionBuilder WithDefaultValue(object defaultValue);

        /// <summary>
        /// Configures the option to use the <see cref="UnityEngine.DelayedAttribute"/>.
        /// </summary>
        /// <returns>The current builder instance for method chaining.</returns>
        /// <remarks>
        /// Apply this setting when the option’s value should only update after the user finishes editing input in the UI.
        /// This is useful for optimizing performance or avoiding intermediate updates during data entry.
        /// </remarks>
        IStateOptionBuilder Delayed();

        /// <summary>
        /// Configures the option to use the <see cref="UnityEngine.TextAreaAttribute"/>.
        /// </summary>
        /// <param name="minLines">The minimum amount of lines the text area will use. Defaults to 3.</param>
        /// <param name="maxLines">The maximum amount of lines the text area can show before it starts using a scrollbar. Defaults to 3.</param>
        /// <returns>The current builder instance for method chaining.</returns>
        /// <remarks>
        /// Applies only to string options. Use this setting to make the option value a Text Area.
        /// A Text Area is a multi-line input field that allows users to enter large amounts of text.
        /// Its height automatically adjusts between specified minimum and maximum lines, and a scrollbar appears if the content exceeds the visible area.
        /// </remarks>
        IStateOptionBuilder AsTextArea(int minLines = 3, int maxLines = 3);
    }

    /// <summary>
    /// Interface used to build and configure state options in a fluent manner.
    /// </summary>
    /// <typeparam name="TData">The data type of the option being built.</typeparam>
    /// <remarks>
    /// Provided by the <see cref="State.IOptionDefinitionContext"/> in a state's <see cref="State.OnDefineOptions"/> method.
    /// </remarks>
    /// <example>
    /// <code>
    /// protected override void OnDefineOptions(IOptionDefinitionContext context)
    /// {
    ///     context.AddOption&lt;int&gt;("MyOption")
    ///         .WithDefaultValue(2)
    ///         .Delayed();
    /// }
    /// </code>
    /// </example>
    public interface IStateOptionBuilder<in TData>
    {
        /// <summary>
        /// Builds and returns the final <see cref="IStateOption"/> instance based on the current configuration of the builder.
        /// </summary>
        /// <returns>The constructed <see cref="IStateOption"/>.</returns>
        /// <remarks>
        /// This method is optional. All options are automatically built when the state's <see cref="State.OnDefineOptions"/> method completes.
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
        ///     context.AddOption&lt;int&gt;("MyOption")
        ///         .WithDefaultValue(2)
        ///         .Delayed()
        ///         .Build();
        /// }
        /// </code>
        /// </example>
        IStateOption Build();

        /// <summary>
        /// Configures the display name of the option being built.
        /// </summary>
        /// <param name="displayName">The display name to assign to the option.</param>
        /// <returns>The current builder instance for method chaining.</returns>
        /// <remarks>
        /// The display name doesn't affect functionality; it can improve usability and readability.
        /// If not set explicitly using this method, the name passed during creation (calling <see cref="State.IOptionDefinitionContext.AddOption"/>) is used as the default display name.
        /// </remarks>
        IStateOptionBuilder<TData> WithDisplayName(string displayName);

        /// <summary>
        /// Configures the tooltip text for the option being built.
        /// </summary>
        /// <param name="tooltip">The tooltip text to assign to the option.</param>
        /// <returns>The current builder instance for method chaining.</returns>
        IStateOptionBuilder<TData> WithTooltip(string tooltip);

        /// <summary>
        /// Configures the default value for the option being built.
        /// </summary>
        /// <param name="defaultValue">The default value to assign to the option.</param>
        /// <returns>The current builder instance for method chaining.</returns>
        IStateOptionBuilder<TData> WithDefaultValue(TData defaultValue);

        /// <summary>
        /// Configures the option to use the <see cref="UnityEngine.DelayedAttribute"/>.
        /// </summary>
        /// <returns>The current builder instance for method chaining.</returns>
        /// <remarks>
        /// Apply this setting when the option’s value should only update after the user finishes editing input in the UI.
        /// This is useful for optimizing performance or avoiding intermediate updates during data entry.
        /// </remarks>
        IStateOptionBuilder<TData> Delayed();

        /// <summary>
        /// Configures the option to use the <see cref="UnityEngine.TextAreaAttribute"/>.
        /// </summary>
        /// <param name="minLines">The minimum amount of lines the text area will use. Defaults to 3.</param>
        /// <param name="maxLines">The maximum amount of lines the text area can show before it starts using a scrollbar. Defaults to 3.</param>
        /// <returns>The current builder instance for method chaining.</returns>
        /// <remarks>
        /// Applies only to string options. Use this setting to make the option value a Text Area.
        /// A Text Area is a multi-line input field that allows users to enter large amounts of text.
        /// Its height automatically adjusts between specified minimum and maximum lines, and a scrollbar appears if the content exceeds the visible area.
        /// </remarks>
        IStateOptionBuilder<TData> AsTextArea(int minLines = 3, int maxLines = 3);
    }
}
