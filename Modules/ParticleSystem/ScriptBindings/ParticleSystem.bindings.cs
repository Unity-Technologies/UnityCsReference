// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.Scripting;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Scripting.LifecycleManagement;

using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.ParticleSystemJobs;

namespace UnityEngine
{
    [NativeHeader("ParticleSystemScriptingClasses.h")]
    [NativeHeader("Modules/ParticleSystem/ParticleSystem.h")]
    [NativeHeader("Modules/ParticleSystem/ParticleSystemGeometryJob.h")]
    [NativeHeader("Modules/ParticleSystem/ScriptBindings/ParticleSystemScriptBindings.h")]
    [UsedByNativeCode]
    [global::UnityEngine.NativeClass("ParticleSystem", PersistentTypeId = 198)]
    [RequireComponent(typeof(Transform))]
    public sealed partial class ParticleSystem : Component
    {
        internal ParticleSystem(global::UnityEngine.EntityId id) : base(id) {}
        public ParticleSystem() {}
        // Properties
        ///<summary>Determines whether the Particle System is playing.</summary>
        ///<remarks>::ref::isPlaying is <c>true</c> from when the Particle System begins to play until its last live particle dies. <see cref="isPlaying" /> is <c>false</c> when the Particle System is no longer spawning particles and is not simulating any live particles. (RO).</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///
        /// // A particle sprite example of isPlaying. A button is created
        /// // that shows whether the Particle System is running.  If not, then
        /// // it can be started.  If it is running then it can be stopped.
        ///
        ///using UnityEngine;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Texture2D tex;
        ///    private ParticleSystem ps;
        ///    private Sprite sprite;
        ///
        ///    void Start()
        ///    {
        ///        ps = GetComponent<ParticleSystem>();
        ///        sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), Vector2.zero);
        ///
        ///        var textureSheetAnimation = ps.textureSheetAnimation;
        ///        textureSheetAnimation.enabled = true;
        ///        textureSheetAnimation.mode = ParticleSystemAnimationMode.Sprites;
        ///        textureSheetAnimation.AddSprite(sprite);
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        if (ps.isPlaying)
        ///        {
        ///            if (GUI.Button(new Rect(10, 70, 150, 50), "Stop and clear"))
        ///            {
        ///                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ///            }
        ///        }
        ///        else
        ///        {
        ///            if (GUI.Button(new Rect(10, 70, 150, 50), "Play"))
        ///            {
        ///                ps.Play(false);
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public bool isPlaying
        {
            [NativeName("SyncJobs(false)->IsPlaying")] get;
        }
        ///<summary>Determines whether the Particle System is emitting particles. A Particle System may stop emitting when its emission module has finished, it has been paused or if the system has been stopped using <see cref="ParticleSystem.Stop">Stop</see> with the <see cref="ParticleSystemStopBehavior.StopEmitting">StopEmitting</see> flag. Resume emitting by calling <see cref="ParticleSystem.Play">Play</see>.</summary>
        extern public bool isEmitting
        {
            [NativeName("SyncJobs(false)->IsEmitting")] get;
        }
        ///<summary>Determines whether the Particle System is in the stopped state.</summary>
        ///<remarks>This property is true after a call to <see cref="ParticleSystem.Stop" /> stops the system, if a non-looping system finishes playing and all its particles die, or if the system has not yet played.
        ///
        ///<see cref="ParticleSystem.IsAlive" /> is also false when the system is in the stopped state.</remarks>
        extern public bool isStopped
        {
            [NativeName("SyncJobs(false)->IsStopped")] get;
        }
        ///<summary>Determines whether the Particle System is paused.</summary>
        extern public bool isPaused
        {
            [NativeName("SyncJobs(false)->IsPaused")] get;
        }
        ///<summary>The current number of particles (Read Only). The number doesn't include particles of child Particle Systems</summary>
        extern public int particleCount
        {
            [NativeName("SyncJobs(false)->GetParticleCount")] get;
        }

        ///<summary>Playback position in seconds.</summary>
        ///<remarks>Use this to read the current playback time or to seek to a new playback time. For a looping system, this value wraps within the duration. For systems that specify a Start Delay, getting the time ignores the delay.</remarks>
        ///<seealso cref="ParticleSystem.totalTime" />
        ///<seealso cref="ParticleSystem.MainModule.startDelay" />
        extern public float time
        {
            [NativeName("SyncJobs(false)->GetSecPosition")]
            get;
            [NativeName("SyncJobs(false)->SetSecPosition")]
            set;
        }

        ///<summary>Total playback time in seconds, including the Start Delay setting.</summary>
        ///<remarks>Use this to read the current playback time. For looping systems, the returned value is not wrapped within the duration, unlike <see cref="ParticleSystem.time" />.</remarks>
        ///<seealso cref="ParticleSystem.MainModule.startDelay" />
        extern public float totalTime
        {
            [NativeName("SyncJobs(false)->GetTotalSecPosition")]
            get;
        }

        ///<summary>Override the random seed used for the Particle System emission.</summary>
        ///<remarks>Setting this will also set <see cref="ParticleSystem.useAutoRandomSeed" /> to false.</remarks>
        extern public UInt32 randomSeed
        {
            [NativeName("GetRandomSeed")]
            get;
            [NativeName("SyncJobs(false)->SetRandomSeed")]
            set;
        }

        ///<summary>Controls whether the Particle System uses an automatically-generated random number to seed the random number generator.</summary>
        ///<remarks>If set to true, the Particle System will generate a new random seed each time it is played. If set to false, <see cref="ParticleSystem.randomSeed" /> will be used instead, allowing for a constant seed (useful if you want your particles to play in exactly the same way each time) or user-defined random value (for example, you may want to cycle through an array of seeds).</remarks>
        extern public bool useAutoRandomSeed
        {
            [NativeName("GetAutoRandomSeed")]
            get;
            [NativeName("SyncJobs(false)->SetAutoRandomSeed")]
            set;
        }

        ///<summary>Determines whether this system supports Procedural Simulation.</summary>
        ///<remarks>Internally, each of the Built-In Particle Systems has two modes of operation: procedural and non-procedural.
        ///
        ///In procedural mode, it is possible to know the state of a Built-in Particle System for any point in time (past and future) whereas a non-procedural system is unpredictable. This means that it is possible to prewarm a system quickly by fast forwarding the procedural system. This also allows Unity to calculate the bounding box of the Built-in Particle System more efficiently.
        ///
        ///In order to support Procedural Simulation, you can only use a subset of the Built-in Particle System modules and properties. For example, using the <see cref="ParticleSystem.limitVelocityOverLifetime">Limit Velocity over Lifetime module</see> will disable Procedural Simulation. Additionally, modifying any properties from script whilst the system is playing will also disable Procedural Simulation.
        ///
        ///To discover if you are using any properties that disable this feature, a small speech bubble appears in the upper right corner of the **Inspector** window. The tooltip for this icon gives you details about why Procedural Simulation is disabled.</remarks>
        extern public bool proceduralSimulationSupported
        {
            get;
        }

        // Current size/color helpers
        [FreeFunction(Name = "ParticleSystemScriptBindings::GetParticleCurrentSize", HasExplicitThis = true)]
        extern internal float GetParticleCurrentSize(ref Particle particle);

        [FreeFunction(Name = "ParticleSystemScriptBindings::GetParticleCurrentSize3D", HasExplicitThis = true)]
        extern internal Vector3 GetParticleCurrentSize3D(ref Particle particle);

        [FreeFunction(Name = "ParticleSystemScriptBindings::GetParticleCurrentColor", HasExplicitThis = true)]
        extern internal Color32 GetParticleCurrentColor(ref Particle particle);

        // Mesh index helper
        [FreeFunction(Name = "ParticleSystemScriptBindings::GetParticleMeshIndex", HasExplicitThis = true)]
        extern internal int GetParticleMeshIndex(ref Particle particle);

        // Set/get particles
        ///<summary>Sets the particles of this Particle System.</summary>
        ///<remarks>Setting the lifetime of a particle to a negative value will result in that particle being removed from the Particle System.</remarks>
        ///<param name="particles">The input particle buffer, which represents the particle state to apply to particles in this Particle System.</param>
        ///<param name="size">The number of elements in the particles array that Unity should write to the Particle System.</param>
        ///<param name="offset">The location in the particle array to start assigning particles. For example, set the value to 4 to assign particles starting with the 4th particle in the array.</param>
        ///<seealso cref="GetParticles" />
        [FreeFunction(Name = "ParticleSystemScriptBindings::SetParticles", HasExplicitThis = true, ThrowsException = true)]
        extern public void SetParticles([In, Out] Particle[] particles, int size, int offset);
        ///<summary>Sets the particles of this Particle System.</summary>
        ///<remarks>Setting the lifetime of a particle to a negative value will result in that particle being removed from the Particle System.</remarks>
        ///<param name="particles">The input particle buffer, which represents the particle state to apply to particles in this Particle System.</param>
        ///<param name="size">The number of elements in the particles array that Unity should write to the Particle System.</param>
        ///<seealso cref="GetParticles" />
        public void SetParticles([Out] Particle[] particles, int size) { SetParticles(particles, size, 0); }
        ///<summary>Sets the particles of this Particle System.</summary>
        ///<remarks>Setting the lifetime of a particle to a negative value will result in that particle being removed from the Particle System.</remarks>
        ///<param name="particles">The input particle buffer, which represents the particle state to apply to particles in this Particle System.</param>
        ///<seealso cref="GetParticles" />
        public void SetParticles([Out] Particle[] particles) { SetParticles(particles, -1); }

