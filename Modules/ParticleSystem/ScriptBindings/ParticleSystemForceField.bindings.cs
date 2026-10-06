// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using MinMaxCurve = UnityEngine.ParticleSystem.MinMaxCurve;
using MinMaxCurveBlittable = UnityEngine.ParticleSystem.MinMaxCurveBlittable;

namespace UnityEngine
{
    ///<summary>Script interface for Particle System Force Fields.</summary>
    ///<remarks>Particle System Force Fields can be used to influence groups of particles that enter each field's zone of influence.
    ///
    ///The shape of the Force Field can be set to a variety of shapes, and how the particles are affected is controlled by various properties in the Force Field.
    ///
    ///As part of choosing the shape, you may define a start and end range. The end range describes the maximum extent of the shape, and the start range can be used to create a hollow shape.
    ///
    ///A number of forces can be applied to particles that are within this volume: directional, gravitational, rotational, drag, and a vector field.
    ///
    ///The settings for each type of force make use of the <see cref="ParticleSystem.MinMaxCurve" /> type, which is also used in the Particle System. This type allows you to set simple uniform values, or more complicated values that vary per-particle, and vary over the lifetime of each particle.</remarks>
    ///<example>
    ///  <code><![CDATA[
    ///using System;
    ///using System.Collections;
    ///using System.Collections.Generic;
    ///using System.Linq;
    ///using UnityEngine;
    ///
    ///[RequireComponent(typeof(ParticleSystem))]
    ///public class ExampleClass : MonoBehaviour
    ///{
    ///    public ParticleSystemForceFieldShape m_Shape = ParticleSystemForceFieldShape.Sphere;
    ///    public float m_StartRange = 0.0f;
    ///    public float m_EndRange = 3.0f;
    ///    public Vector3 m_Direction = Vector3.zero;
    ///    public float m_Gravity = 0.0f;
    ///    public float m_GravityFocus = 0.0f;
    ///    public float m_RotationSpeed = 0.0f;
    ///    public float m_RotationAttraction = 0.0f;
    ///    public Vector2 m_RotationRandomness = Vector2.zero;
    ///    public float m_Drag = 0.0f;
    ///    public bool m_MultiplyDragByParticleSize = false;
    ///    public bool m_MultiplyDragByParticleVelocity = false;
    ///
    ///    private ParticleSystemForceField m_ForceField;
    ///
    ///    void Start()
    ///    {
    ///        // Create a Force Field
    ///        var go = new GameObject("ForceField", typeof(ParticleSystemForceField));
    ///        go.transform.position = new Vector3(0, 2, 0);
    ///        go.transform.rotation = Quaternion.Euler(new Vector3(90.0f, 0.0f, 0.0f));
    ///
    ///        m_ForceField = go.GetComponent<ParticleSystemForceField>();
    ///
    ///        // Configure Particle System
    ///        transform.position = new Vector3(0, -4, 0);
    ///        transform.rotation = Quaternion.identity;
    ///        var ps = GetComponent<ParticleSystem>();
    ///
    ///        var main = ps.main;
    ///        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
    ///        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
    ///        main.maxParticles = 100000;
    ///
    ///        var emission = ps.emission;
    ///        emission.rateOverTime = 0.0f;
    ///        emission.burstCount = 1;
    ///        emission.SetBurst(0, new ParticleSystem.Burst(0.0f, 200, 200, -1, 0.1f));
    ///
    ///        var shape = ps.shape;
    ///        shape.shapeType = ParticleSystemShapeType.SingleSidedEdge;
    ///        shape.radius = 5.0f;
    ///        shape.radiusMode = ParticleSystemShapeMultiModeValue.BurstSpread;
    ///        shape.randomPositionAmount = 0.1f;
    ///        shape.randomDirectionAmount = 0.05f;
    ///
    ///        var forces = ps.externalForces;
    ///        forces.enabled = true;
    ///    }
    ///
    ///    void Update()
    ///    {
    ///        m_ForceField.shape = m_Shape;
    ///        m_ForceField.startRange = m_StartRange;
    ///        m_ForceField.endRange = m_EndRange;
    ///        m_ForceField.directionX = m_Direction.x;
    ///        m_ForceField.directionY = m_Direction.y;
    ///        m_ForceField.directionZ = m_Direction.z;
    ///        m_ForceField.gravity = m_Gravity;
    ///        m_ForceField.gravityFocus = m_GravityFocus;
    ///        m_ForceField.rotationSpeed = m_RotationSpeed;
    ///        m_ForceField.rotationAttraction = m_RotationAttraction;
    ///        m_ForceField.rotationRandomness = m_RotationRandomness;
    ///        m_ForceField.drag = m_Drag;
    ///        m_ForceField.multiplyDragByParticleSize = m_MultiplyDragByParticleSize;
    ///        m_ForceField.multiplyDragByParticleVelocity = m_MultiplyDragByParticleVelocity;
    ///    }
    ///
    ///    void OnGUI()
    ///    {
    ///        GUIContent[] shapeLabels = Enum.GetNames(typeof(ParticleSystemForceFieldShape)).Select(n => new GUIContent(n)).ToArray();
    ///        m_Shape = (ParticleSystemForceFieldShape)GUI.SelectionGrid(new Rect(25, 25, 400, 25), (int)m_Shape, shapeLabels, 4);
    ///
    ///        float y = 80.0f;
    ///        float spacing = 40.0f;
    ///
    ///        GUI.Label(new Rect(25, y, 140, 30), "Start Range");
    ///        m_StartRange = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), m_StartRange, 0.0f, 2.0f);
    ///        y += spacing;
    ///
    ///        GUI.Label(new Rect(25, y, 140, 30), "End Range");
    ///        m_EndRange = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), m_EndRange, 2.0f, 3.0f);
    ///        y += spacing;
    ///
    ///        GUI.Label(new Rect(25, y, 140, 30), "Direction");
    ///        m_Direction.x = GUI.HorizontalSlider(new Rect(165, y + 5, 40, 30), m_Direction.x, -1.0f, 1.0f);
    ///        m_Direction.y = GUI.HorizontalSlider(new Rect(210, y + 5, 40, 30), m_Direction.y, -1.0f, 1.0f);
    ///        m_Direction.z = GUI.HorizontalSlider(new Rect(255, y + 5, 40, 30), m_Direction.z, -1.0f, 1.0f);
    ///        y += spacing;
    ///
    ///        GUI.Label(new Rect(25, y, 140, 30), "Gravity");
    ///        m_Gravity = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), m_Gravity, -0.05f, 0.05f);
    ///        y += spacing;
    ///
    ///        GUI.Label(new Rect(25, y, 140, 30), "Gravity Focus");
    ///        m_GravityFocus = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), m_GravityFocus, 0.0f, 1.0f);
    ///        y += spacing;
    ///
    ///        GUI.Label(new Rect(25, y, 140, 30), "Rotation Speed");
    ///        m_RotationSpeed = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), m_RotationSpeed, -10.0f, 10.0f);
    ///        y += spacing;
    ///
    ///        GUI.Label(new Rect(25, y, 140, 30), "Rotation Attraction");
    ///        m_RotationAttraction = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), m_RotationAttraction, 0.0f, 0.01f);
    ///        y += spacing;
    ///
    ///        GUI.Label(new Rect(25, y, 140, 30), "Rotation Randomness");
    ///        m_RotationRandomness.x = GUI.HorizontalSlider(new Rect(165, y + 5, 60, 30), m_RotationRandomness.x, 0.0f, 1.0f);
    ///        m_RotationRandomness.y = GUI.HorizontalSlider(new Rect(230, y + 5, 60, 30), m_RotationRandomness.y, 0.0f, 1.0f);
    ///        y += spacing;
    ///
    ///        GUI.Label(new Rect(25, y, 140, 30), "Drag");
    ///        m_Drag = GUI.HorizontalSlider(new Rect(165, y + 5, 100, 30), m_Drag, 0.0f, 20.0f);
    ///        y += spacing;
    ///
    ///        m_MultiplyDragByParticleSize = GUI.Toggle(new Rect(25, y, 220, 30), m_MultiplyDragByParticleSize, "Multiply Drag by Particle Size");
    ///        y += spacing;
    ///
    ///        m_MultiplyDragByParticleVelocity = GUI.Toggle(new Rect(25, y, 220, 30), m_MultiplyDragByParticleVelocity, "Multiply Drag by Particle Velocity");
    ///        y += spacing;
    ///    }
    ///}
    ///]]></code>
    ///</example>
    [NativeHeader("ParticleSystemScriptingClasses.h")]
    [NativeHeader("Modules/ParticleSystem/ParticleSystem.h")]
    [NativeHeader("Modules/ParticleSystem/ParticleSystemForceField.h")]
    [NativeHeader("Modules/ParticleSystem/ParticleSystemForceFieldManager.h")]
    [NativeHeader("Modules/ParticleSystem/ScriptBindings/ParticleSystemScriptBindings.h")]
    [global::UnityEngine.NativeClass("ParticleSystemForceField", PersistentTypeId = 330)]
    [RequireComponent(typeof(Transform))]
    public partial class ParticleSystemForceField : Behaviour
    {
        internal ParticleSystemForceField(global::UnityEngine.EntityId id) : base(id) {}
        public ParticleSystemForceField() {}
        ///<summary>Selects the type of shape used for influencing particles.</summary>
        ///<seealso cref="ParticleSystemForceField" />
        [NativeName("ForceShape")]
        extern public ParticleSystemForceFieldShape shape { get; set; }
        ///<summary>Setting a value greater than 0 creates a hollow Force Field shape. This will cause particles to not be affected by the Force Field when closer to the center of the volume than the startRange property.</summary>
        ///<seealso cref="ParticleSystemForceField" />
        extern public float startRange { get; set; }
        ///<summary>Determines the size of the shape used for influencing particles.</summary>
        ///<seealso cref="ParticleSystemForceField" />
        extern public float endRange { get; set; }
        ///<summary>Describes the length of the Cylinder when using the Cylinder Force Field shape to influence particles.</summary>
        ///<seealso cref="ParticleSystemForceField" />
        extern public float length { get; set; }
        ///<summary>When using the gravity force, set this value between 0 and 1 to control the focal point of the gravity effect.</summary>
        ///<remarks>Setting a value of 0 causes particles to be attracted to the center of the volume, whereas setting a value of 1 will result in particles being attracted to the outer edge of the shape. Setting a value in between 0 and 1 will move the focal point between the inner and outer extents of the volume.</remarks>
        ///<seealso cref="ParticleSystemForceField" />
        extern public float gravityFocus { get; set; }
        ///<summary>Apply randomness to the Force Field axis that particles will travel around.</summary>
        ///<remarks>When applying rotational forces to particles, the particles will spin around the Z axis of the Force Field's Transform component by default.
        ///
        ///Using rotationRandomness allows each particle to deviate from this default axis by the specified amount. A value of 1 allows each particle to choose a completely random axis to spin around, whereas smaller values will constrain the movement more closely to the default axis.</remarks>
        ///<seealso cref="ParticleSystemForceField" />
        extern public Vector2 rotationRandomness { get; set; }
        ///<summary>When using Drag, the drag strength will be multiplied by the size of the particles if this toggle is enabled.</summary>
        ///<remarks>Enabling this value results in a more physically accurate drag simulation.</remarks>
        ///<seealso cref="ParticleSystemForceField.drag" />
        ///<seealso cref="ParticleSystemForceField" />
        extern public bool multiplyDragByParticleSize { get; set; }
        ///<summary>When using Drag, the drag strength will be multiplied by the speed of the particles if this toggle is enabled.</summary>
        ///<remarks>Enabling this value results in a more physically accurate drag simulation.</remarks>
        ///<seealso cref="ParticleSystemForceField.drag" />
        ///<seealso cref="ParticleSystemForceField" />
        extern public bool multiplyDragByParticleVelocity { get; set; }
        ///<summary>Apply forces to particles within the volume of the Force Field, by using a 3D texture containing vector field data.</summary>
        ///<seealso cref="ParticleSystemForceField" />
        extern public Texture3D vectorField { get; set; }

