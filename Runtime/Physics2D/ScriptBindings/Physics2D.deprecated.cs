// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
namespace UnityEngine
{
    partial class CircleCollider2D
    {
        [Obsolete("CircleCollider2D.center has been deprecated. Use CircleCollider2D.offset instead (UnityUpgradable) -> offset", true)]
        public Vector2 center { get { return Vector2.zero; } set {} }
    }

    partial class BoxCollider2D
    {
        [Obsolete("BoxCollider2D.center has been deprecated. Use BoxCollider2D.offset instead (UnityUpgradable) -> offset", true)]
        public Vector2 center { get { return Vector2.zero; } set {} }
    }

    partial class Joint2D
    {
        [Obsolete("Joint2D.collideConnected has been deprecated. Use Joint2D.enableCollision instead (UnityUpgradable) -> enableCollision", true)]
        public bool collideConnected { get { return enableCollision; } set { enableCollision = value; } }
    }

    partial class AreaEffector2D
    {
        [Obsolete("AreaEffector2D.forceDirection has been deprecated. Use AreaEffector2D.forceAngle instead (UnityUpgradable) -> forceAngle", true)]
        public float forceDirection { get { return forceAngle; } set { forceAngle = value; } }
    }

    partial class PlatformEffector2D
    {
        [Obsolete("PlatformEffector2D.oneWay has been deprecated. Use PlatformEffector2D.useOneWay instead (UnityUpgradable) -> useOneWay", true)]
        public bool oneWay { get { return useOneWay; } set { useOneWay = value; } }

        [Obsolete("PlatformEffector2D.sideFriction has been deprecated. Use PlatformEffector2D.useSideFriction instead (UnityUpgradable) -> useSideFriction", true)]
        public bool sideFriction { get { return useSideFriction; } set { useSideFriction = value; } }

        [Obsolete("PlatformEffector2D.sideBounce has been deprecated. Use PlatformEffector2D.useSideBounce instead (UnityUpgradable) -> useSideBounce", true)]
        public bool sideBounce { get { return useSideBounce; } set { useSideBounce = value; } }

        [Obsolete("PlatformEffector2D.sideAngleVariance has been deprecated. Use PlatformEffector2D.sideArc instead (UnityUpgradable) -> sideArc", true)]
        public float sideAngleVariance { get { return sideArc; } set { sideArc = value; } }
    }

    partial class Physics2D
    {
        [Obsolete("Physics2D.raycastsHitTriggers is deprecated. Use Physics2D.queriesHitTriggers instead. (UnityUpgradable) -> queriesHitTriggers", true)]
        public static bool raycastsHitTriggers { get { return false; } set {} }

        [Obsolete("Physics2D.raycastsStartInColliders is deprecated. Use Physics2D.queriesStartInColliders instead. (UnityUpgradable) -> queriesStartInColliders", true)]
        public static bool raycastsStartInColliders { get { return false; } set {} }

        [Obsolete("Physics2D.deleteStopsCallbacks is deprecated. Use Physics2D.changeStopsCallbacks instead. (UnityUpgradable) -> changeStopsCallbacks", true)]
        public static bool deleteStopsCallbacks { get { return false; } set {} }

        [Obsolete("Physics2D.minPenetrationForPenalty is deprecated. Use Physics2D.defaultContactOffset instead. (UnityUpgradable) -> defaultContactOffset", false)]
        public static float minPenetrationForPenalty { get { return defaultContactOffset; } set { defaultContactOffset = value; } }
    }
}
