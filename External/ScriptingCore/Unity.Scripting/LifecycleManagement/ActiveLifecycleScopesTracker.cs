using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.Scripting.AssemblyManagement;
using Unity.Scripting.LifecycleManagement.CodeGen;

namespace Unity.Scripting.LifecycleManagement
{
    [System.Diagnostics.DebuggerDisplay("Active Scope Name = {Scope.Name}")]
    internal abstract class ActiveLifecycleScopeBase
    {
        protected ActiveLifecycleScopeBase(LifecycleScopeBase scope)
        {
            Scope = scope;
        }

        public LifecycleScopeBase Scope { get; private set; }

        public abstract LifecycleScopeKey ScopeKey { get; }

        public abstract void OnEnter(ScopeTransitionHelper helper);

        public abstract void OnExit(ScopeTransitionHelper helper);
    }

    internal class ActiveLifecycleScope : ActiveLifecycleScopeBase
    {
        public ActiveLifecycleScope(LifecycleScope scope) : base(scope)
        {
            ScopeKey = LifecycleScopeKey.CreateFromScope(scope);
        }

        public override LifecycleScopeKey ScopeKey { get; }

        public override void OnEnter(ScopeTransitionHelper helper)
        {
            var lifecycleScope = (LifecycleScope)Scope;
            lifecycleScope.OnEnter(helper);
        }

        public override void OnExit(ScopeTransitionHelper helper)
        {
            var lifecycleScope = (LifecycleScope)Scope;
            lifecycleScope.OnExit(helper);
        }
    }

    internal class ActiveLifecycleScopeWithContext<T> : ActiveLifecycleScopeBase where T : class
    {
        public ActiveLifecycleScopeWithContext(LifecycleScopeWithContext<T> scope) : base(scope)
        {
            ScopeKey = LifecycleScopeKey.CreateFromScope(scope);
        }

        public override LifecycleScopeKey ScopeKey { get; }

        public T Context => ((LifecycleScopeWithContext<T>)Scope).Context;

        public override void OnEnter(ScopeTransitionHelper helper)
        {
            var lifecycleScope = (LifecycleScopeWithContext<T>)Scope;
            lifecycleScope.OnEnter(helper);
        }

        public override void OnExit(ScopeTransitionHelper helper)
        {
            var lifecycleScope = (LifecycleScopeWithContext<T>)Scope;
            lifecycleScope.OnExit(helper);
        }
    }

    internal class ActiveLifecycleScopesTracker
    {
        private enum ScopeTransitionType
        {
            EnterScope,
            ExitScope,
        }

        [System.Diagnostics.DebuggerDisplay("Scope Transition Request {Scope.Name}")]
        private abstract class ScopeTransitionRequestBase
        {
            public ScopeTransitionRequestBase(LifecycleScopeBase scope, ScopeTransitionType scopeTransitionType, bool alsoExitNestedScopes)
            {
                Scope = scope;
                TransitionType = scopeTransitionType;
                AlsoExitNestedScopes = alsoExitNestedScopes;
            }

            public LifecycleScopeBase Scope { get; private set; }
            public ScopeTransitionType TransitionType { get; private set; }
            public bool AlsoExitNestedScopes { get; private set; }

            public abstract void Transition(ActiveLifecycleScopesTracker tracker);
        }

        private class ScopeTransitionRequest : ScopeTransitionRequestBase
        {
            public ScopeTransitionRequest(LifecycleScope scope, ScopeTransitionType scopeTransitionType, bool alsoExitNestedScopes)
                : base(scope, scopeTransitionType, alsoExitNestedScopes) { }

            public override void Transition(ActiveLifecycleScopesTracker tracker)
            {
                var scope = (LifecycleScope)Scope;

                if (TransitionType == ScopeTransitionType.EnterScope)
                {
                    tracker.TryEnterScope(scope);  // during this, new ScopeTransitionRequests may be queued up
                }
                else if (TransitionType == ScopeTransitionType.ExitScope)
                {
                    tracker.TryExitScope(scope, AlsoExitNestedScopes);  // during this, new ScopeTransitionRequests may be queued up
                }
            }
        }

