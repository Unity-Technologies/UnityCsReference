// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Unity.Collections;

using static Unity.U2D.Physics.Scripting2D;

namespace Unity.U2D.Physics
{
    /// <summary>
    /// A body is contained within a world and has 3 degrees-of-freedom, two for position and one for rotation.
    /// A body can have forces, torques and impulses applied to it.
    /// A body has three distinct types:
    ///
    ///- Static: This type of body does not move under simulation and behaves as if it has infinite mass, essentially an immovable object. Static bodies never interact with other Static or Kinematic bodies.
    ///- Dynamic: This type of body is fully simulated and moves according to forces and torques applied to its linear/angular velocities. It can interact with all other body types. It always has finite, non-zero mass.
    ///- Kinematic: This type of body moves under simulation and moves according to its linear/angular velocities and never uses forces or torques. It only interacts with Dynamic body types. It behaves as if it has infinite mass.
    ///
    /// A body is automatically destroyed when the world it is in is destroyed. A body cannot exist outside a world.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [MovedFrom(autoUpdateAPI: ScriptUpdateConstants.AutoUpdateAPI, sourceNamespace: ScriptUpdateConstants.SourceNamespace, sourceAssembly: ScriptUpdateConstants.SourceAssembly)]
    public readonly partial struct PhysicsBody : IPhysicsHandle<PhysicsBody>, IEquatable<PhysicsBody>
    {
        #region Handle

        /// <undoc/>
        readonly PhysicsHandle m_PhysicsHandle;

        /// <summary>
        /// Create a body from a physics handle.
        /// 
        /// NOTE: You must ensure that the physics handle represents the correct object type otherwise hard to detect bugs can occur.
        /// </summary>
        /// <param name="physicsHandle">The physics handle to use.</param>
        public PhysicsBody(PhysicsHandle physicsHandle) { m_PhysicsHandle = physicsHandle; }

        /// <summary>
        /// Get the physics handle.
        /// </summary>
        public readonly PhysicsHandle physicsHandle => m_PhysicsHandle;

        /// <undoc/>
        public override readonly string ToString() => isValid ? $"type={type}, {m_PhysicsHandle}" : "<INVALID>";

        #endregion

        #region Equality

        /// <undoc/>
        public override bool Equals(object obj) => obj is PhysicsBody other && Equals(other);

        /// <undoc/>
        public bool Equals(PhysicsBody other) => m_PhysicsHandle == other.m_PhysicsHandle;

        /// <undoc/>
        public static bool operator ==(PhysicsBody lhs, PhysicsBody rhs) => lhs.Equals(rhs);

        /// <undoc/>
        public static bool operator !=(PhysicsBody lhs, PhysicsBody rhs) => !(lhs == rhs);

        /// <undoc/>
        public override int GetHashCode() => m_PhysicsHandle.GetHashCode();

        #endregion

        /// <summary>
        /// A body is one of these three body types, Dynamic, Kinematic or Static, each of which determines how the body behaves in the simulation.
        /// </summary>
        public enum BodyType
        {
            /// <summary>
            /// A dynamic body has positive mass, velocity determined by forces and is moved by solver.
            /// </summary>
            Dynamic = 0,

            /// <summary>
            /// A kinematic body has zero mass, velocity set by user and is moved by solver
            /// </summary>
            Kinematic = 1,

            /// <summary>
            /// A static body has zero mass, zero velocity and may be manually moved.
            /// </summary>
            Static = 2,
        }

        /// <summary>
        /// Body constrains constrain the degrees of freedom a body when solving the simulation.
        /// </summary>
        [Flags]
        public enum BodyConstraints
        {
            /// <summary>
            /// No constraints
            /// </summary>
            None = 0,

            /// <summary>
            /// Constrain motion along the X-axis.
            /// </summary>
            PositionX = 1 << 0,

            /// <summary>
            /// Constrain motion along the Y-axis.
            /// </summary>
            PositionY = 1 << 1,

            /// <summary>
            /// Constrain rotation along the Z-axis.
            /// </summary>
            Rotation = 1 << 2,

            /// <summary>
            /// Constrain motion along all axes.
            /// </summary>
            Position = PositionX | PositionY,

            /// <summary>
            /// Constrain rotation and motion along all axes.
            /// </summary>
            All = Position | Rotation,
        }

        /// <summary>
        /// The method used to Write the body pose to the Transform.
        /// See <see cref="PhysicsWorld.transformWriteMode"/>.
        /// </summary>
        public enum TransformWriteMode
        {
            /// <summary>
            /// The current body pose will be written to the Transform.
            /// </summary>
            Current,

            /// <summary>
            /// The interpolated pose from the previous body pose to the current body pose will be written to the Transform.
            /// The transform pose is essentially historic.
            /// </summary>
            Interpolate,

            /// <summary>
            /// The pose extrapolated from the current body pose to a future pose based upon the current linear/angular velocities will be written to the Transform.
            /// The transform pose is essentially predictive.
            /// </summary>
            Extrapolate,

            /// <summary>
            /// This body pose won't be written to the Transform.
            /// </summary>
            Off
        }

        /// <summary>
        /// Used to define a Transform write "tween" for a body.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct TransformWriteTween
        {
            /// <summary>
            /// The body to be used during the lifetime of the tween.
            /// </summary>
            public PhysicsBody body { readonly get => m_Body; set => m_Body = value; }

            /// <summary>
            /// The transform write mode to be used during the lifetime of the tween.
            /// Anything other than <see cref="PhysicsBody.TransformWriteMode.Interpolate"/> or <see cref="PhysicsBody.TransformWriteMode.Extrapolate"/> will be removed.
            /// </summary>
            public TransformWriteMode transformWriteMode { readonly get => m_TransformWriteMode; set => m_TransformWriteMode = value; }

            /// <summary>
            /// The physics transform to be used during the lifetime of the tween.
            /// When the <see cref="PhysicsBody.TransformWriteTween.transformWriteMode"/> is <see cref="PhysicsBody.TransformWriteMode.Interpolate"/>, this defines the target pose to move to.
            /// When the <see cref="PhysicsBody.TransformWriteTween.transformWriteMode"/> is <see cref="PhysicsBody.TransformWriteMode.Extrapolate"/>, this defines the source pose to move from.
            /// </summary>
            public PhysicsTransform physicsTransform { readonly get => m_PhysicsTransform; set => m_PhysicsTransform = value; }

            /// <summary>
            /// The <see cref="UnityEngine.Transform"/> to be used during the lifetime of the tween.
            /// </summary>
            public Transform transform { readonly get => PhysicsGlobal_GetObject(m_TransformId) as Transform; set => m_TransformId = value != null ? value.GetEntityId() : EntityId.None; }

            /// <summary>
            /// The depth of the <see cref="UnityEngine.Transform"/> in the hierarchy where zero is the root.
            /// When the <see cref="PhysicsWorld.transformTweenMode"/> is anything other than <see cref="PhysicsWorld.TransformTweenMode.Parallel"/>, all <see cref="PhysicsBody.TransformWriteTween"/> are sorted into ascending depth order
            /// so that writing the transforms in tween order will result in the deeper children correctly overwriting any parent transform writes.
            /// This is NOT set when the <see cref="PhysicsWorld.transformTweenMode"/> is set to <see cref="PhysicsWorld.TransformTweenMode.Parallel"/> and will be zero.
            /// </summary>
            public int transformDepth { readonly get => m_TransformDepth; set => m_TransformDepth = value; }

            /// <summary>
            /// The linear velocity of the body to be used during the lifetime of the tween.
            /// This is typically used when the <see cref="PhysicsBody.TransformWriteTween.transformWriteMode"/> is <see cref="PhysicsBody.TransformWriteMode.Extrapolate"/>.
            /// </summary>
            public Vector2 linearVelocity { readonly get => m_LinearVelocity; set => m_LinearVelocity = value; }

            /// <summary>
            /// The angular velocity of the body to be used during the lifetime of the tween, in degrees per second.
            /// This is typically used when the <see cref="PhysicsBody.TransformWriteTween.transformWriteMode"/> is <see cref="PhysicsBody.TransformWriteMode.Extrapolate"/>.
            /// </summary>
            public float angularVelocity { readonly get => m_AngularVelocity; set => m_AngularVelocity = value; }

            /// <summary>
            /// The start position of the tween.
            /// When the <see cref="PhysicsBody.TransformWriteTween.transformWriteMode"/> is <see cref="PhysicsBody.TransformWriteMode.Current"/>, this is set to the last <see cref="UnityEngine.Transform.position"/>. but is not used.
            /// When the <see cref="PhysicsBody.TransformWriteTween.transformWriteMode"/> is <see cref="PhysicsBody.TransformWriteMode.Interpolate"/>, this is set to the last <see cref="UnityEngine.Transform.position"/>.
            /// When the <see cref="PhysicsBody.TransformWriteTween.transformWriteMode"/> is <see cref="PhysicsBody.TransformWriteMode.Extrapolate"/>, this will be calculated from <see cref="PhysicsBody.TransformWriteTween.physicsTransform"/>.
            /// See <see cref="UnityEngine.Transform.position"/>.
            /// </summary>
            public Vector3 positionFrom { readonly get => m_PositionFrom; set => m_PositionFrom = value; }

            /// <summary>
            /// The start rotation of the tween.
            /// When the <see cref="PhysicsBody.TransformWriteTween.transformWriteMode"/> is <see cref="PhysicsBody.TransformWriteMode.Current"/>, this is set to the last <see cref="UnityEngine.Transform.rotation"/> but is not used.
            /// When the <see cref="PhysicsBody.TransformWriteTween.transformWriteMode"/> is <see cref="PhysicsBody.TransformWriteMode.Interpolate"/>, this is set to the last <see cref="UnityEngine.Transform.rotation"/>.
            /// When the <see cref="PhysicsBody.TransformWriteTween.transformWriteMode"/> is <see cref="PhysicsBody.TransformWriteMode.Extrapolate"/>, this will be calculated from <see cref="PhysicsBody.TransformWriteTween.physicsTransform"/>.
            /// See <see cref="UnityEngine.Transform.rotation"/>.
            /// </summary>
            public Quaternion rotationFrom { readonly get => m_RotationFrom; set => m_RotationFrom = value; }

            /// <summary>
            /// Get the write pose for the current write tween.
            /// </summary>
            /// <param name="transformPlane">The transform plane to use to calculate a non-custom transform plane.</param>
            /// <param name="transformPlaneCustom">The custom transform plane to use.</param>
            /// <param name="fast2D">Whether to perform fast 2D or slow 3D calculations. See <see cref="PhysicsWorld.TransformWriteMode"/>.</param>
            /// <param name="position">The calculated position.</param>
            /// <param name="rotation">The calculated rotation.</param>
            public readonly void GetPose(
                PhysicsWorld.TransformPlane transformPlane,
                ref PhysicsWorld.TransformPlaneCustom transformPlaneCustom,
                bool fast2D,
                out Vector3 position, out Quaternion rotation)
            {
                // Handle non-custom plane projection.
                if (transformPlane != PhysicsWorld.TransformPlane.Custom)
                {
                    // Calculate the pose as per the selected TransformPlane.
                    m_PhysicsTransform.GetPositionAndRotation(out var bodyPosition, out var bodyRotation);
                    position = PhysicsMath.ToPosition3D(position: bodyPosition, reference: positionFrom, transformPlane: transformPlane);
                    rotation = fast2D ?
                        PhysicsMath.ToRotationFast3D(angle: bodyRotation.radians, transformPlane: transformPlane) :
                        PhysicsMath.ToRotationSlow3D(angle: bodyRotation.radians, reference: rotationFrom, transformPlane: transformPlane);

                    return;
                }

                // Custom plane projection.
                transformPlaneCustom.PlaneProjection(in m_PhysicsTransform, out position, out rotation);
            }

            /// <summary>
            /// Get the interpolated pose for the current write tween.
            /// </summary>
            /// <param name="transformPlane">The transform plane to use to calculate a non-custom transform plane.</param>
            /// <param name="transformPlaneCustom">The custom transform plane to use.</param>
            /// <param name="fast2D">Whether to perform fast 2D or slow 3D calculations. See <see cref="PhysicsWorld.TransformWriteMode"/>.</param>
            /// <param name="interpolationTime">The interpolation time to use in the range [0, 1].</param>
            /// <param name="position">The calculated position.</param>
            /// <param name="rotation">The calculated rotation.</param>
            public readonly void GetInterpolatedPose(
                PhysicsWorld.TransformPlane transformPlane,
                ref PhysicsWorld.TransformPlaneCustom transformPlaneCustom,
                bool fast2D,
                float interpolationTime,
                out Vector3 position, out Quaternion rotation)
            {
                // Handle non-custom plane projection.
                if (transformPlane != PhysicsWorld.TransformPlane.Custom)
                {
                    position = PhysicsMath.ToPosition3D(position: physicsTransform.position, reference: positionFrom, transformPlane: transformPlane);
                    rotation = fast2D ?
                        PhysicsMath.ToRotationFast3D(angle: physicsTransform.rotation.radians, transformPlane: transformPlane) :
                        PhysicsMath.ToRotationSlow3D(angle: physicsTransform.rotation.radians, reference: rotationFrom, transformPlane: transformPlane);
                }
                else
                {
                    // Custom plane projection.
                    transformPlaneCustom.PlaneProjection(in m_PhysicsTransform, out position, out rotation);
                }

                // Interpolation the pose.
                position = Vector3.Lerp(m_PositionFrom, position, interpolationTime);
                rotation = Quaternion.Slerp(m_RotationFrom, rotation, interpolationTime);
            }

            /// <summary>
            /// Get the extrapolated pose for the current write tween.
            /// </summary>
            /// <param name="transformPlane">The transform plane to use to calculate a non-custom transform plane.</param>
            /// <param name="transformPlaneCustom">The custom transform plane to use.</param>
            /// <param name="extrapolationTime">The extrapolation time to use in the range [0, 1].</param>
            /// <param name="position">The calculated position.</param>
            /// <param name="rotation">The calculated rotation.</param>
            public readonly void GetExtrapolatedPose(
                PhysicsWorld.TransformPlane transformPlane,
                ref PhysicsWorld.TransformPlaneCustom transformPlaneCustom,
                float extrapolationTime,
                out Vector3 position, out Quaternion rotation)
            {
                // Handle non-custom plane projection.
                if (transformPlane != PhysicsWorld.TransformPlane.Custom)
                {
                    var transformedVelocity = PhysicsMath.Swizzle(position: new Vector3(m_LinearVelocity.x * extrapolationTime, m_LinearVelocity.y * extrapolationTime, 0.0f), transformPlane: transformPlane);

                    // Extrapolate the pose.
                    position = m_PositionFrom + transformedVelocity;
                    rotation = PhysicsMath.AngularVelocityToQuaternion(angularVelocity: m_AngularVelocity, deltaTime: extrapolationTime, transformPlane: transformPlane) * m_RotationFrom;

                    return;
                }

                // Calculate the new transform.
                var newTransform = new PhysicsTransform
                {
                    position = m_PhysicsTransform.position + m_LinearVelocity * extrapolationTime,
                    rotation = m_PhysicsTransform.rotation.IntegrateRotation(m_AngularVelocity * extrapolationTime)
                };

                // Custom plane projection.
                transformPlaneCustom.PlaneProjection(in newTransform, out position, out rotation);
            }

            #region Internal

            PhysicsBody m_Body;
            TransformWriteMode m_TransformWriteMode;
            PhysicsTransform m_PhysicsTransform;
            EntityId m_TransformId;
            int m_TransformDepth;
            Vector2 m_LinearVelocity;
            float m_AngularVelocity;
            Vector3 m_PositionFrom;
            Quaternion m_RotationFrom;

            #endregion
        }

        /// <summary>
        /// This holds the mass configuration computed for a PhysicsBody.
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct MassConfiguration
        {
            /// <summary>
            /// The mass of the shape, usually in kilograms.
            /// </summary>
            public float mass { readonly get => m_Mass; set => m_Mass = value; }

            /// <summary>
            /// The position of the shape's centroid relative to the shape's origin.
            /// </summary>
            public Vector2 center { readonly get => m_Center; set => m_Center = value; }

            /// <summary>
            /// The rotational inertia of the shape about the shape center.
            /// </summary>
            public float rotationalInertia { readonly get => m_RotationalInertia; set => m_RotationalInertia = value; }

            #region Internal

            [SerializeField] float m_Mass;
            [SerializeField] Vector2 m_Center;
            [SerializeField] float m_RotationalInertia;

            #endregion
        }

        /// <summary>
        /// Input to <see cref="PhysicsBody.ApplyBuoyancy(BuoyancyInput, float)"/> describing the fluid surface, density, flow and damping used to compute buoyancy, flow and damping forces for the body.
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct BuoyancyInput
        {
            /// <summary>
            /// Create a default <see cref="BuoyancyInput"/>.
            /// The <see cref="mask"/> defaults to <see cref="PhysicsMask.All"/> so every attached shape contributes unless explicitly filtered out.
            /// The surface defaults to a flat horizontal water surface at the world origin (<see cref="surfacePosition"/> = <see cref="Vector2.zero"/>, <see cref="surfaceNormal"/> = <see cref="Vector2.up"/>).
            /// </summary>
            public BuoyancyInput()
            {
                m_SurfacePosition = Vector2.zero;
                m_SurfaceNormal = Vector2.up;
                m_Density = 1f;
                m_FlowDirection = PhysicsRotate.right;
                m_FlowSpeed = 0f;
                m_LinearDamping = 0f;
                m_AngularDamping = 0f;
                m_UseTriggers = false;
                m_Mask = PhysicsMask.All;
            }

            /// <summary>
            /// A point in world space lying on the fluid surface.
            /// Together with <see cref="surfaceNormal"/> this defines the infinite plane of the fluid.
            /// </summary>
            public Vector2 surfacePosition { readonly get => m_SurfacePosition; set => m_SurfacePosition = value; }

            /// <summary>
            /// The outward-pointing surface normal of the fluid, in world space.
            /// Points away from the submerged side; shape points with a negative separation from the plane are submerged.
            /// Defaults to <see cref="Vector2.up"/> (a flat horizontal water surface).
            /// Must be non-zero; the engine normalises it internally so it does not need to be unit length.
            /// </summary>
            public Vector2 surfaceNormal { readonly get => m_SurfaceNormal; set => m_SurfaceNormal = value; }

            /// <summary>
            /// The fluid density, used to compute the Archimedean buoyancy force per submerged unit area.
            /// Clamped to a lower bound of <see cref="Mathf.Epsilon"/>.
            /// </summary>
            public float density { readonly get => m_Density; set => m_Density = Mathf.Max(float.Epsilon, value); }

            /// <summary>
            /// The direction of the fluid flow as a 2D rotation.
            /// Combined with <see cref="flowSpeed"/> to produce the flow velocity vector applied as force per unit submerged area at the submerged centroid.
            /// </summary>
            public PhysicsRotate flowDirection { readonly get => m_FlowDirection; set => m_FlowDirection = value; }

            /// <summary>
            /// The magnitude of the fluid flow along <see cref="flowDirection"/>.
            /// The per-shape force contribution is flowDirection * flowSpeed * submergedArea.
            /// </summary>
            public float flowSpeed { readonly get => m_FlowSpeed; set => m_FlowSpeed = value; }

            /// <summary>
            /// Linear damping coefficient.
            /// Slows the body's linear velocity at the submerged centroid relative to the fluid.
            /// </summary>
            public float linearDamping { readonly get => m_LinearDamping; set => m_LinearDamping = Mathf.Max(0f, value); }

            /// <summary>
            /// Angular damping coefficient.
            /// Slows the body's angular velocity while submerged.
            /// </summary>
            public float angularDamping { readonly get => m_AngularDamping; set => m_AngularDamping = Mathf.Max(0f, value); }

            /// <summary>
            /// When true, trigger shapes contribute to buoyancy alongside solid shapes.
            /// When false, trigger shapes are skipped.
            /// </summary>
            public bool useTriggers { readonly get => m_UseTriggers; set => m_UseTriggers = value; }

            /// <summary>
            /// Category mask used to filter which attached shapes contribute.
            /// A shape participates iff (shape.contactFilter.categories.bitMask &amp; mask.bitMask) != 0.
            /// Defaults to <see cref="PhysicsMask.All"/> when the input is created via the parameterless constructor.
            /// </summary>
            public PhysicsMask mask { readonly get => m_Mask; set => m_Mask = value; }

            #region Internal

            [SerializeField] Vector2 m_SurfacePosition;
            [SerializeField] Vector2 m_SurfaceNormal;
            [SerializeField] [Min(float.Epsilon)] float m_Density;
            [SerializeField] PhysicsRotate m_FlowDirection;
            [SerializeField] float m_FlowSpeed;
            [SerializeField] [Min(0f)] float m_LinearDamping;
            [SerializeField] [Min(0f)] float m_AngularDamping;
            [SerializeField] bool m_UseTriggers;
            [SerializeField] PhysicsMask m_Mask;

            #endregion
        }

        /// <summary>
        /// Input to <see cref="PhysicsBody.ApplyWind(WindInput)"/> describing the wind velocity, drag and lift coefficients and the shape filter used to compute aerodynamic forces per attached shape.
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct WindInput
        {
            /// <summary>
            /// Create a default <see cref="WindInput"/>.
            /// The <see cref="mask"/> defaults to <see cref="PhysicsMask.All"/> so every attached shape contributes unless explicitly filtered out.
            /// </summary>
            public WindInput()
            {
                m_Force = Vector2.zero;
                m_Drag = 0f;
                m_Lift = 0f;
                m_UseTriggers = false;
                m_Mask = PhysicsMask.All;
            }

            /// <summary>
            /// The wind velocity vector.
            /// Scaled by <see cref="drag"/> when computing the per-shape aerodynamic relative velocity.
            /// </summary>
            public Vector2 force { readonly get => m_Force; set => m_Force = value; }

            /// <summary>
            /// Drag coefficient.
            /// Scales the wind contribution in the relative-velocity term that drives the per-shape aerodynamic force.
            /// </summary>
            public float drag { readonly get => m_Drag; set => m_Drag = Mathf.Max(0f, value); }

            /// <summary>
            /// Lift coefficient.
            /// Scales the perpendicular component of the per-edge aerodynamic force (capsules and polygons only; circles ignore lift).
            /// </summary>
            public float lift { readonly get => m_Lift; set => m_Lift = Mathf.Max(0f, value); }

            /// <summary>
            /// When true, trigger shapes contribute to wind alongside solid shapes.
            /// When false, trigger shapes are skipped.
            /// </summary>
            public bool useTriggers { readonly get => m_UseTriggers; set => m_UseTriggers = value; }

            /// <summary>
            /// Category mask used to filter which attached shapes contribute.
            /// A shape participates iff (shape.contactFilter.categories.bitMask &amp; mask.bitMask) != 0.
            /// Defaults to <see cref="PhysicsMask.All"/> when the input is created via the parameterless constructor.
            /// </summary>
            public PhysicsMask mask { readonly get => m_Mask; set => m_Mask = value; }

            #region Internal

            [SerializeField] Vector2 m_Force;
            [SerializeField] [Min(0f)] float m_Drag;
            [SerializeField] [Min(0f)] float m_Lift;
            [SerializeField] bool m_UseTriggers;
            [SerializeField] PhysicsMask m_Mask;

            #endregion
        }

        /// <summary>
        /// A batch item used to set the velocity of a <see cref="PhysicsBody"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct BatchVelocity
        {
            /// <summary>
            /// Create a default batch velocity, assigning the <see cref="PhysicsBody"/>.
            /// </summary>
            /// <param name="physicsBody">The <see cref="PhysicsBody"/> to write to.</param>
            public BatchVelocity(PhysicsBody physicsBody)
            {
                m_PhysicsBody = physicsBody;
                m_LinearVelocity = default;
                m_AngularVelocity = default;
                m_UseLinearVelocity = default;
                m_UseAngularVelocity = default;
            }

            /// <summary>
            /// The <see cref="PhysicsBody"/> to write to.
            /// </summary>
            public PhysicsBody physicsBody { readonly get => m_PhysicsBody; set => m_PhysicsBody = value; }

            /// <summary>
            /// The linear velocity of the body.
            /// <see cref="PhysicsBody.linearVelocity"/>.
            /// </summary>
            public Vector2 linearVelocity { readonly get => m_LinearVelocity; set { m_LinearVelocity = value; m_UseLinearVelocity = true; } }

            /// <summary>
            /// The angular velocity of the body, in degrees per second.
            /// <see cref="PhysicsBody.angularVelocity"/>.
            /// </summary>
            public float angularVelocity { readonly get => m_AngularVelocity; set { m_AngularVelocity = value; m_UseAngularVelocity = true; } }

            #region Internal

            PhysicsBody m_PhysicsBody;
            Vector2 m_LinearVelocity;
            float m_AngularVelocity;

            bool m_UseLinearVelocity;
            bool m_UseAngularVelocity;

            #endregion
        };

        /// <summary>
        /// A batch item used to apply a force to a <see cref="PhysicsBody"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct BatchForce
        {
            /// <summary>
            /// Create a default batch force, assigning the <see cref="PhysicsBody"/>.
            /// </summary>
            /// <param name="physicsBody">The <see cref="PhysicsBody"/> to write to.</param>
            public BatchForce(PhysicsBody physicsBody)
            {
                m_PhysicsBody = physicsBody;

                m_LinearForce = default;
                m_LinearForcePosition = default;
                m_Torque = default;
                m_WakeBody = default;
                m_UseLinearForce = default;
                m_UseLinearForcePosition = default;
                m_UseTorque = default;
            }

            /// <summary>
            /// The <see cref="PhysicsBody"/> to write to.
            /// </summary>
            public PhysicsBody physicsBody { readonly get => m_PhysicsBody; set => m_PhysicsBody = value; }

            /// <summary>
            /// Apply a force at a world point.
            /// If the force is not applied at the center of mass, it will generate a torque and affect the angular velocity.
            /// <see cref="PhysicsBody.ApplyForce(Vector2, Vector2, bool)"/>.
            /// </summary>
            /// <param name="force">The world force vector, usually in newtons (N)</param>
            /// <param name="point">The world position of the point of application.</param>
            /// <param name="wake">Should the body be woken up.</param>
            public void ApplyForce(Vector2 force, Vector2 point, bool wake = true)
            {
                m_LinearForce = force;
                m_LinearForcePosition = point;
                m_WakeBody = wake;
                m_UseLinearForce = true;
                m_UseLinearForcePosition = true;
            }

            /// <summary>
            /// Apply a force to the center of mass.
            /// <see cref="PhysicsBody.ApplyForceToCenter(Vector2, bool)"/>.
            /// </summary>
            /// <param name="force">The world force vector, usually in newtons (N).</param>
            /// <param name="wake">Should the body be woken up.</param>
            public void ApplyForceToCenter(Vector2 force, bool wake = true)
            {
                m_LinearForce = force;
                m_WakeBody = wake;
                m_UseLinearForce = true;
                m_UseLinearForcePosition = false;
            }

            /// <summary>
            /// Apply a torque.
            /// This affects the angular velocity without affecting the linear velocity.
            /// <see cref="PhysicsBody.ApplyTorque(float, bool)"/>.
            /// </summary>
            /// <param name="torque">Torque, usually in N*m.</param>
            /// <param name="wake">Should the body be woken up.</param>
            public void ApplyTorque(float torque, bool wake = true)
            {
                m_Torque = torque;
                m_WakeBody = wake;

                m_UseTorque = true;
            }

            #region Internal

            PhysicsBody m_PhysicsBody;
            Vector2 m_LinearForce;
            Vector2 m_LinearForcePosition;
            float m_Torque;
            bool m_WakeBody;

            bool m_UseLinearForce;
            bool m_UseLinearForcePosition;
            bool m_UseTorque;

            #endregion
        };

        /// <summary>
        /// A batch item used to apply an impulse to a <see cref="PhysicsBody"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct BatchImpulse
        {
            /// <summary>
            /// Create a default batch impulse, assigning the <see cref="PhysicsBody"/>.
            /// </summary>
            /// <param name="physicsBody">The <see cref="PhysicsBody"/> to write to.</param>
            public BatchImpulse(PhysicsBody physicsBody)
            {
                m_PhysicsBody = physicsBody;

                m_LinearImpulse = default;
                m_LinearImpulsePosition = default;
                m_AngularImpulse = default;
                m_WakeBody = default;
                m_UseLinearImpulse = default;
                m_UseLinearImpulsePosition = default;
                m_UseAngularImpulse = default;
            }

            /// <summary>
            /// The <see cref="PhysicsBody"/> to write to.
            /// </summary>
            public PhysicsBody physicsBody { readonly get => m_PhysicsBody; set => m_PhysicsBody = value; }

            /// <summary>
            /// Apply an impulse at a point.
            /// This immediately modifies the velocity and also modifies the angular velocity if the point of application is not at the center of mass.
            ///	This should be used for one-shot impulses.
            ///	If you need a steady force, use a force instead, which will work better with the sub-stepping solver.
            /// <see cref="PhysicsBody.ApplyLinearImpulse(Vector2, Vector2, bool)"/>.
            /// </summary>
            /// <param name="impulse">The world impulse vector, usually in N*s or kg*m/s.</param>
            /// <param name="point">The world position of the point of application.</param>
            /// <param name="wake">Should the body be woken up.</param>
            public void ApplyLinearImpulse(Vector2 impulse, Vector2 point, bool wake = true)
            {
                m_LinearImpulse = impulse;
                m_LinearImpulsePosition = point;
                m_WakeBody = wake;

                m_UseLinearImpulse = true;
                m_UseLinearImpulsePosition = true;
            }

            /// <summary>
            /// Apply an impulse to the center of mass.
            /// This immediately modifies the velocity.
            ///	This should be used for one-shot impulses.
            ///	If you need a steady force, use a force instead, which will work better with the sub-stepping solver.
            /// <see cref="PhysicsBody.ApplyLinearImpulseToCenter(Vector2, bool)"/>.
            /// </summary>
            /// <param name="impulse">The world impulse vector, usually in N*s or kg*m/s.</param>
            /// <param name="wake">Should the body be woken up.</param>
            public void ApplyLinearImpulseToCenter(Vector2 impulse, bool wake = true)
            {
                m_LinearImpulse = impulse;
                m_WakeBody = wake;

                m_UseLinearImpulse = true;
                m_UseLinearImpulsePosition = false;
            }

            /// <summary>
            /// Apply an angular impulse.
            ///	This should be used for one-shot impulses.
            ///	If you need a steady torque, use a torque instead, which will work better with the sub-stepping solver.
            /// <see cref="PhysicsBody.ApplyAngularImpulse(float, bool)"/>.
            /// </summary>
            /// <param name="impulse">The angular impulse, usually in units of kg*m*m/s.</param>
            /// <param name="wake">Should the body be woken up.</param>
            public void ApplyAngularImpulse(float impulse, bool wake = true)
            {
                m_AngularImpulse = impulse;
                m_WakeBody = true;

                m_UseAngularImpulse = true;
            }

            #region Internal

            PhysicsBody m_PhysicsBody;
            Vector2 m_LinearImpulse;
            Vector2 m_LinearImpulsePosition;
            float m_AngularImpulse;
            bool m_WakeBody;

            bool m_UseLinearImpulse;
            bool m_UseLinearImpulsePosition;
            bool m_UseAngularImpulse;

            #endregion
        };

        /// <summary>
        /// A batch item used to get/set the pose of a <see cref="PhysicsBody"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct BatchTransform
        {
            /// <summary>
            /// Create a default batch transform, assigning the <see cref="PhysicsBody"/>.
            /// </summary>
            /// <param name="physicsBody">The <see cref="PhysicsBody"/> to write to.</param>
            public BatchTransform(PhysicsBody physicsBody)
            {
                m_PhysicsBody = physicsBody;

                m_PhysicsTransform = default;
                m_UsePosition = default;
                m_UseRotation = default;
            }

            /// <summary>
            /// The <see cref="PhysicsBody"/> to write to.
            /// </summary>
            public PhysicsBody physicsBody { readonly get => m_PhysicsBody; set => m_PhysicsBody = value; }

            /// <summary>
            /// The position of the body in the world.
            /// <see cref="PhysicsBody.position"/>.
            /// </summary>
            public Vector2 position { readonly get => m_PhysicsTransform.position; set { m_PhysicsTransform.position = value; m_UsePosition = true; } }

            /// <summary>
            /// The rotation of the body.
            /// <see cref="PhysicsBody.rotation"/>.
            /// </summary>
            public PhysicsRotate rotation { readonly get => m_PhysicsTransform.rotation; set { m_PhysicsTransform.rotation = value; m_UseRotation = true; } }

            /// <summary>
            /// The full transform of the body composed of position and rotation.
            /// <see cref="PhysicsBody.transform"/>.
            /// </summary>
            public PhysicsTransform transform { readonly get => m_PhysicsTransform; set { m_PhysicsTransform = value; m_UsePosition = m_UseRotation = true; } }

            #region Internal

            PhysicsBody m_PhysicsBody;
            PhysicsTransform m_PhysicsTransform;

            bool m_UsePosition;
            bool m_UseRotation;

            #endregion
        };

        /// <summary>
        /// Create a body using <see cref="PhysicsBodyDefinition.defaultDefinition"/> in the specified world.
        /// </summary>
        /// <param name="world">The world to create the body in.</param>
        /// <returns>The created body.</returns>
        public static PhysicsBody Create(PhysicsWorld world) => PhysicsBody_Create(world, PhysicsBodyDefinition.defaultDefinition);

        /// <summary>
        /// Create a body in the specified world.
        /// </summary>
        /// <param name="world">The world to create the body in.</param>
        /// <param name="definition">The body definition to use.</param>
        /// <returns>The created body.</returns>
        public static PhysicsBody Create(PhysicsWorld world, PhysicsBodyDefinition definition) => PhysicsBody_Create(world, definition);

        /// <summary>
        /// Create a batch of bodies in the specified world.
        /// </summary>
        /// <param name="world">The world to create the bodies in.</param>
        /// <param name="definition">The body definition to use for all bodies.</param>
        /// <param name="bodyCount">The number of bodies to create.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The created bodies. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public static unsafe NativeArray<PhysicsBody> CreateBatch(PhysicsWorld world, PhysicsBodyDefinition definition, int bodyCount, Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsBody_CreateBatch(world, new ReadOnlySpan<PhysicsBodyDefinition>(&definition, 1), bodyCount, allocator).ToNativeArray<PhysicsBody>();

        /// <summary>
        /// Create a batch of bodies in the specified world.
        /// </summary>
        /// <param name="world">The world to create the bodies in.</param>
        /// <param name="definitions">The definitions used to create the bodies. The number of bodies produced is implicitly controlled by the number of definitions in this span.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The created bodies. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public static NativeArray<PhysicsBody> CreateBatch(PhysicsWorld world, ReadOnlySpan<PhysicsBodyDefinition> definitions, Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsBody_CreateBatch(world, definitions, definitions.Length, allocator).ToNativeArray<PhysicsBody>();

        /// <summary>
        /// Destroy a body, destroying all attached <see cref="PhysicsShape"/> and <see cref="PhysicsJoint"/>.
        /// If the object is owned with <see cref="PhysicsBody.SetOwner(UnityEngine.Object)"/> then you must provide the owner key it returned. Failing to do so will return a warning and the body will not be destroyed.
        /// </summary>
        /// <param name="ownerKey">Optional owner key returned when using <see cref="PhysicsBody.SetOwner(UnityEngine.Object)"/>.</param>
        /// <returns>If the body was destroyed or not.</returns>
        public readonly bool Destroy(int ownerKey = 0) => PhysicsBody_Destroy(this, ownerKey);

        /// <summary>
        /// Destroy a batch of bodies, destroying all attached <see cref="PhysicsShape"/> and <see cref="PhysicsJoint"/>.
        /// Any invalid bodies will be ignored.
        /// Owned bodies will produce a warning and will not be destroyed (See <see cref="PhysicsBody.SetOwner(UnityEngine.Object)"/>).
        /// </summary>
        /// <param name="bodies">The bodies to destroy.</param>
        public static void DestroyBatch(ReadOnlySpan<PhysicsBody> bodies) => PhysicsBody_DestroyBatch(bodies);

        /// <summary>
        /// Set the velocity for a batch of <see cref="PhysicsBody"/> using a span of <see cref="PhysicsBody.BatchVelocity"/>.
        /// If invalid values are passed to the batch, they will simply be ignored.
        /// For best performance, the bodies contained in the batch should all be part of the same <see cref="PhysicsWorld"/>.
        /// If the bodies in the batch are not contained in the same <see cref="PhysicsWorld"/>, the batch should be sorted by the <see cref="PhysicsWorld"/> the bodies are contained within.
        /// </summary>
        /// <param name="batch">The batch of bodies and values to set.</param>
        public static void SetBatchVelocity(ReadOnlySpan<BatchVelocity> batch) => PhysicsBody_SetBatchVelocity(batch);

        /// <summary>
        /// Get the velocity for a batch of <see cref="PhysicsBody"/>.
        /// </summary>
        /// <param name="bodies">The bodies to retrieve the batch of velocity for.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The batch of velocity for the specified bodies.</returns>
        public static NativeArray<BatchVelocity> GetBatchVelocity(ReadOnlySpan<PhysicsBody> bodies, Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsBody_GetBatchVelocity(bodies, allocator).ToNativeArray<BatchVelocity>();

        /// <summary>
        /// Apply a force for a batch of <see cref="PhysicsBody"/> using a span of <see cref="PhysicsBody.BatchForce"/>.
        /// If invalid values are passed to the batch, they will simply be ignored.
        /// For best performance, the bodies contained in the batch should all be part of the same <see cref="PhysicsWorld"/>.
        /// If the bodies in the batch are not contained in the same <see cref="PhysicsWorld"/>, the batch should be sorted by the <see cref="PhysicsWorld"/> the bodies are contained within.
        /// </summary>
        /// <param name="batch">The batch of bodies and values to set.</param>
        public static void SetBatchForce(ReadOnlySpan<BatchForce> batch) => PhysicsBody_SetBatchForce(batch);

        /// <summary>
        /// Apply an impulse for a batch of <see cref="PhysicsBody"/> using a span of <see cref="PhysicsBody.BatchImpulse"/>.
        /// If invalid values are passed to the batch, they will simply be ignored.
        /// For best performance, the bodies contained in the batch should all be part of the same <see cref="PhysicsWorld"/>.
        /// If the bodies in the batch are not contained in the same <see cref="PhysicsWorld"/>, the batch should be sorted by the <see cref="PhysicsWorld"/> the bodies are contained within.
        /// </summary>
        /// <param name="batch">The batch of bodies and values to set.</param>
        public static void SetBatchImpulse(ReadOnlySpan<BatchImpulse> batch) => PhysicsBody_SetBatchImpulse(batch);

        /// <summary>
        /// Set the transform for a batch of <see cref="PhysicsBody"/> using a span of <see cref="PhysicsBody.BatchTransform"/>.
        /// If invalid values are passed to the batch, they will simply be ignored.
        /// For best performance, the bodies contained in the batch should all be part of the same <see cref="PhysicsWorld"/>.
        /// If the bodies in the batch are not contained in the same <see cref="PhysicsWorld"/>, the batch should be sorted by the <see cref="PhysicsWorld"/> the bodies are contained within.
        /// </summary>
        /// <param name="batch">The batch of bodies and values to set.</param>
        public static void SetBatchTransform(ReadOnlySpan<BatchTransform> batch) => PhysicsBody_SetBatchTransform(batch);

        /// <summary>
        /// Get the transform for a batch of <see cref="PhysicsBody"/>.
        /// </summary>
        /// <param name="bodies">The bodies to retrieve the batch of transforms for.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The batch of transform for the specified bodies.</returns>
        public static NativeArray<BatchTransform> GetBatchTransform(ReadOnlySpan<PhysicsBody> bodies, Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsBody_GetBatchTransform(bodies, allocator).ToNativeArray<BatchTransform>();

        /// <summary>
        /// Get/Set a body definition by accessing all of its current properties.
        /// This is provided as convenience only and should not be used when performance is important as all the properties defined in the definition are accessed sequentially.
        /// You should try to only use the specific properties you need rather than using this feature.
        /// </summary>
        public readonly PhysicsBodyDefinition definition { get => PhysicsBody_ReadDefinition(this); set => PhysicsBody_WriteDefinition(this, value, false); }

        /// <summary>
        /// Checks if a body is valid.
        /// </summary>
        public readonly bool isValid => PhysicsBody_IsValid(this);

        /// <summary>
        /// Get the world the body is attached to.
        /// </summary>
        public readonly PhysicsWorld world => PhysicsBody_GetWorld(this);

        /// <summary>
        /// A body is one of these three body types, Dynamic, Kinematic or Static, each of which determines how the body behaves in the simulation.
        /// </summary>
        public readonly PhysicsBody.BodyType type { get => PhysicsBody_GetBodyType(this); set => PhysicsBody_SetBodyType(this, value); }

        /// <summary>
        /// Get/Set the degrees of freedom constraints (locks) for the body of Linear X, Linear Y and Rotation Z.
        /// </summary>
        public readonly PhysicsBody.BodyConstraints constraints { get => PhysicsBody_GetBodyConstraints(this); set => PhysicsBody_SetBodyConstraints(this, value); }

        /// <summary>
        /// The position of the body in the world.
        /// </summary>
        public readonly Vector2 position { get => PhysicsBody_GetPosition(this); set => PhysicsBody_SetPosition(this, value); }

        /// <summary>
        /// The rotation of the body.
        /// </summary>
        public readonly PhysicsRotate rotation { get => PhysicsBody_GetRotation(this); set => PhysicsBody_SetRotation(this, value); }

        /// <summary>
        /// The full transform of the body composed of position and rotation.
        /// </summary>
        public readonly PhysicsTransform transform { get => PhysicsBody_GetTransform(this); set => PhysicsBody_SetTransform(this, value); }

        /// <summary>
        /// Set the <see cref="PhysicsBody.linearVelocity"/> and <see cref="PhysicsBody.angularVelocity"/> to reach the specified transform in the specified time.
        /// The resultant transform will be closed by may not be exact.
        /// This is designed ideally for Kinematic bodies but will work with Dynamic bodies if nothing changes the assigned velocities.
        /// This will be ignored if the calculated <see cref="PhysicsBody.linearVelocity"/> and <see cref="PhysicsBody.angularVelocity"/> would be below the <see cref="PhysicsBody.sleepThreshold"/>.
        /// This will automatically wake the body if it is asleep.
        /// </summary>
        /// <param name="transform">The transform target for the body.</param>
        /// <param name="deltaTime">The timer over which to calculate the required velocities to move to the transform.</param>
        public readonly void SetTransformTarget(PhysicsTransform transform, float deltaTime) => PhysicsBody_SetTransformTarget(this, transform, deltaTime);

        /// <summary>
        /// Read the full 3D position and rotation of the body given the specified <see cref="UnityEngine.Transform"/>.
        /// </summary>
        /// <param name="transform">The Transform object to be used as a reference when converting from 2D position/rotation to 3D position/rotation, usually the same as any TransformObject assigned to the PhysicsBody.</param>
        /// <param name="position">The calculated output position.</param>
        /// <param name="rotation">The calculated output rotation.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if the Transform argument is NULL.</exception>
        public readonly void ReadPose(Transform transform, out Vector3 position, out Quaternion rotation)
        {
            // Validate.
            if (transform == null)
                throw new ArgumentNullException(nameof(transform), "Transform cannot be NULL.");

            // Fetch the body transform.
            var bodyTransform = this.transform;

            // Fetch the world configuration.
            var bodyWorld = world;

            // Fetch the transform plane.
            var transformPlane = bodyWorld.transformPlane;

            // Handle non-custom plane projection.
            if (transformPlane != PhysicsWorld.TransformPlane.Custom)
            {
                // Handle the write mode appropriately.
                switch (bodyWorld.transformWriteMode)
                {
                    // Write the fast case.
                    case PhysicsWorld.TransformWriteMode.Fast2D:
                    {
                        position = PhysicsMath.ToPosition3D(position: bodyTransform.position, reference: transform.position, transformPlane: transformPlane);
                        rotation = PhysicsMath.ToRotationFast3D(angle: bodyTransform.rotation.radians, transformPlane: transformPlane);
                        return;
                    }

                    // Write the slow case.
                    case PhysicsWorld.TransformWriteMode.Slow3D:
                    {
                        transform.GetPositionAndRotation(out var transformPosition, out var transformRotation);
                        position = PhysicsMath.ToPosition3D(position: bodyTransform.position, reference: transformPosition, transformPlane: transformPlane);
                        rotation = PhysicsMath.ToRotationSlow3D(angle: bodyTransform.rotation.radians, reference: transformRotation, transformPlane: transformPlane);
                        return;
                    }

                    default:
                        position = transform.position;
                        rotation = transform.rotation;
                        return;
                }
            }

            // Custom plane projection.
            bodyWorld.transformPlaneCustom.PlaneProjection(in bodyTransform, out position, out rotation);
        }

        /// <summary>
        /// Write the full 3D position and rotation of the body to the currently set <see cref="PhysicsBody.transformObject"/>.
        /// If no <see cref="PhysicsBody.transformObject"/> is assigned, this method will do nothing and false will be returned.
        /// </summary>
        /// <returns>Whether the <see cref="PhysicsBody.transformObject"/> was written to.</returns>
        public readonly bool WritePose()
        {
            // Fetch the transform object.
            var bodyTransformObject = transformObject;
            if (bodyTransformObject == null)
                return false;

            // Fetch the world configuration.
            var bodyWorld = world;
            var transformWriteMode = bodyWorld.transformWriteMode;
            var physicsTransform = transform;
            Vector3 position;
            Quaternion rotation;

            // Handle non-custom plane projection.
            var transformPlane = bodyWorld.transformPlane;
            if (transformPlane != PhysicsWorld.TransformPlane.Custom)
            {
                // Fetch the body position and rotation.
                physicsTransform.GetPositionAndRotation(out var bodyPosition, out var bodyRotation);

                // Calculate the pose as per the selected TransformPlane.
                var fastWrite2D = transformWriteMode == PhysicsWorld.TransformWriteMode.Fast2D;
                position = PhysicsMath.ToPosition3D(position: bodyPosition, reference: bodyTransformObject.position, transformPlane: transformPlane);
                rotation = fastWrite2D ?
                    PhysicsMath.ToRotationFast3D(angle: bodyRotation.radians, transformPlane: transformPlane) :
                    PhysicsMath.ToRotationSlow3D(angle: bodyRotation.radians, reference: bodyTransformObject.rotation, transformPlane: transformPlane);
            }
            else
            {
                // Custom plane projection.
                bodyWorld.transformPlaneCustom.PlaneProjection(in physicsTransform, out position, out rotation);
            }

            // Set the transform pose.
            PhysicsWorld.SetTransform(bodyTransformObject, ref position, ref rotation);
            return true;
        }

        /// <summary>
        /// Gets a local point relative to the body given a world point.
        /// </summary>
        /// <param name="worldPoint">The world point to transform.</param>
        /// <returns>The local point relative to the body.</returns>
        public readonly Vector2 GetLocalPoint(Vector2 worldPoint) => PhysicsBody_GetLocalPoint(this, worldPoint);

        /// <summary>
        /// Gets a world point transformed from a local point relative to the body.
        /// </summary>
        /// <param name="localPoint">The local point to transform.</param>
        /// <returns>The transformed world point.</returns>
        public readonly Vector2 GetWorldPoint(Vector2 localPoint) => PhysicsBody_GetWorldPoint(this, localPoint);

        /// <summary>
        /// Gets a local vector on a body given a world vector.
        /// </summary>
        /// <param name="worldVector">The world vector to transform.</param>
        /// <returns>The local vector relative to the body.</returns>
        public readonly Vector2 GetLocalVector(Vector2 worldVector) => PhysicsBody_GetLocalVector(this, worldVector);

        /// <summary>
        /// Gets a world vector transformed from a local vector relative to the body.
        /// </summary>
        /// <param name="localVector">The local vector to transform.</param>
        /// <returns>The transformed world vector.</returns>
        public readonly Vector2 GetWorldVector(Vector2 localVector) => PhysicsBody_GetWorldVector(this, localVector);

        /// <summary>
        /// Get the linear velocity of a local point attached to a body. Usually in meters per second.
        /// </summary>
        /// <param name="localPoint">The local point to transform.</param>
        /// <returns>The linear velocity at the specified local point attached to a body.</returns>
        public readonly Vector2 GetLocalPointVelocity(Vector2 localPoint) => PhysicsBody_GetLocalPointVelocity(this, localPoint);

        /// <summary>
        /// Get the linear velocity of a world point attached to a body. Usually in meters per second.
        /// </summary>
        /// <param name="worldPoint">The world point to transform.</param>
        /// <returns>The linear velocity at the specified world point attached to a body.</returns>
        public readonly Vector2 GetWorldPointVelocity(Vector2 worldPoint) => PhysicsBody_GetWorldPointVelocity(this, worldPoint);

        /// <summary>
        /// The linear velocity of the body.
        /// </summary>
        public readonly Vector2 linearVelocity { get => PhysicsBody_GetLinearVelocity(this); set => PhysicsBody_SetLinearVelocity(this, value); }

        /// <summary>
        /// The angular velocity of the body, in degrees per second.
        /// </summary>
        public readonly float angularVelocity { get => PhysicsBody_GetAngularVelocity(this); set => PhysicsBody_SetAngularVelocity(this, value); }

        /// <summary>
        /// The calculated mass of the body, usually in kilograms.
        /// This can be accessed as a union of <see cref="PhysicsBody.mass"/>, <see cref="PhysicsBody.rotationalInertia"/> and <see cref="PhysicsBody.localCenterOfMass"/> using <see cref="PhysicsBody.massConfiguration"/>.
        /// </summary>
        public readonly float mass { get => PhysicsBody_GetMass(this); set => PhysicsBody_SetMass(this, value); }

        /// <summary>
        /// The rotational inertia of the body, usually in kg*m^2.
        /// This can be accessed as a union of <see cref="PhysicsBody.mass"/>, <see cref="PhysicsBody.rotationalInertia"/> and <see cref="PhysicsBody.localCenterOfMass"/> using <see cref="PhysicsBody.massConfiguration"/>.
        /// </summary>
        public readonly float rotationalInertia { get => PhysicsBody_GetRotationalInertia(this); set => PhysicsBody_SetRotationalInertia(this, value); }

        /// <summary>
        /// The center of mass position of the body in local space.
        /// This can be accessed as a union of <see cref="PhysicsBody.mass"/>, <see cref="PhysicsBody.rotationalInertia"/> and <see cref="PhysicsBody.localCenterOfMass"/> using <see cref="PhysicsBody.massConfiguration"/>.
        /// </summary>
        public readonly Vector2 localCenterOfMass { get => PhysicsBody_GetLocalCenterOfMass(this); set => PhysicsBody_SetLocalCenterOfMass(this, value); }

        /// <summary>
        /// Get the center of mass position of the body in world space.
        /// This changes as the body moves i.e. as the <see cref="PhysicsBody.transform"/> is changed.
        /// </summary>
        public readonly Vector2 worldCenterOfMass => PhysicsBody_GetWorldCenterOfMass(this);

        /// <summary>
        /// The body mass configuration comprised of the <see cref="PhysicsBody.mass"/>, <see cref="PhysicsBody.rotationalInertia"/> and <see cref="PhysicsBody.localCenterOfMass"/>.
        /// Normally this is computed automatically as each <see cref="PhysicsShape"/> is added, removed or changed on a body.
        /// This will automatically change if the body type changes, for instance, a Static or Kinematic body always have zero mass and rotational inertia.
        /// The individual properties of the <see cref="PhysicsBody.massConfiguration"/> and be accessed using <see cref="PhysicsBody.mass"/>, <see cref="PhysicsBody.rotationalInertia"/> and <see cref="PhysicsBody.localCenterOfMass"/>.
        /// The <see cref="PhysicsBody.MassConfiguration"/> will be overwritten when setting this property or if <see cref="PhysicsBody.ApplyMassFromShapes"/> is called or when adding, removing or changing <see cref="PhysicsShape"/> with <see cref="PhysicsShapeDefinition.startMassUpdate"/> enabled.
        /// </summary>
        public readonly MassConfiguration massConfiguration { get => PhysicsBody_GetMassConfiguration(this); set => PhysicsBody_SetMassConfiguration(this, value); }

        /// <summary>
        /// Typically a body will automatically calculate the <see cref="PhysicsBody.MassConfiguration"/> using all the attached shapes.
        /// The <see cref="PhysicsBody.MassConfiguration"/> is automatically updated whenever a <see cref="PhysicsShape"/> is added, removed or modified.
        /// When adding many shapes to a body, you can choose to stop this automatic calculation, therefore improving performance, by disabling <see cref="PhysicsShapeDefinition.startMassUpdate"/> for each shape being added to the body.
        /// This call will result in the <see cref="PhysicsBody.MassConfiguration"/> being calculated using the currently added <see cref="PhysicsShape"/> so is typically called after many shapes are added if they have <see cref="PhysicsShapeDefinition.startMassUpdate"/> disabled.
        /// Alternately, if you wish to assign your own <see cref="PhysicsBody.MassConfiguration"/> then disabling the automatic calculation also makes sense.
        /// In either case, you must call this method or set <see cref="PhysicsBody.massConfiguration"/> before any simulation step occurs otherwise the <see cref="PhysicsBody"/> will exhibit unstable collision behaviour.
        /// The <see cref="PhysicsBody.MassConfiguration"/> will be overwritten when calling <see cref="PhysicsBody.ApplyMassFromShapes"/>, if <see cref="PhysicsBody.massConfiguration"/> is set or when adding, removing or changing <see cref="PhysicsShape"/> with <see cref="PhysicsShapeDefinition.startMassUpdate"/> enabled.
        /// </summary>
        public readonly void ApplyMassFromShapes() => PhysicsBody_ApplyMassFromShapes(this);

        /// <summary>
        /// The linear damping of the body. This will reduce the linear velocity over time.
        /// See <see cref="PhysicsBody.linearVelocity"/>.
        /// </summary>
        public readonly float linearDamping { get => PhysicsBody_GetLinearDamping(this); set => PhysicsBody_SetLinearDamping(this, value); }

        /// <summary>
        /// The angular damping of the body. This will reduce the angular velocity over time.
        /// See <see cref="PhysicsBody.angularVelocity"/>.
        /// </summary>
        public readonly float angularDamping { get => PhysicsBody_GetAngularDamping(this); set => PhysicsBody_SetAngularDamping(this, value); }

        /// <summary>
        /// Scales the world gravity that is applied to this body.
        /// Setting the gravity scale to zero stops any gravity being applied. Likewise, a negative value inverts gravity.
        /// See <see cref="PhysicsWorld.gravity"/>.
        /// </summary>
        public readonly float gravityScale { get => PhysicsBody_GetGravityScale(this); set => PhysicsBody_SetGravityScale(this, value); }

        /// <summary>
        /// The awake state of the body.
        /// </summary>
        public readonly bool awake { get => PhysicsBody_GetAwake(this); set => PhysicsBody_SetAwake(this, value); }

        /// <summary>
        /// The sleeping ability of the body. If false, the body will never sleep and will be woken up.
        /// See <see cref="PhysicsBody.awake"/>.
        /// </summary>
        public readonly bool sleepingAllowed { get => PhysicsBody_GetSleepingAllowed(this); set => PhysicsBody_SetSleepingAllowed(this, value); }

        /// <summary>
        /// The threshold below which the body will sleep, in meters/sec.
        /// </summary>
        public readonly float sleepThreshold { get => PhysicsBody_GetSleepThreshold(this); set => PhysicsBody_SetSleepThreshold(this, value); }

        /// <summary>
        /// A threshold used to control when continuous collision detection is used when a body moves.
        /// The value is used to compare the body linear velocity movement against the extents of all the shapes added to the body scaled by this threshold.
        /// If the movement exceeds the extents scaled by the threshold then continuous collision detection is used to stop tunneling.
        /// Lower values reduce the distance the body must move before continuous collision detection is used and can have a considerable impact on performance!
        /// Higher values increase the distance the body must move before continuous collision detection is used.
        /// Too low a threshold will result in continuous collision detection being used more often therefore affecting performance so this should be limited to specific bodies only.
        /// The default threshold is 0.5 which equates to half the total shape extents.
        /// The threshold is clamped to a range of 0.0 to 1.0 with 0.0 meaning continuous collision detection will always be used.
        /// </summary>
        public readonly float collisionThreshold { get => PhysicsBody_GetCollisionThreshold(this); set => PhysicsBody_SetCollisionThreshold(this, value); }

        /// <summary>
        /// The enabled state of the body. If false, the body and anything attached to it will not participate in the simulation.
        /// </summary>
        public readonly bool enabled { get => PhysicsBody_GetEnabled(this); set => PhysicsBody_SetEnabled(this, value); }

        /// <summary>
        /// This allows this body to bypass rotational speed limits.
        /// This should only be used for circular objects, such as wheels, balls etc.
        /// </summary>
        public readonly bool fastRotationAllowed { get => PhysicsBody_GetFastRotationAllowed(this); set => PhysicsBody_SetFastRotationAllowed(this, value); }

        /// <summary>
        /// Treat this body as high speed object that performs continuous collision detection against dynamic and kinematic bodies, but not other high speed bodies.
        /// Fast collision bodies should be used sparingly. They are not a solution for general dynamic-versus-dynamic continuous collision.
        /// </summary>
        public readonly bool fastCollisionsAllowed { get => PhysicsBody_GetFastCollisionsAllowed(this); set => PhysicsBody_SetFastCollisionsAllowed(this, value); }

        /// <summary>
        /// Apply a force at a world point.
        /// If the force is not applied at the center of mass, it will generate a torque and affect the angular velocity.
        /// </summary>
        /// <param name="force">The world force vector, usually in newtons (N)</param>
        /// <param name="point">The world position of the point of application.</param>
        /// <param name="wake">Should the body be woken up.</param>
        public readonly void ApplyForce(Vector2 force, Vector2 point, bool wake = true) => PhysicsBody_ApplyForce(this, force, point, wake);

        /// <summary>
        /// Apply a force to the center of mass.
        /// </summary>
        /// <param name="force">The world force vector, usually in newtons (N).</param>
        /// <param name="wake">Should the body be woken up.</param>
        public readonly void ApplyForceToCenter(Vector2 force, bool wake = true) => PhysicsBody_ApplyForceToCenter(this, force, wake);

        /// <summary>
        /// Apply a torque.
        /// This affects the angular velocity without affecting the linear velocity.
        /// </summary>
        /// <param name="torque">Torque, usually in N*m.</param>
        /// <param name="wake">Should the body be woken up.</param>
        public readonly void ApplyTorque(float torque, bool wake = true) => PhysicsBody_ApplyTorque(this, torque, wake);

        /// <summary>
        /// Apply an impulse at a point.
        /// This immediately modifies the velocity and also modifies the angular velocity if the point of application is not at the center of mass.
        ///	This should be used for one-shot impulses.
        ///	If you need a steady force, use a force instead, which will work better with the sub-stepping solver.
        /// </summary>
        /// <param name="impulse">The world impulse vector, usually in N*s or kg*m/s.</param>
        /// <param name="point">The world position of the point of application.</param>
        /// <param name="wake">Should the body be woken up.</param>
        public readonly void ApplyLinearImpulse(Vector2 impulse, Vector2 point, bool wake = true) => PhysicsBody_ApplyLinearImpulse(this, impulse, point, wake);

        /// <summary>
        /// Apply an impulse to the center of mass.
        /// This immediately modifies the velocity.
        ///	This should be used for one-shot impulses.
        ///	If you need a steady force, use a force instead, which will work better with the sub-stepping solver.
        /// </summary>
        /// <param name="impulse">The world impulse vector, usually in N*s or kg*m/s.</param>
        /// <param name="wake">Should the body be woken up.</param>
        public readonly void ApplyLinearImpulseToCenter(Vector2 impulse, bool wake = true) => PhysicsBody_ApplyLinearImpulseToCenter(this, impulse, wake);

        /// <summary>
        /// Apply an angular impulse.
        ///	This should be used for one-shot impulses.
        ///	If you need a steady torque, use a torque instead, which will work better with the sub-stepping solver.
        /// </summary>
        /// <param name="impulse">The angular impulse, usually in units of kg*m*m/s.</param>
        /// <param name="wake">Should the body be woken up.</param>
        public readonly void ApplyAngularImpulse(float impulse, bool wake = true) => PhysicsBody_ApplyAngularImpulse(this, impulse, wake);

        /// <summary>
        /// Apply buoyancy, flow and damping forces to the body based on how its attached shapes are submerged in a fluid plane.
        /// Forces and torques are continuous (not impulses), so this is expected to be called every simulation step.
        /// The body must be <see cref="PhysicsBody.BodyType.Dynamic"/>; otherwise a warning is logged and the call is a no-op.
        /// </summary>
        /// <param name="input">The fluid and force configuration. See <see cref="BuoyancyInput"/>.</param>
        /// <param name="deltaTime">The simulation step duration in seconds. Used to clamp damping so it cannot overshoot in a single step.</param>
        public unsafe readonly void ApplyBuoyancy(BuoyancyInput input, float deltaTime)
        {
            var body = this;
            ApplyBuoyancy(input, new ReadOnlySpan<PhysicsBody>(&body, 1), deltaTime);
        }

        /// <summary>
        /// Apply buoyancy, flow and damping forces to every body in <paramref name="bodies"/> based on how their attached shapes are submerged in a fluid plane.
        /// The same <see cref="BuoyancyInput"/> is applied to all bodies. Each body must be <see cref="PhysicsBody.BodyType.Dynamic"/>; non-dynamic or invalid bodies log a warning and are skipped.
        /// Forces and torques are continuous (not impulses), so this is expected to be called every simulation step.
        /// </summary>
        /// <param name="input">The fluid and force configuration. See <see cref="BuoyancyInput"/>.</param>
        /// <param name="bodies">The bodies that buoyancy should be applied to.</param>
        /// <param name="deltaTime">The simulation step duration in seconds. Used to clamp damping so it cannot overshoot in a single step.</param>
        public static void ApplyBuoyancy(BuoyancyInput input, ReadOnlySpan<PhysicsBody> bodies, float deltaTime)
        {
            if (deltaTime <= 0f)
                return;
            if (input.density <= 0f)
                throw new ArgumentException($"{nameof(BuoyancyInput)}.{nameof(BuoyancyInput.density)} must be greater than zero; no meaningful buoyancy force can be produced otherwise.", nameof(input));
            if (input.mask == PhysicsMask.None)
                throw new ArgumentException($"{nameof(BuoyancyInput)}.{nameof(BuoyancyInput.mask)} is empty; no shape can pass the category filter.", nameof(input));

            PhysicsBody_ApplyBuoyancy(input, bodies, deltaTime);
        }

        /// <summary>
        /// Apply buoyancy, flow and damping forces to every dynamic body shape that overlaps <paramref name="aabb"/> in <paramref name="world"/>.
        /// The same <see cref="BuoyancyInput"/> is applied to all overlapping shapes. Shapes whose body is not <see cref="PhysicsBody.BodyType.Dynamic"/> are silently skipped.
        /// Forces and torques are continuous (not impulses), so this is expected to be called every simulation step.
        /// </summary>
        /// <param name="world">The world to query for overlapping shapes.</param>
        /// <param name="aabb">The world-space axis-aligned box describing the fluid volume. Only shapes whose broadphase AABB overlaps this box are processed.</param>
        /// <param name="input">The fluid and force configuration. See <see cref="BuoyancyInput"/>.</param>
        /// <param name="deltaTime">The simulation step duration in seconds. Used to clamp damping so it cannot overshoot in a single step.</param>
        public static void ApplyBuoyancy(PhysicsWorld world, PhysicsAABB aabb, BuoyancyInput input, float deltaTime)
        {
            if (deltaTime <= 0f)
                return;
            if (input.density <= 0f)
                throw new ArgumentException($"{nameof(BuoyancyInput)}.{nameof(BuoyancyInput.density)} must be greater than zero; no meaningful buoyancy force can be produced otherwise.", nameof(input));
            if (input.mask == PhysicsMask.None)
                throw new ArgumentException($"{nameof(BuoyancyInput)}.{nameof(BuoyancyInput.mask)} is empty; no shape can pass the category filter.", nameof(input));

            PhysicsBody_ApplyBuoyancyOverlap(world, aabb, input, deltaTime);
        }

        /// <summary>
        /// Apply wind forces to this body's attached shapes.
        /// Forces are continuous (not impulses) and are computed per shape by Box2D using the drag/lift coefficients in <paramref name="input"/>; this method is expected to be called every simulation step while the body is exposed to the wind.
        /// The body must be <see cref="PhysicsBody.BodyType.Dynamic"/>; otherwise a warning is logged and the call is a no-op.
        /// Sleeping bodies are woken automatically by Box2D when the per-shape force is non-trivial.
        /// </summary>
        /// <param name="input">The wind configuration. See <see cref="WindInput"/>.</param>
        public unsafe readonly void ApplyWind(WindInput input)
        {
            var body = this;
            ApplyWind(input, new ReadOnlySpan<PhysicsBody>(&body, 1));
        }

        /// <summary>
        /// Apply wind forces to every body in <paramref name="bodies"/> by iterating each body's attached shapes.
        /// The same <see cref="WindInput"/> is applied to all bodies. Each body must be <see cref="PhysicsBody.BodyType.Dynamic"/>; non-dynamic or invalid bodies log a warning and are skipped.
        /// Forces are continuous (not impulses), so this is expected to be called every simulation step.
        /// </summary>
        /// <param name="input">The wind configuration. See <see cref="WindInput"/>.</param>
        /// <param name="bodies">The bodies that wind should be applied to.</param>
        public static void ApplyWind(WindInput input, ReadOnlySpan<PhysicsBody> bodies)
        {
            if (input.mask == PhysicsMask.None)
                throw new ArgumentException($"{nameof(WindInput)}.{nameof(WindInput.mask)} is empty; no shape can pass the category filter.", nameof(input));

            PhysicsBody_ApplyWind(input, bodies);
        }

        /// <summary>
        /// Apply wind forces to every dynamic body shape that overlaps <paramref name="aabb"/> in <paramref name="world"/>.
        /// The same <see cref="WindInput"/> is applied to all overlapping shapes. Shapes whose body is not <see cref="PhysicsBody.BodyType.Dynamic"/> are silently skipped.
        /// Forces are continuous (not impulses), so this is expected to be called every simulation step.
        /// </summary>
        /// <param name="world">The world to query for overlapping shapes.</param>
        /// <param name="aabb">The world-space axis-aligned box describing the wind volume. Only shapes whose broadphase AABB overlaps this box are processed.</param>
        /// <param name="input">The wind configuration. See <see cref="WindInput"/>.</param>
        public static void ApplyWind(PhysicsWorld world, PhysicsAABB aabb, WindInput input)
        {
            if (input.mask == PhysicsMask.None)
                throw new ArgumentException($"{nameof(WindInput)}.{nameof(WindInput.mask)} is empty; no shape can pass the category filter.", nameof(input));

            PhysicsBody_ApplyWindOverlap(world, aabb, input);
        }

        /// <summary>
        /// Clear any user forces that have been applied to this body.
        /// Forces on a body are automatically cleared when a simulation step completes, however under some circumstances it may be desirable to clear the forces explicitly.
        /// </summary>
        public readonly void ClearForces() => PhysicsBody_ClearForces(this);

        /// <summary>
        /// Wake any bodies that are touching this body via their shapes.
        /// This also works for Static bodies.
        /// </summary>
        public readonly void WakeTouching() => PhysicsBody_WakeTouching(this);

        /// <summary>
        /// Enable/disable contact events on all shapes attached to the body.
        /// See <see cref="PhysicsShape.contactEvents"/>.
        /// </summary>
        /// <param name="contactEvents">Whether contact events are allowed on all shapes attached to this body or not.</param>
        public readonly void SetContactEvents(bool contactEvents) => PhysicsBody_SetContactEvents(this, contactEvents);

        /// <summary>
        /// Enable/disable hit events on all shapes attached to the body.
        /// See <see cref="PhysicsShape.hitEvents"/>.
        /// </summary>
        /// <param name="hitEvents">Whether hit events are allowed on all shapes attached to this body or not.</param>
        public readonly void SetHitEvents(bool hitEvents) => PhysicsBody_SetHitEvents(this, hitEvents);

        /// <summary>
        /// Set the owner object using the specified owner key.
        /// You can only set the owner once, multiple attempts will produce a warning.
        /// This call does not bind the lifetime of the specified owner object, it is simply a reference.
        /// Whilst it is valid to not specify an owner object (NULL), it is recommended for debugging purposes.
        /// </summary>
        /// <param name="bodies">The bodies to set ownership for.</param>
        /// <param name="owner">The object that owns this key. Whilst it is valid to not specify an owner object (NULL), it is recommended for debugging purposes.</param>
        /// <param name="ownerKey">The owner key to be used. The value must be non-zero. You can use <see cref="PhysicsWorld.CreateOwnerKey(UnityEngine.Object)"/> for this value although any non-zero integer will work.</param>
        public static void SetOwner(ReadOnlySpan<PhysicsBody> bodies, UnityEngine.Object owner, int ownerKey) => PhysicsBody_SetOwner(bodies, owner, ownerKey);

        /// <summary>
        /// Set the owner object using the specified owner key.
        /// You can only set the owner once, multiple attempts will produce a warning.
        /// This call does not bind the lifetime of the specified owner object, it is simply a reference.
        /// It is also valid to not specify an owner object (NULL) to simply gain an owner key however it can be useful, if simply for debugging purposes and discovery, to know which object is the owner.
        /// </summary>
        /// <param name="owner">The object that owns this key. This can be NULL if not required but is recommended as the key is formed in part by the hash-code of the owner object.</param>
        /// <param name="ownerKey">The owner key to be used. If zero then a new owner key is created. You can use <see cref="PhysicsWorld.CreateOwnerKey(UnityEngine.Object)"/> for this value although any non-zero integer will work.</param>
        public unsafe readonly void SetOwner(UnityEngine.Object owner, int ownerKey)
        {
            var body = this;
            SetOwner(new ReadOnlySpan<PhysicsBody>(&body, 1), owner, ownerKey);
        }

        /// <summary>
        /// Set the owner object using the specified owner key.
        /// You can only set the owner once, multiple attempts will produce a warning.
        /// This call does not bind the lifetime of the specified owner object, it is simply a reference.
        /// It is also valid to not specify an owner object (NULL) to simply gain an owner key however it can be useful, if simply for debugging purposes and discovery, to know which object is the owner.
        /// </summary>
        /// <param name="owner">The object that owns this key. This can be NULL if not required but is recommended as the key is formed in part by the hash-code of the owner object.</param>
        /// <returns>The owner key assigned.</returns>
        public readonly int SetOwner(UnityEngine.Object owner)
        {
            var ownerKey = PhysicsWorld.CreateOwnerKey(owner);
            SetOwner(owner, ownerKey);
            return ownerKey;
        }

        /// <summary>
        /// Get the owner object associated with this body as specified using <see cref="PhysicsBody.SetOwner(UnityEngine.Object)"/>.
        /// </summary>
        /// <returns>The owner object associated with this body or NULL if no owner has been specified.</returns>
        public readonly UnityEngine.Object GetOwner() => PhysicsBody_GetOwner(this);

        /// <summary>
        /// Get if the body is owned.
        /// See <see cref="PhysicsBody.SetOwner(UnityEngine.Object)"/>.
        /// </summary>
        public readonly bool isOwned => PhysicsBody_IsOwned(this);

        /// <summary>
        /// Get/Set the <see cref="System.Object"/> that event callbacks for this body will be sent to.
        /// Care should be taken with any <see cref="System.Object"/> assigned as a callback target that isn't a <see cref="UnityEngine.Object"/> as this assignment will not in itself keep the object alive and can be garbage collected.
        /// To avoid this, you should have at least a single reference to the object in your code.
        /// To remove the object assigned here, set the callback target to NULL.
        ///
        /// This includes the following events:
        ///
        ///- A <see cref="PhysicsEvents.BodyUpdateEvent"/> with call <see cref="PhysicsCallbacks.IBodyUpdateCallback"/>.
        /// </summary>
        public readonly System.Object callbackTarget { get => PhysicsBody_GetCallbackTarget(this); set => PhysicsBody_SetCallbackTarget(this, value); }

        /// <summary>
        /// Get/Set <see cref="PhysicsUserData"/> that can be used for any purpose.
        /// The physics system doesn't use this data, it is entirely for custom use.
        /// </summary>
        public readonly PhysicsUserData userData { get => PhysicsBody_GetUserData(this); set => PhysicsBody_SetUserData(this, value); }

        /// <summary>
        /// Get <see cref="PhysicsUserData"/> that can be used for any purpose, typically by the owner only.
        /// </summary>
        public readonly PhysicsUserData ownerUserData { get => PhysicsBody_GetOwnerUserData(this); }

        /// <summary>
        /// Set <see cref="PhysicsUserData"/> that can be used for any purpose, typically by the owner only.
        /// </summary>
        /// <param name="physicsUserData">The user data to set.</param>
        /// <param name="ownerKey">Optional owner key returned when using <see cref="PhysicsBody.SetOwner(UnityEngine.Object)"/>.</param>
        public readonly void SetOwnerUserData(PhysicsUserData physicsUserData, int ownerKey = 0) => PhysicsBody_SetOwnerUserData(this, physicsUserData, ownerKey);

        /// <summary>
        /// Get/Set the transform object associated with the body.
        /// This can be used as a write transform and/or as a depth-hint for <see cref="PhysicsWorld"/> drawing.
        /// See <see cref="PhysicsBody.transformWriteMode"/>.
        /// </summary>
        public readonly Transform transformObject { get => PhysicsBody_GetTransformObject(this); set => PhysicsBody_SetTransformObject(this, value); }

        /// <summary>
        /// Get/Set how the <see cref="PhysicsBody.transformObject"/> should be written to after the simulation has completed.
        /// Transform write will only occur if it is enabled on the world using <see cref="PhysicsWorld.transformWriteMode"/>.
        /// </summary>
        public readonly PhysicsBody.TransformWriteMode transformWriteMode { get => PhysicsBody_GetTransformWriteMode(this); set => PhysicsBody_SetTransformWriteMode(this, value); }

        /// <summary>
        /// Get the number of shapes attached to this body.
        /// Use <see cref="PhysicsBody.GetShapes"/> to retrieve the shapes.
        /// </summary>
        public readonly int shapeCount => PhysicsBody_GetShapeCount(this);

        /// <summary>
        /// Get the shapes attached to this body.
        /// </summary>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The shapes attached to this body. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsShape> GetShapes(Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsBody_GetShapes(this, allocator).ToNativeArray<PhysicsShape>();

        /// <summary>
        /// Get the number of joints attached to this body.
        /// Use <see cref="PhysicsBody.GetJoints"/> to retrieve the joints.
        /// </summary>
        public readonly int jointCount => PhysicsBody_GetJointCount(this);

        /// <summary>
        /// Get the joints attached to this body.
        /// </summary>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The joints attached to this body. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsJoint> GetJoints(Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsBody_GetJoints(this, allocator).ToNativeArray<PhysicsJoint>();

        /// <summary>
        /// Get all the touching contacts this body is currently participating in. Speculative collision is used so some contact points may be separated, a property available in the provided contact manifold.
        /// </summary>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The touching contacts this body is currently participating in. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsShape.Contact> GetContacts(Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsBody_GetContacts(this, allocator).ToNativeArray<PhysicsShape.Contact>();

        /// <summary>
        /// Get the world AABB that bounds all the shapes attached to this body.
        ///	If there are no shapes attached to the body then the returned AABB is empty and centered on the body origin.
        /// </summary>
        /// <returns>The world AABB that bounds all the shapes attached to this body.</returns>
        public readonly PhysicsAABB GetAABB() => PhysicsBody_CalculateAABB(this);

        /// <summary>
        /// Get the minimum distance between all the shapes attached to this body and the specified shape.
        /// </summary>
        /// <param name="physicsShape">The shape to check the distance of.</param>
        /// <param name="useRadii">Whether to use the radii of all shapes or not.</param>
        /// <returns>The distance result.</returns>
        public readonly PhysicsQuery.DistanceResult Distance(PhysicsShape physicsShape, bool useRadii = true) => physicsShape.Distance(this, useRadii);

        #region Create Shapes

        /// <summary>
        /// Create a Circle shape, using its default definition, attached to this body.
        /// </summary>
        /// <param name="geometry">The geometry to use.</param>
        /// <returns>The created shape.</returns>
        public readonly PhysicsShape CreateShape(CircleGeometry geometry) => PhysicsShape.CreateShape(this, geometry);

        /// <summary>
        /// Create a Circle shape attached to this body.
        /// </summary>
        /// <param name="geometry">The geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <returns>The created shape.</returns>
        public readonly PhysicsShape CreateShape(CircleGeometry geometry, PhysicsShapeDefinition definition) => PhysicsShape.CreateShape(this, geometry, definition);

        /// <summary>
        /// Create a batch of Circle shapes attached to this body.
        /// </summary>
        /// <param name="geometry">The shape geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The created shapes. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsShape> CreateShapeBatch(ReadOnlySpan<CircleGeometry> geometry, PhysicsShapeDefinition definition, Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsShape.CreateShapeBatch(this, geometry, definition, allocator);

        /// <summary>
        /// Create a Polygon shape, using its default definition, attached to this body.
        /// </summary>
        /// <param name="geometry">The geometry to use.</param>
        /// <returns>The created shape.</returns>
        public readonly PhysicsShape CreateShape(PolygonGeometry geometry) => PhysicsShape.CreateShape(this, geometry);

        /// <summary>
        /// Create a Polygon shape attached to this body.
        /// </summary>
        /// <param name="geometry">The geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <returns>The created shape.</returns>
        public readonly PhysicsShape CreateShape(PolygonGeometry geometry, PhysicsShapeDefinition definition) => PhysicsShape.CreateShape(this, geometry, definition);

        /// <summary>
        /// Create a batch of Polygon shapes attached to this body.
        /// </summary>
        /// <param name="geometry">The shape geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The created shapes. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsShape> CreateShapeBatch(ReadOnlySpan<PolygonGeometry> geometry, PhysicsShapeDefinition definition, Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsShape.CreateShapeBatch(this, geometry, definition, allocator);

        /// <summary>
        /// Create a Capsule shape, using its default definition, attached to this body.
        /// </summary>
        /// <param name="geometry">The geometry to use.</param>
        /// <returns>The created shape.</returns>
        public readonly PhysicsShape CreateShape(CapsuleGeometry geometry) => PhysicsShape.CreateShape(this, geometry);

        /// <summary>
        /// Create a Capsule shape attached to this body.
        /// </summary>
        /// <param name="geometry">The geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <returns>The created shape.</returns>
        public readonly PhysicsShape CreateShape(CapsuleGeometry geometry, PhysicsShapeDefinition definition) => PhysicsShape.CreateShape(this, geometry, definition);

        /// <summary>
        /// Create a batch of Capsule shapes attached to this body.
        /// </summary>
        /// <param name="geometry">The shape geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The created shapes. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsShape> CreateShapeBatch(ReadOnlySpan<CapsuleGeometry> geometry, PhysicsShapeDefinition definition, Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsShape.CreateShapeBatch(this, geometry, definition, allocator);

        /// <summary>
        /// Create a Segment shape, using its default definition, attached to this body.
        /// </summary>
        /// <param name="geometry">The geometry to use.</param>
        /// <returns>The created shape.</returns>
        public readonly PhysicsShape CreateShape(SegmentGeometry geometry) => PhysicsShape.CreateShape(this, geometry);

        /// <summary>
        /// Create a Segment shape attached to this body.
        /// </summary>
        /// <param name="geometry">The geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <returns>The created shape.</returns>
        public readonly PhysicsShape CreateShape(SegmentGeometry geometry, PhysicsShapeDefinition definition) => PhysicsShape.CreateShape(this, geometry, definition);

        /// <summary>
        /// Create a batch of Segment shapes attached to this body.
        /// </summary>
        /// <param name="geometry">The shape geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The created shapes. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsShape> CreateShapeBatch(ReadOnlySpan<SegmentGeometry> geometry, PhysicsShapeDefinition definition, Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsShape.CreateShapeBatch(this, geometry, definition, allocator);

        /// <summary>
        /// Create a Chain Segment shape, using its default definition, attached to this body.
        /// </summary>
        /// <param name="geometry">The geometry to use.</param>
        /// <returns>The created shape.</returns>
        public readonly PhysicsShape CreateShape(ChainSegmentGeometry geometry) => PhysicsShape.CreateShape(this, geometry);

        /// <summary>
        /// Create a Chain Segment shape attached to this body.
        /// </summary>
        /// <param name="geometry">The geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <returns>The created shape.</returns>
        public readonly PhysicsShape CreateShape(ChainSegmentGeometry geometry, PhysicsShapeDefinition definition) => PhysicsShape.CreateShape(this, geometry, definition);

        /// <summary>
        /// Create a batch of Chain Segment shapes attached to this body.
        /// </summary>
        /// <param name="geometry">The shape geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The created shapes. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsShape> CreateShapeBatch(ReadOnlySpan<ChainSegmentGeometry> geometry, PhysicsShapeDefinition definition, Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsShape.CreateShapeBatch(this, geometry, definition, allocator);

        /// <summary>
        /// Create a Chain attached to this body.
        /// </summary>
        /// <param name="geometry">The geometry to use.</param>
        /// <param name="definition">The chain definition to use.</param>
        /// <returns>The created chain.</returns>
        public readonly PhysicsChain CreateChain(ChainGeometry geometry, PhysicsChainDefinition definition) => PhysicsChain.Create(this, geometry.vertices, definition);

        /// <summary>
        /// Create a Chain of multiple shapes attached to this body.
        /// </summary>
        /// <param name="vertices">The vertices that will create the ChainSegment shapes.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <returns>The created chain.</returns>
        public readonly PhysicsChain CreateChain(ReadOnlySpan<Vector2> vertices, PhysicsChainDefinition definition) => PhysicsChain.Create(this, vertices, definition);

        #endregion

        #region Debugging

        /// <summary>
        /// Controls whether this body is automatically drawn when the world is drawn.
        /// </summary>
        public readonly bool worldDrawing { get => PhysicsBody_GetWorldDrawing(this); set => PhysicsBody_SetWorldDrawing(this, value); }

        /// <summary>
        /// Draw a body that visually represents its current state in the world.
        /// </summary>
        public readonly void Draw() => PhysicsBody_Draw(this);

        #endregion
    }
}
