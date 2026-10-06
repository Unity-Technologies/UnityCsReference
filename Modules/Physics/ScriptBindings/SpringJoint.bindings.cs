// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    ///<summary>The spring joint ties together 2 rigid bodies, spring forces will be automatically applied to keep the object at the given distance.</summary>
    ///<remarks>The Spring attempts to maintain the distance it has when it starts out.
    ///So if your joint's start at a rest position where the two rigidbodies are far apart, then the joint will attempt to maintain that distance.
    ///The minDistance and maxDistance properties add on top of this implicit distance.</remarks>
    [RequireComponent(typeof(Rigidbody))]
    [NativeHeader("Modules/Physics/SpringJoint.h")]
    [NativeClass("Unity::SpringJoint", PersistentTypeId = 145)]
    public class SpringJoint : Joint
    {
        internal SpringJoint(global::UnityEngine.EntityId id) : base(id) {}
        public SpringJoint() {}
        ///<summary>The spring force used to keep the two objects together.</summary>
        extern public float spring { get; set; }
        ///<summary>The damper force used to dampen the spring force.</summary>
        extern public float damper { get; set; }
        ///<summary>The minimum distance between the bodies relative to their initial distance.</summary>
        ///<remarks>The distanced that will be maintained, will be kept between minDistance and maxDistance.
        ///Both values are relative to the distance between the center of masses when the Scene was first loaded.</remarks>
        extern public float minDistance { get; set; }
        ///<summary>The maximum distance between the bodies relative to their initial distance.</summary>
        ///<remarks>The distanced that will be maintained, will be kept between minDistance and maxDistance.
        ///Both values are relative to the distance between the center of masses when the Scene was first loaded.</remarks>
        extern public float maxDistance { get; set; }
        ///<summary>The maximum allowed error between the current spring length and the length defined by <see cref="minDistance" /> and <see cref="maxDistance" />.</summary>
        ///<remarks>If the tolerance is set very high the solver might not run and the spring would effectively not exist.
        ///
        ///If the tolerance is set close to zero very short spring lengths are possible, but at the increased cost of running the solver more often.</remarks>
        extern public float tolerance { get; set; }
    }
}
