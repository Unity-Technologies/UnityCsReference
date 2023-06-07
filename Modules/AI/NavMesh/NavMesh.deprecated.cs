// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.AI;

public partial struct NavMeshLinkInstance
{
    [Obsolete("valid has been deprecated. Use NavMesh.IsLinkValid() instead.")]
    public bool valid => NavMesh.IsValidLinkHandle(id);

    [Obsolete("Remove() has been deprecated. Use NavMesh.RemoveLink() instead.")]
    public void Remove()
    {
        NavMesh.RemoveLinkInternal(id);
    }

    [Obsolete("owner has been deprecated. Use NavMesh.GetLinkOwner() and NavMesh.SetLinkOwner() instead.")]
    public Object owner
    {
        get => NavMesh.InternalGetLinkOwner(id);
        set
        {
            var ownerID = value != null ? value.GetInstanceID() : 0;
            if (!NavMesh.InternalSetLinkOwner(id, ownerID))
                Debug.LogError("Cannot set 'owner' on an invalid NavMeshLinkInstance");
        }
    }
}
