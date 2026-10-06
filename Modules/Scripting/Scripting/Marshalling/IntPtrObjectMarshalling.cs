// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Scripting;

namespace UnityEngine.Bindings
{
    // Why all of the ENABLE_CORECLR defines?
    // This code is only used on CoreCLR and references API's that we only define on CoreCLR (e.g ScriptingUtility.Get/SetValueAtOffset)
    // But the RequiredByNativeCodeMethods's are processed on the Mono backend so this class and those methods need to exist there, even though we
    // can't implement them and won't call them.

    internal partial class IntPtrObjectMarshalling
    {
        [RequiredByNativeCode]
        internal static object CreateDefault(IntPtr type, out IntPtr nativePointer)
        {
            return GetOrCreateAccess(SystemReflectionMarshalling.UnmarshalSystemType(type)).CreateDefault(out nativePointer);
        }

        [RequiredByNativeCode]
        internal static object CreateFromNative(IntPtr typePtr, IntPtr ptr)
        {
            var type = SystemReflectionMarshalling.UnmarshalSystemType(typePtr);
            var obj = FormatterServices.GetUninitializedObject(type);
            GetOrCreateAccess(type).SetIntPtr(obj, ptr);
            return obj;
        }

        // Returns the pointer through an out parameter rather than the return value: a
        // [RequiredByNativeCode] method's return value is boxed by the native-to-managed invoke, 
        // but out/ref value-type parameters are not.
        [RequiredByNativeCode]
        internal static void GetIntPtr(object obj, out IntPtr result)
        {
            result = obj == null ? IntPtr.Zero : GetOrCreateAccess(obj.GetType()).GetIntPtr(obj);
        }

        [RequiredByNativeCode]
        internal static void SetIntPtr(object obj, IntPtr ptr)
        {
            if (obj != null)
                GetOrCreateAccess(obj.GetType()).SetIntPtr(obj, ptr);
        }


        delegate object CreateDefaultDelegate(out IntPtr nativePointer);

        struct IntPtrObjectAccess
        {
            public CreateDefaultDelegate CreateDefault;
            public Func<object, IntPtr> GetIntPtr;
            public Action<object, IntPtr> SetIntPtr;
        }

        // Caches DynamicMethod-backed accessors keyed by (possibly user-defined) Type. The delegates and
        // Type keys reference user code and pin the old ALC, so the cache is cleared on code reload by
        // ClearAccessCacheOnCodeReload() below; [NoAutoStaticsCleanup] opts out of the auto-cleanup
        // source generator in favor of that manual cleanup.
        [NoAutoStaticsCleanup]
        private static readonly Dictionary<Type, IntPtrObjectAccess> _typeToAccess = new Dictionary<Type, IntPtrObjectAccess>();

        // Plain lock object — no references to user code, safe to persist across code reload.
        [NoAutoStaticsCleanup]
        private static readonly object _accessLock = new object();

        // Drop cached per-type accessors on code reload so stale delegates/types from the previous ALC
        // are released and accessors are regenerated for the reloaded types.
        [OnCodeUnloading]
        static void ClearAccessCacheOnCodeReload()
        {
            lock (_accessLock)
                _typeToAccess.Clear();
        }

        private static IntPtrObjectAccess GetOrCreateAccess(Type type)
        {
            IntPtrObjectAccess access;

            lock (_accessLock)
            {
                if (_typeToAccess.TryGetValue(type, out access))
                    return access;
            }

            access = CreateAccess(type);

            lock (_accessLock)
            {
                _typeToAccess[type] = access;
            }

            return access;
        }

        private static FieldInfo GetFirstField(Type type)
        {
            if (type == null) return null;
            var field = GetFirstField(type.BaseType);
            if (field != null) return field;
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (fields.Length > 0)
                return fields[0];
            return null;
        }

        private static IntPtrObjectAccess CreateAccess(Type type)
        {
            var accessor = new IntPtrObjectAccess();

            var field = GetFirstField(type);
            if (field == null || field.FieldType != typeof(IntPtr))
            {
                var error = $"{nameof(IntPtrObjectAccess)} only be used on types that have a first field of IntPtr {type.FullName} does not";

                Unity.Scripting.LowLevel.Debug.LogError(error);
                System.Diagnostics.Debug.Assert(false, error);
                throw new InvalidOperationException(error);
            }

            // Emit obj->field0 = arg0
            var setterDm = new DynamicMethod($"IntPtrObjectMarshaller_{type.FullName}_{field.Name}_GeneratedSetter", typeof(void), new Type[] { typeof(object), typeof(IntPtr) }, true);
            var setterIl = setterDm.GetILGenerator();
            setterIl.Emit(OpCodes.Ldarg_0); // load object
            setterIl.Emit(OpCodes.Ldarg_1); // load IntPtr
            setterIl.Emit(OpCodes.Stfld, field); // set field
            setterIl.Emit(OpCodes.Ret);
            accessor.SetIntPtr = (Action<object, IntPtr>)setterDm.CreateDelegate(typeof(Action<object, IntPtr>));

            // Emit return obj->field0;
            var getterDm = new DynamicMethod($"IntPtrObjectMarshaller_{type.FullName}_{field.Name}_GeneratedGetter", typeof(IntPtr), new Type[] { typeof(object) }, true);
            var getterIl = getterDm.GetILGenerator(); // corrected to getterDm
            getterIl.Emit(OpCodes.Ldarg_0); // load object
            getterIl.Emit(OpCodes.Ldfld, field); // get field
            getterIl.Emit(OpCodes.Ret);
            accessor.GetIntPtr = (Func<object, IntPtr>)getterDm.CreateDelegate(typeof(Func<object, IntPtr>));

            var defaultCtor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            if (defaultCtor != null)
            {
                var createDefaultDm = new DynamicMethod($"IntPtrObjectMarshaller_{type.FullName}_GeneratedDefaultConstructor", typeof(object), new Type[] {typeof(IntPtr).MakeByRefType()}, true);
                var createDefaultIl = createDefaultDm.GetILGenerator();

                createDefaultIl.DeclareLocal(type); // local 0: T obj

                /* Emit:
                 * T obj = new T();
                 * ref arg = obj.field0;
                 * return obj;
                 */
                createDefaultIl.Emit(OpCodes.Newobj, defaultCtor); // new T()
                createDefaultIl.Emit(OpCodes.Stloc_0); // store in local 0
                createDefaultIl.Emit(OpCodes.Ldarg_0); // load IntPtr arg
                createDefaultIl.Emit(OpCodes.Ldloc_0); // load obj
                createDefaultIl.Emit(OpCodes.Ldfld, field); // load field
                createDefaultIl.Emit(OpCodes.Stind_I);
                createDefaultIl.Emit(OpCodes.Ldloc_0); // load obj
                createDefaultIl.Emit(OpCodes.Ret);

                accessor.CreateDefault = (CreateDefaultDelegate)createDefaultDm.CreateDelegate(typeof(CreateDefaultDelegate));
            }

            return accessor;
        }
    }
}
