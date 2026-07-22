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
        ///<summary>The <see cref="OffMeshLink" /> if the link type is a manually placed Offmeshlink (RO).</summary>
        ///<remarks>Automatically generated Jump and drop links will return null.</remarks>
        [Obsolete("offMeshLink has been deprecated. Use 'owner' instead.")]
        public OffMeshLink offMeshLink => GetOffMeshLinkInternal(m_InstanceID);

#pragma warning disable CS0618 // The OffMeshLink class is obsolete
        [FreeFunction("OffMeshLinkScriptBindings::GetOffMeshLinkInternal")]
        static extern OffMeshLink GetOffMeshLinkInternal(EntityId instanceID);
#pragma warning restore CS0618
    }

    ///<summary>Link allowing movement outside the planar navigation mesh.</summary>
    [MovedFrom("UnityEngine")]
    [Obsolete("The OffMeshLink component is no longer supported and will be removed. Use NavMeshLink instead.")]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/OffMeshLink.html")]
    public sealed class OffMeshLink : Behaviour
    {
        ///<summary>Is link active.</summary>
        [Obsolete("activated has been deprecated together with the class. Declare the object as NavMeshLink and use activated as before.")]
        public extern bool activated { get; set; }

        ///<summary>Is link occupied. (RO)</summary>
        [Obsolete("occupied has been deprecated together with the class. Declare the object as NavMeshLink and use occupied as before.")]
        public extern bool occupied { get; }

        ///<summary>Modify pathfinding cost for the link.</summary>
        ///<remarks>When the costOverride value is non-negative the cost of moving over the OffMeshLink
        ///is equivalent to the costOverride value times the Euclidean distance
        ///between OffMeshLink end points.</remarks>
        [Obsolete("costOverride has been deprecated together with the class. Declare the object as NavMeshLink and use costModifier instead.")]
        public extern float costOverride { get; set; }

        ///<summary>Can link be traversed in both directions.</summary>
        ///<remarks>When false the link can only be traversed from start to end.</remarks>
        [Obsolete("biDirectional has been deprecated together with the class. Declare the object as NavMeshLink and use bidirectional instead.")]
        public extern bool biDirectional { get; set; }

        ///<summary>Explicitly update the link endpoints.</summary>
        ///<remarks>Updates the OffMeshLink endpoints to match the transforms specified by the start and end transforms.</remarks>
        [Obsolete("UpdatePositions() has been deprecated together with the class. Declare the object as NavMeshLink and use UpdateLink() instead.")]
        public extern void UpdatePositions();

        ///<summary>NavMeshLayer for this OffMeshLink component.</summary>
        [Obsolete("navMeshLayer has been deprecated together with the class. Declare the object as NavMeshLink and use area instead. (UnityUpgradable) -> area")]
        public int navMeshLayer { get { return area; }  set { area = value; } }

        ///<summary>NavMesh area index for this OffMeshLink component.</summary>
        [Obsolete("area has been deprecated together with the class. Declare the object as NavMeshLink and use area as before.")]
        public extern int area { get; set; }

        ///<summary>Automatically update endpoints.</summary>
        ///<remarks>The OffMeshLink component will try to match endpoint transforms specified by <see cref="startTransform" /> and <see cref="endTransform" /> . See also <see cref="UpdatePositions" />.</remarks>
        [Obsolete("autoUpdatePositions has been deprecated together with the class. Declare the object as NavMeshLink and use autoUpdate instead.")]
        public extern bool autoUpdatePositions { get; set; }

        ///<summary>The transform representing link start position.</summary>
        [Obsolete("startTransform has been deprecated together with the class. Declare the object as NavMeshLink and use startTransform as before.")]
        public extern Transform startTransform { get; set; }

        ///<summary>The transform representing link end position.</summary>
        [Obsolete("endTransform has been deprecated together with the class. Declare the object as NavMeshLink and use endTransform as before.")]
        public extern Transform endTransform { get; set; }
    }
}
