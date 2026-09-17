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
        // Acquired once per domain generation (statics reset on a Mono reload), so the previous
        // generation's retired-window entry can never block this generation's registrations —
        // the [OnCodeLoaded] window clear below runs after the [OnAssemblyLoaded] bursts, and
        // must not need to run first. NoAutoStaticsCleanup: ordinary static reinitialization
        // is the reset; a scope-transition clear would hand out a stale zero context.
        [NoAutoStaticsCleanup]
        static readonly IntPtr s_DomainContext = AcquireDomainContext();
        static IntPtr DomainContext => s_DomainContext;


        // Anchors registrations made inside the scope to the load context of the assembly that
        // declares the anchor type. Scopes nest; each restores the previous context on dispose.
        public static RegistrationScope BeginRegistration(RuntimeTypeHandle anchorTypeHandle)
        {
            return new RegistrationScope(IntPtr.Zero);
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
            }
        }

        // typeName and hardcodedPersistentId are legacy arguments the source generator still emits;
        // identity is the type handle alone. typeName only feeds diagnostics.
        public static void RegisterFactory(RuntimeTypeHandle typeHandle, string typeName, IntPtr factoryPtr,
                                           int hardcodedPersistentId = 0)
        {
            if (factoryPtr == IntPtr.Zero)
                Debug.LogError($"TypeManagerV2.RegisterFactory: factoryPtr is null for type '{typeName}'");

            RegisterManagedFactory(typeHandle.Value, factoryPtr, DomainContext);
        }

        // Metadata only, a value-type factory would box. Registered with a null factory so
        // diagnostics can distinguish "registered without a factory" from "never registered".
        public static void RegisterType(RuntimeTypeHandle typeHandle, string typeName,
                                        int hardcodedPersistentId = 0)
        {
            RegisterManagedFactory(typeHandle.Value, IntPtr.Zero, DomainContext);
            RegisterComponentTypeClass(typeHandle.Value, DomainContext);
        }

        // Everything lives and dies with the domain; a Mono reload exits this assembly's own
        // scope, and players never unload code. Retirement also opens the retired window, which
        // rejects registrations from teardown code until the new generation clears it below.
        [OnCodeUnloading]
        internal static void RetireDomainContext()
        {
            RetireManagedFactories(DomainContext);
        }

        [OnCodeLoaded]
        internal static void ClearRetiredWindow()
        {
            ClearRetiredContextsWindow();
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

        [NativeMethod(Name = "TypeManager_AcquireDomainContext", IsFreeFunction = true, IsThreadSafe = true)]
        extern static IntPtr AcquireDomainContext();

    }
}
