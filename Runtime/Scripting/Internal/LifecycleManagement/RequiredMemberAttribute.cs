// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Bindings;

namespace Unity.Scripting
{
    internal abstract class RequiredMemberAttribute : Attribute { }

    internal interface IScriptingCoreDebug
    {
        bool IsDiagnosticSwitchEnabled(string name);
        void Log(string message);
        void LogError(string message);
        void Assert(bool condition);
        void AssertMsg(bool condition, string message);
        bool RunAssemblyLoadContextLeakDetection(List<IntPtr> assemblyLoadContextWeakHandles);
    }
}

namespace Unity.Scripting.LifecycleManagement
{
    [VisibleToOtherModules]
    internal enum ScopeTransitionType { Unset, Entering, Exiting, Both }

    [VisibleToOtherModules]
    internal enum CleanupStrategy { Unset, Auto, Clear, CaptureInitializationExpression, ResetToDefaultValue }

    internal abstract class LifecycleAttributeBase : RequiredMemberAttribute { }

    [VisibleToOtherModules]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class AfterAssemblyLoadedAttribute : LifecycleAttributeBase
    {
        public AfterAssemblyLoadedAttribute() { }
    }

    [VisibleToOtherModules]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class BeforeAssemblyUnloadingAttribute : LifecycleAttributeBase
    {
        public BeforeAssemblyUnloadingAttribute() { }
    }

    [VisibleToOtherModules]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event, AllowMultiple = true)]
    internal sealed class AutoStaticsCleanupAttribute : Attribute
    {
        public Type ScopeType { get; set; }
        public ScopeTransitionType TransitionType { get; set; }
        public CleanupStrategy CleanupStrategy { get; set; }
    }

    [VisibleToOtherModules]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event, AllowMultiple = true)]
    internal sealed class AutoStaticsCleanupOnCodeReloadAttribute : Attribute
    {
        public CleanupStrategy CleanupStrategy { get; set; }
    }

    [VisibleToOtherModules]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event, AllowMultiple = false)]
    internal sealed class NoAutoStaticsCleanupAttribute : Attribute { }

    [VisibleToOtherModules]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event, AllowMultiple = true)]
    internal sealed class IgnoreForUAL0015Attribute : Attribute
    {
        public string Reason { get; }
        public IgnoreForUAL0015Attribute(string reason) => Reason = reason;
    }

    internal abstract class LifecycleScopeBase { }

    internal class AssemblyLoadedScopeBase : LifecycleScopeBase, IDisposable
    {
        public AssemblyLoadedScopeBase(IReadOnlyList<Assembly> assemblies) { }
        public void Dispose() { }
    }

    internal sealed class AssemblyLoadedScopeIl2Cpp : AssemblyLoadedScopeBase
    {
        public AssemblyLoadedScopeIl2Cpp(IReadOnlyList<Assembly> assemblies) : base(assemblies) { }
    }

    internal sealed class CodeLoadedScope : LifecycleScopeBase
    {
        public static void CancelIfNotInCorrectGeneration() { }
    }
    internal sealed class ManagedObjectsRestoredScope : LifecycleScopeBase { }
    internal sealed class ManagedObjectsAwokenScope : LifecycleScopeBase { }

    internal static class DebugLifecycle
    {
        public static void ReportError(string message, bool throwException) { }
    }

    internal class DependencyOrderedNativeCallbackProvider { }

    internal sealed class PerAssemblyMethodCatalog : Dictionary<string, List<MethodInfo>> { }

    internal interface IAttributeUsageLocator
    {
        PerAssemblyMethodCatalog FindStaticMethodsWithAttribute(Type attributeType);
        IEnumerable<MethodInfo> FindStaticMethodsWithAttribute(Type attributeType, Assembly assembly);
    }

    internal static class LifecycleController
    {
        internal static LifecycleControllerInstance Instance { get; private set; }
        internal static void InitializeForIl2Cpp(string attributeUsageLocatorTypeName, IScriptingCoreDebug debug) { Instance = new LifecycleControllerInstance(); }
    }

    internal class LifecycleControllerInstance
    {
        internal void EnterScope<T>() where T : LifecycleScopeBase { }
        internal void EnterScope(LifecycleScopeBase scope) { }
        internal void ExitScope<T>() where T : LifecycleScopeBase { }
        internal void ExitScope(LifecycleScopeBase scope) { }
        internal void SetDependency_NativeCallbackProvider(DependencyOrderedNativeCallbackProvider provider) { }
        internal void RegisterAutoCleanup(CodeGen.ClassAutoCleanup cleanup, Type scopeType, ScopeTransitionType cleanOn) { }
    }

    internal sealed class ScopedLazy<TValue, TScope> : CodeGen.ClassAutoCleanup
        where TValue : class
        where TScope : LifecycleScopeBase
    {
        public ScopedLazy(Func<TValue> factory, bool checkScopeActive = true) : base(typeof(TScope), ScopeTransitionType.Exiting) { }
        public ScopedLazy(bool checkScopeActive = true) : this(() => default, checkScopeActive) { }
        public TValue Value => default;
        public override void Cleanup() { }
    }
}

namespace Unity.Scripting.LifecycleManagement.CodeGen
{
    internal abstract class ClassAutoCleanup
    {
        public abstract void Cleanup();
        protected ClassAutoCleanup(Type scopeType, ScopeTransitionType cleanOn) { }
    }
}
