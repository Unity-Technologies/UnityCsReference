// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Search
{
    /// <summary>
    /// Base attribute class used to define a custom filter on a QueryEngine.
    /// All filter types supported by QueryEngine.AddFilter are supported by this attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class QueryEngineFilterAttribute : Attribute
    {
        /// <summary>
        /// The identifier of the filter. Typically what precedes the operator in a filter (i.e. "id" in "id>=2").
        /// </summary>
        public string token;

        /// <summary>
        /// String comparison options.
        /// </summary>
        public StringComparison comparisonOptions;

        /// <summary>
        /// Flag indicating if the filter overrides the global string comparison options.
        /// Set to true when the comparisonOptions are used.
        /// </summary>
        public bool overridesStringComparison;

        /// <summary>
        /// List of supported operator tokens. Null for all operators.
        /// </summary>
        public string[] supportedOperators;

        /// <summary>
        /// Flag indicating if this filter uses a parameter transformer function.
        /// Set to true when paramTransformerFunction is used.
        /// </summary>
        public bool useParamTransformer;

        /// <summary>
        /// Name of the parameter transformer function to use with this filter.
        /// Tag the parameter transformer function with the appropriate ParameterTransformer attribute.
        /// </summary>
        public string paramTransformerFunction;

        /// <summary>
        /// Create a filter with the corresponding token and supported operators.
        /// </summary>
        /// <param name="token">The identifier of the filter. Typically what precedes the operator in a filter (i.e. "id" in "id>=2").</param>
        /// <param name="supportedOperators">List of supported operator tokens. Null for all operators.</param>
        public QueryEngineFilterAttribute(string token, string[] supportedOperators = null)
        {
            this.token = token;
            this.supportedOperators = supportedOperators;
        }

        /// <summary>
        /// Create a filter with the corresponding token, string comparison options and supported operators.
        /// </summary>
        /// <param name="token">The identifier of the filter. Typically what precedes the operator in a filter (i.e. "id" in "id>=2").</param>
        /// <param name="options">String comparison options.</param>
        /// <param name="supportedOperators">List of supported operator tokens. Null for all operators.</param>
        /// <remarks>This sets the flag overridesStringComparison to true.</remarks>
        public QueryEngineFilterAttribute(string token, StringComparison options, string[] supportedOperators = null)
            : this(token, supportedOperators)
        {
            comparisonOptions = options;
            overridesStringComparison = true;
        }

        /// <summary>
        /// Create a filter with the corresponding token, parameter transformer function and supported operators.
        /// </summary>
        /// <param name="token">The identifier of the filter. Typically what precedes the operator in a filter (i.e. "id" in "id>=2").</param>
        /// <param name="paramTransformerFunction">Name of the parameter transformer function to use with this filter. Tag the parameter transformer function with the appropriate ParameterTransformer attribute.</param>
        /// <param name="supportedOperators">List of supported operator tokens. Null for all operators.</param>
        /// <remarks>Sets the flag useParamTransformer to true.</remarks>
        public QueryEngineFilterAttribute(string token, string paramTransformerFunction, string[] supportedOperators = null)
            : this(token, supportedOperators)
        {
            useParamTransformer = true;
            this.paramTransformerFunction = paramTransformerFunction;
        }

        /// <summary>
        /// Create a filter with the corresponding token, parameter transformer function, string comparison options and supported operators.
        /// </summary>
        /// <param name="token">The identifier of the filter. Typically what precedes the operator in a filter (i.e. "id" in "id>=2").</param>
        /// <param name="paramTransformerFunction">Name of the parameter transformer function to use with this filter. Tag the parameter transformer function with the appropriate ParameterTransformer attribute.</param>
        /// <param name="options">String comparison options.</param>
        /// <param name="supportedOperators">List of supported operator tokens. Null for all operators.</param>
        /// <remarks>Sets both overridesStringComparison and useParamTransformer flags to true.</remarks>
        public QueryEngineFilterAttribute(string token, string paramTransformerFunction, StringComparison options, string[] supportedOperators = null)
            : this(token, options, supportedOperators)
        {
            useParamTransformer = true;
            this.paramTransformerFunction = paramTransformerFunction;
        }
    }

    /// <summary>
    /// Base attribute class that defines a custom parameter transformer function.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class QueryEngineParameterTransformerAttribute : Attribute {}
}
