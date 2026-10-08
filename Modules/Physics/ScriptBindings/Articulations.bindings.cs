// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using System.Runtime.InteropServices;
using UnityEngine.Internal;
using System.Collections.Generic;

namespace UnityEngine
{
    ///<summary>The type of the joint that restricts movement of the two connected articulation bodies.</summary>
    public enum ArticulationJointType
    {
        ///<summary>Fixed joint doesn't allow any relative movement of the connected bodies.</summary>
        FixedJoint = 0,
        ///<summary>Prismatic joint only allows relative translation of the connection bodies along one specified axis.</summary>
        PrismaticJoint = 1,
        ///<summary>Revolute joint allows rotational movement around the X axis of the parent's anchor.</summary>
        RevoluteJoint = 2,
        ///<summary>Spherical joint only allows relative rotations of the two connected bodies.</summary>
        ///<remarks>This joint allows twist around the body's anchor X axis, and swing in the conical angle defined in parent's anchor Y and Z axes. It's similar to ConfigurableJoint but doesn't allow linear motion.</remarks>
        SphericalJoint = 3
    };

    ///<summary>The lock type applied to a particular degree of freedom of an articulation body.</summary>
    public enum ArticulationDofLock
    {
        ///<summary>The relative motion of the two connected articulation bodies is not allowed.</summary>
        LockedMotion = 0,
        ///<summary>The relative motion of the two connected articulation bodies is limited to a certain range.</summary>
        LimitedMotion = 1,
        ///<summary>The relative motion of the two connected articulation bodies is unconstrained.</summary>
        FreeMotion = 2
    };

    ///<summary>The drive type applied to a particular drive of an <see cref="ArticulationBody" />.</summary>
    ///<seealso cref="ArticulationDrive.driveType" />
    public enum ArticulationDriveType
    {
        ///<summary>Drives in this mode output forces and torques.</summary>
        ///<remarks>Default mode for <see cref="ArticulationBody" /> drives.</remarks>
        Force = 0,
        ///<summary>Drives in this mode output accelerations instead of forces or torques.</summary>
        Acceleration = 1,
        ///<summary>Drives in this mode have a very high stiffness and track the <see cref="ArticulationDrive.target" /> almost kinematically.</summary>
        ///<remarks>The stiffness value that's used in this mode is not serialized, so switching back to a different <see cref="ArticulationDriveType" /> will use the serialized values.</remarks>
        Target = 2,
        ///<summary>Drives in this mode have a very high damping and track the <see cref="ArticulationDrive.targetVelocity" /> almost kinematically.</summary>
        ///<remarks>The damping value that's used in this mode is not serialized, so switching back to a different <see cref="ArticulationDriveType" /> will use the serialized values.</remarks>
        Velocity = 3,
    };

    ///<summary>Drive applies forces and torques to the connected bodies.</summary>
    ///<remarks>Drive moves the body along one degree of freedom, be it a linear motion along a particular axis or a rotational motion around a particular axis. The drive will apply force to the body that is calculated from the current value of the drive, using this formula: F = stiffness * (currentPosition - target) - damping * (currentVelocity - targetVelocity). In this formula, currentPosition and currentVelocity are linear position and linear velocity in case of the linear drive. In case of the rotational drive, currentPosition and currentVelocity correspond to the angle and angular velocity respectively.</remarks>
    [NativeHeader("Modules/Physics/ArticulationBody.h")]
    [StructLayout(LayoutKind.Sequential)]
    public struct ArticulationDrive
    {
        ///<summary>The lower limit of motion for a particular degree of freedom.</summary>
        ///<remarks>Units of measurement - meters for linear and degrees for angular motion.</remarks>
        public float lowerLimit;
        ///<summary>The upper limit of motion for a particular degree of freedom.</summary>
        ///<remarks>Units of measurement - meters for linear and degrees for angular motion.</remarks>
        public float upperLimit;
        ///<summary>The stiffness of the spring connected to this drive.</summary>
        ///<remarks>Units of measurement - N/m (newtons per meter) for linear and Nm/radian (newton-meters per radian) for angular motion.</remarks>
        public float stiffness;
        ///<summary>The damping of the spring attached to this drive.</summary>
        ///<remarks>Units of measurement - Ns/m (newton-seconds per meter) for linear and Nms/rad (newton-meter-seconds per radian) for angular motion.</remarks>
        public float damping;
        ///<summary>The maximum force this drive can apply to a body.</summary>
        ///<remarks>Units of measurement - N (newtons) for linear and Nm (newton-meters) for angular motion.</remarks>
        public float forceLimit;
        ///<summary>The target value the drive will try to reach.</summary>
        ///<remarks>Units of measurement - meters for linear and degrees for angular motion.</remarks>
        public float target;
        ///<summary>The velocity of the body this drive will try to reach.</summary>
        ///<remarks>Units of measurement - m/s (meters per second) for linear and rad/s (radians per second) for angular motion.</remarks>
        public float targetVelocity;
        ///<summary>Specifies which drive type to use for this drive.</summary>
        ///<seealso cref="ArticulationDriveType" />
        public ArticulationDriveType driveType;
    }

    ///<summary>Coordinates in reduced space.</summary>
    ///<remarks>The length of the internal array is equal to the amount of degrees of freedom (DoF).</remarks>
    [NativeHeader("Modules/Physics/ArticulationBody.h")]
    [StructLayout(LayoutKind.Sequential)]
    public struct ArticulationReducedSpace
    {
        private unsafe fixed float x[3];

        ///<summary>Gets the coordinate along or about a specific degree of freedom.</summary>
        public unsafe float this[int i]
        {
            get
            {
                if (i < 0 || i >= dofCount) throw new IndexOutOfRangeException();

                return x[i];
            }

            set
            {
                if (i < 0 || i >= dofCount) throw new IndexOutOfRangeException();

                x[i] = value;
            }
        }

        ///<summary>Stores coordinates in reduced space.</summary>
        ///<remarks>Used in conjunction with joint parameter accessors in reduced coordinate space, for instance: ArticulationBody.jointPosition.
        ///
        ///Currently, an articulation body can have up to three degrees of freedom:
        ///
        ///- a fixed joint has no degrees of freedom;
        ///
        ///- a revolute joint has one rotational degree of freedom -- rotation around the X axis, called twist;
        ///
        ///- a prismatic joint has one translational degree of freedom -- translation along X, Y, or Z axis;
        ///
        ///- a spherical joint has up to three, depending on the amount of unlocked motions. Currently, if only one axis is unlocked, then the amount of degrees of freedom will be reported as 1, and 3 otherwise. The order of axes is as follows: first is twist, then the two swing values.</remarks>
        ///<param name="a">Coordinate of the first degree of freedom.</param>
        public unsafe ArticulationReducedSpace(float a)
        {
            x[0] = a;
            dofCount = 1;
        }

        ///<summary>Stores coordinates in reduced space.</summary>
        ///<remarks>Used in conjunction with joint parameter accessors in reduced coordinate space, for instance: ArticulationBody.jointPosition.
        ///
        ///Currently, an articulation body can have up to three degrees of freedom:
        ///
        ///- a fixed joint has no degrees of freedom;
        ///
        ///- a revolute joint has one rotational degree of freedom -- rotation around the X axis, called twist;
        ///
        ///- a prismatic joint has one translational degree of freedom -- translation along X, Y, or Z axis;
        ///
        ///- a spherical joint has up to three, depending on the amount of unlocked motions. Currently, if only one axis is unlocked, then the amount of degrees of freedom will be reported as 1, and 3 otherwise. The order of axes is as follows: first is twist, then the two swing values.</remarks>
        ///<param name="a">Coordinate of the first degree of freedom.</param>
        ///<param name="b">Coordinate of the second degree of freedom.</param>
        public unsafe ArticulationReducedSpace(float a, float b)
        {
            x[0] = a;
            x[1] = b;
            dofCount = 2;
        }

        ///<summary>Stores coordinates in reduced space.</summary>
        ///<remarks>Used in conjunction with joint parameter accessors in reduced coordinate space, for instance: ArticulationBody.jointPosition.
        ///
        ///Currently, an articulation body can have up to three degrees of freedom:
        ///
        ///- a fixed joint has no degrees of freedom;
        ///
        ///- a revolute joint has one rotational degree of freedom -- rotation around the X axis, called twist;
        ///
        ///- a prismatic joint has one translational degree of freedom -- translation along X, Y, or Z axis;
        ///
        ///- a spherical joint has up to three, depending on the amount of unlocked motions. Currently, if only one axis is unlocked, then the amount of degrees of freedom will be reported as 1, and 3 otherwise. The order of axes is as follows: first is twist, then the two swing values.</remarks>
        ///<param name="a">Coordinate of the first degree of freedom.</param>
        ///<param name="b">Coordinate of the second degree of freedom.</param>
        ///<param name="c">Coordinate of the third degree of freedom.</param>
        public unsafe ArticulationReducedSpace(float a, float b, float c)
        {
            x[0] = a;
            x[1] = b;
            x[2] = c;
            dofCount = 3;
        }

        ///<summary>The number of degrees of freedom of a body.</summary>
        public int dofCount; // currently, dofCoumt <= 3
    }