        ///<summary>Apply a linear force along the local X axis to particles within the volume of the Force Field.</summary>
        ///<seealso cref="ParticleSystemForceField" />
        public MinMaxCurve directionX { get => directionXBlittable; set => directionXBlittable = value; }
        [NativeName("DirectionX")] private extern MinMaxCurveBlittable directionXBlittable { get; set; }

        ///<summary>Apply a linear force along the local Y axis to particles within the volume of the Force Field.</summary>
        ///<seealso cref="ParticleSystemForceField" />
        public MinMaxCurve directionY { get => directionYBlittable; set => directionYBlittable = value; }
        [NativeName("DirectionY")] private extern MinMaxCurveBlittable directionYBlittable { get; set; }

        ///<summary>Apply a linear force along the local Z axis to particles within the volume of the Force Field.</summary>
        ///<seealso cref="ParticleSystemForceField" />
        public MinMaxCurve directionZ { get => directionZBlittable; set => directionZBlittable = value; }
        [NativeName("DirectionZ")] private extern MinMaxCurveBlittable directionZBlittable { get; set; }

        ///<summary>Apply gravity to particles within the volume of the Force Field.</summary>
        ///<remarks>Particles affected by the gravity effect will be attracted towards the focal point of the gravity. This can be set using the <see cref="ParticleSystemForceField.gravityFocus" /> property.</remarks>
        ///<seealso cref="ParticleSystemForceField" />
        public MinMaxCurve gravity { get => gravityBlittable; set => gravityBlittable = value; }
        [NativeName("Gravity")] private extern MinMaxCurveBlittable gravityBlittable { get; set; }

