using System;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class OnEnteringPlayModeAttribute : LifecycleAttributeBase
    {
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class OnExitingPlayModeAttribute : LifecycleAttributeBase
    {
    }

    // Lives in Unity.Scripting rather than an engine module because DelegateAutoCleanup.CreateForPlayMode
    // (emitted into arbitrary user assemblies by the AutoStaticsCleanup source generator) needs
    // typeof(PlayModeScope), and Unity.Scripting cannot reference engine modules. Engine code that
    // enters/exits the scope (e.g. Application.cs, EditModeScope.cs) gets access via InternalsVisibleTo.
    internal sealed class PlayModeScope : LifecycleScope
    {
        public static readonly string ScopeName = "PlayMode";

        public PlayModeScope() : base(ScopeName)
        {
            ExplicitRequiredOuterScopes.Add(CodeInitializedScope.ScopeName);
        }

        protected override void Enter(ScopeTransitionHelper scopeTransitionHelper)
        {
            scopeTransitionHelper.ExecuteMethodsInOrder<OnEnteringPlayModeAttribute>();
        }

        protected override void Exit(ScopeTransitionHelper scopeTransitionHelper)
        {
            scopeTransitionHelper.ExecuteMethodsInReverseOrder<OnExitingPlayModeAttribute>();
        }
    }
}
