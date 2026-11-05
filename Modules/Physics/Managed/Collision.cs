// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine
{
    ///<summary>Describes a contact point where the collision occurs.</summary>
    ///<remarks>Contact points are stored in <see cref="Collision" /> structure.</remarks>
    ///<seealso cref="Collision" />
    ///<seealso cref="M:UnityEngine.MonoBehaviour.OnCollisionEnter(UnityEngine.Collision)" />
    ///<seealso cref="M:UnityEngine.MonoBehaviour.OnCollisionStay(UnityEngine.Collision)" />
    ///<seealso cref="M:UnityEngine.MonoBehaviour.OnCollisionExit(UnityEngine.Collision)" />
    public struct ContactPoint
    {
        internal Vector3 m_Point;
        internal Vector3 m_Normal;
        internal Vector3 m_Impulse;
        internal EntityId m_ThisColliderEntityId;
        internal EntityId m_OtherColliderEntityId;
        internal float m_Separation;

        ///<summary>The point of contact.</summary>
        ///<remarks>The point of contact in world space where the collision contact occurred.
        ///                    This represents the point on the surface of the collider where the contact was detected. The value is expressed in world space, meaning it is relative to the global coordinate system of the scene, not the local space of either colliding object.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Print how many points are colliding this transform
        ///    // And print the first point that is colliding.
        ///    void OnCollisionEnter(Collision other)
        ///    {
        ///        print("Points colliding: " + other.contacts.Length);
        ///        print("First point that collided: " + other.contacts[0].point);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Vector3 point { get { return m_Point; } }
        ///<summary>Normal of the contact point.</summary>
        ///<remarks>The following example will draw a line to represent every normal from a collision. Each line will be drawn in the Scene view.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void OnCollisionEnter(Collision other)
        ///    {
        ///        // Print how many points are colliding with this transform
        ///        Debug.Log("Points colliding: " + other.contacts.Length);
        ///
        ///        // Print the normal of the first point in the collision.
        ///        Debug.Log("Normal of the first point: " + other.contacts[0].normal);
        ///
        ///        // Draw a different colored ray for every normal in the collision
        ///        foreach (var item in other.contacts)
        ///        {
        ///            Debug.DrawRay(item.point, item.normal * 100, Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f), 10f);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Vector3 normal { get { return m_Normal; } }
        ///<summary>The impulse applied to this contact pair to resolve the collision.</summary>
        ///<remarks>To work out the force applied you can divide the impulse by the last frame's fixedDeltaTime.</remarks>
        public Vector3 impulse { get { return m_Impulse; } }

        ///<summary>The first collider in contact at the point.</summary>
        public Collider thisCollider { get { return Physics.GetColliderByEntityId(m_ThisColliderEntityId); } }
        ///<summary>The other collider in contact at the point.</summary>
        public Collider otherCollider { get { return Physics.GetColliderByEntityId(m_OtherColliderEntityId); } }
        ///<summary>The distance between the colliders at the contact point.</summary>
        ///<remarks>This value represents how far apart or interpenetrated the two colliders are at the time the contact was registered:
        ///                    •    A positive separation means the colliders are close but not touching, and the contact was generated in anticipation of a possible collision (due to contact offset thresholds).
        ///                    •    A zero separation means the colliders are just touching, with their surfaces in contact but not overlapping.
        ///                    •    A negative separation indicates that the colliders are overlapping — the more negative the value, the deeper the penetration at that point.
        ///
        ///                This property is useful for examining how close colliders are, measuring contact depth in overlaps, or fine-tuning collision responses. During the lifetime of a collision, the separation may fluctuate due to simulation corrections, contact offset values, or changes in relative motion.</remarks>
        public float separation { get { return m_Separation; } }

        internal ContactPoint(Vector3 point, Vector3 normal, Vector3 impulse, float separation, EntityId thisEntityId, EntityId otherEntityId)
        {
            m_Point = point;
            m_Normal = normal;
            m_Impulse = impulse;
            m_Separation = separation;
            m_ThisColliderEntityId = thisEntityId;
            m_OtherColliderEntityId = otherEntityId;
        }
    }

    ///<summary>Describes a collision.</summary>
    ///<remarks>Collision information is passed to <see cref="M:UnityEngine.MonoBehaviour.OnCollisionEnter(UnityEngine.Collision)" />, <see cref="M:UnityEngine.MonoBehaviour.OnCollisionStay(UnityEngine.Collision)" /> and <see cref="M:UnityEngine.MonoBehaviour.OnCollisionExit(UnityEngine.Collision)" /> events.
    ///
    ///**Note**: The contact points are in world-space.</remarks>
    ///<seealso cref="ContactPoint" />
    public partial class Collision
    {
        private ContactPairHeader m_Header;
        private ContactPair m_Pair;
        private bool m_Flipped;
        private ContactPoint[] m_LegacyContacts = null;

        ///<summary>The total impulse applied to this contact pair to resolve the collision.</summary>
        ///<remarks>The total impulse is obtained by summing up impulses applied at all contact points in this collision pair. To work out the total force applied you can divide the total impulse by the last frame's fixedDeltaTime.</remarks>
        public Vector3 impulse => m_Pair.impulseSum;
        ///<summary>The relative linear velocity of the two colliding objects (RO).</summary>
        ///<example>
        ///  <code><![CDATA[
        /// // Play a sound when we hit an object with a big velocity
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    AudioSource audioSource;
        ///
        ///    void Start()
        ///    {
        ///        audioSource = GetComponent<AudioSource>();
        ///    }
        ///
        ///    void OnCollisionEnter(Collision collision)
        ///    {
        ///        if (collision.relativeVelocity.magnitude > 2)
        ///            audioSource.Play();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Vector3 relativeVelocity => m_Flipped ? m_Header.bodyLinearVelocity - m_Header.otherBodyLinearVelocity : m_Header.otherBodyLinearVelocity - m_Header.bodyLinearVelocity;
        ///<summary>The <see cref="Rigidbody" /> of the collider of the GameObject which received the <see cref="Collision" /> event (RO).</summary>
        ///<remarks>The <see cref="Rigidbody" /> of the collider of the GameObject which received the <see cref="Collision" /> event. If there is no Rigidbody component attached, this returns <c>null</c>.</remarks>
        public Rigidbody thisRigidbody => thisBody as Rigidbody;
        ///<summary>The <see cref="Rigidbody" /> we hit (RO). This is <c>null</c> if the object we hit is a collider with no rigidbody attached.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Make all rigidbodies we touch fly upwards
        ///    void OnCollisionStay(Collision collision)
        ///    {
        ///        // Check if the collider we hit has a rigidbody
        ///        // Then apply the force
        ///        if (collision.rigidbody)
        ///        {
        ///            collision.rigidbody.AddForce(Vector3.up * 15);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Rigidbody rigidbody => body as Rigidbody;
        ///<summary>The <see cref="ArticulationBody" /> of the collider of the GameObject which received the <see cref="Collision" /> event (RO).</summary>
        ///<remarks>The <see cref="ArticulationBody" /> of the collider of the GameObject which received the <see cref="Collision" /> event. If there is no articulation body attached, this returns <c>null</c>.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        /// // Make all articulation bodies that your GameObject hits move to the left```
        ///    void OnCollisionStay(Collision collision)
        ///    {
        ///        // Check if the collider your GameObject hits has an articulation body
        ///        // Then apply the force
        ///        if (collision.thisArticulationBody)
        ///        {
        ///            collision.thisArticulationBody.AddForce(Vector3.left * 15);
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        public ArticulationBody thisArticulationBody => thisBody as ArticulationBody;
        ///<summary>The <see cref="ArticulationBody" /> of the collider that your GameObject collides with (RO).</summary>
        ///<remarks>This returns the <see cref="ArticulationBody" /> of the collider that your GameObject collides with. If the collider doesn't have an articulation body attached, this returns <c>null</c>.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Make all articulation bodies that your GameObject hits fly upwards
        ///    void OnCollisionStay(Collision collision)
        ///    {
        ///        // Check if the collider your GameObject hits has an articulation body
        ///        // Then apply the force
        ///        if (collision.articulationBody)
        ///        {
        ///            collision.articulationBody.AddForce(Vector3.up * 15);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public ArticulationBody articulationBody => body as ArticulationBody;
        ///<summary>The <see cref="Rigidbody" /> or <see cref="ArticulationBody" /> of the collider that received the <see cref="Collision" /> event (RO).</summary>
        ///<remarks>The <see cref="Rigidbody" /> or <see cref="ArticulationBody" /> of the collider that received the <see cref="Collision" /> event. If the collider doesn't have a rigid or articulation body attached, this returns <c>null</c>.</remarks>
        public Component thisBody => m_Flipped ? m_Header.otherBody : m_Header.body;
        ///<summary>The <see cref="Rigidbody" /> or <see cref="ArticulationBody" /> of the collider that your <see cref="Component" /> collides with (RO).</summary>
        ///<remarks>This returns the <see cref="Rigidbody" /> or <see cref="ArticulationBody" /> of the collider that your <see cref="Component" /> collides with. If the collider doesn't have a rigid or articulation body attached, this returns <c>null</c>.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Print out which type of [[Component]] is attached to the collider we hit
        ///    void OnCollisionStay(Collision collision)
        ///    {
        ///        // Check if the collider your GameObject hits has a rigidbody
        ///        if (collision.body as Rigidbody)
        ///        {
        ///            Debug.Log("Has Rigidbody.");
        ///        }
        ///
        ///        // Check if the collider your GameObject hits has an articulation body
        ///        if (collision.body as ArticulationBody)
        ///        {
        ///            Debug.Log("Has ArticulationBody.");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Component body => m_Flipped ? m_Header.body : m_Header.otherBody;
        ///<summary>The <see cref="Collider" /> that received the <see cref="Collision" /> event (RO).</summary>
        public Collider thisCollider => m_Flipped ? m_Pair.otherCollider : m_Pair.collider;
        ///<summary>The <see cref="Collider" /> we hit (RO).</summary>
        ///<remarks>Fetch the Collider of the GameObject your GameObject hits.
        ///To find all colliders that were hit in detail you have to iterate the contact points (<see cref="contacts" /> property).</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// //In this example, the name of the GameObject that collides with your GameObject is output to the console. Then this checks the name of the Collider and if it matches with the one you specify, it outputs another message.
        ///
        /// //Create a GameObject and make sure it has a Collider component. Attach this script to it.
        /// //Create a second GameObject with a Collider and place it on top of the other GameObject to output that there was a collision. You can add movement to the GameObject to test more.
        ///
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    //If your GameObject starts to collide with another GameObject with a Collider
        ///    void OnCollisionEnter(Collision collision)
        ///    {
        ///        //Output the Collider's GameObject's name
        ///        Debug.Log(collision.collider.name);
        ///    }
        ///
        ///    //If your GameObject keeps colliding with another GameObject with a Collider, do something
        ///    void OnCollisionStay(Collision collision)
        ///    {
        ///        //Check to see if the Collider's name is "Chest"
        ///        if (collision.collider.name == "Chest")
        ///        {
        ///            //Output the message
        ///            Debug.Log("Chest is here!");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Collider collider => m_Flipped ? m_Pair.collider : m_Pair.otherCollider;
        ///<summary>The linear velocity of the <see cref="Rigidbody" /> or <see cref="ArticulationBody" /> belonging to the collider of the <see cref="Component" /> that received the <see cref="Collision" /> event (RO).</summary>
        ///<remarks>The linear velocity of the <see cref="Rigidbody" /> or <see cref="ArticulationBody" /> belonging to the collider of the <see cref="Component" /> that received the <see cref="Collision" /> event. The returned velocity is the one recorded at the moment of collision rather than the current one.  If the collider doesn't have a rigid or articulation body attached, this returns <see cref="Vector3.zero" />.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnCollisionEnter(Collision collision)
        ///    {
        ///         // Print the linear velocity of our body during collision.
        ///	 Debug.Log($"Collided with: {(collision.thisBody ?? collision.thisCollider).gameObject.name}, linear velocity at collision time: {collision.thisLinearVelocity}.");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Vector3 thisLinearVelocity => m_Flipped ? m_Header.otherBodyLinearVelocity : m_Header.bodyLinearVelocity;
        ///<summary>The linear velocity of the <see cref="Rigidbody" /> or <see cref="ArticulationBody" /> belonging to the collider that your <see cref="Component" /> collides with (RO).</summary>
        ///<remarks>The linear velocity of the <see cref="Rigidbody" /> or <see cref="ArticulationBody" /> belonging to the collider that your <see cref="Component" /> collides with. The returned velocity is the one recorded at the moment of collision rather than the current one.  If the collider doesn't have a rigid or articulation body attached, this returns <see cref="Vector3.zero" />.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnCollisionEnter(Collision collision)
        ///    {
        ///        // Print the linear velocity of the body we've collided with.
        ///		Debug.Log($"Collided with: {(collision.body ?? collision.collider).gameObject.name}, linear velocity at collision time {collision.linearVelocity}.");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Vector3 linearVelocity => m_Flipped ? m_Header.bodyLinearVelocity : m_Header.otherBodyLinearVelocity;
        ///<summary>The angular velocity of the <see cref="Rigidbody" /> or <see cref="ArticulationBody" /> belonging to the collider of the <see cref="Component" /> that received the collision event (RO).</summary>
        ///<remarks>The angular velocity of the <see cref="Rigidbody" /> or <see cref="ArticulationBody" /> belonging to the collider of the <see cref="Component" /> that received the collision event. The returned velocity is the one recorded at the moment of collision rather than the current one.  If the collider doesn't have a rigid or articulation body attached, this returns <see cref="Vector3.zero" />.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnCollisionEnter(Collision collision)
        ///    {
        ///         // Print the angular velocity of our body during collision.
        ///	 Debug.Log($"Collided with: {(collision.thisBody ?? collision.thisCollider).gameObject.name}, angular velocity at collision time: {collision.thisAngularVelocity}.");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Vector3 thisAngularVelocity => m_Flipped ? m_Header.otherBodyAngularVelocity : m_Header.bodyAngularVelocity;
        ///<summary>The angular velocity of the <see cref="Rigidbody" /> or <see cref="ArticulationBody" /> that belong to the collider that your <see cref="Component" /> collides with (RO).</summary>
        ///<remarks>The angular velocity of the <see cref="Rigidbody" /> or <see cref="ArticulationBody" /> that belong to the collider that your <see cref="Component" /> collides with. The returned velocity is the one recorded at the moment of collision rather than the current velocity. If the collider doesn't have a <see cref="Rigidbody" /> or <see cref="ArticulationBody" /> attached, this returns <see cref="Vector3.zero" />.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnCollisionEnter(Collision collision)
        ///    {
        ///        // Print the angular velocity of the body we've collided with.
        ///		Debug.Log($"Collided with: {(collision.body ?? collision.collider).gameObject.name}, angular velocity at collision time {collision.angularVelocity}.");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Vector3 angularVelocity => m_Flipped ? m_Header.bodyAngularVelocity : m_Header.otherBodyAngularVelocity;
        ///<summary>The <see cref="Transform" /> of the <see cref="GameObject" /> that received the <see cref="Collision" /> event (RO).</summary>
        ///<remarks>If a <see cref="Rigidbody" /> was attached to the <see cref="Collider" /> that belongs to the <see cref="GameObject" /> that received the <see cref="Collision" /> event, the transform is the transform attached to the <see cref="Rigidbody" />.
        ///If a <see cref="Rigidbody" /> was not attached to the <see cref="Collider" /> that belongs to the <see cref="GameObject" /> that received the collision event, the transform will be the transform attached to the collider.</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// // Attach this script to a GameObject with a Collider and Rigidbody.
        /// // Make sure the object you’re colliding with also has a Collider (and optionally a Rigidbody).
        ///
        ///using UnityEngine;
        ///
        ///public class CollisionLogger : MonoBehaviour
        ///{
        ///    private void OnCollisionEnter(Collision collision)
        ///    {
        ///        Transform ourTransform = collision.thisTransform;
        ///
        ///        // Log the position, rotation, and scale of the hit object
        ///        Debug.Log("Collision Event On: " + ourTransform.name);
        ///        Debug.Log("Position: " + ourTransform.position);
        ///        Debug.Log("Rotation: " + ourTransform.rotation);
        ///        Debug.Log("Scale: " + ourTransform.localScale);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Transform thisTransform { get { return (thisBody ?? thisCollider).transform; } }
        ///<summary>The <see cref="Transform" /> of the object we hit (RO).</summary>
        ///<remarks>If we collided against a collider with a <see cref="Rigidbody" />, the transform will be the transform attached to the rigidbody.
        ///If we collided against a collider without a rigidbody, the transform will be the transform attached to the collider.</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// // Attach this script to a GameObject with a Collider and Rigidbody.
        /// // Make sure the object you’re colliding with also has a Collider (and optionally a Rigidbody).
        ///
        ///using UnityEngine;
        ///
        ///public class CollisionLogger : MonoBehaviour
        ///{
        ///    private void OnCollisionEnter(Collision collision)
        ///    {
        ///        // Get the transform of the object we collided with
        ///        Transform hitTransform = collision.transform;
        ///
        ///        // Log the position, rotation, and scale of the hit object
        ///        Debug.Log("Collided with: " + hitTransform.name);
        ///        Debug.Log("Position: " + hitTransform.position);
        ///        Debug.Log("Rotation: " + hitTransform.rotation);
        ///        Debug.Log("Scale: " + hitTransform.localScale);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Transform transform { get { return (body ?? collider).transform; } }
        ///<summary>The <see cref="GameObject" /> that received the <see cref="Collision" /> event (RO).</summary>
        public GameObject thisGameObject { get { return (thisBody ?? thisCollider).gameObject; } }
        ///<summary>The <see cref="GameObject" /> whose collider you are colliding with. (RO).</summary>
        ///<remarks>This is the GameObject that is colliding with your GameObject. Access this to check properties of the colliding GameObject, for example, the GameObject’s name and tag.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class CollisionGameObjectExample : MonoBehaviour
        ///{
        ///    //Detect collisions between the GameObjects with Colliders attached
        ///    void OnCollisionEnter(Collision collision)
        ///    {
        ///        //Check for a match with the specified name on any GameObject that collides with your GameObject
        ///        if (collision.gameObject.name == "MyGameObjectName")
        ///        {
        ///            //If the GameObject's name matches the one you suggest, output this message in the console
        ///            Debug.Log("Do something here");
        ///        }
        ///
        ///        //Check for a match with the specific tag on any GameObject that collides with your GameObject
        ///        if (collision.gameObject.tag == "MyGameObjectTag")
        ///        {
        ///            //If the GameObject has the same tag as specified, output this message in the console
        ///            Debug.Log("Do something else here");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GameObject gameObject { get { return (body ?? collider).gameObject; } }
        internal bool Flipped { get { return m_Flipped; } set { m_Flipped = value; } }

        ///<summary>Gets the number of contacts for this collision.</summary>
        ///<remarks>A collision can involve multiple contact points. This property allows you to determine how many are involved. You can use this value when retrieving contacts using <see cref="GetContact" /> or <see cref="GetContacts" />.</remarks>
        public int contactCount { get { return (int)m_Pair.m_NbPoints; } }

        // The contact points generated by the physics engine.
        ///<summary>The contact points generated by the physics engine. You should avoid using this as it produces memory garbage. Use <see cref="GetContact" /> or <see cref="GetContacts" /> instead.</summary>
        ///<remarks>Every contact contains a contact point, normal and the two colliders that collided (see <see cref="ContactPoint" />).
        ///From inside <see cref="M:UnityEngine.MonoBehaviour.OnCollisionStay(UnityEngine.Collision)" /> or <see cref="M:UnityEngine.MonoBehaviour.OnCollisionEnter(UnityEngine.Collision)" /> you can
        ///always be sure that <c>contacts</c> has at least one element.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnCollisionStay(Collision collision)
        ///    {
        ///        foreach (ContactPoint contact in collision.contacts)
        ///        {
        ///            print(contact.thisCollider.name + " hit " + contact.otherCollider.name);
        ///            // Visualize the contact point
        ///            Debug.DrawRay(contact.point, contact.normal, Color.white);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        /// // A grenade
        /// // - instantiates an explosion Prefab when hitting a surface
        /// // - then destroys itself
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Transform explosionPrefab;
        ///    void OnCollisionEnter(Collision collision)
        ///    {
        ///        ContactPoint contact = collision.contacts[0];
        ///        Quaternion rot = Quaternion.FromToRotation(Vector3.up, contact.normal);
        ///        Vector3 pos = contact.point;
        ///        Instantiate(explosionPrefab, pos, rot);
        ///        Destroy(gameObject);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public ContactPoint[] contacts
        {
            get
            {
                if (m_LegacyContacts == null)
                {
                    m_LegacyContacts = new ContactPoint[m_Pair.m_NbPoints];
                    m_Pair.ExtractContactsArray(m_LegacyContacts, m_Flipped);
                }

                return m_LegacyContacts;
            }
        }

        ///<exclude />
        public Collision()
        {
            Clear();
        }

        internal void Clear()
        {
            m_Header = new ContactPairHeader();
            m_Pair = new ContactPair();
            m_Flipped = false;

            m_LegacyContacts = null;
        }

        // Assumes we are NOT in the reusing mode
        internal Collision(in ContactPairHeader header, in ContactPair pair, bool flipped)
        {
            m_LegacyContacts = new ContactPoint[pair.m_NbPoints];
            pair.ExtractContactsArray(m_LegacyContacts, flipped);
            m_Header = header;
            m_Pair = pair;
            m_Flipped = flipped;
        }

        // Assumes we are in the reusing mode
        internal void Reuse(in ContactPairHeader header, in ContactPair pair)
        {
            m_Header = header;
            m_Pair = pair;
            m_LegacyContacts = null;
            m_Flipped = false;
        }

        ///<summary>Gets the contact point at the specified <c>index</c>.</summary>
        ///<remarks>You can retrieve individual contacts for this collision by <c>index</c>. You can use <see cref="contactCount" /> to determine how many contacts are available.</remarks>
        ///<param name="index">The index of the contact to retrieve.</param>
        ///<returns>The contact at the specified <c>index</c>.</returns>
        ///<seealso cref="contactCount" />
        ///<seealso cref="GetContacts" />
        public unsafe ContactPoint GetContact(int index)
        {
            if (index < 0 || index >= contactCount)
                throw new ArgumentOutOfRangeException(String.Format("Cannot get contact at index {0}. There are {1} contact(s).", index, contactCount));

            if (m_LegacyContacts != null)
                return m_LegacyContacts[index];

            float sign = m_Flipped ? -1f : 1f;
            var ptr = m_Pair.GetContactPoint_Internal(index);

            return new ContactPoint(
                    ptr->m_Position,
                    ptr->m_Normal * sign,
                    ptr->m_Impulse,
                    ptr->m_Separation,
                    m_Flipped ? m_Pair.otherColliderEntityId : m_Pair.colliderEntityId,
                    m_Flipped ? m_Pair.colliderEntityId : m_Pair.otherColliderEntityId);
        }

        ///<summary>Retrieves all contact points for this collision.</summary>
        ///<remarks>When retrieving contacts, you should ensure that the provided array is large enough to contain all the contacts you are interested in. The array is usually reused, so it should be large enough to return a reasonable quantity of contacts. This function also means that no allocations occur, which means no work is produced for the garbage collector.
        ///
        ///You can check how many contacts are available using <see cref="contactCount" />.</remarks>
        ///<param name="contacts">An array of <see cref="ContactPoint" /> used to receive the results.</param>
        ///<returns>Returns the number of contacts placed in the <c>contacts</c> array.</returns>
        ///<seealso cref="contactCount" />
        public int GetContacts(ContactPoint[] contacts)
        {
            if (contacts == null)
                throw new NullReferenceException("Cannot get contacts as the provided array is NULL.");

            if (m_LegacyContacts != null)
            {
                int length = Mathf.Min(m_LegacyContacts.Length, contacts.Length);
                Array.Copy(m_LegacyContacts, contacts, length);
                return length;
            }

            return m_Pair.ExtractContactsArray(contacts, m_Flipped);
        }

        ///<summary>Retrieves all contact points for this collision.</summary>
        ///<remarks>When retrieving contacts, try to make the provided list large enough to contain all the contacts you need. If the list is not large enough, Unity will automatically increase its size so that it can contain all the contacts. The list is usually reused, so it should be large enough to return a reasonable quantity of contacts. If the list does not have to be increased in size then this function will not allocate any memory, which means no work is produced for the garbage collector.
        ///
        ///You can check how many contacts are available using <see cref="contactCount" />.</remarks>
        ///<param name="contacts">A list of <see cref="ContactPoint" /> used to receive the results.</param>
        ///<returns>Returns the number of contacts placed in the <c>contacts</c> list.</returns>
        ///<seealso cref="contactCount" />
        public int GetContacts(List<ContactPoint> contacts)
        {
            if (contacts == null)
                throw new NullReferenceException("Cannot get contacts as the provided list is NULL.");

            contacts.Clear();

            if (m_LegacyContacts != null)
            {
                contacts.AddRange(m_LegacyContacts);
                return m_LegacyContacts.Length;
            }

            int n = (int)m_Pair.m_NbPoints;

            if (n == 0)
                return 0;

            if (contacts.Capacity < n) // Resize here instead of in native
                contacts.Capacity = n;

            return m_Pair.ExtractContacts(contacts, m_Flipped);
        }
    }
}
