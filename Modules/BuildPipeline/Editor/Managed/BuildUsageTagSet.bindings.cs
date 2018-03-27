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
    [NativeHeader("Modules/BuildPipeline/Editor/Public/BuildUsageTagSet.h")]
    public class BuildUsageTagSet : ISerializable, IDisposable
    {
        private IntPtr m_Ptr;

        public BuildUsageTagSet()
        {
            m_Ptr = Internal_Create();
        }

        ~BuildUsageTagSet()
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

        internal extern string GetBuildUsageJson(ObjectIdentifier objectId);

        public extern ObjectIdentifier[] GetObjectIdentifiers();

        public extern void UnionWith(BuildUsageTagSet other);

        public override bool Equals(object obj)
        {
            BuildUsageTagSet other = obj as BuildUsageTagSet;
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
            info.AddValue("tags", data);
        }

        protected BuildUsageTagSet(System.Runtime.Serialization.SerializationInfo info, StreamingContext context)
        {
            m_Ptr = Internal_Create();
            byte[] data = (byte[])info.GetValue("tags", typeof(byte[]));
            DeserializeFromBinary(data);
        }
    }
}
