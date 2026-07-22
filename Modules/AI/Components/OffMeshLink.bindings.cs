// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.AI
{
    // Keep this enum in sync with the one defined in "NavMeshBindingTypes.h"
    ///<summary>Link type specifier.</summary>
    [MovedFrom("UnityEngine")]
    public enum OffMeshLinkType
    {
        ///<summary>Manually specified type of link.</summary>
        LinkTypeManual = 0,

        ///<summary>Vertical drop.</summary>
        LinkTypeDropDown = 1,

        ///<summary>Horizontal jump.</summary>
        LinkTypeJumpAcross = 2
    }

    // Keep this struct in sync with the one defined in "NavMeshBindingTypes.h"
    ///<summary>State of OffMeshLink.</summary>
    ///<seealso cref="NavMeshAgent.currentOffMeshLinkData" />
    ///<seealso cref="NavMeshAgent.nextOffMeshLinkData" />
    [MovedFrom("UnityEngine")]
    [NativeHeader("Modules/AI/Components/OffMeshLink.bindings.h")]
    public partial struct OffMeshLinkData
    {
        internal int m_Valid;
        internal int m_Activated;
        internal EntityId m_InstanceID;
        internal OffMeshLinkType m_LinkType;
        internal Vector3 m_StartPos;
        internal Vector3 m_EndPos;

        ///<summary>Is link valid (RO).</summary>
        public bool valid => m_Valid != 0;

        ///<summary>Is link active (RO).</summary>
        public bool activated => m_Activated != 0;

        ///<summary>Link type specifier (RO).</summary>
        public OffMeshLinkType linkType => m_LinkType;

        ///<summary>Link start world position (RO).</summary>
        public Vector3 startPos => m_StartPos;

        ///<summary>Link end world position (RO).</summary>
        public Vector3 endPos => m_EndPos;

        ///<summary>Get the object used to create the NavMesh link represented by the data in this struct.</summary>
        ///<remarks>If the link has been instantiated by a call to <see cref="NavMesh.AddLink" /> then this property returns the object that might have been associated to that instance with a call to <see cref="NavMesh.SetLinkOwner" />. If that link instance has no owner assigned to it then this property returns null.
        ///
        ///If the link was instantiated by an <see cref="OffMeshLink" /> component then the owner returns a reference to that component.
        ///
        ///To effectively use this property in your scripts you need to determine the exact type of the returned object. To do that cast the object to the types that you use in your project to create NavMesh links.
        ///
        ///For automatically-generated Jump or Drop links, this property returns null.</remarks>
        ///<seealso cref="NavMesh.SetLinkOwner" />
        public Object owner => GetLinkOwnerInternal(m_InstanceID);

        [FreeFunction("OffMeshLinkScriptBindings::GetLinkOwnerInternal")]
        static extern Object GetLinkOwnerInternal(EntityId instanceID);
    }
}