        ///<summary>The speed at which particles are propelled around a vortex.</summary>
        ///<remarks>Set in conjunction with <see cref="ParticleSystemForceField.rotationAttraction" /> to create a vortex effect within the volume of the Force Field.</remarks>
        ///<seealso cref="ParticleSystemForceField" />
        public MinMaxCurve rotationSpeed { get => rotationSpeedBlittable; set => rotationSpeedBlittable = value; }
        [NativeName("RotationSpeed")] private extern MinMaxCurveBlittable rotationSpeedBlittable { get; set; }

        ///<summary>Controls how strongly particles are dragged into the vortex motion.</summary>
        ///<remarks>Set in conjunction with <see cref="ParticleSystemForceField.rotationSpeed" /> to create a vortex effect within the volume of the Force Field.</remarks>
        ///<seealso cref="ParticleSystemForceField" />
        public MinMaxCurve rotationAttraction { get => rotationAttractionBlittable; set => rotationAttractionBlittable = value; }
        [NativeName("RotationAttraction")] private extern MinMaxCurveBlittable rotationAttractionBlittable { get; set; }

        ///<summary>Apply drag to particles within the volume of the Force Field.</summary>
        ///<remarks>Use this property to slow down particles.</remarks>
        ///<seealso cref="ParticleSystemForceField" />
        public MinMaxCurve drag { get => dragBlittable; set => dragBlittable = value; }
        [NativeName("Drag")] private extern MinMaxCurveBlittable dragBlittable { get; set; }

