// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Collections.Generic;

namespace UnityEngine.XR
{
    ///<summary>A session-unique identifier for trackables in the environment, e.g., planes and feature points.</summary>
    ///<remarks>Ids are generally unique to a particular <c>XRSessionSubsystem</c>, but multiple sessions may produce identical ids for different trackables.</remarks>
    [NativeHeader("Modules/XR/Subsystems/Meshing/XRMeshBindings.h")]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct MeshId : IEquatable<MeshId>
    {
        ///<summary>Generates a nicely formatted version of the id.</summary>
        ///<returns>A string unique to this id</returns>
        public override string ToString()
        {
            return string.Format("{0}-{1}",
                m_SubId1.ToString("X16"),
                m_SubId2.ToString("X16"));
        }

        ///<exclude />
        public override int GetHashCode()
        {
            return m_SubId1.GetHashCode() ^ m_SubId2.GetHashCode();
        }

        ///<exclude />
        public override bool Equals(object obj)
        {
            return obj is MeshId && Equals((MeshId)obj);
        }

        ///<exclude />
        public bool Equals(MeshId other)
        {
            return (m_SubId1 == other.m_SubId1) && (m_SubId2 == other.m_SubId2);
        }

        ///<exclude />
        public static bool operator==(MeshId id1, MeshId id2)
        {
            return
                (id1.m_SubId1 == id2.m_SubId1) &&
                (id1.m_SubId2 == id2.m_SubId2);
        }

        ///<exclude />
        public static bool operator!=(MeshId id1, MeshId id2)
        {
            return
                (id1.m_SubId1 != id2.m_SubId1) ||
                (id1.m_SubId2 != id2.m_SubId2);
        }

        [NoAutoStaticsCleanup] // default value type; no user refs
        private static MeshId s_InvalidId = new MeshId();
        ///<summary>Represents an invalid id.</summary>
        public static MeshId InvalidId { get { return s_InvalidId; } }

        private ulong m_SubId1;
        private ulong m_SubId2;
    }

    ///<summary>The status of a <see cref="XRMeshSubsystem.GenerateMeshAsync" />.</summary>
    ///<remarks>
    ///  <see cref="XRMeshSubsystem.GenerateMeshAsync" /> will always invoke the provided delegate when the generation completes. This enum contains information about whether the generation was successful, or if an error occurred.</remarks>
    ///<seealso cref="XRMeshSubsystem.GenerateMeshAsync" />
    [NativeHeader("Modules/XR/Subsystems/Meshing/XRMeshBindings.h")]
    [RequiredByNativeCode]
    public enum MeshGenerationStatus
    {
        ///<summary>The mesh generation was successful.</summary>
        ///<seealso cref="XRMeshSubsystem.GenerateMeshAsync" />
        ///<seealso cref="MeshGenerationResult" />
        Success,
        ///<summary>The mesh generation failed because the mesh does not exist.</summary>
        ///<remarks>The tracked mesh with the provided <see cref="MeshId" /> is no longer tracked.</remarks>
        ///<seealso cref="XRMeshSubsystem.GenerateMeshAsync" />
        ///<seealso cref="MeshGenerationResult" />
        InvalidMeshId,
        ///<summary>The <see cref="XRMeshSubsystem" /> was already generating the requested mesh.</summary>
        ///<seealso cref="XRMeshSubsystem.GenerateMeshAsync" />
        ///<seealso cref="MeshGenerationResult" />
        GenerationAlreadyInProgress,
        ///<summary>The mesh generation was canceled.</summary>
        ///<remarks>This can happen if the <see cref="XRMeshSubsystem" /> is stopped or destroyed while meshes are being generated.
        ///                    No data is written to the <see cref="Mesh" /> or [[MeshCollider] provided during the call to <see cref="XRMeshSubsystem.GenerateMeshAsync" />.</remarks>
        ///<seealso cref="XRMeshSubsystem.GenerateMeshAsync" />
        ///<seealso cref="MeshGenerationResult" />
        Canceled,
        ///<summary>The mesh generation failed for unknown reasons.</summary>
        ///<seealso cref="XRMeshSubsystem.GenerateMeshAsync" />
        ///<seealso cref="MeshGenerationResult" />
        UnknownError,
    }

    internal static class HashCodeHelper
    {
        const int k_HashCodeMultiplier = 486187739;

        public static int Combine(int hash1, int hash2)
        {
            unchecked
            {
                return hash1 * k_HashCodeMultiplier + hash2;
            }
        }