    ///<summary>The floating point dense Jacobian matrix of the articulation body hierarchy.</summary>
    ///<remarks>Jacobian matrix is important concept used in robotics and inverse kinematics.
    ///                Multiplication with the Jacobian matrix maps the reduced coordinate space joint velocities of the articulated body to world space velocities.
    ///                Also can be used for inverse kinematics, because it can provide relation between joint velocities and end effector velocities of the articulated body.</remarks>
    ///<seealso cref="ArticulationBody.GetDenseJacobian" />
    [NativeHeader("Modules/Physics/ArticulationBody.h")]
    public struct ArticulationJacobian
    {
        private int rowsCount;
        private int colsCount;
        private List<float> matrixData;

        ///<summary>Initializes nRows X nCols Jacobian matrix to zeroes.</summary>
        ///<param name="rows">Number of matrix rows.</param>
        ///<param name="cols">Number of matrix columns.</param>
        public ArticulationJacobian(int rows, int cols)
        {
            rowsCount = rows;
            colsCount = cols;
            matrixData = new List<float>(rows * cols);
            for (int i = 0; i < rows * cols; i++)
                matrixData.Add(0.0f);
        }

        ///<summary>Gets the [row, col] element of the matrix.</summary>
        ///<param name="row">The matrix row.</param>
        ///<param name="col">The matrix column.</param>
        public float this[int row, int col]
        {
            get
            {
                if (row < 0 || row >= rowsCount)
                    throw new IndexOutOfRangeException();
                if (col < 0 || col >= colsCount)
                    throw new IndexOutOfRangeException();
                return matrixData[row * colsCount + col];
            }
            set
            {
                if (row < 0 || row >= rowsCount)
                    throw new IndexOutOfRangeException();
                if (col < 0 || col >= colsCount)
                    throw new IndexOutOfRangeException();
                matrixData[row * colsCount + col] = value;
            }
        }
        ///<summary>Number of rows of the matrix is equal to the number of articulation bodies in hierarchy times 6: 3 rows of linear/positional DOF and 3 rows of angular/rotational DOF for each body.</summary>
        public int rows
        {
            get
            {
                return rowsCount;
            }
            set
            {
                rowsCount = value;
            }
        }
        ///<summary>Number of columns of the matrix is equal to the total number of all joint degrees of freedom(DOF), plus 6 if <see cref="ArticulationBody.immovable" /> is false.</summary>
        public int columns
        {
            get
            {
                return colsCount;
            }
            set
            {
                colsCount = value;
            }
        }
        ///<summary>List of floats representing Jacobian matrix.</summary>
        public List<float> elements
        {
            get
            {
                return matrixData;
            }
            set
            {
                matrixData = value;
            }
        }
    }

    ///<summary>An axis of a drive of an ArticulationBody.</summary>
    public enum ArticulationDriveAxis
    {
        ///<summary>The ArticulationBody drive that acts about the X axis.</summary>
        X = 0,
        ///<summary>The ArticulationBody drive that acts about the Y axis.</summary>
        Y = 1,
        ///<summary>The ArticulationBody drive that acts about the Z axis.</summary>
        Z = 2
    }

    ///<summary>A body that forms part of a Physics articulation.</summary>
    ///<remarks>An articulation is a set of bodies arranged in a logical tree. The parent-child link in this tree reflects that the bodies have their relative motion constrained. Articulations are solved by a Featherstone solver that works in reduced coordinates - that is each body has relative coordinates to its parent but only along the unlocked degrees of freedom. This guarantees there is no unwanted stretch.
    ///
    ///Like with regular Joints, there are two anchors for each pair of connected articulation bodies. One anchor is defined in the parent body's reference frame, whereas the other one is defined in the child's reference frame. Changing the constraints, you directly affect the allowed space for relative positions of the two anchors. For instance, <see cref="ArticulationDofLock.LockedMotion" /> will not allow any relative motion at all.</remarks>
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Physics/ArticulationBody.h")]
    [NativeClass("Physics::ArticulationBody", PersistentTypeId = 0x0A3C9234)]
    public partial class ArticulationBody : Behaviour
    {
        internal ArticulationBody(global::UnityEngine.EntityId id) : base(id) {}
        public ArticulationBody() {}
        ///<summary>The type of joint connecting this body to its parent body.</summary>
        ///<remarks>Changing the joint type can affect the valid range for drive limits. If existing drive limits fall outside the new joint type's valid range, they are automatically clamped and a warning is logged.</remarks>
        extern public ArticulationJointType jointType { get; set; }
        ///<summary>Position of the anchor relative to this body.</summary>
        ///<remarks>Defined in this body's space.
        ///
        ///Unit of measurement - meters (m, m, m).</remarks>
        extern public Vector3 anchorPosition { get; set; }
        ///<summary>Position of the anchor relative to this body's parent.</summary>
        ///<remarks>Defined in the space of the parent articulation body, not in the space of the parent transform, as there can be multiple transform nodes in the hierarchy between two articulation bodies.
        ///
        ///Unit of measurement - meters (m, m, m).</remarks>
        extern public Vector3 parentAnchorPosition { get; set; }
        ///<summary>Rotation of the anchor relative to this body.</summary>
        ///<remarks>Defined in this body's space.
        ///
        ///Units of measurement - quaternions in Scripting and degrees in the Editor.</remarks>
        extern public Quaternion anchorRotation { get; set; }
        ///<summary>Rotation of the anchor relative to this body's parent.</summary>
        ///<remarks>Defined in the space of the parent articulation body.
        ///
        ///Units of measurement - quaternions in Scripting and degrees in the Editor.</remarks>
        extern public Quaternion parentAnchorRotation { get; set; }
        ///<summary>Indicates whether this body is the root body of the articulation (RO).</summary>
        extern public bool isRoot { get; }

        ///<summary>Whether the parent anchor should be computed automatically or not.</summary>
        ///<remarks>If enabled, the parent anchor will be positioned to match the other anchor.</remarks>
        extern public bool matchAnchors { get; set; }

        ///<summary>The type of lock along X axis of movement.</summary>
        extern public ArticulationDofLock linearLockX { get; set; }
        ///<summary>The type of lock along Y axis of movement.</summary>
        extern public ArticulationDofLock linearLockY { get; set; }
        ///<summary>The type of lock along Z axis of movement.</summary>
        extern public ArticulationDofLock linearLockZ { get; set; }

        ///<summary>The magnitude of the conical swing angle relative to Y axis.</summary>
        extern public ArticulationDofLock swingYLock { get; set; }
        ///<summary>The magnitude of the conical swing angle relative to Z axis.</summary>
        extern public ArticulationDofLock swingZLock { get; set; }
        ///<summary>The type of lock for twist movement.</summary>
        extern public ArticulationDofLock twistLock { get; set; }

        ///<summary>The properties of drive along or around X.</summary>
        ///<remarks>This can be linear drive or rotational drive.
        ///
        ///The drive's <c>lowerLimit</c> and <c>upperLimit</c> must be within the range that the physics SDK defines for this joint type. If they don't, the values are automatically clamped and a warning is logged.</remarks>
        extern public ArticulationDrive xDrive { get; set; }
        ///<summary>The properties of drive along or around Y.</summary>
        ///<remarks>This can be linear drive or rotational drive.
        ///
        ///The drive's <c>lowerLimit</c> and <c>upperLimit</c> must be within the range that the physics SDK defines for this joint type. If they don't, the values are automatically clamped and a warning is logged.</remarks>
        extern public ArticulationDrive yDrive { get; set; }
        ///<summary>The properties of drive along or around Z.</summary>
        ///<remarks>This can be linear drive or rotational drive.
        ///
        ///The drive's <c>lowerLimit</c> and <c>upperLimit</c> must be within the range that the physics SDK defines for this joint type. If they don't, the values are automatically clamped and a warning is logged.</remarks>
        extern public ArticulationDrive zDrive { get; set; }

        ///<summary>Allows you to specify that this body is not movable.</summary>
        ///<remarks>You should only set this on the root body of the articulation. For example, the base of a robotic hand.</remarks>
        extern public bool immovable { get; set; }
        ///<summary>Controls whether gravity affects this articulation body.</summary>
        ///<remarks>If you set this property to False, the articulation body will behave as if it was in outer space.</remarks>
        extern public bool useGravity { get; set; }

        ///<summary>Damping factor that affects how this body resists linear motion.</summary>
        ///<remarks>Unit of measurement - 1/s.</remarks>
        extern public float linearDamping { get; set; }
        ///<summary>Damping factor that affects how this body resists rotations.</summary>
        ///<remarks>Unit of measurement - 1/s.</remarks>
        extern public float angularDamping { get; set; }
        ///<summary>Allows you to specify the amount of friction that is applied as a result of the parent body moving relative to this body.</summary>
        ///<remarks>This works alongside drive damping, linear damping and angular damping. The joint friction is proportional to the suspended load of the joint.
        ///
        ///Unit of measurement - 1/s.</remarks>
        extern public float jointFriction { get; set; }

