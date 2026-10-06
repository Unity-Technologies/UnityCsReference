// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.LowLevelPhysics;

namespace UnityEngine
{
    ///<summary>A base class of all colliders.</summary>
    ///<remarks>
    ///
    ///If the object with the Collider needs to be moved during gameplay then you should also attach a Rigidbody component to the object. The Rigidbody can be set to be kinematic if you don't want the object to have physical interaction with other objects.</remarks>
    ///<seealso cref="BoxCollider" />
    ///<seealso cref="SphereCollider" />
    ///<seealso cref="CapsuleCollider" />
    ///<seealso cref="MeshCollider" />
    ///<seealso cref="PhysicsMaterial" />
    ///<seealso cref="Rigidbody" />
    [global::UnityEngine.NativeClass("Collider", PersistentTypeId = 56)]
    [NativeHeader("Modules/Physics/Collider.h")]
    public partial class Collider : Component
    {
        public Collider() {}
        protected internal Collider(global::UnityEngine.EntityId id) : base(id) {}
        ///<summary>Enabled Colliders will collide with other Colliders, disabled Colliders won't.</summary>
        ///<remarks>This is shown as the small checkbox in the inspector of the Colliders. It decides if a GameObject can collide with other Colliders.</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// //This example enables and disables the GameObject's Collider when the space bar is pressed.
        /// //Attach this to a GameObject and attach a Collider to the GameObject
        ///
        ///using UnityEngine;
        ///using UnityEngine.InputSystem;
        ///                        
        ///public class ColliderEnabled : MonoBehaviour
        ///{
        ///    Collider m_Collider;
        ///
        ///    void Start()
        ///    {
        ///        //Fetch the GameObject's Collider (make sure it has a Collider component)
        ///        m_Collider = GetComponent<Collider>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        ///        {
        ///            //Toggle the Collider on and off when pressing the space bar
        ///            m_Collider.enabled = !m_Collider.enabled;
        ///
        ///            //Output to console whether the Collider is on or not
        ///            Debug.Log("Collider.enabled = " + m_Collider.enabled);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public bool enabled { get; set; }
        ///<summary>The rigidbody the collider is attached to.</summary>
        ///<remarks>Returns null if the collider is attached to no rigidbody.
        ///
        ///Colliders are automatically connected to the rigidbody attached
        ///to the same game object or attached to any parent game object.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        // Lift the rigidbody attached to the collider.
        ///        GetComponent<Collider>().attachedRigidbody.AddForce(0, 1, 0);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public Rigidbody attachedRigidbody { [NativeMethod("GetRigidbody")] get; }
        ///<summary>The articulation body the collider is attached to.</summary>
        ///<remarks>Returns null if the collider is attached to no articulation body.
        ///
        ///                    Colliders are automatically connected to the articulation body attached
        ///                    to the same game object or attached to any parent game object.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        // Lift the articulation body attached to the collider.
        ///        GetComponent<Collider>().attachedArticulationBody.AddForce(Vector3.up);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public ArticulationBody attachedArticulationBody { [NativeMethod("GetArticulationBody")] get; }
        ///<summary>Specify if this collider is configured as a trigger.</summary>
        ///<remarks>A trigger doesn't register a collision with an incoming <see cref="Rigidbody" />. Instead, it sends <see cref="M:UnityEngine.MonoBehaviour.OnTriggerEnter(UnityEngine.Collider)" />, <see cref="M:UnityEngine.MonoBehaviour.OnTriggerExit(UnityEngine.Collider)" /> and <see cref="M:UnityEngine.MonoBehaviour.OnTriggerStay(UnityEngine.Collider)" /> message
        ///when a rigidbody enters or exits the trigger volume.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    Collider m_ObjectCollider;
        ///
        ///    void Start()
        ///    {
        ///        //Fetch the GameObject's Collider (make sure they have a Collider component)
        ///        m_ObjectCollider = GetComponent<Collider>();
        ///        //Here the GameObject's Collider is not a trigger
        ///        m_ObjectCollider.isTrigger = false;
        ///        //Output whether the Collider is a trigger type Collider or not
        ///        Debug.Log("Trigger On : " + m_ObjectCollider.isTrigger);
        ///    }
        ///
        ///    void OnMouseDown()
        ///    {
        ///        //GameObject's Collider is now a trigger Collider when the GameObject is clicked. It now acts as a trigger
        ///        m_ObjectCollider.isTrigger = true;
        ///        //Output to console the GameObject’s trigger state
        ///        Debug.Log("Trigger On : " + m_ObjectCollider.isTrigger);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public bool isTrigger { get; set; }
        ///<summary>Contact offset value of this collider.</summary>
        ///<remarks>Colliders whose distance is less than the sum of their contactOffset values will generate contacts. The contact offset must be positive. Contact offset allows the collision detection system to predictively enforce the contact constraint even when the objects are slightly separated.</remarks>
        extern public float contactOffset { get; set; }
        ///<summary>The closest point on the collider given a specified location.</summary>
        ///<remarks>
        ///  <para>This method computes the point on the Collider that is closest to a 3D location in the world. In the example below <c>closestPoint</c> is the point on the Collider and <c>location</c> is the point in 3D space. If <c>location</c> is in the Collider the <c>closestPoint</c> is inside. If the Collider is disabled, the method returns the input <c>position</c>.
        ///
        ///**Note:** The difference from <see cref="Collider.ClosestPointOnBounds" /> is that the returned point is actually on the collider instead of on the bounds of the collider.  (<see cref="bounds" /> is a box that surrounds the collider.)</para>
        ///  <para>**Note:** Same as <see cref="Physics.ClosestPoint" /> but doesn't allow passing a custom position and rotation. Instead, it uses the position of the collider.</para>
        ///</remarks>
        ///<param name="position">Location you want to find the closest point to.</param>
        ///<returns>The closest point on the collider, returned in world space coordinates.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        /// // Note that closestPoint is based on the surface of the collider
        /// // and location represents a point in 3d space.
        /// // The gizmos work in the editor.
        /// // Note that closestPoint functions correctly regardless of whether the collider is set as a trigger.
        /// //
        /// // Create an origin-based cube and give it a scale of (1, 0.5, 3).
        /// // Change the BoxCollider size to (0.8, 1.2, 0.8).  This means that
        /// // collisions will happen when a GameObject gets close to the BoxCollider.
        /// // The ShowClosestPoint.cs script shows spheres that display the location
        /// // and closestPoint. Try changing the BoxCollider size and the location
        /// // values.
        ///
        /// // Attach this to a GameObject that has a Collider component attached
        ///public class ShowClosestPoint : MonoBehaviour
        ///{
        ///    public Vector3 location;
        ///
        ///    public void OnDrawGizmos()
        ///    {
        ///        var collider = GetComponent<Collider>();
        ///
        ///        if (!collider)
        ///        {
        ///            return; // nothing to do without a collider
        ///        }
        ///
        ///        Vector3 closestPoint = collider.ClosestPoint(location);
        ///
        ///        Gizmos.DrawSphere(location, 0.1f);
        ///        Gizmos.DrawWireSphere(closestPoint, 0.1f);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public Vector3 ClosestPoint(Vector3 position);
        ///<summary>The world space bounding volume of the collider (RO).</summary>
        ///<remarks>Note that this will be an empty bounding box if the collider is disabled or the game object
        ///is inactive.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ColliderBounds : MonoBehaviour
        ///{
        ///    Collider m_Collider;
        ///    Vector3 m_Center;
        ///    Vector3 m_Size, m_Min, m_Max;
        ///
        ///    void Start()
        ///    {
        ///        //Fetch the Collider from the GameObject
        ///        m_Collider = GetComponent<Collider>();
        ///        //Fetch the center of the Collider volume
        ///        m_Center = m_Collider.bounds.center;
        ///        //Fetch the size of the Collider volume
        ///        m_Size = m_Collider.bounds.size;
        ///        //Fetch the minimum and maximum bounds of the Collider volume
        ///        m_Min = m_Collider.bounds.min;
        ///        m_Max = m_Collider.bounds.max;
        ///        //Output this data into the console
        ///        OutputData();
        ///    }
        ///
        ///    void OutputData()
        ///    {
        ///        //Output to the console the center and size of the Collider volume
        ///        Debug.Log("Collider Center : " + m_Center);
        ///        Debug.Log("Collider Size : " + m_Size);
        ///        Debug.Log("Collider bound Minimum : " + m_Min);
        ///        Debug.Log("Collider bound Maximum : " + m_Max);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public Bounds bounds { get; }
        ///<summary>Specify whether this Collider's contacts are modifiable or not.</summary>
        ///<remarks>All pairs with Colliders that have this flag set will be available to scripts via Physics.ContactModifyEvent.
        ///
        ///Note that pairs are only notified to scripts as long as the corresponding bodies are awake. Once they fall asleep, there will no longer be any notification for such pairs.</remarks>
        extern public bool hasModifiableContacts { get; set; }
        ///<summary>Whether or not this Collider generates contacts for <see cref="Physics.ContactEvent" />.</summary>
        ///<remarks>If this property is set to <c>true</c>, all contacts produced by this collider will appear in the buffer. If this property is set to false, contact generation will depend on these factors:
        ///
        ///- If the Collider or its Rigidbody have a script with a <see cref="M:UnityEngine.MonoBehaviour.OnCollisionStay(UnityEngine.Collision)" /> method, all contacts will be generated for <see cref="Physics.ContactEvent" />.
        ///- If the Collider or its Rigidbody has a script with either <see cref="M:UnityEngine.MonoBehaviour.OnCollisionEnter(UnityEngine.Collision)" /> or <see cref="M:UnityEngine.MonoBehaviour.OnCollisionExit(UnityEngine.Collision)" /> but not <see cref="M:UnityEngine.MonoBehaviour.OnCollisionStay(UnityEngine.Collision)" />, enter and exit contacts will be generated for <see cref="Physics.ContactEvent" />, but not stay contacts.
        ///- If the <see cref="P:UnityEditor.PhysicsVisualizationSettings.showAllContacts" /> property is set to true, all Colliders will generate all contacts for visualisation purposes!</remarks>
        extern public bool providesContacts { get; set; }
        ///<summary>A decision priority assigned to this <see cref="Collider" /> used when there is a conflicting decision on whether a <see cref="Collider" /> can contact another <see cref="Collider" />.</summary>
        ///<remarks>The Layer Collision Matrix defines which layers can contact other layers. Additionally, you can include and exclude layers per <see cref="Collider" /> or for all <see cref="Collider" />s attached to a specific <see cref="Rigidbody" /> or <see cref="ArticulationBody" />. Any contact involves two different <see cref="Collider" /> instances. Unfortunately this can result in one <see cref="Collider" /> deciding that it should contact the other <see cref="Collider" />, but the other <see cref="Collider" /> deciding it shouldn't.  There are rules to decide how these situations are handled.
        ///
        ///The rules for making a decision between two <see cref="Collider" />s, referred to here as A and B, are made in the following order:
        ///
        ///1. If both A and B make the same decision then use that decision.
        ///2. If only A or B are using a layer include or exclude override, then use the decision for A or B that has the include or exclude override specified.
        ///3. If both A and B are using a layer include or exclude override, then use the decision from A or B that has the higher <see cref="Collider.layerOverridePriority" />.
        ///4. If A and B have the same <see cref="Collider.layerOverridePriority" />, then the decision will be to not create a contact.</remarks>
        ///<seealso cref="Collider.includeLayers" />
        ///<seealso cref="Collider.excludeLayers" />
        ///<seealso cref="Rigidbody.includeLayers" />
        ///<seealso cref="Rigidbody.excludeLayers" />
        ///<seealso cref="ArticulationBody.includeLayers" />
        ///<seealso cref="ArticulationBody.excludeLayers" />
        extern public int layerOverridePriority { get; set; }
        ///<summary>The additional layers that this <see cref="Collider" /> should exclude when deciding if the <see cref="Collider" /> can contact another <see cref="Collider" />.</summary>
        ///<remarks>The Layer Collision Matrix defines which layers can contact other layers. Use this property to specify additional layers that this specific <see cref="Collider" /> instance should not contact.
        ///
        ///When deciding which layers can contact each other, the Layer Collision Matrix first includes layers, then excludes layers. If a layer is set to be included and excluded, it is excluded.
        ///
        ///**NOTE**: Layers can be included or excluded differently depending on the settings of each <see cref="Collider" /> instance. As such, there could be a conflicting decision for whether two <see cref="Collider" /> instances can come into contact with each other. To learn how Unity decides this, see <see cref="Collider.layerOverridePriority" />.</remarks>
        ///<seealso cref="Collider.includeLayers" />
        ///<seealso cref="Rigidbody.includeLayers" />
        ///<seealso cref="Rigidbody.excludeLayers" />
        ///<seealso cref="ArticulationBody.includeLayers" />
        ///<seealso cref="ArticulationBody.excludeLayers" />
        extern public LayerMask excludeLayers { get; set; }
        ///<summary>The additional layers that this <see cref="Collider" /> should include when deciding if the <see cref="Collider" /> can contact another <see cref="Collider" />.</summary>
        ///<remarks>The Layer Collision Matrix defines which layers can contact other layers. Use this property to specify additional layers that this specific <see cref="Collider" /> instance can contact.
        ///
        ///**NOTE**: Layers can be included or excluded differently depending on the settings of each <see cref="Collider" /> instance. As such, there could be a conflicting decision for whether two <see cref="Collider" /> instances can come into contact with each other. To learn how Unity decides this, see <see cref="Collider.layerOverridePriority" />.</remarks>
        ///<seealso cref="Collider.excludeLayers" />
        ///<seealso cref="Rigidbody.includeLayers" />
        ///<seealso cref="Rigidbody.excludeLayers" />
        ///<seealso cref="ArticulationBody.includeLayers" />
        ///<seealso cref="ArticulationBody.excludeLayers" />
        extern public LayerMask includeLayers { get; set; }
        ///<summary>The structure holding the geometric shape of the collider and its type. (RO)</summary>
        public GeometryHolder GeometryHolder { get => this.GetGeometryHolder(); }

