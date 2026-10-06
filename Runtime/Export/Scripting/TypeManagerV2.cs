// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#nullable enable
using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/BaseClasses/TypeManager.h")]
    public partial class TypeManagerV2
    {
        // Registrations are grouped natively by load context — the code that dies together. On
        // CoreCLR the registrant is declared by wrapping the registration calls in a
        // BeginRegistration scope anchored on any type from the registering assembly (the source
        // generator passes its own per-assembly class); native asks the runtime which ALC owns
        // the anchor's assembly. Retirement is driven by the ALC's own death notification (fired
        // as its identity handle is freed, after every unload hook has run), so an assembly can
        // still resolve its own factories from [OnAssemblyUnloading]. The other backends have no
        // per-ALC unloads: everything shares one process-wide context, retired below on code
        // unload (a Mono domain reload; never in players).

        // The active scope's resolved context. Zero = no scope: such registrations are dropped
        // natively with a warning, because a registrant we cannot attribute to a load context
        // could leave a factory dangling past its code's unload. NoAutoStaticsCleanup: only
        // non-zero inside a BeginRegistration scope, so nothing outlives a reload to clean.
        [ThreadStatic]
        [NoAutoStaticsCleanup]
        static IntPtr s_ActiveRegistrationContext;

        // The context new registrations are attributed to: the active BeginRegistration scope's
        // resolved context on CoreCLR, the domain-generation context elsewhere.
        static IntPtr CurrentRegistrationContext
        {
            get
            {
                return s_ActiveRegistrationContext;
            }
        }

        // Anchors registrations made inside the scope to the load context of the assembly that
        // declares the anchor type. Scopes nest; each restores the previous context on dispose.
        public static RegistrationScope BeginRegistration(RuntimeTypeHandle anchorTypeHandle)
        {
            IntPtr previous = s_ActiveRegistrationContext;
            IntPtr resolved = ResolveRegistrantContext(anchorTypeHandle.Value);
            // 1 = scope present but the anchor's assembly has no live Unity load context (value
            // shared with TypeManager.h's kUnresolvedRegistrantContext): a foreign ALC, or one
            // already past its unloading notification. Distinct from zero (no scope) so the two
            // drop warnings stay tellable apart.
            s_ActiveRegistrationContext = resolved == IntPtr.Zero ? (IntPtr)1 : resolved;
            return new RegistrationScope(previous);
        }

        public readonly struct RegistrationScope : IDisposable
        {
            readonly IntPtr m_Previous;

            internal RegistrationScope(IntPtr previous)
            {
                m_Previous = previous;
            }

            public void Dispose()
            {
                s_ActiveRegistrationContext = m_Previous;
                // Fill the proxy-object-factory table downward for everything this scope registered.
                // Batched here rather than per registration so a scope registering n types fills the
                // table once instead of n times; a nested scope just flushes again, which is idempotent.
                FlushProxyObjectFactoryTable();
            }
        }

        // Native-object -> managed-wrapper resolution (UnityEngine.Object.EnsureScriptingWrapperFor).
        // These are read on the proxy-object resolve path, which reads native memory directly rather than
        // calling into C++. Set once by Initialize() (CoreInitializer, before any proxy object is resolved).
        // NoAutoStaticsCleanup: process-stable native addresses/offsets, re-fetched by Initialize()
        // each generation, so keeping the last values across a reload avoids a null-base window before
        // Initialize() runs again.

        // Base of the native proxy-object-factory table: delegate*<EntityId, object> per runtime type index.
        [NoAutoStaticsCleanup] static IntPtr s_ProxyObjectFactoryTableAddress;
        // Mirrors RTTI::RuntimeTypeArray::MAX_NATIVE_RUNTIME_TYPES; native fills the whole capacity.
        const int k_MaxNativeRuntimeTypes = 1024;

        // The built-in factories native stamps into the table itself when it rebuilds it (see
        // TypeManager::SetBuiltinProxyObjectFactories); they are not registrations and never retire.

        // A scripted object's wrapper (MonoBehaviour/ScriptableObject subclass, scripted importers) is
        // the MonoScript machinery's to create; one built here would be the wrong type.
        static object? RefuseScriptedProxyObject(EntityId id) => null;

        // No factory registered anywhere in the type's chain: a bare Object is the correct proxy object.
        static object CreateBareObjectProxy(EntityId id) => new UnityEngine.Object(id);

        // Fills every slot no registered runtime type owns; reaching it means a corrupted or stale
        // native object header.
        static object? ReportUnregisteredProxyObjectSlot(EntityId id)
        {
            Debug.LogError($"TypeManagerV2.CreateProxyObject: no runtime type is registered at the resolved type index (object {id}).");
            return null;
        }

        // Called by CoreInitializer before any proxy object is resolved. Fetches the one-time native
        // table address used by CreateProxyObject and hands native the built-in factories above; from
        // here on the table is total over registered runtime types in every rebuild.
        internal static unsafe void Initialize()
        {
            s_ProxyObjectFactoryTableAddress = GetProxyObjectFactoryTableAddress();
            SetBuiltinProxyObjectFactories(
                (IntPtr)(delegate*<EntityId, object>)&CreateBareObjectProxy,
                (IntPtr)(delegate*<EntityId, object?>)&RefuseScriptedProxyObject,
                (IntPtr)(delegate*<EntityId, object?>)&ReportUnregisteredProxyObjectSlot);
        }

        // typeName and hardcodedPersistentId are legacy arguments the source generator still emits;
        // identity is the type handle alone. typeName only feeds diagnostics.
        public static void RegisterFactory(RuntimeTypeHandle typeHandle, string typeName, IntPtr factoryPtr,
                                           int hardcodedPersistentId = 0)
        {
            if (factoryPtr == IntPtr.Zero)
                Debug.LogError($"TypeManagerV2.RegisterFactory: factoryPtr is null for type '{typeName}'");

            RegisterManagedFactory(typeHandle.Value, factoryPtr, CurrentRegistrationContext);
        }

        // Sized collection factory: keyed by the collection type itself (T[] or List<T>), the
        // thunk is `static object F(int n)` returning the collection's backing array new T[n].
        // The read executors call it in place of Type unmarshalling + Array.CreateInstance.
        public static void RegisterSizedFactory(RuntimeTypeHandle collectionTypeHandle, string typeName, IntPtr factoryPtr)
        {
            if (factoryPtr == IntPtr.Zero)
            {
                Debug.LogError($"TypeManagerV2.RegisterSizedFactory: factoryPtr is null for type '{typeName}'");
                return;
            }

            RegisterSizedFactory(collectionTypeHandle.Value, factoryPtr, CurrentRegistrationContext);
        }

        // Registers the proxy-object factory (delegate*<EntityId, object>) for a [NativeClass] proxy-object type,
        // keyed by its native runtime type index (resolved from hardcodedPersistentId). Kept separate
        // from RegisterFactory so the common per-type registration call carries no extra arguments -
        // only proxy-object types emit this second call. Retired with the active scope's context.
        public static void RegisterProxyObjectFactory(int hardcodedPersistentId, IntPtr proxyObjectFactoryPtr)
        {
            RegisterManagedProxyObjectFactory(hardcodedPersistentId, proxyObjectFactoryPtr, CurrentRegistrationContext);
        }

        // Metadata only, a value-type factory would box. Registered with a null factory so
        // diagnostics can distinguish "registered without a factory" from "never registered".
        public static void RegisterType(RuntimeTypeHandle typeHandle, string typeName,
                                        int hardcodedPersistentId = 0)
        {
            RegisterManagedFactory(typeHandle.Value, IntPtr.Zero, CurrentRegistrationContext);
            RegisterComponentTypeClass(typeHandle.Value, CurrentRegistrationContext);
        }


        // Builds the managed proxy object for an existing native object, from the factory in the table
        // at its runtime type index. The table is total over registered runtime types: native fills it
        // downward at registration-scope exit (TypeManager::RebuildProxyObjectFactoryTableLockTaken),
        // so a native-only type (registered via IMPLEMENT_REGISTER_CLASS with no managed [NativeClass]
        // proxy object, e.g. NativeFormatImporter) carries its nearest managed ancestor's factory
        // (e.g. AssetImporter), a scripted type carries RefuseScriptedProxyObject, and a type with no
        // factory anywhere in its chain carries CreateBareObjectProxy. One indexed load, one calli, no
        // base-chain walk, and no per-object call into C++.
        internal static unsafe object? CreateProxyObject(int runtimeTypeIndex, EntityId id)
        {
            var table = new ReadOnlySpan<IntPtr>((void*)s_ProxyObjectFactoryTableAddress, k_MaxNativeRuntimeTypes);

            // Out of range means a corrupted or stale native object header. The explicit check keeps
            // the index in the error and lets the JIT drop the span's own bounds check.
            if ((uint)runtimeTypeIndex >= (uint)table.Length)
            {
                Debug.LogError($"TypeManagerV2.CreateProxyObject: runtime type index {runtimeTypeIndex} is out of range (object {id}).");
                return null;
            }

            // Every slot holds a factory (native fills the whole capacity), so no null check.
            return ((delegate*<EntityId, object?>)table[runtimeTypeIndex])(id);
        }

        // Used in testing only
        internal static unsafe object? Produce(RuntimeTypeHandle typeHandle)
        {
            if (!IsManagedTypeRegistered(typeHandle.Value))
            {
                Debug.LogError($"TypeManagerV2.Produce: type '{Type.GetTypeFromHandle(typeHandle)}' is not registered");
                return null;
            }

            IntPtr factoryPtr = FindManagedFactory(typeHandle.Value);
            if (factoryPtr == IntPtr.Zero)
            {
                Debug.LogError($"TypeManagerV2.Produce: factory function is null for type '{Type.GetTypeFromHandle(typeHandle)}'");
                return null;
            }

            return ((delegate*<object>)factoryPtr)();
        }

        // Used in testing only
        internal static bool IsRegistered(RuntimeTypeHandle typeHandle)
        {
            return IsManagedTypeRegistered(typeHandle.Value);
        }

        // Used in testing only: raw backend pointer, for handles carried across a domain reload
        // as plain numbers (the originating generation's RuntimeTypeHandle no longer exists).
        internal static bool IsRegistered(IntPtr backendTypePtr)
        {
            return IsManagedTypeRegistered(backendTypePtr);
        }

        [NativeMethod(Name = "TypeManager_RegisterManagedFactory", IsFreeFunction = true, IsThreadSafe = true)]
        extern static void RegisterManagedFactory(IntPtr backendTypePtr, IntPtr factoryPtr, IntPtr contextHandle);

        [NativeMethod(Name = "TypeManager_RegisterSizedFactory", IsFreeFunction = true, IsThreadSafe = true)]
        extern static void RegisterSizedFactory(IntPtr backendTypePtr, IntPtr factoryPtr, IntPtr contextHandle);

        [NativeMethod(Name = "TypeManager_RegisterProxyObjectFactory", IsFreeFunction = true, IsThreadSafe = true)]
        extern static void RegisterManagedProxyObjectFactory(int persistentTypeID, IntPtr proxyObjectFactoryPtr, IntPtr contextHandle);

        [NativeMethod(Name = "TypeManager_FlushProxyObjectFactoryTable", IsFreeFunction = true, IsThreadSafe = true)]
        extern static void FlushProxyObjectFactoryTable();

        [NativeMethod(Name = "TypeManager_SetBuiltinProxyObjectFactories", IsFreeFunction = true, IsThreadSafe = true)]
        extern static void SetBuiltinProxyObjectFactories(IntPtr bareObjectFactoryPtr, IntPtr scriptedRefusalFactoryPtr, IntPtr unregisteredSlotFactoryPtr);

        [NativeMethod(Name = "TypeManager_FindManagedFactory", IsFreeFunction = true, IsThreadSafe = true)]
        extern static IntPtr FindManagedFactory(IntPtr backendTypePtr);

        // Mints the small dense id that EntityId.TypeId carries for value-type components and
        // records the type's scripting class for the component transfer path; retired with the
        // context like every other registration.
        [NativeMethod(Name = "TypeManager_RegisterComponentTypeClass", IsFreeFunction = true, IsThreadSafe = true)]
        extern static void RegisterComponentTypeClass(IntPtr backendTypePtr, IntPtr contextHandle);

        [NativeMethod(Name = "TypeManager_IsManagedTypeRegistered", IsFreeFunction = true, IsThreadSafe = true)]
        extern static bool IsManagedTypeRegistered(IntPtr backendTypePtr);

        [NativeMethod(Name = "TypeManager_RetireManagedFactories", IsFreeFunction = true, IsThreadSafe = true)]
        extern static void RetireManagedFactories(IntPtr contextHandle);

        [NativeMethod(Name = "TypeManager_ClearRetiredContextsWindow", IsFreeFunction = true, IsThreadSafe = true)]
        extern static void ClearRetiredContextsWindow();


        [NativeMethod(Name = "TypeManager_ResolveRegistrantContext", IsFreeFunction = true, IsThreadSafe = true)]
        extern static IntPtr ResolveRegistrantContext(IntPtr anchorTypePtr);

        // Proxy-object-resolution table export (read once by Initialize). CreateProxyObject then reads
        // native memory directly with no per-object call into C++.
        [NativeMethod(Name = "TypeManager::GetProxyObjectFactoryTableAddress", IsFreeFunction = true, IsThreadSafe = true)]
        extern static IntPtr GetProxyObjectFactoryTableAddress();

    }
}
