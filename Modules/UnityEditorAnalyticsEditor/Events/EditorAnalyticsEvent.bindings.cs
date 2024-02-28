// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEditor.Analytics
{
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityEditorAnalyticsEditor/Events/EditorAnalyticsEvent.h")]
    internal class EditorAnalyticsEvent
    {
        IntPtr m_Ptr;

        internal EditorAnalyticsEvent(IntPtr nativePtr)
        {
            m_Ptr = nativePtr;
        }

        public void AddParameter(string name, bool value)
        {
            AddParameterBool(name, value);
        }

        private extern void AddParameterBool(string name, bool value);

        public void AddParameter(string name, int value)
        {
            AddInt32(name, value);
        }

        [NativeMethod("AddParameter")]
        private extern void AddInt32(string name, int value);

        public void AddParameter(string name, uint value)
        {
            AddUInt32(name, value);
        }

        [NativeMethod("AddParameter")]
        private extern void AddUInt32(string name, uint value);

        public void AddParameter(string name, string value)
        {
            AddParameterString(name, value);
        }

        private extern void AddParameterString(string name, string value);

        public void AddParameter(string name, int[] value)
        {
            AddParameterInt32Array(name, value);
        }

        private extern void AddParameterInt32Array(string name, int[] value);

        public void AddParameter(string name, uint[] value)
        {
            AddParameterUInt32Array(name, value);
        }

        private extern void AddParameterUInt32Array(string name, uint[] value);

        public void AddParameter<T>(string name, T value)
            where T : Enum
        {
            var underlyingType = Enum.GetUnderlyingType(typeof(T));
            if (underlyingType == typeof(int))
                AddParameter(name, (int)(ValueType)value);
            else if (underlyingType == typeof(uint))
                AddParameter(name, (uint)(ValueType)value);
            else
                throw new NotSupportedException($"Enums {typeof(T)} underlying type {underlyingType} is not supported");
        }

        public void AddParameter<T>(string name, T[] value)
            where T : Enum
        {
            var underlyingType = Enum.GetUnderlyingType(typeof(T));
            if (underlyingType == typeof(int))
                AddParameter(name, ConvertEnumArray<int, T>(value));
            else if (underlyingType == typeof(uint))
                AddParameter(name, ConvertEnumArray<uint, T>(value));
            else
                throw new NotSupportedException($"Enums {typeof(T)} underlying type {underlyingType} is not supported");
        }

        private PrimitiveT[] ConvertEnumArray<PrimitiveT, EnumT>(EnumT[] value)
            where PrimitiveT : struct
            where EnumT : Enum
        {
            var ret = new PrimitiveT[value.Length];
            for (int i = 0; i < value.Length; ++i)
                ret[i] = (PrimitiveT)(ValueType)value[i];
            return ret;
        }

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(EditorAnalyticsEvent evt) => evt.m_Ptr;
        }
    }
}