        ///<summary>The additional layers that all <see cref="Collider" />s attached to this <see cref="ArticulationBody" /> should exclude when deciding if the <see cref="Collider" /> can come into contact with another <see cref="Collider" />.</summary>
        ///<remarks>The Layer Collision Matrix defines which layers can contact other layers. Use this property to specify additional layers that all <see cref="Collider" />s attached to this <see cref="ArticulationBody" /> instance can't contact.
        ///
        ///When deciding which layers can contact each other, the Layer Collision Matrix first includes layers, then excludes layers. If a layer is set to be included and excluded, it is excluded.
        ///
        ///**NOTE**: Layers can be included or excluded differently depending on the settings of each <see cref="Collider" /> instance. As such, there could be a conflicting decision for whether two <see cref="Collider" /> instances can come into contact with each other. To learn how Unity decides this, see <see cref="Collider.layerOverridePriority" />.</remarks>
        ///<seealso cref="Collider.includeLayers" />
        ///<seealso cref="ArticulationBody.includeLayers" />
        ///<seealso cref="Rigidbody.includeLayers" />
        ///<seealso cref="Rigidbody.excludeLayers" />
        extern public LayerMask excludeLayers { get; set; }

        ///<summary>The additional layers that all <see cref="Collider" />s attached to this <see cref="ArticulationBody" /> should include when deciding if a the <see cref="Collider" /> can come into contact with another <see cref="Collider" />.</summary>
        ///<remarks>The Layer Collision Matrix defines which layers can contact other layers. Use this property to specify additional layers that all <see cref="Collider" />s attached to this <see cref="ArticulationBody" /> instance can contact.
        ///
        ///**NOTE**: Layers can be included or excluded differently depending on the settings of each <see cref="Collider" /> instance. As such, there could be a conflicting decision for whether two <see cref="Collider" /> instances can come into contact with each other. To learn how Unity decides this, see <see cref="Collider.layerOverridePriority" />.</remarks>
        ///<seealso cref="Collider.excludeLayers" />
        ///<seealso cref="ArticulationBody.excludeLayers" />
        ///<seealso cref="Rigidbody.includeLayers" />
        ///<seealso cref="Rigidbody.excludeLayers" />
        extern public LayerMask includeLayers { get; set; }

        ///<summary>Returns the force that the <see cref="ArticulationBody" /> has accumulated before the simulation step.</summary>
        ///<remarks>
        ///  <para>The accumulated force is reset during each physics simulation step.</para>
        ///  <para>In this example, the ArticulationBody doesn't move.</para>
        ///</remarks>
        ///<param name="step">The timestep of the next physics simulation.</param>
        ///<returns>Accumulated force expressed in <see cref="ForceMode.Force" />.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class AddForceScript : MonoBehaviour
        ///{
        ///    private ArticulationBody articulationBody;
        ///
        ///    void Start()
        ///    {
        ///        articulationBody = GetComponent<ArticulationBody>();
        ///        articulationBody.useGravity = false;
        ///    }
        ///
        ///    private void FixedUpdate()
        ///    {
        ///        articulationBody.AddForce(Vector3.up * 10f, ForceMode.Impulse);
        ///        var accumulatedForce = articulationBody.GetAccumulatedForce();
        ///        articulationBody.AddForce(accumulatedForce * -1f, ForceMode.Force);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public Vector3 GetAccumulatedForce([DefaultValue("Time.fixedDeltaTime")] float step);

        [ExcludeFromDocs]
        public Vector3 GetAccumulatedForce()
        {
            return GetAccumulatedForce(Time.fixedDeltaTime);
        }

        ///<summary>Returns the torque that the <see cref="ArticulationBody" /> has accumulated before the simulation step.</summary>
        ///<remarks>
        ///  <para>The accumulated torque is reset during each physics simulation step.</para>
        ///  <para>In this example, the angular velocity of the ArticulationBody is 0.</para>
        ///</remarks>
        ///<param name="step">The timestep of the next physics simulation.</param>
        ///<returns>Accumulated torque expressed in <see cref="ForceMode.Force" />.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class AddTorqueScript : MonoBehaviour
        ///{
        ///    private ArticulationBody articulationBody;
        ///
        ///    void Start()
        ///    {
        ///        articulationBody = GetComponent<ArticulationBody>();
        ///        articulationBody.useGravity = false;
        ///    }
        ///
        ///    private void FixedUpdate()
        ///    {
        ///        articulationBody.AddTorque(Vector3.right * 10f, ForceMode.Impulse);
        ///        var accumulatedTorque = articulationBody.GetAccumulatedTorque();
        ///        articulationBody.AddTorque(accumulatedTorque * -1f, ForceMode.Force);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public Vector3 GetAccumulatedTorque([DefaultValue("Time.fixedDeltaTime")] float step);

        [ExcludeFromDocs]
        public Vector3 GetAccumulatedTorque()
        {
            return GetAccumulatedTorque(Time.fixedDeltaTime);
        }

        ///<summary>Clears all pending forces accumulated on this <see cref="ArticulationBody" />.</summary>
        ///<remarks>Removes any forces that have been applied with <see cref="AddForce" /> since the last simulation step.
        ///Has no effect on immovable or inactive articulation bodies.</remarks>
        extern internal void ClearForce();

        ///<summary>Clears all pending torques accumulated on this <see cref="ArticulationBody" />.</summary>
        ///<remarks>Removes any torques that have been applied with <see cref="AddTorque" /> since the last simulation step.
        ///Has no effect on immovable or inactive articulation bodies.</remarks>
        extern internal void ClearTorque();

        ///<summary>Applies a force to the <see cref="ArticulationBody" />.</summary>
        ///<remarks>Note that the force accumulates over the duration of a simulation frame. It is only physically applied to the articulation body during the simulation step, after <see cref="PlayerLoop.FixedUpdate">FixedUpdate</see> has been called to scripts. Specifying the <see cref="ForceMode" /><c>mode</c> allows the type of force to be changed to an Acceleration, Impulse or Velocity Change.
        ///
        ///You can only apply a force to an active ArticulationBody. If a GameObject is inactive, AddForce has no effect. Also, the ArticulationBody must be movable (cannot be immovable).
        ///
        ///<see cref="ForceMode.Force" /> and <see cref="ForceMode.Acceleration" /> modes modify the Linear Velocity Per Second accumulator and <see cref="ForceMode.Impulse" /> and <see cref="ForceMode.VelocityChange" /> modify the Linear Velocity Per Step accumulator. Mixing these 2 groups of ForceModes doesn't work for Articulation Bodies and will result in only the Linear Velocity Per Second accumulator being applied.
        ///
        ///For more information on how ForceMode affects velocity, see <see cref="Rigidbody.AddForce" />.
        ///
        ///By default the ArticulationBody's state is set to awake when a force is applied, unless the force is <see cref="Vector3.zero" />.
        ///
        ///Unit of measurement - N (newtons).
        ///
        ///
        ///
        ///This example applies a forward force to the GameObject's ArticulationBody.</remarks>
        ///<param name="force">The force vector to apply.</param>
        ///<param name="mode">The type of force to apply.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.InputSystem;
        ///                        
        ///public class Example : MonoBehaviour
        ///{
        ///    ArticulationBody m_ArticulationBody;
        ///    public float m_Thrust = 20f;
        ///
        ///    void Start()
        ///    {
        ///        //Fetch the ArticulationBody from the GameObject with this script attached
        ///        m_ArticulationBody = GetComponent<ArticulationBody>();
        ///    }
        ///
        ///    void FixedUpdate()
        ///    {
        ///        if (Keyboard.current.spaceKey.isPressed)
        ///        {
        ///            //Apply a force to this ArticulationBody in the direction of this GameObject's up-axis
        ///            m_ArticulationBody.AddForce(transform.up * m_Thrust);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="AddForceAtPosition" />
        ///<seealso cref="AddRelativeForce" />
        ///<seealso cref="AddTorque" />
        extern public void AddForce(Vector3 force, [DefaultValue("ForceMode.Force")] ForceMode mode);

        [ExcludeFromDocs]
        public void AddForce(Vector3 force)
        {
            AddForce(force, ForceMode.Force);
        }

        ///<summary>Applies a <c>force</c> to the Articulation Body, relative to its local coordinate system.</summary>
        ///<remarks>You can only apply a force to an active ArticulationBody. If a GameObject is inactive, AddRelativeForce has no effect.
        ///
        ///<see cref="ForceMode.Force" /> and <see cref="ForceMode.Acceleration" /> modes modify the Linear Velocity Per Second accumulator and <see cref="ForceMode.Impulse" /> and <see cref="ForceMode.VelocityChange" /> modify the Linear Velocity Per Step accumulator. Mixing these 2 groups of ForceModes doesn't work for Articulation Bodies and will result in only the Linear Velocity Per Second accumulator being applied.
        ///
        ///For more information on how ForceMode affects velocity, see <see cref="Rigidbody.AddForce" />.
        ///
        ///Applying a force to an ArticulationBody wakes up that body. If the force size is zero then the ArticulationBody does not wake up.
        ///
        ///Unit of measurement - N (newtons).</remarks>
        ///<param name="force">The force vector in local coordinates.</param>
        ///<param name="mode">The type of force to apply.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        /// // Add a thrust force to push an object in its current forward
        /// // direction (to simulate a rocket motor, say).
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public float thrust;
        ///    public ArticulationBody ab;
        ///    void Start()
        ///    {
        ///        ab = GetComponent<ArticulationBody>();
        ///    }
        ///
        ///    void FixedUpdate()
        ///    {
        ///        ab.AddRelativeForce(Vector3.forward * thrust);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="AddForce" />
        ///<seealso cref="AddForceAtPosition" />
        ///<seealso cref="AddRelativeTorque" />
        extern public void AddRelativeForce(Vector3 force, [DefaultValue("ForceMode.Force")] ForceMode mode);

