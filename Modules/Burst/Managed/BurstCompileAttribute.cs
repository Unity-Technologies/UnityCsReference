// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;

// Make internals visible for BurstGlobalCompilerOptions
[assembly: InternalsVisibleTo("Unity.Burst.CodeGen")]
[assembly: InternalsVisibleTo("Unity.Burst.Editor")]
// Make internals visible to Unity burst tests
[assembly: InternalsVisibleTo("Unity.Burst.Tests.UnitTests")]
[assembly: InternalsVisibleTo("Unity.Burst.Editor.Tests")]
[assembly: InternalsVisibleTo("UnityEditor.Modules.Burst.EditModeTests")]
[assembly: InternalsVisibleTo("UnityEngine.Modules.Burst.EditModeTests")]
[assembly: InternalsVisibleTo("UnityEngine.Modules.Burst.PlayModeTests")]
[assembly: InternalsVisibleTo("Unity.Burst.Benchmarks")]

namespace Unity.Burst
{
    /// <summary>
    /// How the code should be optimized.
    /// </summary>
    public enum OptimizeFor
    {
        /// <summary>
        /// The default optimization mode - uses <see cref="OptimizeFor.Balanced"/>.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Optimize for performance - the compiler should make the most optimal binary possible.
        /// </summary>
        Performance = 1,

        /// <summary>
        /// Optimize for size - the compiler should make the smallest binary possible.
        /// </summary>
        Size = 2,

        /// <summary>
        /// Optimize for fast compilation - the compiler should perform some optimization, but take as little time as possible to do it.
        /// </summary>
        FastCompilation = 3,

        /// <summary>
        /// Optimize for balanced compilation - ensuring that good performance is obtained while keeping compile time as low as possible.
        /// </summary>
        Balanced = 4,
    }

    // FloatMode and FloatPrecision must be kept in sync with burst.h / Burst.Backend

    /// <summary>
    /// Represents the floating point optimization mode for compilation.
    /// </summary>
    public enum FloatMode
    {
        /// <summary>
        /// Use the default target floating point mode - <see cref="FloatMode.Strict"/>.
        /// </summary>
        Default = 0,

        /// <summary>
        /// No floating point optimizations are performed.
        /// </summary>
        Strict = 1,

        /// <summary>
        /// Ensure that floating point calculations are deterministic (64-bit only).
        /// </summary>
        Deterministic = 2,

        /// <summary>
        /// Allows algebraically equivalent optimizations (which can alter the results of calculations), it implies :
        /// <para/> optimizations can assume results and arguments contain no NaNs or +/- Infinity and treat sign of zero as insignificant.
        /// <para/> optimizations can use reciprocals - 1/x * y  , instead of  y/x.
        /// <para/> optimizations can use fused instructions, e.g. madd.
        /// </summary>
        Fast = 3,
    }

    /// <summary>
    /// Represents the floating point precision used for certain builtin operations e.g. sin/cos.
    /// </summary>
    public enum FloatPrecision
    {
        /// <summary>
        /// Use the default target floating point precision - <see cref="FloatPrecision.Medium"/>.
        /// </summary>
        Standard = 0,

        /// <summary>
        /// Compute with an accuracy of 1 ULP - highly accurate, but increased runtime as a result, should not be required for most purposes.
        /// </summary>
        High = 1,

        /// <summary>
        /// Compute with an accuracy of 3.5 ULP - considered acceptable accuracy for most tasks.
        /// </summary>
        Medium = 2,

        /// <summary>
        /// Compute with an accuracy lower than or equal to <see cref="FloatPrecision.Medium"/>, with some range restrictions (defined per function).
        /// </summary>
        Low = 3,
    }

    /// <summary>
    /// This attribute is used to tag jobs or function-pointers as being Burst compiled, and optionally set compilation parameters.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Assembly)]
    [UnityEngine.Scripting.RequireAttributeUsages]
    public class BurstCompileAttribute : System.Attribute
    {
        /// <summary>
        /// Gets or sets the float mode of operation for this Burst compilation.
        /// </summary>
        /// <value>
        /// The default is <see cref="FloatMode.Default"/>.
        /// </value>
        public FloatMode FloatMode { get; set; }