        private class ScopeTransitionRequestWithContext<T> : ScopeTransitionRequestBase where T : class
        {
            public ScopeTransitionRequestWithContext(LifecycleScopeWithContext<T> scope, ScopeTransitionType scopeTransitionType, bool alsoExitNestedScopes)
                : base(scope, scopeTransitionType, alsoExitNestedScopes)
            {
            }

            public T Context => ((LifecycleScopeWithContext<T>)Scope).Context;

            public override void Transition(ActiveLifecycleScopesTracker tracker)
            {
                var scope = (LifecycleScopeWithContext<T>)Scope;

                if (TransitionType == ScopeTransitionType.EnterScope)
                {
                    tracker.TryEnterScope(scope);  // during this, new ScopeTransitionRequests may be queued up
                }
                else if (TransitionType == ScopeTransitionType.ExitScope)
                {
                    tracker.TryExitScope(scope, Context, AlsoExitNestedScopes);  // during this, new ScopeTransitionRequests may be queued up
                }
            }
        }

        private readonly Dictionary<LifecycleScopeKey, ActiveLifecycleScopeBase> _activeScopes = new();
        private readonly ScopeTransitionHelper _scopeTransitionHelper;
        private readonly Queue<ScopeTransitionRequestBase> _transitionRequestQueue = new();
        private readonly Dictionary<(Type, ScopeTransitionType), List<ClassAutoCleanup>> _autoCleanups = new();
        private readonly object _autoCleanupsLock = new();

        internal IReadOnlyDictionary<LifecycleScopeKey, ActiveLifecycleScopeBase> ActiveScopes => _activeScopes; // for test debugging only

        public ActiveLifecycleScopesTracker(ScopeTransitionHelper scopeTransitionHelper)
        {
            _scopeTransitionHelper = scopeTransitionHelper;
        }

        public void RequestEnterScope(LifecycleScope lifecycleScope)
        {
            if (lifecycleScope is ImplicitLifecycleScope)
            {
                throw new InvalidOperationException("ImplicitLifecycleScope cannot be entered explicitly.");
            }
            foreach (var impliciteOuterScope in lifecycleScope.ImplicitOuterScopes)
            {
                CreateScopeTransitionRequest(impliciteOuterScope, ScopeTransitionType.EnterScope, false /* unused, for ExitScope transitions only*/);
            }
            CreateScopeTransitionRequest(lifecycleScope, ScopeTransitionType.EnterScope, false /* unused, for ExitScope transitions only*/);
        }

        public void RequestEnterScope<T>(LifecycleScopeWithContext<T> lifecycleScope) where T : class
        {
            foreach (var impliciteOuterScope in lifecycleScope.ImplicitOuterScopes)
            {
                CreateScopeTransitionRequest(impliciteOuterScope, ScopeTransitionType.EnterScope, false /* unused, for ExitScope transitions only*/);
            }
            CreateScopeTransitionRequest(lifecycleScope, ScopeTransitionType.EnterScope, false /* unused, for ExitScope transitions only*/);
        }

        public void RequestExitScope(LifecycleScope lifecycleScope, bool alsoExitNestedScopes = true)
        {
            if (lifecycleScope is ImplicitLifecycleScope)
            {
                throw new InvalidOperationException("ImplicitLifecycleScope cannot be entered explicitly.");
            }
            CreateScopeTransitionRequest(lifecycleScope, ScopeTransitionType.ExitScope, alsoExitNestedScopes);
            var implicitScopes = lifecycleScope.ImplicitOuterScopes;
            for (int i = implicitScopes.Length - 1; i >= 0; i--)
            {
                CreateScopeTransitionRequest(implicitScopes[i], ScopeTransitionType.ExitScope, false);
            }
        }

        public void RequestExitScope<T>(LifecycleScopeWithContext<T> lifecycleScope, bool alsoExitNestedScopes = true) where T : class
        {
            CreateScopeTransitionRequest(lifecycleScope, ScopeTransitionType.ExitScope, alsoExitNestedScopes);
            var implicitScopes = lifecycleScope.ImplicitOuterScopes;
            for (int i = implicitScopes.Length - 1; i >= 0; i--)
            {
                CreateScopeTransitionRequest(implicitScopes[i], ScopeTransitionType.ExitScope, false);
            }
        }

        // used by tests
        internal bool HasAnyPresentScopes() => _activeScopes.Count > 0;