        [FreeFunction(Name = "ParticleSystemScriptBindings::SetParticlesWithNativeArray", HasExplicitThis = true, ThrowsException = true)]
        extern private void SetParticlesWithNativeArray(IntPtr particles, int particlesLength, int size, int offset);
        ///<summary>Sets the particles of this Particle System.</summary>
        ///<remarks>Setting the lifetime of a particle to a negative value will result in that particle being removed from the Particle System.</remarks>
        ///<param name="particles">The input particle buffer, which represents the particle state to apply to particles in this Particle System.</param>
        ///<param name="size">The number of elements in the particles array that Unity should write to the Particle System.</param>
        ///<param name="offset">The location in the particle array to start assigning particles. For example, set the value to 4 to assign particles starting with the 4th particle in the array.</param>
        ///<seealso cref="GetParticles" />
        public void SetParticles([Out] NativeArray<Particle> particles, int size, int offset) { unsafe { SetParticlesWithNativeArray((IntPtr)particles.GetUnsafeReadOnlyPtr(), particles.Length, size, offset); } }
        ///<summary>Sets the particles of this Particle System.</summary>
        ///<remarks>Setting the lifetime of a particle to a negative value will result in that particle being removed from the Particle System.</remarks>
        ///<param name="particles">The input particle buffer, which represents the particle state to apply to particles in this Particle System.</param>
        ///<param name="size">The number of elements in the particles array that Unity should write to the Particle System.</param>
        ///<seealso cref="GetParticles" />
        public void SetParticles([Out] NativeArray<Particle> particles, int size) { SetParticles(particles, size, 0); }
        ///<summary>Sets the particles of this Particle System.</summary>
        ///<remarks>Setting the lifetime of a particle to a negative value will result in that particle being removed from the Particle System.</remarks>
        ///<param name="particles">The input particle buffer, which represents the particle state to apply to particles in this Particle System.</param>
        ///<seealso cref="GetParticles" />
        public void SetParticles([Out] NativeArray<Particle> particles) { SetParticles(particles, -1); }

        ///<summary>Gets the particles of this Particle System.</summary>
        ///<remarks>This method is allocation free as long the input "particles" array is preallocated once (see example below). The method only gets the particles that are currently alive in the Particle System when it is called, so it may only get a small part of the particles array.</remarks>
        ///<param name="particles">Output particle buffer, containing the current particle state.</param>
        ///<param name="size">The number of elements that are read from the Particle System.</param>
        ///<param name="offset">The offset into the active particle list, from which to copy the particles.</param>
        ///<returns>The number of particles written to the input particle array (the number of particles currently alive).</returns>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ParticleFlow : MonoBehaviour
        ///{
        ///    ParticleSystem m_System;
        ///    ParticleSystem.Particle[] m_Particles;
        ///    public float m_Drift = 0.01f;
        ///
        ///    private void LateUpdate()
        ///    {
        ///        InitializeIfNeeded();
        ///
        ///        // GetParticles is allocation free because we reuse the m_Particles buffer between updates
        ///        int numParticlesAlive = m_System.GetParticles(m_Particles);
        ///
        ///        // Change only the particles that are alive
        ///        for (int i = 0; i < numParticlesAlive; i++)
        ///        {
        ///            m_Particles[i].velocity += Vector3.up * m_Drift;
        ///        }
        ///
        ///        // Apply the particle changes to the Particle System
        ///        m_System.SetParticles(m_Particles, numParticlesAlive);
        ///    }
        ///
        ///    void InitializeIfNeeded()
        ///    {
        ///        if (m_System == null)
        ///            m_System = GetComponent<ParticleSystem>();
        ///
        ///        if (m_Particles == null || m_Particles.Length < m_System.main.maxParticles)
        ///            m_Particles = new ParticleSystem.Particle[m_System.main.maxParticles];
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Particle" />
        ///<seealso cref="SetParticles" />
        [FreeFunction(Name = "ParticleSystemScriptBindings::GetParticles", HasExplicitThis = true, ThrowsException = true)]
        extern public int GetParticles([NotNull][Out] Particle[] particles, int size, int offset);
        ///<summary>Gets the particles of this Particle System.</summary>
        ///<remarks>This method is allocation free as long the input "particles" array is preallocated once (see example below). The method only gets the particles that are currently alive in the Particle System when it is called, so it may only get a small part of the particles array.</remarks>
        ///<param name="particles">Output particle buffer, containing the current particle state.</param>
        ///<param name="size">The number of elements that are read from the Particle System.</param>
        ///<returns>The number of particles written to the input particle array (the number of particles currently alive).</returns>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ParticleFlow : MonoBehaviour
        ///{
        ///    ParticleSystem m_System;
        ///    ParticleSystem.Particle[] m_Particles;
        ///    public float m_Drift = 0.01f;
        ///
        ///    private void LateUpdate()
        ///    {
        ///        InitializeIfNeeded();
        ///
        ///        // GetParticles is allocation free because we reuse the m_Particles buffer between updates
        ///        int numParticlesAlive = m_System.GetParticles(m_Particles);
        ///
        ///        // Change only the particles that are alive
        ///        for (int i = 0; i < numParticlesAlive; i++)
        ///        {
        ///            m_Particles[i].velocity += Vector3.up * m_Drift;
        ///        }
        ///
        ///        // Apply the particle changes to the Particle System
        ///        m_System.SetParticles(m_Particles, numParticlesAlive);
        ///    }
        ///
        ///    void InitializeIfNeeded()
        ///    {
        ///        if (m_System == null)
        ///            m_System = GetComponent<ParticleSystem>();
        ///
        ///        if (m_Particles == null || m_Particles.Length < m_System.main.maxParticles)
        ///            m_Particles = new ParticleSystem.Particle[m_System.main.maxParticles];
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Particle" />
        ///<seealso cref="SetParticles" />
        public int GetParticles([Out] Particle[] particles, int size) { return GetParticles(particles, size, 0); }
        ///<summary>Gets the particles of this Particle System.</summary>
        ///<remarks>This method is allocation free as long the input "particles" array is preallocated once (see example below). The method only gets the particles that are currently alive in the Particle System when it is called, so it may only get a small part of the particles array.</remarks>
        ///<param name="particles">Output particle buffer, containing the current particle state.</param>
        ///<returns>The number of particles written to the input particle array (the number of particles currently alive).</returns>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ParticleFlow : MonoBehaviour
        ///{
        ///    ParticleSystem m_System;
        ///    ParticleSystem.Particle[] m_Particles;
        ///    public float m_Drift = 0.01f;
        ///
        ///    private void LateUpdate()
        ///    {
        ///        InitializeIfNeeded();
        ///
        ///        // GetParticles is allocation free because we reuse the m_Particles buffer between updates
        ///        int numParticlesAlive = m_System.GetParticles(m_Particles);
        ///
        ///        // Change only the particles that are alive
        ///        for (int i = 0; i < numParticlesAlive; i++)
        ///        {
        ///            m_Particles[i].velocity += Vector3.up * m_Drift;
        ///        }
        ///
        ///        // Apply the particle changes to the Particle System
        ///        m_System.SetParticles(m_Particles, numParticlesAlive);
        ///    }
        ///
        ///    void InitializeIfNeeded()
        ///    {
        ///        if (m_System == null)
        ///            m_System = GetComponent<ParticleSystem>();
        ///
        ///        if (m_Particles == null || m_Particles.Length < m_System.main.maxParticles)
        ///            m_Particles = new ParticleSystem.Particle[m_System.main.maxParticles];
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Particle" />
        ///<seealso cref="SetParticles" />
        public int GetParticles([Out] Particle[] particles) { return GetParticles(particles, -1); }

        [FreeFunction(Name = "ParticleSystemScriptBindings::GetParticlesWithNativeArray", HasExplicitThis = true, ThrowsException = true)]
        extern private int GetParticlesWithNativeArray(IntPtr particles, int particlesLength, int size, int offset);
        ///<summary>Gets the particles of this Particle System.</summary>
        ///<remarks>This method is allocation free as long the input "particles" array is preallocated once (see example below). The method only gets the particles that are currently alive in the Particle System when it is called, so it may only get a small part of the particles array.</remarks>
        ///<param name="particles">Output particle buffer, containing the current particle state.</param>
        ///<param name="size">The number of elements that are read from the Particle System.</param>
        ///<param name="offset">The offset into the active particle list, from which to copy the particles.</param>
        ///<returns>The number of particles written to the input particle array (the number of particles currently alive).</returns>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ParticleFlow : MonoBehaviour
        ///{
        ///    ParticleSystem m_System;
        ///    ParticleSystem.Particle[] m_Particles;
        ///    public float m_Drift = 0.01f;
        ///
        ///    private void LateUpdate()
        ///    {
        ///        InitializeIfNeeded();
        ///
        ///        // GetParticles is allocation free because we reuse the m_Particles buffer between updates
        ///        int numParticlesAlive = m_System.GetParticles(m_Particles);
        ///
        ///        // Change only the particles that are alive
        ///        for (int i = 0; i < numParticlesAlive; i++)
        ///        {
        ///            m_Particles[i].velocity += Vector3.up * m_Drift;
        ///        }
        ///
        ///        // Apply the particle changes to the Particle System
        ///        m_System.SetParticles(m_Particles, numParticlesAlive);
        ///    }
        ///
        ///    void InitializeIfNeeded()
        ///    {
        ///        if (m_System == null)
        ///            m_System = GetComponent<ParticleSystem>();
        ///
        ///        if (m_Particles == null || m_Particles.Length < m_System.main.maxParticles)
        ///            m_Particles = new ParticleSystem.Particle[m_System.main.maxParticles];
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Particle" />
        ///<seealso cref="SetParticles" />
        public int GetParticles([Out] NativeArray<Particle> particles, int size, int offset) { unsafe { return GetParticlesWithNativeArray((IntPtr)particles.GetUnsafePtr(), particles.Length, size, offset); } }
        ///<summary>Gets the particles of this Particle System.</summary>
        ///<remarks>This method is allocation free as long the input "particles" array is preallocated once (see example below). The method only gets the particles that are currently alive in the Particle System when it is called, so it may only get a small part of the particles array.</remarks>
        ///<param name="particles">Output particle buffer, containing the current particle state.</param>
        ///<param name="size">The number of elements that are read from the Particle System.</param>
        ///<returns>The number of particles written to the input particle array (the number of particles currently alive).</returns>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ParticleFlow : MonoBehaviour
        ///{
        ///    ParticleSystem m_System;
        ///    ParticleSystem.Particle[] m_Particles;
        ///    public float m_Drift = 0.01f;
        ///
        ///    private void LateUpdate()
        ///    {
        ///        InitializeIfNeeded();
        ///
        ///        // GetParticles is allocation free because we reuse the m_Particles buffer between updates
        ///        int numParticlesAlive = m_System.GetParticles(m_Particles);
        ///
        ///        // Change only the particles that are alive
        ///        for (int i = 0; i < numParticlesAlive; i++)
        ///        {
        ///            m_Particles[i].velocity += Vector3.up * m_Drift;
        ///        }
        ///
        ///        // Apply the particle changes to the Particle System
        ///        m_System.SetParticles(m_Particles, numParticlesAlive);
        ///    }
        ///
        ///    void InitializeIfNeeded()
        ///    {
        ///        if (m_System == null)
        ///            m_System = GetComponent<ParticleSystem>();
        ///
        ///        if (m_Particles == null || m_Particles.Length < m_System.main.maxParticles)
        ///            m_Particles = new ParticleSystem.Particle[m_System.main.maxParticles];
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Particle" />
        ///<seealso cref="SetParticles" />
        public int GetParticles([Out] NativeArray<Particle> particles, int size) { return GetParticles(particles, size, 0); }
        ///<summary>Gets the particles of this Particle System.</summary>
        ///<remarks>This method is allocation free as long the input "particles" array is preallocated once (see example below). The method only gets the particles that are currently alive in the Particle System when it is called, so it may only get a small part of the particles array.</remarks>
        ///<param name="particles">Output particle buffer, containing the current particle state.</param>
        ///<returns>The number of particles written to the input particle array (the number of particles currently alive).</returns>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ParticleFlow : MonoBehaviour
        ///{
        ///    ParticleSystem m_System;
        ///    ParticleSystem.Particle[] m_Particles;
        ///    public float m_Drift = 0.01f;
        ///
        ///    private void LateUpdate()
        ///    {
        ///        InitializeIfNeeded();
        ///
        ///        // GetParticles is allocation free because we reuse the m_Particles buffer between updates
        ///        int numParticlesAlive = m_System.GetParticles(m_Particles);
        ///
        ///        // Change only the particles that are alive
        ///        for (int i = 0; i < numParticlesAlive; i++)
        ///        {
        ///            m_Particles[i].velocity += Vector3.up * m_Drift;
        ///        }
        ///
        ///        // Apply the particle changes to the Particle System
        ///        m_System.SetParticles(m_Particles, numParticlesAlive);
        ///    }
        ///
        ///    void InitializeIfNeeded()
        ///    {
        ///        if (m_System == null)
        ///            m_System = GetComponent<ParticleSystem>();
        ///
        ///        if (m_Particles == null || m_Particles.Length < m_System.main.maxParticles)
        ///            m_Particles = new ParticleSystem.Particle[m_System.main.maxParticles];
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Particle" />
        ///<seealso cref="SetParticles" />
        public int GetParticles([Out] NativeArray<Particle> particles) { return GetParticles(particles, -1); }

