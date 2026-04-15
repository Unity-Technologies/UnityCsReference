// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.Bindings
{
    [VisibleToOtherModules]
    internal struct MarshalledMethod
    {
        public IntPtr method;
        public IntPtr declaringType;
    }

    [VisibleToOtherModules]
    internal struct MarshalledField
    {
        public IntPtr field;
        public IntPtr declaringType;
    }

    [VisibleToOtherModules]
    internal static class SystemReflectionMarshalling
    {
        public static IntPtr[] MarshalSystemTypes(ReadOnlySpan<Type> types)
        {
            var handles = new IntPtr[types.Length];
            for (int i = 0; i < types.Length; ++i)
                handles[i] = MarshalSystemType(types[i]);
            return handles;
        }

        public static Type[] UnmarshalSystemTypes(ReadOnlySpan<IntPtr> handlePtrs)
        {
            var types = new Type[handlePtrs.Length];
            for (int i = 0; i < handlePtrs.Length; ++i)
                types[i] = UnmarshalSystemType(handlePtrs[i]);
            return types;
        }

        public static MarshalledMethod[] MarshalMethodBases(ReadOnlySpan<MethodInfo> methods)
        {
            var handles = new MarshalledMethod[methods.Length];
            for (int i = 0; i < methods.Length; ++i)
                handles[i] = MarshalMethodBase(methods[i]);
            return handles;
        }

        public static MethodBase[] UnmarshalMethodBases(ReadOnlySpan<MarshalledMethod> marshalledMethods)
        {
            var methods = new MethodBase[marshalledMethods.Length];
            for (int i = 0; i < marshalledMethods.Length; ++i)
                methods[i] = UnmarshalMethodBase(marshalledMethods[i]);
            return methods;
        }

        /// <summary>
        /// Converts native type handle representation of RuntimeTypeHandle to Type object.
        /// </summary>
        /// <param name="handlePtr">A value of ScriptingBackendNativeTypePtr type in native code. E.g. ScriptingTypePtr.GetBackendPtr()</param>
        /// <returns>Type object</returns>
        public static Type UnmarshalSystemType(IntPtr handlePtr)
        {
            if (handlePtr == IntPtr.Zero)
                return null;

            var handle = UnmarshalRuntimeTypeHandle(handlePtr);
            return Type.GetTypeFromHandle(handle);
        }

        public static FieldInfo UnmarshalFieldInfo(MarshalledField marshalledField)
        {
            if (marshalledField.field == IntPtr.Zero)
                return null;

            var declaringClassHandle = UnmarshalRuntimeTypeHandle(marshalledField.declaringType);

            var fieldHandle = Unsafe.As<IntPtr, RuntimeFieldHandle>(ref marshalledField.field);
            return FieldInfo.GetFieldFromHandle(fieldHandle, declaringClassHandle);
        }

        public static MethodBase UnmarshalMethodBase(MarshalledMethod marshalledMethod)
        {
            if (marshalledMethod.method == IntPtr.Zero)
                return null;

            var declaringClassHandle = UnmarshalRuntimeTypeHandle(marshalledMethod.declaringType);
            var methodHandle         = UnmarshalRuntimeMethodHandle(marshalledMethod.method);

            return MethodBase.GetMethodFromHandle(methodHandle, declaringClassHandle);
        }

        /// <summary>
        /// Converts native type handle representation of RuntimeMethodHandle to RuntimeMethodHandle object.
        /// NOTE: The function RuntimeMethodHandle.FromIntPtr allocates on CoreCLR!
        /// https://github.com/Unity-Technologies/runtime/blob/02255f44de205f944f4a807f8314cc9595c4b552/src/coreclr/System.Private.CoreLib/src/System/RuntimeHandles.cs#L803
        /// </summary>
        /// <param name="handlePtr"></param>
        /// <returns></returns>
        public static RuntimeMethodHandle UnmarshalRuntimeMethodHandle(IntPtr handlePtr)
        {
            return Unsafe.As<IntPtr, RuntimeMethodHandle>(ref handlePtr);
        }

        public static RuntimeTypeHandle UnmarshalRuntimeTypeHandle(IntPtr handlePtr)
        {
            return Unsafe.As<IntPtr, RuntimeTypeHandle>(ref handlePtr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr MarshalSystemType(Type type)
        {
            return type != null ? type.TypeHandle.Value : IntPtr.Zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MarshalledMethod MarshalMethodBase(MethodBase methodBase)
        {
            MarshalledMethod ret = new MarshalledMethod();
            if(methodBase == null)
            {
                ret.declaringType = IntPtr.Zero;
                ret.method = IntPtr.Zero;
            }
            else
            {
                ret.declaringType = MarshalSystemType(methodBase.DeclaringType);
                ret.method = methodBase.MethodHandle.Value;
            }
            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MarshalledField MarshalFieldInfo(FieldInfo fieldInfo)
        {
            return fieldInfo != null ? new MarshalledField() {field = fieldInfo.FieldHandle.Value, declaringType = fieldInfo.DeclaringType.TypeHandle.Value} : default;
        }
    }
}