        [Obsolete("This overload will be deprecated once this becomes Net8, use the strongly typed overloads instead")]
        public bool IsInsideScope(string scopeName)
        {
            foreach (ActiveLifecycleScopeBase activeScope in _activeScopes.Values)
            {
                if (activeScope.Scope.Name == scopeName)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsInsideScope<TScope>()
            where TScope : LifecycleScopeBase
        {
            if (typeof(LifecycleScope).IsAssignableFrom(typeof(TScope)))
            {
                var scopeKey = new LifecycleScopeKey(typeof(TScope));
                return _activeScopes.ContainsKey(scopeKey);
            }

            // Checking for scope with context
            foreach (var scopeKey in _activeScopes.Keys)
            {
                if (scopeKey.Type == typeof(TScope))
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsInsideScope(LifecycleScope scope)
        {
            var scopeKey = new LifecycleScopeKey(scope.GetType());
            return _activeScopes.ContainsKey(scopeKey);
        }

        internal bool IsInsideScopeWithActivationContext<TScope, TContext>(TContext scopeContext)
            where TScope : LifecycleScopeWithContext<TContext>
            where TContext : class
        {
            var scopeKey = new LifecycleScopeKey(typeof(TScope), scopeContext);
            return _activeScopes.ContainsKey(scopeKey);
        }

        public bool IsInsideScopeWithActivationContext<TContext>(LifecycleScopeWithContext<TContext> scopeWithContext)
            where TContext : class
        {
            var scopeKey = new LifecycleScopeKey(scopeWithContext.GetType(), scopeWithContext.Context);
            return _activeScopes.ContainsKey(scopeKey);
        }

        internal bool IsOrWillBeInsideScope(LifecycleScope scope)
        {
            bool isInsideScope = IsInsideScope(scope);
            foreach (var transitionRequest in _transitionRequestQueue)
            {
                if (transitionRequest.Scope == scope
                    || transitionRequest.Scope.GetType() == scope.GetType())
                {
                    isInsideScope = transitionRequest.TransitionType switch
                    {
                        ScopeTransitionType.EnterScope => true,
                        ScopeTransitionType.ExitScope => false,
                        _ => isInsideScope
                    };
                }
            }
            return isInsideScope;
        }

        public bool IsOrWillBeInsideScopeWithActivationContext<TScope, TContext>(TContext activationContext)
            where TScope : LifecycleScopeWithContext<TContext>
            where TContext : class
        {
            bool isInsideScope = IsInsideScopeWithActivationContext<TScope, TContext>(activationContext);
            foreach (var transitionRequest in _transitionRequestQueue)
            {
                if (transitionRequest.Scope is TScope transitionScope
                    && transitionScope.Context == activationContext)
                {
                    isInsideScope = transitionRequest.TransitionType switch
                    {
                        ScopeTransitionType.EnterScope => true,
                        ScopeTransitionType.ExitScope => false,
                        _ => isInsideScope
                    };
                }
            }
            return isInsideScope;
        }

        public bool TryGetActiveScope<TScope>(out TScope scope)
            where TScope : LifecycleScope
        {
            var scopeKey = new LifecycleScopeKey(typeof(TScope));
            if (_activeScopes.TryGetValue(scopeKey, out var activeScope)
                && activeScope.Scope is TScope foundScope)
            {
                scope = foundScope;
                return true;
            }

            scope = null!;
            return false;
        }

        public bool TryGetActiveScope<TScope, TContext>(TContext context, out TScope scope)
            where TScope : LifecycleScopeWithContext<TContext>
            where TContext : class
        {
            var scopeKey = new LifecycleScopeKey(typeof(TScope), context);
            if (_activeScopes.TryGetValue(scopeKey, out var activeScope)
                && activeScope.Scope is TScope foundScope)
            {
                scope = foundScope;
                return true;
            }

            scope = null!;
            return false;
        }

        private bool PrepareTryEnterScope<TScope>(TScope lifecycleScope)
            where TScope : LifecycleScope
        {
            var requiredOuterScopes = lifecycleScope.RequiredOuterScopes;
            bool isInsideAllRequired = true;
            foreach (var requiredOuterScope in requiredOuterScopes)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                if (!IsInsideScope(requiredOuterScope))
#pragma warning restore CS0618 // Type or member is obsolete
                {
                    isInsideAllRequired = false;
                    DebugLifecycle.ReportError($"Lifecycle ERROR : could not enter scope '{lifecycleScope.Name}' due to required outer scope '{requiredOuterScope}' is not active.");
                    return false;
                }
            }

            if (!isInsideAllRequired)
            {
                DebugLifecycle.ReportError($"Lifecycle ERROR : could not enter scope '{lifecycleScope.Name}' as not all required outer scopes are active.");
                return false;
            }

            if (IsInsideScope(lifecycleScope))
            {
                DebugLifecycle.ReportError($"Lifecycle ERROR : could not enter scope '{lifecycleScope.Name}' as it is already active.");
                return false;
            }

            return true;
        }

        private bool PrepareTryEnterScopeWithContext<TContext>(LifecycleScopeWithContext<TContext> lifecycleScope)
            where TContext : class
        {
            var requiredOuterScopes = lifecycleScope.RequiredOuterScopes;
            bool isInsideAllRequired = true;
            foreach (var requiredOuterScope in requiredOuterScopes)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                if (!IsInsideScope(requiredOuterScope))
#pragma warning restore CS0618 // Type or member is obsolete
                {
                    isInsideAllRequired = false;
                    DebugLifecycle.ReportError($"Lifecycle ERROR : could not enter scope '{lifecycleScope.Name}' due to required outer scope '{requiredOuterScope}' is not active.");
                    return false;
                }
            }

            if (!isInsideAllRequired)
            {
                DebugLifecycle.ReportError($"Lifecycle ERROR : could not enter scope '{lifecycleScope.Name}' as not all required outer scopes are active.");
                return false;
            }

            if (IsInsideScopeWithActivationContext<TContext>(lifecycleScope))
            {
                DebugLifecycle.ReportError($"Lifecycle ERROR : could not enter scope '{lifecycleScope.Name}' as it is already active with the given activation context.");
                return false;
            }

            return true;
        }

        private void RaiseAutoCleanups(Type lifecycleScopeType, ScopeTransitionType scopeTransitionType)
        {
            lock (_autoCleanupsLock)
            {
                if (_autoCleanups.TryGetValue((lifecycleScopeType, scopeTransitionType), out var autoCleanups))
                {
                    foreach (var autoCleanup in autoCleanups)
                    {
                        try
                        {
                            autoCleanup.Cleanup();
                        }
                        catch (Exception ex)
                        {
                            DebugLifecycle.ReportError($"AutoStaticsCleanup ERROR: while {scopeTransitionType} {lifecycleScopeType}, cleanup {autoCleanup} (type: {autoCleanup.GetType()}) failed with exception {ex}");
                        }
                    }
                }
            }
        }

        private void TryEnterScope(LifecycleScope lifecycleScope)
        {
            if (!PrepareTryEnterScope(lifecycleScope))
                return;

            var activeScope = new ActiveLifecycleScope(lifecycleScope);
            _activeScopes.Add(LifecycleScopeKey.CreateFromScope(lifecycleScope), activeScope);
            RaiseAutoCleanups(lifecycleScope.GetType(), ScopeTransitionType.EnterScope);
            activeScope.OnEnter(_scopeTransitionHelper);
        }

        private void TryEnterScope<TContext>(LifecycleScopeWithContext<TContext> lifecycleScope)
            where TContext : class
        {
            if (!PrepareTryEnterScopeWithContext<TContext>(lifecycleScope))
                return;

            var activeScope = new ActiveLifecycleScopeWithContext<TContext>(lifecycleScope);
            _activeScopes.Add(LifecycleScopeKey.CreateFromScope(lifecycleScope), activeScope);
            RaiseAutoCleanups(lifecycleScope.GetType(), ScopeTransitionType.EnterScope);
            activeScope.OnEnter(_scopeTransitionHelper);
        }

        private void CollectNestedScopesToExit(string scopeName, List<ActiveLifecycleScopeBase> nestedActiveScopesInOrder)
        {
            foreach (var activeScope in _activeScopes.Values)
            {
                if (activeScope.Scope.MustBeNestedInsideScope(scopeName))
                {
                    var activeScopeName = activeScope.Scope.Name;
                    CollectNestedScopesToExit(activeScopeName, nestedActiveScopesInOrder);
                    nestedActiveScopesInOrder.Add(activeScope);
                }
            }
        }

        private bool PrepareTryExitScope(LifecycleScope lifecycleScope, bool alsoExitNestedScopes)
        {
            if (!IsInsideScope(lifecycleScope))
            {
                DebugLifecycle.ReportError($"Lifecycle ERROR : could not exit scope '{lifecycleScope.Name}' as it is not active currently.");
                return false;
            }

            var nestedActiveScopesInOrder = new List<ActiveLifecycleScopeBase>();

            int numActiveInstancesOfOuterScope = 0;
            foreach (var activeScope in _activeScopes.Values)
            {
                if (activeScope.Scope.Name == lifecycleScope.Name)
                {
                    ++numActiveInstancesOfOuterScope;
                }
            }
            // We only exit nested scopes if we are exiting the last remaining active instance of the outer scope
            if (numActiveInstancesOfOuterScope == 1)
            {
                CollectNestedScopesToExit(lifecycleScope.Name, nestedActiveScopesInOrder);
            }

            if (nestedActiveScopesInOrder.Count > 0)
            {
                if (!alsoExitNestedScopes)
                {
                    foreach (var nestedScope in nestedActiveScopesInOrder)
                    {
                        DebugLifecycle.ReportError($"Lifecycle ERROR : Cannot exit scope '{lifecycleScope.Name}' due to active nested scope '{nestedScope.Scope.Name}'", false);
                    }
                    DebugLifecycle.ReportError($"Lifecycle ERROR : scope '{lifecycleScope.Name}' cannot be deactivate due to active nested scopes.");
                    return false;
                }

                foreach (var nestedScope in nestedActiveScopesInOrder)
                {
                    DebugLifecycle.Log($"Lifecycle : Exiting nested scope '{nestedScope.Scope.Name}' due to exiting scope '{lifecycleScope.Name}'");
                    nestedScope.OnExit(_scopeTransitionHelper);
                    RaiseAutoCleanups(nestedScope.Scope.GetType(), ScopeTransitionType.ExitScope);
                    _activeScopes.Remove(nestedScope.ScopeKey);
                }
            }

            return true;
        }

        private bool PrepareTryExitScope<T>(LifecycleScopeWithContext<T> lifecycleScope, T activationContext, bool alsoExitNestedScopes) where T : class
        {
            if (!IsInsideScopeWithActivationContext(lifecycleScope))
            {
                DebugLifecycle.ReportError($"Lifecycle ERROR : could not exit scope '{lifecycleScope.Name}' as it is not active currently with the provided activation context.");
                return false;
            }

            var nestedActiveScopesInOrder = new List<ActiveLifecycleScopeBase>();
            int numActiveInstancesOfOuterScope = 0;
            foreach (var activeScope in _activeScopes.Values)
            {
                if (activeScope.Scope.Name == lifecycleScope.Name)
                {
                    ++numActiveInstancesOfOuterScope;
                }
            }
            // We only exit nested scopes if we are exiting the last remaining active instance of the outer scope
            if (numActiveInstancesOfOuterScope == 1)
            {
                CollectNestedScopesToExit(lifecycleScope.Name, nestedActiveScopesInOrder);
            }

            if (nestedActiveScopesInOrder.Count > 0)
            {
                if (!alsoExitNestedScopes)
                {
                    foreach (var nestedScope in nestedActiveScopesInOrder)
                    {
                        DebugLifecycle.ReportError($"Lifecycle ERROR : Cannot exit scope '{lifecycleScope.Name}' due to active nested scope '{nestedScope.Scope.Name}'", false);
                    }
                    DebugLifecycle.ReportError($"Lifecycle ERROR : scope '{lifecycleScope.Name}' cannot be deactivate due to active nested scopes.");
                    return false;
                }

                foreach (var nestedScope in nestedActiveScopesInOrder)
                {
                    DebugLifecycle.Log($"Lifecycle : Exiting nested scope '{nestedScope.Scope.Name}' due to exiting scope '{lifecycleScope.Name}'");
                    nestedScope.OnExit(_scopeTransitionHelper);
                    RaiseAutoCleanups(nestedScope.Scope.GetType(), ScopeTransitionType.ExitScope);
                    _activeScopes.Remove(nestedScope.ScopeKey);
                }
            }

            return true;
        }

        private void TryExitScope(LifecycleScope lifecycleScope, bool alsoExitNestedScopes)
        {
            if (!PrepareTryExitScope(lifecycleScope, alsoExitNestedScopes))
                return;

            foreach (var activeScope in _activeScopes.Values)
            {
                if (activeScope.Scope.Name == lifecycleScope.Name)
                {
                    activeScope.OnExit(_scopeTransitionHelper);
                    RaiseAutoCleanups(activeScope.Scope.GetType(), ScopeTransitionType.ExitScope);
                    _activeScopes.Remove(activeScope.ScopeKey);
                    break;
                }
            }
        }

        private void TryExitScope<T>(LifecycleScopeWithContext<T> lifecycleScope, T activationContext, bool alsoExitNestedScopes) where T : class
        {
            if (!PrepareTryExitScope(lifecycleScope, activationContext, alsoExitNestedScopes))
                return;

            foreach (var activeScope in _activeScopes.Values)
            {
                if (activeScope.Scope.Name == lifecycleScope.Name &&
                    activeScope is ActiveLifecycleScopeWithContext<T> activeScopeWithContext &&
                    activeScopeWithContext.Context == activationContext)
                {
                    activeScope.OnExit(_scopeTransitionHelper);
                    RaiseAutoCleanups(activeScope.Scope.GetType(), ScopeTransitionType.ExitScope);
                    _activeScopes.Remove(activeScope.ScopeKey);
                    break;
                }
            }
        }

        // This method may queue up the request if there's already ongoing scope transitions (the queue is not empty).
        // It will only execute the request (and all requests queued up as a result of it) if there's no ongoing scope transitions.
        // This means we expect the method to be called while it is already being executed lower in the stack (if a new scope transition is requested by hooks of another scope transitions being executed).
        // The use case for this is OnCodeInitializingAttribute hook requesting entering PlayModeScope for the Player.
        private void CreateScopeTransitionRequest(LifecycleScope lifecycleScope, ScopeTransitionType scopeTransitionType, bool alsoExitNestedScopes)
        {
            var newRequest = new ScopeTransitionRequest(lifecycleScope, scopeTransitionType, alsoExitNestedScopes);

            bool transitionRequestQueueWasEmpty = _transitionRequestQueue.Count == 0;
            if (!transitionRequestQueueWasEmpty && lifecycleScope.AllowNestedTransitions)
            {
                DebugLifecycle.ReportError($"Lifecycle ERROR : Cannot queue up scope transition request for '{newRequest.Scope.Name}' as it allow nested transitions and the queue is not empty.");
                return;
            }

            _transitionRequestQueue.Enqueue(newRequest);
            if (!transitionRequestQueueWasEmpty)
            {
                DebugLifecycle.Log($"Lifecycle : Scope Transition request has been queued up for '{newRequest.Scope.Name}'");
                // If there's older transition request in the queue, then we should process the new or other request right now, as they are being handled lower in the stack (or potentially in another thread).
                return;
            }


            // We don't have any ongoing scope transition, so we can begin process the request and all requests that may be queued up during the processing.
            // This can also be done in some kind of Tick() functionality that is called from outside.
            ExecuteTransitions(transitionRequestQueueWasEmpty && lifecycleScope.AllowNestedTransitions);
        }

        private void CreateScopeTransitionRequest<T>(LifecycleScopeWithContext<T> lifecycleScope, ScopeTransitionType scopeTransitionType, bool alsoExitNestedScopes) where T : class
        {
            var newRequest = new ScopeTransitionRequestWithContext<T>(lifecycleScope, scopeTransitionType, alsoExitNestedScopes);

            bool transitionRequestQueueWasEmpty = _transitionRequestQueue.Count == 0;
            if (!transitionRequestQueueWasEmpty && lifecycleScope.AllowNestedTransitions)
            {
                DebugLifecycle.ReportError($"Lifecycle ERROR : Cannot queue up scope transition request for '{newRequest.Scope.Name}' with activation context '{lifecycleScope.Context}' as it allows nested transitions and the queue is not empty.");
                return;
            }

            _transitionRequestQueue.Enqueue(newRequest);
            if (!transitionRequestQueueWasEmpty)
            {
                DebugLifecycle.Log($"Lifecycle : Scope Transition request has been queued up for '{newRequest.Scope.Name}'");
                // If there's older transition request in the queue, then we should process the new or other request right now, as they are being handled lower in the stack (or potentially in another thread).
                return;
            }

            // We don't have any ongoing scope transition, so we can begin process the request and all requests that may be queued up during the processing.
            // This can also be done in some kind of Tick() functionality that is called from outside.
            ExecuteTransitions(transitionRequestQueueWasEmpty && lifecycleScope.AllowNestedTransitions);
        }

        private void ExecuteTransitions(bool executeAndPop)
        {
            while (_transitionRequestQueue.Count > 0)
            {
                var request = _transitionRequestQueue.Peek();
                if (executeAndPop)
                {
                    _transitionRequestQueue.Dequeue();
                }

                try
                {
                    request.Transition(this);
                }
                finally
                {
                    if (!executeAndPop)
                    {
                        _transitionRequestQueue.Dequeue();
                    }
                }
            }
        }

        internal void RegisterAutoCleanup(ClassAutoCleanup classAutoCleanup, Type scopeType, LifecycleManagement.ScopeTransitionType cleanOn)
        {
            if (cleanOn == LifecycleManagement.ScopeTransitionType.Unset)
            {
                throw new ArgumentOutOfRangeException(nameof(cleanOn), "cleanOn must be set to either Entering, Exiting or Both");
            }

            if (cleanOn == LifecycleManagement.ScopeTransitionType.Both)
            {
                RegisterAutoCleanup(classAutoCleanup, scopeType, LifecycleManagement.ScopeTransitionType.Entering);
                RegisterAutoCleanup(classAutoCleanup, scopeType, LifecycleManagement.ScopeTransitionType.Exiting);
                return;
            }

            var key = (scopeType, cleanOn == LifecycleManagement.ScopeTransitionType.Entering ? ScopeTransitionType.EnterScope : ScopeTransitionType.ExitScope);
            lock (_autoCleanupsLock)
            {
                if (!_autoCleanups.TryGetValue(key, out var autoCleanups))
                {
                    autoCleanups = new List<ClassAutoCleanup>();
                    _autoCleanups[key] = autoCleanups;
                }
                autoCleanups.Add(classAutoCleanup);
            }
        }

        /// <summary>
        /// Drops ClassAutoCleanup instances which were registered from assemblies which are now unloading.
        /// </summary>
        /// <param name="unloadingAssemblies">HashSet of unloading assemblies</param>
        internal void ClearUnloadingAutoStaticsCleanupCallbacks(IReadonlyOrderedAssemblyList unloadingAssemblies)
        {
            lock (_autoCleanupsLock)
            {
                var unloadingKeys = new List<(Type, ScopeTransitionType)>();
                foreach (var key in _autoCleanups.Keys)
                {
                    if (IsTypeInUnloadingAssembly(key.Item1, unloadingAssemblies))
                    {
                        unloadingKeys.Add(key);
                        continue;
                    }

                    var autoCleanups = _autoCleanups[key];
                    autoCleanups.RemoveAll(cleanup => IsTypeInUnloadingAssembly(cleanup.GetType(), unloadingAssemblies));
                }

                foreach (var key in unloadingKeys)
                {
                    _autoCleanups.Remove(key);
                }
            }
        }

        static bool IsTypeInUnloadingAssembly(Type type, IReadonlyOrderedAssemblyList unloadingAssemblies)
        {
            if (unloadingAssemblies.Contains(type.Assembly.GetName()))
            {
                return true;
            }

            if (type.IsGenericType)
            {
                foreach (var typeArg in type.GetGenericArguments())
                {
                    if (IsTypeInUnloadingAssembly(typeArg, unloadingAssemblies))
                    {
                        return true;
                    }
                }
            }

            if (type.IsArray || type.IsPointer)
            {
                return IsTypeInUnloadingAssembly(type.GetElementType()!, unloadingAssemblies);
            }

            return false;
        }
    }
}