        public static int Combine(int hash1, int hash2, int hash3) => Combine(Combine(hash1, hash2), hash3);
        public static int Combine(int hash1, int hash2, int hash3, int hash4) => Combine(Combine(hash1, hash2, hash3), hash4);
        public static int Combine(int hash1, int hash2, int hash3, int hash4, int hash5) => Combine(Combine(hash1, hash2, hash3, hash4), hash5);
        public static int Combine(int hash1, int hash2, int hash3, int hash4, int hash5, int hash6) => Combine(Combine(hash1, hash2, hash3, hash4, hash5), hash6);
        public static int Combine(int hash1, int hash2, int hash3, int hash4, int hash5, int hash6, int hash7) => Combine(Combine(hash1, hash2, hash3, hash4, hash5, hash6), hash7);
        public static int Combine(int hash1, int hash2, int hash3, int hash4, int hash5, int hash6, int hash7, int hash8) => Combine(Combine(hash1, hash2, hash3, hash4, hash5, hash6, hash7), hash8);
    }

    ///<summary>Contains event information related to a generated mesh.</summary>
    ///<remarks>This struct is used by the <see cref="XRMeshSubsystem" /> to provide information about the result of <see cref="XRMeshSubsystem.GenerateMeshAsync" />.</remarks>
    [NativeHeader("Modules/XR/Subsystems/Meshing/XRMeshBindings.h")]
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct MeshGenerationResult : IEquatable<MeshGenerationResult>
    {
        ///<summary>The <see cref="MeshId" /> of the tracked mesh that was generated.</summary>
        ///<seealso cref="XRMeshSubsystem.GenerateMeshAsync" />
        [NativeName("meshId")]
        public MeshId MeshId { get; }
        ///<summary>If the generation was successful, data has been written to this <see cref="Mesh" />.</summary>
        ///<remarks>Check <see cref="MeshGenerationResult.Status" /> to determine if the generation was successful.</remarks>
        ///<seealso cref="XRMeshSubsystem.GenerateMeshAsync" />
        [NativeName("mesh")]
        public Mesh Mesh { get; }
        ///<summary>If the generation was successful, physics data has been written to this <see cref="MeshCollider" />.</summary>
        ///<remarks>Check <see cref="MeshGenerationResult.Status" /> to determine if the generation was successful. <see cref="MeshCollider" /> may be null if a null <see cref="MeshCollider" /> was provided to <see cref="XRMeshSubsystem.GenerateMeshAsync" />.</remarks>
        ///<seealso cref="XRMeshSubsystem.GenerateMeshAsync" />
        [NativeName("meshCollider")]
        public MeshCollider MeshCollider { get; }
        ///<summary>The <see cref="MeshGenerationStatus" /> of the mesh generation task.</summary>
        ///<remarks>Use this to determine whether the requested mesh generation was successful.</remarks>
        ///<seealso cref="XRMeshSubsystem.GenerateMeshAsync" />
        [NativeName("status")]
        public MeshGenerationStatus Status { get; }
        ///<summary>The <see cref="MeshVertexAttributes" /> that were written to the <see cref="MeshGenerationResult.Mesh" />.</summary>
        ///<remarks>The vertex attributes will be the intersection of those requested by the caller of <see cref="XRMeshSubsystem.GenerateMeshAsync" /> and what the subsystem's mesh provider is able to supply. For example, if you request vertex tangents, but the mesh provider cannot supply them, then <see cref="MeshGenerationResult.Attributes" /> will not include tangents.</remarks>
        ///<seealso cref="XRMeshSubsystem.GenerateMeshAsync" />
        [NativeName("attributes")]
        public MeshVertexAttributes Attributes { get; }
        ///<summary>The timestamp associated with the generated mesh.</summary>
        ///<remarks>Because generation is asynchronous, the transform provided by this <see cref="MeshGenerationResult" /> may be older than the most recent transform provided by
        ///                    <see cref="XRMeshSubsystem.GetUpdatedMeshTransforms" />. Compare timestamps to ensure you are using the most recent transform.</remarks>
        ///<seealso cref="XRMeshSubsystem.GenerateMeshAsync" />
        ///<seealso cref="XRMeshSubsystem.GetUpdatedMeshTransforms" />
        [NativeName("timestamp")]
        public ulong Timestamp { get; }
        ///<summary>The position associated with the generated mesh relative to the session origin.</summary>
        ///<remarks>This value will be zero unless the <see cref="MeshGenerationOptions.ConsumeTransform" /> flag was provided to <see cref="XRMeshSubsystem.GenerateMeshAsync" />.</remarks>
        ///<seealso cref="XRMeshSubsystem.GenerateMeshAsync" />
        [NativeName("position")]
        public Vector3 Position { get; }
        ///<summary>The rotation associated with the generated mesh relative to the session origin.</summary>
        ///<remarks>This value will be <see cref="Quaternion.identity" /> unless the <see cref="MeshGenerationOptions.ConsumeTransform" /> flag was provided to <see cref="XRMeshSubsystem.GenerateMeshAsync" />.</remarks>
        ///<seealso cref="XRMeshSubsystem.GenerateMeshAsync" />
        [NativeName("rotation")]
        public Quaternion Rotation { get; }
        ///<summary>The scale associated with the generated mesh relative to the session origin.</summary>
        ///<remarks>This value will be <see cref="Vector3.one" /> unless the <see cref="MeshGenerationOptions.ConsumeTransform" /> flag was provided to <see cref="XRMeshSubsystem.GenerateMeshAsync" />.</remarks>
        ///<seealso cref="XRMeshSubsystem.GenerateMeshAsync" />
        [NativeName("scale")]
        public Vector3 Scale { get; }

        public override bool Equals(object obj)
        {
            if (!(obj is MeshGenerationResult))
                return false;

            return Equals((MeshGenerationResult)obj);
        }

        public bool Equals(MeshGenerationResult other)
        {
            return
                MeshId.Equals(other.MeshId) &&
                Mesh.Equals(other.Mesh) &&
                MeshCollider.Equals(other.MeshCollider) &&
                Status == other.Status &&
                Attributes == other.Attributes &&
                Position.Equals(other.Position) &&
                Rotation.Equals(other.Rotation) &&
                Scale.Equals(other.Scale);
        }

        ///<exclude />
        public static bool operator==(MeshGenerationResult lhs, MeshGenerationResult rhs)
        {
            return lhs.Equals(rhs);
        }

        ///<exclude />
        public static bool operator!=(MeshGenerationResult lhs, MeshGenerationResult rhs)
        {
            return !lhs.Equals(rhs);
        }

        public override int GetHashCode()
        {
            return HashCodeHelper.Combine(
                MeshId.GetHashCode(), Mesh.GetHashCode(), MeshCollider.GetHashCode(),
                ((int)Status).GetHashCode(), ((int)Attributes).GetHashCode(),
                Position.GetHashCode(), Rotation.GetHashCode(), Scale.GetHashCode());
        }
    }

