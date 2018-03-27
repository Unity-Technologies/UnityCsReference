// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using UnityEngine;

namespace UnityEditor.Build.Content
{
    [Serializable]
    [UsedByNativeCode]
    [NativeHeader("Modules/BuildPipeline/Editor/Public/BuildReferenceMap.h")]
    public class BuildReferenceMap : ISerializable, IDisposable
    {
        private IntPtr m_Ptr;

        public BuildReferenceMap()
        {
            m_Ptr = Internal_Create();
        }

        ~BuildReferenceMap()
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

        public void AddMapping(string internalFileName, long serializationIndex, ObjectIdentifier objectID, bool overwrite = false)
        {
            Internal_AddMapping(internalFileName, serializationIndex, objectID, overwrite);
        }

        [NativeMethod("AddMapping")]
        private extern void Internal_AddMapping(string internalFileName, long serializationIndex, ObjectIdentifier objectID, bool overwrite);

        public void AddMappings(string internalFileName, SerializationInfo[] objectIDs, bool overwrite = false)
        {
            Internal_AddMappings(internalFileName, objectIDs, overwrite);
        }

        [NativeMethod("AddMappings")]
        private extern void Internal_AddMappings(string internalFileName, SerializationInfo[] objectIDs, bool overwrite);

        public override bool Equals(object obj)
        {
            BuildReferenceMap other = obj as BuildReferenceMap;
            if (other == null)
                return false;
            return other.GetHash128() == GetHash128();
        }

        public override int GetHashCode()
        {
            return GetHash128().GetHashCode();
        }

        public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, StreamingContext context)
        {
            byte[] data = SerializeToBinary();
            info.AddValue("referenceMap", data);
        }

        protected BuildReferenceMap(System.Runtime.Serialization.SerializationInfo info, StreamingContext context)
        {
            m_Ptr = Internal_Create();
            byte[] data = (byte[])info.GetValue("referenceMap", typeof(byte[]));
            DeserializeFromBinary(data);
        }
    }
}
