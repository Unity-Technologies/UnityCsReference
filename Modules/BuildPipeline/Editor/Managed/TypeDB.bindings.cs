// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Build.Player
{
    public static class TypeDbHelper
    {
        [Obsolete("TryGet(path, out typeDb) is deprecated. Use TryGet(path, assemblyPath, out typeDb) instead.")]
        public static bool TryGet(string path, out TypeDB typeDb)
        {
            return TryGet(path, path, out typeDb);
        }

        public static bool TryGet(string path, string assemblyPath, out TypeDB typeDb)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Null or Empty is not allowed", nameof(path));
            }

            typeDb = new TypeDB();

            var typeDbFilePathsFrom = BuildPlayerDataGenerator.GetTypeDbFilePathsFrom(path);
            AssemblyInfoManaged[] extractAssemblyTypeInfo = AssemblyHelper.ExtractAssemblyTypeInfoFromFiles(typeDbFilePathsFrom);
            if (extractAssemblyTypeInfo != null && extractAssemblyTypeInfo.Any())
            {
                typeDb.AddAssemblyInfo(extractAssemblyTypeInfo, assemblyPath);
                return true;
            }
            return false;
        }
    }

    [Serializable]
    [UsedByNativeCode]
    [NativeHeader("Modules/BuildPipeline/Editor/Public/TypeDB.h")]
    public class TypeDB : ISerializable, IDisposable
    {
        private IntPtr m_Ptr;

        internal TypeDB()
        {
            m_Ptr = Internal_Create();
        }

        ~TypeDB()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        [NativeMethod(IsThreadSafe = true)]
        private static extern IntPtr Internal_Create();

        [NativeMethod(IsThreadSafe = true)]
        internal extern void AddAssemblyInfo(AssemblyInfoManaged[] assemblyInfos, string assembliesPath);

        [NativeMethod(IsThreadSafe = true)]
        private static extern void Internal_Destroy(IntPtr ptr);

        [NativeMethod(IsThreadSafe = true)]
        public extern Hash128 GetHash128();

        internal extern string SerializeToJson();
        internal extern void DeserializeFromJson(string data);

        [NativeMethod(IsThreadSafe = true)]
        internal extern byte[] SerializeToBinary();
        [NativeMethod(IsThreadSafe = true)]
        internal extern void DeserializeFromBinary([Out] byte[] data);

        public override bool Equals(object obj)
        {
            TypeDB other = obj as TypeDB;
            if (other == null)
                return false;
            return other.GetHash128() == GetHash128();
        }

        public override int GetHashCode()
        {
            return GetHash128().GetHashCode();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            byte[] data = SerializeToBinary();
            info.AddValue("typedb", data);
        }

        protected TypeDB(SerializationInfo info, StreamingContext context)
        {
            m_Ptr = Internal_Create();
            byte[] data = (byte[])info.GetValue("typedb", typeof(byte[]));
            DeserializeFromBinary(data);
        }
    }
}