        ///<summary>Returns the geometric shape of the collider of the requested type.</summary>
        ///<remarks>Throws an InvalidOperationException if you request a shape that is not present in the collider.</remarks>
        ///<returns>Type of geometrical shape.</returns>
        ///<seealso cref="BoxGeometry" />
        ///<seealso cref="SphereGeometry" />
        ///<seealso cref="CapsuleGeometry" />
        ///<seealso cref="ConvexMeshGeometry" />
        ///<seealso cref="TriangleMeshGeometry" />
        ///<seealso cref="TerrainGeometry" />
        public T GetGeometry<T>() where T : struct, IGeometry
        {
            return this.GetGeometryHolder().As<T>();
        }

        ///<summary>The shared physics material of this collider.</summary>
        ///<remarks>Modifying this material will change the surface properties of all colliders using the material.
        ///In most cases you want to modify <see cref="Collider.material" /> instead.</remarks>
        [NativeMethod("Material")]
        extern public PhysicsMaterial sharedMaterial { get; set; }
        ///<summary>The material used by the collider.</summary>
        ///<remarks>If material is shared by colliders,
        ///it will duplicate the material and assign it to the collider.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Expose properties in the inspector for easy adjustment.
        ///    float dynFriction;
        ///    float statFriction;
        ///
        ///    Collider coll;
        ///
        ///    void Start()
        ///    {
        ///        coll = GetComponent<Collider>();
        ///
        ///        coll.material.dynamicFriction = dynFriction;
        ///        coll.material.staticFriction = statFriction;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public PhysicsMaterial material
        {
            [NativeMethod("GetClonedMaterial")]
            get;
            [NativeMethod("SetMaterial")]
            set;
        }

