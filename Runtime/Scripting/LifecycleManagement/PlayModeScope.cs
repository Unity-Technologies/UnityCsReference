// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using Unity.Scripting.LifecycleManagement;
using Unity.Scripting.LifecycleManagement.CodeGen;
using UnityEngine.Internal;

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

    public abstract class PlayModeScopeAutoCleanup : ClassAutoCleanup
    {
        protected PlayModeScopeAutoCleanup() : base(typeof(PlayModeScope)) {}
    }
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