        // Set/get custom particle data
        ///<summary>Set a stream of custom per-particle data.</summary>
        ///<remarks>
        ///  <para>Note that if you enable the Custom Data module, it writes into the particle data buffer during the Particle System update. If you want to provide your own data using this function, disable the Custom Data module.
        ///
        ///However, if you want to modify the data that the Custom Data module generates:
        ///1. Use <see cref="ParticleSystem.GetCustomParticleData" /> to get the particle data.
        ///2. Modify the particle data.
        ///3. Use <c>SetCustomParticleData</c> to apply the modified particle data back to the Custom Data module.</para>
        ///  <para>Here is an example of a custom shader that can be used with the above script:</para>
        ///  <para>Here is an example of a script that assigns a unique ID to each particle when it is born:</para>
        ///</remarks>
        ///<param name="customData">The array of per-particle data.</param>
        ///<param name="streamIndex">Which stream to assign the data to.</param>
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
        ///        psr.EnableVertexStreams(ParticleSystemVertexStreams.Custom1);
        ///    }
        ///
        ///    void Update() {
        ///
        ///        Camera mainCam = Camera.main;
        ///
        ///        ps.GetCustomParticleData(customData, ParticleSystemCustomData.Custom1);
        ///
        ///        int particleCount = ps.particleCount;
        ///        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[particleCount];
        ///        ps.GetParticles(particles);
        ///
        ///        for (int i = 0; i < particles.Length; i++)
        ///        {
        ///            Vector3 sPos = mainCam.WorldToScreenPoint(particles[i].position + ps.transform.position);
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
        ///                float2 texcoord : TEXCOORD0;
        ///                float4 customData : TEXCOORD1;
        ///            };
        ///
        ///            struct v2f {
        ///                float4 vertex : SV_POSITION;
        ///                fixed4 color : COLOR;
        ///                float2 texcoord : TEXCOORD0;
        ///                float4 customData : TEXCOORD1;
        ///                UNITY_FOG_COORDS(2)
        ///            };
        ///
        ///            float4 _MainTex_ST;
        ///
        ///            v2f vert (appdata_t v)
        ///            {
        ///                v.vertex.y = lerp(v.vertex.y, v.vertex.y + _OffsetValue, v.customData.x);
        ///
        ///                v2f o;
        ///                o.vertex = UnityObjectToClipPos(v.vertex);
        ///
        ///                float4 offsetX = float4(-1, 1, 1, -1);
        ///                float4 offsetY = float4(1, 1, -1, -1);
        ///
        ///                o.color = v.color;
        ///                o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
        ///                o.customData = v.customData;
        ///                UNITY_TRANSFER_FOG(o,o.vertex);
        ///
        ///                return o;
        ///            }
        ///
        ///            fixed4 frag (v2f i) : SV_Target
        ///            {
        ///                fixed4 col = 2.0f * i.color * _TintColor * tex2D(_MainTex, i.texcoord);
        ///                fixed4 col2 = fixed4(i.customData.x, i.customData.y, i.customData.z, col.a);
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
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEditor;
        ///using System.Collections.Generic;
        ///
        ///[RequireComponent(typeof(ParticleSystem))]
        ///public class ExampleClass : MonoBehaviour {
        ///
        ///    private ParticleSystem ps;
        ///    private List<Vector4> customData = new List<Vector4>();
        ///    private int uniqueID;
        ///
        ///    void Start() {
        ///
        ///        ps = GetComponent<ParticleSystem>();
        ///    }
        ///
        ///    void Update() {
        ///
        ///        ps.GetCustomParticleData(customData, ParticleSystemCustomData.Custom1);
        ///
        ///        for (int i = 0; i < customData.Count; i++)
        ///        {
        ///            // set custom data to the next ID, if it is in the default 0 state
        ///            if (customData[i].x == 0.0f)
        ///            {
        ///                customData[i] = new Vector4(++uniqueID, 0, 0, 0);
        ///            }
        ///        }
        ///
        ///        ps.SetCustomParticleData(customData, ParticleSystemCustomData.Custom1);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [FreeFunction(Name = "ParticleSystemScriptBindings::SetCustomParticleData", HasExplicitThis = true, ThrowsException = true)]
        extern public void SetCustomParticleData([NotNull] List<Vector4> customData, ParticleSystemCustomData streamIndex);
        ///<summary>Get a stream of custom per-particle data.</summary>
        ///<param name="customData">The array of per-particle data.</param>
        ///<param name="streamIndex">Which stream to retrieve the data from.</param>
        ///<returns>The amount of valid per-particle data.</returns>
        ///<seealso cref="ParticleSystem.SetCustomParticleData" />
        [FreeFunction(Name = "ParticleSystemScriptBindings::GetCustomParticleData", HasExplicitThis = true, ThrowsException = true)]
        extern public int GetCustomParticleData([NotNull] List<Vector4> customData, ParticleSystemCustomData streamIndex);

        // Set/get the playback state
        ///<summary>Returns all the data that relates to the current internal state of the Particle System.</summary>
        ///<remarks>If you want to restore the Particle System to its current state in the future, store the PlaybackState this function returns along with <see cref="ParticleSystem.GetParticles" /> and <see cref="ParticleSystem.GetTrails" />.</remarks>
        ///<returns>The current internal state of the Particle System.</returns>
        ///<seealso cref="PlaybackState" />
        ///<seealso cref="SetPlaybackState" />
        ///<seealso cref="GetTrails" />
        extern public PlaybackState GetPlaybackState();
        ///<summary>Use this method with the results of an earlier call to <see cref="ParticleSystem.GetPlaybackState" />, in order to restore the Particle System to the state stored in the playbackState object.</summary>
        ///<remarks>To fully restore a Particle System to a previous state, use this method along with <see cref="ParticleSystem.SetParticles" /> and <see cref="ParticleSystem.SetTrails" />.</remarks>
        ///<param name="playbackState">The PlaybackState to apply to the Particle System.</param>
        ///<seealso cref="GetPlaybackState" />
        ///<seealso cref="SetParticles" />
        ///<seealso cref="SetTrails" />
        extern public void SetPlaybackState(PlaybackState playbackState);

        // Set/get the trail data
        [FreeFunction(Name = "ParticleSystemScriptBindings::GetTrailData", HasExplicitThis = true)]
        extern private void GetTrailDataInternal(ref Trails trailData);
        ///<summary>Returns all the data relating to the current internal state of the Particle System Trails.</summary>
        ///<remarks>If you want to restore the Particle System to its current state in the future, store the Trails this function returns along with <see cref="ParticleSystem.GetParticles" /> and <see cref="ParticleSystem.GetPlaybackState" />.</remarks>
        ///<returns>The variable to populate with the Trails that currently belong to the Particle System..</returns>
        ///<seealso cref="Trails" />
        ///<seealso cref="SetTrails" />
        ///<seealso cref="GetPlaybackState" />
        public Trails GetTrails()
        {
            var result = new Trails();
            result.Allocate();
            GetTrailDataInternal(ref result);
            return result;
        }

        ///<summary>If you want to restore the Particle System to its current state in the future, store the Trails this function returns along with <see cref="ParticleSystem.GetParticles" /> and <see cref="ParticleSystem.GetPlaybackState" />.
        ///
        ///This method allows you to get the trail data without creating any garbage, if you presize the trail data.
        ///
        ///</summary>
        ///<param name="trailData">The current Trails belonging to the Particle System.</param>
        ///<returns>The number of trails.</returns>
        ///<seealso cref="Trails" />
        ///<seealso cref="SetTrails" />
        ///<seealso cref="GetPlaybackState" />
        public int GetTrails(ref Trails trailData)
        {
            trailData.Allocate();
            GetTrailDataInternal(ref trailData);
            return trailData.positions.Count;
        }

