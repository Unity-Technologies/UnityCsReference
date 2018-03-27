// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.Serialization;

namespace UnityEditor.Build.Player
{
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
        private static extern void Internal_Destroy(IntPtr ptr);

        public extern Hash128 GetHash128();

        internal extern string SerializeToJson();
        internal extern void DeserializeFromJson(string data);

        internal extern byte[] SerializeToBinary();
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