    ///<summary>A set of vertex attributes.</summary>
    ///<remarks>This enum is used by <see cref="XRMeshSubsystem.GenerateMeshAsync" /> to request particular vertex attributes, or by <see cref="MeshGenerationResult" /> to indicate which vertex attributes were written to the <see cref="Mesh" />.</remarks>
    [NativeHeader("Modules/XR/Subsystems/Meshing/XRMeshBindings.h")]
    [UsedByNativeCode]
    [Flags]
    public enum MeshVertexAttributes
    {
        ///<summary>No vertex attributes</summary>
        ///<remarks>Used by <see cref="XRMeshSubsystem.GenerateMeshAsync" /> to indicate that no additional attributes be generated, or by <see cref="MeshGenerationResult" /> to indicate that no attributes (other than positions) were written to the <see cref="Mesh" />.</remarks>
        ///<seealso cref="XRMeshSubsystem.GenerateMeshAsync" />
        ///<seealso cref="MeshGenerationResult" />
        None = 0,
        ///<summary>Vertex normals</summary>
        ///<remarks>Used by <see cref="XRMeshSubsystem.GenerateMeshAsync" /> to request that normals get generated, or by <see cref="MeshGenerationResult" /> to indicate normals were written to the <see cref="Mesh" />.</remarks>
        ///<seealso cref="XRMeshSubsystem.GenerateMeshAsync" />
        ///<seealso cref="MeshGenerationResult" />
        Normals = 1 << 0,
        ///<summary>Vertex tangents</summary>
        ///<remarks>Used by <see cref="XRMeshSubsystem.GenerateMeshAsync" /> to request tangents be generated, or by <see cref="MeshGenerationResult" /> to indicate tangents were written to the <see cref="Mesh" />.</remarks>
        ///<seealso cref="XRMeshSubsystem.GenerateMeshAsync" />
        ///<seealso cref="MeshGenerationResult" />
        Tangents = 1 << 1,
        ///<summary>Vertex UVs</summary>
        ///<remarks>Used by <see cref="XRMeshSubsystem.GenerateMeshAsync" /> to request UVs be generated, or by <see cref="MeshGenerationResult" /> to indicate UVs were written to the <see cref="Mesh" />.</remarks>
        ///<seealso cref="XRMeshSubsystem.GenerateMeshAsync" />
        ///<seealso cref="MeshGenerationResult" />
        UVs = 1 << 2,
        ///<summary>Vertex normals</summary>
        ///<remarks>Used by <see cref="XRMeshSubsystem.GenerateMeshAsync" /> to request that colors get generated, or by <see cref="MeshGenerationResult" /> to indicate colors were written to the <see cref="Mesh" />.</remarks>
        ///<seealso cref="XRMeshSubsystem.GenerateMeshAsync" />
        ///<seealso cref="MeshGenerationResult" />
        Colors = 1 << 3,
    }

