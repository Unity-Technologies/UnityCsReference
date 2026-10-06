// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using System.Text;
using System.Runtime.CompilerServices;
using Unity.Scripting.LifecycleManagement;
using Unity.Collections.LowLevel.Unsafe;


using System.Linq;

using UnityEngine.Bindings;

namespace Unity.Burst
{
    /// <summary>
    /// The burst compiler runtime frontend.
    /// </summary>
    ///
    public static partial class BurstCompiler
    {
        /// <summary>
        /// Check if the LoadAdditionalLibrary API is supported by the current version of Unity
        /// </summary>
        /// <returns>True if the LoadAdditionalLibrary API can be used by the current version of Unity</returns>
        public static bool IsLoadAdditionalLibrarySupported()
        {
            return IsApiAvailable("LoadBurstLibrary");
        }

        // Pins the delegates backing the native callback pointers below; cleared by BurstLoader's
        // [OnCodeUnloading] teardown (FreeGCHandles), which reruns AllocateDelegateHandles on reload.
        [NoAutoStaticsCleanup] // cleared manually by FreeGCHandles(), called by BurstLoader's [OnCodeUnloading] teardown
        private static readonly List<GCHandle> DelegateGcHandles = new List<GCHandle>();

        private static GCHandle AllocGCHandle(object value)
        {
            var handle = GCHandle.Alloc(value);
            lock (DelegateGcHandles)
            {
                DelegateGcHandles.Add(handle);
            }
            return handle;
        }

        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal static void AllocateDelegateHandles()
        {
            // Store pointers to Log and Compile callback methods.
            // For more info about why we need to do this, see comments in CallbackStubManager.
            string GetFunctionPointer<TDelegate>(TDelegate callback)
            {
                AllocGCHandle(callback); // Ensure delegate is not garbage-collected before ALC unload.
                var callbackFunctionPointer = Marshal.GetFunctionPointerForDelegate(callback);
                return "0x" + callbackFunctionPointer.ToInt64().ToString("X16");
            }

            unsafe
            {
                EagerCompileLogCallbackFunctionPointer = GetFunctionPointer<LogCallbackDelegate>(EagerCompileLogCallback);
            }
            ManagedResolverFunctionPointer = GetFunctionPointer<ManagedFnPtrResolverDelegate>(ManagedResolverFunction);
            ProgressCallbackFunctionPointer = GetFunctionPointer<ProgressCallbackDelegate>(ProgressCallback);
            ProfileBeginCallbackFunctionPointer = GetFunctionPointer<ProfileBeginCallbackDelegate>(ProfileBeginCallback);
            ProfileEndCallbackFunctionPointer = GetFunctionPointer<ProfileEndCallbackDelegate>(ProfileEndCallback);
            BurstAbortFunctionPointer = GetFunctionPointer<BurstAbortDelegate>(BurstAbort);
        }

        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal static void FreeGCHandles()
        {
            lock (DelegateGcHandles)
            {
                DelegateGcHandles.ForEach(handle => handle.Free());
                DelegateGcHandles.Clear();
            }

            EagerCompileLogCallbackFunctionPointer = null;
            ManagedResolverFunctionPointer = null;
            ProgressCallbackFunctionPointer = null;
            ProfileBeginCallbackFunctionPointer = null;
            ProfileEndCallbackFunctionPointer = null;
            BurstAbortFunctionPointer = null;
        }

        private class CommandBuilder
        {
            private StringBuilder _builder;
            private bool _hasArgs;

            public CommandBuilder()
            {
                _builder = new StringBuilder();
                _hasArgs = false;
            }

            public CommandBuilder Begin(string cmd)
            {
                _builder.Clear();
                _hasArgs = false;
                _builder.Append(cmd);
                return this;
            }

            public CommandBuilder With(string arg)
            {
                if (!_hasArgs) _builder.Append(' ');
                _hasArgs = true;
                _builder.Append(arg);
                return this;
            }

            public CommandBuilder With(IntPtr arg)
            {
                if (!_hasArgs) _builder.Append(' ');
                _hasArgs = true;
                _builder.AppendFormat("0x{0:X16}", arg.ToInt64());
                return this;
            }

            public CommandBuilder And(char sep = '|')
            {
                _builder.Append(sep);
                return this;
            }

            public string SendToCompiler()
            {
                return SendRawCommandToCompiler(_builder.ToString());
            }
        }

