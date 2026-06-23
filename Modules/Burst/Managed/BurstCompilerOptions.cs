// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Burst;

using UnityEngine.Bindings;

// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
// NOTE: This file is shared via a csproj cs link in Burst.Compiler.IL
// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

namespace Unity.Burst
{
    internal
    enum GlobalSafetyChecksSettingKind
    {
        Off = 0,
        On = 1,
        ForceOn = 2,
    }

    /// <summary>
    /// Options available at Editor time and partially at runtime to control the behavior of the compilation and to enable/disable burst jobs.
    /// </summary>
    public sealed partial class BurstCompilerOptions
    {
        private const string DisableCompilationArg = "--burst-disable-compilation";

        private const string ForceSynchronousCompilationArg = "--burst-force-sync-compilation";

        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal const string DefaultLibraryName = "lib_burst_generated";

        internal const string BurstInitializeExternalsName = "burst.initialize.externals";
        internal const string BurstInitializeStaticsName = "burst.initialize.statics";

        internal const string PostfixManaged = "$BurstManaged";

        internal static readonly string DefaultCacheFolder = Path.Combine(Environment.CurrentDirectory, "Library", "BurstCache", "JIT");
        internal const string DeleteCacheMarkerFileName = "DeleteCache.txt";

        private static readonly string BackendNameOverride = Environment.GetEnvironmentVariable("UNITY_BURST_BACKEND_NAME_OVERRIDE");

        // -------------------------------------------------------
        // Common options used by the compiler
        // -------------------------------------------------------
        internal const string OptionGroup = "group";
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal const string OptionPlatform = "platform=";
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal const string OptionMinimumOSVersion = "minimum-os-version=";
        internal const string OptionBackend = "backend=";
        internal const string OptionGlobalSafetyChecksSetting = "global-safety-checks-setting=";
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal const string OptionDisableSafetyChecks = "disable-safety-checks";
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal const string OptionDisableOpt = "disable-opt";
        internal const string OptionFastMath = "fastmath";
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal const string OptionTarget = "target=";
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal const string OptionOptLevel = "opt-level=";
        internal const string OptionLogTimings = "log-timings";
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal const string OptionOptForSize = "opt-for-size";
        internal const string OptionFloatPrecision = "float-precision=";
        internal const string OptionFloatMode = "float-mode=";
        internal const string OptionBranchProtection = "branch-protection=";
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal const string OptionWasmEnableThreads = "wasm-enable-threads";
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal const string OptionDisableWarnings = "disable-warnings=";
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal const string OptionAssemblyDefines = "assembly-defines=";
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal const string OptionDump = "dump=";
        internal const string OptionFormat = "format=";
        internal const string OptionDebugTrap = "debugtrap";
        internal const string OptionDisableVectors = "disable-vectors";
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal const string OptionDebug = "debug=";
        internal const string OptionDebugMode = "debugMode";
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal const string OptionStaticLinkage = "generate-static-linkage-methods";
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal const string OptionTempDirectory = "temp-folder=";
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal const string OptionLinkerOptions = "linker-options=";
        internal const string OptionEnableAutoLayoutFallbackCheck = "enable-autolayout-fallback-check";
        internal const string OptionGenerateLinkXml = "generate-link-xml=";
        internal const string OptionMetaDataGeneration = "meta-data-generation";
        internal const string OptionDisableStringInterpolationInExceptionMessages = "disable-string-interpolation-in-exception-messages";
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal const string OptionPlatformConfiguration = "platform-configuration=";
        internal const string OptionStackProtector = "stack-protector=";
        internal const string OptionStackProtectorBufferSize = "stack-protector-buffer-size=";

        // -------------------------------------------------------
        // Options used by the Jit and Bcl compilers
        // -------------------------------------------------------
        internal const string OptionCacheDirectory = "cache-directory=";

        // -------------------------------------------------------
        // Options used by the Jit compiler
        // -------------------------------------------------------
        internal const string OptionJitEnableSynchronousCompilation = "enable-synchronous-compilation";
        internal const string OptionJitManagedDelegateHandle = "managed-delegate-handle=";

