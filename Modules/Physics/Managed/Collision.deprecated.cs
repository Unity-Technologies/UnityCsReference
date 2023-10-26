// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.ComponentModel;

namespace UnityEngine
{
    public partial class Collision
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Do not use Collision.GetEnumerator(), enumerate using non-allocating array returned by Collision.GetContacts() or enumerate using Collision.GetContact(index) instead.", false)]
        public virtual IEnumerator GetEnumerator()
        {
            return contacts.GetEnumerator();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use Collision.relativeVelocity instead. (UnityUpgradable) -> relativeVelocity", false)]
        public Vector3 impactForceSum { get { return Vector3.zero; } }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Will always return zero.", true)]
        public Vector3 frictionForceSum { get { return Vector3.zero; } }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Please use Collision.rigidbody, Collision.transform or Collision.collider instead", false)]
        public Component other { get { return body != null ? body : collider; } }
    }
}