        ///<summary>Sets the particles and the trails of this Particle System.</summary>
        ///<remarks>Similar to <see cref="ParticleSystem.SetParticles" />, if you set the lifetime of a particle to a negative value, Unity removes that particle from the particle system. To keep the particle alive until other trails finish, disable Die With Particles in the Trails module Inspector window.</remarks>
        ///<param name="particles">The input particle buffer, which represents the particle state to apply to particles in this Particle System.</param>
        ///<param name="trailData">The Trails to apply to the Particle System.</param>
        ///<param name="size">The number of elements in the particles array that Unity should write to the Particle System.</param>
        ///<param name="offset">The location in the particle array to start assigning particles. For example, set the value to 4 to assign particles starting with the 4th particle in the array.</param>
        ///<seealso cref="GetParticles" />
        ///<seealso cref="SetParticles" />
        ///<seealso cref="SetTrails" />
        [FreeFunction(Name = "ParticleSystemScriptBindings::SetParticlesAndTrailData", HasExplicitThis = true, ThrowsException = true)]
        extern public void SetParticlesAndTrails([NotNull, Out] Particle[] particles, Trails trailData, int size, int offset);
        ///<summary>Sets the particles and the trails of this Particle System.</summary>
        ///<remarks>Similar to <see cref="ParticleSystem.SetParticles" />, if you set the lifetime of a particle to a negative value, Unity removes that particle from the particle system. To keep the particle alive until other trails finish, disable Die With Particles in the Trails module Inspector window.</remarks>
        ///<param name="particles">The input particle buffer, which represents the particle state to apply to particles in this Particle System.</param>
        ///<param name="trailData">The Trails to apply to the Particle System.</param>
        ///<param name="size">The number of elements in the particles array that Unity should write to the Particle System.</param>
        ///<seealso cref="GetParticles" />
        ///<seealso cref="SetParticles" />
        ///<seealso cref="SetTrails" />
        public void SetParticlesAndTrails([Out] Particle[] particles, Trails trailData, int size) { SetParticlesAndTrails(particles, trailData, size, 0); }
        ///<summary>Sets the particles and the trails of this Particle System.</summary>
        ///<remarks>Similar to <see cref="ParticleSystem.SetParticles" />, if you set the lifetime of a particle to a negative value, Unity removes that particle from the particle system. To keep the particle alive until other trails finish, disable Die With Particles in the Trails module Inspector window.</remarks>
        ///<param name="particles">The input particle buffer, which represents the particle state to apply to particles in this Particle System.</param>
        ///<param name="trailData">The Trails to apply to the Particle System.</param>
        ///<seealso cref="GetParticles" />
        ///<seealso cref="SetParticles" />
        ///<seealso cref="SetTrails" />
        public void SetParticlesAndTrails([Out] Particle[] particles, Trails trailData) { SetParticlesAndTrails(particles, trailData, -1); }

        [FreeFunction(Name = "ParticleSystemScriptBindings::SetParticlesAndTrailDataWithNativeArray", HasExplicitThis = true, ThrowsException = true)]
        extern private void SetParticlesAndTrailsWithNativeArray(IntPtr particles, Trails trailData, int particlesLength, int size, int offset);
        ///<summary>Sets the particles and the trails of this Particle System.</summary>
        ///<remarks>Similar to <see cref="ParticleSystem.SetParticles" />, if you set the lifetime of a particle to a negative value, Unity removes that particle from the particle system. To keep the particle alive until other trails finish, disable Die With Particles in the Trails module Inspector window.</remarks>
        ///<param name="particles">The input particle buffer, which represents the particle state to apply to particles in this Particle System.</param>
        ///<param name="trailData">The Trails to apply to the Particle System.</param>
        ///<param name="size">The number of elements in the particles array that Unity should write to the Particle System.</param>
        ///<param name="offset">The location in the particle array to start assigning particles. For example, set the value to 4 to assign particles starting with the 4th particle in the array.</param>
        ///<seealso cref="GetParticles" />
        ///<seealso cref="SetParticles" />
        ///<seealso cref="SetTrails" />
        public void SetParticlesAndTrails([Out] NativeArray<Particle> particles, Trails trailData, int size, int offset) { unsafe { SetParticlesAndTrailsWithNativeArray((IntPtr)particles.GetUnsafeReadOnlyPtr(), trailData, particles.Length, size, offset); } }
        ///<summary>Sets the particles and the trails of this Particle System.</summary>
        ///<remarks>Similar to <see cref="ParticleSystem.SetParticles" />, if you set the lifetime of a particle to a negative value, Unity removes that particle from the particle system. To keep the particle alive until other trails finish, disable Die With Particles in the Trails module Inspector window.</remarks>
        ///<param name="particles">The input particle buffer, which represents the particle state to apply to particles in this Particle System.</param>
        ///<param name="trailData">The Trails to apply to the Particle System.</param>
        ///<param name="size">The number of elements in the particles array that Unity should write to the Particle System.</param>
        ///<seealso cref="GetParticles" />
        ///<seealso cref="SetParticles" />
        ///<seealso cref="SetTrails" />
        public void SetParticlesAndTrails([Out] NativeArray<Particle> particles, Trails trailData, int size) { SetParticlesAndTrails(particles, trailData, size, 0); }
        ///<summary>Sets the particles and the trails of this Particle System.</summary>
        ///<remarks>Similar to <see cref="ParticleSystem.SetParticles" />, if you set the lifetime of a particle to a negative value, Unity removes that particle from the particle system. To keep the particle alive until other trails finish, disable Die With Particles in the Trails module Inspector window.</remarks>
        ///<param name="particles">The input particle buffer, which represents the particle state to apply to particles in this Particle System.</param>
        ///<param name="trailData">The Trails to apply to the Particle System.</param>
        ///<seealso cref="GetParticles" />
        ///<seealso cref="SetParticles" />
        ///<seealso cref="SetTrails" />
        public void SetParticlesAndTrails([Out] NativeArray<Particle> particles, Trails trailData) { SetParticlesAndTrails(particles, trailData, -1); }         

        // Playback
        ///<summary>Fast-forwards the Particle System by simulating particles over the given period of time, then pauses it.</summary>
        ///<param name="t">Time period in seconds to advance the ParticleSystem simulation by. If <c>restart</c> is true, the ParticleSystem will be reset to 0 time, and then advanced by this value. If <c>restart</c> is false, the ParticleSystem simulation will be advanced in time from its current state by this value.</param>
        ///<param name="withChildren">Fast-forward all child Particle Systems as well.</param>
        ///<param name="restart">Restart and start from the beginning.</param>
        ///<param name="fixedTimeStep">Only update the system at fixed intervals, based on the value in "Fixed Time" in the Time options.</param>
        ///<seealso cref="Play" />
        ///<seealso cref="Pause" />
        [FreeFunction(Name = "ParticleSystemScriptBindings::Simulate", HasExplicitThis = true)]
        extern public void Simulate(float t, [DefaultValue("true")] bool withChildren, [DefaultValue("true")] bool restart, [DefaultValue("true")] bool fixedTimeStep);
        ///<summary>Fast-forwards the Particle System by simulating particles over the given period of time, then pauses it.</summary>
        ///<param name="t">Time period in seconds to advance the ParticleSystem simulation by. If <c>restart</c> is true, the ParticleSystem will be reset to 0 time, and then advanced by this value. If <c>restart</c> is false, the ParticleSystem simulation will be advanced in time from its current state by this value.</param>
        ///<param name="withChildren">Fast-forward all child Particle Systems as well.</param>
        ///<param name="restart">Restart and start from the beginning.</param>
        ///<seealso cref="Play" />
        ///<seealso cref="Pause" />
        public void Simulate(float t, [DefaultValue("true")] bool withChildren, [DefaultValue("true")] bool restart) { Simulate(t, withChildren, restart, true); }
        ///<summary>Fast-forwards the Particle System by simulating particles over the given period of time, then pauses it.</summary>
        ///<param name="t">Time period in seconds to advance the ParticleSystem simulation by. If <c>restart</c> is true, the ParticleSystem will be reset to 0 time, and then advanced by this value. If <c>restart</c> is false, the ParticleSystem simulation will be advanced in time from its current state by this value.</param>
        ///<param name="withChildren">Fast-forward all child Particle Systems as well.</param>
        ///<seealso cref="Play" />
        ///<seealso cref="Pause" />
        public void Simulate(float t, [DefaultValue("true")] bool withChildren) { Simulate(t, withChildren, true); }
        ///<summary>Fast-forwards the Particle System by simulating particles over the given period of time, then pauses it.</summary>
        ///<param name="t">Time period in seconds to advance the ParticleSystem simulation by. If <c>restart</c> is true, the ParticleSystem will be reset to 0 time, and then advanced by this value. If <c>restart</c> is false, the ParticleSystem simulation will be advanced in time from its current state by this value.</param>
        ///<seealso cref="Play" />
        ///<seealso cref="Pause" />
        public void Simulate(float t) { Simulate(t, true); }

