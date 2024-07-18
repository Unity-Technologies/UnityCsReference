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
        [Obsolete("OverlapCollider has been renamed to Overlap (UnityUpgradable) -> Overlap(*)", false)]
        [ExcludeFromDocs]
        public int OverlapCollider(ContactFilter2D contactFilter, [Out] Collider2D[] results) { return Overlap(contactFilter, results); }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("OverlapCollider has been renamed to Overlap (UnityUpgradable) -> Overlap(*)", false)]
        [ExcludeFromDocs]
        public int OverlapCollider(ContactFilter2D contactFilter, List<Collider2D> results) { return Overlap(contactFilter, results); }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Rigidbody2D.fixedAngle is deprecated. Use Rigidbody2D.constraints instead.", true)]
        [ExcludeFromDocs]
        public bool fixedAngle { get { return false; } set { } }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Please use Rigidbody2D.bodyType instead.", false)]
        [ExcludeFromDocs]
        public bool isKinematic { get { return bodyType == RigidbodyType2D.Kinematic; } set { bodyType = value ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic; } }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Please use Rigidbody2D.linearDamping instead. (UnityUpgradable) -> linearDamping", false)]
        [ExcludeFromDocs]
        public float drag { get => linearDamping; set => linearDamping = value; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Please use Rigidbody2D.angularDamping instead. (UnityUpgradable) -> angularDamping", false)]
        [ExcludeFromDocs]
        public float angularDrag { get => angularDamping; set => angularDamping = value; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Please use Rigidbody2D.linearVelocity instead. (UnityUpgradable) -> linearVelocity", false)]
        [ExcludeFromDocs]
        public Vector2 velocity { get => linearVelocity; set => linearVelocity = value; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Please use Rigidbody2D.linearVelocityX instead. (UnityUpgradable) -> linearVelocityX", false)]
        [ExcludeFromDocs]
        public float velocityX { get => linearVelocityX; set => linearVelocityX = value; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Please use Rigidbody2D.linearVelocityY instead. (UnityUpgradable) -> linearVelocityY", false)]
        [ExcludeFromDocs]
        public float velocityY { get => linearVelocityY; set => linearVelocityY = value; }
    }

    partial class Collider2D
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("OverlapCollider has been renamed to Overlap (UnityUpgradable) -> Overlap(*)", false)]
        [ExcludeFromDocs]
        public int OverlapCollider(ContactFilter2D contactFilter, Collider2D[] results) { return Overlap(contactFilter, results); }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("OverlapCollider has been renamed to Overlap (UnityUpgradable) -> Overlap(*)", false)]
        [ExcludeFromDocs]
        public int OverlapCollider(ContactFilter2D contactFilter, List<Collider2D> results) { return Overlap(contactFilter, results); }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("usedByComposite has been deprecated. Use Collider2D.compositeOperation instead", false)]
        [ExcludeFromDocs]
        public bool usedByComposite { get { return compositeOperation != CompositeOperation.None; } set { compositeOperation = value ? CompositeOperation.Merge : CompositeOperation.None; } }
    }

    partial class CircleCollider2D
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("CircleCollider2D.center has been deprecated. Use CircleCollider2D.offset instead (UnityUpgradable) -> offset", true)]
        public Vector2 center { get { return Vector2.zero; } set {} }
    }

    partial class BoxCollider2D
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("BoxCollider2D.center has been deprecated. Use BoxCollider2D.offset instead (UnityUpgradable) -> offset", true)]
        public Vector2 center { get { return Vector2.zero; } set {} }
    }

    partial class Joint2D
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Joint2D.collideConnected has been deprecated. Use Joint2D.enableCollision instead (UnityUpgradable) -> enableCollision", true)]
        public bool collideConnected { get { return enableCollision; } set { enableCollision = value; } }
    }

    partial class AreaEffector2D
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("AreaEffector2D.forceDirection has been deprecated. Use AreaEffector2D.forceAngle instead (UnityUpgradable) -> forceAngle", true)]
        public float forceDirection { get { return forceAngle; } set { forceAngle = value; } }
    }

    partial class PlatformEffector2D
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("PlatformEffector2D.oneWay has been deprecated. Use PlatformEffector2D.useOneWay instead (UnityUpgradable) -> useOneWay", true)]
        public bool oneWay { get { return useOneWay; } set { useOneWay = value; } }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("PlatformEffector2D.sideFriction has been deprecated. Use PlatformEffector2D.useSideFriction instead (UnityUpgradable) -> useSideFriction", true)]
        public bool sideFriction { get { return useSideFriction; } set { useSideFriction = value; } }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("PlatformEffector2D.sideBounce has been deprecated. Use PlatformEffector2D.useSideBounce instead (UnityUpgradable) -> useSideBounce", true)]
        public bool sideBounce { get { return useSideBounce; } set { useSideBounce = value; } }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("PlatformEffector2D.sideAngleVariance has been deprecated. Use PlatformEffector2D.sideArc instead (UnityUpgradable) -> sideArc", true)]
        public float sideAngleVariance { get { return sideArc; } set { sideArc = value; } }
    }

    partial class Physics2D
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.raycastsHitTriggers is deprecated. Use Physics2D.queriesHitTriggers instead. (UnityUpgradable) -> queriesHitTriggers", true)]
        public static bool raycastsHitTriggers { get { return false; } set {} }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.raycastsStartInColliders is deprecated. Use Physics2D.queriesStartInColliders instead. (UnityUpgradable) -> queriesStartInColliders", true)]
        public static bool raycastsStartInColliders { get { return false; } set {} }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.deleteStopsCallbacks is deprecated.(UnityUpgradable) -> changeStopsCallbacks", true)]
        public static bool deleteStopsCallbacks { get { return false; } set {} }

        [Obsolete("Physics2D.changeStopsCallbacks is deprecated and will always return false.", true)]
        public static bool changeStopsCallbacks { get { return false; } set {} }

        [Obsolete("Physics2D.minPenetrationForPenalty is deprecated. Use Physics2D.defaultContactOffset instead. (UnityUpgradable) -> defaultContactOffset", true)]
        public static float minPenetrationForPenalty { get { return defaultContactOffset; } set { defaultContactOffset = value; } }

        [ExcludeFromDocs]
        [Obsolete("Physics2D.velocityThreshold is deprecated. Use Physics2D.bounceThreshold instead. (UnityUpgradable) -> bounceThreshold", true)]
        public static float velocityThreshold { get { return bounceThreshold; } set { bounceThreshold = value; } }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.autoSimulation is deprecated. Use Physics2D.simulationMode instead.", true)]
        public static bool autoSimulation { get { return simulationMode != SimulationMode2D.Script; } set { simulationMode = value ? SimulationMode2D.FixedUpdate : SimulationMode2D.Script; } }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.colliderAwakeColor is deprecated. This options has been moved to 2D Preferences.", true)]
        public static Color colliderAwakeColor { get { return Color.magenta; } set { } }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.colliderAsleepColor is deprecated. This options has been moved to 2D Preferences.", true)]
        public static Color colliderAsleepColor { get { return Color.magenta; } set { } }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.colliderContactColor is deprecated. This options has been moved to 2D Preferences.", true)]
        public static Color colliderContactColor { get { return Color.magenta; } set { } }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.colliderAABBColor is deprecated. All Physics 2D colors moved to Preferences. This is now known as 'Collider Bounds Color'.", true)]
        public static Color colliderAABBColor { get { return Color.magenta; } set { } }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.contactArrowScale is deprecated. This options has been moved to 2D Preferences.", true)]
        public static float contactArrowScale { get { return 0.2f; } set { } }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.alwaysShowColliders is deprecated. It is no longer available in the Editor or Builds.", true)]
        public static bool alwaysShowColliders { get; set; }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.showCollidersFilled is deprecated. It is no longer available in the Editor or Builds.", true)]
        public static bool showCollidersFilled { get; set; }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.showColliderSleep is deprecated. It is no longer available in the Editor or Builds.", true)]
        public static bool showColliderSleep { get; set; }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.showColliderContacts is deprecated. It is no longer available in the Editor or Builds.", true)]
        public static bool showColliderContacts { get; set; }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.showColliderAABB is deprecated. It is no longer available in the Editor or Builds.", true)]
        public static bool showColliderAABB { get { return false; } set { } }

        #region Linecast

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.LinecastNonAlloc is deprecated. Use Physics2D.Linecast instead.", false)]
        public static int LinecastNonAlloc(Vector2 start, Vector2 end, RaycastHit2D[] results) { return defaultPhysicsScene.Linecast(start, end, results); }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.LinecastNonAlloc is deprecated. Use Physics2D.Linecast instead.", false)]
        public static int LinecastNonAlloc(Vector2 start, Vector2 end, RaycastHit2D[] results, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return defaultPhysicsScene.Linecast(start, end, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.LinecastNonAlloc is deprecated. Use Physics2D.Linecast instead.", false)]
        public static int LinecastNonAlloc(Vector2 start, Vector2 end, RaycastHit2D[] results, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return defaultPhysicsScene.Linecast(start, end, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.LinecastNonAlloc is deprecated. Use Physics2D.Linecast instead.", false)]
        public static int LinecastNonAlloc(Vector2 start, Vector2 end, RaycastHit2D[] results, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return defaultPhysicsScene.Linecast(start, end, contactFilter, results);
        }

        #endregion

        #region Ray Cast

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.RaycastNonAlloc is deprecated. Use Physics2D.Raycast instead.", false)]
        public static int RaycastNonAlloc(Vector2 origin, Vector2 direction, RaycastHit2D[] results)
        {
            return defaultPhysicsScene.Raycast(origin, direction, Mathf.Infinity, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.RaycastNonAlloc is deprecated. Use Physics2D.Raycast instead.", false)]
        public static int RaycastNonAlloc(Vector2 origin, Vector2 direction, RaycastHit2D[] results, float distance)
        {
            return defaultPhysicsScene.Raycast(origin, direction, distance, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.RaycastNonAlloc is deprecated. Use Physics2D.Raycast instead.", false)]
        public static int RaycastNonAlloc(Vector2 origin, Vector2 direction, RaycastHit2D[] results, float distance, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return defaultPhysicsScene.Raycast(origin, direction, distance, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.RaycastNonAlloc is deprecated. Use Physics2D.Raycast instead.", false)]
        public static int RaycastNonAlloc(Vector2 origin, Vector2 direction, RaycastHit2D[] results, [DefaultValue("Mathf.Infinity")] float distance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return defaultPhysicsScene.Raycast(origin, direction, distance, contactFilter, results);
        }

        #endregion

        #region Circle Cast

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.CircleCastNonAlloc is deprecated. Use Physics2D.CircleCast instead.", false)]
        public static int CircleCastNonAlloc(Vector2 origin, float radius, Vector2 direction, RaycastHit2D[] results)
        {
            return defaultPhysicsScene.CircleCast(origin, radius, direction, Mathf.Infinity, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.CircleCastNonAlloc is deprecated. Use Physics2D.CircleCast instead.", false)]
        public static int CircleCastNonAlloc(Vector2 origin, float radius, Vector2 direction, RaycastHit2D[] results, float distance)
        {
            return defaultPhysicsScene.CircleCast(origin, radius, direction, distance, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.CircleCastNonAlloc is deprecated. Use Physics2D.CircleCast instead.", false)]
        public static int CircleCastNonAlloc(Vector2 origin, float radius, Vector2 direction, RaycastHit2D[] results, float distance, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return defaultPhysicsScene.CircleCast(origin, radius, direction, distance, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.CircleCastNonAlloc is deprecated. Use Physics2D.CircleCast instead.", false)]
        public static int CircleCastNonAlloc(Vector2 origin, float radius, Vector2 direction, RaycastHit2D[] results, float distance, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return defaultPhysicsScene.CircleCast(origin, radius, direction, distance, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.CircleCastNonAlloc is deprecated. Use Physics2D.CircleCast instead.", false)]
        public static int CircleCastNonAlloc(Vector2 origin, float radius, Vector2 direction, RaycastHit2D[] results, [DefaultValue("Mathf.Infinity")] float distance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return defaultPhysicsScene.CircleCast(origin, radius, direction, distance, contactFilter, results);
        }

        #endregion

        #region Box Cast

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.BoxCastNonAlloc is deprecated. Use Physics2D.BoxCast instead.", false)]
        public static int BoxCastNonAlloc(Vector2 origin, Vector2 size, float angle, Vector2 direction, RaycastHit2D[] results)
        {
            return defaultPhysicsScene.BoxCast(origin, size, angle, direction, Mathf.Infinity, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.BoxCastNonAlloc is deprecated. Use Physics2D.BoxCast instead.", false)]
        public static int BoxCastNonAlloc(Vector2 origin, Vector2 size, float angle, Vector2 direction, RaycastHit2D[] results, float distance)
        {
            return defaultPhysicsScene.BoxCast(origin, size, angle, direction, distance, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.BoxCastNonAlloc is deprecated. Use Physics2D.BoxCast instead.", false)]
        public static int BoxCastNonAlloc(Vector2 origin, Vector2 size, float angle, Vector2 direction, RaycastHit2D[] results, float distance, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return defaultPhysicsScene.BoxCast(origin, size, angle, direction, distance, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.BoxCastNonAlloc is deprecated. Use Physics2D.BoxCast instead.", false)]
        public static int BoxCastNonAlloc(Vector2 origin, Vector2 size, float angle, Vector2 direction, RaycastHit2D[] results, float distance, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return defaultPhysicsScene.BoxCast(origin, size, angle, direction, distance, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.BoxCastNonAlloc is deprecated. Use Physics2D.BoxCast instead.", false)]
        public static int BoxCastNonAlloc(Vector2 origin, Vector2 size, float angle, Vector2 direction, RaycastHit2D[] results, [DefaultValue("Mathf.Infinity")] float distance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return defaultPhysicsScene.BoxCast(origin, size, angle, direction, distance, contactFilter, results);
        }

        #endregion

        #region Capsule Cast

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.CapsuleCastNonAlloc is deprecated. Use Physics2D.CapsuleCast instead.", false)]
        public static int CapsuleCastNonAlloc(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, RaycastHit2D[] results)
        {
            return defaultPhysicsScene.CapsuleCast(origin, size, capsuleDirection, angle, direction, Mathf.Infinity, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.CapsuleCastNonAlloc is deprecated. Use Physics2D.CapsuleCast instead.", false)]
        public static int CapsuleCastNonAlloc(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, RaycastHit2D[] results, float distance)
        {
            return defaultPhysicsScene.CapsuleCast(origin, size, capsuleDirection, angle, direction, distance, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.CapsuleCastNonAlloc is deprecated. Use Physics2D.CapsuleCast instead.", false)]
        public static int CapsuleCastNonAlloc(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, RaycastHit2D[] results, float distance, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return defaultPhysicsScene.CapsuleCast(origin, size, capsuleDirection, angle, direction, distance, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.CapsuleCastNonAlloc is deprecated. Use Physics2D.CapsuleCast instead.", false)]
        public static int CapsuleCastNonAlloc(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, RaycastHit2D[] results, float distance, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return defaultPhysicsScene.CapsuleCast(origin, size, capsuleDirection, angle, direction, distance, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.CapsuleCastNonAlloc is deprecated. Use Physics2D.CapsuleCast instead.", false)]
        public static int CapsuleCastNonAlloc(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, RaycastHit2D[] results, [DefaultValue("Mathf.Infinity")] float distance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return defaultPhysicsScene.CapsuleCast(origin, size, capsuleDirection, angle, direction, distance, contactFilter, results);
        }

        #endregion

        #region Ray Intersection

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.GetRayIntersectionNonAlloc is deprecated. Use Physics2D.GetRayIntersection instead.", false)]
        public static int GetRayIntersectionNonAlloc(Ray ray, RaycastHit2D[] results)
        {
            return defaultPhysicsScene.GetRayIntersection(ray, Mathf.Infinity, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.GetRayIntersectionNonAlloc is deprecated. Use Physics2D.GetRayIntersection instead.", false)]
        public static int GetRayIntersectionNonAlloc(Ray ray, RaycastHit2D[] results, float distance)
        {
            return defaultPhysicsScene.GetRayIntersection(ray, distance, results);
        }

        #endregion

        #region Overlap Point

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.OverlapPointNonAlloc is deprecated. Use Physics2D.OverlapPoint instead.", false)]
        public static int OverlapPointNonAlloc(Vector2 point, Collider2D[] results)
        {
            return defaultPhysicsScene.OverlapPoint(point, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.OverlapPointNonAlloc is deprecated. Use Physics2D.OverlapPoint instead.", false)]
        public static int OverlapPointNonAlloc(Vector2 point, Collider2D[] results, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return defaultPhysicsScene.OverlapPoint(point, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.OverlapPointNonAlloc is deprecated. Use Physics2D.OverlapPoint instead.", false)]
        public static int OverlapPointNonAlloc(Vector2 point, Collider2D[] results, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return defaultPhysicsScene.OverlapPoint(point, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.OverlapPointNonAlloc is deprecated. Use Physics2D.OverlapPoint instead.", false)]
        public static int OverlapPointNonAlloc(Vector2 point, Collider2D[] results, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return defaultPhysicsScene.OverlapPoint(point, contactFilter, results);
        }

        #endregion

        #region Overlap Circle

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.OverlapCircleNonAlloc is deprecated. Use Physics2D.OverlapCircle instead.", false)]
        public static int OverlapCircleNonAlloc(Vector2 point, float radius, Collider2D[] results)
        {
            return defaultPhysicsScene.OverlapCircle(point, radius, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.OverlapCircleNonAlloc is deprecated. Use Physics2D.OverlapCircle instead.", false)]
        public static int OverlapCircleNonAlloc(Vector2 point, float radius, Collider2D[] results, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity,  Mathf.Infinity);
            return defaultPhysicsScene.OverlapCircle(point, radius, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.OverlapCircleNonAlloc is deprecated. Use Physics2D.OverlapCircle instead.", false)]
        public static int OverlapCircleNonAlloc(Vector2 point, float radius, Collider2D[] results, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth,  Mathf.Infinity);
            return defaultPhysicsScene.OverlapCircle(point, radius, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.OverlapCircleNonAlloc is deprecated. Use Physics2D.OverlapCircle instead.", false)]
        public static int OverlapCircleNonAlloc(Vector2 point, float radius, Collider2D[] results, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return defaultPhysicsScene.OverlapCircle(point, radius, contactFilter, results);
        }

        #endregion

        #region Overlap Box

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.OverlapBoxNonAlloc is deprecated. Use Physics2D.OverlapBox instead.", false)]
        public static int OverlapBoxNonAlloc(Vector2 point, Vector2 size, float angle, Collider2D[] results)
        {
            return defaultPhysicsScene.OverlapBox(point, size, angle, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.OverlapBoxNonAlloc is deprecated. Use Physics2D.OverlapBox instead.", false)]
        public static int OverlapBoxNonAlloc(Vector2 point, Vector2 size, float angle, Collider2D[] results, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return defaultPhysicsScene.OverlapBox(point, size, angle, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.OverlapBoxNonAlloc is deprecated. Use Physics2D.OverlapBox instead.", false)]
        public static int OverlapBoxNonAlloc(Vector2 point, Vector2 size, float angle, Collider2D[] results, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return defaultPhysicsScene.OverlapBox(point, size, angle, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.OverlapBoxNonAlloc is deprecated. Use Physics2D.OverlapBox instead.", false)]
        public static int OverlapBoxNonAlloc(Vector2 point, Vector2 size, float angle, Collider2D[] results, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return defaultPhysicsScene.OverlapBox(point, size, angle, contactFilter, results);
        }

        #endregion

        #region Overlap Area

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.OverlapAreaNonAlloc is deprecated. Use Physics2D.OverlapArea instead.", false)]
        public static int OverlapAreaNonAlloc(Vector2 pointA, Vector2 pointB, Collider2D[] results)
        {
            return defaultPhysicsScene.OverlapArea(pointA, pointB, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.OverlapAreaNonAlloc is deprecated. Use Physics2D.OverlapArea instead.", false)]
        public static int OverlapAreaNonAlloc(Vector2 pointA, Vector2 pointB, Collider2D[] results, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return defaultPhysicsScene.OverlapArea(pointA, pointB, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.OverlapAreaNonAlloc is deprecated. Use Physics2D.OverlapArea instead.", false)]
        public static int OverlapAreaNonAlloc(Vector2 pointA, Vector2 pointB, Collider2D[] results, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return defaultPhysicsScene.OverlapArea(pointA, pointB, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.OverlapAreaNonAlloc is deprecated. Use Physics2D.OverlapArea instead.", false)]
        public static int OverlapAreaNonAlloc(Vector2 pointA, Vector2 pointB, Collider2D[] results, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return defaultPhysicsScene.OverlapArea(pointA, pointB, contactFilter, results);
        }

        #endregion

        #region Overlap Capsule

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.OverlapCapsuleNonAlloc is deprecated. Use Physics2D.OverlapCapsule instead.", false)]
        public static int OverlapCapsuleNonAlloc(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, Collider2D[] results)
        {
            return defaultPhysicsScene.OverlapCapsule(point, size, direction, angle, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.OverlapCapsuleNonAlloc is deprecated. Use Physics2D.OverlapCapsule instead.", false)]
        public static int OverlapCapsuleNonAlloc(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, Collider2D[] results, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return defaultPhysicsScene.OverlapCapsule(point, size, direction, angle, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.OverlapCapsuleNonAlloc is deprecated. Use Physics2D.OverlapCapsule instead.", false)]
        public static int OverlapCapsuleNonAlloc(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, Collider2D[] results, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return defaultPhysicsScene.OverlapCapsule(point, size, direction, angle, contactFilter, results);
        }

        [ExcludeFromDocs]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics2D.OverlapCapsuleNonAlloc is deprecated. Use Physics2D.OverlapCapsule instead.", false)]
        public static int OverlapCapsuleNonAlloc(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, Collider2D[] results, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return defaultPhysicsScene.OverlapCapsule(point, size, direction, angle, contactFilter, results);
        }

        #endregion
    }
}

