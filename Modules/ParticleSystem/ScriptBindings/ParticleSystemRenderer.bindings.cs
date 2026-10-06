// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [NativeHeader("ParticleSystemScriptingClasses.h")]
    [NativeHeader("Modules/ParticleSystem/ParticleSystemRenderer.h")]
    [NativeHeader("Modules/ParticleSystem/ScriptBindings/ParticleSystemRendererScriptBindings.h")]
    [global::UnityEngine.NativeClass("ParticleSystemRenderer", PersistentTypeId = 199)]
    [RequireComponent(typeof(Transform))]
    public sealed partial class ParticleSystemRenderer : Renderer
    {
        internal ParticleSystemRenderer(global::UnityEngine.EntityId id) : base(id) {}
        public ParticleSystemRenderer() {}
        ///<summary>Control the direction that particles face.</summary>
        ///<remarks>For many applications, it is beneficial for particles to always face the Camera. This property allows you to change whether particles in the system face the Camera or not.
        ///
        ///Particles can face the Camera in two ways:
        ///
        ///1) Aligned to the Camera plane, so that all particles are aligned with the same facing direction.
        ///
        ///2) Aligned individually to face the eye position, which can be more convincing for particles that approach the Camera in close proximity or for VR environments.
        ///
        ///Unaligned particles can be set to align to the world or to their local transform, as required.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///
        ///    private ParticleSystem ps;
        ///    private ParticleSystemRenderer psr;
        ///    public ParticleSystemRenderSpace alignment = ParticleSystemRenderSpace.View;
        ///
        ///    void Start() {
        ///
        ///        Camera.main.transform.rotation = Quaternion.Euler(0.0f, 20.0f, 0.0f);   // rotate the camera so we can see the difference between view and world space
        ///
        ///        ps = GetComponent<ParticleSystem>();
        ///        psr = GetComponent<ParticleSystemRenderer>();
        ///
        ///        var main = ps.main;
        ///        main.startSpeed = 2.0f;
        ///
        ///        psr.material = new Material(Shader.Find("Sprites/Default"));
        ///    }
        ///
        ///    void Update() {
        ///        psr.alignment = alignment;
        ///    }
        ///
        ///    void OnGUI() {
        ///        alignment = (ParticleSystemRenderSpace)GUI.SelectionGrid(new Rect(25, 25, 300, 30), (int)alignment, new GUIContent[] { new GUIContent("View"), new GUIContent("World"), new GUIContent("Local"), new GUIContent("Facing") }, 4);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeName("RenderAlignment")]
        extern public ParticleSystemRenderSpace alignment { get; set; }
        ///<summary>Specifies how the system draws particles.</summary>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///
        ///    private ParticleSystem ps;
        ///    private ParticleSystemRenderer psr;
        ///    public ParticleSystemRenderMode renderMode = ParticleSystemRenderMode.Billboard;
        ///    public float cameraScale = 0.0f;
        ///    public float lengthScale = 0.0f;
        ///    public float velocityScale = 1.0f;
        ///
        ///    void Start() {
        ///
        ///        ps = GetComponent<ParticleSystem>();
        ///        psr = GetComponent<ParticleSystemRenderer>();
        ///
        ///        psr.mesh = Resources.GetBuiltinResource<Mesh>("Capsule.fbx");
        ///
        ///        var main = ps.main;
        ///        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        ///        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.8f);
        ///    }
        ///
        ///    void Update() {
        ///        psr.renderMode = renderMode;
        ///        psr.cameraVelocityScale = cameraScale;
        ///        psr.lengthScale = lengthScale;
        ///        psr.velocityScale = velocityScale;
        ///
        ///        if (renderMode == ParticleSystemRenderMode.Stretch)
        ///            Camera.main.transform.position = new Vector3(Mathf.Sin(Time.time) * 4.0f, 0.0f, -10.0f);    // move the camera so we can see the effect on stretch camera velocity
        ///    }
        ///
        ///    void OnGUI() {
        ///        renderMode = (ParticleSystemRenderMode)GUI.SelectionGrid(new Rect(25, 25, 900, 30), (int)renderMode, new GUIContent[] { new GUIContent("Billboard"), new GUIContent("Stretch"), new GUIContent("HorizontalBillboard"), new GUIContent("VerticalBillboard"), new GUIContent("Mesh"), new GUIContent("None") }, 6);
        ///
        ///        if (renderMode == ParticleSystemRenderMode.Stretch) {
        ///
        ///            GUI.Label(new Rect(25, 80, 100, 30), "Camera Scale");
        ///            GUI.Label(new Rect(25, 120, 100, 30), "Length Scale");
        ///            GUI.Label(new Rect(25, 160, 100, 30), "Velocity Scale");
        ///
        ///            cameraScale = GUI.HorizontalSlider(new Rect(125, 85, 100, 30), cameraScale, 0.0f, 10.0f);
        ///            lengthScale = GUI.HorizontalSlider(new Rect(125, 125, 100, 30), lengthScale, 0.0f, 10.0f);
        ///            velocityScale = GUI.HorizontalSlider(new Rect(125, 165, 100, 30), velocityScale, 0.0f, 10.0f);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public ParticleSystemRenderMode renderMode { get; set; }
        ///<summary>Specifies how the system randomly assigns meshes to particles.</summary>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    private ParticleSystem ps;
        ///    private ParticleSystemRenderer psr;
        ///
        ///    void Start() {
        ///
        ///        ps = GetComponent<ParticleSystem>();
        ///        psr = GetComponent<ParticleSystemRenderer>();
        ///
        ///        psr.renderMode = ParticleSystemRenderMode.Mesh;
        ///        psr.meshDistribution = ParticleSystemMeshDistribution.NonUniformRandom;
        ///        psr.SetMeshes(new Mesh[]{ Resources.GetBuiltinResource<Mesh>("Capsule.fbx"), Resources.GetBuiltinResource<Mesh>("Cube.fbx"), Resources.GetBuiltinResource<Mesh>("Sphere.fbx") });
        ///        psr.SetMeshWeightings(new float[]{ 0.1f, 0.1f, 0.8f });
        ///    }
        ///
        ///    void OnGUI() {
        ///        GUI.Label(new Rect(25, 40, 200, 30), "Mesh Count: " + psr.meshCount.ToString());
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="ParticleSystemRenderer.renderMode" />
        extern public ParticleSystemMeshDistribution meshDistribution { get; set; }
        ///<summary>Specifies how to sort particles within a system.</summary>
        ///<remarks>The sorting behavior is determined by the ParticleSystemSortMode enum. This property controls the order in which particles are sorted for rendering. Sorting modes affect the visual appearance, as particles can be rendered based on attributes like distance, age, or depth.
        ///
        ///To learn more about the available sorting modes, refer to the <see cref="ParticleSystemSortMode" /> enum.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEditor;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///
        ///    private ParticleSystem ps;
        ///    private ParticleSystemRenderer psr;
        ///    public ParticleSystemSortMode sortMode;
        ///
        ///    void Start() {
        ///
        ///        ps = GetComponent<ParticleSystem>();
        ///        psr = GetComponent<ParticleSystemRenderer>();
        ///
        ///        // set a slow start speed and high emission rate, to cause more overlap, to show sorting
        ///        var main = ps.main;
        ///        main.startSpeedMultiplier = 0.2f;
        ///        var emission = ps.emission;
        ///        emission.rateOverTimeMultiplier = 100.0f;
        ///
        ///        // set color over life, so we can see the sorting more easily
        ///        var colorOverLifetime = ps.colorOverLifetime;
        ///        colorOverLifetime.enabled = true;
        ///
        ///        Gradient gradient = new Gradient();
        ///        gradient.SetKeys(
        ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
        ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
        ///            );
        ///
        ///        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
        ///    }
        ///
        ///    void Update() {
        ///
        ///        psr.sortMode = sortMode;
        ///    }
        ///
        ///    void OnGUI() {
        ///
        ///        sortMode = (ParticleSystemSortMode)GUI.SelectionGrid(new Rect(25, 25, 900, 30), (int)sortMode, new GUIContent[] { new GUIContent("None"), new GUIContent("Distance"), new GUIContent("OldestInFront"), new GUIContent("YoungestInFront"), new GUIContent("Depth"), new GUIContent("DistanceReverse"), new GUIContent("DepthReverse") }, 7);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public ParticleSystemSortMode sortMode { get; set; }

        ///<summary>How much are the particles stretched in their direction of motion, defined as the length of the particle compared to its width.</summary>
        ///<remarks>This determines the base length of particles when they don't move. A value of 1 is neutral, causing no stretching or squashing. Use this with a value greater than 1 to make particles always be longer than they are wide.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///
        ///    private ParticleSystem ps;
        ///    private ParticleSystemRenderer psr;
        ///    public ParticleSystemRenderMode renderMode = ParticleSystemRenderMode.Billboard;
        ///    public float cameraScale = 0.0f;
        ///    public float lengthScale = 0.0f;
        ///    public float velocityScale = 1.0f;
        ///
        ///    void Start() {
        ///
        ///        ps = GetComponent<ParticleSystem>();
        ///        psr = GetComponent<ParticleSystemRenderer>();
        ///
        ///        psr.mesh = Resources.GetBuiltinResource<Mesh>("Capsule.fbx");
        ///
        ///        var main = ps.main;
        ///        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        ///        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.8f);
        ///    }
        ///
        ///    void Update() {
        ///        psr.renderMode = renderMode;
        ///        psr.cameraVelocityScale = cameraScale;
        ///        psr.lengthScale = lengthScale;
        ///        psr.velocityScale = velocityScale;
        ///
        ///        if (renderMode == ParticleSystemRenderMode.Stretch)
        ///            Camera.main.transform.position = new Vector3(Mathf.Sin(Time.time) * 4.0f, 0.0f, -10.0f);    // move the camera so we can see the effect on stretch camera velocity
        ///    }
        ///
        ///    void OnGUI() {
        ///        renderMode = (ParticleSystemRenderMode)GUI.SelectionGrid(new Rect(25, 25, 900, 30), (int)renderMode, new GUIContent[] { new GUIContent("Billboard"), new GUIContent("Stretch"), new GUIContent("HorizontalBillboard"), new GUIContent("VerticalBillboard"), new GUIContent("Mesh"), new GUIContent("None") }, 6);
        ///
        ///        if (renderMode == ParticleSystemRenderMode.Stretch) {
        ///
        ///            GUI.Label(new Rect(25, 80, 100, 30), "Camera Scale");
        ///            GUI.Label(new Rect(25, 120, 100, 30), "Length Scale");
        ///            GUI.Label(new Rect(25, 160, 100, 30), "Velocity Scale");
        ///
        ///            cameraScale = GUI.HorizontalSlider(new Rect(125, 85, 100, 30), cameraScale, 0.0f, 10.0f);
        ///            lengthScale = GUI.HorizontalSlider(new Rect(125, 125, 100, 30), lengthScale, 0.0f, 10.0f);
        ///            velocityScale = GUI.HorizontalSlider(new Rect(125, 165, 100, 30), velocityScale, 0.0f, 10.0f);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float lengthScale { get; set; }
        ///<summary>Specifies how much particles stretch depending on their velocity.</summary>
        ///<remarks>Use this to make particles get longer as their speed increases.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///
        ///    private ParticleSystem ps;
        ///    private ParticleSystemRenderer psr;
        ///    public ParticleSystemRenderMode renderMode = ParticleSystemRenderMode.Billboard;
        ///    public float cameraScale = 0.0f;
        ///    public float lengthScale = 0.0f;
        ///    public float velocityScale = 1.0f;
        ///
        ///    void Start() {
        ///
        ///        ps = GetComponent<ParticleSystem>();
        ///        psr = GetComponent<ParticleSystemRenderer>();
        ///
        ///        psr.mesh = Resources.GetBuiltinResource<Mesh>("Capsule.fbx");
        ///
        ///        var main = ps.main;
        ///        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        ///        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.8f);
        ///    }
        ///
        ///    void Update() {
        ///        psr.renderMode = renderMode;
        ///        psr.cameraVelocityScale = cameraScale;
        ///        psr.lengthScale = lengthScale;
        ///        psr.velocityScale = velocityScale;
        ///
        ///        if (renderMode == ParticleSystemRenderMode.Stretch)
        ///            Camera.main.transform.position = new Vector3(Mathf.Sin(Time.time) * 4.0f, 0.0f, -10.0f);    // move the camera so we can see the effect on stretch camera velocity
        ///    }
        ///
        ///    void OnGUI() {
        ///        renderMode = (ParticleSystemRenderMode)GUI.SelectionGrid(new Rect(25, 25, 900, 30), (int)renderMode, new GUIContent[] { new GUIContent("Billboard"), new GUIContent("Stretch"), new GUIContent("HorizontalBillboard"), new GUIContent("VerticalBillboard"), new GUIContent("Mesh"), new GUIContent("None") }, 6);
        ///
        ///        if (renderMode == ParticleSystemRenderMode.Stretch) {
        ///
        ///            GUI.Label(new Rect(25, 80, 100, 30), "Camera Scale");
        ///            GUI.Label(new Rect(25, 120, 100, 30), "Length Scale");
        ///            GUI.Label(new Rect(25, 160, 100, 30), "Velocity Scale");
        ///
        ///            cameraScale = GUI.HorizontalSlider(new Rect(125, 85, 100, 30), cameraScale, 0.0f, 10.0f);
        ///            lengthScale = GUI.HorizontalSlider(new Rect(125, 125, 100, 30), lengthScale, 0.0f, 10.0f);
        ///            velocityScale = GUI.HorizontalSlider(new Rect(125, 165, 100, 30), velocityScale, 0.0f, 10.0f);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float velocityScale { get; set; }
        ///<summary>How much do the particles stretch depending on the <see cref="Camera" />'s speed.</summary>
        ///<remarks>Use this to make particles become larger if the viewing Camera has a large speed.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///
        ///    private ParticleSystem ps;
        ///    private ParticleSystemRenderer psr;
        ///    public ParticleSystemRenderMode renderMode = ParticleSystemRenderMode.Billboard;
        ///    public float cameraScale = 0.0f;
        ///    public float lengthScale = 0.0f;
        ///    public float velocityScale = 1.0f;
        ///
        ///    void Start() {
        ///
        ///        ps = GetComponent<ParticleSystem>();
        ///        psr = GetComponent<ParticleSystemRenderer>();
        ///
        ///        psr.mesh = Resources.GetBuiltinResource<Mesh>("Capsule.fbx");
        ///
        ///        var main = ps.main;
        ///        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        ///        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.8f);
        ///    }
        ///
        ///    void Update() {
        ///        psr.renderMode = renderMode;
        ///        psr.cameraVelocityScale = cameraScale;
        ///        psr.lengthScale = lengthScale;
        ///        psr.velocityScale = velocityScale;
        ///
        ///        if (renderMode == ParticleSystemRenderMode.Stretch)
        ///            Camera.main.transform.position = new Vector3(Mathf.Sin(Time.time) * 4.0f, 0.0f, -10.0f);    // move the camera so we can see the effect on stretch camera velocity
        ///    }
        ///
        ///    void OnGUI() {
        ///        renderMode = (ParticleSystemRenderMode)GUI.SelectionGrid(new Rect(25, 25, 900, 30), (int)renderMode, new GUIContent[] { new GUIContent("Billboard"), new GUIContent("Stretch"), new GUIContent("HorizontalBillboard"), new GUIContent("VerticalBillboard"), new GUIContent("Mesh"), new GUIContent("None") }, 6);
        ///
        ///        if (renderMode == ParticleSystemRenderMode.Stretch) {
        ///
        ///            GUI.Label(new Rect(25, 80, 100, 30), "Camera Scale");
        ///            GUI.Label(new Rect(25, 120, 100, 30), "Length Scale");
        ///            GUI.Label(new Rect(25, 160, 100, 30), "Velocity Scale");
        ///
        ///            cameraScale = GUI.HorizontalSlider(new Rect(125, 85, 100, 30), cameraScale, 0.0f, 10.0f);
        ///            lengthScale = GUI.HorizontalSlider(new Rect(125, 125, 100, 30), lengthScale, 0.0f, 10.0f);
        ///            velocityScale = GUI.HorizontalSlider(new Rect(125, 165, 100, 30), velocityScale, 0.0f, 10.0f);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float cameraVelocityScale { get; set; }

        ///<summary>Specifies how to calculate lighting for the billboard.</summary>
        ///<remarks>A value of 0 means Unity calculates lighting as though the billboard was a sphere. This results in the billboard looking more like a sphere. A value of 1 means Unity calculates lighting for the billboard as a flat quad.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEditor;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///
        ///    private ParticleSystem ps;
        ///    private ParticleSystemRenderer psr;
        ///    public float normalDirection = 1.0f;
        ///
        ///    void Start() {
        ///
        ///        ps = GetComponent<ParticleSystem>();
        ///        psr = GetComponent<ParticleSystemRenderer>();
        ///
        ///        psr.material = new Material(Shader.Find("Sprites/Default"));
        ///    }
        ///
        ///    void Update() {
        ///
        ///        psr.normalDirection = normalDirection;
        ///    }
        ///
        ///    void OnGUI() {
        ///
        ///        normalDirection = GUI.HorizontalSlider(new Rect(25, 25, 100, 30), normalDirection, 0.0f, 1.0f);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float normalDirection { get; set; }
        ///<summary>Apply a shadow bias to prevent self-shadowing artifacts. The specified value is the proportion of the particle size.</summary>
        extern public float shadowBias { get; set; }
        ///<summary>Biases Particle System sorting amongst other transparencies.</summary>
        ///<remarks>Use lower (negative) numbers to prioritize the Particle System to draw closer to the front, and use higher numbers to prioritize other transparent objects.</remarks>
        extern public float sortingFudge { get; set; }
        ///<summary>Clamp the minimum particle size.</summary>
        ///<remarks>Tiny particles can cause aliasing, and not contribute visually to the Scene, whilst still negatively affecting performance. Use this setting to make sure they always maintain a minimum size on screen.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///
        ///    private ParticleSystem ps;
        ///    private ParticleSystemRenderer psr;
        ///    public float minSize = 0.0f;
        ///    public float maxSize = 1.0f;
        ///
        ///    void Start() {
        ///
        ///        ps = GetComponent<ParticleSystem>();
        ///        psr = GetComponent<ParticleSystemRenderer>();
        ///
        ///        var main = ps.main;
        ///        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 5.0f);
        ///    }
        ///
        ///    void Update() {
        ///        psr.minParticleSize = minSize;
        ///        psr.maxParticleSize = maxSize;
        ///    }
        ///
        ///    void OnGUI() {
        ///        GUI.Label(new Rect(25, 40, 200, 30), "Minimum Screen Space Size");
        ///        GUI.Label(new Rect(25, 80, 200, 30), "Maximum Screen Space Size");
        ///
        ///        minSize = GUI.HorizontalSlider(new Rect(245, 45, 100, 30), minSize, 0.0f, 1.0f);
        ///        maxSize = GUI.HorizontalSlider(new Rect(245, 85, 100, 30), maxSize, 0.0f, 1.0f);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float minParticleSize { get; set; }
        ///<summary>Clamp the maximum particle size.</summary>
        ///<remarks>Particles can become very heavy on fillrate. Use this setting to make sure they don't
        ///use too much performance when up close to the viewer.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///
        ///    private ParticleSystem ps;
        ///    private ParticleSystemRenderer psr;
        ///    public float minSize = 0.0f;
        ///    public float maxSize = 1.0f;
        ///
        ///    void Start() {
        ///
        ///        ps = GetComponent<ParticleSystem>();
        ///        psr = GetComponent<ParticleSystemRenderer>();
        ///
        ///        var main = ps.main;
        ///        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 5.0f);
        ///    }
        ///
        ///    void Update() {
        ///        psr.minParticleSize = minSize;
        ///        psr.maxParticleSize = maxSize;
        ///    }
        ///
        ///    void OnGUI() {
        ///        GUI.Label(new Rect(25, 40, 200, 30), "Minimum Screen Space Size");
        ///        GUI.Label(new Rect(25, 80, 200, 30), "Maximum Screen Space Size");
        ///
        ///        minSize = GUI.HorizontalSlider(new Rect(245, 45, 100, 30), minSize, 0.0f, 1.0f);
        ///        maxSize = GUI.HorizontalSlider(new Rect(245, 85, 100, 30), maxSize, 0.0f, 1.0f);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float maxParticleSize { get; set; }
        ///<summary>Modify the pivot point used for rotating particles.</summary>
        ///<remarks>The units are expressed as a multiplier of the particle sizes, relative to their diameters. For example, a value of 0.5 adjusts the pivot by the particle radius, allowing particles to rotate around their edges.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEditor;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///
        ///    private ParticleSystem ps;
        ///    private ParticleSystemRenderer psr;
        ///    public Vector3 pivot;
        ///
        ///    void Start() {
        ///
        ///        ps = GetComponent<ParticleSystem>();
        ///        psr = GetComponent<ParticleSystemRenderer>();
        ///
        ///        psr.material = new Material(Shader.Find("Sprites/Default"));  // square material so we can see the pivot more easily
        ///
        ///        var rotation = ps.rotationOverLifetime;
        ///        rotation.enabled = true;
        ///        rotation.zMultiplier = 180.0f;  // spin so we can see the pivot more easily
        ///    }
        ///
        ///    void Update() {
        ///
        ///        psr.pivot = pivot;
        ///    }
        ///
        ///    void OnGUI() {
        ///
        ///        GUI.Label(new Rect(25, 40, 100, 30), "X");
        ///        GUI.Label(new Rect(25, 80, 100, 30), "Y");
        ///        GUI.Label(new Rect(25, 120, 100, 30), "Z");
        ///
        ///        pivot.x = GUI.HorizontalSlider(new Rect(65, 25, 100, 30), pivot.x, -2.0f, 2.0f);
        ///        pivot.y = GUI.HorizontalSlider(new Rect(65, 65, 100, 30), pivot.y, -2.0f, 2.0f);
        ///        pivot.z = GUI.HorizontalSlider(new Rect(65, 105, 100, 30), pivot.z, -2.0f, 2.0f);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public Vector3 pivot { get; set; }
        ///<summary>Flip a percentage of the particles, along each axis.</summary>
        ///<remarks>Set between 0 and 1, where higher values cause a higher proportion of the particles to flip, and 1 causes all particles to flip.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEditor;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///
        ///    private ParticleSystemRenderer psr;
        ///    public Vector3 flip;
        ///
        ///    void Start() {
        ///
        ///        psr = GetComponent<ParticleSystemRenderer>();
        ///
        ///        psr.material = new Material(Shader.Find("Sprites/Default"));  // square material so we can see the pivot more easily
        ///        psr.mesh = Resources.GetBuiltinResource<Mesh>("Capsule.fbx");
        ///    }
        ///
        ///    void Update() {
        ///
        ///        psr.flip = flip;
        ///    }
        ///
        ///    void OnGUI() {
        ///
        ///        GUI.Label(new Rect(25, 40, 100, 30), "X");
        ///        GUI.Label(new Rect(25, 80, 100, 30), "Y");
        ///        GUI.Label(new Rect(25, 120, 100, 30), "Z");
        ///
        ///        flip.x = GUI.HorizontalSlider(new Rect(65, 25, 100, 30), flip.x, 0.0f, 1.0f);
        ///        flip.y = GUI.HorizontalSlider(new Rect(65, 65, 100, 30), flip.y, 0.0f, 1.0f);
        ///        flip.z = GUI.HorizontalSlider(new Rect(65, 105, 100, 30), flip.z, 0.0f, 1.0f);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public Vector3 flip { get; set; }
        ///<summary>Specifies how the Particle System Renderer interacts with <see cref="T:UnityEngine.SpriteMask" />.</summary>
        ///<remarks>By default, particles do not interact with SpriteMasks and are visible regardless of whether you assign a SpriteMask or not.
        ///You can make the ParticleSystemRenderer visible either inside or outside a SpriteMask. To do the former, set this to <see cref="SpriteMaskInteraction.VisibleInsideMask" />. To do the latter, set this to <see cref="SpriteMaskInteraction.VisibleOutsideMask" />.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    private ParticleSystemRenderer psr;
        ///    public SpriteMaskInteraction maskInteraction;
        ///
        ///    void Start()
        ///    {
        ///        psr = GetComponent<ParticleSystemRenderer>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        psr.maskInteraction = maskInteraction;
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        maskInteraction = (SpriteMaskInteraction)GUI.SelectionGrid(new Rect(25, 25, 900, 30), (int)maskInteraction, new GUIContent[] { new GUIContent("No Masking"), new GUIContent("Visible Inside Mask"), new GUIContent("Visible Outside Mask") }, 3);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="SpriteMaskInteraction" />
        extern public SpriteMaskInteraction maskInteraction { get; set; }
        ///<summary>Set the Material that the TrailModule uses to attach trails to particles.</summary>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///
        ///    private ParticleSystem ps;
        ///    public float hSliderValueRatio = 1.0f;
        ///
        ///    void Start() {
        ///        ps = GetComponent<ParticleSystem>();
        ///
        ///        var main = ps.main;
        ///        main.startColor = new ParticleSystem.MinMaxGradient(Color.red, Color.yellow);
        ///        main.startSize = 0.1f;
        ///
        ///        var trails = ps.trails;
        ///        trails.enabled = true;
        ///
        ///        var psr = GetComponent<ParticleSystemRenderer>();
        ///        psr.trailMaterial = new Material(Shader.Find("Sprites/Default"));
        ///    }
        ///
        ///    void Update() {
        ///        var trails = ps.trails;
        ///        trails.ratio = hSliderValueRatio;
        ///    }
        ///
        ///    void OnGUI() {
        ///        GUI.Label(new Rect(25, 40, 100, 30), "Ratio");
        ///
        ///        hSliderValueRatio = GUI.HorizontalSlider(new Rect(55, 45, 100, 30), hSliderValueRatio, 0.0f, 1.0f);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="ParticleSystem.trails" />
        extern public Material trailMaterial { get; set; }
        extern internal Material oldTrailMaterial { set; }
        ///<summary>Enables GPU Instancing on platforms that support it.</summary>
        ///<remarks>To use GPU Instancing to render a Particle System, the particle must use a Shader that contains a Procedural Instancing pass (that is, it contains the <c>#pragma instancing_options procedural</c> directive).</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    private ParticleSystem ps;
        ///    private ParticleSystemRenderer psr;
        ///    private bool enableGPUInstancing = true;
        ///
        ///    void Start()
        ///    {
        ///        ps = GetComponent<ParticleSystem>();
        ///        psr = GetComponent<ParticleSystemRenderer>();
        ///
        ///        psr.renderMode = ParticleSystemRenderMode.Mesh;
        ///        psr.SetMeshes(new Mesh[] { Resources.GetBuiltinResource<Mesh>("Capsule.fbx"), Resources.GetBuiltinResource<Mesh>("Cube.fbx"), Resources.GetBuiltinResource<Mesh>("Sphere.fbx") });
        ///
        ///        psr.sharedMaterial = new Material(Shader.Find("Particles/Standard Surface"));
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        psr.enableGPUInstancing = enableGPUInstancing;
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        enableGPUInstancing = GUI.Toggle(new Rect(25, 45, 200, 30), enableGPUInstancing, "Enabled");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public bool enableGPUInstancing { get; set; }
        ///<summary>Allow billboard particles to roll around their z-axis.</summary>
        ///<remarks>Allows billboards to roll with the Camera. It is often useful to disable this option when using VR, to give particles a more believable grounding in the world.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///
        ///    private ParticleSystemRenderer psr;
        ///    public bool allowRoll = true;
        ///
        ///    void Start()
        ///    {
        ///        psr = GetComponent<ParticleSystemRenderer>();
        ///        psr.material = new Material(Shader.Find("Sprites/Default"));
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        var psr = GetComponent<ParticleSystemRenderer>();
        ///        psr.allowRoll = allowRoll;
        ///
        ///        Camera.main.transform.rotation = Quaternion.Euler(0.0f, 0.0f, Mathf.Sin(Time.time * 0.2f) * 90.0f);
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        allowRoll = GUI.Toggle(new Rect(25, 45, 200, 30), allowRoll, "Allow Roll");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public bool allowRoll { get; set; }
        ///<summary>Enables freeform stretching behavior.</summary>
        ///<remarks>With this stretching behavior particles don't get thin when viewed head-on and particle rotation can be independent from stretching direction.</remarks>
        ///<seealso cref="rotateWithStretchDirection" />
        extern public bool freeformStretching { get; set; }
        ///<summary>Rotate the particles based on the direction they are stretched in. This is added on top of other particle rotation.</summary>
        ///<remarks>This property only has effect when <see cref="freeformStretching" /> is enabled. When <see cref="freeformStretching" /> is disabled, particles are always rotated based on the direction they are stretched in, even if <see cref="rotateWithStretchDirection" /> is false.</remarks>
        extern public bool rotateWithStretchDirection { get; set; }
        ///<summary>Set whether colors will be converted appropriately before being passed to the GPU when using Linear Rendering.</summary>
        extern public bool applyActiveColorSpace { get; set; }

        // Mesh used as particle instead of billboarded texture.
        ///<summary>The Mesh that the particle uses instead of a billboarded Texture.</summary>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///
        ///    private ParticleSystem ps;
        ///    private ParticleSystemRenderer psr;
        ///    public ParticleSystemRenderMode renderMode = ParticleSystemRenderMode.Billboard;
        ///    public float cameraScale = 0.0f;
        ///    public float lengthScale = 0.0f;
        ///    public float velocityScale = 1.0f;
        ///
        ///    void Start() {
        ///
        ///        ps = GetComponent<ParticleSystem>();
        ///        psr = GetComponent<ParticleSystemRenderer>();
        ///
        ///        psr.mesh = Resources.GetBuiltinResource<Mesh>("Capsule.fbx");
        ///
        ///        var main = ps.main;
        ///        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        ///        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.8f);
        ///    }
        ///
        ///    void Update() {
        ///        psr.renderMode = renderMode;
        ///        psr.cameraVelocityScale = cameraScale;
        ///        psr.lengthScale = lengthScale;
        ///        psr.velocityScale = velocityScale;
        ///
        ///        if (renderMode == ParticleSystemRenderMode.Stretch)
        ///            Camera.main.transform.position = new Vector3(Mathf.Sin(Time.time) * 4.0f, 0.0f, -10.0f);    // move the camera so we can see the effect on stretch camera velocity
        ///    }
        ///
        ///    void OnGUI() {
        ///        renderMode = (ParticleSystemRenderMode)GUI.SelectionGrid(new Rect(25, 25, 900, 30), (int)renderMode, new GUIContent[] { new GUIContent("Billboard"), new GUIContent("Stretch"), new GUIContent("HorizontalBillboard"), new GUIContent("VerticalBillboard"), new GUIContent("Mesh"), new GUIContent("None") }, 6);
        ///
        ///        if (renderMode == ParticleSystemRenderMode.Stretch) {
        ///
        ///            GUI.Label(new Rect(25, 80, 100, 30), "Camera Scale");
        ///            GUI.Label(new Rect(25, 120, 100, 30), "Length Scale");
        ///            GUI.Label(new Rect(25, 160, 100, 30), "Velocity Scale");
        ///
        ///            cameraScale = GUI.HorizontalSlider(new Rect(125, 85, 100, 30), cameraScale, 0.0f, 10.0f);
        ///            lengthScale = GUI.HorizontalSlider(new Rect(125, 125, 100, 30), lengthScale, 0.0f, 10.0f);
        ///            velocityScale = GUI.HorizontalSlider(new Rect(125, 165, 100, 30), velocityScale, 0.0f, 10.0f);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="ParticleSystemRenderer.renderMode" />
        extern public Mesh mesh
        {
            [FreeFunction(Name = "ParticleSystemRendererScriptBindings::GetMesh", HasExplicitThis = true)]
            get;
            [FreeFunction(Name = "ParticleSystemRendererScriptBindings::SetMesh", HasExplicitThis = true)]
            set;
        }

        ///<summary>Gets the array of Meshes to use when selecting particle meshes.</summary>
        ///<param name="meshes">An array this function populates with the list of Meshes the ParticleSystemRenderer uses for particle Mesh selection. If the array is smaller than the number of Meshes, this function cannot populate it with every Mesh. If the array is larger than the number of Meshes, this function ignores indices greater than the number of Meshes. Use <see cref="ParticleSystemRenderer.meshCount" /> to get the number of Meshes the ParticleSystemRenderer has.</param>
        ///<returns>The number of Meshes this function wrote to the destination array.</returns>
        ///<seealso cref="ParticleSystemRenderer.renderMode" />
        [RequiredByNativeCode] // Added to any method to prevent stripping of the class
        [FreeFunction(Name = "ParticleSystemRendererScriptBindings::GetMeshes", HasExplicitThis = true)]
        extern public int GetMeshes([NotNull][Out] Mesh[] meshes);

        ///<summary>Sets the Meshes that the <see cref="ParticleSystemRenderer" /> uses to display particles when the <see cref="ParticleSystemRenderer.renderMode" /> is set to <see cref="ParticleSystemRenderMode.Mesh" />.</summary>
        ///<param name="meshes">The array of Meshes to use.</param>
        ///<param name="size">The number of elements from the Mesh array to apply.</param>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    private ParticleSystem ps;
        ///    private ParticleSystemRenderer psr;
        ///
        ///    void Start() {
        ///
        ///        ps = GetComponent<ParticleSystem>();
        ///        psr = GetComponent<ParticleSystemRenderer>();
        ///
        ///        psr.renderMode = ParticleSystemRenderMode.Mesh;
        ///        psr.SetMeshes(new Mesh[]{ Resources.GetBuiltinResource<Mesh>("Capsule.fbx"), Resources.GetBuiltinResource<Mesh>("Cube.fbx"), Resources.GetBuiltinResource<Mesh>("Sphere.fbx") });
        ///    }
        ///
        ///    void OnGUI() {
        ///        GUI.Label(new Rect(25, 40, 200, 30), "Mesh Count: " + psr.meshCount.ToString());
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="ParticleSystemRenderer.renderMode" />
        [FreeFunction(Name = "ParticleSystemRendererScriptBindings::SetMeshes", HasExplicitThis = true)]
        extern public void SetMeshes([NotNull] Mesh[] meshes, int size);
        ///<summary>Sets the Meshes that the <see cref="ParticleSystemRenderer" /> uses to display particles when the <see cref="ParticleSystemRenderer.renderMode" /> is set to <see cref="ParticleSystemRenderMode.Mesh" />.</summary>
        ///<param name="meshes">The array of Meshes to use.</param>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    private ParticleSystem ps;
        ///    private ParticleSystemRenderer psr;
        ///
        ///    void Start() {
        ///
        ///        ps = GetComponent<ParticleSystem>();
        ///        psr = GetComponent<ParticleSystemRenderer>();
        ///
        ///        psr.renderMode = ParticleSystemRenderMode.Mesh;
        ///        psr.SetMeshes(new Mesh[]{ Resources.GetBuiltinResource<Mesh>("Capsule.fbx"), Resources.GetBuiltinResource<Mesh>("Cube.fbx"), Resources.GetBuiltinResource<Mesh>("Sphere.fbx") });
        ///    }
        ///
        ///    void OnGUI() {
        ///        GUI.Label(new Rect(25, 40, 200, 30), "Mesh Count: " + psr.meshCount.ToString());
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="ParticleSystemRenderer.renderMode" />
        public void SetMeshes(Mesh[] meshes) { SetMeshes(meshes, meshes.Length); }

        ///<summary>Gets the array of Mesh weightings to use when randomly selecting particle meshes.</summary>
        ///<param name="weightings">An array this function populates with the list of Mesh weightings the ParticleSystemRenderer uses for particle Mesh selection. If the array is smaller than the number of weights, this function cannot populate it with every weight. If the array is larger than the number of weights, this function ignores indices greater than the number of weights. Use <see cref="ParticleSystemRenderer.meshCount" /> to get the number of Meshes, and thus weights, the ParticleSystemRenderer has.</param>
        ///<returns>The number of weights this function wrote to the destination array.</returns>
        ///<seealso cref="ParticleSystemRenderer.meshDistribution" />
        [FreeFunction(Name = "ParticleSystemRendererScriptBindings::GetMeshWeightings", HasExplicitThis = true)]
        extern public int GetMeshWeightings([NotNull][Out] float[] weightings);

        ///<summary>Sets the weights that the <see cref="ParticleSystemRenderer" /> uses to assign Meshes to particles.</summary>
        ///<remarks>
        ///  <para>The <see cref="ParticleSystemRenderer" /> only uses these weights if you set <see cref="ParticleSystemRenderer.renderMode" /> to <see cref="ParticleSystemRenderMode.Mesh" /> and set <see cref="ParticleSystemRenderer.meshDistribution" /> to <see cref="ParticleSystemMeshDistribution.NonUniformRandom" />.</para>
        ///  <para>The weightings are relative to each other but can be any positive value. For example, if you set the weights to 6 and 3, the ParticleSystemRenderer randomly selects the first Mesh approximately twice as often as the second Mesh. It can be most intuitive to use percentages for the weightings and ensure that the sum of the weightings adds up to 100, but this is not a requirement.</para>
        ///</remarks>
        ///<param name="weightings">The array of weights to use.</param>
        ///<param name="size">The number of elements from the weighting array to apply.</param>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    private ParticleSystem ps;
        ///    private ParticleSystemRenderer psr;
        ///
        ///    void Start() {
        ///
        ///        ps = GetComponent<ParticleSystem>();
        ///        psr = GetComponent<ParticleSystemRenderer>();
        ///
        ///        psr.renderMode = ParticleSystemRenderMode.Mesh;
        ///        psr.meshDistribution = ParticleSystemMeshDistribution.NonUniformRandom;
        ///        psr.SetMeshes(new Mesh[]{ Resources.GetBuiltinResource<Mesh>("Capsule.fbx"), Resources.GetBuiltinResource<Mesh>("Cube.fbx"), Resources.GetBuiltinResource<Mesh>("Sphere.fbx") });
        ///        psr.SetMeshWeightings(new float[]{ 0.1f, 0.1f, 0.8f });
        ///    }
        ///
        ///    void OnGUI() {
        ///        GUI.Label(new Rect(25, 40, 200, 30), "Mesh Count: " + psr.meshCount.ToString());
        ///    }
        ///}]]></code>
        ///</example>
        [FreeFunction(Name = "ParticleSystemRendererScriptBindings::SetMeshWeightings", HasExplicitThis = true)]
        extern public void SetMeshWeightings([NotNull] float[] weightings, int size);
        ///<summary>Sets the weights that the <see cref="ParticleSystemRenderer" /> uses to assign Meshes to particles.</summary>
        ///<remarks>
        ///  <para>The <see cref="ParticleSystemRenderer" /> only uses these weights if you set <see cref="ParticleSystemRenderer.renderMode" /> to <see cref="ParticleSystemRenderMode.Mesh" /> and set <see cref="ParticleSystemRenderer.meshDistribution" /> to <see cref="ParticleSystemMeshDistribution.NonUniformRandom" />.</para>
        ///  <para>The weightings are relative to each other but can be any positive value. For example, if you set the weights to 6 and 3, the ParticleSystemRenderer randomly selects the first Mesh approximately twice as often as the second Mesh. It can be most intuitive to use percentages for the weightings and ensure that the sum of the weightings adds up to 100, but this is not a requirement.</para>
        ///</remarks>
        ///<param name="weightings">The array of weights to use.</param>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    private ParticleSystem ps;
        ///    private ParticleSystemRenderer psr;
        ///
        ///    void Start() {
        ///
        ///        ps = GetComponent<ParticleSystem>();
        ///        psr = GetComponent<ParticleSystemRenderer>();
        ///
        ///        psr.renderMode = ParticleSystemRenderMode.Mesh;
        ///        psr.meshDistribution = ParticleSystemMeshDistribution.NonUniformRandom;
        ///        psr.SetMeshes(new Mesh[]{ Resources.GetBuiltinResource<Mesh>("Capsule.fbx"), Resources.GetBuiltinResource<Mesh>("Cube.fbx"), Resources.GetBuiltinResource<Mesh>("Sphere.fbx") });
        ///        psr.SetMeshWeightings(new float[]{ 0.1f, 0.1f, 0.8f });
        ///    }
        ///
        ///    void OnGUI() {
        ///        GUI.Label(new Rect(25, 40, 200, 30), "Mesh Count: " + psr.meshCount.ToString());
        ///    }
        ///}]]></code>
        ///</example>
        public void SetMeshWeightings(float[] weightings) { SetMeshWeightings(weightings, weightings.Length); }

        ///<summary>The number of Meshes the system uses for particle rendering.</summary>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///
        ///    private ParticleSystem ps;
        ///    private ParticleSystemRenderer psr;
        ///
        ///    void Start() {
        ///
        ///        ps = GetComponent<ParticleSystem>();
        ///        psr = GetComponent<ParticleSystemRenderer>();
        ///
        ///        psr.renderMode = ParticleSystemRenderMode.Mesh;
        ///        psr.SetMeshes(new Mesh[]{ Resources.GetBuiltinResource<Mesh>("Capsule.fbx"), Resources.GetBuiltinResource<Mesh>("Cube.fbx"), Resources.GetBuiltinResource<Mesh>("Sphere.fbx") });
        ///    }
        ///
        ///    void OnGUI() {
        ///        GUI.Label(new Rect(25, 40, 200, 30), "Mesh Count: " + psr.meshCount.ToString());
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="ParticleSystemRenderer.renderMode" />
        extern public int meshCount { get; }

        ///<summary>Creates a snapshot of ParticleSystemRenderer and stores it in a <c>mesh</c>.</summary>
        ///<param name="mesh">A static Mesh to receive the snapshot of the particles.</param>
        ///<param name="options">Specifies whether to include the rotation and scale of the Transform in the baked Mesh.</param>
        public void BakeMesh(Mesh mesh, ParticleSystemBakeMeshOptions options) { BakeMesh(mesh, Camera.main, options); }
        ///<summary>Creates a snapshot of ParticleSystemRenderer and stores it in a <c>mesh</c>.</summary>
        ///<param name="mesh">A static Mesh to receive the snapshot of the particles.</param>
        ///<param name="camera">The Camera used to determine which way camera-space particles face.</param>
        ///<param name="options">Specifies whether to include the rotation and scale of the Transform in the baked Mesh.</param>
        extern public void BakeMesh([NotNull] Mesh mesh, [NotNull] Camera camera, ParticleSystemBakeMeshOptions options);

        ///<summary>Creates a snapshot of ParticleSystem Trails and stores them in a <c>mesh</c>.</summary>
        ///<param name="mesh">A static Mesh to receive the snapshot of the particle trails.</param>
        ///<param name="options">Specifies whether to include the rotation and scale of the Transform in the baked Mesh.</param>
        public void BakeTrailsMesh(Mesh mesh, ParticleSystemBakeMeshOptions options) { BakeTrailsMesh(mesh, Camera.main, options); }
        ///<summary>Creates a snapshot of ParticleSystem Trails and stores them in a <c>mesh</c>.</summary>
        ///<param name="mesh">A static Mesh to receive the snapshot of the particle trails.</param>
        ///<param name="camera">The Camera used to determine which way camera-space trails face.</param>
        ///<param name="options">Specifies whether to include the rotation and scale of the Transform in the baked Mesh.</param>
        extern public void BakeTrailsMesh([NotNull] Mesh mesh, [NotNull] Camera camera, ParticleSystemBakeMeshOptions options);

        internal struct BakeTextureOutput
        {
            [NativeName("first")] internal Texture2D vertices;
            [NativeName("second")] internal Texture2D indices;
        }

        ///<summary>Creates a snapshot of ParticleSystemRenderer and stores it in a <c>Texture2D</c>.</summary>
        ///<param name="verticesTexture">A Texture2D to receive the snapshot of the particle vertices.</param>
        ///<param name="options">Specifies whether to include the rotation and scale of the Transform in the baked Texture2D.</param>
        ///<returns>The number of indices used by the Particle System.</returns>
        public int BakeTexture(ref Texture2D verticesTexture, ParticleSystemBakeTextureOptions options) { return BakeTexture(ref verticesTexture, Camera.main, options); }
        ///<summary>Creates a snapshot of ParticleSystemRenderer and stores it in a <c>Texture2D</c>.</summary>
        ///<param name="verticesTexture">A Texture2D to receive the snapshot of the particle vertices.</param>
        ///<param name="camera">The Camera used to determine which way camera-space particles face.</param>
        ///<param name="options">Specifies whether to include the rotation and scale of the Transform in the baked Texture2D.</param>
        ///<returns>The number of indices used by the Particle System.</returns>
        public int BakeTexture(ref Texture2D verticesTexture, Camera camera, ParticleSystemBakeTextureOptions options)
        {
            if (renderMode == ParticleSystemRenderMode.Mesh)
                throw new System.InvalidOperationException("Baking mesh particles to texture requires supplying an indices texture");

            int indexCount;
            verticesTexture = BakeTextureNoIndicesInternal(verticesTexture, camera, options, out indexCount);
            return indexCount;
        }

        [FreeFunction(Name = "ParticleSystemRendererScriptBindings::BakeTextureNoIndices", HasExplicitThis = true)]
        extern private Texture2D BakeTextureNoIndicesInternal(Texture2D verticesTexture, [NotNull] Camera camera, ParticleSystemBakeTextureOptions options, out int indexCount);

        ///<summary>Creates a snapshot of ParticleSystemRenderer and stores it in a <c>Texture2D</c>.</summary>
        ///<param name="verticesTexture">A Texture2D to receive the snapshot of the particle vertices.</param>
        ///<param name="indicesTexture">An optional Texture2D to receive the snapshot of the particle indices.</param>
        ///<param name="options">Specifies whether to include the rotation and scale of the Transform in the baked Texture2D.</param>
        ///<returns>The number of indices used by the Particle System.</returns>
        public int BakeTexture(ref Texture2D verticesTexture, ref Texture2D indicesTexture, ParticleSystemBakeTextureOptions options) { return BakeTexture(ref verticesTexture, ref indicesTexture, Camera.main, options); }
        ///<summary>Creates a snapshot of ParticleSystemRenderer and stores it in a <c>Texture2D</c>.</summary>
        ///<param name="verticesTexture">A Texture2D to receive the snapshot of the particle vertices.</param>
        ///<param name="indicesTexture">An optional Texture2D to receive the snapshot of the particle indices.</param>
        ///<param name="camera">The Camera used to determine which way camera-space particles face.</param>
        ///<param name="options">Specifies whether to include the rotation and scale of the Transform in the baked Texture2D.</param>
        ///<returns>The number of indices used by the Particle System.</returns>
        public int BakeTexture(ref Texture2D verticesTexture, ref Texture2D indicesTexture, Camera camera, ParticleSystemBakeTextureOptions options)
        {
            int indexCount;
            var output = BakeTextureInternal(verticesTexture, indicesTexture, camera, options, out indexCount);
            verticesTexture = output.vertices;
            indicesTexture = output.indices;
            return indexCount;
        }

        [FreeFunction(Name = "ParticleSystemRendererScriptBindings::BakeTexture", HasExplicitThis = true)]
        extern private BakeTextureOutput BakeTextureInternal(Texture2D verticesTexture, Texture2D indicesTexture, [NotNull] Camera camera, ParticleSystemBakeTextureOptions options, out int indexCount);

        ///<summary>Creates a snapshot of ParticleSystem Trails and stores them in a <c>Texture2D</c>.</summary>
        ///<param name="verticesTexture">A Texture2D to receive the snapshot of the particle trail vertices.</param>
        ///<param name="indicesTexture">A Texture2D to receive the snapshot of the particle trail indices.</param>
        ///<param name="options">Specifies whether to include the rotation and scale of the Transform in the baked Texture2D.</param>
        ///<returns>The number of indices used by the Particle System trails.</returns>
        public int BakeTrailsTexture(ref Texture2D verticesTexture, ref Texture2D indicesTexture, ParticleSystemBakeTextureOptions options) { return BakeTrailsTexture(ref verticesTexture, ref indicesTexture, Camera.main, options); }

        ///<summary>Creates a snapshot of ParticleSystem Trails and stores them in a <c>Texture2D</c>.</summary>
        ///<param name="verticesTexture">A Texture2D to receive the snapshot of the particle trail vertices.</param>
        ///<param name="indicesTexture">A Texture2D to receive the snapshot of the particle trail indices.</param>
        ///<param name="camera">The Camera used to determine which way camera-space particles face.</param>
        ///<param name="options">Specifies whether to include the rotation and scale of the Transform in the baked Texture2D.</param>
        ///<returns>The number of indices used by the Particle System trails.</returns>
        public int BakeTrailsTexture(ref Texture2D verticesTexture, ref Texture2D indicesTexture, Camera camera, ParticleSystemBakeTextureOptions options)
        {
            int indexCount;
            var output = BakeTrailsTextureInternal(verticesTexture, indicesTexture, camera, options, out indexCount);
            verticesTexture = output.vertices;
            indicesTexture = output.indices;
            return indexCount;
        }

        [FreeFunction(Name = "ParticleSystemRendererScriptBindings::BakeTrailsTexture", HasExplicitThis = true)]
        extern private BakeTextureOutput BakeTrailsTextureInternal(Texture2D verticesTexture, Texture2D indicesTexture, [NotNull] Camera camera, ParticleSystemBakeTextureOptions options, out int indexCount);

        // Vertex streams
        ///<summary>The number of currently active custom vertex streams.</summary>
        ///<seealso cref="ParticleSystemRenderer.SetActiveVertexStreams" />
        ///<seealso cref="ParticleSystemRenderer.GetActiveVertexStreams" />
        extern public int activeVertexStreamsCount { get; }
        ///<summary>Enables a set of Vertex Shader streams on the <see cref="ParticleSystemRenderer" />.</summary>
        ///<remarks>
        ///  <para />
        ///  <para>Here is an example of a custom Shader that you can use with the above script:</para>
        ///</remarks>
        ///<param name="streams">The new array of enabled vertex streams.</param>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEditor;
        ///using System.Collections.Generic;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///
        ///    private ParticleSystem ps;
        ///    private ParticleSystemRenderer psr;
        ///    private List<Vector4> customData = new List<Vector4>();
        ///    public float minDist = 30.0f;
        ///
        ///    void Start() {
        ///
        ///        ps = GetComponent<ParticleSystem>();
        ///        psr = GetComponent<ParticleSystemRenderer>();
        ///
        ///        // emit in a sphere with no speed
        ///        var main = ps.main;
        ///        main.startSpeedMultiplier = 0.0f;
        ///        main.simulationSpace = ParticleSystemSimulationSpace.World; // so our particle positions don't require any extra transformation, to compare with the mouse position
        ///        var emission = ps.emission;
        ///        emission.rateOverTimeMultiplier = 200.0f;
        ///        var shape = ps.shape;
        ///        shape.shapeType = ParticleSystemShapeType.Sphere;
        ///        shape.radius = 4.0f;
        ///        psr.sortMode = ParticleSystemSortMode.YoungestInFront;
        ///
        ///        // send custom data to the shader
        ///        psr.SetActiveVertexStreams(new List<ParticleSystemVertexStream>(new ParticleSystemVertexStream[] { ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Normal, ParticleSystemVertexStream.Color, ParticleSystemVertexStream.UV, ParticleSystemVertexStream.Custom1X }));
        ///    }
        ///
        ///    void Update() {
        ///
        ///        Camera mainCam = Camera.main;
        ///
        ///        ps.GetCustomParticleData(customData, ParticleSystemCustomData.Custom1);
        ///
        ///        // If you know the particle count, or have a reasonable maxParticles threshold, consider caching
        ///        // this array instead of reallocating it on every frame, to avoid per-frame garbage.
        ///        int particleCount = ps.particleCount;
        ///        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[particleCount];
        ///
        ///        ps.GetParticles(particles);
        ///
        ///        for (int i = 0; i < particles.Length; i++)
        ///        {
        ///            Vector3 sPos = mainCam.WorldToScreenPoint(particles[i].position);
        ///
        ///            // set custom data to 1, if close enough to the mouse
        ///            if (Vector2.Distance(sPos, Input.mousePosition) < minDist)
        ///            {
        ///                customData[i] = new Vector4(1, 0, 0, 0);
        ///            }
        ///            // otherwise, fade the custom data back to 0
        ///            else
        ///            {
        ///                float particleLife = particles[i].remainingLifetime / ps.main.startLifetimeMultiplier;
        ///
        ///                if (customData[i].x > 0)
        ///                {
        ///                    float x = customData[i].x;
        ///                    x = Mathf.Max(x - Time.deltaTime, 0.0f);
        ///                    customData[i] = new Vector4(x, 0, 0, 0);
        ///                }
        ///            }
        ///        }
        ///
        ///        ps.SetCustomParticleData(customData, ParticleSystemCustomData.Custom1);
        ///    }
        ///
        ///    void OnGUI() {
        ///
        ///        minDist = GUI.HorizontalSlider(new Rect(25, 40, 100, 30), minDist, 0.0f, 100.0f);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example nocheck="true">
        ///  <code><![CDATA[Shader "Particles/CustomVertexStream" {
        ///Properties {
        ///    _TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
        ///    _MainTex ("Particle Texture", 2D) = "white" {}
        ///    _OffsetValue("Offset Value", Range(0,1)) = 0.4
        ///}
        ///
        ///Category {
        ///    Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        ///    Blend SrcAlpha OneMinusSrcAlpha
        ///    ColorMask RGB
        ///    Cull Off Lighting Off ZWrite Off
        ///
        ///    SubShader {
        ///        Pass {
        ///
        ///            CGPROGRAM
        ///            #pragma vertex vert
        ///            #pragma fragment frag
        ///            #pragma multi_compile_particles
        ///            #pragma multi_compile_fog
        ///
        ///            #include "UnityCG.cginc"
        ///
        ///            sampler2D _MainTex;
        ///            fixed4 _TintColor;
        ///            float _OffsetValue;
        ///
        ///            struct appdata_t {
        ///                float4 vertex : POSITION;
        ///                float3 normal : NORMAL;
        ///                fixed4 color : COLOR;
        ///                float3 texcoordAndCustom : TEXCOORD0;
        ///            };
        ///
        ///            struct v2f {
        ///                float4 vertex : SV_POSITION;
        ///                fixed4 color : COLOR;
        ///                float2 texcoord : TEXCOORD0;
        ///                float customData : TEXCOORD1;
        ///                UNITY_FOG_COORDS(2)
        ///            };
        ///
        ///            float4 _MainTex_ST;
        ///
        ///            v2f vert (appdata_t v)
        ///            {
        ///                v.vertex.y = lerp(v.vertex.y, v.vertex.y + _OffsetValue, v.texcoordAndCustom.z);
        ///
        ///                v2f o;
        ///                o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
        ///
        ///                float4 offsetX = float4(-1, 1, 1, -1);
        ///                float4 offsetY = float4(1, 1, -1, -1);
        ///
        ///                o.color = v.color;
        ///                o.texcoord = TRANSFORM_TEX(v.texcoordAndCustom.xy,_MainTex);
        ///                o.customData = v.texcoordAndCustom.z;
        ///                UNITY_TRANSFER_FOG(o,o.vertex);
        ///
        ///                return o;
        ///            }
        ///
        ///            fixed4 frag (v2f i) : SV_Target
        ///            {
        ///                fixed4 col = 2.0f * i.color * _TintColor * tex2D(_MainTex, i.texcoord);
        ///                fixed4 col2 = fixed4(i.customData, 0, 0, col.a);
        ///                fixed4 final = lerp(col, col*col2, i.customData.x);
        ///
        ///                UNITY_APPLY_FOG(i.fogCoord, final);
        ///                return final;
        ///            }
        ///            ENDCG
        ///        }
        ///    }
        ///}
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="ParticleSystemRenderer.GetActiveVertexStreams" />
        [FreeFunction(Name = "ParticleSystemRendererScriptBindings::SetActiveVertexStreams", HasExplicitThis = true)]
        extern public void SetActiveVertexStreams([NotNull] List<ParticleSystemVertexStream> streams);
        ///<summary>Queries which Vertex Shader streams are enabled on the <see cref="ParticleSystemRenderer" />.</summary>
        ///<param name="streams">The array of streams to populate.</param>
        ///<seealso cref="ParticleSystemRenderer.SetActiveVertexStreams" />
        [FreeFunction(Name = "ParticleSystemRendererScriptBindings::GetActiveVertexStreams", HasExplicitThis = true)]
        extern public void GetActiveVertexStreams([NotNull] List<ParticleSystemVertexStream> streams);
        ///<summary>The number of currently active custom trail vertex streams.</summary>
        ///<seealso cref="ParticleSystemRenderer.SetActiveTrailVertexStreams" />
        ///<seealso cref="ParticleSystemRenderer.GetActiveTrailVertexStreams" />
        extern public int activeTrailVertexStreamsCount { get; }
        ///<summary>Enables a set of Vertex Shader streams on the <see cref="ParticleSystemRenderer" /> for particle trails.</summary>
        ///<remarks>
        ///  <para />
        ///  <para>Here is an example of a custom Shader that you can use with the above script:</para>
        ///</remarks>
        ///<param name="streams">The new array of enabled vertex streams.</param>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEditor;
        ///using System.Collections.Generic;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///
        ///    private ParticleSystem ps;
        ///    private ParticleSystemRenderer psr;
        ///    private List<Vector4> customData = new List<Vector4>();
        ///    public float minDist = 30.0f;
        ///
        ///    void Start() {
        ///
        ///        ps = GetComponent<ParticleSystem>();
        ///        psr = GetComponent<ParticleSystemRenderer>();
        ///
        ///        // emit in a sphere with no speed
        ///        var main = ps.main;
        ///        main.startSpeedMultiplier = 0.0f;
        ///        main.simulationSpace = ParticleSystemSimulationSpace.World; // so our particle positions don't require any extra transformation, to compare with the mouse position
        ///        var emission = ps.emission;
        ///        emission.rateOverTimeMultiplier = 200.0f;
        ///        var shape = ps.shape;
        ///        shape.shapeType = ParticleSystemShapeType.Sphere;
        ///        shape.radius = 4.0f;
        ///        psr.sortMode = ParticleSystemSortMode.YoungestInFront;
        ///        var trails = ps.trails;
        ///        trails.enabled = true;
        ///        trails.mode = ParticleSystemTrailMode.Ribbon;
        ///
        ///        // send custom data to the shader
        ///        psr.SetActiveTrailVertexStreams(new List<ParticleSystemVertexStream>(new ParticleSystemVertexStream[] { ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Color, ParticleSystemVertexStream.UV, ParticleSystemVertexStream.Custom1X }));
        ///    }
        ///
        ///    void Update() {
        ///
        ///        Camera mainCam = Camera.main;
        ///
        ///        ps.GetCustomParticleData(customData, ParticleSystemCustomData.Custom1);
        ///
        ///        // If you know the particle count, or have a reasonable maxParticles threshold, consider caching
        ///        // this array instead of reallocating it on every frame, to avoid per-frame garbage.
        ///        int particleCount = ps.particleCount;
        ///        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[particleCount];
        ///
        ///        ps.GetParticles(particles);
        ///
        ///        for (int i = 0; i < particles.Length; i++)
        ///        {
        ///            Vector3 sPos = mainCam.WorldToScreenPoint(particles[i].position);
        ///
        ///            // set custom data to 1, if close enough to the mouse
        ///            if (Vector2.Distance(sPos, Input.mousePosition) < minDist)
        ///            {
        ///                customData[i] = new Vector4(1, 0, 0, 0);
        ///            }
        ///            // otherwise, fade the custom data back to 0
        ///            else
        ///            {
        ///                float particleLife = particles[i].remainingLifetime / ps.main.startLifetimeMultiplier;
        ///
        ///                if (customData[i].x > 0)
        ///                {
        ///                    float x = customData[i].x;
        ///                    x = Mathf.Max(x - Time.deltaTime, 0.0f);
        ///                    customData[i] = new Vector4(x, 0, 0, 0);
        ///                }
        ///            }
        ///        }
        ///
        ///        ps.SetCustomParticleData(customData, ParticleSystemCustomData.Custom1);
        ///    }
        ///
        ///    void OnGUI() {
        ///
        ///        minDist = GUI.HorizontalSlider(new Rect(25, 40, 100, 30), minDist, 0.0f, 100.0f);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example nocheck="true">
        ///  <code><![CDATA[Shader "Particles/CustomTrailVertexStream" {
        ///Properties {
        ///    _TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
        ///    _MainTex ("Particle Texture", 2D) = "white" {}
        ///    _OffsetValue("Offset Value", Range(0,1)) = 0.4
        ///}
        ///
        ///Category {
        ///    Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        ///    Blend SrcAlpha OneMinusSrcAlpha
        ///    ColorMask RGB
        ///    Cull Off Lighting Off ZWrite Off
        ///
        ///    SubShader {
        ///        Pass {
        ///
        ///            CGPROGRAM
        ///            #pragma vertex vert
        ///            #pragma fragment frag
        ///            #pragma multi_compile_particles
        ///            #pragma multi_compile_fog
        ///
        ///            #include "UnityCG.cginc"
        ///
        ///            sampler2D _MainTex;
        ///            fixed4 _TintColor;
        ///            float _OffsetValue;
        ///
        ///            struct appdata_t {
        ///                float4 vertex : POSITION;
        ///                fixed4 color : COLOR;
        ///                float3 texcoordAndCustom : TEXCOORD0;
        ///            };
        ///
        ///            struct v2f {
        ///                float4 vertex : SV_POSITION;
        ///                fixed4 color : COLOR;
        ///                float2 texcoord : TEXCOORD0;
        ///                float customData : TEXCOORD1;
        ///                UNITY_FOG_COORDS(2)
        ///            };
        ///
        ///            float4 _MainTex_ST;
        ///
        ///            v2f vert (appdata_t v)
        ///            {
        ///                v.vertex.y = lerp(v.vertex.y, v.vertex.y + _OffsetValue, v.texcoordAndCustom.z);
        ///
        ///                v2f o;
        ///                o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
        ///
        ///                float4 offsetX = float4(-1, 1, 1, -1);
        ///                float4 offsetY = float4(1, 1, -1, -1);
        ///
        ///                o.color = v.color;
        ///                o.texcoord = TRANSFORM_TEX(v.texcoordAndCustom.xy,_MainTex);
        ///                o.customData = v.texcoordAndCustom.z;
        ///                UNITY_TRANSFER_FOG(o,o.vertex);
        ///
        ///                return o;
        ///            }
        ///
        ///            fixed4 frag (v2f i) : SV_Target
        ///            {
        ///                fixed4 col = 2.0f * i.color * _TintColor * tex2D(_MainTex, i.texcoord);
        ///                fixed4 col2 = fixed4(i.customData, 0, 0, col.a);
        ///                fixed4 final = lerp(col, col*col2, i.customData.x);
        ///
        ///                UNITY_APPLY_FOG(i.fogCoord, final);
        ///                return final;
        ///            }
        ///            ENDCG
        ///        }
        ///    }
        ///}
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="ParticleSystemRenderer.GetActiveTrailVertexStreams" />
        [FreeFunction(Name = "ParticleSystemRendererScriptBindings::SetActiveTrailVertexStreams", HasExplicitThis = true)]
        extern public void SetActiveTrailVertexStreams([NotNull] List<ParticleSystemVertexStream> streams);
        ///<summary>Queries which trail Vertex Shader streams are enabled on the <see cref="ParticleSystemRenderer" />.</summary>
        ///<param name="streams">The array of streams to populate.</param>
        ///<seealso cref="ParticleSystemRenderer.SetActiveTrailVertexStreams" />
        [FreeFunction(Name = "ParticleSystemRendererScriptBindings::GetActiveTrailVertexStreams", HasExplicitThis = true)]
        extern public void GetActiveTrailVertexStreams([NotNull] List<ParticleSystemVertexStream> streams);

        extern internal bool editorEnabled { get; set; }
        ///<summary>Determines whether the Particle System can be rendered using GPU Instancing.</summary>
        extern public bool supportsMeshInstancing { get; }
        extern internal void ConfigureTrailMaterialSlot(bool trailsEnabled);
    }
}