        /// <summary>
        /// Gets or sets the floating point precision to use for this Burst compilation.
        /// Allows you to trade accuracy for speed of computation, useful when you don't require much precision.
        /// </summary>
        /// <value>
        /// The default is <see cref="FloatPrecision.Standard"/>.
        /// </value>
        public FloatPrecision FloatPrecision { get; set; }

        internal bool? _compileSynchronously;

        /// <summary>
        /// Gets or sets whether or not to Burst compile the code immediately on first use, or in the background over time.
        /// </summary>
        /// <value>The default is <c>false</c>, <c>true</c> will force this code to be compiled synchronously on first invocation.</value>
        public bool CompileSynchronously
        {
            get => _compileSynchronously.HasValue ? _compileSynchronously.Value : false;
            set => _compileSynchronously = value;
        }

        internal bool? _debug;

        /// <summary>
        /// Gets or sets whether to compile the code in a way that allows it to be debugged.
        /// If this is set to <c>true</c>, the current implementation disables optimisations on this method
        /// allowing it to be debugged using a Native debugger.
        /// </summary>
        /// <value>
        /// The default is <c>false</c>.
        /// </value>
        public bool Debug
        {
            get => _debug.HasValue ? _debug.Value : false;
            set => _debug = value;
        }

        internal bool? _disableSafetyChecks;

        /// <summary>
        /// Gets or sets whether to disable safety checks for the current job or function pointer.
        /// If this is set to <c>true</c>, the current job or function pointer will be compiled
        /// with safety checks disabled unless the global 'Safety Checks/Force On' option is active.
        /// </summary>
        /// <value>
        /// The default is <c>false</c>.
        /// </value>
        public bool DisableSafetyChecks
        {
            get => _disableSafetyChecks.HasValue ? _disableSafetyChecks.Value : false;
            set => _disableSafetyChecks = value;
        }

        internal bool? _disableDirectCall;

        /// <summary>
        /// Gets or sets a boolean to disable the translation of a static method call as direct call to
        /// the generated native method. By default, when compiling static methods with Burst and calling
        /// them from C#, they will be translated to a direct call to the Burst generated method.
        /// code.
        /// </summary>
        /// <value>
        /// The default is <c>false</c>.
        /// </value>
        public bool DisableDirectCall
        {
            get => _disableDirectCall.HasValue ? _disableDirectCall.Value : false;
            set => _disableDirectCall = value;
        }

        /// <summary>
        /// How should this entry-point be optimized.
        /// </summary>
        /// <value>
        /// The default is <see cref="OptimizeFor.Default"/>.
        /// </value>
        public OptimizeFor OptimizeFor { get; set; }

        internal string[] Options { get; set; }

        /// <summary>
        /// Tags a struct/method/class as being Burst compiled, with the default <see cref="FloatPrecision"/>, <see cref="FloatMode"/> and <see cref="CompileSynchronously"/>.
        /// </summary>
        /// <example>
        /// <code>
        /// [BurstCompile]
        /// struct MyMethodsAreCompiledByBurstUsingTheDefaultSettings
        /// {
        ///     //....
        /// }
        ///</code>
        /// </example>
        public BurstCompileAttribute()
        {
        }

        /// <summary>
        /// Tags a struct/method/class as being Burst compiled, with the specified <see cref="FloatPrecision"/> and <see cref="FloatMode"/>.
        /// </summary>
        /// <example>
        /// <code>
        /// [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
        /// struct MyMethodsAreCompiledByBurstWithLowPrecisionAndFastFloatingPointMode
        /// {
        ///     //....
        /// }
        ///</code>
        ///</example>
        /// <param name="floatPrecision">Specify the required floating point precision.</param>
        /// <param name="floatMode">Specify the required floating point mode.</param>
        public BurstCompileAttribute(FloatPrecision floatPrecision, FloatMode floatMode)
        {
            FloatMode = floatMode;
            FloatPrecision = floatPrecision;
        }

        internal BurstCompileAttribute(string[] options)
        {
            Options = options;
        }
    }
}