        // -------------------------------------------------------
        // Options used by the Aot compiler
        // -------------------------------------------------------
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal const string OptionAotAssemblyFolder = "assembly-folder=";
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal const string OptionRootAssembly = "root-assembly=";
        internal const string OptionIncludeRootAssemblyReferences = "include-root-assembly-references=";
        internal const string OptionAotMethod = "method=";
        internal const string OptionAotType = "type=";
        internal const string OptionAotAssembly = "assembly=";
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal const string OptionAotOutputPath = "output=";
        internal const string OptionAotKeepIntermediateFiles = "keep-intermediate-files";
        internal const string OptionAotNoLink = "nolink";
        internal const string OptionEnableInterpreter = "enable-interpreter";
        internal const string OptionJitIsForFunctionPointer = "is-for-function-pointer";
        internal const string OptionAotOnlyStaticMethods = "only-static-methods";
        internal const string OptionMethodPrefix = "method-prefix=";
        internal const string OptionAotNoNativeToolchain = "no-native-toolchain";
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal const string OptionAotKeyFolder = "key-folder=";
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal const string OptionAotDecodeFolder = "decode-folder=";
        internal const string OptionVerbose = "verbose";
        internal const string OptionValidateExternalToolChain = "validate-external-tool-chain";
        internal const string OptionCompilerThreads = "threads=";
        internal const string OptionChunkSize = "chunk-size=";
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal const string OptionPrintLogOnMissingPInvokeCallbackAttribute = "print-monopinvokecallbackmissing-message";
        internal const string OptionOutputMode = "output-mode=";
        internal const string OptionAlwaysCreateOutput = "always-create-output=";
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal const string OptionAotPdbSearchPaths = "pdb-search-paths=";
        internal const string OptionSafetyChecks = "safety-checks";
        internal const string OptionLibraryOutputMode = "library-output-mode=";
        internal const string OptionCompilationId = "compilation-id=";
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal const string OptionTargetFramework = "target-framework=";
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal const string OptionEnableExceptionHandling = "enable-exception-handling";
        internal const string OptionDiscardAssemblies = "discard-assemblies=";
        internal const string OptionSaveExtraContext = "save-extra-context";
        internal const string OptionEmitVersionSymbol = "emit-version-symbol=";

        internal const string CompilerCommandShutdown = "$shutdown";
        internal const string CompilerCommandCancel = "$cancel";
        internal const string CompilerCommandEnableCompiler = "$enable_compiler";
        internal const string CompilerCommandDisableCompiler = "$disable_compiler";
        internal const string CompilerCommandSetDefaultOptions = "$set_default_options";
        internal const string CompilerCommandTriggerSetupRecompilation = "$trigger_setup_recompilation";
        internal const string CompilerCommandIsCurrentCompilationDone = "$is_current_compilation_done";

        // This one is annoying special - the Unity editor has a detection for this string being in the command and does some
        // job specific logic - meaning that we **cannot** have this string be present in any other command or bugs will occur.
        internal const string CompilerCommandTriggerRecompilation = "$trigger_recompilation";
        internal const string CompilerCommandInitialize = "$initialize";
        internal const string CompilerCommandDomainReload = "$domain_reload";
        internal const string CompilerCommandVersionNotification = "$version";
        internal const string CompilerCommandSetProfileCallbacks = "$set_profile_callbacks";
        internal const string CompilerCommandSetBurstAbortCallback = "$set_burst_abort_callback";
        internal const string CompilerCommandUnloadBurstNatives = "$unload_burst_natives";
        internal const string CompilerCommandIsNativeApiAvailable = "$is_native_api_available";
        internal const string CompilerCommandNotifyAssemblyCompilationNotRequired = "$notify_assembly_compilation_not_required";
        internal const string CompilerCommandNotifyAssemblyCompilationFinished = "$notify_assembly_compilation_finished";
        internal const string CompilerCommandNotifyCompilationStarted = "$notify_compilation_started";
        internal const string CompilerCommandNotifyCompilationFinished = "$notify_compilation_finished";
        internal const string CompilerCommandDirtyAllAssemblies = "$dirty_all_assemblies";
        internal const string CompilerCommandAotCompilation = "$aot_compilation";
        internal const string CompilerCommandRequestInitialiseDebuggerCommmand = "$request_debug_command";
        internal const string CompilerCommandInitialiseDebuggerCommmand = "$load_debugger_interface";
        internal const string CompilerCommandGetInspectorEntryPoints = "$get_inspector_entry_points";
        internal const string CompilerCommandGetInspectorDisassembly = "$get_inspector_disassembly";