        [ExcludeFromDocs]
        public void AddRelativeForce(Vector3 force)
        {
            AddRelativeForce(force, ForceMode.Force);
        }

        ///<summary>Add torque to the articulation body.</summary>
        ///<remarks>You can only apply a torque to an active ArticulationBody. If a GameObject is inactive, AddTorque has no effect.
        ///
        ///<see cref="ForceMode.Force" /> and <see cref="ForceMode.Acceleration" /> modes modify the Angular Velocity Per Second accumulator and <see cref="ForceMode.Impulse" /> and <see cref="ForceMode.VelocityChange" /> modify the Angular Velocity Per Step accumulator. Mixing these 2 groups of ForceModes doesn't work for Articulation Bodies and will result in only the Angular Velocity Per Second accumulator being applied.
        ///
        ///For more information on how ForceMode affects angular velocity, see <see cref="Rigidbody.AddTorque" />.
        ///
        ///Applying a torque to an ArticulationBody wakes up that body. If the torque size is zero then the ArticulationBody does not wake up.
        ///
        ///Unit of measurement - Nm (Newtonmeters).</remarks>
        ///<param name="torque">The torque to apply.</param>
        ///<param name="mode">The type of torque to apply.</param>
        ///<example>
        ///  <code><![CDATA[
        /// // Rotate an object around its Y (upward) axis in response to
        /// // left/right controls.
        ///using UnityEngine;
        ///using UnityEngine.InputSystem;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public float torque = 10f;
        ///    public ArticulationBody ab;
        ///
        ///    void FixedUpdate()
        ///    {
        ///        float turnInput = Keyboard.current.spaceKey.isPressed ? 1f : 0f;
        ///
        ///        // Apply torque in physics loop
        ///        ab.AddTorque(Vector3.up * torque * turnInput * Time.fixedDeltaTime);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="AddRelativeTorque" />
        ///<seealso cref="AddForce" />
        extern public void AddTorque(Vector3 torque, [DefaultValue("ForceMode.Force")] ForceMode mode);

        [ExcludeFromDocs]
        public void AddTorque(Vector3 torque)
        {
            AddTorque(torque, ForceMode.Force);
        }

        ///<summary>Applies a <c>torque</c> to the articulation body, relative to its local coordinate system.</summary>
        ///<remarks>You can only apply a torque to an active ArticulationBody. If a GameObject is inactive, AddRelativeTorque has no effect.
        ///
        ///<see cref="ForceMode.Force" /> and <see cref="ForceMode.Acceleration" /> modes modify the Angular Velocity Per Second accumulator and <see cref="ForceMode.Impulse" /> and <see cref="ForceMode.VelocityChange" /> modify the Angular Velocity Per Step accumulator. Mixing these 2 groups of ForceModes doesn't work for Articulation Bodies and will result in only the Angular Velocity Per Second accumulator being applied.
        ///
        ///For more information on how ForceMode affects angular velocity, see <see cref="Rigidbody.AddTorque" />.
        ///
        ///Applying a torque to an ArticulationBody wakes up that body. If the torque size is zero then the ArticulationBody does not wake up.
        ///
        ///Unit of measurement - Nm (newton-meters).</remarks>
        ///<param name="torque">The torque vector in local coordinates.</param>
        ///<param name="mode">The type of torque to apply.</param>
        ///<example>
        ///  <code><![CDATA[
        /// // Rotate an object around its Y (upward) axis in response to
        /// // left/right controls.
        ///using UnityEngine;
        ///using UnityEngine.InputSystem;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public float torque = 10f;
        ///    public ArticulationBody ab;
        ///
        ///    void FixedUpdate()
        ///    {
        ///        float turnInput = Keyboard.current.spaceKey.isPressed ? 1f : 0f;
        ///                        
        ///        // Apply torque in physics loop
        ///        ab.AddRelativeTorque(Vector3.up * torque * turnInput * Time.fixedDeltaTime);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="AddTorque" />
        ///<seealso cref="AddRelativeForce" />
        extern public void AddRelativeTorque(Vector3 torque, [DefaultValue("ForceMode.Force")] ForceMode mode);

        [ExcludeFromDocs]
        public void AddRelativeTorque(Vector3 torque)
        {
            AddRelativeTorque(torque, ForceMode.Force);
        }

        ///<summary>Applies a <c>force</c> at a specific <c>position</c>, resulting in applying a torque and force on the object.</summary>
        ///<remarks>For realistic effects, <c>position</c> should be approximately in the range of the surface of the Articulation Body.
        ///This is ideal for simulating explosions. To create realistic explosions, apply forces over several frames instead of just one.
        ///Note that when <c>position</c> is far away from the center of the Articulation Body, the applied torque will be unrealistically large.
        ///
        ///You can only apply a force to an active ArticulationBody. If a GameObject is inactive, AddForceAtPosition has no effect.
        ///
        ///<see cref="ForceMode.Force" /> mode modifies the Linear Velocity Per Second and Angular Velicity Per Second accumulators and <see cref="ForceMode.Impulse" /> mode modifies the Linear Velocity Per Step and Angular Velocity Per Step accumulators. Mixing these 2  ForceModes doesn't work for Articulation Bodies and will result in only the Linear Velocity Per Second accumulator being applied.
        ///
        ///For more information on how ForceMode affects velocity, see <see cref="Rigidbody.AddForce" />.
        ///
        ///Applying a force to an ArticulationBody wakes up that body. If the force size is zero then the ArticulationBody does not wake up.
        ///
        ///Unit of measurement - N (newtons).
        ///
        ///**This method does not support **<see cref="ForceMode.Acceleration" />** and **<see cref="ForceMode.VelocityChange" />**.**</remarks>
        ///<param name="force">The force vector in world coordinates.</param>
        ///<param name="position">A position in world coordinates.</param>
        ///<param name="mode">The type of force to apply.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void ApplyForce(ArticulationBody body)
        ///    {
        ///        Vector3 direction = body.transform.position - transform.position;
        ///        body.AddForceAtPosition(direction.normalized, transform.position);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="AddForce" />
        ///<seealso cref="AddRelativeForce" />
        ///<seealso cref="AddTorque" />
        extern public void AddForceAtPosition(Vector3 force, Vector3 position, [DefaultValue("ForceMode.Force")] ForceMode mode);

        [ExcludeFromDocs]
        public void AddForceAtPosition(Vector3 force, Vector3 position)
        {
            AddForceAtPosition(force, position, ForceMode.Force);
        }

        ///<summary>Linear velocity of the body defined in world space.</summary>
        ///<remarks>Setting a specific velocity only affects the root articulation body.
        ///
        ///Unit of measurement - m/s (meters per second).</remarks>
        extern public Vector3 linearVelocity { get; set; }
        ///<summary>The angular velocity of the body defined in world space.</summary>
        ///<remarks>Setting a specific angular velocity only affects the root articulation body.
        ///
        ///Unit of measurement - radians per second.</remarks>
        extern public Vector3 angularVelocity { get; set; }

        ///<summary>The mass of this articulation body.</summary>
        ///<remarks>Unit of measurement - kg (kilograms).</remarks>
        extern public float mass { get; set; }
        ///<summary>Whether or not to calculate the center of mass automatically.</summary>
        ///<remarks>If enabled, the center of mass is calculated automatically based on the settings of attached <see cref="Collider" />s. If no colliders are present, the center of mass has a default value of Vector3(0,0,0).</remarks>
        extern public bool automaticCenterOfMass { get; set; }
        ///<summary>The center of mass of the body defined in local space.</summary>
        ///<remarks>Unit of measurement - meters. (m, m, m).</remarks>
        extern public Vector3 centerOfMass { get; set; }
        ///<summary>The center of mass of the body defined in world space (RO).</summary>
        extern public Vector3 worldCenterOfMass { get; }
        ///<summary>Whether or not to calculate the inertia tensor automatically.</summary>
        ///<remarks>If enabled, the inertia tensor is calculated automatically based on the settings of attached <see cref="Collider" />s. If no colliders are present, the tensor has a default value of Vector3(1,1,1).</remarks>
        extern public bool automaticInertiaTensor { get; set; }
        ///<summary>The inertia tensor of this body.</summary>
        extern public Vector3 inertiaTensor { get; set; }

        extern internal Matrix4x4 worldInertiaTensorMatrix { get; }

        ///<summary>The rotation of the inertia tensor.</summary>
        extern public Quaternion inertiaTensorRotation { get; set; }
        ///<summary>Resets the center of mass of the articulation body.</summary>
        ///<remarks>Computes the actual center of mass of the articulation body from all the attached colliders and stores it. When you call this function, the center of mass is automatically updated after any modification to the articulation body.</remarks>
        extern public void ResetCenterOfMass();
        ///<summary>Resets the inertia tensor value and rotation.</summary>
        ///<remarks>Computes the inertia tensor and the inertia tensor rotation from the colliders attached to this articulation body and stores it. When you call this method, the inertia tensor value and rotation are automatically updated after any modification of the articulation body.</remarks>
        extern public void ResetInertiaTensor();

