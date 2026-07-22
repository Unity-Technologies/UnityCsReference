// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.AI;

public partial struct NavMeshLinkInstance
{
    ///<summary>True if the NavMesh link is added to the navigation system - otherwise false (RO).</summary>
    ///<seealso cref="NavMesh.IsLinkValid" />
    [Obsolete("valid has been deprecated. Use NavMesh.IsLinkValid() instead.")]
    public bool valid => NavMesh.IsValidLinkHandle(id);

    ///<summary>Removes this instance from the game.</summary>
    ///<remarks>This method is an identical but convenient alternative to <see cref="NavMesh.RemoveLink" />. If the instance is not valid, e.g. has already been removed, the call has no effect.</remarks>
    ///<seealso cref="NavMesh.AddLink" />
    ///<seealso cref="NavMesh.RemoveLink" />
    [Obsolete("Remove() has been deprecated. Use NavMesh.RemoveLink() instead.")]
    public void Remove()
    {
        NavMesh.RemoveLinkInternal(id);
    }

    ///<summary>Get or set the owning <see cref="Object" />.</summary>
    ///<remarks>If the instance is not valid, setting the owner has no effect and getting it returns null.
    ///
    ///Use this property to reference the component that created the link, or more generally, any object that contains useful information about this specific link active in the navigation system. The owner is null for any new link instance created with <see cref="NavMesh.AddLink" />. You can, at any time, assign any Object to this property and retrieve that reference later. 
    ///
    ///When the link instance is <see cref="NavMeshLinkInstance.Remove">removed</see> the owner property returns null once again.</remarks>
    ///<seealso cref="OffMeshLinkData.owner" />
    ///<seealso cref="NavMesh.GetLinkOwner" />
    ///<seealso cref="NavMesh.SetLinkOwner" />
    [Obsolete("owner has been deprecated. Use NavMesh.GetLinkOwner() and NavMesh.SetLinkOwner() instead.")]
    public Object owner
    {
        get => NavMesh.InternalGetLinkOwner(id);
        set
        {
            var ownerID = value != null ? value.GetEntityId() : EntityId.None;
            if (!NavMesh.InternalSetLinkOwner(id, ownerID))
                Debug.LogError("Cannot set 'owner' on an invalid NavMeshLinkInstance");
        }
    }
}
