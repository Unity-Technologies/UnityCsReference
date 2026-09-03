using System;
using Unity.Scripting.LifecycleManagement;
using Unity.Scripting.LifecycleManagement.CodeGen;

namespace UnityEngine
{
    /// <summary>
    /// Lightweight cleanup wrapper that uses a delegate instead of requiring a unique subclass per user type.
    /// Used by the source generator to avoid creating thousands of nested types.
    /// </summary>
    /// <remarks>
    /// This type lives in Unity.Scripting (not an engine module) because the AutoStaticsCleanup source
    /// generator emits references to it into arbitrary user assemblies, so it must sit at the bottom of
    /// the assembly dependency graph. <see cref="CreateForPlayMode(Action, string)"/> needs
    /// typeof(<see cref="PlayModeScope"/>), which forces PlayModeScope to live in this assembly as well —
    /// Unity.Scripting cannot reference engine modules. It originally shipped in UnityEngine.CoreModule,
    /// so CoreModule keeps a type forwarder (see Runtime/Export/Scripting/ModuleTypeForwarders.cs).
    /// </remarks>
    public sealed class DelegateAutoCleanup : ClassAutoCleanup
    {
        private readonly Action _cleanup;
        private readonly string _ownerDescription;

        public override void Cleanup() => _cleanup();

        /// <summary>
        /// Returns a description of the owning class for diagnostic purposes.
        /// </summary>
        public override string ToString() => _ownerDescription;

        public DelegateAutoCleanup(Action cleanup, Type scopeType, string ownerDescription = "") : base(scopeType)
        {
            _cleanup = cleanup ?? throw new ArgumentNullException(nameof(cleanup));
            _ownerDescription = ownerDescription;
        }

        internal DelegateAutoCleanup(Action cleanup, Type scopeType, ScopeTransitionType cleanOn, string ownerDescription = "") : base(scopeType, cleanOn)
        {
            _cleanup = cleanup ?? throw new ArgumentNullException(nameof(cleanup));
            _ownerDescription = ownerDescription;
        }

        /// <summary>
        /// Creates a DelegateAutoCleanup registered against the PlayModeScope with <see cref="ScopeTransitionType.Both"/>.
        /// Avoids exposing the internal PlayModeScope type in generated user code.
        /// </summary>
        public static DelegateAutoCleanup CreateForPlayMode(Action cleanup, string ownerDescription = "")
        {
            return new DelegateAutoCleanup(cleanup, typeof(PlayModeScope), ownerDescription);
        }

        /// <summary>
        /// Creates a DelegateAutoCleanup registered against the PlayModeScope with a specific transition type.
        /// Avoids exposing the internal PlayModeScope type in generated user code.
        /// </summary>
        internal static DelegateAutoCleanup CreateForPlayMode(Action cleanup, ScopeTransitionType cleanOn, string ownerDescription = "")
        {
            return new DelegateAutoCleanup(cleanup, typeof(PlayModeScope), cleanOn, ownerDescription);
        }
    }
}