    ///<summary>Options for generating meshes.</summary>
    ///<remarks>Use this enum with <see cref="XRMeshSubsystem.GenerateMeshAsync" /> to tell Unity how to handle data from the mesh provider.
    ///
    ///                The mesh provider can supply a transform along with the mesh data. When this happens, the value of <c>MeshGenerationOptions</c> determines what Unity does with the supplied transform. If <c>ConsumeTransform</c> is set, Unity ignores the supplied transform. Otherwise, Unity applies the supplied transform to the vertices of the mesh, and rebakes the physics mesh. These transformation and rebaking operations can be CPU-intensive; if you do not need to perform these operations, you should set <c>ConsumeTransform</c>.
    ///
    ///                If the mesh provider does not supply a transform, the value of <c>MeshGenerationOptions</c> has no effect.</remarks>
    ///<seealso cref="XRMeshSubsystem.GenerateMeshAsync" />
    ///<seealso cref="MeshGenerationResult" />
    [NativeHeader("Modules/XR/Subsystems/Meshing/XRMeshBindings.h")]
    [Flags, UsedByNativeCode]
    public enum MeshGenerationOptions
    {
        ///<summary>No options are specified.</summary>
        ///<remarks>Used by <see cref="XRMeshSubsystem.GenerateMeshAsync" />. If the mesh provider supplies a transform and <c>MeshGenerationOptions</c> has this value, Unity applies the supplied transform to the vertices of the mesh, and rebakes the physics mesh. If you do not need to perform these operations, you should set <see cref="XR.MeshGenerationOptions.ConsumeTransform" />.</remarks>
        ///<seealso cref="XRMeshSubsystem.GenerateMeshAsync" />
        ///<seealso cref="MeshGenerationResult" />
        None = 0,
        ///<summary>Indicates you plan to consume the resulting mesh's transform.</summary>
        ///<remarks>Used by <see cref="XRMeshSubsystem.GenerateMeshAsync" /> when the mesh provider supplies a transform. If <c>MeshGenerationOptions</c> has this value, Unity does not apply the supplied transform to the vertices. Otherwise, Unity applies the supplied transform to the vertices of the mesh, and rebakes the physics mesh.</remarks>
        ///<seealso cref="XRMeshSubsystem.GenerateMeshAsync" />
        ///<seealso cref="MeshGenerationResult" />
        ConsumeTransform = 1 << 0,
    }

    ///<summary>The state of a tracked mesh since the last query.</summary>
    ///<remarks>This enum is used by <see cref="MeshInfo" /> to indicate which meshes have been added, updated, or removed.</remarks>
    ///<seealso cref="XRMeshSubsystem.TryGetMeshInfos" />
    [NativeHeader("Modules/XR/Subsystems/Meshing/XRMeshBindings.h")]
    [UsedByNativeCode]
    public enum MeshChangeState
    {
        ///<summary>The mesh has been added since the last call to <see cref="XRMeshSubsystem.TryGetMeshInfos" />.</summary>
        Added,
        ///<summary>The mesh has been updated since the last call to <see cref="XRMeshSubsystem.TryGetMeshInfos" />.</summary>
        Updated,
        ///<summary>The mesh has been removed since the last call to <see cref="XRMeshSubsystem.TryGetMeshInfos" />.</summary>
        Removed,
        ///<summary>The mesh has not changed since the last call to <see cref="XRMeshSubsystem.TryGetMeshInfos" />.</summary>
        Unchanged,
    }