        ///<summary>Forces an articulation body to sleep.</summary>
        ///<remarks>A common use is to call this from Awake in order to make an articulation body sleep at startup.
        ///                    See the [Rigidbodies Overview](xref:RigidbodiesOverview) in the manual for more information about Rigidbody/ArticulationBody sleeping.</remarks>
        extern public void Sleep();
        ///<summary>Indicates whether the articulation body is sleeping.</summary>
        ///<remarks>See the [Physics Overview](xref:PhysicsOverview) in the manual for more information about Rigidbody/ArticulationBody sleeping.</remarks>
        extern public bool IsSleeping();
        ///<summary>Forces an articulation body to wake up.</summary>
        ///<remarks>See the [Rigidbody overview](xref:RigidbodiesOverview) page in the manual for more information about Rigidbody/ArticulationBody sleeping.</remarks>
        extern public void WakeUp();
        ///<summary>The mass-normalized energy threshold, below which objects start going to sleep.</summary>
        extern public float sleepThreshold { get; set; }

        ///<summary>The solverIterations determines how accurately articulation body joints and collision contacts are resolved.</summary>
        ///<remarks>If you are having trouble with connected articulation bodies oscillating and behaving erratically, setting
        ///                    a higher solver iteration count may improve their stability, but is slower to compute.
        ///                    Overrides <see cref="Physics.defaultSolverIterations" />. Must be positive.</remarks>
        ///<seealso cref="ArticulationBody.solverVelocityIterations" />
        extern public int solverIterations { get; set; }
        ///<summary>The solverVelocityIterations affects how accurately articulation body joints and collision contacts are resolved during bounce.</summary>
        ///<remarks>Increasing this value will result in higher accuracy of the resulting exit velocity after an articulation body bounce.
        ///                    You can try to increase this value if you are experiencing issues with jointed articulation bodies moving too much after collisions.
        ///                    Overrides <see cref="Physics.defaultSolverVelocityIterations" />. Must be positive.</remarks>
        ///<seealso cref="ArticulationBody.solverIterations" />
        extern public int solverVelocityIterations { get; set; }

        ///<summary>The maximum angular velocity of the articulation body measured in radians per second.</summary>
        ///<remarks>The angular velocity of articulation bodies is clamped to maxAngularVelocity to avoid numerical instability with fast rotating bodies.
        ///                    The maxAngularVelocity is applied to the body before the simulation step. This means that after the simulation frame, the angular velocity might exceed the set maximum. You can override this value per articulation body to enable faster rotations on objects such as wheels.
        ///                    The default value is 50. Accepted values are 0 or greater.
        ///
        ///Unit of measurement - rad/s (radians per second).</remarks>
        extern public float maxAngularVelocity { get; set; }
        ///<summary>The maximum linear velocity of the articulation body measured in meters per second.</summary>
        ///<remarks>The linear velocity of articulation bodies is clamped to maxLinearVelocity to avoid numerical instability with fast moving bodies.
        ///The maxLinearVelocity is applied to the body before the simulation step. This means that after the simulation frame, the linear velocity might exceed the set maximum. You can override this value per articulation body.
        ///
        ///Unit of measurement - m/s (meters per second).</remarks>
        extern public float maxLinearVelocity { get; set; }
        ///<summary>The maximum joint velocity of the articulation body joint in reduced coordinates.</summary>
        ///<remarks>This value is applied to the body before the simulation step. This means that after the simulation frame the velocity might exceed the set maximum. To limit velocity more realistically from a physics perspective, use <see cref="ArticulationDrive.forceLimit" /> to limit how much force the drive applies to the joint.
        ///
        ///Units of measurement - m/s (meters per second) for linear and rad/s (radians per second) for angular motion.</remarks>
        extern public float maxJointVelocity { get; set; }
        ///<summary>The maximum velocity of an articulation body when moving out of penetrating state.</summary>
        ///<remarks>Use this property to move bodies out of a colliding state more smoothly than the default.
        ///
        ///Unit of measurement - m/s (meters per second).</remarks>
        extern public float maxDepenetrationVelocity { get; set; }

        ///<summary>The joint position in reduced coordinates.</summary>
        ///<remarks>Units of measurement - meters for linear and radians for angular motion.</remarks>
        extern public ArticulationReducedSpace jointPosition { get; set; }
        ///<summary>The joint velocity in reduced coordinates.</summary>
        ///<remarks>Units of measurement - m/s (meters per second) for linear and rad/s (radians per second) for angular motion.</remarks>
        extern public ArticulationReducedSpace jointVelocity { get; set; }
        ///<summary>The joint acceleration in reduced coordinates.</summary>
        ///<remarks>Units of measurement - m/s^2 (meters per second squared) for linear and rad/s^2 (radians per second squared) for angular motion.</remarks>
        extern public ArticulationReducedSpace jointAcceleration { get;
        [Obsolete("Setting joint accelerations is not supported in forward kinematics. To have inverse dynamics take acceleration into account, use GetJointForcesForAcceleration instead", true)]
        set; }
        ///<summary>The joint force in reduced coordinates.</summary>
        ///<remarks>Units of measurement - N (newtons) for linear and Nm (newton-meters) for angular motion.</remarks>
        extern public ArticulationReducedSpace jointForce { get; set; }
        ///<summary>The drive force in reduced coordinates.</summary>
        ///<remarks>Units of measurement - N (newtons) for linear and Nm (newton-meters) for angular motion.</remarks>
        extern public ArticulationReducedSpace driveForce { get; }

        ///<summary>The amount of degrees of freedom of a body.</summary>
        extern public int dofCount { get; }
        ///<summary>The index of the body in the hierarchy of articulation bodies.</summary>
        extern public int index { [NativeMethod("GetBodyIndex")] get; }

        ///<summary>Teleport the root body of the articulation to a new pose.</summary>
        ///<remarks>Articulations are immutable once created, so it's not possible to change positions, orientations or velocities of the articulation bodies. However, you can still move the root body of the articulation with this function. To do so, call the function with the root body of the articulation. Assign zero vectors to <see cref="ArticulationBody.linearVelocity" /> and <see cref="ArticulationBody.angularVelocity" /> of the root articulation to reset the movement after <see cref="ArticulationBody.TeleportRoot" />.</remarks>
        ///<param name="position">The new position of the root articulation body.</param>
        ///<param name="rotation">The new orientation of the root articulation body.</param>
        extern public void TeleportRoot(Vector3 position, Quaternion rotation);
        ///<summary>Return the point on the articulation body that is closest to a given one.</summary>
        ///<remarks>This returns the input point in case it was not possible to calculate the actual closest point for some reason.</remarks>
        ///<param name="point">The point of interest.</param>
        ///<returns>The point on the surfaces of all Colliders attached to this articulation body that is closest to the given one.</returns>
        extern public Vector3 GetClosestPoint(Vector3 point);

        ///<summary>The velocity relative to the articulation body at the point <c>relativePoint</c>.</summary>
        ///<remarks>Gets the velocity relative to the articulation body at the specified <c>relativePoint</c>.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public ArticulationBody ab;
        ///
        ///    void Start()
        ///    {
        ///        ab = GetComponent<ArticulationBody>();
        ///    }
        ///
        ///    // Get the velocity of a wheel, specified by its position in local space.
        ///    // This method assumes that the wheel is a child of the ArticulationBody, or that the wheel translates relative to the ArticulationBody.
        ///    Vector3 CalcWheelVelocity(Vector3 localWheelPos)
        ///    {
        ///        return ab.GetRelativePointVelocity(localWheelPos);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public Vector3 GetRelativePointVelocity(Vector3 relativePoint);
        ///<summary>Gets the velocity of the articulation body at the specified <c>worldPoint</c> in global space.</summary>
        ///<remarks>GetPointVelocity takes the angularVelocity of the articulation body into account when calculating the velocity.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public ArticulationBody ab;
        ///
        ///    void Start()
        ///    {
        ///        ab = GetComponent<ArticulationBody>();
        ///    }
        ///
        ///    // Get the velocity of a wheel, specified by its position in local space.
        ///    Vector3 CalcWheelVelocity(Vector3 localWheelPos)
        ///    {
        ///        return ab.GetPointVelocity(transform.TransformPoint(localWheelPos));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public Vector3 GetPointVelocity(Vector3 worldPoint);

        [NativeMethod("GetDenseJacobianFields")]
        extern private int GetDenseJacobianFields_Internal(ref int jacobianRowsCount, ref int jacobianColsCount, [Out,NotNull] List<float> jacobianMatrixData);

        ///<summary>Calculates and writes dense Jacobian matrix of the articulation body hierarchy to the supplied struct.</summary>
        ///<remarks>This calculates dense Jacobian matrix of the entire articulation body hierarchy starting from root articulation body.
        ///                    Number of rows <see cref="ArticulationJacobian.rows" /> of the matrix is equal to the number of articulation bodies in hierarchy times 6: 3 rows of linear/positional DOF and 3 rows of angular/rotational DOF for each body.
        ///                    Number of columns <see cref="ArticulationJacobian.columns" /> of the matrix is equal to the total number of all joints degrees of freedom(DOF), plus 6 if <see cref="ArticulationBody.immovable" /> is false.</remarks>
        ///<param name="jacobian">Supplied struct to read back and store Jacobian matrix values.</param>
        ///<returns>Number of elements in Jacobian matrix.</returns>
        ///<seealso cref="index" />
        ///<seealso cref="ArticulationJacobian" />
        ///<seealso cref="GetDofStartIndices" />
        public int GetDenseJacobian(ref ArticulationJacobian jacobian)
        {
            // Initialize matrixData if ArticulationJacobian struct was created with default constructor
            if(jacobian.elements == null)
                jacobian.elements = new List<float>();

            int rowsCount = jacobian.rows;
            int colsCount = jacobian.columns;

            int ret = GetDenseJacobianFields_Internal(ref rowsCount, ref colsCount, jacobian.elements);
            jacobian.rows = rowsCount;
            jacobian.columns = colsCount;

            return ret;
        }

