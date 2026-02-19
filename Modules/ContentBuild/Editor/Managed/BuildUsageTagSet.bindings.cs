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
    ///<summary>Container for holding information about how objects are being used in a build.</summary>
    ///<remarks>This class helps ensure the correct Shared Variants, Mesh Channels, and more are included in the build correctly.
    ///
    ///Note: this class and its members exist to provide low-level support for the **Scriptable Build Pipeline** package. This is intended for internal use only; use the &lt;a href="https://docs.unity3d.com/Packages/com.unity.scriptablebuildpipeline@latest/index.html"&gt;Scriptable Build Pipeline package&lt;/a&gt; to implement a fully featured build pipeline. You can install this via the [Package Manager window](/upm-ui.md).</remarks>
    [Serializable]
    [UsedByNativeCode]
    [NativeHeader("Modules/ContentBuild/Editor/BuildUsage/BuildUsageTagSet.h")]
    public class BuildUsageTagSet : ISerializable, IDisposable
    {
        private IntPtr m_Ptr;

        ///<summary>Default constructor for an empty BuildUsageTagSet.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.BuildUsageTagSet" />.</remarks>
        public BuildUsageTagSet()
        {
            m_Ptr = Internal_Create();
        }

        ~BuildUsageTagSet()
        {
            Dispose(false);
        }

        ///<summary>Dispose the BuildUsageTagSet destroying all internal state.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.BuildUsageTagSet" />.</remarks>
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
        ///<remarks>Internal use only. See <see cref="Build.Content.BuildUsageTagSet" />.</remarks>
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

        [NativeMethod(IsThreadSafe = true)]
        internal extern string GetBuildUsageJson(ObjectIdentifier objectId);

        ///<summary>Returns an array of <see cref="ObjectIdentifiers" /> that this BuildUsageTagSet contains usage information about.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.BuildUsageTagSet" />.</remarks>
        [NativeMethod(IsThreadSafe = true)]
        public extern ObjectIdentifier[] GetObjectIdentifiers();

        ///<summary>Adds the Object usage information from another BuildUsageTagSet to this BuildUsageTagSet.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.BuildUsageTagSet" />.</remarks>
        ///<param name="other">Object usage information to be added to this BuildUsageTagSet.</param>
        [NativeMethod(IsThreadSafe = true)]
        public extern void UnionWith(BuildUsageTagSet other);

        ///<summary>Filters this BuildUsageTagSet instance to remove references to any objects that are not in the array of <see cref="ObjectIdentifier" />s specified by <c>objectIds</c>.</summary>
        ///<param name="objectIds">The set of desired objects.</param>
        [NativeMethod(IsThreadSafe = true)]
        public extern void FilterToSubset(ObjectIdentifier[] objectIds);

        ///<summary>Returns true if the objects are equal.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.BuildUsageTagSet" />.</remarks>
        public override bool Equals(object obj)
        {
            BuildUsageTagSet other = obj as BuildUsageTagSet;
            if (other == null)
                return false;
            return other.GetHash128() == GetHash128();
        }

        ///<summary>Gets the hash code for the BuildUsageTagSet.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.BuildUsageTagSet" />.</remarks>
        ///<returns>The hash code of the BuildUsageTagSet.</returns>
        public override int GetHashCode()
        {
            return GetHash128().GetHashCode();
        }

        ///<summary>ISerializable method for serialization support outside of Unity's internal serialization system.</summary>
        ///<param name="info">The SerializationInfo to populate with data.</param>
        ///<param name="context">The destination for this serialization.</param>
        ///<remarks>Internal use only. See <see cref="Build.Content.BuildUsageTagSet" />.</remarks>
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

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(BuildUsageTagSet buildUsageTagSet) => buildUsageTagSet.m_Ptr;
        }
    }
}
