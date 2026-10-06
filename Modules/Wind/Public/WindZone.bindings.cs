// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;
using UnityEngine.Internal;

namespace UnityEngine
{
    ///<summary>Modes a Wind Zone can have, either Spherical or Directional.</summary>
    ///<remarks>You can have more than one Spherical Wind Zone in a Scene, but it does not make much
    ///sense to have more than one Directional Wind Zone in your Scene as it affects
    ///the whole Scene. This Wind Zone Mode is used by the <see cref="WindZone.mode" /> member.</remarks>
    public enum WindZoneMode
    {
        ///<summary>Wind zone affects the entire Scene in one direction.</summary>
        ///<example>
        ///  <code><![CDATA[
        /// // Creates a Directional Wind Zone that blows wind up.
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        var wind = gameObject.AddComponent<WindZone>();
        ///        wind.mode = WindZoneMode.Directional;
        ///        transform.rotation = Quaternion.LookRotation(Vector3.up);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        Directional,
        ///<summary>Wind zone only has an effect inside the radius, and has a falloff from the center towards the edge.</summary>
        ///<example>
        ///  <code><![CDATA[
        /// // Creates a Spherical Wind Zone.
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        var wind = gameObject.AddComponent<WindZone>();
        ///        wind.mode = WindZoneMode.Spherical;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        Spherical
    }

    ///<summary>Wind Zones add realism to the trees you create by making them wave their branches and leaves as if blown by the wind.</summary>
    ///<remarks>**Note:** This only works with trees created by the tree creator or imported from SpeedTree Modeler.</remarks>
    [global::UnityEngine.NativeClass("WindZone", PersistentTypeId = 182)]
    [NativeHeader("Modules/Wind/Public/Wind.h")]
    public class WindZone : Component
    {
        internal WindZone(global::UnityEngine.EntityId id) : base(id) {}
        public WindZone() {}
        ///<summary>Defines the type of wind zone to be used (Spherical or Directional).</summary>
        ///<example>
        ///  <code><![CDATA[
        /// // Creates a Directional Wind Zone.
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        var wind = gameObject.AddComponent<WindZone>();
        ///        wind.mode = WindZoneMode.Directional;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public WindZoneMode mode {get; set; }
        ///<summary>Radius of the Spherical Wind Zone (only active if the WindZoneMode is set to Spherical).</summary>
        ///<example>
        ///  <code><![CDATA[
        /// // Creates a Spherical Wind Zone and sets its radius to 10.
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        var wind = gameObject.AddComponent<WindZone>();
        ///        wind.mode = WindZoneMode.Spherical;
        ///        wind.radius = 10f;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float radius {get; set; }
        ///<summary>The primary wind force.</summary>
        ///<remarks>It produces a softly changing wind Pressure.</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// // Creates a wind zone with the effect of a helicopter passing by
        /// // Just place this into an empty game object and move it over a tree
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        var wind = gameObject.AddComponent<WindZone>();
        ///        wind.mode = WindZoneMode.Spherical;
        ///        wind.radius = 10.0f;
        ///        wind.windMain = 3.0f;
        ///        wind.windTurbulence = 0.5f;
        ///        wind.windPulseMagnitude = 2.0f;
        ///        wind.windPulseFrequency = 0.01f;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float windMain {get; set; }
        ///<summary>The turbulence wind force.</summary>
        ///<remarks>Produces a rapidly changing wind pressure.</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// // Creates a wind zone to produce a softly changing general wind
        /// // Just place this into an empty game object
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        var wind = gameObject.AddComponent<WindZone>();
        ///        wind.mode = WindZoneMode.Directional;
        ///        wind.windMain = 0.70f;
        ///        wind.windTurbulence = 0.1f;
        ///        wind.windPulseMagnitude = 2.0f;
        ///        wind.windPulseFrequency = 0.25f;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float windTurbulence {get; set; }
        ///<summary>Defines how much the wind changes over time.</summary>
        ///<example>
        ///  <code><![CDATA[
        /// // Creates a wind zone with the effect of a helicopter passing by
        /// // Place this into an empty GameObject and move it over a tree
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        var wind = gameObject.AddComponent<WindZone>();
        ///        wind.mode = WindZoneMode.Spherical;
        ///        wind.radius = 10.0f;
        ///        wind.windMain = 3.0f;
        ///        wind.windTurbulence = 0.5f;
        ///        wind.windPulseMagnitude = 2.0f;
        ///        wind.windPulseFrequency = 0.01f;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float windPulseMagnitude  {get; set; }
        ///<summary>Defines the frequency of the wind changes.</summary>
        ///<example>
        ///  <code><![CDATA[
        /// // Creates a wind zone to produce a softly changing general wind
        /// // Just place this into an empty game object
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        var wind = gameObject.AddComponent<WindZone>();
        ///        wind.mode = WindZoneMode.Directional;
        ///        wind.windMain = 0.70f;
        ///        wind.windTurbulence = 0.1f;
        ///        wind.windPulseMagnitude = 2.0f;
        ///        wind.windPulseFrequency = 0.25f;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float windPulseFrequency {get; set; }
    }
}