        ///<summary>Reads back articulation body joint positions of the entire hierarchy to the supplied list of floats .</summary>
        ///<remarks>This returns joint positions in the reduced coordinate space for the entire articulation hierarchy starting from root to the supplied list of floats.
        ///                    Every joint position DOF is represented by one float value, however depending on the type of the articulation joint there might be zero, one or 3 DOFs per joint.
        ///                    The exact location of the data in resulting list for the specific articulation body can be found by calling <see cref="ArticulationBody.GetDofStartIndices" /> and indexing returned dofStartIndices list by the particular body index via <see cref="ArticulationBody.index" />.
        ///                    Number of degrees of freedom for the articulation body can be found using <see cref="ArticulationBody.dofCount" />.
        ///
        ///                    Units of measurement - meters for linear and radians for angular motion.</remarks>
        ///<param name="positions">Supplied list of floats to read back and store the joint positions data.</param>
        ///<returns>Total degrees of freedom for the entire hierarchy of articulation bodies.</returns>
        ///<seealso cref="index" />
        ///<seealso cref="GetDofStartIndices" />
        ///<seealso cref="dofCount" />
        ///<seealso cref="SetJointPositions" />
        extern public int GetJointPositions(List<float> positions);
        ///<summary>Assigns articulation body joint positions for the entire hierarchy of bodies.</summary>
        ///<remarks>This sets joint positions in the reduced coordinate space for the entire articulation hierarchy starting from root using the supplied list of floats.
        ///                    Every joint position DOF is represented by one float value, however depending on the type of the articulation joint there might be zero, one or 3 DOFs per joint.
        ///                    The exact location of the data to be set in the supplied list for the specific articulation body can be found by calling <see cref="ArticulationBody.GetDofStartIndices" /> and indexing returned dofStartIndices list by the particular body index via <see cref="ArticulationBody.index" />.
        ///                    Number of degrees of freedom(DOF) for the articulation body can be found using <see cref="ArticulationBody.dofCount" />.
        ///
        ///                    Units of measurement - meters for linear and radians for angular motion.</remarks>
        ///<param name="positions">Supplied list of floats used to set the joint positions.</param>
        ///<seealso cref="index" />
        ///<seealso cref="GetDofStartIndices" />
        ///<seealso cref="dofCount" />
        ///<seealso cref="GetJointPositions" />
        extern public void SetJointPositions(List<float> positions);
        ///<summary>Reads back articulation body joint velocities of the entire hierarchy to the supplied list of floats .</summary>
        ///<remarks>This returns joint velocities in the reduced coordinate space for the entire articulation hierarchy starting from root to the supplied list of floats.
        ///                    Every joint velocity DOF is represented by one float value, however depending on the type of the articulation joint there might be zero, one or 3 DOFs per joint.
        ///                    The exact location of the data in resulting list for the specific articulation body can be found by calling <see cref="ArticulationBody.GetDofStartIndices" /> and indexing returned dofStartIndices list by the particular body index via <see cref="ArticulationBody.index" />.
        ///                    Number of degrees of freedom for the articulation body can be found using <see cref="ArticulationBody.dofCount" />.
        ///
        ///                    Units of measurement - m/s (meters per second) for linear and rad/s (radians per second) for angular motion.</remarks>
        ///<param name="velocities">Supplied list of floats to read back and store the joint velocities data.</param>
        ///<returns>Total degrees of freedom for the entire hierarchy of articulation bodies.</returns>
        ///<seealso cref="index" />
        ///<seealso cref="GetDofStartIndices" />
        ///<seealso cref="dofCount" />
        ///<seealso cref="SetJointVelocities" />
        extern public int GetJointVelocities(List<float> velocities);
        ///<summary>Assigns articulation body joint velocities for the entire hierarchy of bodies.</summary>
        ///<remarks>This sets joint velocities in the reduced coordinate space for the entire articulation hierarchy starting from root using the supplied list of floats.
        ///                    Every joint velocity DOF is represented by one float value, however depending on the type of the articulation joint there might be zero, one or 3 DOFs per joint.
        ///                    The exact location of the data to be set in the supplied list for the specific articulation body can be found by calling <see cref="ArticulationBody.GetDofStartIndices" /> and indexing returned dofStartIndices list by the particular body index via <see cref="ArticulationBody.index" />.
        ///                    Number of degrees of freedom(DOF) for the articulation body can be found using <see cref="ArticulationBody.dofCount" />.
        ///
        ///                    Units of measurement - m/s (meters per second) for linear and rad/s (radians per second) for angular motion.</remarks>
        ///<param name="velocities">Supplied list of floats used to set the joint velocities.</param>
        ///<seealso cref="index" />
        ///<seealso cref="GetDofStartIndices" />
        ///<seealso cref="dofCount" />
        ///<seealso cref="GetJointVelocities" />
        extern public void SetJointVelocities(List<float> velocities);
        ///<summary>Reads back articulation body joint accelerations of the entire hierarchy to the supplied list of floats .</summary>
        ///<remarks>This returns joint accelerations in the reduced coordinate space for the entire articulation hierarchy starting from root to the supplied list of floats.
        ///                    Every joint acceleration DOF is represented by one float value, however depending on the type of the articulation joint there might be zero, one or 3 DOFs per joint.
        ///                    The exact location of the data in resulting list for the specific articulation body can be found by calling <see cref="ArticulationBody.GetDofStartIndices" /> and indexing returned dofStartIndices list by the particular body index via <see cref="ArticulationBody.index" />.
        ///                    Number of degrees of freedom for the articulation body can be found using <see cref="ArticulationBody.dofCount" />.
        ///
        ///                    Units of measurement - m/s^2 (meters per second squared) for linear and rad/s^2 (radians per second squared) for angular motion.</remarks>
        ///<param name="accelerations">Supplied list of floats to read back and store the joint accelerations data.</param>
        ///<returns>Total degrees of freedom for the entire hierarchy of articulation bodies.</returns>
        ///<seealso cref="index" />
        ///<seealso cref="GetDofStartIndices" />
        ///<seealso cref="dofCount" />
        ///<seealso cref="SetJointAccelerations" />
        extern public int GetJointAccelerations(List<float> accelerations);
        ///<summary>Reads back articulation body joint forces of the entire hierarchy to the supplied list of floats .</summary>
        ///<remarks>This returns joint forces in the reduced coordinate space for the entire articulation hierarchy starting from root to the supplied list of floats.
        ///                    Every joint force DOF is represented by one float value, however depending on the type of the articulation joint there might be zero, one or 3 DOFs per joint.
        ///                    The exact location of the data in resulting list for the specific articulation body can be found by calling <see cref="ArticulationBody.GetDofStartIndices" /> and indexing returned dofStartIndices list by the particular body index via <see cref="ArticulationBody.index" />.
        ///                    Number of degrees of freedom for the articulation body can be found using <see cref="ArticulationBody.dofCount" />.
        ///
        ///                    Units of measurement - N (newtons) for linear and Nm (newton-meters) for angular motion.</remarks>
        ///<param name="forces">Supplied list of floats to read back and store the joint forces data.</param>
        ///<returns>Total degrees of freedom for the entire hierarchy of articulation bodies.</returns>
        ///<seealso cref="index" />
        ///<seealso cref="GetDofStartIndices" />
        ///<seealso cref="dofCount" />
        ///<seealso cref="SetJointForces" />
        extern public int GetJointForces(List<float> forces);
        ///<summary>Assigns articulation body joint forces for the entire hierarchy of bodies.</summary>
        ///<remarks>This sets joint forces in the reduced coordinate space for the entire articulation hierarchy starting from root using the supplied list of floats.
        ///                    Every joint force DOF is represented by one float value, however depending on the type of the articulation joint there might be zero, one or 3 DOFs per joint.
        ///                    The exact location of the data to be set in the supplied list for the specific articulation body can be found by calling <see cref="ArticulationBody.GetDofStartIndices" /> and indexing returned dofStartIndices list by the particular body index via <see cref="ArticulationBody.index" />.
        ///                    Number of degrees of freedom(DOF) for the articulation body can be found using <see cref="ArticulationBody.dofCount" />.
        ///
        ///                    Units of measurement - N (newtons) for linear and Nm (newton-meters) for angular motion.</remarks>
        ///<param name="forces">Supplied list of floats used to set the joint forces.</param>
        ///<seealso cref="index" />
        ///<seealso cref="GetDofStartIndices" />
        ///<seealso cref="dofCount" />
        ///<seealso cref="GetJointForces" />
        extern public void SetJointForces(List<float> forces);
        ///<summary>Returns the forces required for the body to reach the provided acceleration in reduced space.</summary>
        ///<remarks>The number of DOF in the provided acceleration must match the DOF count of the inbound joint. The calculation does **not** consider gravity and therefore gravity must be counteracted with <see cref="ArticulationBody.GetJointGravityForces" />. ArticulationDrives and potential damping terms are not considered in the computation (for example, linear/angular damping or joint friction). 
        ///
        ///The returned forces can then be applied with <see cref="ArticulationBody.jointForce" />.
        ///
        ///                Units of measurement - N (newtons) for linear and Nm (newton-meters) for angular motion.</remarks>
        ///<param name="acceleration">The desired acceleration in reduced space.</param>
        ///<returns>The forces needed for the body to reach the desired acceleration in reduced space.</returns>
        extern public ArticulationReducedSpace GetJointForcesForAcceleration(ArticulationReducedSpace acceleration);
        ///<summary>Reads the entire hierarchy of Articulation Bodies and fills the supplied list of floats with Articulation Drive forces.</summary>
        ///<remarks>This returns Articulation Drive forces in the reduced coordinate space for the entire Articulation hierarchy starting from the root to the supplied list of floats.
        ///                    Every drive force DOF is represented by one float value. However, there might be zero, one or three DOFs per joint, depending on the type of the articulation joint.
        ///                    To find the exact location of the data in the resulting list for the specific Articulation Body, call <see cref="ArticulationBody.GetDofStartIndices" /> and index the returned dofStartIndices list by the particular body index with <see cref="ArticulationBody.index" />.
        ///                    To find the number of DOF for the Articulation Body, use <see cref="ArticulationBody.dofCount" />.</remarks>
        ///<param name="forces">Supplied list of floats to store the drive force data.</param>
        ///<returns>Total degrees of freedom (DOF) for the entire hierarchy of Articulation Bodies.</returns>
        ///<seealso cref="index" />
        ///<seealso cref="GetDofStartIndices" />
        ///<seealso cref="dofCount" />
        ///<seealso cref="SetJointForces" />
        extern public int GetDriveForces(List<float> forces);
        ///<summary>Fills the supplied list of floats with forces required to counteract gravity for each Articulation Body in the articulation.</summary>
        ///<remarks>This returns the forces required to counteract gravity in the reduced coordinate space for the entire articulation hierarchy starting from root to the supplied list of floats.
        ///                    Every joint drive force DOF is represented by one float value. Depending on the type of the articulation joint there might be zero, one or three DOFs per joint.
        ///                    To find the exact location of the data in the resulting list for the specific articulation body, call <see cref="ArticulationBody.GetDofStartIndices" /> and index the returned dofStartIndices list by the particular body index via <see cref="ArticulationBody.index" />.
        ///                   To find the number of DOF for the articulation body, use <see cref="ArticulationBody.dofCount" />. 
        ///
        ///ArticulationDrives and potential damping terms are not considered in the computation (for example, linear/angular damping or joint friction). 
        ///
        ///
        ///                    Units of measurement - N (newtons) for linear and Nm (newton-meters) for angular motion.</remarks>
        ///<param name="forces">Supplied list of floats to store the counteracting gravity force data.</param>
        ///<returns>Total degrees of freedom (DOF) for the entire hierarchy of articulation bodies.</returns>
        ///<seealso cref="index" />
        ///<seealso cref="GetDofStartIndices" />
        ///<seealso cref="dofCount" />
        ///<seealso cref="SetDriveTargets" />
        extern public int GetJointGravityForces(List<float> forces);
        ///<summary>Fills the supplied list of floats with the forces required to counteract the Coriolis and centrifugal forces for each Articulation Body in the articulation.</summary>
        ///<remarks>This returns the forces required to counteract the Coriolis and centrifugal forces in the reduced coordinate space for the entire articulation hierarchy starting from root to the supplied list of floats.
        ///                    Every joint drive force DOF is represented by one float value. Depending on the type of the articulation joint there might be zero, one or three DOFs per joint.
        ///                    To find the exact location of the data in the resulting list for the specific articulation body, call <see cref="ArticulationBody.GetDofStartIndices" /> and index the returned dofStartIndices list by the particular body index via <see cref="ArticulationBody.index" />.
        ///                   To find the number of DOF for the articulation body, use <see cref="ArticulationBody.dofCount" />. 
        ///
        ///ArticulationDrives and potential damping terms are not considered in the computation (for example, linear/angular damping or joint friction).</remarks>
        ///<param name="forces">Supplied list of floats to store the counteracting Coriolis/centrifugal force data.</param>
        ///<returns>Total degrees of freedom (DoF) for the entire hierarchy of articulation bodies.</returns>
        ///<seealso cref="index" />
        ///<seealso cref="GetDofStartIndices" />
        ///<seealso cref="dofCount" />
        ///<seealso cref="SetDriveTargets" />
        extern public int GetJointCoriolisCentrifugalForces(List<float> forces);
        ///<summary>Fills the supplied list of floats with forces required to counteract any existing external forces (applied using <see cref="ArticulationBody.AddForce" /> or <see cref="ArticulationBody.AddTorque" />) for each Articulation Body in the articulation.</summary>
        ///<remarks>This returns the forces required to counteract external forces in the reduced coordinate space for the entire articulation hierarchy starting from root to the supplied list of floats. This function utilizes the <see cref="ArticulationBody.GetAccumulatedForce" /> and <see cref="ArticulationBody.GetAccumulatedTorque" /> methods. As such, you must supply the timestep of the next simulation. 
        ///
        ///                    Every joint drive force DOF is represented by one float value. Depending on the type of the articulation joint, a joint can have zero, one or three DOFs.
        ///                    To find the exact location of the data in the resulting list for the specific articulation body, call <see cref="ArticulationBody.GetDofStartIndices" /> and index the returned dofStartIndices list by the particular body index via <see cref="ArticulationBody.index" />.
        ///                   To find the number of DOF for the articulation body, use <see cref="ArticulationBody.dofCount" />. 
        ///
        ///Gravity, ArticulationDrives and potential damping terms are not considered in the computation (for example, linear/angular damping or joint friction).</remarks>
        ///<param name="forces">Supplied list of floats to store the counteracting external force data.</param>
        ///<param name="step">The timestep of the next physics simulation.</param>
        ///<returns>Total degrees of freedom (DOF) for the entire hierarchy of articulation bodies.</returns>
        ///<seealso cref="index" />
        ///<seealso cref="GetDofStartIndices" />
        ///<seealso cref="dofCount" />
        ///<seealso cref="SetDriveTargets" />
        extern public int GetJointExternalForces(List<float> forces, float step);

