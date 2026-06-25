// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics;
using UnityEngine.Bindings;

namespace Unity.Burst
{
    [DebuggerDisplay("{FullName,nq}")]
    [VisibleToOtherModules("UnityEditor.BurstModule")]
    internal sealed class BurstCompileTarget
    {
        public BurstCompileTarget(bool isStaticMethod, string scope, string displayName, string searchText, string fullName, string methodName, string defaultOptions)
        {
            // This is important to clone the options as we don't want to modify the global instance
            var options = BurstCompiler.Options.Clone();
            options.EnableBurstCompilation = true;
            // Enable safety checks by default to match inspector default behavior
            options.EnableBurstSafetyChecks = true;
            // Don't set debug mode, because it disables optimizations.
            options.EnableBurstDebug = false;

            DefaultOptions = defaultOptions;

            TargetCpu = BurstTargetCpu.Auto;
            IsStaticMethod = isStaticMethod;

            Scope = scope;
            DisplayName = displayName;
            SearchText = searchText;
            FullName = fullName;
            MethodName = methodName;

            IsValidTarget = true;
        }

        /// <summary>
        /// <c>true</c> if the Method is directly tagged with a [BurstCompile] attribute
        /// </summary>
        public readonly bool IsStaticMethod;

        public readonly string MethodName;

        public readonly string FullName;

        /// <summary>
        /// Created from concatenating:
        /// - Namespace of the job type, or declaring type of the method.
        /// - Name of the enclosing type if this is a nested type
        /// - Name of the type if this is a method
        /// </summary>
        public readonly string Scope;

        public readonly string DisplayName;

        /// <summary>
        /// The default compiler options
        /// </summary>
        public readonly string DefaultOptions;

        public BurstTargetCpu TargetCpu { get; set; }

        /// <summary>
        /// Set to true if burst compilation is actually requested via proper `[BurstCompile]` attribute:
        /// - On the job if it is a job only
        /// - On the method and parent class it if is a static method
        /// </summary>
        public bool IsValidTarget { get; }

        /// <summary>
        /// Generated raw disassembly (IR, IL, ASM...), or null if disassembly failed (only valid for the current TargetCpu)
        /// </summary>
        public string RawDisassembly;

        /// <summary>
        /// Formatted disassembly for the associated <see cref="RawDisassembly"/>, currently only valid for <see cref="Unity.Burst.DisassemblyKind.Asm"/>
        /// </summary>
        public string FormattedDisassembly;

        public DisassemblyKind DisassemblyKind;

        public bool IsDarkMode { get; set; }

        public bool IsBurstError { get; set; }

        public bool IsLoading = false;

        public bool JustLoaded = false;

        /// <summary>
        /// String to search for in the disassembly for auto-scrolling to entry point.
        /// </summary>
        public string SearchText { get; }

        public bool EnableBurstSafetyChecks { get; set; }
    }

    [VisibleToOtherModules("UnityEditor.BurstModule")]
    internal enum DisassemblyKind
    {
        Asm = 0,
        IL = 1,
        UnoptimizedIR = 2,
        OptimizedIR = 3,
        IRPassAnalysis = 4
    }
}
