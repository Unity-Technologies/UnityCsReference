// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.AI
{
    public partial struct OffMeshLinkData
    {
        // The [[OffMeshLink]] if the link type is a manually placed Offmeshlink (RO).
        [Obsolete("offMeshLink has been deprecated. Use 'owner' instead.")]
        public OffMeshLink offMeshLink => GetOffMeshLinkInternal(m_InstanceID);

#pragma warning disable CS0618 // The OffMeshLink class is obsolete
        [FreeFunction("OffMeshLinkScriptBindings::GetOffMeshLinkInternal")]
        static extern OffMeshLink GetOffMeshLinkInternal(int instanceID);
#pragma warning restore CS0618
    }

    // Link allowing movement outside the planar navigation mesh.
    [MovedFrom("UnityEngine")]
    [Obsolete("The OffMeshLink component is no longer supported and will be removed. Use NavMeshLink instead.")]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/OffMeshLink.html")]
    public sealed class OffMeshLink : Behaviour
    {
        // Is link active.
        [Obsolete("activated has been deprecated together with the class. Declare the object as NavMeshLink and use activated as before.")]
        public extern bool activated { get; set; }

        // Is link occupied. (RO)
        [Obsolete("occupied has been deprecated together with the class. Declare the object as NavMeshLink and use occupied as before.")]
        public extern bool occupied { get; }

        // Modify pathfinding cost for the link.
        [Obsolete("costOverride has been deprecated together with the class. Declare the object as NavMeshLink and use costModifier instead.")]
        public extern float costOverride { get; set; }

        [Obsolete("biDirectional has been deprecated together with the class. Declare the object as NavMeshLink and use bidirectional instead.")]
        public extern bool biDirectional { get; set; }

        [Obsolete("UpdatePositions() has been deprecated together with the class. Declare the object as NavMeshLink and use UpdateLink() instead.")]
        public extern void UpdatePositions();

        [Obsolete("navMeshLayer has been deprecated together with the class. Declare the object as NavMeshLink and use area instead. (UnityUpgradable) -> area")]
        public int navMeshLayer { get { return area; }  set { area = value; } }

        [Obsolete("area has been deprecated together with the class. Declare the object as NavMeshLink and use area as before.")]
        public extern int area { get; set; }

        [Obsolete("autoUpdatePositions has been deprecated together with the class. Declare the object as NavMeshLink and use autoUpdate instead.")]
        public extern bool autoUpdatePositions { get; set; }

        [Obsolete("startTransform has been deprecated together with the class. Declare the object as NavMeshLink and use startTransform as before.")]
        public extern Transform startTransform { get; set; }

        [Obsolete("endTransform has been deprecated together with the class. Declare the object as NavMeshLink and use endTransform as before.")]
        public extern Transform endTransform { get; set; }
    }
}
