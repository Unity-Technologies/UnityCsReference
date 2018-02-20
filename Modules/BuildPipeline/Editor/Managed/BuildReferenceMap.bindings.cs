// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.Serialization;

namespace UnityEditor.Experimental.Build.AssetBundle
{
    [NativeHeader("Modules/BuildPipeline/Editor/Public/BuildReferenceMap.h")]
    [Serializable]
    [UsedByNativeCode]
    public class BuildReferenceMap : ISerializable, IDisposable
    {
        private IntPtr m_Ptr;

        public BuildReferenceMap()
        {
            m_Ptr = Internal_Create();
        }

        ~BuildReferenceMap()
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

        [FreeFunction("BuildPipeline::BuildReferenceMap::Internal_Create")]
        private static extern IntPtr Internal_Create();

        [FreeFunction("BuildPipeline::BuildReferenceMap::Internal_Destroy", true)]
        private static extern void Internal_Destroy(IntPtr ptr);

        private extern int GetHash();

        internal extern string SerializeNativeToString();

        internal extern void DeserializeNativeFromString(string data);

        public extern void AddMapping(string internalFileName, long serializationIndex, ObjectIdentifier objectID);

        public extern void AddMappings(string internalFileName, SerializationInfo[] objectIDs);

        public override bool Equals(object obj)
        {
            BuildReferenceMap other = obj as BuildReferenceMap;
            if (other == null)
                return false;
            return other.GetHash() == GetHash();
        }

        public override int GetHashCode()
        {
            return GetHash();
        }

        public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, StreamingContext context)
        {
            string text = SerializeNativeToString();
            info.AddValue("tags", text);
        }

        protected BuildReferenceMap(System.Runtime.Serialization.SerializationInfo info, StreamingContext context)
        {
            m_Ptr = Internal_Create();
            string serializedBuildReferenceMapString = (string)info.GetValue("tags", typeof(string));
            DeserializeNativeFromString(serializedBuildReferenceMapString);
        }
    }
}