        extern private RaycastHit Raycast(Ray ray, float maxDistance, ref bool hasHit);

        ///<summary>Casts a <see cref="Ray" /> that ignores all Colliders except this one.</summary>
        ///<param name="ray">The starting point and direction of the ray.</param>
        ///<param name="hitInfo">If true is returned, <c>hitInfo</c> will contain more information about where the collider was hit.</param>
        ///<param name="maxDistance">The max length of the ray.</param>
        ///<returns>True when the ray intersects the collider, otherwise false.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///using UnityEngine.InputSystem;
        ///                        
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Collider coll;
        ///
        ///    void Start()
        ///    {
        ///        coll = GetComponent<Collider>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        // Move this object to the position clicked by the mouse.
        ///        if (Mouse.current.leftButton.wasPressedThisFrame)
        ///        {
        ///            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        ///            RaycastHit hit;
        ///
        ///            if (coll.Raycast(ray, out hit, 100.0f))
        ///            {
        ///                transform.position = ray.GetPoint(100.0f);
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="RaycastHit" />
        public bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance)
        {
            bool hasHit = false;
            hitInfo = Raycast(ray, maxDistance, ref hasHit);
            return hasHit;
        }

        [NativeName("ClosestPointOnBounds")]
        extern private void Internal_ClosestPointOnBounds(Vector3 point, ref Vector3 outPos, ref float distance);

        ///<summary>The closest point to the bounding box of the attached collider.</summary>
        ///<remarks>This can be used to calculate hit points when applying explosion damage.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public float hitPoints = 100.0F;
        ///    public Collider coll;
        ///    void Start()
        ///    {
        ///        coll = GetComponent<Collider>();
        ///    }
        ///
        ///    void ApplyHitPoints(Vector3 explosionPos, float radius)
        ///    {
        ///        // The distance from the explosion position to the surface of the collider.
        ///        Vector3 closestPoint = coll.ClosestPointOnBounds(explosionPos);
        ///        float distance = Vector3.Distance(closestPoint, explosionPos);
        ///
        ///        // The damage should decrease with distance from the explosion.
        ///        float damage = 1.0F - Mathf.Clamp01(distance / radius);
        ///        hitPoints -= damage * 10.0F;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Vector3 ClosestPointOnBounds(Vector3 position)
        {
            float dist = 0f;
            Vector3 outpos = Vector3.zero;
            Internal_ClosestPointOnBounds(position, ref outpos, ref dist);
            return outpos;
        }
    }
}
