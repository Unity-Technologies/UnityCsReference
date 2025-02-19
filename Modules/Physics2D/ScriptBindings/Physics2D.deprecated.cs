// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Internal;

namespace UnityEngine
{
    partial class Rigidbody2D
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("OverlapCollider has been deprecated. Please use Overlap (UnityUpgradable) -> Overlap(*)", false)]
        [ExcludeFromDocs]
        public int OverlapCollider(ContactFilter2D contactFilter, [Out] Collider2D[] results) => Overlap(contactFilter, results);

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("OverlapCollider has been deprecated. Please use Overlap (UnityUpgradable) -> Overlap(*)", false)]
        [ExcludeFromDocs]
        public int OverlapCollider(ContactFilter2D contactFilter, List<Collider2D> results) => Overlap(contactFilter, results);

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Rigidbody2D.fixedAngle is obsolete. Use Rigidbody2D.constraints instead.", true)]
        [ExcludeFromDocs]
        public bool fixedAngle { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("isKinematic has been deprecated. Please use bodyType.", false)]
        [ExcludeFromDocs]
        public bool isKinematic { get => bodyType == RigidbodyType2D.Kinematic; set => bodyType = value ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("drag has been deprecated. Please use linearDamping. (UnityUpgradable) -> linearDamping", false)]
        [ExcludeFromDocs]
        public float drag { get => linearDamping; set => linearDamping = value; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("angularDrag has been deprecated. Please use angularDamping. (UnityUpgradable) -> angularDamping", false)]
        [ExcludeFromDocs]
        public float angularDrag { get => angularDamping; set => angularDamping = value; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("velocity has been deprecated. Please use linearVelocity. (UnityUpgradable) -> linearVelocity", false)]
        [ExcludeFromDocs]
        public Vector2 velocity { get => linearVelocity; set => linearVelocity = value; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("velocityX has been deprecated. Please use linearVelocityX. (UnityUpgradable) -> linearVelocityX", false)]
        [ExcludeFromDocs]
        public float velocityX { get => linearVelocityX; set => linearVelocityX = value; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("velocityY has been deprecated. Please use linearVelocityY (UnityUpgradable) -> linearVelocityY", false)]
        [ExcludeFromDocs]
        public float velocityY { get => linearVelocityY; set => linearVelocityY = value; }
    }

    partial class Collider2D
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("OverlapCollider has been deprecated. Please use Overlap. (UnityUpgradable) -> Overlap(*)", false)]
        [ExcludeFromDocs]
        public int OverlapCollider(ContactFilter2D contactFilter, Collider2D[] results) => Overlap(contactFilter, results);

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("OverlapCollider has been deprecated. Please use Overlap. (UnityUpgradable) -> Overlap(*)", false)]
        [ExcludeFromDocs]
        public int OverlapCollider(ContactFilter2D contactFilter, List<Collider2D> results) => Overlap(contactFilter, results);

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("usedByComposite has been deprecated. Please use compositeOperation.", false)]
        [ExcludeFromDocs]
        public bool usedByComposite { get => compositeOperation != CompositeOperation.None; set => compositeOperation = value ? CompositeOperation.Merge : CompositeOperation.None; }
    }

    partial class CircleCollider2D
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("CircleCollider2D.center has been obsolete. Use CircleCollider2D.offset instead (UnityUpgradable) -> offset", true)]
        public Vector2 center { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    }

    partial class BoxCollider2D
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("BoxCollider2D.center has been obsolete. Use BoxCollider2D.offset instead (UnityUpgradable) -> offset", true)]
        public Vector2 center { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    }

    partial class Joint2D
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Joint2D.collideConnected has been obsolete. Use Joint2D.enableCollision instead (UnityUpgradable) -> enableCollision", true)]
        public bool collideConnected { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    }

    partial class AreaEffector2D
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("AreaEffector2D.forceDirection has been obsolete. Use AreaEffector2D.forceAngle instead (UnityUpgradable) -> forceAngle", true)]
        public float forceDirection { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("AreaEffector2D.drag has been obsolete. Use AreaEffector2D.linearDamping instead (UnityUpgradable) -> linearDamping", true)]
        public float drag { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("AreaEffector2D.angularDrag has been obsolete. Use AreaEffector2D.angularDamping instead (UnityUpgradable) -> angularDamping", true)]
        public float angularDrag { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    }

    partial class BuoyancyEffector2D
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("BuoyancyEffector2D.drag has been obsolete. Use BuoyancyEffector2D.linearDamping instead (UnityUpgradable) -> linearDamping", true)]
        public float drag { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("BuoyancyEffector2D.angularDrag has been obsolete. Use BuoyancyEffector2D.angularDamping instead (UnityUpgradable) -> angularDamping", true)]
        public float angularDrag { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    }

    partial class PointEffector2D
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("PointEffector2D.drag has been obsolete. Use PointEffector2D.linearDamping instead (UnityUpgradable) -> linearDamping", true)]
        public float drag { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("PointEffector2D.angularDrag has been obsolete. Use PointEffector2D.angularDamping instead (UnityUpgradable) -> angularDamping", true)]
        public float angularDrag { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    }

    partial class PlatformEffector2D
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("PlatformEffector2D.oneWay has been obsolete. Use PlatformEffector2D.useOneWay instead (UnityUpgradable) -> useOneWay", true)]
        public bool oneWay { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("PlatformEffector2D.sideFriction has been obsolete. Use PlatformEffector2D.useSideFriction instead (UnityUpgradable) -> useSideFriction", true)]
        public bool sideFriction { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("PlatformEffector2D.sideBounce has been obsolete. Use PlatformEffector2D.useSideBounce instead (UnityUpgradable) -> useSideBounce", true)]
        public bool sideBounce { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("PlatformEffector2D.sideAngleVariance has been obsolete. Use PlatformEffector2D.sideArc instead (UnityUpgradable) -> sideArc", true)]
        public float sideAngleVariance { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    }

    partial class Physics2D
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.raycastsHitTriggers is obsolete. Use Physics2D.queriesHitTriggers instead. (UnityUpgradable) -> queriesHitTriggers", true)]
        public static bool raycastsHitTriggers { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.raycastsStartInColliders is obsolete. Use Physics2D.queriesStartInColliders instead. (UnityUpgradable) -> queriesStartInColliders", true)]
        public static bool raycastsStartInColliders { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.deleteStopsCallbacks is obsolete.(UnityUpgradable) -> changeStopsCallbacks", true)]
        public static bool deleteStopsCallbacks { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [Obsolete("Physics2D.changeStopsCallbacks is obsolete and will always return false.", true)]
        public static bool changeStopsCallbacks { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [Obsolete("Physics2D.minPenetrationForPenalty is obsolete. Use Physics2D.defaultContactOffset instead. (UnityUpgradable) -> defaultContactOffset", true)]
        public static float minPenetrationForPenalty { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [ExcludeFromDocs]
        [Obsolete("Physics2D.velocityThreshold is obsolete. Use Physics2D.bounceThreshold instead. (UnityUpgradable) -> bounceThreshold", true)]
        public static float velocityThreshold { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.autoSimulation is obsolete. Use Physics2D.simulationMode instead.", true)]
        public static bool autoSimulation { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.colliderAwakeColor is obsolete. This options has been moved to 2D Preferences.", true)]
        public static Color colliderAwakeColor { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.colliderAsleepColor is obsolete. This options has been moved to 2D Preferences.", true)]
        public static Color colliderAsleepColor { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.colliderContactColor is obsolete. This options has been moved to 2D Preferences.", true)]
        public static Color colliderContactColor { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.colliderAABBColor is obsolete. All Physics 2D colors moved to Preferences. This is now known as 'Collider Bounds Color'.", true)]
        public static Color colliderAABBColor { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.contactArrowScale is obsolete. This options has been moved to 2D Preferences.", true)]
        public static float contactArrowScale { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.alwaysShowColliders is obsolete. It is no longer available in the Editor or Builds.", true)]
        public static bool alwaysShowColliders { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.showCollidersFilled is obsolete. It is no longer available in the Editor or Builds.", true)]
        public static bool showCollidersFilled { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.showColliderSleep is obsolete. It is no longer available in the Editor or Builds.", true)]
        public static bool showColliderSleep { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.showColliderContacts is obsolete. It is no longer available in the Editor or Builds.", true)]
        public static bool showColliderContacts { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.showColliderAABB is obsolete. It is no longer available in the Editor or Builds.", true)]
        public static bool showColliderAABB { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        #region Linecast

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("LinecastNonAlloc has neen deprecated. Please use Linecast.", false)]
        public static int LinecastNonAlloc(Vector2 start, Vector2 end, RaycastHit2D[] results) => defaultPhysicsScene.Linecast(start, end, results);

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("LinecastNonAlloc has been deprecated. Please use Linecast.", false)]
        public static int LinecastNonAlloc(Vector2 start, Vector2 end, RaycastHit2D[] results, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return defaultPhysicsScene.Linecast(start, end, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("LinecastNonAlloc has been deprecated. Please use Linecast.", false)]
        public static int LinecastNonAlloc(Vector2 start, Vector2 end, RaycastHit2D[] results, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return defaultPhysicsScene.Linecast(start, end, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("LinecastNonAlloc has been deprecated. Please use Linecast.", false)]
        public static int LinecastNonAlloc(Vector2 start, Vector2 end, RaycastHit2D[] results, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return defaultPhysicsScene.Linecast(start, end, contactFilter, results);
        }

        #endregion

        #region Ray Cast

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("RaycastNonAlloc has been deprecated. Please use Raycast.", false)]
        public static int RaycastNonAlloc(Vector2 origin, Vector2 direction, RaycastHit2D[] results) => defaultPhysicsScene.Raycast(origin, direction, Mathf.Infinity, results);

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("RaycastNonAlloc has been deprecated. Please use Raycast.", false)]
        public static int RaycastNonAlloc(Vector2 origin, Vector2 direction, RaycastHit2D[] results, float distance) => defaultPhysicsScene.Raycast(origin, direction, distance, results);

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("RaycastNonAlloc has been deprecated. Please use Raycast.", false)]
        public static int RaycastNonAlloc(Vector2 origin, Vector2 direction, RaycastHit2D[] results, float distance, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return defaultPhysicsScene.Raycast(origin, direction, distance, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("RaycastNonAlloc has been deprecated. Please use Raycast.", false)]
        public static int RaycastNonAlloc(Vector2 origin, Vector2 direction, RaycastHit2D[] results, [DefaultValue("Mathf.Infinity")] float distance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return defaultPhysicsScene.Raycast(origin, direction, distance, contactFilter, results);
        }

        #endregion

        #region Circle Cast

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("CircleCastNonAlloc has been deprecated. Please use CircleCast instead.", false)]
        public static int CircleCastNonAlloc(Vector2 origin, float radius, Vector2 direction, RaycastHit2D[] results) => defaultPhysicsScene.CircleCast(origin, radius, direction, Mathf.Infinity, results);

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("CircleCastNonAlloc has been deprecated. Please use CircleCast instead.", false)]
        public static int CircleCastNonAlloc(Vector2 origin, float radius, Vector2 direction, RaycastHit2D[] results, float distance) => defaultPhysicsScene.CircleCast(origin, radius, direction, distance, results);

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("CircleCastNonAlloc has been deprecated. Please use CircleCast instead.", false)]
        public static int CircleCastNonAlloc(Vector2 origin, float radius, Vector2 direction, RaycastHit2D[] results, float distance, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return defaultPhysicsScene.CircleCast(origin, radius, direction, distance, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("CircleCastNonAlloc has been deprecated. Please use CircleCast instead.", false)]
        public static int CircleCastNonAlloc(Vector2 origin, float radius, Vector2 direction, RaycastHit2D[] results, float distance, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return defaultPhysicsScene.CircleCast(origin, radius, direction, distance, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("CircleCastNonAlloc has been deprecated. Please use CircleCast instead.", false)]
        public static int CircleCastNonAlloc(Vector2 origin, float radius, Vector2 direction, RaycastHit2D[] results, [DefaultValue("Mathf.Infinity")] float distance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return defaultPhysicsScene.CircleCast(origin, radius, direction, distance, contactFilter, results);
        }

        #endregion

        #region Box Cast

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("BoxCastNonAlloc has been deprecated. Please use BoxCast.", false)]
        public static int BoxCastNonAlloc(Vector2 origin, Vector2 size, float angle, Vector2 direction, RaycastHit2D[] results) => defaultPhysicsScene.BoxCast(origin, size, angle, direction, Mathf.Infinity, results);

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("BoxCastNonAlloc has been deprecated. Please use BoxCast.", false)]
        public static int BoxCastNonAlloc(Vector2 origin, Vector2 size, float angle, Vector2 direction, RaycastHit2D[] results, float distance) => defaultPhysicsScene.BoxCast(origin, size, angle, direction, distance, results);

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("BoxCastNonAlloc has been deprecated. Please use BoxCast.", false)]
        public static int BoxCastNonAlloc(Vector2 origin, Vector2 size, float angle, Vector2 direction, RaycastHit2D[] results, float distance, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return defaultPhysicsScene.BoxCast(origin, size, angle, direction, distance, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("BoxCastNonAlloc has been deprecated. Please use BoxCast.", false)]
        public static int BoxCastNonAlloc(Vector2 origin, Vector2 size, float angle, Vector2 direction, RaycastHit2D[] results, float distance, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return defaultPhysicsScene.BoxCast(origin, size, angle, direction, distance, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("BoxCastNonAlloc has been deprecated. Please use BoxCast.", false)]
        public static int BoxCastNonAlloc(Vector2 origin, Vector2 size, float angle, Vector2 direction, RaycastHit2D[] results, [DefaultValue("Mathf.Infinity")] float distance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return defaultPhysicsScene.BoxCast(origin, size, angle, direction, distance, contactFilter, results);
        }

        #endregion

        #region Capsule Cast

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("CapsuleCastNonAlloc has been deprecated. Please use CapsuleCast.", false)]
        public static int CapsuleCastNonAlloc(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, RaycastHit2D[] results) => defaultPhysicsScene.CapsuleCast(origin, size, capsuleDirection, angle, direction, Mathf.Infinity, results);

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("CapsuleCastNonAlloc has been deprecated. Please use CapsuleCast.", false)]
        public static int CapsuleCastNonAlloc(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, RaycastHit2D[] results, float distance) => defaultPhysicsScene.CapsuleCast(origin, size, capsuleDirection, angle, direction, distance, results);

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("CapsuleCastNonAlloc has been deprecated. Please use CapsuleCast.", false)]
        public static int CapsuleCastNonAlloc(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, RaycastHit2D[] results, float distance, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return defaultPhysicsScene.CapsuleCast(origin, size, capsuleDirection, angle, direction, distance, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("CapsuleCastNonAlloc has been deprecated. Please use CapsuleCast.", false)]
        public static int CapsuleCastNonAlloc(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, RaycastHit2D[] results, float distance, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return defaultPhysicsScene.CapsuleCast(origin, size, capsuleDirection, angle, direction, distance, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("CapsuleCastNonAlloc has been deprecated. Please use CapsuleCast.", false)]
        public static int CapsuleCastNonAlloc(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, RaycastHit2D[] results, [DefaultValue("Mathf.Infinity")] float distance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return defaultPhysicsScene.CapsuleCast(origin, size, capsuleDirection, angle, direction, distance, contactFilter, results);
        }

        #endregion

        #region Ray Intersection

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("GetRayIntersectionNonAlloc is deprecated. Please use GetRayIntersection.", false)]
        public static int GetRayIntersectionNonAlloc(Ray ray, RaycastHit2D[] results) => defaultPhysicsScene.GetRayIntersection(ray, Mathf.Infinity, results);

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("GetRayIntersectionNonAlloc is deprecated. Please use GetRayIntersection.", false)]
        public static int GetRayIntersectionNonAlloc(Ray ray, RaycastHit2D[] results, float distance) => defaultPhysicsScene.GetRayIntersection(ray, distance, results);

        #endregion

        #region Overlap Point

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("OverlapPointNonAlloc has been deprecated. Please use OverlapPoint.", false)]
        public static int OverlapPointNonAlloc(Vector2 point, Collider2D[] results) => defaultPhysicsScene.OverlapPoint(point, results);

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("OverlapPointNonAlloc has been deprecated. Please use OverlapPoint.", false)]
        public static int OverlapPointNonAlloc(Vector2 point, Collider2D[] results, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return defaultPhysicsScene.OverlapPoint(point, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("OverlapPointNonAlloc has been deprecated. Please use OverlapPoint.", false)]
        public static int OverlapPointNonAlloc(Vector2 point, Collider2D[] results, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return defaultPhysicsScene.OverlapPoint(point, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("OverlapPointNonAlloc has been deprecated. Please use OverlapPoint.", false)]
        public static int OverlapPointNonAlloc(Vector2 point, Collider2D[] results, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return defaultPhysicsScene.OverlapPoint(point, contactFilter, results);
        }

        #endregion

        #region Overlap Circle

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("OverlapCircleNonAlloc has been deprecated. Please use OverlapCircle.", false)]
        public static int OverlapCircleNonAlloc(Vector2 point, float radius, Collider2D[] results) => defaultPhysicsScene.OverlapCircle(point, radius, results);

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("OverlapCircleNonAlloc has been deprecated. Please use OverlapCircle.", false)]
        public static int OverlapCircleNonAlloc(Vector2 point, float radius, Collider2D[] results, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity,  Mathf.Infinity);
            return defaultPhysicsScene.OverlapCircle(point, radius, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("OverlapCircleNonAlloc has been deprecated. Please use OverlapCircle.", false)]
        public static int OverlapCircleNonAlloc(Vector2 point, float radius, Collider2D[] results, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth,  Mathf.Infinity);
            return defaultPhysicsScene.OverlapCircle(point, radius, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("OverlapCircleNonAlloc has been deprecated. Please use OverlapCircle.", false)]
        public static int OverlapCircleNonAlloc(Vector2 point, float radius, Collider2D[] results, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return defaultPhysicsScene.OverlapCircle(point, radius, contactFilter, results);
        }

        #endregion

        #region Overlap Box

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("OverlapBoxNonAlloc has been deprecated. Please use OverlapBox.", false)]
        public static int OverlapBoxNonAlloc(Vector2 point, Vector2 size, float angle, Collider2D[] results) => defaultPhysicsScene.OverlapBox(point, size, angle, results);

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("OverlapBoxNonAlloc has been deprecated. Please use OverlapBox.", false)]
        public static int OverlapBoxNonAlloc(Vector2 point, Vector2 size, float angle, Collider2D[] results, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return defaultPhysicsScene.OverlapBox(point, size, angle, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("OverlapBoxNonAlloc has been deprecated. Please use OverlapBox.", false)]
        public static int OverlapBoxNonAlloc(Vector2 point, Vector2 size, float angle, Collider2D[] results, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return defaultPhysicsScene.OverlapBox(point, size, angle, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("OverlapBoxNonAlloc has been deprecated. Please use OverlapBox.", false)]
        public static int OverlapBoxNonAlloc(Vector2 point, Vector2 size, float angle, Collider2D[] results, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return defaultPhysicsScene.OverlapBox(point, size, angle, contactFilter, results);
        }

        #endregion

        #region Overlap Area

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("OverlapAreaNonAlloc has been deprecated. Please use OverlapArea.", false)]
        public static int OverlapAreaNonAlloc(Vector2 pointA, Vector2 pointB, Collider2D[] results) => defaultPhysicsScene.OverlapArea(pointA, pointB, results);

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("OverlapAreaNonAlloc has been deprecated. Please use OverlapArea.", false)]
        public static int OverlapAreaNonAlloc(Vector2 pointA, Vector2 pointB, Collider2D[] results, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return defaultPhysicsScene.OverlapArea(pointA, pointB, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("OverlapAreaNonAlloc has been deprecated. Please use OverlapArea.", false)]
        public static int OverlapAreaNonAlloc(Vector2 pointA, Vector2 pointB, Collider2D[] results, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return defaultPhysicsScene.OverlapArea(pointA, pointB, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("OverlapAreaNonAlloc has been deprecated. Please use OverlapArea.", false)]
        public static int OverlapAreaNonAlloc(Vector2 pointA, Vector2 pointB, Collider2D[] results, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return defaultPhysicsScene.OverlapArea(pointA, pointB, contactFilter, results);
        }

        #endregion

        #region Overlap Capsule

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("OverlapCapsuleNonAlloc has been deprecated. Please use OverlapCapsule.", false)]
        public static int OverlapCapsuleNonAlloc(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, Collider2D[] results) => defaultPhysicsScene.OverlapCapsule(point, size, direction, angle, results);

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("OverlapCapsuleNonAlloc has been deprecated. Please use OverlapCapsule.", false)]
        public static int OverlapCapsuleNonAlloc(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, Collider2D[] results, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return defaultPhysicsScene.OverlapCapsule(point, size, direction, angle, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("OverlapCapsuleNonAlloc has been deprecated. Please use OverlapCapsule.", false)]
        public static int OverlapCapsuleNonAlloc(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, Collider2D[] results, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return defaultPhysicsScene.OverlapCapsule(point, size, direction, angle, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("OverlapCapsuleNonAlloc has been deprecated. Please use OverlapCapsule.", false)]
        public static int OverlapCapsuleNonAlloc(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, Collider2D[] results, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return defaultPhysicsScene.OverlapCapsule(point, size, direction, angle, contactFilter, results);
        }

        #endregion
    }

    partial struct ContactFilter2D
    {
        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("ContactFilter2D.NoFilter method has been deprecated. Please use the static ContactFilter2D.noFilter property.", false)]
        public ContactFilter2D NoFilter() => noFilter;
    }
}