        internal static string SerialiseCompilationOptionsSafe(string[] roots, string[] folders, string options)
        {
            var finalSerialise = new string[3];
            finalSerialise[0] = SafeStringArrayHelper.SerialiseStringArraySafe(roots);
            finalSerialise[1] = SafeStringArrayHelper.SerialiseStringArraySafe(folders);
            finalSerialise[2] = options;
            return SafeStringArrayHelper.SerialiseStringArraySafe(finalSerialise);
        }

        internal static (string[] roots, string[] folders, string options) DeserialiseCompilationOptionsSafe(string from)
        {
            var set = SafeStringArrayHelper.DeserialiseStringArraySafe(from);

            return (SafeStringArrayHelper.DeserialiseStringArraySafe(set[0]), SafeStringArrayHelper.DeserialiseStringArraySafe(set[1]), set[2]);
        }

        internal static void AddOption(StringBuilder builder, string option)
        {
            if (builder.Length != 0)
                builder.Append('\n'); // Use \n to separate options

            builder.Append(option);
        }

        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal static string GetOption(string optionName, object value = null)
        {
            if (optionName == null) throw new ArgumentNullException(nameof(optionName));
            return "--" + optionName + (value ?? String.Empty);
        }

        internal static void SerialiseAssemblyDefines(TextWriter writer, KeyValuePair<string, HashSet<string>> assemblyDefines)
        {
            writer.Write(assemblyDefines.Key);
            writer.Write(';');

            var first = true;
            foreach (var asmDefValue in assemblyDefines.Value)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    writer.Write(';');
                }
                writer.Write(asmDefValue);
            }
        }

        // All the following content is exposed to the public interface

        // These fields are only setup at startup
        private static readonly bool _forceDisableBurstCompilation;
        private static readonly bool ForceBurstCompilationSynchronously;

        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal static bool IsSecondaryUnityProcess;

        internal static bool ForceDisableBurstCompilation
        {
        [VisibleToOtherModules("UnityEditor.BurstModule")]
            get => IsSecondaryUnityProcess || _forceDisableBurstCompilation;
        }

        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal bool IsInitializing;

        private bool _enableBurstCompilation;
        private bool _enableBurstCompileSynchronously;
        private bool _enableBurstSafetyChecks;
        private bool _enableBurstTimings;
        private bool _enableBurstDebug;
        private bool _forceEnableBurstSafetyChecks;
        private FloatMode _jitFloatMode;

        private BurstCompilerOptions() : this(false)
        {
        }

        internal BurstCompilerOptions(bool isGlobal)
        {
            IsInitializing = true;

            try
            {
                IsGlobal = isGlobal;
                // By default, burst is enabled as well as safety checks
                EnableBurstCompilation = true;
                EnableBurstSafetyChecks = true;
            }
            finally
            {
                IsInitializing = false;
            }
        }

        /// <summary>
        /// <c>true</c> if this option is the global options that affects menus
        /// </summary>
        private bool IsGlobal { get; }

        /// <summary>
        /// Gets a boolean indicating whether burst is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get => EnableBurstCompilation && !ForceDisableBurstCompilation;
        }

        /// <summary>
        /// Gets or sets a boolean to enable or disable compilation of burst jobs.
        /// </summary>
        public bool EnableBurstCompilation
        {
            get => _enableBurstCompilation;
            set
            {
                // If we are in the global settings, and we are forcing to no burst compilation
                if (IsGlobal && ForceDisableBurstCompilation) value = false;

                bool changed = _enableBurstCompilation != value;

                _enableBurstCompilation = value;

                // Modify only JobsUtility.JobCompilerEnabled when modifying global settings
                if (IsGlobal)
                {
                    if (changed)
                    {
                        // Send the command to the compiler service
                        if (value)
                        {
                            BurstCompiler.Enable();
                            MaybeTriggerRecompilation();
                        }
                        else
                        {
                            BurstCompiler.Disable();
                        }
                    }

                    // Store the option directly into BurstCompiler.IsEnabled
                    BurstCompiler._IsEnabled = value;
                }

                if (changed)
                {
                    OnOptionsChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets a boolean to force the compilation of all burst jobs synchronously.
        /// </summary>
        /// <remarks>
        /// This is only available at Editor time. Does not have an impact on player mode.
        /// </remarks>
        public bool EnableBurstCompileSynchronously
        {
            get => _enableBurstCompileSynchronously;
            set
            {
                bool changed = _enableBurstCompileSynchronously != value;
                _enableBurstCompileSynchronously = value;
                if (changed) OnOptionsChanged();
            }
        }

        /// <summary>
        /// Gets or sets a boolean to enable or disable safety checks.
        /// </summary>
        /// <remarks>
        /// This is only available at Editor time. Does not have an impact on player mode.
        /// </remarks>
        public bool EnableBurstSafetyChecks
        {
            get => _enableBurstSafetyChecks;
            set
            {
                bool changed = _enableBurstSafetyChecks != value;

                _enableBurstSafetyChecks = value;
                if (changed)
                {
                    OnOptionsChanged();
                    MaybeTriggerRecompilation();
                }
            }
        }

        /// <summary>
        /// Gets or sets a boolean to force enable safety checks, irrespective of what
        /// <c>EnableBurstSafetyChecks</c> is set to, or whether the job or function
        /// has <c>DisableSafetyChecks</c> set.
        /// </summary>
        /// <remarks>
        /// This is only available at Editor time. Does not have an impact on player mode.
        /// </remarks>
        public bool ForceEnableBurstSafetyChecks
        {
            get => _forceEnableBurstSafetyChecks;
            set
            {
                bool changed = _forceEnableBurstSafetyChecks != value;

                _forceEnableBurstSafetyChecks = value;
                if (changed)
                {
                    OnOptionsChanged();
                    MaybeTriggerRecompilation();
                }
            }
        }

        /// <summary>
        /// Floating point mode for JIT compilation
        /// </summary>
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal FloatMode JitFloatMode
        {
            get => _jitFloatMode;
            set
            {
                bool changed = _jitFloatMode != value;
                _jitFloatMode = value;
                if (changed)
                    OnOptionsChanged();
            }
        }

        /// <summary>
        /// Enable debugging mode
        /// </summary>
        public bool EnableBurstDebug
        {
            get => _enableBurstDebug;
            set
            {
                bool changed = _enableBurstDebug != value;

                _enableBurstDebug = value;
                if (changed)
                {
                    OnOptionsChanged();
                    MaybeTriggerRecompilation();
                }
            }
        }

        /// <summary>
        /// This property is no longer used and will be removed in a future major release.
        /// </summary>
        [Obsolete("This property is no longer used and will be removed in a future major release")]
        public bool DisableOptimizations
        {
            get => false;
            set
            {
            }
        }

        /// <summary>
        /// This property is no longer used and will be removed in a future major release. Use the [BurstCompile(FloatMode = FloatMode.Fast)] on the method directly to enable this feature
        /// </summary>
        [Obsolete("This property is no longer used and will be removed in a future major release. Use the [BurstCompile(FloatMode = FloatMode.Fast)] on the method directly to enable this feature")]
        public bool EnableFastMath
        {
            get => true;

            set
            {
                // ignored
            }
        }

        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal bool EnableBurstTimings
        {
            get => _enableBurstTimings;
            set
            {
                bool changed = _enableBurstTimings != value;
                _enableBurstTimings = value;
                if (changed) OnOptionsChanged();
            }
        }

        internal bool RequiresSynchronousCompilation => EnableBurstCompileSynchronously || ForceBurstCompilationSynchronously;

        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal Action OptionsChanged { get; set; }

        internal BurstCompilerOptions Clone()
        {
            // WARNING: for some reason MemberwiseClone() is NOT WORKING on Mono/Unity
            // so we are creating a manual clone
            var clone = new BurstCompilerOptions
            {
                EnableBurstCompilation = EnableBurstCompilation,
                EnableBurstCompileSynchronously = EnableBurstCompileSynchronously,
                EnableBurstSafetyChecks = EnableBurstSafetyChecks,
                EnableBurstTimings = EnableBurstTimings,
                EnableBurstDebug = EnableBurstDebug,
                ForceEnableBurstSafetyChecks = ForceEnableBurstSafetyChecks,
                JitFloatMode = JitFloatMode,
            };
            return clone;
        }

        private static bool TryGetAttribute(MemberInfo member, out BurstCompileAttribute attribute)
        {
            attribute = null;
            // We don't fail if member == null as this method is being called by native code and doesn't expect to crash
            if (member == null)
            {
                return false;
            }

            // Fetch options from attribute
            attribute = GetBurstCompileAttribute(member);
            if (attribute == null)
            {
                return false;
            }

            return true;
        }

        private static bool TryGetAttribute(Assembly assembly, out BurstCompileAttribute attribute)
        {
            // We don't fail if assembly == null as this method is being called by native code and doesn't expect to crash
            if (assembly == null)
            {
                attribute = null;
                return false;
            }

            // Fetch options from attribute
            attribute = assembly.GetCustomAttribute<BurstCompileAttribute>();

            return attribute != null;
        }

        private static BurstCompileAttribute GetBurstCompileAttribute(MemberInfo memberInfo)
        {
            var result = memberInfo.GetCustomAttribute<BurstCompileAttribute>();
            if (result != null)
            {
                return result;
            }

            foreach (var a in memberInfo.GetCustomAttributes())
            {
                var attributeType = a.GetType();

                if (attributeType.FullName == "Burst.Compiler.IL.Tests.TestCompilerAttribute")
                {
                    var options = new List<string>();

                    return new BurstCompileAttribute(FloatPrecision.Standard, FloatMode.Default)
                    {
                        CompileSynchronously = true,
                        Options = options.ToArray(),
                    };
                }

                /*
                 * TODO @elliotc: once the publicification of burstlike lands in <like a proton but without the charge> main, change this to
                 * actually copy over all the info from inside the burstlike burstcompile attribute into the real one.
                 * This is just a stopgap because currently BurstLike is internal in <like a proton but without the charge> main.
                 */
                if (attributeType.FullName == "Unity.Collections.LowLevel.Unsafe.BurstLike.BurstCompileAttribute" ||
                    attributeType.FullName == "Unity.Collections.LowLevel.Unsafe.BurstLike+BurstCompileAttribute" ||  // prior to BurstLike being a namespace
                    attributeType.FullName == "Unity.Collections.LowLevel.Unsafe.BurstLike/BurstCompileAttribute")  // prior to BurstLike being a namespace
                {
                    var options = new List<string>();

                    return new BurstCompileAttribute(FloatPrecision.Standard, FloatMode.Default)
                    {
                        CompileSynchronously = false,
                        Options = options.ToArray(),
                    };
                }
            }

            return null;
        }

        internal static bool HasBurstCompileAttribute(MemberInfo member)
        {
            if (member == null) throw new ArgumentNullException(nameof(member));
            BurstCompileAttribute attr;
            return TryGetAttribute(member, out attr);
        }

        /// <summary>
        /// Merges the attributes from the assembly into the member attribute, such that if any field of the member attribute
        /// was not specifically set by the user (or is a default), the assembly level setting is used for the Burst compilation.
        /// </summary>
        private static void MergeAttributes(ref BurstCompileAttribute memberAttribute, in BurstCompileAttribute assemblyAttribute)
        {
            if (memberAttribute.FloatMode == FloatMode.Default)
            {
                memberAttribute.FloatMode = assemblyAttribute.FloatMode;
            }

            if (memberAttribute.FloatPrecision == FloatPrecision.Standard)
            {
                memberAttribute.FloatPrecision = assemblyAttribute.FloatPrecision;
            }

            if (memberAttribute.OptimizeFor == OptimizeFor.Default)
            {
                memberAttribute.OptimizeFor = assemblyAttribute.OptimizeFor;
            }

            if (!memberAttribute._compileSynchronously.HasValue && assemblyAttribute._compileSynchronously.HasValue)
            {
                memberAttribute._compileSynchronously = assemblyAttribute._compileSynchronously;
            }

            if (!memberAttribute._debug.HasValue && assemblyAttribute._debug.HasValue)
            {
                memberAttribute._debug = assemblyAttribute._debug;
            }

            if (!memberAttribute._disableDirectCall.HasValue && assemblyAttribute._disableDirectCall.HasValue)
            {
                memberAttribute._disableDirectCall = assemblyAttribute._disableDirectCall;
            }

            if (!memberAttribute._disableSafetyChecks.HasValue && assemblyAttribute._disableSafetyChecks.HasValue)
            {
                memberAttribute._disableSafetyChecks = assemblyAttribute._disableSafetyChecks;
            }
        }

        /// <summary>
        /// Gets the options for the specified member. Returns <c>false</c> if the `[BurstCompile]` attribute was not found.
        /// </summary>
        /// <returns><c>false</c> if the `[BurstCompile]` attribute was not found; otherwise <c>true</c></returns>
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal bool TryGetOptions(MemberInfo member, out string flagsOut)
        {
            flagsOut = null;
            if (!TryGetAttribute(member, out var memberAttribute))
            {
                return false;
            }

            if (TryGetAttribute(member.Module.Assembly, out var assemblyAttribute))
            {
                MergeAttributes(ref memberAttribute, in assemblyAttribute);
            }

            flagsOut = GetOptions(memberAttribute);
            return true;
        }

        private string GetOptions(BurstCompileAttribute attr)
        {
            var flagsBuilderOut = new StringBuilder();

            if ((attr?.CompileSynchronously ?? false) || RequiresSynchronousCompilation)
            {
                AddOption(flagsBuilderOut, GetOption(OptionJitEnableSynchronousCompilation));
            }

            return flagsBuilderOut.ToString();
        }

        internal string GetCompilerClientDefaultOptions()
        {
            var flagsBuilderOut = new StringBuilder();

            AddOption(flagsBuilderOut, GetOption(OptionDebug, BurstCompiler.IsScriptDebugInfoEnabled && EnableBurstDebug ? "Full" : "LineOnly"));

            if (ForceEnableBurstSafetyChecks)
            {
                AddOption(flagsBuilderOut, GetOption(OptionGlobalSafetyChecksSetting, GlobalSafetyChecksSettingKind.ForceOn));
            }
            else if (EnableBurstSafetyChecks)
            {
                AddOption(flagsBuilderOut, GetOption(OptionGlobalSafetyChecksSetting, GlobalSafetyChecksSettingKind.On));
            }
            else
            {
                AddOption(flagsBuilderOut, GetOption(OptionGlobalSafetyChecksSetting, GlobalSafetyChecksSettingKind.Off));
            }

            if (EnableBurstTimings)
            {
                AddOption(flagsBuilderOut, GetOption(OptionLogTimings));
            }

            if (EnableBurstDebug)
            {
                AddOption(flagsBuilderOut, GetOption(OptionDebugMode));
            }

            if (JitFloatMode != FloatMode.Default)
            {
                AddOption(flagsBuilderOut, GetOption(OptionFloatMode, JitFloatMode));
            }

            if (BackendNameOverride != null)
            {
                AddOption(flagsBuilderOut, GetOption(OptionBackend, BackendNameOverride));
            }

            AddOption(flagsBuilderOut, GetOption(OptionTargetFramework, "NetFramework"));

            AddOption(flagsBuilderOut, GetOption(OptionTempDirectory, Path.Combine(Environment.CurrentDirectory, "Temp", "Burst")));

            return flagsBuilderOut.ToString();
        }

        private void OnOptionsChanged()
        {
            OptionsChanged?.Invoke();
        }

        private void MaybeTriggerRecompilation()
        {
            if (IsGlobal && IsEnabled && !IsInitializing)
            {
                BurstCompiler.TriggerRecompilation();
            }
        }

        /// <summary>
        /// Static initializer based on command line arguments
        /// </summary>
        static BurstCompilerOptions()
        {
            foreach (var arg in Environment.GetCommandLineArgs())
            {
                switch (arg)
                {
                    case DisableCompilationArg:
                        _forceDisableBurstCompilation = true;
                        break;
                    case ForceSynchronousCompilationArg:
                        ForceBurstCompilationSynchronously = true;
                        break;
                }
            }
        }
    }

    // NOTE: This must be synchronized with Backend.TargetPlatform
    [VisibleToOtherModules("UnityEditor.BurstModule")]
    internal enum TargetPlatform
    {
        Windows = 0,
        macOS = 1,
        Linux = 2,
        Android = 3,
        iOS = 4,
        PS4 = 5,
        XboxOne_Deprecated = 6,
        WASM = 7,
        UWP = 8,
        Lumin = 9,
        Switch = 10,
        Stadia_Deprecated = 11,
        tvOS = 12,
        EmbeddedLinux = 13,
        GameCoreXboxOne = 14,
        GameCoreXboxSeries = 15,
        PS5 = 16,
        QNX = 17,
        visionOS = 18,
        visionSimulator = 19,
        Switch2 = 20,
        iOSSimulator = 21,
        tvOSSimulator = 22,
    }

    // Don't expose the enum in Burst.Compiler.IL, need only in Unity.Burst.dll which is referenced by Burst.Compiler.IL.Tests
    // Make the enum public for btests via Unity.Burst.dll; leave it internal in the package
        [VisibleToOtherModules("UnityEditor.BurstModule")]
    internal
    // NOTE: This must be synchronized with Backend.TargetCpu
    enum BurstTargetCpu
    {
        Auto = 0,
        X86_SSE2 = 1,
        X86_SSE4 = 2,
        X64_SSE2 = 3,
        X64_SSE4 = 4,
        AVX = 5,
        AVX2 = 6,
        WASM32 = 7,
        ARMV7A_NEON32 = 8,
        ARMV8A_AARCH64 = 9,
        THUMB2_NEON32 = 10,
        ARMV8A_AARCH64_HALFFP = 11,
        ARMV9A = 12,
        WASM64 = 13,
    }

    /// <summary>
    /// Flags used by NativeCompiler.CompileMethod to dump intermediate compiler results.
    /// Note please ensure MonoDebuggerHandling/Constants.h is updated if you change this enum
    /// </summary>
    [Flags]
        [VisibleToOtherModules("UnityEditor.BurstModule")]
    internal enum NativeDumpFlags
    {
        /// <summary>
        /// Nothing is selected.
        /// </summary>
        None = 0,

        /// <summary>
        /// Dumps the IL of the method being compiled
        /// </summary>
        IL = 1 << 0,

        /// <summary>
        /// Unused dump state.
        /// </summary>
        Unused = 1 << 1,

        /// <summary>
        /// Dumps the generated module without optimizations
        /// </summary>
        IR = 1 << 2,

        /// <summary>
        /// Dumps the generated backend code after optimizations (if enabled)
        /// </summary>
        IROptimized = 1 << 3,

        /// <summary>
        /// Dumps the generated ASM code
        /// </summary>
        Asm = 1 << 4,

        /// <summary>
        /// Generate the native code
        /// </summary>
        Function = 1 << 5,

        /// <summary>
        /// Dumps the result of analysis
        /// </summary>
        Analysis = 1 << 6,

        /// <summary>
        /// Dumps the diagnostics from optimisation
        /// </summary>
        IRPassAnalysis = 1 << 7,

        /// <summary>
        /// Dumps the IL before all transformation of the method being compiled
        /// </summary>
        ILPre = 1 << 8,

        /// <summary>
        /// Dumps the per-entry-point module
        /// </summary>
        IRPerEntryPoint = 1 << 9,

        /// <summary>
        /// Dumps all normal output.
        /// </summary>
        All = IL | ILPre | IR | IROptimized | IRPerEntryPoint | Asm | Function | Analysis | IRPassAnalysis
    }

    /// <summary>
    /// Some options cannot be applied until after an Editor restart, in Editor versions prior to 2019.3.
    /// This class assists with allowing the relevant settings to be changed via the menu,
    /// followed by displaying a message to the user to say a restart is necessary.
    /// </summary>
        [VisibleToOtherModules("UnityEditor.BurstModule")]
    internal static class RequiresRestartUtility
    {
        [ThreadStatic]
        public static bool CalledFromUI;

        [ThreadStatic]
        public static bool RequiresRestart;
    }
}