        ///<summary>Reads back articulation body joint drive targets of the entire hierarchy to the supplied list of floats.</summary>
        ///<remarks>This returns joint drive targets in the reduced coordinate space for the entire articulation hierarchy starting from root to the supplied list of floats.
        ///                    Every joint drive target DOF is represented by one float value, however depending on the type of the articulation joint there might be zero, one or 3 DOFs per joint.
        ///                    The exact location of the data in resulting list for the specific articulation body can be found by calling <see cref="ArticulationBody.GetDofStartIndices" /> and indexing returned dofStartIndices list by the particular body index via <see cref="ArticulationBody.index" />.
        ///                    Number of degrees of freedom for the articulation body can be found using <see cref="ArticulationBody.dofCount" />.
        ///
        ///                    Units of measurement - meters for linear and radians for angular motion.</remarks>
        ///<param name="targets">Supplied list of floats to read back and store the joint drive targets data.</param>
        ///<returns>Total degrees of freedom for the entire hierarchy of articulation bodies.</returns>
        ///<seealso cref="index" />
        ///<seealso cref="GetDofStartIndices" />
        ///<seealso cref="dofCount" />
        ///<seealso cref="SetDriveTargets" />
        extern public int GetDriveTargets(List<float> targets);
        ///<summary>Assigns articulation body joint drive targets for the entire hierarchy of bodies.</summary>
        ///<remarks>This sets joint drive targets in the reduced coordinate space for the entire articulation hierarchy starting from root using the supplied list of floats.
        ///                    Every joint drive target DOF is represented by one float value, however depending on the type of the articulation joint there might be zero, one or 3 DOFs per joint.
        ///                    The exact location of the data to be set in the supplied list for the specific articulation body can be found by calling <see cref="ArticulationBody.GetDofStartIndices" /> and indexing returned dofStartIndices list by the particular body index via <see cref="ArticulationBody.index" />.
        ///                    Number of degrees of freedom(DOF) for the articulation body can be found using <see cref="ArticulationBody.dofCount" />.
        ///
        ///                    Units of measurement - meters for linear and radians for angular motion.</remarks>
        ///<param name="targets">Supplied list of floats used to set the joint drive targets.</param>
        ///<seealso cref="index" />
        ///<seealso cref="GetDofStartIndices" />
        ///<seealso cref="dofCount" />
        ///<seealso cref="GetDriveTargets" />
        extern public void SetDriveTargets(List<float> targets);
        ///<summary>Reads back articulation body joint drive target velocities of the entire hierarchy to the supplied list of floats .</summary>
        ///<remarks>This returns joint drive target velocities in the reduced coordinate space for the entire articulation hierarchy starting from root to the supplied list of floats.
        ///                    Every joint target velocity DOF is represented by one float value, however depending on the type of the articulation joint there might be zero, one or 3 DOFs per joint.
        ///                    The exact location of the data in resulting list for the specific articulation body can be found by calling <see cref="ArticulationBody.GetDofStartIndices" /> and indexing returned dofStartIndices list by the particular body index via <see cref="ArticulationBody.index" />.
        ///                    Number of degrees of freedom for the articulation body can be found using <see cref="ArticulationBody.dofCount" />.
        ///
        ///                    Units of measurement - m/s (meters per second) for linear and rad/s (radians per second) for angular motion.</remarks>
        ///<param name="targetVelocities">Supplied list of floats to read back and store the joint drive target velocities data.</param>
        ///<returns>Total degrees of freedom for the entire hierarchy of articulation bodies.</returns>
        ///<seealso cref="index" />
        ///<seealso cref="GetDofStartIndices" />
        ///<seealso cref="dofCount" />
        ///<seealso cref="SetDriveTargets" />
        extern public int GetDriveTargetVelocities(List<float> targetVelocities);
        ///<summary>Assigns articulation body joint drive target velocities for the entire hierarchy of bodies.</summary>
        ///<remarks>This sets joint drive target velocities in the reduced coordinate space for the entire articulation hierarchy starting from root using the supplied list of floats.
        ///                    Every joint drive target velocity DOF is represented by one float value, however depending on the type of the articulation joint there might be zero, one or 3 DOFs per joint.
        ///                    The exact location of the data to be set in the supplied list for the specific articulation body can be found by calling <see cref="ArticulationBody.GetDofStartIndices" /> and indexing returned dofStartIndices list by the particular body index via <see cref="ArticulationBody.index" />.
        ///                    Number of degrees of freedom(DOF) for the articulation body can be found using <see cref="ArticulationBody.dofCount" />.
        ///
        ///                    Units of measurement - m/s (meters per second) for linear and rad/s (radians per second) for angular motion.</remarks>
        ///<param name="targetVelocities">Supplied list of floats used to set the joint drive target velocities.</param>
        ///<seealso cref="index" />
        ///<seealso cref="GetDofStartIndices" />
        ///<seealso cref="dofCount" />
        ///<seealso cref="GetDriveTargetVelocities" />
        extern public void SetDriveTargetVelocities(List<float> targetVelocities);
        ///<summary>Calculates and reads back reduced coordinate data start indexes in reduced coordinate data buffer for every articulation body in the hierarchy.</summary>
        ///<remarks>In order to read back or set entire articulation hierarchy data in reduced coordinates where every degree of freedom is represented by float value, one needs to find the location of reduced cordinates data for particular ArticulationBody.
        ///                    This can be achieved by calling <see cref="ArticulationBody.GetDofStartIndices" /> and indexing resulting list by the particular body index via <see cref="ArticulationBody.index" />.
        ///                    Number of degrees of freedom for particular articulation body can be found using <see cref="ArticulationBody.dofCount" />.</remarks>
        ///<param name="dofStartIndices">Supplied list of integers to read back and store the data.</param>
        ///<returns>Total degrees of freedom for the entire hierarchy of articulation bodies.</returns>
        ///<seealso cref="index" />
        ///<seealso cref="GetDofStartIndices" />
        ///<seealso cref="dofCount" />
        extern public int GetDofStartIndices(List<int> dofStartIndices);