        ///<summary>The speed at which particles are propelled through the vector field.</summary>
        ///<remarks>Set in conjunction with <see cref="ParticleSystemForceField.vectorFieldAttraction" /> to apply a vector field to the particle motion.</remarks>
        ///<seealso cref="ParticleSystemForceField" />
        public MinMaxCurve vectorFieldSpeed { get => vectorFieldSpeedBlittable; set => vectorFieldSpeedBlittable = value; }
        [NativeName("VectorFieldSpeed")] private extern MinMaxCurveBlittable vectorFieldSpeedBlittable { get; set; }

        ///<summary>Controls how strongly particles are dragged into the vector field motion.</summary>
        ///<remarks>Set in conjunction with <see cref="ParticleSystemForceField.vectorFieldSpeed" /> to apply a vector field to the particle motion.</remarks>
        ///<seealso cref="ParticleSystemForceField" />
        public MinMaxCurve vectorFieldAttraction { get => vectorFieldAttractionBlittable; set => vectorFieldAttractionBlittable = value; }
        [NativeName("VectorFieldAttraction")] private extern MinMaxCurveBlittable vectorFieldAttractionBlittable { get; set; }

        ///<exclude />
        [StaticAccessor("GetParticleSystemForceFieldManager()", StaticAccessorType.Dot)]
        [NativeMethod("GetForceFields")]
        extern public static ParticleSystemForceField[] FindAll();
    }
}