        ///<summary>Starts the Particle System and resets its playback time to 0.</summary>
        ///<remarks>
        ///  <para>The Play() function switches the Particle System into play mode and enables particle emission (if it was previously disabled). The exact behavior varies based on the system's current state:
        ///
        ///If the Particle System has been **paused**, then this resumes playing from the previous time.
        ///
        ///If the Particle System has **stopped**, then the system starts from time 0, and, if it is relevant, the <see cref="ParticleSystem.MainModule.startDelay" /> is applied.
        ///
        ///If the Particle System is **already playing**, the system continues to play and reset the playback time to 0. For looping systems, the reset may have no visible impact. However, for non-looping systems, particles may start to emit again, depending on the system’s configuration.</para>
        ///  <para>For scripted control of Particle System playback, such as responding to game events or user input, <c>Play()</c> is the method used to initiate or restart particle emission. You can achieve more comprehensive control by using <c>Play()</c> in combination with methods like <see cref="Pause" /> and <see cref="Stop" />, and by monitoring properties such as time and <see cref="particleCount" />.
        ///
        ///**Note**: Unity does not apply <see cref="ParticleSystem.MainModule.prewarm" /> when the Particle System resumes from a paused state. To apply <c>prewarm</c> when the Particle System resumes, call <see cref="Clear" /> after <see cref="Stop" />.
        ///
        ///**Note**: If you invoke this function again before the particle system has had time to spawn a particle, the particle system restarts its internal counters. This means that if you invoke this function continuously, a particle system with a low emission rate will never start to play.</para>
        ///  <para />
        ///</remarks>
        ///<param name="withChildren">Play all child Particle Systems as well.</param>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///
        ///public class SimpleParticleSystemController : MonoBehaviour
        ///{
        ///    [SerializeField] private ParticleSystem particleSystem;
        ///
        ///    void Update()
        ///    {
        ///        // Press the spacebar to restart the Particle System.
        ///        if (Input.GetKeyDown(KeyCode.Space))
        ///        {
        ///            particleSystem.Play();
        ///            Debug.Log("Particle System restarted. Playback time reset to 0.");
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///
        /// //The window allows you to play, pause, or stop the system, toggle child system inclusion, and view information like playback time and particle count.
        ///public class ParticleSystemControllerWindow : MonoBehaviour
        ///{
        ///    ParticleSystem m_ParticleSystem;
        ///    Rect m_WindowRect = new Rect(0, 0, 300, 120);
        ///    bool m_IncludeChildren = true;
        ///
        ///    void Start()
        ///    {
        ///        m_ParticleSystem = GetComponent<ParticleSystem>();
        ///    }
        ///    
        ///    void OnGUI()
        ///    {
        ///        m_WindowRect = GUI.Window("ParticleController".GetHashCode(), m_WindowRect, DrawWindowContents, m_ParticleSystem.name);
        ///    }
        ///
        ///    void DrawWindowContents(int windowId)
        ///    {
        ///        if (m_ParticleSystem)
        ///        {
        ///            GUILayout.BeginHorizontal();
        ///            GUILayout.Toggle(m_ParticleSystem.isPlaying, "Playing");
        ///            GUILayout.Toggle(m_ParticleSystem.isEmitting, "Emitting");
        ///            GUILayout.Toggle(m_ParticleSystem.isPaused, "Paused");
        ///            GUILayout.EndHorizontal();
        ///
        ///            GUILayout.BeginHorizontal();
        ///            if (GUILayout.Button("Play"))
        ///                m_ParticleSystem.Play(m_IncludeChildren);
        ///            if (GUILayout.Button("Pause"))
        ///                m_ParticleSystem.Pause(m_IncludeChildren);
        ///            if (GUILayout.Button("Stop Emitting"))
        ///                m_ParticleSystem.Stop(m_IncludeChildren, ParticleSystemStopBehavior.StopEmitting);
        ///            if (GUILayout.Button("Stop & Clear"))
        ///                m_ParticleSystem.Stop(m_IncludeChildren, ParticleSystemStopBehavior.StopEmittingAndClear);
        ///            GUILayout.EndHorizontal();
        ///
        ///            m_IncludeChildren = GUILayout.Toggle(m_IncludeChildren, "Include Children");
        ///
        ///            GUILayout.BeginHorizontal();
        ///            GUILayout.Label("Time(" + m_ParticleSystem.time + ")");
        ///            GUILayout.Label("Particle Count(" + m_ParticleSystem.particleCount + ")");
        ///            GUILayout.EndHorizontal();
        ///        }
        ///        else
        ///            GUILayout.Label("No Particle System found");
        ///
        ///        GUI.DragWindow();
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="Stop" />
        ///<seealso cref="Pause" />
        ///<seealso cref="isEmitting" />
        [FreeFunction(Name = "ParticleSystemScriptBindings::Play", HasExplicitThis = true)]
        extern public void Play([DefaultValue("true")] bool withChildren);
        ///<summary>Starts the Particle System and resets its playback time to 0.</summary>
        ///<remarks>
        ///  <para>The Play() function switches the Particle System into play mode and enables particle emission (if it was previously disabled). The exact behavior varies based on the system's current state:
        ///
        ///If the Particle System has been **paused**, then this resumes playing from the previous time.
        ///
        ///If the Particle System has **stopped**, then the system starts from time 0, and, if it is relevant, the <see cref="ParticleSystem.MainModule.startDelay" /> is applied.
        ///
        ///If the Particle System is **already playing**, the system continues to play and reset the playback time to 0. For looping systems, the reset may have no visible impact. However, for non-looping systems, particles may start to emit again, depending on the system’s configuration.</para>
        ///  <para>For scripted control of Particle System playback, such as responding to game events or user input, <c>Play()</c> is the method used to initiate or restart particle emission. You can achieve more comprehensive control by using <c>Play()</c> in combination with methods like <see cref="Pause" /> and <see cref="Stop" />, and by monitoring properties such as time and <see cref="particleCount" />.
        ///
        ///**Note**: Unity does not apply <see cref="ParticleSystem.MainModule.prewarm" /> when the Particle System resumes from a paused state. To apply <c>prewarm</c> when the Particle System resumes, call <see cref="Clear" /> after <see cref="Stop" />.
        ///
        ///**Note**: If you invoke this function again before the particle system has had time to spawn a particle, the particle system restarts its internal counters. This means that if you invoke this function continuously, a particle system with a low emission rate will never start to play.</para>
        ///  <para />
        ///</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///
        ///public class SimpleParticleSystemController : MonoBehaviour
        ///{
        ///    [SerializeField] private ParticleSystem particleSystem;
        ///
        ///    void Update()
        ///    {
        ///        // Press the spacebar to restart the Particle System.
        ///        if (Input.GetKeyDown(KeyCode.Space))
        ///        {
        ///            particleSystem.Play();
        ///            Debug.Log("Particle System restarted. Playback time reset to 0.");
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///
        /// //The window allows you to play, pause, or stop the system, toggle child system inclusion, and view information like playback time and particle count.
        ///public class ParticleSystemControllerWindow : MonoBehaviour
        ///{
        ///    ParticleSystem m_ParticleSystem;
        ///    Rect m_WindowRect = new Rect(0, 0, 300, 120);
        ///    bool m_IncludeChildren = true;
        ///
        ///    void Start()
        ///    {
        ///        m_ParticleSystem = GetComponent<ParticleSystem>();
        ///    }
        ///    
        ///    void OnGUI()
        ///    {
        ///        m_WindowRect = GUI.Window("ParticleController".GetHashCode(), m_WindowRect, DrawWindowContents, m_ParticleSystem.name);
        ///    }
        ///
        ///    void DrawWindowContents(int windowId)
        ///    {
        ///        if (m_ParticleSystem)
        ///        {
        ///            GUILayout.BeginHorizontal();
        ///            GUILayout.Toggle(m_ParticleSystem.isPlaying, "Playing");
        ///            GUILayout.Toggle(m_ParticleSystem.isEmitting, "Emitting");
        ///            GUILayout.Toggle(m_ParticleSystem.isPaused, "Paused");
        ///            GUILayout.EndHorizontal();
        ///
        ///            GUILayout.BeginHorizontal();
        ///            if (GUILayout.Button("Play"))
        ///                m_ParticleSystem.Play(m_IncludeChildren);
        ///            if (GUILayout.Button("Pause"))
        ///                m_ParticleSystem.Pause(m_IncludeChildren);
        ///            if (GUILayout.Button("Stop Emitting"))
        ///                m_ParticleSystem.Stop(m_IncludeChildren, ParticleSystemStopBehavior.StopEmitting);
        ///            if (GUILayout.Button("Stop & Clear"))
        ///                m_ParticleSystem.Stop(m_IncludeChildren, ParticleSystemStopBehavior.StopEmittingAndClear);
        ///            GUILayout.EndHorizontal();
        ///
        ///            m_IncludeChildren = GUILayout.Toggle(m_IncludeChildren, "Include Children");
        ///
        ///            GUILayout.BeginHorizontal();
        ///            GUILayout.Label("Time(" + m_ParticleSystem.time + ")");
        ///            GUILayout.Label("Particle Count(" + m_ParticleSystem.particleCount + ")");
        ///            GUILayout.EndHorizontal();
        ///        }
        ///        else
        ///            GUILayout.Label("No Particle System found");
        ///
        ///        GUI.DragWindow();
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="Stop" />
        ///<seealso cref="Pause" />
        ///<seealso cref="isEmitting" />
        public void Play() { Play(true);  }

        ///<summary>Pauses the system so no new particles are emitted and the existing particles are not updated.</summary>
        ///<remarks>
        ///
        ///The following example creates a GUI window for manipulating a Particle System.</remarks>
        ///<param name="withChildren">Pause all child Particle Systems as well.</param>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///
        ///public class ParticleSystemControllerWindow : MonoBehaviour
        ///{
        ///    ParticleSystem system
        ///    {
        ///        get
        ///        {
        ///            if (_CachedSystem == null)
        ///                _CachedSystem = GetComponent<ParticleSystem>();
        ///            return _CachedSystem;
        ///        }
        ///    }
        ///    private ParticleSystem _CachedSystem;
        ///
        ///    public Rect windowRect = new Rect(0, 0, 300, 120);
        ///
        ///    public bool includeChildren = true;
        ///
        ///    void OnGUI()
        ///    {
        ///        windowRect = GUI.Window("ParticleController".GetHashCode(), windowRect, DrawWindowContents, system.name);
        ///    }
        ///
        ///    void DrawWindowContents(int windowId)
        ///    {
        ///        if (system)
        ///        {
        ///            GUILayout.BeginHorizontal();
        ///            GUILayout.Toggle(system.isPlaying, "Playing");
        ///            GUILayout.Toggle(system.isEmitting, "Emitting");
        ///            GUILayout.Toggle(system.isPaused, "Paused");
        ///            GUILayout.EndHorizontal();
        ///
        ///            GUILayout.BeginHorizontal();
        ///            if (GUILayout.Button("Play"))
        ///                system.Play(includeChildren);
        ///            if (GUILayout.Button("Pause"))
        ///                system.Pause(includeChildren);
        ///            if (GUILayout.Button("Stop Emitting"))
        ///                system.Stop(includeChildren, ParticleSystemStopBehavior.StopEmitting);
        ///            if (GUILayout.Button("Stop & Clear"))
        ///                system.Stop(includeChildren, ParticleSystemStopBehavior.StopEmittingAndClear);
        ///            GUILayout.EndHorizontal();
        ///
        ///            includeChildren = GUILayout.Toggle(includeChildren, "Include Children");
        ///
        ///            GUILayout.BeginHorizontal();
        ///            GUILayout.Label("Time(" + system.time + ")");
        ///            GUILayout.Label("Particle Count(" + system.particleCount + ")");
        ///            GUILayout.EndHorizontal();
        ///        }
        ///        else
        ///            GUILayout.Label("No Particle System found");
        ///
        ///        GUI.DragWindow();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Play" />
        ///<seealso cref="Stop" />
        [FreeFunction(Name = "ParticleSystemScriptBindings::Pause", HasExplicitThis = true)]
        extern public void Pause([DefaultValue("true")] bool withChildren);
        ///<summary>Pauses the system so no new particles are emitted and the existing particles are not updated.</summary>
        ///<remarks>
        ///
        ///The following example creates a GUI window for manipulating a Particle System.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///
        ///public class ParticleSystemControllerWindow : MonoBehaviour
        ///{
        ///    ParticleSystem system
        ///    {
        ///        get
        ///        {
        ///            if (_CachedSystem == null)
        ///                _CachedSystem = GetComponent<ParticleSystem>();
        ///            return _CachedSystem;
        ///        }
        ///    }
        ///    private ParticleSystem _CachedSystem;
        ///
        ///    public Rect windowRect = new Rect(0, 0, 300, 120);
        ///
        ///    public bool includeChildren = true;
        ///
        ///    void OnGUI()
        ///    {
        ///        windowRect = GUI.Window("ParticleController".GetHashCode(), windowRect, DrawWindowContents, system.name);
        ///    }
        ///
        ///    void DrawWindowContents(int windowId)
        ///    {
        ///        if (system)
        ///        {
        ///            GUILayout.BeginHorizontal();
        ///            GUILayout.Toggle(system.isPlaying, "Playing");
        ///            GUILayout.Toggle(system.isEmitting, "Emitting");
        ///            GUILayout.Toggle(system.isPaused, "Paused");
        ///            GUILayout.EndHorizontal();
        ///
        ///            GUILayout.BeginHorizontal();
        ///            if (GUILayout.Button("Play"))
        ///                system.Play(includeChildren);
        ///            if (GUILayout.Button("Pause"))
        ///                system.Pause(includeChildren);
        ///            if (GUILayout.Button("Stop Emitting"))
        ///                system.Stop(includeChildren, ParticleSystemStopBehavior.StopEmitting);
        ///            if (GUILayout.Button("Stop & Clear"))
        ///                system.Stop(includeChildren, ParticleSystemStopBehavior.StopEmittingAndClear);
        ///            GUILayout.EndHorizontal();
        ///
        ///            includeChildren = GUILayout.Toggle(includeChildren, "Include Children");
        ///
        ///            GUILayout.BeginHorizontal();
        ///            GUILayout.Label("Time(" + system.time + ")");
        ///            GUILayout.Label("Particle Count(" + system.particleCount + ")");
        ///            GUILayout.EndHorizontal();
        ///        }
        ///        else
        ///            GUILayout.Label("No Particle System found");
        ///
        ///        GUI.DragWindow();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Play" />
        ///<seealso cref="Stop" />
        public void Pause() { Pause(true); }

