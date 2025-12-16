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
    ///<summary>Container for holding information about where objects will be serialized in a build.</summary>
    ///<remarks>This class helps ensure that Object references can be correctly resolved in the final built data.
    ///
    ///Note: this class and its members exist to provide low-level support for the **Scriptable Build Pipeline** package. This is intended for internal use only; use the &lt;a href="https://docs.unity3d.com/Packages/com.unity.scriptablebuildpipeline@latest/index.html"&gt;Scriptable Build Pipeline package&lt;/a&gt; to implement a fully featured build pipeline. You can install this via the [Package Manager window](/upm-ui.md).</remarks>
    [Serializable]
    [UsedByNativeCode]
    [NativeHeader("Modules/ContentBuild/Editor/SBPSupport/BuildReferenceMap.h")]
    public class BuildReferenceMap : ISerializable, IDisposable
    {
        private IntPtr m_Ptr;

        ///<summary>Default constructor for an empty BuildReferenceMap.</summary>
        ///<remarks>Internal use only. See <see cref="BuildReferenceMap" />.</remarks>
        public BuildReferenceMap()
        {
            m_Ptr = Internal_Create();
        }

        ~BuildReferenceMap()
        {
            Dispose(false);
        }

        ///<summary>Dispose the BuildReferenceMap destroying all internal state.</summary>
        ///<remarks>Internal use only. See <see cref="BuildReferenceMap" />.</remarks>
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

        ///<summary>Gets the hash for the BuildReferenceMap.</summary>
        ///<remarks>Internal use only. See <see cref="BuildReferenceMap" />.</remarks>
        [NativeMethod(IsThreadSafe = true)]
        public extern Hash128 GetHash128();

        [NativeMethod(IsThreadSafe = true)]
        internal extern string SerializeToJson();
        [NativeMethod(IsThreadSafe = true)]
        internal extern void DeserializeFromJson(string data);

        [NativeMethod(IsThreadSafe = true)]
        internal extern byte[] SerializeToBinary();
        [NativeMethod(IsThreadSafe = true)]
        internal extern void DeserializeFromBinary([Out] byte[] data);

        ///<summary>Adds a mapping for a single Object to where it will be serialized out to the build.</summary>
        ///<remarks>Internal use only. See <see cref="BuildReferenceMap" />.</remarks>
        public void AddMapping(string internalFileName, long serializationIndex, ObjectIdentifier objectID, bool overwrite = false)
        {
            Internal_AddMapping(internalFileName, serializationIndex, objectID, overwrite);
        }

        [NativeMethod("AddMapping", IsThreadSafe = true)]
        private extern void Internal_AddMapping(string internalFileName, long serializationIndex, ObjectIdentifier objectID, bool overwrite);

        ///<summary>Adds mappings for a set of Objects to where they will be serialized out to the build.</summary>
        ///<remarks>Internal use only. See <see cref="BuildReferenceMap" />.</remarks>
        public void AddMappings(string internalFileName, SerializationInfo[] objectIDs, bool overwrite = false)
        {
            Internal_AddMappings(internalFileName, objectIDs, overwrite);
        }

        [NativeMethod("AddMappings", IsThreadSafe = true)]
        private extern void Internal_AddMappings(string internalFileName, SerializationInfo[] objectIDs, bool overwrite);

        ///<summary>Filters this BuildReferenceMap instance to remove references to any objects that are not in the array of <see cref="ObjectIdentifier" />s specified by <c>objectIds</c>.</summary>
        ///<param name="objectIds">The set of desired objects.</param>
        [NativeMethod(IsThreadSafe = true)]
        public extern void FilterToSubset(ObjectIdentifier[] objectIds);

        ///<summary>Returns true if the objects are equal.</summary>
        ///<remarks>Internal use only. See <see cref="BuildReferenceMap" />.</remarks>
        public override bool Equals(object obj)
        {
            BuildReferenceMap other = obj as BuildReferenceMap;
            if (other == null)
                return false;
            return other.GetHash128() == GetHash128();
        }

        ///<summary>Gets the hash code for the BuildReferenceMap.</summary>
        ///<remarks>Internal use only. See <see cref="BuildReferenceMap" />.</remarks>
        public override int GetHashCode()
        {
            return GetHash128().GetHashCode();
        }

        ///<summary>ISerializable method for serialization support outside of Unity's internal serialization system.</summary>
        ///<param name="info">The SerializationInfo to populate with data.</param>
        ///<param name="context">The destination for this serialization.</param>
        ///<remarks>Internal use only. See <see cref="BuildReferenceMap" />.</remarks>
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

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(BuildReferenceMap buildReferenceMap) => buildReferenceMap.m_Ptr;
        }
    }
}
