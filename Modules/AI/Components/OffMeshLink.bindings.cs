// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.AI
{
    // Keep this enum in sync with the one defined in "NavMeshBindingTypes.h"
    // Link type specifier.
    [MovedFrom("UnityEngine")]
    public enum OffMeshLinkType
    {
        // Manually specified type of link.
        LinkTypeManual = 0,

        // Vertical drop.
        LinkTypeDropDown = 1,

        // Horizontal jump.
        LinkTypeJumpAcross = 2
    }

    // Keep this struct in sync with the one defined in "NavMeshBindingTypes.h"
    // State of OffMeshLink.
    [MovedFrom("UnityEngine")]
    [NativeHeader("Modules/AI/Components/OffMeshLink.bindings.h")]
    public partial struct OffMeshLinkData
    {
        internal int m_Valid;
        internal int m_Activated;
        internal int m_InstanceID;
        internal OffMeshLinkType m_LinkType;
        internal Vector3 m_StartPos;
        internal Vector3 m_EndPos;

        // Is link valid (RO).
        public bool valid => m_Valid != 0;

        // Is link active (RO).
        public bool activated => m_Activated != 0;

        // Link type specifier (RO).
        public OffMeshLinkType linkType => m_LinkType;

        // Link start world position (RO).
        public Vector3 startPos => m_StartPos;

        // Link end world position (RO).
        public Vector3 endPos => m_EndPos;

        // The object that created this link instance if the link type is a manually placed [[Offmeshlink]] or [[NavMeshLinkData]] (RO).
        public Object owner => GetLinkOwnerInternal(m_InstanceID);

        [FreeFunction("OffMeshLinkScriptBindings::GetLinkOwnerInternal")]
        static extern Object GetLinkOwnerInternal(int instanceID);
    }
}
