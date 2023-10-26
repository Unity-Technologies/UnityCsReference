// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    public readonly partial struct ContactPairHeader
    {
        [Obsolete("Please use ContactPairHeader.bodyInstanceID instead. (UnityUpgradable) -> bodyInstanceID", false)]
        public int BodyInstanceID => bodyInstanceID;
        
        [Obsolete("Please use ContactPairHeader.otherBodyInstanceID instead. (UnityUpgradable) -> otherBodyInstanceID", false)]
        public int OtherBodyInstanceID => otherBodyInstanceID;

        [Obsolete("Please use ContactPairHeader.body instead. (UnityUpgradable) -> body", false)]
        public Component Body => body;

        [Obsolete("Please use ContactPairHeader.otherBody instead. (UnityUpgradable) -> otherBody", false)]
        public Component OtherBody => otherBody;
        
        [Obsolete("Please use ContactPairHeader.pairCount instead. (UnityUpgradable) -> pairCount", false)]
        public int PairCount => pairCount;
    }

    public unsafe readonly partial struct ContactPair
    {
        [Obsolete("Please use ContactPair.colliderInstanceID instead. (UnityUpgradable) -> colliderInstanceID", false)]
        public int ColliderInstanceID => colliderInstanceID;

        [Obsolete("Please use ContactPair.otherColliderInstanceID instead. (UnityUpgradable) -> otherColliderInstanceID", false)]
        public int OtherColliderInstanceID => otherColliderInstanceID;

        [Obsolete("Please use ContactPair.collider instead. (UnityUpgradable) -> collider", false)]
        public Collider Collider => collider;

        [Obsolete("Please use ContactPair.otherCollider instead. (UnityUpgradable) -> otherCollider", false)]
        public Collider OtherCollider => otherCollider;

        [Obsolete("Please use ContactPair.contactCount instead. (UnityUpgradable) -> contactCount", false)]
        public int ContactCount => contactCount;

        [Obsolete("Please use ContactPair.impulseSum instead. (UnityUpgradable) -> impulseSum", false)]
        public Vector3 ImpulseSum => impulseSum;

        [Obsolete("Please use ContactPair.isCollisionEnter instead. (UnityUpgradable) -> isCollisionEnter", false)]
        public bool IsCollisionEnter => isCollisionEnter;

        [Obsolete("Please use ContactPair.isCollisionExit instead. (UnityUpgradable) -> isCollisionExit", false)]
        public bool IsCollisionExit => isCollisionExit;

        [Obsolete("Please use ContactPair.isCollisionStay instead. (UnityUpgradable) -> isCollisionStay", false)]
        public bool IsCollisionStay => isCollisionStay;
    }

    public readonly partial struct ContactPairPoint
    {
        [Obsolete("Please use ContactPairPoint.position instead. (UnityUpgradable) -> position", false)]
        public Vector3 Position => position;

        [Obsolete("Please use ContactPairPoint.separation instead. (UnityUpgradable) -> separation", false)]
        public float Separation => separation;

        [Obsolete("Please use ContactPairPoint.normal instead. (UnityUpgradable) -> normal", false)]
        public Vector3 Normal => normal;

        [Obsolete("Please use ContactPairPoint.impulse instead. (UnityUpgradable) -> impulse", false)]
        public Vector3 Impulse => impulse;
    }
}
