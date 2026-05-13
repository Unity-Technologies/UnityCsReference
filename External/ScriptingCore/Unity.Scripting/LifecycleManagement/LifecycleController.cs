using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Unity.Scripting.LifecycleManagement.CodeGen;

namespace Unity.Scripting.LifecycleManagement
{
    internal enum LifecycleScopePresence
    {
        Absent,
        Present,
    }

    internal sealed class LifecycleController
    {
        private static LifecycleController? _instance;
        public static LifecycleController Instance
        {
            get
            {
                _instance ??= new();
                return _instance;
            }

            // used by Tests
            internal set => _instance = value;
        }

        private bool IsOnMainThread => MainThreadId == Thread.CurrentThread.ManagedThreadId;

        private readonly ScopeTransitionHelper _scopeTransitionHelper;
        internal ScopeTransitionHelper ScopeTransitionHelper => _scopeTransitionHelper;
        private readonly ActiveLifecycleScopesTracker _lifecycleTracker;
        private readonly LifecycleMethodRegistry _lifecycleMethodRegistry;

        private readonly object _lock = new();

        internal int MainThreadId { get; set; } // setter should only be used by Tests
        internal ActiveLifecycleScopesTracker LifecycleScopesTracker => _lifecycleTracker; // for test debugging

        internal LifecycleController()
        {
            MainThreadId = Thread.CurrentThread.ManagedThreadId;
            _lifecycleMethodRegistry = new LifecycleMethodRegistry();
            _scopeTransitionHelper = new ScopeTransitionHelper(_lifecycleMethodRegistry);
            _lifecycleTracker = new ActiveLifecycleScopesTracker(_scopeTransitionHelper);
        }

        public void OnAssembliesLoaded(ReadOnlyAssemblyList loadedAssemblies)
        {
            _scopeTransitionHelper.PushStack(loadedAssemblies);

            ExecuteInitializationMethods(loadedAssemblies);
        }

        private void ExecuteInitializationMethods(IReadOnlyList<Assembly> loadedAssemblies)
        {
            foreach (var assembly in loadedAssemblies)
            {
                if (TryGetInitializationMethod(assembly, out var initializationMethod))
                {
                    initializationMethod.Invoke(null, null);
                }
            }
        }

        private bool TryGetInitializationMethod(Assembly assembly, [NotNullWhen(true)] out MethodInfo? initializationMethod)
        {
            var unityModuleInitializationType = assembly.GetType("__UnityModuleInitialization");
            if (unityModuleInitializationType == null)
            {
                initializationMethod = null;
                return false;
            }

            initializationMethod = unityModuleInitializationType.GetMethod("Initialize", BindingFlags.Static | BindingFlags.NonPublic);
            return initializationMethod != null;
        }

        public bool HasInitializationMethod(Assembly assembly)
        {
            return TryGetInitializationMethod(assembly, out _);
        }

        public void OnAssemblyLoadedScopeExiting(ReadOnlyAssemblyList unloadingAssemblies)
        {
            _lifecycleTracker.ClearUnloadingAutoStaticsCleanupCallbacks(unloadingAssemblies);

            _scopeTransitionHelper.PopStack();
        }

        public void OnAssemblyLoadedScopeExited(ReadOnlyAssemblyList unloadingAssemblies)
        {
            _lifecycleMethodRegistry.Clear(unloadingAssemblies);
        }

        internal INativeCallbackProvider? SetDependency_NativeCallbackProvider(INativeCallbackProvider nativeCallbackProvider)
        {
            lock (_lock)
            {
                var currentNativeCallbackProvider = _scopeTransitionHelper.NativeCallbackProvider;
                _scopeTransitionHelper.NativeCallbackProvider = nativeCallbackProvider;
                return currentNativeCallbackProvider;
            }
        }

        internal IReadOnlyList<Assembly> GetAllAssembliesOrdered()
        {
            lock (_lock)
            {
                return _scopeTransitionHelper.AllAssemblies;
            }
        }

        internal static void InitializeForIl2Cpp(IScriptingCoreDebug depDebug)
        {
            Debug.ScriptingCoreDebug = depDebug;
            _instance = new LifecycleController();
        }

        [Obsolete("This overload will be deprecated once this becomes Net8, use the strongly typed overloads instead")]
        public bool IsScopePresent(string scopeName)
        {
            lock (_lock)
            {
                return _lifecycleTracker.IsInsideScope(scopeName);
            }
        }

        public bool IsScopePresent(LifecycleScope scope)
        {
            lock (_lock)
            {
                return _lifecycleTracker.IsInsideScope(scope);
            }
        }

        public bool IsScopePresent<TScope>()
            where TScope : LifecycleScopeBase
        {
            lock (_lock)
            {
                return _lifecycleTracker.IsInsideScope<TScope>();
            }
        }

        public bool IsScopePresentWithContext<TContext>(LifecycleScopeWithContext<TContext> scope)
            where TContext : class
        {
            lock (_lock)
            {
                return _lifecycleTracker.IsInsideScopeWithActivationContext(scope);
            }
        }

        public bool IsScopePresentWithContext<TScope, TContext>(TContext activationContext)
            where TScope : LifecycleScopeWithContext<TContext>
            where TContext : class
        {
            lock (_lock)
            {
                return _lifecycleTracker.IsInsideScopeWithActivationContext<TScope, TContext>(activationContext);
            }
        }