    ///<summary>Contains state information related to a tracked mesh.</summary>
    ///<remarks>This struct is used by the <see cref="XRMeshSubsystem" /> to determine which meshes have been added, updated, or removed.</remarks>
    ///<seealso cref="XRMeshSubsystem.TryGetMeshInfos" />
    [NativeHeader("Modules/XR/Subsystems/Meshing/XRMeshBindings.h")]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct MeshInfo : IEquatable<MeshInfo>
    {
        ///<summary>The <see cref="MeshId" /> of the tracked mesh.</summary>
        public MeshId MeshId { get; set; }
        ///<summary>The change state (e.g., Added, Removed) of the tracked mesh.</summary>
        ///<remarks>The <c>ChangeState</c> is relative to the last call to <see cref="XRMeshSubsystem.TryGetMeshInfos" />. For example, a value of <see cref="MeshChangeState.Added" /> indicates the mesh has been added since the last call to <c>TryGetMeshInfos</c>.</remarks>
        public MeshChangeState ChangeState { get; set; }
        ///<summary>A hint that can be used to determine when this mesh should be processed.</summary>
        public int PriorityHint { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is MeshInfo))
                return false;

            return Equals((MeshInfo)obj);
        }

        public bool Equals(MeshInfo other)
        {
            return
                MeshId.Equals(other.MeshId) &&
                ChangeState.Equals(other.ChangeState) &&
                PriorityHint.Equals(other.PriorityHint);
        }

        ///<exclude />
        public static bool operator==(MeshInfo lhs, MeshInfo rhs)
        {
            return lhs.Equals(rhs);
        }

        ///<exclude />
        public static bool operator!=(MeshInfo lhs, MeshInfo rhs)
        {
            return !lhs.Equals(rhs);
        }

        public override int GetHashCode() =>
            HashCodeHelper.Combine(MeshId.GetHashCode(), ((int)ChangeState).GetHashCode(), PriorityHint.GetHashCode());
    }

    ///<summary>Contains transform information related to a tracked mesh.</summary>
    ///<remarks>This struct is used by the <see cref="XRMeshSubsystem" /> to communication information about a mesh's transform.</remarks>
    ///<seealso cref="XRMeshSubsystem.GetUpdatedMeshTransforms" />
    [NativeHeader("Modules/XR/Subsystems/Meshing/XRMeshBindings.h")]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    readonly public struct MeshTransform : IEquatable<MeshTransform>
    {
        ///<summary>The session-unique identifier of the tracked mesh.</summary>
        public MeshId MeshId { get; }
        ///<summary>The timestamp associated with this transform.</summary>
        ///<remarks>A larger value indicates a more recent time. This timestamp can be compared with <see cref="MeshGenerationResult.Timestamp" /> to determine whether the generation result (which is asynchronous) contains a newer transform.</remarks>
        ///<seealso cref="XRMeshSubsystem.GetUpdatedMeshTransforms" />
        public ulong Timestamp { get; }
        ///<summary>The position of the mesh, relative to the session origin.</summary>
        public Vector3 Position { get; }
        ///<summary>The rotation of the mesh, relative to the session origin.</summary>
        public Quaternion Rotation { get; }
        ///<summary>The scale of the mesh, relative to the session origin.</summary>
        public Vector3 Scale { get; }

        ///<summary>Creates a new <see cref="MeshTransform" />.</summary>
        ///<param name="meshId">The identifier of the mesh.</param>
        ///<param name="timestamp">The timestamp for the mesh's transform. Larger values indicate newer transforms.</param>
        ///<param name="position">The position of the mesh relative to the session origin.</param>
        ///<param name="rotation">The rotation of the mesh relative to the session origin.</param>
        ///<param name="scale">The scale of the mesh relative to the session origin.</param>
        public MeshTransform(in MeshId meshId, ulong timestamp, in Vector3 position, in Quaternion rotation, in Vector3 scale)
        {
            MeshId = meshId;
            Timestamp = timestamp;
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }

        public override bool Equals(object obj) => obj is MeshTransform other && Equals(other);
        public bool Equals(MeshTransform other) =>
            MeshId.Equals(other.MeshId) &&
            Timestamp == other.Timestamp &&
            Position.Equals(other.Position) &&
            Rotation.Equals(other.Rotation) &&
            Scale.Equals(other.Scale);
        ///<exclude />
        public static bool operator==(MeshTransform lhs, MeshTransform rhs) => lhs.Equals(rhs);
        ///<exclude />
        public static bool operator!=(MeshTransform lhs, MeshTransform rhs) => !lhs.Equals(rhs);
        public override int GetHashCode() =>
            HashCodeHelper.Combine(MeshId.GetHashCode(), Timestamp.GetHashCode(), Position.GetHashCode(), Rotation.GetHashCode(), Scale.GetHashCode());
    }

    ///<summary>Allows external systems to provide dynamic meshes to Unity.</summary>
    ///<remarks>The XRMeshSubsystem enables external systems to provide dynamic meshes to Unity. The meshes are processed on background threads, including physics baking, so as not to block the main thread during execution. This is useful for that provide dynamic meshes during runtime, such as spatially-aware AR devices.</remarks>
    [NativeHeader("Modules/XR/XRPrefix.h")]
    [NativeHeader("Modules/XR/Subsystems/Meshing/XRMeshingSubsystem.h")]
    [NativeConditional("ENABLE_XR")]
    [UsedByNativeCode]
    public class XRMeshSubsystem : IntegratedSubsystem<XRMeshSubsystemDescriptor>
    {
        ///<summary>Gets information about every Mesh the system currently tracks.</summary>
        ///<remarks>Use this to determine which meshes have been added, changed, or removed.
        ///
        ///                    **Note:** This method provides state changes since the last time the method was called. Typically, a single system should manage this information.</remarks>
        ///<param name="meshInfosOut">A <c>List</c> of <see cref="MeshInfo" />s to be filled. Passing <c>null</c> will throw an <c>ArgumentNullException</c>.</param>
        ///<returns>True if the <c>List</c> was populated.</returns>
        ///<seealso cref="XRMeshSubsystem.GenerateMeshAsync" />
        public bool TryGetMeshInfos(List<MeshInfo> meshInfosOut)
        {
            if (meshInfosOut == null)
                throw new ArgumentNullException("meshInfosOut");

            return GetMeshInfosAsList(meshInfosOut);
        }

        private extern bool GetMeshInfosAsList(List<MeshInfo> meshInfos);

        private extern MeshInfo[] GetMeshInfosAsFixedArray();

        ///<summary>Requests the generation of the Mesh with <see cref="MeshId" /><c>meshId</c>. Unity calls <c>onMeshGenerationComplete</c> when generation finishes.</summary>
        ///<remarks>Use this method to request that a mesh is asynchronously generated. "Generation" includes extracting the mesh data from the subsystem's mesh provider (e.g., an AR device) and baking the <see cref="MeshCollider" /> (if <c>meshCollider</c> is not null).
        ///
        ///                    This happens in a background thread. For large meshes, this can take several frames to complete. <c>onMeshGenerationComplete</c> is invoked when the generation completes.
        ///
        ///                    The mesh vertices are provided in session space.</remarks>
        ///<param name="meshId">The <see cref="MeshId" /> of the mesh you wish to generate.</param>
        ///<param name="mesh">The <see cref="Mesh" /> to write the results into.</param>
        ///<param name="meshCollider">(Optional) The <see cref="MeshCollider" /> to populate with physics data. This may be null.</param>
        ///<param name="attributes">The vertex attributes you'd like to use.</param>
        ///<param name="onMeshGenerationComplete">The delegate to invoke when the generation completes.</param>
        ///<seealso cref="XRMeshSubsystem.TryGetMeshInfos" />
        public void GenerateMeshAsync(
            MeshId meshId,
            Mesh mesh,
            MeshCollider meshCollider,
            MeshVertexAttributes attributes,
            Action<MeshGenerationResult> onMeshGenerationComplete)
        {
            GenerateMeshAsync(meshId, mesh, meshCollider, attributes, onMeshGenerationComplete, MeshGenerationOptions.None);
        }

        ///<summary>Requests the generation of the Mesh with <see cref="MeshId" /><c>meshId</c>. Unity calls <c>onMeshGenerationComplete</c> when generation finishes.</summary>
        ///<remarks>This variant allows you to specify additional mesh generation options.
        ///
        ///                    **Note:** If the <see cref="MeshGenerationOptions.ConsumeTransform" /> flag is set in the <c>options</c> argument, the resulting mesh will be relative to the transform provided by the <see cref="MeshGenerationResult" />. If this flag is not set, the vertices are transformed into session space and the <see cref="MeshGenerationResult" /> will contain an identity transform.</remarks>
        ///<param name="meshId">The <see cref="MeshId" /> of the mesh you wish to generate.</param>
        ///<param name="mesh">The <see cref="Mesh" /> to write the results into.</param>
        ///<param name="meshCollider">(Optional) The <see cref="MeshCollider" /> to populate with physics data. This may be null.</param>
        ///<param name="attributes">The vertex attributes you'd like to use.</param>
        ///<param name="onMeshGenerationComplete">The delegate to invoke when the generation completes.</param>
        ///<param name="options">The mesh generation options.</param>
        ///<seealso cref="XRMeshSubsystem.TryGetMeshInfos" />
        public extern void GenerateMeshAsync(
            MeshId meshId,
            Mesh mesh,
            MeshCollider meshCollider,
            MeshVertexAttributes attributes,
            Action<MeshGenerationResult> onMeshGenerationComplete,
            MeshGenerationOptions options);

        [RequiredByNativeCode]
        static void InvokeMeshReadyDelegate(
            IntPtr resultPtr,
            Action<MeshGenerationResult> onMeshGenerationComplete)
        {
            var result = GetMeshGenerationResult(resultPtr);
            onMeshGenerationComplete?.Invoke(result);
        }

        [NativeMethod]
        static extern MeshGenerationResult GetMeshGenerationResult(IntPtr resultPtr);

        ///<summary>Call this function to request a change in the density of the generated Meshes. Unity gives the density level as a value within the range 0.0 to 1.0 and the provider determines how to map that value to their implementation.
        ///Setting this value does not guarantee an immediate change in the density of any currently created Mesh and may only change the density for new or updated Meshes.</summary>
        public extern float meshDensity { get; set; }

        ///<summary>Get this property to determine the currently requested state of submesh classification. Set this property to True to enable submesh classification, if the platform supports it. Set it to False to disable. Note that this property may require a restart of the subsystem in order to take effect.</summary>
        public extern bool submeshClassificationEnabled { get; set; }

        ///<summary>Set the bounding volume to restrict the space in which Unity generates and tracks Meshes.
        ///
        ///The bounding volume is an Axis Aligned Bounding Box (AABB) centered at the <c>origin</c> and extends in each dimension as defined in <c>extents</c>.
        ///
        ///The units of measurement depend on the provider.</summary>
        ///<param name="origin">The center of the bounding volume.</param>
        ///<param name="extents">The extents of the bounding volume in each dimension.</param>
        ///<returns>True if the bounding volume was set. Otherwise, false.</returns>
        public extern bool SetBoundingVolume(Vector3 origin, Vector3 extents);

        [NativeConditional("ENABLE_XR")]
        readonly struct MeshTransformList : IDisposable
        {
            readonly IntPtr m_Self;

            public MeshTransformList(IntPtr self) => m_Self = self;

            public int Count => GetLength(m_Self);

            public IntPtr Data => GetData(m_Self);

            public void Dispose() => Dispose(m_Self);

            [FreeFunction("UnityXRMeshTransformList_get_Length")]
            static extern int GetLength(IntPtr self);

            [FreeFunction("UnityXRMeshTransformList_get_Data")]
            static extern IntPtr GetData(IntPtr self);

            [FreeFunction("UnityXRMeshTransformList_Dispose")]
            static extern void Dispose(IntPtr self);
        }

        ///<summary>Gets the updated mesh transforms.</summary>
        ///<remarks>Use this to get updated transforms for each mesh tracked by the subsystem. The number of transforms returned may be less than the total number of tracked meshes. The results may be affected by previous calls to this method. That is, only transforms that have changed since the last call to this method may be returned.
        ///
        ///                    Typically, you should call this at regular intervals, for example, once per frame, in order to update the transform of each mesh. When a mesh is generated using <see cref="XRMeshSubsystem.GenerateMeshAsync" />, the <see cref="MeshGenerationResult" />
        ///                    also contains a transform and timestamp. Because generation is asynchronous, you can compare timestamps to ensure you are using the most recent transform. Larger values indicate newer transforms.
        ///
        ///                    This method always returns a new <see cref="NativeArray{T}"/>, even when there are no updated transforms. The caller is responsible for disposing the returned <see cref="NativeArray{T}"/>.</remarks>
        ///<param name="allocator">The allocator to use for the returned <see cref="NativeArray{T}"/>.</param>
        ///<returns>A new <see cref="NativeArray{T}"/> of <see cref="MeshTransform" />s.</returns>
        ///<seealso cref="MeshTransform" />
        ///<seealso cref="XRMeshSubsystem.GenerateMeshAsync" />
        ///<seealso cref="MeshGenerationResult" />
        public NativeArray<MeshTransform> GetUpdatedMeshTransforms(Allocator allocator)
        {
            unsafe
            {
                using var transforms = new MeshTransformList(GetUpdatedMeshTransforms());
                var result = new NativeArray<MeshTransform>(transforms.Count, allocator, NativeArrayOptions.UninitializedMemory);
                UnsafeUtility.MemCpy(result.GetUnsafePtr(), transforms.Data.ToPointer(), transforms.Count * sizeof(MeshTransform));
                return result;
            }
        }

        extern IntPtr GetUpdatedMeshTransforms();

        ///<summary>Gets classification information for vertices or vertex sets for meshes obtained through <see cref="XRMeshSubsystem.TryGetMeshInfos" />. This must be enabled through <see cref="submeshClassificationEnabled" />.</summary>
        ///<remarks>Use this to retrieve semantic classifications for vertex or vertex group components of the meshes tracked by the subsystem.
        ///
        ///                    This method always returns new <see cref="NativeArray{T}"/>s, even when there are no results. The caller is responsible for disposing the returned <see cref="NativeArray{T}"/>, though specifying the appropriate <see cref="Allocator" /> will manage this for you.</remarks>
        ///<param name="id">A <see cref="MeshId" /> obtained earlier through TryGetMeshInfos for which to retrieve classification information</param>
        ///<param name="allocator">The <see cref="Allocator" /> type to use for the returned <c>NativeArray</c>s</param>
        ///<param name="elementsPerVector">The number of packed elements in <c>vertexIndexVectors</c> to treat as one classification unit. Platform-specific; for example, some platforms may use `1` for a single vertex per classification; others may use `3` to represent a triangle face per classification.</param>
        ///<param name="vertexIndexVectors">The indices referring to <see cref="Mesh" /> vertices that the classifications apply to, coallated with <c>classifications</c>.</param>
        ///<param name="classifications">The opaque classification enumerations returned by the provider for the defined vertex components above.</param>
        ///<returns>True if the retrieval is successful, otherwise False</returns>
        ///<seealso cref="XRMeshSubsystem.TryGetMeshInfos" />
        public unsafe bool TryGetSubmeshClassifications(
            MeshId id,
            Allocator allocator,
            out uint elementsPerVector,
            out NativeArray<uint> vertexIndexVectors,
            out NativeArray<uint> classifications
        )
        {
            uint* vertexIndexVectorsPtr = null;
            uint* classificationsPtr = null;
            ulong vertexIndexCount = 0;
            ulong classificationCount = 0;

            if (
                !NativeTryGetSubmeshClassifications(
                    id,
                    out elementsPerVector,
                    ref vertexIndexCount,
                    vertexIndexVectorsPtr,
                    ref classificationCount,
                    classificationsPtr
                )
            )
            {
                vertexIndexVectors = default;
                classifications = default;
                return false;
            }

            vertexIndexVectors = new NativeArray<uint>((int)vertexIndexCount, allocator);
            vertexIndexVectorsPtr = (uint*)
                NativeArrayUnsafeUtility.GetUnsafePtr<uint>(vertexIndexVectors);
            classifications = new NativeArray<uint>((int)classificationCount, allocator);
            classificationsPtr = (uint*)
                NativeArrayUnsafeUtility.GetUnsafePtr<uint>(classifications);

            return NativeTryGetSubmeshClassifications(
                id,
                out elementsPerVector,
                ref vertexIndexCount,
                vertexIndexVectorsPtr,
                ref classificationCount,
                classificationsPtr
            );
        }

        [return: MarshalAs(UnmanagedType.U1)]
        [NativeMethod("TryGetSubmeshClassifications")]
        extern unsafe bool NativeTryGetSubmeshClassifications(
            MeshId id,
            out uint elementsPerVector,
            ref ulong vertexVectorCount,
            uint* vertexIndexVectors,
            ref ulong classificationCount,
            uint* classificationBuffer
        );

        new internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(XRMeshSubsystem subsystem) => subsystem.m_Ptr;
        }
    }
}
