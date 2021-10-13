// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;
using UnityEngine.Internal;
using System.Collections.Generic;



namespace UnityEngine
{
    public enum ArticulationJointType
    {
        FixedJoint = 0,
        PrismaticJoint = 1,
        RevoluteJoint = 2,
        SphericalJoint = 3
    };

    public enum ArticulationDofLock
    {
        LockedMotion = 0,
        LimitedMotion = 1,
        FreeMotion = 2
    };

    [NativeHeader("Modules/Physics/ArticulationBody.h")]
    [StructLayout(LayoutKind.Sequential)]
    public struct ArticulationDrive
    {
        public float lowerLimit;
        public float upperLimit;
        public float stiffness;
        public float damping;
        public float forceLimit;
        public float target;
        public float targetVelocity;
    }

    [NativeHeader("Modules/Physics/ArticulationBody.h")]
    [StructLayout(LayoutKind.Sequential)]
    public struct ArticulationReducedSpace
    {
        private unsafe fixed float x[3];

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

        public unsafe ArticulationReducedSpace(float a)
        {
            x[0] = a;
            dofCount = 1;
        }

        public unsafe ArticulationReducedSpace(float a, float b)
        {
            x[0] = a;
            x[1] = b;
            dofCount = 2;
        }

        public unsafe ArticulationReducedSpace(float a, float b, float c)
        {
            x[0] = a;
            x[1] = b;
            x[2] = c;
            dofCount = 3;
        }

        public int dofCount; // currently, dofCoumt <= 3
    }

    [NativeHeader("Modules/Physics/ArticulationBody.h")]
    public struct ArticulationJacobian
    {
        private int rowsCount;
        private int colsCount;
        private List<float> matrixData;

        public ArticulationJacobian(int rows, int cols)
        {
            rowsCount = rows;
            colsCount = cols;
            matrixData = new List<float>(rows * cols);
            for (int i = 0; i < rows * cols; i++)
                matrixData.Add(0.0f);
        }

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

    [NativeHeader("Modules/Physics/ArticulationBody.h")]
    [NativeClass("Unity::ArticulationBody")]
    public class ArticulationBody : Behaviour
    {
        extern public ArticulationJointType jointType { get; set; }
        extern public Vector3 anchorPosition { get; set; }
        extern public Vector3 parentAnchorPosition { get; set; }
        extern public Quaternion anchorRotation { get; set; }
        extern public Quaternion parentAnchorRotation { get; set; }
        extern public bool isRoot { get; }

        [Obsolete("computeParentAnchor has been renamed to matchAnchors (UnityUpgradable) -> matchAnchors")]
        public bool computeParentAnchor
        {
            get
            {
                return matchAnchors;
            }
            set
            {
                matchAnchors = value;
            }
        }
        extern public bool matchAnchors { get; set; }

        extern public ArticulationDofLock linearLockX { get; set; }
        extern public ArticulationDofLock linearLockY { get; set; }
        extern public ArticulationDofLock linearLockZ { get; set; }

        extern public ArticulationDofLock swingYLock { get; set; }
        extern public ArticulationDofLock swingZLock { get; set; }
        extern public ArticulationDofLock twistLock { get; set; }

        extern public ArticulationDrive xDrive { get; set; }
        extern public ArticulationDrive yDrive { get; set; }
        extern public ArticulationDrive zDrive { get; set; }

        extern public bool immovable { get; set; }
        extern public bool useGravity { get; set; }

        extern public float linearDamping { get; set; }
        extern public float angularDamping { get; set; }
        extern public float jointFriction { get; set; }

        extern public void AddForce(Vector3 force, [DefaultValue("ForceMode.Force")] ForceMode mode);

        [ExcludeFromDocs]
        public void AddForce(Vector3 force)
        {
            AddForce(force, ForceMode.Force);
        }

        extern public void AddRelativeForce(Vector3 force, [DefaultValue("ForceMode.Force")] ForceMode mode);

        [ExcludeFromDocs]
        public void AddRelativeForce(Vector3 force)
        {
            AddRelativeForce(force, ForceMode.Force);
        }

        extern public void AddTorque(Vector3 torque, [DefaultValue("ForceMode.Force")] ForceMode mode);

        [ExcludeFromDocs]
        public void AddTorque(Vector3 torque)
        {
            AddTorque(torque, ForceMode.Force);
        }

        extern public void AddRelativeTorque(Vector3 torque, [DefaultValue("ForceMode.Force")] ForceMode mode);

        [ExcludeFromDocs]
        public void AddRelativeTorque(Vector3 torque)
        {
            AddRelativeTorque(torque, ForceMode.Force);
        }

        extern public void AddForceAtPosition(Vector3 force, Vector3 position, [DefaultValue("ForceMode.Force")] ForceMode mode);

        [ExcludeFromDocs]
        public void AddForceAtPosition(Vector3 force, Vector3 position)
        {
            AddForceAtPosition(force, position, ForceMode.Force);
        }

        extern public Vector3 velocity { get; set; }
        extern public Vector3 angularVelocity { get; set; }

        extern public float mass { get; set; }
        extern public Vector3 centerOfMass { get; set; }
        extern public Vector3 worldCenterOfMass { get; }
        extern public Vector3 inertiaTensor { get; set; }
        extern public Quaternion inertiaTensorRotation { get; set; }
        extern public void ResetCenterOfMass();
        extern public void ResetInertiaTensor();

        extern public void Sleep();
        extern public bool IsSleeping();
        extern public void WakeUp();
        extern public float sleepThreshold { get; set; }

        extern public int solverIterations { get; set; }
        extern public int solverVelocityIterations { get; set; }

        extern public float maxAngularVelocity { get; set; }
        extern public float maxLinearVelocity { get; set; }
        extern public float maxJointVelocity { get; set; }
        extern public float maxDepenetrationVelocity { get; set; }

        extern public ArticulationReducedSpace jointPosition { get; set; }
        extern public ArticulationReducedSpace jointVelocity { get; set; }
        extern public ArticulationReducedSpace jointAcceleration { get; set; }
        extern public ArticulationReducedSpace jointForce { get; set; }
        extern public ArticulationReducedSpace driveForce { get; }

        extern public int dofCount { get; }
        extern public int index { [NativeMethod("GetBodyIndex")] get; }

        extern public void TeleportRoot(Vector3 position, Quaternion rotation);
        extern public Vector3 GetClosestPoint(Vector3 point);

        extern public Vector3 GetRelativePointVelocity(Vector3 relativePoint);
        extern public Vector3 GetPointVelocity(Vector3 worldPoint);

        extern public int GetDenseJacobian(ref ArticulationJacobian jacobian);

        extern public int GetJointPositions(List<float> positions);
        extern public void SetJointPositions(List<float> positions);
        extern public int GetJointVelocities(List<float> velocities);
        extern public void SetJointVelocities(List<float> velocities);
        extern public int GetJointAccelerations(List<float> accelerations);
        extern public void SetJointAccelerations(List<float> accelerations);
        extern public int GetJointForces(List<float> forces);
        extern public void SetJointForces(List<float> forces);
        extern public int GetDriveForces(List<float> forces);
        extern public int GetDriveTargets(List<float> targets);
        extern public void SetDriveTargets(List<float> targets);
        extern public int GetDriveTargetVelocities(List<float> targetVelocities);
        extern public void SetDriveTargetVelocities(List<float> targetVelocities);
        extern public int GetDofStartIndices(List<int> dofStartIndices);

        extern public CollisionDetectionMode collisionDetectionMode { get; set; }

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

