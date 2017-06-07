// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.Serialization;

namespace UnityEditor.Experimental.Build.Player
{
    [NativeHeader("Modules/BuildPipeline/Editor/Public/TypeDB.h")]
    [Serializable]
    public class TypeDB : ISerializable, IDisposable
    {
        [FreeFunction("TypeDB::Internal_Create")]
        private static extern IntPtr Internal_Create();
        [FreeFunction("TypeDB::Internal_Destroy", true)]
        private static extern void Internal_Destroy(IntPtr ptr);

        [NativeMethod("TypeDB::GetHash")]
        private extern int GetHash();

        [NativeMethod("SerializeNativeTypeDB")]
        private extern string SerializeNativeTypeDB();
        [NativeMethod("DeserializeNativeTypeDB")]
        private extern void DeserializeNativeTypeDB(string data);

        private IntPtr m_Ptr;

        internal TypeDB()
        {
            m_Ptr = Internal_Create();
        }

        ~TypeDB()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        public override bool Equals(object obj)
        {
            TypeDB other = obj as TypeDB;
            if (other == null)
                return false;
            return other.GetHash() == GetHash();
        }

        public override int GetHashCode()
        {
            return GetHash();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            string text = SerializeNativeTypeDB();
            info.AddValue("typedb", text);
        }

        protected TypeDB(SerializationInfo info, StreamingContext context)
        {
            m_Ptr = Internal_Create();
            string serializedTypeDBString = (string)info.GetValue("typedb", typeof(string));
            DeserializeNativeTypeDB(serializedTypeDBString);
        }
    }
}