        // used by tests
        internal bool HasAnyPresentScopes()
        {
            lock (_lock)
            {
                return _lifecycleTracker.HasAnyPresentScopes();
            }
        }

        internal void ExpectPresentScope(string scopeName, LifecycleScopePresence expectation = LifecycleScopePresence.Present)
        {
            lock (_lock)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                bool insideScope = _lifecycleTracker.IsInsideScope(scopeName);
#pragma warning restore CS0618 // Type or member is obsolete
                switch (expectation)
                {
                    case LifecycleScopePresence.Present when !insideScope:
                        {
                            DebugLifecycle.ReportError($"Lifecycle ERROR : Expected to be inside lifecycle scope '{scopeName}' but we are not.");
                            return;
                        }
                    case LifecycleScopePresence.Present:
                        {
                            break;
                        }
                    case LifecycleScopePresence.Absent when insideScope:
                        {
                            DebugLifecycle.ReportError($"Lifecycle ERROR : Expected to not be inside lifecycle scope '{scopeName}' but we are.");
                            return;
                        }
                    case LifecycleScopePresence.Absent:
                        {
                            break;
                        }
                    default:
                        {
                            DebugLifecycle.ReportError($"Lifecycle ERROR : Unknown expectation '{expectation}' for lifecycle scope presence");
                            return;
                        }
                }
            }
        }

        private void ExecuteOnMainThread(string transitionType, string scopeName, Action action)
        {
            if (!IsOnMainThread)
            {
                DebugLifecycle.ReportError($"Lifecycle ERROR : {transitionType} scope {scopeName} can only be executed on the main thread\n" +
                    $"Calling thread was {Thread.CurrentThread.ManagedThreadId} while main thread is {MainThreadId}.");
                return;
            }

            lock (_lock)
            {
                action.Invoke();
            }
        }

        internal void EnterScope<TScope>()
            where TScope : LifecycleScope, new()
        {
            var scope = new TScope();
            ExecuteOnMainThread("Enter", scope.Name, () =>
            {
                _lifecycleTracker.RequestEnterScope(scope);
            });
        }

        // EnterScope() and ExitScope() functions have a void return value even though this scope transition request may fail.
        // This is because the processing the request may be delayed due to currently queued-up scope transitions being processed first
        // and the request may not succeed at the time it is being processed potentially later on. 
        // If there's need to follow the result of the request, the caller should use the IsScopePresent() function to check if the scope is present at a later point.
        // To improve on this we can return a small struct which contains info whether the request has been processed, whether it was succesful and the reason for failure in case it isn't
        internal void EnterScope(LifecycleScope scope)
        {
            ExecuteOnMainThread("Enter", scope.Name, () =>
            {
                _lifecycleTracker.RequestEnterScope(scope);
            });
        }

        internal void EnterScope<T>(LifecycleScopeWithContext<T> scope)
            where T : class
        {
            ExecuteOnMainThread("Enter", scope.Name, () =>
            {
                _lifecycleTracker.RequestEnterScope(scope);
            });
        }

        internal void ExitScope<TScope>()
            where TScope : LifecycleScope, new()
        {
            var scopeName = typeof(TScope).Name;
            ExecuteOnMainThread("Exit", scopeName, () =>
            {
                if (!_lifecycleTracker.TryGetActiveScope<TScope>(out var scope))
                {
                    DebugLifecycle.ReportError($"Lifecycle ERROR : Cannot exit scope of type '{scopeName}', no active scope of that type found");
                    return;
                }

                _lifecycleTracker.RequestExitScope(scope!);
                Debug.Assert(!_lifecycleTracker.IsOrWillBeInsideScope(scope));
            });
        }

        internal void ExitScope<TScope, TContext>(TContext context)
            where TContext : class
            where TScope : LifecycleScopeWithContext<TContext>
        {
            var scopeName = typeof(TScope).Name;
            ExecuteOnMainThread("Exit", scopeName, () =>
            {
                if (!_lifecycleTracker.TryGetActiveScope<TScope, TContext>(context, out var scope))
                {
                    DebugLifecycle.ReportError($"Lifecycle ERROR : Cannot exit scope of type '{scopeName}' for context '{context}', no active scope of that type and context found");
                    return;
                }

                _lifecycleTracker.RequestExitScope(scope);
                Debug.Assert(!_lifecycleTracker.IsOrWillBeInsideScopeWithActivationContext<TScope, TContext>(scope.Context));
            });
        }

        internal void ExitScope(LifecycleScope scope)
        {
            ExecuteOnMainThread("Exit", scope.Name, () =>
            {
                _lifecycleTracker.RequestExitScope(scope);
            });
        }

        internal void ExitScope<TContext>(LifecycleScopeWithContext<TContext> scope)
            where TContext : class
        {
            ExecuteOnMainThread("Exit", scope.Name, () =>
            {
                _lifecycleTracker.RequestExitScope(scope);
            });
        }

        internal void RegisterAutoCleanup(ClassAutoCleanup classAutoCleanup, Type scopeType, ScopeTransitionType cleanOn)
        {
            _lifecycleTracker.RegisterAutoCleanup(classAutoCleanup, scopeType, cleanOn);
        }

        internal void RegisterLifecycleMethod(Type lifecycleAttributeType, Assembly assembly, string methodFullName, Action callback)
        {
            _lifecycleMethodRegistry.Register(lifecycleAttributeType, assembly, methodFullName, callback);
        }
    }
}
