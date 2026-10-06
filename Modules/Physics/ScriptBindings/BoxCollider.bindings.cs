// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    ///<summary>A box-shaped primitive collider.</summary>
    ///<seealso cref="SphereCollider" />
    ///<seealso cref="CapsuleCollider" />
    ///<seealso cref="PhysicsMaterial" />
    ///<seealso cref="Rigidbody" />
    [RequireComponent(typeof(Transform))]
    [global::UnityEngine.NativeClass("BoxCollider", PersistentTypeId = 65)]
    [NativeHeader("Modules/Physics/BoxCollider.h")]
    public partial class BoxCollider : Collider
    {
        internal BoxCollider(global::UnityEngine.EntityId id) : base(id) {}
        public BoxCollider() {}
        ///<summary>The center of the box, measured in the object's local space.</summary>
        extern public Vector3 center { get; set; }
        ///<summary>The size of the box, measured in the object's local space.</summary>
        ///<remarks>Use this to return or set the size of the BoxCollider component of a GameObject.  Unity measures the size in the GameObject's local space.
        ///                    Changing the BoxCollider size scales it by the Transform’s scale. Note: BoxCollider.size represents full extents (width, height, depth), not half-extents.</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// //Attach this script to a GameObject. Make sure the GameObject has a BoxCollider component (Unity Cubes have these automatically as long as they weren’t removed).
        /// //Create three Sliders ( __Create__>__UI__>__Slider__). These are for manipulating the x, y, and z values of the size.
        /// //Click on the GameObject and attach each of the Sliders to the fields in the Inspector.
        /// //In Play Mode, click the GameObject and enable Gizmos to visualize the BoxCollider.
        ///
        ///using UnityEngine;
        ///using UnityEngine.UI;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    //Make sure there is a BoxCollider component attached to your GameObject
        ///    BoxCollider m_Collider;
        ///    float m_ScaleX, m_ScaleY, m_ScaleZ;
        ///    public Slider m_SliderX, m_SliderY, m_SliderZ;
        ///
        ///
        ///    void Start()
        ///    {
        ///        //Fetch the Collider from the GameObject
        ///        m_Collider = GetComponent<BoxCollider>();
        ///        //These are the starting sizes for the Collider component
        ///        m_ScaleX = 1.0f;
        ///        m_ScaleY = 1.0f;
        ///        m_ScaleZ = 1.0f;
        ///
        ///        //Set all the sliders max values to 20 so the size values don't go above 20
        ///        m_SliderX.maxValue = 20;
        ///        m_SliderY.maxValue = 20;
        ///        m_SliderZ.maxValue = 20;
        ///
        ///        //Set all the sliders min values to 1 so the size don't go below 1
        ///        m_SliderX.minValue = 1;
        ///        m_SliderY.minValue = 1;
        ///        m_SliderZ.minValue = 1;
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        m_ScaleX = m_SliderX.value;
        ///        m_ScaleY = m_SliderY.value;
        ///        m_ScaleZ = m_SliderZ.value;
        ///
        ///        m_Collider.size = new Vector3(m_ScaleX, m_ScaleY, m_ScaleZ);
        ///        Debug.Log("Current BoxCollider Size : " + m_Collider.size);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public Vector3 size { get; set; }
    }
}