        ///<summary>Stops playing the Particle System using the supplied stop behaviour.</summary>
        ///<remarks>
        ///
        ///The following example creates a GUI window for manipulating a Particle System.</remarks>
        ///<param name="withChildren">Stop all child Particle Systems as well.</param>
        ///<param name="stopBehavior">Stop emitting or stop emitting and clear the system.</param>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///
        ///public class ParticleSystemControllerWindow : MonoBehaviour
        ///{
        ///    ParticleSystem system
        ///    {
        ///        get
        ///        {
        ///            if (_CachedSystem == null)
        ///                _CachedSystem = GetComponent<ParticleSystem>();
        ///            return _CachedSystem;
        ///        }
        ///    }
        ///    private ParticleSystem _CachedSystem;
        ///
        ///    public Rect windowRect = new Rect(0, 0, 300, 120);
        ///
        ///    public bool includeChildren = true;
        ///
        ///    void OnGUI()
        ///    {
        ///        windowRect = GUI.Window("ParticleController".GetHashCode(), windowRect, DrawWindowContents, system.name);
        ///    }
        ///
        ///    void DrawWindowContents(int windowId)
        ///    {
        ///        if (system)
        ///        {
        ///            GUILayout.BeginHorizontal();
        ///            GUILayout.Toggle(system.isPlaying, "Playing");
        ///            GUILayout.Toggle(system.isEmitting, "Emitting");
        ///            GUILayout.Toggle(system.isPaused, "Paused");
        ///            GUILayout.EndHorizontal();
        ///
        ///            GUILayout.BeginHorizontal();
        ///            if (GUILayout.Button("Play"))
        ///                system.Play(includeChildren);
        ///            if (GUILayout.Button("Pause"))
        ///                system.Pause(includeChildren);
        ///            if (GUILayout.Button("Stop Emitting"))
        ///                system.Stop(includeChildren, ParticleSystemStopBehavior.StopEmitting);
        ///            if (GUILayout.Button("Stop & Clear"))
        ///                system.Stop(includeChildren, ParticleSystemStopBehavior.StopEmittingAndClear);
        ///            GUILayout.EndHorizontal();
        ///
        ///            includeChildren = GUILayout.Toggle(includeChildren, "Include Children");
        ///
        ///            GUILayout.BeginHorizontal();
        ///            GUILayout.Label("Time(" + system.time + ")");
        ///            GUILayout.Label("Particle Count(" + system.particleCount + ")");
        ///            GUILayout.EndHorizontal();
        ///        }
        ///        else
        ///            GUILayout.Label("No Particle System found");
        ///
        ///        GUI.DragWindow();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Play" />
        ///<seealso cref="Pause" />
        [FreeFunction(Name = "ParticleSystemScriptBindings::Stop", HasExplicitThis = true)]
        extern public void Stop([DefaultValue("true")] bool withChildren, [DefaultValue("ParticleSystemStopBehavior.StopEmitting")] ParticleSystemStopBehavior stopBehavior);
        ///<summary>Stops playing the Particle System using the supplied stop behaviour.</summary>
        ///<remarks>
        ///
        ///The following example creates a GUI window for manipulating a Particle System.</remarks>
        ///<param name="withChildren">Stop all child Particle Systems as well.</param>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///
        ///public class ParticleSystemControllerWindow : MonoBehaviour
        ///{
        ///    ParticleSystem system
        ///    {
        ///        get
        ///        {
        ///            if (_CachedSystem == null)
        ///                _CachedSystem = GetComponent<ParticleSystem>();
        ///            return _CachedSystem;
        ///        }
        ///    }
        ///    private ParticleSystem _CachedSystem;
        ///
        ///    public Rect windowRect = new Rect(0, 0, 300, 120);
        ///
        ///    public bool includeChildren = true;
        ///
        ///    void OnGUI()
        ///    {
        ///        windowRect = GUI.Window("ParticleController".GetHashCode(), windowRect, DrawWindowContents, system.name);
        ///    }
        ///
        ///    void DrawWindowContents(int windowId)
        ///    {
        ///        if (system)
        ///        {
        ///            GUILayout.BeginHorizontal();
        ///            GUILayout.Toggle(system.isPlaying, "Playing");
        ///            GUILayout.Toggle(system.isEmitting, "Emitting");
        ///            GUILayout.Toggle(system.isPaused, "Paused");
        ///            GUILayout.EndHorizontal();
        ///
        ///            GUILayout.BeginHorizontal();
        ///            if (GUILayout.Button("Play"))
        ///                system.Play(includeChildren);
        ///            if (GUILayout.Button("Pause"))
        ///                system.Pause(includeChildren);
        ///            if (GUILayout.Button("Stop Emitting"))
        ///                system.Stop(includeChildren, ParticleSystemStopBehavior.StopEmitting);
        ///            if (GUILayout.Button("Stop & Clear"))
        ///                system.Stop(includeChildren, ParticleSystemStopBehavior.StopEmittingAndClear);
        ///            GUILayout.EndHorizontal();
        ///
        ///            includeChildren = GUILayout.Toggle(includeChildren, "Include Children");
        ///
        ///            GUILayout.BeginHorizontal();
        ///            GUILayout.Label("Time(" + system.time + ")");
        ///            GUILayout.Label("Particle Count(" + system.particleCount + ")");
        ///            GUILayout.EndHorizontal();
        ///        }
        ///        else
        ///            GUILayout.Label("No Particle System found");
        ///
        ///        GUI.DragWindow();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Play" />
        ///<seealso cref="Pause" />
        public void Stop([DefaultValue("true")] bool withChildren) { Stop(withChildren, ParticleSystemStopBehavior.StopEmitting); }
        ///<summary>Stops playing the Particle System using the supplied stop behaviour.</summary>
        ///<remarks>
        ///
        ///The following example creates a GUI window for manipulating a Particle System.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///
        ///public class ParticleSystemControllerWindow : MonoBehaviour
        ///{
        ///    ParticleSystem system
        ///    {
        ///        get
        ///        {
        ///            if (_CachedSystem == null)
        ///                _CachedSystem = GetComponent<ParticleSystem>();
        ///            return _CachedSystem;
        ///        }
        ///    }
        ///    private ParticleSystem _CachedSystem;
        ///
        ///    public Rect windowRect = new Rect(0, 0, 300, 120);
        ///
        ///    public bool includeChildren = true;
        ///
        ///    void OnGUI()
        ///    {
        ///        windowRect = GUI.Window("ParticleController".GetHashCode(), windowRect, DrawWindowContents, system.name);
        ///    }
        ///
        ///    void DrawWindowContents(int windowId)
        ///    {
        ///        if (system)
        ///        {
        ///            GUILayout.BeginHorizontal();
        ///            GUILayout.Toggle(system.isPlaying, "Playing");
        ///            GUILayout.Toggle(system.isEmitting, "Emitting");
        ///            GUILayout.Toggle(system.isPaused, "Paused");
        ///            GUILayout.EndHorizontal();
        ///
        ///            GUILayout.BeginHorizontal();
        ///            if (GUILayout.Button("Play"))
        ///                system.Play(includeChildren);
        ///            if (GUILayout.Button("Pause"))
        ///                system.Pause(includeChildren);
        ///            if (GUILayout.Button("Stop Emitting"))
        ///                system.Stop(includeChildren, ParticleSystemStopBehavior.StopEmitting);
        ///            if (GUILayout.Button("Stop & Clear"))
        ///                system.Stop(includeChildren, ParticleSystemStopBehavior.StopEmittingAndClear);
        ///            GUILayout.EndHorizontal();
        ///
        ///            includeChildren = GUILayout.Toggle(includeChildren, "Include Children");
        ///
        ///            GUILayout.BeginHorizontal();
        ///            GUILayout.Label("Time(" + system.time + ")");
        ///            GUILayout.Label("Particle Count(" + system.particleCount + ")");
        ///            GUILayout.EndHorizontal();
        ///        }
        ///        else
        ///            GUILayout.Label("No Particle System found");
        ///
        ///        GUI.DragWindow();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Play" />
        ///<seealso cref="Pause" />
        public void Stop() { Stop(true); }

