// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
    public struct OffMeshLinkData
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

        // The [[OffMeshLink]] if the link type is a manually placed Offmeshlink (RO).
        public OffMeshLink offMeshLink => GetOffMeshLinkInternal(m_InstanceID);

        [FreeFunction("OffMeshLinkScriptBindings::GetOffMeshLinkInternal")]
        internal static extern OffMeshLink GetOffMeshLinkInternal(int instanceID);
    }

    // Link allowing movement outside the planar navigation mesh.
    [MovedFrom("UnityEngine")]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.ai.navigation@1.1/manual/OffMeshLink.html")]
    public sealed class OffMeshLink : Behaviour
    {
        // Is link active.
        public extern bool activated { get; set; }

        // Is link occupied. (RO)
        public extern bool occupied { get; }

        // Modify pathfinding cost for the link.
        public extern float costOverride { get; set; }

        public extern bool biDirectional { get; set; }

        public extern void UpdatePositions();

        [System.Obsolete("Use area instead.")]
        public int navMeshLayer { get { return area; }  set { area = value; } }

        public extern int area { get; set; }

        public extern bool autoUpdatePositions { get; set; }

        public extern Transform startTransform { get; set; }

        public extern Transform endTransform { get; set; }
    }
}
