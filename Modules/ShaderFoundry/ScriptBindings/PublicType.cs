// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.ShaderFoundry
{
    [FoundryAPI]
    internal interface IPublicType
    {
        public ShaderContainer Container { get; }
        public bool IsValid { get; }
        internal FoundryHandle Handle { get; }
    }

    [FoundryAPI]
    internal interface IPublicType<T> : IPublicType where T : struct
    {
        internal T ConstructFromHandle(ShaderContainer container, FoundryHandle handle);
    }

    // This is a static cache of information about a public type to avoid looking up that data in DataTypeStatic constantly.
    internal static class PublicTypeStatic<T> where T : struct, IPublicType<T>
    {
        // We store all of the static data about a public type here
        // since we can't require that IPublicType make static data available generically through the interface
        // (at least not until C#10 with static abstract interface members).
        static DataTypeStatic.DataTypeInfo GetDataTypeInfo()
        {
            var info = DataTypeStatic.GetInfoFromPublicType<T>();
            if (info.dataType == DataType.Unknown)
                throw new Exception("Unknown Public Type. Type was likely never registered");
            return info;
        }

        private static DataTypeStatic.DataTypeInfo dataTypeInfo = GetDataTypeInfo();
        public static DataType dataType => dataTypeInfo.dataType;
        public static Type internalType => dataTypeInfo.internalType;
        public static int internalTypeSizeInBytes => dataTypeInfo.internalTypeSizeInBytes;
        public static T Invalid => ConstructFromHandle(null, FoundryHandle.Invalid());
        public static T ConstructFromHandle(ShaderContainer container, FoundryHandle handle)
        {
            // We can't add static functions to interfaces, so we use the Invalid struct to call it as a member function
            return new T().ConstructFromHandle(container, handle);
        }
    }
}