        ///<summary>Remove all particles in the Particle System.</summary>
        ///<remarks>This method also removes the particles from any linked sub-emitters. Use the withChildren parameter to remove particles from child Particle Systems that are not sub-emitters of the system.</remarks>
        ///<param name="withChildren">Clear all child Particle Systems as well.</param>
        [FreeFunction(Name = "ParticleSystemScriptBindings::Clear", HasExplicitThis = true)]
        extern public void Clear([DefaultValue("true")] bool withChildren);
        ///<summary>Remove all particles in the Particle System.</summary>
        ///<remarks>This method also removes the particles from any linked sub-emitters. Use the withChildren parameter to remove particles from child Particle Systems that are not sub-emitters of the system.</remarks>
        public void Clear() { Clear(true); }

        ///<summary>Does the Particle System contain any live particles, or will it produce more?</summary>
        ///<param name="withChildren">Check all child Particle Systems as well.</param>
        ///<returns>True if the Particle System contains live particles or is still creating new particles. False if the Particle System has stopped emitting particles and all particles are dead.</returns>
        [FreeFunction(Name = "ParticleSystemScriptBindings::IsAlive", HasExplicitThis = true)]
        extern public bool IsAlive([DefaultValue("true")] bool withChildren);
        ///<summary>Does the Particle System contain any live particles, or will it produce more?</summary>
        ///<returns>True if the Particle System contains live particles or is still creating new particles. False if the Particle System has stopped emitting particles and all particles are dead.</returns>
        public bool IsAlive() { return IsAlive(true); }

        /// <summary>
        /// A callback used by polyspatial to capture emits driven by script and replay it.
        /// </summary>
        /// <remarks>
        /// Arguments, in order:
        /// <list type="number">
        /// <item><description>The ParticleSystem that emitted.</description></item>
        /// <item><description>The EmitParams that were passed, or default when Emit(int) was called.</description></item>
        /// <item><description>The number of particles requested.</description></item>
        /// <item><description>True when the call supplied EmitParams. The two overloads do not share a start path, so a consumer replaying the call needs to know which one to use.</description></item>
        /// </list>
        /// </remarks>
        [AutoStaticsCleanupOnCodeReload]
        internal static event Action<ParticleSystem, EmitParams, int, bool> onEmit;

        // Emission
        ///<summary>Emit <c>count</c> particles immediately.</summary>
        ///<param name="count">Number of particles to emit.</param>
        [RequiredByNativeCode]
        public void Emit(int count)
        {
            Emit_Internal(count);
            onEmit?.Invoke(this, default, count, false);
        }

        [NativeName("SyncJobs()->Emit")]
        extern private void Emit_Internal(int count);

        ///<summary>Emit a number of particles from script.</summary>
        ///<remarks>Setting properties in the emitParams will override those properties in the emitted particles. Any properties not modified will inherit the behavior specified in the inspector.</remarks>
        ///<param name="emitParams">Overidden particle properties.</param>
        ///<param name="count">Number of particles to emit.</param>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///
        /// // In this example, we have a Particle System emitting green particles; we then emit and override some properties every 2 seconds.
        ///public class EmitExample : MonoBehaviour
        ///{
        ///    public ParticleSystem system;
        ///
        ///    void Start()
        ///    {
        ///        // A simple particle material with no texture.
        ///        Material particleMaterial = new Material(Shader.Find("Particles/Standard Unlit"));
        ///
        ///        // Create a green Particle System.
        ///        var go = new GameObject("Particle System");
        ///        go.transform.Rotate(-90, 0, 0); // Rotate so the system emits upwards.
        ///        system = go.AddComponent<ParticleSystem>();
        ///        go.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
        ///        var mainModule = system.main;
        ///        mainModule.startColor = Color.green;
        ///        mainModule.startSize = 0.5f;
        ///
        ///        // Every 2 secs we will emit.
        ///        InvokeRepeating("DoEmit", 2.0f, 2.0f);
        ///    }
        ///
        ///    void DoEmit()
        ///    {
        ///        // Any parameters we assign in emitParams will override the current system's when we call Emit.
        ///        // Here we will override the start color and size.
        ///        var emitParams = new ParticleSystem.EmitParams();
        ///        emitParams.startColor = Color.red;
        ///        emitParams.startSize = 0.2f;
        ///        system.Emit(emitParams, 10);
        ///        system.Play(); // Continue normal emissions
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void Emit(EmitParams emitParams, int count)
        {
            EmitWithParams_Internal(emitParams, count);
            onEmit?.Invoke(this, emitParams, count, true);
        }

        [NativeName("SyncJobs()->EmitParticlesExternal")]
        extern private void EmitWithParams_Internal(EmitParams emitParams, int count);

        [NativeName("SyncJobs()->EmitParticleExternal")]
        extern private void EmitOld_Internal(ref ParticleSystem.Particle particle);

        // Fire a sub-emitter
        ///<summary>Triggers the specified sub emitter on all particles of the Particle System.</summary>
        ///<param name="subEmitterIndex">Index of the sub emitter to trigger.</param>
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
        ///        Material particleMaterial = new Material(Shader.Find("Particles/Standard Unlit"));
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
        public void TriggerSubEmitter(int subEmitterIndex)
        {
            TriggerSubEmitterForAllParticles(subEmitterIndex);
        }

        ///<summary>Triggers the specified sub emitter on the specified particle(s) of the Particle System.</summary>
        ///<param name="subEmitterIndex">Index of the sub emitter to trigger.</param>
        ///<param name="particle">Triggers the sub emtter on a single particle.</param>
        public void TriggerSubEmitter(int subEmitterIndex, ref ParticleSystem.Particle particle)
        {
            TriggerSubEmitterForParticle(subEmitterIndex, particle);
        }

        ///<summary>Triggers the specified sub emitter on the specified particle(s) of the Particle System.</summary>
        ///<param name="subEmitterIndex">Index of the sub emitter to trigger.</param>
        ///<param name="particles">Triggers the sub emtter on a list of particles.</param>
        public void TriggerSubEmitter(int subEmitterIndex, List<ParticleSystem.Particle> particles)
        {
            if (particles == null)
                TriggerSubEmitterForAllParticles(subEmitterIndex);
            else
                TriggerSubEmitterForParticles(subEmitterIndex, particles);
        }

        [FreeFunction(Name = "ParticleSystemScriptBindings::TriggerSubEmitterForParticle", HasExplicitThis = true)]
        extern internal void TriggerSubEmitterForParticle(int subEmitterIndex, ParticleSystem.Particle particle);

        [FreeFunction(Name = "ParticleSystemScriptBindings::TriggerSubEmitterForParticles", HasExplicitThis = true)]
        extern private void TriggerSubEmitterForParticles(int subEmitterIndex, List<ParticleSystem.Particle> particles);

        [FreeFunction(Name = "ParticleSystemScriptBindings::TriggerSubEmitterForAllParticles", HasExplicitThis = true)]
        extern private void TriggerSubEmitterForAllParticles(int subEmitterIndex);

        ///<summary>Reset the cache of reserved graphics memory used for efficient rendering of Particle Systems.</summary>
        ///<remarks>In order to efficiently write its data into graphics memory, the Particle System uses a pool of pre-allocated vertex buffers. When rendering a large number of particles, this pool will increase in size, and then maintain this size even when rendering fewer particles afterwards.
        ///
        ///Maintaining a large pool can make future rendering more efficient, in situations where a large number of particles are being rendered, and the pool is already pre-sized appropriately. However, a large pool uses more memory, so this function allows that memory to be released.
        ///
        ///This method is most useful when you know that you have finished rendering a high number of particles, and will not need to render a similar amount in the near future, i.e. when transitioning from a scene containing a large number of particles, to one where fewer particles will be rendered.
        ///
        ///Modern graphics APIs, such as DirectX12, Vulkan and Metal, do not use a pre-allocated pool of vertex buffers, because they can operate efficiently without it. This method does nothing on these devices.</remarks>
        [FreeFunction(Name = "ParticleSystemGeometryJob::ResetPreMappedBufferMemory")]
        extern public static void ResetPreMappedBufferMemory();

        ///<summary>Limits the amount of graphics memory Unity reserves for efficient rendering of Particle Systems.</summary>
        ///<remarks>To efficiently write particle data into graphics memory, Unity uses a pool of pre-allocated vertex buffers. When rendering a large number of particles, this pool will increase in size. If the number of particles decreases later, the pool still maintains this size.
        ///
        ///                    Maintaining a large pool can make future rendering more efficient, in situations where a large number of particles are being rendered, and the pool is already pre-sized appropriately. However, a large pool uses more memory, so this function allows you to set a limit on the number of buffers in the cache.
        ///
        ///                    If the total number of visible particles reaches the limit, Unity allocates new buffers and releases them on-demand within the frames that need them, rather than saving them for re-use on multiple frames. This can be slower but prevents the cache from using too much memory.
        ///
        ///                    Modern graphics APIs, such as DirectX12, Vulkan and Metal, do not use a pre-allocated pool of vertex buffers, because they can operate efficiently without it. This method does nothing on these devices.</remarks>
        ///<param name="vertexBuffersCount">The maximum number of cached vertex buffers.</param>
        ///<param name="indexBuffersCount">The maximum number of cached index buffers.</param>
        [FreeFunction(Name = "ParticleSystemGeometryJob::SetMaximumPreMappedBufferCounts")]
        extern public static void SetMaximumPreMappedBufferCounts(int vertexBuffersCount, int indexBuffersCount);

        ///<summary>Ensures that the <see cref="ParticleSystemJobs.ParticleSystemJobData.axisOfRotations">axisOfRotations</see> particle attribute array is allocated.</summary>
        ///<remarks>This is important if you want to use this attribute in a job, such as <see cref="IJobParticleSystem" />.</remarks>
        [NativeName("SetUsesAxisOfRotation")]
        extern public void AllocateAxisOfRotationAttribute();
        ///<summary>Ensures that the <see cref="ParticleSystemJobs.ParticleSystemJobData.meshIndices">meshIndices</see> particle attribute array is allocated.</summary>
        ///<remarks>This is important if you want to use this attribute in a job, such as <see cref="IJobParticleSystem" />.</remarks>
        [NativeName("SetUsesMeshIndex")]
        extern public void AllocateMeshIndexAttribute();
        ///<summary>Ensures that the <see cref="ParticleSystemJobs.ParticleSystemJobData.customData1">customData1</see> and <see cref="ParticleSystemJobs.ParticleSystemJobData.customData1">customData2</see> particle attribute arrays are allocated.</summary>
        ///<remarks>This is important if you want to use either of these attributes in a job, such as <see cref="IJobParticleSystem" />.</remarks>
        ///<param name="stream">The custom data stream to allocate.</param>
        [NativeName("SetUsesCustomData")]
        extern public void AllocateCustomDataAttribute(ParticleSystemCustomData stream);

