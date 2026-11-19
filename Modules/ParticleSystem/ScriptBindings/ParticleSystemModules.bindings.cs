// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEngine
{
    [NativeHeader("ParticleSystemScriptingClasses.h")]
    [NativeHeader("Modules/ParticleSystem/ParticleSystem.h")]
    [NativeHeader("Modules/ParticleSystem/ScriptBindings/ParticleSystemScriptBindings.h")]
    [NativeHeader("Modules/ParticleSystem/ScriptBindings/ParticleSystemModulesScriptBindings.h")]
    public partial class ParticleSystem : Component
    {
        // Modules
        public partial struct MainModule
        {
            internal MainModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            ///<summary>The current Particle System velocity.</summary>
            ///<remarks>If you set this property to a particular value, the <see cref="emitterVelocityMode" /> automatically switches to <see cref="ParticleSystemEmitterVelocityMode.Custom" />.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class Example : MonoBehaviour
            ///{
            ///    ParticleSystem.MainModule mainModule;
            ///
            ///    void Start()
            ///    {
            ///        var particleSystem = GetComponent<ParticleSystem>();
            ///        mainModule = particleSystem.main;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUILayout.Label("Velocity: " + mainModule.emitterVelocity);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public Vector3 emitterVelocity { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>The duration of the Particle System in seconds.</summary>
            ///<remarks>You can only set this property when the Particle System is not playing.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///        ps.Stop(); // Cannot set duration whilst Particle System is playing
            ///
            ///        var main = ps.main;
            ///        main.duration = 10.0f;
            ///
            ///        ps.Play();
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float duration { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Specifies whether the Particle System is looping.</summary>
            ///<remarks>If you disable looping on a playing Particle System, it stops at the end of the current loop.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.loop = true;
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public bool loop { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>If <see cref="ParticleSystem.MainModule.loop" /> is true, when you enable this property, the Particle System looks like it has already simulated for one loop when first becoming visible.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool usePrewarm;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.loop = true;   // prewarm only works on looping systems
            ///
            ///        Restart();
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        bool newPrewarm = GUI.Toggle(new Rect(10, 60, 200, 30), usePrewarm, "Use Prewarm");
            ///
            ///        if (newPrewarm != usePrewarm)
            ///        {
            ///            usePrewarm = newPrewarm;
            ///            Restart();
            ///        }
            ///    }
            ///
            ///    void Restart()
            ///    {
            ///        ps.Stop();
            ///        ps.Clear();
            ///
            ///        var main = ps.main;
            ///        main.prewarm = usePrewarm;
            ///
            ///        ps.Play();
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public bool prewarm { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Start delay in seconds.</summary>
            ///<remarks>Use this to delay when the playback starts on the system.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    private bool restart;
            ///    public float hSliderValue = 0.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        if (restart)
            ///        {
            ///            ps.Stop();
            ///            ps.Clear();
            ///
            ///            var main = ps.main;
            ///            main.startDelay = hSliderValue;
            ///
            ///            ps.Play();
            ///
            ///            restart = false;
            ///        }
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 0.0F, 5.0F);
            ///        restart = GUI.Button(new Rect(25, 75, 100, 30), "Restart");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve startDelay { get => startDelayBlittable; set => startDelayBlittable = value; }
            [NativeName("StartDelay")] private extern MinMaxCurveBlittable startDelayBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>A multiplier for <see cref="ParticleSystem.MainModule.startDelay" /> in seconds.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall start delay multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    private bool restart;
            ///    public float hSliderValue = 0.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        if (restart)
            ///        {
            ///            ps.Stop();
            ///            ps.Clear();
            ///
            ///            var main = ps.main;
            ///            main.startDelayMultiplier = hSliderValue;
            ///
            ///            ps.Play();
            ///
            ///            restart = false;
            ///        }
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 0.0F, 5.0F);
            ///        restart = GUI.Button(new Rect(25, 75, 100, 30), "Restart");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float startDelayMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>The total lifetime in seconds that each new particle has.</summary>
            ///<remarks>This value is set on the particle when the Particle System creates it. Assign a value of float.PositiveInfinity to particles to prevent the Particle System from destroying them. This gives the particles an infinite lifespan.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startLifetime = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 0.0F, 5.0F);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve startLifetime { get => startLifetimeBlittable; set => startLifetimeBlittable = value; }
            [NativeName("StartLifetime")] private extern MinMaxCurveBlittable startLifetimeBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>A multiplier for <see cref="ParticleSystem.MainModule.startLifetime" />.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall lifetime multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startLifetimeMultiplier = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 0.0F, 5.0F);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float startLifetimeMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>The initial speed of particles when the Particle System first spawns them.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startSpeed = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 0.0F, 10.0F);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve startSpeed { get => startSpeedBlittable; set => startSpeedBlittable = value; }
            [NativeName("StartSpeed")] private extern MinMaxCurveBlittable startSpeedBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>A multiplier for <see cref="ParticleSystem.MainModule.startSpeed" />.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall speed multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startSpeedMultiplier = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 0.0F, 10.0F);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float startSpeedMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>A flag to enable specifying particle size individually for each axis.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueX = 1.0F;
            ///    public float hSliderValueY = 1.0F;
            ///    public float hSliderValueZ = 1.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startSize3D = true;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.renderMode = ParticleSystemRenderMode.Mesh;
            ///        psr.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");// use a mesh, because billboards have no Z axis size
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startSizeXMultiplier = hSliderValueX;
            ///        main.startSizeYMultiplier = hSliderValueY;
            ///        main.startSizeZMultiplier = hSliderValueZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValueX = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValueX, 0.0F, 360.0F * Mathf.Deg2Rad);
            ///        hSliderValueY = GUI.HorizontalSlider(new Rect(25, 75, 100, 30), hSliderValueY, 0.0F, 360.0F * Mathf.Deg2Rad);
            ///        hSliderValueZ = GUI.HorizontalSlider(new Rect(25, 105, 100, 30), hSliderValueZ, 0.0F, 360.0F * Mathf.Deg2Rad);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            extern public bool startSize3D { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>The initial size of particles when the Particle System first spawns them.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startSize = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 0.0F, 10.0F);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve startSize { get => startSizeBlittable; set => startSizeBlittable = value; }
            [NativeName("StartSizeX")] private extern MinMaxCurveBlittable startSizeBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>A multiplier for the initial size of particles when the Particle System first spawns them.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall size multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startSizeMultiplier = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 0.0F, 10.0F);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            [NativeName("StartSizeXMultiplier")]
            extern public float startSizeMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>The initial size of particles along the x-axis when the Particle System first spawns them.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startSize3D = true;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startSizeX = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 0.0F, 10.0F);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve startSizeX { get => startSizeXBlittable; set => startSizeXBlittable = value; }
            [NativeName("StartSizeX")] private extern MinMaxCurveBlittable startSizeXBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>A multiplier for <see cref="ParticleSystem.MainModule.startSizeX" />.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall size multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startSize3D = true;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startSizeXMultiplier = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 0.0F, 10.0F);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float startSizeXMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>The initial size of particles along the y-axis when the Particle System first spawns them.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startSize3D = true;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startSizeY = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 0.0F, 10.0F);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve startSizeY { get => startSizeYBlittable; set => startSizeYBlittable = value; }
            [NativeName("StartSizeY")] private extern MinMaxCurveBlittable startSizeYBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>A multiplier for <see cref="ParticleSystem.MainModule.startSizeY" />.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall size multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startSize3D = true;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startSizeYMultiplier = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 0.0F, 10.0F);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float startSizeYMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>The initial size of particles along the z-axis when the Particle System first spawns them.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startSize3D = true;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.renderMode = ParticleSystemRenderMode.Mesh;
            ///        psr.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");// use a mesh, because billboards have no Z axis size
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startSizeZ = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 0.0F, 10.0F);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve startSizeZ { get => startSizeZBlittable; set => startSizeZBlittable = value; }
            [NativeName("StartSizeZ")] private extern MinMaxCurveBlittable startSizeZBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>A multiplier for <see cref="ParticleSystem.MainModule.startSizeZ" />.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall size multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startSize3D = true;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.renderMode = ParticleSystemRenderMode.Mesh;
            ///        psr.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");// use a mesh, because billboards have no Z axis size
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startSizeZMultiplier = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 0.0F, 10.0F);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float startSizeZMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>A flag to enable 3D particle rotation.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueX = 0.0F;
            ///    public float hSliderValueY = 0.0F;
            ///    public float hSliderValueZ = 0.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startRotation3D = true;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, so we can see the rotation
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startRotationXMultiplier = hSliderValueX;
            ///        main.startRotationYMultiplier = hSliderValueY;
            ///        main.startRotationZMultiplier = hSliderValueZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValueX = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValueX, 0.0F, 360.0F * Mathf.Deg2Rad);
            ///        hSliderValueY = GUI.HorizontalSlider(new Rect(25, 75, 100, 30), hSliderValueY, 0.0F, 360.0F * Mathf.Deg2Rad);
            ///        hSliderValueZ = GUI.HorizontalSlider(new Rect(25, 105, 100, 30), hSliderValueZ, 0.0F, 360.0F * Mathf.Deg2Rad);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            extern public bool startRotation3D { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>The initial rotation of particles when the Particle System first spawns them.</summary>
            ///<remarks>Note that you should specify the value in radians.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, so we can see the rotation
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startRotation = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 0.0F, 360.0F * Mathf.Deg2Rad);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve startRotation { get => startRotationBlittable; set => startRotationBlittable = value; }
            [NativeName("StartRotationZ")] private extern MinMaxCurveBlittable startRotationBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>A multiplier for <see cref="ParticleSystem.MainModule.startRotation" />.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall rotation multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, so we can see the rotation
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startRotationMultiplier = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 0.0F, 360.0F * Mathf.Deg2Rad);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            [NativeName("StartRotationZMultiplier")]
            extern public float startRotationMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>The initial rotation of particles around the x-axis when emitted.</summary>
            ///<remarks>Note that you should specify the value in radians.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startRotation3D = true;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, so we can see the rotation
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startRotationX = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 0.0F, 360.0F * Mathf.Deg2Rad);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve startRotationX { get => startRotationXBlittable; set => startRotationXBlittable = value; }
            [NativeName("StartRotationX")] private extern MinMaxCurveBlittable startRotationXBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>The initial rotation multiplier of particles around the x-axis when the Particle System first spawns them.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall rotation multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startRotation3D = true;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, so we can see the rotation
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startRotationXMultiplier = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 0.0F, 360.0F * Mathf.Deg2Rad);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float startRotationXMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>The initial rotation of particles around the y-axis when the Particle System first spawns them.</summary>
            ///<remarks>Note that you should specify the value in radians.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startRotation3D = true;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, so we can see the rotation
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startRotationY = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 0.0F, 360.0F * Mathf.Deg2Rad);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve startRotationY { get => startRotationYBlittable; set => startRotationYBlittable = value; }
            [NativeName("StartRotationY")] private extern MinMaxCurveBlittable startRotationYBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>The initial rotation multiplier of particles around the y-axis when the Particle System first spawns them..</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall rotation multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startRotation3D = true;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, so we can see the rotation
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startRotationYMultiplier = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 0.0F, 360.0F * Mathf.Deg2Rad);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float startRotationYMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>The initial rotation of particles around the z-axis when the Particle System first spawns them</summary>
            ///<remarks>Note that you should specify the value in radians..</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startRotation3D = true;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, so we can see the rotation
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startRotationZ = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 0.0F, 360.0F * Mathf.Deg2Rad);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve startRotationZ { get => startRotationZBlittable; set => startRotationZBlittable = value; }
            [NativeName("StartRotationZ")] private extern MinMaxCurveBlittable startRotationZBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>The initial rotation multiplier of particles around the z-axis when the Particle System first spawns them.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall rotation multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startRotation3D = true;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, so we can see the rotation
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startRotationZMultiplier = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 0.0F, 360.0F * Mathf.Deg2Rad);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float startRotationZMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Makes some particles spin in the opposite direction.</summary>
            ///<remarks>Set between 0 and 1. Higher values cause a higher proportion of particles to spin in the opposite direction.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 0.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var rot = ps.rotationOverLifetime;
            ///        rot.enabled = true;
            ///        rot.zMultiplier = 90.0f * Mathf.Deg2Rad;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, so we can see the rotation
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.flipRotation = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 25, 100, 30), hSliderValue, 0.0F, 1.0F);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float flipRotation { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>The initial color of particles when the Particle System first spawns them.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueR = 0.0F;
            ///    public float hSliderValueG = 0.0F;
            ///    public float hSliderValueB = 0.0F;
            ///    public float hSliderValueA = 1.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startColor = new Color(hSliderValueR, hSliderValueG, hSliderValueB, hSliderValueA);
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Red");
            ///        GUI.Label(new Rect(25, 70, 100, 30), "Green");
            ///        GUI.Label(new Rect(25, 100, 100, 30), "Blue");
            ///        GUI.Label(new Rect(25, 130, 100, 30), "Alpha");
            ///
            ///        hSliderValueR = GUI.HorizontalSlider(new Rect(95, 45, 100, 30), hSliderValueR, 0.0F, 1.0F);
            ///        hSliderValueG = GUI.HorizontalSlider(new Rect(95, 75, 100, 30), hSliderValueG, 0.0F, 1.0F);
            ///        hSliderValueB = GUI.HorizontalSlider(new Rect(95, 105, 100, 30), hSliderValueB, 0.0F, 1.0F);
            ///        hSliderValueA = GUI.HorizontalSlider(new Rect(95, 135, 100, 30), hSliderValueA, 0.0F, 1.0F);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxGradient" />
            public MinMaxGradient startColor { get => startColorBlittable; set => startColorBlittable = value; }
            [NativeName("StartColor")] private extern MinMaxGradientBlittable startColorBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Specify whether to use the gravity strength from the 2D or 3D physics system.</summary>
            ///<seealso cref="ParticleSystem.MainModule.gravityModifier" />
            extern public ParticleSystemGravitySource gravitySource { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>A scale that this Particle System applies to gravity, defined either by <see cref="P:UnityEngine.Physics.gravity" /> or <see cref="P:UnityEngine.Physics2D.gravity" />.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 0.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.gravityModifier = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, -5.0F, 5.0F);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            ///<seealso cref="ParticleSystem.MainModule.gravitySource" />
            public MinMaxCurve gravityModifier { get => gravityModifierBlittable; set => gravityModifierBlittable = value; }
            [NativeName("GravityModifier")] private extern MinMaxCurveBlittable gravityModifierBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Change the gravity multiplier.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve (<see cref="ParticleSystem.MainModule.gravityModifier" />), if you only want to change the overall gravity multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 0.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.gravityModifierMultiplier = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, -5.0F, 5.0F);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float gravityModifierMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>This selects the space in which to simulate particles. It can be either world or local space.</summary>
            ///<remarks>
            ///  <para>Toggle between local and world space simulation using the following example:</para>
            ///  <para>Simulate particles relative to an independent game object using the following example:</para>
            ///</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool useLocal = true;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///        var main = ps.main;
            ///        useLocal = main.simulationSpace == ParticleSystemSimulationSpace.Local;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.simulationSpace = useLocal ? ParticleSystemSimulationSpace.Local : ParticleSystemSimulationSpace.World;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        useLocal = GUI.Toggle(new Rect(10, 60, 200, 30), useLocal, "Use Local Simulation Space");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<example>
            ///  <code><![CDATA[
            ///using System.Collections;
            ///using System.Collections.Generic;
            ///using UnityEngine;
            ///
            ///public class ParticleSystemScript : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public Transform relativeTo;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.simulationSpace = ParticleSystemSimulationSpace.Custom;
            ///        main.customSimulationSpace = relativeTo;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public ParticleSystemSimulationSpace simulationSpace { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Simulate particles relative to a custom transform component.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using System.Collections;
            ///using UnityEngine;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public Transform relativeTo;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.simulationSpace = ParticleSystemSimulationSpace.Custom;
            ///        main.customSimulationSpace = relativeTo;
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystemSimulationSpace.Custom" />
            extern public Transform customSimulationSpace { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Override the default playback speed of the Particle System.</summary>
            ///<remarks>Useful for speeding up or slowing down the entire simulation.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.simulationSpeed = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 0.0F, 5.0F);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float simulationSpeed { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>When true, use the unscaled delta time to simulate the Particle System. Otherwise, use the scaled delta time.</summary>
            ///<remarks>This is useful for playing effects whilst the game is paused and <see cref="P:UnityEngine.Time.timeScale" /> is set to zero.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0f;
            ///    public bool useUnscaledTime = false;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.useUnscaledTime = useUnscaledTime;
            ///
            ///        Time.timeScale = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Time Scale");
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(105, 45, 100, 30), hSliderValue, 0.0F, 10.0F);
            ///        useUnscaledTime = GUI.Toggle(new Rect(25, 75, 100, 30), useUnscaledTime, "Use Unscaled Time");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public bool useUnscaledTime { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Control how the Particle System applies its Transform component to the particles it emits.</summary>
            ///<remarks>Hierarchy: Scale according to its Transform and all its parents.
            ///Local: Scale using only its own Transform, ignoring all parents.
            ///Shape: Only apply scale to the source positions of the particles, but not their size. The source positions are defined by the Shape module.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            /// // add this to a Particle System which has a parent game object, to see how each scaling mode works
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float sliderValue = 1.0F;
            ///    public float parentSliderValue = 1.0F;
            ///    public ParticleSystemScalingMode scaleMode;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        ps.transform.localScale = new Vector3(sliderValue, sliderValue, sliderValue);
            ///        if (ps.transform.parent != null)
            ///            ps.transform.parent.localScale = new Vector3(parentSliderValue, parentSliderValue, parentSliderValue);
            ///
            ///        var main = ps.main;
            ///        main.scalingMode = scaleMode;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        scaleMode = (ParticleSystemScalingMode)GUI.SelectionGrid(new Rect(25, 25, 300, 30), (int)scaleMode, new GUIContent[] { new GUIContent("Hierarchy"), new GUIContent("Local"), new GUIContent("Shape") }, 3);
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Scale");
            ///        sliderValue = GUI.HorizontalSlider(new Rect(125, 85, 100, 30), sliderValue, 0.0F, 5.0F);
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Parent Scale");
            ///        parentSliderValue = GUI.HorizontalSlider(new Rect(125, 125, 100, 30), parentSliderValue, 0.0F, 5.0F);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public ParticleSystemScalingMode scalingMode { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>If set to true, the Particle System automatically begins to play on startup.</summary>
            ///<remarks>Note that every Particle System in the current particle effect shares this setting.</remarks>
            extern public bool playOnAwake { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>The maximum number of particles to emit.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    public float hSliderValue = 0.0F;
            ///    private ParticleSystem ps;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.maxParticles = Mathf.RoundToInt(hSliderValue);
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 25, 100, 30), hSliderValue, 0.0F, 100.0F);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public int maxParticles { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Control how the Particle System calculates its velocity, when moving in the world.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public ParticleSystemEmitterVelocityMode velocityMode;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.emitterVelocityMode = velocityMode;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        velocityMode = (ParticleSystemEmitterVelocityMode)GUI.SelectionGrid(new Rect(25, 25, 300, 30), (int)velocityMode, new GUIContent[] { new GUIContent("Transform"), new GUIContent("Rigidbody") }, 2);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public ParticleSystemEmitterVelocityMode emitterVelocityMode { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Select whether to Disable or Destroy the GameObject, or to call the <see cref="M:UnityEngine.MonoBehaviour.OnParticleSystemStopped" /> script Callback, when the Particle System stops and all particles have died.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///        ps.Stop();
            ///
            ///        var main = ps.main;
            ///        main.loop = false;
            ///        main.duration = 1.0f;
            ///        main.stopAction = ParticleSystemStopAction.Destroy;
            ///
            ///        ps.Play();
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public ParticleSystemStopAction stopAction { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Configure the Particle System to not kill its particles when their lifetimes are exceeded.</summary>
            ///<remarks>Rather than using the particle lifetimes to kill particles, the system replaces particles with new ones when there are more particles than specified in Max Particles.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.ringBufferMode = ParticleSystemRingBufferMode.PauseUntilReplaced;
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MainModule.ringBufferLoopRange" />
            extern public ParticleSystemRingBufferMode ringBufferMode { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>When <see cref="ParticleSystem.MainModule.ringBufferMode" /> is set to loop, this value defines the proportion of the particle life that loops.</summary>
            ///<remarks>This enables you to use other particle properties that are applied over the particle lifetimes, such as SizeOverLifetime. When the system must replace a particle, it plays the particle from its current age to its full lifetime. Then, removes it.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.ringBufferLoopRange = new Vector2(0.1f, 0.6f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public Vector2 ringBufferLoopRange { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Configure whether the Particle System will still be simulated each frame, when it is offscreen.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public ParticleSystemCullingMode cullingMode { get; [NativeMethod(ThrowsException = true)] set; }
        }

        public partial struct EmissionModule
        {
            internal EmissionModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            ///<summary>Specifies whether the EmissionModule is enabled or disabled.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool moduleEnabled;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var emission = ps.emission;
            ///        emission.enabled = moduleEnabled;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        moduleEnabled = GUI.Toggle(new Rect(25, 45, 100, 30), moduleEnabled, "Enabled");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.emission" />
            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>The rate at which the emitter spawns new particles over time.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 5.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var emission = ps.emission;
            ///        emission.rateOverTime = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 5.0f, 200.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            ///<seealso cref="ParticleSystem.EmissionModule.rateOverDistance" />
            public MinMaxCurve rateOverTime { get => rateOverTimeBlittable; set => rateOverTimeBlittable = value; }
            [NativeName("RateOverTime")] private extern MinMaxCurveBlittable rateOverTimeBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Change the rate over time multiplier.</summary>
            ///<remarks>This is more efficient than accessing the whole curve, if you only want to change the overall rate multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 5.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var emission = ps.emission;
            ///        emission.rateOverTimeMultiplier = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 5.0f, 200.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float rateOverTimeMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>The rate at which the emitter spawns new particles over distance.</summary>
            ///<remarks>The emitter only spawns new particles when it moves. If the system's GameObject contains a Rigidbody or Rigidbody2D component, and the system's **Emitter Velocity** property is set to **Rigidbody**, Unity calculates the distance based on the velocity of the Rigidbody. Otherwise, Unity calculates the distance based on how far the GameObject's Transform component has moved since the last update.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 5.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.simulationSpace = ParticleSystemSimulationSpace.World;
            ///
            ///        var shape = ps.shape;
            ///        shape.enabled = false;
            ///
            ///        var emission = ps.emission;
            ///        emission.rateOverTime = 0.0f;
            ///
            ///        // add a sphere so we can see our transform position as it moves
            ///        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ///        sphere.transform.parent = ps.transform;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var emission = ps.emission;
            ///        emission.rateOverDistance = hSliderValue;
            ///
            ///        ps.transform.position = new Vector3(Mathf.Sin(Time.time) * 2.0f, 0.0f, 0.0f);
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 1.0f, 20.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            ///<seealso cref="ParticleSystem.EmissionModule.rateOverTime" />
            ///<seealso cref="ParticleSystem.MainModule.emitterVelocityMode" />
            public MinMaxCurve rateOverDistance { get => rateOverDistanceBlittable; set => rateOverDistanceBlittable = value; }
            [NativeName("RateOverDistance")] private extern MinMaxCurveBlittable rateOverDistanceBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Change the rate over distance multiplier.</summary>
            ///<remarks>This is more efficient than accessing the whole curve, if you only want to change the overall rate multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 5.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.simulationSpace = ParticleSystemSimulationSpace.World;
            ///
            ///        var shape = ps.shape;
            ///        shape.enabled = false;
            ///
            ///        var emission = ps.emission;
            ///        emission.rateOverTime = 0.0f;
            ///
            ///        // add a sphere so we can see our transform position as it moves
            ///        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ///        sphere.transform.parent = ps.transform;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var emission = ps.emission;
            ///        emission.rateOverDistanceMultiplier = hSliderValue;
            ///
            ///        ps.transform.position = new Vector3(Mathf.Sin(Time.time) * 2.0f, 0.0f, 0.0f);
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 1.0f, 20.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float rateOverDistanceMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Sets the burst array.</summary>
            ///<param name="bursts">Array of bursts.</param>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var emission = ps.emission;
            ///        emission.enabled = true;
            ///        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 100, 200), new ParticleSystem.Burst(1.0f, 10, 20) });
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.Burst" />
            ///<seealso cref="ParticleSystem.EmissionModule.GetBursts" />
            public void SetBursts(Burst[] bursts)
            {
                SetBursts(bursts, bursts.Length);
            }

            ///<summary>Sets the burst array.</summary>
            ///<param name="bursts">Array of bursts.</param>
            ///<param name="size">Optional array size for if the burst count is less than the array size.</param>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var emission = ps.emission;
            ///        emission.enabled = true;
            ///        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 100, 200), new ParticleSystem.Burst(1.0f, 10, 20) });
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.Burst" />
            ///<seealso cref="ParticleSystem.EmissionModule.GetBursts" />
            public void SetBursts(Burst[] bursts, int size)
            {
                burstCount = size;
                for (int i = 0; i < size; i++)
                    SetBurst(i, bursts[i]);
            }

            ///<summary>Gets the burst array.</summary>
            ///<param name="bursts">Array of bursts to fill.</param>
            ///<returns>The number of bursts in the array.</returns>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 5.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var emission = ps.emission;
            ///        emission.enabled = true;
            ///        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 100) });
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var emission = ps.emission;
            ///        ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[emission.burstCount];
            ///        emission.GetBursts(bursts);
            ///
            ///        var main = ps.main;
            ///        bursts[0].minCount = bursts[0].maxCount = (short)hSliderValue;
            ///
            ///        emission.SetBursts(bursts);
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 5.0f, 200.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.Burst" />
            ///<seealso cref="M:UnityEngine.ParticleSystem.EmissionModule.SetBursts" />
            public int GetBursts(Burst[] bursts)
            {
                int returnValue = burstCount;
                for (int i = 0; i < returnValue; i++)
                    bursts[i] = GetBurst(i);
                return returnValue;
            }

            ///<summary>Sets a single burst in the array of bursts.</summary>
            ///<param name="index">The index of the burst to set.</param>
            ///<param name="burst">The new burst data to apply to the Particle System.</param>
            ///<seealso cref="ParticleSystem.Burst" />
            ///<seealso cref="M:UnityEngine.ParticleSystem.EmissionModule.SetBursts" />
            [NativeMethod(ThrowsException = true)]
            extern public void SetBurst(int index, Burst burst);
            ///<summary>Gets a single burst from the array of bursts.</summary>
            ///<param name="index">The index of the burst to retrieve.</param>
            ///<returns>The burst data at the given index.</returns>
            ///<seealso cref="ParticleSystem.Burst" />
            ///<seealso cref="ParticleSystem.EmissionModule.GetBursts" />
            [NativeMethod(ThrowsException = true)]
            extern public Burst GetBurst(int index);
            ///<summary>The current number of bursts.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 5.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var emission = ps.emission;
            ///        emission.enabled = true;
            ///        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 100) });
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var emission = ps.emission;
            ///        ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[emission.burstCount];
            ///        emission.GetBursts(bursts);
            ///
            ///        var main = ps.main;
            ///        bursts[0].minCount = bursts[0].maxCount = (short)hSliderValue;
            ///
            ///        emission.SetBursts(bursts);
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 5.0f, 200.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.Burst" />
            ///<seealso cref="ParticleSystem.EmissionModule.GetBursts" />
            ///<seealso cref="M:UnityEngine.ParticleSystem.EmissionModule.SetBursts" />
            extern public int burstCount { get; [NativeMethod(ThrowsException = true)] set; }
        }

        public partial struct ShapeModule
        {
            internal ShapeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            ///<summary>Specifies whether the ShapeModule is enabled or disabled.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool moduleEnabled = true;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var shape = ps.shape;
            ///        shape.enabled = moduleEnabled;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        moduleEnabled = GUI.Toggle(new Rect(25, 25, 200, 30), moduleEnabled, "Enabled");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.shape" />
            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>The type of shape to emit particles from.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System;
            ///using System.Collections;
            ///using System.Collections.Generic;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public ParticleSystemShapeType shapeType = ParticleSystemShapeType.Cone;
            ///    private int shapeTypeIndex = 2;
            ///    public float arc = 360.0f;
            ///    public ParticleSystemShapeMultiModeValue arcMode = ParticleSystemShapeMultiModeValue.Random;
            ///    public float arcSpread = 0.0f;
            ///    public float arcSpeed = 1.0f;
            ///    public float angle = 25.0f;
            ///    public float radius = 1.0f;
            ///    public float radiusThickness = 1.0f;
            ///    public ParticleSystemShapeMultiModeValue radiusMode = ParticleSystemShapeMultiModeValue.Random;
            ///    public float radiusSpread = 0.0f;
            ///    public float radiusSpeed = 1.0f;
            ///    public float donutRadius = 0.2f;
            ///    public float length = 2.0f;
            ///    public Vector3 boxThickness = new Vector3(0.0f, 0.0f, 0.0f);
            ///    public ParticleSystemMeshShapeType meshShapeType;
            ///    public float normalOffset = 0.0f;
            ///    public float randomizeDirection = 0.0f;
            ///    public float spherizeDirection = 0.0f;
            ///    public float randomizePosition = 0.0f;
            ///    public Vector3 position = new Vector3(0.0f, 0.0f, 0.0f);
            ///    public Vector3 rotation = new Vector3(0.0f, 0.0f, 0.0f);
            ///    public Vector3 scale = new Vector3(1.0f, 1.0f, 1.0f);
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startSpeed = 0.1f;
            ///        main.startSize = 0.1f;
            ///        main.startLifetime = 1.0f;
            ///
            ///        var emission = ps.emission;
            ///        emission.rateOverTime = 500.0f;
            ///
            ///        var shape = ps.shape;
            ///        shape.mesh = Resources.GetBuiltinResource<Mesh>("Capsule.fbx");
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var shape = ps.shape;
            ///        shape.shapeType = shapeType;
            ///        shape.arc = arc;
            ///        shape.arcMode = arcMode;
            ///        shape.arcSpread = arcSpread;
            ///        shape.arcSpeed = arcSpeed;
            ///        shape.angle = angle;
            ///        shape.radius = radius;
            ///        shape.radiusMode = radiusMode;
            ///        shape.radiusSpread = radiusSpread;
            ///        shape.radiusSpeed = radiusSpeed;
            ///        shape.radiusThickness = radiusThickness;
            ///        shape.donutRadius = donutRadius;
            ///        shape.length = length;
            ///        shape.boxThickness = boxThickness;
            ///        shape.meshShapeType = meshShapeType;
            ///        shape.normalOffset = normalOffset;
            ///        shape.randomDirectionAmount = randomizeDirection;
            ///        shape.sphericalDirectionAmount = spherizeDirection;
            ///        shape.randomPositionAmount = randomizePosition;
            ///        shape.position = position;
            ///        shape.rotation = rotation;
            ///        shape.scale = scale;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        List<GUIContent> content = new List<GUIContent>();
            ///        for (int i = 0; i < (int)ParticleSystemShapeType.SpriteRenderer + 1; i++)
            ///        {
            ///            ParticleSystemShapeType currentShapeType = (ParticleSystemShapeType)i;
            ///            var obsoleteAttribute = Attribute.GetCustomAttribute(currentShapeType.GetType().GetField(currentShapeType.ToString()), typeof(ObsoleteAttribute), false);   // skip the obsolete shape types
            ///            if (obsoleteAttribute == null)
            ///                content.Add(new GUIContent(currentShapeType.ToString(), i.ToString()));
            ///        }
            ///        shapeTypeIndex = GUI.SelectionGrid(new Rect(25, 25, 1000, 80), shapeTypeIndex, content.ToArray(), content.Count / 3);
            ///        shapeType = (ParticleSystemShapeType)int.Parse(content[shapeTypeIndex].tooltip);
            ///
            ///        float y = 120.0f;
            ///        float spacing = 40.0f;
            ///
            ///        if (shapeType == ParticleSystemShapeType.Sphere || shapeType == ParticleSystemShapeType.Hemisphere)
            ///        {
            ///            GUI.Label(new Rect(25, y, 140, 30), "Radius");
            ///            radius = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), radius, 1.0f, 5.0f);
            ///            y += spacing;
            ///
            ///            GUI.Label(new Rect(25, y, 140, 30), "Radius Thickness");
            ///            radiusThickness = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), radiusThickness, 0.0f, 1.0f);
            ///            y += spacing;
            ///
            ///            GUI.Label(new Rect(25, y, 140, 30), "Arc");
            ///            arc = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), arc, 1.0f, 360.0f);
            ///            y += spacing;
            ///
            ///            GUI.Label(new Rect(25, y, 140, 30), "Arc Mode");
            ///            arcMode = (ParticleSystemShapeMultiModeValue)GUI.SelectionGrid(new Rect(165, 280, 360, 20), (int)arcMode, new GUIContent[] { new GUIContent("Random"), new GUIContent("Loop"), new GUIContent("Ping-Pong"), new GUIContent("Burst Spread") }, 4);
            ///            y += spacing;
            ///
            ///            GUI.Label(new Rect(25, y, 140, 30), "Arc Spread");
            ///            arcSpread = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), arcSpread, 0.0f, 1.0f);
            ///            y += spacing;
            ///
            ///            GUI.Label(new Rect(25, y, 140, 30), "Arc Speed");
            ///            arcSpeed = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), arcSpeed, 0.0f, 2.0f);
            ///            y += spacing;
            ///        }
            ///
            ///        if (shapeType == ParticleSystemShapeType.Cone || shapeType == ParticleSystemShapeType.ConeVolume)
            ///        {
            ///            GUI.Label(new Rect(25, y, 140, 30), "Angle");
            ///            angle = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), angle, 1.0f, 90.0f);
            ///            y += spacing;
            ///
            ///            GUI.Label(new Rect(25, y, 140, 30), "Radius");
            ///            radius = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), radius, 0.2f, 5.0f);
            ///            y += spacing;
            ///
            ///            GUI.Label(new Rect(25, y, 140, 30), "Radius Thickness");
            ///            radiusThickness = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), radiusThickness, 0.0f, 1.0f);
            ///            y += spacing;
            ///
            ///            GUI.Label(new Rect(25, y, 140, 30), "Arc");
            ///            arc = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), arc, 1.0f, 360.0f);
            ///            y += spacing;
            ///
            ///            GUI.Label(new Rect(25, y, 140, 30), "Arc Mode");
            ///            arcMode = (ParticleSystemShapeMultiModeValue)GUI.SelectionGrid(new Rect(165, 280, 360, 20), (int)arcMode, new GUIContent[] { new GUIContent("Random"), new GUIContent("Loop"), new GUIContent("Ping-Pong"), new GUIContent("Burst Spread") }, 4);
            ///            y += spacing;
            ///
            ///            GUI.Label(new Rect(25, y, 140, 30), "Arc Spread");
            ///            arcSpread = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), arcSpread, 0.0f, 1.0f);
            ///            y += spacing;
            ///
            ///            GUI.Label(new Rect(25, y, 140, 30), "Arc Speed");
            ///            arcSpeed = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), arcSpeed, 0.0f, 2.0f);
            ///            y += spacing;
            ///
            ///            if (shapeType == ParticleSystemShapeType.ConeVolume)
            ///            {
            ///                GUI.Label(new Rect(25, y, 140, 30), "Length");
            ///                length = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), length, 1.0f, 5.0f);
            ///                y += spacing;
            ///            }
            ///        }
            ///
            ///        if (shapeType == ParticleSystemShapeType.Box || shapeType == ParticleSystemShapeType.BoxShell || shapeType == ParticleSystemShapeType.BoxEdge)
            ///        {
            ///            if (shapeType == ParticleSystemShapeType.BoxShell || shapeType == ParticleSystemShapeType.BoxEdge)
            ///            {
            ///                GUI.Label(new Rect(25, y, 140, 30), "Box Thickness");
            ///                boxThickness.x = GUI.HorizontalSlider(new Rect(165, y + 5, 50, 30), boxThickness.x, 0.0f, 1.0f);
            ///                boxThickness.y = GUI.HorizontalSlider(new Rect(220, y + 5, 50, 30), boxThickness.y, 0.0f, 1.0f);
            ///                boxThickness.z = GUI.HorizontalSlider(new Rect(275, y + 5, 50, 30), boxThickness.z, 0.0f, 1.0f);
            ///                y += spacing;
            ///            }
            ///        }
            ///
            ///        if (shapeType == ParticleSystemShapeType.Donut)
            ///        {
            ///            GUI.Label(new Rect(25, y, 140, 30), "Arc");
            ///            arc = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), arc, 1.0f, 360.0f);
            ///            y += spacing;
            ///
            ///            GUI.Label(new Rect(25, y, 140, 30), "Arc Mode");
            ///            arcMode = (ParticleSystemShapeMultiModeValue)GUI.SelectionGrid(new Rect(165, y, 360, 20), (int)arcMode, new GUIContent[] { new GUIContent("Random"), new GUIContent("Loop"), new GUIContent("Ping-Pong"), new GUIContent("Burst Spread") }, 4);
            ///            y += spacing;
            ///
            ///            GUI.Label(new Rect(25, y, 140, 30), "Arc Spread");
            ///            arcSpread = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), arcSpread, 0.0f, 1.0f);
            ///            y += spacing;
            ///
            ///            GUI.Label(new Rect(25, y, 140, 30), "Arc Speed");
            ///            arcSpeed = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), arcSpeed, 0.0f, 2.0f);
            ///            y += spacing;
            ///
            ///            GUI.Label(new Rect(25, y, 140, 30), "Radius");
            ///            radius = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), radius, 0.2f, 5.0f);
            ///            y += spacing;
            ///
            ///            GUI.Label(new Rect(25, y, 140, 30), "Radius Thickness");
            ///            radiusThickness = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), radiusThickness, 0.0f, 1.0f);
            ///            y += spacing;
            ///
            ///            GUI.Label(new Rect(25, y, 140, 30), "Donut Radius");
            ///            donutRadius = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), donutRadius, 0.0f, 5.0f);
            ///            y += spacing;
            ///        }
            ///
            ///        if (shapeType == ParticleSystemShapeType.Circle)
            ///        {
            ///            GUI.Label(new Rect(25, y, 140, 30), "Arc");
            ///            arc = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), arc, 1.0f, 360.0f);
            ///            y += spacing;
            ///
            ///            GUI.Label(new Rect(25, y, 140, 30), "Arc Mode");
            ///            arcMode = (ParticleSystemShapeMultiModeValue)GUI.SelectionGrid(new Rect(165, y, 360, 20), (int)arcMode, new GUIContent[] { new GUIContent("Random"), new GUIContent("Loop"), new GUIContent("Ping-Pong"), new GUIContent("Burst Spread") }, 4);
            ///            y += spacing;
            ///
            ///            GUI.Label(new Rect(25, y, 140, 30), "Arc Spread");
            ///            arcSpread = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), arcSpread, 0.0f, 1.0f);
            ///            y += spacing;
            ///
            ///            GUI.Label(new Rect(25, y, 140, 30), "Arc Speed");
            ///            arcSpeed = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), arcSpeed, 0.0f, 2.0f);
            ///            y += spacing;
            ///
            ///            GUI.Label(new Rect(25, y, 140, 30), "Radius");
            ///            radius = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), radius, 0.2f, 5.0f);
            ///            y += spacing;
            ///
            ///            GUI.Label(new Rect(25, y, 140, 30), "Radius Thickness");
            ///            radiusThickness = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), radiusThickness, 0.0f, 1.0f);
            ///            y += spacing;
            ///        }
            ///
            ///        if (shapeType == ParticleSystemShapeType.SingleSidedEdge)
            ///        {
            ///            GUI.Label(new Rect(25, y, 140, 30), "Radius");
            ///            radius = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), radius, 0.2f, 5.0f);
            ///            y += spacing;
            ///
            ///            GUI.Label(new Rect(25, y, 140, 30), "Radius Mode");
            ///            radiusMode = (ParticleSystemShapeMultiModeValue)GUI.SelectionGrid(new Rect(165, y, 360, 20), (int)radiusMode, new GUIContent[] { new GUIContent("Random"), new GUIContent("Loop"), new GUIContent("Ping-Pong"), new GUIContent("Burst Spread") }, 4);
            ///            y += spacing;
            ///
            ///            GUI.Label(new Rect(25, y, 140, 30), "Radius Spread");
            ///            radiusSpread = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), radiusSpread, 0.0f, 1.0f);
            ///            y += spacing;
            ///
            ///            GUI.Label(new Rect(25, y, 140, 30), "Radius Speed");
            ///            radiusSpeed = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), radiusSpeed, 0.0f, 2.0f);
            ///            y += spacing;
            ///        }
            ///
            ///        if (shapeType == ParticleSystemShapeType.Mesh || shapeType == ParticleSystemShapeType.Sprite)
            ///        {
            ///            meshShapeType = (ParticleSystemMeshShapeType)GUI.SelectionGrid(new Rect(25, y + 5, 300, 20), (int)meshShapeType, new GUIContent[] { new GUIContent("Vertex"), new GUIContent("Edge"), new GUIContent("Polygon") }, 3);
            ///            y += spacing;
            ///
            ///            GUI.Label(new Rect(25, y, 140, 30), "Normal Offset");
            ///            normalOffset = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), normalOffset, -3.0f, 3.0f);
            ///            y += spacing;
            ///        }
            ///
            ///        GUI.Label(new Rect(25, y, 140, 30), "Randomize Direction");
            ///        randomizeDirection = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), randomizeDirection, 0.0f, 1.0f);
            ///        y += spacing;
            ///
            ///        if (shapeType != ParticleSystemShapeType.Sphere)
            ///        {
            ///            GUI.Label(new Rect(25, y, 140, 30), "Spherize Direction");
            ///            spherizeDirection = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), spherizeDirection, 0.0f, 1.0f);
            ///            y += spacing;
            ///        }
            ///
            ///        GUI.Label(new Rect(25, y, 140, 30), "Randomize Position");
            ///        randomizePosition = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), randomizePosition, 0.0f, 1.0f);
            ///        y += spacing;
            ///
            ///        GUI.Label(new Rect(25, y, 140, 30), "Position");
            ///        position.x = GUI.HorizontalSlider(new Rect(165, y + 5, 50, 30), position.x, -2.0f, 2.0f);
            ///        position.y = GUI.HorizontalSlider(new Rect(220, y + 5, 50, 30), position.y, -2.0f, 2.0f);
            ///        position.z = GUI.HorizontalSlider(new Rect(275, y + 5, 50, 30), position.z, -2.0f, 2.0f);
            ///        y += spacing;
            ///
            ///        GUI.Label(new Rect(25, y, 140, 30), "Rotation");
            ///        rotation.x = GUI.HorizontalSlider(new Rect(165, y + 5, 50, 30), rotation.x, 0.0f, 360.0f);
            ///        rotation.y = GUI.HorizontalSlider(new Rect(220, y + 5, 50, 30), rotation.y, 0.0f, 360.0f);
            ///        rotation.z = GUI.HorizontalSlider(new Rect(275, y + 5, 50, 30), rotation.z, 0.0f, 360.0f);
            ///        y += spacing;
            ///
            ///        GUI.Label(new Rect(25, y, 140, 30), "Scale");
            ///        scale.x = GUI.HorizontalSlider(new Rect(165, y + 5, 50, 30), scale.x, 0.0f, 3.0f);
            ///        scale.y = GUI.HorizontalSlider(new Rect(220, y + 5, 50, 30), scale.y, 0.0f, 3.0f);
            ///        scale.z = GUI.HorizontalSlider(new Rect(275, y + 5, 50, 30), scale.z, 0.0f, 3.0f);
            ///        y += spacing;
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public ParticleSystemShapeType shapeType { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Randomizes the starting direction of particles.</summary>
            ///<remarks>This accepts values from 0 to 1, where 0 causes the particles to retain their default direction and 1 causes a completely random direction. Values in between 0 and 1 blend the default shape direction with a random direction. This means that at 0.5, you get 50% of the default shape and 50% of a randomized direction.</remarks>
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            extern public float randomDirectionAmount { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Makes particles move in a spherical direction from their starting point.</summary>
            ///<remarks>This accepts values from 0 to 1, where 0 causes the particles to retain their default direction and 1 causes a completely spherical direction. Values in between 0 and 1 blend the default shape direction with a spherical direction. This means that at 0.5, you get 50% of the default shape and 50% of a spherical direction. At 1, the particle direction points outwards from the center to create a uniform sphere.</remarks>
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            extern public float sphericalDirectionAmount { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Randomizes the starting position of particles.</summary>
            ///<remarks>A higher value applies more randomization to the particle positions.</remarks>
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            extern public float randomPositionAmount { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Align particles based on their initial direction of travel.</summary>
            ///<remarks>The Shape Module supports setting the initial rotation of particles based on their direction of travel. This can be useful to make particles appear to have originated from the surface of a Mesh (for example, paint flaking off a surface). This works with any shape type. Unity applies any <see cref="ParticleSystem.startRotation" /> on top of this setting, so you can use both together.
            ///
            ///You can use this setting in conjunction with the <see cref="ParticleSystem.MainModule.startRotation" /> setting; Unity adds the rotation given by <see cref="ParticleSystem.MainModule.startRotation" /> on top of the value that <see cref="ParticleSystem.ShapeModule.alignToDirection" /> calculates.
            ///
            ///For example: add a <see cref="ParticleSystem.MainModule.startRotation" /> of 90 degrees when using <see cref="ParticleSystem.ShapeModule.alignToDirection" />, and all the particles become perpendicular to the surface, like little spikes sticking out of it.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool toggle = true;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var shape = ps.shape;
            ///        shape.alignToDirection = toggle;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        toggle = GUI.Toggle(new Rect(25, 45, 200, 30), toggle, "Align To Direction");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public bool alignToDirection { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Radius of the shape to emit particles from.</summary>
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            extern public float radius { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>The mode to use to generate particles along the radius.</summary>
            ///<seealso cref="ParticleSystem.ShapeModule.radiusMode" />
            ///<seealso cref="ParticleSystemShapeMultiModeValue" />
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            extern public ParticleSystemShapeMultiModeValue radiusMode { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Control the gap between particle emission points along the radius.</summary>
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            extern public float radiusSpread { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>In animated modes, this determines how quickly the particle emission position moves along the radius.</summary>
            ///<remarks>The value is specified in world units.</remarks>
            ///<seealso cref="ParticleSystem.ShapeModule.radiusMode" />
            ///<seealso cref="ParticleSystemShapeMultiModeValue" />
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            public MinMaxCurve radiusSpeed { get => radiusSpeedBlittable; set => radiusSpeedBlittable = value; }
            [NativeName("RadiusSpeed")] private extern MinMaxCurveBlittable radiusSpeedBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>A multiplier of the radius speed of the particle emission shape.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall speed multiplier.</remarks>
            ///<seealso cref="ParticleSystem.ShapeModule.radiusMode" />
            ///<seealso cref="ParticleSystemShapeMultiModeValue" />
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            extern public float radiusSpeedMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Radius thickness of the shape's edge from which to emit particles.</summary>
            ///<remarks>When emitting from the edge of Circles, Cones, and Spheres, you can use this to add a thicker border to the edge where the system emits particles.</remarks>
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            extern public float radiusThickness { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Angle of the cone to emit particles from.</summary>
            ///<remarks>Note that you should specify the value in degrees.</remarks>
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            extern public float angle { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Length of the cone to emit particles from.</summary>
            ///<remarks>**Note:** <see cref="length" /> is only valid when <see cref="ParticleSystem.ShapeModule.shapeType" /> is set to <c>ConeVolume</c> or <c>ConeVolumeShell</c>.</remarks>
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            extern public float length { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Thickness of the box to emit particles from.</summary>
            ///<remarks>When using edge and shell modes, this controls how far from the perimeter that Unity can generate particles.</remarks>
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            extern public Vector3 boxThickness { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Where on the Mesh to emit particles from.</summary>
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            extern public ParticleSystemMeshShapeType meshShapeType { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Mesh to emit particles from.</summary>
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            extern public Mesh mesh { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>MeshRenderer to emit particles from.</summary>
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            extern public MeshRenderer meshRenderer { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>SkinnedMeshRenderer to emit particles from.</summary>
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            extern public SkinnedMeshRenderer skinnedMeshRenderer { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Sprite to emit particles from.</summary>
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            extern public Sprite sprite { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>SpriteRenderer to emit particles from.</summary>
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            extern public SpriteRenderer spriteRenderer { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Emit particles from a single Material, or the whole Mesh.</summary>
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            extern public bool useMeshMaterialIndex { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Emit particles from a single Material of a Mesh.</summary>
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            extern public int meshMaterialIndex { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Modulate the particle colors with the vertex colors, or the Material color if no vertex colors exist.</summary>
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            extern public bool useMeshColors { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Move particles away from the surface of the source Mesh.</summary>
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            extern public float normalOffset { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>The mode to use to generate particles on a Mesh.</summary>
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            extern public ParticleSystemShapeMultiModeValue meshSpawnMode { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Control the gap between particle emission points across the Mesh.</summary>
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            extern public float meshSpawnSpread { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>In animated modes, this determines how quickly the particle emission position moves across the Mesh.</summary>
            ///<remarks>The value is specified in terms of the number of complete passes over the Mesh. A value of 2 would mean that the particle emission position traversed the Mesh twice per second.</remarks>
            ///<seealso cref="ParticleSystem.ShapeModule.meshSpawnMode" />
            ///<seealso cref="ParticleSystemShapeMultiModeValue" />
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            public MinMaxCurve meshSpawnSpeed { get => meshSpawnSpeedBlittable; set => meshSpawnSpeedBlittable = value; }
            [NativeName("MeshSpawnSpeed")] private extern MinMaxCurveBlittable meshSpawnSpeedBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>A multiplier of the Mesh spawn speed.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall speed multiplier.</remarks>
            ///<seealso cref="ParticleSystem.ShapeModule.meshSpawnMode" />
            ///<seealso cref="ParticleSystemShapeMultiModeValue" />
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            extern public float meshSpawnSpeedMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Angle of the circle arc to emit particles from.</summary>
            ///<remarks>Note that you should specify the value in degrees.</remarks>
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            extern public float arc { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>The mode that Unity uses to generate particles around the arc.</summary>
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            extern public ParticleSystemShapeMultiModeValue arcMode { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Control the gap between particle emission points around the arc.</summary>
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            extern public float arcSpread { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>In animated modes, this determines how quickly the particle emission position moves around the arc.</summary>
            ///<remarks>The value is specified in terms of the number of complete 360 degree rotations. A value of 2 would mean that the particle emission position rotated 2 full times per second.</remarks>
            ///<seealso cref="ParticleSystem.ShapeModule.arcMode" />
            ///<seealso cref="ParticleSystemShapeMultiModeValue" />
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            public MinMaxCurve arcSpeed { get => arcSpeedBlittable; set => arcSpeedBlittable = value; }
            [NativeName("ArcSpeed")] private extern MinMaxCurveBlittable arcSpeedBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>A multiplier of the arc speed of the particle emission shape.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall speed multiplier.</remarks>
            ///<seealso cref="ParticleSystem.ShapeModule.arcMode" />
            ///<seealso cref="ParticleSystemShapeMultiModeValue" />
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            extern public float arcSpeedMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>The thickness of the Donut shape to emit particles from.</summary>
            ///<remarks>Controls the secondary radius of the Donut shape.</remarks>
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            extern public float donutRadius { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Apply an offset to the position from which the system emits particles.</summary>
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            extern public Vector3 position { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Apply a rotation to the shape from which the system emits particles.</summary>
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            extern public Vector3 rotation { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Apply scale to the shape from which the system emits particles.</summary>
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            extern public Vector3 scale { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Specifies a Texture to tint the particle's start colors.</summary>
            ///<remarks>To tint the particles' start color, the Shape module reads pixels from this Texture on the CPU. This means you must enable the read/write option in the assigned Texture's Import Settings.
            ///
            ///To tint particles, the Shape module first stretches the Texture over the shape you specify. Then, when the system emits a particle from a point on the shape, the Shape module uses the color of the Texture at that location as the particle color.
            ///
            ///To see how the Texture stretches over the shape, select the Particle System in the Hierarchy view and expand the Shape module. The Scene View visualization of the shape includes the Texture preview.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEditor;
            ///using UnityEngine;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float alphaThreshold = 0.0f;
            ///    public bool colorAffectsParticles = true;
            ///    public bool alphaAffectsParticles = true;
            ///    public bool bilinearFiltering = false;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startSpeed = 0.0f;
            ///        main.startSize = 0.5f;
            ///        main.startLifetime = 1.0f;
            ///
            ///        var emission = ps.emission;
            ///        emission.rateOverTime = 500.0f;
            ///
            ///        var shape = ps.shape;
            ///        shape.shapeType = ParticleSystemShapeType.Circle;
            ///        shape.radius = 6.0f;
            ///        shape.texture = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var shape = ps.shape;
            ///        shape.textureClipThreshold = alphaThreshold;
            ///        shape.textureColorAffectsParticles = colorAffectsParticles;
            ///        shape.textureAlphaAffectsParticles = alphaAffectsParticles;
            ///        shape.textureBilinearFiltering = bilinearFiltering;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        float y = 120.0f;
            ///        float spacing = 40.0f;
            ///
            ///        GUI.Label(new Rect(25, y, 140, 30), "Alpha Threshold");
            ///        alphaThreshold = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), alphaThreshold, 0.0f, 1.0f);
            ///        y += spacing;
            ///
            ///        colorAffectsParticles = GUI.Toggle(new Rect(25, y + 5, 200, 30), colorAffectsParticles, "Color Affects Particles");
            ///        y += spacing;
            ///
            ///        alphaAffectsParticles = GUI.Toggle(new Rect(25, y + 5, 200, 30), alphaAffectsParticles, "Alpha Affects Particles");
            ///        y += spacing;
            ///
            ///        bilinearFiltering = GUI.Toggle(new Rect(25, y + 5, 200, 30), bilinearFiltering, "Bilinear Filtering");
            ///        y += spacing;
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public Texture2D texture { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Selects which channel of the Texture to use for discarding particles.</summary>
            ///<seealso cref="ParticleSystem.ShapeModule.textureClipThreshold" />
            ///<seealso cref="ParticleSystem.ShapeModule.texture" />
            extern public ParticleSystemShapeTextureChannel textureClipChannel { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Discards particles when they spawn on an area of the Texture with a value lower than this threshold.</summary>
            ///<seealso cref="ParticleSystem.ShapeModule.textureClipChannel" />
            ///<seealso cref="ParticleSystem.ShapeModule.texture" />
            extern public float textureClipThreshold { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>When enabled, the system applies the RGB channels of the Texture to the particle color when the particle spawns.</summary>
            ///<seealso cref="ParticleSystem.ShapeModule.texture" />
            extern public bool textureColorAffectsParticles { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>When enabled, the system applies the alpha channel of the Texture to the particle alpha when the particle spawns.</summary>
            ///<seealso cref="ParticleSystem.ShapeModule.texture" />
            extern public bool textureAlphaAffectsParticles { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>When enabled, the system takes four neighboring samples from the Texture then combines them to give the final particle value.</summary>
            ///<remarks>Enabling this option increases the performance cost, but reduces abrupt color changes when particles spawn between neighboring pixels on the source Texture.</remarks>
            ///<seealso cref="ParticleSystem.ShapeModule.texture" />
            extern public bool textureBilinearFiltering { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>When using a Mesh as a source shape type, this option controls which UV channel on the Mesh to use for reading the source Texture.</summary>
            ///<seealso cref="ParticleSystem.ShapeModule.texture" />
            ///<seealso cref="ParticleSystem.ShapeModule.shapeType" />
            extern public int textureUVChannel { get; [NativeMethod(ThrowsException = true)] set; }
        }

        ///<summary>Script interface for the VelocityOverLifetimeModule.</summary>
        ///<remarks>This module sets the velocity of particles during their lifetime.</remarks>
        ///<seealso cref="ParticleSystem" />
        ///<seealso cref="ParticleSystem.velocityOverLifetime" />
        public partial struct VelocityOverLifetimeModule
        {
            internal VelocityOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            ///<summary>Specifies whether the VelocityOverLifetimeModule is enabled or disabled.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueX = 1.0f;
            ///    public float hSliderValueY = 1.0f;
            ///    public float hSliderValueZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var velocityOverLifetime = ps.velocityOverLifetime;
            ///        velocityOverLifetime.enabled = true;
            ///        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        ParticleSystem.MinMaxCurve minMaxCurve = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///
            ///        velocityOverLifetime.x = minMaxCurve;
            ///        velocityOverLifetime.y = minMaxCurve;
            ///        velocityOverLifetime.z = minMaxCurve;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, making it easier to see the size
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var velocityOverLifetime = ps.velocityOverLifetime;
            ///        velocityOverLifetime.xMultiplier = hSliderValueX;
            ///        velocityOverLifetime.yMultiplier = hSliderValueY;
            ///        velocityOverLifetime.zMultiplier = hSliderValueZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Z");
            ///
            ///        hSliderValueX = GUI.HorizontalSlider(new Rect(55, 45, 100, 30), hSliderValueX, -50.0f, 50.0f);
            ///        hSliderValueY = GUI.HorizontalSlider(new Rect(55, 85, 100, 30), hSliderValueY, -50.0f, 50.0f);
            ///        hSliderValueZ = GUI.HorizontalSlider(new Rect(55, 125, 100, 30), hSliderValueZ, -50.0f, 50.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.velocityOverLifetime" />
            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Curve to control particle speed based on lifetime, on the x-axis.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueX = 1.0f;
            ///    public float hSliderValueY = 1.0f;
            ///    public float hSliderValueZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var velocityOverLifetime = ps.velocityOverLifetime;
            ///        velocityOverLifetime.enabled = true;
            ///        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        ParticleSystem.MinMaxCurve minMaxCurve = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///
            ///        velocityOverLifetime.x = minMaxCurve;
            ///        velocityOverLifetime.y = minMaxCurve;
            ///        velocityOverLifetime.z = minMaxCurve;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var velocityOverLifetime = ps.velocityOverLifetime;
            ///        velocityOverLifetime.xMultiplier = hSliderValueX;
            ///        velocityOverLifetime.yMultiplier = hSliderValueY;
            ///        velocityOverLifetime.zMultiplier = hSliderValueZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Z");
            ///
            ///        hSliderValueX = GUI.HorizontalSlider(new Rect(55, 45, 100, 30), hSliderValueX, -50.0f, 50.0f);
            ///        hSliderValueY = GUI.HorizontalSlider(new Rect(55, 85, 100, 30), hSliderValueY, -50.0f, 50.0f);
            ///        hSliderValueZ = GUI.HorizontalSlider(new Rect(55, 125, 100, 30), hSliderValueZ, -50.0f, 50.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve x { get => xBlittable; set => xBlittable = value; }
            [NativeName("X")] private extern MinMaxCurveBlittable xBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Curve to control particle speed based on lifetime, on the y-axis.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueX = 1.0f;
            ///    public float hSliderValueY = 1.0f;
            ///    public float hSliderValueZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var velocityOverLifetime = ps.velocityOverLifetime;
            ///        velocityOverLifetime.enabled = true;
            ///        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        ParticleSystem.MinMaxCurve minMaxCurve = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///
            ///        velocityOverLifetime.x = minMaxCurve;
            ///        velocityOverLifetime.y = minMaxCurve;
            ///        velocityOverLifetime.z = minMaxCurve;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var velocityOverLifetime = ps.velocityOverLifetime;
            ///        velocityOverLifetime.xMultiplier = hSliderValueX;
            ///        velocityOverLifetime.yMultiplier = hSliderValueY;
            ///        velocityOverLifetime.zMultiplier = hSliderValueZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Z");
            ///
            ///        hSliderValueX = GUI.HorizontalSlider(new Rect(55, 45, 100, 30), hSliderValueX, -50.0f, 50.0f);
            ///        hSliderValueY = GUI.HorizontalSlider(new Rect(55, 85, 100, 30), hSliderValueY, -50.0f, 50.0f);
            ///        hSliderValueZ = GUI.HorizontalSlider(new Rect(55, 125, 100, 30), hSliderValueZ, -50.0f, 50.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve y { get => yBlittable; set => yBlittable = value; }
            [NativeName("Y")] private extern MinMaxCurveBlittable yBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Curve to control particle speed based on lifetime, on the z-axis.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueX = 1.0f;
            ///    public float hSliderValueY = 1.0f;
            ///    public float hSliderValueZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var velocityOverLifetime = ps.velocityOverLifetime;
            ///        velocityOverLifetime.enabled = true;
            ///        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        ParticleSystem.MinMaxCurve minMaxCurve = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///
            ///        velocityOverLifetime.x = minMaxCurve;
            ///        velocityOverLifetime.y = minMaxCurve;
            ///        velocityOverLifetime.z = minMaxCurve;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var velocityOverLifetime = ps.velocityOverLifetime;
            ///        velocityOverLifetime.xMultiplier = hSliderValueX;
            ///        velocityOverLifetime.yMultiplier = hSliderValueY;
            ///        velocityOverLifetime.zMultiplier = hSliderValueZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Z");
            ///
            ///        hSliderValueX = GUI.HorizontalSlider(new Rect(55, 45, 100, 30), hSliderValueX, -50.0f, 50.0f);
            ///        hSliderValueY = GUI.HorizontalSlider(new Rect(55, 85, 100, 30), hSliderValueY, -50.0f, 50.0f);
            ///        hSliderValueZ = GUI.HorizontalSlider(new Rect(55, 125, 100, 30), hSliderValueZ, -50.0f, 50.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve z { get => zBlittable; set => zBlittable = value; }
            [NativeName("Z")] private extern MinMaxCurveBlittable zBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>A multiplier for <see cref="ParticleSystem.VelocityOverLifetimeModule.x" /></summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall speed multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueX = 1.0f;
            ///    public float hSliderValueY = 1.0f;
            ///    public float hSliderValueZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var velocityOverLifetime = ps.velocityOverLifetime;
            ///        velocityOverLifetime.enabled = true;
            ///        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        ParticleSystem.MinMaxCurve minMaxCurve = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///
            ///        velocityOverLifetime.x = minMaxCurve;
            ///        velocityOverLifetime.y = minMaxCurve;
            ///        velocityOverLifetime.z = minMaxCurve;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var velocityOverLifetime = ps.velocityOverLifetime;
            ///        velocityOverLifetime.xMultiplier = hSliderValueX;
            ///        velocityOverLifetime.yMultiplier = hSliderValueY;
            ///        velocityOverLifetime.zMultiplier = hSliderValueZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Z");
            ///
            ///        hSliderValueX = GUI.HorizontalSlider(new Rect(55, 45, 100, 30), hSliderValueX, -50.0f, 50.0f);
            ///        hSliderValueY = GUI.HorizontalSlider(new Rect(55, 85, 100, 30), hSliderValueY, -50.0f, 50.0f);
            ///        hSliderValueZ = GUI.HorizontalSlider(new Rect(55, 125, 100, 30), hSliderValueZ, -50.0f, 50.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float xMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>A multiplier for <see cref="ParticleSystem.VelocityOverLifetimeModule.y" />.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall speed multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueX = 1.0f;
            ///    public float hSliderValueY = 1.0f;
            ///    public float hSliderValueZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var velocityOverLifetime = ps.velocityOverLifetime;
            ///        velocityOverLifetime.enabled = true;
            ///        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        ParticleSystem.MinMaxCurve minMaxCurve = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///
            ///        velocityOverLifetime.x = minMaxCurve;
            ///        velocityOverLifetime.y = minMaxCurve;
            ///        velocityOverLifetime.z = minMaxCurve;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var velocityOverLifetime = ps.velocityOverLifetime;
            ///        velocityOverLifetime.xMultiplier = hSliderValueX;
            ///        velocityOverLifetime.yMultiplier = hSliderValueY;
            ///        velocityOverLifetime.zMultiplier = hSliderValueZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Z");
            ///
            ///        hSliderValueX = GUI.HorizontalSlider(new Rect(55, 45, 100, 30), hSliderValueX, -50.0f, 50.0f);
            ///        hSliderValueY = GUI.HorizontalSlider(new Rect(55, 85, 100, 30), hSliderValueY, -50.0f, 50.0f);
            ///        hSliderValueZ = GUI.HorizontalSlider(new Rect(55, 125, 100, 30), hSliderValueZ, -50.0f, 50.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float yMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>A multiplier for <see cref="ParticleSystem.VelocityOverLifetimeModule.z" />.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall speed multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueX = 1.0f;
            ///    public float hSliderValueY = 1.0f;
            ///    public float hSliderValueZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var velocityOverLifetime = ps.velocityOverLifetime;
            ///        velocityOverLifetime.enabled = true;
            ///        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        ParticleSystem.MinMaxCurve minMaxCurve = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///
            ///        velocityOverLifetime.x = minMaxCurve;
            ///        velocityOverLifetime.y = minMaxCurve;
            ///        velocityOverLifetime.z = minMaxCurve;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var velocityOverLifetime = ps.velocityOverLifetime;
            ///        velocityOverLifetime.xMultiplier = hSliderValueX;
            ///        velocityOverLifetime.yMultiplier = hSliderValueY;
            ///        velocityOverLifetime.zMultiplier = hSliderValueZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Z");
            ///
            ///        hSliderValueX = GUI.HorizontalSlider(new Rect(55, 45, 100, 30), hSliderValueX, -50.0f, 50.0f);
            ///        hSliderValueY = GUI.HorizontalSlider(new Rect(55, 85, 100, 30), hSliderValueY, -50.0f, 50.0f);
            ///        hSliderValueZ = GUI.HorizontalSlider(new Rect(55, 125, 100, 30), hSliderValueZ, -50.0f, 50.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float zMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Curve to control particle speed based on lifetime, around the x-axis.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueX = 0.0f;
            ///    public float hSliderValueY = 0.0f;
            ///    public float hSliderValueZ = 0.0f;
            ///    public float hSliderValueRadial = 0.0f;
            ///    public float hSliderValueOffset = 0.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///        ps.transform.rotation = Quaternion.identity;
            ///
            ///        var main = ps.main;
            ///        main.startSpeedMultiplier = 0.0f;
            ///
            ///        var shape = ps.shape;
            ///        shape.shapeType = ParticleSystemShapeType.Circle;
            ///        shape.radius = 5.0f;
            ///
            ///        var velocityOverLifetime = ps.velocityOverLifetime;
            ///        velocityOverLifetime.enabled = true;
            ///
            ///        var trails = ps.trails;
            ///        trails.enabled = true;
            ///        trails.widthOverTrailMultiplier = 0.1f;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.trailMaterial = psr.material;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var velocityOverLifetime = ps.velocityOverLifetime;
            ///        velocityOverLifetime.orbitalXMultiplier = hSliderValueX;
            ///        velocityOverLifetime.orbitalYMultiplier = hSliderValueY;
            ///        velocityOverLifetime.orbitalZMultiplier = hSliderValueZ;
            ///        velocityOverLifetime.radialMultiplier = hSliderValueRadial;
            ///        velocityOverLifetime.orbitalOffsetX = hSliderValueOffset;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Z");
            ///        GUI.Label(new Rect(25, 160, 100, 30), "Radial");
            ///        GUI.Label(new Rect(25, 200, 100, 30), "Offset");
            ///
            ///        hSliderValueX = GUI.HorizontalSlider(new Rect(85, 45, 100, 30), hSliderValueX, -5.0f, 5.0f);
            ///        hSliderValueY = GUI.HorizontalSlider(new Rect(85, 85, 100, 30), hSliderValueY, -5.0f, 5.0f);
            ///        hSliderValueZ = GUI.HorizontalSlider(new Rect(85, 125, 100, 30), hSliderValueZ, -5.0f, 5.0f);
            ///        hSliderValueRadial = GUI.HorizontalSlider(new Rect(85, 165, 100, 30), hSliderValueRadial, -2.0f, 2.0f);
            ///        hSliderValueOffset = GUI.HorizontalSlider(new Rect(85, 205, 100, 30), hSliderValueOffset, -5.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            ///<seealso cref="ParticleSystem.VelocityOverLifetimeModule.orbitalOffsetX" />
            ///<seealso cref="ParticleSystem.VelocityOverLifetimeModule.orbitalOffsetY" />
            ///<seealso cref="ParticleSystem.VelocityOverLifetimeModule.orbitalOffsetZ" />
            public MinMaxCurve orbitalX { get => orbitalXBlittable; set => orbitalXBlittable = value; }
            [NativeName("OrbitalX")] private extern MinMaxCurveBlittable orbitalXBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Curve to control particle speed based on lifetime, around the y-axis.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueX = 0.0f;
            ///    public float hSliderValueY = 0.0f;
            ///    public float hSliderValueZ = 0.0f;
            ///    public float hSliderValueRadial = 0.0f;
            ///    public float hSliderValueOffset = 0.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///        ps.transform.rotation = Quaternion.identity;
            ///
            ///        var main = ps.main;
            ///        main.startSpeedMultiplier = 0.0f;
            ///
            ///        var shape = ps.shape;
            ///        shape.shapeType = ParticleSystemShapeType.Circle;
            ///        shape.radius = 5.0f;
            ///
            ///        var velocityOverLifetime = ps.velocityOverLifetime;
            ///        velocityOverLifetime.enabled = true;
            ///
            ///        var trails = ps.trails;
            ///        trails.enabled = true;
            ///        trails.widthOverTrailMultiplier = 0.1f;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.trailMaterial = psr.material;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var velocityOverLifetime = ps.velocityOverLifetime;
            ///        velocityOverLifetime.orbitalXMultiplier = hSliderValueX;
            ///        velocityOverLifetime.orbitalYMultiplier = hSliderValueY;
            ///        velocityOverLifetime.orbitalZMultiplier = hSliderValueZ;
            ///        velocityOverLifetime.radialMultiplier = hSliderValueRadial;
            ///        velocityOverLifetime.orbitalOffsetX = hSliderValueOffset;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Z");
            ///        GUI.Label(new Rect(25, 160, 100, 30), "Radial");
            ///        GUI.Label(new Rect(25, 200, 100, 30), "Offset");
            ///
            ///        hSliderValueX = GUI.HorizontalSlider(new Rect(85, 45, 100, 30), hSliderValueX, -5.0f, 5.0f);
            ///        hSliderValueY = GUI.HorizontalSlider(new Rect(85, 85, 100, 30), hSliderValueY, -5.0f, 5.0f);
            ///        hSliderValueZ = GUI.HorizontalSlider(new Rect(85, 125, 100, 30), hSliderValueZ, -5.0f, 5.0f);
            ///        hSliderValueRadial = GUI.HorizontalSlider(new Rect(85, 165, 100, 30), hSliderValueRadial, -2.0f, 2.0f);
            ///        hSliderValueOffset = GUI.HorizontalSlider(new Rect(85, 205, 100, 30), hSliderValueOffset, -5.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            ///<seealso cref="ParticleSystem.VelocityOverLifetimeModule.orbitalOffsetX" />
            ///<seealso cref="ParticleSystem.VelocityOverLifetimeModule.orbitalOffsetY" />
            ///<seealso cref="ParticleSystem.VelocityOverLifetimeModule.orbitalOffsetZ" />
            public MinMaxCurve orbitalY { get => orbitalYBlittable; set => orbitalYBlittable = value; }
            [NativeName("OrbitalY")] private extern MinMaxCurveBlittable orbitalYBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Curve to control particle speed based on lifetime, around the z-axis.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueX = 0.0f;
            ///    public float hSliderValueY = 0.0f;
            ///    public float hSliderValueZ = 0.0f;
            ///    public float hSliderValueRadial = 0.0f;
            ///    public float hSliderValueOffset = 0.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///        ps.transform.rotation = Quaternion.identity;
            ///
            ///        var main = ps.main;
            ///        main.startSpeedMultiplier = 0.0f;
            ///
            ///        var shape = ps.shape;
            ///        shape.shapeType = ParticleSystemShapeType.Circle;
            ///        shape.radius = 5.0f;
            ///
            ///        var velocityOverLifetime = ps.velocityOverLifetime;
            ///        velocityOverLifetime.enabled = true;
            ///
            ///        var trails = ps.trails;
            ///        trails.enabled = true;
            ///        trails.widthOverTrailMultiplier = 0.1f;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.trailMaterial = psr.material;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var velocityOverLifetime = ps.velocityOverLifetime;
            ///        velocityOverLifetime.orbitalXMultiplier = hSliderValueX;
            ///        velocityOverLifetime.orbitalYMultiplier = hSliderValueY;
            ///        velocityOverLifetime.orbitalZMultiplier = hSliderValueZ;
            ///        velocityOverLifetime.radialMultiplier = hSliderValueRadial;
            ///        velocityOverLifetime.orbitalOffsetX = hSliderValueOffset;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Z");
            ///        GUI.Label(new Rect(25, 160, 100, 30), "Radial");
            ///        GUI.Label(new Rect(25, 200, 100, 30), "Offset");
            ///
            ///        hSliderValueX = GUI.HorizontalSlider(new Rect(85, 45, 100, 30), hSliderValueX, -5.0f, 5.0f);
            ///        hSliderValueY = GUI.HorizontalSlider(new Rect(85, 85, 100, 30), hSliderValueY, -5.0f, 5.0f);
            ///        hSliderValueZ = GUI.HorizontalSlider(new Rect(85, 125, 100, 30), hSliderValueZ, -5.0f, 5.0f);
            ///        hSliderValueRadial = GUI.HorizontalSlider(new Rect(85, 165, 100, 30), hSliderValueRadial, -2.0f, 2.0f);
            ///        hSliderValueOffset = GUI.HorizontalSlider(new Rect(85, 205, 100, 30), hSliderValueOffset, -5.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            ///<seealso cref="ParticleSystem.VelocityOverLifetimeModule.orbitalOffsetX" />
            ///<seealso cref="ParticleSystem.VelocityOverLifetimeModule.orbitalOffsetY" />
            ///<seealso cref="ParticleSystem.VelocityOverLifetimeModule.orbitalOffsetZ" />
            public MinMaxCurve orbitalZ { get => orbitalZBlittable; set => orbitalZBlittable = value; }
            [NativeName("OrbitalZ")] private extern MinMaxCurveBlittable orbitalZBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Speed multiplier along the x-axis.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall speed multiplier.</remarks>
            extern public float orbitalXMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Speed multiplier along the y-axis.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall speed multiplier.</remarks>
            extern public float orbitalYMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Speed multiplier along the z-axis.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall speed multiplier.</remarks>
            extern public float orbitalZMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Specify a custom center of rotation for the orbital and radial velocities.</summary>
            ///<seealso cref="ParticleSystem.VelocityOverLifetimeModule.orbitalX" />
            ///<seealso cref="ParticleSystem.VelocityOverLifetimeModule.orbitalY" />
            ///<seealso cref="ParticleSystem.VelocityOverLifetimeModule.orbitalZ" />
            ///<seealso cref="ParticleSystem.VelocityOverLifetimeModule.radial" />
            public MinMaxCurve orbitalOffsetX { get => orbitalOffsetXBlittable; set => orbitalOffsetXBlittable = value; }
            [NativeName("OrbitalOffsetX")] private extern MinMaxCurveBlittable orbitalOffsetXBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Specify a custom center of rotation for the orbital and radial velocities.</summary>
            ///<seealso cref="ParticleSystem.VelocityOverLifetimeModule.orbitalX" />
            ///<seealso cref="ParticleSystem.VelocityOverLifetimeModule.orbitalY" />
            ///<seealso cref="ParticleSystem.VelocityOverLifetimeModule.orbitalZ" />
            ///<seealso cref="ParticleSystem.VelocityOverLifetimeModule.radial" />
            public MinMaxCurve orbitalOffsetY { get => orbitalOffsetYBlittable; set => orbitalOffsetYBlittable = value; }
            [NativeName("OrbitalOffsetY")] private extern MinMaxCurveBlittable orbitalOffsetYBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Specify a custom center of rotation for the orbital and radial velocities.</summary>
            ///<seealso cref="ParticleSystem.VelocityOverLifetimeModule.orbitalX" />
            ///<seealso cref="ParticleSystem.VelocityOverLifetimeModule.orbitalY" />
            ///<seealso cref="ParticleSystem.VelocityOverLifetimeModule.orbitalZ" />
            ///<seealso cref="ParticleSystem.VelocityOverLifetimeModule.radial" />
            public MinMaxCurve orbitalOffsetZ { get => orbitalOffsetZBlittable; set => orbitalOffsetZBlittable = value; }
            [NativeName("OrbitalOffsetZ")] private extern MinMaxCurveBlittable orbitalOffsetZBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>A multiplier for <see cref="orbitalOffsetX" />.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall offset multiplier.</remarks>
            extern public float orbitalOffsetXMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>A multiplier for <see cref="orbitalOffsetY" />.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall offset multiplier.</remarks>
            extern public float orbitalOffsetYMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>A multiplier for <see cref="orbitalOffsetY" />.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall offset multiplier.</remarks>
            extern public float orbitalOffsetZMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Curve to control particle speed based on lifetime, away from a center position.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueX = 0.0f;
            ///    public float hSliderValueY = 0.0f;
            ///    public float hSliderValueZ = 0.0f;
            ///    public float hSliderValueRadial = 0.0f;
            ///    public float hSliderValueOffset = 0.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///        ps.transform.rotation = Quaternion.identity;
            ///
            ///        var main = ps.main;
            ///        main.startSpeedMultiplier = 0.0f;
            ///
            ///        var shape = ps.shape;
            ///        shape.shapeType = ParticleSystemShapeType.Circle;
            ///        shape.radius = 5.0f;
            ///
            ///        var velocityOverLifetime = ps.velocityOverLifetime;
            ///        velocityOverLifetime.enabled = true;
            ///
            ///        var trails = ps.trails;
            ///        trails.enabled = true;
            ///        trails.widthOverTrailMultiplier = 0.1f;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.trailMaterial = psr.material;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var velocityOverLifetime = ps.velocityOverLifetime;
            ///        velocityOverLifetime.orbitalXMultiplier = hSliderValueX;
            ///        velocityOverLifetime.orbitalYMultiplier = hSliderValueY;
            ///        velocityOverLifetime.orbitalZMultiplier = hSliderValueZ;
            ///        velocityOverLifetime.radialMultiplier = hSliderValueRadial;
            ///        velocityOverLifetime.orbitalOffsetX = hSliderValueOffset;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Z");
            ///        GUI.Label(new Rect(25, 160, 100, 30), "Radial");
            ///        GUI.Label(new Rect(25, 200, 100, 30), "Offset");
            ///
            ///        hSliderValueX = GUI.HorizontalSlider(new Rect(85, 45, 100, 30), hSliderValueX, -5.0f, 5.0f);
            ///        hSliderValueY = GUI.HorizontalSlider(new Rect(85, 85, 100, 30), hSliderValueY, -5.0f, 5.0f);
            ///        hSliderValueZ = GUI.HorizontalSlider(new Rect(85, 125, 100, 30), hSliderValueZ, -5.0f, 5.0f);
            ///        hSliderValueRadial = GUI.HorizontalSlider(new Rect(85, 165, 100, 30), hSliderValueRadial, -2.0f, 2.0f);
            ///        hSliderValueOffset = GUI.HorizontalSlider(new Rect(85, 205, 100, 30), hSliderValueOffset, -5.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            ///<seealso cref="ParticleSystem.VelocityOverLifetimeModule.orbitalOffsetX" />
            ///<seealso cref="ParticleSystem.VelocityOverLifetimeModule.orbitalOffsetY" />
            ///<seealso cref="ParticleSystem.VelocityOverLifetimeModule.orbitalOffsetZ" />
            public MinMaxCurve radial { get => radialBlittable; set => radialBlittable = value; }
            [NativeName("Radial")] private extern MinMaxCurveBlittable radialBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>A multiplier for <see cref="ParticleSystem.VelocityOverLifetimeModule.radial" />.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall speed multiplier.</remarks>
            extern public float radialMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Curve to control particle speed based on lifetime, without affecting the direction of the particles.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueSpeed = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var velocityOverLifetime = ps.velocityOverLifetime;
            ///        velocityOverLifetime.enabled = true;
            ///        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        ParticleSystem.MinMaxCurve minMaxCurve = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///
            ///        velocityOverLifetime.speedModifier = minMaxCurve;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var velocityOverLifetime = ps.velocityOverLifetime;
            ///        velocityOverLifetime.speedModifierMultiplier = hSliderValueSpeed;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Speed");
            ///
            ///        hSliderValueSpeed = GUI.HorizontalSlider(new Rect(55, 45, 100, 30), hSliderValueSpeed, -50.0f, 50.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve speedModifier { get => speedModifierBlittable; set => speedModifierBlittable = value; }
            [NativeName("SpeedModifier")] private extern MinMaxCurveBlittable speedModifierBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>A multiplier for <see cref="ParticleSystem.VelocityOverLifetimeModule.speedModifier" />.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall speed multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueSpeed = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var velocityOverLifetime = ps.velocityOverLifetime;
            ///        velocityOverLifetime.enabled = true;
            ///        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        ParticleSystem.MinMaxCurve minMaxCurve = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///
            ///        velocityOverLifetime.speedModifier = minMaxCurve;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var velocityOverLifetime = ps.velocityOverLifetime;
            ///        velocityOverLifetime.speedModifierMultiplier = hSliderValueSpeed;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Speed");
            ///
            ///        hSliderValueSpeed = GUI.HorizontalSlider(new Rect(55, 45, 100, 30), hSliderValueSpeed, -50.0f, 50.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float speedModifierMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Specifies if the velocities are in local space (rotated with the transform) or world space.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueX = 1.0f;
            ///    public float hSliderValueY = 1.0f;
            ///    public float hSliderValueZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var velocityOverLifetime = ps.velocityOverLifetime;
            ///        velocityOverLifetime.enabled = true;
            ///        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        ParticleSystem.MinMaxCurve minMaxCurve = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///
            ///        velocityOverLifetime.x = minMaxCurve;
            ///        velocityOverLifetime.y = minMaxCurve;
            ///        velocityOverLifetime.z = minMaxCurve;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, making it easier to see the size
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var velocityOverLifetime = ps.velocityOverLifetime;
            ///        velocityOverLifetime.xMultiplier = hSliderValueX;
            ///        velocityOverLifetime.yMultiplier = hSliderValueY;
            ///        velocityOverLifetime.zMultiplier = hSliderValueZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Z");
            ///
            ///        hSliderValueX = GUI.HorizontalSlider(new Rect(55, 45, 100, 30), hSliderValueX, -50.0f, 50.0f);
            ///        hSliderValueY = GUI.HorizontalSlider(new Rect(55, 85, 100, 30), hSliderValueY, -50.0f, 50.0f);
            ///        hSliderValueZ = GUI.HorizontalSlider(new Rect(55, 125, 100, 30), hSliderValueZ, -50.0f, 50.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public ParticleSystemSimulationSpace space { get; [NativeMethod(ThrowsException = true)] set; }
        }


        ///<summary>Script interface for LimitVelocityOverLifetimemeModule.</summary>
        ///<remarks>This module reduces particle velocities. To do this, it either applies drag or just reduces velocity over time.</remarks>
        ///<seealso cref="ParticleSystem" />
        ///<seealso cref="ParticleSystem.limitVelocityOverLifetime" />
        public partial struct LimitVelocityOverLifetimeModule
        {
            internal LimitVelocityOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            ///<summary>Specifies whether the LimitForceOverLifetimeModule is enabled or disabled.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool moduleEnabled = true;
            ///    public float hSliderValueSpeed = 0.0f;
            ///    public float hSliderValueDampen = 0.1f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var limitVelocityOverLifetime = ps.limitVelocityOverLifetime;
            ///        limitVelocityOverLifetime.enabled = moduleEnabled;
            ///        limitVelocityOverLifetime.limitMultiplier = hSliderValueSpeed;
            ///        limitVelocityOverLifetime.dampen = hSliderValueDampen;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        moduleEnabled = GUI.Toggle(new Rect(25, 45, 200, 30), moduleEnabled, "Enabled");
            ///
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Speed Limit");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Dampen");
            ///
            ///        hSliderValueSpeed = GUI.HorizontalSlider(new Rect(95, 85, 100, 30), hSliderValueSpeed, 0.0f, 2.0f);
            ///        hSliderValueDampen = GUI.HorizontalSlider(new Rect(95, 125, 100, 30), hSliderValueDampen, 0.0f, 1.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.limitVelocityOverLifetime" />
            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Maximum velocity curve for the x-axis.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueSpeedX = 0.0f;
            ///    public float hSliderValueSpeedY = 0.0f;
            ///    public float hSliderValueSpeedZ = 0.0f;
            ///    public float hSliderValueDampen = 0.1f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var limitVelocityOverLifetime = ps.limitVelocityOverLifetime;
            ///        limitVelocityOverLifetime.enabled = true;
            ///        limitVelocityOverLifetime.separateAxes = true;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var limitVelocityOverLifetime = ps.limitVelocityOverLifetime;
            ///        limitVelocityOverLifetime.limitX = hSliderValueSpeedX;
            ///        limitVelocityOverLifetime.limitY = hSliderValueSpeedY;
            ///        limitVelocityOverLifetime.limitZ = hSliderValueSpeedZ;
            ///        limitVelocityOverLifetime.dampen = hSliderValueDampen;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Speed Limit X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Speed Limit Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Speed Limit Z");
            ///        GUI.Label(new Rect(25, 160, 100, 30), "Dampen");
            ///
            ///        hSliderValueSpeedX = GUI.HorizontalSlider(new Rect(135, 45, 100, 30), hSliderValueSpeedX, 0.0f, 2.0f);
            ///        hSliderValueSpeedY = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValueSpeedY, 0.0f, 2.0f);
            ///        hSliderValueSpeedZ = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueSpeedZ, 0.0f, 2.0f);
            ///        hSliderValueDampen = GUI.HorizontalSlider(new Rect(135, 165, 100, 30), hSliderValueDampen, 0.0f, 1.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve limitX { get => limitXBlittable; set => limitXBlittable = value; }
            [NativeName("LimitX")] private extern MinMaxCurveBlittable limitXBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Change the limit multiplier on the x-axis.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall limit multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueSpeedX = 0.0f;
            ///    public float hSliderValueSpeedY = 0.0f;
            ///    public float hSliderValueSpeedZ = 0.0f;
            ///    public float hSliderValueDampen = 0.1f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var limitVelocityOverLifetime = ps.limitVelocityOverLifetime;
            ///        limitVelocityOverLifetime.enabled = true;
            ///        limitVelocityOverLifetime.separateAxes = true;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var limitVelocityOverLifetime = ps.limitVelocityOverLifetime;
            ///        limitVelocityOverLifetime.limitXMultiplier = hSliderValueSpeedX;
            ///        limitVelocityOverLifetime.limitYMultiplier = hSliderValueSpeedY;
            ///        limitVelocityOverLifetime.limitZMultiplier = hSliderValueSpeedZ;
            ///        limitVelocityOverLifetime.dampen = hSliderValueDampen;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Speed Limit X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Speed Limit Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Speed Limit Z");
            ///        GUI.Label(new Rect(25, 160, 100, 30), "Dampen");
            ///
            ///        hSliderValueSpeedX = GUI.HorizontalSlider(new Rect(135, 45, 100, 30), hSliderValueSpeedX, 0.0f, 2.0f);
            ///        hSliderValueSpeedY = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValueSpeedY, 0.0f, 2.0f);
            ///        hSliderValueSpeedZ = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueSpeedZ, 0.0f, 2.0f);
            ///        hSliderValueDampen = GUI.HorizontalSlider(new Rect(135, 165, 100, 30), hSliderValueDampen, 0.0f, 1.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float limitXMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Maximum velocity curve for the y-axis.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueSpeedX = 0.0f;
            ///    public float hSliderValueSpeedY = 0.0f;
            ///    public float hSliderValueSpeedZ = 0.0f;
            ///    public float hSliderValueDampen = 0.1f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var limitVelocityOverLifetime = ps.limitVelocityOverLifetime;
            ///        limitVelocityOverLifetime.enabled = true;
            ///        limitVelocityOverLifetime.separateAxes = true;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var limitVelocityOverLifetime = ps.limitVelocityOverLifetime;
            ///        limitVelocityOverLifetime.limitX = hSliderValueSpeedX;
            ///        limitVelocityOverLifetime.limitY = hSliderValueSpeedY;
            ///        limitVelocityOverLifetime.limitZ = hSliderValueSpeedZ;
            ///        limitVelocityOverLifetime.dampen = hSliderValueDampen;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Speed Limit X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Speed Limit Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Speed Limit Z");
            ///        GUI.Label(new Rect(25, 160, 100, 30), "Dampen");
            ///
            ///        hSliderValueSpeedX = GUI.HorizontalSlider(new Rect(135, 45, 100, 30), hSliderValueSpeedX, 0.0f, 2.0f);
            ///        hSliderValueSpeedY = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValueSpeedY, 0.0f, 2.0f);
            ///        hSliderValueSpeedZ = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueSpeedZ, 0.0f, 2.0f);
            ///        hSliderValueDampen = GUI.HorizontalSlider(new Rect(135, 165, 100, 30), hSliderValueDampen, 0.0f, 1.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve limitY { get => limitYBlittable; set => limitYBlittable = value; }
            [NativeName("LimitY")] private extern MinMaxCurveBlittable limitYBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Change the limit multiplier on the y-axis.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall limit multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueSpeedX = 0.0f;
            ///    public float hSliderValueSpeedY = 0.0f;
            ///    public float hSliderValueSpeedZ = 0.0f;
            ///    public float hSliderValueDampen = 0.1f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var limitVelocityOverLifetime = ps.limitVelocityOverLifetime;
            ///        limitVelocityOverLifetime.enabled = true;
            ///        limitVelocityOverLifetime.separateAxes = true;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var limitVelocityOverLifetime = ps.limitVelocityOverLifetime;
            ///        limitVelocityOverLifetime.limitXMultiplier = hSliderValueSpeedX;
            ///        limitVelocityOverLifetime.limitYMultiplier = hSliderValueSpeedY;
            ///        limitVelocityOverLifetime.limitZMultiplier = hSliderValueSpeedZ;
            ///        limitVelocityOverLifetime.dampen = hSliderValueDampen;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Speed Limit X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Speed Limit Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Speed Limit Z");
            ///        GUI.Label(new Rect(25, 160, 100, 30), "Dampen");
            ///
            ///        hSliderValueSpeedX = GUI.HorizontalSlider(new Rect(135, 45, 100, 30), hSliderValueSpeedX, 0.0f, 2.0f);
            ///        hSliderValueSpeedY = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValueSpeedY, 0.0f, 2.0f);
            ///        hSliderValueSpeedZ = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueSpeedZ, 0.0f, 2.0f);
            ///        hSliderValueDampen = GUI.HorizontalSlider(new Rect(135, 165, 100, 30), hSliderValueDampen, 0.0f, 1.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float limitYMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Maximum velocity curve for the z-axis.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueSpeedX = 0.0f;
            ///    public float hSliderValueSpeedY = 0.0f;
            ///    public float hSliderValueSpeedZ = 0.0f;
            ///    public float hSliderValueDampen = 0.1f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var limitVelocityOverLifetime = ps.limitVelocityOverLifetime;
            ///        limitVelocityOverLifetime.enabled = true;
            ///        limitVelocityOverLifetime.separateAxes = true;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var limitVelocityOverLifetime = ps.limitVelocityOverLifetime;
            ///        limitVelocityOverLifetime.limitX = hSliderValueSpeedX;
            ///        limitVelocityOverLifetime.limitY = hSliderValueSpeedY;
            ///        limitVelocityOverLifetime.limitZ = hSliderValueSpeedZ;
            ///        limitVelocityOverLifetime.dampen = hSliderValueDampen;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Speed Limit X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Speed Limit Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Speed Limit Z");
            ///        GUI.Label(new Rect(25, 160, 100, 30), "Dampen");
            ///
            ///        hSliderValueSpeedX = GUI.HorizontalSlider(new Rect(135, 45, 100, 30), hSliderValueSpeedX, 0.0f, 2.0f);
            ///        hSliderValueSpeedY = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValueSpeedY, 0.0f, 2.0f);
            ///        hSliderValueSpeedZ = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueSpeedZ, 0.0f, 2.0f);
            ///        hSliderValueDampen = GUI.HorizontalSlider(new Rect(135, 165, 100, 30), hSliderValueDampen, 0.0f, 1.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve limitZ { get => limitZBlittable; set => limitZBlittable = value; }
            [NativeName("LimitZ")] private extern MinMaxCurveBlittable limitZBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Change the limit multiplier on the z-axis.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall limit multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueSpeedX = 0.0f;
            ///    public float hSliderValueSpeedY = 0.0f;
            ///    public float hSliderValueSpeedZ = 0.0f;
            ///    public float hSliderValueDampen = 0.1f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var limitVelocityOverLifetime = ps.limitVelocityOverLifetime;
            ///        limitVelocityOverLifetime.enabled = true;
            ///        limitVelocityOverLifetime.separateAxes = true;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var limitVelocityOverLifetime = ps.limitVelocityOverLifetime;
            ///        limitVelocityOverLifetime.limitXMultiplier = hSliderValueSpeedX;
            ///        limitVelocityOverLifetime.limitYMultiplier = hSliderValueSpeedY;
            ///        limitVelocityOverLifetime.limitZMultiplier = hSliderValueSpeedZ;
            ///        limitVelocityOverLifetime.dampen = hSliderValueDampen;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Speed Limit X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Speed Limit Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Speed Limit Z");
            ///        GUI.Label(new Rect(25, 160, 100, 30), "Dampen");
            ///
            ///        hSliderValueSpeedX = GUI.HorizontalSlider(new Rect(135, 45, 100, 30), hSliderValueSpeedX, 0.0f, 2.0f);
            ///        hSliderValueSpeedY = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValueSpeedY, 0.0f, 2.0f);
            ///        hSliderValueSpeedZ = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueSpeedZ, 0.0f, 2.0f);
            ///        hSliderValueDampen = GUI.HorizontalSlider(new Rect(135, 165, 100, 30), hSliderValueDampen, 0.0f, 1.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float limitZMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Maximum velocity curve, when not using one curve per axis.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool moduleEnabled = true;
            ///    public float hSliderValueSpeed = 0.0f;
            ///    public float hSliderValueDampen = 0.1f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var limitVelocityOverLifetime = ps.limitVelocityOverLifetime;
            ///        limitVelocityOverLifetime.enabled = moduleEnabled;
            ///        limitVelocityOverLifetime.limit = hSliderValueSpeed;
            ///        limitVelocityOverLifetime.dampen = hSliderValueDampen;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        moduleEnabled = GUI.Toggle(new Rect(25, 45, 200, 30), moduleEnabled, "Enabled");
            ///
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Speed Limit");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Dampen");
            ///
            ///        hSliderValueSpeed = GUI.HorizontalSlider(new Rect(95, 85, 100, 30), hSliderValueSpeed, 0.0f, 2.0f);
            ///        hSliderValueDampen = GUI.HorizontalSlider(new Rect(95, 125, 100, 30), hSliderValueDampen, 0.0f, 1.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve limit { get => limitBlittable; set => limitBlittable = value; }
            [NativeName("Magnitude")] private extern MinMaxCurveBlittable limitBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Change the limit multiplier.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall limit multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool moduleEnabled = true;
            ///    public float hSliderValueSpeed = 0.0f;
            ///    public float hSliderValueDampen = 0.1f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var limitVelocityOverLifetime = ps.limitVelocityOverLifetime;
            ///        limitVelocityOverLifetime.enabled = moduleEnabled;
            ///        limitVelocityOverLifetime.limitMultiplier = hSliderValueSpeed;
            ///        limitVelocityOverLifetime.dampen = hSliderValueDampen;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        moduleEnabled = GUI.Toggle(new Rect(25, 45, 200, 30), moduleEnabled, "Enabled");
            ///
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Speed Limit");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Dampen");
            ///
            ///        hSliderValueSpeed = GUI.HorizontalSlider(new Rect(95, 85, 100, 30), hSliderValueSpeed, 0.0f, 2.0f);
            ///        hSliderValueDampen = GUI.HorizontalSlider(new Rect(95, 125, 100, 30), hSliderValueDampen, 0.0f, 1.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            [NativeName("MagnitudeMultiplier")]
            extern public float limitMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Controls how much this module dampens particle velocities that exceed the velocity limit.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool moduleEnabled = true;
            ///    public float hSliderValueSpeed = 0.0f;
            ///    public float hSliderValueDampen = 0.1f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var limitVelocityOverLifetime = ps.limitVelocityOverLifetime;
            ///        limitVelocityOverLifetime.enabled = moduleEnabled;
            ///        limitVelocityOverLifetime.limitMultiplier = hSliderValueSpeed;
            ///        limitVelocityOverLifetime.dampen = hSliderValueDampen;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        moduleEnabled = GUI.Toggle(new Rect(25, 45, 200, 30), moduleEnabled, "Enabled");
            ///
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Speed Limit");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Dampen");
            ///
            ///        hSliderValueSpeed = GUI.HorizontalSlider(new Rect(95, 85, 100, 30), hSliderValueSpeed, 0.0f, 2.0f);
            ///        hSliderValueDampen = GUI.HorizontalSlider(new Rect(95, 125, 100, 30), hSliderValueDampen, 0.0f, 1.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float dampen { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Set the velocity limit on each axis separately. This module uses <see cref="ParticleSystem.LimitVelocityOverLifetimeModule.drag" /> to dampen a particle's velocity if the velocity exceeds this value.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueSpeedX = 0.0f;
            ///    public float hSliderValueSpeedY = 0.0f;
            ///    public float hSliderValueSpeedZ = 0.0f;
            ///    public float hSliderValueDampen = 0.1f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var limitVelocityOverLifetime = ps.limitVelocityOverLifetime;
            ///        limitVelocityOverLifetime.enabled = true;
            ///        limitVelocityOverLifetime.separateAxes = true;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var limitVelocityOverLifetime = ps.limitVelocityOverLifetime;
            ///        limitVelocityOverLifetime.limitXMultiplier = hSliderValueSpeedX;
            ///        limitVelocityOverLifetime.limitYMultiplier = hSliderValueSpeedY;
            ///        limitVelocityOverLifetime.limitZMultiplier = hSliderValueSpeedZ;
            ///        limitVelocityOverLifetime.dampen = hSliderValueDampen;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Speed Limit X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Speed Limit Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Speed Limit Z");
            ///        GUI.Label(new Rect(25, 160, 100, 30), "Dampen");
            ///
            ///        hSliderValueSpeedX = GUI.HorizontalSlider(new Rect(135, 45, 100, 30), hSliderValueSpeedX, 0.0f, 2.0f);
            ///        hSliderValueSpeedY = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValueSpeedY, 0.0f, 2.0f);
            ///        hSliderValueSpeedZ = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueSpeedZ, 0.0f, 2.0f);
            ///        hSliderValueDampen = GUI.HorizontalSlider(new Rect(135, 165, 100, 30), hSliderValueDampen, 0.0f, 1.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public bool separateAxes { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Specifies if the velocity limits are in local space (rotated with the transform) or world space.</summary>
            ///<remarks>Only applies when separateAxes is set to true.</remarks>
            ///<seealso cref="ParticleSystem.LimitVelocityOverLifetimeModule.separateAxes" />
            extern public ParticleSystemSimulationSpace space { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Controls the amount of drag that this modules applies to the particle velocities.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueDrag = 1.0f;
            ///    public bool hToggleUseSize = false;
            ///    public bool hToggleUseVelocity = false;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var limitVelocityOverLifetime = ps.limitVelocityOverLifetime;
            ///        limitVelocityOverLifetime.enabled = true;
            ///
            ///        var main = ps.main;
            ///        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 1.5f);
            ///        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 5.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var limitVelocityOverLifetime = ps.limitVelocityOverLifetime;
            ///        limitVelocityOverLifetime.drag = hSliderValueDrag;
            ///        limitVelocityOverLifetime.multiplyDragByParticleSize = hToggleUseSize;
            ///        limitVelocityOverLifetime.multiplyDragByParticleVelocity = hToggleUseVelocity;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Drag");
            ///
            ///        hSliderValueDrag = GUI.HorizontalSlider(new Rect(135, 45, 100, 30), hSliderValueDrag, 0.0f, 3.0f);
            ///        hToggleUseSize = GUI.Toggle(new Rect(25, 85, 200, 30), hToggleUseSize, "Multiply by Size");
            ///        hToggleUseVelocity = GUI.Toggle(new Rect(25, 125, 200, 30), hToggleUseVelocity, "Multiply by Velocity");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.LimitVelocityOverLifetimeModule.multiplyDragByParticleSize" />
            ///<seealso cref="ParticleSystem.LimitVelocityOverLifetimeModule.multiplyDragByParticleVelocity" />
            public MinMaxCurve drag { get => dragBlittable; set => dragBlittable = value; }
            [NativeName("Drag")] private extern MinMaxCurveBlittable dragBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Specifies the drag multiplier.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall drag multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueDrag = 1.0f;
            ///    public bool hToggleUseSize = false;
            ///    public bool hToggleUseVelocity = false;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var limitVelocityOverLifetime = ps.limitVelocityOverLifetime;
            ///        limitVelocityOverLifetime.enabled = true;
            ///
            ///        var main = ps.main;
            ///        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 1.5f);
            ///        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 5.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var limitVelocityOverLifetime = ps.limitVelocityOverLifetime;
            ///        limitVelocityOverLifetime.dragMultiplier = hSliderValueDrag;
            ///        limitVelocityOverLifetime.multiplyDragByParticleSize = hToggleUseSize;
            ///        limitVelocityOverLifetime.multiplyDragByParticleVelocity = hToggleUseVelocity;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Drag");
            ///
            ///        hSliderValueDrag = GUI.HorizontalSlider(new Rect(135, 45, 100, 30), hSliderValueDrag, 0.0f, 3.0f);
            ///        hToggleUseSize = GUI.Toggle(new Rect(25, 85, 200, 30), hToggleUseSize, "Multiply by Size");
            ///        hToggleUseVelocity = GUI.Toggle(new Rect(25, 125, 200, 30), hToggleUseVelocity, "Multiply by Velocity");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float dragMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Adjust the amount of drag this module applies to particles, based on their sizes.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueDrag = 1.0f;
            ///    public bool hToggleUseSize = false;
            ///    public bool hToggleUseVelocity = false;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var limitVelocityOverLifetime = ps.limitVelocityOverLifetime;
            ///        limitVelocityOverLifetime.enabled = true;
            ///
            ///        var main = ps.main;
            ///        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 1.5f);
            ///        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 5.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var limitVelocityOverLifetime = ps.limitVelocityOverLifetime;
            ///        limitVelocityOverLifetime.dragMultiplier = hSliderValueDrag;
            ///        limitVelocityOverLifetime.multiplyDragByParticleSize = hToggleUseSize;
            ///        limitVelocityOverLifetime.multiplyDragByParticleVelocity = hToggleUseVelocity;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Drag");
            ///
            ///        hSliderValueDrag = GUI.HorizontalSlider(new Rect(135, 45, 100, 30), hSliderValueDrag, 0.0f, 3.0f);
            ///        hToggleUseSize = GUI.Toggle(new Rect(25, 85, 200, 30), hToggleUseSize, "Multiply by Size");
            ///        hToggleUseVelocity = GUI.Toggle(new Rect(25, 125, 200, 30), hToggleUseVelocity, "Multiply by Velocity");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.LimitVelocityOverLifetimeModule.drag" />
            ///<seealso cref="ParticleSystem.LimitVelocityOverLifetimeModule.multiplyDragByParticleVelocity" />
            extern public bool multiplyDragByParticleSize { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Adjust the amount of drag this module applies to particles, based on their speeds.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueDrag = 1.0f;
            ///    public bool hToggleUseSize = false;
            ///    public bool hToggleUseVelocity = false;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var limitVelocityOverLifetime = ps.limitVelocityOverLifetime;
            ///        limitVelocityOverLifetime.enabled = true;
            ///
            ///        var main = ps.main;
            ///        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 1.5f);
            ///        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 5.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var limitVelocityOverLifetime = ps.limitVelocityOverLifetime;
            ///        limitVelocityOverLifetime.dragMultiplier = hSliderValueDrag;
            ///        limitVelocityOverLifetime.multiplyDragByParticleSize = hToggleUseSize;
            ///        limitVelocityOverLifetime.multiplyDragByParticleVelocity = hToggleUseVelocity;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Drag");
            ///
            ///        hSliderValueDrag = GUI.HorizontalSlider(new Rect(135, 45, 100, 30), hSliderValueDrag, 0.0f, 3.0f);
            ///        hToggleUseSize = GUI.Toggle(new Rect(25, 85, 200, 30), hToggleUseSize, "Multiply by Size");
            ///        hToggleUseVelocity = GUI.Toggle(new Rect(25, 125, 200, 30), hToggleUseVelocity, "Multiply by Velocity");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.LimitVelocityOverLifetimeModule.drag" />
            ///<seealso cref="ParticleSystem.LimitVelocityOverLifetimeModule.multiplyDragByParticleSize" />
            extern public bool multiplyDragByParticleVelocity { get; [NativeMethod(ThrowsException = true)] set; }
        }

        ///<summary>Script interface for InheritVelocityModule.</summary>
        ///<remarks>This module controls how the emitter tranfers its velocity to the particles as it emits them. It applies velocities to particles based on the velocity of the GameObject that spawned them. For most Particle Systems, this is the GameObject velocity, but for sub-emitters, the velocity comes from the parent particle that the sub-emitter particle originated from.</remarks>
        ///<seealso cref="ParticleSystem" />
        ///<seealso cref="ParticleSystem.inheritVelocity" />
        public partial struct InheritVelocityModule
        {
            internal InheritVelocityModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            ///<summary>Specifies whether the InheritVelocityModule is enabled or disabled.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool moduleEnabled = true;
            ///    public float hSliderValue = 5.0f;
            ///    public ParticleSystemInheritVelocityMode inheritMode;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.simulationSpace = ParticleSystemSimulationSpace.World; // rate over distance only works for world space simulations
            ///
            ///        // add a sphere so we can see our transform position as it moves
            ///        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ///        sphere.transform.parent = ps.transform;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var inheritVelocity = ps.inheritVelocity;
            ///        inheritVelocity.enabled = moduleEnabled;
            ///        inheritVelocity.curveMultiplier = hSliderValue;
            ///        inheritVelocity.mode = inheritMode;
            ///
            ///        ps.transform.position = new Vector3(Mathf.Sin(Time.time * 2.0f) * 2.0f, 0.0f, 0.0f);
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        moduleEnabled = GUI.Toggle(new Rect(25, 45, 100, 30), moduleEnabled, "Enabled");
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 85, 100, 30), hSliderValue, 0.0f, 10.0f);
            ///        inheritMode = (ParticleSystemInheritVelocityMode)GUI.SelectionGrid(new Rect(25, 125, 300, 30), (int)inheritMode, new GUIContent[] { new GUIContent("Initial"), new GUIContent("Current") }, 2);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.inheritVelocity" />
            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Specifies how to apply emitter velocity to particles.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool moduleEnabled = true;
            ///    public float hSliderValue = 5.0f;
            ///    public ParticleSystemInheritVelocityMode inheritMode;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.simulationSpace = ParticleSystemSimulationSpace.World; // rate over distance only works for world space simulations
            ///
            ///        // add a sphere so we can see our transform position as it moves
            ///        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ///        sphere.transform.parent = ps.transform;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var inheritVelocity = ps.inheritVelocity;
            ///        inheritVelocity.enabled = moduleEnabled;
            ///        inheritVelocity.curveMultiplier = hSliderValue;
            ///        inheritVelocity.mode = inheritMode;
            ///
            ///        ps.transform.position = new Vector3(Mathf.Sin(Time.time * 2.0f) * 2.0f, 0.0f, 0.0f);
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        moduleEnabled = GUI.Toggle(new Rect(25, 45, 100, 30), moduleEnabled, "Enabled");
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 85, 100, 30), hSliderValue, 0.0f, 10.0f);
            ///        inheritMode = (ParticleSystemInheritVelocityMode)GUI.SelectionGrid(new Rect(25, 125, 300, 30), (int)inheritMode, new GUIContent[] { new GUIContent("Initial"), new GUIContent("Current") }, 2);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public ParticleSystemInheritVelocityMode mode { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Curve to define how much of the emitter velocity the system applies during the lifetime of a particle.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool isEnabled = true;
            ///    public float hSliderValue = 5.0f;
            ///    public ParticleSystemInheritVelocityMode inheritMode;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.simulationSpace = ParticleSystemSimulationSpace.World; // rate over distance only works for world space simulations
            ///
            ///        // add a sphere so we can see our transform position as it moves
            ///        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ///        sphere.transform.parent = ps.transform;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var inheritVelocity = ps.inheritVelocity;
            ///        inheritVelocity.enabled = isEnabled;
            ///        inheritVelocity.curve = hSliderValue;
            ///        inheritVelocity.mode = inheritMode;
            ///
            ///        ps.transform.position = new Vector3(Mathf.Sin(Time.time * 2.0f) * 2.0f, 0.0f, 0.0f);
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        isEnabled = GUI.Toggle(new Rect(25, 45, 100, 30), isEnabled, "Enabled");
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 85, 100, 30), hSliderValue, 0.0f, 10.0f);
            ///        inheritMode = (ParticleSystemInheritVelocityMode)GUI.SelectionGrid(new Rect(25, 125, 300, 30), (int)inheritMode, new GUIContent[] { new GUIContent("Initial"), new GUIContent("Current") }, 2);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve curve { get => curveBlittable; set => curveBlittable = value; }
            [NativeName("Curve")] private extern MinMaxCurveBlittable curveBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Change the curve multiplier.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall curve multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool moduleEnabled = true;
            ///    public float hSliderValue = 5.0f;
            ///    public ParticleSystemInheritVelocityMode inheritMode;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.simulationSpace = ParticleSystemSimulationSpace.World; // rate over distance only works for world space simulations
            ///
            ///        // add a sphere so we can see our transform position as it moves
            ///        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ///        sphere.transform.parent = ps.transform;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var inheritVelocity = ps.inheritVelocity;
            ///        inheritVelocity.enabled = moduleEnabled;
            ///        inheritVelocity.curveMultiplier = hSliderValue;
            ///        inheritVelocity.mode = inheritMode;
            ///
            ///        ps.transform.position = new Vector3(Mathf.Sin(Time.time * 2.0f) * 2.0f, 0.0f, 0.0f);
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        moduleEnabled = GUI.Toggle(new Rect(25, 45, 100, 30), moduleEnabled, "Enabled");
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 85, 100, 30), hSliderValue, 0.0f, 10.0f);
            ///        inheritMode = (ParticleSystemInheritVelocityMode)GUI.SelectionGrid(new Rect(25, 125, 300, 30), (int)inheritMode, new GUIContent[] { new GUIContent("Initial"), new GUIContent("Current") }, 2);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float curveMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
        }

        ///<summary>The Lifetime By Emitter Speed Module controls the initial lifetime of each particle based on the speed of the emitter when the particle was spawned.</summary>
        ///<remarks>This module multiplies the start lifetime of particles with a value that depends on the speed of the object that spawned them. For most Particle Systems, this is the GameObject velocity, but for sub-emitters, the velocity comes from the parent particle that the sub-emitter particle originated from.</remarks>
        ///<seealso cref="ParticleSystem" />
        ///<seealso cref="ParticleSystem.MainModule.startLifetime" />
        public partial struct LifetimeByEmitterSpeedModule
        {
            internal LifetimeByEmitterSpeedModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            ///<summary>Use this property to enable or disable the LifetimeByEmitterSpeed module.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool moduleEnabled = true;
            ///    public float maxSpeed = 5.0f;
            ///    public AnimationCurve curve = AnimationCurve.EaseInOut(0.0f, 1.0f, 1.0f, 0.2f);
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var mainModule = ps.main;
            ///        mainModule.startLifetime = 1.0f;
            ///
            ///        // make particles less random to more clearly see effect of lifetime.
            ///        var shapeModule = ps.shape;
            ///        shapeModule.radius = 0.1f;
            ///        shapeModule.angle = 1.0f;
            ///
            ///        var main = ps.main;
            ///        main.simulationSpace = ParticleSystemSimulationSpace.World;
            ///
            ///        // add a sphere so we can see our transform position as it moves
            ///        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ///        sphere.transform.parent = ps.transform;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var lifetimeByEmitterSpeed = ps.lifetimeByEmitterSpeed;
            ///        lifetimeByEmitterSpeed.enabled = moduleEnabled;
            ///        lifetimeByEmitterSpeed.range = new Vector2(0, maxSpeed);
            ///        lifetimeByEmitterSpeed.curve = new ParticleSystem.MinMaxCurve(1f, curve);
            ///
            ///        ps.transform.position = new Vector3(Mathf.Sin(Time.time * 2.0f) * 4.0f, 0.0f, 0.0f);
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        moduleEnabled = GUI.Toggle(new Rect(25, 45, 100, 30), moduleEnabled, "Enabled");
            ///        maxSpeed = GUI.HorizontalSlider(new Rect(25, 85, 100, 30), maxSpeed, 0.0f, 10.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Use this curve to define which value to multiply the start lifetime of a particle with, based on the speed of the emitter when the particle is spawned.</summary>
            ///<remarks>Note that you should set this curve to <see cref="ParticleSystemCurveMode.Curve" /> or <see cref="ParticleSystemCurveMode.TwoCurves" /> in order to provide any value, as setting it to <see cref="ParticleSystemCurveMode.Constant" /> or <see cref="ParticleSystemCurveMode.TwoConstants" /> doesn't do anything that couldn't also be done without using this module, simply by using <see cref="ParticleSystem.MainModule.startLifetime" /> alone.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool moduleEnabled = true;
            ///    public float maxSpeed = 5.0f;
            ///    public AnimationCurve curve = AnimationCurve.EaseInOut(0.0f, 1.0f, 1.0f, 0.2f);
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var mainModule = ps.main;
            ///        mainModule.startLifetime = 1.0f;
            ///
            ///        // make particles less random to more clearly see effect of lifetime.
            ///        var shapeModule = ps.shape;
            ///        shapeModule.radius = 0.1f;
            ///        shapeModule.angle = 1.0f;
            ///
            ///        var main = ps.main;
            ///        main.simulationSpace = ParticleSystemSimulationSpace.World;
            ///
            ///        // add a sphere so we can see our transform position as it moves
            ///        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ///        sphere.transform.parent = ps.transform;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var lifetimeByEmitterSpeed = ps.lifetimeByEmitterSpeed;
            ///        lifetimeByEmitterSpeed.enabled = moduleEnabled;
            ///        lifetimeByEmitterSpeed.range = new Vector2(0, maxSpeed);
            ///        lifetimeByEmitterSpeed.curve = new ParticleSystem.MinMaxCurve(1f, curve);
            ///
            ///        ps.transform.position = new Vector3(Mathf.Sin(Time.time * 2.0f) * 4.0f, 0.0f, 0.0f);
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        moduleEnabled = GUI.Toggle(new Rect(25, 45, 100, 30), moduleEnabled, "Enabled");
            ///        maxSpeed = GUI.HorizontalSlider(new Rect(25, 85, 100, 30), maxSpeed, 0.0f, 10.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve curve { get => curveBlittable; set => curveBlittable = value; }
            [NativeName("Curve")] private extern MinMaxCurveBlittable curveBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Use this property to change the curve multiplier.</summary>
            ///<remarks>If you only want to change the overall curve multiplier, using this property is more efficient than accessing the whole curve.</remarks>
            extern public float curveMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Control the start lifetime multiplier between these minimum and maximum speeds of the emitter.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool moduleEnabled = true;
            ///    public float maxSpeed = 5.0f;
            ///    public AnimationCurve curve = AnimationCurve.EaseInOut(0.0f, 1.0f, 1.0f, 0.2f);
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var mainModule = ps.main;
            ///        mainModule.startLifetime = 1.0f;
            ///
            ///        // make particles less random to more clearly see effect of lifetime.
            ///        var shapeModule = ps.shape;
            ///        shapeModule.radius = 0.1f;
            ///        shapeModule.angle = 1.0f;
            ///
            ///        var main = ps.main;
            ///        main.simulationSpace = ParticleSystemSimulationSpace.World;
            ///
            ///        // add a sphere so we can see our transform position as it moves
            ///        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ///        sphere.transform.parent = ps.transform;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var lifetimeByEmitterSpeed = ps.lifetimeByEmitterSpeed;
            ///        lifetimeByEmitterSpeed.enabled = moduleEnabled;
            ///        lifetimeByEmitterSpeed.range = new Vector2(0, maxSpeed);
            ///        lifetimeByEmitterSpeed.curve = new ParticleSystem.MinMaxCurve(1f, curve);
            ///
            ///        ps.transform.position = new Vector3(Mathf.Sin(Time.time * 2.0f) * 4.0f, 0.0f, 0.0f);
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        moduleEnabled = GUI.Toggle(new Rect(25, 45, 100, 30), moduleEnabled, "Enabled");
            ///        maxSpeed = GUI.HorizontalSlider(new Rect(25, 85, 100, 30), maxSpeed, 0.0f, 10.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public Vector2 range { get; [NativeMethod(ThrowsException = true)] set; }
        }

        ///<summary>Script interface for ForceOverLifetimeModule.</summary>
        ///<remarks>Use this module to apply forces to particles. The system applies forces to the particle velocities on each frame.</remarks>
        ///<seealso cref="ParticleSystem" />
        ///<seealso cref="ParticleSystem.forceOverLifetime" />
        public partial struct ForceOverLifetimeModule
        {
            internal ForceOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            ///<summary>Specifies whether the ForceOverLifetimeModule is enabled or disabled.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool moduleEnabled = true;
            ///    public float hSliderValue = 10.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var forceOverLifetime = ps.forceOverLifetime;
            ///        forceOverLifetime.enabled = moduleEnabled;
            ///        forceOverLifetime.x = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        moduleEnabled = GUI.Toggle(new Rect(25, 45, 100, 30), moduleEnabled, "Enabled");
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 85, 100, 30), hSliderValue, -50.0f, 50.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.forceOverLifetime" />
            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>The curve that defines particle forces in the x-axis.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool moduleEnabled = true;
            ///    public float hSliderValue = 10.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var forceOverLifetime = ps.forceOverLifetime;
            ///        forceOverLifetime.enabled = moduleEnabled;
            ///        forceOverLifetime.x = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        moduleEnabled = GUI.Toggle(new Rect(25, 45, 100, 30), moduleEnabled, "Enabled");
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 85, 100, 30), hSliderValue, -50.0f, 50.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve x { get => xBlittable; set => xBlittable = value; }
            [NativeName("X")] private extern MinMaxCurveBlittable xBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>The curve defining particle forces in the y-axis.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool moduleEnabled = true;
            ///    public float hSliderValue = 10.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var forceOverLifetime = ps.forceOverLifetime;
            ///        forceOverLifetime.enabled = moduleEnabled;
            ///        forceOverLifetime.y = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        moduleEnabled = GUI.Toggle(new Rect(25, 45, 100, 30), moduleEnabled, "Enabled");
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 85, 100, 30), hSliderValue, -50.0f, 50.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve y { get => yBlittable; set => yBlittable = value; }
            [NativeName("Y")] private extern MinMaxCurveBlittable yBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>The curve defining particle forces in the z-axis.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool moduleEnabled = true;
            ///    public float hSliderValue = 10.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var forceOverLifetime = ps.forceOverLifetime;
            ///        forceOverLifetime.enabled = moduleEnabled;
            ///        forceOverLifetime.z = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        moduleEnabled = GUI.Toggle(new Rect(25, 45, 100, 30), moduleEnabled, "Enabled");
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 85, 100, 30), hSliderValue, -50.0f, 50.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve z { get => zBlittable; set => zBlittable = value; }
            [NativeName("Z")] private extern MinMaxCurveBlittable zBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Defines the x-axis multiplier.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall x-axis multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool moduleEnabled = true;
            ///    public float hSliderValue = 10.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var forceOverLifetime = ps.forceOverLifetime;
            ///        forceOverLifetime.enabled = moduleEnabled;
            ///        forceOverLifetime.xMultiplier = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        moduleEnabled = GUI.Toggle(new Rect(25, 45, 100, 30), moduleEnabled, "Enabled");
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 85, 100, 30), hSliderValue, -50.0f, 50.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float xMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Defines the y-axis multiplier.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall y-axis multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool moduleEnabled = true;
            ///    public float hSliderValue = 10.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var forceOverLifetime = ps.forceOverLifetime;
            ///        forceOverLifetime.enabled = moduleEnabled;
            ///        forceOverLifetime.yMultiplier = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        moduleEnabled = GUI.Toggle(new Rect(25, 45, 100, 30), moduleEnabled, "Enabled");
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 85, 100, 30), hSliderValue, -50.0f, 50.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float yMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Defines the z-axis multiplier.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall z-axis multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool moduleEnabled = true;
            ///    public float hSliderValue = 10.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var forceOverLifetime = ps.forceOverLifetime;
            ///        forceOverLifetime.enabled = moduleEnabled;
            ///        forceOverLifetime.zMultiplier = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        moduleEnabled = GUI.Toggle(new Rect(25, 45, 100, 30), moduleEnabled, "Enabled");
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 85, 100, 30), hSliderValue, -50.0f, 50.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float zMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Specifies whether the modules applies the forces in local or world space.</summary>
            extern public ParticleSystemSimulationSpace space { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>When randomly selecting values between two curves or constants, this flag causes the system to choose a new random force on each frame.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool randomized;
            ///    public float hSliderValue = 30.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var forceOverLifetime = ps.forceOverLifetime;
            ///        forceOverLifetime.enabled = true;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var forceOverLifetime = ps.forceOverLifetime;
            ///        forceOverLifetime.randomized = randomized;
            ///        forceOverLifetime.x = new ParticleSystem.MinMaxCurve(-hSliderValue, hSliderValue);
            ///        forceOverLifetime.y = new ParticleSystem.MinMaxCurve(-hSliderValue, hSliderValue);
            ///        forceOverLifetime.z = new ParticleSystem.MinMaxCurve(-hSliderValue, hSliderValue);
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        randomized = GUI.Toggle(new Rect(25, 45, 100, 30), randomized, "Randomized Per Frame");
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 85, 100, 30), hSliderValue, 0.0f, 50.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public bool randomized { get; [NativeMethod(ThrowsException = true)] set; }
        }

        ///<summary>Script interface for ColorOverLifetimeModule.</summary>
        ///<remarks>This module changes the colors of particles over time, based on how long each particle has been alive.</remarks>
        ///<seealso cref="ParticleSystem" />
        ///<seealso cref="ParticleSystem.colorOverLifetime" />
        public partial struct ColorOverLifetimeModule
        {
            internal ColorOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            ///<summary>Specifies whether the ColorOverLifetimeModule is enabled or disabled.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var colorOverLifetime = ps.colorOverLifetime;
            ///        colorOverLifetime.enabled = true;
            ///
            ///        Gradient gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///        );
            ///
            ///        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startLifetime = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.colorOverLifetime" />
            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>The gradient that controls the particle colors.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var colorOverLifetime = ps.colorOverLifetime;
            ///        colorOverLifetime.enabled = true;
            ///
            ///        Gradient gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///        );
            ///
            ///        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startLifetime = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxGradient" />
            public MinMaxGradient color { get => colorBlittable; set => colorBlittable = value; }
            [NativeName("Color")] private extern MinMaxGradientBlittable colorBlittable { get; [NativeMethod(ThrowsException = true)] set; }
        }

        ///<summary>Script interface for the ColorBySpeedModule.</summary>
        ///<remarks>This module assigns colors to the particles based on the speed that they are travelling.</remarks>
        ///<seealso cref="ParticleSystem" />
        ///<seealso cref="ParticleSystem.colorBySpeed" />
        public partial struct ColorBySpeedModule
        {
            internal ColorBySpeedModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            ///<summary>Specifies whether the ColorBySpeedModule is enabled or disabled.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var colorBySpeed = ps.colorBySpeed;
            ///        colorBySpeed.enabled = true;
            ///
            ///        Gradient gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///        );
            ///
            ///        colorBySpeed.color = new ParticleSystem.MinMaxGradient(gradient);
            ///        colorBySpeed.range = new Vector2(1.0f, 5.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startSpeed = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.colorBySpeed" />
            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>The gradient that controls the particle colors.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var colorBySpeed = ps.colorBySpeed;
            ///        colorBySpeed.enabled = true;
            ///
            ///        Gradient gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///        );
            ///
            ///        colorBySpeed.color = new ParticleSystem.MinMaxGradient(gradient);
            ///        colorBySpeed.range = new Vector2(1.0f, 5.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startSpeed = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxGradient" />
            public MinMaxGradient color { get => colorBlittable; set => colorBlittable = value; }
            [NativeName("Color")] private extern MinMaxGradientBlittable colorBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Apply the color gradient between these minimum and maximum speeds.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var colorBySpeed = ps.colorBySpeed;
            ///        colorBySpeed.enabled = true;
            ///
            ///        Gradient gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///        );
            ///
            ///        colorBySpeed.color = new ParticleSystem.MinMaxGradient(gradient);
            ///        colorBySpeed.range = new Vector2(1.0f, 5.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startSpeed = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.ColorBySpeedModule.color" />
            extern public Vector2 range { get; [NativeMethod(ThrowsException = true)] set; }
        }

        ///<summary>Script interface for the SizeOverLifetimeModule.</summary>
        ///<remarks>This module controls the size of particles throughout their lifetime.</remarks>
        ///<seealso cref="ParticleSystem" />
        ///<seealso cref="ParticleSystem.sizeOverLifetime" />
        public partial struct SizeOverLifetimeModule
        {
            internal SizeOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            ///<summary>Specifies whether the SizeOverLifetimeModule is enabled or disabled.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var sizeOverLifetime = ps.sizeOverLifetime;
            ///        sizeOverLifetime.enabled = true;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, making it easier to see the size
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startLifetimeMultiplier = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.sizeOverLifetime" />
            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Curve to control particle size based on lifetime.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var sizeOverLifetime = ps.sizeOverLifetime;
            ///        sizeOverLifetime.enabled = true;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, making it easier to see the size
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startLifetimeMultiplier = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve size { get => sizeBlittable; set => sizeBlittable = value; }
            [NativeName("X")] private extern MinMaxCurveBlittable sizeBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>A multiplier for <see cref="ParticleSystem.SizeOverLifetimeModule.size" />.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall size multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var sizeOverLifetime = ps.sizeOverLifetime;
            ///        sizeOverLifetime.enabled = true;
            ///        sizeOverLifetime.sizeMultiplier = 1.0f;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, making it easier to see the size
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startLifetimeMultiplier = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            [NativeName("XMultiplier")]
            extern public float sizeMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Size over lifetime curve for the x-axis.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueX = 1.0f;
            ///    public float hSliderValueY = 1.0f;
            ///    public float hSliderValueZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var sizeOverLifetime = ps.sizeOverLifetime;
            ///        sizeOverLifetime.enabled = true;
            ///        sizeOverLifetime.separateAxes = true;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        ParticleSystem.MinMaxCurve minMaxCurve = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///
            ///        sizeOverLifetime.x = minMaxCurve;
            ///        sizeOverLifetime.y = minMaxCurve;
            ///        sizeOverLifetime.z = minMaxCurve;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, making it easier to see the size
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var sizeOverLifetime = ps.sizeOverLifetime;
            ///        sizeOverLifetime.xMultiplier = hSliderValueX;
            ///        sizeOverLifetime.yMultiplier = hSliderValueY;
            ///        sizeOverLifetime.zMultiplier = hSliderValueZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Z");
            ///
            ///        hSliderValueX = GUI.HorizontalSlider(new Rect(55, 45, 100, 30), hSliderValueX, 1.0f, 5.0f);
            ///        hSliderValueY = GUI.HorizontalSlider(new Rect(55, 85, 100, 30), hSliderValueY, 1.0f, 5.0f);
            ///        hSliderValueZ = GUI.HorizontalSlider(new Rect(55, 125, 100, 30), hSliderValueZ, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve x { get => xBlittable; set => xBlittable = value; }
            [NativeName("X")] private extern MinMaxCurveBlittable xBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Size multiplier along the x-axis.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall size multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueX = 1.0f;
            ///    public float hSliderValueY = 1.0f;
            ///    public float hSliderValueZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var sizeOverLifetime = ps.sizeOverLifetime;
            ///        sizeOverLifetime.enabled = true;
            ///        sizeOverLifetime.separateAxes = true;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        ParticleSystem.MinMaxCurve minMaxCurve = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///
            ///        sizeOverLifetime.x = minMaxCurve;
            ///        sizeOverLifetime.y = minMaxCurve;
            ///        sizeOverLifetime.z = minMaxCurve;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, making it easier to see the size
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var sizeOverLifetime = ps.sizeOverLifetime;
            ///        sizeOverLifetime.xMultiplier = hSliderValueX;
            ///        sizeOverLifetime.yMultiplier = hSliderValueY;
            ///        sizeOverLifetime.zMultiplier = hSliderValueZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Z");
            ///
            ///        hSliderValueX = GUI.HorizontalSlider(new Rect(55, 45, 100, 30), hSliderValueX, 1.0f, 5.0f);
            ///        hSliderValueY = GUI.HorizontalSlider(new Rect(55, 85, 100, 30), hSliderValueY, 1.0f, 5.0f);
            ///        hSliderValueZ = GUI.HorizontalSlider(new Rect(55, 125, 100, 30), hSliderValueZ, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float xMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Size over lifetime curve for the y-axis.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueX = 1.0f;
            ///    public float hSliderValueY = 1.0f;
            ///    public float hSliderValueZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var sizeOverLifetime = ps.sizeOverLifetime;
            ///        sizeOverLifetime.enabled = true;
            ///        sizeOverLifetime.separateAxes = true;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        ParticleSystem.MinMaxCurve minMaxCurve = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///
            ///        sizeOverLifetime.x = minMaxCurve;
            ///        sizeOverLifetime.y = minMaxCurve;
            ///        sizeOverLifetime.z = minMaxCurve;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, making it easier to see the size
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var sizeOverLifetime = ps.sizeOverLifetime;
            ///        sizeOverLifetime.xMultiplier = hSliderValueX;
            ///        sizeOverLifetime.yMultiplier = hSliderValueY;
            ///        sizeOverLifetime.zMultiplier = hSliderValueZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Z");
            ///
            ///        hSliderValueX = GUI.HorizontalSlider(new Rect(55, 45, 100, 30), hSliderValueX, 1.0f, 5.0f);
            ///        hSliderValueY = GUI.HorizontalSlider(new Rect(55, 85, 100, 30), hSliderValueY, 1.0f, 5.0f);
            ///        hSliderValueZ = GUI.HorizontalSlider(new Rect(55, 125, 100, 30), hSliderValueZ, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve y { get => yBlittable; set => yBlittable = value; }
            [NativeName("Y")] private extern MinMaxCurveBlittable yBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Size multiplier along the y-axis.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall size multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueX = 1.0f;
            ///    public float hSliderValueY = 1.0f;
            ///    public float hSliderValueZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var sizeOverLifetime = ps.sizeOverLifetime;
            ///        sizeOverLifetime.enabled = true;
            ///        sizeOverLifetime.separateAxes = true;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        ParticleSystem.MinMaxCurve minMaxCurve = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///
            ///        sizeOverLifetime.x = minMaxCurve;
            ///        sizeOverLifetime.y = minMaxCurve;
            ///        sizeOverLifetime.z = minMaxCurve;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, making it easier to see the size
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var sizeOverLifetime = ps.sizeOverLifetime;
            ///        sizeOverLifetime.xMultiplier = hSliderValueX;
            ///        sizeOverLifetime.yMultiplier = hSliderValueY;
            ///        sizeOverLifetime.zMultiplier = hSliderValueZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Z");
            ///
            ///        hSliderValueX = GUI.HorizontalSlider(new Rect(55, 45, 100, 30), hSliderValueX, 1.0f, 5.0f);
            ///        hSliderValueY = GUI.HorizontalSlider(new Rect(55, 85, 100, 30), hSliderValueY, 1.0f, 5.0f);
            ///        hSliderValueZ = GUI.HorizontalSlider(new Rect(55, 125, 100, 30), hSliderValueZ, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float yMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Size over lifetime curve for the z-axis.</summary>
            ///<remarks>This modules only uses this property for Mesh particles.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueX = 1.0f;
            ///    public float hSliderValueY = 1.0f;
            ///    public float hSliderValueZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var sizeOverLifetime = ps.sizeOverLifetime;
            ///        sizeOverLifetime.enabled = true;
            ///        sizeOverLifetime.separateAxes = true;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        ParticleSystem.MinMaxCurve minMaxCurve = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///
            ///        sizeOverLifetime.x = minMaxCurve;
            ///        sizeOverLifetime.y = minMaxCurve;
            ///        sizeOverLifetime.z = minMaxCurve;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, making it easier to see the size
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var sizeOverLifetime = ps.sizeOverLifetime;
            ///        sizeOverLifetime.xMultiplier = hSliderValueX;
            ///        sizeOverLifetime.yMultiplier = hSliderValueY;
            ///        sizeOverLifetime.zMultiplier = hSliderValueZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Z");
            ///
            ///        hSliderValueX = GUI.HorizontalSlider(new Rect(55, 45, 100, 30), hSliderValueX, 1.0f, 5.0f);
            ///        hSliderValueY = GUI.HorizontalSlider(new Rect(55, 85, 100, 30), hSliderValueY, 1.0f, 5.0f);
            ///        hSliderValueZ = GUI.HorizontalSlider(new Rect(55, 125, 100, 30), hSliderValueZ, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve z { get => zBlittable; set => zBlittable = value; }
            [NativeName("Z")] private extern MinMaxCurveBlittable zBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Size multiplier along the z-axis.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall size multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueX = 1.0f;
            ///    public float hSliderValueY = 1.0f;
            ///    public float hSliderValueZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var sizeOverLifetime = ps.sizeOverLifetime;
            ///        sizeOverLifetime.enabled = true;
            ///        sizeOverLifetime.separateAxes = true;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        ParticleSystem.MinMaxCurve minMaxCurve = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///
            ///        sizeOverLifetime.x = minMaxCurve;
            ///        sizeOverLifetime.y = minMaxCurve;
            ///        sizeOverLifetime.z = minMaxCurve;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, making it easier to see the size
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var sizeOverLifetime = ps.sizeOverLifetime;
            ///        sizeOverLifetime.xMultiplier = hSliderValueX;
            ///        sizeOverLifetime.yMultiplier = hSliderValueY;
            ///        sizeOverLifetime.zMultiplier = hSliderValueZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Z");
            ///
            ///        hSliderValueX = GUI.HorizontalSlider(new Rect(55, 45, 100, 30), hSliderValueX, 1.0f, 5.0f);
            ///        hSliderValueY = GUI.HorizontalSlider(new Rect(55, 85, 100, 30), hSliderValueY, 1.0f, 5.0f);
            ///        hSliderValueZ = GUI.HorizontalSlider(new Rect(55, 125, 100, 30), hSliderValueZ, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float zMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Set the size over lifetime on each axis separately.</summary>
            ///<remarks>When disabled, this module only uses the x-axis size curve and applies it uniformly.</remarks>
            extern public bool separateAxes { get; [NativeMethod(ThrowsException = true)] set; }
        }

        ///<summary>Script interface for the SizeBySpeedModule.</summary>
        ///<remarks>This module controls the size of particles based on their speeds.</remarks>
        ///<seealso cref="ParticleSystem" />
        ///<seealso cref="ParticleSystem.sizeBySpeed" />
        public partial struct SizeBySpeedModule
        {
            internal SizeBySpeedModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            ///<summary>Specifies whether the SizeBySpeedModule is enabled or disabled.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var sizeBySpeed = ps.sizeBySpeed;
            ///        sizeBySpeed.enabled = true;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        sizeBySpeed.size = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///        sizeBySpeed.range = new Vector2(0.9f, 5.0f);
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, making it easier to see the size
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startSpeed = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.sizeBySpeed" />
            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Curve to control particle size based on speed.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var sizeBySpeed = ps.sizeBySpeed;
            ///        sizeBySpeed.enabled = true;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        sizeBySpeed.size = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///        sizeBySpeed.range = new Vector2(0.9f, 5.0f);
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, making it easier to see the size
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startSpeed = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve size { get => sizeBlittable; set => sizeBlittable = value; }
            [NativeName("X")] private extern MinMaxCurveBlittable sizeBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>A multiplier for <see cref="ParticleSystem.SizeBySpeedModule.size" />.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall size multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var sizeBySpeed = ps.sizeBySpeed;
            ///        sizeBySpeed.enabled = true;
            ///
            ///        sizeBySpeed.sizeMultiplier = 1.0f;
            ///        sizeBySpeed.range = new Vector2(0.9f, 5.0f);
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, making it easier to see the size
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startSpeed = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            [NativeName("XMultiplier")]
            extern public float sizeMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Size by speed curve for the x-axis.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueX = 1.0f;
            ///    public float hSliderValueY = 1.0f;
            ///    public float hSliderValueZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var sizeBySpeed = ps.sizeBySpeed;
            ///        sizeBySpeed.enabled = true;
            ///        sizeBySpeed.separateAxes = true;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        ParticleSystem.MinMaxCurve minMaxCurve = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///
            ///        sizeBySpeed.x = minMaxCurve;
            ///        sizeBySpeed.y = minMaxCurve;
            ///        sizeBySpeed.z = minMaxCurve;
            ///        sizeBySpeed.range = new Vector2(1.0f, 10.0f);
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, making it easier to see the size
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var sizeBySpeed = ps.sizeBySpeed;
            ///        sizeBySpeed.xMultiplier = hSliderValueX;
            ///        sizeBySpeed.yMultiplier = hSliderValueY;
            ///        sizeBySpeed.zMultiplier = hSliderValueZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Z");
            ///
            ///        hSliderValueX = GUI.HorizontalSlider(new Rect(55, 45, 100, 30), hSliderValueX, 1.0f, 5.0f);
            ///        hSliderValueY = GUI.HorizontalSlider(new Rect(55, 85, 100, 30), hSliderValueY, 1.0f, 5.0f);
            ///        hSliderValueZ = GUI.HorizontalSlider(new Rect(55, 125, 100, 30), hSliderValueZ, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve x { get => xBlittable; set => xBlittable = value; }
            [NativeName("X")] private extern MinMaxCurveBlittable xBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Size multiplier along the x-axis.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall size multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueX = 1.0f;
            ///    public float hSliderValueY = 1.0f;
            ///    public float hSliderValueZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var sizeBySpeed = ps.sizeBySpeed;
            ///        sizeBySpeed.enabled = true;
            ///        sizeBySpeed.separateAxes = true;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        ParticleSystem.MinMaxCurve minMaxCurve = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///
            ///        sizeBySpeed.x = minMaxCurve;
            ///        sizeBySpeed.y = minMaxCurve;
            ///        sizeBySpeed.z = minMaxCurve;
            ///        sizeBySpeed.range = new Vector2(1.0f, 10.0f);
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, making it easier to see the size
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var sizeBySpeed = ps.sizeBySpeed;
            ///        sizeBySpeed.xMultiplier = hSliderValueX;
            ///        sizeBySpeed.yMultiplier = hSliderValueY;
            ///        sizeBySpeed.zMultiplier = hSliderValueZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Z");
            ///
            ///        hSliderValueX = GUI.HorizontalSlider(new Rect(55, 45, 100, 30), hSliderValueX, 1.0f, 5.0f);
            ///        hSliderValueY = GUI.HorizontalSlider(new Rect(55, 85, 100, 30), hSliderValueY, 1.0f, 5.0f);
            ///        hSliderValueZ = GUI.HorizontalSlider(new Rect(55, 125, 100, 30), hSliderValueZ, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float xMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Size by speed curve for the y-axis.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueX = 1.0f;
            ///    public float hSliderValueY = 1.0f;
            ///    public float hSliderValueZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var sizeBySpeed = ps.sizeBySpeed;
            ///        sizeBySpeed.enabled = true;
            ///        sizeBySpeed.separateAxes = true;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        ParticleSystem.MinMaxCurve minMaxCurve = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///
            ///        sizeBySpeed.x = minMaxCurve;
            ///        sizeBySpeed.y = minMaxCurve;
            ///        sizeBySpeed.z = minMaxCurve;
            ///        sizeBySpeed.range = new Vector2(1.0f, 10.0f);
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, making it easier to see the size
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var sizeBySpeed = ps.sizeBySpeed;
            ///        sizeBySpeed.xMultiplier = hSliderValueX;
            ///        sizeBySpeed.yMultiplier = hSliderValueY;
            ///        sizeBySpeed.zMultiplier = hSliderValueZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Z");
            ///
            ///        hSliderValueX = GUI.HorizontalSlider(new Rect(55, 45, 100, 30), hSliderValueX, 1.0f, 5.0f);
            ///        hSliderValueY = GUI.HorizontalSlider(new Rect(55, 85, 100, 30), hSliderValueY, 1.0f, 5.0f);
            ///        hSliderValueZ = GUI.HorizontalSlider(new Rect(55, 125, 100, 30), hSliderValueZ, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve y { get => yBlittable; set => yBlittable = value; }
            [NativeName("Y")] private extern MinMaxCurveBlittable yBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Size multiplier along the y-axis.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall size multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueX = 1.0f;
            ///    public float hSliderValueY = 1.0f;
            ///    public float hSliderValueZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var sizeBySpeed = ps.sizeBySpeed;
            ///        sizeBySpeed.enabled = true;
            ///        sizeBySpeed.separateAxes = true;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        ParticleSystem.MinMaxCurve minMaxCurve = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///
            ///        sizeBySpeed.x = minMaxCurve;
            ///        sizeBySpeed.y = minMaxCurve;
            ///        sizeBySpeed.z = minMaxCurve;
            ///        sizeBySpeed.range = new Vector2(1.0f, 10.0f);
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, making it easier to see the size
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var sizeBySpeed = ps.sizeBySpeed;
            ///        sizeBySpeed.xMultiplier = hSliderValueX;
            ///        sizeBySpeed.yMultiplier = hSliderValueY;
            ///        sizeBySpeed.zMultiplier = hSliderValueZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Z");
            ///
            ///        hSliderValueX = GUI.HorizontalSlider(new Rect(55, 45, 100, 30), hSliderValueX, 1.0f, 5.0f);
            ///        hSliderValueY = GUI.HorizontalSlider(new Rect(55, 85, 100, 30), hSliderValueY, 1.0f, 5.0f);
            ///        hSliderValueZ = GUI.HorizontalSlider(new Rect(55, 125, 100, 30), hSliderValueZ, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float yMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Size by speed curve for the z-axis.</summary>
            ///<remarks>This modules only uses this property for Mesh particles.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueX = 1.0f;
            ///    public float hSliderValueY = 1.0f;
            ///    public float hSliderValueZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var sizeBySpeed = ps.sizeBySpeed;
            ///        sizeBySpeed.enabled = true;
            ///        sizeBySpeed.separateAxes = true;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        ParticleSystem.MinMaxCurve minMaxCurve = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///
            ///        sizeBySpeed.x = minMaxCurve;
            ///        sizeBySpeed.y = minMaxCurve;
            ///        sizeBySpeed.z = minMaxCurve;
            ///        sizeBySpeed.range = new Vector2(1.0f, 10.0f);
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, making it easier to see the size
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var sizeBySpeed = ps.sizeBySpeed;
            ///        sizeBySpeed.xMultiplier = hSliderValueX;
            ///        sizeBySpeed.yMultiplier = hSliderValueY;
            ///        sizeBySpeed.zMultiplier = hSliderValueZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Z");
            ///
            ///        hSliderValueX = GUI.HorizontalSlider(new Rect(55, 45, 100, 30), hSliderValueX, 1.0f, 5.0f);
            ///        hSliderValueY = GUI.HorizontalSlider(new Rect(55, 85, 100, 30), hSliderValueY, 1.0f, 5.0f);
            ///        hSliderValueZ = GUI.HorizontalSlider(new Rect(55, 125, 100, 30), hSliderValueZ, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve z { get => zBlittable; set => zBlittable = value; }
            [NativeName("Z")] private extern MinMaxCurveBlittable zBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Size multiplier along the z-axis.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueX = 1.0f;
            ///    public float hSliderValueY = 1.0f;
            ///    public float hSliderValueZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var sizeBySpeed = ps.sizeBySpeed;
            ///        sizeBySpeed.enabled = true;
            ///        sizeBySpeed.separateAxes = true;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        ParticleSystem.MinMaxCurve minMaxCurve = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///
            ///        sizeBySpeed.x = minMaxCurve;
            ///        sizeBySpeed.y = minMaxCurve;
            ///        sizeBySpeed.z = minMaxCurve;
            ///        sizeBySpeed.range = new Vector2(1.0f, 10.0f);
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, making it easier to see the size
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var sizeBySpeed = ps.sizeBySpeed;
            ///        sizeBySpeed.xMultiplier = hSliderValueX;
            ///        sizeBySpeed.yMultiplier = hSliderValueY;
            ///        sizeBySpeed.zMultiplier = hSliderValueZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Z");
            ///
            ///        hSliderValueX = GUI.HorizontalSlider(new Rect(55, 45, 100, 30), hSliderValueX, 1.0f, 5.0f);
            ///        hSliderValueY = GUI.HorizontalSlider(new Rect(55, 85, 100, 30), hSliderValueY, 1.0f, 5.0f);
            ///        hSliderValueZ = GUI.HorizontalSlider(new Rect(55, 125, 100, 30), hSliderValueZ, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float zMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Set the size by speed on each axis separately.</summary>
            ///<remarks>When disabled, only the x-axis speed curve is used.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueX = 1.0f;
            ///    public float hSliderValueY = 1.0f;
            ///    public float hSliderValueZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var sizeBySpeed = ps.sizeBySpeed;
            ///        sizeBySpeed.enabled = true;
            ///        sizeBySpeed.separateAxes = true;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        ParticleSystem.MinMaxCurve minMaxCurve = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///
            ///        sizeBySpeed.x = minMaxCurve;
            ///        sizeBySpeed.y = minMaxCurve;
            ///        sizeBySpeed.z = minMaxCurve;
            ///        sizeBySpeed.range = new Vector2(1.0f, 10.0f);
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, making it easier to see the size
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var sizeBySpeed = ps.sizeBySpeed;
            ///        sizeBySpeed.xMultiplier = hSliderValueX;
            ///        sizeBySpeed.yMultiplier = hSliderValueY;
            ///        sizeBySpeed.zMultiplier = hSliderValueZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Z");
            ///
            ///        hSliderValueX = GUI.HorizontalSlider(new Rect(55, 45, 100, 30), hSliderValueX, 1.0f, 5.0f);
            ///        hSliderValueY = GUI.HorizontalSlider(new Rect(55, 85, 100, 30), hSliderValueY, 1.0f, 5.0f);
            ///        hSliderValueZ = GUI.HorizontalSlider(new Rect(55, 125, 100, 30), hSliderValueZ, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public bool separateAxes { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Set the minimum and maximum speed that this modules applies the size curve between.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var sizeBySpeed = ps.sizeBySpeed;
            ///        sizeBySpeed.enabled = true;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        sizeBySpeed.size = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///        sizeBySpeed.range = new Vector2(0.9f, 5.0f);
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, making it easier to see the size
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startSpeed = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.SizeBySpeedModule.size" />
            extern public Vector2 range { get; [NativeMethod(ThrowsException = true)] set; }
        }

        ///<summary>Script interface for the RotationOverLifetimeModule.</summary>
        ///<remarks>Rotate particles throughout their lifetime.</remarks>
        ///<seealso cref="ParticleSystem" />
        ///<seealso cref="ParticleSystem.rotationOverLifetime" />
        public partial struct RotationOverLifetimeModule
        {
            internal RotationOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            ///<summary>Specifies whether the RotationOverLifetimeModule is enabled or disabled.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var rotationOverLifetime = ps.rotationOverLifetime;
            ///        rotationOverLifetime.enabled = true;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(Mathf.PI * 2.0f, curve);
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, so we can see the rotation
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startLifetime = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.rotationOverLifetime" />
            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Rotation over lifetime curve for the x-axis.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var rotationOverLifetime = ps.rotationOverLifetime;
            ///        rotationOverLifetime.enabled = true;
            ///        rotationOverLifetime.separateAxes = true;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        rotationOverLifetime.x = new ParticleSystem.MinMaxCurve(Mathf.PI * 2.0f, curve);
            ///        rotationOverLifetime.y = 0.0f;
            ///        rotationOverLifetime.z = 0.0f;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, so we can see the rotation
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startLifetime = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve x { get => xBlittable; set => xBlittable = value; }
            [NativeName("X")] private extern MinMaxCurveBlittable xBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Rotation multiplier around the x-axis.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall rotation multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var rotationOverLifetime = ps.rotationOverLifetime;
            ///        rotationOverLifetime.enabled = true;
            ///        rotationOverLifetime.separateAxes = true;
            ///
            ///        rotationOverLifetime.xMultiplier = (Mathf.PI * 2.0f);
            ///        rotationOverLifetime.yMultiplier = 0.0f;
            ///        rotationOverLifetime.zMultiplier = 0.0f;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, so we can see the rotation
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startLifetime = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float xMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Rotation over lifetime curve for the y-axis.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var rotationOverLifetime = ps.rotationOverLifetime;
            ///        rotationOverLifetime.enabled = true;
            ///        rotationOverLifetime.separateAxes = true;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        rotationOverLifetime.x = 0.0f;
            ///        rotationOverLifetime.y = new ParticleSystem.MinMaxCurve(Mathf.PI * 2.0f, curve);
            ///        rotationOverLifetime.z = 0.0f;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, so we can see the rotation
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startLifetime = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve y { get => yBlittable; set => yBlittable = value; }
            [NativeName("Y")] private extern MinMaxCurveBlittable yBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Rotation multiplier around the y-axis.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall rotation multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var rotationOverLifetime = ps.rotationOverLifetime;
            ///        rotationOverLifetime.enabled = true;
            ///        rotationOverLifetime.separateAxes = true;
            ///
            ///        rotationOverLifetime.xMultiplier = 0.0f;
            ///        rotationOverLifetime.yMultiplier = 1.0f;
            ///        rotationOverLifetime.zMultiplier = 0.0f;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, so we can see the rotation
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startLifetime = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float yMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Rotation over lifetime curve for the z-axis.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var rotationOverLifetime = ps.rotationOverLifetime;
            ///        rotationOverLifetime.enabled = true;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(Mathf.PI * 2.0f, curve);
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, so we can see the rotation
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startLifetime = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve z { get => zBlittable; set => zBlittable = value; }
            [NativeName("Z")] private extern MinMaxCurveBlittable zBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Rotation multiplier around the z-axis.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall rotation multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var rotationOverLifetime = ps.rotationOverLifetime;
            ///        rotationOverLifetime.enabled = true;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        rotationOverLifetime.zMultiplier = (Mathf.PI * 2.0f);
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, so we can see the rotation
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startLifetime = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float zMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Set the rotation over lifetime on each axis separately.</summary>
            ///<remarks>When disabled, only the z-axis rotation curve is used.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var rotationOverLifetime = ps.rotationOverLifetime;
            ///        rotationOverLifetime.enabled = true;
            ///        rotationOverLifetime.separateAxes = true;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        rotationOverLifetime.x = new ParticleSystem.MinMaxCurve(Mathf.PI * 2.0f, curve);
            ///        rotationOverLifetime.y = 0.0f;
            ///        rotationOverLifetime.z = 0.0f;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, so we can see the rotation
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startLifetime = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public bool separateAxes { get; [NativeMethod(ThrowsException = true)] set; }
        }

        ///<summary>Script interface for the RotationBySpeedModule.</summary>
        ///<remarks>Rotate particles based on their speed.</remarks>
        ///<seealso cref="ParticleSystem" />
        ///<seealso cref="ParticleSystem.rotationBySpeed" />
        public partial struct RotationBySpeedModule
        {
            internal RotationBySpeedModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            ///<summary>ESpecifies whether the RotationBySpeedModule is enabled or disabled.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var rotationBySpeed = ps.rotationBySpeed;
            ///        rotationBySpeed.enabled = true;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        rotationBySpeed.z = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///        rotationBySpeed.range = new Vector2(1.0f, 5.0f);
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, so we can see the rotation
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startSpeed = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.rotationBySpeed" />
            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Rotation by speed curve for the x-axis.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var rotationBySpeed = ps.rotationBySpeed;
            ///        rotationBySpeed.enabled = true;
            ///        rotationBySpeed.separateAxes = true;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        rotationBySpeed.x = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///        rotationBySpeed.y = 0.0f;
            ///        rotationBySpeed.z = 0.0f;
            ///        rotationBySpeed.range = new Vector2(1.0f, 5.0f);
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, so we can see the rotation
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startSpeed = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve x { get => xBlittable; set => xBlittable = value; }
            [NativeName("X")] private extern MinMaxCurveBlittable xBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Speed multiplier along the x-axis.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall speed multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var rotationBySpeed = ps.rotationBySpeed;
            ///        rotationBySpeed.enabled = true;
            ///        rotationBySpeed.separateAxes = true;
            ///
            ///        rotationBySpeed.xMultiplier = 1.0f;
            ///        rotationBySpeed.yMultiplier = 0.0f;
            ///        rotationBySpeed.zMultiplier = 0.0f;
            ///        rotationBySpeed.range = new Vector2(1.0f, 5.0f);
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, so we can see the rotation
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startSpeed = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float xMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Rotation by speed curve for the y-axis.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var rotationBySpeed = ps.rotationBySpeed;
            ///        rotationBySpeed.enabled = true;
            ///        rotationBySpeed.separateAxes = true;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        rotationBySpeed.x = 0.0f;
            ///        rotationBySpeed.y = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///        rotationBySpeed.z = 0.0f;
            ///        rotationBySpeed.range = new Vector2(1.0f, 5.0f);
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, so we can see the rotation
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startSpeed = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve y { get => yBlittable; set => yBlittable = value; }
            [NativeName("Y")] private extern MinMaxCurveBlittable yBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Speed multiplier along the y-axis.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall speed multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var rotationBySpeed = ps.rotationBySpeed;
            ///        rotationBySpeed.enabled = true;
            ///        rotationBySpeed.separateAxes = true;
            ///
            ///        rotationBySpeed.xMultiplier = 0.0f;
            ///        rotationBySpeed.yMultiplier = 1.0f;
            ///        rotationBySpeed.zMultiplier = 0.0f;
            ///        rotationBySpeed.range = new Vector2(1.0f, 5.0f);
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, so we can see the rotation
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startSpeed = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float yMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Rotation by speed curve for the z-axis.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var rotationBySpeed = ps.rotationBySpeed;
            ///        rotationBySpeed.enabled = true;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        rotationBySpeed.z = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///        rotationBySpeed.range = new Vector2(1.0f, 5.0f);
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, so we can see the rotation
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startSpeed = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve z { get => zBlittable; set => zBlittable = value; }
            [NativeName("Z")] private extern MinMaxCurveBlittable zBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Speed multiplier along the z-axis.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall speed multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var rotationBySpeed = ps.rotationBySpeed;
            ///        rotationBySpeed.enabled = true;
            ///
            ///        rotationBySpeed.zMultiplier = 1.0f;
            ///        rotationBySpeed.range = new Vector2(1.0f, 5.0f);
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, so we can see the rotation
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startSpeed = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float zMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Set the rotation by speed on each axis separately.</summary>
            ///<remarks>When disabled, only the z-axis rotation curve is used.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var rotationBySpeed = ps.rotationBySpeed;
            ///        rotationBySpeed.enabled = true;
            ///        rotationBySpeed.separateAxes = true;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        rotationBySpeed.x = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///        rotationBySpeed.y = 0.0f;
            ///        rotationBySpeed.z = 0.0f;
            ///        rotationBySpeed.range = new Vector2(1.0f, 5.0f);
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, so we can see the rotation
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startSpeed = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public bool separateAxes { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Set the minimum and maximum speeds that this module applies the rotation curve between.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var rotationBySpeed = ps.rotationBySpeed;
            ///        rotationBySpeed.enabled = true;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        rotationBySpeed.z = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///        rotationBySpeed.range = new Vector2(1.0f, 5.0f);
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, so we can see the rotation
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var main = ps.main;
            ///        main.startSpeed = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 45, 100, 30), hSliderValue, 1.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.RotationBySpeedModule.z" />
            extern public Vector2 range { get; [NativeMethod(ThrowsException = true)] set; }
        }

        ///<summary>Script interface for ExternalForcesModule.</summary>
        ///<remarks>This module makes <see cref="ParticleSystemForceField" /> and <see cref="T:UnityEngine.WindZone" /> components to affect the Particle System.</remarks>
        ///<seealso cref="ParticleSystem" />
        ///<seealso cref="ParticleSystem.externalForces" />
        public partial struct ExternalForcesModule
        {
            internal ExternalForcesModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            ///<summary>Specifies whether the ExternalForcesModule is enabled or disabled.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool moduleEnabled = true;
            ///    public float hSliderValue = 10.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        GameObject wind = new GameObject("Wind", typeof(WindZone));
            ///        wind.transform.parent = ps.transform;
            ///        wind.transform.localPosition = new Vector3(-4.0f, 0.0f, 0.0f);
            ///        wind.GetComponent<WindZone>().mode = WindZoneMode.Spherical;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var externalForces = ps.externalForces;
            ///        externalForces.enabled = moduleEnabled;
            ///        externalForces.multiplier = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        moduleEnabled = GUI.Toggle(new Rect(25, 45, 100, 30), moduleEnabled, "Enabled");
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 85, 100, 30), hSliderValue, 0.0f, 100.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.externalForces" />
            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Multiplies the magnitude of external forces affecting the particles.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall force multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool moduleEnabled = true;
            ///    public float hSliderValue = 10.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        GameObject wind = new GameObject("Wind", typeof(WindZone));
            ///        wind.transform.parent = ps.transform;
            ///        wind.transform.localPosition = new Vector3(-4.0f, 0.0f, 0.0f);
            ///        wind.GetComponent<WindZone>().mode = WindZoneMode.Spherical;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var externalForces = ps.externalForces;
            ///        externalForces.enabled = moduleEnabled;
            ///        externalForces.multiplier = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        moduleEnabled = GUI.Toggle(new Rect(25, 45, 100, 30), moduleEnabled, "Enabled");
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 85, 100, 30), hSliderValue, 0.0f, 100.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float multiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Multiplies the magnitude of applied external forces.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool moduleEnabled = true;
            ///    public float hSliderValue = 10.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        GameObject wind = new GameObject("Wind", typeof(WindZone));
            ///        wind.transform.parent = ps.transform;
            ///        wind.transform.localPosition = new Vector3(-4.0f, 0.0f, 0.0f);
            ///        wind.GetComponent<WindZone>().mode = WindZoneMode.Spherical;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var externalForces = ps.externalForces;
            ///        externalForces.enabled = moduleEnabled;
            ///        externalForces.multiplierCurve = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        moduleEnabled = GUI.Toggle(new Rect(25, 45, 100, 30), moduleEnabled, "Enabled");
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 85, 100, 30), hSliderValue, 0.0f, 100.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            public MinMaxCurve multiplierCurve { get => multiplierCurveBlittable; set => multiplierCurveBlittable = value; }
            [NativeName("MultiplierCurve")] private extern MinMaxCurveBlittable multiplierCurveBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Apply all Force Fields belonging to a matching Layer to this Particle System.</summary>
            extern public ParticleSystemGameObjectFilter influenceFilter { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Particle System Force Field Components with a matching Layer affect this Particle System.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool layerToggle;
            ///    private readonly int layer0 = 0;
            ///    private readonly int layer1 = 1;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startColor = Color.red;
            ///
            ///        var shape = ps.shape;
            ///        shape.enabled = false;
            ///
            ///        var externalForces = ps.externalForces;
            ///        externalForces.enabled = true;
            ///
            ///        var forceField1 = new GameObject("Force Field 1", typeof(ParticleSystemForceField)).GetComponent<ParticleSystemForceField>();
            ///        forceField1.transform.parent = ps.transform;
            ///        forceField1.transform.localPosition = new Vector3(-3.0f, 0.0f, 3.0f);
            ///        forceField1.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 180.0f);
            ///        forceField1.transform.localScale = new Vector3(5.0f, 5.0f, 5.0f);
            ///        forceField1.gameObject.layer = layer0;
            ///
            ///        forceField1.gravity = 0.04f;
            ///        forceField1.rotationSpeed = 2.0f;
            ///        forceField1.rotationAttraction = 0.02f;
            ///
            ///        var forceField2 = new GameObject("Force Field 2", typeof(ParticleSystemForceField)).GetComponent<ParticleSystemForceField>();
            ///        forceField2.transform.parent = ps.transform;
            ///        forceField2.transform.localPosition = new Vector3(3.0f, 0.0f, 3.0f);
            ///        forceField2.transform.localRotation = Quaternion.identity;
            ///        forceField2.transform.localScale = new Vector3(5.0f, 5.0f, 5.0f);
            ///        forceField2.gameObject.layer = layer1;
            ///
            ///        forceField2.gravity = 0.04f;
            ///        forceField2.rotationSpeed = 2.0f;
            ///        forceField2.rotationAttraction = 0.02f;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var externalForces = ps.externalForces;
            ///        externalForces.influenceMask = layerToggle ? (1 << layer0) : (1 << layer1);
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        layerToggle = GUI.Toggle(new Rect(25, 40, 100, 30), layerToggle, "Toggle Layer");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="LayerMask" />
            extern public LayerMask influenceMask { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>The number of Force Fields explicitly provided to the influencers list.</summary>
            ///<remarks>When <see cref="influenceFilter" /> is set to <see cref="ParticleSystemGameObjectFilter.List" /> then only Force Fields in the influencers list affect the Particle System.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            ///public class Example : MonoBehaviour
            ///{
            ///    ParticleSystem.ExternalForcesModule externalForcesModule;
            ///
            ///    void Start()
            ///    {
            ///        // Create a default particle system
            ///        var particleSystemGameObject = new GameObject("Particle System");
            ///        var system = particleSystemGameObject.AddComponent<ParticleSystem>();
            ///
            ///        // Create a force field to influence the particle system
            ///        var forceFieldGameObject = new GameObject("Force Field");
            ///        var forceField = forceFieldGameObject.AddComponent<ParticleSystemForceField>();
            ///        forceField.endRange = 5;
            ///        forceFieldGameObject.transform.position = new Vector3(0, 0, 10);
            ///
            ///        // Add the force to the particle systems external forces influencers.
            ///        externalForcesModule = system.externalForces;
            ///        externalForcesModule.enabled = true;
            ///        externalForcesModule.influenceFilter = ParticleSystemGameObjectFilter.List;
            ///        externalForcesModule.AddInfluence(forceField);
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUILayout.Label("Particle System Influencers:");
            ///        for (int i = 0; i < externalForcesModule.influenceCount; ++i)
            ///        {
            ///            var influence = externalForcesModule.GetInfluence(i);
            ///            GUILayout.Label(i + ": " + influence.name);
            ///        }
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystemForceField" />
            [NativeMethod(ThrowsException = true)]
            extern public int influenceCount { get; }

            ///<summary>Determines whether any particles are inside the influence of a Force Field.</summary>
            ///<param name="field">The Force Field to test.</param>
            ///<returns>Whether the Force Field affects the Particle System.</returns>
            extern public bool IsAffectedBy(ParticleSystemForceField field);

            ///<summary>Adds a <see cref="ParticleSystemForceField" /> to the influencers list.</summary>
            ///<remarks>When <see cref="influenceFilter" /> is set to <see cref="ParticleSystemGameObjectFilter.List" /> then only Force Fields in the influencers list affect the Particle System.</remarks>
            ///<param name="field">The Force Field to add to the influencers list.</param>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            ///public class Example : MonoBehaviour
            ///{
            ///    ParticleSystem.ExternalForcesModule externalForcesModule;
            ///
            ///    void Start()
            ///    {
            ///        // Create a default particle system
            ///        var particleSystemGameObject = new GameObject("Particle System");
            ///        var system = particleSystemGameObject.AddComponent<ParticleSystem>();
            ///
            ///        // Create a force field to influence the particle system
            ///        var forceFieldGameObject = new GameObject("Force Field");
            ///        var forceField = forceFieldGameObject.AddComponent<ParticleSystemForceField>();
            ///        forceField.endRange = 5;
            ///        forceFieldGameObject.transform.position = new Vector3(0, 0, 10);
            ///
            ///        // Add the force to the particle systems external forces influencers.
            ///        externalForcesModule = system.externalForces;
            ///        externalForcesModule.enabled = true;
            ///        externalForcesModule.influenceFilter = ParticleSystemGameObjectFilter.List;
            ///        externalForcesModule.AddInfluence(forceField);
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUILayout.Label("Particle System Influencers:");
            ///        for (int i = 0; i < externalForcesModule.influenceCount; ++i)
            ///        {
            ///            var influence = externalForcesModule.GetInfluence(i);
            ///            GUILayout.Label(i + ": " + influence.name);
            ///        }
            ///    }
            ///}
            ///]]></code>
            ///</example>
            [NativeMethod(ThrowsException = true)]
            extern public void AddInfluence([NotNull] ParticleSystemForceField field);

            [NativeMethod(ThrowsException = true)]
            extern private void RemoveInfluenceAtIndex(int index);
            ///<summary>Removes the Force Field from the influencers list at the given index.</summary>
            ///<remarks>When <see cref="influenceFilter" /> is set to <see cref="ParticleSystemGameObjectFilter.List" /> then only Force Fields in the influencers list affect the Particle System.</remarks>
            ///<param name="index">The index to remove the chosen Force Field from.</param>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            ///public class Example : MonoBehaviour
            ///{
            ///    ParticleSystem.ExternalForcesModule externalForcesModule;
            ///
            ///    void Start()
            ///    {
            ///        // Create a default particle system
            ///        var particleSystemGameObject = new GameObject("Particle System");
            ///        var system = particleSystemGameObject.AddComponent<ParticleSystem>();
            ///
            ///        // Create a force field to influence the particle system
            ///        var forceFieldGameObject = new GameObject("Force Field");
            ///        var forceField = forceFieldGameObject.AddComponent<ParticleSystemForceField>();
            ///        forceField.endRange = 5;
            ///        forceFieldGameObject.transform.position = new Vector3(0, 0, 10);
            ///
            ///        // Add the force to the particle systems external forces influencers.
            ///        externalForcesModule = system.externalForces;
            ///        externalForcesModule.enabled = true;
            ///        externalForcesModule.influenceFilter = ParticleSystemGameObjectFilter.List;
            ///        externalForcesModule.AddInfluence(forceField);
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUILayout.Label("Particle System Influencers:");
            ///        for (int i = 0; i < externalForcesModule.influenceCount; ++i)
            ///        {
            ///            var influence = externalForcesModule.GetInfluence(i);
            ///
            ///            GUILayout.BeginHorizontal();
            ///            GUILayout.Label(i + ": " + influence.name);
            ///            if (GUILayout.Button("Remove"))
            ///            {
            ///                externalForcesModule.RemoveInfluence(i);
            ///                --i;
            ///            }
            ///            GUILayout.EndHorizontal();
            ///        }
            ///    }
            ///}
            ///]]></code>
            ///</example>
            public void RemoveInfluence(int index) { RemoveInfluenceAtIndex(index); }

            ///<summary>Removes the Force Field from the influencers list at the given index.</summary>
            ///<remarks>When <see cref="influenceFilter" /> is set to <see cref="ParticleSystemGameObjectFilter.List" /> then only Force Fields in the influencers list affect the Particle System.</remarks>
            ///<param name="field">The Force Field to remove from the list.</param>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            ///public class Example : MonoBehaviour
            ///{
            ///    ParticleSystem.ExternalForcesModule externalForcesModule;
            ///
            ///    void Start()
            ///    {
            ///        // Create a default particle system
            ///        var particleSystemGameObject = new GameObject("Particle System");
            ///        var system = particleSystemGameObject.AddComponent<ParticleSystem>();
            ///
            ///        // Create a force field to influence the particle system
            ///        var forceFieldGameObject = new GameObject("Force Field");
            ///        var forceField = forceFieldGameObject.AddComponent<ParticleSystemForceField>();
            ///        forceField.endRange = 5;
            ///        forceFieldGameObject.transform.position = new Vector3(0, 0, 10);
            ///
            ///        // Add the force to the particle systems external forces influencers.
            ///        externalForcesModule = system.externalForces;
            ///        externalForcesModule.enabled = true;
            ///        externalForcesModule.influenceFilter = ParticleSystemGameObjectFilter.List;
            ///        externalForcesModule.AddInfluence(forceField);
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUILayout.Label("Particle System Influencers:");
            ///        for (int i = 0; i < externalForcesModule.influenceCount; ++i)
            ///        {
            ///            var influence = externalForcesModule.GetInfluence(i);
            ///
            ///            GUILayout.BeginHorizontal();
            ///            GUILayout.Label(i + ": " + influence.name);
            ///            if (GUILayout.Button("Remove"))
            ///            {
            ///                externalForcesModule.RemoveInfluence(i);
            ///                --i;
            ///            }
            ///            GUILayout.EndHorizontal();
            ///        }
            ///    }
            ///}
            ///]]></code>
            ///</example>
            [NativeMethod(ThrowsException = true)]
            extern public void RemoveInfluence([NotNull] ParticleSystemForceField field);
            ///<summary>Removes every Force Field from the influencers list.</summary>
            ///<remarks>When <see cref="influenceFilter" /> is set to <see cref="ParticleSystemGameObjectFilter.List" /> then only Force Fields in the influencers list affect the Particle System.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            ///public class Example : MonoBehaviour
            ///{
            ///    ParticleSystem.ExternalForcesModule externalForcesModule;
            ///
            ///    void Start()
            ///    {
            ///        // Create a default particle system
            ///        var particleSystemGameObject = new GameObject("Particle System");
            ///        var system = particleSystemGameObject.AddComponent<ParticleSystem>();
            ///
            ///        // Create a force field to influence the particle system
            ///        var forceFieldGameObject = new GameObject("Force Field");
            ///        var forceField = forceFieldGameObject.AddComponent<ParticleSystemForceField>();
            ///        forceField.endRange = 5;
            ///        forceFieldGameObject.transform.position = new Vector3(0, 0, 10);
            ///
            ///        // Add the force to the particle systems external forces influencers.
            ///        externalForcesModule = system.externalForces;
            ///        externalForcesModule.enabled = true;
            ///        externalForcesModule.influenceFilter = ParticleSystemGameObjectFilter.List;
            ///        externalForcesModule.AddInfluence(forceField);
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        if (GUILayout.Button("Remove All"))
            ///        {
            ///            externalForcesModule.RemoveAllInfluences();
            ///        }
            ///    }
            ///}
            ///]]></code>
            ///</example>
            [NativeMethod(ThrowsException = true)]
            extern public void RemoveAllInfluences();
            ///<summary>Assigns the Force Field at the given index in the influencers list.</summary>
            ///<remarks>When <see cref="influenceFilter" /> is set to <see cref="ParticleSystemGameObjectFilter.List" /> then only Force Fields in the influencers list affect the Particle System.</remarks>
            ///<param name="index">Index to assign the Force Field.</param>
            ///<param name="field">Force Field that to assign.</param>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            ///public class Example : MonoBehaviour
            ///{
            ///    public ParticleSystem system;
            ///    public ParticleSystemForceField field1;
            ///    public ParticleSystemForceField field2;
            ///
            ///    ParticleSystem.ExternalForcesModule m_ExternalForcesModule;
            ///
            ///    void Start()
            ///    {
            ///        if (system == null)
            ///        {
            ///            Debug.LogError("Please assign a Particle System to `system`.");
            ///            enabled = false;
            ///            return;
            ///        }
            ///
            ///        if (field1 == null || field2 == null)
            ///        {
            ///            Debug.LogError("Please assign a ParticleSystemForceField to `field1` and `field2`.");
            ///            enabled = false;
            ///            return;
            ///        }
            ///
            ///        m_ExternalForcesModule = system.externalForces;
            ///        m_ExternalForcesModule.enabled = true;
            ///        m_ExternalForcesModule.influenceFilter = ParticleSystemGameObjectFilter.List;
            ///        m_ExternalForcesModule.AddInfluence(field1);
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        Debug.Assert(m_ExternalForcesModule.influenceCount == 1);
            ///        var currentForceField = m_ExternalForcesModule.GetInfluence(0);
            ///
            ///        GUILayout.BeginHorizontal();
            ///        GUILayout.Label("Influence: " + currentForceField.name);
            ///
            ///        if (GUILayout.Button("Toggle"))
            ///        {
            ///            m_ExternalForcesModule.SetInfluence(0, currentForceField == field1 ? field2 : field1);
            ///        }
            ///
            ///        GUILayout.EndHorizontal();
            ///    }
            ///}
            ///]]></code>
            ///</example>
            [NativeMethod(ThrowsException = true)]
            extern public void SetInfluence(int index, [NotNull] ParticleSystemForceField field);
            ///<summary>Gets the <see cref="ParticleSystemForceField" /> at the given index in the influencers list.</summary>
            ///<remarks>When <see cref="influenceFilter" /> is set to <see cref="ParticleSystemGameObjectFilter.List" /> then only Force Fields in the influencers list affect the Particle System.</remarks>
            ///<param name="index">The index to return the chosen Force Field from.</param>
            ///<returns>The ForceField from the list.</returns>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            ///public class Example : MonoBehaviour
            ///{
            ///    ParticleSystem.ExternalForcesModule externalForcesModule;
            ///
            ///    void Start()
            ///    {
            ///        // Create a default particle system
            ///        var particleSystemGameObject = new GameObject("Particle System");
            ///        var system = particleSystemGameObject.AddComponent<ParticleSystem>();
            ///
            ///        // Create a force field to influence the particle system
            ///        var forceFieldGameObject = new GameObject("Force Field");
            ///        var forceField = forceFieldGameObject.AddComponent<ParticleSystemForceField>();
            ///        forceField.endRange = 5;
            ///        forceFieldGameObject.transform.position = new Vector3(0, 0, 10);
            ///
            ///        // Add the force to the particle systems external forces influencers.
            ///        externalForcesModule = system.externalForces;
            ///        externalForcesModule.enabled = true;
            ///        externalForcesModule.influenceFilter = ParticleSystemGameObjectFilter.List;
            ///        externalForcesModule.AddInfluence(forceField);
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUILayout.Label("Particle System Influencers:");
            ///        for (int i = 0; i < externalForcesModule.influenceCount; ++i)
            ///        {
            ///            var influence = externalForcesModule.GetInfluence(i);
            ///            GUILayout.Label(i + ": " + influence.name);
            ///        }
            ///    }
            ///}
            ///]]></code>
            ///</example>
            [NativeMethod(ThrowsException = true)]
            extern public ParticleSystemForceField GetInfluence(int index);
        }

        ///<summary>Script interface for the NoiseModule.</summary>
        ///<remarks>The Noise Module allows you to apply turbulence to the movement of your particles. Use the low quality settings to create computationally efficient Noise, or simulate smoother, richer Noise with the higher quality settings. You can also choose to define the behavior of the Noise individually for each axis.</remarks>
        ///<seealso cref="ParticleSystem" />
        ///<seealso cref="ParticleSystem.noise" />
        public partial struct NoiseModule
        {
            internal NoiseModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            ///<summary>Specifies whether the the NoiseModule is enabled or disabled.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool moduleEnabled = true;
            ///    public float hSliderValueStrength = 1.0f;
            ///    public float hSliderValueFrequency = 1.0f;
            ///    public float hSliderValueScrollSpeed = 0.0f;
            ///    public bool damping = true;
            ///    public ParticleSystemNoiseQuality quality = ParticleSystemNoiseQuality.High;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        // Set color by speed, to demonstrate the effects of the Noise Module
            ///        var colorBySpeed = ps.colorBySpeed;
            ///        colorBySpeed.enabled = true;
            ///
            ///        Gradient gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///        );
            ///
            ///        colorBySpeed.color = new ParticleSystem.MinMaxGradient(gradient);
            ///        colorBySpeed.range = new Vector2(3.0f, 7.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var noise = ps.noise;
            ///        noise.enabled = moduleEnabled;
            ///        noise.strengthMultiplier = hSliderValueStrength;
            ///        noise.frequency = hSliderValueFrequency;
            ///        noise.scrollSpeedMultiplier = hSliderValueScrollSpeed;
            ///        noise.damping = damping;
            ///        noise.quality = quality;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        moduleEnabled = GUI.Toggle(new Rect(25, 45, 200, 30), moduleEnabled, "Enabled");
            ///
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Strength");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Frequency");
            ///        GUI.Label(new Rect(25, 160, 100, 30), "Scroll Speed");
            ///
            ///        hSliderValueStrength = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValueStrength, 0.0f, 5.0f);
            ///        hSliderValueFrequency = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueFrequency, 0.0f, 5.0f);
            ///        hSliderValueScrollSpeed = GUI.HorizontalSlider(new Rect(135, 165, 100, 30), hSliderValueScrollSpeed, 0.0f, 5.0f);
            ///
            ///        damping = GUI.Toggle(new Rect(25, 205, 200, 30), damping, "Damping");
            ///
            ///        quality = (ParticleSystemNoiseQuality)GUI.SelectionGrid(new Rect(25, 245, 300, 30), (int)quality, new GUIContent[] { new GUIContent("Low"), new GUIContent("Medium"), new GUIContent("High") }, 3);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.noise" />
            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Control the noise separately for each axis.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueStrengthX = 1.0f;
            ///    public float hSliderValueStrengthY = 1.0f;
            ///    public float hSliderValueStrengthZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var noise = ps.noise;
            ///        noise.enabled = true;
            ///        noise.separateAxes = true;
            ///
            ///        // Set color by speed, to demonstrate the effects of the Noise Module
            ///        var colorBySpeed = ps.colorBySpeed;
            ///        colorBySpeed.enabled = true;
            ///
            ///        Gradient gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///        );
            ///
            ///        colorBySpeed.color = new ParticleSystem.MinMaxGradient(gradient);
            ///        colorBySpeed.range = new Vector2(3.0f, 7.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var noise = ps.noise;
            ///        noise.strengthXMultiplier = hSliderValueStrengthX;
            ///        noise.strengthYMultiplier = hSliderValueStrengthY;
            ///        noise.strengthZMultiplier = hSliderValueStrengthZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Strength X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Strength Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Strength Z");
            ///
            ///        hSliderValueStrengthX = GUI.HorizontalSlider(new Rect(135, 45, 100, 30), hSliderValueStrengthX, 0.0f, 5.0f);
            ///        hSliderValueStrengthY = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValueStrengthY, 0.0f, 5.0f);
            ///        hSliderValueStrengthZ = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueStrengthZ, 0.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public bool separateAxes { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>How strong the overall noise effect is.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool moduleEnabled = true;
            ///    public float hSliderValueStrength = 1.0f;
            ///    public float hSliderValueFrequency = 1.0f;
            ///    public float hSliderValueScrollSpeed = 0.0f;
            ///    public bool damping = true;
            ///    public ParticleSystemNoiseQuality quality = ParticleSystemNoiseQuality.High;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        // Set color by speed, to demonstrate the effects of the Noise Module
            ///        var colorBySpeed = ps.colorBySpeed;
            ///        colorBySpeed.enabled = true;
            ///
            ///        Gradient gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///        );
            ///
            ///        colorBySpeed.color = new ParticleSystem.MinMaxGradient(gradient);
            ///        colorBySpeed.range = new Vector2(3.0f, 7.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var noise = ps.noise;
            ///        noise.enabled = moduleEnabled;
            ///        noise.strength = hSliderValueStrength;
            ///        noise.frequency = hSliderValueFrequency;
            ///        noise.scrollSpeedMultiplier = hSliderValueScrollSpeed;
            ///        noise.damping = damping;
            ///        noise.quality = quality;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        moduleEnabled = GUI.Toggle(new Rect(25, 45, 200, 30), moduleEnabled, "Enabled");
            ///
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Strength");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Frequency");
            ///        GUI.Label(new Rect(25, 160, 100, 30), "Scroll Speed");
            ///
            ///        hSliderValueStrength = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValueStrength, 0.0f, 5.0f);
            ///        hSliderValueFrequency = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueFrequency, 0.0f, 5.0f);
            ///        hSliderValueScrollSpeed = GUI.HorizontalSlider(new Rect(135, 165, 100, 30), hSliderValueScrollSpeed, 0.0f, 5.0f);
            ///
            ///        damping = GUI.Toggle(new Rect(25, 205, 200, 30), damping, "Damping");
            ///
            ///        quality = (ParticleSystemNoiseQuality)GUI.SelectionGrid(new Rect(25, 245, 300, 30), (int)quality, new GUIContent[] { new GUIContent("Low"), new GUIContent("Medium"), new GUIContent("High") }, 3);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve strength { get => strengthBlittable; set => strengthBlittable = value; }
            [NativeName("StrengthX")] private extern MinMaxCurveBlittable strengthBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Strength multiplier.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall strength multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool moduleEnabled = true;
            ///    public float hSliderValueStrength = 1.0f;
            ///    public float hSliderValueFrequency = 1.0f;
            ///    public float hSliderValueScrollSpeed = 0.0f;
            ///    public bool damping = true;
            ///    public ParticleSystemNoiseQuality quality = ParticleSystemNoiseQuality.High;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        // Set color by speed, to demonstrate the effects of the Noise Module
            ///        var colorBySpeed = ps.colorBySpeed;
            ///        colorBySpeed.enabled = true;
            ///
            ///        Gradient gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///        );
            ///
            ///        colorBySpeed.color = new ParticleSystem.MinMaxGradient(gradient);
            ///        colorBySpeed.range = new Vector2(3.0f, 7.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var noise = ps.noise;
            ///        noise.enabled = moduleEnabled;
            ///        noise.strengthMultiplier = hSliderValueStrength;
            ///        noise.frequency = hSliderValueFrequency;
            ///        noise.scrollSpeedMultiplier = hSliderValueScrollSpeed;
            ///        noise.damping = damping;
            ///        noise.quality = quality;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        moduleEnabled = GUI.Toggle(new Rect(25, 45, 200, 30), moduleEnabled, "Enabled");
            ///
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Strength");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Frequency");
            ///        GUI.Label(new Rect(25, 160, 100, 30), "Scroll Speed");
            ///
            ///        hSliderValueStrength = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValueStrength, 0.0f, 5.0f);
            ///        hSliderValueFrequency = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueFrequency, 0.0f, 5.0f);
            ///        hSliderValueScrollSpeed = GUI.HorizontalSlider(new Rect(135, 165, 100, 30), hSliderValueScrollSpeed, 0.0f, 5.0f);
            ///
            ///        damping = GUI.Toggle(new Rect(25, 205, 200, 30), damping, "Damping");
            ///
            ///        quality = (ParticleSystemNoiseQuality)GUI.SelectionGrid(new Rect(25, 245, 300, 30), (int)quality, new GUIContent[] { new GUIContent("Low"), new GUIContent("Medium"), new GUIContent("High") }, 3);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.NoiseModule.strength" />
            [NativeName("StrengthXMultiplier")]
            extern public float strengthMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Define the strength of the effect on the x-axis, when using the <see cref="ParticleSystem.NoiseModule.separateAxes" /> option.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueStrengthX = 1.0f;
            ///    public float hSliderValueStrengthY = 1.0f;
            ///    public float hSliderValueStrengthZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var noise = ps.noise;
            ///        noise.enabled = true;
            ///        noise.separateAxes = true;
            ///
            ///        // Set color by speed, to demonstrate the effects of the Noise Module
            ///        var colorBySpeed = ps.colorBySpeed;
            ///        colorBySpeed.enabled = true;
            ///
            ///        Gradient gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///        );
            ///
            ///        colorBySpeed.color = new ParticleSystem.MinMaxGradient(gradient);
            ///        colorBySpeed.range = new Vector2(3.0f, 7.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var noise = ps.noise;
            ///        noise.strengthX = hSliderValueStrengthX;
            ///        noise.strengthY = hSliderValueStrengthY;
            ///        noise.strengthZ = hSliderValueStrengthZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Strength X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Strength Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Strength Z");
            ///
            ///        hSliderValueStrengthX = GUI.HorizontalSlider(new Rect(135, 45, 100, 30), hSliderValueStrengthX, 0.0f, 5.0f);
            ///        hSliderValueStrengthY = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValueStrengthY, 0.0f, 5.0f);
            ///        hSliderValueStrengthZ = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueStrengthZ, 0.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve strengthX { get => strengthXBlittable; set => strengthXBlittable = value; }
            [NativeName("StrengthX")] private extern MinMaxCurveBlittable strengthXBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>x-axis strength multiplier.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall strength multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueStrengthX = 1.0f;
            ///    public float hSliderValueStrengthY = 1.0f;
            ///    public float hSliderValueStrengthZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var noise = ps.noise;
            ///        noise.enabled = true;
            ///        noise.separateAxes = true;
            ///
            ///        // Set color by speed, to demonstrate the effects of the Noise Module
            ///        var colorBySpeed = ps.colorBySpeed;
            ///        colorBySpeed.enabled = true;
            ///
            ///        Gradient gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///        );
            ///
            ///        colorBySpeed.color = new ParticleSystem.MinMaxGradient(gradient);
            ///        colorBySpeed.range = new Vector2(3.0f, 7.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var noise = ps.noise;
            ///        noise.strengthXMultiplier = hSliderValueStrengthX;
            ///        noise.strengthYMultiplier = hSliderValueStrengthY;
            ///        noise.strengthZMultiplier = hSliderValueStrengthZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Strength X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Strength Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Strength Z");
            ///
            ///        hSliderValueStrengthX = GUI.HorizontalSlider(new Rect(135, 45, 100, 30), hSliderValueStrengthX, 0.0f, 5.0f);
            ///        hSliderValueStrengthY = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValueStrengthY, 0.0f, 5.0f);
            ///        hSliderValueStrengthZ = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueStrengthZ, 0.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.NoiseModule.strengthX" />
            extern public float strengthXMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Define the strength of the effect on the y-axis, when using the <see cref="ParticleSystem.NoiseModule.separateAxes" /> option.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueStrengthX = 1.0f;
            ///    public float hSliderValueStrengthY = 1.0f;
            ///    public float hSliderValueStrengthZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var noise = ps.noise;
            ///        noise.enabled = true;
            ///        noise.separateAxes = true;
            ///
            ///        // Set color by speed, to demonstrate the effects of the Noise Module
            ///        var colorBySpeed = ps.colorBySpeed;
            ///        colorBySpeed.enabled = true;
            ///
            ///        Gradient gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///        );
            ///
            ///        colorBySpeed.color = new ParticleSystem.MinMaxGradient(gradient);
            ///        colorBySpeed.range = new Vector2(3.0f, 7.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var noise = ps.noise;
            ///        noise.strengthX = hSliderValueStrengthX;
            ///        noise.strengthY = hSliderValueStrengthY;
            ///        noise.strengthZ = hSliderValueStrengthZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Strength X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Strength Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Strength Z");
            ///
            ///        hSliderValueStrengthX = GUI.HorizontalSlider(new Rect(135, 45, 100, 30), hSliderValueStrengthX, 0.0f, 5.0f);
            ///        hSliderValueStrengthY = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValueStrengthY, 0.0f, 5.0f);
            ///        hSliderValueStrengthZ = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueStrengthZ, 0.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve strengthY { get => strengthYBlittable; set => strengthYBlittable = value; }
            [NativeName("StrengthY")] private extern MinMaxCurveBlittable strengthYBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>y-axis strength multiplier.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall strength multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueStrengthX = 1.0f;
            ///    public float hSliderValueStrengthY = 1.0f;
            ///    public float hSliderValueStrengthZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var noise = ps.noise;
            ///        noise.enabled = true;
            ///        noise.separateAxes = true;
            ///
            ///        // Set color by speed, to demonstrate the effects of the Noise Module
            ///        var colorBySpeed = ps.colorBySpeed;
            ///        colorBySpeed.enabled = true;
            ///
            ///        Gradient gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///        );
            ///
            ///        colorBySpeed.color = new ParticleSystem.MinMaxGradient(gradient);
            ///        colorBySpeed.range = new Vector2(3.0f, 7.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var noise = ps.noise;
            ///        noise.strengthXMultiplier = hSliderValueStrengthX;
            ///        noise.strengthYMultiplier = hSliderValueStrengthY;
            ///        noise.strengthZMultiplier = hSliderValueStrengthZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Strength X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Strength Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Strength Z");
            ///
            ///        hSliderValueStrengthX = GUI.HorizontalSlider(new Rect(135, 45, 100, 30), hSliderValueStrengthX, 0.0f, 5.0f);
            ///        hSliderValueStrengthY = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValueStrengthY, 0.0f, 5.0f);
            ///        hSliderValueStrengthZ = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueStrengthZ, 0.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.NoiseModule.strengthY" />
            extern public float strengthYMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Define the strength of the effect on the z-axis, when using the <see cref="ParticleSystem.NoiseModule.separateAxes" /> option.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueStrengthX = 1.0f;
            ///    public float hSliderValueStrengthY = 1.0f;
            ///    public float hSliderValueStrengthZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var noise = ps.noise;
            ///        noise.enabled = true;
            ///        noise.separateAxes = true;
            ///
            ///        // Set color by speed, to demonstrate the effects of the Noise Module
            ///        var colorBySpeed = ps.colorBySpeed;
            ///        colorBySpeed.enabled = true;
            ///
            ///        Gradient gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///        );
            ///
            ///        colorBySpeed.color = new ParticleSystem.MinMaxGradient(gradient);
            ///        colorBySpeed.range = new Vector2(3.0f, 7.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var noise = ps.noise;
            ///        noise.strengthX = hSliderValueStrengthX;
            ///        noise.strengthY = hSliderValueStrengthY;
            ///        noise.strengthZ = hSliderValueStrengthZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Strength X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Strength Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Strength Z");
            ///
            ///        hSliderValueStrengthX = GUI.HorizontalSlider(new Rect(135, 45, 100, 30), hSliderValueStrengthX, 0.0f, 5.0f);
            ///        hSliderValueStrengthY = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValueStrengthY, 0.0f, 5.0f);
            ///        hSliderValueStrengthZ = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueStrengthZ, 0.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve strengthZ { get => strengthZBlittable; set => strengthZBlittable = value; }
            [NativeName("StrengthZ")] private extern MinMaxCurveBlittable strengthZBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>z-axis strength multiplier.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall strength multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueStrengthX = 1.0f;
            ///    public float hSliderValueStrengthY = 1.0f;
            ///    public float hSliderValueStrengthZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var noise = ps.noise;
            ///        noise.enabled = true;
            ///        noise.separateAxes = true;
            ///
            ///        // Set color by speed, to demonstrate the effects of the Noise Module
            ///        var colorBySpeed = ps.colorBySpeed;
            ///        colorBySpeed.enabled = true;
            ///
            ///        Gradient gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///        );
            ///
            ///        colorBySpeed.color = new ParticleSystem.MinMaxGradient(gradient);
            ///        colorBySpeed.range = new Vector2(3.0f, 7.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var noise = ps.noise;
            ///        noise.strengthXMultiplier = hSliderValueStrengthX;
            ///        noise.strengthYMultiplier = hSliderValueStrengthY;
            ///        noise.strengthZMultiplier = hSliderValueStrengthZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Strength X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Strength Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Strength Z");
            ///
            ///        hSliderValueStrengthX = GUI.HorizontalSlider(new Rect(135, 45, 100, 30), hSliderValueStrengthX, 0.0f, 5.0f);
            ///        hSliderValueStrengthY = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValueStrengthY, 0.0f, 5.0f);
            ///        hSliderValueStrengthZ = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueStrengthZ, 0.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.NoiseModule.strengthZ" />
            extern public float strengthZMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Low values create soft, smooth noise, and high values create rapidly changing noise.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool moduleEnabled = true;
            ///    public float hSliderValueStrength = 1.0f;
            ///    public float hSliderValueFrequency = 1.0f;
            ///    public float hSliderValueScrollSpeed = 0.0f;
            ///    public bool damping = true;
            ///    public ParticleSystemNoiseQuality quality = ParticleSystemNoiseQuality.High;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        // Set color by speed, to demonstrate the effects of the Noise Module
            ///        var colorBySpeed = ps.colorBySpeed;
            ///        colorBySpeed.enabled = true;
            ///
            ///        Gradient gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///        );
            ///
            ///        colorBySpeed.color = new ParticleSystem.MinMaxGradient(gradient);
            ///        colorBySpeed.range = new Vector2(3.0f, 7.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var noise = ps.noise;
            ///        noise.enabled = moduleEnabled;
            ///        noise.strengthMultiplier = hSliderValueStrength;
            ///        noise.frequency = hSliderValueFrequency;
            ///        noise.scrollSpeedMultiplier = hSliderValueScrollSpeed;
            ///        noise.damping = damping;
            ///        noise.quality = quality;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        moduleEnabled = GUI.Toggle(new Rect(25, 45, 200, 30), moduleEnabled, "Enabled");
            ///
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Strength");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Frequency");
            ///        GUI.Label(new Rect(25, 160, 100, 30), "Scroll Speed");
            ///
            ///        hSliderValueStrength = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValueStrength, 0.0f, 5.0f);
            ///        hSliderValueFrequency = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueFrequency, 0.0f, 5.0f);
            ///        hSliderValueScrollSpeed = GUI.HorizontalSlider(new Rect(135, 165, 100, 30), hSliderValueScrollSpeed, 0.0f, 5.0f);
            ///
            ///        damping = GUI.Toggle(new Rect(25, 205, 200, 30), damping, "Damping");
            ///
            ///        quality = (ParticleSystemNoiseQuality)GUI.SelectionGrid(new Rect(25, 245, 300, 30), (int)quality, new GUIContent[] { new GUIContent("Low"), new GUIContent("Medium"), new GUIContent("High") }, 3);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float frequency { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Higher frequency noise reduces the strength by a proportional amount, if enabled.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool moduleEnabled = true;
            ///    public float hSliderValueStrength = 1.0f;
            ///    public float hSliderValueFrequency = 1.0f;
            ///    public float hSliderValueScrollSpeed = 0.0f;
            ///    public bool damping = true;
            ///    public ParticleSystemNoiseQuality quality = ParticleSystemNoiseQuality.High;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        // Set color by speed, to demonstrate the effects of the Noise Module
            ///        var colorBySpeed = ps.colorBySpeed;
            ///        colorBySpeed.enabled = true;
            ///
            ///        Gradient gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///        );
            ///
            ///        colorBySpeed.color = new ParticleSystem.MinMaxGradient(gradient);
            ///        colorBySpeed.range = new Vector2(3.0f, 7.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var noise = ps.noise;
            ///        noise.enabled = moduleEnabled;
            ///        noise.strengthMultiplier = hSliderValueStrength;
            ///        noise.frequency = hSliderValueFrequency;
            ///        noise.scrollSpeedMultiplier = hSliderValueScrollSpeed;
            ///        noise.damping = damping;
            ///        noise.quality = quality;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        moduleEnabled = GUI.Toggle(new Rect(25, 45, 200, 30), moduleEnabled, "Enabled");
            ///
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Strength");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Frequency");
            ///        GUI.Label(new Rect(25, 160, 100, 30), "Scroll Speed");
            ///
            ///        hSliderValueStrength = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValueStrength, 0.0f, 5.0f);
            ///        hSliderValueFrequency = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueFrequency, 0.0f, 5.0f);
            ///        hSliderValueScrollSpeed = GUI.HorizontalSlider(new Rect(135, 165, 100, 30), hSliderValueScrollSpeed, 0.0f, 5.0f);
            ///
            ///        damping = GUI.Toggle(new Rect(25, 205, 200, 30), damping, "Damping");
            ///
            ///        quality = (ParticleSystemNoiseQuality)GUI.SelectionGrid(new Rect(25, 245, 300, 30), (int)quality, new GUIContent[] { new GUIContent("Low"), new GUIContent("Medium"), new GUIContent("High") }, 3);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public bool damping { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Layers of noise that combine to produce final noise.</summary>
            ///<remarks>Please note that adding octaves substantially decreases performance.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueOctaves = 1.0f;
            ///    public float hSliderValueOctaveMultiplier = 0.5f;
            ///    public float hSliderValueOctaveScale = 2.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var noise = ps.noise;
            ///        noise.enabled = true;
            ///
            ///        // Set color by speed, to demonstrate the effects of the Noise Module
            ///        var colorBySpeed = ps.colorBySpeed;
            ///        colorBySpeed.enabled = true;
            ///
            ///        Gradient gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///        );
            ///
            ///        colorBySpeed.color = new ParticleSystem.MinMaxGradient(gradient);
            ///        colorBySpeed.range = new Vector2(3.0f, 7.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var noise = ps.noise;
            ///        noise.octaveCount = (int)hSliderValueOctaves;
            ///        noise.octaveMultiplier = hSliderValueOctaveMultiplier;
            ///        noise.octaveScale = hSliderValueOctaveScale;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Octave Count");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Octave Multiplier");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Octave Scale");
            ///
            ///        hSliderValueOctaves = GUI.HorizontalSlider(new Rect(135, 45, 100, 30), hSliderValueOctaves, 1.0f, 4.0f);
            ///        hSliderValueOctaveMultiplier = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValueOctaveMultiplier, 0.0f, 1.0f);
            ///        hSliderValueOctaveScale = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueOctaveScale, 1.0f, 4.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public int octaveCount { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>When combining each octave, scale the intensity by this amount.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueOctaves = 1.0f;
            ///    public float hSliderValueOctaveMultiplier = 0.5f;
            ///    public float hSliderValueOctaveScale = 2.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var noise = ps.noise;
            ///        noise.enabled = true;
            ///
            ///        // Set color by speed, to demonstrate the effects of the Noise Module
            ///        var colorBySpeed = ps.colorBySpeed;
            ///        colorBySpeed.enabled = true;
            ///
            ///        Gradient gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///        );
            ///
            ///        colorBySpeed.color = new ParticleSystem.MinMaxGradient(gradient);
            ///        colorBySpeed.range = new Vector2(3.0f, 7.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var noise = ps.noise;
            ///        noise.octaveCount = (int)hSliderValueOctaves;
            ///        noise.octaveMultiplier = hSliderValueOctaveMultiplier;
            ///        noise.octaveScale = hSliderValueOctaveScale;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Octave Count");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Octave Multiplier");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Octave Scale");
            ///
            ///        hSliderValueOctaves = GUI.HorizontalSlider(new Rect(135, 45, 100, 30), hSliderValueOctaves, 1.0f, 4.0f);
            ///        hSliderValueOctaveMultiplier = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValueOctaveMultiplier, 0.0f, 1.0f);
            ///        hSliderValueOctaveScale = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueOctaveScale, 1.0f, 4.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float octaveMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>When combining each octave, zoom in by this amount.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueOctaves = 1.0f;
            ///    public float hSliderValueOctaveMultiplier = 0.5f;
            ///    public float hSliderValueOctaveScale = 2.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var noise = ps.noise;
            ///        noise.enabled = true;
            ///
            ///        // Set color by speed, to demonstrate the effects of the Noise Module
            ///        var colorBySpeed = ps.colorBySpeed;
            ///        colorBySpeed.enabled = true;
            ///
            ///        Gradient gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///        );
            ///
            ///        colorBySpeed.color = new ParticleSystem.MinMaxGradient(gradient);
            ///        colorBySpeed.range = new Vector2(3.0f, 7.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var noise = ps.noise;
            ///        noise.octaveCount = (int)hSliderValueOctaves;
            ///        noise.octaveMultiplier = hSliderValueOctaveMultiplier;
            ///        noise.octaveScale = hSliderValueOctaveScale;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Octave Count");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Octave Multiplier");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Octave Scale");
            ///
            ///        hSliderValueOctaves = GUI.HorizontalSlider(new Rect(135, 45, 100, 30), hSliderValueOctaves, 1.0f, 4.0f);
            ///        hSliderValueOctaveMultiplier = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValueOctaveMultiplier, 0.0f, 1.0f);
            ///        hSliderValueOctaveScale = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueOctaveScale, 1.0f, 4.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float octaveScale { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Generate 1D, 2D or 3D noise.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool moduleEnabled = true;
            ///    public float hSliderValueStrength = 1.0f;
            ///    public float hSliderValueFrequency = 1.0f;
            ///    public float hSliderValueScrollSpeed = 0.0f;
            ///    public bool damping = true;
            ///    public ParticleSystemNoiseQuality quality = ParticleSystemNoiseQuality.High;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        // Set color by speed, to demonstrate the effects of the Noise Module
            ///        var colorBySpeed = ps.colorBySpeed;
            ///        colorBySpeed.enabled = true;
            ///
            ///        Gradient gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///        );
            ///
            ///        colorBySpeed.color = new ParticleSystem.MinMaxGradient(gradient);
            ///        colorBySpeed.range = new Vector2(3.0f, 7.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var noise = ps.noise;
            ///        noise.enabled = moduleEnabled;
            ///        noise.strengthMultiplier = hSliderValueStrength;
            ///        noise.frequency = hSliderValueFrequency;
            ///        noise.scrollSpeedMultiplier = hSliderValueScrollSpeed;
            ///        noise.damping = damping;
            ///        noise.quality = quality;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        moduleEnabled = GUI.Toggle(new Rect(25, 45, 200, 30), moduleEnabled, "Enabled");
            ///
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Strength");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Frequency");
            ///        GUI.Label(new Rect(25, 160, 100, 30), "Scroll Speed");
            ///
            ///        hSliderValueStrength = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValueStrength, 0.0f, 5.0f);
            ///        hSliderValueFrequency = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueFrequency, 0.0f, 5.0f);
            ///        hSliderValueScrollSpeed = GUI.HorizontalSlider(new Rect(135, 165, 100, 30), hSliderValueScrollSpeed, 0.0f, 5.0f);
            ///
            ///        damping = GUI.Toggle(new Rect(25, 205, 200, 30), damping, "Damping");
            ///
            ///        quality = (ParticleSystemNoiseQuality)GUI.SelectionGrid(new Rect(25, 245, 300, 30), (int)quality, new GUIContent[] { new GUIContent("Low"), new GUIContent("Medium"), new GUIContent("High") }, 3);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public ParticleSystemNoiseQuality quality { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Scroll the noise map over the Particle System.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool moduleEnabled = true;
            ///    public float hSliderValueStrength = 1.0f;
            ///    public float hSliderValueFrequency = 1.0f;
            ///    public float hSliderValueScrollSpeed = 0.0f;
            ///    public bool damping = true;
            ///    public ParticleSystemNoiseQuality quality = ParticleSystemNoiseQuality.High;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        // Set color by speed, to demonstrate the effects of the Noise Module
            ///        var colorBySpeed = ps.colorBySpeed;
            ///        colorBySpeed.enabled = true;
            ///
            ///        Gradient gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///        );
            ///
            ///        colorBySpeed.color = new ParticleSystem.MinMaxGradient(gradient);
            ///        colorBySpeed.range = new Vector2(3.0f, 7.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var noise = ps.noise;
            ///        noise.enabled = moduleEnabled;
            ///        noise.strengthMultiplier = hSliderValueStrength;
            ///        noise.frequency = hSliderValueFrequency;
            ///        noise.scrollSpeed = hSliderValueScrollSpeed;
            ///        noise.damping = damping;
            ///        noise.quality = quality;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        moduleEnabled = GUI.Toggle(new Rect(25, 45, 200, 30), moduleEnabled, "Enabled");
            ///
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Strength");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Frequency");
            ///        GUI.Label(new Rect(25, 160, 100, 30), "Scroll Speed");
            ///
            ///        hSliderValueStrength = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValueStrength, 0.0f, 5.0f);
            ///        hSliderValueFrequency = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueFrequency, 0.0f, 5.0f);
            ///        hSliderValueScrollSpeed = GUI.HorizontalSlider(new Rect(135, 165, 100, 30), hSliderValueScrollSpeed, 0.0f, 5.0f);
            ///
            ///        damping = GUI.Toggle(new Rect(25, 205, 200, 30), damping, "Damping");
            ///
            ///        quality = (ParticleSystemNoiseQuality)GUI.SelectionGrid(new Rect(25, 245, 300, 30), (int)quality, new GUIContent[] { new GUIContent("Low"), new GUIContent("Medium"), new GUIContent("High") }, 3);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve scrollSpeed { get => scrollSpeedBlittable; set => scrollSpeedBlittable = value; }
            [NativeName("ScrollSpeed")] private extern MinMaxCurveBlittable scrollSpeedBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Scroll speed multiplier.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall scroll speed multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool moduleEnabled = true;
            ///    public float hSliderValueStrength = 1.0f;
            ///    public float hSliderValueFrequency = 1.0f;
            ///    public float hSliderValueScrollSpeed = 0.0f;
            ///    public bool damping = true;
            ///    public ParticleSystemNoiseQuality quality = ParticleSystemNoiseQuality.High;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        // Set color by speed, to demonstrate the effects of the Noise Module
            ///        var colorBySpeed = ps.colorBySpeed;
            ///        colorBySpeed.enabled = true;
            ///
            ///        Gradient gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///        );
            ///
            ///        colorBySpeed.color = new ParticleSystem.MinMaxGradient(gradient);
            ///        colorBySpeed.range = new Vector2(3.0f, 7.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var noise = ps.noise;
            ///        noise.enabled = moduleEnabled;
            ///        noise.strengthMultiplier = hSliderValueStrength;
            ///        noise.frequency = hSliderValueFrequency;
            ///        noise.scrollSpeedMultiplier = hSliderValueScrollSpeed;
            ///        noise.damping = damping;
            ///        noise.quality = quality;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        moduleEnabled = GUI.Toggle(new Rect(25, 45, 200, 30), moduleEnabled, "Enabled");
            ///
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Strength");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Frequency");
            ///        GUI.Label(new Rect(25, 160, 100, 30), "Scroll Speed");
            ///
            ///        hSliderValueStrength = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValueStrength, 0.0f, 5.0f);
            ///        hSliderValueFrequency = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueFrequency, 0.0f, 5.0f);
            ///        hSliderValueScrollSpeed = GUI.HorizontalSlider(new Rect(135, 165, 100, 30), hSliderValueScrollSpeed, 0.0f, 5.0f);
            ///
            ///        damping = GUI.Toggle(new Rect(25, 205, 200, 30), damping, "Damping");
            ///
            ///        quality = (ParticleSystemNoiseQuality)GUI.SelectionGrid(new Rect(25, 245, 300, 30), (int)quality, new GUIContent[] { new GUIContent("Low"), new GUIContent("Medium"), new GUIContent("High") }, 3);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.NoiseModule.scrollSpeed" />
            extern public float scrollSpeedMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Enable remapping of the final noise values, allowing for noise values to be translated into different values.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueRemap = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var noise = ps.noise;
            ///        noise.enabled = true;
            ///        noise.remapEnabled = true;
            ///
            ///        // An unusual curve to show off different noise behavior (See curve preview in the Inspector)
            ///        AnimationCurve ourCurve;
            ///        ourCurve = new AnimationCurve();
            ///        ourCurve.AddKey(0.0f, 0.0f);
            ///        ourCurve.AddKey(0.45f, -0.75f);
            ///        ourCurve.AddKey(0.50f, 1.0f);
            ///        ourCurve.AddKey(0.55f, -0.75f);
            ///        ourCurve.AddKey(1.0f, 0.0f);
            ///        noise.remap = new ParticleSystem.MinMaxCurve(1.0f, ourCurve);
            ///
            ///        // Set color by speed, to demonstrate the effects of the Noise Module
            ///        var colorBySpeed = ps.colorBySpeed;
            ///        colorBySpeed.enabled = true;
            ///
            ///        Gradient gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///        );
            ///
            ///        colorBySpeed.color = new ParticleSystem.MinMaxGradient(gradient);
            ///        colorBySpeed.range = new Vector2(3.0f, 7.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var noise = ps.noise;
            ///        noise.remapMultiplier = hSliderValueRemap;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Remap");
            ///
            ///        hSliderValueRemap = GUI.HorizontalSlider(new Rect(135, 45, 100, 30), hSliderValueRemap, 0.0f, 1.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public bool remapEnabled { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Define how the noise values are remapped.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueRemap = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var noise = ps.noise;
            ///        noise.enabled = true;
            ///        noise.remapEnabled = true;
            ///
            ///        // An unusual curve to show off different noise behavior (See curve preview in the Inspector)
            ///        AnimationCurve ourCurve;
            ///        ourCurve = new AnimationCurve();
            ///        ourCurve.AddKey(0.0f, 0.0f);
            ///        ourCurve.AddKey(0.45f, -0.75f);
            ///        ourCurve.AddKey(0.50f, 1.0f);
            ///        ourCurve.AddKey(0.55f, -0.75f);
            ///        ourCurve.AddKey(1.0f, 0.0f);
            ///        noise.remap = new ParticleSystem.MinMaxCurve(1.0f, ourCurve);
            ///
            ///        // Set color by speed, to demonstrate the effects of the Noise Module
            ///        var colorBySpeed = ps.colorBySpeed;
            ///        colorBySpeed.enabled = true;
            ///
            ///        Gradient gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///        );
            ///
            ///        colorBySpeed.color = new ParticleSystem.MinMaxGradient(gradient);
            ///        colorBySpeed.range = new Vector2(3.0f, 7.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var noise = ps.noise;
            ///        noise.remapMultiplier = hSliderValueRemap;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Remap");
            ///
            ///        hSliderValueRemap = GUI.HorizontalSlider(new Rect(135, 45, 100, 30), hSliderValueRemap, 0.0f, 1.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve remap { get => remapBlittable; set => remapBlittable = value; }
            [NativeName("RemapX")] private extern MinMaxCurveBlittable remapBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Remap multiplier.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall remap multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueRemap = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var noise = ps.noise;
            ///        noise.enabled = true;
            ///        noise.remapEnabled = true;
            ///
            ///        // An unusual curve to show off different noise behavior (See curve preview in the Inspector)
            ///        AnimationCurve ourCurve;
            ///        ourCurve = new AnimationCurve();
            ///        ourCurve.AddKey(0.0f, 0.0f);
            ///        ourCurve.AddKey(0.45f, -0.75f);
            ///        ourCurve.AddKey(0.50f, 1.0f);
            ///        ourCurve.AddKey(0.55f, -0.75f);
            ///        ourCurve.AddKey(1.0f, 0.0f);
            ///        noise.remap = new ParticleSystem.MinMaxCurve(1.0f, ourCurve);
            ///
            ///        // Set color by speed, to demonstrate the effects of the Noise Module
            ///        var colorBySpeed = ps.colorBySpeed;
            ///        colorBySpeed.enabled = true;
            ///
            ///        Gradient gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///        );
            ///
            ///        colorBySpeed.color = new ParticleSystem.MinMaxGradient(gradient);
            ///        colorBySpeed.range = new Vector2(3.0f, 7.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var noise = ps.noise;
            ///        noise.remapMultiplier = hSliderValueRemap;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Remap");
            ///
            ///        hSliderValueRemap = GUI.HorizontalSlider(new Rect(135, 45, 100, 30), hSliderValueRemap, 0.0f, 1.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.NoiseModule.remap" />
            [NativeName("RemapXMultiplier")]
            extern public float remapMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Define how the noise values are remapped on the x-axis, when using the <see cref="ParticleSystem.NoiseModule.separateAxes" /> option.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueRemapX = 1.0f;
            ///    public float hSliderValueRemapY = 1.0f;
            ///    public float hSliderValueRemapZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var noise = ps.noise;
            ///        noise.enabled = true;
            ///        noise.remapEnabled = true;
            ///        noise.separateAxes = true;
            ///
            ///        // An unusual curve to show off different noise behavior (See curve preview in the Inspector)
            ///        AnimationCurve ourCurve;
            ///        ourCurve = new AnimationCurve();
            ///        ourCurve.AddKey(0.0f, 0.0f);
            ///        ourCurve.AddKey(0.45f, -0.75f);
            ///        ourCurve.AddKey(0.50f, 1.0f);
            ///        ourCurve.AddKey(0.55f, -0.75f);
            ///        ourCurve.AddKey(1.0f, 0.0f);
            ///
            ///        // Apply the curve
            ///        noise.remapX = new ParticleSystem.MinMaxCurve(1.0f, ourCurve);
            ///        noise.remapY = new ParticleSystem.MinMaxCurve(1.0f, ourCurve);
            ///        noise.remapZ = new ParticleSystem.MinMaxCurve(1.0f, ourCurve);
            ///
            ///        // Set color by speed, to demonstrate the effects of the Noise Module
            ///        var colorBySpeed = ps.colorBySpeed;
            ///        colorBySpeed.enabled = true;
            ///
            ///        Gradient gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///        );
            ///
            ///        colorBySpeed.color = new ParticleSystem.MinMaxGradient(gradient);
            ///        colorBySpeed.range = new Vector2(3.0f, 7.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var noise = ps.noise;
            ///        noise.remapXMultiplier = hSliderValueRemapX;
            ///        noise.remapYMultiplier = hSliderValueRemapY;
            ///        noise.remapZMultiplier = hSliderValueRemapZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Remap X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Remap Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Remap Z");
            ///
            ///        hSliderValueRemapX = GUI.HorizontalSlider(new Rect(135, 45, 100, 30), hSliderValueRemapX, 0.0f, 5.0f);
            ///        hSliderValueRemapY = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValueRemapY, 0.0f, 5.0f);
            ///        hSliderValueRemapZ = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueRemapZ, 0.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve remapX { get => remapXBlittable; set => remapXBlittable = value; }
            [NativeName("RemapX")] private extern MinMaxCurveBlittable remapXBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>x-axis remap multiplier.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall remap multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueRemapX = 1.0f;
            ///    public float hSliderValueRemapY = 1.0f;
            ///    public float hSliderValueRemapZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var noise = ps.noise;
            ///        noise.enabled = true;
            ///        noise.remapEnabled = true;
            ///        noise.separateAxes = true;
            ///
            ///        // An unusual curve to show off different noise behavior (See curve preview in the Inspector)
            ///        AnimationCurve ourCurve;
            ///        ourCurve = new AnimationCurve();
            ///        ourCurve.AddKey(0.0f, 0.0f);
            ///        ourCurve.AddKey(0.45f, -0.75f);
            ///        ourCurve.AddKey(0.50f, 1.0f);
            ///        ourCurve.AddKey(0.55f, -0.75f);
            ///        ourCurve.AddKey(1.0f, 0.0f);
            ///
            ///        // Apply the curve
            ///        noise.remapX = new ParticleSystem.MinMaxCurve(1.0f, ourCurve);
            ///        noise.remapY = new ParticleSystem.MinMaxCurve(1.0f, ourCurve);
            ///        noise.remapZ = new ParticleSystem.MinMaxCurve(1.0f, ourCurve);
            ///
            ///        // Set color by speed, to demonstrate the effects of the Noise Module
            ///        var colorBySpeed = ps.colorBySpeed;
            ///        colorBySpeed.enabled = true;
            ///
            ///        Gradient gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///        );
            ///
            ///        colorBySpeed.color = new ParticleSystem.MinMaxGradient(gradient);
            ///        colorBySpeed.range = new Vector2(3.0f, 7.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var noise = ps.noise;
            ///        noise.remapXMultiplier = hSliderValueRemapX;
            ///        noise.remapYMultiplier = hSliderValueRemapY;
            ///        noise.remapZMultiplier = hSliderValueRemapZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Remap X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Remap Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Remap Z");
            ///
            ///        hSliderValueRemapX = GUI.HorizontalSlider(new Rect(135, 45, 100, 30), hSliderValueRemapX, 0.0f, 5.0f);
            ///        hSliderValueRemapY = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValueRemapY, 0.0f, 5.0f);
            ///        hSliderValueRemapZ = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueRemapZ, 0.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.NoiseModule.remapX" />
            extern public float remapXMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Define how the noise values are remapped on the y-axis, when using the <see cref="ParticleSystem.NoiseModule.separateAxes" /> option.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueRemapX = 1.0f;
            ///    public float hSliderValueRemapY = 1.0f;
            ///    public float hSliderValueRemapZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var noise = ps.noise;
            ///        noise.enabled = true;
            ///        noise.remapEnabled = true;
            ///        noise.separateAxes = true;
            ///
            ///        // An unusual curve to show off different noise behavior (See curve preview in the Inspector)
            ///        AnimationCurve ourCurve;
            ///        ourCurve = new AnimationCurve();
            ///        ourCurve.AddKey(0.0f, 0.0f);
            ///        ourCurve.AddKey(0.45f, -0.75f);
            ///        ourCurve.AddKey(0.50f, 1.0f);
            ///        ourCurve.AddKey(0.55f, -0.75f);
            ///        ourCurve.AddKey(1.0f, 0.0f);
            ///
            ///        // Apply the curve
            ///        noise.remapX = new ParticleSystem.MinMaxCurve(1.0f, ourCurve);
            ///        noise.remapY = new ParticleSystem.MinMaxCurve(1.0f, ourCurve);
            ///        noise.remapZ = new ParticleSystem.MinMaxCurve(1.0f, ourCurve);
            ///
            ///        // Set color by speed, to demonstrate the effects of the Noise Module
            ///        var colorBySpeed = ps.colorBySpeed;
            ///        colorBySpeed.enabled = true;
            ///
            ///        Gradient gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///        );
            ///
            ///        colorBySpeed.color = new ParticleSystem.MinMaxGradient(gradient);
            ///        colorBySpeed.range = new Vector2(3.0f, 7.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var noise = ps.noise;
            ///        noise.remapXMultiplier = hSliderValueRemapX;
            ///        noise.remapYMultiplier = hSliderValueRemapY;
            ///        noise.remapZMultiplier = hSliderValueRemapZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Remap X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Remap Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Remap Z");
            ///
            ///        hSliderValueRemapX = GUI.HorizontalSlider(new Rect(135, 45, 100, 30), hSliderValueRemapX, 0.0f, 5.0f);
            ///        hSliderValueRemapY = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValueRemapY, 0.0f, 5.0f);
            ///        hSliderValueRemapZ = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueRemapZ, 0.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve remapY { get => remapYBlittable; set => remapYBlittable = value; }
            [NativeName("RemapY")] private extern MinMaxCurveBlittable remapYBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>y-axis remap multiplier.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall remap multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueRemapX = 1.0f;
            ///    public float hSliderValueRemapY = 1.0f;
            ///    public float hSliderValueRemapZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var noise = ps.noise;
            ///        noise.enabled = true;
            ///        noise.remapEnabled = true;
            ///        noise.separateAxes = true;
            ///
            ///        // An unusual curve to show off different noise behavior (See curve preview in the Inspector)
            ///        AnimationCurve ourCurve;
            ///        ourCurve = new AnimationCurve();
            ///        ourCurve.AddKey(0.0f, 0.0f);
            ///        ourCurve.AddKey(0.45f, -0.75f);
            ///        ourCurve.AddKey(0.50f, 1.0f);
            ///        ourCurve.AddKey(0.55f, -0.75f);
            ///        ourCurve.AddKey(1.0f, 0.0f);
            ///
            ///        // Apply the curve
            ///        noise.remapX = new ParticleSystem.MinMaxCurve(1.0f, ourCurve);
            ///        noise.remapY = new ParticleSystem.MinMaxCurve(1.0f, ourCurve);
            ///        noise.remapZ = new ParticleSystem.MinMaxCurve(1.0f, ourCurve);
            ///
            ///        // Set color by speed, to demonstrate the effects of the Noise Module
            ///        var colorBySpeed = ps.colorBySpeed;
            ///        colorBySpeed.enabled = true;
            ///
            ///        Gradient gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///        );
            ///
            ///        colorBySpeed.color = new ParticleSystem.MinMaxGradient(gradient);
            ///        colorBySpeed.range = new Vector2(3.0f, 7.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var noise = ps.noise;
            ///        noise.remapXMultiplier = hSliderValueRemapX;
            ///        noise.remapYMultiplier = hSliderValueRemapY;
            ///        noise.remapZMultiplier = hSliderValueRemapZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Remap X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Remap Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Remap Z");
            ///
            ///        hSliderValueRemapX = GUI.HorizontalSlider(new Rect(135, 45, 100, 30), hSliderValueRemapX, 0.0f, 5.0f);
            ///        hSliderValueRemapY = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValueRemapY, 0.0f, 5.0f);
            ///        hSliderValueRemapZ = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueRemapZ, 0.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.NoiseModule.remapY" />
            extern public float remapYMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Define how the noise values are remapped on the z-axis, when using the <see cref="ParticleSystem.NoiseModule.separateAxes" /> option.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueRemapX = 1.0f;
            ///    public float hSliderValueRemapY = 1.0f;
            ///    public float hSliderValueRemapZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var noise = ps.noise;
            ///        noise.enabled = true;
            ///        noise.remapEnabled = true;
            ///        noise.separateAxes = true;
            ///
            ///        // An unusual curve to show off different noise behavior (See curve preview in the Inspector)
            ///        AnimationCurve ourCurve;
            ///        ourCurve = new AnimationCurve();
            ///        ourCurve.AddKey(0.0f, 0.0f);
            ///        ourCurve.AddKey(0.45f, -0.75f);
            ///        ourCurve.AddKey(0.50f, 1.0f);
            ///        ourCurve.AddKey(0.55f, -0.75f);
            ///        ourCurve.AddKey(1.0f, 0.0f);
            ///
            ///        // Apply the curve
            ///        noise.remapX = new ParticleSystem.MinMaxCurve(1.0f, ourCurve);
            ///        noise.remapY = new ParticleSystem.MinMaxCurve(1.0f, ourCurve);
            ///        noise.remapZ = new ParticleSystem.MinMaxCurve(1.0f, ourCurve);
            ///
            ///        // Set color by speed, to demonstrate the effects of the Noise Module
            ///        var colorBySpeed = ps.colorBySpeed;
            ///        colorBySpeed.enabled = true;
            ///
            ///        Gradient gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///        );
            ///
            ///        colorBySpeed.color = new ParticleSystem.MinMaxGradient(gradient);
            ///        colorBySpeed.range = new Vector2(3.0f, 7.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var noise = ps.noise;
            ///        noise.remapXMultiplier = hSliderValueRemapX;
            ///        noise.remapYMultiplier = hSliderValueRemapY;
            ///        noise.remapZMultiplier = hSliderValueRemapZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Remap X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Remap Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Remap Z");
            ///
            ///        hSliderValueRemapX = GUI.HorizontalSlider(new Rect(135, 45, 100, 30), hSliderValueRemapX, 0.0f, 5.0f);
            ///        hSliderValueRemapY = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValueRemapY, 0.0f, 5.0f);
            ///        hSliderValueRemapZ = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueRemapZ, 0.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve remapZ { get => remapZBlittable; set => remapZBlittable = value; }
            [NativeName("RemapZ")] private extern MinMaxCurveBlittable remapZBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>z-axis remap multiplier.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall remap multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueRemapX = 1.0f;
            ///    public float hSliderValueRemapY = 1.0f;
            ///    public float hSliderValueRemapZ = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var noise = ps.noise;
            ///        noise.enabled = true;
            ///        noise.remapEnabled = true;
            ///        noise.separateAxes = true;
            ///
            ///        // An unusual curve to show off different noise behavior (See curve preview in the Inspector)
            ///        AnimationCurve ourCurve;
            ///        ourCurve = new AnimationCurve();
            ///        ourCurve.AddKey(0.0f, 0.0f);
            ///        ourCurve.AddKey(0.45f, -0.75f);
            ///        ourCurve.AddKey(0.50f, 1.0f);
            ///        ourCurve.AddKey(0.55f, -0.75f);
            ///        ourCurve.AddKey(1.0f, 0.0f);
            ///
            ///        // Apply the curve
            ///        noise.remapX = new ParticleSystem.MinMaxCurve(1.0f, ourCurve);
            ///        noise.remapY = new ParticleSystem.MinMaxCurve(1.0f, ourCurve);
            ///        noise.remapZ = new ParticleSystem.MinMaxCurve(1.0f, ourCurve);
            ///
            ///        // Set color by speed, to demonstrate the effects of the Noise Module
            ///        var colorBySpeed = ps.colorBySpeed;
            ///        colorBySpeed.enabled = true;
            ///
            ///        Gradient gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///        );
            ///
            ///        colorBySpeed.color = new ParticleSystem.MinMaxGradient(gradient);
            ///        colorBySpeed.range = new Vector2(3.0f, 7.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var noise = ps.noise;
            ///        noise.remapXMultiplier = hSliderValueRemapX;
            ///        noise.remapYMultiplier = hSliderValueRemapY;
            ///        noise.remapZMultiplier = hSliderValueRemapZ;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Remap X");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Remap Y");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Remap Z");
            ///
            ///        hSliderValueRemapX = GUI.HorizontalSlider(new Rect(135, 45, 100, 30), hSliderValueRemapX, 0.0f, 5.0f);
            ///        hSliderValueRemapY = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValueRemapY, 0.0f, 5.0f);
            ///        hSliderValueRemapZ = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueRemapZ, 0.0f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.NoiseModule.remapZ" />
            extern public float remapZMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>How much the noise affects the particle positions.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValuePositionAmount = 1.0f;
            ///    public float hSliderValueRotationAmount = 0.0f;
            ///    public float hSliderValueSizeAmount = 0.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var noise = ps.noise;
            ///        noise.enabled = true;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, so we can see the rotation
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var noise = ps.noise;
            ///        noise.enabled = enabled;
            ///        noise.positionAmount = hSliderValuePositionAmount;
            ///        noise.rotationAmount = hSliderValueRotationAmount;
            ///        noise.sizeAmount = hSliderValueSizeAmount;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Position Amount");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Rotation Amount");
            ///        GUI.Label(new Rect(25, 160, 100, 30), "Size Amount");
            ///
            ///        hSliderValuePositionAmount = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValuePositionAmount, 0.0f, 5.0f);
            ///        hSliderValueRotationAmount = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueRotationAmount, 0.0f, 180.0f);
            ///        hSliderValueSizeAmount = GUI.HorizontalSlider(new Rect(135, 165, 100, 30), hSliderValueSizeAmount, 0.0f, 1.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            public MinMaxCurve positionAmount { get => positionAmountBlittable; set => positionAmountBlittable = value; }
            [NativeName("PositionAmount")] private extern MinMaxCurveBlittable positionAmountBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>How much the noise affects the particle rotation, in degrees per second.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValuePositionAmount = 0.0f;
            ///    public float hSliderValueRotationAmount = 90.0f;
            ///    public float hSliderValueSizeAmount = 0.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var noise = ps.noise;
            ///        noise.enabled = true;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, so we can see the rotation
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var noise = ps.noise;
            ///        noise.enabled = enabled;
            ///        noise.positionAmount = hSliderValuePositionAmount;
            ///        noise.rotationAmount = hSliderValueRotationAmount;
            ///        noise.sizeAmount = hSliderValueSizeAmount;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Position Amount");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Rotation Amount");
            ///        GUI.Label(new Rect(25, 160, 100, 30), "Size Amount");
            ///
            ///        hSliderValuePositionAmount = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValuePositionAmount, 0.0f, 5.0f);
            ///        hSliderValueRotationAmount = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueRotationAmount, 0.0f, 180.0f);
            ///        hSliderValueSizeAmount = GUI.HorizontalSlider(new Rect(135, 165, 100, 30), hSliderValueSizeAmount, 0.0f, 1.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            public MinMaxCurve rotationAmount { get => rotationAmountBlittable; set => rotationAmountBlittable = value; }
            [NativeName("RotationAmount")] private extern MinMaxCurveBlittable rotationAmountBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>How much the noise affects the particle sizes, applied as a multiplier on the size of each particle.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValuePositionAmount = 0.0f;
            ///    public float hSliderValueRotationAmount = 0.0f;
            ///    public float hSliderValueSizeAmount = 0.5f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var noise = ps.noise;
            ///        noise.enabled = true;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.material = new Material(Shader.Find("Sprites/Default"));    // this material renders a square billboard, so we can see the rotation
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var noise = ps.noise;
            ///        noise.enabled = enabled;
            ///        noise.positionAmount = hSliderValuePositionAmount;
            ///        noise.rotationAmount = hSliderValueRotationAmount;
            ///        noise.sizeAmount = hSliderValueSizeAmount;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Position Amount");
            ///        GUI.Label(new Rect(25, 120, 100, 30), "Rotation Amount");
            ///        GUI.Label(new Rect(25, 160, 100, 30), "Size Amount");
            ///
            ///        hSliderValuePositionAmount = GUI.HorizontalSlider(new Rect(135, 85, 100, 30), hSliderValuePositionAmount, 0.0f, 5.0f);
            ///        hSliderValueRotationAmount = GUI.HorizontalSlider(new Rect(135, 125, 100, 30), hSliderValueRotationAmount, 0.0f, 180.0f);
            ///        hSliderValueSizeAmount = GUI.HorizontalSlider(new Rect(135, 165, 100, 30), hSliderValueSizeAmount, 0.0f, 1.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            public MinMaxCurve sizeAmount { get => sizeAmountBlittable; set => sizeAmountBlittable = value; }
            [NativeName("SizeAmount")] private extern MinMaxCurveBlittable sizeAmountBlittable { get; [NativeMethod(ThrowsException = true)] set; }
        }

        public partial struct CollisionModule
        {
            internal CollisionModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            ///<summary>Specifies whether the CollisionModule is enabled or disabled.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var collision = ps.collision;
            ///        collision.enabled = true;
            ///        collision.type = ParticleSystemCollisionType.World;
            ///        collision.mode = ParticleSystemCollisionMode.Collision3D;
            ///
            ///        var collider = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ///        collider.transform.parent = ps.transform;
            ///        collider.transform.localPosition = new Vector3(0.0f, 0.0f, 13.0f);
            ///        collider.transform.localScale = new Vector3(20.0f, 20.0f, 20.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.collision" />
            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>The type of particle collision to perform.</summary>
            ///<remarks>
            ///  <para>Here is an example of plane collsiion:</para>
            ///  <para>Here is an example of world collision:</para>
            ///</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var collision = ps.collision;
            ///        collision.enabled = true;
            ///        collision.type = ParticleSystemCollisionType.Planes;
            ///        collision.mode = ParticleSystemCollisionMode.Collision3D;
            ///
            ///        var collider = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ///        collider.transform.parent = ps.transform;
            ///        collider.transform.localPosition = new Vector3(0.0f, 0.0f, 5.0f);
            ///        collider.transform.localScale = new Vector3(20.0f, 20.0f, 20.0f);
            ///        collider.transform.localRotation = Quaternion.Euler(new Vector3(-90.0f, 0.0f, 0.0f));
            ///
            ///        collision.SetPlane(0, collider.transform);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var collision = ps.collision;
            ///        collision.enabled = true;
            ///        collision.type = ParticleSystemCollisionType.World;
            ///        collision.mode = ParticleSystemCollisionMode.Collision3D;
            ///
            ///        var collider = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ///        collider.transform.parent = ps.transform;
            ///        collider.transform.localPosition = new Vector3(0.0f, 0.0f, 13.0f);
            ///        collider.transform.localScale = new Vector3(20.0f, 20.0f, 20.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public ParticleSystemCollisionType type { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Choose between 2D and 3D world collisions.</summary>
            extern public ParticleSystemCollisionMode mode { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>How much speed does each particle lose after a collision.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 0.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var collision = ps.collision;
            ///        collision.enabled = true;
            ///        collision.type = ParticleSystemCollisionType.World;
            ///        collision.mode = ParticleSystemCollisionMode.Collision3D;
            ///
            ///        var collider = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ///        collider.transform.parent = ps.transform;
            ///        collider.transform.localPosition = new Vector3(0.0f, 0.0f, 13.0f);
            ///        collider.transform.localScale = new Vector3(20.0f, 20.0f, 20.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var collision = ps.collision;
            ///        collision.dampen = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 40, 100, 30), hSliderValue, 0.0F, 1.0F);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.collision" />
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve dampen { get => dampenBlittable; set => dampenBlittable = value; }
            [NativeName("Dampen")] private extern MinMaxCurveBlittable dampenBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Change the dampen multiplier.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall dampen multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 0.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var collision = ps.collision;
            ///        collision.enabled = true;
            ///        collision.type = ParticleSystemCollisionType.World;
            ///        collision.mode = ParticleSystemCollisionMode.Collision3D;
            ///
            ///        var collider = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ///        collider.transform.parent = ps.transform;
            ///        collider.transform.localPosition = new Vector3(0.0f, 0.0f, 13.0f);
            ///        collider.transform.localScale = new Vector3(20.0f, 20.0f, 20.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var collision = ps.collision;
            ///        collision.dampenMultiplier = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 40, 100, 30), hSliderValue, 0.0F, 1.0F);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float dampenMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>How much force is applied to each particle after a collision.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var collision = ps.collision;
            ///        collision.enabled = true;
            ///        collision.type = ParticleSystemCollisionType.World;
            ///        collision.mode = ParticleSystemCollisionMode.Collision3D;
            ///
            ///        var collider = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ///        collider.transform.parent = ps.transform;
            ///        collider.transform.localPosition = new Vector3(0.0f, 0.0f, 13.0f);
            ///        collider.transform.localScale = new Vector3(20.0f, 20.0f, 20.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var collision = ps.collision;
            ///        collision.bounce = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 40, 100, 30), hSliderValue, 0.0F, 2.0F);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.collision" />
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve bounce { get => bounceBlittable; set => bounceBlittable = value; }
            [NativeName("Bounce")] private extern MinMaxCurveBlittable bounceBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>A multiplier for <see cref="ParticleSystem.CollisionModule.bounce" />.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall bounce multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 1.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var collision = ps.collision;
            ///        collision.enabled = true;
            ///        collision.type = ParticleSystemCollisionType.World;
            ///        collision.mode = ParticleSystemCollisionMode.Collision3D;
            ///
            ///        var collider = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ///        collider.transform.parent = ps.transform;
            ///        collider.transform.localPosition = new Vector3(0.0f, 0.0f, 13.0f);
            ///        collider.transform.localScale = new Vector3(20.0f, 20.0f, 20.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var collision = ps.collision;
            ///        collision.bounceMultiplier = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 40, 100, 30), hSliderValue, 0.0F, 2.0F);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float bounceMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>How much a collision reduces a particle's lifetime.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 0.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var collision = ps.collision;
            ///        collision.enabled = true;
            ///        collision.type = ParticleSystemCollisionType.World;
            ///        collision.mode = ParticleSystemCollisionMode.Collision3D;
            ///
            ///        var collider = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ///        collider.transform.parent = ps.transform;
            ///        collider.transform.localPosition = new Vector3(0.0f, 0.0f, 13.0f);
            ///        collider.transform.localScale = new Vector3(20.0f, 20.0f, 20.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var collision = ps.collision;
            ///        collision.lifetimeLoss = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 40, 100, 30), hSliderValue, 0.0F, 1.0F);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.collision" />
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve lifetimeLoss { get => lifetimeLossBlittable; set => lifetimeLossBlittable = value; }
            [NativeName("LifetimeLoss")] private extern MinMaxCurveBlittable lifetimeLossBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Change the lifetime loss multiplier.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall lifetime loss multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 0.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var collision = ps.collision;
            ///        collision.enabled = true;
            ///        collision.type = ParticleSystemCollisionType.World;
            ///        collision.mode = ParticleSystemCollisionMode.Collision3D;
            ///
            ///        var collider = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ///        collider.transform.parent = ps.transform;
            ///        collider.transform.localPosition = new Vector3(0.0f, 0.0f, 13.0f);
            ///        collider.transform.localScale = new Vector3(20.0f, 20.0f, 20.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var collision = ps.collision;
            ///        collision.lifetimeLossMultiplier = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 40, 100, 30), hSliderValue, 0.0F, 1.0F);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float lifetimeLossMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Kill particles whose speed falls below this threshold, after a collision.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 2.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var collision = ps.collision;
            ///        collision.enabled = true;
            ///        collision.type = ParticleSystemCollisionType.World;
            ///        collision.mode = ParticleSystemCollisionMode.Collision3D;
            ///
            ///        var collider = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ///        collider.transform.parent = ps.transform;
            ///        collider.transform.localPosition = new Vector3(0.0f, 0.0f, 13.0f);
            ///        collider.transform.localScale = new Vector3(20.0f, 20.0f, 20.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var collision = ps.collision;
            ///        collision.minKillSpeed = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 40, 100, 30), hSliderValue, 0.0F, 10.0F);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.collision" />
            extern public float minKillSpeed { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Kill particles whose speed goes above this threshold, after a collision.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 8.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var collision = ps.collision;
            ///        collision.enabled = true;
            ///        collision.type = ParticleSystemCollisionType.World;
            ///        collision.mode = ParticleSystemCollisionMode.Collision3D;
            ///
            ///        var collider = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ///        collider.transform.parent = ps.transform;
            ///        collider.transform.localPosition = new Vector3(0.0f, 0.0f, 13.0f);
            ///        collider.transform.localScale = new Vector3(20.0f, 20.0f, 20.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var collision = ps.collision;
            ///        collision.maxKillSpeed = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 40, 100, 30), hSliderValue, 0.0F, 10.0F);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.collision" />
            extern public float maxKillSpeed { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Control which Layers this Particle System collides with.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool layerToggle;
            ///    private readonly int layer0 = 0;
            ///    private readonly int layer1 = 1;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startColor = Color.red;
            ///
            ///        var shape = ps.shape;
            ///        shape.shapeType = ParticleSystemShapeType.Sphere;
            ///
            ///        var collision = ps.collision;
            ///        collision.enabled = true;
            ///        collision.type = ParticleSystemCollisionType.World;
            ///        collision.mode = ParticleSystemCollisionMode.Collision3D;
            ///        collision.bounce = 0.0f;
            ///
            ///        var collider1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ///        collider1.transform.parent = ps.transform;
            ///        collider1.transform.localPosition = new Vector3(0.0f, 0.0f, 13.0f);
            ///        collider1.transform.localScale = new Vector3(20.0f, 20.0f, 20.0f);
            ///        collider1.layer = layer0;
            ///
            ///        var collider2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ///        collider2.transform.parent = ps.transform;
            ///        collider2.transform.localPosition = new Vector3(0.0f, 0.0f, -13.0f);
            ///        collider2.transform.localScale = new Vector3(20.0f, 20.0f, 20.0f);
            ///        collider2.layer = layer1;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var collision = ps.collision;
            ///        collision.collidesWith = layerToggle ? (1 << layer0) : (1 << layer1);
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        layerToggle = GUI.Toggle(new Rect(25, 40, 100, 30), layerToggle, "Toggle Layer");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="LayerMask" />
            extern public LayerMask collidesWith { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Allow particles to collide with dynamic colliders when using world collision mode.</summary>
            ///<seealso cref="ParticleSystem.collision" />
            extern public bool enableDynamicColliders { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>The maximum number of collision shapes Unity considers for particle collisions. It ignores excess shapes. Terrains take priority.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool maxToggle;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startColor = Color.red;
            ///
            ///        var shape = ps.shape;
            ///        shape.shapeType = ParticleSystemShapeType.Sphere;
            ///
            ///        var collision = ps.collision;
            ///        collision.enabled = true;
            ///        collision.type = ParticleSystemCollisionType.World;
            ///        collision.mode = ParticleSystemCollisionMode.Collision3D;
            ///        collision.bounce = 0.0f;
            ///
            ///        var collider1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ///        collider1.transform.parent = ps.transform;
            ///        collider1.transform.localPosition = new Vector3(0.0f, 0.0f, 13.0f);
            ///        collider1.transform.localScale = new Vector3(20.0f, 20.0f, 20.0f);
            ///
            ///        var collider2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ///        collider2.transform.parent = ps.transform;
            ///        collider2.transform.localPosition = new Vector3(0.0f, 0.0f, -13.0f);
            ///        collider2.transform.localScale = new Vector3(20.0f, 20.0f, 20.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var collision = ps.collision;
            ///        collision.maxCollisionShapes = maxToggle ? 2 : 1;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        maxToggle = GUI.Toggle(new Rect(25, 40, 300, 30), maxToggle, "Toggle Max Collision Shapes (" + (maxToggle ? "2" : "1") + ")");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.collision" />
            extern public int maxCollisionShapes { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Specifies the accuracy of particle collisions against colliders in the Scene.</summary>
            ///<remarks>The high quality setting is the least likely to exhibit any leaked particles through colliders, but comes with the highest CPU load. Medium and low quality use simpler approximations and may leak particles, but offer faster performance.</remarks>
            extern public ParticleSystemCollisionQuality quality { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Size of voxels in the collision cache.</summary>
            ///<remarks>Smaller values improve accuracy, but require higher memory usage and are less efficient.</remarks>
            extern public float voxelSize { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>A multiplier that Unity applies to the size of each particle before collisions are processed.</summary>
            ///<remarks>Useful for improving the visual accuracy of collisions, for example when there is an alpha border in the particle texture.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 8.0F;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var collision = ps.collision;
            ///        collision.enabled = true;
            ///        collision.type = ParticleSystemCollisionType.World;
            ///        collision.mode = ParticleSystemCollisionMode.Collision3D;
            ///        collision.dampen = 1.0f;
            ///
            ///        var collider = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ///        collider.transform.parent = ps.transform;
            ///        collider.transform.localPosition = new Vector3(0.0f, 0.0f, 13.0f);
            ///        collider.transform.localScale = new Vector3(20.0f, 20.0f, 20.0f);
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var collision = ps.collision;
            ///        collision.radiusScale = hSliderValue;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 40, 100, 30), hSliderValue, 0.0F, 2.0F);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.collision" />
            extern public float radiusScale { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Send collision callback messages.</summary>
            ///<seealso cref="ParticleSystem.collision" />
            ///<seealso cref="M:UnityEngine.MonoBehaviour.OnParticleCollision" />
            extern public bool sendCollisionMessages { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>How much force is applied to a Collider when hit by particles from this Particle System.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValue = 0.02F;
            ///    public bool hToggleUseCollisionAngle = true;
            ///    public bool hToggleUseSpeed = true;
            ///    public bool hToggleUseSize = true;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 1.0f);
            ///
            ///        var collision = ps.collision;
            ///        collision.enabled = true;
            ///        collision.type = ParticleSystemCollisionType.World;
            ///        collision.mode = ParticleSystemCollisionMode.Collision3D;
            ///
            ///        var collider = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ///        collider.transform.parent = ps.transform;
            ///        collider.transform.localPosition = new Vector3(0.0f, 0.0f, 13.0f);
            ///        collider.transform.localScale = new Vector3(20.0f, 20.0f, 20.0f);
            ///
            ///        var rb = collider.AddComponent<Rigidbody>();
            ///        rb.isKinematic = false;
            ///        rb.useGravity = false;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var collision = ps.collision;
            ///        collision.colliderForce = hSliderValue;
            ///        collision.multiplyColliderForceByCollisionAngle = hToggleUseCollisionAngle;
            ///        collision.multiplyColliderForceByParticleSpeed = hToggleUseSpeed;
            ///        collision.multiplyColliderForceByParticleSize = hToggleUseSize;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 40, 100, 30), hSliderValue, 0.0F, 1.0F);
            ///        hToggleUseCollisionAngle = GUI.Toggle(new Rect(25, 80, 140, 30), hToggleUseCollisionAngle, "Use Collision Angle");
            ///        hToggleUseSpeed = GUI.Toggle(new Rect(25, 120, 140, 30), hToggleUseSpeed, "Use Particle Speed");
            ///        hToggleUseSize = GUI.Toggle(new Rect(25, 160, 140, 30), hToggleUseSize, "Use Particle Size");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.CollisionModule.multiplyColliderForceByParticleSize" />
            ///<seealso cref="ParticleSystem.CollisionModule.multiplyColliderForceByParticleSpeed" />
            ///<seealso cref="ParticleSystem.CollisionModule.multiplyColliderForceByCollisionAngle" />
            ///<seealso cref="ParticleSystem.collision" />
            extern public float colliderForce { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Specifies whether the physics system considers the collision angle when it applies forces from particles to Colliders.</summary>
            ///<seealso cref="ParticleSystem.CollisionModule.colliderForce" />
            ///<seealso cref="ParticleSystem.collision" />
            extern public bool multiplyColliderForceByCollisionAngle { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Specifies whether the physics system considers particle speeds when it applies forces to Colliders.</summary>
            ///<seealso cref="ParticleSystem.CollisionModule.colliderForce" />
            ///<seealso cref="ParticleSystem.collision" />
            extern public bool multiplyColliderForceByParticleSpeed { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Specifies whether the physics system considers particle sizes when it applies forces to Colliders.</summary>
            ///<remarks>If enabled, the force applied to a collider is proportional to the area (2D) or volume (3D) of the particle hitting the collider.</remarks>
            ///<seealso cref="ParticleSystem.CollisionModule.colliderForce" />
            ///<seealso cref="ParticleSystem.collision" />
            extern public bool multiplyColliderForceByParticleSize { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Adds a collision plane to use with this Particle System.</summary>
            ///<param name="transform">The plane to add.</param>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var collision = ps.collision;
            ///        collision.enabled = true;
            ///        collision.type = ParticleSystemCollisionType.Planes;
            ///        collision.mode = ParticleSystemCollisionMode.Collision3D;
            ///
            ///        var collider = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ///        collider.transform.parent = ps.transform;
            ///        collider.transform.localPosition = new Vector3(0.0f, 0.0f, 5.0f);
            ///        collider.transform.localScale = new Vector3(20.0f, 20.0f, 20.0f);
            ///        collider.transform.localRotation = Quaternion.Euler(new Vector3(-90.0f, 0.0f, 0.0f));
            ///
            ///        collision.AddPlane(collider.transform);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            [NativeMethod(ThrowsException = true)]
            extern public void AddPlane(Transform transform);
            ///<summary>Removes a collision plane associated with this Particle System.</summary>
            ///<param name="index">The collision plane to remove.</param>
            [NativeMethod(ThrowsException = true)]
            extern public void RemovePlane(int index);
            ///<summary>Removes a collision plane associated with this Particle System.</summary>
            ///<param name="transform">The collision plane to remove.</param>
            public void RemovePlane(Transform transform) { RemovePlaneObject(transform); }
            [NativeMethod(ThrowsException = true)]
            extern private void RemovePlaneObject(Transform transform);
            ///<summary>Set a collision plane to use with this Particle System.</summary>
            ///<remarks>If the index is greater than the number of planes currently assigned to the Particle System, Unity adds empty entries to ensure the list is large enough.</remarks>
            ///<param name="index">The plane entry to set.</param>
            ///<param name="transform">The plane to collide particles against.</param>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var collision = ps.collision;
            ///        collision.enabled = true;
            ///        collision.type = ParticleSystemCollisionType.Planes;
            ///        collision.mode = ParticleSystemCollisionMode.Collision3D;
            ///
            ///        var collider = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ///        collider.transform.parent = ps.transform;
            ///        collider.transform.localPosition = new Vector3(0.0f, 0.0f, 5.0f);
            ///        collider.transform.localScale = new Vector3(20.0f, 20.0f, 20.0f);
            ///        collider.transform.localRotation = Quaternion.Euler(new Vector3(-90.0f, 0.0f, 0.0f));
            ///
            ///        collision.SetPlane(0, collider.transform);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            [NativeMethod(ThrowsException = true)]
            extern public void SetPlane(int index, Transform transform);
            ///<summary>Get a collision plane associated with this Particle System.</summary>
            ///<param name="index">The plane to return.</param>
            ///<returns>The plane.</returns>
            [NativeMethod(ThrowsException = true)]
            extern public Transform GetPlane(int index);
            ///<summary>Shows the number of planes currently set as Colliders.</summary>
            [NativeMethod(ThrowsException = true)]
            extern public int planeCount { get; }

            ///<summary>Allow particles to collide when inside colliders.</summary>
            ///<remarks>This can be particularly useful if you move colliders around in a script, and using particles for pickups, in order to avoid missing any collision events. However, it may be necessary to disable it in other cases, where you could find particles getting trapped inside colliders.</remarks>
            [Obsolete("enableInteriorCollisions property is deprecated and is no longer required and has no effect on the particle system.", false)]
            extern public bool enableInteriorCollisions { get; [NativeMethod(ThrowsException = true)] set; }
        }

        public partial struct TriggerModule
        {
            internal TriggerModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            ///<summary>Specifies whether the TriggerModule is enabled or disabled.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using UnityEditor;
            ///using System.Collections.Generic;
            ///using UnityEngine.EventSystems;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool enter;
            ///    public bool exit;
            ///    public bool inside;
            ///    public bool outside;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ///        sphere.transform.parent = ps.transform;
            ///        sphere.transform.localPosition = new Vector3(0.0f, 0.0f, 3.0f);
            ///        sphere.transform.localScale = new Vector3(3.0f, 3.0f, 3.0f);
            ///        sphere.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Particles/Standard Unlit"));
            ///
            ///        var shape = ps.shape;
            ///        shape.enabled = false;
            ///
            ///        var trigger = ps.trigger;
            ///        trigger.enabled = true;
            ///        trigger.SetCollider(0, sphere.GetComponent<Collider>());
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var trigger = ps.trigger;
            ///        trigger.enter = enter ? ParticleSystemOverlapAction.Callback : ParticleSystemOverlapAction.Ignore;
            ///        trigger.exit = exit ? ParticleSystemOverlapAction.Callback : ParticleSystemOverlapAction.Ignore;
            ///        trigger.inside = inside ? ParticleSystemOverlapAction.Callback : ParticleSystemOverlapAction.Ignore;
            ///        trigger.outside = outside ? ParticleSystemOverlapAction.Callback : ParticleSystemOverlapAction.Ignore;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        enter = GUI.Toggle(new Rect(25, 40, 200, 30), enter, "Enter Callback");
            ///        exit = GUI.Toggle(new Rect(25, 80, 200, 30), exit, "Exit Callback");
            ///        inside = GUI.Toggle(new Rect(25, 120, 200, 30), inside, "Inside Callback");
            ///        outside = GUI.Toggle(new Rect(25, 160, 200, 30), outside, "Outside Callback");
            ///    }
            ///
            ///    void OnParticleTrigger()
            ///    {
            ///        if (enter)
            ///        {
            ///            List<ParticleSystem.Particle> enterList = new List<ParticleSystem.Particle>();
            ///            int numEnter = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, enterList);
            ///
            ///            for (int i = 0; i < numEnter; i++)
            ///            {
            ///                ParticleSystem.Particle p = enterList[i];
            ///                p.startColor = new Color32(255, 0, 0, 255);
            ///                enterList[i] = p;
            ///            }
            ///
            ///            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Enter, enterList);
            ///        }
            ///
            ///        if (exit)
            ///        {
            ///            List<ParticleSystem.Particle> exitList = new List<ParticleSystem.Particle>();
            ///            int numExit = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Exit, exitList);
            ///
            ///            for (int i = 0; i < numExit; i++)
            ///            {
            ///                ParticleSystem.Particle p = exitList[i];
            ///                p.startColor = new Color32(0, 255, 0, 255);
            ///                exitList[i] = p;
            ///            }
            ///
            ///            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Exit, exitList);
            ///        }
            ///
            ///        if (inside)
            ///        {
            ///            List<ParticleSystem.Particle> insideList = new List<ParticleSystem.Particle>();
            ///            int numInside = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Inside, insideList);
            ///
            ///            for (int i = 0; i < numInside; i++)
            ///            {
            ///                ParticleSystem.Particle p = insideList[i];
            ///                p.startColor = new Color32(0, 0, 255, 255);
            ///                insideList[i] = p;
            ///            }
            ///
            ///            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Inside, insideList);
            ///        }
            ///
            ///        if (outside)
            ///        {
            ///            List<ParticleSystem.Particle> outsideList = new List<ParticleSystem.Particle>();
            ///            int numOutside = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Outside, outsideList);
            ///
            ///            for (int i = 0; i < numOutside; i++)
            ///            {
            ///                ParticleSystem.Particle p = outsideList[i];
            ///                p.startColor = new Color32(0, 255, 255, 255);
            ///                outsideList[i] = p;
            ///            }
            ///
            ///            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Outside, outsideList);
            ///        }
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.trigger" />
            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Choose what action to perform when particles are inside the trigger volume.</summary>
            ///<remarks>The system performs this action every frame that particles are inside the trigger volume.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using UnityEditor;
            ///using System.Collections.Generic;
            ///using UnityEngine.EventSystems;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool enter;
            ///    public bool exit;
            ///    public bool inside;
            ///    public bool outside;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ///        sphere.transform.parent = ps.transform;
            ///        sphere.transform.localPosition = new Vector3(0.0f, 0.0f, 3.0f);
            ///        sphere.transform.localScale = new Vector3(3.0f, 3.0f, 3.0f);
            ///        sphere.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Particles/Standard Unlit"));
            ///
            ///        var shape = ps.shape;
            ///        shape.enabled = false;
            ///
            ///        var trigger = ps.trigger;
            ///        trigger.enabled = true;
            ///        trigger.SetCollider(0, sphere.GetComponent<Collider>());
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var trigger = ps.trigger;
            ///        trigger.enter = enter ? ParticleSystemOverlapAction.Callback : ParticleSystemOverlapAction.Ignore;
            ///        trigger.exit = exit ? ParticleSystemOverlapAction.Callback : ParticleSystemOverlapAction.Ignore;
            ///        trigger.inside = inside ? ParticleSystemOverlapAction.Callback : ParticleSystemOverlapAction.Ignore;
            ///        trigger.outside = outside ? ParticleSystemOverlapAction.Callback : ParticleSystemOverlapAction.Ignore;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        enter = GUI.Toggle(new Rect(25, 40, 200, 30), enter, "Enter Callback");
            ///        exit = GUI.Toggle(new Rect(25, 80, 200, 30), exit, "Exit Callback");
            ///        inside = GUI.Toggle(new Rect(25, 120, 200, 30), inside, "Inside Callback");
            ///        outside = GUI.Toggle(new Rect(25, 160, 200, 30), outside, "Outside Callback");
            ///    }
            ///
            ///    void OnParticleTrigger()
            ///    {
            ///        if (enter)
            ///        {
            ///            List<ParticleSystem.Particle> enterList = new List<ParticleSystem.Particle>();
            ///            int numEnter = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, enterList);
            ///
            ///            for (int i = 0; i < numEnter; i++)
            ///            {
            ///                ParticleSystem.Particle p = enterList[i];
            ///                p.startColor = new Color32(255, 0, 0, 255);
            ///                enterList[i] = p;
            ///            }
            ///
            ///            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Enter, enterList);
            ///        }
            ///
            ///        if (exit)
            ///        {
            ///            List<ParticleSystem.Particle> exitList = new List<ParticleSystem.Particle>();
            ///            int numExit = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Exit, exitList);
            ///
            ///            for (int i = 0; i < numExit; i++)
            ///            {
            ///                ParticleSystem.Particle p = exitList[i];
            ///                p.startColor = new Color32(0, 255, 0, 255);
            ///                exitList[i] = p;
            ///            }
            ///
            ///            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Exit, exitList);
            ///        }
            ///
            ///        if (inside)
            ///        {
            ///            List<ParticleSystem.Particle> insideList = new List<ParticleSystem.Particle>();
            ///            int numInside = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Inside, insideList);
            ///
            ///            for (int i = 0; i < numInside; i++)
            ///            {
            ///                ParticleSystem.Particle p = insideList[i];
            ///                p.startColor = new Color32(0, 0, 255, 255);
            ///                insideList[i] = p;
            ///            }
            ///
            ///            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Inside, insideList);
            ///        }
            ///
            ///        if (outside)
            ///        {
            ///            List<ParticleSystem.Particle> outsideList = new List<ParticleSystem.Particle>();
            ///            int numOutside = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Outside, outsideList);
            ///
            ///            for (int i = 0; i < numOutside; i++)
            ///            {
            ///                ParticleSystem.Particle p = outsideList[i];
            ///                p.startColor = new Color32(0, 255, 255, 255);
            ///                outsideList[i] = p;
            ///            }
            ///
            ///            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Outside, outsideList);
            ///        }
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="M:UnityEngine.MonoBehaviour.OnParticleTrigger" />
            extern public ParticleSystemOverlapAction inside { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Choose what action to perform when particles are outside the trigger volume.</summary>
            ///<remarks>The system performs this action every frame that particles are outside the trigger volume.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using UnityEditor;
            ///using System.Collections.Generic;
            ///using UnityEngine.EventSystems;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool enter;
            ///    public bool exit;
            ///    public bool inside;
            ///    public bool outside;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ///        sphere.transform.parent = ps.transform;
            ///        sphere.transform.localPosition = new Vector3(0.0f, 0.0f, 3.0f);
            ///        sphere.transform.localScale = new Vector3(3.0f, 3.0f, 3.0f);
            ///        sphere.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Particles/Standard Unlit"));
            ///
            ///        var shape = ps.shape;
            ///        shape.enabled = false;
            ///
            ///        var trigger = ps.trigger;
            ///        trigger.enabled = true;
            ///        trigger.SetCollider(0, sphere.GetComponent<Collider>());
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var trigger = ps.trigger;
            ///        trigger.enter = enter ? ParticleSystemOverlapAction.Callback : ParticleSystemOverlapAction.Ignore;
            ///        trigger.exit = exit ? ParticleSystemOverlapAction.Callback : ParticleSystemOverlapAction.Ignore;
            ///        trigger.inside = inside ? ParticleSystemOverlapAction.Callback : ParticleSystemOverlapAction.Ignore;
            ///        trigger.outside = outside ? ParticleSystemOverlapAction.Callback : ParticleSystemOverlapAction.Ignore;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        enter = GUI.Toggle(new Rect(25, 40, 200, 30), enter, "Enter Callback");
            ///        exit = GUI.Toggle(new Rect(25, 80, 200, 30), exit, "Exit Callback");
            ///        inside = GUI.Toggle(new Rect(25, 120, 200, 30), inside, "Inside Callback");
            ///        outside = GUI.Toggle(new Rect(25, 160, 200, 30), outside, "Outside Callback");
            ///    }
            ///
            ///    void OnParticleTrigger()
            ///    {
            ///        if (enter)
            ///        {
            ///            List<ParticleSystem.Particle> enterList = new List<ParticleSystem.Particle>();
            ///            int numEnter = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, enterList);
            ///
            ///            for (int i = 0; i < numEnter; i++)
            ///            {
            ///                ParticleSystem.Particle p = enterList[i];
            ///                p.startColor = new Color32(255, 0, 0, 255);
            ///                enterList[i] = p;
            ///            }
            ///
            ///            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Enter, enterList);
            ///        }
            ///
            ///        if (exit)
            ///        {
            ///            List<ParticleSystem.Particle> exitList = new List<ParticleSystem.Particle>();
            ///            int numExit = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Exit, exitList);
            ///
            ///            for (int i = 0; i < numExit; i++)
            ///            {
            ///                ParticleSystem.Particle p = exitList[i];
            ///                p.startColor = new Color32(0, 255, 0, 255);
            ///                exitList[i] = p;
            ///            }
            ///
            ///            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Exit, exitList);
            ///        }
            ///
            ///        if (inside)
            ///        {
            ///            List<ParticleSystem.Particle> insideList = new List<ParticleSystem.Particle>();
            ///            int numInside = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Inside, insideList);
            ///
            ///            for (int i = 0; i < numInside; i++)
            ///            {
            ///                ParticleSystem.Particle p = insideList[i];
            ///                p.startColor = new Color32(0, 0, 255, 255);
            ///                insideList[i] = p;
            ///            }
            ///
            ///            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Inside, insideList);
            ///        }
            ///
            ///        if (outside)
            ///        {
            ///            List<ParticleSystem.Particle> outsideList = new List<ParticleSystem.Particle>();
            ///            int numOutside = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Outside, outsideList);
            ///
            ///            for (int i = 0; i < numOutside; i++)
            ///            {
            ///                ParticleSystem.Particle p = outsideList[i];
            ///                p.startColor = new Color32(0, 255, 255, 255);
            ///                outsideList[i] = p;
            ///            }
            ///
            ///            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Outside, outsideList);
            ///        }
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="M:UnityEngine.MonoBehaviour.OnParticleTrigger" />
            extern public ParticleSystemOverlapAction outside { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Choose what action to perform when particles enter the trigger volume.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using UnityEditor;
            ///using System.Collections.Generic;
            ///using UnityEngine.EventSystems;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool enter;
            ///    public bool exit;
            ///    public bool inside;
            ///    public bool outside;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ///        sphere.transform.parent = ps.transform;
            ///        sphere.transform.localPosition = new Vector3(0.0f, 0.0f, 3.0f);
            ///        sphere.transform.localScale = new Vector3(3.0f, 3.0f, 3.0f);
            ///        sphere.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Particles/Standard Unlit"));
            ///
            ///        var shape = ps.shape;
            ///        shape.enabled = false;
            ///
            ///        var trigger = ps.trigger;
            ///        trigger.enabled = true;
            ///        trigger.SetCollider(0, sphere.GetComponent<Collider>());
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var trigger = ps.trigger;
            ///        trigger.enter = enter ? ParticleSystemOverlapAction.Callback : ParticleSystemOverlapAction.Ignore;
            ///        trigger.exit = exit ? ParticleSystemOverlapAction.Callback : ParticleSystemOverlapAction.Ignore;
            ///        trigger.inside = inside ? ParticleSystemOverlapAction.Callback : ParticleSystemOverlapAction.Ignore;
            ///        trigger.outside = outside ? ParticleSystemOverlapAction.Callback : ParticleSystemOverlapAction.Ignore;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        enter = GUI.Toggle(new Rect(25, 40, 200, 30), enter, "Enter Callback");
            ///        exit = GUI.Toggle(new Rect(25, 80, 200, 30), exit, "Exit Callback");
            ///        inside = GUI.Toggle(new Rect(25, 120, 200, 30), inside, "Inside Callback");
            ///        outside = GUI.Toggle(new Rect(25, 160, 200, 30), outside, "Outside Callback");
            ///    }
            ///
            ///    void OnParticleTrigger()
            ///    {
            ///        if (enter)
            ///        {
            ///            List<ParticleSystem.Particle> enterList = new List<ParticleSystem.Particle>();
            ///            int numEnter = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, enterList);
            ///
            ///            for (int i = 0; i < numEnter; i++)
            ///            {
            ///                ParticleSystem.Particle p = enterList[i];
            ///                p.startColor = new Color32(255, 0, 0, 255);
            ///                enterList[i] = p;
            ///            }
            ///
            ///            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Enter, enterList);
            ///        }
            ///
            ///        if (exit)
            ///        {
            ///            List<ParticleSystem.Particle> exitList = new List<ParticleSystem.Particle>();
            ///            int numExit = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Exit, exitList);
            ///
            ///            for (int i = 0; i < numExit; i++)
            ///            {
            ///                ParticleSystem.Particle p = exitList[i];
            ///                p.startColor = new Color32(0, 255, 0, 255);
            ///                exitList[i] = p;
            ///            }
            ///
            ///            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Exit, exitList);
            ///        }
            ///
            ///        if (inside)
            ///        {
            ///            List<ParticleSystem.Particle> insideList = new List<ParticleSystem.Particle>();
            ///            int numInside = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Inside, insideList);
            ///
            ///            for (int i = 0; i < numInside; i++)
            ///            {
            ///                ParticleSystem.Particle p = insideList[i];
            ///                p.startColor = new Color32(0, 0, 255, 255);
            ///                insideList[i] = p;
            ///            }
            ///
            ///            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Inside, insideList);
            ///        }
            ///
            ///        if (outside)
            ///        {
            ///            List<ParticleSystem.Particle> outsideList = new List<ParticleSystem.Particle>();
            ///            int numOutside = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Outside, outsideList);
            ///
            ///            for (int i = 0; i < numOutside; i++)
            ///            {
            ///                ParticleSystem.Particle p = outsideList[i];
            ///                p.startColor = new Color32(0, 255, 255, 255);
            ///                outsideList[i] = p;
            ///            }
            ///
            ///            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Outside, outsideList);
            ///        }
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="M:UnityEngine.MonoBehaviour.OnParticleTrigger" />
            extern public ParticleSystemOverlapAction enter { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Choose what action to perform when particles leave the trigger volume.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using UnityEditor;
            ///using System.Collections.Generic;
            ///using UnityEngine.EventSystems;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool enter;
            ///    public bool exit;
            ///    public bool inside;
            ///    public bool outside;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ///        sphere.transform.parent = ps.transform;
            ///        sphere.transform.localPosition = new Vector3(0.0f, 0.0f, 3.0f);
            ///        sphere.transform.localScale = new Vector3(3.0f, 3.0f, 3.0f);
            ///        sphere.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Particles/Standard Unlit"));
            ///
            ///        var shape = ps.shape;
            ///        shape.enabled = false;
            ///
            ///        var trigger = ps.trigger;
            ///        trigger.enabled = true;
            ///        trigger.SetCollider(0, sphere.GetComponent<Collider>());
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var trigger = ps.trigger;
            ///        trigger.enter = enter ? ParticleSystemOverlapAction.Callback : ParticleSystemOverlapAction.Ignore;
            ///        trigger.exit = exit ? ParticleSystemOverlapAction.Callback : ParticleSystemOverlapAction.Ignore;
            ///        trigger.inside = inside ? ParticleSystemOverlapAction.Callback : ParticleSystemOverlapAction.Ignore;
            ///        trigger.outside = outside ? ParticleSystemOverlapAction.Callback : ParticleSystemOverlapAction.Ignore;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        enter = GUI.Toggle(new Rect(25, 40, 200, 30), enter, "Enter Callback");
            ///        exit = GUI.Toggle(new Rect(25, 80, 200, 30), exit, "Exit Callback");
            ///        inside = GUI.Toggle(new Rect(25, 120, 200, 30), inside, "Inside Callback");
            ///        outside = GUI.Toggle(new Rect(25, 160, 200, 30), outside, "Outside Callback");
            ///    }
            ///
            ///    void OnParticleTrigger()
            ///    {
            ///        if (enter)
            ///        {
            ///            List<ParticleSystem.Particle> enterList = new List<ParticleSystem.Particle>();
            ///            int numEnter = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, enterList);
            ///
            ///            for (int i = 0; i < numEnter; i++)
            ///            {
            ///                ParticleSystem.Particle p = enterList[i];
            ///                p.startColor = new Color32(255, 0, 0, 255);
            ///                enterList[i] = p;
            ///            }
            ///
            ///            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Enter, enterList);
            ///        }
            ///
            ///        if (exit)
            ///        {
            ///            List<ParticleSystem.Particle> exitList = new List<ParticleSystem.Particle>();
            ///            int numExit = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Exit, exitList);
            ///
            ///            for (int i = 0; i < numExit; i++)
            ///            {
            ///                ParticleSystem.Particle p = exitList[i];
            ///                p.startColor = new Color32(0, 255, 0, 255);
            ///                exitList[i] = p;
            ///            }
            ///
            ///            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Exit, exitList);
            ///        }
            ///
            ///        if (inside)
            ///        {
            ///            List<ParticleSystem.Particle> insideList = new List<ParticleSystem.Particle>();
            ///            int numInside = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Inside, insideList);
            ///
            ///            for (int i = 0; i < numInside; i++)
            ///            {
            ///                ParticleSystem.Particle p = insideList[i];
            ///                p.startColor = new Color32(0, 0, 255, 255);
            ///                insideList[i] = p;
            ///            }
            ///
            ///            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Inside, insideList);
            ///        }
            ///
            ///        if (outside)
            ///        {
            ///            List<ParticleSystem.Particle> outsideList = new List<ParticleSystem.Particle>();
            ///            int numOutside = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Outside, outsideList);
            ///
            ///            for (int i = 0; i < numOutside; i++)
            ///            {
            ///                ParticleSystem.Particle p = outsideList[i];
            ///                p.startColor = new Color32(0, 255, 255, 255);
            ///                outsideList[i] = p;
            ///            }
            ///
            ///            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Outside, outsideList);
            ///        }
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="M:UnityEngine.MonoBehaviour.OnParticleTrigger" />
            extern public ParticleSystemOverlapAction exit { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Determines whether collider information is available when calling <see cref="M:UnityEngine.ParticlePhysicsExtensions.GetTriggerParticles" />.</summary>
            ///<remarks>Use this option if you need to know which Colliders particles interacted with, inside the OnParticleTrigger callback.
            ///
            ///Using it has an impact on performance, therefore it is disabled by default.</remarks>
            extern public ParticleSystemColliderQueryMode colliderQueryMode { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>A multiplier Unity applies to the size of each particle before it processes overlaps.</summary>
            ///<remarks>Useful for improving the visual accuracy of overlaps, for example when there is an alpha border in the particle texture.</remarks>
            ///<seealso cref="ParticleSystem.trigger" />
            extern public float radiusScale { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Adds a Collision shape associated with this Particle System trigger.</summary>
            ///<param name="collider">The Collider to associate with this trigger.</param>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using UnityEditor;
            ///using System.Collections.Generic;
            ///using UnityEngine.EventSystems;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool enter;
            ///    public bool exit;
            ///    public bool inside;
            ///    public bool outside;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ///        sphere.transform.parent = ps.transform;
            ///        sphere.transform.localPosition = new Vector3(0.0f, 0.0f, 3.0f);
            ///        sphere.transform.localScale = new Vector3(3.0f, 3.0f, 3.0f);
            ///        sphere.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Particles/Standard Unlit"));
            ///
            ///        var shape = ps.shape;
            ///        shape.enabled = false;
            ///
            ///        var trigger = ps.trigger;
            ///        trigger.enabled = true;
            ///        trigger.AddCollider(sphere.GetComponent<Collider>());
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var trigger = ps.trigger;
            ///        trigger.enter = enter ? ParticleSystemOverlapAction.Callback : ParticleSystemOverlapAction.Ignore;
            ///        trigger.exit = exit ? ParticleSystemOverlapAction.Callback : ParticleSystemOverlapAction.Ignore;
            ///        trigger.inside = inside ? ParticleSystemOverlapAction.Callback : ParticleSystemOverlapAction.Ignore;
            ///        trigger.outside = outside ? ParticleSystemOverlapAction.Callback : ParticleSystemOverlapAction.Ignore;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        enter = GUI.Toggle(new Rect(25, 40, 200, 30), enter, "Enter Callback");
            ///        exit = GUI.Toggle(new Rect(25, 80, 200, 30), exit, "Exit Callback");
            ///        inside = GUI.Toggle(new Rect(25, 120, 200, 30), inside, "Inside Callback");
            ///        outside = GUI.Toggle(new Rect(25, 160, 200, 30), outside, "Outside Callback");
            ///    }
            ///
            ///    void OnParticleTrigger()
            ///    {
            ///        if (enter)
            ///        {
            ///            List<ParticleSystem.Particle> enterList = new List<ParticleSystem.Particle>();
            ///            int numEnter = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, enterList);
            ///
            ///            for (int i = 0; i < numEnter; i++)
            ///            {
            ///                ParticleSystem.Particle p = enterList[i];
            ///                p.startColor = new Color32(255, 0, 0, 255);
            ///                enterList[i] = p;
            ///            }
            ///
            ///            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Enter, enterList);
            ///        }
            ///
            ///        if (exit)
            ///        {
            ///            List<ParticleSystem.Particle> exitList = new List<ParticleSystem.Particle>();
            ///            int numExit = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Exit, exitList);
            ///
            ///            for (int i = 0; i < numExit; i++)
            ///            {
            ///                ParticleSystem.Particle p = exitList[i];
            ///                p.startColor = new Color32(0, 255, 0, 255);
            ///                exitList[i] = p;
            ///            }
            ///
            ///            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Exit, exitList);
            ///        }
            ///
            ///        if (inside)
            ///        {
            ///            List<ParticleSystem.Particle> insideList = new List<ParticleSystem.Particle>();
            ///            int numInside = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Inside, insideList);
            ///
            ///            for (int i = 0; i < numInside; i++)
            ///            {
            ///                ParticleSystem.Particle p = insideList[i];
            ///                p.startColor = new Color32(0, 0, 255, 255);
            ///                insideList[i] = p;
            ///            }
            ///
            ///            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Inside, insideList);
            ///        }
            ///
            ///        if (outside)
            ///        {
            ///            List<ParticleSystem.Particle> outsideList = new List<ParticleSystem.Particle>();
            ///            int numOutside = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Outside, outsideList);
            ///
            ///            for (int i = 0; i < numOutside; i++)
            ///            {
            ///                ParticleSystem.Particle p = outsideList[i];
            ///                p.startColor = new Color32(0, 255, 255, 255);
            ///                outsideList[i] = p;
            ///            }
            ///
            ///            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Outside, outsideList);
            ///        }
            ///    }
            ///}
            ///]]></code>
            ///</example>
            [NativeMethod(ThrowsException = true)]
            extern public void AddCollider(Component collider);
            ///<summary>Removes a collision shape associated with this Particle System trigger.</summary>
            ///<param name="index">The Collider to remove.</param>
            ///<seealso cref="ParticleSystem.TriggerModule.AddCollider" />
            [NativeMethod(ThrowsException = true)]
            extern public void RemoveCollider(int index);
            ///<summary>Removes a collision shape associated with this Particle System trigger.</summary>
            ///<param name="collider">The Collider to remove.</param>
            ///<seealso cref="ParticleSystem.TriggerModule.AddCollider" />
            public void RemoveCollider(Component collider) { RemoveColliderObject(collider); }
            [NativeMethod(ThrowsException = true)]
            extern private void RemoveColliderObject(Component collider);
            ///<summary>Sets a Collision shape associated with this Particle System trigger.</summary>
            ///<remarks>If the index is greater than the number of Colliders currently assigned to the Particle System, Unity adds empty entries to ensure the list is large enough.</remarks>
            ///<param name="index">The Collider entry to assign.</param>
            ///<param name="collider">The Collider to associate with this trigger.</param>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using UnityEditor;
            ///using System.Collections.Generic;
            ///using UnityEngine.EventSystems;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool enter;
            ///    public bool exit;
            ///    public bool inside;
            ///    public bool outside;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ///        sphere.transform.parent = ps.transform;
            ///        sphere.transform.localPosition = new Vector3(0.0f, 0.0f, 3.0f);
            ///        sphere.transform.localScale = new Vector3(3.0f, 3.0f, 3.0f);
            ///        sphere.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Particles/Standard Unlit"));
            ///
            ///        var shape = ps.shape;
            ///        shape.enabled = false;
            ///
            ///        var trigger = ps.trigger;
            ///        trigger.enabled = true;
            ///        trigger.SetCollider(0, sphere.GetComponent<Collider>());
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var trigger = ps.trigger;
            ///        trigger.enter = enter ? ParticleSystemOverlapAction.Callback : ParticleSystemOverlapAction.Ignore;
            ///        trigger.exit = exit ? ParticleSystemOverlapAction.Callback : ParticleSystemOverlapAction.Ignore;
            ///        trigger.inside = inside ? ParticleSystemOverlapAction.Callback : ParticleSystemOverlapAction.Ignore;
            ///        trigger.outside = outside ? ParticleSystemOverlapAction.Callback : ParticleSystemOverlapAction.Ignore;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        enter = GUI.Toggle(new Rect(25, 40, 200, 30), enter, "Enter Callback");
            ///        exit = GUI.Toggle(new Rect(25, 80, 200, 30), exit, "Exit Callback");
            ///        inside = GUI.Toggle(new Rect(25, 120, 200, 30), inside, "Inside Callback");
            ///        outside = GUI.Toggle(new Rect(25, 160, 200, 30), outside, "Outside Callback");
            ///    }
            ///
            ///    void OnParticleTrigger()
            ///    {
            ///        if (enter)
            ///        {
            ///            List<ParticleSystem.Particle> enterList = new List<ParticleSystem.Particle>();
            ///            int numEnter = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, enterList);
            ///
            ///            for (int i = 0; i < numEnter; i++)
            ///            {
            ///                ParticleSystem.Particle p = enterList[i];
            ///                p.startColor = new Color32(255, 0, 0, 255);
            ///                enterList[i] = p;
            ///            }
            ///
            ///            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Enter, enterList);
            ///        }
            ///
            ///        if (exit)
            ///        {
            ///            List<ParticleSystem.Particle> exitList = new List<ParticleSystem.Particle>();
            ///            int numExit = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Exit, exitList);
            ///
            ///            for (int i = 0; i < numExit; i++)
            ///            {
            ///                ParticleSystem.Particle p = exitList[i];
            ///                p.startColor = new Color32(0, 255, 0, 255);
            ///                exitList[i] = p;
            ///            }
            ///
            ///            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Exit, exitList);
            ///        }
            ///
            ///        if (inside)
            ///        {
            ///            List<ParticleSystem.Particle> insideList = new List<ParticleSystem.Particle>();
            ///            int numInside = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Inside, insideList);
            ///
            ///            for (int i = 0; i < numInside; i++)
            ///            {
            ///                ParticleSystem.Particle p = insideList[i];
            ///                p.startColor = new Color32(0, 0, 255, 255);
            ///                insideList[i] = p;
            ///            }
            ///
            ///            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Inside, insideList);
            ///        }
            ///
            ///        if (outside)
            ///        {
            ///            List<ParticleSystem.Particle> outsideList = new List<ParticleSystem.Particle>();
            ///            int numOutside = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Outside, outsideList);
            ///
            ///            for (int i = 0; i < numOutside; i++)
            ///            {
            ///                ParticleSystem.Particle p = outsideList[i];
            ///                p.startColor = new Color32(0, 255, 255, 255);
            ///                outsideList[i] = p;
            ///            }
            ///
            ///            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Outside, outsideList);
            ///        }
            ///    }
            ///}
            ///]]></code>
            ///</example>
            [NativeMethod(ThrowsException = true)]
            extern public void SetCollider(int index, Component collider);
            ///<summary>Gets a collision shape associated with this Particle System trigger.</summary>
            ///<param name="index">The Collider to return.</param>
            ///<returns>The Collider at the given index.</returns>
            ///<seealso cref="ParticleSystem.TriggerModule.SetCollider" />
            [NativeMethod(ThrowsException = true)]
            extern public Component GetCollider(int index);
            ///<summary>Indicates the number of collision shapes attached to this Particle System trigger.</summary>
            [NativeMethod(ThrowsException = true)]
            extern public int colliderCount { get; }
        }

        public partial struct SubEmittersModule
        {
            internal SubEmittersModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            ///<summary>Specifies whether the SubEmittersModule is enabled or disabled.</summary>
            ///<seealso cref="ParticleSystem.subEmitters" />
            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>The total number of sub-emitters.</summary>
            extern public int subEmittersCount { get; }

            ///<summary>Add a new sub-emitter.</summary>
            ///<param name="subEmitter">The sub-emitter to add.</param>
            ///<param name="type">The event that creates new particles.</param>
            ///<param name="properties">The properties of the new particles.</param>
            ///<param name="emitProbability">The probability that the sub-emitter emits particles. Accepts values from 0 to 1, where 0 is never and 1 is always.</param>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            /// // Add a birth sub-emitter
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    void Start()
            ///    {
            ///        // A simple particle material with no texture.
            ///        Material particleMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Lit"));
            ///
            ///        // Create a green Particle System.
            ///        var rootSystemGO = new GameObject("Particle System");
            ///        rootSystemGO.transform.Rotate(-90, 0, 0); // Rotate so the system emits upwards.
            ///        var rootParticleSystem = rootSystemGO.AddComponent<ParticleSystem>();
            ///        rootSystemGO.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
            ///        var mainModule = rootParticleSystem.main;
            ///        mainModule.startColor = Color.green;
            ///        mainModule.startSize = 0.5f;
            ///
            ///        // Spread the particles out more so the sub-emitter effect is more obvious.
            ///        var shapeModule = rootParticleSystem.shape;
            ///        shapeModule.radius = 100;
            ///
            ///        // Create our sub-emitter and set up bursts.
            ///        var subSystemGO = new GameObject("Particle System");
            ///        var subParticleSystem = subSystemGO.AddComponent<ParticleSystem>();
            ///        subSystemGO.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
            ///        var subMainModule = subParticleSystem.main;
            ///        subMainModule.startColor = Color.red;
            ///        subMainModule.startSize = 0.25f;
            ///        subMainModule.startLifetime = 0.5f; // very short life particles.
            ///        var emissionModule = subParticleSystem.emission;
            ///        emissionModule.rateOverTime = 2; // 1 particle will emit every 0.5 sec.
            ///        emissionModule.SetBursts(new ParticleSystem.Burst[] // A burst will emit at 1 and 3 secs.
            ///        {
            ///            new ParticleSystem.Burst(1.0f, 10),
            ///            new ParticleSystem.Burst(3.0f, 10)
            ///        });
            ///
            ///        // Set up the sub particles so they fade over time.
            ///        var colorModule = subParticleSystem.colorOverLifetime;
            ///        colorModule.enabled = true;
            ///        var gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) }, // Color remains untouched.
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }); // Alpha fades
            ///        colorModule.color = gradient;
            ///
            ///        // Setup the sub-emitter.
            ///        subSystemGO.transform.SetParent(rootSystemGO.transform);
            ///        var subEmittersModule = rootParticleSystem.subEmitters;
            ///        subEmittersModule.enabled = true;
            ///        subEmittersModule.AddSubEmitter(subParticleSystem, ParticleSystemSubEmitterType.Birth, ParticleSystemSubEmitterProperties.InheritNothing);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            /// // Add a collision sub-emitter
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    void Start()
            ///    {
            ///        // For this example we will need something to collide with in the world.
            ///        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ///        cube.transform.position = new Vector3(0, 10, 0); // Position above the Particle System.
            ///        cube.transform.localScale = new Vector3(10, 10, 10);
            ///
            ///        // A simple particle material with no texture.
            ///        Material particleMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Lit"));
            ///
            ///        // Create a green Particle System.
            ///        var rootSystemGO = new GameObject("Particle System");
            ///        rootSystemGO.transform.Rotate(-90, 0, 0); // Rotate so the system emits upwards.
            ///        var rootParticleSystem = rootSystemGO.AddComponent<ParticleSystem>();
            ///        rootSystemGO.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
            ///        var mainModule = rootParticleSystem.main;
            ///        mainModule.startColor = Color.green;
            ///        mainModule.startSize = 0.5f;
            ///
            ///        // Enable and setup the collisions module.
            ///        var collisionModule = rootParticleSystem.collision;
            ///        collisionModule.enabled = true;
            ///        collisionModule.type = ParticleSystemCollisionType.World;
            ///
            ///        // Create our sub-emitter and setup bursts.
            ///        var subSystemGO = new GameObject("Particle System");
            ///        var subParticleSystem = subSystemGO.AddComponent<ParticleSystem>();
            ///        subSystemGO.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
            ///        var subMainModule = subParticleSystem.main;
            ///        subMainModule.startColor = Color.red;
            ///        subMainModule.startSize = 0.25f;
            ///        var emissionModule = subParticleSystem.emission;
            ///        emissionModule.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 10) }); // We will emit 10 particles upon collision.
            ///
            ///        // Set up the sub-emitter.
            ///        subSystemGO.transform.SetParent(rootSystemGO.transform);
            ///        var subEmittersModule = rootParticleSystem.subEmitters;
            ///        subEmittersModule.enabled = true;
            ///        subEmittersModule.AddSubEmitter(subParticleSystem, ParticleSystemSubEmitterType.Collision, ParticleSystemSubEmitterProperties.InheritNothing);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            /// // Add a death sub-emitter
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    void Start()
            ///    {
            ///        // A simple particle material with no texture.
            ///        Material particleMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Lit"));
            ///
            ///        // Create a green Particle System.
            ///        var rootSystemGO = new GameObject("Particle System");
            ///        rootSystemGO.transform.Rotate(-90, 0, 0); // Rotate so the system emits upwards.
            ///        var rootParticleSystem = rootSystemGO.AddComponent<ParticleSystem>();
            ///        rootSystemGO.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
            ///        var mainModule = rootParticleSystem.main;
            ///        mainModule.startColor = Color.green;
            ///        mainModule.startSize = 0.5f;
            ///
            ///        // Create our sub-emitter and setup bursts.
            ///        var subSystemGO = new GameObject("Particle System");
            ///        var subParticleSystem = subSystemGO.AddComponent<ParticleSystem>();
            ///        subSystemGO.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
            ///        var subMainModule = subParticleSystem.main;
            ///        subMainModule.startColor = Color.red;
            ///        subMainModule.startSize = 0.25f;
            ///        var emissionModule = subParticleSystem.emission;
            ///        emissionModule.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 10) }); // We will emit 10 particles upon death.
            ///
            ///        // Set up the sub-emitter.
            ///        subSystemGO.transform.SetParent(rootSystemGO.transform);
            ///        var subEmittersModule = rootParticleSystem.subEmitters;
            ///        subEmittersModule.enabled = true;
            ///        subEmittersModule.AddSubEmitter(subParticleSystem, ParticleSystemSubEmitterType.Death, ParticleSystemSubEmitterProperties.InheritNothing);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            /// // Add a manual sub-emitter
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    private float m_Timer = 0.0f;
            ///    public float m_Interval = 2.0f;
            ///
            ///    void Start()
            ///    {
            ///        // A simple particle material with no texture.
            ///        Material particleMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Lit"));
            ///
            ///        // Create a green Particle System.
            ///        var rootSystemGO = new GameObject("Particle System");
            ///        rootSystemGO.transform.Rotate(-90, 0, 0); // Rotate so the system emits upwards.
            ///        ps = rootSystemGO.AddComponent<ParticleSystem>();
            ///        rootSystemGO.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
            ///        var mainModule = ps.main;
            ///        mainModule.startColor = Color.green;
            ///        mainModule.startSize = 0.5f;
            ///
            ///        // Create our sub-emitter and setup bursts.
            ///        var subSystemGO = new GameObject("Particle System");
            ///        var subParticleSystem = subSystemGO.AddComponent<ParticleSystem>();
            ///        subSystemGO.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
            ///        var subMainModule = subParticleSystem.main;
            ///        subMainModule.startColor = Color.red;
            ///        subMainModule.startSize = 0.25f;
            ///        var emissionModule = subParticleSystem.emission;
            ///        emissionModule.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 4) }); // We will emit 10 particles when triggered.
            ///
            ///        // Set up the sub-emitter.
            ///        subSystemGO.transform.SetParent(rootSystemGO.transform);
            ///        var subEmittersModule = ps.subEmitters;
            ///        subEmittersModule.enabled = true;
            ///        subEmittersModule.AddSubEmitter(subParticleSystem, ParticleSystemSubEmitterType.Manual, ParticleSystemSubEmitterProperties.InheritNothing);
            ///    }
            ///
            ///    private void Update()
            ///    {
            ///        m_Timer += Time.deltaTime;
            ///        while (m_Timer >= m_Interval)
            ///        {
            ///            ps.TriggerSubEmitter(0);
            ///            m_Timer -= m_Interval;
            ///        }
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.SubEmittersModule.RemoveSubEmitter" />
            [NativeMethod(ThrowsException = true)]
            extern public void AddSubEmitter(ParticleSystem subEmitter, ParticleSystemSubEmitterType type, ParticleSystemSubEmitterProperties properties, float emitProbability);
            ///<summary>Add a new sub-emitter.</summary>
            ///<param name="subEmitter">The sub-emitter to add.</param>
            ///<param name="type">The event that creates new particles.</param>
            ///<param name="properties">The properties of the new particles.</param>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            /// // Add a birth sub-emitter
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    void Start()
            ///    {
            ///        // A simple particle material with no texture.
            ///        Material particleMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Lit"));
            ///
            ///        // Create a green Particle System.
            ///        var rootSystemGO = new GameObject("Particle System");
            ///        rootSystemGO.transform.Rotate(-90, 0, 0); // Rotate so the system emits upwards.
            ///        var rootParticleSystem = rootSystemGO.AddComponent<ParticleSystem>();
            ///        rootSystemGO.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
            ///        var mainModule = rootParticleSystem.main;
            ///        mainModule.startColor = Color.green;
            ///        mainModule.startSize = 0.5f;
            ///
            ///        // Spread the particles out more so the sub-emitter effect is more obvious.
            ///        var shapeModule = rootParticleSystem.shape;
            ///        shapeModule.radius = 100;
            ///
            ///        // Create our sub-emitter and set up bursts.
            ///        var subSystemGO = new GameObject("Particle System");
            ///        var subParticleSystem = subSystemGO.AddComponent<ParticleSystem>();
            ///        subSystemGO.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
            ///        var subMainModule = subParticleSystem.main;
            ///        subMainModule.startColor = Color.red;
            ///        subMainModule.startSize = 0.25f;
            ///        subMainModule.startLifetime = 0.5f; // very short life particles.
            ///        var emissionModule = subParticleSystem.emission;
            ///        emissionModule.rateOverTime = 2; // 1 particle will emit every 0.5 sec.
            ///        emissionModule.SetBursts(new ParticleSystem.Burst[] // A burst will emit at 1 and 3 secs.
            ///        {
            ///            new ParticleSystem.Burst(1.0f, 10),
            ///            new ParticleSystem.Burst(3.0f, 10)
            ///        });
            ///
            ///        // Set up the sub particles so they fade over time.
            ///        var colorModule = subParticleSystem.colorOverLifetime;
            ///        colorModule.enabled = true;
            ///        var gradient = new Gradient();
            ///        gradient.SetKeys(
            ///            new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) }, // Color remains untouched.
            ///            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }); // Alpha fades
            ///        colorModule.color = gradient;
            ///
            ///        // Setup the sub-emitter.
            ///        subSystemGO.transform.SetParent(rootSystemGO.transform);
            ///        var subEmittersModule = rootParticleSystem.subEmitters;
            ///        subEmittersModule.enabled = true;
            ///        subEmittersModule.AddSubEmitter(subParticleSystem, ParticleSystemSubEmitterType.Birth, ParticleSystemSubEmitterProperties.InheritNothing);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            /// // Add a collision sub-emitter
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    void Start()
            ///    {
            ///        // For this example we will need something to collide with in the world.
            ///        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ///        cube.transform.position = new Vector3(0, 10, 0); // Position above the Particle System.
            ///        cube.transform.localScale = new Vector3(10, 10, 10);
            ///
            ///        // A simple particle material with no texture.
            ///        Material particleMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Lit"));
            ///
            ///        // Create a green Particle System.
            ///        var rootSystemGO = new GameObject("Particle System");
            ///        rootSystemGO.transform.Rotate(-90, 0, 0); // Rotate so the system emits upwards.
            ///        var rootParticleSystem = rootSystemGO.AddComponent<ParticleSystem>();
            ///        rootSystemGO.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
            ///        var mainModule = rootParticleSystem.main;
            ///        mainModule.startColor = Color.green;
            ///        mainModule.startSize = 0.5f;
            ///
            ///        // Enable and setup the collisions module.
            ///        var collisionModule = rootParticleSystem.collision;
            ///        collisionModule.enabled = true;
            ///        collisionModule.type = ParticleSystemCollisionType.World;
            ///
            ///        // Create our sub-emitter and setup bursts.
            ///        var subSystemGO = new GameObject("Particle System");
            ///        var subParticleSystem = subSystemGO.AddComponent<ParticleSystem>();
            ///        subSystemGO.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
            ///        var subMainModule = subParticleSystem.main;
            ///        subMainModule.startColor = Color.red;
            ///        subMainModule.startSize = 0.25f;
            ///        var emissionModule = subParticleSystem.emission;
            ///        emissionModule.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 10) }); // We will emit 10 particles upon collision.
            ///
            ///        // Set up the sub-emitter.
            ///        subSystemGO.transform.SetParent(rootSystemGO.transform);
            ///        var subEmittersModule = rootParticleSystem.subEmitters;
            ///        subEmittersModule.enabled = true;
            ///        subEmittersModule.AddSubEmitter(subParticleSystem, ParticleSystemSubEmitterType.Collision, ParticleSystemSubEmitterProperties.InheritNothing);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            /// // Add a death sub-emitter
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    void Start()
            ///    {
            ///        // A simple particle material with no texture.
            ///        Material particleMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Lit"));
            ///
            ///        // Create a green Particle System.
            ///        var rootSystemGO = new GameObject("Particle System");
            ///        rootSystemGO.transform.Rotate(-90, 0, 0); // Rotate so the system emits upwards.
            ///        var rootParticleSystem = rootSystemGO.AddComponent<ParticleSystem>();
            ///        rootSystemGO.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
            ///        var mainModule = rootParticleSystem.main;
            ///        mainModule.startColor = Color.green;
            ///        mainModule.startSize = 0.5f;
            ///
            ///        // Create our sub-emitter and setup bursts.
            ///        var subSystemGO = new GameObject("Particle System");
            ///        var subParticleSystem = subSystemGO.AddComponent<ParticleSystem>();
            ///        subSystemGO.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
            ///        var subMainModule = subParticleSystem.main;
            ///        subMainModule.startColor = Color.red;
            ///        subMainModule.startSize = 0.25f;
            ///        var emissionModule = subParticleSystem.emission;
            ///        emissionModule.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 10) }); // We will emit 10 particles upon death.
            ///
            ///        // Set up the sub-emitter.
            ///        subSystemGO.transform.SetParent(rootSystemGO.transform);
            ///        var subEmittersModule = rootParticleSystem.subEmitters;
            ///        subEmittersModule.enabled = true;
            ///        subEmittersModule.AddSubEmitter(subParticleSystem, ParticleSystemSubEmitterType.Death, ParticleSystemSubEmitterProperties.InheritNothing);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            /// // Add a manual sub-emitter
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    private float m_Timer = 0.0f;
            ///    public float m_Interval = 2.0f;
            ///
            ///    void Start()
            ///    {
            ///        // A simple particle material with no texture.
            ///        Material particleMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Lit"));
            ///
            ///        // Create a green Particle System.
            ///        var rootSystemGO = new GameObject("Particle System");
            ///        rootSystemGO.transform.Rotate(-90, 0, 0); // Rotate so the system emits upwards.
            ///        ps = rootSystemGO.AddComponent<ParticleSystem>();
            ///        rootSystemGO.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
            ///        var mainModule = ps.main;
            ///        mainModule.startColor = Color.green;
            ///        mainModule.startSize = 0.5f;
            ///
            ///        // Create our sub-emitter and setup bursts.
            ///        var subSystemGO = new GameObject("Particle System");
            ///        var subParticleSystem = subSystemGO.AddComponent<ParticleSystem>();
            ///        subSystemGO.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
            ///        var subMainModule = subParticleSystem.main;
            ///        subMainModule.startColor = Color.red;
            ///        subMainModule.startSize = 0.25f;
            ///        var emissionModule = subParticleSystem.emission;
            ///        emissionModule.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 4) }); // We will emit 10 particles when triggered.
            ///
            ///        // Set up the sub-emitter.
            ///        subSystemGO.transform.SetParent(rootSystemGO.transform);
            ///        var subEmittersModule = ps.subEmitters;
            ///        subEmittersModule.enabled = true;
            ///        subEmittersModule.AddSubEmitter(subParticleSystem, ParticleSystemSubEmitterType.Manual, ParticleSystemSubEmitterProperties.InheritNothing);
            ///    }
            ///
            ///    private void Update()
            ///    {
            ///        m_Timer += Time.deltaTime;
            ///        while (m_Timer >= m_Interval)
            ///        {
            ///            ps.TriggerSubEmitter(0);
            ///            m_Timer -= m_Interval;
            ///        }
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.SubEmittersModule.RemoveSubEmitter" />
            public void AddSubEmitter(ParticleSystem subEmitter, ParticleSystemSubEmitterType type, ParticleSystemSubEmitterProperties properties) { AddSubEmitter(subEmitter, type, properties, 1.0f); }
            ///<summary>Removes a sub-emitter from the given index in the array.</summary>
            ///<param name="index">The index from which to remove a sub-emitter.</param>
            ///<seealso cref="ParticleSystem.SubEmittersModule.AddSubEmitter" />
            [NativeMethod(ThrowsException = true)]
            extern public void RemoveSubEmitter(int index);
            ///<summary>Removes a sub-emitter from the given index in the array.</summary>
            ///<param name="subEmitter">The sub-emitter to remove.</param>
            ///<seealso cref="ParticleSystem.SubEmittersModule.AddSubEmitter" />
            public void RemoveSubEmitter(ParticleSystem subEmitter) { RemoveSubEmitterObject(subEmitter); }
            [NativeMethod(ThrowsException = true)]
            extern private void RemoveSubEmitterObject(ParticleSystem subEmitter);
            ///<summary>Sets the Particle System to use as the sub-emitter at the given index.</summary>
            ///<param name="index">The index of the sub-emitter you want to modify.</param>
            ///<param name="subEmitter">The Particle System to use as the sub-emitter at the specified index.</param>
            [NativeMethod(ThrowsException = true)]
            extern public void SetSubEmitterSystem(int index, ParticleSystem subEmitter);
            ///<summary>Sets the type of the sub-emitter at the given index.</summary>
            ///<param name="index">The index of the sub-emitter you want to modify.</param>
            ///<param name="type">The new spawning type to assign to this sub-emitter.</param>
            [NativeMethod(ThrowsException = true)]
            extern public void SetSubEmitterType(int index, ParticleSystemSubEmitterType type);
            ///<summary>Sets the properties of the sub-emitter at the given index.</summary>
            ///<param name="index">The index of the sub-emitter you want to modify.</param>
            ///<param name="properties">The new properties to assign to this sub-emitter.</param>
            [NativeMethod(ThrowsException = true)]
            extern public void SetSubEmitterProperties(int index, ParticleSystemSubEmitterProperties properties);
            ///<summary>Sets the probability that the sub-emitter emits particles.</summary>
            ///<remarks>Accepts a value from 0 to 1, where 0 is never and 1 is always.</remarks>
            ///<param name="index">The index of the sub-emitter you want to modify.</param>
            ///<param name="emitProbability">The probability value.</param>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            ///public class Example : MonoBehaviour
            ///{
            ///    void Start()
            ///    {
            ///        // A simple particle material with no texture.
            ///        var particleMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Lit"));
            ///
            ///        // Emit 1 particle per second.
            ///        var particleSystemGameObject = new GameObject("Particle System");
            ///        var particleSystemMain = particleSystemGameObject.AddComponent<ParticleSystem>();
            ///        var emitMain = particleSystemMain.emission;
            ///        emitMain.rateOverTime = 1;
            ///        particleSystemGameObject.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
            ///
            ///        // Create a sub-emitter with a 10% chance to emit a red particle when "Particle System" emits.
            ///        var subEmitterGo = new GameObject("Sub Emitter");
            ///        subEmitterGo.transform.SetParent(particleSystemGameObject.transform);
            ///        var subEmitter = subEmitterGo.AddComponent<ParticleSystem>();
            ///        var emitSub = subEmitter.emission;
            ///        emitSub.rateOverTime = 0;
            ///        emitSub.burstCount = 1;
            ///        emitSub.SetBurst(0, new ParticleSystem.Burst(0, 1));
            ///        var mainModule = subEmitter.main;
            ///        mainModule.startColor = Color.red;
            ///        subEmitterGo.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
            ///
            ///        // Add the sub-emitter, and set the probability.
            ///        var subEmittersModule = particleSystemMain.subEmitters;
            ///        subEmittersModule.enabled = true;
            ///        subEmittersModule.AddSubEmitter(subEmitter, ParticleSystemSubEmitterType.Birth, new ParticleSystemSubEmitterProperties(), 0.1f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            [NativeMethod(ThrowsException = true)]
            extern public void SetSubEmitterEmitProbability(int index, float emitProbability);
            ///<summary>Gets the sub-emitter Particle System at the given index.</summary>
            ///<param name="index">The index of the desired sub-emitter.</param>
            ///<returns>The sub-emitter at the index.</returns>
            [NativeMethod(ThrowsException = true)]
            extern public ParticleSystem GetSubEmitterSystem(int index);
            ///<summary>Gets the type of the sub-emitter at the given index.</summary>
            ///<param name="index">The index of the desired sub-emitter.</param>
            ///<returns>The type of sub-emitter at the index.</returns>
            [NativeMethod(ThrowsException = true)]
            extern public ParticleSystemSubEmitterType GetSubEmitterType(int index);
            ///<summary>Gets the properties of the sub-emitter at the given index.</summary>
            ///<param name="index">The index of the sub-emitter.</param>
            ///<returns>The properties of the sub-emitter at the index.</returns>
            [NativeMethod(ThrowsException = true)]
            extern public ParticleSystemSubEmitterProperties GetSubEmitterProperties(int index);
            ///<summary>Gets the probability that the sub-emitter emits particles.</summary>
            ///<remarks>The return value ranges from 0 to 1, where 0 is never and 1 is always.</remarks>
            ///<param name="index">The index of the sub-emitter.</param>
            ///<returns>The emission probability for the sub-emitter</returns>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            ///public class Example : MonoBehaviour
            ///{
            ///    void Start()
            ///    {
            ///        // A simple particle material with no texture.
            ///        var particleMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Lit"));
            ///
            ///        // Emit 1 particle per second.
            ///        var particleSystemGameObject = new GameObject("Particle System");
            ///        var particleSystemMain = particleSystemGameObject.AddComponent<ParticleSystem>();
            ///        var emitMain = particleSystemMain.emission;
            ///        emitMain.rateOverTime = 1;
            ///        particleSystemGameObject.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
            ///
            ///        // Create a sub-emitter that has a 10% chance of emitting a red particle when "Particle System" emits.
            ///        var subEmitterGo = new GameObject("Sub Emitter");
            ///        subEmitterGo.transform.SetParent(particleSystemGameObject.transform);
            ///        var subEmitter = subEmitterGo.AddComponent<ParticleSystem>();
            ///        var emitSub = subEmitter.emission;
            ///        emitSub.rateOverTime = 0;
            ///        emitSub.burstCount = 1;
            ///        emitSub.SetBurst(0, new ParticleSystem.Burst(0, 1));
            ///        var mainModule = subEmitter.main;
            ///        mainModule.startColor = Color.red;
            ///        subEmitterGo.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
            ///
            ///        // Add the sub-emitter, and set the probability.
            ///        var subEmittersModule = particleSystemMain.subEmitters;
            ///        subEmittersModule.enabled = true;
            ///        subEmittersModule.AddSubEmitter(subEmitter, ParticleSystemSubEmitterType.Birth, new ParticleSystemSubEmitterProperties(), 0.1f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            [NativeMethod(ThrowsException = true)]
            extern public float GetSubEmitterEmitProbability(int index);
        }

        public partial struct TextureSheetAnimationModule
        {
            internal TextureSheetAnimationModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            ///<summary>Specifies whether the TextureSheetAnimationModule is enabled or disabled.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public ParticleSystemAnimationType animType;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startLifetimeMultiplier = 2.0f;
            ///
            ///        var tex = ps.textureSheetAnimation;
            ///        tex.enabled = true;
            ///        tex.numTilesX = 4;
            ///        tex.numTilesY = 2;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var tex = ps.textureSheetAnimation;
            ///        tex.animation = animType;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        animType = (ParticleSystemAnimationType)GUI.SelectionGrid(new Rect(25, 25, 300, 30), (int)animType, new GUIContent[] { new GUIContent("WholeSheet"), new GUIContent("SingleRow") }, 2);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.textureSheetAnimation" />
            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Select whether the animated Texture information comes from a grid of frames on a single Texture, or from a list of Sprite objects.</summary>
            ///<seealso cref="Sprite" />
            extern public ParticleSystemAnimationMode mode { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Select whether the system bases the playback on mapping a curve to the lifetime of each particle, by using the particle speeds, or if playback simply uses a constant frames per second.</summary>
            ///<seealso cref="ParticleSystem.TextureSheetAnimationModule.frameOverTime" />
            ///<seealso cref="ParticleSystem.TextureSheetAnimationModule.speedRange" />
            ///<seealso cref="ParticleSystem.TextureSheetAnimationModule.fps" />
            extern public ParticleSystemAnimationTimeMode timeMode { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Control how quickly the animation plays.</summary>
            ///<remarks>The system uses this property when <see cref="ParticleSystem.TextureSheetAnimationModule.timeMode" /> is set to FPS.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float fps = 30.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startLifetimeMultiplier = 2.0f;
            ///
            ///        var tex = ps.textureSheetAnimation;
            ///        tex.enabled = true;
            ///        tex.timeMode = ParticleSystemAnimationTimeMode.FPS;
            ///        tex.numTilesX = 4;
            ///        tex.numTilesY = 2;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var tex = ps.textureSheetAnimation;
            ///        tex.fps = fps;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 20, 100, 30), "FPS");
            ///
            ///        fps = GUI.HorizontalSlider(new Rect(125, 25, 100, 30), fps, 1.0f, 60.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float fps { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Defines the tiling of the Texture in the x-axis.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public ParticleSystemAnimationType animType;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startLifetimeMultiplier = 2.0f;
            ///
            ///        var tex = ps.textureSheetAnimation;
            ///        tex.enabled = true;
            ///        tex.numTilesX = 4;
            ///        tex.numTilesY = 2;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var tex = ps.textureSheetAnimation;
            ///        tex.animation = animType;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        animType = (ParticleSystemAnimationType)GUI.SelectionGrid(new Rect(25, 25, 300, 30), (int)animType, new GUIContent[] { new GUIContent("WholeSheet"), new GUIContent("SingleRow") }, 2);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public int numTilesX { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Defines the tiling of the texture in the y-axis.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public ParticleSystemAnimationType animType;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startLifetimeMultiplier = 2.0f;
            ///
            ///        var tex = ps.textureSheetAnimation;
            ///        tex.enabled = true;
            ///        tex.numTilesX = 4;
            ///        tex.numTilesY = 2;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var tex = ps.textureSheetAnimation;
            ///        tex.animation = animType;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        animType = (ParticleSystemAnimationType)GUI.SelectionGrid(new Rect(25, 25, 300, 30), (int)animType, new GUIContent[] { new GUIContent("WholeSheet"), new GUIContent("SingleRow") }, 2);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public int numTilesY { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Specifies the animation type.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public ParticleSystemAnimationType animType;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startLifetimeMultiplier = 2.0f;
            ///
            ///        var tex = ps.textureSheetAnimation;
            ///        tex.enabled = true;
            ///        tex.numTilesX = 4;
            ///        tex.numTilesY = 2;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var tex = ps.textureSheetAnimation;
            ///        tex.animation = animType;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        animType = (ParticleSystemAnimationType)GUI.SelectionGrid(new Rect(25, 25, 300, 30), (int)animType, new GUIContent[] { new GUIContent("WholeSheet"), new GUIContent("SingleRow") }, 2);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public ParticleSystemAnimationType animation { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Select how particles choose which row of a Texture Sheet Animation to use.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool useRandomRow = true;
            ///    public int row = 0;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startLifetimeMultiplier = 2.0f;
            ///
            ///        var tex = ps.textureSheetAnimation;
            ///        tex.enabled = true;
            ///        tex.numTilesX = 4;
            ///        tex.numTilesY = 2;
            ///        tex.animation = ParticleSystemAnimationType.SingleRow;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var tex = ps.textureSheetAnimation;
            ///        tex.rowMode = ParticleSystemAnimationRowMode.Custom;
            ///        tex.rowIndex = row;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        useRandomRow = GUI.Toggle(new Rect(25, 20, 100, 30), useRandomRow, "Use Random Row");
            ///
            ///        if (useRandomRow == false)
            ///        {
            ///            GUI.Label(new Rect(25, 60, 100, 30), "Row Index");
            ///            row = (int)GUI.HorizontalSlider(new Rect(125, 65, 100, 30), (float)row, 0.0f, 1.0f);
            ///        }
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.TextureSheetAnimationModule.rowIndex" />
            extern public ParticleSystemAnimationRowMode rowMode { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>A curve to control which frame of the Texture sheet animation to play.</summary>
            ///<remarks>The system uses this property when <see cref="ParticleSystem.TextureSheetAnimationModule.timeMode" /> is set to Curve.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startLifetimeMultiplier = 2.0f;
            ///
            ///        var tex = ps.textureSheetAnimation;
            ///        tex.enabled = true;
            ///        tex.numTilesX = 4;
            ///        tex.numTilesY = 2;
            ///
            ///        // A simple ping-pong curve.
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(0.5f, 1.0f);
            ///        curve.AddKey(1.0f, 0.0f);
            ///
            ///        // Apply the curve.
            ///        tex.frameOverTime = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve frameOverTime { get => frameOverTimeBlittable; set => frameOverTimeBlittable = value; }
            [NativeName("FrameOverTime")] private extern MinMaxCurveBlittable frameOverTimeBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>The frame over time mutiplier.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall frame over time multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startLifetimeMultiplier = 2.0f;
            ///
            ///        var tex = ps.textureSheetAnimation;
            ///        tex.enabled = true;
            ///        tex.numTilesX = 4;
            ///        tex.numTilesY = 2;
            ///        tex.frameOverTimeMultiplier = 2.0f;
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float frameOverTimeMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Define a random starting frame for the Texture sheet animation.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float startFrame = 0.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startLifetimeMultiplier = 2.0f;
            ///
            ///        var tex = ps.textureSheetAnimation;
            ///        tex.enabled = true;
            ///        tex.numTilesX = 4;
            ///        tex.numTilesY = 2;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var tex = ps.textureSheetAnimation;
            ///        tex.startFrame = startFrame;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        startFrame = GUI.HorizontalSlider(new Rect(25, 25, 100, 30), startFrame, 0.0f, 7.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve startFrame { get => startFrameBlittable; set => startFrameBlittable = value; }
            [NativeName("StartFrame")] private extern MinMaxCurveBlittable startFrameBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>The starting frame multiplier.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall start frame multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float startFrame = 0.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startLifetimeMultiplier = 2.0f;
            ///
            ///        var tex = ps.textureSheetAnimation;
            ///        tex.enabled = true;
            ///        tex.numTilesX = 4;
            ///        tex.numTilesY = 2;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var tex = ps.textureSheetAnimation;
            ///        tex.startFrameMultiplier = startFrame;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        startFrame = GUI.HorizontalSlider(new Rect(25, 25, 100, 30), startFrame, 0.0f, 7.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float startFrameMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Specifies how many times the animation loops during the lifetime of the particle.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float cycleCount = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startLifetimeMultiplier = 2.0f;
            ///
            ///        var tex = ps.textureSheetAnimation;
            ///        tex.enabled = true;
            ///        tex.numTilesX = 4;
            ///        tex.numTilesY = 2;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var tex = ps.textureSheetAnimation;
            ///        tex.cycleCount = (int)cycleCount;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        cycleCount = GUI.HorizontalSlider(new Rect(25, 25, 100, 30), cycleCount, 0.1f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public int cycleCount { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Explicitly select which row of the Texture sheet to use. The system uses this property when <see cref="ParticleSystem.TextureSheetAnimationModule.rowMode" /> is set to Custom.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool useRandomRow = true;
            ///    public int row = 0;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startLifetimeMultiplier = 2.0f;
            ///
            ///        var tex = ps.textureSheetAnimation;
            ///        tex.enabled = true;
            ///        tex.numTilesX = 4;
            ///        tex.numTilesY = 2;
            ///        tex.animation = ParticleSystemAnimationType.SingleRow;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var tex = ps.textureSheetAnimation;
            ///        tex.rowMode = ParticleSystemAnimationRowMode.Custom;
            ///        tex.rowIndex = row;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        useRandomRow = GUI.Toggle(new Rect(25, 20, 100, 30), useRandomRow, "Use Random Row");
            ///
            ///        if (useRandomRow == false)
            ///        {
            ///            GUI.Label(new Rect(25, 60, 100, 30), "Row Index");
            ///            row = (int)GUI.HorizontalSlider(new Rect(125, 65, 100, 30), (float)row, 0.0f, 1.0f);
            ///        }
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public int rowIndex { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Choose which UV channels receive Texture animation.</summary>
            ///<remarks>By default, all UV channels receive animation.</remarks>
            extern public Rendering.UVChannelFlags uvChannelMask { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>The total number of sprites.</summary>
            extern public int spriteCount { get; }
            ///<summary>Specify how particle speeds are mapped to the animation frames.</summary>
            ///<remarks>If a particle is travelling slower than the minimum speed, it uses the first frame. If a particle is travelling faster than the maximum speed, then it uses the final frame. For all other speeds, the particle chooses a frame based on how far between the minimum and maximum value its speed is.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var textureSheetAnimation = ps.textureSheetAnimation;
            ///        textureSheetAnimation.enabled = true;
            ///        textureSheetAnimation.speedRange = new Vector2(0.9f, 5.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public Vector2 speedRange { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Add a new Sprite.</summary>
            ///<param name="sprite">The Sprite to be added.</param>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    private Sprite sprite;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
            ///
            ///        var textureSheetAnimation = ps.textureSheetAnimation;
            ///        textureSheetAnimation.enabled = true;
            ///        textureSheetAnimation.mode = ParticleSystemAnimationMode.Sprites;
            ///        textureSheetAnimation.AddSprite(sprite);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.TextureSheetAnimationModule.RemoveSprite" />
            [NativeMethod(ThrowsException = true)]
            extern public void AddSprite(Sprite sprite);
            ///<summary>Remove a Sprite from the given index in the array.</summary>
            ///<param name="index">The index from which to remove a Sprite.</param>
            ///<seealso cref="ParticleSystem.TextureSheetAnimationModule.AddSprite" />
            [NativeMethod(ThrowsException = true)]
            extern public void RemoveSprite(int index);
            ///<summary>Set the Sprite at the given index.</summary>
            ///<param name="index">The index of the Sprite being modified.</param>
            ///<param name="sprite">The Sprite being assigned.</param>
            ///<seealso cref="ParticleSystem.TextureSheetAnimationModule.GetSprite" />
            [NativeMethod(ThrowsException = true)]
            extern public void SetSprite(int index, Sprite sprite);
            ///<summary>Get the Sprite at the given index.</summary>
            ///<param name="index">The index of the desired Sprite.</param>
            ///<returns>The Sprite being requested.</returns>
            ///<seealso cref="ParticleSystem.TextureSheetAnimationModule.SetSprite" />
            [NativeMethod(ThrowsException = true)]
            extern public Sprite GetSprite(int index);
        }

        ///<summary>Script interface for LightsModule.</summary>
        ///<remarks>This module allows you to attach real-time Lights to a percentage of your particles.
        ///
        ///This module allows particles to cast light onto their environment easily. Lights can inherit properties from the particles they are attached to, such as color and size. This module supports Point and Spot Lights, including shadow casting and Light cookies.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Light myLight;
        ///
        ///    void Start()
        ///    {
        ///        ParticleSystem ps = GetComponent<ParticleSystem>();
        ///        var lights = ps.lights;
        ///        lights.enabled = true;
        ///        lights.ratio = 0.5f;
        ///        lights.light = myLight;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="ParticleSystem" />
        ///<seealso cref="ParticleSystem.lights" />
        public partial struct LightsModule
        {
            internal LightsModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            ///<summary>Specifies whether the LightsModule is enabled or disabled.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            /// // For best results, use Deferred Rendering (see Camera settings)
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public Light lightPrefab;     // Provide a light Prefab in the inspector (eg a default Point Light)
            ///    public bool moduleEnabled = true;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///        ps.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 45.0f);
            ///
            ///        var shape = ps.shape;
            ///        shape.shapeType = ParticleSystemShapeType.Circle;
            ///
            ///        var lights = ps.lights;
            ///        lights.light = lightPrefab;
            ///        lights.ratio = 1.0f;
            ///        lights.maxLights = 1000;
            ///
            ///        // plane to receive lights
            ///        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ///        plane.transform.parent = ps.transform;
            ///        plane.transform.localPosition = new Vector3(0.0f, 0.0f, 3.0f);
            ///        plane.transform.localScale = new Vector3(20.0f, 20.0f, 20.0f);
            ///        plane.transform.localRotation = Quaternion.Euler(-90.0f, 0.0f, 0.0f);
            ///
            ///        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            ///        material.color = new Color(0.1f, 0.1f, 0.1f, 1.0f);
            ///        plane.GetComponent<MeshRenderer>().material = material;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var lights = ps.lights;
            ///        lights.enabled = moduleEnabled;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        moduleEnabled = GUI.Toggle(new Rect(25, 45, 200, 30), moduleEnabled, "Enabled");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.lights" />
            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Choose what proportion of particles receive a dynamic light.</summary>
            ///<remarks>Use a value between 0 and 1, where 0 attaches Lights to no particles, and 1 attaches Lights to every particle.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            /// // For best results, use Deferred Rendering (see Camera settings)
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public Light lightPrefab;     // Provide a light Prefab in the inspector
            ///    public float hSliderValueRatio = 1.0f;
            ///    public float hSliderValueMax = 1000.0f;
            ///    public bool randomDistribution = true;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///        ps.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 45.0f);
            ///
            ///        var shape = ps.shape;
            ///        shape.shapeType = ParticleSystemShapeType.Circle;
            ///
            ///        var lights = ps.lights;
            ///        lights.enabled = true;
            ///        lights.light = lightPrefab;
            ///
            ///        // plane to receive lights
            ///        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ///        plane.transform.parent = ps.transform;
            ///        plane.transform.localPosition = new Vector3(0.0f, 0.0f, 3.0f);
            ///        plane.transform.localScale = new Vector3(20.0f, 20.0f, 20.0f);
            ///        plane.transform.localRotation = Quaternion.Euler(-90.0f, 0.0f, 0.0f);
            ///
            ///        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            ///        material.color = new Color(0.1f, 0.1f, 0.1f, 1.0f);
            ///        plane.GetComponent<MeshRenderer>().material = material;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var lights = ps.lights;
            ///        lights.ratio = hSliderValueRatio;
            ///        lights.maxLights = (int)hSliderValueMax;
            ///        lights.useRandomDistribution = randomDistribution;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Ratio");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Max Lights");
            ///
            ///        hSliderValueRatio = GUI.HorizontalSlider(new Rect(95, 45, 100, 30), hSliderValueRatio, 0.0f, 1.0f);
            ///        hSliderValueMax = GUI.HorizontalSlider(new Rect(95, 85, 100, 30), hSliderValueMax, 0.0f, 500.0f);
            ///
            ///        randomDistribution = GUI.Toggle(new Rect(25, 125, 400, 30), randomDistribution, "Randomly assign Lights to Particles instead of evenly distributing them.");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float ratio { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Randomly assign Lights to new particles based on <see cref="ParticleSystem.LightsModule.ratio" />.</summary>
            ///<remarks>When this property is false, the system assigns Lights at regular intervals according to the <see cref="ParticleSystem.LightsModule.ratio" />.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            /// // For best results, use Deferred Rendering (see Camera settings)
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public Light lightPrefab;     // Provide a light Prefab in the inspector
            ///    public float hSliderValueRatio = 1.0f;
            ///    public float hSliderValueMax = 1000.0f;
            ///    public bool randomDistribution = true;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///        ps.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 45.0f);
            ///
            ///        var shape = ps.shape;
            ///        shape.shapeType = ParticleSystemShapeType.Circle;
            ///
            ///        var lights = ps.lights;
            ///        lights.enabled = true;
            ///        lights.light = lightPrefab;
            ///
            ///        // plane to receive lights
            ///        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ///        plane.transform.parent = ps.transform;
            ///        plane.transform.localPosition = new Vector3(0.0f, 0.0f, 3.0f);
            ///        plane.transform.localScale = new Vector3(20.0f, 20.0f, 20.0f);
            ///        plane.transform.localRotation = Quaternion.Euler(-90.0f, 0.0f, 0.0f);
            ///
            ///        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            ///        material.color = new Color(0.1f, 0.1f, 0.1f, 1.0f);
            ///        plane.GetComponent<MeshRenderer>().material = material;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var lights = ps.lights;
            ///        lights.ratio = hSliderValueRatio;
            ///        lights.maxLights = (int)hSliderValueMax;
            ///        lights.useRandomDistribution = randomDistribution;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Ratio");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Max Lights");
            ///
            ///        hSliderValueRatio = GUI.HorizontalSlider(new Rect(95, 45, 100, 30), hSliderValueRatio, 0.0f, 1.0f);
            ///        hSliderValueMax = GUI.HorizontalSlider(new Rect(95, 85, 100, 30), hSliderValueMax, 0.0f, 500.0f);
            ///
            ///        randomDistribution = GUI.Toggle(new Rect(25, 125, 400, 30), randomDistribution, "Randomly assign Lights to Particles instead of evenly distributing them.");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public bool useRandomDistribution { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Select what Light Prefab you want to base your particle lights on.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            /// // For best results, use Deferred Rendering (see Camera settings)
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public Light lightPrefab;     // Provide a light Prefab in the inspector (eg a default Point Light)
            ///    public bool moduleEnabled = true;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///        ps.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 45.0f);
            ///
            ///        var shape = ps.shape;
            ///        shape.shapeType = ParticleSystemShapeType.Circle;
            ///
            ///        var lights = ps.lights;
            ///        lights.light = lightPrefab;
            ///        lights.ratio = 1.0f;
            ///        lights.maxLights = 1000;
            ///
            ///        // plane to receive lights
            ///        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ///        plane.transform.parent = ps.transform;
            ///        plane.transform.localPosition = new Vector3(0.0f, 0.0f, 3.0f);
            ///        plane.transform.localScale = new Vector3(20.0f, 20.0f, 20.0f);
            ///        plane.transform.localRotation = Quaternion.Euler(-90.0f, 0.0f, 0.0f);
            ///
            ///        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            ///        material.color = new Color(0.1f, 0.1f, 0.1f, 1.0f);
            ///        plane.GetComponent<MeshRenderer>().material = material;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var lights = ps.lights;
            ///        lights.enabled = moduleEnabled;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        moduleEnabled = GUI.Toggle(new Rect(25, 45, 200, 30), moduleEnabled, "Enabled");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="Light" />
            extern public Light light { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Toggle whether the particle lights multiply their color by the particle color.</summary>
            ///<remarks>Remember to also set your light color to white, if you want the lights to have the exact same color as the particles.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            /// // For best results, use Deferred Rendering (see Camera settings)
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public Light lightPrefab;     // Provide a light Prefab in the inspector
            ///    public float hSliderValueR = 1.0f;
            ///    public float hSliderValueG = 1.0f;
            ///    public float hSliderValueB = 1.0f;
            ///    public float hSliderValueA = 1.0f;
            ///    public bool useParticleColor = true;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///        ps.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 45.0f);
            ///
            ///        var shape = ps.shape;
            ///        shape.shapeType = ParticleSystemShapeType.Circle;
            ///
            ///        var lights = ps.lights;
            ///        lights.enabled = true;
            ///        lights.light = lightPrefab;
            ///        lights.ratio = 1.0f;
            ///        lights.maxLights = 1000;
            ///
            ///        // plane to receive lights
            ///        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ///        plane.transform.parent = ps.transform;
            ///        plane.transform.localPosition = new Vector3(0.0f, 0.0f, 3.0f);
            ///        plane.transform.localScale = new Vector3(20.0f, 20.0f, 20.0f);
            ///        plane.transform.localRotation = Quaternion.Euler(-90.0f, 0.0f, 0.0f);
            ///
            ///        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            ///        material.color = new Color(0.1f, 0.1f, 0.1f, 1.0f);
            ///        plane.GetComponent<MeshRenderer>().material = material;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var lights = ps.lights;
            ///        lights.useParticleColor = useParticleColor;
            ///
            ///        var main = ps.main;
            ///        main.startColor = new Color(hSliderValueR, hSliderValueG, hSliderValueB, hSliderValueA);
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Red");
            ///        GUI.Label(new Rect(25, 70, 100, 30), "Green");
            ///        GUI.Label(new Rect(25, 100, 100, 30), "Blue");
            ///        GUI.Label(new Rect(25, 130, 100, 30), "Alpha");
            ///
            ///        hSliderValueR = GUI.HorizontalSlider(new Rect(95, 45, 100, 30), hSliderValueR, 0.0f, 1.0f);
            ///        hSliderValueG = GUI.HorizontalSlider(new Rect(95, 75, 100, 30), hSliderValueG, 0.0f, 1.0f);
            ///        hSliderValueB = GUI.HorizontalSlider(new Rect(95, 105, 100, 30), hSliderValueB, 0.0f, 1.0f);
            ///        hSliderValueA = GUI.HorizontalSlider(new Rect(95, 135, 100, 30), hSliderValueA, 0.0f, 1.0f);
            ///
            ///        useParticleColor = GUI.Toggle(new Rect(25, 175, 200, 30), useParticleColor, "Use Particle Color for Light Color");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public bool useParticleColor { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Toggle whether the system multiplies the particle size by the light range to determine the final light range.</summary>
            ///<remarks>This is useful for shrinking light influence at the same time as particles disappear, to avoid popping.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            /// // For best results, use Deferred Rendering (see Camera settings)
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public Light lightPrefab;     // Provide a light Prefab in the inspector (eg a default Point Light)
            ///    public float hSliderValueSize = 1.0f;
            ///    public bool sizeAffectsRange = true;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///        ps.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 45.0f);
            ///
            ///        var shape = ps.shape;
            ///        shape.shapeType = ParticleSystemShapeType.Circle;
            ///
            ///        var lights = ps.lights;
            ///        lights.enabled = true;
            ///        lights.light = lightPrefab;
            ///        lights.ratio = 1.0f;
            ///        lights.maxLights = 1000;
            ///
            ///        // plane to receive lights
            ///        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ///        plane.transform.parent = ps.transform;
            ///        plane.transform.localPosition = new Vector3(0.0f, 0.0f, 3.0f);
            ///        plane.transform.localScale = new Vector3(20.0f, 20.0f, 20.0f);
            ///        plane.transform.localRotation = Quaternion.Euler(-90.0f, 0.0f, 0.0f);
            ///
            ///        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            ///        material.color = new Color(0.1f, 0.1f, 0.1f, 1.0f);
            ///        plane.GetComponent<MeshRenderer>().material = material;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var lights = ps.lights;
            ///        lights.sizeAffectsRange = sizeAffectsRange;
            ///
            ///        var main = ps.main;
            ///        main.startSize = hSliderValueSize;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Size");
            ///
            ///        hSliderValueSize = GUI.HorizontalSlider(new Rect(95, 45, 100, 30), hSliderValueSize, 0.0f, 1.0f);
            ///        sizeAffectsRange = GUI.Toggle(new Rect(25, 85, 200, 30), sizeAffectsRange, "Particle Size Affects Light Range");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public bool sizeAffectsRange { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Toggle whether the system multiplies the particle alpha by the light intensity when it computes the final light intensity.</summary>
            ///<remarks>This is useful to fade the Lights out at the same time as the particles, to avoid popping.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            /// // For best results, use Deferred Rendering (see Camera settings)
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public Light lightPrefab;     // Provide a light Prefab in the inspector (eg a default Point Light)
            ///    public float hSliderValueA = 1.0f;
            ///    public bool alphaAffectsIntensity = true;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///        ps.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 45.0f);
            ///
            ///        var shape = ps.shape;
            ///        shape.shapeType = ParticleSystemShapeType.Circle;
            ///
            ///        var lights = ps.lights;
            ///        lights.enabled = true;
            ///        lights.light = lightPrefab;
            ///        lights.ratio = 1.0f;
            ///        lights.maxLights = 1000;
            ///
            ///        // plane to receive lights
            ///        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ///        plane.transform.parent = ps.transform;
            ///        plane.transform.localPosition = new Vector3(0.0f, 0.0f, 3.0f);
            ///        plane.transform.localScale = new Vector3(20.0f, 20.0f, 20.0f);
            ///        plane.transform.localRotation = Quaternion.Euler(-90.0f, 0.0f, 0.0f);
            ///
            ///        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            ///        material.color = new Color(0.1f, 0.1f, 0.1f, 1.0f);
            ///        plane.GetComponent<MeshRenderer>().material = material;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var lights = ps.lights;
            ///        lights.alphaAffectsIntensity = alphaAffectsIntensity;
            ///
            ///        var main = ps.main;
            ///        main.startColor = new Color(1.0f, 1.0f, 1.0f, hSliderValueA);
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Alpha");
            ///
            ///        hSliderValueA = GUI.HorizontalSlider(new Rect(95, 45, 100, 30), hSliderValueA, 0.0f, 1.0f);
            ///        alphaAffectsIntensity = GUI.Toggle(new Rect(25, 85, 200, 30), alphaAffectsIntensity, "Particle Alpha Affects Light Intensity");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public bool alphaAffectsIntensity { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Define a curve to apply custom range scaling to particle Lights.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            /// // For best results, use Deferred Rendering (see Camera settings)
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public Light lightPrefab;     // Provide a light Prefab in the inspector (eg a default Point Light)
            ///    public float hSliderValueIntensity = 1.0f;
            ///    public float hSliderValueRange = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///        ps.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 45.0f);
            ///
            ///        var shape = ps.shape;
            ///        shape.shapeType = ParticleSystemShapeType.Circle;
            ///
            ///        var lights = ps.lights;
            ///        lights.enabled = true;
            ///        lights.light = lightPrefab;
            ///        lights.ratio = 1.0f;
            ///        lights.maxLights = 1000;
            ///
            ///        // plane to receive lights
            ///        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ///        plane.transform.parent = ps.transform;
            ///        plane.transform.localPosition = new Vector3(0.0f, 0.0f, 3.0f);
            ///        plane.transform.localScale = new Vector3(20.0f, 20.0f, 20.0f);
            ///        plane.transform.localRotation = Quaternion.Euler(-90.0f, 0.0f, 0.0f);
            ///
            ///        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            ///        material.color = new Color(0.1f, 0.1f, 0.1f, 1.0f);
            ///        plane.GetComponent<MeshRenderer>().material = material;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var lights = ps.lights;
            ///        lights.intensity = hSliderValueIntensity;
            ///        lights.range = hSliderValueRange;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Intensity");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Range");
            ///        hSliderValueIntensity = GUI.HorizontalSlider(new Rect(95, 45, 100, 30), hSliderValueIntensity, 0.0f, 10.0f);
            ///        hSliderValueRange = GUI.HorizontalSlider(new Rect(95, 85, 100, 30), hSliderValueRange, 0.0f, 10.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve range { get => rangeBlittable; set => rangeBlittable = value; }
            [NativeName("Range")] private extern MinMaxCurveBlittable rangeBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Range multiplier.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall range multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            /// // For best results, use Deferred Rendering (see Camera settings)
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public Light lightPrefab;     // Provide a light Prefab in the inspector (eg a default Point Light)
            ///    public float hSliderValueIntensity = 1.0f;
            ///    public float hSliderValueRange = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///        ps.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 45.0f);
            ///
            ///        var shape = ps.shape;
            ///        shape.shapeType = ParticleSystemShapeType.Circle;
            ///
            ///        var lights = ps.lights;
            ///        lights.enabled = true;
            ///        lights.light = lightPrefab;
            ///        lights.ratio = 1.0f;
            ///        lights.maxLights = 1000;
            ///
            ///        // plane to receive lights
            ///        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ///        plane.transform.parent = ps.transform;
            ///        plane.transform.localPosition = new Vector3(0.0f, 0.0f, 3.0f);
            ///        plane.transform.localScale = new Vector3(20.0f, 20.0f, 20.0f);
            ///        plane.transform.localRotation = Quaternion.Euler(-90.0f, 0.0f, 0.0f);
            ///
            ///        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            ///        material.color = new Color(0.1f, 0.1f, 0.1f, 1.0f);
            ///        plane.GetComponent<MeshRenderer>().material = material;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var lights = ps.lights;
            ///        lights.intensityMultiplier = hSliderValueIntensity;
            ///        lights.rangeMultiplier = hSliderValueRange;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Intensity");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Range");
            ///        hSliderValueIntensity = GUI.HorizontalSlider(new Rect(95, 45, 100, 30), hSliderValueIntensity, 0.0f, 10.0f);
            ///        hSliderValueRange = GUI.HorizontalSlider(new Rect(95, 85, 100, 30), hSliderValueRange, 0.0f, 10.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.LightsModule.range" />
            extern public float rangeMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Define a curve to apply custom intensity scaling to particle Lights.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            /// // For best results, use Deferred Rendering (see Camera settings)
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public Light lightPrefab;     // Provide a light Prefab in the inspector (eg a default Point Light)
            ///    public float hSliderValueIntensity = 1.0f;
            ///    public float hSliderValueRange = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///        ps.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 45.0f);
            ///
            ///        var shape = ps.shape;
            ///        shape.shapeType = ParticleSystemShapeType.Circle;
            ///
            ///        var lights = ps.lights;
            ///        lights.enabled = true;
            ///        lights.light = lightPrefab;
            ///        lights.ratio = 1.0f;
            ///        lights.maxLights = 1000;
            ///
            ///        // plane to receive lights
            ///        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ///        plane.transform.parent = ps.transform;
            ///        plane.transform.localPosition = new Vector3(0.0f, 0.0f, 3.0f);
            ///        plane.transform.localScale = new Vector3(20.0f, 20.0f, 20.0f);
            ///        plane.transform.localRotation = Quaternion.Euler(-90.0f, 0.0f, 0.0f);
            ///
            ///        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            ///        material.color = new Color(0.1f, 0.1f, 0.1f, 1.0f);
            ///        plane.GetComponent<MeshRenderer>().material = material;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var lights = ps.lights;
            ///        lights.intensity = hSliderValueIntensity;
            ///        lights.range = hSliderValueRange;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Intensity");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Range");
            ///        hSliderValueIntensity = GUI.HorizontalSlider(new Rect(95, 45, 100, 30), hSliderValueIntensity, 0.0f, 10.0f);
            ///        hSliderValueRange = GUI.HorizontalSlider(new Rect(95, 85, 100, 30), hSliderValueRange, 0.0f, 10.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve intensity { get => intensityBlittable; set => intensityBlittable = value; }
            [NativeName("Intensity")] private extern MinMaxCurveBlittable intensityBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Intensity multiplier.</summary>
            ///<remarks>Changing this property is more efficient than accessing the entire curve, if you only want to change the overall intensity multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            /// // For best results, use Deferred Rendering (see Camera settings)
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public Light lightPrefab;     // Provide a light Prefab in the inspector (eg a default Point Light)
            ///    public float hSliderValueIntensity = 1.0f;
            ///    public float hSliderValueRange = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///        ps.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 45.0f);
            ///
            ///        var shape = ps.shape;
            ///        shape.shapeType = ParticleSystemShapeType.Circle;
            ///
            ///        var lights = ps.lights;
            ///        lights.enabled = true;
            ///        lights.light = lightPrefab;
            ///        lights.ratio = 1.0f;
            ///        lights.maxLights = 1000;
            ///
            ///        // plane to receive lights
            ///        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ///        plane.transform.parent = ps.transform;
            ///        plane.transform.localPosition = new Vector3(0.0f, 0.0f, 3.0f);
            ///        plane.transform.localScale = new Vector3(20.0f, 20.0f, 20.0f);
            ///        plane.transform.localRotation = Quaternion.Euler(-90.0f, 0.0f, 0.0f);
            ///
            ///        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            ///        material.color = new Color(0.1f, 0.1f, 0.1f, 1.0f);
            ///        plane.GetComponent<MeshRenderer>().material = material;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var lights = ps.lights;
            ///        lights.intensityMultiplier = hSliderValueIntensity;
            ///        lights.rangeMultiplier = hSliderValueRange;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Intensity");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Range");
            ///        hSliderValueIntensity = GUI.HorizontalSlider(new Rect(95, 45, 100, 30), hSliderValueIntensity, 0.0f, 10.0f);
            ///        hSliderValueRange = GUI.HorizontalSlider(new Rect(95, 85, 100, 30), hSliderValueRange, 0.0f, 10.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.LightsModule.intensity" />
            extern public float intensityMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Set a limit on how many Lights this Module can create.</summary>
            ///<remarks>This value is useful for avoiding bad performance. It is very easy to create a high number of Lights, which negatively effects performance. Capping the number of Lights helps to maintain performance.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            /// // For best results, use Deferred Rendering (see Camera settings)
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public Light lightPrefab;     // Provide a light Prefab in the inspector
            ///    public float hSliderValueRatio = 1.0f;
            ///    public float hSliderValueMax = 1000.0f;
            ///    public bool randomDistribution = true;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///        ps.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 45.0f);
            ///
            ///        var shape = ps.shape;
            ///        shape.shapeType = ParticleSystemShapeType.Circle;
            ///
            ///        var lights = ps.lights;
            ///        lights.enabled = true;
            ///        lights.light = lightPrefab;
            ///
            ///        // plane to receive lights
            ///        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ///        plane.transform.parent = ps.transform;
            ///        plane.transform.localPosition = new Vector3(0.0f, 0.0f, 3.0f);
            ///        plane.transform.localScale = new Vector3(20.0f, 20.0f, 20.0f);
            ///        plane.transform.localRotation = Quaternion.Euler(-90.0f, 0.0f, 0.0f);
            ///
            ///        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            ///        material.color = new Color(0.1f, 0.1f, 0.1f, 1.0f);
            ///        plane.GetComponent<MeshRenderer>().material = material;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var lights = ps.lights;
            ///        lights.ratio = hSliderValueRatio;
            ///        lights.maxLights = (int)hSliderValueMax;
            ///        lights.useRandomDistribution = randomDistribution;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Ratio");
            ///        GUI.Label(new Rect(25, 80, 100, 30), "Max Lights");
            ///
            ///        hSliderValueRatio = GUI.HorizontalSlider(new Rect(95, 45, 100, 30), hSliderValueRatio, 0.0f, 1.0f);
            ///        hSliderValueMax = GUI.HorizontalSlider(new Rect(95, 85, 100, 30), hSliderValueMax, 0.0f, 500.0f);
            ///
            ///        randomDistribution = GUI.Toggle(new Rect(25, 125, 400, 30), randomDistribution, "Randomly assign Lights to Particles instead of evenly distributing them.");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public int maxLights { get; [NativeMethod(ThrowsException = true)] set; }
        }

        ///<summary>Script interface for the TrailsModule.</summary>
        ///<remarks>This module adds trails to your particles. For example, you can make the trails stay in the wake of particles as they move, or make them connect each particle in the system together.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        ParticleSystem ps = GetComponent<ParticleSystem>();
        ///        var trails = ps.trails;
        ///        trails.enabled = true;
        ///        trails.ratio = 0.5f;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="ParticleSystem" />
        ///<seealso cref="ParticleSystem.trails" />
        public partial struct TrailModule
        {
            internal TrailModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            ///<summary>Specifies whether the TrailModule is enabled or disabled.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueRatio = 1.0f;
            ///
            ///    void Start()
            ///    {
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
            ///    void Update()
            ///    {
            ///        var trails = ps.trails;
            ///        trails.ratio = hSliderValueRatio;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Ratio");
            ///
            ///        hSliderValueRatio = GUI.HorizontalSlider(new Rect(55, 45, 100, 30), hSliderValueRatio, 0.0f, 1.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.trails" />
            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Choose how the system generates the particle trails.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool ribbonMode = false;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startColor = new ParticleSystem.MinMaxGradient(Color.red, Color.yellow);
            ///        main.startSize = 0.1f;
            ///        main.startLifetime = 2.5f;
            ///        main.gravityModifierMultiplier = 0.2f;
            ///
            ///        var trails = ps.trails;
            ///        trails.enabled = true;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.trailMaterial = new Material(Shader.Find("Sprites/Default"));
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var trails = ps.trails;
            ///        trails.mode = ribbonMode ? ParticleSystemTrailMode.Ribbon : ParticleSystemTrailMode.PerParticle;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        ribbonMode = GUI.Toggle(new Rect(25, 25, 200, 30), ribbonMode, "Ribbon mode");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystemTrailMode" />
            extern public ParticleSystemTrailMode mode { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Choose what proportion of particles receive a trail.</summary>
            ///<remarks>Use a value between 0 and 1, where 0 doesn't add trails to any particles, and 1 adds trails to every particle.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueRatio = 1.0f;
            ///
            ///    void Start()
            ///    {
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
            ///    void Update()
            ///    {
            ///        var trails = ps.trails;
            ///        trails.ratio = hSliderValueRatio;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Ratio");
            ///
            ///        hSliderValueRatio = GUI.HorizontalSlider(new Rect(55, 45, 100, 30), hSliderValueRatio, 0.0f, 1.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float ratio { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>The curve describing the trail lifetime, throughout the lifetime of the particle.</summary>
            ///<remarks>This value is relative to the particle lifetime.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueLifetime = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startColor = new ParticleSystem.MinMaxGradient(Color.red, Color.yellow);
            ///        main.startSize = 0.1f;
            ///        main.startLifetime = 1.5f;
            ///
            ///        var trails = ps.trails;
            ///        trails.enabled = true;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.trailMaterial = new Material(Shader.Find("Sprites/Default"));
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var trails = ps.trails;
            ///        trails.lifetime = hSliderValueLifetime;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Lifetime");
            ///
            ///        hSliderValueLifetime = GUI.HorizontalSlider(new Rect(95, 45, 100, 30), hSliderValueLifetime, 0.0f, 1.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve lifetime { get => lifetimeBlittable; set => lifetimeBlittable = value; }
            [NativeName("Lifetime")] private extern MinMaxCurveBlittable lifetimeBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>A multiplier for <see cref="ParticleSystem.TrailModule.lifetime" />.</summary>
            ///<remarks>This is more efficient than accessing the whole curve, if you only want to change the overall lifetime multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueLifetime = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startColor = new ParticleSystem.MinMaxGradient(Color.red, Color.yellow);
            ///        main.startSize = 0.1f;
            ///        main.startLifetime = 1.5f;
            ///
            ///        var trails = ps.trails;
            ///        trails.enabled = true;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.trailMaterial = new Material(Shader.Find("Sprites/Default"));
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var trails = ps.trails;
            ///        trails.lifetimeMultiplier = hSliderValueLifetime;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Lifetime");
            ///
            ///        hSliderValueLifetime = GUI.HorizontalSlider(new Rect(95, 45, 100, 30), hSliderValueLifetime, 0.0f, 1.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float lifetimeMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Set the minimum distance each trail can travel before the system adds a new vertex to it.</summary>
            ///<remarks>Smaller values give smoother trails that consist of more vertices. Smoother trails require more memory, and are more performance-intensive to update.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueDistance = 0.2f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startColor = new ParticleSystem.MinMaxGradient(Color.red, Color.yellow);
            ///        main.startSize = 0.1f;
            ///        main.startLifetime = 2.5f;
            ///        main.gravityModifier = 0.35f;
            ///
            ///        var trails = ps.trails;
            ///        trails.enabled = true;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.trailMaterial = new Material(Shader.Find("Sprites/Default"));
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var trails = ps.trails;
            ///        trails.minVertexDistance = hSliderValueDistance;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Vertex Distance");
            ///
            ///        hSliderValueDistance = GUI.HorizontalSlider(new Rect(125, 45, 100, 30), hSliderValueDistance, 0.01f, 1.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float minVertexDistance { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Choose whether the U coordinate of the trail Texture is tiled or stretched.</summary>
            ///<remarks>Stretching maps the texture along the trail with no repeats. Tiling maps the texture along the trail with repeats every world unit. To change the repeat rate, use <see cref="ParticleSystem.TrailModule.textureScale" /> or <see cref="Material.SetTextureScale" />.</remarks>
            extern public ParticleSystemTrailTextureMode textureMode { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>A multiplier for the UV coordinates of the trail texture.</summary>
            extern public Vector2 textureScale { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Drop new trail points in world space, regardless of Particle System Simulation Space.</summary>
            ///<remarks>When set to true, trails are always in world space, and do not move relative to the Transform component.
            ///When set to false, trails move with the Particle System Transform, if also using local Simulation Space.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool worldSpace = true;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startColor = new ParticleSystem.MinMaxGradient(Color.red, Color.yellow);
            ///        main.startSize = 0.1f;
            ///        main.startLifetime = 0.5f;
            ///
            ///        var trails = ps.trails;
            ///        trails.enabled = true;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.trailMaterial = new Material(Shader.Find("Sprites/Default"));
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        ps.transform.position = new Vector3(Mathf.Sin(Time.time * 2.0f) * 2.0f, 0.0f, 0.0f);
            ///
            ///        var trails = ps.trails;
            ///        trails.worldSpace = worldSpace;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        worldSpace = GUI.Toggle(new Rect(25, 25, 200, 30), worldSpace, "World Space");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MainModule.simulationSpace" />
            extern public bool worldSpace { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Specifies whether trails disappear immediately when their owning particle dies. When false, each trail persists until all its points have naturally expired, based on its lifetime.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool dieWithParticles = true;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startColor = new ParticleSystem.MinMaxGradient(Color.red, Color.yellow);
            ///        main.startSizeMultiplier = 0.1f;
            ///        main.startLifetimeMultiplier = 1.0f;
            ///
            ///        var trails = ps.trails;
            ///        trails.enabled = true;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.trailMaterial = new Material(Shader.Find("Sprites/Default"));
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var trails = ps.trails;
            ///        trails.dieWithParticles = dieWithParticles;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        dieWithParticles = GUI.Toggle(new Rect(25, 25, 200, 30), dieWithParticles, "Die With Particles");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public bool dieWithParticles { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Set whether the particle size acts as a multiplier on top of the trail width.</summary>
            ///<remarks>Useful for making larger particles have wider trails.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool sizeAffectsWidth = true;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startColor = new ParticleSystem.MinMaxGradient(Color.red, Color.yellow);
            ///        main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 1.0f);
            ///        main.startLifetime = 1.5f;
            ///
            ///        var trails = ps.trails;
            ///        trails.enabled = true;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.trailMaterial = new Material(Shader.Find("Sprites/Default"));
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var trails = ps.trails;
            ///        trails.sizeAffectsWidth = sizeAffectsWidth;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        sizeAffectsWidth = GUI.Toggle(new Rect(25, 25, 200, 30), sizeAffectsWidth, "Size affects width");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public bool sizeAffectsWidth { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Set whether the particle size acts as a multiplier on top of the trail lifetime.</summary>
            ///<remarks>Useful for making larger particles have longer trails.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool sizeAffectsLifetime = true;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startColor = new ParticleSystem.MinMaxGradient(Color.red, Color.yellow);
            ///        main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 1.0f);
            ///        main.startLifetime = 1.5f;
            ///
            ///        var trails = ps.trails;
            ///        trails.enabled = true;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.trailMaterial = new Material(Shader.Find("Sprites/Default"));
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var trails = ps.trails;
            ///        trails.sizeAffectsLifetime = sizeAffectsLifetime;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        sizeAffectsLifetime = GUI.Toggle(new Rect(25, 25, 200, 30), sizeAffectsLifetime, "Size affects lifetime");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public bool sizeAffectsLifetime { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Toggle whether the trail inherits the particle color as its starting color.</summary>
            ///<remarks>When enabled, this applies any Trail module color overrides on top of the particle color.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool inheritParticleColor = true;
            ///
            ///    void Start()
            ///    {
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
            ///    void Update()
            ///    {
            ///        var trails = ps.trails;
            ///        trails.inheritParticleColor = inheritParticleColor;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        inheritParticleColor = GUI.Toggle(new Rect(25, 25, 200, 30), inheritParticleColor, "Inherit Particle Color");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public bool inheritParticleColor { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>The gradient that controls the trail colors during the lifetime of the attached particle.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    private Gradient gradient = new Gradient();
            ///    public bool swapColors = true;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startSize = 0.1f;
            ///        main.startLifetime = 1.5f;
            ///
            ///        var trails = ps.trails;
            ///        trails.enabled = true;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.trailMaterial = new Material(Shader.Find("Sprites/Default"));
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var trails = ps.trails;
            ///        if (swapColors)
            ///        {
            ///            gradient.SetKeys(
            ///                new GradientColorKey[] { new GradientColorKey(Color.blue, 0.0f), new GradientColorKey(Color.green, 1.0f) },
            ///                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///            );
            ///        }
            ///        else
            ///        {
            ///            gradient.SetKeys(
            ///                new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.blue, 1.0f) },
            ///                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///            );
            ///        }
            ///
            ///        trails.colorOverLifetime = gradient;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        swapColors = GUI.Toggle(new Rect(25, 25, 200, 30), swapColors, "Swap Trail Colors");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxGradient" />
            public MinMaxGradient colorOverLifetime { get => colorOverLifetimeBlittable; set => colorOverLifetimeBlittable = value; }
            [NativeName("ColorOverLifetime")] private extern MinMaxGradientBlittable colorOverLifetimeBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>The curve describing the width of each trail point.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    private AnimationCurve curve = new AnimationCurve();
            ///    public bool widthOverTrail = true;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startColor = new ParticleSystem.MinMaxGradient(Color.red, Color.yellow);
            ///        main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 1.0f);
            ///        main.startLifetime = 1.5f;
            ///
            ///        var trails = ps.trails;
            ///        trails.enabled = true;
            ///
            ///        curve.AddKey(0.0f, 1.0f);
            ///        curve.AddKey(1.0f, 0.0f);
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.trailMaterial = new Material(Shader.Find("Sprites/Default"));
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var trails = ps.trails;
            ///        if (widthOverTrail)
            ///            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1.0f, curve);
            ///        else
            ///            trails.widthOverTrail = 1.0f;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        widthOverTrail = GUI.Toggle(new Rect(25, 25, 200, 30), widthOverTrail, "Width over Trail");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            public MinMaxCurve widthOverTrail { get => widthOverTrailBlittable; set => widthOverTrailBlittable = value; }
            [NativeName("WidthOverTrail")] private extern MinMaxCurveBlittable widthOverTrailBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>A multiplier for <see cref="ParticleSystem.TrailModule.widthOverTrail" />.</summary>
            ///<remarks>This is more efficient than accessing the whole curve, if you only want to change the overall width multiplier.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool widthOverTrail = true;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startColor = new ParticleSystem.MinMaxGradient(Color.red, Color.yellow);
            ///        main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 1.0f);
            ///        main.startLifetime = 1.5f;
            ///
            ///        var trails = ps.trails;
            ///        trails.enabled = true;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.trailMaterial = new Material(Shader.Find("Sprites/Default"));
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var trails = ps.trails;
            ///        if (widthOverTrail)
            ///            trails.widthOverTrailMultiplier = 0.2f;
            ///        else
            ///            trails.widthOverTrailMultiplier = 1.0f;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        widthOverTrail = GUI.Toggle(new Rect(25, 25, 200, 30), widthOverTrail, "Width over Trail");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float widthOverTrailMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>The gradient that controls the trail colors over the length of the trail.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    private Gradient gradient = new Gradient();
            ///    public bool swapColors = true;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startSize = 0.1f;
            ///        main.startLifetime = 1.5f;
            ///
            ///        var trails = ps.trails;
            ///        trails.enabled = true;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.trailMaterial = new Material(Shader.Find("Sprites/Default"));
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var trails = ps.trails;
            ///        if (swapColors)
            ///        {
            ///            gradient.SetKeys(
            ///                new GradientColorKey[] { new GradientColorKey(Color.blue, 0.0f), new GradientColorKey(Color.green, 1.0f) },
            ///                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///            );
            ///        }
            ///        else
            ///        {
            ///            gradient.SetKeys(
            ///                new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.blue, 1.0f) },
            ///                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            ///            );
            ///        }
            ///
            ///        trails.colorOverTrail = gradient;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        swapColors = GUI.Toggle(new Rect(25, 25, 200, 30), swapColors, "Swap Trail Colors");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.MinMaxGradient" />
            public MinMaxGradient colorOverTrail { get => colorOverTrailBlittable; set => colorOverTrailBlittable = value; }
            [NativeName("ColorOverTrail")] private extern MinMaxGradientBlittable colorOverTrailBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Configures the trails to generate Normals and Tangents. With this data, Scene lighting can affect the trails via Normal Maps and the Unity Standard Shader, or your own custom-built Shaders.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startSize = 0.1f;
            ///
            ///        var trails = ps.trails;
            ///        trails.enabled = true;
            ///        trails.generateLightingData = true;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.trailMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Lit"));
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public bool generateLightingData { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Select how many lines to create through the Particle System.</summary>
            ///<remarks>For example, if using 1 ribbon, this draws a single line, starting at the youngest particle and travelling through each subsequent particle based on their ages, until finally reaching the oldest. If using a value larger than 1, this draws multiple lines, connecting each Nth oldest particle. For example, if using 3 ribbons, this conntects every 3rd particle (1,4,7, and 2,5,8 and 3,6,9).</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueRibbonCount = 1.0f;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startColor = new ParticleSystem.MinMaxGradient(Color.red, Color.yellow);
            ///        main.startSize = 0.1f;
            ///        main.startLifetime = 2.5f;
            ///
            ///        var trails = ps.trails;
            ///        trails.enabled = true;
            ///        trails.mode = ParticleSystemTrailMode.Ribbon;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.trailMaterial = new Material(Shader.Find("Sprites/Default"));
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var trails = ps.trails;
            ///        trails.ribbonCount = (int)hSliderValueRibbonCount;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Ribbon Count");
            ///
            ///        hSliderValueRibbonCount = GUI.HorizontalSlider(new Rect(125, 45, 100, 30), hSliderValueRibbonCount, 1.0f, 4.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public int ribbonCount { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Apply a shadow bias to prevent self-shadowing artifacts. The specified value is the proportion of the trail width at each segment.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public float hSliderValueBias = 1.0f;
            ///
            ///    void Start()
            ///    {
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
            ///    void Update()
            ///    {
            ///        var trails = ps.trails;
            ///        trails.shadowBias = hSliderValueBias;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        GUI.Label(new Rect(25, 40, 100, 30), "Shadow Bias");
            ///
            ///        hSliderValueBias = GUI.HorizontalSlider(new Rect(55, 45, 100, 30), hSliderValueBias, 0.0f, 1.0f);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public float shadowBias { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Specifies whether, if you use this system as a sub-emitter, ribbons connect particles from each parent particle independently.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    private ParticleSystem sps;
            ///    public bool splitSubEmitterRibbons = true;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startColor = new ParticleSystem.MinMaxGradient(Color.red, Color.yellow);
            ///        main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 1.0f);
            ///        main.startLifetime = 1.5f;
            ///
            ///        sps = new GameObject("SubEmitter", typeof(ParticleSystem)).GetComponent<ParticleSystem>();
            ///        sps.transform.parent = ps.transform;
            ///
            ///        var sub = ps.subEmitters;
            ///        sub.enabled = true;
            ///        sub.AddSubEmitter(sps, ParticleSystemSubEmitterType.Birth, ParticleSystemSubEmitterProperties.InheritColor);
            ///
            ///        var smain = sps.main;
            ///        smain.startSpeed = 0.0f;
            ///
            ///        var sshape = sps.shape;
            ///        sshape.enabled = false;
            ///
            ///        var strails = sps.trails;
            ///        strails.enabled = true;
            ///        strails.mode = ParticleSystemTrailMode.Ribbon;
            ///        strails.widthOverTrail = 0.1f;
            ///
            ///        var spsr = sps.GetComponent<ParticleSystemRenderer>();
            ///        spsr.renderMode = ParticleSystemRenderMode.None;
            ///        spsr.trailMaterial = new Material(Shader.Find("Sprites/Default"));
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var strails = sps.trails;
            ///        strails.splitSubEmitterRibbons = splitSubEmitterRibbons;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        splitSubEmitterRibbons = GUI.Toggle(new Rect(25, 25, 200, 30), splitSubEmitterRibbons, "Split Sub Emitter Ribbons");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public bool splitSubEmitterRibbons { get; [NativeMethod(ThrowsException = true)] set; }
            ///<summary>Adds an extra position to each ribbon, connecting it to the location of the Transform Component.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool attachRibbonsToTransform = true;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///
            ///        var main = ps.main;
            ///        main.startColor = new ParticleSystem.MinMaxGradient(Color.red, Color.yellow);
            ///        main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 1.0f);
            ///        main.startLifetime = 1.5f;
            ///
            ///        var shape = ps.shape;
            ///        shape.radius = 2.0f;
            ///        shape.radiusThickness = 0.0f;
            ///
            ///        var trails = ps.trails;
            ///        trails.enabled = true;
            ///        trails.mode = ParticleSystemTrailMode.Ribbon;
            ///
            ///        var psr = GetComponent<ParticleSystemRenderer>();
            ///        psr.trailMaterial = new Material(Shader.Find("Sprites/Default"));
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var trails = ps.trails;
            ///        trails.attachRibbonsToTransform = attachRibbonsToTransform;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        attachRibbonsToTransform = GUI.Toggle(new Rect(25, 25, 200, 30), attachRibbonsToTransform, "Attach Ribbons To Transform");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            extern public bool attachRibbonsToTransform { get; [NativeMethod(ThrowsException = true)] set; }
        }

        ///<summary>Script interface for CustomDataModule.</summary>
        ///<remarks>Once configured, this module generates custom per-particle data, which you can use either in script or shaders.
        ///To read the data from script, simply call <see cref="ParticleSystem.GetCustomParticleData" />.
        ///To read it in a shader, enable the custom data streams in the <see cref="ParticleSystemRenderer" /> Module, or call <see cref="ParticleSystemRenderer.SetActiveVertexStreams" /> from script. Once enabled, the custom data will be passed to your vertex shader through a TEXCOORD channel. The <see cref="ParticleSystemRenderer" /> Inspector will tell you which channels are being used.</remarks>
        ///<seealso cref="ParticleSystem" />
        ///<seealso cref="ParticleSystem.customData" />
        public partial struct CustomDataModule
        {
            internal CustomDataModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            ///<summary>Specifies whether the CustomDataModule is enabled or disabled.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem ps;
            ///    public bool moduleEnabled;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        var customData = ps.customData;
            ///        customData.enabled = moduleEnabled;
            ///    }
            ///
            ///    void OnGUI()
            ///    {
            ///        moduleEnabled = GUI.Toggle(new Rect(25, 45, 100, 30), moduleEnabled, "Enabled");
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.customData" />
            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            ///<summary>Choose the type of custom data to generate for the chosen data stream.</summary>
            ///<param name="stream">The name of the custom data stream to enable data generation on.</param>
            ///<param name="mode">The type of data to generate.</param>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    void Start()
            ///    {
            ///        ParticleSystem ps = GetComponent<ParticleSystem>();
            ///        var customData = ps.customData;
            ///        customData.enabled = true;
            ///
            ///        Gradient grad = new Gradient();
            ///        grad.SetKeys(new GradientColorKey[] { new GradientColorKey(Color.blue, 0.0f), new GradientColorKey(Color.red, 1.0f) }, new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) });
            ///
            ///        customData.SetMode(ParticleSystemCustomData.Custom1, ParticleSystemCustomDataMode.Color);
            ///        customData.SetColor(ParticleSystemCustomData.Custom1, grad);
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        customData.SetMode(ParticleSystemCustomData.Custom2, ParticleSystemCustomDataMode.Vector);
            ///        customData.SetVectorComponentCount(ParticleSystemCustomData.Custom2, 1);
            ///        customData.SetVector(ParticleSystemCustomData.Custom2, 0, new ParticleSystem.MinMaxCurve(1.0f, curve));
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.CustomDataModule.GetMode" />
            ///<seealso cref="ParticleSystem.GetCustomParticleData" />
            [NativeMethod(ThrowsException = true)]
            extern public void SetMode(ParticleSystemCustomData stream, ParticleSystemCustomDataMode mode);
            ///<summary>Find out the type of custom data that is being generated for the chosen data stream.</summary>
            ///<param name="stream">The name of the custom data stream to query.</param>
            ///<returns>The type of data being generated for the requested stream.</returns>
            ///<seealso cref="ParticleSystem.CustomDataModule.SetMode" />
            ///<seealso cref="ParticleSystem.GetCustomParticleData" />
            [NativeMethod(ThrowsException = true)]
            extern public ParticleSystemCustomDataMode GetMode(ParticleSystemCustomData stream);
            ///<summary>Specify how many curves are used to generate custom data for this stream.</summary>
            ///<param name="stream">The name of the custom data stream to apply the curve to.</param>
            ///<param name="count">The number of curves to generate data for.</param>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    void Start()
            ///    {
            ///        ParticleSystem ps = GetComponent<ParticleSystem>();
            ///        var customData = ps.customData;
            ///        customData.enabled = true;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        customData.SetMode(ParticleSystemCustomData.Custom1, ParticleSystemCustomDataMode.Vector);
            ///        customData.SetVectorComponentCount(ParticleSystemCustomData.Custom1, 1);
            ///        customData.SetVector(ParticleSystemCustomData.Custom1, 0, new ParticleSystem.MinMaxCurve(1.0f, curve));
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.CustomDataModule.GetVectorComponentCount" />
            ///<seealso cref="ParticleSystem.MinMaxCurve" />
            ///<seealso cref="ParticleSystem.GetCustomParticleData" />
            [NativeMethod(ThrowsException = true)]
            extern public void SetVectorComponentCount(ParticleSystemCustomData stream, int count);
            ///<summary>Query how many <see cref="ParticleSystem.MinMaxCurve" /> elements are being used to generate this stream of custom data.</summary>
            ///<param name="stream">The name of the custom data stream to retrieve the curve from.</param>
            ///<returns>The number of curves.</returns>
            ///<seealso cref="ParticleSystem.CustomDataModule.SetVectorComponentCount" />
            ///<seealso cref="ParticleSystem.GetCustomParticleData" />
            [NativeMethod(ThrowsException = true)]
            extern public int GetVectorComponentCount(ParticleSystemCustomData stream);

            ///<summary>Set a <see cref="ParticleSystem.MinMaxCurve" />, in order to generate custom data.</summary>
            ///<param name="stream">The name of the custom data stream to apply the curve to.</param>
            ///<param name="component">The component index to apply the curve to (0-3, mapping to the xyzw components of a Vector4 or float4).</param>
            ///<param name="curve">The curve to be used for generating custom data.</param>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    void Start()
            ///    {
            ///        ParticleSystem ps = GetComponent<ParticleSystem>();
            ///        var customData = ps.customData;
            ///        customData.enabled = true;
            ///
            ///        AnimationCurve curve = new AnimationCurve();
            ///        curve.AddKey(0.0f, 0.0f);
            ///        curve.AddKey(1.0f, 1.0f);
            ///
            ///        customData.SetMode(ParticleSystemCustomData.Custom1, ParticleSystemCustomDataMode.Vector);
            ///        customData.SetVectorComponentCount(ParticleSystemCustomData.Custom1, 1);
            ///        customData.SetVector(ParticleSystemCustomData.Custom1, 0, new ParticleSystem.MinMaxCurve(1.0f, curve));
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.CustomDataModule.GetVector" />
            ///<seealso cref="ParticleSystem.GetCustomParticleData" />
            public void SetVector(ParticleSystemCustomData stream, int component, MinMaxCurve curve)
            {
                SetVectorInternal(stream, component, MinMaxCurveBlittable.FromMixMaxCurve(curve));
            }

            [NativeMethod(ThrowsException = true)]
            private extern void SetVectorInternal(ParticleSystemCustomData stream, int component, MinMaxCurveBlittable curve);

            ///<summary>Get a <see cref="ParticleSystem.MinMaxCurve" />, that is being used to generate custom data.</summary>
            ///<param name="stream">The name of the custom data stream to retrieve the curve from.</param>
            ///<param name="component">The component index to retrieve the curve for (0-3, mapping to the xyzw components of a Vector4 or float4).</param>
            ///<returns>The curve being used to generate custom data.</returns>
            ///<seealso cref="ParticleSystem.CustomDataModule.SetVector" />
            ///<seealso cref="ParticleSystem.GetCustomParticleData" />
            public MinMaxCurve GetVector(ParticleSystemCustomData stream, int component)
            {
                return MinMaxCurveBlittable.ToMinMaxCurve(GetVectorInternal(stream, component));
            }
            [NativeMethod(ThrowsException = true)]
            private extern MinMaxCurveBlittable GetVectorInternal(ParticleSystemCustomData stream, int component);

            ///<summary>Set a <see cref="ParticleSystem.MinMaxGradient" />, in order to generate custom HDR color data.</summary>
            ///<param name="stream">The name of the custom data stream to apply the gradient to.</param>
            ///<param name="gradient">The gradient to be used for generating custom color data.</param>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    void Start()
            ///    {
            ///        ParticleSystem ps = GetComponent<ParticleSystem>();
            ///        var customData = ps.customData;
            ///        customData.enabled = true;
            ///
            ///        Gradient grad = new Gradient();
            ///        grad.SetKeys(new GradientColorKey[] { new GradientColorKey(Color.blue, 0.0f), new GradientColorKey(Color.red, 1.0f) }, new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) });
            ///
            ///        customData.SetMode(ParticleSystemCustomData.Custom1, ParticleSystemCustomDataMode.Color);
            ///        customData.SetColor(ParticleSystemCustomData.Custom1, grad);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.CustomDataModule.GetColor" />
            ///<seealso cref="ParticleSystem.GetCustomParticleData" />
            public void SetColor(ParticleSystemCustomData stream, MinMaxGradient gradient)
            {
                SetColorInternal(stream, MinMaxGradientBlittable.FromMixMaxGradient(gradient));
            }
            [NativeMethod(ThrowsException = true)]
            private extern void SetColorInternal(ParticleSystemCustomData stream, MinMaxGradientBlittable gradient);

            ///<summary>Get a <see cref="ParticleSystem.MinMaxGradient" />, that is being used to generate custom HDR color data.</summary>
            ///<param name="stream">The name of the custom data stream to retrieve the gradient from.</param>
            ///<returns>The color gradient being used to generate custom color data.</returns>
            ///<seealso cref="ParticleSystem.CustomDataModule.SetColor" />
            ///<seealso cref="ParticleSystem.GetCustomParticleData" />
            public MinMaxGradient GetColor(ParticleSystemCustomData stream)
            {
                return MinMaxGradientBlittable.ToMinMaxGradient(GetColorInternal(stream));
            }
            [NativeMethod(ThrowsException = true)]
            extern private MinMaxGradientBlittable GetColorInternal(ParticleSystemCustomData stream);
        }

        // Module Accessors
        ///<summary>Access the main Particle System settings.</summary>
        ///<remarks>This module provides access to the general settings that are displayed above all of the other module settings in the Particle System's Inspector window.
        ///
        ///Particle System modules do not need to be reassigned back to the system; they are interfaces and not independent objects.</remarks>
        ///<example>
        ///  <code><![CDATA[ // Create a Particle System
        /// // Set a 5 second start delay for the system, and a 2 second lifetime for each particle
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        ParticleSystem ps = GetComponent<ParticleSystem>();
        ///        var main = ps.main;
        ///
        ///        main.startDelay = 5.0f;
        ///        main.startLifetime = 2.0f;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public MainModule main { get { return new MainModule(this); } }
        ///<summary>Script interface for the EmissionModule of a Particle System.</summary>
        ///<remarks>This module provides control over how many particles are emitted.
        ///
        ///Particle System modules do not need to be reassigned back to the system; they are interfaces and not independent objects.</remarks>
        ///<example>
        ///  <code><![CDATA[ // Create a Particle System
        /// // At 2 and 4 secs the number of particles are changed to 100, then 200
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        ParticleSystem ps = GetComponent<ParticleSystem>();
        ///        var em = ps.emission;
        ///        em.enabled = true;
        ///
        ///        em.rateOverTime = 20.0f;
        ///
        ///        em.SetBursts(
        ///            new ParticleSystem.Burst[]{
        ///                new ParticleSystem.Burst(2.0f, 100),
        ///                new ParticleSystem.Burst(4.0f, 100)
        ///            });
        ///    }
        ///}]]></code>
        ///</example>
        public EmissionModule emission { get { return new EmissionModule(this); } }
        ///<summary>Script interface for the ShapeModule of a Particle System.</summary>
        ///<remarks>Configures the initial positions and directions of particles.
        ///
        ///Particle System modules do not need to be reassigned back to the system; they are interfaces and not independent objects.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///    public Mesh myMesh;
        ///
        ///    void Start() {
        ///        ParticleSystem ps = GetComponent<ParticleSystem>();
        ///        var sh = ps.shape;
        ///        sh.enabled = true;
        ///        sh.shapeType = ParticleSystemShapeType.Mesh;
        ///        sh.mesh = myMesh;
        ///    }
        ///}]]></code>
        ///</example>
        public ShapeModule shape { get { return new ShapeModule(this); } }
        ///<summary>Script interface for the VelocityOverLifetimeModule of a Particle System.</summary>
        ///<remarks>This module sets the velocity of particles during their lifetime.
        ///
        ///Particle System modules do not need to be reassigned back to the system; they are interfaces and not independent objects.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///    void Start() {
        ///        ParticleSystem ps = GetComponent<ParticleSystem>();
        ///        var vel = ps.velocityOverLifetime;
        ///        vel.enabled = true;
        ///        vel.space = ParticleSystemSimulationSpace.Local;
        ///
        ///        AnimationCurve curve = new AnimationCurve();
        ///        curve.AddKey(0.0f, 1.0f);
        ///        curve.AddKey(1.0f, 0.0f);
        ///        vel.x = new ParticleSystem.MinMaxCurve(10.0f, curve);
        ///    }
        ///}]]></code>
        ///</example>
        public VelocityOverLifetimeModule velocityOverLifetime { get { return new VelocityOverLifetimeModule(this); } }
        ///<summary>Script interface for the LimitVelocityOverLifetimeModule of a Particle System. .</summary>
        ///<remarks>This module reduces particle velocities by either applying drag or simply reducing velocity over time.
        ///
        ///Particle System modules do not need to be reassigned back to the system; they are interfaces and not independent objects.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///    void Start() {
        ///        ParticleSystem ps = GetComponent<ParticleSystem>();
        ///        var lv = ps.limitVelocityOverLifetime;
        ///        lv.enabled = true;
        ///        lv.dampen = 0.5f;
        ///
        ///        AnimationCurve curve = new AnimationCurve();
        ///        curve.AddKey(0.0f, 1.0f);
        ///        curve.AddKey(1.0f, 0.0f);
        ///        lv.limit = new ParticleSystem.MinMaxCurve(10.0f, curve);
        ///    }
        ///}]]></code>
        ///</example>
        public LimitVelocityOverLifetimeModule limitVelocityOverLifetime { get { return new LimitVelocityOverLifetimeModule(this); } }
        ///<summary>Script interface for the InheritVelocityModule of a Particle System.</summary>
        ///<remarks>This module applies velocities to particles based on the velocity of the object that spawned them. For most Particle Systems, this is the GameObject velocity, but for sub-emitters, the velocity comes from the parent particle that the sub-emitter particle originated from.
        ///
        ///Particle System modules do not need to be reassigned back to the system; they are interfaces and not independent objects.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///    void Start() {
        ///        ParticleSystem ps = GetComponent<ParticleSystem>();
        ///        var iv = ps.inheritVelocity;
        ///        iv.enabled = true;
        ///
        ///        AnimationCurve curve = new AnimationCurve();
        ///        curve.AddKey(0.0f, 1.0f);
        ///        curve.AddKey(1.0f, 0.0f);
        ///        iv.curve = new ParticleSystem.MinMaxCurve(1.0f, curve);
        ///    }
        ///}]]></code>
        ///</example>
        public InheritVelocityModule inheritVelocity { get { return new InheritVelocityModule(this); } }
        ///<summary>Script interface for the Particle System Lifetime By Emitter Speed module.</summary>
        ///<remarks>This module controls the initial lifetime of each particle based on the speed of the emitter when the particle was spawned.
        ///
        ///Particle System modules do not need to be reassigned back to the system; they are interfaces and not independent objects.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    private ParticleSystem ps;
        ///    public bool moduleEnabled = true;
        ///    public float maxSpeed = 5.0f;
        ///    public AnimationCurve curve = AnimationCurve.EaseInOut(0.0f, 1.0f, 1.0f, 0.2f);
        ///
        ///    void Start()
        ///    {
        ///        ps = GetComponent<ParticleSystem>();
        ///
        ///        var mainModule = ps.main;
        ///        mainModule.startLifetime = 1.0f;
        ///
        ///        // make particles less random to more clearly see effect of lifetime.
        ///        var shapeModule = ps.shape;
        ///        shapeModule.radius = 0.1f;
        ///        shapeModule.angle = 1.0f;
        ///
        ///        var main = ps.main;
        ///        main.simulationSpace = ParticleSystemSimulationSpace.World;
        ///
        ///        // add a sphere so we can see our transform position as it moves
        ///        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ///        sphere.transform.parent = ps.transform;
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        var lifetimeByEmitterSpeed = ps.lifetimeByEmitterSpeed;
        ///        lifetimeByEmitterSpeed.enabled = moduleEnabled;
        ///        lifetimeByEmitterSpeed.range = new Vector2(0, maxSpeed);
        ///        lifetimeByEmitterSpeed.curve = new ParticleSystem.MinMaxCurve(1f, curve);
        ///
        ///        ps.transform.position = new Vector3(Mathf.Sin(Time.time * 2.0f) * 4.0f, 0.0f, 0.0f);
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        moduleEnabled = GUI.Toggle(new Rect(25, 45, 100, 30), moduleEnabled, "Enabled");
        ///        maxSpeed = GUI.HorizontalSlider(new Rect(25, 85, 100, 30), maxSpeed, 0.0f, 10.0f);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public LifetimeByEmitterSpeedModule lifetimeByEmitterSpeed { get { return new LifetimeByEmitterSpeedModule(this); } }
        ///<summary>Script interface for the ForceOverLifetimeModule of a Particle System.</summary>
        ///<remarks>Apply forces to particles using this module. Forces are applied to the particle velocities on each frame.
        ///
        ///Particle System modules do not need to be reassigned back to the system; they are interfaces and not independent objects.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///    void Start() {
        ///        ParticleSystem ps = GetComponent<ParticleSystem>();
        ///        var fo = ps.forceOverLifetime;
        ///        fo.enabled = true;
        ///
        ///        AnimationCurve curve = new AnimationCurve();
        ///        curve.AddKey(0.0f, 0.1f);
        ///        curve.AddKey(0.75f, 1.0f);
        ///        fo.x = new ParticleSystem.MinMaxCurve(1.5f, curve);
        ///    }
        ///}]]></code>
        ///</example>
        public ForceOverLifetimeModule forceOverLifetime { get { return new ForceOverLifetimeModule(this); } }
        ///<summary>Script interface for the ColorOverLifetimeModule of a Particle System.</summary>
        ///<remarks>This module changes the colors assigned to particles over time, based on how long each particle has been alive.
        ///
        ///Particle System modules do not need to be reassigned back to the system; they are interfaces and not independent objects.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///    void Start() {
        ///        ParticleSystem ps = GetComponent<ParticleSystem>();
        ///        var col = ps.colorOverLifetime;
        ///        col.enabled = true;
        ///
        ///        Gradient grad = new Gradient();
        ///        grad.SetKeys( new GradientColorKey[] { new GradientColorKey(Color.blue, 0.0f), new GradientColorKey(Color.red, 1.0f) }, new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) } );
        ///
        ///        col.color = grad;
        ///    }
        ///}]]></code>
        ///</example>
        public ColorOverLifetimeModule colorOverLifetime { get { return new ColorOverLifetimeModule(this); } }
        ///<summary>Script interface for the ColorByLifetimeModule of a Particle System.</summary>
        ///<remarks>This module assigns colors to the particles based on the speed that they are travelling.
        ///
        ///Particle System modules do not need to be reassigned back to the system; they are interfaces and not independent objects.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///    void Start() {
        ///        ParticleSystem ps = GetComponent<ParticleSystem>();
        ///        var col = ps.colorBySpeed;
        ///        col.enabled = true;
        ///
        ///            Gradient grad = new Gradient();
        ///            grad.SetKeys( new GradientColorKey[] { new GradientColorKey(Color.blue, 0.0f), new GradientColorKey(Color.red, 1.0f) }, new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) } );
        ///
        ///            col.color = grad;
        ///    }
        ///}]]></code>
        ///</example>
        public ColorBySpeedModule colorBySpeed { get { return new ColorBySpeedModule(this); } }
        ///<summary>Script interface for the SizeOverLifetimeModule of a Particle System.</summary>
        ///<remarks>This module controls the size of particles throughout their lifetime.
        ///
        ///Particle System modules do not need to be reassigned back to the system; they are interfaces and not independent objects.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///    void Start() {
        ///        ParticleSystem ps = GetComponent<ParticleSystem>();
        ///        var sz = ps.sizeOverLifetime;
        ///        sz.enabled = true;
        ///
        ///        AnimationCurve curve = new AnimationCurve();
        ///        curve.AddKey(0.0f, 0.1f);
        ///        curve.AddKey(0.75f, 1.0f);
        ///
        ///        sz.size = new ParticleSystem.MinMaxCurve(1.5f, curve);
        ///    }
        ///}]]></code>
        ///</example>
        public SizeOverLifetimeModule sizeOverLifetime { get { return new SizeOverLifetimeModule(this); } }
        ///<summary>Script interface for the SizeBySpeedModule of a Particle System.</summary>
        ///<remarks>This module controls the size of particles based on their speeds.
        ///
        ///Particle System modules do not need to be reassigned back to the system; they are interfaces and not independent objects.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///    void Start() {
        ///        ParticleSystem ps = GetComponent<ParticleSystem>();
        ///        var ss = ps.sizeBySpeed;
        ///        ss.enabled = true;
        ///        ss.range = new Vector2(0.0f, 2.0f);
        ///
        ///        AnimationCurve curve = new AnimationCurve();
        ///        curve.AddKey(0.0f, 0.1f);
        ///        curve.AddKey(0.75f, 1.0f);
        ///        ss.size = new ParticleSystem.MinMaxCurve(10.0f, curve);
        ///    }
        ///}]]></code>
        ///</example>
        public SizeBySpeedModule sizeBySpeed { get { return new SizeBySpeedModule(this); } }
        ///<summary>Script interface for the RotationOverLifetimeModule of a Particle System.</summary>
        ///<remarks>Rotate particles throughout their lifetime.
        ///
        ///Particle System modules do not need to be reassigned back to the system; they are interfaces and not independent objects.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///    void Start() {
        ///        ParticleSystem ps = GetComponent<ParticleSystem>();
        ///        var rot = ps.rotationOverLifetime;
        ///        rot.enabled = true;
        ///
        ///        AnimationCurve curve = new AnimationCurve();
        ///        curve.AddKey(0.0f, 0.1f);
        ///        curve.AddKey(0.75f, 0.6f);
        ///
        ///        AnimationCurve curve2 = new AnimationCurve();
        ///        curve2.AddKey(0.0f, 0.2f);
        ///        curve2.AddKey(0.5f, 0.9f);
        ///
        ///        rot.z = new ParticleSystem.MinMaxCurve(2.0f, curve, curve2);
        ///    }
        ///}]]></code>
        ///</example>
        public RotationOverLifetimeModule rotationOverLifetime { get { return new RotationOverLifetimeModule(this); } }
        ///<summary>Script interface for the RotationBySpeedModule of a Particle System.</summary>
        ///<remarks>Rotate particles based on their speed.
        ///
        ///Particle System modules do not need to be reassigned back to the system; they are interfaces and not independent objects.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///    void Start() {
        ///        ParticleSystem ps = GetComponent<ParticleSystem>();
        ///        var rot = ps.rotationBySpeed;
        ///        rot.enabled = true;
        ///
        ///        AnimationCurve curve = new AnimationCurve();
        ///        curve.AddKey(0.0f, 0.1f);
        ///        curve.AddKey(0.75f, 0.6f);
        ///
        ///        AnimationCurve curve2 = new AnimationCurve();
        ///        curve2.AddKey(0.0f, 0.2f);
        ///        curve2.AddKey(0.5f, 0.9f);
        ///
        ///        rot.z = new ParticleSystem.MinMaxCurve(2.0f, curve, curve2);
        ///    }
        ///}]]></code>
        ///</example>
        public RotationBySpeedModule rotationBySpeed { get { return new RotationBySpeedModule(this); } }
        ///<summary>Script interface for the ExternalForcesModule of a Particle System.</summary>
        ///<remarks>This module enables <see cref="ParticleSystemForceField" /> and <see cref="T:UnityEngine.WindZone" /> components to affect the Particle System.
        ///
        ///Particle System modules do not need to be reassigned back to the system; they are interfaces and not independent objects.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///    void Start() {
        ///        ParticleSystem ps = GetComponent<ParticleSystem>();
        ///        var ex = ps.externalForces;
        ///        ex.enabled = true;
        ///        ex.multiplier = 0.1f;
        ///    }
        ///}]]></code>
        ///</example>
        public ExternalForcesModule externalForces { get { return new ExternalForcesModule(this); } }
        ///<summary>Script interface for the NoiseModule of a Particle System.</summary>
        ///<remarks>The Noise Module allows you to apply turbulence to the movement of your particles. Use the low quality settings to create computationally efficient Noise, or simulate smoother, richer Noise with the higher quality settings. You can also choose to define the behavior of the Noise individually for each axis.
        ///
        ///Particle System modules do not need to be reassigned back to the system; they are interfaces and not independent objects.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///    void Start() {
        ///        ParticleSystem ps = GetComponent<ParticleSystem>();
        ///        var no = ps.noise;
        ///        no.enabled = true;
        ///        no.strength = 1.0f;
        ///        no.quality = ParticleSystemNoiseQuality.High;
        ///    }
        ///}]]></code>
        ///</example>
        public NoiseModule noise { get { return new NoiseModule(this); } }
        ///<summary>Script interface for the CollisionModule of a Particle System.</summary>
        ///<remarks>This module allows particles to collide with a predefined list of planes, or with the 2D and 3D physics worlds.
        ///
        ///Particle System modules do not need to be reassigned back to the system; they are interfaces and not independent objects.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///    void Start() {
        ///        ParticleSystem ps = GetComponent<ParticleSystem>();
        ///        var coll = ps.collision;
        ///        coll.enabled = true;
        ///        coll.bounce = 0.5f;
        ///    }
        ///}]]></code>
        ///</example>
        public CollisionModule collision { get { return new CollisionModule(this); } }
        ///<summary>Script interface for the TriggerModule of a Particle System.</summary>
        ///<remarks>This module is useful for killing particles when they touch a set of collision shapes, or for calling a script command to let you apply custom particle behaviors when the trigger is activated.
        ///
        ///The example code for <see cref="M:UnityEngine.MonoBehaviour.OnParticleTrigger" /> shows how the callback type action works.
        ///
        ///Particle System modules do not need to be reassigned back to the system; they are interfaces and not independent objects.</remarks>
        public TriggerModule trigger { get { return new TriggerModule(this); } }
        ///<summary>Script interface for the SubEmittersModule of a Particle System.</summary>
        ///<remarks>The triggering of the child particle emission is linked to events such as the birth, death and collision of particles in the parent system.
        ///
        ///Particle System modules do not need to be reassigned back to the system; they are interfaces and not independent objects.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        /// // A simple example showing access.
        ///public class ExampleClass : MonoBehaviour {
        ///
        ///    public ParticleSystem mySubEmitter;
        ///
        ///    void Start() {
        ///        ParticleSystem ps = GetComponent<ParticleSystem>();
        ///        var sub = ps.subEmitters;
        ///        sub.enabled = true;
        ///        sub.AddSubEmitter(mySubEmitter, ParticleSystemSubEmitterType.Death, ParticleSystemSubEmitterProperties.InheritNothing);
        ///    }
        ///}]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///
        /// // An example showing how to create 2 Particle Systems; one as a sub-emitter.
        ///public class SubEmitterDeathExample : MonoBehaviour
        ///{
        ///    void Start ()
        ///    {
        ///        // A simple particle material with no texture.
        ///        Material particleMaterial = new Material(Shader.Find("Particles/Standard Unlit"));
        ///
        ///        // Create a green Particle System.
        ///        var rootSystemGO = new GameObject("Particle System");
        ///        rootSystemGO.transform.Rotate(-90, 0, 0); // Rotate so the system emits upwards.
        ///        var rootParticleSystem = rootSystemGO.AddComponent<ParticleSystem>();
        ///        rootSystemGO.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
        ///        var mainModule = rootParticleSystem.main;
        ///        mainModule.startColor = Color.green;
        ///        mainModule.startSize = 0.5f;
        ///
        ///        // Create our sub-emitter and set up bursts.
        ///        var subSystemGO = new GameObject("Particle System");
        ///        var subParticleSystem = subSystemGO.AddComponent<ParticleSystem>();
        ///        subSystemGO.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
        ///        var subMainModule = subParticleSystem.main;
        ///        subMainModule.startColor = Color.red;
        ///        subMainModule.startSize = 0.25f;
        ///        var emissionModule = subParticleSystem.emission;
        ///        emissionModule.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 10) }); // We will emit 10 particles upon death.
        ///
        ///        // Set up the sub-emitter.
        ///        subSystemGO.transform.SetParent(rootSystemGO.transform);
        ///        var subEmittersModule = rootParticleSystem.subEmitters;
        ///        subEmittersModule.enabled = true;
        ///        subEmittersModule.AddSubEmitter(subParticleSystem, ParticleSystemSubEmitterType.Death, ParticleSystemSubEmitterProperties.InheritNothing);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public SubEmittersModule subEmitters { get { return new SubEmittersModule(this); } }
        ///<summary>Script interface for the TextureSheetAnimationModule of a Particle System.</summary>
        ///<remarks>This module allows you to add animations to your particle textures. This is achieved by authoring flipbook textures.
        ///
        ///<img src="ParticleFlipbook.png" />
        ///
        ///A flipbook texture sheet that contains eight sub-images of the numbers 1-8 across two rows of four columns. The first row contains the numbers 1-4 and the second row contains the numbers 5-8.
        ///
        ///Each numbered region represents a frame of the animation, and must be distributed evenly across the texture.
        ///Select a variable below to see script examples. You may want to use this texture on your Particle System with each example, to see how the module works.
        ///
        ///Particle System modules do not need to be reassigned back to the system; they are interfaces and not independent objects.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///    void Start() {
        ///        ParticleSystem ps = GetComponent<ParticleSystem>();
        ///        var ts = ps.textureSheetAnimation;
        ///            ts.enabled = true;
        ///        ts.numTilesX = 2;
        ///        ts.rowMode = ParticleSystemAnimationRowMode.Random;
        ///    }
        ///}]]></code>
        ///</example>
        public TextureSheetAnimationModule textureSheetAnimation { get { return new TextureSheetAnimationModule(this); } }
        ///<summary>Script interface for the LightsModule of a Particle System.</summary>
        ///<remarks>This module allows you to attach real-time Lights to a percentage of your particles.
        ///
        ///The lights module is a simple and powerful module that allows particles to cast light onto their environment easily. Lights can inherit properties from the particles they are attached to, such as color and size. Point and Spot Lights are supported, including shadow casting and Light cookies.
        ///
        ///Particle System modules do not need to be reassigned back to the system; they are interfaces and not independent objects.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///
        ///    public Light lightPrefab;
        ///
        ///    void Start() {
        ///        ParticleSystem ps = GetComponent<ParticleSystem>();
        ///        var lights = ps.lights;
        ///        lights.enabled = true;
        ///        lights.ratio = 0.5f;
        ///        lights.light = lightPrefab;
        ///    }
        ///}]]></code>
        ///</example>
        public LightsModule lights { get { return new LightsModule(this); } }
        ///<summary>Script interface for the TrailsModule of a Particle System.</summary>
        ///<remarks>This module adds trails to your particles. Trails can either be left in the wake of particles as they move, or can connect each particle in the system together.
        ///
        ///Particle System modules do not need to be reassigned back to the system; they are interfaces and not independent objects.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///
        ///    void Start() {
        ///        ParticleSystem ps = GetComponent<ParticleSystem>();
        ///        var trails = ps.trails;
        ///        trails.enabled = true;
        ///        trails.ratio = 0.5f;
        ///    }
        ///}]]></code>
        ///</example>
        public TrailModule trails { get { return new TrailModule(this); } }
        ///<summary>Script interface for the CustomDataModule of a Particle System.</summary>
        ///<remarks>Once configured, this module will generate custom per-particle data, which you can use either in script or shaders.
        ///To read the data from script, simply call <see cref="ParticleSystem.GetCustomParticleData" />.
        ///To read it in a shader, enable the custom data streams in the <see cref="ParticleSystemRenderer" /> Module, or call <see cref="ParticleSystemRenderer.SetActiveVertexStreams" /> from script. Once enabled, the custom data will be passed to your vertex shader through a TEXCOORD channel. The <see cref="ParticleSystemRenderer" /> Inspector will tell you which channels are being used.
        ///
        ///Particle System modules do not need to be reassigned back to the system; they are interfaces and not independent objects.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        ParticleSystem ps = GetComponent<ParticleSystem>();
        ///        var customData = ps.customData;
        ///        customData.enabled = true;
        ///
        ///        Gradient grad = new Gradient();
        ///        grad.SetKeys(new GradientColorKey[] { new GradientColorKey(Color.blue, 0.0f), new GradientColorKey(Color.red, 1.0f) }, new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) });
        ///
        ///        customData.SetMode(ParticleSystemCustomData.Custom1, UnityEngine.ParticleSystemCustomDataMode.Color);
        ///        customData.SetColor(ParticleSystemCustomData.Custom1, grad);
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="ParticleSystemRenderer.SetActiveVertexStreams" />
        public CustomDataModule customData { get { return new CustomDataModule(this); } }
    }
}