        [ThreadStatic]
        [NoAutoStaticsCleanup] // reusable scratch buffer, reset via Begin() before each command, holds no cross-call state
        private static CommandBuilder _cmdBuilder;

        private static CommandBuilder BeginCompilerCommand(string cmd)
        {
            if (_cmdBuilder == null)
            {
                _cmdBuilder = new CommandBuilder();
            }

            return _cmdBuilder.Begin(cmd);
        }


        /// <summary>
        /// Internal variable setup by BurstCompilerOptions.
        /// </summary>
        [NoAutoStaticsCleanup] // mirrors BurstCompilerOptions.Options.EnableBurstCompilation, which itself persists across reload
        internal
            static bool _IsEnabled;

        /// <summary>
        /// Gets a value indicating whether Burst is enabled.
        /// </summary>
        public static bool IsEnabled => _IsEnabled;

        /// <summary>
        /// Gets the global options for the burst compiler.
        /// </summary>
        [NoAutoStaticsCleanup] // singleton settings instance; its state is meant to persist across reload
        public static readonly BurstCompilerOptions Options = new BurstCompilerOptions(true);

        /// <summary>
        /// Sets the execution mode for all jobs spawned from now on.
        /// </summary>
        /// <param name="mode">Specifiy the required execution mode</param>
        public static void SetExecutionMode(BurstExecutionEnvironment mode)
        {
            Burst.LowLevel.BurstCompilerService.SetCurrentExecutionMode((uint)mode);
        }
        /// <summary>
        /// Retrieve the current execution mode that is configured.
        /// </summary>
        /// <returns>Currently configured execution mode</returns>
        public static BurstExecutionEnvironment GetExecutionMode()
        {
            return (BurstExecutionEnvironment)Burst.LowLevel.BurstCompilerService.GetCurrentExecutionMode();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void VerifyDelegateIsNotMulticast<T>(T delegateMethod) where T : class
        {
            var delegateKind = delegateMethod as Delegate;
            if (delegateKind.GetInvocationList().Length > 1)
            {
                throw new InvalidOperationException($"Burst does not support multicast delegates, please use a regular delegate for `{delegateMethod}'");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void VerifyDelegateHasCorrectUnmanagedFunctionPointerAttribute<T>(T delegateMethod) where T : class
        {
            var attrib = delegateMethod.GetType().GetCustomAttribute<System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute>();
            if (attrib == null || attrib.CallingConvention != CallingConvention.Cdecl)
            {
                Unity.Scripting.LowLevel.Debug.LogWarning($"The delegate type {delegateMethod.GetType().FullName} should be decorated with [UnmanagedFunctionPointer(CallingConvention.Cdecl)] to ensure runtime interoperabilty between managed code and Burst-compiled code.");
            }
        }

        /// <summary>
        /// Compile the following delegate into a function pointer with burst, invokable from a Burst Job or from regular C#.
        /// </summary>
        /// <typeparam name="T">Type of the delegate of the function pointer</typeparam>
        /// <param name="delegateMethod">The delegate to compile</param>
        /// <returns>A function pointer invokable from a Burst Job or from regular C#</returns>
        public static unsafe FunctionPointer<T> CompileFunctionPointer<T>(T delegateMethod) where T : class
        {
            VerifyDelegateIsNotMulticast<T>(delegateMethod);
            VerifyDelegateHasCorrectUnmanagedFunctionPointerAttribute<T>(delegateMethod);
            void* function = Compile(delegateMethod);
            return new FunctionPointer<T>(new IntPtr(function));
        }

        // Called from IL-postprocessed direct calls.
        [BurstDiscard]
        internal static void CheckPendingException()
        {
            ExceptionMarshaller.CheckPendingException();
        }

        public unsafe delegate void BurstAbortUserDelegate(string exceptionKind, string message, string stackTrace, void* userData);

        private static Exception ConstructException(string kind, string message, string callStack)
        {
            var getExceptionType = Type.GetType(kind);
            if (getExceptionType != null)
            {
                var text = $"{message}\n{callStack}\nEnd of stack trace from the original location.\n";

                // Prefer the (message, innerException) overload: it is consistently message-first across the BCL,
                // whereas the single-string overload of e.g. ArgumentNullException takes a paramName, which would
                // leave the exception reporting "Value cannot be null. (Parameter '<everything we passed>')".
                var ctor = getExceptionType.GetConstructor(new Type[] { typeof(string), typeof(Exception) });
                if (ctor != null)
                {
                    return (Exception)ctor.Invoke(new object[] { text, null });
                }

                ctor = getExceptionType.GetConstructor(new Type[] { typeof(string) });
                if (ctor != null)
                {
                    return (Exception)ctor.Invoke(new object[] { text });
                }
            }
            return new Exception($"{kind}: {message}\n{callStack}\nEnd of stack trace from the original location.\n");
        }

        [BurstDiscard]
        private static unsafe void SafeInvokeDirectCallFreeAndReThrow(SafeInvoke info)
        {
            if (info.aborted == 0)
            {
                return;
            }
            var kind = Marshal.PtrToStringUni(info.kind);
            var message = Marshal.PtrToStringUni(info.message);
            var callStack = Marshal.PtrToStringUni(info.callStack);
            Marshal.FreeHGlobal(info.kind);                         // If we threw, we allocated memory for the strings, free it
            Marshal.FreeHGlobal(info.message);
            Marshal.FreeHGlobal(info.callStack);
            throw ConstructException(kind, message, callStack);
        }

        [BurstDiscard]
        private static unsafe void DirectCallSafeAbort(string kind, string message, string callStack, void* user)
        {
            var safeInvoke = (SafeInvoke*)user;
            safeInvoke->kind = Marshal.StringToHGlobalUni(kind);
            safeInvoke->message = Marshal.StringToHGlobalUni(message);
            safeInvoke->callStack = Marshal.StringToHGlobalUni(callStack);
            safeInvoke->aborted = 1;
        }

        private unsafe struct CallbackData
        {
            public BurstAbortUserDelegate callback;
            public void* userData;
        }

        private struct SafeInvoke
        {
            public IntPtr kind;
            public IntPtr message;
            public IntPtr callStack;
            public byte aborted;
        }

        [ThreadStatic]
        [NoAutoStaticsCleanup] // empty except while a single Push/Pop-bracketed call is on this thread's stack; holds no cross-call state
        static Stack<CallbackData> threadUserCallbacks = null;

        [NoAutoStaticsCleanup] // fixed delegate to this class's own DirectCallSafeAbort; never reassigned, not a user-registered callback
        private static readonly unsafe BurstAbortUserDelegate SafeInvokeDelegate = DirectCallSafeAbort;

        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal static void ManagedBurstAbort(string kind, string message, string callstack)
        {
            if (kind == "*ThrowPendingException*" && message == "*ThrowPendingException*")
            {
                try
                {
                    BurstCompiler.CheckPendingException();
                    return;
                } catch (Exception e)
                {
                    if (Unity.Burst.LowLevel.BurstCompilerService.IsInBurstedJob())
                    {
                        Unity.Scripting.LowLevel.Debug.LogException(e, invokePostProcessCallbacks: true);
                        return;
                    } else
                    {
                        kind = e.GetType().FullName;
                        message = e.Message;
                        callstack = e.StackTrace;
                    }
                }
            }

            // Don't want to re-add the exception we are currently handling to BurstFPException
            // if (!callstack.Contains("BurstFPException.CheckAndThrow")
            //     && !callstack.Contains("burst.check_pending_exception"))
            // {
            //     BurstFPException.AddException(BurstFPException.GetID(), kind, message, callstack);
            // }

            if ((threadUserCallbacks != null) && (threadUserCallbacks.Count > 0))
            {
                var data = threadUserCallbacks.Peek();
                unsafe
                {
                    data.callback(kind, message, callstack, data.userData);
                }
            }
            else if (Unity.Burst.LowLevel.BurstCompilerService.IsInBurstedJob())
            {
                var e = ConstructException(kind, message, callstack);
                Unity.Scripting.LowLevel.Debug.LogException(e, invokePostProcessCallbacks: true);
            }
            else
            {
                Unity.Scripting.LowLevel.Debug.LogError($"A method compiled with Burst, has thrown a Managed Exception - {kind}({message}) at :\n{callstack}");
            }
        }

        [BurstDiscard]
        public unsafe static void PushUserAbortCallbackForThread(BurstAbortUserDelegate callback, void* userData)
        {
            if (threadUserCallbacks == null)
            {
                threadUserCallbacks = new Stack<CallbackData>();
            }
            threadUserCallbacks.Push(new CallbackData { callback = callback, userData = userData });
        }

        [BurstDiscard]
        public unsafe static void PopUserAbortCallbackForThread()
        {
            threadUserCallbacks.Pop();
        }

        private static unsafe void* Compile(object delegateObj)
        {
            if (!(delegateObj is Delegate)) throw new ArgumentException("object instance must be a System.Delegate", nameof(delegateObj));
            var delegateMethod = (Delegate)delegateObj;
            return Compile(delegateMethod, delegateMethod.Method);
        }

        private static unsafe void* Compile(object delegateObj, MethodInfo methodInfo)
        {
            if (delegateObj == null) throw new ArgumentNullException(nameof(delegateObj));

            if (delegateObj.GetType().IsGenericType)
            {
                throw new InvalidOperationException($"The delegate type `{delegateObj.GetType()}` must be a non-generic type");
            }
            if (!methodInfo.IsStatic)
            {
                throw new InvalidOperationException($"The method `{methodInfo}` must be static. Instance methods are not supported");
            }
            if (methodInfo.IsGenericMethod)
            {
                throw new InvalidOperationException($"The method `{methodInfo}` must be a non-generic method");
            }


            void* function;


            Delegate managedFallbackDelegateMethod = delegateObj as Delegate;

            var delegateMethod = delegateObj as Delegate;

            // In case Burst is disabled entirely from the command line
            if (BurstCompilerOptions.ForceDisableBurstCompilation)
            {
                AllocGCHandle(managedFallbackDelegateMethod);
                function = (void*)Marshal.GetFunctionPointerForDelegate(managedFallbackDelegateMethod);
                return function;
            }

            // Make sure that the delegate will never be collected
            var delHandle = AllocGCHandle(managedFallbackDelegateMethod);
            var defaultOptions = "--" + BurstCompilerOptions.OptionJitManagedDelegateHandle + "0x" + ManagedResolverFunctionPointer + "|" + "0x" + GCHandle.ToIntPtr(delHandle).ToInt64().ToString("X16");

            string extraOptions;
            // The attribute is directly on the method, so we recover the underlying method here
            if (Options.TryGetOptions(methodInfo, out extraOptions))
            {
                if (!string.IsNullOrWhiteSpace(extraOptions))
                {
                    defaultOptions += "\n" + extraOptions;
                }

                var delegateMethodId = Unity.Burst.LowLevel.BurstCompilerService.CompileAsyncDelegateMethod(delegateObj, defaultOptions);
                function = Unity.Burst.LowLevel.BurstCompilerService.GetAsyncCompiledAsyncDelegateMethod(delegateMethodId);
            }
            else
            {
                throw new InvalidOperationException($"Burst cannot compile the function pointer `{methodInfo}` because the `[BurstCompile]` attribute is missing");
            }
            // Should not happen but in that case, we are still trying to generated an error
            // It can be null if we are trying to compile a function in a standalone player
            // and the function was not compiled. In that case, we need to output an error
            if (function == null)
            {
                throw new InvalidOperationException($"Burst failed to compile the function pointer `{methodInfo}`");
            }

            // When burst compilation is disabled, we are still returning a valid stub function pointer (the a pointer to the managed function)
            // so that CompileFunctionPointer actually returns a delegate in all cases
            return function;
        }

        /// <summary>
        /// Lets the compiler service know we are shutting down, called by the event on OnDomainUnload, if EditorApplication.quitting was called
        /// </summary>
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal static void Shutdown()
        {
            SendCommandToCompiler(BurstCompilerOptions.CompilerCommandShutdown);
        }

        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal static void SetDefaultOptions()
        {
            SendCommandToCompiler(BurstCompilerOptions.CompilerCommandSetDefaultOptions, Options.GetCompilerClientDefaultOptions());
        }


        [VisibleToOtherModules("UnityEditor.BurstModule")]
        // We need this to be queried each domain reload in a static constructor so that it is called on the main thread only!
        [NoAutoStaticsCleanup] // reassigned by BurstLoader.InitBurstLoader on every domain reload before this is read
        internal static bool IsScriptDebugInfoEnabled { get; set; }

        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal static void DomainReload(IEnumerable<(string, string[])> assemblyNamesAndDefines, bool packagesChanged)
        {
            Debug.Assert(EagerCompileLogCallbackFunctionPointer != null);
            Debug.Assert(ProgressCallbackFunctionPointer != null);

            const string parameterSeparator = "***";
            const string assemblySeparator = "```";

            var cmdBuilder =
                BeginCompilerCommand(BurstCompilerOptions.CompilerCommandDomainReload)
                    .With(ProgressCallbackFunctionPointer)
                    .With(parameterSeparator)
                    .With(EagerCompileLogCallbackFunctionPointer)
                    .With(parameterSeparator);

            foreach (var (name, defines) in assemblyNamesAndDefines)
            {
                cmdBuilder
                    .With($"{name}|{string.Join(";", defines)}")
                    .With(assemblySeparator);
            }

            cmdBuilder.SendToCompiler();

            if (packagesChanged)
            {
                BeginCompilerCommand(BurstCompilerOptions.CompilerCommandDirtyAllAssemblies)
                    .SendToCompiler();
            }
        }

        internal static string VersionNotify(string version)
        {
            return SendCommandToCompiler(BurstCompilerOptions.CompilerCommandVersionNotification, version);
        }

        /// <summary>
        /// Cancel any compilation being processed by the JIT Compiler in the background.
        /// </summary>
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal static void Cancel()
        {
            SendCommandToCompiler(BurstCompilerOptions.CompilerCommandCancel);
        }

        /// <summary>
        /// Check if there is any job pending related to the last compilation ID.
        /// </summary>
        internal static bool IsCurrentCompilationDone()
        {
            return SendCommandToCompiler(BurstCompilerOptions.CompilerCommandIsCurrentCompilationDone) == "True";
        }

        internal static void Enable()
        {
            SendCommandToCompiler(BurstCompilerOptions.CompilerCommandEnableCompiler);
        }

        internal static void Disable()
        {
            SendCommandToCompiler(BurstCompilerOptions.CompilerCommandDisableCompiler);
        }

        internal static void TriggerRecompilation()
        {
            OnBeginProgressBar("Waiting for compilation to finish");
            try
            {
                SetDefaultOptions();

                // This is done separately from CompilerCommandTriggerRecompilation below,
                // because CompilerCommandTriggerRecompilation will cause all jobs to re-request
                // their function pointers from Burst, and we need to have actually triggered
                // compilation by that point.
                SendCommandToCompiler(BurstCompilerOptions.CompilerCommandTriggerSetupRecompilation);

                SendCommandToCompiler(BurstCompilerOptions.CompilerCommandTriggerRecompilation, Options.RequiresSynchronousCompilation.ToString());
            }
            finally
            {
                OnEndProgressBar();
            }
        }

        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal static void UnloadAdditionalLibraries()
        {
            SendCommandToCompiler(BurstCompilerOptions.CompilerCommandUnloadBurstNatives);
        }

        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal static bool IsApiAvailable(string apiName)
        {
            return SendCommandToCompiler(BurstCompilerOptions.CompilerCommandIsNativeApiAvailable, apiName) == "True";
        }

        private unsafe delegate void LogCallbackDelegate(void* userData, int logType, byte* message, byte* fileName, int lineNumber);

        private static unsafe void EagerCompileLogCallback(void* userData, int logType, byte* message, byte* fileName, int lineNumber)
        {
            if (EagerCompilationLoggingEnabled)
            {
                BurstRuntime.Log(message, logType, fileName, lineNumber);
            }
        }


        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr ManagedFnPtrResolverDelegate(IntPtr handleVal);

        private static IntPtr ManagedResolverFunction(IntPtr handleVal)
        {
            var delegateObj = GCHandle.FromIntPtr(handleVal).Target;
            var fnptr = Marshal.GetFunctionPointerForDelegate(delegateObj);
            return fnptr;
        }

        [VisibleToOtherModules("UnityEditor.BurstModule")]
        [NoAutoStaticsCleanup] // unconditionally re-set true by BurstLoader.InitBurstLoader on every domain reload before use
        internal static bool EagerCompilationLoggingEnabled = false;

        [NoAutoStaticsCleanup] // recomputed by AllocateDelegateHandles(), called by BurstLoader.InitBurstLoader on every domain reload before use
        private static string EagerCompileLogCallbackFunctionPointer;
        [NoAutoStaticsCleanup] // see EagerCompileLogCallbackFunctionPointer above
        private static string ManagedResolverFunctionPointer;

        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal static void Initialize(string dotNetPath, string burstRuntimePath, string applicationContentsPath, string[] assemblyFolders, string[] ignoreAssemblies)
        {
            var glued = new string[6];
            glued[0] = "CoreClr";
            glued[1] = dotNetPath;
            glued[2] = burstRuntimePath;
            glued[3] = applicationContentsPath;
            glued[4] = SafeStringArrayHelper.SerialiseStringArraySafe(assemblyFolders);
            glued[5] = SafeStringArrayHelper.SerialiseStringArraySafe(ignoreAssemblies);
            var optionsSet = SafeStringArrayHelper.SerialiseStringArraySafe(glued);
            SendCommandToCompiler(BurstCompilerOptions.CompilerCommandInitialize, optionsSet);
        }

        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal static void NotifyCompilationStarted(string[] assemblyFolders, string[] ignoreAssemblies)
        {
            var glued = new string[2];
            glued[0] = SafeStringArrayHelper.SerialiseStringArraySafe(assemblyFolders);
            glued[1] = SafeStringArrayHelper.SerialiseStringArraySafe(ignoreAssemblies);
            var optionsSet = SafeStringArrayHelper.SerialiseStringArraySafe(glued);
            SendCommandToCompiler(BurstCompilerOptions.CompilerCommandNotifyCompilationStarted, optionsSet);
        }

        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal static void NotifyAssemblyCompilationNotRequired(string assemblyName)
        {
            SendCommandToCompiler(BurstCompilerOptions.CompilerCommandNotifyAssemblyCompilationNotRequired, assemblyName);
        }

        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal static void NotifyAssemblyCompilationFinished(string assemblyName, string[] defines)
        {
            BeginCompilerCommand(BurstCompilerOptions.CompilerCommandNotifyAssemblyCompilationFinished)
                .With(assemblyName).And()
                .With(string.Join(";", defines))
                .SendToCompiler();
        }


        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal static void NotifyCompilationFinished()
        {
            SendCommandToCompiler(BurstCompilerOptions.CompilerCommandNotifyCompilationFinished);
        }

        internal static string AotCompilation(string[] assemblyFolders, string[] assemblyRoots, string options)
        {
            var result = "failed";
            result = SendCommandToCompiler(
                BurstCompilerOptions.CompilerCommandAotCompilation,
                BurstCompilerOptions.SerialiseCompilationOptionsSafe(assemblyRoots, assemblyFolders, options));
            return result;
        }

        [NoAutoStaticsCleanup] // recomputed by AllocateDelegateHandles(), called by BurstLoader.InitBurstLoader on every domain reload before use
        private static string ProgressCallbackFunctionPointer;

        private delegate void ProgressCallbackDelegate(int current, int total);

        private static void ProgressCallback(int current, int total)
        {
            OnProgress?.Invoke(current, total);
        }

        [VisibleToOtherModules("UnityEditor.BurstModule")]
        [AutoStaticsCleanupOnCodeReload] // subscribers (BurstLoader) re-register on every reload; must not accumulate across reloads
        internal static event Action<int, int> OnProgress;

        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal static void SetProfilerCallbacks()
        {
            Debug.Assert(ProfileBeginCallbackFunctionPointer != null);
            Debug.Assert(ProfileEndCallbackFunctionPointer != null);

            BeginCompilerCommand(BurstCompilerOptions.CompilerCommandSetProfileCallbacks)
                .With(ProfileBeginCallbackFunctionPointer).And(';')
                .With(ProfileEndCallbackFunctionPointer)
                .SendToCompiler();
        }

        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal static void SetBurstAbortCallback()
        {
            Debug.Assert(BurstAbortFunctionPointer != null);
            BeginCompilerCommand(BurstCompilerOptions.CompilerCommandSetBurstAbortCallback)
                .With(BurstAbortFunctionPointer)
                .SendToCompiler();
        }

        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal delegate void ProfileBeginCallbackDelegate(string markerName, string metadataName, string metadataValue);
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal delegate void ProfileEndCallbackDelegate(string markerName);
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal delegate void BurstAbortDelegate(string exceptionKind, string message, string stackTrace);

        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal delegate void BeginProgressDelegate(string message);
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal delegate void EndProgressDelegate();

        [NoAutoStaticsCleanup] // recomputed by AllocateDelegateHandles(), called by BurstLoader.InitBurstLoader on every domain reload before use
        private static string ProfileBeginCallbackFunctionPointer;
        [NoAutoStaticsCleanup] // see ProfileBeginCallbackFunctionPointer above
        private static string ProfileEndCallbackFunctionPointer;
        [NoAutoStaticsCleanup] // see ProfileBeginCallbackFunctionPointer above
        private static string BurstAbortFunctionPointer;

        [VisibleToOtherModules("UnityEditor.BurstModule")]
        private static void ProfileBeginCallback(string markerName, string metadataName, string metadataValue) => OnProfileBegin?.Invoke(markerName, metadataName, metadataValue);
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        private static void ProfileEndCallback(string markerName) => OnProfileEnd?.Invoke(markerName);
        private static void BurstAbort(string exceptionKind, string message, string stackTrace) => OnBurstAbort?.Invoke(exceptionKind, message, stackTrace);

        [VisibleToOtherModules("UnityEditor.BurstModule")]
        [AutoStaticsCleanupOnCodeReload] // subscribers (BurstLoader) re-register on every reload; must not accumulate across reloads
        internal static event ProfileBeginCallbackDelegate OnProfileBegin;
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        [AutoStaticsCleanupOnCodeReload] // see OnProfileBegin above
        internal static event ProfileEndCallbackDelegate OnProfileEnd;
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        [AutoStaticsCleanupOnCodeReload] // see OnProfileBegin above
        internal static event BurstAbortDelegate OnBurstAbort;
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        [AutoStaticsCleanupOnCodeReload] // see OnProfileBegin above
        internal static event BeginProgressDelegate OnBeginProgressBar;
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        [AutoStaticsCleanupOnCodeReload] // see OnProfileBegin above
        internal static event EndProgressDelegate OnEndProgressBar;

        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal static BurstCompileTarget[] GetInspectorEntryPoints()
        {
            var resultString = SendCommandToCompiler(BurstCompilerOptions.CompilerCommandGetInspectorEntryPoints);

            var unglued = SafeStringArrayHelper.DeserialiseStringArraySafe(resultString);

            var result = new BurstCompileTarget[unglued.Length];

            for (var i = 0; i < unglued.Length; i++)
            {
                var inner = SafeStringArrayHelper.DeserialiseStringArraySafe(unglued[i]);

                result[i] = new BurstCompileTarget(
                    bool.Parse(inner[0]),
                    inner[1],
                    inner[2],
                    inner[3],
                    inner[4],
                    inner[5],
                    inner[6]);
            }

            return result;
        }

        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal static string GetInspectorDisassembly(string methodName, string options)
        {
            var gluedOptions = SafeStringArrayHelper.SerialiseStringArraySafe(new[]
            {
                methodName,
                options
            });

            return SendCommandToCompiler(BurstCompilerOptions.CompilerCommandGetInspectorDisassembly, gluedOptions);
        }

        private static string SendRawCommandToCompiler(string command)
        {
            var results = Unity.Burst.LowLevel.BurstCompilerService.GetDisassembly(DummyMethodInfo, command);
            if (!string.IsNullOrEmpty(results))
                return results.TrimStart('\n');
            return "";
        }

        private static string SendCommandToCompiler(string commandName, string commandArgs = null)
        {
            if (commandName == null) throw new ArgumentNullException(nameof(commandName));

            if (commandArgs == null)
            {
                // If there are no arguments then there's no reason to go through the builder
                return SendRawCommandToCompiler(commandName);
            }

            // Otherwise use the builder for building the final command
            return BeginCompilerCommand(commandName)
                .With(commandArgs)
                .SendToCompiler();
        }

        // Reflection metadata pins the assembly/type it was resolved from, so it must be re-resolved
        // against the freshly-reloaded type on every code reload.
        [AutoStaticsCleanupOnCodeReload]
        private static MethodInfo DummyMethodInfo = typeof(BurstCompiler).GetMethod(nameof(DummyMethod), BindingFlags.Static | BindingFlags.NonPublic);

        /// <summary>
        /// Dummy empty method for being able to send a command to the compiler
        /// </summary>
        private static void DummyMethod() { }


        /// <summary>
        /// Fake delegate class to make BurstCompilerService.CompileAsyncDelegateMethod happy
        /// so that it can access the underlying static method via the property get_Method.
        /// So this class is not a delegate.
        /// </summary>
        private class FakeDelegate
        {
            public FakeDelegate(MethodInfo method)
            {
                Method = method;
            }

            [Unity.Scripting.RequiredByAssembly]
            public MethodInfo Method { get; }
        }
    }
}
