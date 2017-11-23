// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.Serialization;

namespace UnityEditor.Experimental.Build.AssetBundle
{
    [NativeHeader("Modules/BuildPipeline/Editor/Public/BuildUsageTagSet.h")]
    [Serializable]
    [UsedByNativeCode]
    public class BuildUsageTagSet : ISerializable, IDisposable
    {
        [FreeFunction("BuildPipeline::BuildUsageTagSet::Internal_Create")]
        private static extern IntPtr Internal_Create();
        [FreeFunction("BuildPipeline::BuildUsageTagSet::Internal_Destroy", true)]
        private static extern void Internal_Destroy(IntPtr ptr);

        [NativeMethod("GetHash")]
        private extern int GetHash();

        [NativeMethod("SerializeNativeToString")]
        private extern string SerializeNativeToString();
        [NativeMethod("DeserializeNativeFromString")]
        private extern void DeserializeNativeFromString(string data);

        [NativeMethod("GetBuildUsageJson")]
        internal extern string GetBuildUsageJson(ObjectIdentifier objectId);

        [NativeMethod("GetObjectIdentifiers")]
        public extern ObjectIdentifier[] GetObjectIdentifiers();

        private IntPtr m_Ptr;

        public BuildUsageTagSet()
        {
            m_Ptr = Internal_Create();
        }

        ~BuildUsageTagSet()
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
            BuildUsageTagSet other = obj as BuildUsageTagSet;
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

        protected BuildUsageTagSet(System.Runtime.Serialization.SerializationInfo info, StreamingContext context)
        {
            m_Ptr = Internal_Create();
            string serializedBuildUsageTagSetString = (string)info.GetValue("tags", typeof(string));
            DeserializeNativeFromString(serializedBuildUsageTagSetString);
        }
    }
}