        ///<summary>Determines whether the Particle System rotates its particles around only the Z axis, or whether the system specifies separate values for the X, Y and Z axes.</summary>
        extern public bool has3DParticleRotations { [NativeName("Has3DParticleRotations")] get; }
        ///<summary>Determines whether the Particle System uses a single value for the width and height (and depth, when using meshes), or if the system specifies different values for each axis.</summary>
        extern public bool hasNonUniformParticleSizes { [NativeName("HasNonUniformParticleSizes")] get; }

        unsafe extern internal void* GetManagedJobData();
        extern internal JobHandle GetManagedJobHandle();
        extern internal void SetManagedJobHandle(JobHandle handle);
        [FreeFunction("ScheduleManagedJob", ThrowsException = true)]
        unsafe internal static extern JobHandle ScheduleManagedJob(ref JobsUtility.JobScheduleParameters parameters, void* additionalData);
        [NativeMethod(IsThreadSafe = true)]
        unsafe internal static extern void CopyManagedJobData(void* systemPtr, out NativeParticleData particleData);
        internal static extern bool UserJobCanBeScheduled();


        [FreeFunction(Name = "ParticleSystemEditor::SetupDefaultParticleSystemType", HasExplicitThis = true)]
        extern internal void SetupDefaultType(ParticleSystemSubEmitterType type);

        [NativeProperty("GetState()->localToWorld", TargetType = TargetType.Field)]
        extern internal Matrix4x4 localToWorldMatrix { get; }

        [NativeName("GetNoiseModule().GeneratePreviewTexture")]
        extern internal void GenerateNoisePreviewTexture(Texture2D dst);

        extern internal void CalculateEffectUIData(ref int particleCount, ref float fastestParticle, ref float slowestParticle);

        extern internal int GenerateRandomSeed();

        [FreeFunction(Name = "ParticleSystemScriptBindings::CalculateEffectUISubEmitterData", HasExplicitThis = true)]
        extern internal bool CalculateEffectUISubEmitterData(ref int particleCount, ref float fastestParticle, ref float slowestParticle);

        [FreeFunction(Name = "ParticleSystemScriptBindings::CheckVertexStreamsMatchShader")]
        extern internal static bool CheckVertexStreamsMatchShader(bool hasTangent, bool hasColor, int texCoordChannelCount, Material material, ref bool tangentError, ref bool colorError, ref bool uvError);

        [FreeFunction(Name = "ParticleSystemScriptBindings::GetMaxTexCoordStreams")]
        extern internal static int GetMaxTexCoordStreams();

    }

    public partial struct ParticleCollisionEvent
    {
        [FreeFunction(Name = "ParticleSystemScriptBindings::InstanceIDToColliderComponent")]
        extern static private Component InstanceIDToColliderComponent(EntityId entityId);
    }

    internal class ParticleSystemExtensionsImpl
    {
        [FreeFunction(Name = "ParticleSystemScriptBindings::GetSafeCollisionEventSize")]
        extern internal static int GetSafeCollisionEventSize([NotNull] ParticleSystem ps);

        [FreeFunction(Name = "ParticleSystemScriptBindings::GetCollisionEventsDeprecated")]
        extern internal static int GetCollisionEventsDeprecated([NotNull] ParticleSystem ps, GameObject go, [Out] ParticleCollisionEvent[] collisionEvents);

        [FreeFunction(Name = "ParticleSystemScriptBindings::GetSafeTriggerParticlesSize")]
        extern internal static int GetSafeTriggerParticlesSize([NotNull] ParticleSystem ps, int type);

        [FreeFunction(Name = "ParticleSystemScriptBindings::GetCollisionEvents")]
        extern internal static int GetCollisionEvents([NotNull] ParticleSystem ps, [NotNull] GameObject go, [NotNull] List<ParticleCollisionEvent> collisionEvents);

        [FreeFunction(Name = "ParticleSystemScriptBindings::GetTriggerParticles")]
        extern internal static int GetTriggerParticles([NotNull] ParticleSystem ps, int type, [NotNull] List<ParticleSystem.Particle> particles);

        [FreeFunction(Name = "ParticleSystemScriptBindings::GetTriggerParticlesWithData")]
        extern internal static int GetTriggerParticlesWithData([NotNull] ParticleSystem ps, int type, [NotNull] List<ParticleSystem.Particle> particles, out ParticleSystem.ColliderData colliderData);

        [FreeFunction(Name = "ParticleSystemScriptBindings::SetTriggerParticles")]
        extern internal static void SetTriggerParticles([NotNull] ParticleSystem ps, int type, [NotNull] List<ParticleSystem.Particle> particles, int offset, int count);
    }

    public static partial class ParticlePhysicsExtensions
    {
        ///<summary>Safe array size for use with <see cref="ParticlePhysicsExtensions.GetCollisionEvents" />.</summary>
        ///<remarks>This is guaranteed to be large enough to use with <see cref="ParticlePhysicsExtensions.GetCollisionEvents" /> for the current frame. GetCollisionEvents may return fewer elements for some GameObjects though.</remarks>
        public static int GetSafeCollisionEventSize(this ParticleSystem ps)
        {
            return ParticleSystemExtensionsImpl.GetSafeCollisionEventSize(ps);
        }

        ///<summary>Get the particle collision events for a GameObject. Returns the number of events written to the array.</summary>
        ///<remarks>This method is typically called from <see cref="M:UnityEngine.MonoBehaviour.OnParticleCollision" /> in response to a collision callback.
        ///
        ///If the array used is too short, the list of collision events will be truncated. This means you will not have every event that occurred. To avoid this use <see cref="ParticlePhysicsExtensions.GetSafeCollisionEventSize" /> to determine an appropriate array size prior the call.</remarks>
        ///<param name="go">The GameObject for which to retrieve collision events.</param>
        ///<param name="collisionEvents">Array to write collision events to.</param>
        ///<param name="ps">The Particle System that owns the potentially colliding particles.</param>
        ///<returns>The number of collision events.</returns>
        ///<seealso cref="M:UnityEngine.MonoBehaviour.OnParticleCollision" />
        public static int GetCollisionEvents(this ParticleSystem ps, GameObject go, List<ParticleCollisionEvent> collisionEvents)
        {
            return ParticleSystemExtensionsImpl.GetCollisionEvents(ps, go, collisionEvents);
        }

        ///<summary>Safe array size for use with <see cref="M:UnityEngine.ParticlePhysicsExtensions.GetTriggerParticles" />.</summary>
        ///<remarks>This is guaranteed to be large enough to use with <see cref="M:UnityEngine.ParticlePhysicsExtensions.GetTriggerParticles" /> for the current frame. GetTriggerParticles may return fewer elements for some GameObjects though.</remarks>
        ///<param name="ps">Particle system.</param>
        ///<param name="type">Type of trigger to return size for.</param>
        ///<returns>Number of particles with this trigger event type.</returns>
        public static int GetSafeTriggerParticlesSize(this ParticleSystem ps, ParticleSystemTriggerEventType type)
        {
            return ParticleSystemExtensionsImpl.GetSafeTriggerParticlesSize(ps, (int)type);
        }

        ///<summary>Get the particles that met the condition in the particle trigger module. Returns the number of particles written to the array.</summary>
        ///<remarks>This method is typically called from <see cref="M:UnityEngine.MonoBehaviour.OnParticleTrigger" /> in response to a trigger callback.</remarks>
        ///<param name="ps">Particle system.</param>
        ///<param name="type">Type of trigger to return particles for.</param>
        ///<param name="particles">The array of particles matching the trigger event type.</param>
        ///<returns>Number of particles with this trigger event type.</returns>
        public static int GetTriggerParticles(this ParticleSystem ps, ParticleSystemTriggerEventType type, List<ParticleSystem.Particle> particles)
        {
            return ParticleSystemExtensionsImpl.GetTriggerParticles(ps, (int)type, particles);
        }

        public static int GetTriggerParticles(this ParticleSystem ps, ParticleSystemTriggerEventType type, List<ParticleSystem.Particle> particles, out ParticleSystem.ColliderData colliderData)
        {
            if (type == ParticleSystemTriggerEventType.Exit)
                throw new InvalidOperationException("Querying the collider data for the Exit event is not currently supported.");
            else if (type == ParticleSystemTriggerEventType.Outside)
                throw new InvalidOperationException("Querying the collider data for the Outside event is not supported, because when a particle is outside the collision volume, it is always outside every collider.");

            colliderData = new ParticleSystem.ColliderData();
            return ParticleSystemExtensionsImpl.GetTriggerParticlesWithData(ps, (int)type, particles, out colliderData);
        }

        ///<summary>Write modified particles back to the Particle System, during a call to OnParticleTrigger.</summary>
        ///<param name="ps">Particle system.</param>
        ///<param name="type">Type of trigger to set particles for.</param>
        ///<param name="particles">Particle array.</param>
        ///<param name="offset">Offset into the array, if you only want to write back a subset of the returned particles.</param>
        ///<param name="count">Number of particles to write, if you only want to write back a subset of the returned particles.</param>
        ///<seealso cref="M:UnityEngine.ParticlePhysicsExtensions.GetTriggerParticles" />
        public static void SetTriggerParticles(this ParticleSystem ps, ParticleSystemTriggerEventType type, List<ParticleSystem.Particle> particles, int offset, int count)
        {
            if (particles == null) throw new ArgumentNullException("particles");
            if (offset >= particles.Count) throw new ArgumentOutOfRangeException("offset", "offset should be smaller than the size of the particles list.");
            if ((offset + count) >= particles.Count) throw new ArgumentOutOfRangeException("count", "offset+count should be smaller than the size of the particles list.");

            ParticleSystemExtensionsImpl.SetTriggerParticles(ps, (int)type, particles, offset, count);
        }

        ///<summary>Write modified particles back to the Particle System, during a call to OnParticleTrigger.</summary>
        ///<param name="ps">Particle system.</param>
        ///<param name="type">Type of trigger to set particles for.</param>
        ///<param name="particles">Particle array.</param>
        ///<seealso cref="M:UnityEngine.ParticlePhysicsExtensions.GetTriggerParticles" />
        public static void SetTriggerParticles(this ParticleSystem ps, ParticleSystemTriggerEventType type, List<ParticleSystem.Particle> particles)
        {
            ParticleSystemExtensionsImpl.SetTriggerParticles(ps, (int)type, particles, 0, particles.Count);
        }
    }
}
