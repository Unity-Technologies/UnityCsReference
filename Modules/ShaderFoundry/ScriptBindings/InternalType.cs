// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.ShaderFoundry
{
    internal interface IInternalType<T> where T : struct
    {
        internal T ConstructInvalid();
    }

    // This is a static cache of information about an internal type to avoid looking up that data in DataTypeStatic constantly.
    internal static class InternalTypeStatic<T> where T : struct, IInternalType<T>
    {
        // We store all of the static data about an internal type here
        // since we can't require that IInternalType make static data available generically through the interface
        // (at least not until C#10 with static abstract interface members).
        static DataTypeStatic.DataTypeInfo GetDataTypeInfo()
        {
            var info = DataTypeStatic.GetInfoFromInternalType<T>();
            if (info.dataType == DataType.Unknown)
                throw new Exception("Unknown Internal Type. Type was likely never registered");
            return info;
        }

        private static DataTypeStatic.DataTypeInfo dataTypeInfo = GetDataTypeInfo();
        public static DataType dataType => dataTypeInfo.dataType;
        public static Type publicType => dataTypeInfo.publicType;
        public static int internalTypeSizeInBytes => dataTypeInfo.internalTypeSizeInBytes;
        // We can't add static functions to interfaces, so we use the Invalid struct to call it as a member function
        public static T Invalid => new T().ConstructInvalid();
    }
}
