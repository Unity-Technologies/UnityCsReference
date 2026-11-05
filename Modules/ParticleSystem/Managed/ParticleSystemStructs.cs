// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    public partial class ParticleSystem
    {
        ///<summary>Script interface for a Burst.</summary>
        ///<remarks>A burst is a particle emission event, where the system emits a number of particles at the same time.</remarks>
        ///<seealso cref="ParticleSystem.emission" />
        ///<seealso cref="M:UnityEngine.ParticleSystem.EmissionModule.SetBursts" />
        ///<seealso cref="ParticleSystem.EmissionModule.GetBursts" />
        [StructLayout(LayoutKind.Sequential), NativeType(CodegenOptions.Custom, "MonoBurst"), NativeHeader("Runtime/Scripting/ScriptingCommonStructDefinitions.h")]
        public partial struct Burst
        {
            ///<summary>Construct a new Burst with a time and count.</summary>
            ///<param name="_time">Time to emit the burst.</param>
            ///<param name="_count">Number of particles to emit.</param>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            /// // Create a looping Particle System.
            /// // At 0, 1 and 2 secs the number of particles in each loop
            /// // are changed from 10, to 50, then to 100.
            /// // The loops repeat after 3 seconds.
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    public Material part;
            ///
            ///    void Start()
            ///    {
            ///        // create a red ground plane
            ///        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ///        ground.transform.rotation = Quaternion.Euler(90, 0, 0);
            ///        ground.transform.localScale = new Vector3(10, 10, 10);
            ///        ground.GetComponent<Renderer>().material.color = Color.red;
            ///
            ///        // rotate the GameObject so particles rise up in the y-axis
            ///        gameObject.transform.rotation = Quaternion.Euler(-90, 0, 0);
            ///        gameObject.AddComponent<ParticleSystem>();
            ///
            ///        // create the ParticleSystem
            ///        ParticleSystem ps;
            ///        ps = gameObject.GetComponent<ParticleSystem>();
            ///
            ///        ps.Stop();
            ///
            ///        // set the MainModule default values
            ///        var main = ps.main;
            ///        main.startColor = Color.yellow;
            ///        main.duration = 3;
            ///
            ///        // create a cone and change it into a cylinder
            ///        var shape = ps.shape;
            ///        shape.shapeType = ParticleSystemShapeType.Cone;
            ///        shape.angle = 0.0f;
            ///        shape.radius = 2.0f;
            ///        shape.radiusThickness = 0.0f;
            ///
            ///        // use the passed in material
            ///        gameObject.GetComponent<ParticleSystemRenderer>().material = part;
            ///
            ///        // set up the emission to generate particles
            ///        var em = ps.emission;
            ///        em.enabled = true;
            ///        em.rateOverTime = 0;
            ///
            ///        em.SetBursts(
            ///            new ParticleSystem.Burst[]
            ///            {
            ///                new ParticleSystem.Burst(0.0f, 10),
            ///                new ParticleSystem.Burst(1.0f, 50),
            ///                new ParticleSystem.Burst(2.0f, 100)
            ///            });
            ///
            ///        ps.Play();
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="M:UnityEngine.ParticleSystem.EmissionModule.SetBursts" />
            ///<seealso cref="ParticleSystem.EmissionModule.GetBursts" />
            public Burst(float _time, short _count) { m_Time = _time; m_Count = MinMaxCurveBlittable.FromMixMaxCurve(_count); m_RepeatCount = 0; m_RepeatInterval = 0.0f; m_InvProbability = 0.0f; }
            ///<summary>Construct a new Burst with a time and count.</summary>
            ///<param name="_time">Time to emit the burst.</param>
            ///<param name="_minCount">Minimum number of particles to emit.</param>
            ///<param name="_maxCount">Maximum number of particles to emit.</param>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            /// // Create a looping Particle System.
            /// // At 0, 1 and 2 secs the number of particles in each loop
            /// // are changed from 10, to 50, then to 100.
            /// // The loops repeat after 3 seconds.
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    public Material part;
            ///
            ///    void Start()
            ///    {
            ///        // create a red ground plane
            ///        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ///        ground.transform.rotation = Quaternion.Euler(90, 0, 0);
            ///        ground.transform.localScale = new Vector3(10, 10, 10);
            ///        ground.GetComponent<Renderer>().material.color = Color.red;
            ///
            ///        // rotate the GameObject so particles rise up in the y-axis
            ///        gameObject.transform.rotation = Quaternion.Euler(-90, 0, 0);
            ///        gameObject.AddComponent<ParticleSystem>();
            ///
            ///        // create the ParticleSystem
            ///        ParticleSystem ps;
            ///        ps = gameObject.GetComponent<ParticleSystem>();
            ///
            ///        ps.Stop();
            ///
            ///        // set the MainModule default values
            ///        var main = ps.main;
            ///        main.startColor = Color.yellow;
            ///        main.duration = 3;
            ///
            ///        // create a cone and change it into a cylinder
            ///        var shape = ps.shape;
            ///        shape.shapeType = ParticleSystemShapeType.Cone;
            ///        shape.angle = 0.0f;
            ///        shape.radius = 2.0f;
            ///        shape.radiusThickness = 0.0f;
            ///
            ///        // use the passed in material
            ///        gameObject.GetComponent<ParticleSystemRenderer>().material = part;
            ///
            ///        // set up the emission to generate particles
            ///        var em = ps.emission;
            ///        em.enabled = true;
            ///        em.rateOverTime = 0;
            ///
            ///        em.SetBursts(
            ///            new ParticleSystem.Burst[]
            ///            {
            ///                new ParticleSystem.Burst(0.0f, 10),
            ///                new ParticleSystem.Burst(1.0f, 50),
            ///                new ParticleSystem.Burst(2.0f, 100)
            ///            });
            ///
            ///        ps.Play();
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="M:UnityEngine.ParticleSystem.EmissionModule.SetBursts" />
            ///<seealso cref="ParticleSystem.EmissionModule.GetBursts" />
            public Burst(float _time, short _minCount, short _maxCount) { m_Time = _time; m_Count = MinMaxCurveBlittable.FromMixMaxCurve(new MinMaxCurve(_minCount, _maxCount)); m_RepeatCount = 0; m_RepeatInterval = 0.0f; m_InvProbability = 0.0f; }
            ///<summary>Construct a new Burst with a time and count.</summary>
            ///<param name="_time">Time to emit the burst.</param>
            ///<param name="_minCount">Minimum number of particles to emit.</param>
            ///<param name="_maxCount">Maximum number of particles to emit.</param>
            ///<param name="_cycleCount">Specifies how many times the system should play the burst. Set this to 0 to make it play indefinitely.</param>
            ///<param name="_repeatInterval">How often to repeat the burst, in seconds.</param>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            /// // Create a looping Particle System.
            /// // At 0, 1 and 2 secs the number of particles in each loop
            /// // are changed from 10, to 50, then to 100.
            /// // The loops repeat after 3 seconds.
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    public Material part;
            ///
            ///    void Start()
            ///    {
            ///        // create a red ground plane
            ///        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ///        ground.transform.rotation = Quaternion.Euler(90, 0, 0);
            ///        ground.transform.localScale = new Vector3(10, 10, 10);
            ///        ground.GetComponent<Renderer>().material.color = Color.red;
            ///
            ///        // rotate the GameObject so particles rise up in the y-axis
            ///        gameObject.transform.rotation = Quaternion.Euler(-90, 0, 0);
            ///        gameObject.AddComponent<ParticleSystem>();
            ///
            ///        // create the ParticleSystem
            ///        ParticleSystem ps;
            ///        ps = gameObject.GetComponent<ParticleSystem>();
            ///
            ///        ps.Stop();
            ///
            ///        // set the MainModule default values
            ///        var main = ps.main;
            ///        main.startColor = Color.yellow;
            ///        main.duration = 3;
            ///
            ///        // create a cone and change it into a cylinder
            ///        var shape = ps.shape;
            ///        shape.shapeType = ParticleSystemShapeType.Cone;
            ///        shape.angle = 0.0f;
            ///        shape.radius = 2.0f;
            ///        shape.radiusThickness = 0.0f;
            ///
            ///        // use the passed in material
            ///        gameObject.GetComponent<ParticleSystemRenderer>().material = part;
            ///
            ///        // set up the emission to generate particles
            ///        var em = ps.emission;
            ///        em.enabled = true;
            ///        em.rateOverTime = 0;
            ///
            ///        em.SetBursts(
            ///            new ParticleSystem.Burst[]
            ///            {
            ///                new ParticleSystem.Burst(0.0f, 10),
            ///                new ParticleSystem.Burst(1.0f, 50),
            ///                new ParticleSystem.Burst(2.0f, 100)
            ///            });
            ///
            ///        ps.Play();
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="M:UnityEngine.ParticleSystem.EmissionModule.SetBursts" />
            ///<seealso cref="ParticleSystem.EmissionModule.GetBursts" />
            public Burst(float _time, short _minCount, short _maxCount, int _cycleCount, float _repeatInterval) { m_Time = _time; m_Count = MinMaxCurveBlittable.FromMixMaxCurve(new MinMaxCurve(_minCount, _maxCount)); m_RepeatCount = _cycleCount - 1; m_RepeatInterval = _repeatInterval; m_InvProbability = 0.0f; }
            ///<summary>Construct a new Burst with a time and count.</summary>
            ///<param name="_time">Time to emit the burst.</param>
            ///<param name="_count">Number of particles to emit.</param>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            /// // Create a looping Particle System.
            /// // At 0, 1 and 2 secs the number of particles in each loop
            /// // are changed from 10, to 50, then to 100.
            /// // The loops repeat after 3 seconds.
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    public Material part;
            ///
            ///    void Start()
            ///    {
            ///        // create a red ground plane
            ///        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ///        ground.transform.rotation = Quaternion.Euler(90, 0, 0);
            ///        ground.transform.localScale = new Vector3(10, 10, 10);
            ///        ground.GetComponent<Renderer>().material.color = Color.red;
            ///
            ///        // rotate the GameObject so particles rise up in the y-axis
            ///        gameObject.transform.rotation = Quaternion.Euler(-90, 0, 0);
            ///        gameObject.AddComponent<ParticleSystem>();
            ///
            ///        // create the ParticleSystem
            ///        ParticleSystem ps;
            ///        ps = gameObject.GetComponent<ParticleSystem>();
            ///
            ///        ps.Stop();
            ///
            ///        // set the MainModule default values
            ///        var main = ps.main;
            ///        main.startColor = Color.yellow;
            ///        main.duration = 3;
            ///
            ///        // create a cone and change it into a cylinder
            ///        var shape = ps.shape;
            ///        shape.shapeType = ParticleSystemShapeType.Cone;
            ///        shape.angle = 0.0f;
            ///        shape.radius = 2.0f;
            ///        shape.radiusThickness = 0.0f;
            ///
            ///        // use the passed in material
            ///        gameObject.GetComponent<ParticleSystemRenderer>().material = part;
            ///
            ///        // set up the emission to generate particles
            ///        var em = ps.emission;
            ///        em.enabled = true;
            ///        em.rateOverTime = 0;
            ///
            ///        em.SetBursts(
            ///            new ParticleSystem.Burst[]
            ///            {
            ///                new ParticleSystem.Burst(0.0f, 10),
            ///                new ParticleSystem.Burst(1.0f, 50),
            ///                new ParticleSystem.Burst(2.0f, 100)
            ///            });
            ///
            ///        ps.Play();
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="M:UnityEngine.ParticleSystem.EmissionModule.SetBursts" />
            ///<seealso cref="ParticleSystem.EmissionModule.GetBursts" />
            public Burst(float _time, MinMaxCurve _count) { m_Time = _time; m_Count = MinMaxCurveBlittable.FromMixMaxCurve(_count); m_RepeatCount = 0; m_RepeatInterval = 0.0f; m_InvProbability = 0.0f; }
            ///<summary>Construct a new Burst with a time and count.</summary>
            ///<param name="_time">Time to emit the burst.</param>
            ///<param name="_count">Number of particles to emit.</param>
            ///<param name="_cycleCount">Specifies how many times the system should play the burst. Set this to 0 to make it play indefinitely.</param>
            ///<param name="_repeatInterval">How often to repeat the burst, in seconds.</param>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///using System.Collections;
            ///
            /// // Create a looping Particle System.
            /// // At 0, 1 and 2 secs the number of particles in each loop
            /// // are changed from 10, to 50, then to 100.
            /// // The loops repeat after 3 seconds.
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    public Material part;
            ///
            ///    void Start()
            ///    {
            ///        // create a red ground plane
            ///        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ///        ground.transform.rotation = Quaternion.Euler(90, 0, 0);
            ///        ground.transform.localScale = new Vector3(10, 10, 10);
            ///        ground.GetComponent<Renderer>().material.color = Color.red;
            ///
            ///        // rotate the GameObject so particles rise up in the y-axis
            ///        gameObject.transform.rotation = Quaternion.Euler(-90, 0, 0);
            ///        gameObject.AddComponent<ParticleSystem>();
            ///
            ///        // create the ParticleSystem
            ///        ParticleSystem ps;
            ///        ps = gameObject.GetComponent<ParticleSystem>();
            ///
            ///        ps.Stop();
            ///
            ///        // set the MainModule default values
            ///        var main = ps.main;
            ///        main.startColor = Color.yellow;
            ///        main.duration = 3;
            ///
            ///        // create a cone and change it into a cylinder
            ///        var shape = ps.shape;
            ///        shape.shapeType = ParticleSystemShapeType.Cone;
            ///        shape.angle = 0.0f;
            ///        shape.radius = 2.0f;
            ///        shape.radiusThickness = 0.0f;
            ///
            ///        // use the passed in material
            ///        gameObject.GetComponent<ParticleSystemRenderer>().material = part;
            ///
            ///        // set up the emission to generate particles
            ///        var em = ps.emission;
            ///        em.enabled = true;
            ///        em.rateOverTime = 0;
            ///
            ///        em.SetBursts(
            ///            new ParticleSystem.Burst[]
            ///            {
            ///                new ParticleSystem.Burst(0.0f, 10),
            ///                new ParticleSystem.Burst(1.0f, 50),
            ///                new ParticleSystem.Burst(2.0f, 100)
            ///            });
            ///
            ///        ps.Play();
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="M:UnityEngine.ParticleSystem.EmissionModule.SetBursts" />
            ///<seealso cref="ParticleSystem.EmissionModule.GetBursts" />
            public Burst(float _time, MinMaxCurve _count, int _cycleCount, float _repeatInterval) { m_Time = _time; m_Count = MinMaxCurveBlittable.FromMixMaxCurve(_count); m_RepeatCount = _cycleCount - 1; m_RepeatInterval = _repeatInterval; m_InvProbability = 0.0f; }

            ///<summary>The time that each burst occurs.</summary>
            ///<remarks>Can be either Time or Distance, based on the current emission mode.</remarks>
            ///<seealso cref="ParticleSystem.EmissionModule.type" />
            public float time { get { return m_Time; } set { m_Time = value; } }                                                // The time the burst happens.
            ///<summary>Specify the number of particles to emit.</summary>
            public MinMaxCurve count { get { return MinMaxCurveBlittable.ToMinMaxCurve(m_Count); } set { m_Count = MinMaxCurveBlittable.FromMixMaxCurve(value); } }                                       // Number of particles to be emitted.
            ///<summary>The minimum number of particles to emit.</summary>
            public short minCount { get { return (short)m_Count.m_ConstantMin; } set { m_Count.m_ConstantMin = (short)value; } }    // Minimum number of particles to be emitted.
            ///<summary>The maximum number of particles to emit.</summary>
            public short maxCount { get { return (short)m_Count.m_ConstantMax; } set { m_Count.m_ConstantMax = (short)value; } }    // Maximum number of particles to be emitted.

            // How many times to play the burst.
            ///<summary>Specifies how many times the system should play the burst. Set this to 0 to make it play indefinitely.</summary>
            public int cycleCount
            {
                get
                {
                    return m_RepeatCount + 1;
                }
                set
                {
                    if (value < 0)
                        throw new ArgumentOutOfRangeException("cycleCount", "cycleCount must be at least 0: " + value);
                    m_RepeatCount = value - 1;
                }
            }

            // The interval between repeats of the burst.
            ///<summary>How often to repeat the burst, in seconds.</summary>
            public float repeatInterval
            {
                get
                {
                    return m_RepeatInterval;
                }
                set
                {
                    if (value <= 0.0f)
                        throw new ArgumentOutOfRangeException("repeatInterval", "repeatInterval must be greater than 0.0f: " + value);
                    m_RepeatInterval = value;
                }
            }

            // The chance a burst will trigger.
            ///<summary>The probability that the system triggers a burst.</summary>
            ///<remarks>Set this value between 0 and 1. Higher values increase the probability that the system triggers a burst.</remarks>
            public float probability
            {
                get
                {
                    return 1.0f - m_InvProbability;
                }
                set
                {
                    if (value < 0.0f || value > 1.0f)
                        throw new ArgumentOutOfRangeException("probability", "probability must be between 0.0f and 1.0f: " + value);
                    m_InvProbability = 1.0f - value;
                }
            }

            private float m_Time;
            private MinMaxCurveBlittable m_Count;
            private int m_RepeatCount; // externally, we use "cycles", because users preferred that, but internally, we must use something that defaults to 0, due to C# struct rules
            private float m_RepeatInterval;
            private float m_InvProbability; // internally, we must use something that defaults to 0, due to C# struct rules, so reverse the storage from 0-1 to 1-0
        }

        [Serializable]
        public partial struct MinMaxCurve
        {
            ///<summary>A single constant value for the entire curve.</summary>
            ///<param name="constant">Constant value.</param>
            public MinMaxCurve(float constant) { m_Mode = ParticleSystemCurveMode.Constant; m_CurveMultiplier = 0.0f; m_CurveMin = null; m_CurveMax = null; m_ConstantMin = 0.0f; m_ConstantMax = constant; }
            ///<summary>Use one curve when evaluating numbers along this Min-Max curve.</summary>
            ///<param name="multiplier">A multiplier to apply to the curve.</param>
            ///<param name="curve">A single curve to evaluate against.</param>
            public MinMaxCurve(float multiplier, AnimationCurve curve) { m_Mode = ParticleSystemCurveMode.Curve; m_CurveMultiplier = multiplier; m_CurveMin = null; m_CurveMax = curve; m_ConstantMin = 0.0f; m_ConstantMax = 0.0f; }
            ///<summary>Randomly select values based on the interval between the minimum and maximum curves.</summary>
            ///<param name="multiplier">A multiplier to apply to the curves.</param>
            ///<param name="min">The curve describing the minimum values to be evaluated.</param>
            ///<param name="max">The curve describing the maximum values to be evaluated.</param>
            public MinMaxCurve(float multiplier, AnimationCurve min, AnimationCurve max) { m_Mode = ParticleSystemCurveMode.TwoCurves; m_CurveMultiplier = multiplier; m_CurveMin = min; m_CurveMax = max; m_ConstantMin = 0.0f; m_ConstantMax = 0.0f; }
            ///<summary>Randomly select values based on the interval between the minimum and maximum constants.</summary>
            ///<param name="min">The constant describing the minimum values to be evaluated.</param>
            ///<param name="max">The constant describing the maximum values to be evaluated.</param>
            public MinMaxCurve(float min, float max) { m_Mode = ParticleSystemCurveMode.TwoConstants; m_CurveMultiplier = 0.0f; m_CurveMin = null; m_CurveMax = null; m_ConstantMin = min; m_ConstantMax = max; }

            ///<summary>Set the mode that the min-max curve uses to evaluate values.</summary>
            public ParticleSystemCurveMode mode { get { return m_Mode; } set { m_Mode = value; } }                      // The current curve mode.
            ///<summary>Set a multiplier to apply to the curves.</summary>
            ///<seealso cref="ParticleSystem.MinMaxCurve.curve" />
            ///<seealso cref="ParticleSystem.MinMaxCurve.curveMin" />
            ///<seealso cref="ParticleSystem.MinMaxCurve.curveMax" />
            public float curveMultiplier { get { return m_CurveMultiplier; } set { m_CurveMultiplier = value; } }       // The multiplier applied to the 0-1 curves.
            ///<summary>Set a curve for the upper bound.</summary>
            ///<remarks>You should set the values between 0 and 1. Use the curve multiplier to define the overall range of the curve.
            ///Useful when <see cref="ParticleSystem.MinMaxCurve.mode" /> is set to <see cref="ParticleSystemCurveMode.TwoCurves" />.</remarks>
            ///<seealso cref="P:ParticleSystem.MinMaxCurve.curveScalar" />
            public AnimationCurve curveMax { get { return m_CurveMax; } set { m_CurveMax = value; } }                   // The maximum curve.
            ///<summary>Set a curve for the lower bound.</summary>
            ///<remarks>You should set the values between 0 and 1. Use the curve multiplier to define the overall range of the curve.
            ///Useful when <see cref="ParticleSystem.MinMaxCurve.mode" /> is set to <see cref="ParticleSystemCurveMode.TwoCurves" />.</remarks>
            ///<seealso cref="P:ParticleSystem.MinMaxCurve.curveScalar" />
            public AnimationCurve curveMin { get { return m_CurveMin; } set { m_CurveMin = value; } }                   // The minimum curve.
            ///<summary>Set a constant for the upper bound.</summary>
            ///<remarks>Useful when <see cref="ParticleSystem.MinMaxCurve.mode" /> is set to <see cref="ParticleSystemCurveMode.TwoConstants" />.</remarks>
            public float constantMax { get { return m_ConstantMax; } set { m_ConstantMax = value; } }                   // The maximum constant.
            ///<summary>Set a constant for the lower bound.</summary>
            ///<remarks>Useful when <see cref="ParticleSystem.MinMaxCurve.mode" /> is set to <see cref="ParticleSystemCurveMode.TwoConstants" />.</remarks>
            public float constantMin { get { return m_ConstantMin; } set { m_ConstantMin = value; } }                   // The minimum constant.
            ///<summary>Set the constant value.</summary>
            ///<remarks>Useful when <see cref="ParticleSystem.MinMaxCurve.mode" /> is set to <see cref="ParticleSystemCurveMode.Constant" />.</remarks>
            public float constant { get { return m_ConstantMax; } set { m_ConstantMax = value; } }                      // The single constant.
            ///<summary>Set the curve.</summary>
            ///<remarks>You should set the values between 0 and 1. Use the curve multiplier to define the overall range of the curve.
            ///Useful when <see cref="ParticleSystem.MinMaxCurve.mode" /> is set to <see cref="ParticleSystemCurveMode.Curve" />.</remarks>
            public AnimationCurve curve { get { return m_CurveMax; } set { m_CurveMax = value; } }                      // The single curve.

            // Evaluate the curve
            ///<summary>Manually query the curve to calculate values based on what mode it is in.</summary>
            ///<remarks>This automatically clamps the time and lerpFactor properties between 0 and 1.</remarks>
            ///<param name="time">Normalized time (in the range 0 - 1, where 1 represents 100%) at which to evaluate the curve. This is valid when <see cref="ParticleSystem.MinMaxCurve.mode" /> is set to <see cref="ParticleSystemCurveMode.Curve" /> or <see cref="ParticleSystemCurveMode.TwoCurves" />.</param>
            ///<returns>Calculated curve/constant value.</returns>
            public float Evaluate(float time) { return Evaluate(time, 1.0f); }
            ///<summary>Manually query the curve to calculate values based on what mode it is in.</summary>
            ///<remarks>This automatically clamps the time and lerpFactor properties between 0 and 1.</remarks>
            ///<param name="time">Normalized time (in the range 0 - 1, where 1 represents 100%) at which to evaluate the curve. This is valid when <see cref="ParticleSystem.MinMaxCurve.mode" /> is set to <see cref="ParticleSystemCurveMode.Curve" /> or <see cref="ParticleSystemCurveMode.TwoCurves" />.</param>
            ///<param name="lerpFactor">Blend between the two curves/constants (Valid when <see cref="ParticleSystem.MinMaxCurve.mode" /> is set to <see cref="ParticleSystemCurveMode.TwoConstants" /> or <see cref="ParticleSystemCurveMode.TwoCurves" />).</param>
            ///<returns>Calculated curve/constant value.</returns>
            public float Evaluate(float time, float lerpFactor)
            {
                switch (mode)
                {
                    case ParticleSystemCurveMode.Constant:
                        return m_ConstantMax;
                    case ParticleSystemCurveMode.TwoCurves:
                        return Mathf.Lerp(m_CurveMin.Evaluate(time), m_CurveMax.Evaluate(time), lerpFactor) * m_CurveMultiplier;
                    case ParticleSystemCurveMode.TwoConstants:
                        return Mathf.Lerp(m_ConstantMin, m_ConstantMax, lerpFactor);
                    default: // ParticleSystemCurveMode.Curve:
                        return m_CurveMax.Evaluate(time) * m_CurveMultiplier;
                }
            }

            // Implicit conversion operator, to allow better syntax when using 1 float
            /// <exclude />
            static public implicit operator MinMaxCurve(float constant)
            {
                return new MinMaxCurve(constant);
            }

            [SerializeField] internal ParticleSystemCurveMode m_Mode;
            [SerializeField] internal float m_CurveMultiplier;
            [SerializeField] internal AnimationCurve m_CurveMin;
            [SerializeField] internal AnimationCurve m_CurveMax;
            [SerializeField] internal float m_ConstantMin;
            [SerializeField] internal float m_ConstantMax;
        }

        [Serializable, NativeType(CodegenOptions.Custom, "MonoMinMaxCurve"), NativeHeader("Runtime/Scripting/ScriptingCommonStructDefinitions.h")]
        [StructLayout(LayoutKind.Sequential)]
        [RequiredByNativeCode]
        internal struct MinMaxCurveBlittable
        {
            private ParticleSystemCurveMode m_Mode;
            private float m_CurveMultiplier;
            private IntPtr m_CurveMin;
            private IntPtr m_CurveMax;
            internal float m_ConstantMin;
            internal float m_ConstantMax;

            public static implicit operator MinMaxCurve(MinMaxCurveBlittable minMaxCurveBlittable) => ToMinMaxCurve(minMaxCurveBlittable);
            public static implicit operator MinMaxCurveBlittable(MinMaxCurve minMaxCurve) => FromMixMaxCurve(minMaxCurve);

            internal static MinMaxCurveBlittable FromMixMaxCurve(in MinMaxCurve minMaxCurve)
            {
                var minMaxCurveBlittable = new MinMaxCurveBlittable
                {
                    m_Mode = minMaxCurve.m_Mode,
                    m_CurveMultiplier = minMaxCurve.m_CurveMultiplier,
                    m_ConstantMin = minMaxCurve.m_ConstantMin,
                    m_ConstantMax = minMaxCurve.m_ConstantMax,
                };

                if (minMaxCurve.m_CurveMin != null)
                    minMaxCurveBlittable.m_CurveMin = minMaxCurve.m_CurveMin.m_Ptr;
                if (minMaxCurve.m_CurveMax != null)
                    minMaxCurveBlittable.m_CurveMax = minMaxCurve.m_CurveMax.m_Ptr;

                return minMaxCurveBlittable;
            }

            internal static MinMaxCurve ToMinMaxCurve(in MinMaxCurveBlittable minMaxCurveBlittable)
            {
                var minMaxCurve = new MinMaxCurve();

                minMaxCurve.m_Mode = minMaxCurveBlittable.m_Mode;
                minMaxCurve.m_CurveMultiplier = minMaxCurveBlittable.m_CurveMultiplier;
                if (minMaxCurveBlittable.m_CurveMin != IntPtr.Zero)
                    minMaxCurve.m_CurveMin = new AnimationCurve(minMaxCurveBlittable.m_CurveMin, false);
                if (minMaxCurveBlittable.m_CurveMax != IntPtr.Zero)
                    minMaxCurve.m_CurveMax = new AnimationCurve(minMaxCurveBlittable.m_CurveMax, false);
                minMaxCurve.m_ConstantMin = minMaxCurveBlittable.m_ConstantMin;
                minMaxCurve.m_ConstantMax = minMaxCurveBlittable.m_ConstantMax;

                return minMaxCurve;
            }
        }

        ///<summary>Script interface for a Min-Max Gradient.</summary>
        ///<remarks>This contains two <see cref="Gradient" />s, and returns a <see cref="Color" /> based on <see cref="ParticleSystem.MinMaxGradient.mode" />. Depending on the mode, this may return the value randomized.
        ///Gradients are edited via the ParticleSystem Inspector once a <see cref="ParticleSystemGradientMode" /> requiring them has been selected. Some modes do not require gradients, only colors.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        /// // This example shows setting a constant color value.
        ///public class ConstantColorExample : MonoBehaviour
        ///{
        ///    ParticleSystem myParticleSystem;
        ///    ParticleSystem.ColorOverLifetimeModule colorModule;
        ///
        ///    void Start()
        ///    {
        ///        // Get the system and the color module.
        ///        myParticleSystem = GetComponent<ParticleSystem>();
        ///        colorModule = myParticleSystem.colorOverLifetime;
        ///
        ///        GetValue();
        ///        SetValue();
        ///    }
        ///
        ///    void GetValue()
        ///    {
        ///        print("The constant color is " + colorModule.color.color);
        ///    }
        ///
        ///    void SetValue()
        ///    {
        ///        colorModule.color = Color.red;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        /// // This example shows using 2 colors to drive the color over lifetime.
        ///public class TwoConstantColorsExample : MonoBehaviour
        ///{
        ///    ParticleSystem myParticleSystem;
        ///    ParticleSystem.ColorOverLifetimeModule colorModule;
        ///
        ///    void Start()
        ///    {
        ///        // Get the system and the color module.
        ///        myParticleSystem = GetComponent<ParticleSystem>();
        ///        colorModule = myParticleSystem.colorOverLifetime;
        ///
        ///        GetValue();
        ///        SetValue();
        ///    }
        ///
        ///    void GetValue()
        ///    {
        ///        print(string.Format("The constant values are: min {0} max {1}.", colorModule.color.colorMin, colorModule.color.colorMax));
        ///    }
        ///
        ///    void SetValue()
        ///    {
        ///        colorModule.color = new ParticleSystem.MinMaxGradient(Color.green, Color.red);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        /// // This example shows using a gradient to drive the color over lifetime.
        ///public class GradientColorExample : MonoBehaviour
        ///{
        ///    ParticleSystem myParticleSystem;
        ///    ParticleSystem.ColorOverLifetimeModule colorModule;
        ///    Gradient ourGradient;
        ///
        ///    void Start()
        ///    {
        ///        // Get the system and the color module.
        ///        myParticleSystem = GetComponent<ParticleSystem>();
        ///        colorModule = myParticleSystem.colorOverLifetime;
        ///
        ///        // A simple 2 color gradient with a fixed alpha of 1.0f.
        ///        float alpha = 1.0f;
        ///        ourGradient = new Gradient();
        ///        ourGradient.SetKeys(
        ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
        ///            new GradientAlphaKey[] { new GradientAlphaKey(alpha, 0.0f), new GradientAlphaKey(alpha, 1.0f) }
        ///        );
        ///
        ///        // Apply the gradient.
        ///        colorModule.color = ourGradient;
        ///
        ///        // In 5 seconds we will modify the gradient.
        ///        Invoke("ModifyGradient", 5.0f);
        ///    }
        ///
        ///    void ModifyGradient()
        ///    {
        ///        // Reduce the alpha
        ///        float alpha = 0.5f;
        ///        ourGradient.SetKeys(
        ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
        ///            new GradientAlphaKey[] { new GradientAlphaKey(alpha, 0.0f), new GradientAlphaKey(alpha, 1.0f) }
        ///        );
        ///
        ///        // Apply the changed gradient.
        ///        colorModule.color = ourGradient;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        /// // This example shows using 2 gradients to drive the color over lifetime.
        ///public class TwoGradientColorExample : MonoBehaviour
        ///{
        ///    ParticleSystem myParticleSystem;
        ///    ParticleSystem.ColorOverLifetimeModule colorModule;
        ///
        ///    Gradient ourGradientMin;
        ///    Gradient ourGradientMax;
        ///
        ///    void Start()
        ///    {
        ///        // Get the system and the emission module.
        ///        myParticleSystem = GetComponent<ParticleSystem>();
        ///        colorModule = myParticleSystem.colorOverLifetime;
        ///
        ///        // A simple 2 color gradient with a fixed alpha of 1.0f.
        ///        float alpha1 = 1.0f;
        ///        ourGradientMin = new Gradient();
        ///        ourGradientMin.SetKeys(
        ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
        ///            new GradientAlphaKey[] { new GradientAlphaKey(alpha1, 0.0f), new GradientAlphaKey(alpha1, 1.0f) }
        ///        );
        ///
        ///        // A simple 2 color gradient with a fixed alpha of 0.0f.
        ///        float alpha2 = 0.0f;
        ///        ourGradientMax = new Gradient();
        ///        ourGradientMax.SetKeys(
        ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
        ///            new GradientAlphaKey[] { new GradientAlphaKey(alpha2, 0.0f), new GradientAlphaKey(alpha2, 1.0f) }
        ///        );
        ///
        ///        // Apply the gradients.
        ///        colorModule.color = new ParticleSystem.MinMaxGradient(ourGradientMin, ourGradientMax);
        ///
        ///        // In 5 seconds we will modify the gradient.
        ///        Invoke("ModifyGradient", 5.0f);
        ///    }
        ///
        ///    void ModifyGradient()
        ///    {
        ///        // Reduce the alpha
        ///        float alpha = 0.5f;
        ///        ourGradientMin.SetKeys(
        ///            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
        ///            new GradientAlphaKey[] { new GradientAlphaKey(alpha, 0.0f), new GradientAlphaKey(alpha, 1.0f) }
        ///        );
        ///
        ///        // Apply the changed gradients.
        ///        colorModule.color = new ParticleSystem.MinMaxGradient(ourGradientMin, ourGradientMax);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        /// // This example shows how to retrieve existing color and alpha keys from a MinMaxGradient
        ///public class ReadGradientExample : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        // Get the system and the color module.
        ///        var myParticleSystem = GetComponent<ParticleSystem>();
        ///        var colorModule = myParticleSystem.colorOverLifetime;
        ///
        ///        // Get the gradient (assuming the MinMaxGradient is in Gradient mode)
        ///        Gradient gradient = colorModule.color.gradient;
        ///
        ///        // Get the keys
        ///        GradientColorKey[] colorKeys = gradient.colorKeys;
        ///        GradientAlphaKey[] alphaKeys = gradient.alphaKeys;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="ParticleSystem" />
        [Serializable]
        public partial struct MinMaxGradient
        {
            ///<summary>A single constant color for the entire gradient.</summary>
            ///<param name="color">Constant color.</param>
            public MinMaxGradient(Color color) { m_Mode = ParticleSystemGradientMode.Color; m_GradientMin = null; m_GradientMax = null; m_ColorMin = Color.black; m_ColorMax = color; }
            ///<summary>Use one gradient when evaluating numbers along this Min-Max Gradient.</summary>
            ///<param name="gradient">A single gradient for evaluating against.</param>
            public MinMaxGradient(Gradient gradient) { m_Mode = ParticleSystemGradientMode.Gradient; m_GradientMin = null; m_GradientMax = gradient; m_ColorMin = Color.black; m_ColorMax = Color.black; }
            ///<summary>Randomly select colors based on the interval between the minimum and maximum constants.</summary>
            ///<param name="min">The constant color describing the minimum colors to be evaluated.</param>
            ///<param name="max">The constant color describing the maximum colors to be evaluated.</param>
            public MinMaxGradient(Color min, Color max) { m_Mode = ParticleSystemGradientMode.TwoColors; m_GradientMin = null; m_GradientMax = null; m_ColorMin = min; m_ColorMax = max; }
            ///<summary>Randomly select colors based on the interval between the minimum and maximum gradients.</summary>
            ///<param name="min">The gradient describing the minimum colors to be evaluated.</param>
            ///<param name="max">The gradient describing the maximum colors to be evaluated.</param>
            public MinMaxGradient(Gradient min, Gradient max) { m_Mode = ParticleSystemGradientMode.TwoGradients; m_GradientMin = min; m_GradientMax = max; m_ColorMin = Color.black; m_ColorMax = Color.black; }

            ///<summary>Set the mode that the Min-Max Gradient uses to evaluate colors.</summary>
            public ParticleSystemGradientMode mode { get { return m_Mode; } set { m_Mode = value; } }           // The current gradient mode.
            ///<summary>Set a gradient for the upper bound.</summary>
            ///<remarks>Useful when <see cref="ParticleSystem.MinMaxGradient.mode" /> is set to <see cref="ParticleSystemGradientMode.TwoGradients" />.</remarks>
            public Gradient gradientMax { get { return m_GradientMax; } set { m_GradientMax = value; } }        // The maximum gradient.
            ///<summary>Set a gradient for the lower bound.</summary>
            ///<remarks>Useful when <see cref="ParticleSystem.MinMaxGradient.mode" /> is set to <see cref="ParticleSystemGradientMode.TwoGradients" />.</remarks>
            public Gradient gradientMin { get { return m_GradientMin; } set { m_GradientMin = value; } }        // The minimum gradient.
            ///<summary>Set a constant color for the upper bound.</summary>
            ///<remarks>Useful when <see cref="ParticleSystem.MinMaxGradient.mode" /> is set to <see cref="ParticleSystemGradientMode.TwoColors" />.</remarks>
            public Color colorMax { get { return m_ColorMax; } set { m_ColorMax = value; } }                    // The maximum color.
            ///<summary>Set a constant color for the lower bound.</summary>
            ///<remarks>Useful when <see cref="ParticleSystem.MinMaxGradient.mode" /> is set to <see cref="ParticleSystemGradientMode.TwoColors" />.</remarks>
            public Color colorMin { get { return m_ColorMin; } set { m_ColorMin = value; } }                    // The minimum color.
            ///<summary>Set a constant color.</summary>
            ///<remarks>Useful when <see cref="ParticleSystem.MinMaxGradient.mode" /> is set to <see cref="ParticleSystemGradientMode.Color" />.</remarks>
            public Color color { get { return m_ColorMax; } set { m_ColorMax = value; } }                       // The single color.
            ///<summary>Set the gradient.</summary>
            ///<remarks>Useful when <see cref="ParticleSystem.MinMaxGradient.mode" /> is set to <see cref="ParticleSystemGradientMode.Gradient" />.</remarks>
            public Gradient gradient { get { return m_GradientMax; } set { m_GradientMax = value; } }           // The single gradient.

            // Evaluate the gradient
            ///<summary>Manually query the gradient to calculate colors based on what mode it is in.</summary>
            ///<remarks>This automatically clamps the time and lerpFactor properties between 0 and 1.</remarks>
            ///<param name="time">Normalized time (in the range 0 - 1, where 1 represents 100%) at which to evaluate the gradient. This is valid when <see cref="ParticleSystem.MinMaxGradient.mode" /> is set to <see cref="ParticleSystemGradientMode.Gradient" /> or <see cref="ParticleSystemGradientMode.TwoGradients" />.</param>
            ///<returns>Calculated gradient/color value.</returns>
            public Color Evaluate(float time) { return Evaluate(time, 1.0f); }
            ///<summary>Manually query the gradient to calculate colors based on what mode it is in.</summary>
            ///<remarks>This automatically clamps the time and lerpFactor properties between 0 and 1.</remarks>
            ///<param name="time">Normalized time (in the range 0 - 1, where 1 represents 100%) at which to evaluate the gradient. This is valid when <see cref="ParticleSystem.MinMaxGradient.mode" /> is set to <see cref="ParticleSystemGradientMode.Gradient" /> or <see cref="ParticleSystemGradientMode.TwoGradients" />.</param>
            ///<param name="lerpFactor">Blend between the two gradients/colors (Valid when <see cref="ParticleSystem.MinMaxGradient.mode" /> is set to <see cref="ParticleSystemGradientMode.TwoColors" /> or <see cref="ParticleSystemGradientMode.TwoGradients" />).</param>
            ///<returns>Calculated gradient/color value.</returns>
            public Color Evaluate(float time, float lerpFactor)
            {
                switch (m_Mode)
                {
                    case ParticleSystemGradientMode.Color:
                        return m_ColorMax;
                    case ParticleSystemGradientMode.TwoColors:
                        return Color.Lerp(m_ColorMin, m_ColorMax, lerpFactor);
                    case ParticleSystemGradientMode.TwoGradients:
                        return Color.Lerp(m_GradientMin.Evaluate(time), m_GradientMax.Evaluate(time), lerpFactor);
                    case ParticleSystemGradientMode.RandomColor:
                        return m_GradientMax.Evaluate(lerpFactor);
                    default: // ParticleSystemGradientMode.Gradient
                        return m_GradientMax.Evaluate(time);
                }
            }

            // Implicit conversion operator, to allow better syntax when using 1 color or 1 gradient
            ///<exclude />
            static public implicit operator MinMaxGradient(Color color)
            {
                return new MinMaxGradient(color);
            }

            ///<exclude />
            static public implicit operator MinMaxGradient(Gradient gradient)
            {
                return new MinMaxGradient(gradient);
            }

            [SerializeField] internal ParticleSystemGradientMode m_Mode;
            [SerializeField] internal Gradient m_GradientMin;
            [SerializeField] internal Gradient m_GradientMax;
            [SerializeField] internal Color m_ColorMin;
            [SerializeField] internal Color m_ColorMax;
        }

        [Serializable, NativeType(CodegenOptions.Custom, "MonoMinMaxGradient"), NativeHeader("Runtime/Scripting/ScriptingCommonStructDefinitions.h")]
        [StructLayout(LayoutKind.Sequential)]
        [RequiredByNativeCode]
        internal struct MinMaxGradientBlittable
        {
            private ParticleSystemGradientMode m_Mode;
            private IntPtr m_GradientMin;
            private IntPtr m_GradientMax;
            private Color m_ColorMin;
            private Color m_ColorMax;

            public static implicit operator MinMaxGradient(MinMaxGradientBlittable minMaxGradientBlittable) => ToMinMaxGradient(minMaxGradientBlittable);
            public static implicit operator MinMaxGradientBlittable(MinMaxGradient minMaxGradient) => FromMixMaxGradient(minMaxGradient);

            internal static MinMaxGradientBlittable FromMixMaxGradient(in MinMaxGradient minMaxGradient)
            {
                var minMaxGradientBlittable = new MinMaxGradientBlittable
                {
                    m_Mode = minMaxGradient.m_Mode,
                    m_ColorMin = minMaxGradient.m_ColorMin,
                    m_ColorMax = minMaxGradient.m_ColorMax,
                };

                if (minMaxGradient.m_GradientMin != null)
                    minMaxGradientBlittable.m_GradientMin = minMaxGradient.m_GradientMin.m_Ptr;
                if (minMaxGradient.m_GradientMax != null)
                    minMaxGradientBlittable.m_GradientMax = minMaxGradient.m_GradientMax.m_Ptr;

                return minMaxGradientBlittable;
            }

            internal static MinMaxGradient ToMinMaxGradient(in MinMaxGradientBlittable minMaxGradientBlittable)
            {
                var minMaxGradient = new MinMaxGradient();

                minMaxGradient.m_Mode = minMaxGradientBlittable.m_Mode;
                if (minMaxGradientBlittable.m_GradientMin != IntPtr.Zero)
                    minMaxGradient.m_GradientMin = new Gradient(minMaxGradientBlittable.m_GradientMin);
                if (minMaxGradientBlittable.m_GradientMax != IntPtr.Zero)
                    minMaxGradient.m_GradientMax = new Gradient(minMaxGradientBlittable.m_GradientMax);
                minMaxGradient.m_ColorMin = minMaxGradientBlittable.m_ColorMin;
                minMaxGradient.m_ColorMax = minMaxGradientBlittable.m_ColorMax;

                return minMaxGradient;
            }

        }

        [RequiredByNativeCode("particleSystemParticle", Optional = true)]
        [StructLayout(LayoutKind.Sequential)]
        public partial struct Particle
        {
            [Flags]
            private enum Flags
            {
                Size3D = 1 << 0,
                Rotation3D = 1 << 1,
                MeshIndex = 1 << 2
            }

            ///<summary>The position of the particle.</summary>
            ///<remarks>The position is defined relative to the simulation space (ie, world space or local space) set by the simulationSpace property. You can use <see cref="Transform.TransformPoint" /> and <see cref="Transform.InverseTransformPoint" /> to convert points between local and world space as necessary.</remarks>
            public Vector3 position { get { return m_Position; } set { m_Position = value; } }
            ///<summary>The velocity of the particle, measured in units per second.</summary>
            ///<remarks>This velocity is used for effects that are based on physics. Examples of features that use this type of velocity are the Force module, Gravity, and Start Speed. The system stores this velocity across frames, and reapplies it to the particle position on each simulation step.
            ///
            ///The velocity is also used by the <see cref="ParticleSystemRenderer" /> if Render Mode is
            ///set to <see cref="ParticleSystemRenderMode.Stretch" />.</remarks>
            ///<seealso cref="ParticleSystem.Particle.animatedVelocity" />
            ///<seealso cref="ParticleSystem.Particle.totalVelocity" />
            public Vector3 velocity { get { return m_Velocity; } set { m_Velocity = value; } }
            ///<summary>The animated velocity of the particle.</summary>
            ///<remarks>You can use animated velocity for effects that are not based on physics, but are instead based on creating a specific velocity over time. Modules such as Noise and VelocityOverLifetime use this type of velocity. This module does not store this velocity across frames, because modules that use this value calculate a new velocity value each frame.
            ///
            ///<see cref="ParticleSystemRenderer" /> also uses animated velocity if Render Mode is
            ///set to <see cref="ParticleSystemRenderMode.Stretch" />.</remarks>
            ///<seealso cref="ParticleSystem.Particle.velocity" />
            ///<seealso cref="ParticleSystem.Particle.totalVelocity" />
            public Vector3 animatedVelocity { get { return m_AnimatedVelocity; } }
            ///<summary>The total velocity of the particle.</summary>
            ///<remarks>This is calculated as the sum of <see cref="ParticleSystem.Particle.velocity" /> and <see cref="ParticleSystem.Particle.animatedVelocity" />. Some particle features use the physics-based velocity, and other features use the animated velocity. Use this property to obtain the total combined velocity of the particle.</remarks>
            public Vector3 totalVelocity { get { return m_Velocity + m_AnimatedVelocity; } }
            ///<summary>The remaining lifetime of the particle.</summary>
            ///<remarks>This is the time, in seconds, for how long this particle remains alive.
            ///When the lifetime drops below zero, the system destroys the particle.</remarks>
            public float remainingLifetime { get { return m_Lifetime; } set { m_Lifetime = value; } }
            ///<summary>The starting lifetime of the particle.</summary>
            ///<remarks>This is the total lifetime of this particle in seconds. The Particle System sets this value when it first spawns the particle.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            ///public class Example : MonoBehaviour
            ///{
            ///    void Start()
            ///    {
            ///        ParticleSystem.Particle particle = new ParticleSystem.Particle();
            ///
            ///        // Calculate how long the particle has been alive.
            ///        float timeAlive = particle.startLifetime - particle.lifetime;
            ///    }
            ///}
            ///]]></code>
            ///</example>
            public float startLifetime { get { return m_StartLifetime; } set { m_StartLifetime = value; } }
            ///<summary>The initial color of the particle. The current color of the particle is calculated procedurally based on this value and the active color modules.</summary>
            ///<remarks>Alpha channel of the color is used to fade out particles.</remarks>
            public Color32 startColor { get { return m_StartColor; } set { m_StartColor = value; } }
            ///<summary>The random seed of the particle.</summary>
            ///<remarks>Each particle has its own seed, in order to produce deterministic results during simulation. For example, if a particle uses a random color selected from a gradient, the seed ensures that the same color is generated on each frame.
            ///
            ///You may also use this seed when generating per-particle random numbers, by passing it to <see cref="Random.InitState" />.</remarks>
            public UInt32 randomSeed { get { return m_RandomSeed; } set { m_RandomSeed = value; } }
            ///<summary>Mesh particles rotate around this axis.</summary>
            ///<remarks>Mesh particles travel around an axis set up for each particle.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            ///[RequireComponent(typeof(ParticleSystem))]
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    public bool overrideAxisOfRotation;
            ///    private ParticleSystem ps;
            ///
            ///    void Start()
            ///    {
            ///        ps = GetComponent<ParticleSystem>();
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        if (overrideAxisOfRotation)
            ///        {
            ///            ParticleSystem.Particle[] particles = new ParticleSystem.Particle[ps.particleCount];
            ///            ps.GetParticles(particles);
            ///
            ///            for (int i = 0; i < particles.Length; i++)
            ///                particles[i].axisOfRotation = Vector3.up;
            ///
            ///            ps.SetParticles(particles, ps.particleCount);
            ///        }
            ///    }
            ///
            ///    private void OnGUI()
            ///    {
            ///        bool newValue = GUI.Toggle(new Rect(10, 10, 200, 30), overrideAxisOfRotation, new GUIContent("Override Axis of Rotation"));
            ///        if (newValue != overrideAxisOfRotation)
            ///        {
            ///            ps.Clear();
            ///            overrideAxisOfRotation = newValue;
            ///        }
            ///    }
            ///}
            ///]]></code>
            ///</example>
            public Vector3 axisOfRotation { get { return m_AxisOfRotation; } set { m_AxisOfRotation = value; } }

            ///<summary>The initial size of the particle. The current size of the particle is calculated procedurally based on this value and the active size modules.</summary>
            ///<remarks>This is particle's size in meters in world space.</remarks>
            public float startSize { get { return m_StartSize.x; } set { m_StartSize = new Vector3(value, value, value); } }
            ///<summary>The initial 3D size of the particle. The current size of the particle is calculated procedurally based on this value and the active size modules.</summary>
            ///<remarks>This is particle's size in meters in world space.</remarks>
            public Vector3 startSize3D { get { return m_StartSize; } set { m_StartSize = value; m_Flags |= (UInt32)Flags.Size3D; } }

            ///<summary>The rotation of the particle.</summary>
            ///<remarks>This is the particle's Euler rotation in degrees.</remarks>
            public float rotation { get { return m_Rotation.z * Mathf.Rad2Deg; } set { m_Rotation = new Vector3(0.0f, 0.0f, value * Mathf.Deg2Rad); } }
            ///<summary>The 3D rotation of the particle.</summary>
            ///<remarks>This is the inverse of the particle's Euler rotation in degrees, on each axis.</remarks>
            public Vector3 rotation3D { get { return m_Rotation * Mathf.Rad2Deg; } set { m_Rotation = value * Mathf.Deg2Rad; m_Flags |= (UInt32)Flags.Rotation3D; } }

            ///<summary>The angular velocity of the particle.</summary>
            ///<remarks>This is the particle's angular velocity in degrees per second.</remarks>
            public float angularVelocity { get { return m_AngularVelocity.z * Mathf.Rad2Deg; } set { m_AngularVelocity = new Vector3(0.0f, 0.0f, value * Mathf.Deg2Rad); } }
            ///<summary>The 3D angular velocity of the particle.</summary>
            ///<remarks>This is the particle's angular velocity in degrees per second, around each axis.</remarks>
            public Vector3 angularVelocity3D { get { return m_AngularVelocity * Mathf.Rad2Deg; } set { m_AngularVelocity = value * Mathf.Deg2Rad; m_Flags |= (UInt32)Flags.Rotation3D; } }

            ///<summary>Calculate the current size of the particle by applying the relevant curves to its startSize property.</summary>
            ///<param name="system">The Particle System from which this particle was emitted.</param>
            ///<returns>Current size.</returns>
            public float GetCurrentSize(ParticleSystem system) { return system.GetParticleCurrentSize(ref this); }              // The current (curve-corrected) size of the particle.
            ///<summary>Calculate the current 3D size of the particle by applying the relevant curves to its startSize3D property.</summary>
            ///<param name="system">The Particle System from which this particle was emitted.</param>
            ///<returns>Current size.</returns>
            public Vector3 GetCurrentSize3D(ParticleSystem system) { return system.GetParticleCurrentSize3D(ref this); }        // The current (curve-corrected) 3D size of the particle.
            ///<summary>Calculate the current color of the particle by applying the relevant curves to its startColor property.</summary>
            ///<param name="system">The Particle System from which this particle was emitted.</param>
            ///<returns>Current color.</returns>
            public Color32 GetCurrentColor(ParticleSystem system) { return system.GetParticleCurrentColor(ref this); }          // The current (curve-corrected) color of the particle.

            ///<summary>Sets the Mesh index of the particle, used for choosing which Mesh a particle is rendered with.</summary>
            ///<param name="index">The Mesh index.</param>
            public void SetMeshIndex(int index) { m_MeshIndex = index; m_Flags |= (UInt32)Flags.MeshIndex; }
            ///<summary>Calculate the Mesh index of the particle, used for choosing which Mesh a particle is rendered with.</summary>
            ///<param name="system">The Particle System from which this particle was emitted.</param>
            ///<returns>The index of the mesh used for rendering the particle.</returns>
            public int GetMeshIndex(ParticleSystem system) { return system.GetParticleMeshIndex(ref this); }                    // Clamped based on the mesh count in the Renderer Module

            private Vector3 m_Position;
            private Vector3 m_Velocity;
            private Vector3 m_AnimatedVelocity;
            private Vector3 m_InitialVelocity;
            private Vector3 m_AxisOfRotation;
            private Vector3 m_Rotation;
            private Vector3 m_AngularVelocity;
            private Vector3 m_StartSize;
            private Color32 m_StartColor;
            private UInt32 m_RandomSeed;
            private UInt32 m_ParentRandomSeed;
            private float m_Lifetime;
            private float m_StartLifetime;
            private int m_MeshIndex;
            private float m_EmitAccumulator0;
            private float m_EmitAccumulator1;
            private UInt32 m_Flags;
        }

        // Script interface for emitting Particles, whilst allowing for overriding of some/all properties
        ///<summary>Script interface for particle emission properties.</summary>
        [StructLayout(LayoutKind.Sequential)]
        public partial struct EmitParams
        {
            ///<summary>Override all the properties of particles this system emits.</summary>
            ///<remarks>When you assign a particle to this property, it changes every other property on this type individually to match the new particle.</remarks>
            public Particle particle
            {
                get
                {
                    return m_Particle;
                }

                set
                {
                    m_Particle = value;

                    m_PositionSet = true;
                    m_VelocitySet = true;
                    m_AxisOfRotationSet = true;
                    m_RotationSet = true;
                    m_AngularVelocitySet = true;
                    m_StartSizeSet = true;
                    m_StartColorSet = true;
                    m_RandomSeedSet = true;
                    m_StartLifetimeSet = true;
                    m_MeshIndexSet = true;
                }
            }

            ///<summary>Override the position of particles this system emits.</summary>
            ///<remarks>When you use this property, the system ignores the Shape module and gives you direct control over particle spawn positions.
            ///To retain the effects of the Shape module, set <see cref="applyShapeToPosition" /> to true.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            /// // In this example we have a Particle System emitting aligned particles; we then emit and override the position and velocity every 2 seconds.
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem system;
            ///
            ///    void Start()
            ///    {
            ///        // A simple particle material with no texture.
            ///        Material particleMaterial = new Material(Shader.Find("Particles/Standard Unlit"));
            ///
            ///        // Create a Particle System.
            ///        var go = new GameObject("Particle System");
            ///        go.transform.Rotate(-90, 0, 0); // Rotate so the system emits upwards.
            ///        system = go.AddComponent<ParticleSystem>();
            ///        go.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
            ///
            ///        // Every 2 seconds we will emit.
            ///        InvokeRepeating("DoEmit", 2.0f, 2.0f);
            ///    }
            ///
            ///    void DoEmit()
            ///    {
            ///        // Any parameters we assign in emitParams will override the current system's when we call Emit.
            ///        // Here we will override the position and velocity. All other parameters will use the behavior defined in the Inspector.
            ///        var emitParams = new ParticleSystem.EmitParams();
            ///        emitParams.position = new Vector3(0.0f, 0.0f, 0.0f);
            ///        emitParams.velocity = new Vector3(0.0f, 0.0f, -2.0f);
            ///        system.Emit(emitParams, 1);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.Particle.position" />
            public Vector3 position { get { return m_Particle.position; } set { m_Particle.position = value; m_PositionSet = true; } }
            ///<summary>When overriding the position of particles, setting this flag to true allows you to retain the influence of the shape module.</summary>
            ///<remarks>With this flag set to false, the position specified is the exact position where particles spawn from, and the shape module is ignored.
            ///If true, the Particle System moves the shape module to the position specified in the EmitParams, then spawns new particles using the shape.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            /// // In this example we have a Particle System emitting aligned particles; we then emit and override the position every 2 seconds.
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem system;
            ///
            ///    void Start()
            ///    {
            ///        // A simple particle material with no texture.
            ///        Material particleMaterial = new Material(Shader.Find("Particles/Standard Unlit"));
            ///
            ///        // Create a Particle System.
            ///        var go = new GameObject("Particle System");
            ///        go.transform.Rotate(-90, 0, 0); // Rotate so the system emits upwards.
            ///        system = go.AddComponent<ParticleSystem>();
            ///        go.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
            ///
            ///        // Every 2 seconds we will emit.
            ///        InvokeRepeating("DoEmit", 2.0f, 2.0f);
            ///    }
            ///
            ///    void DoEmit()
            ///    {
            ///        // Any parameters we assign in emitParams will override the current system's when we call Emit.
            ///        // Here we will override the position. All other parameters will use the behavior defined in the Inspector.
            ///        var emitParams = new ParticleSystem.EmitParams();
            ///        emitParams.position = new Vector3(-5.0f, 0.0f, 0.0f);
            ///        emitParams.applyShapeToPosition = true;
            ///        system.Emit(emitParams, 10);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.EmitParams.position" />
            public bool applyShapeToPosition { get { return m_ApplyShapeToPosition; } set { m_ApplyShapeToPosition = value; } }
            ///<summary>Override the velocity of particles this system emits.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            /// // In this example we have a Particle System emitting aligned particles; we then emit and override the position and velocity every 2 seconds.
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem system;
            ///
            ///    void Start()
            ///    {
            ///        // A simple particle material with no texture.
            ///        Material particleMaterial = new Material(Shader.Find("Particles/Standard Unlit"));
            ///
            ///        // Create a Particle System.
            ///        var go = new GameObject("Particle System");
            ///        go.transform.Rotate(-90, 0, 0); // Rotate so the system emits upwards.
            ///        system = go.AddComponent<ParticleSystem>();
            ///        go.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
            ///
            ///        // Every 2 seconds we will emit.
            ///        InvokeRepeating("DoEmit", 2.0f, 2.0f);
            ///    }
            ///
            ///    void DoEmit()
            ///    {
            ///        // Any parameters we assign in emitParams will override the current system's when we call Emit.
            ///        // Here we will override the position and velocity. All other parameters will use the behavior defined in the Inspector.
            ///        var emitParams = new ParticleSystem.EmitParams();
            ///        emitParams.position = new Vector3(0.0f, 0.0f, 0.0f);
            ///        emitParams.velocity = new Vector3(0.0f, 0.0f, -2.0f);
            ///        system.Emit(emitParams, 1);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.Particle.velocity" />
            public Vector3 velocity { get { return m_Particle.velocity; } set { m_Particle.velocity = value; m_VelocitySet = true; } }
            ///<summary>Override the lifetime of particles this system emits.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            /// // In this example we have a Particle System emitting aligned particles; we then emit and override the lifetime every 2 seconds.
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem system;
            ///
            ///    void Start()
            ///    {
            ///        // A simple particle material with no texture.
            ///        Material particleMaterial = new Material(Shader.Find("Particles/Standard Unlit"));
            ///
            ///        // Create a Particle System.
            ///        var go = new GameObject("Particle System");
            ///        go.transform.Rotate(-90, 0, 0); // Rotate so the system emits upwards.
            ///        system = go.AddComponent<ParticleSystem>();
            ///        go.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
            ///        var mainModule = system.main;
            ///        mainModule.startLifetimeMultiplier = 0.1f;
            ///
            ///        // Every 2 seconds we will emit.
            ///        InvokeRepeating("DoEmit", 2.0f, 2.0f);
            ///    }
            ///
            ///    void DoEmit()
            ///    {
            ///        // Any parameters we assign in emitParams will override the current system's when we call Emit.
            ///        // Here we will override the lifetime. All other parameters will use the behavior defined in the Inspector.
            ///        var emitParams = new ParticleSystem.EmitParams();
            ///        emitParams.startLifetime = 5.0f;
            ///        system.Emit(emitParams, 10);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.Particle.startLifetime" />
            public float startLifetime { get { return m_Particle.startLifetime; } set { m_Particle.startLifetime = value; m_StartLifetimeSet = true; } }
            ///<summary>Override the initial size of particles this system emits.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            /// // In this example we have a Particle System emitting small particles; we then emit and override the size every 2 seconds.
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem system;
            ///
            ///    void Start()
            ///    {
            ///        // A simple particle material with no texture.
            ///        Material particleMaterial = new Material(Shader.Find("Particles/Standard Unlit"));
            ///
            ///        // Create a Particle System.
            ///        var go = new GameObject("Particle System");
            ///        go.transform.Rotate(-90, 0, 0); // Rotate so the system emits upwards.
            ///        system = go.AddComponent<ParticleSystem>();
            ///        go.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
            ///        var mainModule = system.main;
            ///        mainModule.startSize = 0.1f;
            ///
            ///        // Every 2 seconds we will emit.
            ///        InvokeRepeating("DoEmit", 2.0f, 2.0f);
            ///    }
            ///
            ///    void DoEmit()
            ///    {
            ///        // Any parameters we assign in emitParams will override the current system's when we call Emit.
            ///        // Here we will override the size. All other parameters will use the behavior defined in the Inspector.
            ///        var emitParams = new ParticleSystem.EmitParams();
            ///        emitParams.startSize = 0.5f;
            ///        system.Emit(emitParams, 10);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.Particle.startSize" />
            public float startSize { get { return m_Particle.startSize; } set { m_Particle.startSize = value; m_StartSizeSet = true; } }
            ///<summary>Override the initial 3D size of particles this system emits.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            /// // In this example we have a Particle System emitting small particles; we then emit and override the size every 2 seconds.
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem system;
            ///
            ///    void Start()
            ///    {
            ///        // A simple particle material with no texture.
            ///        Material particleMaterial = new Material(Shader.Find("Particles/Standard Unlit"));
            ///
            ///        // Create a Particle System.
            ///        var go = new GameObject("Particle System");
            ///        go.transform.Rotate(-90, 0, 0); // Rotate so the system emits upwards.
            ///        system = go.AddComponent<ParticleSystem>();
            ///        go.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
            ///        var mainModule = system.main;
            ///        mainModule.startSizeXMultiplier = 0.2f;
            ///        mainModule.startSizeYMultiplier = 0.2f;
            ///        mainModule.startSizeZMultiplier = 0.2f;
            ///        mainModule.startSize3D = true;
            ///
            ///        // Every 2 seconds we will emit.
            ///        InvokeRepeating("DoEmit", 2.0f, 2.0f);
            ///    }
            ///
            ///    void DoEmit()
            ///    {
            ///        // Any parameters we assign in emitParams will override the current system's when we call Emit.
            ///        // Here we will override the size. All other parameters will use the behavior defined in the Inspector.
            ///        var emitParams = new ParticleSystem.EmitParams();
            ///        emitParams.startSize3D = new Vector3(0.5f, 0.1f, 0.0f);
            ///        system.Emit(emitParams, 10);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.Particle.startSize3D" />
            public Vector3 startSize3D { get { return m_Particle.startSize3D; } set { m_Particle.startSize3D = value; m_StartSizeSet = true; } }
            ///<summary>Override the axis of rotation of particles this system emits.</summary>
            public Vector3 axisOfRotation { get { return m_Particle.axisOfRotation; } set { m_Particle.axisOfRotation = value; m_AxisOfRotationSet = true; } }
            ///<summary>Override the rotation of particles this system emits.</summary>
            ///<remarks>Note that the value is an Euler angle.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            /// // In this example we have a Particle System emitting aligned particles; we then emit and override the rotation every 2 seconds.
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem system;
            ///
            ///    void Start()
            ///    {
            ///        // A simple particle material with no texture.
            ///        Material particleMaterial = new Material(Shader.Find("Particles/Standard Unlit"));
            ///
            ///        // Create a Particle System.
            ///        var go = new GameObject("Particle System");
            ///        go.transform.Rotate(-90, 0, 0); // Rotate so the system emits upwards.
            ///        system = go.AddComponent<ParticleSystem>();
            ///        go.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
            ///
            ///        // Every 2 seconds we will emit.
            ///        InvokeRepeating("DoEmit", 2.0f, 2.0f);
            ///    }
            ///
            ///    void DoEmit()
            ///    {
            ///        // Any parameters we assign in emitParams will override the current system's when we call Emit.
            ///        // Here we will override the rotation. All other parameters will use the behavior defined in the Inspector.
            ///        var emitParams = new ParticleSystem.EmitParams();
            ///        emitParams.rotation = 45.0f;
            ///        system.Emit(emitParams, 10);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.Particle.rotation" />
            public float rotation { get { return m_Particle.rotation; } set { m_Particle.rotation = value; m_RotationSet = true; } }
            ///<summary>Override the 3D rotation of particles this system emits.</summary>
            ///<remarks>Note that the values are Euler angles.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            /// // In this example we have a Particle System emitting aligned particles; we then emit and override the rotation every 2 seconds.
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem system;
            ///
            ///    void Start()
            ///    {
            ///        // A simple particle material with no texture.
            ///        Material particleMaterial = new Material(Shader.Find("Particles/Standard Unlit"));
            ///
            ///        // Create a Particle System.
            ///        var go = new GameObject("Particle System");
            ///        go.transform.Rotate(-90, 0, 0); // Rotate so the system emits upwards.
            ///        system = go.AddComponent<ParticleSystem>();
            ///        go.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
            ///        var mainModule = system.main;
            ///        mainModule.startRotation3D = true;
            ///
            ///        // Every 2 seconds we will emit.
            ///        InvokeRepeating("DoEmit", 2.0f, 2.0f);
            ///    }
            ///
            ///    void DoEmit()
            ///    {
            ///        // Any parameters we assign in emitParams will override the current system's when we call Emit.
            ///        // Here we will override the rotation. All other parameters will use the behavior defined in the Inspector.
            ///        var emitParams = new ParticleSystem.EmitParams();
            ///        emitParams.rotation3D = new Vector3(45.0f, 20.0f, 35.0f);
            ///        system.Emit(emitParams, 10);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.Particle.rotation3D" />
            public Vector3 rotation3D { get { return m_Particle.rotation3D; } set { m_Particle.rotation3D = value; m_RotationSet = true; } }
            ///<summary>Override the angular velocity of particles this system emits.</summary>
            ///<remarks>This value will be ignored if the Particle System uses either of the velocity modules.</remarks>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            /// // In this example we have a Particle System emitting aligned particles; we then emit and override the rotation speed every 2 seconds.
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem system;
            ///
            ///    void Start()
            ///    {
            ///        // A simple particle material with no texture.
            ///        Material particleMaterial = new Material(Shader.Find("Particles/Standard Unlit"));
            ///
            ///        // Create a Particle System.
            ///        var go = new GameObject("Particle System");
            ///        go.transform.Rotate(-90, 0, 0); // Rotate so the system emits upwards.
            ///        system = go.AddComponent<ParticleSystem>();
            ///        go.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
            ///
            ///        // Every 2 seconds we will emit.
            ///        InvokeRepeating("DoEmit", 2.0f, 2.0f);
            ///    }
            ///
            ///    void DoEmit()
            ///    {
            ///        // Any parameters we assign in emitParams will override the current system's when we call Emit.
            ///        // Here we will override the rotation speed. All other parameters will use the behavior defined in the Inspector.
            ///        var emitParams = new ParticleSystem.EmitParams();
            ///        emitParams.angularVelocity = 180.0f;
            ///        system.Emit(emitParams, 10);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.Particle.angularVelocity" />
            public float angularVelocity { get { return m_Particle.angularVelocity; } set { m_Particle.angularVelocity = value; m_AngularVelocitySet = true; } }
            ///<summary>Override the 3D angular velocity of particles this system emits.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            /// // In this example we have a Particle System emitting aligned particles; we then emit and override the rotation speed every 2 seconds.
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem system;
            ///
            ///    void Start()
            ///    {
            ///        // A simple particle material with no texture.
            ///        Material particleMaterial = new Material(Shader.Find("Particles/Standard Unlit"));
            ///
            ///        // Create a Particle System.
            ///        var go = new GameObject("Particle System");
            ///        go.transform.Rotate(-90, 0, 0); // Rotate so the system emits upwards.
            ///        system = go.AddComponent<ParticleSystem>();
            ///        go.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
            ///        var mainModule = system.main;
            ///        mainModule.startRotation3D = true;
            ///
            ///        // Every 2 seconds we will emit.
            ///        InvokeRepeating("DoEmit", 2.0f, 2.0f);
            ///    }
            ///
            ///    void DoEmit()
            ///    {
            ///        // Any parameters we assign in emitParams will override the current system's when we call Emit.
            ///        // Here we will override the rotation speed. All other parameters will use the behavior defined in the Inspector.
            ///        var emitParams = new ParticleSystem.EmitParams();
            ///        emitParams.angularVelocity3D = new Vector3(180.0f, 80.0f, 95.0f);
            ///        system.Emit(emitParams, 10);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.Particle.angularVelocity3D" />
            public Vector3 angularVelocity3D { get { return m_Particle.angularVelocity3D; } set { m_Particle.angularVelocity3D = value; m_AngularVelocitySet = true; } }
            ///<summary>Override the initial color of particles this system emits.</summary>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            /// // In this example we have a Particle System emitting green particles; we then emit and override the color every 2 seconds.
            ///public class ExampleClass : MonoBehaviour
            ///{
            ///    private ParticleSystem system;
            ///
            ///    void Start()
            ///    {
            ///        // A simple particle material with no texture.
            ///        Material particleMaterial = new Material(Shader.Find("Particles/Standard Unlit"));
            ///
            ///        // Create a Particle System.
            ///        var go = new GameObject("Particle System");
            ///        go.transform.Rotate(-90, 0, 0); // Rotate so the system emits upwards.
            ///        system = go.AddComponent<ParticleSystem>();
            ///        go.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
            ///        var mainModule = system.main;
            ///        mainModule.startColor = Color.green;
            ///
            ///        // Every 2 seconds we will emit.
            ///        InvokeRepeating("DoEmit", 2.0f, 2.0f);
            ///    }
            ///
            ///    void DoEmit()
            ///    {
            ///        // Any parameters we assign in emitParams will override the current system's when we call Emit.
            ///        // Here we will override the start color. All other parameters will use the behavior defined in the Inspector.
            ///        var emitParams = new ParticleSystem.EmitParams();
            ///        emitParams.startColor = Color.red;
            ///        system.Emit(emitParams, 10);
            ///    }
            ///}
            ///]]></code>
            ///</example>
            ///<seealso cref="ParticleSystem.Particle.startColor" />
            public Color32 startColor { get { return m_Particle.startColor; } set { m_Particle.startColor = value; m_StartColorSet = true; } }
            ///<summary>Override the random seed of particles this system emits.</summary>
            ///<seealso cref="ParticleSystem.Particle.randomSeed" />
            public UInt32 randomSeed { get { return m_Particle.randomSeed; } set { m_Particle.randomSeed = value; m_RandomSeedSet = true; } }
            ///<summary>Set the index that specifies which Mesh to emit.</summary>
            ///<seealso cref="ParticleSystemRenderer.SetMeshes" />
            public int meshIndex { set { m_Particle.SetMeshIndex(value); m_MeshIndexSet = true; } }

            ///<summary>Revert the position back to the value specified in the Inspector.</summary>
            public void ResetPosition() { m_PositionSet = false; }
            ///<summary>Revert the velocity back to the value specified in the Inspector.</summary>
            public void ResetVelocity() { m_VelocitySet = false; }
            ///<summary>Revert the axis of rotation back to the value specified in the Inspector.</summary>
            public void ResetAxisOfRotation() { m_AxisOfRotationSet = false; }
            ///<summary>Reverts rotation and rotation3D back to the values specified in the Inspector.</summary>
            public void ResetRotation() { m_RotationSet = false; }
            ///<summary>Reverts angularVelocity and angularVelocity3D back to the values specified in the Inspector.</summary>
            public void ResetAngularVelocity() { m_AngularVelocitySet = false; }
            ///<summary>Revert the initial size back to the value specified in the Inspector.</summary>
            public void ResetStartSize() { m_StartSizeSet = false; }
            ///<summary>Revert the initial color back to the value specified in the Inspector.</summary>
            public void ResetStartColor() { m_StartColorSet = false; }
            ///<summary>Revert the random seed back to the value specified in the Inspector.</summary>
            public void ResetRandomSeed() { m_RandomSeedSet = false; }
            ///<summary>Revert the lifetime back to the value specified in the Inspector.</summary>
            public void ResetStartLifetime() { m_StartLifetimeSet = false; }
            ///<summary>Revert the Mesh selection back to the default randomized behavior.</summary>
            public void ResetMeshIndex() { m_MeshIndexSet = false; }

            [NativeName("particle")] private Particle m_Particle;
            [NativeName("positionSet")] private bool m_PositionSet;
            [NativeName("velocitySet")] private bool m_VelocitySet;
            [NativeName("axisOfRotationSet")] private bool m_AxisOfRotationSet;
            [NativeName("rotationSet")] private bool m_RotationSet;
            [NativeName("rotationalSpeedSet")] private bool m_AngularVelocitySet;
            [NativeName("startSizeSet")] private bool m_StartSizeSet;
            [NativeName("startColorSet")] private bool m_StartColorSet;
            [NativeName("randomSeedSet")] private bool m_RandomSeedSet;
            [NativeName("startLifetimeSet")] private bool m_StartLifetimeSet;
            [NativeName("meshIndexSet")] private bool m_MeshIndexSet;
            [NativeName("applyShapeToPosition")] private bool m_ApplyShapeToPosition;
        }

        ///<summary>Script interface for storing the particle playback state.</summary>
        ///<seealso cref="ParticleSystem" />
        ///<seealso cref="ParticleSystem.GetPlaybackState" />
        ///<seealso cref="ParticleSystem.SetPlaybackState" />
        [StructLayout(LayoutKind.Sequential)]
        public struct PlaybackState
        {
            [StructLayout(LayoutKind.Sequential)]
            internal struct Seed
            {
                public UInt32 x, y, z, w;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct Seed4
            {
                public Seed x, y, z, w;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct Emission
            {
                public float m_ParticleSpacing;
                public float m_ToEmitAccumulator;
                public Seed m_Random;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct Initial
            {
                public Seed4 m_Random;
            };

            [StructLayout(LayoutKind.Sequential)]
            internal struct Shape
            {
                public Seed4 m_Random;
                public float m_RadiusTimer;
                public float m_RadiusTimerPrev;
                public float m_ArcTimer;
                public float m_ArcTimerPrev;
                public float m_MeshSpawnTimer;
                public float m_MeshSpawnTimerPrev;
                public int m_OrderedMeshVertexIndex;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct Force
            {
                public Seed4 m_Random;
            };

            [StructLayout(LayoutKind.Sequential)]
            internal struct Collision
            {
                public Seed4 m_Random;
            };

            [StructLayout(LayoutKind.Sequential)]
            internal struct Noise
            {
                public float m_ScrollOffset;
            };

            [StructLayout(LayoutKind.Sequential)]
            internal struct Lights
            {
                public Seed m_Random;
                public float m_ParticleEmissionCounter;
            };

            [StructLayout(LayoutKind.Sequential)]
            internal struct Trail
            {
                public float m_Timer;
            };

            internal float m_AccumulatedDt;
            internal float m_StartDelay;
            internal float m_PlaybackTime;
            internal int m_RingBufferIndex;
            internal Emission m_Emission;
            internal Initial m_Initial;
            internal Shape m_Shape;
            internal Force m_Force;
            internal Collision m_Collision;
            internal Noise m_Noise;
            internal Lights m_Lights;
            internal Trail m_Trail;
        }

        // Native type for Trails can be found in ParticleTrails.h
        ///<summary>Script interface for storing the particle trail data.</summary>
        ///<seealso cref="ParticleSystem" />
        ///<seealso cref="ParticleSystem.GetTrails" />
        ///<seealso cref="ParticleSystem.SetTrails" />
        [StructLayout(LayoutKind.Sequential)]
        public struct Trails
        {
            [NativeName("m_Positions")]
            internal List<Vector4> positions;
            [NativeName("m_FrontPositions")]
            internal List<int> frontPositions;
            [NativeName("m_BackPositions")]
            internal List<int> backPositions;
            [NativeName("m_NumPositions")]
            internal List<int> positionCounts;
            [NativeName("m_TextureOffsets")]
            internal List<float> textureOffsets;
            [NativeName("m_MaxTrails")]
            internal int maxTrailCount;
            [NativeName("m_MaxPositionsPerTrail")]
            internal int maxPositionsPerTrailCount;

            internal void Allocate()
            {
                if (positions == null)
                    positions = new List<Vector4>();
                if (frontPositions == null)
                    frontPositions = new List<int>();
                if (backPositions == null)
                    backPositions = new List<int>();
                if (positionCounts == null)
                    positionCounts = new List<int>();
                if (textureOffsets == null)
                    textureOffsets = new List<float>();
            }

            ///<summary>Reserve memory for the particle trail data.</summary>
            public int capacity
            {
                set
                {
                    Allocate();
                    positions.Capacity = value;
                    frontPositions.Capacity = value;
                    backPositions.Capacity = value;
                    positionCounts.Capacity = value;
                    textureOffsets.Capacity = value;
                }
                get
                {
                    if (positions == null)
                        return 0;
                    return positions.Capacity;
                }
            }
        }

        ///<summary>Script interface for particle Collider data.</summary>
        ///<remarks>ColliderData contains information about which Colliders a particle is interacting with. It can be used in the MonoBehaviour.OnParticleTrigger callback, when the Trigger module is enabled.</remarks>
        public struct ColliderData
        {
            internal Component[] colliders;       // The list of colliders assigned to the trigger module
            internal int[] colliderIndices;      // The full list of collider indices that every particle triggered
            internal int[] particleStartIndices; // Lookup for every particle index, to say which entry in the colliderIndices its results begin from

            // How many colliders the particle triggered
            ///<summary>Returns how how many Colliders a particle is interacting with.</summary>
            ///<param name="particleIndex">The index of the particle event.</param>
            ///<returns>The number of Colliders the particle is interacting with.</returns>
            public int GetColliderCount(int particleIndex)
            {
                if (particleIndex < particleStartIndices.Length - 1)
                    return particleStartIndices[particleIndex + 1] - particleStartIndices[particleIndex];
                return colliderIndices.Length - particleStartIndices[particleIndex];
            }

            // Get a collider for a given particle
            ///<summary>Retrieve a specific Collider that a particle iss interacting with.</summary>
            ///<param name="particleIndex">The index of the particle event.</param>
            ///<param name="colliderIndex">The index of the collider to obtain.</param>
            ///<returns>The Collider or Collider2D Component that a particle is interacting with.</returns>
            public Component GetCollider(int particleIndex, int colliderIndex)
            {
                if (colliderIndex >= GetColliderCount(particleIndex))
                    throw new IndexOutOfRangeException("colliderIndex exceeded the total number of colliders for the requested particle");

                int index = particleStartIndices[particleIndex] + colliderIndex;
                return colliders[colliderIndices[index]];
            }
        }
    }

    [RequiredByNativeCode(Optional = true)]
    public partial struct ParticleCollisionEvent
    {
        internal Vector3 m_Intersection;
        internal Vector3 m_Normal;
        internal Vector3 m_Velocity;
        internal EntityId m_ColliderEntityId;

        ///<summary>Intersection point of the collision in world coordinates.</summary>
        ///<remarks>The intersection point is reported in world coordinates regardless of whether the Particle System is simulated in local coordinates.</remarks>
        public Vector3 intersection { get { return m_Intersection; } }
        ///<summary>Geometry normal at the intersection point of the collision.</summary>
        ///<remarks>The normal is reported in world coordinates regardless of whether the Particle System is simulated in local coordinates.</remarks>
        public Vector3 normal { get { return m_Normal; } }
        ///<summary>Incident velocity at the intersection point of the collision.</summary>
        ///<remarks>The velocity is reported in world coordinates regardless of whether the Particle System is simulated in local coordinates.</remarks>
        public Vector3 velocity { get { return m_Velocity; } }

        ///<summary>The <see cref="T:UnityEngine.Collider" /> or <see cref="T:UnityEngine.Collider2D" /> for the GameObject struck by the particles.</summary>
        ///<remarks>Cast the return value and test for null, if you expect both Collider and Collider2D components to be returned.</remarks>
        public Component colliderComponent { get { return EntityIdToColliderComponent(m_ColliderEntityId); } }
    }
}