        ///<summary>Sets the target value of the specified drive.</summary>
        ///<param name="axis">The drive axis.</param>
        ///<param name="value">The value to set the target to.</param>
        ///<seealso cref="ArticulationDrive.target" />
        extern public void SetDriveTarget(ArticulationDriveAxis axis, float value);
        ///<summary>Sets the target velocity value of the specified drive.</summary>
        ///<param name="axis">The drive axis.</param>
        ///<param name="value">The value to set the target velocity to.</param>
        ///<seealso cref="ArticulationDrive.targetVelocity" />
        extern public void SetDriveTargetVelocity(ArticulationDriveAxis axis, float value);
        ///<summary>Sets the lower and upper limits of the drive.</summary>
        ///<param name="axis">The drive axis.</param>
        ///<param name="lower">The lower limit of the drive.</param>
        ///<param name="upper">The upper limit of the drive.</param>
        ///<seealso cref="ArticulationDrive.lowerLimit" />
        ///<seealso cref="ArticulationDrive.upperLimit" />
        extern public void SetDriveLimits(ArticulationDriveAxis axis, float lower, float upper);
        ///<summary>Sets the stiffness value of the specified drive.</summary>
        ///<param name="axis">The drive axis.</param>
        ///<param name="value">The value to set the stiffness to.</param>
        ///<seealso cref="ArticulationDrive.stiffness" />
        extern public void SetDriveStiffness(ArticulationDriveAxis axis, float value);
        ///<summary>Sets the damping value of the specified drive.</summary>
        ///<param name="axis">The drive axis.</param>
        ///<param name="value">The value to set the damping to.</param>
        ///<seealso cref="ArticulationDrive.damping" />
        extern public void SetDriveDamping(ArticulationDriveAxis axis, float value);
        ///<summary>Sets the force limit of the specified drive.</summary>
        ///<param name="axis">The drive axis.</param>
        ///<param name="value">The value to set the force limit to.</param>
        ///<seealso cref="ArticulationDrive.forceLimit" />
        extern public void SetDriveForceLimit(ArticulationDriveAxis axis, float value);

        ///<summary>The ArticulationBody's collision detection mode.</summary>
        ///<remarks>Use this property to set up an ArticulationBody for continuous collision detection, in order to prevent fast moving objects from passing through other objects without detecting collisions. For best results, set this property to <see cref="CollisionDetectionMode.ContinuousDynamic" /> for fast moving objects, and set it to <see cref="CollisionDetectionMode.Continuous" /> for other objects these fast moving objects need to collide with.
        ///
        ///Note: These two options have a big impact on the physics engine processing resources. To consume fewer processing resources, you can alternatively use <see cref="CollisionDetectionMode.ContinuousSpeculative" /> (which you can also use on kinematic objects). However, if you don't have issues with collisions of fast objects, you should leave this property set to the default value <see cref="CollisionDetectionMode.Discrete" />.
        ///
        ///Restriction: You can use Continuous Collision Detection only for ArticulationBodies with Sphere-, Capsule- or BoxColliders.</remarks>
        ///<seealso cref="CollisionDetectionMode" />
        extern public CollisionDetectionMode collisionDetectionMode { get; set; }

        ///<summary>Reads the position and rotation of the Articulation Body from the physics system and applies it to the corresponding <see cref="Transform" /> component.</summary>
        ///<remarks>
        ///  <para>Note: This method doesn't update the child Transforms. It should be called from the topmost Transform, down the hierarchy.</para>
        ///  <para>Simulate a <see cref="PhysicsScene" /> with an <see cref="ArticulationBody" /> and use <see cref="PublishTransform" /> to read the information from the physics system to the <see cref="Transform" /> component.</para>
        ///</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using System.Linq;
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    private PhysicsScene m_SomeScene;
        ///    private ArticulationBody m_RootArticulation;
        ///
        ///    public void SimulateTrajectory(float totalTime)
        ///    {
        ///        m_SomeScene.RunSimulationStages(0f, SimulationStage.PrepareSimulation);
        ///
        ///        for (int i = 0; i < totalTime / Time.fixedDeltaTime; i++)
        ///            m_SomeScene.RunSimulationStages(Time.fixedDeltaTime, SimulationStage.RunSimulation);
        ///
        ///        // Transforms of the ArticulationBody tree are still like they were at the start of the method
        ///
        ///        var links = m_RootArticulation.gameObject.GetComponentsInChildren<ArticulationBody>().ToList();
        ///        links.Sort((a0, a1) => a0.index.CompareTo(a1.index));
        ///        foreach (var ab in links)
        ///            ab.PublishTransform();
        ///
        ///        // Transforms of the ArticulationBody tree are now up to date and can be accessed to see where the bodies ended up after simulating for "totalTime" seconds
        ///    }
        ///
        ///    // Teleports the root body of the Articulation and applies the resulting position and rotation to the Transform component
        ///    public void TeleportAndUpdate(Vector3 position, Quaternion rotation)
        ///    {
        ///        m_RootArticulation.TeleportRoot(position, rotation);
        ///        m_RootArticulation.PublishTransform();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public void PublishTransform();

        ///<summary>Snap the anchor to the closest contact between the connected bodies.</summary>
        ///<remarks>Computes the point on the surface of this body closest to the center of mass of the parent body and sets the anchor to it. If <see cref="ArticulationBody.computeParentAnchor" /> is set, the parent anchor will be updated accordingly too.
        ///
        ///Note that, usually, local zero is not a great default position of the anchor in case the connected bodies have colliders attached, because the joint is likely to be trying to push the bodies into each other then. To address that, this function picks a reasonable default location of the anchors that will work with many articulations.</remarks>
        public void SnapAnchorToClosestContact()
        {
            if (!transform.parent)
                return;

            // GetComponentInParent returns enabled/disabled components, need to find enabled one.
            ArticulationBody parentBody = transform.parent.GetComponentInParent<ArticulationBody>();
            while (parentBody && !parentBody.enabled)
            {
                parentBody = parentBody.transform.parent.GetComponentInParent<ArticulationBody>();
            }

            if (!parentBody)
                return;

            Vector3 com = parentBody.worldCenterOfMass;
            Vector3 closestOnSurface = GetClosestPoint(com);

            anchorPosition = transform.InverseTransformPoint(closestOnSurface);
            anchorRotation = Quaternion.FromToRotation(Vector3.right, transform.InverseTransformDirection(com - closestOnSurface).normalized);
        }
    }
}
