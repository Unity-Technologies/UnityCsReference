// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.Scripting.LifecycleManagement
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class OnEnteringEditModeAttribute : LifecycleAttributeBase
    {
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class OnExitingEditModeAttribute : LifecycleAttributeBase
    {
    }

    internal sealed class EditModeScope : LifecycleScope
    {
        public static readonly string ScopeName = "EditMode";

        public EditModeScope() : base(ScopeName)
        {
            ExplicitRequiredOuterScopes.Add(CodeInitializedScope.ScopeName);
        }

        protected override void Enter(ScopeTransitionHelper scopeTransitionHelper)
        {
            if (LifecycleController.Instance.IsScopePresent<UnityEngine.PlayModeScope>())
            {
                DebugLifecycle.ReportError("Lifecycle ERROR : Cannot enter EditModeScope while PlayModeScope is active.", false);
                return;
            }

            DebugLifecycle.Log($"Lifecycle : Entering {ScopeName} scope");
            scopeTransitionHelper.ExecuteMethodsInOrder<OnEnteringEditModeAttribute>();
        }

        protected override void Exit(ScopeTransitionHelper scopeTransitionHelper)
        {
            DebugLifecycle.Log($"Lifecycle : Exiting {ScopeName} scope");
            scopeTransitionHelper.ExecuteMethodsInReverseOrder<OnExitingEditModeAttribute>();
        }
    }
}
